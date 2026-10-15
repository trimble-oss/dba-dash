using DBADashGUI.Charts;
using LiveChartsCore.Measure;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Spinlock stats (sys.dm_os_spinlock_stats) for an instance - a chart over time by spinlock of the measure
    /// picked (spins, backoffs, collisions or sleep time per second) with a summary grid below it.
    ///
    /// The collection is off by default, so the report is shown on its own tab next to Waits/Latches rather than in the
    /// Reports folder, and that tab is only shown where the collection is enabled or has data (see
    /// <see cref="DBADashContext.HasSpinlockStats"/>).
    /// </summary>
    internal class SpinlockStatsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "SpinlockStats_Get",
            QualifiedProcedureName = "dbo.SpinlockStats_Get",
            ReportVisibilityRole = "public",
            ReportName = "Spinlocks",
            Description = "Spinlock spins, collisions and backoffs by spinlock (sys.dm_os_spinlock_stats).",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string> { "SpinlockStats" },
            ChartVisible = true,
            TableVisible = true,
            ChartLayoutColumns = 1,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = 1,
                    Title = "Spinlocks",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.StackedColumn,
                        ChartTitle = "Spinlocks",
                        XColumn = "SnapshotDate",
                        // The proc returns the measure picked in @Measure (per second) as Value.  The axis is
                        // deliberately unlabelled as the measure is a picker - see the menu bar for what's shown.
                        MetricColumn = "Value",
                        SeriesColumn = "SpinlockName",
                        YAxisFormat = "N1",
                        YAxisMin = 0
                    },
                    // SQL Server's CPU on an axis of its own on the right, as spinning burns CPU in the SQL Server
                    // process - spins rising with it is what makes spinlock contention worth looking at.  Total CPU
                    // is in the CPU result set but not drawn: a second line needs a second colour the columns don't
                    // use, and there isn't one that reads well on both themes.  The colour is purple as it's outside
                    // the theme palettes (Material 500 / 200), whose nine colours the columns can use all of.
                    Overlays = new List<CustomReportChartOverlay>
                    {
                        new()
                        {
                            TableIndex = 2,
                            Overlay = new ChartOverlay
                            {
                                MetricColumns = new[] { "SQLProcessCPU" },
                                SeriesNames = new Dictionary<string, string>
                                {
                                    ["SQLProcessCPU"] = "SQL Server CPU %"
                                },
                                SeriesStyles = new Dictionary<string, ChartSeriesStyle>
                                {
                                    // Thinner than the theme's lines (4) so it doesn't dominate the columns
                                    ["SQLProcessCPU"] = new ChartSeriesStyle { Color = "#9C27B0", StrokeThickness = 2 }
                                },
                                YAxis = new YAxisConfiguration
                                {
                                    Name = "CPU",
                                    Label = "CPU %",
                                    Format = "0",
                                    MinLimit = 0,
                                    MaxLimit = 100,
                                    Position = AxisPosition.End
                                },
                                // Straight lines - smoothing overshoots a step in CPU, showing values that never happened
                                LineSmoothness = 0,
                                GeometrySize = 0
                            }
                        }
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Summary",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SpinlockName"] = new ColumnMetadata { Alias = "Spinlock" },
                        // Rates are averaged over the part of the selected date range that the collection covers, so
                        // they mean the same thing whatever the length of the range.
                        ["SpinsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Spins/sec",
                            FormatString = "N0",
                            Description = "Spins per second - CPU burned spinning while waiting for the spinlock."
                        },
                        ["SpinsPerMsPerCPU"] = new ColumnMetadata
                        {
                            Alias = "Spins/ms/CPU",
                            FormatString = "N1",
                            Description = "Spins per millisecond per CPU (scheduler).  Spinning burns CPU, so this shows the spinlock's cost relative to the size of the server.  Highlighted above 10,000, where the spinlock is likely causing high CPU (the threshold used by SQL Nexus).",
                            // SQL Nexus's usp_Spinlock_HighCPU rule.  The proc applies it (including excluding the
                            // "dbcc execution" spinlocks, as Nexus does) and returns the result as IsHighCPUSpinlock.
                            Highlighting = new CellHighlightingRuleSet("IsHighCPUSpinlock", true)
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new()
                                    {
                                        ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                        Value1 = "1",
                                        Status = DBADashStatus.DBADashStatusEnum.Warning
                                    }
                                }
                            }
                        },
                        ["IsHighCPUSpinlock"] = new ColumnMetadata { Visible = false },
                        // The average over a long range can hide a burst of contention, so the busiest chart bucket is
                        // shown too, highlighted by the same rule (applied by the proc as IsHighCPUSpinlockPeak).
                        ["PeakSpinsPerMsPerCPU"] = new ColumnMetadata
                        {
                            Alias = "Peak Spins/ms/CPU",
                            FormatString = "N1",
                            Description = "The highest spins per millisecond per CPU (scheduler) in any one interval of the chart.  The interval depends on the length of the date range (from a minute up to a day), so the peak is closer to the average on longer ranges.  Highlighted above 10,000, as for Spins/ms/CPU.",
                            Highlighting = new CellHighlightingRuleSet("IsHighCPUSpinlockPeak", true)
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new()
                                    {
                                        ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                        Value1 = "1",
                                        Status = DBADashStatus.DBADashStatusEnum.Warning
                                    }
                                }
                            }
                        },
                        ["IsHighCPUSpinlockPeak"] = new ColumnMetadata { Visible = false },
                        ["PctOfTotal"] = new ColumnMetadata { Alias = "% of Total Spins", FormatString = "P1" },
                        ["CollisionsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Collisions/sec",
                            FormatString = "N1",
                            Description = "Times per second a thread tried to acquire the spinlock and found it held by another thread."
                        },
                        ["SpinsPerCollision"] = new ColumnMetadata { Alias = "Spins/Collision", FormatString = "N1" },
                        ["BackoffsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Backoffs/sec",
                            FormatString = "N2",
                            Description = "Times per second a spinning thread gave up and slept (backed off) before trying again.  A sign the contention is bad enough to hurt."
                        },
                        ["SleepTimeMsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Sleep Time (ms/sec)",
                            FormatString = "N2",
                            Description = "Time per second threads spent sleeping after backing off - this is also the average number of sleeping threads x 1000.  Highlighted at 1,000 ms/sec or more, where on average at least one thread was asleep waiting on this spinlock the whole time.",
                            // Backing off and sleeping is the spinlock's last resort, and ms/sec of it is the average number
                            // of threads asleep, so 1000 is a threshold with a meaning rather than a guess.  Spins, backoffs
                            // and collisions aren't highlighted - healthy busy servers have very large numbers of them.
                            Highlighting = new CellHighlightingRuleSet("SleepTimeMsPerSec")
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new()
                                    {
                                        ConditionType = CellHighlightingRule.ConditionTypes.GreaterThanOrEqual,
                                        Value1 = "1000",
                                        Status = DBADashStatus.DBADashStatusEnum.Warning
                                    }
                                }
                            }
                        },
                        // Same threshold as the average - a bucket is at least a minute, so at 1000 a thread was asleep on
                        // average for the whole of it.
                        ["PeakSleepTimeMsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Peak Sleep Time (ms/sec)",
                            FormatString = "N2",
                            Description = "The highest sleep time per second in any one interval of the chart.  The interval depends on the length of the date range (from a minute up to a day), so the peak is closer to the average on longer ranges.  Highlighted at 1,000 ms/sec or more, where on average at least one thread was asleep waiting on this spinlock for the whole interval.",
                            Highlighting = new CellHighlightingRuleSet("PeakSleepTimeMsPerSec")
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new()
                                    {
                                        ConditionType = CellHighlightingRule.ConditionTypes.GreaterThanOrEqual,
                                        Value1 = "1000",
                                        Status = DBADashStatus.DBADashStatusEnum.Warning
                                    }
                                }
                            }
                        },
                        // Totals for the date range.  Hidden as they depend on its length - the rates are comparable
                        // across ranges - but available from the column chooser.
                        ["Spins"] = new ColumnMetadata { Alias = "Total Spins", FormatString = "N0", Visible = false },
                        ["Collisions"] = new ColumnMetadata { Alias = "Total Collisions", FormatString = "N0", Visible = false },
                        ["Backoffs"] = new ColumnMetadata { Alias = "Total Backoffs", FormatString = "N0", Visible = false },
                        ["SleepTimeMs"] = new ColumnMetadata { Alias = "Total Sleep Time (ms)", FormatString = "N0", Visible = false },
                        ["Description"] = new ColumnMetadata(),
                        // Shown when the collection hasn't run for the instance; the proc returns this instead.
                        ["Message"] = new ColumnMetadata()
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Over Time",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new ColumnMetadata { Alias = "Snapshot Date" },
                        ["SpinlockName"] = new ColumnMetadata { Alias = "Spinlock" },
                        ["Value"] = new ColumnMetadata { Alias = "Selected Measure (per sec)", FormatString = "N1" },
                        ["SpinsPerSec"] = new ColumnMetadata { Alias = "Spins/sec", FormatString = "N0" },
                        ["CollisionsPerSec"] = new ColumnMetadata { Alias = "Collisions/sec", FormatString = "N1" },
                        ["BackoffsPerSec"] = new ColumnMetadata { Alias = "Backoffs/sec", FormatString = "N2" },
                        ["SleepTimeMsPerSec"] = new ColumnMetadata { Alias = "Sleep Time (ms/sec)", FormatString = "N2" },
                        ["SpinsPerCollision"] = new ColumnMetadata { Alias = "Spins/Collision", FormatString = "N1" }
                    }
                },
                // Drawn over the chart (see Overlays above) and available as a grid too
                [2] = new CustomReportResult
                {
                    ResultName = "CPU",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new ColumnMetadata { Alias = "Snapshot Date" },
                        ["SQLProcessCPU"] = new ColumnMetadata { Alias = "SQL Server CPU %", FormatString = "N1", Description = "Average CPU used by SQL Server over the interval." },
                        ["TotalCPU"] = new ColumnMetadata { Alias = "Total CPU %", FormatString = "N1", Description = "Average CPU used by everything on the server over the interval." },
                        ["MaxTotalCPU"] = new ColumnMetadata { Alias = "Max Total CPU %", FormatString = "N0", Description = "Highest total CPU in any one minute of the interval." }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@TopN", ParamType = "INT" },
                    new() { ParamName = "@Measure", ParamType = "VARCHAR" }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    // Spins show where the CPU goes; backoffs show where contention is bad enough that threads give
                    // up spinning and sleep.  The top N spinlocks are ranked by whichever is picked.
                    ParameterName = "@Measure",
                    Name = "Measure",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["Spins"] = "Spins/sec",
                        ["Backoffs"] = "Backoffs/sec",
                        ["Collisions"] = "Collisions/sec",
                        ["SleepTime"] = "Sleep Time (ms/sec)"
                    },
                    DefaultValue = "Spins",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@TopN",
                    Name = "Spinlocks",
                    PickerItems = new Dictionary<object, string>
                    {
                        [5] = "Top 5",
                        [10] = "Top 10",
                        [20] = "Top 20",
                        [50] = "Top 50"
                    },
                    DefaultValue = 10,
                    MenuBar = true,
                    DataType = typeof(int)
                }
            }
        };
    }
}
