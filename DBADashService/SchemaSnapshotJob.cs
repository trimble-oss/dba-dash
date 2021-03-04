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
            string connectionString = cfg.GetSource();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            var collector = new DBCollector(connectionString, true);
            var dsSnapshot = collector.Data;
            var dbs = schemaSnapshotDBs.Split(',');

            var cn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            var ss = new SchemaSnapshotDB(connectionString, SchedulerServiceConfig.Config.SchemaSnapshotOptions);
            var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
            if (instance.ServerType ==  Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase && (builder.InitialCatalog == null || builder.InitialCatalog == "master" || builder.InitialCatalog==""))
            {
                return Task.CompletedTask;
            }
            ScheduleService.InfoLogger("DB Snapshots " + " from Instance:" + builder.DataSource);
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
                        ScheduleService.InfoLogger("DB Snapshot {" + db.Name + "} from Instance:" + builder.DataSource);
                        DateTime StartTime = DateTime.UtcNow;
                        try
                        {
                           var  dt = ss.SnapshotDB(db.Name);
                            DateTime EndTime = DateTime.UtcNow;
                            dt.TableName = "Snapshot_" + db.Name;
                            dt.ExtendedProperties.Add("StartTime", StartTime);
                            dt.ExtendedProperties.Add("EndTime", EndTime);
                            dt.ExtendedProperties.Add("SnapshotOptions", JsonConvert.SerializeObject(SchedulerServiceConfig.Config.SchemaSnapshotOptions));
                            dsSnapshot.Tables.Add(dt);

                            string fileName = cfg.GenerateFileName(true,cfg.SourceConnection.ConnectionForFileName);
                            DestinationHandling.WriteAllDestinations(dsSnapshot, cfg,fileName);
                            dsSnapshot.Tables.Remove(dt);
                        }
                        catch(Exception ex)
                        {
                            ScheduleService.InfoLogger("Snapshot error:" + ex.Message);
                        }

                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
