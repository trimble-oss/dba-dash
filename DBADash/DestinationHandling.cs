using Polly;
using Polly.Retry;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class DestinationHandling
    {
        private static readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                MaxRetryAttempts = int.MaxValue, // Forever retry
                Delay = TimeSpan.FromSeconds(2),
                OnRetry = args =>
                {
                    Log.Error(args.Outcome.Exception, "Error connecting to repository database");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        public static async Task WriteAllDestinationsAsync(DataSet ds, DBADashSource src, string fileName, CollectionConfig cfg)
        {
            List<Exception> exceptions = new();
            foreach (var d in cfg.AllDestinations)
            {
                try
                {
                    if (!src.WriteToSecondaryDestinations &&
                        d != cfg.DestinationConnection) continue;
                    using var op = Operation.Begin("Write to destination {destination} from {source}",
                        d.ConnectionForPrint, src.SourceConnection.ConnectionForPrint);
                    await WriteAsync(ds, d, fileName, cfg);
                    op.Complete();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing to destination {destination} from {source}", d.ConnectionForPrint, src.SourceConnection.ConnectionForPrint);
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static async Task WriteAllDestinationsAsync(DataSet ds, string fileName, CollectionConfig cfg)
        {
            List<Exception> exceptions = new();
            var tasks = cfg.AllDestinations.Select(d => Task.Run(async () =>
                {
                    try
                    {
                        using var op = Operation.Begin("Write to destination {destination}", d.ConnectionForPrint);
                        await WriteAsync(ds, d, fileName, cfg);
                        op.Complete();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error writing to destination {destination}", d.ConnectionForPrint);
                        exceptions.Add(ex);
                    }
                }))
                .ToList();
            await Task.WhenAll(tasks);
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static async Task WriteAsync(DataSet ds, DBADashConnection d, string fileName, CollectionConfig cfg)
        {
            switch (d.Type)
            {
                case ConnectionType.AWSS3:
                    await WriteS3Async(ds, d.ConnectionString, fileName, cfg);
                    break;

                case ConnectionType.Directory:
                    await WriteFolderAsync(ds, d.ConnectionString, fileName, cfg);
                    break;

                case ConnectionType.SQL:
                    await WriteDBAsync(ds, d.ConnectionString, cfg);
                    break;
            }
        }

        public static async Task WriteS3Async(DataSet ds, string destination, string fileName, CollectionConfig cfg)
        {
            if (ds.Tables.Contains("DBADash") && ds.Tables["DBADash"]!.Columns.Contains("S3Path"))
            {
                ds.Tables["DBADash"].Rows[0]["S3Path"] = destination;
            }

            DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion

            var uri = new Amazon.S3.Util.AmazonS3Uri(destination);

            using var s3Cli = await AWSTools.GetS3ClientForEndpointAsync(cfg.AWSProfile, cfg.AccessKey, cfg.GetSecretKey(), destination);

            var r = new Amazon.S3.Model.PutObjectRequest()
            {
                BucketName = uri.Bucket,
                Key = (uri.Key + "/" + fileName).Replace("//", "/")
            };

            using var ms = new MemoryStream();
            ds.WriteXml(ms, XmlWriteMode.WriteSchema);
            r.InputStream = ms;
            await s3Cli.PutObjectAsync(r);
        }

        public static async Task WriteFolderAsync(DataSet ds, string destination, string fileName, CollectionConfig cfg)
        {
            if (Directory.Exists(destination))
            {
                var filePath = Path.Combine(destination, fileName);
                var extension = Path.GetExtension(fileName);
                if (extension == ".xml")
                {
                    await using FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion
                    await Task.Run(() => ds.WriteXml(fs, XmlWriteMode.WriteSchema));
                }
                else
                {
                    throw new Exception("Invalid extension");
                }
            }
            else
            {
                Log.Error("Destination Folder doesn't exist {folder}", destination);
            }
        }

        public static async Task WriteDBAsync(DataSet ds, string destination, CollectionConfig cfg)
        {
            var importAgent = DBADashAgent.GetCurrent();
            var importer = new DBImporter(ds, destination, importAgent, cfg.ImportCommandTimeout ?? CollectionConfig.DefaultImportCommandTimeout);
            // Wait until we can connect to the repository DB.  If it's down, wait for it to become available.
            await pipeline.ExecuteAsync(async _ => await importer.TestConnectionAsync(), CancellationToken.None);

            await importer.UpdateAsync();
        }
    }
}