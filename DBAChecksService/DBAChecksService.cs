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


    public class DBAChecksJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<CollectionConfig>(dataMap.GetString("CFG"));
            var types = JsonConvert.DeserializeObject<CollectionType[]>(dataMap.GetString("Type"));

            if (cfg.SourceConnectionType() == CollectionConfig.ConnectionType.Directory)
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
            else if (cfg.SourceConnectionType() == CollectionConfig.ConnectionType.AWSS3)
            {
                Console.WriteLine("Import from S3: " + cfg.Source);
                var uri = new Amazon.S3.Util.AmazonS3Uri(cfg.Source);
                var s3Cli = AWSTools.GetAWSClient(cfg.AWSProfile,cfg.AccessKey,cfg.GetSecretKey(), uri);
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
                writeDestination(cfg, collector.Data);
            }




        }

        private void writeDestination(CollectionConfig cfg, DataSet ds)
        {
            string destination = cfg.GetDestination();
            if (cfg.DestinationConnectionType() == CollectionConfig.ConnectionType.AWSS3)
            {
                Console.WriteLine("Upload to S3");
                var uri = new Amazon.S3.Util.AmazonS3Uri(destination);
                var s3Cli = AWSTools.GetAWSClient(cfg.AWSProfile,cfg.AccessKey,cfg.GetSecretKey(), uri);
                var r = new Amazon.S3.Model.PutObjectRequest();
                string fileName = cfg.GenerateFileName();
                string filePath = Path.Combine(destination, fileName);
                string json = JsonConvert.SerializeObject(ds, Formatting.None);
                r.ContentBody = json;
                r.BucketName = uri.Bucket;
                r.Key = (uri.Key + "/" + fileName).Replace("//", "/");

                s3Cli.PutObject(r);
            }
            else if (cfg.DestinationConnectionType() == CollectionConfig.ConnectionType.Directory)
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
                importer.Update(cfg.GetDestination(), ds);

            }
        }

    }

    internal static class ConfigureService
    {
        internal static void Configure(CollectionConfig[] configs)
        {
            HostFactory.Run(configure =>
            {

                configure.Service<DBAChecksService>(service =>
                {
                    service.ConstructUsing(s => new DBAChecksService());
                    ServiceConfiguratorExtensions.WhenStarted<DBAChecksService>(service, s => s.Start());
                    ServiceConfiguratorExtensions.WhenStopped<DBAChecksService>(service, s => s.Stop());
                    foreach (CollectionConfig cfg in configs)
                    {

                        string cfgString = JsonConvert.SerializeObject(cfg);

                        foreach (var s in cfg.GetSchedule())
                        {

                            var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                              q.WithJob(() =>
                              JobBuilder.Create<DBAChecksJob>()
                                   .UsingJobData("Type", JsonConvert.SerializeObject(s.CollectionTypes))
                                   .UsingJobData("CFG", cfgString)
                                  .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                      .WithSimpleSchedule(b => b
                                          .WithIntervalInSeconds(1)
                                          .WithRepeatCount(0))
                                      .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                  .WithCronSchedule(s.ChronSchedule)
                                  .Build()
                                  ));
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
