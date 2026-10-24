using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static DBADash.DBADashConnection;

namespace DBADash
{
    /// <summary>
    /// The options chosen when adding new source connections - everything on the "Add New Connection" options apart from
    /// the connection strings themselves.  Shared by the service configuration tool and the GUI (which sends them to the
    /// service in a <see cref="Messaging.ConnectionAddMessage"/>), so a connection added either way gets the same settings.
    ///
    /// <para>Deliberately not a base class of <see cref="DBADashSource"/>, though the properties overlap:</para>
    /// <list type="bullet">
    /// <item>It's the allowlist of what the GUI can set.  Json.NET serializes the runtime type, so a DBADashSource passed
    /// as a message's options would send everything - including the connection string, whose password is only
    /// obfuscated - unencrypted in the queue.</item>
    /// <item>The defaults differ: these are the add form's (e.g. <see cref="UseDualEventSession"/> off), while
    /// DBADashSource's [DefaultValue]s are part of the ServiceConfig.json format.</item>
    /// <item>DBADashSource's getters return "off" for folder and S3 sources, which a base class has no connection to check.</item>
    /// </list>
    /// <para>The cost is that a new option has to be added here, in <see cref="ApplyTo"/> and <see cref="FromSource"/>,
    /// as well as on DBADashSource and the options tabs.</para>
    /// </summary>
    public class SourceConnectionOptions
    {
        public bool NoWMI { get; set; }
        public bool UseDualEventSession { get; set; }
        public bool PersistXESessions { get; set; }

        /// <summary>-1 disables slow query capture</summary>
        public int SlowQueryThresholdMs { get; set; } = -1;

        public DBADashSource.SlowQueryCaptureModes SlowQueryCaptureMode { get; set; }
        public string SlowQueryXESessionName { get; set; }
        public bool KeepSlowQueryXESessionRunning { get; set; }

        /// <summary>See <see cref="DBADashSource.DeadlockXESessionName"/> - blank disables deadlock collection.</summary>
        public string DeadlockXESessionName { get; set; } = string.Empty;

        public bool BackfillDeadlocksFromSystemHealth { get; set; } = true;

        /// <summary>Zero switches query stats collection off - see <see cref="DBADashSource.QueryStatsTopN"/>.</summary>
        public int QueryStatsTopN { get; set; }

        public int QueryStatsPlansPerCollection { get; set; } = 50;

        /// <summary>Null when plans aren't collected for running queries</summary>
        public PlanCollectionThreshold RunningQueryPlanThreshold { get; set; }

        /// <summary>Null when live plans aren't captured</summary>
        public LiveProfileThreshold RunningQueryLiveProfileThreshold { get; set; }

        public string SchemaSnapshotDBs { get; set; }
        public bool CollectSessionWaits { get; set; } = true;
        public bool CollectTaskWaits { get; set; }
        public bool CollectCursors { get; set; }
        public bool ScriptAgentJobs { get; set; } = true;
        public DBADashSource.IOCollectionLevels IOCollectionLevel { get; set; } = DBADashSource.IOCollectionLevels.Full;
        public bool WriteToSecondaryDestinations { get; set; } = true;
        public bool CollectTempDB { get; set; }

        /// <summary>
        /// Custom collections run SQL of the user's choosing with the service's credentials, so they can only be set in the
        /// service configuration tool - a <see cref="Messaging.ConnectionAddMessage"/> with custom collections is rejected.
        /// </summary>
        public Dictionary<string, CustomCollection> CustomCollections { get; set; }

        /// <summary>
        /// A connection string for <paramref name="source"/>.  A plain instance name (no ';', ':' or UNC prefix) gets a
        /// connection string using Windows authentication, so a list of instance names can be pasted in.  Anything else is
        /// returned as it is - a connection string, folder or S3 path.
        /// </summary>
        public static string GetConnectionString(string source)
        {
            source = source?.Trim() ?? string.Empty;
            if (source.Length == 0 || source.Contains(';') || source.Contains(':') || source.StartsWith("\\\\") ||
                source.StartsWith("//"))
            {
                return source;
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = source,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                Encrypt = true,
                ApplicationName = "DBADash"
            };
            return builder.ConnectionString;
        }

        /// <summary>A new source connection for <paramref name="source"/> (see <see cref="GetConnectionString"/>) with these options.</summary>
        public DBADashSource CreateSource(string source)
        {
            var src = new DBADashSource(GetConnectionString(source));
            ApplyTo(src, includeCustomCollections: false);
            src.CustomCollections = src.SourceConnection.Type == ConnectionType.SQL ? CustomCollections : null;
            return src;
        }

        /// <summary>
        /// Changes an existing connection to these options.  Everything else about it - connection string, ConnectionID,
        /// schedules, and settings that aren't among these options (event file size, query stats tuning, perfmon
        /// counters...) - is kept.  Custom collections are only changed when <paramref name="includeCustomCollections"/>.
        /// </summary>
        public void ApplyTo(DBADashSource src, bool includeCustomCollections)
        {
            src.NoWMI = NoWMI;
            src.UseDualEventSession = UseDualEventSession;
            src.PersistXESessions = PersistXESessions;
            src.SlowQueryThresholdMs = SlowQueryThresholdMs;
            src.SlowQueryCaptureMode = SlowQueryCaptureMode;
            src.SlowQueryXESessionName = SlowQueryXESessionName?.Trim();
            src.KeepSlowQueryXESessionRunning = KeepSlowQueryXESessionRunning;
            src.DeadlockXESessionName = DeadlockXESessionName;
            src.BackfillDeadlocksFromSystemHealth = BackfillDeadlocksFromSystemHealth;
            src.QueryStatsTopN = QueryStatsTopN;
            src.QueryStatsPlansPerCollection = QueryStatsPlansPerCollection;
            src.RunningQueryPlanThreshold = RunningQueryPlanThreshold;
            src.RunningQueryLiveProfileThreshold = RunningQueryLiveProfileThreshold;
            src.SchemaSnapshotDBs = SchemaSnapshotDBs;
            src.CollectSessionWaits = CollectSessionWaits;
            src.CollectTaskWaits = CollectTaskWaits;
            src.CollectCursors = CollectCursors;
            src.ScriptAgentJobs = ScriptAgentJobs;
            src.IOCollectionLevel = IOCollectionLevel;
            src.WriteToSecondaryDestinations = WriteToSecondaryDestinations;
            src.CollectTempDB = CollectTempDB;
            if (includeCustomCollections)
            {
                src.CustomCollections = CustomCollections;
            }
        }

        /// <summary>The options <paramref name="src"/> was configured with - e.g. to edit an existing connection.</summary>
        public static SourceConnectionOptions FromSource(DBADashSource src) => new()
        {
            NoWMI = src.NoWMI,
            UseDualEventSession = src.UseDualEventSession,
            PersistXESessions = src.PersistXESessions,
            SlowQueryThresholdMs = src.SlowQueryThresholdMs,
            SlowQueryCaptureMode = src.SlowQueryCaptureMode,
            SlowQueryXESessionName = src.SlowQueryXESessionName,
            KeepSlowQueryXESessionRunning = src.KeepSlowQueryXESessionRunning,
            DeadlockXESessionName = src.DeadlockXESessionName,
            BackfillDeadlocksFromSystemHealth = src.BackfillDeadlocksFromSystemHealth,
            QueryStatsTopN = src.QueryStatsTopN,
            QueryStatsPlansPerCollection = src.QueryStatsPlansPerCollection,
            RunningQueryPlanThreshold = src.RunningQueryPlanThreshold,
            RunningQueryLiveProfileThreshold = src.RunningQueryLiveProfileThreshold,
            SchemaSnapshotDBs = src.SchemaSnapshotDBs,
            CollectSessionWaits = src.CollectSessionWaits,
            CollectTaskWaits = src.CollectTaskWaits,
            CollectCursors = src.CollectCursors,
            ScriptAgentJobs = src.ScriptAgentJobs,
            IOCollectionLevel = src.IOCollectionLevel,
            WriteToSecondaryDestinations = src.WriteToSecondaryDestinations,
            CollectTempDB = src.CollectTempDB,
            CustomCollections = src.CustomCollections
        };

        /// <summary>
        /// Why these options can't be used, or null if they can.  The same checks as the options tabs make, repeated by the
        /// service for options it receives in a message.
        /// </summary>
        public string Validate()
        {
            if (SlowQueryThresholdMs >= 0 && SlowQueryCaptureMode == DBADashSource.SlowQueryCaptureModes.ExistingSession)
            {
                if (string.IsNullOrWhiteSpace(SlowQueryXESessionName))
                {
                    return "Enter the name of the extended events session to read slow queries from";
                }

                if (DBADashSource.IsReservedSlowQueryXESessionName(SlowQueryXESessionName))
                {
                    return "This is a session DBA Dash creates and removes itself.  Use the EventFile or RingBuffer capture mode, or a session of your own with a different name.";
                }
            }

            if (QueryStatsTopN < 0 || QueryStatsPlansPerCollection < 0)
            {
                return "Query stats options can't be negative";
            }

            return null;
        }

        public string Serialize() => JsonConvert.SerializeObject(this);
    }
}
