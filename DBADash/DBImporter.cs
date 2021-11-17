using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Serilog;

namespace DBADash
{
    public class DBImporter
    {

        private DataSet data;
        private string connectionString;
        private Policy  retryPolicy;
        private int? instanceID;
        private DateTime snapshotDate;
        private static readonly int commandTimeout = 60;
        private DBADashAgent importAgent;

        public DBImporter(DataSet data, string connectionString,DBADashAgent importAgent)
        {
            this.importAgent = importAgent;
            this.data = data;
            upgradeDS();
            this.connectionString = connectionString;

            retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                }, (exception, timeSpan, retryCount, context) =>
                {
                    logError((string)context.OperationKey, exception, "Import[Retrying]");
                });
        }

        public void TestConnection()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
            }            
        }

        // Adds error to Errors datatable to be imported into CollectionErrorLog table later.
        private void logError(string errorSource, Exception ex, string errorContext = "Import")
        {
            
            DataTable dtErrors;
            if (data.Tables.Contains("Errors"))
            {
                dtErrors = data.Tables["Errors"];
                if (dtErrors.Columns.Count == 0)
                {
                    dtErrors.Columns.Add("ErrorSource");
                    dtErrors.Columns.Add("ErrorMessage");
                    dtErrors.Columns.Add("ErrorContext");
                }
                if (dtErrors.Columns.Count == 2)
                {
                    dtErrors.Columns.Add("ErrorContext");
                }
            }
            else
            {
                dtErrors = new DataTable("Errors");
                dtErrors.Columns.Add("ErrorSource");
                dtErrors.Columns.Add("ErrorMessage");
                dtErrors.Columns.Add("ErrorContext");
                data.Tables.Add(dtErrors);
            }

            Log.Error(ex,"Error from {ErrorContext} {ErrorSource}", errorContext, errorSource);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = ex.ToString();
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }

        

        // handle schema changes between agent versions
        private void upgradeDS()
        {
            if (data.Tables.Contains("BlockingSnapshot"))
            {
                var bss = data.Tables["BlockingSnapshot"];
                if (!bss.Columns.Contains("session_status"))
                {
                    bss.Columns.Add("session_status");
                }
                if (!bss.Columns.Contains("transaction_isolation_level"))
                {
                    bss.Columns.Add("transaction_isolation_level", typeof(Int16));
                }
            }
            if (data.Tables.Contains("DatabasesHADR"))
            {
                var dtDatabasesHADR = data.Tables["DatabasesHADR"];
                if (dtDatabasesHADR.Rows.Count == 0)
                {
                    data.Tables.Remove("DatabasesHADR");
                }
                else if (!dtDatabasesHADR.Columns.Contains("replica_id"))
                {
                    dtDatabasesHADR.Columns.Add("replica_id", typeof(Guid));
                    dtDatabasesHADR.Columns.Add("group_id", typeof(Guid));
                    dtDatabasesHADR.Columns.Add("is_commit_participant", typeof(bool));
                    dtDatabasesHADR.Columns.Add("database_state", typeof(Int16));
                    dtDatabasesHADR.Columns.Add("is_local", typeof(bool));
                    dtDatabasesHADR.Columns.Add("secondary_lag_seconds", typeof(long));
                }
            }
            if (data.Tables.Contains("Databases"))
            {
                var dtDB = data.Tables["Databases"];
                if (dtDB.Columns["owner_sid"].DataType == typeof(string))
                {
                    Int32 pos = dtDB.Columns["owner_sid"].Ordinal;
                    dtDB.Columns["owner_sid"].ColumnName = "owner_sid_string";
                    var newCol = dtDB.Columns.Add("owner_sid", typeof(byte[]));
                    foreach (DataRow r in dtDB.Rows)
                    {
                        r["owner_sid"] = Convert.FromBase64String((string)r["owner_sid_string"]);
                    }
                    dtDB.Columns.Remove("owner_sid_string");
                    newCol.SetOrdinal(pos);
                }
            }
            if (data.Tables.Contains("Backups"))
            {
                var dtBackups = data.Tables["Backups"];
                if (dtBackups.Columns.Contains("LastBackup"))
                {
                    dtBackups.Columns["LastBackup"].ColumnName = "backup_start_date";
                    dtBackups.Columns.Add("backup_finish_date", typeof(DateTime));
                    dtBackups.Columns.Add("backup_set_id", typeof(int));
                    dtBackups.Columns.Add("time_zone", typeof(short));
                    dtBackups.Columns.Add("backup_size", typeof(decimal));
                    dtBackups.Columns.Add("is_password_protected", typeof(bool));
                    dtBackups.Columns.Add("recovery_model", typeof(string));
                    dtBackups.Columns.Add("has_bulk_logged_data", typeof(bool));
                    dtBackups.Columns.Add("is_snapshot", typeof(bool));
                    dtBackups.Columns.Add("is_readonly", typeof(bool));
                    dtBackups.Columns.Add("is_single_uiser", typeof(bool));
                    dtBackups.Columns.Add("has_backup_checksums", typeof(bool));
                    dtBackups.Columns.Add("is_damaged", typeof(bool));
                    dtBackups.Columns.Add("has_incomplete_metadata", typeof(bool));
                    dtBackups.Columns.Add("is_force_offline", typeof(bool));
                    dtBackups.Columns.Add("is_copy_only", typeof(bool));
                    dtBackups.Columns.Add("database_guid", typeof(Guid));
                    dtBackups.Columns.Add("family_guid", typeof(Guid));
                    dtBackups.Columns.Add("compressed_backup_size", typeof(decimal));
                    dtBackups.Columns.Add("key_algorithm", typeof(string));
                    dtBackups.Columns.Add("encryptor_type", typeof(string));
                }
            }
            if (data.Tables.Contains("LogRestores"))
            {
                var dtLR = data.Tables["LogRestores"];
                if (!dtLR.Columns.Contains("backup_time_zone"))
                {
                    dtLR.Columns.Add("backup_time_zone", typeof(short));
                }
            }
            if (data.Tables.Contains("SlowQueries"))
            {
                var dtSlowQueries = data.Tables["SlowQueries"];
                if (!dtSlowQueries.Columns.Contains("session_id"))
                {
                    dtSlowQueries.Columns.Add("session_id", typeof(int));
                }
            }
            if (data.Tables.Contains("RunningQueries"))
            {
                var dtRunningQueries = data.Tables["RunningQueries"];
                if (!dtRunningQueries.Columns.Contains("login_time_utc"))
                {
                    dtRunningQueries.Columns.Add("login_time_utc", typeof(DateTime));
                }
            }
        }

        public void Update()
        {
            List<Exception> exceptions = new List<Exception>();
            var rInstance = data.Tables["DBADash"].Rows[0];
            snapshotDate = (DateTime)rInstance["SnapshotDateUTC"];

            // we need to get the instanceID to continue further.  retry based on policy then exception will be thrown to catch higher up
            instanceID = retryPolicy.Execute(
              context => updateInstance(ref rInstance),
              new Context("Instance")
            );

            string[] tables = { "Databases","Drives", "ServerProperties", "Backups", "AgentJobs", "LogRestores", "DBFiles", "DBConfig", "Corruption", "DatabasesHADR", "SysConfig", "OSInfo", "TraceFlags", "CPU", "Drivers",
                                    "BlockingSnapshot", "IOStats", "Waits", "OSLoadedModules", "DBTuningOptions", "AzureDBResourceStats", "AzureDBServiceObjectives", "AzureDBElasticPoolResourceStats", "SlowQueries",
                                    "SlowQueriesStats", "LastGoodCheckDB", "Alerts", "ObjectExecutionStats", "ServerPrincipals", "ServerRoleMembers", "ServerPermissions", "DatabasePrincipals", "DatabaseRoleMembers",
                                    "DatabasePermissions", "CustomChecks", "PerformanceCounters", "VLF", "DatabaseMirroring", "Jobs", "JobHistory","AvailabilityReplicas","AvailabilityGroups","JobSteps",
                                    "DatabaseQueryStoreOptions", "ResourceGovernorConfiguration","AzureDBResourceGovernance","RunningQueries","QueryText","QueryPlans","InternalPerformanceCounters","MemoryUsage","SessionWaits" };

            var tablesInDataSet = data.Tables
                 .Cast<DataTable>()
                 .Select(dt => dt.TableName);

            // Process standard tables in list order. Process Databases first as some collections will need the list of databases to be populated.
            foreach (string tableName in tables.Where(t => tablesInDataSet.Contains(t)))
            {
                try
                {
                    retryPolicy.Execute(
                          context => update(tableName),
                          new Context(tableName)
                      );
                }
                catch (Exception ex)
                {
                    logError(tableName, ex);
                    exceptions.Add(ex);
                }
            }
            // Process tables that are database schema snapshots
            string snapshotPrefix = "Snapshot_";
            foreach (string tableName in tablesInDataSet.Where(t=> t.StartsWith(snapshotPrefix)))
            {
                string databaseName = tableName.Substring(snapshotPrefix.Length);
                try
                {
                    retryPolicy.Execute(
                          context => updateSnapshot(tableName, databaseName),
                          new Context(tableName)
                      );
                }
                catch (Exception ex)
                {
                    logError(tableName, ex);
                    exceptions.Add(ex);
                }
            }
            try
            {
                retryPolicy.Execute(
                   context => updateServerExtraProperties(),
                   new Context("ServerExtraProperties")
               );
            }
            catch(Exception ex)
            {
                logError("ServerExtraProperties", ex);
                exceptions.Add(ex);
            }
            // retry based on policy then let caller handle the exception
            retryPolicy.Execute(
            context => InsertErrors(),
                            new Context("InsertErrors")
                );
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private void updateSnapshot(string tableName, string databaseName)
        {
            DataTable dtSS = data.Tables[tableName];
            try
            {
                using (var cn = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("DDLSnapshot_Add", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout })
                    {
                        cn.Open();
                        DateTime StartTime;
                        DateTime EndTime;
                        if (dtSS.ExtendedProperties.ContainsKey("StartTime"))
                        {
                            // legacy
                            StartTime = Convert.ToDateTime(dtSS.ExtendedProperties["StartTime"]);
                            EndTime = Convert.ToDateTime(dtSS.ExtendedProperties["EndTime"]);
                        }
                        else if (dtSS.ExtendedProperties.Contains("StartTimeBin"))
                        {
                            StartTime = DateTime.FromBinary(long.Parse((string)dtSS.ExtendedProperties["StartTimeBin"]));
                            EndTime = DateTime.FromBinary(long.Parse((string)dtSS.ExtendedProperties["EndTimeBin"]));
                        }
                        else
                        {
                            throw new Exception("Extended properies missing from schema snapshot");
                        }
                        string snapshotOptions = (string)dtSS.ExtendedProperties["SnapshotOptions"];
                        byte[] snapshptOptionsHash;
                        using (var crypt = SHA256.Create())
                        {
                            snapshptOptionsHash = crypt.ComputeHash(System.Text.Encoding.ASCII.GetBytes(snapshotOptions));
                        }
                        cmd.Parameters.AddWithValue("ss", dtSS);
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                        cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                        cmd.Parameters.AddWithValue("DB", databaseName);
                        cmd.Parameters.AddWithValue("StartTime", StartTime);
                        cmd.Parameters.AddWithValue("EndTime", EndTime);
                        cmd.Parameters.AddWithValue("SnapshotOptions", snapshotOptions);
                        cmd.Parameters.AddWithValue("SnapshotOptionsHash", snapshptOptionsHash);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                throw new Exception($"DDLSnapshot:{ databaseName}. Primary key violation.  This can occur if you have a case sensitive database collation that contains tables, SPs or other database objects with names that are no longer unique with a case insensitive comparison.", ex);
            }

        }

        private void updateServerExtraProperties()
        {
            if (data.Tables.Contains("ServerExtraProperties") && data.Tables["ServerExtraProperties"].Rows.Count == 1)
            {
                var r = data.Tables["ServerExtraProperties"].Rows[0];
                using (var cn = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("ServerExtraProperties_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout })
                    {
                        cn.Open();
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                        cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                        cmd.Parameters.AddWithValue("ActivePowerPlanGUID", r["ActivePowerPlanGUID"]);
                        cmd.Parameters.AddWithValue("ActivePowerPlan", r["ActivePowerPlan"]);
                        cmd.Parameters.AddWithValue("ProcessorNameString", r["ProcessorNameString"]);
                        cmd.Parameters.AddWithValue("SystemManufacturer", r["SystemManufacturer"]);
                        cmd.Parameters.AddWithValue("SystemProductName", r["SystemProductName"]);
                        cmd.Parameters.AddWithValue("IsAgentRunning", r["IsAgentRunning"]);
                        cmd.Parameters.AddWithValue("InstantFileInitializationEnabled", r["InstantFileInitializationEnabled"]);
                        cmd.Parameters.AddWithValue("OfflineSchedulers", r["OfflineSchedulers"]);
                        cmd.Parameters.AddWithValue("ResourceGovernorEnabled", r["ResourceGovernorEnabled"]);
                        if (r.Table.Columns.Contains("WindowsRelease"))
                        { // older version of the agent.  no longer collected here
                            cmd.Parameters.AddWithValue("WindowsRelease", r["WindowsRelease"]);
                            cmd.Parameters.AddWithValue("WindowsSP", r["WindowsServicePackLevel"]);
                            cmd.Parameters.AddWithValue("WindowsSKU", r["WindowsSKU"]);
                        }
                        cmd.Parameters.AddWithValue("LastMemoryDump", r["LastMemoryDump"]);
                        cmd.Parameters.AddWithValue("MemoryDumpCount", r["MemoryDumpCount"]);
                        cmd.Parameters.AddWithValue("WindowsCaption", r["WindowsCaption"]);
                        if (r.Table.Columns.Contains("DBMailStatus")) // Backward compatibility with older agent versions
                        {
                            cmd.Parameters.AddWithValue("DBMailStatus", r["DBMailStatus"]);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        private void update(string tableName)
        {
            var dt = data.Tables[tableName];
            using (var cn = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(dt.TableName + "_Upd", cn) { CommandTimeout = commandTimeout, CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    if (dt.Rows.Count > 0)
                    {
                        cmd.Parameters.AddWithValue(dt.TableName, dt);
                    }
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                    cmd.ExecuteNonQuery();

                }
            }
        }


        public void InsertErrors()
        {
            InsertErrors(connectionString, instanceID, snapshotDate, data);
        }

        public static void InsertErrors(string connectionString, Int32? instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Errors") && ds.Tables["Errors"].Rows.Count > 0)
            {
                if (ds.Tables["Errors"].Columns.Count == 2)
                {
                    ds.Tables["Errors"].Columns.Add("ErrorContext");
                }
                using (var cn = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("CollectionErrorLog_Add", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout })
                    {
                        cn.Open();

                        cmd.Parameters.AddWithValue("Errors", ds.Tables["Errors"]);
                        if (instanceID == null)
                        {
                            cmd.Parameters.AddWithValue("InstanceID", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("InstanceID", instanceID);
                        }
                        cmd.Parameters.AddWithValue("ErrorDate", SnapshotDate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }



        private Int32 updateInstance(ref DataRow rInstance)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand("Instance_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout })
                {
                    cn.Open();

                    cmd.Parameters.AddWithValue("ConnectionID", (string)rInstance["ConnectionID"]);
                    cmd.Parameters.AddWithValue("Instance", (string)rInstance["Instance"]);
                    cmd.Parameters.AddWithValue("SnapshotDate", (DateTime)rInstance["SnapshotDateUTC"]);

                    var importAgentID = importAgent.GetDBADashAgentID(connectionString);
                    int collectAgentID;
                    var collectAgent = new DBADashAgent()
                    {
                        AgentHostName = (string)rInstance["AgentHostName"],
                        AgentVersion = (string)rInstance["AgentVersion"],
                        AgentPath = rInstance.Table.Columns.Contains("AgentPath") ?  (string)rInstance["AgentPath"] : "",
                        AgentServiceName = rInstance.Table.Columns.Contains("AgentServiceName") ? (string)rInstance["AgentServiceName"] : "{DBADashService}",
                    };
                    if (collectAgent.Equals(importAgent))
                    {
                        collectAgentID = importAgentID;
                    }
                    else
                    {
                        collectAgentID = collectAgent.GetDBADashAgentID(connectionString);
                    }

                    cmd.Parameters.AddWithValue("CollectAgentID", collectAgentID);
                    cmd.Parameters.AddWithValue("ImportAgentID", importAgentID);
                    if (rInstance.Table.Columns.Contains("host_platform"))
                    {
                        cmd.Parameters.AddWithValue("HostPlatform", rInstance["host_platform"]);
                        cmd.Parameters.AddWithValue("HostDistribution", rInstance["host_distribution"]);
                        cmd.Parameters.AddWithValue("HostRelease", rInstance["host_release"]);
                        cmd.Parameters.AddWithValue("HostServicePackLevel", rInstance["host_service_pack_level"]);
                        cmd.Parameters.AddWithValue("HostSKU", rInstance["host_sku"]);
                        cmd.Parameters.AddWithValue("OSLanguageVersion", rInstance["os_language_version"]);
                    }
                    cmd.Parameters.AddWithValue("EditionID", (long)rInstance["EditionID"]);
                    if (rInstance.Table.Columns.Contains("UTCOffset"))
                    {
                        cmd.Parameters.AddWithValue("UTCOffset", Convert.ToInt32(rInstance["UTCOffset"]));
                    }
                    var pInstanceID = cmd.Parameters.Add("InstanceID", SqlDbType.Int);
                    pInstanceID.Direction = ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    return (Int32)pInstanceID.Value;
                }
            }
        }




    }
}
