using DBADash;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Quartz;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Serilog;

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

            var collector = new DBCollector(cfg,SchedulerServiceConfig.Config.ServiceName);
            var dsSnapshot = collector.Data;
            var dbs = schemaSnapshotDBs.Split(',');

            using var cn = new SqlConnection(connectionString);
            var ss = new SchemaSnapshotDB(cfg.SourceConnection, SchedulerServiceConfig.Config.SchemaSnapshotOptions);
            var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));

            if (instance.ServerType ==  Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase && (builder.InitialCatalog == null || builder.InitialCatalog == "master" || builder.InitialCatalog==""))
            {
                return Task.CompletedTask;
            }
            Log.Information("DB Snapshots from instance {source}",cfg.SourceConnection.ConnectionForPrint);
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
                        Log.Information("DB Snapshot {db} from instance {instance}", db.Name, builder.DataSource);
                        DateTime StartTime = DateTime.UtcNow;
                        try
                        {
                           var  dt = ss.SnapshotDB(db.Name);
                            DateTime EndTime = DateTime.UtcNow;
                            dt.TableName = "Snapshot_" + db.Name;
                            dt.ExtendedProperties.Add("StartTimeBin",StartTime.ToBinary().ToString());
                            dt.ExtendedProperties.Add("EndTimeBin", EndTime.ToBinary().ToString());
                            dt.ExtendedProperties.Add("SnapshotOptions", JsonConvert.SerializeObject(SchedulerServiceConfig.Config.SchemaSnapshotOptions));
                            dsSnapshot.Tables.Add(dt);

                            string fileName = cfg.GenerateFileName(cfg.SourceConnection.ConnectionForFileName);
                            DestinationHandling.WriteAllDestinations(dsSnapshot, cfg,fileName).Wait();
                            dsSnapshot.Tables.Remove(dt);
                        }
                        catch(Exception ex)
                        {
                            Log.Error(ex,"Error creating schema snapshot {db}", db.Name);
                        }

                    }
                }
            }
            return Task.CompletedTask;
        }

    }
}
