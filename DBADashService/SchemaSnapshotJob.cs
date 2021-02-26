using DBADash;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using static DBADash.DBADashConnection;

namespace DBADashService
{
    public class SchemaSnapshotJob : IJob
    {
    
        public Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var cfg = JsonConvert.DeserializeObject<DBADashSource>(dataMap.GetString("CFG"));
            var schemaSnapshotDBs = dataMap.GetString("SchemaSnapshotDBs");
            string strSchemaSnapshotOptions = dataMap.GetString("Options");
            var schemaSnapshotOptions = JsonConvert.DeserializeObject <SchemaSnapshotDBOptions> (strSchemaSnapshotOptions);
            string connectionString = cfg.GetSource();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            var collector = new DBCollector(connectionString, true);
            var dsSnapshot = collector.Data;
            var dbs = schemaSnapshotDBs.Split(',');

            var cn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
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
                            DestinationHandling.Write(dsSnapshot, cfg);
                            dsSnapshot.Tables.Remove(dt);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Snapshot error:" + ex.Message);
                        }

                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
