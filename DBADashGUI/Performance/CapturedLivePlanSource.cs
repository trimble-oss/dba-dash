using DBADash;
using DBADash.QueryPlan.Live;
using DBADashGUI.Viewers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// A live plan kept by the running queries collection (<see cref="LiveProfileCollector"/>): the plan from the
    /// repository with the operator counters the collection last saw written into it, shown as the live plan viewer
    /// would have shown it then.  The counters kept before those say which operators were moving rows.<br/>
    /// Refreshing follows the query live through messaging (<see cref="LivePlanSource"/>), which only finds it if it is
    /// still running.
    /// </summary>
    internal sealed class CapturedLivePlanSource : ILivePlanSource
    {
        private readonly DBADashContext _context;
        private readonly int _sessionId;
        private readonly DateTime _startTimeUtc;
        private readonly DataRow _planRow;
        private LivePlanSource _live;

        private CapturedLivePlanSource(DBADashContext context, int sessionId, DateTime startTimeUtc, DataRow planRow, LivePlanCapture initial)
        {
            _context = context;
            _sessionId = sessionId;
            _startTimeUtc = startTimeUtc;
            _planRow = planRow;
            Initial = initial;
        }

        /// <summary>The plan with the kept counters written into it.</summary>
        public string PlanXml => (string)_planRow["query_plan"];

        public LivePlanCapture Initial { get; }

        public string Description => $"Session {_sessionId} on {_context.InstanceName}";

        // None, so each capture opens in a tab of its own.  A request can have a capture for each of its statements, and a
        // tab already following the request - live, or a capture refreshed - may have moved on to another statement since,
        // so selecting a tab by request could show a different statement from the one asked for.
        public string RequestKey => null;

        public Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken)
        {
            if (!_context.CanMessage)
            {
                throw new InvalidOperationException("Messaging is not available for this instance, so the query can't be followed live.");
            }
            // Carries on from the kept capture: the same plan and statement, so newer counters are written into it
            _live ??= new LivePlanSource(_context, _sessionId, _startTimeUtc, _context.UTCOffset, _planRow);
            return _live.CaptureAsync(cancellationToken);
        }

        /// <summary>
        /// The capture kept for the request, statement and plan, or null when there isn't one or its plan is no longer in
        /// the repository.  Offsets are only unique within a batch, so the batch (sql_handle) is part of which statement it
        /// is, and the plan hash which plan.
        /// </summary>
        public static CapturedLivePlanSource Load(DBADashContext context, int sessionId, DateTime startTimeUtc, byte[] sqlHandle, int startOffset, int endOffset, byte[] planHash)
        {
            var dt = new DataTable();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueryLiveProfile_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", context.InstanceID);
                cmd.Parameters.AddWithValue("session_id", sessionId);
                cmd.Parameters.Add("start_time_utc", SqlDbType.DateTime).Value = startTimeUtc;
                cmd.Parameters.AddWithValue("statement_start_offset", startOffset);
                cmd.Parameters.AddWithValue("statement_end_offset", endOffset);
                cmd.Parameters.Add("sql_handle", SqlDbType.VarBinary, 64).Value = sqlHandle;
                cmd.Parameters.Add("query_plan_hash", SqlDbType.Binary, 8).Value = planHash;
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0 || dt.Rows[0]["query_plan_compresed"] == DBNull.Value) return null;
            var row = dt.Rows[0];

            var counters = CountersOf((byte[])row["Counters"]);
            var previous = row["PreviousCounters"] == DBNull.Value ? null : CountersOf((byte[])row["PreviousCounters"]);
            if (previous != null && Restarted(counters, previous)) previous = null;
            var capturedUtc = Convert.ToDateTime(row["LastCapturedUTC"]);
            var firstUtc = Convert.ToDateTime(row["FirstCapturedUTC"]);
            var captures = Convert.ToInt32(row["CaptureCount"]);

            var plan = PlanLivePatcher.Apply(PlanLiveMark.Apply(SMOBaseClass.Unzip((byte[])row["query_plan_compresed"])), counters);

            // What LivePlanSource needs to carry on from it: the plan, and which statement of which batch it is for
            var planTable = new DataTable();
            planTable.Columns.Add("query_plan", typeof(string));
            planTable.Columns.Add("statement_start_offset", typeof(int));
            planTable.Columns.Add("statement_end_offset", typeof(int));
            planTable.Columns.Add("query_plan_hash", typeof(byte[]));
            planTable.Columns.Add("sql_handle", typeof(byte[]));
            var planRow = planTable.Rows.Add(plan, startOffset, endOffset, row["query_plan_hash"], row["sql_handle"]);

            var initial = new LivePlanCapture
            {
                Live = PlanLiveSnapshot.From(counters, previous, capturedUtc),
                CapturedAt = capturedUtc.ToAppTimeZone(),
                Elapsed = row["total_elapsed_time"] == DBNull.Value ? null : TimeSpan.FromMilliseconds(Convert.ToInt64(row["total_elapsed_time"])),
                Message = (captures == 1
                              ? "Kept by the running queries collection."
                              : $"Kept by the running queries collection - the last of {captures} captures since {firstUtc.ToAppTimeZone():G}.")
                          + "  Refresh to follow the query live if it is still running."
            };
            return new CapturedLivePlanSource(context, sessionId, startTimeUtc, planRow, initial);
        }

        /// <summary>
        /// True when an operator has fewer rows or executions than the capture before: the statement ran again within the
        /// request - in a loop, say - and its counters started again.  Compared with the earlier run, operators would look
        /// active for having changed, so the earlier capture says nothing about this one.
        /// </summary>
        internal static bool Restarted(IEnumerable<PlanLiveCounters> current, IEnumerable<PlanLiveCounters> previous)
        {
            var now = current.GroupBy(c => c.NodeId).ToDictionary(g => g.Key, g => (Rows: g.Sum(c => c.Rows), Executions: g.Sum(c => c.Executions)));
            return previous.GroupBy(c => c.NodeId).Any(g =>
                now.TryGetValue(g.Key, out var n) && (n.Rows < g.Sum(c => c.Rows) || n.Executions < g.Sum(c => c.Executions)));
        }

        private static List<PlanLiveCounters> CountersOf(byte[] counters) =>
            LiveProfileCounters.Decode(counters).Rows.Cast<DataRow>().Select(LivePlanFollower.CountersOf).ToList();
    }
}
