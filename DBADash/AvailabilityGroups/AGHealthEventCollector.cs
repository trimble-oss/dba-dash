using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DBADash.XE;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DBADash.AvailabilityGroups
{
    /// <summary>
    /// Reads availability group events from the built-in <c>AlwaysOn_health</c> extended events session - replica role
    /// changes with their exact time, the DDL that caused a manual or forced failover, lease timeouts and the HADR errors
    /// the session captures - and returns them as the AGHealthEvents table that <c>dbo.AGHealthEvents_Upd</c> takes.
    ///
    /// <para>Read-only: the session belongs to SQL Server, so it is never created, started or altered.  When it isn't
    /// running there is nothing to read, and role changes still come from comparing AvailabilityReplicas snapshots.</para>
    ///
    /// <para>The event file target is followed with a cursor kept by <see cref="AGHealthEventCursorStore"/>, so each run
    /// reads only what is new.  The first run reads the whole file set from the start, which is what back-fills the
    /// failover history the instance already holds.  Unlike system_health the session is quiet - a few events per
    /// failover - so a full read is cheap and needs no separate backfill.</para>
    ///
    /// <para>Each run starts with one probe for the availability group count and the session's event_file target.  When
    /// either says there is nothing to read, the caller skips the instance until <see cref="RecheckInterval"/> has
    /// passed - see <see cref="IsSkipped"/> and <see cref="RecordStatus"/>.</para>
    /// </summary>
    public static class AGHealthEventCollector
    {
        public const string SessionName = "AlwaysOn_health";

        public const string TableName = "AGHealthEvents";

        public const string ReplicaStateChangeEvent = "availability_replica_state_change";
        public const string DDLExecutedEvent = "alwayson_ddl_executed";
        public const string LeaseExpiredEvent = "availability_group_lease_expired";
        public const string FailoverValidationEvent = "availability_replica_automatic_failover_validation";
        public const string ErrorReportedEvent = "error_reported";

        /// <summary>The sp_server_diagnostics health check.  AlwaysOn_health captures only the ERROR state - the result
        /// that triggers an automatic failover, depending on the AG's failure_condition_level.</summary>
        public const string ServerDiagnosticsEvent = "sp_server_diagnostics_component_result";

        /// <summary>The HADR manager going ONLINE / OFFLINE / PENDING_WSFC_COMMUNICATION - usually cluster or quorum.</summary>
        public const string ReplicaManagerStateChangeEvent = "availability_replica_manager_state_change";

        /// <summary>A replica Starting or Stopping - an instance restart, or the AG being taken offline.</summary>
        public const string ReplicaStartStopEvent = "availability_replica_state";

        /// <summary>A database's synchronization with a partner replica changing.  IDs only - names are resolved by the repository.</summary>
        public const string DatabaseSyncStateEvent = "hadr_db_partner_set_sync_state";

        /// <summary>The events kept.  Everything else in the session is skipped on the server - see
        /// <see cref="EventFileTraceReader"/>'s event name filter.  Not kept: hadr_trace_message (verbose internal
        /// tracing), ucs_connection_setup (noisy - the connection errors that matter arrive as error_reported) and
        /// lock_redo_blocked (can be high volume).</summary>
        public static readonly IReadOnlyList<string> EventNames = new[]
        {
            ReplicaStateChangeEvent, DDLExecutedEvent, LeaseExpiredEvent, FailoverValidationEvent, ErrorReportedEvent,
            ServerDiagnosticsEvent, ReplicaManagerStateChangeEvent, ReplicaStartStopEvent, DatabaseSyncStateEvent
        };

        /// <summary>Rows one read asks for.  The run pages through the file in batches of this size.</summary>
        public const int MaxEventsPerRun = 20000;

        /// <summary>Backstop on the batches a run pages through - the cursor means the next run carries on.</summary>
        public const int MaxBatchesPerRun = 50;

        private const int DetailsMaxLength = 4000;

        private static int CommandTimeout => CollectionType.AGHealthEvents.GetCommandTimeout();

        /// <summary>
        /// How long an instance with nothing to collect - no availability groups, or AlwaysOn_health not running - is
        /// skipped before it is checked again.  Nothing is lost by the wait: the session's files are read from where the
        /// last read stopped (or from the start), so events captured in the meantime are still collected.
        /// </summary>
        public static readonly TimeSpan RecheckInterval = TimeSpan.FromHours(1);

        public enum CollectionStatus
        {
            Collected,
            NoAvailabilityGroups,
            SessionUnavailable
        }

        public sealed class Result
        {
            public DataTable Events { get; init; }

            public CollectionStatus Status { get; init; }
        }

        /// <summary>Per connection: why the last check found nothing to collect, and when to check again.</summary>
        private static readonly ConcurrentDictionary<string, (CollectionStatus Status, DateTime NextCheckUtc)> Unavailable =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>True when a recent check found nothing to collect for <paramref name="key"/> and the recheck isn't due.</summary>
        public static bool IsSkipped(string key) =>
            !string.IsNullOrEmpty(key) && Unavailable.TryGetValue(key, out var entry) && entry.NextCheckUtc > DateTime.UtcNow;

        /// <summary>
        /// Records the outcome of a check.  Returns true when the status differs from the previous check, so a caller can
        /// log a change once rather than on every recheck.
        /// </summary>
        public static bool RecordStatus(string key, CollectionStatus status)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (status == CollectionStatus.Collected)
            {
                return Unavailable.TryRemove(key, out _);
            }
            var changed = !Unavailable.TryGetValue(key, out var previous) || previous.Status != status;
            Unavailable[key] = (status, DateTime.UtcNow.Add(RecheckInterval));
            return changed;
        }

        /// <summary>
        /// One round trip for both reasons there could be nothing to read: the number of availability groups, and the
        /// AlwaysOn_health session's event_file target data (NULL when the session isn't running or has no event_file).
        /// </summary>
        internal const string ProbeSql =
            "SELECT (SELECT COUNT(*) FROM sys.availability_groups) AS AGCount, " +
            "(SELECT CAST(t.target_data AS NVARCHAR(MAX)) " +
            "FROM sys.dm_xe_sessions s " +
            "JOIN sys.dm_xe_session_targets t ON t.event_session_address = s.address " +
            "WHERE s.name = @name AND t.target_name = N'event_file') AS TargetData;";

        public static async Task<Result> CollectAsync(string connectionString, AGHealthEventCollectionState state,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(state);
            var dt = CreateTable();

            int agCount;
            string targetData;
            await using (var cn = new SqlConnection(connectionString))
            await using (var cmd = new SqlCommand(ProbeSql, cn) { CommandType = CommandType.Text, CommandTimeout = CommandTimeout })
            {
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = SessionName;
                await cn.OpenAsync(cancellationToken);
                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                await rdr.ReadAsync(cancellationToken);
                agCount = rdr.GetInt32(0);
                targetData = rdr.IsDBNull(1) ? null : rdr.GetString(1);
            }

            if (agCount == 0)
            {
                return new Result { Events = dt, Status = CollectionStatus.NoAvailabilityGroups };
            }

            var readPath = string.IsNullOrEmpty(targetData) ? null : XESessionTargetResolver.ResolveEventFileReadPath(targetData);
            if (string.IsNullOrEmpty(readPath))
            {
                return new Result { Events = dt, Status = CollectionStatus.SessionUnavailable };
            }

            var reader = new EventFileTraceReader(connectionString, readPath, state.Cursor, MaxEventsPerRun,
                newestFirst: false, eventNameFilter: EventNames, commandTimeout: CommandTimeout);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var batch = 0; batch < MaxBatchesPerRun; batch++)
            {
                var before = reader.Cursor;
                var rows = await reader.ReadRawNextAsync(cancellationToken);

                // Advance even when nothing matched - the rows were still read.
                state.Cursor = reader.Cursor;

                foreach (var row in rows.Where(r => !string.IsNullOrEmpty(r.EventData)))
                {
                    AddEvent(dt, row.EventData, seen);
                }

                if (reader.LastRawRowCount < MaxEventsPerRun) break;
                if (SamePosition(before, reader.Cursor)) break;
            }

            return new Result { Events = dt, Status = CollectionStatus.Collected };
        }

        public static DataTable CreateTable()
        {
            var dt = new DataTable(TableName);
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("EventName", typeof(string));
            dt.Columns.Add("EventHash", typeof(byte[]));
            dt.Columns.Add("group_id", typeof(Guid));
            dt.Columns.Add("AvailabilityGroupName", typeof(string));
            dt.Columns.Add("replica_id", typeof(Guid));
            dt.Columns.Add("ReplicaName", typeof(string));
            dt.Columns.Add("PreviousState", typeof(string));
            dt.Columns.Add("CurrentState", typeof(string));
            dt.Columns.Add("DDLAction", typeof(string));
            dt.Columns.Add("DDLPhase", typeof(string));
            dt.Columns.Add("ErrorNumber", typeof(int));
            dt.Columns.Add("Details", typeof(string));
            dt.Columns.Add("Component", typeof(string));
            dt.Columns.Add("database_id", typeof(int));
            dt.Columns.Add("group_database_id", typeof(Guid));
            return dt;
        }

        /// <summary>
        /// Adds one event's XML to <paramref name="dt"/>.  The hash of the XML is the event's identity, so the same
        /// event read twice - a cursor reset, or a run whose write failed - is recognised by the repository.
        /// </summary>
        internal static void AddEvent(DataTable dt, string eventXml, HashSet<string> seen)
        {
            XElement ev;
            try
            {
                ev = XElement.Parse(eventXml);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not parse an AlwaysOn_health event.");
                return;
            }

            var name = ev.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name) || !TryGetEventTime(ev, out var eventTime)) return;

            var hash = SHA256.HashData(Encoding.Unicode.GetBytes(eventXml));
            // The table type is keyed on (EventTime, EventHash): a duplicate in one batch would fail the import.
            if (!seen.Add($"{eventTime:O}|{Convert.ToHexString(hash)}")) return;

            var fields = ev.Elements("data")
                .Where(d => d.Attribute("name") != null)
                .GroupBy(d => d.Attribute("name")!.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => FieldValue(g.First()), StringComparer.OrdinalIgnoreCase);

            var row = dt.NewRow();
            row["EventTime"] = eventTime;
            row["EventName"] = Trim(name, 60);
            row["EventHash"] = hash;
            row["group_id"] = GuidOrNull(fields, "availability_group_id", "group_id");
            row["AvailabilityGroupName"] = Value(Trim(Get(fields, "availability_group_name"), 128));
            row["replica_id"] = GuidOrNull(fields, "availability_replica_id", "replica_id");
            row["ReplicaName"] = Value(Trim(Get(fields, "availability_replica_name"), 256));

            switch (name)
            {
                case ReplicaStateChangeEvent:
                    row["PreviousState"] = Value(Trim(Get(fields, "previous_state"), 60));
                    row["CurrentState"] = Value(Trim(Get(fields, "current_state"), 60));
                    break;
                case DDLExecutedEvent:
                    row["DDLAction"] = Value(Trim(Get(fields, "ddl_action")?.ToUpperInvariant(), 60));
                    row["DDLPhase"] = Value(Trim(Get(fields, "ddl_phase")?.ToUpperInvariant(), 60)); // The map text is lower case (begin, commit, rollback)
                    row["Details"] = Value(Trim(Get(fields, "statement"), DetailsMaxLength));
                    break;
                case ErrorReportedEvent:
                    row["ErrorNumber"] = int.TryParse(Get(fields, "error_number"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var errorNumber)
                        ? errorNumber
                        : DBNull.Value;
                    row["Details"] = Value(Trim(Get(fields, "message"), DetailsMaxLength));
                    break;
                case FailoverValidationEvent:
                    row["Details"] = Value(Trim(string.Join(", ", fields
                        .Where(f => f.Key is "forced_quorum" or "joined_and_synchronized" or
                            "previous_primary_or_automatic_failover_target")
                        .Select(f => $"{f.Key}={f.Value}")), DetailsMaxLength));
                    break;
                case ServerDiagnosticsEvent:
                    row["Component"] = Value(Trim(Get(fields, "component")?.ToUpperInvariant(), 60));
                    row["CurrentState"] = Value(Trim(Get(fields, "state")?.ToUpperInvariant(), 60));
                    row["Details"] = Value(Trim(SummarizeDiagnostics(ev), DetailsMaxLength));
                    break;
                case ReplicaManagerStateChangeEvent:
                case ReplicaStartStopEvent:
                    row["CurrentState"] = Value(Trim(Get(fields, "current_state")?.ToUpperInvariant(), 60));
                    break;
                case DatabaseSyncStateEvent:
                    row["CurrentState"] = Value(Trim(Get(fields, "sync_state"), 60));
                    row["database_id"] = int.TryParse(Get(fields, "database_id"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var databaseId)
                        ? databaseId
                        : DBNull.Value;
                    row["group_database_id"] = GuidOrNull(fields, "ag_database_id");
                    row["Details"] = Value(Trim(string.Join(", ", fields
                        .Where(f => f.Key is "commit_policy" or "commit_policy_target")
                        .Select(f => $"{f.Key}={f.Value}")), DetailsMaxLength));
                    break;
            }

            dt.Rows.Add(row);
        }

        /// <summary>
        /// A short form of the sp_server_diagnostics component's <c>data</c> payload: the root element's attributes, which
        /// hold the counters the component's state was judged on (e.g. nonYieldingTasksReported, intervalLongIos,
        /// hasUnresolvableDeadlockOccurred).  The full payload - blocking task lists and the like - can run to many KB.
        /// </summary>
        private static string SummarizeDiagnostics(XElement ev)
        {
            var root = ev.Elements("data")
                .FirstOrDefault(d => string.Equals(d.Attribute("name")?.Value, "data", StringComparison.OrdinalIgnoreCase))
                ?.Element("value")?.Elements().FirstOrDefault();
            if (root == null) return null;
            var attributes = string.Join(", ", root.Attributes().Select(a => $"{a.Name.LocalName}={a.Value}"));
            return attributes.Length > 0 ? $"{root.Name.LocalName}: {attributes}" : root.Name.LocalName;
        }

        /// <summary>The text of a mapped value (e.g. PRIMARY_NORMAL) where there is one, otherwise the raw value.</summary>
        private static string FieldValue(XElement data)
        {
            var text = data.Element("text")?.Value;
            return string.IsNullOrEmpty(text) ? data.Element("value")?.Value : text;
        }

        private static string Get(Dictionary<string, string> fields, string name) =>
            fields.TryGetValue(name, out var value) ? value : null;

        /// <summary>The first of <paramref name="names"/> that holds a GUID - hadr_db_partner_set_sync_state names its IDs differently.</summary>
        private static object GuidOrNull(Dictionary<string, string> fields, params string[] names)
        {
            foreach (var name in names)
            {
                if (Guid.TryParse(Get(fields, name), out var value)) return value;
            }
            return DBNull.Value;
        }

        private static bool TryGetEventTime(XElement ev, out DateTime eventTime)
        {
            eventTime = default;
            var raw = ev.Attribute("timestamp")?.Value;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return false;
            }
            // Stored as DATETIME2(3) - truncate so the key matched on is the key stored.
            eventTime = new DateTime(parsed.Ticks - parsed.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Unspecified);
            return true;
        }

        private static bool SamePosition(FileTargetCursor a, FileTargetCursor b) =>
            a.HasValue == b.HasValue
            && a.FileName == b.FileName
            && a.Offset == b.Offset
            && a.ConsumedAtOffset == b.ConsumedAtOffset;

        private static string Trim(string value, int maxLength) =>
            value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;

        private static object Value(object value) => value ?? DBNull.Value;
    }
}
