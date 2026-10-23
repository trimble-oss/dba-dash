using DBADash.QueryPlan.Live;
using System.Data;
using System.Globalization;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Follows a running request for the plan viewer, whatever reads the instance for it.  The plan is fetched whole once
    /// (LiveQuerySql.LivePlan); each refresh after that asks only for the operators' counters (LiveQuerySql.LiveQueryProfile)
    /// and writes them into the plan it has.  If the request has moved on to another statement the counters belong to a
    /// different plan, so that one is fetched whole instead.<br/>
    /// The DBA Dash GUI reads through messaging; the viewers read over a direct connection when a live plan is opened
    /// from SSMS.  Each says how in <see cref="ReadProfileAsync"/> and <see cref="ReadPlanAsync"/>, returning what the SQL
    /// returns: a Request and a Profiles table, and a LivePlan table.
    /// </summary>
    public abstract class LivePlanFollower : ILivePlanSource
    {
        private string _planXml;
        private string _statement;
        private List<PlanLiveCounters> _previous;

        // Shown again for a refresh that had to be skipped - see CaptureAsync.
        private PlanLiveSnapshot _lastSnapshot;

        /// <param name="sessionId">The session running the request, for messages.</param>
        /// <param name="initial">The LivePlan row the plan was first opened from, which says which statement it is for.</param>
        protected LivePlanFollower(int sessionId, DataRow initial)
        {
            SessionId = sessionId;
            _planXml = PlanOf(initial);
            _statement = StatementOf(initial);
        }

        protected int SessionId { get; }

        public abstract string Description { get; }

        public abstract string RequestKey { get; }

        /// <summary>The request's status and its operator counters: a Request table, and a Profiles table while it is running.</summary>
        protected abstract Task<DataSet> ReadProfileAsync(CancellationToken cancellationToken);

        /// <summary>The request's whole live plan: a LivePlan table.</summary>
        protected abstract Task<DataSet> ReadPlanAsync(CancellationToken cancellationToken);

        /// <summary>A UTC time from the instance, in the time zone the host shows times in.</summary>
        protected abstract DateTime ToDisplayTime(DateTime utc);

        public async Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken)
        {
            var data = await ReadProfileAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var request = data.Tables["Request"]?.Rows.Cast<DataRow>().FirstOrDefault()
                          ?? throw new InvalidOperationException("The request's status was not returned.");
            var captured = Convert.ToDateTime(request["captured_utc"]);
            var status = Convert.ToString(request["Status"]);

            // The request changed statement while the counters were read, so they couldn't be put down to one plan.  The
            // plan and its states stay as they were until the next refresh.
            if (string.Equals(status, "Moving", StringComparison.OrdinalIgnoreCase))
            {
                return new LivePlanCapture
                {
                    Live = _lastSnapshot,
                    CapturedAt = ToDisplayTime(captured),
                    Message = "Skipped - the request moved on to another statement"
                };
            }

            if (!string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                return new LivePlanCapture
                {
                    Completed = true,
                    CapturedAt = ToDisplayTime(captured),
                    Message = $"Session {SessionId} is no longer running this request - the query has completed.  Showing the plan as last captured."
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
                        CapturedAt = ToDisplayTime(captured),
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
                        CapturedAt = ToDisplayTime(captured),
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
                CapturedAt = ToDisplayTime(captured),
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
            var data = await ReadPlanAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var row = data.Tables["LivePlan"]?.Rows.Cast<DataRow>().FirstOrDefault()
                      ?? throw new InvalidOperationException("The plan for the new statement was not found.  The query may have completed.");
            if (row["query_plan"] == DBNull.Value) return false;

            _planXml = PlanOf(row);
            _statement = StatementOf(row);
            return true;
        }

        /// <summary>The plan XML, with the statement text filled in when the statistics XML left it blank.</summary>
        public static string PlanOf(DataRow row)
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

        public static PlanLiveCounters CountersOf(DataRow row) => new()
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

        /// <summary>The start of a key for <see cref="RequestKey"/>: the session and its request start time.</summary>
        protected string RequestKeyOf(string instance, DateTime startTimeUtc) =>
            string.Join('|', instance, SessionId.ToString(CultureInfo.InvariantCulture), startTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture));

        private static long? LongOrNull(object value) => value == DBNull.Value || value is null ? null : Convert.ToInt64(value);
    }
}
