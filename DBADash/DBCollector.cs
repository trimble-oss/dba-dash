using Microsoft.Data.SqlClient;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.SqlServer.Management.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;

namespace DBADash
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CollectionType
    {
        AgentJobs,
        Databases,
        DatabasesHADR,
        SysConfig,
        Drives,
        DBConfig,
        DBFiles,
        Corruption,
        OSInfo,
        TraceFlags,
        DriversWMI,
        CPU,
        BlockingSnapshot,
        IOStats,
        Waits,
        Backups,
        LogRestores,
        ServerProperties,
        ServerExtraProperties,
        OSLoadedModules,
        DBTuningOptions,
        AzureDBResourceStats,
        AzureDBServiceObjectives,
        AzureDBElasticPoolResourceStats,
        SlowQueries,
        LastGoodCheckDB,
        Alerts,
        ObjectExecutionStats,
        ServerPrincipals,
        ServerRoleMembers,
        ServerPermissions,
        DatabasePrincipals,
        DatabaseRoleMembers,
        DatabasePermissions,
        CustomChecks,
        PerformanceCounters,
        VLF,
        DatabaseMirroring,
        Jobs,
        JobHistory,
        AvailabilityReplicas,
        AvailabilityGroups,
        ResourceGovernorConfiguration,
        DatabaseQueryStoreOptions,
        AzureDBResourceGovernance,
        RunningQueries,
        MemoryUsage,
        SchemaSnapshot,
        IdentityColumns,
        Instance,
        RunningJobs,
        TableSize
    }

    public enum HostPlatform
    {
        Linux,
        Windows
    }

    public class DBCollector: IErrorLogger
    {
        public DataSet Data;
        private string ConnectionString => Source.SourceConnection.ConnectionString;
        private DataTable dtErrors;
        public bool LogInternalPerformanceCounters = false;
        private DataTable dtInternalPerfCounters;
        public int PerformanceCollectionPeriodMins = 60;
        private string computerName;
        private readonly CollectionType[] azureCollectionTypes = new[] { CollectionType.SlowQueries, CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceStats, CollectionType.CPU, CollectionType.DBFiles, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ObjectExecutionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions, CollectionType.SysConfig, CollectionType.DatabasePrincipals, CollectionType.DatabaseRoleMembers, CollectionType.DatabasePermissions, CollectionType.OSInfo, CollectionType.CustomChecks, CollectionType.PerformanceCounters, CollectionType.VLF, CollectionType.DatabaseQueryStoreOptions, CollectionType.AzureDBResourceGovernance, CollectionType.RunningQueries, CollectionType.IdentityColumns, CollectionType.TableSize };
        private readonly CollectionType[] azureOnlyCollectionTypes = new[] { CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceGovernance };
        private readonly CollectionType[] azureMasterOnlyCollectionTypes = new[] { CollectionType.AzureDBElasticPoolResourceStats };
        public DBADashSource Source;
        private bool noWMI;
        private bool IsAzureDB;
        private bool isAzureMasterDB;
        private bool IsRDS;
        private string instanceName;
        private string dbName;
        private string productVersion;
        private HostPlatform platform;
        public DateTime JobLastModified = DateTime.MinValue;
        private bool IsHadrEnabled;
        private Policy retryPolicy;
        private DatabaseEngineEdition engineEdition;
        private DBADashAgent dashAgent;
        public bool IsExtendedEventsNotSupportedException;
        private readonly bool DisableRetry;

        public const int DefaultIdentityCollectionThreshold = 5;

        /// <summary>
        /// % Used threshold for IdentityColumns collection
        /// </summary>
        public int IdentityCollectionThreshold = DefaultIdentityCollectionThreshold;

        private readonly CacheItemPolicy policy = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        };

        private readonly MemoryCache cache = MemoryCache.Default;
        private readonly Stopwatch swatch = new();
        private string currentCollection;
        private Version SQLVersion = new();

        private bool IsTableValuedConstructorsSupported => SQLVersion.Major > 9;

        private const int TableSizeCollectionThresholdMBDefault = 100;
        private const string TableSizeDatabasesDefault = "*";
        private const int TableSizeMaxTableThresholdDefault = 2000;
        private const int TableSizeMaxDatabaseThreshold = 500;
        public string ConnectionID => Data.Tables["DBADash"]?.Rows[0]["ConnectionID"].ToString();

        public int Job_instance_id
        {
            get
            {
                if (!Data.Tables.Contains("JobHistory")) return job_instance_id;
                var jh = Data.Tables["JobHistory"];
                if (jh is { Rows.Count: > 0 })
                {
                    job_instance_id = Convert.ToInt32(jh.Compute("max(instance_id)", string.Empty));
                }
                return job_instance_id;
            }
            set => job_instance_id = value;
        }

        private int job_instance_id;

        public DateTime GetJobLastModified()
        {
            using SqlConnection cn = new(ConnectionString);
            using SqlCommand cmd = new("SELECT MAX(date_modified) FROM msdb.dbo.sysjobs", cn);
            cn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? DateTime.MinValue : (DateTime)result;
        }

        public bool IsXESupported => !IsExtendedEventsNotSupportedException && DBADashConnection.IsXESupported(productVersion);

        public bool IsQueryStoreSupported => IsAzureDB || (!productVersion.StartsWith("8.") && !productVersion.StartsWith("9.") && !productVersion.StartsWith("10.") && !productVersion.StartsWith("11.") && !productVersion.StartsWith("12."));

        public DBCollector(DBADashSource source, string serviceName, bool disableRetry = false)
        {
            DisableRetry = disableRetry;
            Source = source;
            Startup();
        }

        public void LogError(Exception ex, string errorSource, string errorContext = "Collect")
        {
            Log.Error(ex, "{ErrorContext} {ErrorSource} {Connection}", errorContext, errorSource, Source.SourceConnection.ConnectionForPrint);
            LogDBError(errorSource, ex.ToString(), errorContext);
        }

        public void ClearErrors()
        {
            dtErrors.Rows.Clear();
        }

        public int ErrorCount => dtErrors.Rows.Count;

        private void LogDBError(string errorSource, string errorMessage, string errorContext = "Collect")
        {
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }

        private void CreateInternalPerformanceCountersDataTable()
        {
            dtInternalPerfCounters = new("InternalPerformanceCounters")
            {
                Columns =
                {
                    new DataColumn("SnapshotDate", typeof(DateTime)),
                    new DataColumn("object_name"),
                    new DataColumn("counter_name"),
                    new DataColumn("instance_name"),
                    new DataColumn("cntr_value", typeof(decimal)),
                    new DataColumn("cntr_type", typeof(int))
                }
            };
            Data.Tables.Add(dtInternalPerfCounters);
        }

        private void LogInternalPerformanceCounter(string objectName, string counterName, string instance, decimal counterValue)
        {
            if (!LogInternalPerformanceCounters) return;
            if (dtInternalPerfCounters == null)
            {
                CreateInternalPerformanceCountersDataTable();
            }
            var row = dtInternalPerfCounters!.NewRow();
            row["SnapshotDate"] = DateTime.UtcNow;
            row["object_name"] = objectName;
            row["counter_name"] = counterName;
            row["instance_name"] = instance;
            row["cntr_value"] = counterValue;
            row["cntr_type"] = 65792;
            dtInternalPerfCounters.Rows.Add(row);
        }

        private void StartCollection(string type)
        {
            swatch.Reset();
            swatch.Start();
            currentCollection = type;
        }

        private void StopCollection()
        {
            if (!swatch.IsRunning) return;
            swatch.Stop();
            Log.Debug("Collect {0} on {1} completed in {2}ms", currentCollection, instanceName, swatch.ElapsedMilliseconds);
            LogInternalPerformanceCounter("DBADash", "Collection Duration (ms)", currentCollection, Convert.ToDecimal(swatch.Elapsed.TotalMilliseconds));
        }

        private static readonly HashSet<int> ExcludedErrorCodes = new()
        {
            -2, // retryPolicy excludes query timeout #581
            218, // Could not find the type '%.*ls'. Either it does not exist or you do not have the necessary permission.
            219, // The type '%.*ls' already exists, or you do not have permission to create it.
            229, // The %ls permission was denied on the object '%.*ls', database '%.*ls', schema '%.*ls'.
            230, //	The %ls permission was denied on the column '%.*ls' of the object '%.*ls', database '%.*ls', schema '%.*ls'.
            245,   // Conversion failed when converting the %ls value '%.*ls' to data type %ls.
            262, // %ls permission denied in database '%.*ls'.
            297, //The user does not have permission to perform this action.
            300, // %ls permission was denied on object '%.*ls', database '%.*ls'.
            349,  // The procedure "%.*ls" has no parameter named "%.*ls".
            500,  // Trying to pass a table-valued parameter with %d column(s) where the corresponding user-defined table type requires %d column(s).
            2812, // Could not find stored procedure '%.*ls'.
            6335, // XML data type instance has too many levels of nested nodes. Maximum allowed depth is %d levels.
        };

        public bool ShouldRetry(Exception ex)
        {
            if (DisableRetry) return false;
            if (ex is SqlException sqlEx)
            {
                return !ExcludedErrorCodes.Contains(sqlEx.Number) && sqlEx.Message != "Max databases exceeded for Table Size collection";
            }
            return true;
        }

        private void Startup()
        {
            noWMI = Source.NoWMI;
            dashAgent = DBADashAgent.GetCurrent();

            retryPolicy = Policy.Handle<Exception>(ShouldRetry)
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10)
                }, (exception, _, _, context) =>
                {
                    LogError(exception, context.OperationKey, "Collect[Retrying]");
                });

            Data = new DataSet("DBADash");
            dtErrors = new("Errors")
            {
                Columns =
                {
                    new DataColumn("ErrorSource"),
                    new DataColumn("ErrorMessage"),
                    new DataColumn("ErrorContext")
                }
            };

            Data.Tables.Add(dtErrors);

            retryPolicy.Execute(
                _ => GetInstance(),
                new Context("Instance")
              );
        }

        public async Task RemoveEventSessionsAsync()
        {
            if (IsXESupported)
            {
                var removeSQL = IsAzureDB ? SqlStrings.RemoveEventSessionsAzure : SqlStrings.RemoveEventSessions;
                await using var cn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(removeSQL, cn);
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task StopEventSessionsAsync()
        {
            if (IsXESupported)
            {
                var removeSQL = IsAzureDB ? SqlStrings.StopEventSessionsAzure : SqlStrings.StopEventSessions;
                await using var cn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(removeSQL, cn);
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Add MetaData relating to the DBA Dash service used for collection.
        /// </summary>
        private void AddDBADashServiceMetaData(ref DataTable dt)
        {
            dt.Columns.AddRange(new[]
            {
                new DataColumn("AgentVersion", typeof(string)),
                new DataColumn("ConnectionID", typeof(string)),
                new DataColumn("AgentHostName", typeof(string)),
                new DataColumn("AgentServiceName", typeof(string)),
                new DataColumn("AgentPath", typeof(string)),
                new DataColumn("ServiceSQSQueueUrl",typeof(string)),
                new DataColumn("S3Path",typeof(string)),
                new DataColumn("MessagingEnabled", typeof(bool))
            });
            dt.Rows[0]["AgentVersion"] = dashAgent.AgentVersion;
            dt.Rows[0]["AgentHostName"] = dashAgent.AgentHostName;
            dt.Rows[0]["AgentServiceName"] = dashAgent.AgentServiceName;
            dt.Rows[0]["AgentPath"] = dashAgent.AgentPath;
            dt.Rows[0]["ServiceSQSQueueUrl"] = dashAgent.ServiceSQSQueueUrl;
            dt.Rows[0]["MessagingEnabled"] = dashAgent.MessagingEnabled;
        }

        public void GetInstance()
        {
            StartCollection(CollectionType.Instance.ToString());
            var dt = GetDT("DBADash", SqlStrings.Instance, CollectionCommandTimeout.GetDefaultCommandTimeout());
            AddDBADashServiceMetaData(ref dt);
            var clusterOrComputerName = (string)dt.Rows[0]["MachineName"];
            computerName = (string)dt.Rows[0]["ComputerNamePhysicalNetBIOS"];
            dbName = (string)dt.Rows[0]["DBName"];
            if (dt.Rows[0]["Instance"] == DBNull.Value)
            {
                dt.Rows[0]["Instance"] = "";
                Log.Warning("@@SERVERNAME returned NULL for {connection}.  Consider fixing with sp_addserver", Source.SourceConnection.ConnectionForPrint);
            }
            instanceName = (string)dt.Rows[0]["Instance"];
            productVersion = (string)dt.Rows[0]["ProductVersion"];
            if (!Version.TryParse(productVersion, out SQLVersion))
            {
                Log.Warning("Unable to parse ProductVersion to Version object");
            }
            string hostPlatform = (string)dt.Rows[0]["host_platform"];
            engineEdition = (DatabaseEngineEdition)Convert.ToInt32(dt.Rows[0]["EngineEdition"]);
            string containedAGName = Convert.ToString(dt.Rows[0]["contained_availability_group_name"]);

            if (!Enum.TryParse(hostPlatform, out platform))
            {
                Log.Error("GetInstance: host_platform parse error");
                LogDBError("Instance", "host_platform parse error");
                platform = HostPlatform.Windows;
            }
            if (platform == HostPlatform.Linux) // Disable WMI collection for Linux
            {
                Log.Debug("Instance {0} is a Linux instance. WMI collections are disabled.", instanceName);
                noWMI = true;
            }
            if (computerName.StartsWith("EC2AMAZ-")) // Disable WMI collection for RDS
            {
                Log.Debug("Instance {0} is a RDS instance. WMI collections are disabled.", instanceName);
                IsRDS = true;
                noWMI = true;
            }
            if (engineEdition == DatabaseEngineEdition.SqlDatabase)
            {
                IsAzureDB = true;
                if (dbName == "master")
                {
                    isAzureMasterDB = true;
                }
            }

            if (computerName.Length == 0)
            {
                noWMI = true;
            }
            if (string.IsNullOrEmpty(Source.ConnectionID))
            {
                if (IsAzureDB)
                {
                    dt.Rows[0]["ConnectionID"] = instanceName + "|" + dbName;
                    noWMI = true;
                }
                else if (!string.IsNullOrEmpty(containedAGName)) // Use the name of the contained AG as the ConnectionID.
                {
                    dt.Rows[0]["ConnectionID"] = containedAGName;
                }
                else if (string.IsNullOrEmpty(instanceName))
                {
                    dt.Rows[0]["ConnectionID"] = clusterOrComputerName;
                }
                else
                {
                    dt.Rows[0]["ConnectionID"] = instanceName;
                }
            }
            else
            {
                dt.Rows[0]["ConnectionID"] = Source.ConnectionID;
            }
            IsHadrEnabled = dt.Rows[0]["IsHadrEnabled"] != DBNull.Value && Convert.ToBoolean(dt.Rows[0]["IsHadrEnabled"]);

            Data.Tables.Add(dt);
            StopCollection();
        }

        public void Collect(CollectionType[] collectionTypes)
        {
            foreach (CollectionType type in collectionTypes)
            {
                Collect(type);
            }
        }

        public void Collect(Dictionary<string, CustomCollection> customCollections)
        {
            foreach (var customCollection in customCollections)
            {
                var collectionReference = "UserData." + customCollection.Key;
                try
                {
                    retryPolicy.Execute(
                        _ =>
                        {
                            StartCollection(collectionReference);
                            Collect(customCollection);
                            StopCollection();
                        },
                        new Context(collectionReference)
                    );
                }
                catch (SqlException ex) when (ex.Number == 2812)
                {
                    var message = $"Warning: Custom collection stored procedure {customCollection.Value.ProcedureName} doesn't exist";
                    LogDBError(collectionReference, message);
                    Log.Error(ex, message);
                }
                catch (Exception ex)
                {
                    StopCollection();
                    LogError(ex, collectionReference);
                }
            }
        }

        public void Collect(KeyValuePair<string, CustomCollection> customCollection)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(customCollection.Value.ProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = customCollection.Value.CommandTimeout ?? CollectionCommandTimeout.GetDefaultCommandTimeout() };
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable("UserData." + customCollection.Key);
            da.Fill(dt);
            Data.Tables.Add(dt);
        }

        private static string EnumToString(Enum en)
        {
            return Enum.GetName(en.GetType(), en);
        }

        private bool CollectionTypeIsApplicable(CollectionType collectionType)
        {
            var collectionTypeString = EnumToString(collectionType);
            if (collectionType == CollectionType.DatabaseQueryStoreOptions && !IsQueryStoreSupported)
            {
                // Query store not supported on this instance
                return false;
            }
            else if (Data.Tables.Contains(collectionTypeString))
            {
                // Already collected
                return false;
            }
            else if (IsAzureDB && (!azureCollectionTypes.Contains(collectionType)))
            {
                // Collection Type doesn't apply to AzureDB
                return false;
            }
            else if (!IsAzureDB && azureOnlyCollectionTypes.Contains(collectionType))
            {
                // Collection Type doesn't apply to normal standalone instance
                return false;
            }
            else if (azureMasterOnlyCollectionTypes.Contains(collectionType) && !isAzureMasterDB)
            {
                // Collection type only applies to Azure master db
                return false;
            }
            else if (!IsHadrEnabled & (collectionType == CollectionType.AvailabilityGroups || collectionType == CollectionType.AvailabilityReplicas || collectionType == CollectionType.DatabasesHADR))
            {
                // Availability group collection and Hadr isn't enabled.
                return false;
            }
            else if (collectionType == CollectionType.SchemaSnapshot)
            {
                //Schema snapshots are not handled via DBCollector
                return false;
            }
            else if (collectionType == CollectionType.ResourceGovernorConfiguration)
            {
                return SQLVersion.Major > 9 && engineEdition == DatabaseEngineEdition.Enterprise && !IsAzureDB; // Must be enterprise edition and not AzureDB, SQL 2005
            }
            else if ((new[] { CollectionType.Backups, CollectionType.DatabaseMirroring, CollectionType.LogRestores, CollectionType.AvailabilityGroups, CollectionType.AvailabilityReplicas, CollectionType.DatabasesHADR }).Contains(collectionType)
                        && engineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                // Don't need to collect these types for Azure MI
                return false;
            }
            else if ((new[] { CollectionType.JobHistory, CollectionType.AgentJobs, CollectionType.Jobs, CollectionType.RunningJobs }).Contains(collectionType) && engineEdition == DatabaseEngineEdition.Express)
            {
                // SQL Agent not supported on express
                return false;
            }
            else if (collectionType == CollectionType.Drives && platform != HostPlatform.Windows) // drive collection not supported on linux
            {
                return false;
            }
            else if (collectionType == CollectionType.SlowQueries)
            {
                return Source.SlowQueryThresholdMs >= 0 && (!(IsAzureDB && isAzureMasterDB)); // Threshold must be set.  Azure master DB is excluded
            }
            else if (collectionType == CollectionType.Instance)
            {
                return false; // Required & collected by default
            }
            else if (collectionType == CollectionType.TableSize && SQLVersion.Major <= 12 && !IsAzureDB)
            {
                return false; // Table size collection not supported on SQL 2014 and below
            }
            else
            {
                return true;
            }
        }

        public void Collect(CollectionType collectionType)
        {
            var collectionTypeString = EnumToString(collectionType);

            if (!CollectionTypeIsApplicable(collectionType))
            {
                return;
            }

            try
            {
                retryPolicy.Execute(
                    _ =>
                    {
                        StartCollection(collectionType.ToString());
                        ExecuteCollection(collectionType);
                        StopCollection();
                    },
                    new Context(collectionTypeString)
                );
            }
            catch (Exception ex)
            {
                LogError(ex, collectionTypeString);
                StopCollection();
            }
            if (collectionType == CollectionType.RunningQueries)
            {
                try
                {
                    CollectText();
                }
                catch (Exception ex)
                {
                    LogError(new Exception("Error collecting text for Running Queries", ex), "RunningQueries");
                }
                try
                {
                    CollectPlans();
                }
                catch (Exception ex)
                {
                    LogError(new Exception("Error collecting plans for Running Queries", ex), "RunningQueries");
                }
            }
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }

        private DataTable GetPlans(string plansSQL)
        {
            if (string.IsNullOrEmpty(plansSQL)) return null;

            using var cn = new SqlConnection(ConnectionString);
            using var da = new SqlDataAdapter(plansSQL, cn);
            var dt = new DataTable("QueryPlans");
            da.Fill(dt);
            return dt;
        }

        private void CollectPlans()
        {
            if (!Data.Tables.Contains("RunningQueries") || !Source.PlanCollectionEnabled) return;
            var plansSQL = GetPlansSQL();

            var dt = GetPlans(plansSQL);

            if (dt is { Rows.Count: > 0 })
            {
                dt.Columns.Add("query_plan_hash", typeof(byte[]));
                dt.Columns.Add("query_plan_compressed", typeof(byte[]));
                foreach (DataRow r in dt.Rows)
                {
                    try
                    {
                        string strPlan = r["query_plan"] == DBNull.Value ? string.Empty : (string)r["query_plan"];
                        r["query_plan_compressed"] = SMOBaseClass.Zip(strPlan);
                        var hash = GetPlanHash(strPlan);
                        r["query_plan_hash"] = hash;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error processing query plans");
                    }
                }
                dt.Columns.Remove("query_plan");

                var nullRows = dt.Select("query_plan_hash IS NULL");

                // Remove rows with null query plan hash
                if (nullRows.Length > 0)
                {
                    Log.Information("Removing {0} rows with NULL query_plan_hash", nullRows.Length);
                    foreach (var row in nullRows)
                    {
                        row.Delete();
                    }
                    dt.AcceptChanges();
                }
                if (dt.Rows.Count > 0) // Check if we still have rows
                {
                    Data.Tables.Add(dt);
                }
            }
            LogInternalPerformanceCounter("DBADash", "Count of plans collected", "", dt?.Rows.Count ?? 0); // Count of plans actually collected - might be less than the list of plans we wanted to collect
        }

        ///<summary>
        ///Get the query plan hash from a  string of the plan XML
        ///</summary>
        public static byte[] GetPlanHash(string strPlan)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(strPlan)))
            {
                ms.Position = 0;
                using (var xr = new XmlTextReader(ms))
                {
                    while (xr.Read())
                    {
                        if (xr.Name == "StmtSimple")
                        {
                            string strHash = xr.GetAttribute("QueryPlanHash");
                            return StringToByteArray(strHash);
                        }
                    }
                }
            }
            return Array.Empty<byte>();
        }

        public static byte[] StringToByteArray(string hex)
        {
            if (hex.StartsWith("0x"))
            {
                hex = hex.Remove(0, 2);
            }
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        ///<summary>
        ///Generate a SQL query to get the query plan text for running queries. Captured plan handles get cached with a call to CacheCollectedPlans later <br/>
        ///Limits the cost associated with plan capture - less plans to capture, send and process<br/>
        ///Note: Caching takes query_plan_hash into account as a statement can get recompiled without the plan handle changing.
        ///</summary>
        public string GetPlansSQL()
        {
            var plans = GetPlansList();
            var sb = new StringBuilder();
            sb.Append(@"DECLARE @plans TABLE(plan_handle VARBINARY(64),statement_start_offset int,statement_end_offset int)
INSERT INTO @plans(plan_handle,statement_start_offset,statement_end_offset)
VALUES");

            // Already have a distinct list by plan handle, hash and offsets.
            // Filter this list by plans not already collected and get a distinct list by handle and offsets (excluding the hash as this can cause duplicates in rare cases)
            var collectList = plans.Where(p => !cache.Contains(p.Key))
                .GroupBy(p => Convert.ToBase64String(p.PlanHandle.Concat(BitConverter.GetBytes(p.StartOffset)).Concat(BitConverter.GetBytes(p.EndOffset)).ToArray()))
                .Select(p => p.First())
                .ToList();

            collectList.ForEach(p => sb.AppendFormat("{3}(0x{0},{1},{2}),", ByteArrayToHexString(p.PlanHandle), p.StartOffset, p.EndOffset, Environment.NewLine));

            Log.Information("Plans {0}, {1} to collect from {2}", plans.Count, collectList.Count, instanceName);

            LogInternalPerformanceCounter("DBADash", "Count of plans meeting threshold for collection", "", plans.Count); // Total number of plans that meet the threshold for collection
            LogInternalPerformanceCounter("DBADash", "Count of plans to collect", "", collectList.Count); // Total number of plans we want to collect (plans that meet the threshold that are not cached)
            LogInternalPerformanceCounter("DBADash", "Count of plans from cache", "", plans.Count - collectList.Count); // Plan count we didn't collect because they have been collected previously and we cached the handles/hashes.

            if (collectList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine("OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                sb.AppendLine();
                sb.Append(@"SELECT t.plan_handle,
        t.statement_start_offset,
        t.statement_end_offset,
        pln.dbid,
        pln.objectid,
        pln.encrypted,
        pln.query_plan
FROM @plans t
CROSS APPLY sys.dm_exec_text_query_plan(t.plan_handle,t.statement_start_offset,t.statement_end_offset) pln
OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                return sb.ToString();
            }
        }

        ///<summary>
        ///Get a list of plan handles from RunningQueries including statement start/end offsets as we want to capture plans at the statement level. Query plan hash is used to detect changes in the plan for caching purposes <br/>
        ///Capture a distinct list so we collect the plan for each statement once even if there are multiple instances of statements running with the same plan.<br/>
        ///Filter for plans matching the specified threshold to limit the plans captured to the ones that are likely to be of interest<br/>
        ///</summary>
        private List<Plan> GetPlansList()
        {
            if (Data.Tables.Contains("RunningQueries"))
            {
                DataTable dt = Data.Tables["RunningQueries"];
                var plans = (from r in dt!.AsEnumerable()
                             where r["plan_handle"] != DBNull.Value
                             && r["query_plan_hash"] != DBNull.Value
                             && r["statement_start_offset"] != DBNull.Value
                             && r["statement_end_offset"] != DBNull.Value
                             && ((byte[])r["query_plan_hash"]).Any(b => b != 0) // Not 0x00000000
                             group r by new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]) into g
                             where g.Sum(r => Convert.ToInt64(r["cpu_time"])) >= Source.PlanCollectionCPUThreshold || g.Sum(r => Convert.ToInt64(r["granted_query_memory"])) >= Source.PlanCollectionMemoryGrantThreshold || g.Count() >= Source.PlanCollectionCountThreshold || g.Max(r => ((DateTime)r["SnapshotDateUTC"]).Subtract((DateTime)r["last_request_start_time_utc"])).TotalMilliseconds >= Source.PlanCollectionDurationThreshold
                             select g.Key).Distinct().ToList();
                return plans;
            }
            else
            {
                return new List<Plan>();
            }
        }

        ///<summary>
        ///Collect query text associated with captured running queries
        ///</summary>
        private void CollectText()
        {
            if (!Data.Tables.Contains("RunningQueries")) return;
            var handlesSQL = GetTextFromHandlesSQL();
            if (string.IsNullOrEmpty(handlesSQL)) return;
            using var cn = new SqlConnection(ConnectionString);
            using var da = new SqlDataAdapter(handlesSQL, cn);
            var dt = new DataTable("QueryText");
            da.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                Data.Tables.Add(dt);
            }
            LogInternalPerformanceCounter("DBADash", "Count of text collected", "", dt.Rows.Count); // Count of text collected from sql_handles
            LogInternalPerformanceCounter("DBADash", "Count of running queries", "", Data.Tables["RunningQueries"]!.Rows.Count); // Total number of running queries
        }

        ///<summary>
        ///Once written to the destination, call this function to cache the plan handles and query plan hash. If the plan is cached it won't be collected in future.<br/>
        ///Note: We are just caching the plan handle and hash with the statement offsets.
        ///</summary>
        public void CacheCollectedPlans()
        {
            if (!Data.Tables.Contains("QueryPlans")) return;
            var dt = Data.Tables["QueryPlans"];
            foreach (DataRow r in dt!.Rows)
            {
                if (r["query_plan_hash"] != DBNull.Value)
                {
                    var plan = new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]);
                    cache.Add(plan.Key, "", policy);
                }
            }
        }

        ///<summary>
        ///Once written to the destination, call this function to cache the sql_handles for captured query text. If the handle is cached it won't be collected in future.<br/>
        ///Note: We capture text at the batch level and can use the statement offsets to get the statement text.
        ///</summary>
        public void CacheCollectedText()
        {
            if (!Data.Tables.Contains("QueryText")) return;
            var dt = Data.Tables["QueryText"];
            foreach (DataRow r in dt!.Rows)
            {
                cache.Add(ByteArrayToHexString((byte[])r["sql_handle"]), "", policy);
            }
        }

        ///<summary>
        ///Generate a SQL query to get the query text associated with the plan handles for running queries
        ///</summary>
        private string GetTextFromHandlesSQL()
        {
            var handles = RunningQueriesHandles();
            int cnt = 0;
            int cacheCount = 0;
            var sb = new StringBuilder();
            sb.Append(@"DECLARE @handles TABLE(sql_handle VARBINARY(64))
INSERT INTO @handles(sql_handle)");

            if (IsTableValuedConstructorsSupported)
            {
                sb.Append("\nVALUES");
            }

            foreach (string strHandle in handles)
            {
                if (!cache.Contains(strHandle))
                {
                    if (IsTableValuedConstructorsSupported)
                    {
                        if (cnt > 0) sb.Append(',');
                        sb.Append($"(0x{strHandle})");
                    }
                    else
                    {
                        if (cnt > 0) sb.Append("\nUNION ALL");
                        sb.Append($"\nSELECT 0x{strHandle}");
                    }
                    cnt++;
                }
                else
                {
                    cacheCount += 1;
                }
            }

            LogInternalPerformanceCounter("DBADash", "Distinct count of text (sql_handle)", "", handles.Count); // Total number of distinct sql_handles
            LogInternalPerformanceCounter("DBADash", "Count of text (sql_handle) to collect", "", cnt); // Count of sql_handles we need to collect
            LogInternalPerformanceCounter("DBADash", "Count of text (sql_handle) from cache", "", handles.Count - cnt); // Count of sql_handles we didn't need to collect because they were collected previously and we cached the sql_handle.

            if ((cnt + cacheCount) > 0)
            {
                Log.Information("QueryText: {0} from cache, {1} to collect from {2}", cacheCount, cnt, instanceName);
            }
            if (cnt == 0)
            {
                return string.Empty;
            }
            else
            {
                sb.AppendLine("OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(@"SELECT H.sql_handle,
    txt.dbid,
    txt.objectid as object_id,
    txt.encrypted,
    txt.text
FROM @handles H
CROSS APPLY sys.dm_exec_sql_text(H.sql_handle) txt
OPTION(RECOMPILE)"); // Plan caching is not beneficial.  RECOMPILE hint to avoid polluting the plan cache
                return sb.ToString();
            }
        }

        ///<summary>
        ///Get a distinct list of sql_handle for running queries.  The handles are later used to capture query text
        ///</summary>
        private List<string> RunningQueriesHandles()
        {
            var handles = (from r in Data.Tables["RunningQueries"]!.AsEnumerable()
                           where r["sql_handle"] != DBNull.Value
                           select ByteArrayToHexString((byte[])r["sql_handle"])).Distinct().ToList();
            return handles;
        }

        private void ExecuteCollection(CollectionType collectionType)
        {
            var collectionTypeString = EnumToString(collectionType);
            // Add params where required
            SqlParameter[] param = null;
            if (collectionType == CollectionType.JobHistory)
            {
                param = new[] { new SqlParameter { DbType = DbType.Int32, Value = Job_instance_id, ParameterName = "instance_id" }, new SqlParameter { DbType = DbType.Int32, ParameterName = "run_date", Value = Convert.ToInt32(DateTime.Now.AddDays(-7).ToString("yyyyMMdd")) } };
                Log.Debug("JobHistory From {JobInstanceID} on {Instance}", Job_instance_id, Source.SourceConnection.ConnectionForPrint);
            }
            else if (collectionType is CollectionType.AzureDBResourceStats or CollectionType.AzureDBElasticPoolResourceStats)
            {
                param = new[] { new SqlParameter("Date", DateTime.UtcNow.AddMinutes(-PerformanceCollectionPeriodMins)) };
            }
            else if (collectionType == CollectionType.CPU)
            {
                param = new[] { new SqlParameter("TOP", PerformanceCollectionPeriodMins) };
            }
            else if (collectionType == CollectionType.IdentityColumns)
            {
                param = new[] { new SqlParameter("IdentityCollectionThreshold", IdentityCollectionThreshold) };
            }
            else if (collectionType == CollectionType.IOStats)
            {
                param = new[] { new SqlParameter("IOCollectionLevel", Source.IOCollectionLevel) };
            }
            else if (collectionType == CollectionType.TableSize)
            {
                param = new[] { new SqlParameter("SizeThresholdMB", Source.TableSizeCollectionThresholdMB ?? TableSizeCollectionThresholdMBDefault),
                    new SqlParameter("TableSizeDatabases", Source.TableSizeDatabases ?? TableSizeDatabasesDefault),
                    new SqlParameter("MaxTables", Source.TableSizeMaxTableThreshold ?? TableSizeMaxTableThresholdDefault ),
                    new SqlParameter("MaxDatabases", Source.TableSizeMaxDatabaseThreshold ?? TableSizeMaxDatabaseThreshold)
                };
            }

            if (collectionType == CollectionType.Drives)
            {
                CollectDrives();
            }
            else if (collectionType == CollectionType.ServerExtraProperties)
            {
                CollectServerExtraProperties();
            }
            else if (collectionType == CollectionType.DriversWMI)
            {
                CollectDriversWMI();
            }
            else if (collectionType == CollectionType.SlowQueries)
            {
                try
                {
                    CollectSlowQueries();
                }
                catch (SqlException ex) when (ex.Message.StartsWith("Unable to create/alter extended events session: RDS for SQL Server supports extended events"))
                {
                    // RDS instances only support extended events for Standard and Enterprise editions.  If we encounter an error because it's not supported, log the error and continue
                    // Set IsExtendedEventsNotSupportedException which is used to disable the collection for future schedules
                    Log.Warning("Slow query capture relies on extended events which is not supported for {0}: {1}", instanceName, ex.Message);
                    LogDBError(collectionTypeString, "Slow query capture relies on extended events which is not supported for this instance", "Collect[Warning]");
                    IsExtendedEventsNotSupportedException = true;
                }
            }
            else if (collectionType == CollectionType.PerformanceCounters)
            {
                CollectPerformanceCounters();
            }
            else if (collectionType == CollectionType.Jobs)
            {
                var currentJobModified = GetJobLastModified();
                if (currentJobModified > JobLastModified)
                {
                    var ss = new AgentJobs(Source.SourceConnection, new SchemaSnapshotDBOptions());
                    try
                    {
                        //ss.SnapshotJobs(ref Data);
                        ss.CollectJobs(ref Data, Source.ScriptAgentJobs,IsRDS);
                        JobLastModified = currentJobModified;
                    }
                    catch (Microsoft.SqlServer.Management.Smo.UnsupportedFeatureException ex)
                    {
                        Log.Warning("SnapshotJobs not supported: {0}. From {1}", ex.Message, Source.SourceConnection.ConnectionForPrint);
                    }
                    catch (AggregateException ex) // thrown if any issues scripting jobs
                    {
                        LogDBError(collectionTypeString, ex.ToString());
                    }
                }
                else
                {
                    Log.Information("Skipping jobs collection for {Instance}.  Not Modified since {JobLastModified}.", instanceName, JobLastModified);
                }
            }
            else if (collectionType == CollectionType.ResourceGovernorConfiguration)
            {
                CollectResourceGovernor();
            }
            else if (collectionType == CollectionType.RunningQueries)
            {
                CollectRunningQueries();
            }
            else if (collectionType == CollectionType.RunningJobs)
            {
                CollectRunningJobs();
            }
            else
            {
                AddDT(collectionTypeString, SqlStrings.GetSqlString(collectionType), collectionType.GetCommandTimeout(), param);
            }
        }


        private DataTable GetRunningJobsSchema()
        {
            var dtRunningJobs = new DataTable("RunningJobs");
            dtRunningJobs.Columns.Add("job_id", typeof(Guid));
            dtRunningJobs.Columns.Add("run_requested_date_utc", typeof(DateTime));
            dtRunningJobs.Columns.Add("run_requested_source", typeof(string));
            dtRunningJobs.Columns.Add("queued_date_utc", typeof(DateTime));
            dtRunningJobs.Columns.Add("start_execution_date_utc", typeof(DateTime));
            dtRunningJobs.Columns.Add("last_executed_step_id", typeof(int));
            dtRunningJobs.Columns.Add("last_executed_step_date_utc", typeof(DateTime));
            dtRunningJobs.Columns.Add("SnapshotDate", typeof(DateTime));

            dtRunningJobs.Columns.Add("current_execution_step_id", typeof(int));
            dtRunningJobs.Columns.Add("current_execution_step_name", typeof(string));
            dtRunningJobs.Columns.Add("current_retry_attempt", typeof(int));
            dtRunningJobs.Columns.Add("current_execution_status", typeof(int));
            return dtRunningJobs;
        }

        private void CollectRunningJobsRDS() //Permissions Issue with msdb.dbo.syssessions prevents usual collection.  Get the data we can from sp_help_job
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand("msdb.dbo.sp_help_job", cn) { CommandType = CommandType.StoredProcedure};
            cmd.Parameters.AddWithValue("@execution_status", 0);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var dtRunningJobs = GetRunningJobsSchema();
            var snapshotDate = DateTime.UtcNow;
            while (rdr.Read())
            {
                var row = dtRunningJobs.NewRow();
                row["job_id"] = rdr["job_id"];
                var step = rdr["current_execution_step"] as string;
                ParseJobStep(step, out int stepId, out string stepName);
                row["current_execution_step_id"] = stepId;
                row["current_execution_step_name"] = stepName;
                row["current_retry_attempt"] = rdr["current_retry_attempt"];
                row["current_execution_status"] = rdr["current_execution_status"];
                row["SnapshotDate"] = snapshotDate;
                dtRunningJobs.Rows.Add(row);
            }
            Data.Tables.Add(dtRunningJobs);
        }

        private void CollectRunningJobs()
        {
            if (IsRDS)
            {
                CollectRunningJobsRDS();
                return;
            }
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(SqlStrings.RunningJobs, cn);
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var dtRunningJobs = GetRunningJobsSchema();
            dtRunningJobs.Load(rdr);
            
            var spHelpJobInfo = new Dictionary<Guid, (int ExecutionStepID, string ExecutionStep, int RetryAttempts, int ExecutionStatus)>();

            while (rdr.Read())
            {
                var step = rdr["current_execution_step"] as string;
                var jobId = (Guid)rdr["job_id"];
                var retry = (int)rdr["current_retry_attempt"];
                var status = (int)rdr["current_execution_status"];

                ParseJobStep(step, out int stepId, out string stepName);
                spHelpJobInfo[jobId] = (stepId, stepName, retry, status);
            }

            foreach (DataRow row in dtRunningJobs.Rows)
            {
                var jobId = (Guid)row["job_id"];
                if (!spHelpJobInfo.TryGetValue(jobId, out var info)) continue;
                row["current_execution_step_id"] = info.ExecutionStepID;
                row["current_execution_step_name"] = info.ExecutionStep;
                row["current_retry_attempt"] = info.RetryAttempts;
                row["current_execution_status"] = info.ExecutionStatus;
            }
            Data.Tables.Add(dtRunningJobs);
        }

        private static bool ParseJobStep(string jobStepAndId, out int stepId, out string stepName)
        {
            stepId = -1;
            stepName = jobStepAndId;

            // Using regular expression to extract ID and name
            var pattern = @"(\d+) \(([^)]+)\)";
            var match = Regex.Match(jobStepAndId, pattern);

            if (!match.Success) return false;
            if (match.Groups.Count != 3) return false;
            if (!int.TryParse(match.Groups[1].Value, out stepId)) return false;
            stepName = match.Groups[2].Value;
            return true;
        }

        private void CollectRunningQueries()
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(SqlStrings.RunningQueries, cn);
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("CollectSessionWaits", Source.CollectSessionWaits);
            var ds = new DataSet();
            da.Fill(ds);
            var dtRunningQueries = ds.Tables[0];
            dtRunningQueries.TableName = "RunningQueries";
            ds.Tables.Remove(dtRunningQueries);
            Data.Tables.Add(dtRunningQueries);
            // We might have a second table if we are collecting session waits
            if (ds.Tables.Count != 1) return;
            var dtSessionWaits = ds.Tables[0];
            dtSessionWaits.TableName = "SessionWaits";
            ds.Tables.Remove(dtSessionWaits);
            Data.Tables.Add(dtSessionWaits);
        }

        private void CollectResourceGovernor()
        {
            var ss = new SchemaSnapshotDB(Source.SourceConnection);
            var dtRG = ss.ResourceGovernorConfiguration();
            Data.Tables.Add(dtRG);
        }

        private string PerformanceCountersSQL()
        {
            string xml = PerformanceCounters.PerformanceCountersXML;
            if (xml.Length <= 0) return string.Empty;
            string sql = SqlStrings.PerformanceCounters;
            if (productVersion.StartsWith("8") || productVersion.StartsWith("9"))
            {
                sql = sql.Replace("SYSUTCDATETIME()", "GETUTCDATE()");
            }
            return sql;
        }

        private DataSet GetPerformanceCounters()
        {
            string sql = PerformanceCountersSQL();
            if (sql == string.Empty) return null;
            using var cn = new SqlConnection(ConnectionString);
            using var da = new SqlDataAdapter(sql, cn);
            cn.Open();
            var ds = new DataSet();
            SqlParameter pCountersXML = new("CountersXML", PerformanceCounters.PerformanceCountersXML)
            {
                SqlDbType = SqlDbType.Xml
            };
            da.SelectCommand.CommandTimeout = CollectionType.PerformanceCounters.GetCommandTimeout();
            da.SelectCommand.Parameters.Add(pCountersXML);
            da.Fill(ds);
            return ds;
        }

        private void MergeCustomPerformanceCounters(DataTable dt, DataTable userDT)
        {
            if (dt.Columns.Count == userDT.Columns.Count)
            {
                try
                {
                    for (var i = 0; i < (dt.Columns.Count - 1); i++)
                    {
                        if (dt.Columns[i].ColumnName != userDT.Columns[i].ColumnName)
                        {
                            throw new Exception(
                                $"Invalid schema for custom metrics.  Expected column '{dt.Columns[i].ColumnName}' in position {i + 1} instead of '{userDT.Columns[i].ColumnName}'");
                        }
                        if (dt.Columns[i].DataType != userDT.Columns[i].DataType)
                        {
                            throw new Exception(
                                $"Invalid schema for custom metrics.  Column {dt.Columns[i].ColumnName} expected data type is {dt.Columns[i].DataType.Name} instead of {userDT.Columns[i].DataType.Name}");
                        }
                    }
                    dt.Merge(userDT);
                }
                catch (Exception ex)
                {
                    LogError(ex, "PerformanceCounters");
                }
            }
            else
            {
                throw new Exception($"Invalid schema for custom metrics. Expected {dt.Columns.Count} columns instead of {userDT.Columns.Count}.");
            }
        }

        private void CollectPerformanceCounters()
        {
            var ds = GetPerformanceCounters();
            if (ds == null) return;
            var dt = ds.Tables[0];
            if (ds.Tables.Count == 2)
            {
                var userDT = ds.Tables[1];
                MergeCustomPerformanceCounters(dt, userDT);
            }
            ds.Tables.Remove(dt);
            dt.TableName = "PerformanceCounters";
            Data.Tables.Add(dt);
        }

        private object GetSlowQueries()
        {
            SqlConnectionStringBuilder builder = new(ConnectionString)
            {
                ApplicationName = "DBADashXE"
            };
            var slowQueriesSQL = IsAzureDB ? SqlStrings.SlowQueriesAzure : SqlStrings.SlowQueries;
            using var cn = new SqlConnection(builder.ConnectionString);
            using var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = CollectionType.SlowQueries.GetCommandTimeout() };
            cn.Open();

            cmd.Parameters.AddWithValue("SlowQueryThreshold", Source.SlowQueryThresholdMs * 1000);
            cmd.Parameters.AddWithValue("MaxMemory", Source.SlowQuerySessionMaxMemoryKB);
            cmd.Parameters.AddWithValue("UseDualSession", Source.UseDualEventSession);
            cmd.Parameters.AddWithValue("MaxTargetMemory", Source.SlowQueryTargetMaxMemoryKB);
            return cmd.ExecuteScalar();
        }

        private void CollectSlowQueries()
        {
            if (!IsXESupported) return;
            var result = GetSlowQueries();
            if (result == DBNull.Value)
            {
                throw new Exception("Result is NULL");
            }
            var ringBuffer = (string)result;
            if (ringBuffer.Length <= 0) return;
            var dt = XETools.XEStrToDT(ringBuffer, out RingBufferTargetAttributes ringBufferAtt);
            dt.TableName = "SlowQueries";
            AddDT(dt);
            var dtAtt = ringBufferAtt.GetTable();
            dtAtt.TableName = "SlowQueriesStats";
            AddDT(dtAtt);
        }

        private void CollectServerExtraProperties()
        {
            if (IsAzureDB)
            {
                throw new Exception("ServerExtraProperties collection not supported on AzureDB");
            }
            if (!noWMI)
            {
                CollectComputerSystemWMI();
                CollectOperatingSystemWMI();
                CollectProcessorWMI();
            }
            var query = SqlStrings.ServerExtraProperties;
            if (SQLVersion.Major <= 9)
            {
                query = query.Replace("DATETIMEOFFSET", "DATETIME");
            }
            AddDT("ServerExtraProperties", query, CollectionType.ServerExtraProperties.GetCommandTimeout());
            Data.Tables["ServerExtraProperties"]!.Columns.Add("WindowsCaption");
            if (manufacturer != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemManufacturer"] = manufacturer; }
            if (model != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemProductName"] = model; }

            if (!string.IsNullOrEmpty(ProcessorName)) { Data.Tables["ServerExtraProperties"].Rows[0]["ProcessorNameString"] = ProcessorName; }
            Data.Tables["ServerExtraProperties"].Rows[0]["WindowsCaption"] = WindowsCaption;
            if (Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlanGUID"] == DBNull.Value && noWMI == false)
            {
                CollectPowerPlanWMI();
                Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlanGUID"] = activePowerPlanGUID;
                Data.Tables["ServerExtraProperties"].Rows[0]["ActivePowerPlan"] = activePowerPlan;
            }
        }

        public DataTable GetDT(string tableName, string SQL, int commandTimeout, SqlParameter[] param = null)
        {
            using var cn = new SqlConnection(ConnectionString);
            using var da = new SqlDataAdapter(SQL, cn);
            cn.Open();
            DataTable dt = new();
            da.SelectCommand.CommandTimeout = commandTimeout;
            if (param != null)
            {
                da.SelectCommand.Parameters.AddRange(param);
            }
            da.Fill(dt);
            dt.TableName = tableName;
            return dt;
        }

        public void AddDT(string tableName, string sql, int commandTimeout, SqlParameter[] param = null)
        {
            if (!Data.Tables.Contains(tableName))
            {
                Data.Tables.Add(GetDT(tableName, sql, commandTimeout, param));
            }
        }

        private void AddDT(DataTable dt)
        {
            if (!Data.Tables.Contains(dt.TableName))
            {
                Data.Tables.Add(dt);
            }
        }

        public void CollectDrivesSQL()
        {
            try
            {
                AddDT("Drives", SqlStrings.Drives, CollectionType.Drives.GetCommandTimeout());
            }
            catch (Exception ex)
            {
                LogError(ex, "Drives");
            }
        }

        public void CollectDrives()
        {
            if (noWMI)
            {
                CollectDrivesSQL();
            }
            else
            {
                try
                {
                    CollectDrivesWMI();
                }
                catch (Exception ex)
                {
                    LogDBError("Drives", "Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete.  Use No WMI option to collect through SQL as default." + Environment.NewLine + ex.Message, "Collect:WMI");
                    Log.Warning(ex, "Error collecting drives via WMI.Drive info will be collected from SQL, but might be incomplete. Use NoWMI to collect through SQL as default.");
                    CollectDrivesSQL();
                }
            }
        }

        private string activePowerPlan;
        private Guid activePowerPlanGUID;
        private string manufacturer;
        private string model;
        private string WindowsCaption;
        private string ProcessorName;

        #region "WMI"

        private void CollectOperatingSystemWMI()
        {
            if (noWMI || !OperatingSystem.IsWindows()) return;
            try
            {
                using CimSession session = CimSession.Create(computerName, WMISessionOptions);
                IEnumerable<CimInstance> results = session.QueryInstances(@"root\cimv2", "WQL", "SELECT Caption FROM Win32_OperatingSystem");

                var item = results.SingleOrDefault();
                if (item == null)
                {
                    return;
                }
                var caption = item.CimInstanceProperties["Caption"].Value?.ToString();
                WindowsCaption = caption?.Truncate(256);
            }
            catch (Exception ex)
            {
                LogError(ex, "ServerExtraProperties", "Collect:Win32_OperatingSystem WMI");
            }
        }

        private CimSessionOptions localSessionOptions;
        public AggregateException WMIException;

        /// <summary>
        /// Cache the session options (DCom or WSMan) - avoid the need to test on the next run
        /// </summary>
        private void SetWMISessionOptions(CimSessionOptions options)
        {
            localSessionOptions = options;
            cache.Set("WMISessionOptions_" + computerName, localSessionOptions, new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddDays(1) });
            Log.Debug("Cache WMI options {options} on {Computer}", localSessionOptions is DComSessionOptions ? "DCom" : "WSMan", computerName);
        }

        /// <summary>
        /// Get the session options (DCom or WSMan).  Avoid the need to test if it's previously cached
        /// </summary>
        private void GetWMISessionOptionsFromCache()
        {
            localSessionOptions = (CimSessionOptions)cache.Get("WMISessionOptions_" + computerName);
            if (localSessionOptions != null)
            {
                Log.Debug("Set WMISessionOptions from cache to {options} on {Computer}", localSessionOptions is DComSessionOptions ? "DCom" : "WSMan", computerName);
            }
        }

        public CimSessionOptions WMISessionOptions
        {
            get
            {
                if (WMIException != null) // Throw previous error if connection attempts previously failed
                {
                    throw WMIException;
                }
                if (localSessionOptions == null) // Get session options from the cache.  Saves the need to re-test.
                {
                    GetWMISessionOptionsFromCache();
                }
                if (localSessionOptions == null)
                {
                    // Options are not in cache.  Try connection with WSMan then try DCom if this fails.
                    try
                    {
                        // Try to connect with WSMan first
                        TestWMI(new WSManSessionOptions());
                        SetWMISessionOptions(new WSManSessionOptions());
                        Log.Debug("WMI Connection succeeded using WSMan on {computer}", computerName);
                    }
                    catch (Exception wsManEx)
                    {
                        // Connection failed, try again with DCom
                        Log.Debug(wsManEx, "WMI Connection failed using WSMan on {computer}.  Connection will be attempted using DCom.", computerName);
                        try
                        {
                            TestWMI(new DComSessionOptions());
                            SetWMISessionOptions(new DComSessionOptions());
                            Log.Debug("WMI Connection succeeded using DCom on {computer}", computerName);
                        }
                        catch (Exception dcomEx)
                        {
                            // WMI failed using WSMan and DCom
                            WMIException = new AggregateException($"Error connecting to WMI on {computerName}", wsManEx, dcomEx);
                            throw WMIException;
                        }
                    }
                }
                return localSessionOptions;
            }
            set => localSessionOptions = value;
        }

        private void TestWMI(CimSessionOptions sessionOptions)
        {
            using CimSession session = CimSession.Create(computerName, sessionOptions);
            // Changed from QueryInstances to GetClass which causes a failure on older OS when using WSMan.
            // This failure is desired as it will cause the session option to change to DCom.  If session option isn't switched to DCom for older OS, drivers collection will fail. #299
            using CimClass win32CS = session.GetClass(@"root\cimv2", "Win32_ComputerSystem");
        }

        private void CollectComputerSystemWMI()
        {
            if (noWMI || !OperatingSystem.IsWindows()) return;
            try
            {
                using CimSession session = CimSession.Create(computerName, WMISessionOptions);
                var result = session.QueryInstances(@"root\cimv2", "WQL", "SELECT Manufacturer, Model FROM Win32_ComputerSystem").SingleOrDefault();

                if (result == null) return;

                manufacturer = Convert.ToString(result.CimInstanceProperties["Manufacturer"].Value).Truncate(200);
                model = Convert.ToString(result.CimInstanceProperties["Model"].Value).Truncate(200);
            }
            catch (Exception ex)
            {
                LogError(ex, "ServerExtraProperties", "Collect:Win32_ComputerSystem WMI");
            }
        }

        private void CollectPowerPlanWMI()
        {
            if (noWMI || !OperatingSystem.IsWindows()) return;
            try
            {
                using CimSession session = CimSession.Create(computerName, WMISessionOptions);
                var results = session.QueryInstances(@"root\cimv2\power", "WQL", "SELECT InstanceID,ElementName FROM Win32_PowerPlan WHERE IsActive=1");
                var item = results.SingleOrDefault();

                if (item == null) return;

                var instanceId = Convert.ToString(item.CimInstanceProperties["InstanceID"].Value);
                activePowerPlan = Convert.ToString(item.CimInstanceProperties["ElementName"].Value);

                if (!string.IsNullOrEmpty(instanceId))
                {
                    activePowerPlanGUID = Guid.Parse(instanceId.AsSpan(instanceId.Length - 38, 38));
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ServerExtraProperties", "Collect:Win32_PowerPlan WMI");
            }
        }

        private void CollectDriversWMI()
        {
            if (noWMI || !OperatingSystem.IsWindows()) return;
            try
            {
                if (Data.Tables.Contains("Drivers")) return;
                DataTable dtDrivers = new("Drivers");
                string[] selectedProperties = new[] { "ClassGuid", "DeviceClass", "DeviceID", "DeviceName", "DriverDate", "DriverProviderName", "DriverVersion", "FriendlyName", "HardWareID", "Manufacturer", "PDO" };
                foreach (string p in selectedProperties)
                {
                    Type columnType = p switch
                    {
                        "DriverDate" => typeof(DateTime),
                        "ClassGuid" => typeof(Guid),
                        _ => typeof(string)
                    };

                    dtDrivers.Columns.Add(p, columnType);
                }

                string query = "SELECT " + string.Join(",", selectedProperties) + " FROM Win32_PnPSignedDriver";

                using CimSession session = CimSession.Create(computerName, WMISessionOptions);
                IEnumerable<CimInstance> results = session.QueryInstances(@"root\cimv2", "WQL", query);

                foreach (CimInstance itm in results)
                {
                    var r = dtDrivers.NewRow();
                    foreach (DataColumn col in dtDrivers.Columns)
                    {
                        object value = itm.CimInstanceProperties[col.ColumnName].Value;
                        if (col.DataType == typeof(DateTime))
                        {
                            r[col.ColumnName] = Convert.ToDateTime(value) == DateTime.MinValue ? DBNull.Value : Convert.ToDateTime(value);
                        }
                        else if (col.DataType == typeof(Guid))
                        {
                            if (Guid.TryParse(Convert.ToString(value), out Guid g))
                            {
                                r[col.ColumnName] = g;
                            }
                        }
                        else
                        {
                            r[col.ColumnName] = Convert.ToString(value).Truncate(200);
                        }
                    }
                    dtDrivers.Rows.Add(r);
                }
                // Get AWS PV Driver from registry
                AddPVDriverVersion(ref dtDrivers);

                Data.Tables.Add(dtDrivers);
            }
            catch (Exception ex)
            {
                LogError(ex, "Drivers", "Collect:WMI");
            }
        }

        private void AddPVDriverVersion(ref DataTable dtDrivers)
        {
            try
            {
                var PVVersion = PVDriverVersion();
                if (string.IsNullOrEmpty(PVVersion)) return;
                var rDriver = dtDrivers.NewRow();
                rDriver["DeviceID"] = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Amazon\\PVDriver";
                rDriver["Manufacturer"] = "Amazon Inc.";
                rDriver["DriverProviderName"] = "Amazon Inc.";
                rDriver["DeviceName"] = "AWS PV Driver";
                rDriver["DriverVersion"] = PVVersion;
                dtDrivers.Rows.Add(rDriver);
            }
            catch (Exception ex)
            {
                LogError(ex, "Drivers", "Collect:AWSPVDriver");
            }
        }

        private const uint LOCAL_MACHINE = 2147483650;

        private void CollectProcessorWMI()
        {
            try
            {
                ProcessorName = GetProcessorName();
            }
            catch (Exception ex)
            {
                LogError(ex, "ServerExtraProperties", "Collect:Processor WMI");
            }
        }

        private string GetProcessorName()
        {
            using CimMethodParametersCollection CimParams = new()
            {
                CimMethodParameter.Create("hDefKey", LOCAL_MACHINE, CimFlags.In),
                CimMethodParameter.Create("sSubKeyName", @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", CimFlags.In),
                CimMethodParameter.Create("sValueName", "ProcessorNameString", CimFlags.In)
            };

            using CimSession session = CimSession.Create(computerName, WMISessionOptions);
            using CimMethodResult results = session.InvokeMethod(new CimInstance("StdRegProv", @"root\default"), "GetStringValue", CimParams);

            return Convert.ToString(results.OutParameters["sValue"].Value);
        }

        private string PVDriverVersion()
        {
            using CimMethodParametersCollection CimParams = new()
            {
                CimMethodParameter.Create("hDefKey", LOCAL_MACHINE, CimFlags.In),
                CimMethodParameter.Create("sSubKeyName", @"SOFTWARE\Amazon\PVDriver", CimFlags.In),
                CimMethodParameter.Create("sValueName", "Version", CimFlags.In)
            };

            using CimSession session = CimSession.Create(computerName, WMISessionOptions);
            using CimMethodResult results = session.InvokeMethod(new CimInstance("StdRegProv", @"root\default"), "GetStringValue", CimParams);

            return Convert.ToString(results.OutParameters["sValue"].Value);
        }

        private void CollectDrivesWMI()
        {
            if (Data.Tables.Contains("Drives") || !OperatingSystem.IsWindows()) return;
            DataTable drives = new("Drives")
            {
                Columns =
                {
                    new DataColumn("Name", typeof(string)),
                    new DataColumn("Capacity", typeof(long)),
                    new DataColumn("FreeSpace", typeof(long)),
                    new DataColumn("Label", typeof(string))
                }
            };

            using CimSession session = CimSession.Create(computerName, WMISessionOptions);
            IEnumerable<CimInstance> results = session.QueryInstances(@"root\cimv2", "WQL", @"SELECT FreeSpace,Name,Capacity,Caption,Label FROM Win32_Volume WHERE DriveType=3 AND NOT Name LIKE '%?%'");

            foreach (CimInstance vol in results)
            {
                var rDrive = drives.NewRow();
                rDrive["FreeSpace"] = Convert.ToInt64(vol.CimInstanceProperties["FreeSpace"].Value);
                rDrive["Name"] = (string)vol.CimInstanceProperties["Name"].Value;
                rDrive["Capacity"] = Convert.ToInt64(vol.CimInstanceProperties["Capacity"].Value);
                rDrive["Label"] = (string)vol.CimInstanceProperties["Label"].Value;
                drives.Rows.Add(rDrive);
            }
            Data.Tables.Add(drives);
        }

        #endregion "WMI"
    }
}