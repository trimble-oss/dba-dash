using Polly;
using Polly.Retry;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADash
{
    public class DestinationHandling
    {
        public const string FileNamePrefix = "DBADash_";
        public const string FileExtension = ".xml";

        /// <summary>
        /// Extension of a gzip-compressed DataSet file.  Written when <see cref="CollectionConfig.CompressionLevel"/> is not NoCompression,
        /// and read by the importer alongside <see cref="FileExtension"/>.
        /// </summary>
        public const string CompressedFileExtension = ".xml.gz";

        /// <summary>Extension used while a file is being written to a folder destination.  Renamed to the final extension once the write completes.</summary>
        public const string TempFileExtension = ".tmp";

        public const string FileSearchPattern = FileNamePrefix + "*";

        /// <summary>Whether the file name is a completed DataSet file the importer should read, compressed or not.</summary>
        public static bool IsCompletedFileName(string fileName) =>
            fileName.EndsWith(FileExtension, StringComparison.Ordinal) || IsCompressedFileName(fileName);

        /// <summary>Whether the file name is a gzip-compressed DataSet file.</summary>
        public static bool IsCompressedFileName(string fileName) =>
            fileName.EndsWith(CompressedFileExtension, StringComparison.Ordinal);

        /// <summary>
        /// The file name in the requested format.  Callers name files with <see cref="FileExtension"/>; this
        /// adds or drops the gzip extension to match <paramref name="compressed"/>.  Names that aren't DataSet
        /// files (see <see cref="IsCompletedFileName"/>) are returned unchanged.
        /// </summary>
        public static string ToFileFormat(string fileName, bool compressed)
        {
            if (!IsCompletedFileName(fileName))
            {
                return fileName;
            }
            var plainName = IsCompressedFileName(fileName) ? fileName[..^".gz".Length] : fileName;
            return compressed ? plainName + ".gz" : plainName;
        }

        /// <summary>Whether files written at this level are gzip-compressed.  <c>NoCompression</c> means plain XML.</summary>
        public static bool IsCompressed(CompressionLevel level) => level != CompressionLevel.NoCompression;

        /// <summary>
        /// The compression level to write <paramref name="fileName"/> at.  Only DataSet files (see <see cref="IsCompletedFileName"/>)
        /// are compressed.  Other files, such as the <c>.message</c> payloads an SQS reply points to, are always plain XML:
        /// the reply carries their exact path, and the agent that reads them (ResponseMessage.DownloadData) may be an
        /// older version that expects XML.
        /// </summary>
        internal static CompressionLevel CompressionLevelFor(string fileName, CompressionLevel level) =>
            IsCompletedFileName(fileName) ? level : CompressionLevel.NoCompression;

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
            CollectionRunsTable.MarkSent(ds); // Before the destinations are written in parallel, so they only read it
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
            CollectionRunsTable.MarkSent(ds);
            if (ds.Tables.Contains("DBADash") && ds.Tables["DBADash"]!.Columns.Contains("S3Path"))
            {
                ds.Tables["DBADash"].Rows[0]["S3Path"] = destination;
            }

            DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion

            // Try to parse using AmazonS3Uri first; fallback to generic Uri for MinIO etc
            string bucket;
            string keyPrefix;
            if (Amazon.S3.Util.AmazonS3Uri.TryParseAmazonS3Uri(destination, out var s3Uri))
            {
                bucket = s3Uri.Bucket;
                keyPrefix = s3Uri.Key ?? string.Empty;
            }
            else
            {
                // Fallback parsing: assume format like https://host:port/bucket[/prefix]
                var genericUri = new Uri(destination, UriKind.Absolute);
                // Split path segments, ignore leading slash
                var segments = genericUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length == 0)
                {
                    throw new ArgumentException("S3 destination must include bucket name in path", nameof(destination));
                }
                bucket = segments[0];
                keyPrefix = string.Join('/', segments.Skip(1));
            }

            using var s3Cli = await AWSTools.GetS3ClientForEndpointAsync(cfg.AWSProfile, cfg.AccessKey, cfg.GetSecretKey(), destination);

            var level = CompressionLevelFor(fileName, cfg.CompressionLevel);
            var objectName = ToFileFormat(fileName, IsCompressed(level));
            var r = new Amazon.S3.Model.PutObjectRequest()
            {
                BucketName = bucket,
                Key = string.IsNullOrEmpty(keyPrefix)
                    ? objectName
                    : string.Join('/', new[] { keyPrefix.TrimEnd('/'), objectName })
            };

            using var ms = new MemoryStream();
            WriteDataSet(ds, ms, level);
            ms.Position = 0;
            r.InputStream = ms;
            await s3Cli.PutObjectAsync(r);
        }

        /// <summary>
        /// Writes a DataSet to a file in a folder, as XML or gzip-compressed XML according to
        /// <see cref="CollectionConfig.CompressionLevel"/>.  <paramref name="fileName"/> is named with
        /// <see cref="FileExtension"/>; the extension written is adjusted to match the setting.
        /// </summary>
        /// <returns>The path of the file written, or null if the folder doesn't exist.</returns>
        public static async Task<string> WriteFolderAsync(DataSet ds, string destination, string fileName, CollectionConfig cfg)
        {
            CollectionRunsTable.MarkSent(ds);
            if (Directory.Exists(destination))
            {
                var outputName = ToFileFormat(fileName, IsCompressed(cfg.CompressionLevel));
                if (!IsCompletedFileName(outputName))
                {
                    throw new Exception("Invalid extension");
                }
                var filePath = Path.Combine(destination, outputName);

                // Write to a temp file and rename it once the write is complete.  The service importing
                // from this folder ignores anything that isn't a completed .xml or .xml.gz file, so it can't read
                // a partially written file, and a service killed mid-write leaves an inert temp file behind
                // instead of a truncated file that could never be imported.
                var tempPath = filePath + TempFileExtension;
                DataSetSerialization.SetDateTimeKind(ds); // Required to prevent timezone conversion
                try
                {
                    await using (FileStream fs = new(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        await Task.Run(() => WriteDataSet(ds, fs, cfg.CompressionLevel));
                    }
                    File.Move(tempPath, filePath, true);
                }
                catch
                {
                    TryDeleteTempFile(tempPath);
                    throw;
                }
                return filePath;
            }
            else
            {
                Log.Error("Destination Folder doesn't exist {folder}", destination);
                return null;
            }
        }

        /// <summary>
        /// Writes the DataSet as XML to <paramref name="stream"/>, gzip-compressed at <paramref name="level"/> unless the
        /// level is NoCompression, which writes plain XML.  The gzip footer is written before this returns, so the
        /// stream is complete once it does.  The stream is left open for the caller to dispose.
        /// </summary>
        internal static void WriteDataSet(DataSet ds, Stream stream, CompressionLevel level)
        {
            if (IsCompressed(level))
            {
                using var gz = new GZipStream(stream, level, leaveOpen: true);
                ds.WriteXml(gz, XmlWriteMode.WriteSchema);
            }
            else
            {
                ds.WriteXml(stream, XmlWriteMode.WriteSchema);
            }
        }

        /// <summary>
        /// Remove a temp file left behind by a failed write.  Failure to delete it isn't an error worth
        /// propagating - the service importing from the folder cleans up any temp file that gets left behind.
        /// </summary>
        private static void TryDeleteTempFile(string tempPath)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error deleting temp file {tempPath}", tempPath);
            }
        }

        public static async Task WriteDBAsync(DataSet ds, string destination, CollectionConfig cfg)
        {
            // Straight after collecting for a SQL destination - so its lag is near zero.  Kept from the first write for a file
            // imported from S3 or a folder, where the lag is the time it waited there.
            CollectionRunsTable.MarkSent(ds);
            var importAgent = DBADashAgent.GetCurrent();
            var importer = new DBImporter(ds, destination, importAgent, cfg.ImportCommandTimeout ?? CollectionConfig.DefaultImportCommandTimeout);
            // Wait until we can connect to the repository DB.  If it's down, wait for it to become available.
            await pipeline.ExecuteAsync(async _ => await importer.TestConnectionAsync(), CancellationToken.None);

            await importer.UpdateAsync();
        }
    }
}