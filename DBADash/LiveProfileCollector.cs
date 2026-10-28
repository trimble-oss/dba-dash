using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBADash
{
    /// <summary>A request from the running queries snapshot to keep the operator counters of - see <see cref="LiveProfileCollector"/>.</summary>
    public sealed record LiveProfileRequest(
        short SessionID,
        DateTime StartTimeUtc,
        int StartOffset,
        int EndOffset,
        byte[] PlanHandle,
        byte[] PlanHash,
        byte[] SqlHandle);

    /// <summary>
    /// Keeps the operator counters of long running queries as the running queries collection sees them, so the plan
    /// can be shown later with how far each operator had got - the same view a live plan gives, for a query nobody was
    /// watching at the time.<br/>
    /// Only the counters from sys.dm_exec_query_profiles are read: a few small rows per operator.  The plan they go with
    /// is collected the usual way, once per plan rather than per capture (<see cref="Plan"/>), and the GUI writes the
    /// counters into it.  Each collection replaces what the last one kept for the same request and statement, so a query
    /// that runs for an hour is one row rather than sixty.<br/>
    /// Needs lightweight query profiling (on by default from SQL 2019, trace flag 7412 on SQL 2016 SP1 and 2017).  Without
    /// it there are no counters and nothing is kept.  It is never turned on from here.
    /// </summary>
    public static class LiveProfileCollector
    {
        public const string TableName = "RunningQueryLiveProfiles";

        /// <summary>
        /// The counters of each of <paramref name="requests"/> that is still running the statement the snapshot saw, as
        /// a <see cref="TableName"/> table.  One that has finished or moved on to another statement since is left out:
        /// its counters would belong to a different plan.
        /// </summary>
        public static async Task<DataTable> CollectAsync(IReadOnlyList<LiveProfileRequest> requests, string connectionString)
        {
            var result = NewTable();
            if (requests.Count == 0) return result;

            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(SqlStrings.GetSqlString("LiveProfiles"), cn);
            cmd.Parameters.Add("@Requests", SqlDbType.Xml).Value = RequestsXml(requests);
            await cn.OpenAsync();

            var captured = new DataTable();
            var profiles = new DataTable();
            await using (var rdr = await cmd.ExecuteReaderAsync())
            {
                captured.Load(rdr);
                if (!rdr.IsClosed) profiles.Load(rdr);
            }

            var bySession = requests.ToDictionary(r => r.SessionID);
            foreach (DataRow row in captured.Rows)
            {
                var sessionId = Convert.ToInt16(row["session_id"]);
                if (!bySession.TryGetValue(sessionId, out var request)) continue;

                // No counters: lightweight profiling is off, or the statement has nothing to profile
                if (!profiles.Rows.Cast<DataRow>().Any(p => Convert.ToInt16(p["session_id"]) == sessionId)) continue;

                result.Rows.Add(
                    sessionId,
                    request.StartTimeUtc,
                    request.StartOffset,
                    request.EndOffset,
                    request.PlanHandle,
                    request.PlanHash,
                    request.SqlHandle,
                    row["captured_utc"],
                    row["cpu_time"],
                    row["total_elapsed_time"],
                    LiveProfileCounters.Encode(profiles, p => Convert.ToInt16(p["session_id"]) == sessionId));
            }
            return result;
        }

        /// <summary>
        /// The requests in a running queries snapshot to capture: those meeting <paramref name="threshold"/> whose
        /// statement's plan meets the plan collection threshold (<paramref name="planKeys"/>, see <see cref="Plan.Key"/>),
        /// highest CPU first, up to the limit per collection.
        /// </summary>
        public static List<LiveProfileRequest> SelectRequests(DataTable runningQueries, IReadOnlySet<string> planKeys, LiveProfileThreshold threshold) =>
            runningQueries.AsEnumerable()
                // One request per session - the snapshot can have more than one for a MARS session.  The one the import
                // keeps (RunningQueries_Upd: highest CPU), chosen before the thresholds: a capture of another request would
                // have no snapshot row to open it from, and nothing keeping its plan.
                .GroupBy(r => Convert.ToInt16(r["session_id"]))
                .Select(g => g.OrderByDescending(r => r["cpu_time"] == DBNull.Value ? -1L : Convert.ToInt64(r["cpu_time"])).First())
                .Where(r => r["start_time_utc"] != DBNull.Value
                            && r["sql_handle"] != DBNull.Value
                            && r["plan_handle"] != DBNull.Value
                            && r["query_plan_hash"] != DBNull.Value
                            && r["statement_start_offset"] != DBNull.Value
                            && r["statement_end_offset"] != DBNull.Value
                            && r["cpu_time"] != DBNull.Value)
                .Where(r => (Convert.ToInt64(r["cpu_time"]) >= threshold.CPUThreshold
                             && StatementCouldHaveUsed(r) >= threshold.CPUThreshold)
                            // A blocked request's operators aren't moving, so how long it has been waiting doesn't make its counters worth keeping
                            || (Convert.ToInt32(r["blocking_session_id"]) == 0
                                && ((DateTime)r["SnapshotDateUTC"]).Subtract((DateTime)r["start_time_utc"]).TotalMilliseconds >= threshold.DurationThreshold
                                && StatementRunningFor(r) >= threshold.DurationThreshold))
                .Where(r => planKeys.Contains(new Plan((byte[])r["plan_handle"], (byte[])r["query_plan_hash"], (int)r["statement_start_offset"], (int)r["statement_end_offset"]).Key))
                .OrderByDescending(r => Convert.ToInt64(r["cpu_time"]))
                .Take(threshold.MaxPerCollection)
                .Select(r => new LiveProfileRequest(
                    Convert.ToInt16(r["session_id"]),
                    (DateTime)r["start_time_utc"],
                    (int)r["statement_start_offset"],
                    (int)r["statement_end_offset"],
                    (byte[])r["plan_handle"],
                    (byte[])r["query_plan_hash"],
                    (byte[])r["sql_handle"]))
                .ToList();

        /// <summary>
        /// The longest the current statement can have been running, in ms.  The thresholds are on the request, which can
        /// span many statements: a batch that has used minutes of CPU can be a few ms into a statement that isn't the
        /// expensive one.  Outside an explicit transaction each statement runs in its own, so the session's oldest
        /// transaction began no earlier than the statement.  Inside one it began earlier and this is only an upper bound,
        /// which lets the request through as before.  Without a transaction begin time (not collected, or no transaction)
        /// there is nothing to go on and nothing is held back.
        /// </summary>
        private static double StatementRunningFor(DataRow r) =>
            r.Table.Columns.Contains("transaction_begin_time_utc") && r["transaction_begin_time_utc"] != DBNull.Value
                ? ((DateTime)r["SnapshotDateUTC"]).Subtract((DateTime)r["transaction_begin_time_utc"]).TotalMilliseconds
                : double.MaxValue;

        /// <summary>
        /// The most CPU the current statement can have used, in ms - <see cref="StatementRunningFor"/> on each of its
        /// threads.  Without the DOP a parallel statement could have used any amount, so nothing is held back.
        /// </summary>
        private static double StatementCouldHaveUsed(DataRow r)
        {
            var runningFor = StatementRunningFor(r);
            if (runningFor == double.MaxValue) return runningFor;
            if (!r.Table.Columns.Contains("dop") || r["dop"] == DBNull.Value) return double.MaxValue;
            return runningFor * Math.Max(Convert.ToInt32(r["dop"]), 1);
        }

        /// <summary>
        /// The table the import passes to dbo.RunningQueryLiveProfiles_Upd.  A table-valued parameter binds by ordinal, so
        /// the columns are in the order of the dbo.RunningQueryLiveProfiles type.
        /// </summary>
        public static DataTable NewTable()
        {
            var dt = new DataTable(TableName);
            dt.Columns.Add("session_id", typeof(short));
            dt.Columns.Add("start_time_utc", typeof(DateTime));
            dt.Columns.Add("statement_start_offset", typeof(int));
            dt.Columns.Add("statement_end_offset", typeof(int));
            dt.Columns.Add("plan_handle", typeof(byte[]));
            dt.Columns.Add("query_plan_hash", typeof(byte[]));
            dt.Columns.Add("sql_handle", typeof(byte[]));
            dt.Columns.Add("captured_utc", typeof(DateTime));
            dt.Columns.Add("cpu_time", typeof(int));
            dt.Columns.Add("total_elapsed_time", typeof(int));
            dt.Columns.Add("counters", typeof(byte[]));
            return dt;
        }

        /// <summary>
        /// The requests as the SQL takes them - see SQL\SQLLiveProfiles.sql.  XML rather than a list built into the
        /// statement, so the SQL is fixed, and rather than JSON, which needs a database compatibility level of 130.
        /// </summary>
        private static string RequestsXml(IEnumerable<LiveProfileRequest> requests) =>
            new XElement("requests", requests.Select(r => new XElement("request",
                new XAttribute("session_id", r.SessionID),
                new XAttribute("start_time_utc", r.StartTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)),
                new XAttribute("statement_start_offset", r.StartOffset),
                new XAttribute("statement_end_offset", r.EndOffset),
                new XAttribute("query_plan_hash", r.PlanHash.ToHexString()),
                new XAttribute("sql_handle", r.SqlHandle.ToHexString())))).ToString(SaveOptions.DisableFormatting);
    }
}
