using DBADash.Messaging;
using DBADash.QueryPlan.Live;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Follows a running request for the plan viewer, through messaging.  The plan is fetched whole once
    /// (<see cref="LivePlanMessage"/>); each refresh after that asks only for the operators' counters
    /// (<see cref="LiveQueryProfileMessage"/>) and writes them into the plan it has.  If the request has moved on to
    /// another statement the counters belong to a different plan, so that one is fetched whole instead.
    /// </summary>
    internal sealed class LivePlanSource : ILivePlanSource
    {
        private readonly DBADashContext _context;
        private readonly int _sessionId;
        private readonly DateTime _startTimeUtc;
        private readonly int _snapshotUtcOffset;

        private string _planXml;
        private string _statement;
        private List<PlanLiveCounters> _previous;

        // Shown again for a refresh that had to be skipped - see CaptureAsync.
        private PlanLiveSnapshot _lastSnapshot;

        /// <param name="initial">The LivePlan row the plan was first opened from, which says which statement it is for.</param>
        /// <param name="snapshotUtcOffset">The instance UTC offset, in minutes, captured with the snapshot. Sent on every poll so a daylight saving change is the only start-time shift that still matches.</param>
        public LivePlanSource(DBADashContext context, int sessionId, DateTime startTimeUtc, int snapshotUtcOffset, DataRow initial)
        {
            _context = context;
            _sessionId = sessionId;
            _startTimeUtc = startTimeUtc;
            _snapshotUtcOffset = snapshotUtcOffset;
            _planXml = PlanOf(initial);
            _statement = StatementOf(initial);
        }

        public string Description => $"Session {_sessionId} on {_context.InstanceName}";

        public string RequestKey => string.Join('|', _context.ConnectionID, _sessionId.ToString(CultureInfo.InvariantCulture), _startTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));

        public async Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken)
        {
            var data = await SendAsync(new LiveQueryProfileMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                SessionID = _sessionId,
                StartTimeUtc = _startTimeUtc,
                SnapshotUtcOffset = _snapshotUtcOffset,
                Lifetime = 30
            }, cancellationToken);

            var request = data.Tables["Request"]?.Rows.Cast<DataRow>().FirstOrDefault()
                          ?? throw new InvalidOperationException("The service didn't return the request's status.");
            var captured = Convert.ToDateTime(request["captured_utc"]);
            var status = Convert.ToString(request["Status"]);

            // The request changed statement while the counters were read, so they couldn't be put down to one plan.  The
            // plan and its states stay as they were until the next refresh.
            if (string.Equals(status, "Moving", StringComparison.OrdinalIgnoreCase))
            {
                return new LivePlanCapture
                {
                    Live = _lastSnapshot,
                    CapturedAt = captured.ToAppTimeZone(),
                    Message = "Skipped - the request moved on to another statement"
                };
            }

            if (!string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                return new LivePlanCapture
                {
                    Completed = true,
                    CapturedAt = captured.ToAppTimeZone(),
                    Message = $"Session {_sessionId} is no longer running this request - the query has completed.  Showing the plan as last captured."
                };
            }

            // Node ids are only unique within a statement: counters for another statement would be written into the
            // wrong operators, so a new statement means a new plan.
            var statement = StatementOf(request);
            var changed = !string.Equals(statement, _statement, StringComparison.Ordinal);
            if (changed)
            {
                // A statement such as WAITFOR has no plan. That is the request carrying on, not a failed refresh:
                // counting it as a failure would stop auto refresh before a later statement could be followed.
                if (!await FetchPlanAsync(cancellationToken))
                {
                    return new LivePlanCapture
                    {
                        Live = _lastSnapshot,
                        CapturedAt = captured.ToAppTimeZone(),
                        Message = "Skipped - this statement has no plan"
                    };
                }

                _previous = null;
                _lastSnapshot = null;

                // The plan is fetched after the counters were read, and the request can have moved on again in between.
                // The counters are only for the statement they were read with, so a plan for any other is shown as it was
                // fetched - its own counters - and the next refresh carries on from it.
                if (!string.Equals(statement, _statement, StringComparison.Ordinal))
                {
                    return new LivePlanCapture
                    {
                        PlanXml = _planXml,
                        CapturedAt = captured.ToAppTimeZone(),
                        PlanChanged = true
                    };
                }
            }

            var counters = data.Tables["Profiles"]?.Rows.Cast<DataRow>().Select(CountersOf).ToList() ?? [];
            _planXml = PlanLivePatcher.Apply(_planXml, counters);

            var snapshot = PlanLiveSnapshot.From(counters, _previous, captured);
            _previous = counters;
            _lastSnapshot = snapshot;

            return new LivePlanCapture
            {
                PlanXml = _planXml,
                Live = snapshot,
                CapturedAt = captured.ToAppTimeZone(),
                PlanChanged = changed,
                Elapsed = LongOrNull(request["total_elapsed_time"]) is { } ms ? TimeSpan.FromMilliseconds(ms) : null,
                Message = counters.Count == 0 ? "No operator counters were returned - lightweight query profiling may be off" : null
            };
        }

        /// <summary>
        /// The whole plan, for when the request has moved on to another statement.
        /// False when that statement has no plan: the request is still the one being followed.
        /// </summary>
        private async Task<bool> FetchPlanAsync(CancellationToken cancellationToken)
        {
            var data = await SendAsync(new LivePlanMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                SessionID = _sessionId,
                StartTimeUtc = _startTimeUtc,
                SnapshotUtcOffset = _snapshotUtcOffset,
                Lifetime = 60
            }, cancellationToken);

            var row = data.Tables["LivePlan"]?.Rows.Cast<DataRow>().FirstOrDefault()
                      ?? throw new InvalidOperationException("The plan for the new statement was not found.  The query may have completed.");
            if (row["query_plan"] == DBNull.Value) return false;

            _planXml = PlanOf(row);
            _statement = StatementOf(row);
            return true;
        }

        private async Task<DataSet> SendAsync(MessageBase message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_context.ImportAgentID is not { } importAgent)
            {
                throw new InvalidOperationException("Messaging is not available for this instance.");
            }

            // Failures to send are reported through the status rather than the outcome, so the last one is kept to say
            // why there was no reply.
            string failure = null;
            var outcome = await MessagingHelper.SendAndWaitAsync(message, importAgent, (text, _, colour) =>
            {
                if (colour == DashColors.Fail) failure = text;
            });
            cancellationToken.ThrowIfCancellationRequested();

            if (!outcome.Succeeded) throw new InvalidOperationException(failure ?? outcome.Error);
            return outcome.Data ?? throw new InvalidOperationException("The service returned no data.");
        }

        /// <summary>The plan XML, with the statement text filled in when the statistics XML left it blank.</summary>
        private static string PlanOf(DataRow row)
        {
            var text = row.Table.Columns.Contains("statement_text") && row["statement_text"] != DBNull.Value
                ? Convert.ToString(row["statement_text"])
                : null;
            return PlanLiveStatementText.Apply(PlanLiveMark.Apply((string)row["query_plan"]), text);
        }

        /// <summary>Which statement of which batch is running: its offsets, plan hash and sql_handle.</summary>
        private static string StatementOf(DataRow row)
        {
            var columns = row.Table.Columns;
            if (!columns.Contains("statement_start_offset")) return string.Empty;

            var hash = row["query_plan_hash"] is byte[] bytes ? Convert.ToHexString(bytes) : string.Empty;
            var handle = columns.Contains("sql_handle") && row["sql_handle"] is byte[] handleBytes
                ? Convert.ToHexString(handleBytes)
                : string.Empty;
            return $"{row["statement_start_offset"]}:{row["statement_end_offset"]}:{hash}:{handle}";
        }

        internal static PlanLiveCounters CountersOf(DataRow row) => new()
        {
            NodeId = Convert.ToInt32(row["node_id"]),
            Thread = Convert.ToInt32(row["thread_id"]),
            Rows = LongOrNull(row["row_count"]) ?? 0,
            Rebinds = LongOrNull(row["rebind_count"]) ?? 0,
            Rewinds = LongOrNull(row["rewind_count"]) ?? 0,
            EndOfScans = LongOrNull(row["end_of_scan_count"]) ?? 0,
            ElapsedMs = LongOrNull(row["elapsed_time_ms"]),
            CpuMs = LongOrNull(row["cpu_time_ms"]),
            Scans = LongOrNull(row["scan_count"]),
            LogicalReads = LongOrNull(row["logical_read_count"]),
            PhysicalReads = LongOrNull(row["physical_read_count"]),
            ReadAheads = LongOrNull(row["read_ahead_count"]),
            LobLogicalReads = LongOrNull(row["lob_logical_read_count"]),
            LobPhysicalReads = LongOrNull(row["lob_physical_read_count"]),
            RowsRead = LongOrNull(row["actual_read_row_count"]),
            OpenTimeMs = LongOrNull(row["open_time"]),
            CloseTimeMs = LongOrNull(row["close_time"])
        };

        private static long? LongOrNull(object value) => value == DBNull.Value || value is null ? null : Convert.ToInt64(value);
    }
}
