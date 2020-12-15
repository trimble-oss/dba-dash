using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;

namespace DBAChecks
{
    public class DBImporter
    {

        public DBImporter()
        {

        }

        private void logError(string errorSource, string errorMessage, DataTable dt,string errorContext="Import")
        {
            logError(errorSource, errorMessage, dt.DataSet);
        }

        private void logError(string errorSource, string errorMessage, DataSet Data,string errorContext="Import")
        {
            DataTable dtErrors;
            if (Data.Tables.Contains("Errors"))
            {
                dtErrors = Data.Tables["Errors"];
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
                Data.Tables.Add(dtErrors);
            }

            Console.WriteLine(errorSource + " : " + errorMessage);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }

        public void Update(string connectionString, DataSet Data)
        {
            var rInstance = Data.Tables["DBAChecks"].Rows[0];
            DateTime snapshotDate = (DateTime)rInstance["SnapshotDateUTC"];
            Int32 instanceID;
            string connectionID = (string)rInstance["ConnectionID"];

            instanceID = updateInstance(connectionString, rInstance);


            updateDB(connectionString, instanceID, snapshotDate, Data);
            foreach (DataTable dt in Data.Tables)
            {
                string[] tables = { "Drives", "ServerProperties", "Backups", "AgentJobs", "LogRestores", "DBFiles", "DBConfig", "Corruption", "DatabasesHADR", "SysConfig", "OSInfo", "TraceFlags", "CPU", "Drivers", "BlockingSnapshot", "IOStats", "Waits", "OSLoadedModules", "DBTuningOptions", "AzureDBResourceStats", "AzureDBServiceObjectives", "AzureDBElasticPoolResourceStats", "SlowQueries", "SlowQueriesStats", "LastGoodCheckDB", "Alerts" ,"ObjectExecutionStats","ServerPrincipals","ServerRoleMembers","ServerPermissions","DatabasePrincipals","DatabaseRoleMembers","DatabasePermissions","CustomChecks","PerformanceCounters"};

                if (tables.Contains(dt.TableName))
                {
                    update(connectionString, instanceID, snapshotDate, dt);
                }
                string snapshotPrefix = "Snapshot_";
                if (dt.TableName.StartsWith(snapshotPrefix))
                {
                    string databaseName = dt.TableName.Substring(snapshotPrefix.Length);
                    updateSnapshot(connectionString, instanceID, snapshotDate, dt, databaseName);

                }
            }

            updateServerExtraProperties(connectionString, instanceID, snapshotDate, Data);
            InsertErrors(connectionString, instanceID, snapshotDate, Data);
        }

        private void updateSnapshot(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataTable dtSS, string databaseName)
        {

            try
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    DateTime StartTime = (DateTime)dtSS.ExtendedProperties["StartTime"];
                    DateTime EndTime = (DateTime)dtSS.ExtendedProperties["EndTime"];
                    string snapshotOptions = (string)dtSS.ExtendedProperties["SnapshotOptions"];
                    var crypt = new SHA256Managed();
                    var snapshptOptionsHash = crypt.ComputeHash(System.Text.Encoding.ASCII.GetBytes(snapshotOptions));
                   

                    SqlCommand cmd = new SqlCommand("DDLSnapshot_Add", cn);
                    cmd.Parameters.AddWithValue("ss", dtSS);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.Parameters.AddWithValue("DB", databaseName);
                    cmd.Parameters.AddWithValue("StartTime", StartTime);
                    cmd.Parameters.AddWithValue("EndTime", EndTime);
                    cmd.Parameters.AddWithValue("SnapshotOptions", snapshotOptions);
                    cmd.Parameters.AddWithValue("SnapshotOptionsHash",snapshptOptionsHash );
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
            catch(System.Data.SqlClient.SqlException ex) when(ex.Number == 2627)
            {
                logError("DDLSnapshot:" + databaseName,"Primary key violation.  This can occur if you have a case sensitive database collation that contains tables, SPs or other database objects with names that are no longer unique with a case insensitive comparison." + Environment.NewLine + ex.Message, dtSS);
            }
            catch (Exception ex)
            {
                logError("DDLSnapshot:" + databaseName, ex.Message, dtSS);
            }
        }

        private void updateServerExtraProperties(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("ServerExtraProperties") && ds.Tables["ServerExtraProperties"].Rows.Count == 1)
            {
                try
                {
                    var r = ds.Tables["ServerExtraProperties"].Rows[0];
                    var cn = new SqlConnection(connectionString);
                    using (cn)
                    {
                        cn.Open();
                        SqlCommand cmd = new SqlCommand("ServerExtraProperties_Upd", cn);
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                        cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                        cmd.Parameters.AddWithValue("ActivePowerPlanGUID", r["ActivePowerPlanGUID"]);
                        cmd.Parameters.AddWithValue("ActivePowerPlan", r["ActivePowerPlan"]);
                        cmd.Parameters.AddWithValue("ProcessorNameString", r["ProcessorNameString"]);
                        cmd.Parameters.AddWithValue("SystemManufacturer", r["SystemManufacturer"]);
                        cmd.Parameters.AddWithValue("SystemProductName", r["SystemProductName"]);
                        cmd.Parameters.AddWithValue("IsAgentRunning", r["IsAgentRunning"]);
                        cmd.Parameters.AddWithValue("InstantFileInitializationEnabled", r["InstantFileInitializationEnabled"]);
                        cmd.Parameters.AddWithValue("OfflineSchedulers", r["OfflineSchedulers"]);
                        cmd.Parameters.AddWithValue("ResourceGovernorEnabled", r["ResourceGovernorEnabled"]);
                        cmd.Parameters.AddWithValue("WindowsRelease", r["WindowsRelease"]);
                        cmd.Parameters.AddWithValue("WindowsSP", r["WindowsServicePackLevel"]);
                        cmd.Parameters.AddWithValue("WindowsSKU", r["WindowsSKU"]);
                        cmd.Parameters.AddWithValue("LastMemoryDump", r["LastMemoryDump"]);
                        cmd.Parameters.AddWithValue("MemoryDumpCount", r["MemoryDumpCount"]);
                        cmd.Parameters.AddWithValue("WindowsCaption", r["WindowsCaption"]);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    logError("ServerExtraProperties", ex.Message, ds);
                }
            }
        }


        private void update(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataTable dt)
        {
            try
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand(dt.TableName + "_Upd", cn);
                    if (dt.Rows.Count > 0)
                    {
                        cmd.Parameters.AddWithValue(dt.TableName, dt);
                    }
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                logError(dt.TableName, ex.Message, dt);
            }
        }



        public static void InsertErrors(string connectionString, Int32? instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Errors") && ds.Tables["Errors"].Rows.Count > 0)
            {
                if (ds.Tables["Errors"].Columns.Count == 2)
                {
                    ds.Tables["Errors"].Columns.Add("ErrorContext");
                }
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("CollectionErrorLog_Add", cn);
                    cmd.Parameters.AddWithValue("Errors", ds.Tables["Errors"]);
                    if (instanceID ==null)
                    {
                        cmd.Parameters.AddWithValue("InstanceID", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    }
                    cmd.Parameters.AddWithValue("ErrorDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }



        private Int32 updateInstance(string connectionString, DataRow rInstance)
        {

            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Instance_Upd", cn);
                cmd.Parameters.AddWithValue("ConnectionID", (string)rInstance["ConnectionID"]);
                cmd.Parameters.AddWithValue("Instance", (string)rInstance["Instance"]);
                cmd.Parameters.AddWithValue("SnapshotDate", (DateTime)rInstance["SnapshotDateUTC"]);

                cmd.Parameters.AddWithValue("AgentHostName", (string)rInstance["AgentHostName"]);
                if (rInstance.Table.Columns.Contains("AgentVersion"))
                {
                    cmd.Parameters.AddWithValue("AgentVersion", (string)rInstance["AgentVersion"]);
                }
                var pInstanceID = cmd.Parameters.Add("InstanceID", SqlDbType.Int);
                pInstanceID.Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                return (Int32)pInstanceID.Value;
            }
        }

        private void updateDB(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Databases") && ds.Tables["Databases"].Rows.Count > 0)
            {
                try
                {
                    var cn = new SqlConnection(connectionString);
                    using (cn)
                    {
                        cn.Open();
                        var dtDB = ds.Tables["Databases"];
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


                        SqlCommand cmd = new SqlCommand("Database_Upd", cn);
                        cmd.Parameters.AddWithValue("DB", ds.Tables["Databases"]);
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                        cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    logError("Database", ex.Message, ds);
                }
            }
        }


    }
}
