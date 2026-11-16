using DBADash.QueryStats;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash.DailyReport
{
    /// <summary>A lead blocker from the blocking snapshots (dbo.DailyReport_Blockers_Get): what was at the head of the blocking chains</summary>
    public class DailyReportBlocker
    {
        public int InstanceID { get; init; }
        public string DatabaseName { get; init; }
        /// <summary>schema.name of the procedure, function or trigger the statement belongs to, when known</summary>
        public string ObjectName { get; init; }
        public string QueryHash { get; init; }
        public string ProgramName { get; init; }
        /// <summary>The statement as captured.  It can hold literal values, so it's only ever shown masked - see <see cref="DailyReportBlockerAnalyzer.Title"/></summary>
        public string StatementText { get; init; }
        /// <summary>Blocking snapshots it was a lead blocker in</summary>
        public int Snapshots { get; init; }
        /// <summary>Of those, the snapshots it was sleeping in: idle, holding locks in an open transaction</summary>
        public int SleepingSnapshots { get; init; }
        /// <summary>The most sessions blocked under it (directly or further down the chain) in one snapshot</summary>
        public int MaxBlockedSessions { get; init; }
        /// <summary>The most blocked wait under it (the waits of every session blocked under it, added up) in one snapshot</summary>
        public long MaxBlockedWaitMs { get; init; }
        public DateTime FirstSeenUtc { get; init; }
        public DateTime LastSeenUtc { get; init; }
        /// <summary>When it was first and last seen blocking, as local times for the report ("09:10-09:42") - set by <see cref="DailyReportBlockerAnalyzer.Apply"/></summary>
        public string SeenText { get; set; }
    }

    public static class DailyReportBlockerRepository
    {
        /// <param name="includedHours">The hours the report counts (PerformanceData.IncludedHours) - only snapshots in them, from fromUtc.  Null: all</param>
        public static List<DailyReportBlocker> Get(string connectionString, IEnumerable<int> instanceIDs, DateTime fromUtc, DateTime toUtc,
            HashSet<(int Period, int HourOffset)> includedHours = null)
        {
            var list = new List<DailyReportBlocker>();
            var ids = string.Join(",", instanceIDs);
            if (ids.Length == 0) return list;
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Blockers_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cmd.Parameters.AddWithValue("@InstanceIDs", ids);
            cmd.Parameters.Add(new SqlParameter("@FromUTC", SqlDbType.DateTime2) { Value = fromUtc });
            cmd.Parameters.Add(new SqlParameter("@ToUTC", SqlDbType.DateTime2) { Value = toUtc });
            cmd.Parameters.Add(new SqlParameter("@Hours", SqlDbType.VarChar, -1) { Value = (object)PerformanceData.HoursParameter(includedHours?.Where(h => h.Period == 0).ToHashSet()) ?? DBNull.Value });
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string S(string c) => r[c] == DBNull.Value ? null : Convert.ToString(r[c], CultureInfo.InvariantCulture);
                long L(string c) => r[c] == DBNull.Value ? 0 : Convert.ToInt64(r[c], CultureInfo.InvariantCulture);
                list.Add(new DailyReportBlocker
                {
                    InstanceID = Convert.ToInt32(r["InstanceID"]),
                    DatabaseName = S("DatabaseName"),
                    ObjectName = S("ObjectName"),
                    QueryHash = S("QueryHash"),
                    ProgramName = S("ProgramName"),
                    StatementText = S("StatementText"),
                    Snapshots = (int)L("Snapshots"),
                    SleepingSnapshots = (int)L("SleepingSnapshots"),
                    MaxBlockedSessions = (int)L("MaxBlockedSessions"),
                    MaxBlockedWaitMs = L("MaxBlockedWaitMs"),
                    FirstSeenUtc = Convert.ToDateTime(r["FirstSeenUTC"], CultureInfo.InvariantCulture),
                    LastSeenUtc = Convert.ToDateTime(r["LastSeenUTC"], CultureInfo.InvariantCulture)
                });
            }
            return list;
        }
    }

    /// <summary>
    /// The lead blockers behind an instance's blocking findings - the queries using the most CPU don't explain blocking, as a session can
    /// hold locks while using next to none.  Statement text is shown with its literal values masked (<see cref="QueryTemplate"/>), and
    /// never to the AI.  Pure logic, so it can be unit tested.
    /// </summary>
    public static class DailyReportBlockerAnalyzer
    {
        /// <summary>Findings the lead blockers explain</summary>
        private static readonly HashSet<string> BlockingMetrics = new(StringComparer.OrdinalIgnoreCase) { "Blocking", "LockWaits" };

        public static bool IsNeeded(InstancePerformanceResult result) => result.Findings.Any(f => BlockingMetrics.Contains(f.Metric));

        public static void Apply(PerformanceAnalysis analysis, IEnumerable<DailyReportBlocker> blockers, int count, Func<DateTime, string> formatTime)
        {
            var byInstance = blockers.GroupBy(b => b.InstanceID).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var result in analysis.Flagged.Where(IsNeeded))
            {
                if (!byInstance.TryGetValue(result.InstanceID, out var list)) continue;
                foreach (var b in list.OrderByDescending(b => b.Snapshots).ThenByDescending(b => b.MaxBlockedSessions).ThenByDescending(b => b.MaxBlockedWaitMs).Take(count))
                {
                    var first = formatTime(b.FirstSeenUtc);
                    var last = formatTime(b.LastSeenUtc);
                    b.SeenText = first == last ? first : $"{first}-{last}";
                    result.Blockers.Add(b);
                }
            }
        }

        /// <summary>
        /// The procedure's name, or the statement on one line with its literal values masked, or what's known without text.  For the AI it's
        /// never the statement, masked or not: "ad hoc query 0x... in Sales"
        /// </summary>
        public static string Title(DailyReportBlocker b, bool forAi)
        {
            var database = string.IsNullOrWhiteSpace(b.DatabaseName) ? null : b.DatabaseName;
            if (!string.IsNullOrWhiteSpace(b.ObjectName)) return database == null ? b.ObjectName : $"{database}.{b.ObjectName}";
            var inDatabase = database == null ? string.Empty : $" in {database}";
            if (!forAi && !string.IsNullOrWhiteSpace(b.StatementText))
            {
                var masked = Regex.Replace(QueryTemplate.Create(b.StatementText), @"\s+", " ").Trim();
                if (masked.Length > 0) return (masked.Length <= 90 ? masked : masked[..87] + "...") + inDatabase;
            }
            if (!string.IsNullOrWhiteSpace(b.QueryHash)) return $"ad hoc query {b.QueryHash}{inDatabase}";
            return $"{(string.IsNullOrWhiteSpace(b.StatementText) ? "session with no statement captured" : "statement")}{inDatabase}";
        }

        /// <summary>
        /// "lead blocker in 14 snapshots (09:10-09:42), up to 23 sessions and 4m blocked wait in one snapshot - sleeping with an open
        /// transaction in 9 - App".  The same terms as the blocking finding's context
        /// </summary>
        public static string Describe(DailyReportBlocker b)
        {
            var parts = new List<string>
            {
                $"lead blocker in {Plural(b.Snapshots, "snapshot")}{(b.SeenText == null ? string.Empty : $" ({b.SeenText})")}" +
                $", up to {Plural(b.MaxBlockedSessions, "session")}" +
                (b.MaxBlockedWaitMs > 0 ? $" and {PerformanceAnalyzer.DurationMs(b.MaxBlockedWaitMs)} blocked wait" : " blocked") + " in one snapshot"
            };
            if (b.SleepingSnapshots > 0)
            {
                parts.Add(b.SleepingSnapshots == b.Snapshots
                    ? "sleeping with an open transaction"
                    : $"sleeping with an open transaction in {b.SleepingSnapshots}");
            }
            if (!string.IsNullOrWhiteSpace(b.ProgramName)) parts.Add(b.ProgramName.Length <= 40 ? b.ProgramName : b.ProgramName[..37] + "...");
            return string.Join(" - ", parts);
        }

        private static string Plural(int n, string word) => n == 1 ? $"1 {word}" : $"{n.ToString("#,0", CultureInfo.InvariantCulture)} {word}s";
    }
}
