using Amazon.S3.Model;
using DBAChecks;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Topshelf;
using Topshelf.Quartz;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecksService
{
    class DBAChecksService
    {
        public void Start()
        {

        }
        public void Stop()
        {

        }

    }

    public class MaintenanceJob: IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string connectionString = dataMap.GetString("ConnectionString");
            AddPartitions(connectionString);
            PurgeData(connectionString);
        }

        public static void AddPartitions(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Partitions_Add", cn);
                Console.WriteLine("Maintenance: Creating partitions");
                cmd.ExecuteNonQuery();             
            }
        }
        public static void PurgeData(string connectionString)
        {
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                Console.WriteLine("Maintenance: PurgeData");
                SqlCommand cmd = new SqlCommand("PurgeData", cn);
                cmd.ExecuteNonQuery();
            }
        }
    }
    

    public class DBAChecksJob : IJob
    {

        string AccessKey;
        string SecretKey;
        string AWSProfile;
        string source;
        string destination;
        ConnectionType sourceType;
        ConnectionType destinationType;
         

        public void Execute(IJobExecutionContext context)
        {

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<DBAChecksSource>(dataMap.GetString("CFG"));
            var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type"));
             AccessKey = dataMap.GetString("AccessKey");
             SecretKey = dataMap.GetString("SecretKey");
            AWSProfile = dataMap.GetString("AWSProfile");
            source = dataMap.GetString("Source");
            destination = dataMap.GetString("Destination");
            sourceType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("SourceType"));
            destinationType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("DestinationType"));

            try
            {
                if (cfg.SourceConnection.Type == ConnectionType.Directory)
                {
                    string folder = cfg.GetSource();
                    Console.WriteLine("Import from folder:" + folder);
                    if (System.IO.Directory.Exists(folder))
                    {
                        foreach (string f in System.IO.Directory.GetFiles(folder, "DBAChecks_*.json"))
                        {
                            string json = System.IO.File.ReadAllText(f);
                            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                            writeDestination(cfg, ds);
                            System.IO.File.Delete(f);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Source directory doesn't exist: " + folder);
                    }
                }
                else if (cfg.SourceConnection.Type == ConnectionType.AWSS3)
                {
                    Console.WriteLine("Import from S3: " + cfg.ConnectionString);
                    var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.ConnectionString);
                    var s3Cli = AWSTools.GetAWSClient(AWSProfile, AccessKey, SecretKey, uri);
                    var resp = s3Cli.ListObjects(uri.Bucket, (uri.Key + "/DBAChecks_").Replace("//", "/"));
                    foreach (var f in resp.S3Objects)
                    {
                        if (f.Key.EndsWith(".json"))
                        {
                            using (GetObjectResponse response = s3Cli.GetObject(f.BucketName, f.Key))
                            using (Stream responseStream = response.ResponseStream)
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                string json = reader.ReadToEnd();
                                DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
                                writeDestination(cfg, ds);
                                s3Cli.DeleteObject(f.BucketName, f.Key);
                            }
                            Console.WriteLine("Imported:" + f.Key);
                        }

                    }
                }
                else
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(cfg.GetSource());
                    Console.WriteLine("Collect " + string.Join(", ", types.Select(s => s.ToString()).ToArray()) + " from Instance:" + builder.DataSource);
                    var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI);
                    if (context.PreviousFireTimeUtc.HasValue)
                    {
                        collector.CPUCollectionPeriod = (Int32)DateTime.UtcNow.Subtract(context.PreviousFireTimeUtc.Value.UtcDateTime).TotalMinutes + 5;
                    }
                    else
                    {
                        collector.CPUCollectionPeriod = 30;
                    }
                    collector.Collect(types);
                    try
                    {
                        writeDestination(cfg, collector.Data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error writing to destination:" + ex.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());               
            }



        }

        private void writeDestination(DBAChecksSource cfg, DataSet ds)
        {
      
            if (destinationType == ConnectionType.AWSS3)
            {
                Console.WriteLine("Upload to S3");
                var uri = new Amazon.S3.Util.AmazonS3Uri(destination);
                var s3Cli = AWSTools.GetAWSClient(AWSProfile,AccessKey,SecretKey, uri);
                var r = new Amazon.S3.Model.PutObjectRequest();
                string fileName = cfg.GenerateFileName();
                string filePath = Path.Combine(destination, fileName);
                string json = JsonConvert.SerializeObject(ds, Formatting.None);
                r.ContentBody = json;
                r.BucketName = uri.Bucket;
                r.Key = (uri.Key + "/" + fileName).Replace("//", "/");

                s3Cli.PutObject(r);
            }
            else if (destinationType == ConnectionType.Directory)
            {
                if (System.IO.Directory.Exists(destination))
                {
                    Console.WriteLine("Write to folder");
                    string fileName = cfg.GenerateFileName();
                    string filePath = Path.Combine(destination, fileName);
                    string json = JsonConvert.SerializeObject(ds, Formatting.None);
                    File.WriteAllText(filePath, json);
                }
                else
                {
                    Console.WriteLine("Destination Folder doesn't exist");
                }
            }
            else
            {
                var importer = new DBImporter();
                Console.WriteLine("Update DBAChecks DB");
                importer.Update(destination, ds);

            }
        }

    }

    internal static class ConfigureService
    {
        internal static void Configure(CollectionConfig config)
        {
            HostFactory.Run(configure =>
            {

                configure.Service<DBAChecksService>(service =>
                {
                    service.ConstructUsing(s => new DBAChecksService());
                    ServiceConfiguratorExtensions.WhenStarted<DBAChecksService>(service, s => s.Start());
                    ServiceConfiguratorExtensions.WhenStopped<DBAChecksService>(service, s => s.Stop());
                    if (config.DestinationConnection.Type == ConnectionType.SQL)
                    {
                        string maintenanceChron = config.GetMaintenanceChron();
                        var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                     q.WithJob(() =>
                     JobBuilder.Create<MaintenanceJob>()
                          .UsingJobData("ConnectionString", config.DestinationConnection.ConnectionString)
                          .Build())
                          .AddTrigger(() => TriggerBuilder.Create()
                              .WithCronSchedule(maintenanceChron)
                              .Build()
                              )); ; ;
                        MaintenanceJob.AddPartitions(config.DestinationConnection.ConnectionString);

                    }
                    foreach (DBAChecksSource cfg in config.SourceConnections)
                    {

                        string cfgString = JsonConvert.SerializeObject(cfg);

                        foreach (var s in cfg.GetSchedule())
                        {

                            var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                              q.WithJob(() =>
                              JobBuilder.Create<DBAChecksJob>()
                                   .UsingJobData("Type", JsonConvert.SerializeObject(s.CollectionTypes))
                                   .UsingJobData("CFG", cfgString)
                                   .UsingJobData("AccessKey", config.AccessKey)
                                   .UsingJobData("SecretKey", config.GetSecretKey())
                                   .UsingJobData("AWSProfile", config.AWSProfile)
                                   .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                                   .UsingJobData("Destination", config.DestinationConnection.ConnectionString)
                                   .UsingJobData("SourceType", JsonConvert.SerializeObject(cfg.SourceConnection.Type))
                                   .UsingJobData("DestinationType", JsonConvert.SerializeObject(config.DestinationConnection.Type))
                                  .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                      .WithSimpleSchedule(b => b
                                          .WithIntervalInSeconds(1)
                                          .WithRepeatCount(0))
                                      .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                  .WithCronSchedule(s.ChronSchedule)
                                  .Build()
                                  )); ; ;
                        }

             

                    }
                });

                //Setup Account that window service use to run.  
               // configure.RunAsPrompt();
                //configure.RunAsLocalSystem();
                configure.SetServiceName("DBAChecksService");
                configure.SetDisplayName("DBAChecksService");
                configure.SetDescription("Collect data from SQL Instances");
            });
        }
    }
}
