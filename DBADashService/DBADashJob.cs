using Amazon.S3.Model;
using DBADash;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    [DisallowConcurrentExecution]
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
                    ScheduleService.InfoLogger("Import from folder:" + folder);
                    if (System.IO.Directory.Exists(folder))
                    {
                        try
                        {
                            var files = System.IO.Directory.GetFiles(folder, "DBADash_*.json");

                            Parallel.ForEach(files, f =>
                            {
                                string json = System.IO.File.ReadAllText(f);
                                DataSet ds = DataSetSerialization.DeserializeDS(json);
                                lock (Program.Locker.GetLock(GetID(ds)))
                                {
                                    DestinationHandling.WriteAllDestinations(ds, cfg, Path.GetFileName(f));
                                }
                                System.IO.File.Delete(f);
                            }
                            );
                            files = System.IO.Directory.GetFiles(folder, "DBADash_*.bin");
                            Parallel.ForEach(files, f =>
                            {

                                BinaryFormatter fmt = new BinaryFormatter();
                                DataSet ds;
                                using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                                {
                                    ds = (DataSet)fmt.Deserialize(fs);
                                }
                                lock (Program.Locker.GetLock(GetID(ds)))
                                {
                                    DestinationHandling.WriteAllDestinations(ds, cfg, Path.GetFileName(f));
                                }
                                System.IO.File.Delete(f);
                            });
                        }
                        catch (Exception ex)
                        {
                            DBADashService.ScheduleService.ErrorLogger(ex, "Import from folder");
                        }
                    }
                    else
                    {
                        DBADashService.ScheduleService.ErrorLogger(new Exception("Source directory doesn't exist: " + folder), "Import from Folder");
                    }
                }
                else if (cfg.SourceConnection.Type == ConnectionType.AWSS3)
                {
                    ScheduleService.InfoLogger("Import from S3: " + cfg.ConnectionString);
                    try
                    {
                        var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.ConnectionString);
                        var s3Cli = AWSTools.GetAWSClient(config.AWSProfile, config.AccessKey, config.GetSecretKey(), uri);
                        ListObjectsRequest request = new ListObjectsRequest() { BucketName = uri.Bucket, Prefix = (uri.Key + "/DBADash_").Replace("//", "/") };
                        do
                        {
                            ListObjectsResponse resp = s3Cli.ListObjects(request);
                            Parallel.ForEach(resp.S3Objects.Where(f => f.Key.EndsWith(".json") || f.Key.EndsWith(".bin")), f =>
                                {
                                    lock (Program.Locker.GetLock(f.Key))
                                    {
                                        using (GetObjectResponse response = s3Cli.GetObject(f.BucketName, f.Key))
                                        using (Stream responseStream = response.ResponseStream)
                                        {
                                            DataSet ds;
                                            if (f.Key.EndsWith(".bin"))
                                            {
                                                BinaryFormatter fmt = new BinaryFormatter();
                                                ds = (DataSet)fmt.Deserialize(responseStream);
                                            }
                                            else
                                            {
                                                using (StreamReader reader = new StreamReader(responseStream))
                                                {
                                                    string json = reader.ReadToEnd();

                                                    ds = DataSetSerialization.DeserializeDS(json);

                                                }
                                            }
                                            lock (Program.Locker.GetLock(GetID(ds)))
                                            {
                                                DestinationHandling.WriteAllDestinations(ds, cfg, Path.GetFileName(f.Key));
                                            }
                                            s3Cli.DeleteObject(f.BucketName, f.Key);
                                            ScheduleService.InfoLogger("Imported:" + f.Key);
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
                        DBADashService.ScheduleService.ErrorLogger(ex, "Import from S3");
                    }

                }
                else
                {

                    string collectDescription = "Collect " + string.Join(", ", types.Select(s => s.ToString()).ToArray()) + " from Instance:" + cfg.SourceConnection.ConnectionForPrint;
                    ScheduleService.InfoLogger(collectDescription);
                    try
                    {
                        var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI);
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
                        collector.Collect(types);

                        try
                        {
                            DestinationHandling.WriteAllDestinations(collector.Data, cfg, cfg.GenerateFileName(SchedulerServiceConfig.Config.BinarySerialization, cfg.SourceConnection.ConnectionForFileName));
                        }
                        catch (Exception ex)
                        {
                            DBADashService.ScheduleService.ErrorLogger(ex, "Write to destination");
                        }
                    }
                    catch (Exception ex)
                    {
                        DBADashService.ScheduleService.ErrorLogger(ex, collectDescription);
                    }

                }
            }
            catch (Exception ex)
            {
                DBADashService.ScheduleService.ErrorLogger(ex, "JobExecute");
            }

            return Task.CompletedTask;

        }


    }

}
