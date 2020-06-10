using Amazon.S3.Model;
using DBAChecks;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Quartz;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecksService
{
    class DBAChecksService
    {
        public void Start(CollectionConfig config)
        {
            removeEventSessions(config);
        }

        private void removeEventSessions(CollectionConfig config)
        {
            foreach (DBAChecksSource cfg in config.SourceConnections)
            {

                string cfgString = JsonConvert.SerializeObject(cfg);
                if (cfg.SourceConnection.Type == ConnectionType.SQL)
                {
                    var collector = new DBCollector(cfg.GetSource(), cfg.NoWMI);
                    if (cfg.PersistXESessions)
                    {
                        try
                        {
                            Console.WriteLine("Stop DBAChecks event sessions: " + cfg.SourceConnection.DataSource());
                            collector.StopEventSessions();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error stopping event sessions" + ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            Console.WriteLine("Remove DBAChecks event sessions: " + cfg.SourceConnection.DataSource());
                            collector.RemoveEventSessions();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error removing event sessions" + ex.Message);
                        }
                    }

                }
            }
        }


        public void Stop(CollectionConfig config)
        {
            removeEventSessions(config);
        }

        public void Shutdown(CollectionConfig config)
        {
            removeEventSessions(config);
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
    
    public class SchemaSnapshotJob : IJob
    {

        string AccessKey;
        string SecretKey;
        string AWSProfile;
        string source;
        string destination;
        ConnectionType destinationType;
        string schemaSnapshotDBs;

        public void Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<DBAChecksSource>(dataMap.GetString("CFG")); 
            AccessKey = dataMap.GetString("AccessKey");
            SecretKey = dataMap.GetString("SecretKey");
            AWSProfile = dataMap.GetString("AWSProfile");
            source = dataMap.GetString("Source");
            destination = dataMap.GetString("Destination");
            schemaSnapshotDBs = dataMap.GetString("SchemaSnapshotDBs");
            destinationType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("DestinationType"));
            string connectionString = cfg.GetSource();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
           
            var collector = new DBCollector(connectionString, true);
            var dsSnapshot= collector.Data;
            var dbs = schemaSnapshotDBs.Split(',');

            var cn = new System.Data.SqlClient.SqlConnection(connectionString);
            var ss = new SchemaSnapshotDB(connectionString);
            var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
            Console.WriteLine("DB Snapshots " + " from Instance:" + builder.DataSource);
            foreach (Database db in instance.Databases)
            {
                bool include = false;
                if(db.IsUpdateable && db.IsAccessible && db.IsSystemObject==false){
                    foreach(string strDB in dbs)
                    {
                        if (strDB.StartsWith("-"))
                        {
                            if (db.Name == strDB.Substring(1))
                            {
                                include = false;
                                break;
                            }
                        }
                        if (strDB == db.Name || strDB == "*")
                        {
                            include = true;
                        }
                    }
                    if (include)
                    {
                        Console.WriteLine("DB Snapshot {" + db.Name +  "} from Instance:" + builder.DataSource);
                        var dt = ss.SnapshotDB(db.Name);
                       dt.TableName = "Snapshot_" + db.Name;
                        dsSnapshot.Tables.Add(dt);
                        writeDestination(cfg, dsSnapshot);
                        dsSnapshot.Tables.Remove(dt);
                    }
                }
            }

        
        }

        private void writeDestination(DBAChecksSource cfg, DataSet ds)
        {

            if (destinationType == ConnectionType.AWSS3)
            {
                Console.WriteLine("Upload to S3");
                var uri = new Amazon.S3.Util.AmazonS3Uri(destination);
                var s3Cli = AWSTools.GetAWSClient(AWSProfile, AccessKey, SecretKey, uri);
                var r = new Amazon.S3.Model.PutObjectRequest();
                string fileName =System.IO.Path.ChangeExtension(cfg.GenerateFileName(),".bin");
                string filePath = Path.Combine(destination, fileName);
                ds.RemotingFormat = SerializationFormat.Binary;
                BinaryFormatter fmt = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                fmt.Serialize(ms, ds);
                r.InputStream = ms;
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
                    ds.RemotingFormat = SerializationFormat.Binary;
                    BinaryFormatter fmt = new BinaryFormatter();
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        fmt.Serialize(fs, ds);
                    }                 
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
                        collector.PerformanceCollectionPeriodMins = (Int32)DateTime.UtcNow.Subtract(context.PreviousFireTimeUtc.Value.UtcDateTime).TotalMinutes + 5;
                    }
                    else
                    {
                        collector.PerformanceCollectionPeriodMins = 30;
                    }
                    collector.SlowQueryThresholdMs = cfg.SlowQueryThresholdMs;
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
                    ServiceConfiguratorExtensions.WhenStarted<DBAChecksService>(service, s => s.Start(config));
                    ServiceConfiguratorExtensions.WhenStopped<DBAChecksService>(service, s => s.Stop(config));
                    ServiceConfiguratorExtensions.WhenShutdown<DBAChecksService>(service, s => s.Shutdown(config));
                    if (config.DestinationConnection.Type == ConnectionType.SQL)
                    {
                        string maintenanceCron = config.GetMaintenanceCron();
                        var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                     q.WithJob(() =>
                     JobBuilder.Create<MaintenanceJob>()
                          .UsingJobData("ConnectionString", config.DestinationConnection.ConnectionString)
                          .Build())
                          .AddTrigger(() => TriggerBuilder.Create()
                              .WithCronSchedule(maintenanceCron)
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
                                          .WithRepeatCount(0)).StartAt(DateTime.Now.AddSeconds(5))
                                      .Build())
                              .AddTrigger(() => TriggerBuilder.Create()
                                  .WithCronSchedule(s.CronSchedule)
                                  .Build()
                                  )); ; ;
                        }
                        if (cfg.SchemaSnapshotDBs.Length > 0)
                        {
                            var x = ScheduleJobServiceConfiguratorExtensions.ScheduleQuartzJob<DBAChecksService>(service, q =>
                 q.WithJob(() =>
                 JobBuilder.Create<SchemaSnapshotJob>()
                      .UsingJobData("CFG", cfgString)
                      .UsingJobData("AccessKey", config.AccessKey)
                      .UsingJobData("SecretKey", config.GetSecretKey())
                      .UsingJobData("AWSProfile", config.AWSProfile)
                      .UsingJobData("Source", cfg.SourceConnection.ConnectionString)
                      .UsingJobData("Destination", config.DestinationConnection.ConnectionString)
                      .UsingJobData("DestinationType", JsonConvert.SerializeObject(config.DestinationConnection.Type))
                      .UsingJobData("SchemaSnapshotDBs", cfg.SchemaSnapshotDBs)
                     .Build())
                 .AddTrigger(() => TriggerBuilder.Create()
                         .WithSimpleSchedule(b => b
                             .WithIntervalInSeconds(1)
                             .WithRepeatCount(0)).StartAt(DateTime.Now.AddSeconds(5))
                         .Build())
                 .AddTrigger(() => TriggerBuilder.Create()
                     .WithCronSchedule(cfg.SchemaSnapshotCron)
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
