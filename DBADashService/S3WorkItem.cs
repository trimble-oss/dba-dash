using Amazon.S3;
using Amazon.S3.Model;
using AsyncKeyedLock;
using DBADash;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    public sealed class S3WorkItem : IWorkItem
    {
        private static readonly AsyncKeyedLocker<string> _s3Locker = new();
        public DBADashSource Source { get; set; }

        public string DedupKey => Source.ConnectionString;

        public string Schedule { get; set; }

        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

        public string Description => $"Import from {Source.ConnectionString}";

        public async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            Log.Information("Import from S3 {connection}", Source.ConnectionString);

            // One job per S3 source at a time
            using (await _s3Locker.LockAsync(Source.ConnectionString).ConfigureAwait(false))
            {
                try
                {
                    string bucket;
                    string keyPrefix;
                    if (Amazon.S3.Util.AmazonS3Uri.TryParseAmazonS3Uri(Source.ConnectionString, out var s3Uri))
                    {
                        bucket = s3Uri.Bucket;
                        keyPrefix = s3Uri.Key ?? string.Empty;
                    }
                    else
                    {
                        var endpointUri = new Uri(Source.ConnectionString, UriKind.Absolute);
                        var segments = endpointUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length == 0)
                        {
                            throw new ArgumentException("S3 source must include bucket name in path", nameof(Source.ConnectionString));
                        }
                        bucket = segments[0];
                        keyPrefix = string.Join('/', segments.Skip(1));
                    }

                    using var s3Cli = await AWSTools.GetS3ClientForEndpointAsync(config.AWSProfile, config.AccessKey, config.GetSecretKey(), Source.ConnectionString).ConfigureAwait(false);

                    var normalizedPrefix = string.IsNullOrEmpty(keyPrefix)
                        ? DestinationHandling.FileNamePrefix
                        : string.Join('/', new[] { keyPrefix.TrimEnd('/'), DestinationHandling.FileNamePrefix });

                    var request = new ListObjectsRequest
                    {
                        BucketName = bucket,
                        Prefix = normalizedPrefix
                    };

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var resp = await s3Cli.ListObjectsAsync(request).ConfigureAwait(false);
                        if (resp is { S3Objects: not null })
                        {
                            var fileList = resp.S3Objects
                                .Where(f => f.Key.EndsWith(DestinationHandling.FileExtension))
                                .Select(f => f.Key)
                                .ToList();

                            var filesByInstance = DirectoryWorkItem.GetFilesToProcessByInstance(fileList);

                            Log.Information("Processing {count} files from {prefix}. Instances: {instances}", fileList.Count, string.IsNullOrEmpty(keyPrefix) ? "(root)" : keyPrefix, filesByInstance.Count);

                            // Pass per-instance lists to tasks to avoid concurrent mutations of the same list
                            var tasks = filesByInstance
                                .Select(kv => ProcessS3FileListForCollectS3Async(kv.Value, s3Cli, bucket, Source, config))
                                .ToList();

                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }

                        if (resp?.IsTruncated == true)
                        {
                            Log.Debug("Response truncated. Processing next marker for {prefix}", keyPrefix);
                            request.Marker = resp.NextMarker;
                        }
                        else
                        {
                            request = null!;
                        }
                    }
                    while (request != null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error importing files from S3");
                }
            }
        }

        /// <summary>
        /// Process a given list of S3 files for a specific instance in order, writing collected data to DBA Dash repository database
        /// </summary>
        private static async Task ProcessS3FileListForCollectS3Async(List<string> instanceFiles, AmazonS3Client s3Cli, string bucket, DBADashSource source, CollectionConfig config)
        {
            instanceFiles.Sort(); // Ensure files are processed in order

            foreach (var s3Path in instanceFiles)
            {
                using var response = await s3Cli.GetObjectAsync(bucket, s3Path);
                await using var responseStream = response.ResponseStream;

                var ds = new DataSet();
                ds.ReadXml(responseStream);
                var id = DirectoryWorkItem.GetID(ds);
                using (await Locker.AsyncLocker.LockAsync(id))
                {
                    var fileName = Path.GetFileName(s3Path);
                    try
                    {
                        await DestinationHandling.WriteAllDestinationsAsync(ds, source, fileName, config);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex,
                            "Error importing file {filename}.  Writing file to failed message folder {folder}",
                            fileName, SchedulerServiceConfig.FailedMessageFolder);
                        await DestinationHandling.WriteFolderAsync(ds, SchedulerServiceConfig.FailedMessageFolder,
                            fileName, config);
                    }
                    finally
                    {
                        await s3Cli.DeleteObjectAsync(bucket, s3Path);
                    }
                }

                Log.Information("Imported {file}", s3Path);
            }
        }
    }
}