using Amazon.S3.Model;
using DBADash;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADashService
{

   [DisallowConcurrentExecution, PersistJobDataAfterExecution]
    public class DBADashJob : IJob
    {
        private static readonly CollectionConfig config = SchedulerServiceConfig.Config;
        /* Ensure the Jobs collection runs once every ~24hrs.  Allowing 10mins as Jobs runs every 1hr by default */
        private static readonly int MAX_TIME_SINCE_LAST_JOB_COLLECTION = 1430;

        private static string GetID(DataSet ds)
        {
            return ds.Tables["DBADash"].Rows[0]["Instance"] + "_" + ds.Tables["DBADash"].Rows[0]["DBName"];
        }

        /// <summary>
        /// Parse Instance from filename.  File format is DBADash_YYYYMMDD_HHMM_SS_{InstanceName}_{random}.xml
        /// </summary>
        public static string ParseInstance(string fileName)
        {
            return fileName[25..fileName.LastIndexOf('_')];
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Log.Information("Processing Job : " + context.JobDetail.Key);
            var dataMap = context.JobDetail.JobDataMap;
            var cfg = JsonConvert.DeserializeObject<DBADashSource>(dataMap.GetString("CFG"));

            try
            {
                switch (cfg.SourceConnection.Type)
                {
                    case ConnectionType.Directory:
                    {
                        Log.Debug("Wait for lock {0}", context.JobDetail.Key);
                        // Ensures that this folder can only be processed by 1 job instance at a time.
                        // Note: DisallowConcurrentExecution didn't prevent triggered at startup job from overlapping with the scheduled one
                        lock (Program.Locker.GetLock(cfg.ConnectionString))
                        {
                            Log.Debug("Lock acquired {0}", context.JobDetail.Key);
                            CollectFolder(cfg);
                        }

                        break;
                    }
                    case ConnectionType.AWSS3:
                    {
                        Log.Debug("Wait for lock {0}", context.JobDetail.Key);
                        // Ensures that S3 folder can only be processed by 1 job instance at a time.
                        // Note: DisallowConcurrentExecution didn't prevent triggered at startup job from overlapping with the scheduled one
                        var semaphore =  ScheduleService.Locker.GetOrAdd(cfg.ConnectionString, _ => new SemaphoreSlim(1, 1));
                        await semaphore.WaitAsync();
                        try
                        {
                            Log.Debug("Lock acquired {0}", context.JobDetail.Key);
                            await CollectS3(cfg);
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                        break;
                    }
                    case ConnectionType.SQL:
                        CollectSQL(cfg, dataMap, context);
                        break;
                    case ConnectionType.Invalid:
                    default:
                        throw new Exception("Invalid Connection Type");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "JobExecute");
            }
        }

        /// <summary>
        /// Collect data from monitored SQL instance
        /// </summary>
        private static void CollectSQL(DBADashSource cfg, JobDataMap dataMap, IJobExecutionContext context)
        {
            var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type"));
            var collectJobs = types.Contains(CollectionType.Jobs);

            var customCollections = JsonConvert.DeserializeObject<Dictionary<string, CustomCollection>>(dataMap.GetString("CustomCollections"));
            if (collectJobs)
            {
                types = types.Where(t => t != CollectionType.Jobs).ToArray<CollectionType>(); // Remove Jobs collection - we will save this to last
            }
            try
            {
                if (types.Length > 0 || customCollections.Count > 0) // Might be zero if we are only collecting Jobs in this batch (collected in the next section)
                {
                    // Value used to disable future collections of SlowQueries if we encounter a not supported error on a RDS instance not running Standard or Enterprise edition
                    dataMap.TryGetBooleanValue("IsExtendedEventsNotSupportedException", out var dataMapExtendedEventsNotSupported);
                    var collector = new DBCollector(cfg, config.ServiceName)
                    {
                        Job_instance_id = dataMap.GetInt("Job_instance_id"),
                        IsExtendedEventsNotSupportedException = dataMapExtendedEventsNotSupported,
                    };
                    if (SchedulerServiceConfig.Config.IdentityCollectionThreshold.HasValue)
                    {
                        collector.IdentityCollectionThreshold = (int)SchedulerServiceConfig.Config.IdentityCollectionThreshold;
                    }

                    if (context.PreviousFireTimeUtc.HasValue)
                    {
                        collector.PerformanceCollectionPeriodMins = (Int32)DateTime.UtcNow.Subtract(context.PreviousFireTimeUtc.Value.UtcDateTime).TotalMinutes + 5;
                    }
                    else
                    {
                        collector.PerformanceCollectionPeriodMins = 30;
                    }
                    collector.LogInternalPerformanceCounters = SchedulerServiceConfig.Config.LogInternalPerformanceCounters;
                    using (var op = Operation.Begin("Collect {types} from instance {instance}", string.Join(", ", types.Select(s => s.ToString()).ToArray()), cfg.SourceConnection.ConnectionForPrint))
                    {
                        collector.Collect(types);
                        if (!dataMapExtendedEventsNotSupported && collector.IsExtendedEventsNotSupportedException)
                        {
                            // We encountered an error setting up extended events on a RDS instance because it's only supported for Standard and Enterprise editions.  Disable the collection
                            Log.Information("Disabling Extended events collection for {0}.  Instance type doesn't support extended events", cfg.SourceConnection.ConnectionForPrint);
                            dataMap.Put("IsExtendedEventsNotSupportedException", true);
                        }
                        dataMap.Put("Job_instance_id", collector.Job_instance_id); // Store instance_id so we can get new history only on next run
                        op.Complete();
                    }

                    if (customCollections.Count > 0)
                    {
                        using (var op = Operation.Begin("Collect Custom Collections {types} from instance {instance}",
                                   string.Join(", ", customCollections.Select(s => s.Key).ToArray()),
                                   cfg.SourceConnection.ConnectionForPrint))
                        {
                            collector.Collect(customCollections);
                            op.Complete();
                        }
                    }

                    var fileName = DBADashSource.GenerateFileName(cfg.SourceConnection.ConnectionForFileName);
                    try
                    {
                        DestinationHandling.WriteAllDestinations(collector.Data, cfg, fileName, config).Wait();

                        collector.CacheCollectedText();
                        collector.CacheCollectedPlans();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error writing {filename} to destination.  File will be copied to {folder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                        DestinationHandling.WriteFolder(collector.Data, SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                    }
                }

                if (collectJobs)
                {
                    try
                    {
                        using (var op = Operation.Begin("Collect Jobs from instance {instance}", cfg.SourceConnection.ConnectionForPrint))
                        {
                            CollectJobs(cfg, dataMap);
                            op.Complete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error running CollectJobs");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error collecting types {types} from instance {instance}", string.Join(", ", types.Select(s => s.ToString()).ToArray()), cfg.SourceConnection.ConnectionForPrint);
            }
        }

        private static void CollectJobs(DBADashSource cfg, JobDataMap dataMap)
        {
            dataMap.TryGetDateTimeValue("JobCollectDate", out var jobLastCollected);
            dataMap.TryGetDateTimeValue("JobLastModified", out var jobLastModified);
            var minsSinceLastCollection = DateTime.Now.Subtract(jobLastCollected).TotalMinutes;
            var forcedCollectionDate = jobLastCollected.AddMinutes(MAX_TIME_SINCE_LAST_JOB_COLLECTION);

            var collector = new DBCollector(cfg, config.ServiceName)
            {
                LogInternalPerformanceCounters = SchedulerServiceConfig.Config.LogInternalPerformanceCounters
            };

            // Setting the JobLastModified means we will only collect job data if jobs have been updated since the last collection.
            // This won't detect all changes - like changes to schedules.  Skip setting JobLastModified if we haven't collected in 1 day to ensure we collect at least once per day

            if (jobLastCollected == DateTime.MinValue)
            {
                Log.Debug("Skipping setting JobLastModified (First collection on startup) on {Connection}", cfg.SourceConnection.ConnectionForPrint);
            }
            else if (DateTime.Now < forcedCollectionDate)
            {
                collector.JobLastModified = jobLastModified;
                Log.Debug("Setting JobLastModified to {JobLastModified}. Forced collection will run after {ForcedCollectionDate}.  {MinsSinceLastCollection}mins since last collection ({LastCollected}) on {Connection}", jobLastModified, forcedCollectionDate, minsSinceLastCollection.ToString("N0"), jobLastCollected, cfg.SourceConnection.ConnectionForPrint);
            }
            else
            {
                Log.Debug("Skipping setting JobLastModified to {JobLastModified} - forcing job collection to run. {MinsSinceLastCollection}mins since last collection ({LastCollected}) on {Connection}.", jobLastModified, minsSinceLastCollection.ToString("N0"), jobLastCollected, cfg.SourceConnection.ConnectionForPrint);
            }

            collector.Collect(CollectionType.Jobs);
            bool containsJobs = collector.Data.Tables.Contains("Jobs");
            if (containsJobs) // Only set JobLastModified/JobCollectDate and write to destination if Jobs collection ran
            {
                // We have collected jobs data - Store JobLastModified and time we have collected the jobs.
                // Used on next run to determine if we need to refresh this data.
                dataMap.Put("JobLastModified", collector.JobLastModified);
                dataMap.Put("JobCollectDate", DateTime.Now);

                var fileName = DBADashSource.GenerateFileName(cfg.SourceConnection.ConnectionForFileName);
                try
                {
                    DestinationHandling.WriteAllDestinations(collector.Data, cfg, fileName, config).Wait();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error writing {filename} to destination.  File will be copied to {folder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                    DestinationHandling.WriteFolder(collector.Data, SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                }
            }
        }

        /// <summary>
        /// Split file list by Instance parsed from the filename.  Each instance will have 1 item in the dictionary containing a list of files to process for that instance
        /// </summary>
        private static Dictionary<string, List<string>> GetFilesToProcessByInstance(List<string> files)
        {
            Dictionary<string, List<string>> filesToProcessByInstance = new();
            foreach (var path in files)
            {
                string instance;
                try
                {
                    instance = ParseInstance(Path.GetFileName(path));
                }
                catch (Exception ex)
                {
                    instance = "default";
                    Log.Warning("Unable to parse Instance from {0}: {1}", path, ex.Message);
                }
                if (filesToProcessByInstance.TryGetValue(instance, out var value))
                {
                    value.Add(path);
                }
                else
                {
                    filesToProcessByInstance.Add(instance, new List<string> { path });
                }
            }
            return filesToProcessByInstance;
        }

        /// <summary>
        /// Get files to import from folder and process in parallel for each instance.
        /// </summary>
        private static void CollectFolder(DBADashSource cfg)
        {
            var folder = cfg.GetSource();
            Log.Logger.Information("Import from folder {folder}", folder);
            if (System.IO.Directory.Exists(folder))
            {
                try
                {
                    var files = System.IO.Directory.EnumerateFiles(folder, "DBADash_*", SearchOption.TopDirectoryOnly).Where(f => f.EndsWith(".xml")).ToList();

                    Dictionary<string, List<string>> filesToProcessByInstance = GetFilesToProcessByInstance(files);
                    // Parallel processing of files for each instance, but process the files for a given instance in order
                    Parallel.ForEach(filesToProcessByInstance, instanceItem =>
                    {
                        List<string> instanceFiles = instanceItem.Value;
                        ProcessFileListForCollectFolder(instanceFiles, cfg);
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Import from folder {folder}", folder);
                }
            }
            else
            {
                Log.Error("Source directory doesn't exist {folder}", folder);
            }
        }

        /// <summary>
        /// Process a given list of files in order for a specific instance, writing collected data to the DBADash repository database
        /// </summary>
        private static void ProcessFileListForCollectFolder(List<string> files, DBADashSource cfg)
        {
            files.Sort(); // Ensure we process files in order
            foreach (string f in files)
            {
                Log.Information("Processing file {0}", f);
                var fileName = Path.GetFileName(f);
                try
                {
                    var ds = DataSetSerialization.DeserializeFromFile(f);
                    lock (Program.Locker.GetLock(GetID(ds)))
                    {
                        DestinationHandling.WriteAllDestinations(ds, cfg, fileName, config).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error importing from {filename}.  File will be copied to {failedmessagefolder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                    File.Copy(f, Path.Combine(SchedulerServiceConfig.FailedMessageFolder, f));
                }
                finally
                {
                    System.IO.File.Delete(f);
                }
            }
        }

        /// <summary>
        /// Process The S3 bucket source.  Run a separate thread per instance and process the files for each instance sequentially in the order they were collected
        /// </summary>
        private static async Task CollectS3(DBADashSource cfg)
        {
            Log.Information("Import from S3 {connection}", cfg.ConnectionString);
            try
            {
                var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.ConnectionString);
                using var s3Cli = await AWSTools.GetAWSClientAsync(config.AWSProfile, config.AccessKey, config.GetSecretKey(), uri);
                ListObjectsRequest request = new() { BucketName = uri.Bucket, Prefix = (uri.Key + "/DBADash_").Replace("//", "/") };

                do
                {
                    ListObjectsResponse resp;
                    using (var listObjectsTask = s3Cli.ListObjectsAsync(request))
                    {
                        listObjectsTask.Wait();
                        resp = listObjectsTask.Result;
                    }

                    var fileList = resp.S3Objects.Where(f => f.Key.EndsWith(".xml")).Select(f => f.Key).ToList();
                    var filesToProcessByInstance = GetFilesToProcessByInstance(fileList);

                    Log.Information("Processing {0} files from {1}. Instance Count: {2}", resp.S3Objects.Count, uri.Key, filesToProcessByInstance.Count);

                    // Start a thread to process the files associated with each instance.  Each instance will have it's files processed sequentially in the order they were collected.
                    Parallel.ForEach(filesToProcessByInstance, instanceItem =>
                    {
                        List<string> instanceFiles = instanceItem.Value;
                        ProcessS3FileListForCollectS3(instanceFiles, s3Cli, uri, cfg);
                    });
                    if (resp.IsTruncated)
                    {
                        Log.Debug("Response truncated.  Processing next marker for {0}", uri.Key);
                        request.Marker = resp.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                }
                while (request != null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing files from S3");
            }
        }

        /// <summary>
        /// Process a given list of S3 files for a specific instance in order, writing collected data to DBA Dash repository database
        /// </summary>
        private static void ProcessS3FileListForCollectS3(List<string> instanceFiles, Amazon.S3.AmazonS3Client s3Cli, Amazon.S3.Util.AmazonS3Uri uri, DBADashSource cfg)
        {
            instanceFiles.Sort(); // Ensure files are processed in order
            foreach (var s3Path in instanceFiles)
            {
                using var getObjectTask = s3Cli.GetObjectAsync(uri.Bucket, s3Path);
                getObjectTask.Wait();

                using var response = getObjectTask.Result;
                using var responseStream = response.ResponseStream;

                var ds = new DataSet();
                ds.ReadXml(responseStream);

                lock (Program.Locker.GetLock(GetID(ds))) // Ensures we process 1 item at a time for each instance
                {
                    var fileName = Path.GetFileName(s3Path);
                    try
                    {
                        DestinationHandling.WriteAllDestinations(ds, cfg, fileName, config).Wait();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error importing file {filename}.  Writing file to failed message folder {folder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                        DestinationHandling.WriteFolder(ds, SchedulerServiceConfig.FailedMessageFolder, fileName, config);
                    }
                    finally
                    {
                        using var deleteTask = s3Cli.DeleteObjectAsync(uri.Bucket, s3Path);
                        deleteTask.Wait();
                    }
                }
                Log.Information("Imported {file}", s3Path);
            }
        }
    }
}