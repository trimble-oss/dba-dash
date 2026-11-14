using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash.DailyReport
{
    /// <summary>A long running request from the running query snapshots (dbo.DailyReport_Activity_Get) - DBCC, a backup, an index rebuild...</summary>
    public class DailyReportActivity
    {
        public int InstanceID { get; init; }
        public DateTime StartUtc { get; init; }
        public DateTime LastSeenUtc { get; init; }
        public int DurationSec { get; init; }
        public string Command { get; init; }
        public string DatabaseName { get; init; }
        public string JobName { get; init; }
        public int? StepID { get; init; }
        public string ProgramName { get; init; }
        public long CPUMs { get; init; }
        public long Reads { get; init; }
        public long Writes { get; init; }
        public long LogicalReads { get; init; }
        /// <summary>The statement, cut short.  Not sent to the AI - it can hold literal values</summary>
        public string StatementText { get; init; }
        /// <summary>When it started, as a local time for the report ("01:00") - set by <see cref="DailyReportActivityAnalyzer.Apply"/></summary>
        public string StartText { get; set; }
    }

    public static class DailyReportActivityRepository
    {
        /// <param name="includedHours">The hours the report counts (PerformanceData.IncludedHours) - only requests seen in them, from fromUtc.  Null: all</param>
        public static List<DailyReportActivity> Get(string connectionString, IEnumerable<int> instanceIDs, DateTime fromUtc, DateTime toUtc,
            HashSet<(int Period, int HourOffset)> includedHours = null)
        {
            var list = new List<DailyReportActivity>();
            var ids = string.Join(",", instanceIDs);
            if (ids.Length == 0) return list;
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Activity_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
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
                list.Add(new DailyReportActivity
                {
                    InstanceID = Convert.ToInt32(r["InstanceID"]),
                    StartUtc = Convert.ToDateTime(r["StartUTC"], CultureInfo.InvariantCulture),
                    LastSeenUtc = Convert.ToDateTime(r["LastSeenUTC"], CultureInfo.InvariantCulture),
                    DurationSec = Convert.ToInt32(r["DurationSec"]),
                    Command = S("Command"),
                    DatabaseName = S("DatabaseName"),
                    JobName = S("JobName"),
                    StepID = r["StepID"] == DBNull.Value ? null : Convert.ToInt32(r["StepID"]),
                    ProgramName = S("ProgramName"),
                    CPUMs = L("CPUMs"),
                    Reads = L("Reads"),
                    Writes = L("Writes"),
                    LogicalReads = L("LogicalReads"),
                    StatementText = S("StatementText")
                });
            }
            return list;
        }
    }

    /// <summary>
    /// Picks the long running requests that fit each listed instance's findings - the most writes for write latency, reads for read latency,
    /// CPU for CPU - and describes them.  Pure logic, so it can be unit tested.
    /// </summary>
    public static class DailyReportActivityAnalyzer
    {
        public const int PerInstance = 3;

        public static void Apply(PerformanceAnalysis analysis, IEnumerable<DailyReportActivity> activity, Func<DateTime, string> formatTime)
        {
            var byInstance = activity.GroupBy(a => a.InstanceID).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var result in analysis.Flagged)
            {
                if (!byInstance.TryGetValue(result.InstanceID, out var list)) continue;
                var (measure, value) = MeasureFor(result);
                foreach (var a in list.Where(a => value(a) > 0 && InCountedHours(analysis, a)).OrderByDescending(value).ThenByDescending(a => a.DurationSec).Take(PerInstance))
                {
                    a.StartText = formatTime(a.StartUtc);
                    result.Activity.Add(a);
                }
                result.ActivityMeasure = measure;
            }
        }

        /// <summary>Whether a request ran during any of the hours the report counts - all of them unless it counts only certain hours of the day</summary>
        public static bool InCountedHours(PerformanceAnalysis analysis, DailyReportActivity a)
        {
            if (analysis.IncludedHours == null) return true;
            var last = a.LastSeenUtc < a.StartUtc ? a.StartUtc : a.LastSeenUtc;
            return analysis.IncludedHours.Where(h => h.Period == 0).Any(h =>
            {
                var from = analysis.WindowStartUtc.AddHours(h.HourOffset);
                return a.StartUtc < from.AddHours(1) && last >= from;
            });
        }

        /// <summary>What the instance's activity is ranked by: its findings' resource, otherwise how long each request ran</summary>
        public static (string Measure, Func<DailyReportActivity, double> Value) MeasureFor(InstancePerformanceResult result)
        {
            var metrics = result.Findings.Select(f => f.Metric).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (metrics.Contains("WriteLatency")) return ("writes", a => a.Writes);
            if (metrics.Contains("ReadLatency")) return ("reads", a => a.Reads);
            if (metrics.Contains("CPU") || metrics.Contains("HighCPU")) return ("CPU", a => a.CPUMs);
            return ("duration", a => a.DurationSec);
        }

        /// <summary>
        /// "DBCC CHECKDB([Sales])" - the statement on one line, or the command when there isn't one.  For the AI it's always the command:
        /// statement text can hold literal values
        /// </summary>
        public static string Title(DailyReportActivity a, bool forAi)
        {
            var command = string.IsNullOrWhiteSpace(a.Command) ? "Request" : a.Command.Trim();
            if (forAi || string.IsNullOrWhiteSpace(a.StatementText)) return command;
            var oneLine = Regex.Replace(a.StatementText.Trim(), @"\s+", " ");
            return oneLine.Length <= 90 ? oneLine : oneLine[..87] + "...";
        }

        /// <summary>"Sales - Agent job DatabaseIntegrityCheck (step 1) - started 01:00, ran 42m - 1.2M writes, 3.4M reads, CPU 8m"</summary>
        public static string Describe(DailyReportActivity a)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(a.DatabaseName)) parts.Add(a.DatabaseName);
            if (!string.IsNullOrWhiteSpace(a.JobName)) parts.Add($"Agent job {a.JobName}{(a.StepID.HasValue ? $" (step {a.StepID})" : string.Empty)}");
            else if (!string.IsNullOrWhiteSpace(a.ProgramName)) parts.Add(a.ProgramName.Length <= 40 ? a.ProgramName : a.ProgramName[..37] + "...");
            parts.Add($"started {a.StartText ?? a.StartUtc.ToString("HH:mm", CultureInfo.InvariantCulture)}, ran {PerformanceAnalyzer.Duration(a.DurationSec / 60.0)}");
            var figures = new List<string>();
            if (a.Writes > 0) figures.Add($"{TopQueryAnalyzer.Compact(a.Writes)} writes");
            if (a.Reads > 0) figures.Add($"{TopQueryAnalyzer.Compact(a.Reads)} reads");
            if (a.CPUMs > 0) figures.Add($"CPU {TopQueryAnalyzer.CPUTime(a.CPUMs)}");
            if (figures.Count > 0) parts.Add(string.Join(", ", figures));
            return string.Join(" - ", parts);
        }
    }
}
