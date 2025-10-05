using DBADash.InstanceMetadata;
using Microsoft.Data.SqlClient;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.SqlServer.Management.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        TableSize,
        ServerServices,
        AvailableProcs,
        InstanceMetadata
    }

    public enum HostPlatform
    {
        Linux,
        Windows
    }

    public class DBCollector : IErrorLogger
    {
        public DataSet Data;
        private string ConnectionString => Source.SourceConnection.ConnectionString;
        private DataTable dtErrors;
        public bool LogInternalPerformanceCounters = false;
        private DataTable dtInternalPerfCounters;
        public int PerformanceCollectionPeriodMins = 60;
        private string computerName;
        private readonly CollectionType[] azureCollectionTypes = new[] { CollectionType.SlowQueries, CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceStats, CollectionType.CPU, CollectionType.DBFiles, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ObjectExecutionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions, CollectionType.SysConfig, CollectionType.DatabasePrincipals, CollectionType.DatabaseRoleMembers, CollectionType.DatabasePermissions, CollectionType.OSInfo, CollectionType.CustomChecks, CollectionType.PerformanceCounters, CollectionType.VLF, CollectionType.DatabaseQueryStoreOptions, CollectionType.AzureDBResourceGovernance, CollectionType.RunningQueries, CollectionType.IdentityColumns, CollectionType.TableSize, CollectionType.AvailableProcs };
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
        private static readonly ResiliencePipeline EnabledPipeline = CreatePipeline();
        private static readonly ResiliencePipeline DisabledRetryPipeline = new ResiliencePipelineBuilder().Build();
        private ResiliencePipeline pipeline => DisableRetry ? DisabledRetryPipeline : EnabledPipeline;
        private DatabaseEngineEdition engineEdition;
        public bool IsExtendedEventsNotSupportedException;
        private readonly bool DisableRetry;
        public List<Exception> Exceptions = new();

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

        private static ResiliencePipeline CreatePipeline()
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(Utility.ShouldRetry),
                    DelayGenerator = static args =>
                    {
                        var delay = args.AttemptNumber switch
                        {
                            0 => TimeSpan.FromSeconds(2),
                            1 => TimeSpan.FromSeconds(10),
                            _ => TimeSpan.FromSeconds(30)
                        };
                        return new ValueTask<TimeSpan?>(delay);
                    },
                    MaxRetryAttempts = 2,
                })
                .Build();
        }

        public async Task<DateTime> GetJobLastModifiedAsync()
        {
            await using SqlConnection cn = new(ConnectionString);
            await using SqlCommand cmd = new(SqlStrings.MaxJobLastModified, cn);
            await cn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? DateTime.MinValue : (DateTime)result;
        }

        public bool IsXESupported => !IsExtendedEventsNotSupportedException && DBADashConnection.IsXESupported(productVersion);

        public bool IsQueryStoreSupported => IsAzureDB || (!productVersion.StartsWith("8.") && !productVersion.StartsWith("9.") && !productVersion.StartsWith("10.") && !productVersion.StartsWith("11.") && !productVersion.StartsWith("12."));

        private DBCollector(DBADashSource source, string serviceName, bool disableRetry, bool logInternalPerformanceCounters)
        {
            DisableRetry = disableRetry;
            LogInternalPerformanceCounters = logInternalPerformanceCounters;
            Source = source;
        }

        public static async Task<DBCollector> CreateAsync(DBADashSource source, string serviceName, bool disableRetry = false, bool logInternalPerformanceCounters = false)
        {
            var collector = new DBCollector(source, serviceName, disableRetry, logInternalPerformanceCounters);
            await collector.StartupAsync();
            return collector;
        }

        public void LogError(Exception ex, string errorSource, string errorContext = "Collect")
        {
            Log.Error(ex, "{ErrorContext} {ErrorSource} {Connection}", errorContext, errorSource, Source.SourceConnection.ConnectionForPrint);
            Exceptions.Add(ex);
            LogDBError(errorSource, ex.ToString(), errorContext);
        }

        public void ClearErrors()
        {
            dtErrors.Rows.Clear();
        }

        public int ErrorCount => dtErrors.Rows.Count;

        private void LogDBError(string errorSource, string errorMessage, string errorContext = "Collect")
        {
            AddErrorRow(ref dtErrors, errorSource, errorMessage, errorContext);
        }

        public static void AddErrorRow(ref DataTable dt, string errorSource, string errorMessage, string errorContext)
        {
            var rError = dt.NewRow();
            rError["ErrorSource"] = errorSource;
            rError["ErrorMessage"] = errorMessage;
            rError["ErrorContext"] = errorContext;
            dt.Rows.Add(rError);
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

        private async Task StartupAsync()
        {
            noWMI = Source.NoWMI;
            Data = new DataSet("DBADash");
            dtErrors = GetErrorDataTableSchema();

            Data.Tables.Add(dtErrors);

            await TryCollectAsync(async _ => await GetInstanceAsync(), "Instance");
        }

        /// <summary>
        /// Runs collection action and retries on failure.  Errors are logged and collected in an AggregateException which is thrown if the action fails after all retries.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        private async Task TryCollectAsync(Func<CancellationToken, ValueTask> action, string collection)
        {
            var errors = new List<Exception>();
            try
            {
                await pipeline.ExecuteAsync(async _ =>
                {
                    try
                    {
                        await action(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        if (Utility.ShouldRetry(ex) && !DisableRetry)
                        {
                            Log.Warning(ex, "Error collecting {collection}", collection);
                        }

                        errors.Add(ex);
                        throw;
                    }
                });
                if (errors.Count > 0)
                {
                    LogError(new AggregateException($"{collection} succeeded after {errors.Count} attempts", errors),
                        collection, "Collect[Retry]");
                }
            }
            catch (DatabaseConnectionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!errors.Contains(ex))
                {
                    errors.Add(ex);
                }
                if (errors.Count == 1)
                {
                    throw;
                }
                throw new AggregateException($"{collection} collection failed after {errors.Count} attempts", errors);
            }
        }

        public static DataTable GetErrorDataTableSchema()
        {
            return new DataTable("Errors")
            {
                Columns =
                {
                    new DataColumn("ErrorSource"),
                    new DataColumn("ErrorMessage"),
                    new DataColumn("ErrorContext")
                }
            };
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
        /// Add Metadata relating to the DBA Dash service used for collection.
        /// </summary>
        public static void AddDBADashServiceMetadata(ref DataTable dt)
        {
            var dashAgent = DBADashAgent.GetCurrent();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("AgentVersion", typeof(string)),
                new DataColumn("ConnectionID", typeof(string)),
                new DataColumn("AgentHostName", typeof(string)),
                new DataColumn("AgentServiceName", typeof(string)),
                new DataColumn("AgentPath", typeof(string)),
                new DataColumn("ServiceSQSQueueUrl",typeof(string)),
                new DataColumn("S3Path",typeof(string)),
                new DataColumn("MessagingEnabled", typeof(bool)),
                new DataColumn("AllowedScripts", typeof(string)),
                new DataColumn("AllowedCustomProcs",typeof(string))
            });
            if (dt.Rows.Count == 0)
            {
                dt.Rows.Add(dt.NewRow());
            }
            dt.Rows[0]["AgentVersion"] = dashAgent.AgentVersion;
            dt.Rows[0]["AgentHostName"] = dashAgent.AgentHostName;
            dt.Rows[0]["AgentServiceName"] = dashAgent.AgentServiceName;
            dt.Rows[0]["AgentPath"] = dashAgent.AgentPath;
            dt.Rows[0]["ServiceSQSQueueUrl"] = dashAgent.ServiceSQSQueueUrl;
            dt.Rows[0]["MessagingEnabled"] = dashAgent.MessagingEnabled;
            dt.Rows[0]["AllowedScripts"] = dashAgent.AllowedScripts == null || dashAgent.AllowedScripts.Count == 0
                ? DBNull.Value
                : string.Join(',', dashAgent.AllowedScripts);
            dt.Rows[0]["AllowedCustomProcs"] = dashAgent.AllowedCustomProcs == null || dashAgent.AllowedCustomProcs.Count == 0
                ? DBNull.Value
                : string.Join(',', dashAgent.AllowedCustomProcs);
        }

        public async Task GetInstanceAsync()
        {
            StartCollection(CollectionType.Instance.ToString());
            var dt = await GetDataTableAsync("DBADash", SqlStrings.Instance, CollectionCommandTimeout.GetDefaultCommandTimeout());
            AddDBADashServiceMetadata(ref dt);
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

        public async Task CollectAsync(CollectionType[] collectionTypes)
        {
            foreach (CollectionType type in collectionTypes)
            {
                await CollectAsync(type);
            }
        }

        public async Task CollectAsync(Dictionary<string, CustomCollection> customCollections)
        {
            foreach (var customCollection in customCollections)
            {
                var collectionReference = "UserData." + customCollection.Key;
                try
                {
                    await TryCollectAsync(async _ =>
                    {
                        StartCollection(collectionReference);
                        await CollectAsync(customCollection);
                        StopCollection();
                    }, collectionReference);
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

        public async Task CollectAsync(KeyValuePair<string, CustomCollection> customCollection)
        {
            await using var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(customCollection.Value.ProcedureName, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = customCollection.Value.CommandTimeout ?? CollectionCommandTimeout.GetDefaultCommandTimeout() };
            using var da = new SqlDataAdapter(cmd);
            await cn.OpenAsync();
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
            else if (collectionType == CollectionType.ServerServices && SQLVersion.Major < 11)
            {
                return false;
            }
            else if (collectionType == CollectionType.InstanceMetadata && (IsAzureDB || IsRDS || noWMI))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task CollectAsync(CollectionType collectionType)
        {
            var collectionTypeString = EnumToString(collectionType);

            if (!CollectionTypeIsApplicable(collectionType))
            {
                return;
            }

            try
            {
                await TryCollectAsync(async _ =>
                    {
                        StartCollection(collectionType.ToString());
                        await ExecuteCollectionAsync(collectionType);
                        StopCollection();
                    },
                    collectionTypeString
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
                    await CollectPlansAsync();
                }
                catch (Exception ex)
                {
                    LogError(new Exception("Error collecting plans for Running Queries", ex), "RunningQueries");
                }
            }
        }

        private async Task CollectPlansAsync()
        {
            if (!Data.Tables.Contains("RunningQueries") || !Source.PlanCollectionEnabled) return;
            var plans = GetPlansToCollect();
            DataTable dt = null;
            if (plans.Count > 0)
            {
                dt = await Plan.GetPlansAsync(plans, ConnectionString);
                if (dt is { Rows.Count: > 0 })
                {
                    Data.Tables.Add(dt);
                }
            }
            LogInternalPerformanceCounter("DBADash", "Count of plans collected", "", dt?.Rows.Count ?? 0); // Count of plans actually collected - might be less than the list of plans we wanted to collect
        }

        ///<summary>
        ///Get a list of plan handles from RunningQueries including statement start/end offsets as we want to capture plans at the statement level. Query plan hash is used to detect changes in the plan for caching purposes <br/>
        ///Capture a distinct list so we collect the plan for each statement once even if there are multiple instances of statements running with the same plan.<br/>
        ///Filter for plans matching the specified threshold to limit the plans captured to the ones that are likely to be of interest<br/>
        ///</summary>
        private List<Plan> GetPlansToCollect()
        {
            if (Data.Tables.Contains("RunningQueries"))
            {
                var dt = Data.Tables["RunningQueries"];
                var plans = (from r in dt!.AsEnumerable()
                             where r["plan_handle"] != DBNull.Value
                             && r["query_plan_hash"] != DBNull.Value
                             && r["statement_start_offset"] != DBNull.Value
                             && r["statement_end_offset"] != DBNull.Value
                             && ((byte[])r["query_plan_hash"]).Any(b => b != 0) // Not 0x00000000
                             group r by new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]) into g
                             where g.Sum(r => Convert.ToInt64(r["cpu_time"])) >= Source.PlanCollectionCPUThreshold || g.Sum(r => Convert.ToInt64(r["granted_query_memory"])) >= Source.PlanCollectionMemoryGrantThreshold || g.Count() >= Source.PlanCollectionCountThreshold || g.Max(r => ((DateTime)r["SnapshotDateUTC"]).Subtract((DateTime)r["last_request_start_time_utc"])).TotalMilliseconds >= Source.PlanCollectionDurationThreshold
                             select g.Key).Distinct().ToList();

                // Already have a distinct list by plan handle, hash and offsets.
                // Filter this list by plans not already collected and get a distinct list by handle and offsets (excluding the hash as this can cause duplicates in rare cases)
                var collectList = plans.Where(p => !cache.Contains(p.Key))
                    .GroupBy(p => Convert.ToBase64String(p.PlanHandle.Concat(BitConverter.GetBytes(p.StartOffset)).Concat(BitConverter.GetBytes(p.EndOffset)).ToArray()))
                    .Select(p => p.First())
                    .ToList();

                Log.Information("Plans {0}, {1} to collect from {2}", plans.Count, collectList.Count, instanceName);

                LogInternalPerformanceCounter("DBADash", "Count of plans meeting threshold for collection", "", plans.Count); // Total number of plans that meet the threshold for collection
                LogInternalPerformanceCounter("DBADash", "Count of plans to collect", "", collectList.Count); // Total number of plans we want to collect (plans that meet the threshold that are not cached)
                LogInternalPerformanceCounter("DBADash", "Count of plans from cache", "", plans.Count - collectList.Count); // Plan count we didn't collect because they have been collected previously and we cached the handles/hashes.

                return collectList;
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
                cache.Add(((byte[])r["sql_handle"]).ToHexString(), "", policy);
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
                           select ((byte[])r["sql_handle"]).ToHexString()).Distinct().ToList();
            return handles;
        }

        private async Task ExecuteCollectionAsync(CollectionType collectionType)
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
                await CollectDrivesAsync();
            }
            else if (collectionType == CollectionType.ServerExtraProperties)
            {
                await CollectServerExtraPropertiesAsync();
            }
            else if (collectionType == CollectionType.DriversWMI)
            {
                CollectDriversWMI();
            }
            else if (collectionType == CollectionType.SlowQueries)
            {
                try
                {
                    await CollectSlowQueriesAsync();
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
                await CollectPerformanceCountersAsync();
            }
            else if (collectionType == CollectionType.Jobs)
            {
                var currentJobModified = await GetJobLastModifiedAsync();
                if (currentJobModified > JobLastModified)
                {
                    var ss = new AgentJobs(Source.SourceConnection, new SchemaSnapshotDBOptions());
                    try
                    {
                        //ss.SnapshotJobs(ref Data);
                        await ss.CollectJobsAsync(Data, Source.ScriptAgentJobs, IsRDS);
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
                await CollectRunningQueriesAsync();
            }
            else if (collectionType == CollectionType.RunningJobs)
            {
                await CollectRunningJobsAsync();
            }
            else if (collectionType == CollectionType.InstanceMetadata)
            {
                await CollectInstanceMetadataAsync();
            }
            else
            {
                await AddDataTableAsync(collectionTypeString, SqlStrings.GetSqlString(collectionType), collectionType.GetCommandTimeout(), param);
            }
        }

        private static DataTable GetRunningJobsSchema()
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

        private async Task CollectRunningJobsRDSAsync() //Permissions Issue with msdb.dbo.syssessions prevents usual collection.  Get the data we can from sp_help_job
        {
            await using var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand("msdb.dbo.sp_help_job", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@execution_status", 0);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
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

        private async Task CollectRunningJobsAsync()
        {
            if (IsRDS)
            {
                await CollectRunningJobsRDSAsync();
                return;
            }

            await using var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(SqlStrings.RunningJobs, cn);
            using var da = new SqlDataAdapter(cmd);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
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

        private async Task CollectRunningQueriesAsync()
        {
            await using var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(SqlStrings.RunningQueries, cn);
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("CollectSessionWaits", Source.CollectSessionWaits);
            cmd.Parameters.AddWithValue("CollectTranBeginTime", Source.CollectTranBeginTime);
            cmd.Parameters.AddWithValue("CollectTempDB", Source.CollectTempDB);
            await cn.OpenAsync();
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

        private static string PerformanceCountersSQL(string productVersion)
        {
            var xml = PerformanceCounters.PerformanceCountersXML;
            if (xml.Length <= 0) return string.Empty;
            var sql = SqlStrings.PerformanceCounters;
            if (productVersion.StartsWith("8") || productVersion.StartsWith("9"))
            {
                sql = sql.Replace("SYSUTCDATETIME()", "GETUTCDATE()").Replace("DATETIME2", "DATETIME");
            }
            return sql;
        }

        private static async Task<DataSet> GetPerformanceCountersAsync(string connectionString, string productVersion, string countersXML)
        {
            var sql = PerformanceCountersSQL(productVersion);
            if (sql == string.Empty) return null;
            await using var cn = new SqlConnection(connectionString);
            using var da = new SqlDataAdapter(sql, cn);
            await cn.OpenAsync();
            var ds = new DataSet();
            SqlParameter pCountersXML = new("CountersXML", countersXML)
            {
                SqlDbType = SqlDbType.Xml
            };
            da.SelectCommand.CommandTimeout = CollectionType.PerformanceCounters.GetCommandTimeout();
            da.SelectCommand.Parameters.Add(pCountersXML);
            da.Fill(ds);
            return ds;
        }

        private static void MergeCustomPerformanceCounters(DataTable dt, DataTable userDT)
        {
            if (dt.Columns.Count == userDT.Columns.Count)
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
            else
            {
                throw new Exception($"Invalid schema for custom metrics. Expected {dt.Columns.Count} columns instead of {userDT.Columns.Count}.");
            }
        }

        public async Task CollectPerformanceCountersAsync()
        {
            try
            {
                var dt = await GetPerformanceCountersDataTableAsync(ConnectionString, productVersion, PerformanceCounters.PerformanceCountersXML);
                if (dt == null) return;
                Data.Tables.Add(dt);
            }
            catch (Exception ex)
            {
                LogError(ex, "PerformanceCounters");
            }
        }

        public static async Task<DataTable> GetPerformanceCountersDataTableAsync(string connectionString, string productVersion, string countersXML)
        {
            var ds = await GetPerformanceCountersAsync(connectionString, productVersion, countersXML);
            if (ds == null) return null;
            var dt = ds.Tables[0];
            if (ds.Tables.Count == 2)
            {
                var userDT = ds.Tables[1];
                MergeCustomPerformanceCounters(dt, userDT);
            }
            ds.Tables.Remove(dt);
            dt.TableName = "PerformanceCounters";
            return dt;
        }

        private async Task<object> GetSlowQueriesAsync()
        {
            SqlConnectionStringBuilder builder = new(ConnectionString)
            {
                ApplicationName = "DBADashXE"
            };
            var slowQueriesSQL = IsAzureDB ? SqlStrings.SlowQueriesAzure : SqlStrings.SlowQueries;
            await using var cn = new SqlConnection(builder.ConnectionString);
            await using var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = CollectionType.SlowQueries.GetCommandTimeout() };
            await cn.OpenAsync();

            cmd.Parameters.AddWithValue("SlowQueryThreshold", Source.SlowQueryThresholdMs * 1000);
            cmd.Parameters.AddWithValue("MaxMemory", Source.SlowQuerySessionMaxMemoryKB);
            cmd.Parameters.AddWithValue("UseDualSession", Source.UseDualEventSession);
            cmd.Parameters.AddWithValue("MaxTargetMemory", Source.SlowQueryTargetMaxMemoryKB);
            return await cmd.ExecuteScalarAsync();
        }

        private async Task CollectSlowQueriesAsync()
        {
            if (!IsXESupported) return;
            var result = await GetSlowQueriesAsync();
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

        private async Task CollectServerExtraPropertiesAsync()
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
            await AddDataTableAsync("ServerExtraProperties", query, CollectionType.ServerExtraProperties.GetCommandTimeout());
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
            if (Data.Tables["ServerExtraProperties"].Rows[0]["IsWindowsUpdate"] == DBNull.Value && !noWMI)
            {
                CollectIsWindowsUpdateWMI();
                Data.Tables["ServerExtraProperties"].Rows[0]["IsWindowsUpdate"] = IsWindowsUpdate == null ? DBNull.Value : IsWindowsUpdate;
            }
        }

        public async Task<DataTable> GetDataTableAsync(string tableName, string SQL, int commandTimeout, SqlParameter[] param = null)
        {
            await using var cn = new SqlConnection(ConnectionString);
            var cmd = new SqlCommand(SQL, cn) { CommandTimeout = commandTimeout };
            using var da = new SqlDataAdapter(cmd);
            try
            {
                await cn.OpenAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to connect to the SQL instance.");
                throw new DatabaseConnectionException("Unable to connect to the SQL instance.", ex);
            }

            DataTable dt = new();
            if (param != null)
            {
                cmd.Parameters.AddRange(param);
            }
            da.Fill(dt);
            dt.TableName = tableName;
            return dt;
        }

        public async Task AddDataTableAsync(string tableName, string sql, int commandTimeout, SqlParameter[] param = null)
        {
            if (!Data.Tables.Contains(tableName))
            {
                Data.Tables.Add(await GetDataTableAsync(tableName, sql, commandTimeout, param));
            }
        }

        private void AddDT(DataTable dt)
        {
            if (!Data.Tables.Contains(dt.TableName))
            {
                Data.Tables.Add(dt);
            }
        }

        public async Task CollectDrivesSQLAsync()
        {
            try
            {
                await AddDataTableAsync("Drives", SqlStrings.Drives, CollectionType.Drives.GetCommandTimeout());
            }
            catch (Exception ex)
            {
                LogError(ex, "Drives");
            }
        }

        public async Task CollectDrivesAsync()
        {
            if (noWMI)
            {
                await CollectDrivesSQLAsync();
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
                    await CollectDrivesSQLAsync();
                }
            }
        }

        private string activePowerPlan;
        private Guid activePowerPlanGUID;
        private string manufacturer;
        private string model;
        private string WindowsCaption;
        private string ProcessorName;
        private bool? IsWindowsUpdate;

        #region "WMI"

        private async Task CollectInstanceMetadataAsync()
        {
            try
            {
                if (noWMI) return;
                if (InstanceMetadataProviders.EnabledProviders.Count == 0) return;
                var meta = await InstanceMetadataProviders.GetMetadataAsync(computerName, cancellationToken: CancellationToken.None);
                var dt = new DataTable("InstanceMetadata");
                dt.Columns.Add("Provider", typeof(string));
                dt.Columns.Add("Metadata", typeof(string));
                var row = dt.NewRow();
                row["Provider"] = meta.ProviderName;
                row["Metadata"] = meta.Json;
                dt.Rows.Add(row);
                Data.Tables.Add(dt);
            }
            catch (Exception ex)
            {
                LogError(ex, "InstanceMetadata", "Collect:InstanceMetadata");
            }
        }

        private void CollectIsWindowsUpdateWMI()
        {
            try
            {
                var allowMUUpdateService = GetIsWindowsUpdateAllowMUUpdateService(); //Group Policy
                if (allowMUUpdateService)
                {
                    IsWindowsUpdate = true;
                    return;
                }
                var regWithAU = GetIsWindowsUpdateRegisteredWithAU(); // UI
                var defaultService = GetWindowsUpdateDefaultService(); //UI
                IsWindowsUpdate = string.Equals(defaultService, "7971f918-a847-4430-9279-4a52d1efe18d", StringComparison.OrdinalIgnoreCase) && regWithAU;
            }
            catch (Exception ex)
            {
                LogError(ex, "ServerExtraProperties", "Collect:CollectIsWindowsUpdateWMI");
            }
        }

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

        private string GetWindowsUpdateDefaultService()
        {
            using CimMethodParametersCollection CimParams = new()
            {
                CimMethodParameter.Create("hDefKey", LOCAL_MACHINE, CimFlags.In),
                CimMethodParameter.Create("sSubKeyName", @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Services", CimFlags.In),
                CimMethodParameter.Create("sValueName", "DefaultService", CimFlags.In)
            };

            using var session = CimSession.Create(computerName, WMISessionOptions);
            using var results = session.InvokeMethod(new CimInstance("StdRegProv", @"root\default"), "GetStringValue", CimParams);

            return Convert.ToString(results.OutParameters["sValue"].Value);
        }

        private bool GetIsWindowsUpdateRegisteredWithAU()
        {
            using CimMethodParametersCollection CimParams = new()
            {
                CimMethodParameter.Create("hDefKey", LOCAL_MACHINE, CimFlags.In),
                CimMethodParameter.Create("sSubKeyName", @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Services\7971F918-A847-4430-9279-4A52D1EFE18D", CimFlags.In),
                CimMethodParameter.Create("sValueName", "RegisteredWithAU", CimFlags.In)
            };

            using var session = CimSession.Create(computerName, WMISessionOptions);
            using var results = session.InvokeMethod(new CimInstance("StdRegProv", @"root\default"), "GetDWORDValue", CimParams);

            return Convert.ToUInt32(results.OutParameters["uValue"].Value) == 1;
        }

        private bool GetIsWindowsUpdateAllowMUUpdateService()
        {
            using CimMethodParametersCollection CimParams = new()
            {
                CimMethodParameter.Create("hDefKey", LOCAL_MACHINE, CimFlags.In),
                CimMethodParameter.Create("sSubKeyName", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", CimFlags.In),
                CimMethodParameter.Create("sValueName", "AllowMUUpdateService", CimFlags.In)
            };

            using var session = CimSession.Create(computerName, WMISessionOptions);
            using var results = session.InvokeMethod(new CimInstance("StdRegProv", @"root\default"), "GetDWORDValue", CimParams);

            return Convert.ToUInt32(results.OutParameters["uValue"].Value) == 1;
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