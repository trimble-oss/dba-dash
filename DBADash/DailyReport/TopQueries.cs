using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DBADash.DailyReport
{
    /// <summary>One period's figures for a procedure or ad hoc query, from dbo.DailyReport_TopQueries_Get</summary>
    public class TopQueryRow
    {
        /// <summary>"Object" (procedure, function or trigger) or "Query" (ad hoc statement)</summary>
        public string Source { get; init; }
        public int InstanceID { get; init; }
        public long ID { get; init; }
        public int Period { get; init; }
        public string DatabaseName { get; init; }
        /// <summary>schema.name for an object, the statement's short label for a query.  A query label can hold literal values</summary>
        public string Name { get; init; }
        public string ObjectType { get; init; }
        public string QueryHash { get; init; }
        public double CPUMs { get; init; }
        public long LogicalReads { get; init; }
        public long PhysicalReads { get; init; }
        public long Executions { get; init; }
    }

    /// <summary>A procedure or query listed under an instance's performance findings</summary>
    public class TopQuery
    {
        public string Source { get; init; }
        public string DatabaseName { get; init; }
        public string Name { get; init; }
        public string QueryHash { get; init; }
        public double Current { get; init; }
        public double? Usual { get; init; }
        public long Executions { get; init; }
        public double? UsualExecutions { get; init; }
        public bool IsQuery => Source == "Query";

        /// <summary>How it's named in the email: "Sales.dbo.usp_Orders", or "SELECT ... in Sales" for an ad hoc query</summary>
        public string DisplayName => IsQuery
            ? $"{(string.IsNullOrWhiteSpace(Name) ? "Ad hoc query " + QueryHash : Name)} in {DatabaseName ?? "?"}"
            : $"{(DatabaseName == null ? string.Empty : DatabaseName + ".")}{Name}";

        /// <summary>How it's named to the AI service: no statement text, which can hold literal values</summary>
        public string MetadataName => IsQuery ? $"ad hoc query {QueryHash} in {DatabaseName ?? "?"}" : DisplayName;
    }

    /// <summary>
    /// Picks the procedures and ad hoc queries behind an instance's performance findings: ranked by how much more CPU (or logical reads,
    /// for IO findings) they used than the median of the baseline weeks.  Without a baseline, ranked by their total.
    /// </summary>
    public static class TopQueryAnalyzer
    {
        public const string MeasureCPU = "CPU";
        public const string MeasureReads = "Logical reads";

        private static readonly HashSet<string> IOMetrics = new(StringComparer.OrdinalIgnoreCase) { "ReadLatency", "WriteLatency" };

        /// <summary>Logical reads for an instance whose findings are about IO and not CPU, otherwise CPU</summary>
        public static string MeasureFor(InstancePerformanceResult result)
        {
            var metrics = result.Findings.Select(f => f.Metric).ToList();
            return metrics.Any(IOMetrics.Contains) && !metrics.Any(m => m is "CPU" or "HighCPU") ? MeasureReads : MeasureCPU;
        }

        public static void Apply(PerformanceAnalysis analysis, IEnumerable<TopQueryRow> rows, int count)
        {
            var byInstance = rows.GroupBy(r => r.InstanceID).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var result in analysis.Flagged)
            {
                if (!byInstance.TryGetValue(result.InstanceID, out var instanceRows)) continue;
                var measure = MeasureFor(result);
                var byIncrease = analysis.ComparedWithBaseline && result.BaselineWeeks > 0;
                result.TopQueriesMeasure = measure;
                result.TopQueriesByIncrease = byIncrease;
                // The weeks the instance has any performance data for - a week where none of today's candidates ran still counts, as zero
                var coverage = result.Data?.Hours.Where(h => h.Period > 0).Select(h => h.Period).Distinct().ToList();
                result.TopQueries.AddRange(Rank(instanceRows, measure, byIncrease, count, coverage));
            }
        }

        public static List<TopQuery> Rank(List<TopQueryRow> rows, string measure, bool byIncrease, int count, IReadOnlyCollection<int> baselineCoverage = null)
        {
            double Value(TopQueryRow r) => measure == MeasureReads ? r.LogicalReads : r.CPUMs;
            // Baseline weeks with figures for the instance: a candidate missing from one of them used nothing that week.  The rows are only
            // today's top candidates, so a week none of them ran in has no rows - the coverage from the instance's hourly data says it was there
            var baselinePeriods = rows.Where(r => r.Period > 0).Select(r => r.Period).Concat(baselineCoverage ?? Array.Empty<int>()).Distinct().ToList();

            var candidates = rows.GroupBy(r => (r.Source, r.ID)).Select(g =>
            {
                var current = g.FirstOrDefault(r => r.Period == 0);
                var any = current ?? g.First();
                double? usual = null, usualExecutions = null;
                if (baselinePeriods.Count > 0)
                {
                    usual = PerformanceAnalyzer.Median(baselinePeriods.Select(p => (double?)(g.Where(r => r.Period == p).Sum(Value))));
                    usualExecutions = PerformanceAnalyzer.Median(baselinePeriods.Select(p => (double?)g.Where(r => r.Period == p).Sum(r => r.Executions)));
                }
                return new TopQuery
                {
                    Source = any.Source,
                    DatabaseName = any.DatabaseName,
                    Name = any.Name,
                    QueryHash = any.QueryHash,
                    Current = current == null ? 0 : Value(current),
                    Usual = usual,
                    Executions = current?.Executions ?? 0,
                    UsualExecutions = usualExecutions
                };
            }).Where(q => q.Current > 0);

            return byIncrease
                ? candidates.Where(q => q.Current - (q.Usual ?? 0) > 0).OrderByDescending(q => q.Current - (q.Usual ?? 0)).Take(count).ToList()
                : candidates.OrderByDescending(q => q.Current).Take(count).ToList();
        }

        /// <summary>"CPU 1h 10m (usual 5m), 12,040 executions (usual 11,900)"</summary>
        public static string Describe(TopQuery q, string measure)
        {
            string Fmt(double v) => measure == MeasureReads ? Compact(v) + " reads" : CPUTime(v);
            var text = measure == MeasureReads ? $"{Fmt(q.Current)}" : $"CPU {Fmt(q.Current)}";
            // None in the baseline weeks: it's new, which says more than "usual 0ms"
            if (q.Usual is > 0) text += $" (usual {(measure == MeasureReads ? Compact(q.Usual.Value) : CPUTime(q.Usual.Value))})";
            else if (q.Usual.HasValue) text += " (new)";
            text += $", {q.Executions.ToString("#,0", CultureInfo.InvariantCulture)} executions";
            if (q.UsualExecutions is > 0) text += $" (usual {q.UsualExecutions.Value.ToString("#,0", CultureInfo.InvariantCulture)})";
            return text;
        }

        /// <summary>CPU ms as "850ms", "42s", "1h 10m"</summary>
        public static string CPUTime(double ms) => ms < 1000 ? $"{Math.Round(ms)}ms" : ms < 60000 ? $"{Math.Round(ms / 1000)}s" : PerformanceAnalyzer.Duration(ms / 60000);

        /// <summary>1,234 / 45.6K / 1.2M / 3.4B</summary>
        public static string Compact(double v) =>
            v >= 1e9 ? (v / 1e9).ToString("0.#", CultureInfo.InvariantCulture) + "B"
            : v >= 1e6 ? (v / 1e6).ToString("0.#", CultureInfo.InvariantCulture) + "M"
            : v >= 1e4 ? (v / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + "K"
            : v.ToString("#,0", CultureInfo.InvariantCulture);
    }

    public static class TopQueryRepository
    {
        public static List<TopQueryRow> Get(string connectionString, IEnumerable<int> instanceIDs, DateTime windowEndUtc, int baselineWeeks,
            HashSet<(int Period, int HourOffset)> includedHours = null)
        {
            var rows = new List<TopQueryRow>();
            var ids = string.Join(",", instanceIDs.Distinct());
            if (ids.Length == 0) return rows;

            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_TopQueries_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cmd.Parameters.AddWithValue("@InstanceIDs", ids);
            cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = windowEndUtc });
            cmd.Parameters.AddWithValue("@BaselineWeeks", (byte)baselineWeeks);
            cmd.Parameters.Add(new SqlParameter("@Hours", SqlDbType.VarChar, -1) { Value = (object)PerformanceData.HoursParameter(includedHours) ?? DBNull.Value });
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                rows.Add(new TopQueryRow
                {
                    Source = r.GetString(r.GetOrdinal("Source")),
                    InstanceID = r.GetInt32(r.GetOrdinal("InstanceID")),
                    ID = r.GetInt64(r.GetOrdinal("ID")),
                    Period = Convert.ToInt32(r["Period"]),
                    DatabaseName = r["DatabaseName"] as string,
                    Name = r["Name"] as string,
                    ObjectType = r["ObjectType"] as string,
                    QueryHash = r["QueryHash"] as string,
                    CPUMs = Convert.ToDouble(r["CPUMs"], CultureInfo.InvariantCulture),
                    LogicalReads = Convert.ToInt64(r["LogicalReads"]),
                    PhysicalReads = Convert.ToInt64(r["PhysicalReads"]),
                    Executions = Convert.ToInt64(r["Executions"])
                });
            }
            return rows;
        }
    }
}
