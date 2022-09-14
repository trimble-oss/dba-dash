using Microsoft.SqlServer.Management.Common;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
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
        Instance
    }


    public enum HostPlatform
    {
        Linux,
        Windows
    }


    public class DBCollector
    {
        public DataSet Data;
        string ConnectionString { get => Source.SourceConnection.ConnectionString; }   
        private DataTable dtErrors;
        public bool LogInternalPerformanceCounters=false;
        private DataTable dtInternalPerfCounters;
        public Int32 PerformanceCollectionPeriodMins = 60;
        string computerName;
        readonly CollectionType[] azureCollectionTypes = new CollectionType[] { CollectionType.SlowQueries, CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceStats, CollectionType.CPU, CollectionType.DBFiles, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ObjectExecutionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions, CollectionType.SysConfig, CollectionType.DatabasePrincipals, CollectionType.DatabaseRoleMembers, CollectionType.DatabasePermissions, CollectionType.OSInfo,CollectionType.CustomChecks,CollectionType.PerformanceCounters,CollectionType.VLF, CollectionType.DatabaseQueryStoreOptions, CollectionType.AzureDBResourceGovernance, CollectionType.RunningQueries, CollectionType.IdentityColumns};
        readonly CollectionType[] azureOnlyCollectionTypes = new CollectionType[] { CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceGovernance };
        readonly CollectionType[] azureMasterOnlyCollectionTypes = new CollectionType[] { CollectionType.AzureDBElasticPoolResourceStats };
        public DBADashSource Source;
        private bool noWMI;
        private bool IsAzureDB = false;
        private bool isAzureMasterDB = false;
        private string instanceName;
        string dbName;
        string productVersion;
        public Int32 RetryCount=1;
        public Int32 RetryInterval = 30;
        private HostPlatform platform;
        public DateTime JobLastModified=DateTime.MinValue;
        private bool IsHadrEnabled=false;
        private Policy retryPolicy;
        private DatabaseEngineEdition engineEdition;
        private DBADashAgent dashAgent;
        public bool IsExtendedEventsNotSupportedException=false;

        public const int DefaultIdentityCollectionThreshold = 5;
        /// <summary>
        /// % Used threshold for IdentityColumns collection
        /// </summary>
        public int IdentityCollectionThreshold = DefaultIdentityCollectionThreshold;
        readonly CacheItemPolicy policy = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        };
        readonly MemoryCache cache = MemoryCache.Default;
        private readonly Stopwatch swatch = new();
        private CollectionType currentCollection;

        public int Job_instance_id {
            get
            {
                if (Data.Tables.Contains("JobHistory"))
                {
                    DataTable jh = Data.Tables["JobHistory"];
                    if (jh.Rows.Count > 0)
                    {
                        job_instance_id = Convert.ToInt32(jh.Compute("max(instance_id)", string.Empty));
                    }                   
                }
                return job_instance_id;
            }
            set {
                job_instance_id=value;
            }
        }
        int job_instance_id = 0;

        public DateTime GetJobLastModified()
        {
            using (SqlConnection cn = new(ConnectionString))
            using (SqlCommand cmd = new("SELECT MAX(date_modified) FROM msdb.dbo.sysjobs", cn))
            {
                cn.Open();
                var result = cmd.ExecuteScalar();
                if (result == DBNull.Value)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return (DateTime)result;
                }
            }
        }


        public bool IsXESupported()
        {
            return !IsExtendedEventsNotSupportedException && DBADashConnection.IsXESupported(productVersion);
        }

        public bool IsQueryStoreSupported()
        {
            if (IsAzureDB)
            {
                return true;
            }
            else
            {
                if (productVersion.StartsWith("8.") || productVersion.StartsWith("9.") || productVersion.StartsWith("10.") || productVersion.StartsWith("11.") || productVersion.StartsWith("12."))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public DBCollector(DBADashSource source,string serviceName)
        {
            Source = source;
            Startup(serviceName);
        }

        private void LogError(Exception ex,string errorSource, string errorContext = "Collect")
        {           
            Log.Error(ex,"{ErrorContext} {ErrorSource} {Connection}" ,errorContext,errorSource, Source.SourceConnection.ConnectionForPrint);
            LogDBError(errorSource, ex.ToString(), errorContext);
        }

        private void LogDBError(string errorSource, string errorMessage, string errorContext = "Collect")
        {
            var rError = dtErrors.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            rError["ErrorContext"] = errorContext;
            dtErrors.Rows.Add(rError);
        }

        private void LogInternalPerformanceCounter(string objectName, string counterName,string instanceName, decimal counterValue)
        {
            if (LogInternalPerformanceCounters)
            {
                if (dtInternalPerfCounters == null)
                {
                    dtInternalPerfCounters = new DataTable("InternalPerformanceCounters");
                    dtInternalPerfCounters.Columns.Add("SnapshotDate",typeof(DateTime));
                    dtInternalPerfCounters.Columns.Add("object_name");
                    dtInternalPerfCounters.Columns.Add("counter_name");
                    dtInternalPerfCounters.Columns.Add("instance_name");
                    dtInternalPerfCounters.Columns.Add("cntr_value", typeof(decimal));
                    dtInternalPerfCounters.Columns.Add("cntr_type", typeof(int));
                    Data.Tables.Add(dtInternalPerfCounters);
                }
                var row = dtInternalPerfCounters.NewRow();
                row["SnapshotDate"] = DateTime.UtcNow;
                row["object_name"] = objectName;
                row["counter_name"] = counterName;
                row["instance_name"] = instanceName;
                row["cntr_value"] = counterValue;
                row["cntr_type"] = 65792;
                dtInternalPerfCounters.Rows.Add(row);
            }
        }

        private void StartCollection(CollectionType type)
        {
            swatch.Reset();
            swatch.Start();
            currentCollection = type;
        }

  
        private void StopCollection()
        {
            if (swatch.IsRunning)
            {
                swatch.Stop();
                Log.Debug("Collect {0} on {1} completed in {2}ms", currentCollection.ToString(),instanceName, swatch.ElapsedMilliseconds);
                LogInternalPerformanceCounter("DBADash", "Collection Duration (ms)", currentCollection.ToString(), Convert.ToDecimal(swatch.Elapsed.TotalMilliseconds));
            }
        }

        private void Startup(string serviceName)
        {
            noWMI = Source.NoWMI;
            dashAgent = DBADashAgent.GetCurrent(serviceName);
            retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(new[]
                {
                                TimeSpan.FromSeconds(2),
                                TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(10)
                }, (exception, timeSpan, retryCount, context) =>
                {
                    LogError(exception,(string)context.OperationKey, "Collect[Retrying]");
                });

            Data = new DataSet("DBADash");
            dtErrors = new DataTable("Errors");
            dtErrors.Columns.Add("ErrorSource");
            dtErrors.Columns.Add("ErrorMessage");
            dtErrors.Columns.Add("ErrorContext");

            Data.Tables.Add(dtErrors);

            retryPolicy.Execute(
                context => GetInstance(),
                new Context("Instance")
              );
            
        }


        public async Task RemoveEventSessionsAsync()
        {
            if (IsXESupported())
            {
                string removeSQL;
                if (IsAzureDB)
                {
                    removeSQL = SqlStrings.RemoveEventSessionsAzure;
                }
                else
                {
                    removeSQL = SqlStrings.RemoveEventSessions;
                }
                using (var cn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(removeSQL, cn))
                {
                    await cn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task StopEventSessionsAsync()
        {
            if (IsXESupported())
            {
                string removeSQL;
                if (IsAzureDB)
                {
                    removeSQL = SqlStrings.StopEventSessionsAzure;
                }
                else
                {
                    removeSQL = SqlStrings.StopEventSessions;
                }
                using (var cn = new SqlConnection(ConnectionString))  
                using (var cmd = new SqlCommand(removeSQL, cn))
                {
                    await cn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                
            }
        }

        public void GetInstance()
        {
            StartCollection(CollectionType.Instance);
            var dt = GetDT("DBADash", SqlStrings.Instance, CollectionCommandTimeout.DefaultCommandTimeout);
            dt.Columns.Add("AgentVersion", typeof(string));
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("AgentHostName", typeof(string));
            dt.Columns.Add("AgentServiceName", typeof(string));
            dt.Columns.Add("AgentPath", typeof(string));
            dt.Rows[0]["AgentVersion"] = dashAgent.AgentVersion;
            dt.Rows[0]["AgentHostName"] = dashAgent.AgentHostName;
            dt.Rows[0]["AgentServiceName"] = dashAgent.AgentServiceName;
            dt.Rows[0]["AgentPath"] = dashAgent.AgentPath;

            computerName = (string)dt.Rows[0]["ComputerNamePhysicalNetBIOS"];
            dbName = (string)dt.Rows[0]["DBName"];
            instanceName = (string)dt.Rows[0]["Instance"];
            productVersion = (string)dt.Rows[0]["ProductVersion"];
            string hostPlatform = (string)dt.Rows[0]["host_platform"];
            engineEdition = (DatabaseEngineEdition)Convert.ToInt32(dt.Rows[0]["EngineEdition"]);

            if (!Enum.TryParse(hostPlatform, out platform))
            {
                Log.Error("GetInstance: host_platform parse error");
                LogDBError("Instance", "host_platform parse error");
                platform = HostPlatform.Windows;
            }
            if(!noWMI && platform == HostPlatform.Linux) // Disable WMI collection for Linux
            {
                noWMI = true;
                Log.Debug("WMI Disabled for Linux Instance: {0}", instanceName);
            }
            if (!noWMI && instanceName.StartsWith("EC2AMAZ-")) // Disable WMI collection for RDS
            {
                noWMI = true;
                Log.Debug("WMI Disabled for RDS Instance: {0}", instanceName);
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
            if (String.IsNullOrEmpty(Source.ConnectionID))
            {
                if (IsAzureDB)
                {
                    dt.Rows[0]["ConnectionID"] = instanceName + "|" + dbName;
                    noWMI = true;
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
        private static string EnumToString(Enum en)
        {
            return Enum.GetName(en.GetType(), en);
        }


        private bool CollectionTypeIsApplicable(CollectionType collectionType)
        {
            var collectionTypeString = EnumToString(collectionType);
            if (collectionType == CollectionType.DatabaseQueryStoreOptions && !IsQueryStoreSupported())
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
                return !(productVersion.StartsWith("8.") || productVersion.StartsWith("9."));
            }
            else if ((new CollectionType[] { CollectionType.Backups, CollectionType.DatabaseMirroring, CollectionType.LogRestores, CollectionType.AvailabilityGroups, CollectionType.AvailabilityReplicas, CollectionType.DatabasesHADR }).Contains(collectionType)
                        && engineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                // Don't need to collect these types for Azure MI
                return false;
            }
            else if ((new CollectionType[] { CollectionType.JobHistory, CollectionType.AgentJobs, CollectionType.Jobs }).Contains(collectionType) && engineEdition == DatabaseEngineEdition.Express)
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
            else if (collectionType == CollectionType.ResourceGovernorConfiguration)
            {
                return engineEdition == DatabaseEngineEdition.Enterprise && !IsAzureDB; // Must be enterprise edition and not AzureDB
            }
            else if(collectionType == CollectionType.Instance)
            {
                return false; // Required & collected by default
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
                  context => {
                      StartCollection(collectionType);
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
            if(collectionType == CollectionType.RunningQueries)
            {
                CollectText();
                CollectPlans();
            }
          
        }

        static string ByteArrayToHexString(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }

        private void CollectPlans()
        {
            if (Data.Tables.Contains("RunningQueries") && Source.PlanCollectionEnabled)
            {
                var plansSQL = GetPlansSQL();
                if (!String.IsNullOrEmpty(plansSQL))
                {
                    using (var cn = new SqlConnection(ConnectionString))
                    using (var da = new SqlDataAdapter(plansSQL, cn))
                    {
                        var dt = new DataTable("QueryPlans");                      
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            dt.Columns.Add("query_plan_hash", typeof(byte[]));
                            dt.Columns.Add("query_plan_compressed", typeof(byte[]));
                            foreach (DataRow r in dt.Rows){                         
                                try
                                {
                                    string strPlan = r["query_plan"] == DBNull.Value ? string.Empty : (string)r["query_plan"];
                                    r["query_plan_compressed"] = SchemaSnapshotDB.Zip(strPlan);
                                    var hash = GetPlanHash(strPlan);
                                    r["query_plan_hash"] = hash;
                                }
                                catch(Exception ex)
                                {
                                    Log.Error(ex, "Error processing query plans");
                                }
                            }
                            dt.Columns.Remove("query_plan");

                            Data.Tables.Add(dt);
                        }
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
                        LogInternalPerformanceCounter("DBADash", "Count of plans collected", "", dt.Rows.Count); // Count of plans actually collected - might be less than the list of plans we wanted to collect
                    }
                }
                else
                {
                    LogInternalPerformanceCounter("DBADash", "Count of plans collected", "", 0); // Count of plans actually collected - might be less than the list of plans we wanted to collect
                }
            }
        }


        ///<summary>
        ///Get the query plan hash from a  string of the plan XML
        ///</summary>
        public static byte[] GetPlanHash(string strPlan)
        {
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(strPlan)))
            {
                ms.Position = 0;
                using (var xr = new XmlTextReader(ms))
                {
                    while (xr.Read())
                    {
                        if (xr.Name== "StmtSimple")
                        {
                            string strHash= xr.GetAttribute("QueryPlanHash");
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
            // Filter this list by plans not already colllected and get a distinct list by handle and offsets (excluding the hash as this can cause duplicates in rare cases)
            var collectList =  plans.Where(p => !cache.Contains(p.Key))
                .GroupBy(p => Convert.ToBase64String(p.PlanHandle.Concat(BitConverter.GetBytes(p.StartOffset)).Concat(BitConverter.GetBytes(p.EndOffset)).ToArray()))
                .Select(p => p.First())
                .ToList();

            collectList.ForEach(p =>sb.AppendFormat("{3}(0x{0},{1},{2}),", ByteArrayToHexString(p.PlanHandle), p.StartOffset, p.EndOffset,Environment.NewLine));

            Log.Information("Plans {0}, {1} to collect from {2}", plans.Count, collectList.Count,instanceName);

            LogInternalPerformanceCounter("DBADash", "Count of plans meeting threshold for collection", "", plans.Count); // Total number of plans that meet the threshold for collection
            LogInternalPerformanceCounter("DBADash", "Count of plans to collect", "", collectList.Count); // Total number of plans we want to collect (plans that meet the threshold that are not cached)
            LogInternalPerformanceCounter("DBADash", "Count of plans from cache", "", plans.Count- collectList.Count); // Plan count we didn't collect because they have been collected previously and we cached the handles/hashes.

            if (collectList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
                sb.Append(@"SELECT t.plan_handle,
        t.statement_start_offset,
        t.statement_end_offset,
        pln.dbid,
        pln.objectid,
        pln.encrypted,
        pln.query_plan
FROM @plans t 
CROSS APPLY sys.dm_exec_text_query_plan(t.plan_handle,t.statement_start_offset,t.statement_end_offset) pln");
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
                var plans = (from r in dt.AsEnumerable()
                             where r["plan_handle"] != DBNull.Value && r["query_plan_hash"] != DBNull.Value && r["statement_start_offset"] != DBNull.Value && r["statement_end_offset"] != DBNull.Value
                             group r by new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]) into g
                             where g.Sum(r => Convert.ToInt32(r["cpu_time"])) >= Source.PlanCollectionCPUThreshold || g.Sum(r => Convert.ToInt32(r["granted_query_memory"])) >= Source.PlanCollectionMemoryGrantThreshold || g.Count() >= Source.PlanCollectionCountThreshold || g.Max(r=> ((DateTime)r["SnapshotDateUTC"]).Subtract((DateTime)r["last_request_start_time_utc"])).TotalMilliseconds >= Source.PlanCollectionDurationThreshold
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
            if (Data.Tables.Contains("RunningQueries"))
            {
                var handlesSQL = GetTextFromHandlesSQL();
                if (!String.IsNullOrEmpty(handlesSQL))
                {
                    using (var cn = new SqlConnection(ConnectionString))
                    using (var da = new SqlDataAdapter(handlesSQL, cn))
                    {
                        var dt = new DataTable("QueryText");
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Data.Tables.Add(dt);
                        }
                        LogInternalPerformanceCounter("DBADash", "Count of text collected", "", dt.Rows.Count); // Count of text collected from sql_handles
                        LogInternalPerformanceCounter("DBADash", "Count of running queries", "", Data.Tables["RunningQueries"].Rows.Count); // Total number of running queries
                    }                   
                }
            }
        }

        ///<summary>
        ///Once written to the destination, call this function to cache the plan handles and query plan hash. If the plan is cached it won't be collected in future.<br/>
        ///Note: We are just caching the plan handle and hash with the statement offsets.
        ///</summary>
        public void CacheCollectedPlans()
        {
            if (Data.Tables.Contains("QueryPlans"))
            {
                var dt = Data.Tables["QueryPlans"];
                foreach (DataRow r in dt.Rows)
                {
                    if (r["query_plan_hash"] != DBNull.Value)
                    {
                        var plan = new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]);
                        cache.Add(plan.Key, "", policy);
                    }
                }
            }
        }

        ///<summary>
        ///Once written to the destination, call this function to cache the sql_handles for captured query text. If the handle is cached it won't be collected in future.<br/>
        ///Note: We capture text at the batch level and can use the statement offsets to get the statement text.
        ///</summary>
        public void CacheCollectedText()
        {
            if (Data.Tables.Contains("QueryText"))
            {
                var dt = Data.Tables["QueryText"];
                foreach(DataRow r in dt.Rows)
                {
                    cache.Add(ByteArrayToHexString((byte[])r["sql_handle"]), "",policy);
                }
            }
        }

        ///<summary>
        ///Generate a SQL query to get the query text associated with the plan handles for running queries
        ///</summary>
        private string GetTextFromHandlesSQL()
        {
            var handles = RunningQueriesHandles();
            Int32 cnt = 0;
            Int32 cacheCount = 0;
            var sb = new StringBuilder();
            sb.Append(@"DECLARE @handles TABLE(sql_handle VARBINARY(64))
INSERT INTO @handles(sql_handle)
VALUES
");
            foreach (string strHandle in handles)
            {
                if (!cache.Contains(strHandle))
                {
                    cnt += 1;
                    sb.Append(string.Format("(0x{0}),", strHandle));
                }
                else
                {
                    cacheCount += 1;
                }
            }

            LogInternalPerformanceCounter("DBADash", "Distinct count of text (sql_handle)", "", handles.Count); // Total number of distinct sql_handles
            LogInternalPerformanceCounter("DBADash", "Count of text (sql_handle) to collect", "", cnt); // Count of sql_handles we need to collect
            LogInternalPerformanceCounter("DBADash", "Count of text (sql_handle) from cache", "", handles.Count - cnt); // Count of sql_handles we didn't need to collect becasue they were collected previously and we cached the sql_handle.

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
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
                sb.Append(@"SELECT H.sql_handle,
    txt.dbid,
    txt.objectid as object_id,
    txt.encrypted,
    txt.text
FROM @handles H 
CROSS APPLY sys.dm_exec_sql_text(H.sql_handle) txt");
                return sb.ToString();
            }
        }

        ///<summary>
        ///Get a distinct list of sql_handle for running queries.  The handles are later used to capture query text
        ///</summary>
        private List<string> RunningQueriesHandles()
        {
            var handles = (from r in Data.Tables["RunningQueries"].AsEnumerable()
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
                param = new SqlParameter[] { new SqlParameter { DbType = DbType.Int32, Value = Job_instance_id, ParameterName = "instance_id" }, new SqlParameter { DbType = DbType.Int32, ParameterName = "run_date", Value = Convert.ToInt32(DateTime.Now.AddDays(-7).ToString("yyyyMMdd")) } };
            }
            else if (collectionType == CollectionType.AzureDBResourceStats || collectionType == CollectionType.AzureDBElasticPoolResourceStats)
            {
                param = new SqlParameter[] { new SqlParameter("Date", DateTime.UtcNow.AddMinutes(-PerformanceCollectionPeriodMins)) };
            }
            else if (collectionType == CollectionType.CPU)
            {
                param = new SqlParameter[] { new SqlParameter("TOP", PerformanceCollectionPeriodMins) };
            }
            else if (collectionType== CollectionType.IdentityColumns)
            {
                param = new SqlParameter[] { new SqlParameter("IdentityCollectionThreshold", IdentityCollectionThreshold) };
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
                catch(SqlException ex) when (ex.Message.StartsWith("Unable to create/alter extended events session: RDS for SQL Server supports extended events")){
                    // RDS instances only support extended events for Standard and Enterprise editions.  If we encounter an error because it's not supported, log the error and continue
                    // Set IsExtendedEventsNotSupportedException which is used to disable the collection for future schedules
                    Log.Warning("Slow query capture relies on extended events which is not supported for {0}: {1}",instanceName, ex.Message);
                    LogDBError(collectionTypeString, "Slow query capture relies on extended events which is not supported for this instance","Collect[Warning]");
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
                    var ss = new SchemaSnapshotDB(ConnectionString, new SchemaSnapshotDBOptions());
                    try
                    {
                        ss.SnapshotJobs(ref Data);
                        JobLastModified = currentJobModified;
                    }
                    catch (Microsoft.SqlServer.Management.Smo.UnsupportedFeatureException ex)
                    {
                        Log.Warning("SnapshotJobs not supported: {0}. From {1}", ex.Message, Source.SourceConnection.ConnectionForPrint);
                    }
                }
                else
                {
                    Log.Information("Skipping jobs collection for {0}.  Not Modified.", instanceName);
                }
            }
            else if (collectionType == CollectionType.ResourceGovernorConfiguration)
            {   
                CollectResourceGovernor();               
            }
            else if(collectionType == CollectionType.RunningQueries)
            {
                CollectRunningQueries();
            }
            else
            {
                AddDT(collectionTypeString, SqlStrings.GetSqlString(collectionType),collectionType.GetCommandTimeout(),  param);
            }
        }

        private void CollectRunningQueries()
        {
            using(var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(SqlStrings.RunningQueries, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("CollectSessionWaits", Source.CollectSessionWaits);
                var ds = new DataSet();
                da.Fill(ds);
                var dtRunningQueries = ds.Tables[0];
                dtRunningQueries.TableName = "RunningQueries";
                ds.Tables.Remove(dtRunningQueries);
                Data.Tables.Add(dtRunningQueries);
                // We might have a second table if we are collecting session waits
                if (ds.Tables.Count == 1)
                {
                    var dtSessionWaits = ds.Tables[0];
                    dtSessionWaits.TableName = "SessionWaits";
                    ds.Tables.Remove(dtSessionWaits);
                    Data.Tables.Add(dtSessionWaits);
                }

            }
        }

        private void CollectResourceGovernor()
        {
            var ss = new SchemaSnapshotDB(ConnectionString);
            var dtRG = ss.ResourceGovernorConfiguration();
            Data.Tables.Add(dtRG);
        }

        private void CollectPerformanceCounters()
        {
  
            string xml = PerformanceCounters.PerformanceCountersXML;
            if (xml.Length > 0)
            {
       
                string sql = SqlStrings.PerformanceCounters;
                if(productVersion.StartsWith("8") || productVersion.StartsWith("9"))
                {
                    sql = sql.Replace("SYSUTCDATETIME()", "GETUTCDATE()");
                }
                using (var cn = new SqlConnection(ConnectionString))
                {
                    using (var da = new SqlDataAdapter(sql, cn))
                    {
                        cn.Open();
                        var ds = new DataSet();
                        SqlParameter pCountersXML = new("CountersXML", PerformanceCounters.PerformanceCountersXML)
                        {
                            SqlDbType = SqlDbType.Xml
                        };
                        da.SelectCommand.CommandTimeout = CollectionType.PerformanceCounters.GetCommandTimeout();
                        da.SelectCommand.Parameters.Add(pCountersXML);
                        da.Fill(ds);


                        var dt = ds.Tables[0];
                        if (ds.Tables.Count == 2)
                        {
                            var userDT = ds.Tables[1];
                            if (dt.Columns.Count == userDT.Columns.Count)
                            {
                                try
                                {
                                    for (Int32 i = 0; i < (dt.Columns.Count - 1); i++)
                                    {
                                        if (dt.Columns[i].ColumnName != userDT.Columns[i].ColumnName)
                                        {
                                            throw new Exception(String.Format("Invalid schema for custom metrics.  Expected column '{0}' in position {1} instead of '{2}'", dt.Columns[i].ColumnName, i + 1, userDT.Columns[i].ColumnName));
                                        }
                                        if (dt.Columns[i].DataType != userDT.Columns[i].DataType)
                                        {
                                            throw new Exception(String.Format("Invalid schema for custom metrics.  Column {0} expected data type is {1} instead of {2}", dt.Columns[i].ColumnName, dt.Columns[i].DataType.Name, userDT.Columns[i].DataType.Name));
                                        }
                                    }
                                    dt.Merge(userDT);
                                }
                                catch (Exception ex)
                                {
                                    LogError(ex,"PerformanceCounters");
                                }
                            }
                            else
                            {
                                throw new Exception($"Invalid schema for custom metrics. Expected {dt.Columns.Count} columns instead of {userDT.Columns.Count}.");
                            }
                        }
                        ds.Tables.Remove(dt);
                        dt.TableName = "PerformanceCounters";
                        Data.Tables.Add(dt);
                    }
                }
            }
            
       
        }


        private void CollectSlowQueries()
        {

            if (IsXESupported())
            {
                SqlConnectionStringBuilder builder = new(ConnectionString)
                {
                    ApplicationName = "DBADashXE"
                };
                string slowQueriesSQL;
                if (IsAzureDB)
                {
                    slowQueriesSQL = SqlStrings.SlowQueriesAzure;
                }
                else
                {
                    slowQueriesSQL = SqlStrings.SlowQueries;
                }
                using (var cn = new SqlConnection(builder.ConnectionString))
                {
                    using (var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = CollectionType.SlowQueries.GetCommandTimeout() })
                    {
                        cn.Open();

                        cmd.Parameters.AddWithValue("SlowQueryThreshold", Source.SlowQueryThresholdMs * 1000);
                        cmd.Parameters.AddWithValue("MaxMemory",Source.SlowQuerySessionMaxMemoryKB);
                        cmd.Parameters.AddWithValue("UseDualSession", Source.UseDualEventSession);
                        cmd.Parameters.AddWithValue("MaxTargetMemory", Source.SlowQueryTargetMaxMemoryKB);
                        var result = cmd.ExecuteScalar();
                        if (result == DBNull.Value)
                        {
                            throw new Exception("Result is NULL");
                        }
                        string ringBuffer = (string)result;
                        if (ringBuffer.Length > 0)
                        {
                            var dt = XETools.XEStrToDT(ringBuffer, out RingBufferTargetAttributes ringBufferAtt);
                            dt.TableName = "SlowQueries";
                            AddDT(dt);
                            var dtAtt = ringBufferAtt.GetTable();
                            dtAtt.TableName = "SlowQueriesStats";
                            AddDT(dtAtt);

                        }
                    }
                }
            }
        }


        private void CollectServerExtraProperties()
        {
            if (this.IsAzureDB)
            {
                throw new Exception("ServerExtraProperties collection not supported on AzureDB");
            }
            if (!noWMI)
            {
                CollectComputerSystemWMI();
                CollectOperatingSystemWMI();
            }
            AddDT("ServerExtraProperties", SqlStrings.ServerExtraProperties,CollectionType.ServerExtraProperties.GetCommandTimeout());
            Data.Tables["ServerExtraProperties"].Columns.Add("WindowsCaption");
            if (manufacturer != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemManufacturer"] = manufacturer; }
            if (model != "") { Data.Tables["ServerExtraProperties"].Rows[0]["SystemProductName"] = model; }
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
            using (var cn = new SqlConnection(ConnectionString))
            using (var da = new SqlDataAdapter(SQL, cn))
            {
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
        }

        public void AddDT(string tableName, string sql,int commandTimeout, SqlParameter[] param = null)
        {
            if (!Data.Tables.Contains(tableName))
            {
                Data.Tables.Add(GetDT(tableName, sql,commandTimeout, param));
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
                LogError(ex,"Drives");
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
                    LogDBError("Drives", "Error collecting drives via WMI.  Drive info will be collected from SQL, but might be incomplete.  Use --nowmi switch to collect through SQL as default." + Environment.NewLine + ex.Message, "Collect:WMI");
                    Log.Warning(ex, "Error collecting drives via WMI.Drive info will be collected from SQL, but might be incomplete.Use--nowmi switch to collect through SQL as default.");
                    CollectDrivesSQL();
                }
            }
        }

        string activePowerPlan;
        Guid activePowerPlanGUID;
        string manufacturer;
        string model;
        string WindowsCaption;

        #region "WMI"

        private void CollectOperatingSystemWMI()
        {
            if (!noWMI && OperatingSystem.IsWindows())
            {
                try
                {
                    ManagementPath path = new()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scopeCIMV2 = new(path);

                    SelectQuery query = new("Win32_OperatingSystem", "", new string[] { "Caption" });
                    using (ManagementObjectSearcher searcher = new(scopeCIMV2, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        if (results.Count == 1)
                        {
                            var mo = results.OfType<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                WindowsCaption = GetMOStringValue(mo, "Caption", 256);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex,"ServerExtraProperties","Collect:Win32_OperatingSystem WMI");
                }
            }
        }

        private void CollectComputerSystemWMI()
        {
            if (!noWMI && OperatingSystem.IsWindows())
            {
                try
                {
                    ManagementPath path = new()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scopeCIMV2 = new(path);

                    SelectQuery query = new("Win32_ComputerSystem", "", new string[] { "Manufacturer", "Model" });
                    using (ManagementObjectSearcher searcher = new(scopeCIMV2, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        if (results.Count == 1)
                        {
                            var mo = results.OfType<ManagementObject>().FirstOrDefault();
                            if (mo != null)
                            {
                                manufacturer = GetMOStringValue(mo, "Manufacturer", 200);
                                model = GetMOStringValue(mo, "Model", 200);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex,"ServerExtraProperties", "Collect:Win32_ComputerSystem WMI");
                }
            }
        }

        private static string GetMOStringValue(ManagementObject mo, string propertyName, Int32 truncateLength = 0)
        {
            string value = "";
            if (OperatingSystem.IsWindows())
            {
                if (mo.GetPropertyValue(propertyName) != null)
                {
                    value = mo.GetPropertyValue(propertyName).ToString();
                    if (truncateLength > 0 && value.Length > truncateLength)
                    {
                        value = value[..200];
                    }
                }
            }
            return value;
        }

        private void CollectPowerPlanWMI()
        {
            if (!noWMI && OperatingSystem.IsWindows())
            {
                try
                {
                    ManagementPath pathPower = new()
                    {
                        NamespacePath = @"root\cimv2\power",
                        Server = computerName
                    };
                    ManagementScope scopePower = new(pathPower);
                    SelectQuery query = new("Win32_PowerPlan", "IsActive=1", new string[] { "InstanceID", "ElementName" });
                    using (ManagementObjectSearcher searcher = new(scopePower, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {

                        var mo = results.OfType<ManagementObject>().FirstOrDefault();
                        if (mo != null)
                        {


                            string instanceId = GetMOStringValue(mo, "InstanceID");
                            if (instanceId.Length > 0)
                            {
                                activePowerPlanGUID = Guid.Parse(instanceId.AsSpan(instanceId.Length - 38, 38));
                            }
                            activePowerPlan = GetMOStringValue(mo, "ElementName");
                        }

                    }
                }
                catch (Exception ex)
                {
                    LogError(ex,"ServerExtraProperties", "Collect:Win32_PowerPlan WMI");
                }
            }
        }

        private void CollectDriversWMI()
        {
            if (!noWMI && OperatingSystem.IsWindows())
            {
                try
                {
                    if (!Data.Tables.Contains("Drivers"))
                    {
                        DataTable dtDrivers = new("Drivers");
                        string[] selectedProperties = new string[] { "ClassGuid", "DeviceClass", "DeviceID", "DeviceName", "DriverDate", "DriverProviderName", "DriverVersion", "FriendlyName", "HardWareID", "Manufacturer", "PDO" };
                        foreach (string p in selectedProperties)
                        {
                            if (p == "DriverDate")
                            {
                                dtDrivers.Columns.Add(p, typeof(DateTime));
                            }
                            else if (p == "ClassGuid")
                            {
                                dtDrivers.Columns.Add(p, typeof(Guid));
                            }
                            else
                            {
                                dtDrivers.Columns.Add(p, typeof(string));
                            }
                        }

                        ManagementPath path = new()
                        {
                            NamespacePath = @"root\cimv2",
                            Server = computerName
                        };
                        ManagementScope scope = new(path);

                        SelectQuery query = new("Win32_PnPSignedDriver", "", selectedProperties);

                        using (ManagementObjectSearcher searcher = new(scope, query))
                        using (ManagementObjectCollection results = searcher.Get())
                        {
                            foreach (ManagementObject mo in results.Cast<ManagementObject>())
                            {
                                if (mo != null)
                                {
                                    var rDriver = dtDrivers.NewRow();
                                    foreach (string p in selectedProperties)
                                    {
                                        if (mo.GetPropertyValue(p) != null)
                                        {
                                            if (p == "DriverDate" || p == "InstallDate")
                                            {

                                                try
                                                {
                                                    rDriver[p] = ManagementDateTimeConverter.ToDateTime(mo.GetPropertyValue(p).ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogError(ex,"Drivers");
                                                }
                                            }

                                            else if (p == "ClassGuid")
                                            {
                                                try
                                                {
                                                    rDriver[p] = Guid.Parse(mo.GetPropertyValue(p).ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogError(ex,"Drivers");
                                                }

                                            }
                                            else
                                            {
                                                try
                                                {
                                                    string value = mo.GetPropertyValue(p).ToString();

                                                    rDriver[p] = value.Length <= 200 ? value : value[..200];

                                                }
                                                catch (Exception ex)
                                                {
                                                    LogError(ex,"Drivers");
                                                }

                                            }

                                        }
                                    }
                                    dtDrivers.Rows.Add(rDriver);
                                }
                            }
                        }
                        try
                        {
                            var PVKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, computerName, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Amazon\\PVDriver");
                            if (PVKey != null)
                            {
                                var rDriver = dtDrivers.NewRow();
                                rDriver["DeviceID"] = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Amazon\\PVDriver";
                                rDriver["Manufacturer"] = "Amazon Inc.";
                                rDriver["DriverProviderName"] = "Amazon Inc.";
                                rDriver["DeviceName"] = "AWS PV Driver";
                                rDriver["DriverVersion"] = PVKey.GetValue("Version");
                                dtDrivers.Rows.Add(rDriver);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError(ex,"Drivers", "Collect:AWSPVDriver");
                        }
                        Data.Tables.Add(dtDrivers);
                    }

                }
                catch (Exception ex)
                {
                    LogError(ex,"Drivers","Collect:WMI");
                }
            }
        }

        private void CollectDrivesWMI()
        {
            try
            {
                if (!Data.Tables.Contains("Drives") && OperatingSystem.IsWindows())
                {
                    DataTable drives = new("Drives");
                    drives.Columns.Add("Name", typeof(string));
                    drives.Columns.Add("Capacity", typeof(Int64));
                    drives.Columns.Add("FreeSpace", typeof(Int64));
                    drives.Columns.Add("Label", typeof(string));

                    ManagementPath path = new()
                    {
                        NamespacePath = @"root\cimv2",
                        Server = computerName
                    };
                    ManagementScope scope = new(path);
                    //string condition = "DriveLetter = 'C:'";
                    string[] selectedProperties = new string[] { "FreeSpace", "Name", "Capacity", "Caption", "Label" };
                    // Using @ to avoid doubling up the backslashes for C#.  The doubling up is for WQL which also uses backslashes as an escape character.
                    // Drive Type 3 = Local Disk.  Not like \\?\ excludes system voume and recovery partition
                    string condition = @"DriveType=3 AND NOT Name LIKE '\\\\?\\%'"; 
                    SelectQuery query = new("Win32_Volume", condition, selectedProperties);

                    using (ManagementObjectSearcher searcher = new(scope, query))
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject volume in results.Cast<ManagementObject>())
                        {


                            if (volume != null)
                            {
                                var rDrive = drives.NewRow();
                                rDrive["FreeSpace"] = (UInt64)volume.GetPropertyValue("FreeSpace");
                                rDrive["Name"] = (string)volume.GetPropertyValue("Name");
                                rDrive["Capacity"] = (UInt64)volume.GetPropertyValue("Capacity");
                                rDrive["Label"] = (string)volume.GetPropertyValue("Label");
                                drives.Rows.Add(rDrive);
                                // Use freeSpace here...
                            }
                        }
                    }

                    Data.Tables.Add(drives);

                }
            }
            catch (Exception ex)
            {
                LogError(ex,"Drives", "Collect:WMI");
            }
        }

        #endregion

    }
}
