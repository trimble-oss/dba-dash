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
            var src = new DBADashSource(GetConnectionString(source))
            {
                NoWMI = NoWMI,
                UseDualEventSession = UseDualEventSession,
                PersistXESessions = PersistXESessions,
                SlowQueryThresholdMs = SlowQueryThresholdMs,
                SlowQueryCaptureMode = SlowQueryCaptureMode,
                SlowQueryXESessionName = SlowQueryXESessionName?.Trim(),
                KeepSlowQueryXESessionRunning = KeepSlowQueryXESessionRunning,
                DeadlockXESessionName = DeadlockXESessionName,
                BackfillDeadlocksFromSystemHealth = BackfillDeadlocksFromSystemHealth,
                QueryStatsTopN = QueryStatsTopN,
                QueryStatsPlansPerCollection = QueryStatsPlansPerCollection,
                RunningQueryPlanThreshold = RunningQueryPlanThreshold,
                RunningQueryLiveProfileThreshold = RunningQueryLiveProfileThreshold,
                SchemaSnapshotDBs = SchemaSnapshotDBs,
                CollectSessionWaits = CollectSessionWaits,
                CollectTaskWaits = CollectTaskWaits,
                CollectCursors = CollectCursors,
                ScriptAgentJobs = ScriptAgentJobs,
                IOCollectionLevel = IOCollectionLevel,
                WriteToSecondaryDestinations = WriteToSecondaryDestinations,
                CollectTempDB = CollectTempDB,
            };
            src.CustomCollections = src.SourceConnection.Type == ConnectionType.SQL ? CustomCollections : null;
            return src;
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
