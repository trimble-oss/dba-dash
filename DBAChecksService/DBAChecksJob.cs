using Amazon.S3.Model;
using DBAChecks;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecksService
{
    public class DBAChecksJob : IJob
    {


        public Task Execute(IJobExecutionContext context)
        {

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<DBAChecksSource>(dataMap.GetString("CFG"));
            var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type"));
            var AccessKey = dataMap.GetString("AccessKey");
            var SecretKey = dataMap.GetString("SecretKey");
            var AWSProfile = dataMap.GetString("AWSProfile");
            var source = dataMap.GetString("Source");
            var binarySerialization = dataMap.GetBoolean("BinarySerialization");
            var destination = dataMap.GetString("Destination");
            var sourceType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("SourceType"));
            var destinationType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("DestinationType"));

            try
            {
                if (cfg.SourceConnection.Type == ConnectionType.Directory)
                {
                    string folder = cfg.GetSource();
                    Console.WriteLine("Import from folder:" + folder);
                    if (System.IO.Directory.Exists(folder))
                    {
                        try
                        {
                            foreach (string f in System.IO.Directory.GetFiles(folder, "DBAChecks_*.json"))
                            {
                                string json = System.IO.File.ReadAllText(f);
                                DataSet ds  = DataSetSerialization.DeserializeDS(json);
                                DestinationHandling.WriteDB(ds, destination);
                                System.IO.File.Delete(f);
                            }
                            foreach (string f in System.IO.Directory.GetFiles(folder, "DBAChecks_*.bin"))
                            {
                                BinaryFormatter fmt = new BinaryFormatter();
                                DataSet ds;
                                using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                                {
                                    ds = (DataSet)fmt.Deserialize(fs);
                                }
                                DestinationHandling.WriteDB(ds, destination);
                                System.IO.File.Delete(f);
                            }
                        }
                        catch(Exception ex)
                        {
                            DBAChecksService.ScheduleService.ErrorLogger(ex, "Import from folder");
                        }
                    }
                    else
                    {
                        DBAChecksService.ScheduleService.ErrorLogger(new Exception("Source directory doesn't exist: " + folder), "Import from Folder");
                    }
                }
                else if (cfg.SourceConnection.Type == ConnectionType.AWSS3)
                {
                    Console.WriteLine("Import from S3: " + cfg.ConnectionString);
                    try
                    {
                        var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.ConnectionString);
                        var s3Cli = AWSTools.GetAWSClient(AWSProfile, AccessKey, SecretKey, uri);
                        var resp = s3Cli.ListObjects(uri.Bucket, (uri.Key + "/DBAChecks_").Replace("//", "/"));
                        foreach (var f in resp.S3Objects)
                        {
                            if (f.Key.EndsWith(".json") || f.Key.EndsWith(".bin"))
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
                                    DestinationHandling.WriteDB(ds, destination);
                                    s3Cli.DeleteObject(f.BucketName, f.Key);
                                    Console.WriteLine("Imported:" + f.Key);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        DBAChecksService.ScheduleService.ErrorLogger(ex, "Import from S3");
                    }
       
                }
                else
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(cfg.GetSource());
                    string collectDescription = "Collect " + string.Join(", ", types.Select(s => s.ToString()).ToArray()) + " from Instance:" + builder.DataSource;
                    Console.WriteLine(collectDescription);
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
                        collector.Collect(types);
                        string fileName = cfg.GenerateFileName(binarySerialization);
                        try
                        {
                            DestinationHandling.Write(collector.Data, destination, fileName, AWSProfile, AccessKey, SecretKey, destinationType);
                        }
                        catch (Exception ex)
                        {
                            DBAChecksService.ScheduleService.ErrorLogger(ex, "Write to destination");
                        }
                    }
                    catch (Exception ex)
                    {
                        DBAChecksService.ScheduleService.ErrorLogger(ex, collectDescription);
                    }

                }
            }
            catch (Exception ex)
            {
                DBAChecksService.ScheduleService.ErrorLogger(ex, "JobExecute");
            }

            return Task.CompletedTask;

        }


    }

}
