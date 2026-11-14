using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.DailyReport
{
    /// <summary>
    /// An issue in a report, identified the same way on every run so runs can be compared.  Status: lower is worse - 1 critical, 2 warning
    /// for checks; 10 Critical, 11 High, 12 Medium, 13 Low for alerts; null for performance findings
    /// </summary>
    public record ReportIssue(string Key, string Description, int? Status = null);

    public record ResolvedIssue(string Key, string Description, DateTime FirstSeenUtc, int? Status = null);

    /// <summary>
    /// How this run's issues compare with the last scheduled run's (dbo.DailyReportIssueState): which are new, how long the rest have been
    /// there, and which have gone.  On the first run nothing is called new - there's nothing to compare with.
    /// </summary>
    public class IssueHistory
    {
        public const int MaxKeyLength = 400;

        public DateTime RunUtc { get; init; }
        public bool IsFirstRun { get; init; } = true;
        public DateTime? PreviousRunUtc { get; init; }
        public Dictionary<string, DateTime> FirstSeen { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Each ongoing issue's status at the last run (see <see cref="ReportIssue"/>), where it had one</summary>
        public Dictionary<string, int> PreviousStatus { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<ResolvedIssue> Resolved { get; init; } = new();

        /// <summary>No history: nothing is marked new, nothing resolved.  Used when the history can't be read</summary>
        public static IssueHistory None(DateTime runUtc) => new() { RunUtc = runUtc, IsFirstRun = true };

        public bool IsNew(string key) => !IsFirstRun && !FirstSeen.ContainsKey(Trim(key));

        /// <summary>An ongoing issue that's worse than at the last run: a warning that's critical, an alert that escalated</summary>
        public bool IsWorse(string key, int? status) => Comparable(key, status, out var previous) && status!.Value < previous;

        /// <summary>An ongoing issue that's better than at the last run</summary>
        public bool IsBetter(string key, int? status) => Comparable(key, status, out var previous) && status!.Value > previous;

        /// <summary>Both statuses are known and on the same scale (checks 1-2, alerts 10 and up)</summary>
        private bool Comparable(string key, int? status, out int previous)
        {
            previous = 0;
            return status.HasValue && !IsFirstRun && PreviousStatus.TryGetValue(Trim(key), out previous) && (status.Value >= 10) == (previous >= 10);
        }

        /// <summary>Whole days the issue has been in the report, or null when it's new or there's no history</summary>
        public int? DaysOngoing(string key) =>
            !IsFirstRun && FirstSeen.TryGetValue(Trim(key), out var first) ? Math.Max(0, (int)Math.Floor((RunUtc - first).TotalDays + 0.01)) : null;

        /// <summary>"new", "for 3 days" - or null when there's nothing to say</summary>
        public string AgeText(string key)
        {
            if (IsFirstRun) return null;
            if (IsNew(key)) return "new";
            var days = DaysOngoing(key);
            return days is >= 1 ? (days == 1 ? "for 1 day" : $"for {days} days") : null;
        }

        public static string Trim(string key) => key.Length <= MaxKeyLength ? key : key.Substring(0, MaxKeyLength);

        /// <summary>
        /// A check can be flagged on an instance with no object detail behind it (an instance-only key, e.g. while a collection is part way
        /// through), then on its objects - or the other way round.  That's the same problem described differently, not one resolved and one
        /// new, so for the same check and instance: a new instance-only key where there were object keys (or new object keys where there was
        /// the instance-only key) is ongoing from when the earliest of them was first seen, and the keys it replaced aren't resolved.
        /// A new object next to existing ones - a second drive filling up - is still new.
        /// </summary>
        public void ReconcileObjectDetail(IEnumerable<string> currentKeys)
        {
            if (IsFirstRun) return;
            var current = currentKeys.Select(Trim).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var previous = FirstSeen.Select(f => (Key: f.Key, First: f.Value, Status: PreviousStatus.TryGetValue(f.Key, out var s) ? s : (int?)null))
                .Concat(Resolved.Select(r => (Key: Trim(r.Key), First: r.FirstSeenUtc, r.Status)))
                .ToList();

            var previousByFamily = previous.Select(p => (p.Key, p.First, p.Status, Part: ObjectFamily(p.Key))).Where(p => p.Part != null)
                .GroupBy(p => p.Part.Value.Family, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (InstanceOnly: g.Any(p => p.Part.Value.InstanceOnly), Objects: g.Any(p => !p.Part.Value.InstanceOnly), First: g.Min(p => p.First), Status: g.Min(p => p.Status)), StringComparer.OrdinalIgnoreCase);
            var currentByFamily = current.Select(k => ObjectFamily(k)).Where(p => p != null)
                .GroupBy(p => p.Value.Family, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (InstanceOnly: g.Any(p => p.Value.InstanceOnly), Objects: g.Any(p => !p.Value.InstanceOnly)), StringComparer.OrdinalIgnoreCase);

            foreach (var key in current.Where(k => !FirstSeen.ContainsKey(k)))
            {
                var part = ObjectFamily(key);
                if (part == null || !previousByFamily.TryGetValue(part.Value.Family, out var before)) continue;
                if (part.Value.InstanceOnly ? before.Objects : before.InstanceOnly)
                {
                    FirstSeen[key] = before.First;
                    // The worst it was, so a warning described on the instance that's now critical on an object still shows as worse
                    if (before.Status.HasValue) PreviousStatus[key] = before.Status.Value;
                }
            }

            Resolved.RemoveAll(r =>
            {
                var part = ObjectFamily(Trim(r.Key));
                return part != null && currentByFamily.TryGetValue(part.Value.Family, out var now) && (part.Value.InstanceOnly ? now.Objects : now.InstanceOnly);
            });
        }

        /// <summary>For an object key (obj|check|instance|object): the check and instance, and whether it's the instance-only key.  Null for other keys</summary>
        private static (string Family, bool InstanceOnly)? ObjectFamily(string key)
        {
            // obj|check|instance|object - the object (a job name, say) can itself contain |, so it's after the third one
            if (!key.StartsWith("obj|", StringComparison.Ordinal)) return null;
            var checkEnd = key.IndexOf('|', 4);
            var instanceEnd = checkEnd < 0 ? -1 : key.IndexOf('|', checkEnd + 1);
            if (instanceEnd < 0) return null;
            return (key[..(instanceEnd + 1)], instanceEnd == key.Length - 1);
        }

        // Keys.  Built from names rather than IDs, as that's what the report shows - a renamed instance is a new issue
        public static string ObjectKey(string group, string instance, string objectName) => Trim($"obj|{group}|{instance}|{objectName}");
        public static string CheckKey(string column, string instance) => Trim($"chk|{column}|{instance}");
        public static string PerformanceKey(string metric, string instance) => Trim($"perf|{metric}|{instance}");
        public static string AlertKey(string instance, string alertKey) => Trim($"alert|{instance}|{alertKey}");
    }

    public static class IssueHistoryRepository
    {
        /// <summary>
        /// Compares the issues with the last recorded run, and with update, records them as the last run.  Update is for a scheduled run
        /// once it's been sent; a preview or Send now only compares.  An update records an issue that's now described differently
        /// (<see cref="IssueHistory.ReconcileObjectDetail"/>) with its original first seen time, so it keeps its age on later runs too.
        /// keepKeyPrefix: keys an update leaves in place although they're not in issues - their part of the report couldn't be read (e.g. "alert|").
        /// </summary>
        public static IssueHistory Compare(string connectionString, int dailyReportID, IEnumerable<ReportIssue> issues, DateTime runUtc, bool update, string keepKeyPrefix = null)
        {
            var issueList = issues.ToList();
            var reconciled = update ? Compare(connectionString, dailyReportID, issueList, runUtc, update: false) : null;

            var table = new DataTable();
            table.Columns.Add("IssueKey", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Status", typeof(byte));
            table.Columns.Add("FirstSeenUTC", typeof(DateTime));
            foreach (var issue in issueList.GroupBy(i => IssueHistory.Trim(i.Key), StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                var description = issue.Description ?? string.Empty;
                var key = IssueHistory.Trim(issue.Key);
                object firstSeenUtc = reconciled != null && reconciled.FirstSeen.TryGetValue(key, out var first) ? first : DBNull.Value;
                table.Rows.Add(key, description.Length > 400 ? description[..400] : description, issue.Status.HasValue ? (object)(byte)issue.Status.Value : DBNull.Value, firstSeenUtc);
            }

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_IssueState_Upd", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            cmd.Parameters.AddWithValue("@DailyReportID", dailyReportID);
            cmd.Parameters.Add(new SqlParameter("@Issues", SqlDbType.Structured) { TypeName = "dbo.DailyReportIssues", Value = table });
            cmd.Parameters.Add(new SqlParameter("@RunUTC", SqlDbType.DateTime2) { Value = runUtc });
            cmd.Parameters.AddWithValue("@Update", update);
            cmd.Parameters.AddWithValue("@KeepKeyPrefix", (object)keepKeyPrefix ?? DBNull.Value);
            cn.Open();
            using var r = cmd.ExecuteReader();

            r.Read();
            var isFirstRun = r.GetBoolean(0);
            DateTime? previous = r.IsDBNull(1) ? null : r.GetDateTime(1);

            var firstSeen = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var previousStatus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            r.NextResult();
            while (r.Read())
            {
                firstSeen[r.GetString(0)] = r.GetDateTime(1);
                if (!r.IsDBNull(2)) previousStatus[r.GetString(0)] = Convert.ToInt32(r.GetValue(2));
            }

            var resolved = new List<ResolvedIssue>();
            r.NextResult();
            while (r.Read())
            {
                resolved.Add(new ResolvedIssue(r.GetString(0), r.IsDBNull(1) ? r.GetString(0) : r.GetString(1), r.GetDateTime(2), r.IsDBNull(3) ? null : Convert.ToInt32(r.GetValue(3))));
            }

            var history = new IssueHistory { RunUtc = runUtc, IsFirstRun = isFirstRun, PreviousRunUtc = previous, FirstSeen = firstSeen, PreviousStatus = previousStatus, Resolved = resolved };
            history.ReconcileObjectDetail(table.Rows.Cast<DataRow>().Select(row => (string)row["IssueKey"]));
            return history;
        }
    }
}
