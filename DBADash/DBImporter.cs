using Microsoft.Data.SqlClient;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DBADash
{
    public class DBImporter
    {
        private readonly DataSet data;
        private readonly string connectionString;
        private readonly Policy retryPolicy;
        private int? instanceID;
        private DateTime snapshotDate;
        private readonly int CommandTimeout;
        private readonly DBADashAgent importAgent;

        public DBImporter(DataSet data, string connectionString, DBADashAgent importAgent, int commandTimeout = 60)
        {
            CommandTimeout = commandTimeout;
            this.importAgent = importAgent;
            this.data = data;
            UpgradeDS();
            this.connectionString = connectionString;

            retryPolicy = Policy.Handle<Exception>(ShouldRetry)
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                }, (exception, timeSpan, retryCount, context) =>
                {
                    LogError(context.OperationKey, exception, "Import[Retrying]");
                });
        }

        private static readonly HashSet<int> ExcludedErrorCodes = new()
        {
            2812, // Could not find stored procedure '%.*ls'.
            349,  // The procedure "%.*ls" has no parameter named "%.*ls".
            500,  // Trying to pass a table-valued parameter with %d column(s) where the corresponding user-defined table type requires %d column(s).
            245   // Conversion failed when converting the %ls value '%.*ls' to data type %ls.
        };

        public static bool ShouldRetry(Exception ex)
        {
            if (ex is SqlException sqlEx)
            {
                return !ExcludedErrorCodes.Contains(sqlEx.Number);
            }
            return true;
        }

        public async Task TestConnectionAsync()
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync();
        }

        // Adds error to Errors DataTable to be imported into CollectionErrorLog table later.
        private void LogError(string errorSource, Exception ex, string errorContext = "Import")
        {
            DataTable dtErrors;
            if (data.Tables.Contains("Errors"))
            {
                dtErrors = data.Tables["Errors"];
                if (dtErrors!.Columns.Count == 0)
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

            Log.Error(ex, "Error from {ErrorContext} {ErrorSource}", errorContext, errorSource);
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = ex.ToString();
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }

        // handle schema changes between agent versions
        private void UpgradeDS()
        {
            if (data.Tables.Contains("BlockingSnapshot"))
            {
                var bss = data.Tables["BlockingSnapshot"];
                if (!bss!.Columns.Contains("session_status"))
                {
                    bss.Columns.Add("session_status");
                }
                if (!bss.Columns.Contains("transaction_isolation_level"))
                {
                    bss.Columns.Add("transaction_isolation_level", typeof(short));
                }
            }
            if (data.Tables.Contains("DatabasesHADR"))
            {
                var dtDatabasesHADR = data.Tables["DatabasesHADR"];
                if (!dtDatabasesHADR!.Columns.Contains("replica_id"))
                {
                    dtDatabasesHADR.Columns.Add("replica_id", typeof(Guid));
                    dtDatabasesHADR.Columns.Add("group_id", typeof(Guid));
                    dtDatabasesHADR.Columns.Add("is_commit_participant", typeof(bool));
                    dtDatabasesHADR.Columns.Add("database_state", typeof(short));
                    dtDatabasesHADR.Columns.Add("is_local", typeof(bool));
                    dtDatabasesHADR.Columns.Add("secondary_lag_seconds", typeof(long));
                }

                if (!dtDatabasesHADR!.Columns.Contains("last_sent_time"))
                {
                    dtDatabasesHADR.Columns.Add("last_sent_time", typeof(DateTimeOffset));
                    dtDatabasesHADR.Columns.Add("last_received_time", typeof(DateTimeOffset));
                    dtDatabasesHADR.Columns.Add("last_hardened_time", typeof(DateTimeOffset));
                    dtDatabasesHADR.Columns.Add("last_redone_time", typeof(DateTimeOffset));
                    dtDatabasesHADR.Columns.Add("log_send_queue_size", typeof(long));
                    dtDatabasesHADR.Columns.Add("log_send_rate", typeof(long));
                    dtDatabasesHADR.Columns.Add("redo_queue_size", typeof(long));
                    dtDatabasesHADR.Columns.Add("redo_rate", typeof(long));
                    dtDatabasesHADR.Columns.Add("filestream_send_rate", typeof(long));
                    dtDatabasesHADR.Columns.Add("last_commit_time", typeof(DateTimeOffset));
                }
            }
            if (data.Tables.Contains("Databases"))
            {
                var dtDB = data.Tables["Databases"];
                if (dtDB!.Columns["owner_sid"]!.DataType == typeof(string))
                {
                    var pos = dtDB.Columns["owner_sid"].Ordinal;
                    dtDB.Columns["owner_sid"].ColumnName = "owner_sid_string";
                    var newCol = dtDB.Columns.Add("owner_sid", typeof(byte[]));
                    foreach (DataRow r in dtDB.Rows)
                    {
                        r["owner_sid"] = Convert.FromBase64String((string)r["owner_sid_string"]);
                    }
                    dtDB.Columns.Remove("owner_sid_string");
                    newCol.SetOrdinal(pos);
                }
                if (!dtDB.Columns.Contains("is_ledger_on"))
                {
                    dtDB.Columns.Add("is_ledger_on", typeof(bool));
                }
            }
            if (data.Tables.Contains("Backups"))
            {
                var dtBackups = data.Tables["Backups"];
                if (dtBackups!.Columns.Contains("LastBackup"))
                {
                    dtBackups.Columns["LastBackup"]!.ColumnName = "backup_start_date";
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
                if (!dtBackups.Columns.Contains("compression_algorithm"))
                {
                    dtBackups.Columns.Add("compression_algorithm", typeof(string));
                }
            }
            if (data.Tables.Contains("LogRestores"))
            {
                var dtLR = data.Tables["LogRestores"];
                if (!dtLR!.Columns.Contains("backup_time_zone"))
                {
                    dtLR.Columns.Add("backup_time_zone", typeof(short));
                }
            }
            if (data.Tables.Contains("SlowQueries"))
            {
                var dtSlowQueries = data.Tables["SlowQueries"];
                if (!dtSlowQueries!.Columns.Contains("session_id"))
                {
                    dtSlowQueries.Columns.Add("session_id", typeof(int));
                }
                if (!dtSlowQueries.Columns.Contains("context_info"))
                {
                    dtSlowQueries.Columns.Add("context_info", typeof(byte[]));
                }
                if (!dtSlowQueries.Columns.Contains("row_count"))
                {
                    dtSlowQueries.Columns.Add("row_count", typeof(long));
                }
            }

            if (data.Tables.Contains("RunningQueries"))
            {
                var dtRunningQueries = data.Tables["RunningQueries"];
                if (!dtRunningQueries!.Columns.Contains("login_time_utc"))
                {
                    dtRunningQueries.Columns.Add("login_time_utc", typeof(DateTime));
                }
                if (!dtRunningQueries.Columns.Contains("last_request_end_time_utc"))
                {
                    dtRunningQueries.Columns.Add("last_request_end_time_utc", typeof(DateTime));
                }
                if (!dtRunningQueries.Columns.Contains("context_info"))
                {
                    dtRunningQueries.Columns.Add("context_info", typeof(byte[]));
                }
                if (!dtRunningQueries.Columns.Contains("transaction_begin_time_utc"))
                {
                    dtRunningQueries.Columns.Add("transaction_begin_time_utc", typeof(DateTime));
                }
                if (!dtRunningQueries.Columns.Contains("is_implicit_transaction"))
                {
                    dtRunningQueries.Columns.Add("is_implicit_transaction", typeof(bool));
                }
            }

            if (data.Tables.Contains("IdentityColumns"))
            {
                var dtIdentityColumns = data.Tables["IdentityColumns"];
                if (!dtIdentityColumns!.Columns.Contains("schema_name"))
                {
                    dtIdentityColumns.Columns.Add("schema_name", typeof(string));
                }
            }
            if (data.Tables.Contains("Corruption"))
            {
                var dtCorruption = data.Tables["Corruption"];
                if (!dtCorruption!.Columns.Contains("CountOfRows"))
                {
                    dtCorruption.Columns.Add("CountOfRows", typeof(int));
                }
            }
            if (data.Tables.Contains("IOStats"))
            {
                var dtIOStats = data.Tables["IOStats"];
                if (!dtIOStats!.Columns.Contains("drive"))
                {
                    dtIOStats.Columns.Add("drive", typeof(char));
                    foreach (DataRow row in dtIOStats.Rows)
                    {
                        row["drive"] = "*";
                    }
                }
            }

            if (data.Tables.Contains("ServerProperties"))
            {
                var dtServerProperties = data.Tables["ServerProperties"];
                if (!dtServerProperties!.Columns.Contains("ProductMinorVersion")) // Check we haven't already processed table
                {
                    if (dtServerProperties.Columns.Contains("ProductMajorVersion")) // Remove old string columns
                    {
                        dtServerProperties.Columns.Remove("ProductMajorVersion");
                        dtServerProperties.Columns.Remove("ProductBuild");
                    }

                    dtServerProperties.Columns.Add("ProductMajorVersion", typeof(int));
                    dtServerProperties.Columns.Add("ProductMinorVersion", typeof(int));
                    dtServerProperties.Columns.Add("ProductBuild", typeof(int));
                    dtServerProperties.Columns.Add("ProductRevision", typeof(int));

                    foreach (DataRow row in dtServerProperties.Rows)
                    {
                        var sVersion = row["ProductVersion"] as string;
                        if (string.IsNullOrEmpty(sVersion)) continue;
                        try
                        {
                            var version = new Version(sVersion);
                            row["ProductMajorVersion"] = version.Major;
                            row["ProductMinorVersion"] = version.Minor;
                            row["ProductBuild"] = version.Build;
                            row["ProductRevision"] = version.Revision;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error parsing version info");
                        }
                    }
                }
            }
            if (data.Tables.Contains("ObjectExecutionStats") && !data.Tables["ObjectExecutionStats"]!.Columns.Contains("database_name"))
            {
                data.Tables["ObjectExecutionStats"].TableName = "ObjectExecutionStatsLegacy"; // Call ObjectExecutionStatsLegacy_Upd which will add the database_name column and call ObjectExecutionStats_Upd
            }
        }

        private readonly HashSet<string> tablesToProcess = new()
        {
            "Databases", "Drives", "ServerProperties", "Backups", "AgentJobs", "LogRestores", "DBFiles", "DBConfig",
            "Corruption", "DatabasesHADR", "SysConfig", "OSInfo", "TraceFlags", "CPU", "Drivers",
            "BlockingSnapshot", "IOStats", "Waits", "OSLoadedModules", "DBTuningOptions", "AzureDBResourceStats",
            "AzureDBServiceObjectives", "AzureDBElasticPoolResourceStats", "SlowQueries",
            "SlowQueriesStats", "LastGoodCheckDB", "Alerts", "ObjectExecutionStats", "ServerPrincipals",
            "ServerRoleMembers", "ServerPermissions", "DatabasePrincipals", "DatabaseRoleMembers",
            "DatabasePermissions", "CustomChecks", "PerformanceCounters", "VLF", "DatabaseMirroring",
            "Jobs", "JobHistory", "AvailabilityReplicas", "AvailabilityGroups", "JobSteps",
            "DatabaseQueryStoreOptions", "ResourceGovernorConfiguration", "AzureDBResourceGovernance",
            "RunningQueries", "QueryText", "QueryPlans", "InternalPerformanceCounters", "MemoryUsage",
            "SessionWaits", "IdentityColumns", "RunningJobs", "TableSize", "ServerServices","ObjectExecutionStatsLegacy"
        };

        private async Task UpdateOfflineAsync()
        {
            var agentRow = data.Tables["DBADash"]!.Rows[0];
            var collectAgent = GetAgent(agentRow);
            snapshotDate = (DateTime)agentRow["SnapshotDateUTC"];
            var dt = data.Tables["OfflineInstances"];
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("OfflineInstances_Add", cn) { CommandTimeout = CommandTimeout, CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync();
            if (dt.Rows.Count > 0)
            {
                cmd.Parameters.AddWithValue("OfflineInstances", dt);
            }

            cmd.Parameters.AddWithValue("CollectAgentID", collectAgent.GetDBADashAgentID(connectionString));
            cmd.Parameters.AddWithValue("ImportAgentID", importAgent.GetDBADashAgentID(connectionString));
            cmd.Parameters.AddWithValue("SnapshotDate", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync()
        {
            if (data.DataSetName == "OfflineInstances")
            {
                await UpdateOfflineAsync();
                return;
            }
            List<Exception> exceptions = new();
            var rInstance = data.Tables["DBADash"]?.Rows[0];
            snapshotDate = (DateTime)rInstance!["SnapshotDateUTC"];

            // we need to get the instanceID to continue further.  retry based on policy then exception will be thrown to catch higher up
            instanceID = await retryPolicy.Execute(async _ => await UpdateInstanceAsync(rInstance),
                new Context("Instance")
            );

            var tablesInDataSet = new HashSet<string>(data.Tables
                .Cast<DataTable>()
                .Select(dt => dt.TableName));

            // Process Databases first as some collections will need the list of databases to be populated.
            if (tablesInDataSet.Contains("Databases"))
            {
                await TryUpdateAsync("Databases", exceptions);
            }
            foreach (var tableName in tablesInDataSet.Where(tableName => tableName != "Databases"))
            {
                if (tablesToProcess.Contains(tableName) || tableName.StartsWith("UserData."))
                {
                    await TryUpdateAsync(tableName, exceptions);
                }
            }

            // Process tables that are database schema snapshots
            const string snapshotPrefix = "Snapshot_";
            foreach (var tableName in tablesInDataSet.Where(t => t.StartsWith(snapshotPrefix)))
            {
                var databaseName = tableName[snapshotPrefix.Length..];
                try
                {
                    await retryPolicy.Execute(async _ => await UpdateSnapshotAsync(tableName, databaseName),
                          new Context(tableName)
                      );
                }
                catch (Exception ex)
                {
                    LogError(tableName, ex);
                    exceptions.Add(ex);
                }
            }
            try
            {
                await retryPolicy.Execute(async _ => await UpdateServerExtraPropertiesAsync(),
                    new Context("ServerExtraProperties")
                );
            }
            catch (Exception ex)
            {
                LogError("ServerExtraProperties", ex);
                exceptions.Add(ex);
            }
            // retry based on policy then let caller handle the exception
            await retryPolicy.Execute(async _ => await InsertErrorsAsync(),
                new Context("InsertErrors")
            );
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private async Task UpdateSnapshotAsync(string tableName, string databaseName)
        {
            var dtSS = data.Tables[tableName];
            try
            {
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand("DDLSnapshot_Add", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout };
                await cn.OpenAsync();
                DateTime StartTime;
                DateTime EndTime;
                if (dtSS!.ExtendedProperties.ContainsKey("StartTime"))
                {
                    // legacy
                    StartTime = Convert.ToDateTime(dtSS.ExtendedProperties["StartTime"]);
                    EndTime = Convert.ToDateTime(dtSS.ExtendedProperties["EndTime"]);
                }
                else if (dtSS.ExtendedProperties.Contains("StartTimeBin"))
                {
                    StartTime = DateTime.FromBinary(long.Parse(((string)dtSS.ExtendedProperties["StartTimeBin"])!));
                    EndTime = DateTime.FromBinary(long.Parse(((string)dtSS.ExtendedProperties["EndTimeBin"])!));
                }
                else
                {
                    throw new Exception("Extended properties missing from schema snapshot");
                }
                var snapshotOptions = (string)dtSS.ExtendedProperties["SnapshotOptions"];
                var snapshotOptionsHash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(snapshotOptions!));
                cmd.Parameters.AddWithValue("ss", dtSS);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("DB", databaseName);
                cmd.Parameters.AddWithValue("StartTime", StartTime);
                cmd.Parameters.AddWithValue("EndTime", EndTime);
                cmd.Parameters.AddWithValue("SnapshotOptions", snapshotOptions);
                cmd.Parameters.AddWithValue("SnapshotOptionsHash", snapshotOptionsHash);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                throw new Exception($"DDLSnapshot:{databaseName}. Primary key violation.  This can occur if you have a case sensitive database collation that contains tables, SPs or other database objects with names that are no longer unique with a case insensitive comparison.", ex);
            }
        }

        private async Task UpdateServerExtraPropertiesAsync()
        {
            if (data.Tables.Contains("ServerExtraProperties") && data.Tables["ServerExtraProperties"]!.Rows.Count == 1)
            {
                var r = data.Tables["ServerExtraProperties"].Rows[0];
                await using var cn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand("ServerExtraProperties_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout };

                await cn.OpenAsync();
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
                if (r.Table.Columns.Contains("IsWindowsUpdate"))
                {
                    cmd.Parameters.AddWithValue("IsWindowsUpdate", r["IsWindowsUpdate"]);
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task TryUpdateAsync(string tableName, List<Exception> exceptions)
        {
            try
            {
                await retryPolicy.Execute(async _ => await UpdateAsync(tableName),
                    new Context(tableName)
                );
            }
            catch (SqlException ex) when (ex.Number == 2812 && tableName.StartsWith("UserData."))
            {
                var ex2 = new Exception(
                    "Warning: The stored procedure for custom collection not found.  Please create the stored procedure. ",
                    ex);
                LogError(tableName, ex2);
                exceptions.Add(ex2);
            }
            catch (SqlException ex) when (ex.Number == 349 && tableName.StartsWith("UserData."))
            {
                var ex2 = new Exception(
                    "Warning: The stored procedure for custom collection does not have the required parameters.  Please update the stored procedure. ",
                    ex);
                LogError(tableName, ex2);
                exceptions.Add(ex2);
            }
            catch (SqlException ex) when (ex.Number == 500 && tableName.StartsWith("UserData."))
            {
                var ex2 = new Exception(
                    "Warning: The associated used defined table type for the custom collection does not have the correct number of columns. ",
                    ex);
                LogError(tableName, ex2);
                exceptions.Add(ex2);
            }
            catch (Exception ex)
            {
                LogError(tableName, ex);
                exceptions.Add(ex);
            }
        }

        private async Task UpdateAsync(string tableName)
        {
            var dt = data.Tables[tableName];
            if (dt == null) return;
            await UpdateCollectionAsync(dt, instanceID, snapshotDate, connectionString, CommandTimeout);
        }

        public static async Task UpdateCollectionAsync(DataTable dt, int? instanceID, DateTime snapshotDate, string connectionString, int timeOut = 30)
        {
            var tableName = dt.TableName;
            var procName = (tableName.StartsWith("UserData.") ? "" : "dbo.") + tableName + "_Upd";
            var paramName = tableName.Replace("UserData.", "");
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(procName, cn) { CommandTimeout = timeOut, CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync();
            if (dt.Rows.Count > 0)
            {
                cmd.Parameters.AddWithValue(paramName, dt);
            }
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertErrorsAsync()
        {
            await InsertErrorsAsync(connectionString, instanceID, snapshotDate, data, CommandTimeout);
        }

        public static async Task InsertErrorsAsync(string connectionString, int? instanceID, DateTime SnapshotDate, DataSet ds, int commandTimeout)
        {
            if (!ds.Tables.Contains("Errors") || ds.Tables["Errors"]!.Rows.Count <= 0) return;
            var dt = ds.Tables["Errors"];
            await InsertErrorsAsync(connectionString, instanceID, SnapshotDate, dt, commandTimeout);
        }

        public static async Task InsertErrorsAsync(string connectionString, int? instanceID, DateTime SnapshotDate, DataTable dt,
            int commandTimeout = 30)
        {
            if (dt.Rows.Count == 0) return;
            if (dt.Columns.Count == 2)
            {
                dt.Columns.Add("ErrorContext");
            }

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("CollectionErrorLog_Add", cn)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };

            await cn.OpenAsync();

            cmd.Parameters.AddWithValue("Errors", dt);
            if (instanceID == null)
            {
                cmd.Parameters.AddWithValue("InstanceID", DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
            }
            cmd.Parameters.AddWithValue("ErrorDate", SnapshotDate);
            await cmd.ExecuteNonQueryAsync();
        }

        private static DBADashAgent GetAgent(DataRow row)
        {
            return new DBADashAgent()
            {
                AgentHostName = (string)row["AgentHostName"],
                AgentVersion = (string)row["AgentVersion"],
                AgentPath = row.Table.Columns.Contains("AgentPath") ? (string)row["AgentPath"] : "",
                AgentServiceName = row.Table.Columns.Contains("AgentServiceName") ? (string)row["AgentServiceName"] : "{DBADashService}",
                ServiceSQSQueueUrl = row.Table.Columns.Contains("ServiceSQSQueueUrl") && row["ServiceSQSQueueUrl"] != DBNull.Value ? (string)row["ServiceSQSQueueUrl"] : null,
                S3Path = row.Table.Columns.Contains("S3Path") && row["S3Path"] != DBNull.Value ? (string)row["S3Path"] : null,
                MessagingEnabled = row.Table.Columns.Contains("MessagingEnabled") && row["MessagingEnabled"] != DBNull.Value && (bool)row["MessagingEnabled"],
                AllowedScripts = row.Table.Columns.Contains("AllowedScripts") && row["AllowedScripts"] != DBNull.Value ? new HashSet<string>(((string)row["AllowedScripts"]).Split(',').Select(part => part.Trim())) : new HashSet<string>(),
            };
        }

        private async Task<int> UpdateInstanceAsync(DataRow rInstance)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Instance_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout };

            await cn.OpenAsync();
            var connectionID = (string)rInstance["ConnectionID"];
            cmd.Parameters.AddWithValue("ConnectionID", connectionID);
            cmd.Parameters.AddWithValue("Instance", (string)rInstance["Instance"]);
            cmd.Parameters.AddWithValue("SnapshotDate", (DateTime)rInstance["SnapshotDateUTC"]);

            var importAgentID = importAgent.GetDBADashAgentID(connectionString);
            var collectAgent = GetAgent(rInstance);
            var collectAgentID = collectAgent.Equals(importAgent) ? importAgentID : collectAgent.GetDBADashAgentID(connectionString);

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
            if (rInstance.Table.Columns.Contains("contained_availability_group_id"))
            {
                cmd.Parameters.AddWithValue("contained_availability_group_id", rInstance["contained_availability_group_id"]);
                cmd.Parameters.AddWithValue("contained_availability_group_name", rInstance["contained_availability_group_name"]);
            }
            if (rInstance.Table.Columns.Contains("EngineEdition"))
            {
                cmd.Parameters.AddWithValue("EngineEdition", rInstance["EngineEdition"]);
            }
            var pInstanceID = cmd.Parameters.Add(new SqlParameter("InstanceID", SqlDbType.Int) { Direction = ParameterDirection.Output });
            var pIsActive = cmd.Parameters.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Direction = ParameterDirection.Output });
            await cmd.ExecuteNonQueryAsync();
            if (!(bool)pIsActive.Value)
            {
                Log.Warning("Connection {ConnectionID} is marked deleted in the repository database.  Remove the connection from the service config tool and restart the service or use the recycle bin folder to restore the instance.", connectionID);
            }
            return (int)pInstanceID.Value;
        }
    }
}