#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using static DBADash.PerformanceAnalysis.PerformanceAnalysisTable;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>One section as it is sent: a title and the text under it.</summary>
    public sealed class PerformanceAnalysisPayloadSection
    {
        public PerformanceSection Section { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;
    }

    /// <summary>
    /// Everything a performance analysis is given, assembled in one place so that what the reader is
    /// shown before submitting and what is actually sent cannot drift apart - the performance counterpart
    /// of the plan and deadlock payloads, and the same shape.
    ///
    /// Built from <see cref="PerformanceAnalysisData"/> without going back to the repository, so a
    /// section switched on or off, or a Top N changed, rebuilds the preview at once.
    ///
    /// What goes in is summarised rather than shipped: a rate per section for the window and its
    /// baseline, the top few series over time where the shape matters, and the rest rolled up.  Seven days
    /// of minute-grain counters would be megabytes and say less.
    /// </summary>
    public sealed class PerformanceAnalysisPayload
    {
        /// <summary>
        /// What kind of request this is - which is to say, what the model was given to reason from.  Bump
        /// it when the content changes materially, not for wording.
        /// </summary>
        public const string Revision = "1";

        /// <summary>
        /// A payload large enough to be worth a word before it goes.  Not a limit - the service has its
        /// own - but past this a smaller model is likely to refuse it, and the reader can trim it first.
        /// </summary>
        public const int LargeLength = 120 * 1024;

        public string Instance { get; private init; } = string.Empty;

        public int InstanceId { get; private init; }

        public PerformanceAnalysisFocus Focus { get; private init; }

        /// <summary>The window, as the model and the reader are told it - in the reader's time zone.</summary>
        public string Window { get; private init; } = string.Empty;

        /// <summary>The baseline, likewise.  Null when the reader switched it off.</summary>
        public string? Baseline { get; private init; }

        /// <summary>How the data was chosen, where the reader should know.</summary>
        public IReadOnlyList<string> Notes { get; private init; } = Array.Empty<string>();

        public IReadOnlyList<PerformanceAnalysisPayloadSection> Sections { get; private init; } = Array.Empty<PerformanceAnalysisPayloadSection>();

        public string Version => Revision;

        /// <summary>
        /// Roughly what a length of this payload costs in tokens.  Mostly numbers and separators, which
        /// tokenise at about three characters - close enough for the one decision it informs.
        /// </summary>
        public static int EstimateTokens(int characters) => characters / 3;

        public static string DescribeSize(int characters) =>
            $"{characters / 1024d:N0} KB, roughly {EstimateTokens(characters) / 1000d:N1}k tokens";

        public int Length => ToPreview().Length;

        public bool IsLarge => Length > LargeLength;

        /// <summary>
        /// The payload as one text: the header, then each section under its title.  This is what the
        /// preview shows and what the prompt carries - the service rebuilds it from the same fields.
        /// </summary>
        public string ToPreview()
        {
            var text = new StringBuilder();
            text.AppendLine($"Instance: {Instance}");
            text.AppendLine($"Focus: {DescribeFocus(Focus)}");
            text.AppendLine($"Window: {Window}");
            text.AppendLine(Baseline is null ? "Baseline: none" : $"Baseline: {Baseline}");
            foreach (var note in Notes)
            {
                text.AppendLine($"Note: {note}");
            }

            foreach (var section in Sections)
            {
                text.AppendLine();
                text.AppendLine($"## {section.Title}");
                text.Append(section.Content);
            }

            return text.ToString();
        }

        /// <summary>
        /// The whole prompt the AI service would build from this payload - its instructions, then the figures -
        /// for a reader to paste into an assistant of their own.
        /// </summary>
        public string ToPrompt() =>
            PerformanceAnalysisPrompt.Build(
                Focus == PerformanceAnalysisFocus.LatchesAndSpinlocks,
                Instance,
                Window,
                Baseline,
                Notes,
                Sections.Select(s => (s.Title, s.Content)));

        public static string DescribeFocus(PerformanceAnalysisFocus focus) => focus switch
        {
            PerformanceAnalysisFocus.LatchesAndSpinlocks => "Latch and spinlock contention",
            _ => "General performance"
        };

        /// <summary>
        /// Builds the payload.
        /// </summary>
        /// <param name="data">What the repository returned.</param>
        /// <param name="options">What the reader chose to send.</param>
        /// <param name="toLocal">Converts a UTC time to the reader's time zone, which is how every time is shown.</param>
        /// <param name="timeZone">The reader's time zone as it should be named in the payload.</param>
        public static PerformanceAnalysisPayload Build(
            PerformanceAnalysisData data,
            PerformanceAnalysisOptions options,
            Func<DateTime, DateTime> toLocal,
            string timeZone)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(toLocal);

            var builder = new Builder(data, options, toLocal);
            var window = data.Window;
            var (baseFrom, baseTo) = window.Baseline;

            var notes = new List<string>(data.Notes);
            if (window.WasClamped)
            {
                notes.Add($"The range selected was longer than {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)}, so only its last {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)} are included.");
            }

            notes.Add("Rates are per second of time the collection covered, as in the DBA Dash charts.  Times are local to the reader.");

            return new PerformanceAnalysisPayload
            {
                Instance = data.Instance,
                InstanceId = data.InstanceId,
                Focus = options.Focus,
                Window = $"{FormatFull(toLocal(window.FromUtc))} to {FormatFull(toLocal(window.ToUtc))} ({timeZone}), " +
                         $"{PerformanceAnalysisWindow.Describe(window.Length)}.  Time series are in {window.BucketMinutes}-minute buckets.",
                Baseline = options.IncludeBaseline
                    ? $"{FormatFull(toLocal(baseFrom))} to {FormatFull(toLocal(baseTo))} - the same window one week earlier, in whole hours from the hourly rollups."
                    : null,
                Notes = notes,
                Sections = options.Sections
                    .Where(s => s.Include)
                    .Select(builder.Build)
                    .ToList()
            };
        }

        /// <summary>
        /// The object name DBA Dash logs its own counters under - collection durations, plan counts.  They
        /// are about the monitoring, not the instance, so they are left out of an analysis of the instance.
        /// </summary>
        public const string InternalCounterObject = "DBADash";

        public static bool IsInternalCounter(DataRow row) =>
            PerformanceAnalysisTable.Text(row, "object_name").Trim().Equals(InternalCounterObject, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// DBA Dash's own objects whose duration is time spent waiting rather than working.  The messaging
        /// receive sits in WAITFOR (RECEIVE) with a 30 second timeout, so on a quiet instance it can top the
        /// object execution list while costing nothing.  Its other monitoring objects stay in: if they are
        /// expensive, that is worth knowing.
        /// </summary>
        internal static readonly (string Schema, string Name)[] IdleWaitObjects = [("Messaging", "ReceiveMessageToService")];

        public static bool IsIdleWait(DataRow row)
        {
            var schema = PerformanceAnalysisTable.Text(row, "SchemaName").Trim();
            var name = PerformanceAnalysisTable.Text(row, "ObjectName").Trim();
            return IdleWaitObjects.Any(o => o.Schema.Equals(schema, StringComparison.OrdinalIgnoreCase) && o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatFull(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        /// <summary>The per-section work, holding what every section needs.</summary>
        private sealed class Builder
        {
            private readonly PerformanceAnalysisData _data;
            private readonly PerformanceAnalysisOptions _options;
            private readonly Func<DateTime, DateTime> _toLocal;

            /// <summary>Series times leave out the year - the window header carries it - and the date when the window is within a day.</summary>
            private readonly string _timeFormat;

            public Builder(PerformanceAnalysisData data, PerformanceAnalysisOptions options, Func<DateTime, DateTime> toLocal)
            {
                _data = data;
                _options = options;
                _toLocal = toLocal;
                _timeFormat = toLocal(data.Window.FromUtc).Date == toLocal(data.Window.ToUtc).Date ? "HH:mm" : "MM-dd HH:mm";
            }

            private bool Baseline => _options.IncludeBaseline;

            public PerformanceAnalysisPayloadSection Build(PerformanceSectionOptions section)
            {
                var text = new StringBuilder();

                if (_data.Errors.TryGetValue(section.Section, out var error))
                {
                    text.AppendLine($"Not available - the data could not be loaded: {error}");
                }
                else
                {
                    switch (section.Section)
                    {
                        case PerformanceSection.Context: Context(text); break;
                        case PerformanceSection.CPU: Cpu(text, section); break;
                        case PerformanceSection.Waits: Waits(text, section); break;
                        case PerformanceSection.IO: IO(text, section); break;
                        case PerformanceSection.Metrics: Metrics(text, section); break;
                        case PerformanceSection.Memory: Memory(text, section); break;
                        case PerformanceSection.Blocking: Blocking(text, section); break;
                        case PerformanceSection.ObjectExecution: ObjectExecution(text, section); break;
                        case PerformanceSection.Latches: Latches(text, section); break;
                        case PerformanceSection.Spinlocks: Spinlocks(text, section); break;
                    }
                }

                return new PerformanceAnalysisPayloadSection
                {
                    Section = section.Section,
                    Title = PerformanceAnalysisOptions.Title(section.Section),
                    Content = text.ToString()
                };
            }

            private string Time(DateTime utc) => _toLocal(utc).ToString(_timeFormat, CultureInfo.InvariantCulture);

            private int Top(PerformanceSectionOptions section, int fallback) =>
                Math.Clamp(section.TopN ?? fallback, 1, PerformanceAnalysisOptions.MaxTopN);

            private static bool HasRows(DataTable? table) => table is { Rows.Count: > 0 };

            private static IEnumerable<DataRow> Rows(DataTable? table) => table?.Rows.Cast<DataRow>() ?? Enumerable.Empty<DataRow>();

            /// <summary>Rows of a baseline table keyed by a name column, for looking up the baseline of a window row.</summary>
            private Dictionary<string, DataRow> ByName(DataTable? table, Func<DataRow, string> key)
            {
                var rows = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
                if (!Baseline) return rows;

                foreach (var row in Rows(table))
                {
                    rows.TryAdd(key(row), row);
                }

                return rows;
            }

            private void Context(StringBuilder text)
            {
                foreach (var row in Rows(_data.InstanceInfo))
                {
                    var name = PerformanceAnalysisTable.Text(row, "Name");
                    var value = PerformanceAnalysisTable.Text(row, "Value").Trim();
                    // The connection ID is an internal identifier, not anything about the server, and a display name
                    // the same as the instance name says nothing twice.
                    if (value.Length == 0 || value.EndsWith(':')
                        || name.Equals("Connection ID", StringComparison.OrdinalIgnoreCase)
                        || (name.Equals("Instance Display Name", StringComparison.OrdinalIgnoreCase) && value.Equals(_data.Instance, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    text.AppendLine($"{name}: {value}");
                }

                // The settings that change how a workload behaves, whatever their values, and any other that
                // has been changed from its default.  A full sp_configure listing is mostly defaults.
                var settings = Rows(_data.Configuration)
                    .Where(r => KeySettings.Contains((int)(PerformanceAnalysisTable.Double(r, "configuration_id") ?? 0))
                                || PerformanceAnalysisTable.Get(r, "IsDefault") is false)
                    .ToList();

                if (settings.Count > 0)
                {
                    text.AppendLine("Configuration (key settings, and any changed from the default):");
                    foreach (var row in settings)
                    {
                        var defaultValue = PerformanceAnalysisTable.Text(row, "default_value");
                        text.AppendLine($"- {PerformanceAnalysisTable.Text(row, "name")} = {PerformanceAnalysisTable.Text(row, "value")}" +
                                        (defaultValue.Length > 0 ? $" (default {defaultValue})" : string.Empty));
                    }
                }

                if (text.Length == 0) text.AppendLine("No instance details are available.");
            }

            /// <summary>Cost threshold for parallelism, MAXDOP, max and min server memory, optimize for ad hoc workloads.</summary>
            private static readonly HashSet<int> KeySettings = [1538, 1539, 1543, 1544, 1581];

            private void Cpu(StringBuilder text, PerformanceSectionOptions section)
            {
                if (!HasRows(_data.Cpu))
                {
                    text.AppendLine("No CPU data was collected in this window.");
                    return;
                }

                text.AppendLine($"% of the server's CPU.  Averages are over {_data.Window.BucketMinutes}-minute buckets; max is the highest single sample.");
                var summary = new PerformanceAnalysisTable("period", "sql_avg", "other_avg", "total_avg", "total_p95", "max");
                AddCpuSummary(summary, "window", _data.Cpu);
                if (Baseline) AddCpuSummary(summary, "baseline", _data.CpuBaseline);
                summary.AppendTo(text);

                if (section.TimeSeries != true) return;

                text.AppendLine();
                text.AppendLine("Over time:");
                var series = new PerformanceAnalysisTable("time", "sql", "other", "max");
                foreach (var row in Rows(_data.Cpu))
                {
                    if (PerformanceAnalysisTable.Time(row, "EventTime") is not { } time) continue;
                    series.Add(Time(time), Num(PerformanceAnalysisTable.Double(row, "SQLProcessCPU")),
                        Num(PerformanceAnalysisTable.Double(row, "OtherCPU")), Num(PerformanceAnalysisTable.Double(row, "MaxCPU")));
                }

                series.AppendTo(text);
            }

            private static void AddCpuSummary(PerformanceAnalysisTable table, string period, DataTable? cpu)
            {
                var rows = Rows(cpu).ToList();
                if (rows.Count == 0)
                {
                    table.Add(period, "no data", "", "", "", "");
                    return;
                }

                var sql = rows.Select(r => PerformanceAnalysisTable.Double(r, "SQLProcessCPU") ?? 0).ToList();
                var other = rows.Select(r => PerformanceAnalysisTable.Double(r, "OtherCPU") ?? 0).ToList();
                var total = sql.Zip(other, (s, o) => s + o).ToList();
                var max = rows.Max(r => PerformanceAnalysisTable.Double(r, "MaxCPU") ?? 0);

                // Weighted by samples, so a part bucket at either end of the window counts for what it holds.
                var weights = rows.Select(r => PerformanceAnalysisTable.Double(r, "SampleCount") ?? 1).ToList();

                table.Add(period, Num(WeightedAverage(sql, weights)), Num(WeightedAverage(other, weights)), Num(WeightedAverage(total, weights)),
                    Num(Percentile(total, 0.95)), Num(max));
            }

            private static double WeightedAverage(IReadOnlyList<double> values, IReadOnlyList<double> weights)
            {
                var sum = weights.Sum();
                return sum > 0 ? values.Zip(weights, (v, w) => v * w).Sum() / sum : values.Average();
            }

            internal static double Percentile(IReadOnlyList<double> values, double percentile)
            {
                if (values.Count == 0) return 0;

                var sorted = values.OrderBy(v => v).ToList();
                var rank = percentile * (sorted.Count - 1);
                var lower = (int)Math.Floor(rank);
                var upper = (int)Math.Ceiling(rank);
                return sorted[lower] + (sorted[upper] - sorted[lower]) * (rank - lower);
            }

            private void Waits(StringBuilder text, PerformanceSectionOptions section)
            {
                if (!HasRows(_data.Waits))
                {
                    text.AppendLine("No wait stats were collected in this window.");
                    return;
                }

                var rows = Rows(_data.Waits).ToList();
                var baseline = ByName(_data.WaitsBaseline, r => PerformanceAnalysisTable.Text(r, "WaitType"));
                var top = Top(section, 15);

                var total = rows.Sum(r => PerformanceAnalysisTable.Double(r, "WaitTimeMsPerSec") ?? 0);
                var signal = rows.Sum(r => PerformanceAnalysisTable.Double(r, "SignalWaitSec") ?? 0);
                var waited = rows.Sum(r => PerformanceAnalysisTable.Double(r, "TotalWaitSec") ?? 0);
                // No rows at all for the baseline is no history - a newly monitored instance - not a week without waits.
                var hasBaseline = HasRows(_data.WaitsBaseline);
                var baseTotal = Baseline && hasBaseline ? Rows(_data.WaitsBaseline).Sum(r => PerformanceAnalysisTable.Double(r, "WaitTimeMsPerSec") ?? 0) : (double?)null;

                text.AppendLine("Benign waits are already excluded.  ms_per_sec is wait time per second of elapsed time; critical marks waits DBA Dash flags as critical.");
                text.AppendLine($"All waits: {Num(total)} ms/sec, signal waits {Num(waited == 0 ? 0 : signal * 100 / waited)}% of wait time." +
                                (baseTotal is { } bt ? $"  Baseline: {Num(bt)} ms/sec ({Change(total, bt)})." :
                                    Baseline ? "  Baseline: no wait stats were collected, so there is nothing to compare with." : string.Empty));
                text.AppendLine($"Top {Math.Min(top, rows.Count)} of {rows.Count} wait types:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("wait_type", "ms_per_sec", "ms_per_core_sec", "signal_pct", "avg_wait_ms", "waits_per_sec", "critical", "baseline_ms_per_sec", "change")
                    : new PerformanceAnalysisTable("wait_type", "ms_per_sec", "ms_per_core_sec", "signal_pct", "avg_wait_ms", "waits_per_sec", "critical");

                foreach (var row in rows.OrderByDescending(r => PerformanceAnalysisTable.Double(r, "WaitTimeMsPerSec") ?? 0).Take(top))
                {
                    var type = PerformanceAnalysisTable.Text(row, "WaitType");
                    var msPerSec = PerformanceAnalysisTable.Double(row, "WaitTimeMsPerSec");
                    var sampleSec = PerformanceAnalysisTable.Double(row, "SampleDurationSec");
                    var tasks = PerformanceAnalysisTable.Double(row, "WaitingTasksCount");
                    var values = new List<string>
                    {
                        type,
                        Num(msPerSec),
                        Num(PerformanceAnalysisTable.Double(row, "WaitTimeMsPerCorePerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "SignalWaitPct") * 100),
                        Num(PerformanceAnalysisTable.Double(row, "AvgWaitTimeMs")),
                        Num(sampleSec is > 0 ? tasks / sampleSec : null),
                        PerformanceAnalysisTable.Flag(row, "CriticalWait") ? "yes" : ""
                    };

                    if (Baseline)
                    {
                        // Within a baseline that has waits, one that is missing did not happen.
                        double? before = !hasBaseline ? null : baseline.TryGetValue(type, out var b) ? PerformanceAnalysisTable.Double(b, "WaitTimeMsPerSec") ?? 0 : 0;
                        values.Add(Num(before));
                        values.Add(Change(msPerSec, before));
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);

                if (section.TimeSeries == true && HasRows(_data.WaitsSeries))
                {
                    text.AppendLine();
                    text.AppendLine($"Over time, ms_per_sec for the top {PerformanceAnalysisOptions.SeriesTopN} wait types and the rest as Other:");
                    Pivot(_data.WaitsSeries, "Time", "WaitType", "WaitTimeMsPerSec",
                        name => name.StartsWith("!!", StringComparison.Ordinal) ? name[2..] : name == "{Other}" ? "Other" : name).AppendTo(text);
                }
            }

            private void IO(StringBuilder text, PerformanceSectionOptions section)
            {
                if (!HasRows(_data.IODrives))
                {
                    text.AppendLine("No IO stats were collected in this window.");
                    return;
                }

                text.AppendLine("Latency is the average ms per read or write; peak latency is the worst single collection interval.");
                text.AppendLine("By drive ({All} is the instance total):");

                var baseline = ByName(_data.IODrivesBaseline, r => PerformanceAnalysisTable.Text(r, "Grp"));
                var drives = Baseline
                    ? new PerformanceAnalysisTable("drive", "read_iops", "write_iops", "read_mb_sec", "write_mb_sec", "read_ms", "write_ms", "peak_read_ms", "peak_write_ms", "baseline_iops", "baseline_read_ms", "baseline_write_ms")
                    : new PerformanceAnalysisTable("drive", "read_iops", "write_iops", "read_mb_sec", "write_mb_sec", "read_ms", "write_ms", "peak_read_ms", "peak_write_ms");

                // {All} first, then the busiest drives.
                foreach (var row in Rows(_data.IODrives)
                             .OrderByDescending(r => PerformanceAnalysisTable.Text(r, "Grp").StartsWith("{All}", StringComparison.Ordinal))
                             .ThenByDescending(r => PerformanceAnalysisTable.Double(r, "IOPs") ?? 0))
                {
                    var drive = PerformanceAnalysisTable.Text(row, "Grp");
                    var values = new List<string>
                    {
                        drive,
                        Num(PerformanceAnalysisTable.Double(row, "ReadIOPs")),
                        Num(PerformanceAnalysisTable.Double(row, "WriteIOPs")),
                        Num(PerformanceAnalysisTable.Double(row, "ReadMBsec")),
                        Num(PerformanceAnalysisTable.Double(row, "WriteMBsec")),
                        Num(PerformanceAnalysisTable.Double(row, "ReadLatency")),
                        Num(PerformanceAnalysisTable.Double(row, "WriteLatency")),
                        Num(PerformanceAnalysisTable.Double(row, "MaxReadLatency")),
                        Num(PerformanceAnalysisTable.Double(row, "MaxWriteLatency"))
                    };

                    if (Baseline)
                    {
                        baseline.TryGetValue(drive, out var b);
                        values.Add(b is null ? "" : Num(PerformanceAnalysisTable.Double(b, "IOPs")));
                        values.Add(b is null ? "" : Num(PerformanceAnalysisTable.Double(b, "ReadLatency")));
                        values.Add(b is null ? "" : Num(PerformanceAnalysisTable.Double(b, "WriteLatency")));
                    }

                    drives.Add(values.ToArray());
                }

                drives.AppendTo(text);

                // Ranked by stall - the time spent waiting on IO per second - which is what makes a database's IO matter:
                // a busy database on fast storage costs less than a quiet one on slow storage.
                var databases = Rows(_data.IODatabases)
                    .Where(r => !PerformanceAnalysisTable.Text(r, "Grp").StartsWith("{All}", StringComparison.Ordinal))
                    .Select(r => (Row: r, Stall: (PerformanceAnalysisTable.Double(r, "IOPs") ?? 0) * (PerformanceAnalysisTable.Double(r, "Latency") ?? 0)))
                    .Where(d => d.Stall > 0)
                    .OrderByDescending(d => d.Stall)
                    .ToList();

                if (databases.Count > 0)
                {
                    var top = Top(section, 10);
                    text.AppendLine();
                    text.AppendLine($"Top {Math.Min(top, databases.Count)} of {databases.Count} databases by IO stall (ms of IO wait per second):");
                    var table = new PerformanceAnalysisTable("database", "stall_ms_per_sec", "read_iops", "write_iops", "mb_sec", "read_ms", "write_ms");
                    foreach (var (row, stall) in databases.Take(top))
                    {
                        table.Add(PerformanceAnalysisTable.Text(row, "Grp"), Num(stall),
                            Num(PerformanceAnalysisTable.Double(row, "ReadIOPs")), Num(PerformanceAnalysisTable.Double(row, "WriteIOPs")),
                            Num(PerformanceAnalysisTable.Double(row, "MBsec")),
                            Num(PerformanceAnalysisTable.Double(row, "ReadLatency")), Num(PerformanceAnalysisTable.Double(row, "WriteLatency")));
                    }

                    table.AppendTo(text);
                }

                if (section.TimeSeries == true && HasRows(_data.IOSeries))
                {
                    text.AppendLine();
                    text.AppendLine("Over time, all drives:");
                    var series = new PerformanceAnalysisTable("time", "iops", "mb_sec", "read_ms", "write_ms");
                    foreach (var row in Rows(_data.IOSeries))
                    {
                        if (PerformanceAnalysisTable.Time(row, "SnapshotDate") is not { } time) continue;
                        series.Add(Time(time), Num(PerformanceAnalysisTable.Double(row, "IOPs")), Num(PerformanceAnalysisTable.Double(row, "MBsec")),
                            Num(PerformanceAnalysisTable.Double(row, "ReadLatency")), Num(PerformanceAnalysisTable.Double(row, "WriteLatency")));
                    }

                    series.AppendTo(text);
                }
            }

            private static string CounterName(DataRow row)
            {
                var instance = PerformanceAnalysisTable.Text(row, "instance_name");
                return $"{PerformanceAnalysisTable.Text(row, "object_name").Trim()}\\{PerformanceAnalysisTable.Text(row, "counter_name").Trim()}" +
                       (string.IsNullOrWhiteSpace(instance) ? string.Empty : $"[{instance.Trim()}]");
            }

            /// <summary>The status DBA Dash gives a counter against its thresholds: 1 critical, 2 warning.</summary>
            private static string Status(DataRow row) => (int)(PerformanceAnalysisTable.Double(row, "Status") ?? 0) switch
            {
                1 => "critical",
                2 => "warning",
                _ => ""
            };

            private void Metrics(StringBuilder text, PerformanceSectionOptions section)
            {
                var rows = Rows(_data.Metrics).Where(r => !IsInternalCounter(r)).ToList();
                if (rows.Count == 0)
                {
                    text.AppendLine("No performance counters were collected in this window.");
                    return;
                }

                var baseline = ByName(_data.MetricsBaseline, CounterName);
                var top = Top(section, 40);

                text.AppendLine("status is against the thresholds configured in DBA Dash, where there are any.");
                text.AppendLine(rows.Count > top
                    ? $"{top} of {rows.Count} counters - those over a threshold first:"
                    : $"{rows.Count} counters:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("counter", "avg", "min", "max", "status", "baseline_avg")
                    : new PerformanceAnalysisTable("counter", "avg", "min", "max", "status");

                // Over a threshold first, then the order the repository returns them in, which groups each object's counters.
                foreach (var row in rows.Select((r, i) => (Row: r, Index: i))
                             .OrderBy(r => Status(r.Row) switch { "critical" => 0, "warning" => 1, _ => 2 })
                             .ThenBy(r => r.Index)
                             .Take(top)
                             .Select(r => r.Row))
                {
                    var name = CounterName(row);
                    var values = new List<string>
                    {
                        name,
                        Num(PerformanceAnalysisTable.Double(row, "AvgValue")),
                        Num(PerformanceAnalysisTable.Double(row, "MinValue")),
                        Num(PerformanceAnalysisTable.Double(row, "MaxValue")),
                        Status(row)
                    };

                    if (Baseline)
                    {
                        values.Add(baseline.TryGetValue(name, out var b) ? Num(PerformanceAnalysisTable.Double(b, "AvgValue")) : "");
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);
            }

            private static double? Mb(DataRow row, string column) => PerformanceAnalysisTable.Double(row, column) / 1024;

            private void Memory(StringBuilder text, PerformanceSectionOptions section)
            {
                if (!HasRows(_data.Memory))
                {
                    text.AppendLine("No memory clerk data was collected in this window.");
                    return;
                }

                var rows = Rows(_data.Memory).ToList();
                var baseline = ByName(_data.MemoryBaseline, r => PerformanceAnalysisTable.Text(r, "MemoryClerkType"));
                var top = Top(section, 10);
                var total = rows.Sum(r => Mb(r, "pages_kb") ?? 0);

                text.AppendLine($"Memory clerks by pages allocated (MB).  current is the last snapshot in the window; all clerks total {Num(total)} MB.");
                text.AppendLine($"Top {Math.Min(top, rows.Count)} of {rows.Count} clerks:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("clerk", "current_mb", "pct", "min_mb", "avg_mb", "max_mb", "baseline_avg_mb")
                    : new PerformanceAnalysisTable("clerk", "current_mb", "pct", "min_mb", "avg_mb", "max_mb");

                foreach (var row in rows.OrderByDescending(r => PerformanceAnalysisTable.Double(r, "pages_kb") ?? 0).Take(top))
                {
                    var clerk = PerformanceAnalysisTable.Text(row, "MemoryClerkType");
                    var values = new List<string>
                    {
                        clerk,
                        Num(Mb(row, "pages_kb")),
                        Num(PerformanceAnalysisTable.Double(row, "Pct") * 100),
                        Num(Mb(row, "min_pages_kb")),
                        Num(Mb(row, "avg_pages_kb")),
                        Num(Mb(row, "max_pages_kb"))
                    };

                    if (Baseline)
                    {
                        values.Add(baseline.TryGetValue(clerk, out var b) ? Num(Mb(b, "avg_pages_kb")) : "");
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);
            }

            private void Blocking(StringBuilder text, PerformanceSectionOptions section)
            {
                var rows = Rows(_data.Blocking).ToList();
                var summary = BlockingSummary(rows);
                text.AppendLine("From blocking snapshots taken with the running queries collection.  Blocked wait is the total wait of every blocked session in the snapshot - " +
                                "how long each has been waiting so far, so a block that lasts is seen again, longer, in each snapshot.  Snapshots are not additive.");
                text.AppendLine($"Window: {summary}");
                if (Baseline) text.AppendLine($"Baseline: {BlockingSummary(Rows(_data.BlockingBaseline).ToList())}");

                if (rows.Count == 0) return;

                var top = Top(section, 5);
                text.AppendLine($"Worst {Math.Min(top, rows.Count)} snapshots:");
                var table = new PerformanceAnalysisTable("time", "blocked_sessions", "blocked_wait_sec");
                foreach (var row in rows.OrderByDescending(r => PerformanceAnalysisTable.Double(r, "BlockedWaitTime") ?? 0).Take(top))
                {
                    if (PerformanceAnalysisTable.Time(row, "SnapshotDateUTC") is not { } time) continue;
                    table.Add(Time(time), Num(PerformanceAnalysisTable.Double(row, "BlockedSessionCount")),
                        Num(PerformanceAnalysisTable.Double(row, "BlockedWaitTime") / 1000));
                }

                table.AppendTo(text);
            }

            private string BlockingSummary(IReadOnlyList<DataRow> rows)
            {
                if (rows.Count == 0) return "no blocking snapshots.";

                var count = rows.Count >= _data.BlockingSnapshotLimit ? $"at least {rows.Count:N0}" : $"{rows.Count:N0}";
                var wait = rows.Max(r => PerformanceAnalysisTable.Double(r, "BlockedWaitTime") ?? 0) / 1000;
                var sessions = rows.Max(r => PerformanceAnalysisTable.Double(r, "BlockedSessionCount") ?? 0);
                return $"{count} snapshots with blocking, at most {Num(wait)} sec of blocked wait in one snapshot, at most {Num(sessions)} sessions blocked at once.";
            }

            private void ObjectExecution(StringBuilder text, PerformanceSectionOptions section)
            {
                var all = Rows(_data.ObjectExecution)
                    .Where(r => PerformanceAnalysisTable.Double(r, "duration_ms_per_sec") is > 0)
                    .ToList();
                var rows = all
                    .Where(r => !IsIdleWait(r))
                    .OrderByDescending(r => PerformanceAnalysisTable.Double(r, "duration_ms_per_sec"))
                    .ToList();
                var leftOut = all.Count - rows.Count;

                var leftOutNote = $"Left out: {string.Join(", ", IdleWaitObjects.Select(o => $"{o.Schema}.{o.Name}"))} - DBA Dash's own messaging, which waits on WAITFOR (RECEIVE) and so shows as duration without doing any work.";
                if (rows.Count == 0)
                {
                    text.AppendLine(leftOut > 0
                        ? "No object recorded any duration in this window, apart from what is left out."
                        : "No object execution stats were collected in this window.");
                    if (leftOut > 0) text.AppendLine(leftOutNote);
                    return;
                }

                var top = Top(section, 10);
                text.AppendLine("Stored procedures, functions and triggers.  duration_ms_per_sec is the time spent in the object per second of elapsed time - its share of the workload.");
                if (leftOut > 0) text.AppendLine(leftOutNote);

                text.AppendLine($"Top {Math.Min(top, rows.Count)} of {rows.Count} objects by duration:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("object", "type", "execs_per_min", "duration_ms_per_sec", "avg_ms", "cpu_ms_per_sec", "avg_cpu_ms", "avg_logical_reads", "avg_physical_reads", "baseline_execs_per_min", "baseline_avg_ms", "avg_ms_change")
                    : new PerformanceAnalysisTable("object", "type", "execs_per_min", "duration_ms_per_sec", "avg_ms", "cpu_ms_per_sec", "avg_cpu_ms", "avg_logical_reads", "avg_physical_reads");

                foreach (var row in rows.Take(top))
                {
                    var avgMs = PerformanceAnalysisTable.Double(row, "avg_duration_sec") * 1000;
                    var values = new List<string>
                    {
                        $"{PerformanceAnalysisTable.Text(row, "DB")}.{PerformanceAnalysisTable.Text(row, "SchemaName")}.{PerformanceAnalysisTable.Text(row, "ObjectName")}",
                        PerformanceAnalysisTable.Text(row, "ObjectType").Trim(),
                        Num(PerformanceAnalysisTable.Double(row, "execs_per_min")),
                        Num(PerformanceAnalysisTable.Double(row, "duration_ms_per_sec")),
                        Num(avgMs),
                        Num(PerformanceAnalysisTable.Double(row, "cpu_ms_per_sec")),
                        Num(PerformanceAnalysisTable.Double(row, "avg_cpu_sec") * 1000),
                        Num(PerformanceAnalysisTable.Double(row, "avg_logical_reads")),
                        Num(PerformanceAnalysisTable.Double(row, "avg_physical_reads"))
                    };

                    if (Baseline)
                    {
                        var baseAvgMs = PerformanceAnalysisTable.Double(row, "compare_avg_duration_sec") * 1000;
                        values.Add(Num(PerformanceAnalysisTable.Double(row, "compare_execs_per_min")));
                        values.Add(Num(baseAvgMs));
                        values.Add(Change(avgMs, baseAvgMs));
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);
            }

            /// <summary>The message a latch or spinlock procedure returns in place of data when the collection has not run.</summary>
            private static string? NotCollected(DataSet? set) =>
                set is { Tables.Count: > 0 } && set.Tables[0].Columns.Contains("Message") && set.Tables[0].Rows.Count > 0
                    ? PerformanceAnalysisTable.Text(set.Tables[0].Rows[0], "Message")
                    : null;

            private static DataTable? Summary(DataSet? set) => set is { Tables.Count: > 0 } && NotCollected(set) is null ? set.Tables[0] : null;

            private static DataTable? Series(DataSet? set) => set is { Tables.Count: > 1 } && NotCollected(set) is null ? set.Tables[1] : null;

            private void Latches(StringBuilder text, PerformanceSectionOptions section)
            {
                if (NotCollected(_data.Latches) is { } message)
                {
                    text.AppendLine($"Not collected for this instance.  ({message})");
                    return;
                }

                var rows = Rows(Summary(_data.Latches)).ToList();
                if (rows.Count == 0)
                {
                    text.AppendLine("No latch waits were recorded in this window.");
                    return;
                }

                var baseline = ByName(Summary(_data.LatchesBaseline), r => PerformanceAnalysisTable.Text(r, "LatchClass"));
                var top = Top(section, 5);

                text.AppendLine("Non-buffer latch waits (sys.dm_os_latch_stats).  Buffer latch waits are the PAGELATCH and PAGEIOLATCH waits in the Waits section.  " +
                                "peak is the worst bucket's rate; max_wait_ms is the longest single wait since the instance started.");
                text.AppendLine($"Top {Math.Min(top, rows.Count)} of {rows.Count} latch classes:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("latch_class", "wait_ms_per_sec", "peak_wait_ms_per_sec", "ms_per_core_sec", "pct", "requests_per_sec", "avg_wait_ms", "max_wait_ms", "baseline_wait_ms_per_sec", "change")
                    : new PerformanceAnalysisTable("latch_class", "wait_ms_per_sec", "peak_wait_ms_per_sec", "ms_per_core_sec", "pct", "requests_per_sec", "avg_wait_ms", "max_wait_ms");

                foreach (var row in rows.Take(top))
                {
                    var latch = PerformanceAnalysisTable.Text(row, "LatchClass");
                    var rate = PerformanceAnalysisTable.Double(row, "WaitTimeMsPerSec");
                    var values = new List<string>
                    {
                        latch,
                        Num(rate),
                        Num(PerformanceAnalysisTable.Double(row, "PeakWaitTimeMsPerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "WaitTimeMsPerCorePerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "PctOfTotal") * 100),
                        Num(PerformanceAnalysisTable.Double(row, "WaitingRequestsPerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "AvgWaitMs")),
                        Num(PerformanceAnalysisTable.Double(row, "MaxWaitMs"))
                    };

                    if (Baseline)
                    {
                        var before = baseline.TryGetValue(latch, out var b) ? PerformanceAnalysisTable.Double(b, "WaitTimeMsPerSec") ?? 0 : (double?)null;
                        values.Add(Num(before));
                        values.Add(Change(rate, before));
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);

                if (section.TimeSeries == true && Series(_data.Latches) is { Rows.Count: > 0 } series)
                {
                    text.AppendLine();
                    text.AppendLine($"Over time, wait_ms_per_sec for the top {PerformanceAnalysisOptions.SeriesTopN} latch classes and the rest as Other:");
                    Pivot(series, "SnapshotDate", "LatchClass", "WaitTimeMsPerSec", name => name).AppendTo(text);
                }
            }

            private void Spinlocks(StringBuilder text, PerformanceSectionOptions section)
            {
                if (NotCollected(_data.Spinlocks) is { } message)
                {
                    text.AppendLine($"Not collected for this instance.  ({message})");
                    return;
                }

                var rows = Rows(Summary(_data.Spinlocks)).ToList();
                if (rows.Count == 0)
                {
                    text.AppendLine("No spinlock activity was recorded in this window.");
                    return;
                }

                var baseline = ByName(Summary(_data.SpinlocksBaseline), r => PerformanceAnalysisTable.Text(r, "SpinlockName"));
                var top = Top(section, 5);

                text.AppendLine("sys.dm_os_spinlock_stats.  Spins burn CPU; backoffs and sleep time are where spinning gave up, a sign contention is hurting.  " +
                                "high_cpu marks a spinlock over 10,000 spins/ms/CPU (the SQL Nexus threshold), on average or at its peak.");
                text.AppendLine($"Top {Math.Min(top, rows.Count)} of {rows.Count} spinlocks by spins:");

                var table = Baseline
                    ? new PerformanceAnalysisTable("spinlock", "spins_per_sec", "spins_per_ms_per_cpu", "peak_spins_per_ms_per_cpu", "high_cpu", "pct", "collisions_per_sec", "spins_per_collision", "backoffs_per_sec", "sleep_ms_per_sec", "baseline_spins_per_sec", "change")
                    : new PerformanceAnalysisTable("spinlock", "spins_per_sec", "spins_per_ms_per_cpu", "peak_spins_per_ms_per_cpu", "high_cpu", "pct", "collisions_per_sec", "spins_per_collision", "backoffs_per_sec", "sleep_ms_per_sec");

                foreach (var row in rows.Take(top))
                {
                    var name = PerformanceAnalysisTable.Text(row, "SpinlockName");
                    var rate = PerformanceAnalysisTable.Double(row, "SpinsPerSec");
                    var high = PerformanceAnalysisTable.Flag(row, "IsHighCPUSpinlock") ? "avg"
                        : PerformanceAnalysisTable.Flag(row, "IsHighCPUSpinlockPeak") ? "peak" : "";
                    var values = new List<string>
                    {
                        name,
                        Num(rate),
                        Num(PerformanceAnalysisTable.Double(row, "SpinsPerMsPerCPU")),
                        Num(PerformanceAnalysisTable.Double(row, "PeakSpinsPerMsPerCPU")),
                        high,
                        Num(PerformanceAnalysisTable.Double(row, "PctOfTotal") * 100),
                        Num(PerformanceAnalysisTable.Double(row, "CollisionsPerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "SpinsPerCollision")),
                        Num(PerformanceAnalysisTable.Double(row, "BackoffsPerSec")),
                        Num(PerformanceAnalysisTable.Double(row, "SleepTimeMsPerSec"))
                    };

                    if (Baseline)
                    {
                        var before = baseline.TryGetValue(name, out var b) ? PerformanceAnalysisTable.Double(b, "SpinsPerSec") ?? 0 : (double?)null;
                        values.Add(Num(before));
                        values.Add(Change(rate, before));
                    }

                    table.Add(values.ToArray());
                }

                table.AppendTo(text);

                if (section.TimeSeries == true && Series(_data.Spinlocks) is { Rows.Count: > 0 } series)
                {
                    text.AppendLine();
                    text.AppendLine($"Over time, spins_per_sec for the top {PerformanceAnalysisOptions.SeriesTopN} spinlocks and the rest as Other:");
                    Pivot(series, "SnapshotDate", "SpinlockName", "SpinsPerSec", name => name).AppendTo(text);
                }
            }

            /// <summary>
            /// A long series - one row per time and name - as one row per time and a column per name, which is
            /// how it reads as a chart and a fraction of the size.  Names keep the order they first appear in,
            /// which is the procedures' ranking, with Other last.
            /// </summary>
            private PerformanceAnalysisTable Pivot(DataTable? source, string timeColumn, string nameColumn, string valueColumn, Func<string, string> clean)
            {
                var points = Rows(source)
                    .Select(r => (Time: PerformanceAnalysisTable.Time(r, timeColumn), Name: clean(PerformanceAnalysisTable.Text(r, nameColumn)), Value: PerformanceAnalysisTable.Double(r, valueColumn)))
                    .Where(p => p.Time.HasValue)
                    .ToList();

                var names = points.Select(p => p.Name).Distinct()
                    .OrderBy(n => n == "Other" ? 1 : 0)
                    .ToList();

                var table = new PerformanceAnalysisTable(new[] { "time" }.Concat(names).ToArray());
                // Summed rather than keyed outright: two names can clean to the same one.
                var values = points
                    .GroupBy(p => (p.Time!.Value, p.Name))
                    .ToDictionary(g => g.Key, g => (double?)g.Sum(p => p.Value ?? 0));

                foreach (var time in points.Select(p => p.Time!.Value).Distinct().OrderBy(t => t))
                {
                    table.Add(new[] { Time(time) }
                        .Concat(names.Select(n => values.TryGetValue((time, n), out var v) ? Num(v) : "0"))
                        .ToArray());
                }

                return table;
            }
        }
    }
}
