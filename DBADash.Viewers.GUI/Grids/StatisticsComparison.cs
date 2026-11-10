using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Two or more runs' statistics side by side - the parts of a query split at PRINT 'DBADash-Compare', or queries on
    /// different tabs: their totals, each later run's change from the first, and each table's I/O in every run.
    /// </summary>
    public static class StatisticsComparison
    {
        private const string Arrow = " → ";
        private const string Separator = "   ·   ";
        private const string ChangeFormat = "+#,0;-#,0;0";
        private const string PercentFormat = "+#,0.0'%';-#,0.0'%';0'%'";

        /// <summary>
        /// The headline measures: what a run is compared on at a glance.  Null for a run that didn't print it - with
        /// STATISTICS TIME or IO off - rather than 0, which would compare as if it took nothing.
        /// </summary>
        private static readonly (string Name, Func<StatisticsReport, long?> Value, string Unit)[] Headline =
        [
            ("CPU", r => Time(r, r.CpuMs), " ms"),
            ("Elapsed", r => Time(r, r.ElapsedMs), " ms"),
            ("Logical reads", r => IO(r, StatisticsMessages.LogicalReads), ""),
            ("Physical reads", r => IO(r, StatisticsMessages.PhysicalReads), "")
        ];

        /// <summary><paramref name="value"/>, where the run printed STATISTICS TIME; null otherwise.</summary>
        private static long? Time(StatisticsReport report, long value) => report.HasTime ? value : null;

        /// <summary>The run's total of a counter, where it printed STATISTICS IO; null otherwise.</summary>
        private static long? IO(StatisticsReport report, string counter) => report.HasIO ? report.Counter(counter) : null;

        /// <summary>"(1) EXEC dbo.ProcV1" - the run's number, which the I/O grid's columns go by, and its label.</summary>
        public static string Name(IReadOnlyList<StatisticsRun> runs, int index) => $"({index + 1}) {runs[index].Label}";

        /// <summary>
        /// The card's lines: the first run against the last on the headline measures, which runs they are, and how far
        /// the comparison can be trusted.
        /// </summary>
        public static (string Headline, string Details, string Note, bool Warning) Summary(IReadOnlyList<StatisticsRun> runs)
        {
            // Only what every run printed: a run with STATISTICS TIME or IO off has nothing to set against the others'.
            var headline = Headline
                .Select(m => (m.Name, m.Unit, Values: runs.Select(r => m.Value(r.Report)).ToList()))
                .Where(m => m.Values.All(v => v != null))
                .Select(m => $"{m.Name} {string.Join(Arrow, m.Values.Select(v => $"{v:N0}"))}{m.Unit}" +
                             Percent(m.Values[0]!.Value, m.Values[^1]!.Value))
                .ToList();
            if (headline.Count == 0) headline.Add("Nothing in common to compare: the runs printed different statistics");

            var details = string.Join(Arrow, runs.Select((_, i) => Name(runs, i)));

            var notes = new List<string>
            {
                "Each run is read as a query of its own, so a run that's a single call has its own time as its total."
            };
            foreach (var (kind, has) in new (string, Func<StatisticsReport, bool>)[] { ("TIME", r => r.HasTime), ("IO", r => r.HasIO) })
            {
                var without = runs.Where(r => !has(r.Report)).Select(r => r.Label).ToList();
                if (without.Count == 0 || without.Count == runs.Count) continue;
                notes.Add($"{Join(without)} had STATISTICS {kind} off, so {(without.Count == 1 ? "its" : "their")} " +
                          $"{(kind == "TIME" ? "times are" : "I/O is")} left blank rather than compared.");
            }
            var ranges = runs.Where(r => StatisticsViewerControl.LikelyCountsTwice(r.Report)).Select(r => r.Label).ToList();
            if (ranges.Count > 0)
            {
                notes.Add($"{Join(ranges)} {(ranges.Count == 1 ? "makes" : "make")} several calls, so the times are " +
                          "everything added up, which counts some time twice - see the run's own tab for its range.");
            }
            var errors = runs.Where(r => r.Report.ErrorCount > 0).Select(r => r.Label).ToList();
            if (errors.Count > 0)
            {
                notes.Add($"{Join(errors)} had errors.");
            }
            notes.Add("Times vary from run to run: compare a few runs of each, and warm runs (no physical reads) with warm " +
                      "runs.  CPU is counted in ticks of about 15 ms.");

            return (string.Join(Separator, headline), details, string.Join("  ", notes), ranges.Count > 0 || errors.Count > 0);
        }

        private static string Join(List<string> labels) =>
            labels.Count == 1 ? labels[0] : string.Join(", ", labels.Take(labels.Count - 1)) + " and " + labels[^1];

        /// <summary>" (+26%)" - the change from <paramref name="from"/>, or nothing where there's nothing to compare.</summary>
        private static string Percent(long from, long to)
        {
            if (from == to) return " (no change)";
            if (from == 0) return "";
            var change = (to - from) * 100m / from;
            return $" ({change:+#,0.#;-#,0.#}%)";
        }

        /// <summary>
        /// The totals of every run, a measure a row - times, compile, each I/O counter, statements - with each later
        /// run's change from the first.
        /// </summary>
        public static DataTable Totals(IReadOnlyList<StatisticsRun> runs)
        {
            var table = NewTable("Totals");
            table.Columns.Add("Measure", typeof(string));
            for (var i = 0; i < runs.Count; i++) AddColumn(table, Name(runs, i), typeof(long), "N0");
            AddChangeColumns(table, runs, i => i == 1 && runs.Count == 2 ? "" : $" ({i + 1})");

            // Blank, not 0, for a run that didn't print the measure - STATISTICS TIME or IO off - and no change against it.
            var measures = new List<(string Name, Func<StatisticsReport, long?> Value)>();
            if (runs.Any(r => r.Report.HasTime))
            {
                measures.Add(("CPU ms", r => Time(r, r.CpuMs)));
                measures.Add(("Elapsed ms", r => Time(r, r.ElapsedMs)));
                measures.Add(("Compile CPU ms", r => Time(r, r.CompileCpuMs)));
                measures.Add(("Compile elapsed ms", r => Time(r, r.CompileElapsedMs)));
            }
            foreach (var counter in Counters(runs))
            {
                measures.Add((counter, r => IO(r, counter)));
            }
            measures.Add(("Statements", r => r.Statements.Count));

            foreach (var (name, value) in measures)
            {
                var row = table.NewRow();
                row["Measure"] = name;
                var values = runs.Select(r => value(r.Report)).ToList();
                for (var i = 0; i < runs.Count; i++) row[Name(runs, i)] = (object)values[i] ?? DBNull.Value;
                SetChanges(row, values, i => i == 1 && runs.Count == 2 ? "" : $" ({i + 1})");
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Each table's I/O in every run, the busiest first: a column a counter a run - "Logical reads (1)" - and with two
        /// runs, each counter's change.
        /// </summary>
        public static DataTable TableIO(IReadOnlyList<StatisticsRun> runs)
        {
            var counters = Counters(runs);
            var table = NewTable("Table I/O");
            table.Columns.Add("Table", typeof(string));
            foreach (var counter in counters)
            {
                for (var i = 0; i < runs.Count; i++) StatisticsBars.MoreIsWorse(AddColumn(table, $"{counter} ({i + 1})", typeof(long), "N0"));
                if (runs.Count == 2) StatisticsBars.Change(AddColumn(table, $"{counter} change", typeof(long), ChangeFormat));
            }
            if (runs.Count == 2 && counters.Contains(StatisticsMessages.LogicalReads))
            {
                StatisticsBars.Change(AddColumn(table, "Logical reads change %", typeof(decimal), PercentFormat));
            }

            var byRun = runs.Select(r => r.Report.Statements.SelectMany(s => s.Tables)
                    .GroupBy(t => t.Table, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal))
                .ToList();
            var tables = byRun.SelectMany(d => d.Keys).Distinct(StringComparer.Ordinal)
                .OrderByDescending(t => byRun.Max(d => d.TryGetValue(t, out var io) ? io.Sum(x => x.Counter(StatisticsMessages.LogicalReads)) : 0))
                .ThenBy(t => t, StringComparer.Ordinal);

            foreach (var name in tables)
            {
                var row = table.NewRow();
                row["Table"] = name;
                foreach (var counter in counters)
                {
                    // 0 for a table a run didn't read; blank for a run that printed no I/O at all.
                    var values = runs.Select((r, i) => r.Report.HasIO
                            ? byRun[i].TryGetValue(name, out var io) ? io.Sum(x => x.Counter(counter)) : 0
                            : (long?)null)
                        .ToList();
                    for (var i = 0; i < runs.Count; i++) row[$"{counter} ({i + 1})"] = (object)values[i] ?? DBNull.Value;
                    if (runs.Count != 2 || values[0] is not { } from || values[1] is not { } to) continue;
                    row[$"{counter} change"] = to - from;
                    if (counter == StatisticsMessages.LogicalReads && from != 0)
                    {
                        row["Logical reads change %"] = (to - from) * 100m / from;
                    }
                }
                table.Rows.Add(row);
            }

            // A counter's runs on one scale, so a run's bar is longer than another's only where its figure is.
            foreach (var counter in counters)
            {
                StatisticsBars.ShareScale(table, Enumerable.Range(1, runs.Count).Select(i => $"{counter} ({i})").ToList());
            }
            return table;
        }

        /// <summary>The I/O counters any run shows, in the order the first to show each has them.</summary>
        private static List<string> Counters(IReadOnlyList<StatisticsRun> runs) =>
            runs.SelectMany(r => StatisticsMessages.ShownCounters(r.Report)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        private static void AddChangeColumns(DataTable table, IReadOnlyList<StatisticsRun> runs, Func<int, string> suffix)
        {
            for (var i = 1; i < runs.Count; i++)
            {
                AddColumn(table, $"Change{suffix(i)}", typeof(long), ChangeFormat);
                StatisticsBars.Change(AddColumn(table, $"Change %{suffix(i)}", typeof(decimal), PercentFormat));
            }
        }

        private static void SetChanges(DataRow row, List<long?> values, Func<int, string> suffix)
        {
            if (values[0] is not { } from) return;
            for (var i = 1; i < values.Count; i++)
            {
                if (values[i] is not { } to) continue;
                row[$"Change{suffix(i)}"] = to - from;
                if (from != 0) row[$"Change %{suffix(i)}"] = (to - from) * 100m / from;
            }
        }

        private static DataColumn AddColumn(DataTable table, string name, Type type, string format)
        {
            var column = table.Columns.Add(name, type);
            column.ExtendedProperties["Format"] = format;
            return column;
        }

        /// <summary>
        /// A new table for a comparison: its columns are named for the runs, so bars a user changes on it would come back
        /// on another comparison with nothing to do with this one - not remembered.
        /// </summary>
        private static DataTable NewTable(string name)
        {
            var table = new DataTable(name);
            table.ExtendedProperties[GridViewerControl.RememberDataBarsProperty] = false;
            return table;
        }
    }
}
