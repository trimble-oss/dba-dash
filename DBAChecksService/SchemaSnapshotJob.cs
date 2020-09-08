using DBAChecks;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using static DBAChecks.DBAChecksConnection;

namespace DBAChecksService
{
    public class SchemaSnapshotJob : IJob
    {


        public void Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<DBAChecksSource>(dataMap.GetString("CFG"));
            var AccessKey = dataMap.GetString("AccessKey");
            var SecretKey = dataMap.GetString("SecretKey");
            var AWSProfile = dataMap.GetString("AWSProfile");
            var source = dataMap.GetString("Source");
            var destination = dataMap.GetString("Destination");
            var schemaSnapshotDBs = dataMap.GetString("SchemaSnapshotDBs");
            string strSchemaSnapshotOptions = dataMap.GetString("Options");
            var schemaSnapshotOptions = JsonConvert.DeserializeObject <SchemaSnapshotDBOptions> (strSchemaSnapshotOptions);
            var destinationType = JsonConvert.DeserializeObject<ConnectionType>(dataMap.GetString("DestinationType"));
            string connectionString = cfg.GetSource();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            var collector = new DBCollector(connectionString, true);
            var dsSnapshot = collector.Data;
            var dbs = schemaSnapshotDBs.Split(',');

            var cn = new System.Data.SqlClient.SqlConnection(connectionString);
            var ss = new SchemaSnapshotDB(connectionString, schemaSnapshotOptions);
            var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
           

            Console.WriteLine("DB Snapshots " + " from Instance:" + builder.DataSource);
            foreach (Database db in instance.Databases)
            {
                bool include = false;
                if (db.IsUpdateable && db.IsAccessible && db.IsSystemObject == false)
                {
                    foreach (string strDB in dbs)
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
                        Console.WriteLine("DB Snapshot {" + db.Name + "} from Instance:" + builder.DataSource);
                        DateTime StartTime = DateTime.UtcNow;
                        try
                        {
                           var  dt = ss.SnapshotDB(db.Name);
                            DateTime EndTime = DateTime.UtcNow;
                            dt.TableName = "Snapshot_" + db.Name;
                            dt.ExtendedProperties.Add("StartTime", StartTime);
                            dt.ExtendedProperties.Add("EndTime", EndTime);
                            dt.ExtendedProperties.Add("SnapshotOptions", strSchemaSnapshotOptions);
                            dsSnapshot.Tables.Add(dt);

                            string fileName = cfg.GenerateFileName(true);
                            DestinationHandling.Write(dsSnapshot, destination, fileName, AWSProfile, AccessKey, SecretKey, destinationType);
                            dsSnapshot.Tables.Remove(dt);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Snapshot error:" + ex.Message);
                        }

                    }
                }
            }

        }

    }
}
