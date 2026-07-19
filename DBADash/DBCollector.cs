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
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        Drivers,
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
        DatabaseExtendedProperties,
        IdentityColumns,
        Instance,
        RunningJobs,
        TableSize,
        ServerServices,
        AvailableProcs,
        InstanceMetadata,
        FailedLogins,
        ResourceGovernorWorkloadGroups,
        ResourceGovernorResourcePools,
        ScheduleInfo,
        PerfmonCounters
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
        public List<PerfmonCounter> PerfmonCounters { get; set; }

        // Perfmon counter schema (PERF type + base property) is immutable per class, so resolve it at most
        // once per (host, class) for the life of the process rather than on every collection.  Only used as
        // a fallback for counters whose type isn't persisted in config (see PerfmonCounter.CounterType).
        private static readonly ConcurrentDictionary<string, Dictionary<string, PerfmonCounterDiscovery.DiscoveredCounter>> PerfmonMetadataCache
            = new(StringComparer.OrdinalIgnoreCase);

        private string computerName;
        private readonly CollectionType[] azureCollectionTypes = new[] { CollectionType.SlowQueries, CollectionType.AzureDBElasticPoolResourceStats, CollectionType.AzureDBServiceObjectives, CollectionType.AzureDBResourceStats, CollectionType.CPU, CollectionType.DBFiles, CollectionType.Databases, CollectionType.DBConfig, CollectionType.TraceFlags, CollectionType.ObjectExecutionStats, CollectionType.BlockingSnapshot, CollectionType.IOStats, CollectionType.Waits, CollectionType.ServerProperties, CollectionType.DBTuningOptions, CollectionType.SysConfig, CollectionType.DatabasePrincipals, CollectionType.DatabaseRoleMembers, CollectionType.DatabasePermissions, CollectionType.OSInfo, CollectionType.CustomChecks, CollectionType.PerformanceCounters, CollectionType.VLF, CollectionType.DatabaseQueryStoreOptions, CollectionType.AzureDBResourceGovernance, CollectionType.RunningQueries, CollectionType.IdentityColumns, CollectionType.TableSize, CollectionType.AvailableProcs, CollectionType.DatabaseExtendedProperties };
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

        /// <summary>
        /// Set when a Jobs collection ran but was skipped because nothing changed since <see cref="JobLastModified"/>.
        /// Distinguishes a legitimate no-op (emit a heartbeat so monitoring knows the collection is alive) from a
        /// failure or unsupported instance (no heartbeat, so the collection is allowed to go stale/critical).
        /// </summary>
        public bool JobsSkippedNoChange;
        private bool IsHadrEnabled;
        private static readonly ResiliencePipeline EnabledPipeline = CreatePipeline();
        private static readonly ResiliencePipeline DisabledRetryPipeline = new ResiliencePipelineBuilder().Build();
        private ResiliencePipeline pipeline => DisableRetry ? DisabledRetryPipeline : EnabledPipeline;
        private DatabaseEngineEdition engineEdition;
        public bool IsExtendedEventsNotSupportedException;
        private readonly bool DisableRetry;

        // SqlClient connections never issue SET ARITHABORT ON, and ARITHABORT is only implicitly ON when
        // ANSI_WARNINGS is ON *and* the connection database's compatibility level is >= 90.  On instances
        // where the connection database is at compatibility level 80 (or has ANSI_WARNINGS OFF) any collection
        // query that uses XML data type methods (e.g. CPU ring-buffer parsing) fails with "SET options have
        // incorrect settings: 'ARITHABORT'".  This can't be set via the connection string in Microsoft.Data.SqlClient,
        // and it doesn't survive pooled connection reuse (sp_reset_connection resets SET options), so it must ride
        // along in each command batch.  Prepending it makes collection independent of the target's session/database
        // settings.  See issue #1981 (same compatibility-level root cause).
        private const string SetArithAbortOn = "SET ARITHABORT ON;\n";
        public List<Exception> Exceptions = new();
        public int FailedLoginsBackfillMinutes { get; set; } = CollectionConfig.DefaultFailedLoginsBackfillMinutes;

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

        /// <summary>
        /// Cache lifetime in minutes for Resource Governor Workload Groups applicability check
        /// </summary>
        public const int ResourceGovernorWorkloadGroupsCacheMinutes = 60;

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

        public bool IsQueryStoreSupported => IsAzureDB || engineEdition == DatabaseEngineEdition.SqlManagedInstance || (!productVersion.StartsWith("8.") && !productVersion.StartsWith("9.") && !productVersion.StartsWith("10.") && !productVersion.StartsWith("11.") && !productVersion.StartsWith("12."));

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

        /// <summary>
        /// Minimum database compatibility level DBA-Dash supports.  90 = SQL Server 2005, the minimum
        /// supported version.  Below this (i.e. compatibility level 80 / SQL 2000 semantics) collections that
        /// use table-valued dynamic management functions (e.g. IOStats, Drives, VLF) or XML data type methods
        /// fail against the connection database.  See issue #1981.
        /// </summary>
        private const int MinSupportedCompatibilityLevel = 90;

        // Compatibility level of the connection database (the initial catalog), captured during the Instance
        // collection (which always runs first).  Null if it couldn't be determined.
        private int? connectionDBCompatibilityLevel;

        public void LogError(Exception ex, string errorSource, string errorContext = "Collect")
        {
            // When the failure is explained by an unsupported connection-database compatibility level, put the
            // note on the error line itself (not a separate lower-severity line that a raised minimum log level
            // could filter out) and prepend it to the CollectionErrorLog message.
            var unsupportedCompatibilityNote = GetUnsupportedCompatibilityNote(ex);
            if (unsupportedCompatibilityNote == null)
            {
                Log.Error(ex, "{ErrorContext} {ErrorSource} {Connection}", errorContext, errorSource, Source.SourceConnection.ConnectionForPrint);
            }
            else
            {
                Log.Error(ex, "{Note} {ErrorContext} {ErrorSource} {Connection}", unsupportedCompatibilityNote, errorContext, errorSource, Source.SourceConnection.ConnectionForPrint);
            }
            Exceptions.Add(ex);

            var errorMessage = unsupportedCompatibilityNote == null
                ? ex.ToString()
                : unsupportedCompatibilityNote + Environment.NewLine + Environment.NewLine + ex;
            LogDBError(errorSource, errorMessage, errorContext);
        }

        /// <summary>
        /// When a collection fails with a syntax error (Msg 102) and the database used as the initial catalog for
        /// the source connection is at an unsupported compatibility level (below 90), returns a clear message
        /// explaining that the configuration is unsupported and how to resolve it.  Returns null otherwise.
        /// The syntax error is restricted deliberately: under compatibility level 80 the collection queries that
        /// use table-valued DMVs (IOStats, Drives, VLF) fail to parse with Msg 102, whereas other errors on the
        /// same instance are unrelated to the compatibility level and shouldn't be mislabelled.  This is a single
        /// generic check rather than per-collection handling.  The compatibility level is captured during the
        /// Instance collection.
        /// </summary>
        private string GetUnsupportedCompatibilityNote(Exception ex)
        {
            if (!ContainsSyntaxError(ex)) return null;
            if (connectionDBCompatibilityLevel is null or >= MinSupportedCompatibilityLevel) return null;

            var database = string.IsNullOrEmpty(dbName) ? "The database used as the initial catalog for the source connection" : $"The database '{dbName}' (the initial catalog for the source connection)";
            var resolveDatabase = string.IsNullOrEmpty(dbName) ? "that database" : $"the '{dbName}' database";
            return $"{database} is at compatibility level {connectionDBCompatibilityLevel}, which is not supported.  DBA-Dash requires compatibility level {MinSupportedCompatibilityLevel} (SQL Server 2005) or higher.  Some collections use table-valued dynamic management functions or XML data type methods that fail at this compatibility level.  To resolve, increase the compatibility level of {resolveDatabase}, or change the initial catalog of the source connection to a database with a supported compatibility level.";
        }

        /// <summary>
        /// True if a SQL syntax error (Msg 102 - "Incorrect syntax near ...") appears anywhere in the exception
        /// tree.  Some call sites wrap the original SqlException (e.g. RunningQueries text/plan collection wraps it
        /// in a new Exception), so the whole InnerException / AggregateException tree is searched rather than only
        /// the top-level exception.
        /// </summary>
        private static bool ContainsSyntaxError(Exception ex) => ex switch
        {
            null => false,
            SqlException { Number: 102 } => true,
            AggregateException agg => agg.InnerExceptions.Any(ContainsSyntaxError),
            _ => ContainsSyntaxError(ex.InnerException)
        };

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

        private bool IsResourceGovernorApplicable()
        {
            // Always true for SQL MI
            if (engineEdition == DatabaseEngineEdition.SqlManagedInstance) return true;

            // Feature didn't exist before SQL 2008 (v10)
            if (SQLVersion.Major < 10) return false;

            if (SQLVersion.Major >= 17)
            {
                // SQL 2025+ supports both Enterprise and Standard (Including Enterprise Developer & Standard Developer)
                return engineEdition is DatabaseEngineEdition.Enterprise or DatabaseEngineEdition.Standard;
            }

            // 2008 - 2022: Enterprise Edition Only (Including Developer)
            return engineEdition == DatabaseEngineEdition.Enterprise;
        }

        private bool IsResourceGovernorInUse()
        {
            var cacheKey = $"ResourceGovernorInUse_{ConnectionID}";

            // Check cache first to avoid quering the DB each time
            if (cache.Get(cacheKey) is bool cachedResult)
            {
                return cachedResult;
            }
            bool hasWorkloadGroups;
            try
            {
                // Not in cache, query the database
                hasWorkloadGroups = HasWorkloadGroups();
            }
            catch (Exception ex)
            {
                Log.Error("Error checking if Resource Governor is in use for instance {instanceName}. Assuming not applicable. Error: {errorMessage}", ConnectionID, ex.Message);
                return false;
            }

            // Cache the result
            var cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(ResourceGovernorWorkloadGroupsCacheMinutes)
            };
            cache.Set(cacheKey, hasWorkloadGroups, cachePolicy);
            if (!hasWorkloadGroups)
            {
                Log.Information("ResourceGovernorWorkloadGroups/ResourceGovernorResourcePools for instance {instanceName} are disabled as no workload groups have been defined.", ConnectionID);
            }

            return hasWorkloadGroups;
        }

        private bool HasWorkloadGroups()
        {
            using var cn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(SqlStrings.HasWorkloadGroups, cn);
            cn.Open();
            return (bool)cmd.ExecuteScalar();
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
            if (dt.Columns.Contains("ConnectionDBCompatibilityLevel") && dt.Rows[0]["ConnectionDBCompatibilityLevel"] != DBNull.Value)
            {
                connectionDBCompatibilityLevel = Convert.ToInt32(dt.Rows[0]["ConnectionDBCompatibilityLevel"]);
            }
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
            IsRDS = (bool)dt.Rows[0]["IsRDS"];
            if (IsRDS)
            {
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
                return IsResourceGovernorApplicable();
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
            else if (collectionType == CollectionType.TableSize && SQLVersion.Major <= 12 && !IsAzureDB
                && engineEdition != DatabaseEngineEdition.SqlManagedInstance)
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
            else if (collectionType == CollectionType.ScheduleInfo)
            {
                // Reported independently of the monitored instance (see ScheduleInfoReporter) - should
                // never reach the normal per-instance collection pipeline.
                return false;
            }
            if (collectionType == CollectionType.ResourceGovernorWorkloadGroups || collectionType == CollectionType.ResourceGovernorResourcePools)
            {
                return IsResourceGovernorApplicable() && IsResourceGovernorInUse();
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
        ///Get a distinct list of sql_handle from RunningQueries and RunningQueriesCursors tables (if they exist).  The handles are later used to capture query text
        ///</summary>
        private List<string> RunningQueriesHandles()
        {
            var handles = new List<string>();

            if (Data.Tables.Contains("RunningQueries"))
            {
                var runningQueriesHandles = from r in Data.Tables["RunningQueries"]!.AsEnumerable()
                                            where r["sql_handle"] != DBNull.Value
                                            select ((byte[])r["sql_handle"]).ToHexString();
                handles.AddRange(runningQueriesHandles);
            }

            if (Data.Tables.Contains("RunningQueriesCursors"))
            {
                var cursorsHandles = from r in Data.Tables["RunningQueriesCursors"]!.AsEnumerable()
                                     where r["sql_handle"] != DBNull.Value
                                     select ((byte[])r["sql_handle"]).ToHexString();
                handles.AddRange(cursorsHandles);
            }

            return handles.Distinct().ToList();
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
                    new SqlParameter("TableSizeDatabases", SqlDbType.NVarChar,-1) { Value =  Source.TableSizeDatabases ?? TableSizeDatabasesDefault},
                    new SqlParameter("MaxTables", Source.TableSizeMaxTableThreshold ?? TableSizeMaxTableThresholdDefault ),
                    new SqlParameter("MaxDatabases", Source.TableSizeMaxDatabaseThreshold ?? TableSizeMaxDatabaseThreshold)
                };
            }
            else if (collectionType == CollectionType.FailedLogins)
            {
                param = new[] { new SqlParameter("FailedLoginsBackfillMinutes", FailedLoginsBackfillMinutes) };
            }

            if (collectionType == CollectionType.Drives)
            {
                await CollectDrivesAsync();
            }
            else if (collectionType == CollectionType.ServerExtraProperties)
            {
                await CollectServerExtraPropertiesAsync();
            }
            else if (collectionType == CollectionType.Drivers)
            {
                CollectDriversWMI();
            }
            else if (collectionType == CollectionType.PerfmonCounters)
            {
                await CollectPerfmonCountersWMIAsync();
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
                // Reset each attempt so the flag reflects only this run's outcome - guards against a stale
                // "skipped" leaking into a later run (or reused collector) and masking a real failure as a
                // heartbeat.  Only the "not modified" branch below sets it true.
                JobsSkippedNoChange = false;
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
                    JobsSkippedNoChange = true;
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
            await using var cmd = new SqlCommand(SqlStrings.RunningQueries, cn) { CommandTimeout = CollectionType.RunningQueries.GetCommandTimeout() };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("CollectSessionWaits", Source.CollectSessionWaits);
            cmd.Parameters.AddWithValue("CollectTranBeginTime", Source.CollectTranBeginTime);
            cmd.Parameters.AddWithValue("CollectTempDB", Source.CollectTempDB);
            cmd.Parameters.AddWithValue("CollectTaskWaits", Source.CollectTaskWaits);
            cmd.Parameters.AddWithValue("CollectCursors", Source.CollectCursors);
            await cn.OpenAsync();
            var ds = new DataSet();
            da.Fill(ds);
            var dtRunningQueries = ds.Tables[0];
            dtRunningQueries.TableName = "RunningQueries";
            ds.Tables.Remove(dtRunningQueries);
            Data.Tables.Add(dtRunningQueries);
            while (ds.Tables.Count > 0)
            {
                var dt = ds.Tables[0];
                if (dt.Columns.Contains("waiting_tasks_count"))
                {
                    dt.TableName = "SessionWaits";
                }
                else if (dt.Columns.Contains("fetch_status"))
                {
                    dt.TableName = "RunningQueriesCursors";
                }
                ds.Tables.Remove(dt);
                Data.Tables.Add(dt);
            }
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
            using var da = new SqlDataAdapter(SetArithAbortOn + sql, cn);
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

        private async Task<XElement> GetSlowQueriesAsync()
        {
            SqlConnectionStringBuilder builder = new(ConnectionString)
            {
                ApplicationName = "DBADashXE"
            };
            var slowQueriesSQL = IsAzureDB ? SqlStrings.SlowQueriesAzure : SqlStrings.SlowQueries;
            await using var cn = new SqlConnection(builder.ConnectionString);
            await using var cmd = new SqlCommand(slowQueriesSQL, cn) { CommandTimeout = CollectionType.SlowQueries.GetCommandTimeout() };
            await cn.OpenAsync();
            bool collectGroupIDAndPoolID = IsResourceGovernorApplicable() && IsResourceGovernorInUse();
            cmd.Parameters.AddWithValue("SlowQueryThreshold", Source.SlowQueryThresholdMs * 1000);
            cmd.Parameters.AddWithValue("MaxMemory", Source.SlowQuerySessionMaxMemoryKB);
            cmd.Parameters.AddWithValue("UseDualSession", Source.UseDualEventSession);
            cmd.Parameters.AddWithValue("MaxTargetMemory", Source.SlowQueryTargetMaxMemoryKB);
            cmd.Parameters.AddWithValue("CollectGroupIDAndPoolID", collectGroupIDAndPoolID);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
            if (!await reader.ReadAsync() || reader.IsDBNull(0))
                return null;
            using var textReader = reader.GetTextReader(0);
            var settings = new System.Xml.XmlReaderSettings
            {
                Async = true,
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using var xmlReader = System.Xml.XmlReader.Create(textReader, settings);
            return await XElement.LoadAsync(xmlReader, LoadOptions.None, CancellationToken.None);
        }

        private async Task CollectSlowQueriesAsync()
        {
            if (!IsXESupported) return;
            var result = await GetSlowQueriesAsync();
            if (result == null)
            {
                throw new Exception("Result is NULL");
            }
            var dt = XETools.XEStrToDT(result, out RingBufferTargetAttributes ringBufferAtt);
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
            var cmd = new SqlCommand(SetArithAbortOn + SQL, cn) { CommandTimeout = commandTimeout };
            using var da = new SqlDataAdapter(cmd);
            try
            {
                await cn.OpenAsync();
            }
            catch (Exception ex)
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                Log.Error(ex, $"Unable to connect to the SQL instance. {builder.DataSource}");
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

        // Perfmon (PERF_*) counter types we normalise to.  The raw WMI CounterType is mapped to one of
        // these; dbo.PerfmonCounters_Upd cooks each one (see that proc for the formulas).
        private const int PerfLargeRawCount = 65792;   // value as-is
        private const int PerfBulkCount = 272696576;   // rate: Δvalue/Δseconds
        private const int PerfLargeRawFraction = 537003264; // value*100/base
        private const int Perf100NsTimer = 542180608;  // 0x20510500 - % over interval
        private const int Perf100NsTimerInv = 558957824; // 0x21510500 - inverse % over interval
        private const int PerfAverageTimer = 805438464; // (Δvalue/1e7)/Δbase - value pre-scaled to 100ns
        private const int PerfLargeRawBase = 1073939712; // base row marker
        private const int PerfMultiCounter = 0x02000000; // flag: value is summed across instances (_Total)

        // Per-class WMI query timeout.  A wedged perf provider must not hang the collection (and its
        // Task.WhenAll) indefinitely - the collection runs on a 1-minute schedule, so this is a generous
        // backstop that still frees the worker well within the next collection.
        private static readonly TimeSpan PerfmonQueryTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Collect OS-level (perfmon) performance counters from the host via WMI - the RAW
        /// (Win32_PerfRawData_*) classes.  Emits raw accumulators (plus a "&lt;counter&gt; Base" row and,
        /// for timer counters, a 100ns-scaled value) into a "PerfmonCounters" table which
        /// dbo.PerfmonCounters_Upd cooks over the collection interval.  Uses the same WSMan/DCom
        /// session as the other WMI collections.  Distinct from the DMV PerformanceCounters collection.
        /// </summary>
        private async Task CollectPerfmonCountersWMIAsync()
        {
            if (noWMI || !OperatingSystem.IsWindows()) return;
            var counters = PerfmonCounters;
            if (counters == null || counters.Count == 0) return; // no counters configured => nothing to collect
            if (Data.Tables.Contains("PerfmonCounters")) return; // already collected this batch

            try
            {
                var dt = new DataTable("PerfmonCounters");
                dt.Columns.Add("SnapshotDate", typeof(DateTime));
                dt.Columns.Add("object_name", typeof(string));
                dt.Columns.Add("counter_name", typeof(string));
                dt.Columns.Add("instance_name", typeof(string));
                dt.Columns.Add("cntr_value", typeof(decimal));
                dt.Columns.Add("cntr_type", typeof(int));
                dt.Columns.Add("timebase", typeof(decimal)); // Timestamp_Sys100NS for 100ns timers, else 0
                // Stable WMI identity (persisted on dbo.Counters).  Appended last to keep the TVP ordinals stable.
                dt.Columns.Add("WmiClass", typeof(string));
                dt.Columns.Add("WmiProperty", typeof(string));

                var snapshotDate = DateTime.UtcNow;

                var debug = Log.IsEnabled(LogEventLevel.Debug);
                var swSession = debug ? Stopwatch.StartNew() : null;
                using CimSession session = CimSession.Create(computerName, WMISessionOptions);
                if (debug) Log.Debug("Perfmon {computer}: CimSession created in {ms}ms", computerName, swSession.ElapsedMilliseconds);

                // Build a query plan per class (metadata resolve + projection) - cheap and synchronous.
                var plans = counters
                    .Where(c => !string.IsNullOrEmpty(c.WmiClass) && !string.IsNullOrEmpty(c.WmiProperty))
                    .GroupBy(c => c.WmiClass, StringComparer.OrdinalIgnoreCase)
                    .Select(g => BuildPerfmonClassPlan(session, g))
                    .Where(p => p != null)
                    .ToList();
                if (plans.Count == 0) return;

                // Each class query is an independent WSMan round-trip, and cost is dominated by that
                // round-trip (not row count), so run them concurrently via native async I/O.  Awaiting
                // (rather than blocking) releases the collection worker thread for the duration of the
                // WMI wait instead of parking it.
                var perClassRows = await Task.WhenAll(plans.Select(p => CollectPerfmonClassAsync(session, p, snapshotDate)))
                    .ConfigureAwait(false);

                foreach (var rows in perClassRows)
                    foreach (var row in rows)
                        dt.Rows.Add(row);

                if (dt.Rows.Count > 0) Data.Tables.Add(dt);
            }
            catch (Exception ex)
            {
                LogError(ex, "PerfmonCounters", "Collect:WMI");
            }
        }

        /// <summary>A resolved plan to collect one WMI perf class: the narrowed WQL query and, per configured
        /// counter, the metadata (PERF type + base property) needed to cook it.</summary>
        private sealed class PerfmonClassPlan
        {
            public string WmiClass;
            public string Query;
            public List<(PerfmonCounter Ctr, PerfmonCounterDiscovery.DiscoveredCounter Meta)> Resolved;
        }

        private PerfmonClassPlan BuildPerfmonClassPlan(CimSession session, IGrouping<string, PerfmonCounter> classGroup)
        {
            // Resolve each counter's PERF type + base property.  Prefer the values persisted in config
            // (no WMI round-trip); only fall back to the class schema (process-cached) for counters that
            // lack a persisted type - e.g. a hand-edited config.
            Dictionary<string, PerfmonCounterDiscovery.DiscoveredCounter> schema = null;
            var resolved = new List<(PerfmonCounter Ctr, PerfmonCounterDiscovery.DiscoveredCounter Meta)>();
            foreach (var ctr in classGroup)
            {
                PerfmonCounterDiscovery.DiscoveredCounter cm;
                if (ctr.CounterType != 0)
                {
                    cm = new PerfmonCounterDiscovery.DiscoveredCounter
                    {
                        WmiProperty = ctr.WmiProperty,
                        CounterType = ctr.CounterType,
                        BaseWmiProperty = ctr.BaseWmiProperty
                    };
                }
                else
                {
                    schema ??= GetCachedPerfmonMetadata(session, classGroup.Key);
                    if (!schema.TryGetValue(ctr.WmiProperty, out cm)) continue;
                }
                resolved.Add((ctr, cm));
            }
            if (resolved.Count == 0) return null;

            // Only project the columns we actually read (plus the always-present timing columns), rather
            // than SELECT * which marshals every counter the class exposes.
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Name", "Frequency_PerfTime", "Timestamp_Sys100NS" };
            foreach (var (_, cm) in resolved)
            {
                columns.Add(cm.WmiProperty);
                if (!string.IsNullOrEmpty(cm.BaseWmiProperty)) columns.Add(cm.BaseWmiProperty);
            }
            return new PerfmonClassPlan
            {
                WmiClass = classGroup.Key,
                Query = "SELECT " + string.Join(", ", columns) + " FROM " + classGroup.Key,
                Resolved = resolved
            };
        }

        /// <summary>Query one perf class asynchronously and cook its rows.  Never throws - a class absent on
        /// this OS (or any other per-class failure) is logged and yields no rows, so it can't fail the batch
        /// (or the Task.WhenAll).</summary>
        private async Task<List<object[]>> CollectPerfmonClassAsync(CimSession session, PerfmonClassPlan plan, DateTime snapshotDate)
        {
            var rows = new List<object[]>();
            // dbo.Counters key is (object_name, counter_name, instance_name); dedupe overlapping config
            // within the class (cross-class collisions can't happen - object_name is class-derived).
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<CimInstance> instances = null;
            // Per-class timing: which WMI class dominates varies by host, so log it to pinpoint slow classes
            // (some perf providers are far slower to enumerate on certain machines).  Gated so the Stopwatch
            // and the 4-arg log (which allocates + boxes) cost nothing when Debug isn't enabled.
            var debug = Log.IsEnabled(LogEventLevel.Debug);
            var swClass = debug ? Stopwatch.StartNew() : null;
            try
            {
                instances = await QueryInstancesAsync(session, plan.Query).ConfigureAwait(false);
                if (debug) Log.Debug("Perfmon {computer}: {class} queried {instances} instance(s) in {ms}ms",
                    computerName, plan.WmiClass, instances.Count, swClass.ElapsedMilliseconds);

                foreach (var itm in instances)
                {
                    // Single-instance classes (e.g. Memory, System) return a null Name.
                    // The raw _Total for timer counters is already the per-core average (not a sum),
                    // so no instance-count division is needed.
                    var wmiName = Convert.ToString(itm.CimInstanceProperties["Name"]?.Value) ?? string.Empty;
                    var freqPerfTime = ToDecimalOrZero(itm.CimInstanceProperties["Frequency_PerfTime"]?.Value);
                    // The counter's own 100ns timestamp, sampled atomically with the values - used as the
                    // exact time base for 100ns-timer counters instead of wall-clock time.
                    var timestampSys100Ns = ToDecimalOrZero(itm.CimInstanceProperties["Timestamp_Sys100NS"]?.Value);

                    foreach (var (ctr, cm) in plan.Resolved)
                    {
                        // "*" => every instance; otherwise match the WMI Name property.
                        if (ctr.InstanceName != "*" &&
                            !string.Equals(ctr.InstanceName ?? string.Empty, wmiName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        var rawValue = itm.CimInstanceProperties[ctr.WmiProperty]?.Value;
                        if (rawValue == null) continue;

                        // Resolve the display names deterministically from the WMI identity (not from the
                        // per-config names) so the same WMI counter always stores under one identity.  The
                        // stable key (WmiClass / WmiProperty) is persisted alongside for the app to key off.
                        var (resolvedObject, counterName) = PerfmonCounter.ResolveStorageNames(ctr.WmiClass, ctr.WmiProperty);
                        // Namespace the stored object_name so perfmon counters can never collide with DMV
                        // counters in dbo.Counters (see ObjectNamePrefix).
                        var objectName = PerfmonCounter.ObjectNamePrefix + resolvedObject;
                        if (!seen.Add(objectName + "|" + counterName + "|" + wmiName)) continue;

                        var value = ToDecimalOrZero(rawValue);
                        decimal? baseValue = null;
                        int emitType;
                        // Some timers carry the MULTI flag (0x02000000); clear it to reduce to the base
                        // type we cook (harmless for non-multi types).
                        switch (cm.CounterType & ~PerfMultiCounter)
                        {
                            case 65536: case 65792: // RAWCOUNT / LARGE_RAWCOUNT
                                emitType = PerfLargeRawCount; break;
                            case 272696320: case 272696576: // COUNTER / BULK_COUNT (rate)
                                emitType = PerfBulkCount; break;
                            case 537003008: case 537003264: // RAW_FRACTION / LARGE_RAW_FRACTION
                                // Fraction counters can't be cooked without their base (value*100/base).
                                // A base-less config (e.g. stale Win32_PerfFormattedData_* data) is skipped.
                                if (string.IsNullOrEmpty(cm.BaseWmiProperty)) continue;
                                emitType = PerfLargeRawFraction;
                                baseValue = ToDecimalOrZero(itm.CimInstanceProperties[cm.BaseWmiProperty]?.Value);
                                break;
                            case 542180608: emitType = Perf100NsTimer; break;    // 100NSEC_TIMER (+ MULTI)
                            case 558957824: emitType = Perf100NsTimerInv; break; // 100NSEC_TIMER_INV (+ MULTI)
                            case 805438464: // AVERAGE_TIMER (e.g. disk latency)
                                // Average-timer counters need their base too (Δvalue/Δbase); skip if absent.
                                if (string.IsNullOrEmpty(cm.BaseWmiProperty)) continue;
                                emitType = PerfAverageTimer;
                                baseValue = ToDecimalOrZero(itm.CimInstanceProperties[cm.BaseWmiProperty]?.Value);
                                if (freqPerfTime > 0) value = value * 10000000m / freqPerfTime; // ticks -> 100ns units
                                break;
                            default: // unhandled type: collect raw as-is, but flag it so it's not silently wrong
                                Log.Warning("Perfmon counter {object}\\{counter} has unhandled CounterType {type} - collected as a raw value",
                                    objectName, counterName, cm.CounterType);
                                emitType = PerfLargeRawCount; break;
                        }

                        var timebase = emitType is Perf100NsTimer or Perf100NsTimerInv ? timestampSys100Ns : 0m;
                        // Column order must match the DataTable schema built in CollectPerfmonCountersWMIAsync.
                        rows.Add(new object[] { snapshotDate, objectName, counterName, wmiName, value, emitType, timebase, ctr.WmiClass, ctr.WmiProperty });
                        if (baseValue.HasValue)
                        {
                            // Base rows aren't registered as counters (excluded by cntr_type), so their WMI
                            // identity carries the base property for traceability only.
                            rows.Add(new object[] { snapshotDate, objectName, counterName.TrimEnd() + " Base", wmiName, baseValue.Value, PerfLargeRawBase, 0m, ctr.WmiClass, cm.BaseWmiProperty });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // A class absent on this OS shouldn't fail the whole collection.
                LogError(ex, "PerfmonCounters", $"Collect:WMI {plan.WmiClass}");
            }
            finally
            {
                if (instances != null) foreach (var itm in instances) itm.Dispose();
            }
            return rows;
        }

        /// <summary>
        /// Bridge the MI async query (an IObservable&lt;CimInstance&gt;) to a Task that completes with all
        /// instances.  Lets several class queries overlap their WSMan round-trips without a thread each.
        /// A server-side operation timeout (<see cref="PerfmonQueryTimeout"/>) bounds a wedged provider so
        /// it can't hang the collection forever, and the subscription is disposed once the operation ends.
        /// </summary>
        private static Task<List<CimInstance>> QueryInstancesAsync(CimSession session, string query)
        {
            var tcs = new TaskCompletionSource<List<CimInstance>>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Not a 'using': keep the options alive for the whole operation, not just until this method
            // returns, in case MI reads them past Subscribe.  Disposed in the completion continuation below.
            var options = new CimOperationOptions { Timeout = PerfmonQueryTimeout };
            IObservable<CimInstance> op = session.QueryInstancesAsync(@"root\cimv2", "WQL", query, options);
            IDisposable subscription = op.Subscribe(new CimInstanceObserver(tcs));
            // Release the subscription and options once the operation finishes (completed, faulted, or timed out).
            _ = tcs.Task.ContinueWith(_ =>
            {
                subscription.Dispose();
                options.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return tcs.Task;
        }

        private sealed class CimInstanceObserver : IObserver<CimInstance>
        {
            private readonly List<CimInstance> results = new();
            private readonly TaskCompletionSource<List<CimInstance>> tcs;
            public CimInstanceObserver(TaskCompletionSource<List<CimInstance>> tcs) => this.tcs = tcs;
            public void OnNext(CimInstance value) => results.Add(value); // delivered sequentially per operation

            public void OnError(Exception error)
            {
                // On the error path the buffered instances are never handed to the consumer (it awaits the
                // faulted task), so nothing else can dispose them - do it here.  Only when we win the race to
                // complete the task, otherwise OnCompleted already handed ownership to the consumer.
                if (tcs.TrySetException(error))
                    foreach (var itm in results) itm.Dispose();
            }

            public void OnCompleted() => tcs.TrySetResult(results);
        }

        private static decimal ToDecimalOrZero(object value)
        {
            try { return value == null ? 0m : Convert.ToDecimal(value); }
            catch { return 0m; }
        }

        /// <summary>
        /// Resolve a perf class's counter metadata, cached for the life of the process (schema never
        /// changes).  A transient empty result is not cached, so a failed lookup is retried next time.
        /// </summary>
        private Dictionary<string, PerfmonCounterDiscovery.DiscoveredCounter> GetCachedPerfmonMetadata(CimSession session, string wmiClass)
        {
            var key = (computerName ?? string.Empty) + "|" + wmiClass;
            if (PerfmonMetadataCache.TryGetValue(key, out var cached)) return cached;
            var meta = PerfmonCounterDiscovery.GetCounterMetadata(session, computerName, wmiClass);
            if (meta.Count > 0) PerfmonMetadataCache[key] = meta;
            return meta;
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