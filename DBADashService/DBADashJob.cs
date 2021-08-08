using Amazon.S3.Model;
using DBADash;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;
using Serilog;
using SerilogTimings;

namespace DBADashService
{
    [DisallowConcurrentExecution, PersistJobDataAfterExecution]
    public class DBADashJob : IJob
    {
        static readonly CollectionConfig config = SchedulerServiceConfig.Config;

        string GetID(DataSet ds)
        {
            return ds.Tables["DBADash"].Rows[0]["Instance"] + "_" + ds.Tables["DBADash"].Rows[0]["DBName"];
        }

  
        public Task Execute(IJobExecutionContext context)
        {

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            
            var cfg = JsonConvert.DeserializeObject<DBADashSource>(dataMap.GetString("CFG"));
            var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type"));
            try
            {
                if (cfg.SourceConnection.Type == ConnectionType.Directory)
                {
                    string folder = cfg.GetSource();
                    Log.Logger.Information("Import from folder {folder}", folder);
                    if (System.IO.Directory.Exists(folder))
                    {
                        try
                        {
                            var files = System.IO.Directory.EnumerateFiles(folder, "DBADash_*", SearchOption.TopDirectoryOnly).Where(f=> f.EndsWith(".json") || f.EndsWith(".bin") || f.EndsWith(".xml"));

                            Parallel.ForEach(files, f =>
                            {
                                string fileName = Path.GetFileName(f);
                                try
                                {
                                    var ds = DataSetSerialization.DeserializeFromFile(f);
                                    lock (Program.Locker.GetLock(GetID(ds)))
                                    {
                                                                         
                                        DestinationHandling.WriteAllDestinations(ds, cfg, fileName);                    
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
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex,"Import from folder {folder}",folder);
                        }
                    }
                    else
                    {
                        Log.Error("Source directory doesn't exist {folder}", folder);
                    }
                }
                else if (cfg.SourceConnection.Type == ConnectionType.AWSS3)
                {
                    Log.Information("Import from S3 {connection}",cfg.ConnectionString);
                    try
                    {
                        var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.ConnectionString);
                        var s3Cli = AWSTools.GetAWSClient(config.AWSProfile, config.AccessKey, config.GetSecretKey(), uri);
                        ListObjectsRequest request = new ListObjectsRequest() { BucketName = uri.Bucket, Prefix = (uri.Key + "/DBADash_").Replace("//", "/") };
                        do
                        {
                            ListObjectsResponse resp = s3Cli.ListObjects(request);
                            Parallel.ForEach(resp.S3Objects.Where(f => f.Key.EndsWith(".json") || f.Key.EndsWith(".bin") || f.Key.EndsWith(".xml")), f =>
                                {
                                    lock (Program.Locker.GetLock(f.Key))
                                    {
                                        using (GetObjectResponse response = s3Cli.GetObject(f.BucketName, f.Key))
                                        using (Stream responseStream = response.ResponseStream)
                                        {
                                            DataSet ds;
                                            if (f.Key.EndsWith(".bin"))
                                            {
                                                // obsolete - to be removed
                                                BinaryFormatter fmt = new BinaryFormatter();
                                                ds = (DataSet)fmt.Deserialize(responseStream);
                                            }
                                            else if (f.Key.EndsWith(".xml"))
                                            {
                                                    ds = new DataSet();
                                                    ds.ReadXml(responseStream);  
                                            }
                                            else if (f.Key.EndsWith(".json"))
                                            {
                                                using (StreamReader reader = new StreamReader(responseStream))
                                                {
                                                    string json = reader.ReadToEnd();

                                                    ds = DataSetSerialization.DeserializeDS(json);

                                                }
                                            }
                                            else
                                            {
                                                throw new Exception("Invalid extension");
                                            }
                                            lock (Program.Locker.GetLock(GetID(ds)))
                                            {
                                                string fileName = Path.GetFileName(f.Key);
                                                try
                                                {
                                                    DestinationHandling.WriteAllDestinations(ds, cfg, fileName);
                                                }
                                                catch(Exception ex)
                                                {
                                                    Log.Error(ex, "Error importing file {filename}.  Writing file to failed message folder {folder}",fileName, SchedulerServiceConfig.FailedMessageFolder);
                                                    DestinationHandling.WriteFolder(ds, SchedulerServiceConfig.FailedMessageFolder, fileName);
                                                }
                                                finally
                                                {
                                                    s3Cli.DeleteObject(f.BucketName, f.Key);
                                                }
                                            }
                                            Log.Information("Imported {file}", f.Key);                                          
                                        }
                                    }
                                });
                            if (resp.IsTruncated)
                            {
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
                else
                {

                    try
                    {
                        var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI)
                        {
                            Job_instance_id = dataMap.GetInt("Job_instance_id")
                        };              

                        var jobLastCollected = dataMap.GetDateTime("JobCollectDate");

                        // Setting the JobLastModified means we will only collect job data if jobs have been updated since the last collection.
                        // This won't detect all changes - like changes to schedules.  Skip setting JobLastModified if we haven't collected in 1 day to ensure we collect at least once per day
                        if (DateTime.UtcNow.Subtract(jobLastCollected).TotalMinutes < 1430) // Allow 10min
                        {
                            collector.JobLastModified = dataMap.GetDateTime("JobLastModified");
                        }

                        if (context.PreviousFireTimeUtc.HasValue)
                        {
                            collector.PerformanceCollectionPeriodMins = (Int32)DateTime.UtcNow.Subtract(context.PreviousFireTimeUtc.Value.UtcDateTime).TotalMinutes + 5;
                        }
                        else
                        {
                            collector.PerformanceCollectionPeriodMins = 30;
                        }
                        collector.SlowQueryThresholdMs = cfg.SlowQueryThresholdMs;
                        collector.SlowQueryMaxMemoryKB = cfg.SlowQuerySessionMaxMemoryKB;
                        collector.UseDualEventSession = cfg.UseDualEventSession;
                        collector.LogInternalPerformanceCounters = SchedulerServiceConfig.Config.LogInternalPerformanceCounters;
                        collector.PlanThreshold = cfg.RunningQueryPlanThreshold == null ? PlanCollectionThreshold.PlanCollectionDisabledThreshold : cfg.RunningQueryPlanThreshold;
                        using (var op = Operation.Begin("Collect {types} from instance {instance}", string.Join(", ", types.Select(s => s.ToString()).ToArray()), cfg.SourceConnection.ConnectionForPrint))
                        {
                            collector.Collect(types);
                            op.Complete();
                        }
                        bool containsJobs = collector.Data.Tables.Contains("Jobs");
                        string fileName = cfg.GenerateFileName(cfg.SourceConnection.ConnectionForFileName);
                        try
                        {
                            DestinationHandling.WriteAllDestinations(collector.Data, cfg, fileName);
                            dataMap.Put("Job_instance_id", collector.Job_instance_id); // Store instance_id so we can get new history only on next run
                            if (containsJobs)
                            {
                                // We have collected jobs data - Store JobLastModified and time we have collected the jobs.
                                // Used on next run to determine if we need to refresh this data.
                                dataMap.Put("JobLastModified", collector.JobLastModified);
                                dataMap.Put("JobCollectDate", DateTime.UtcNow);
                            }
                            collector.CacheCollectedText();
                            collector.CacheCollectedPlans();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error writing {filename} to destination.  File will be copied to {folder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                            DestinationHandling.WriteFolder(collector.Data, SchedulerServiceConfig.FailedMessageFolder, fileName);
                        }
                                                
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Error collecting types {types} from instance {instance}", string.Join(", ", types.Select(s => s.ToString()).ToArray()), cfg.SourceConnection.ConnectionForPrint);
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "JobExecute");
            }

            return Task.CompletedTask;

        }


    }

}
