using DBADashGUI.Charts;
using System.Collections.Generic;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Latch stats (sys.dm_os_latch_stats) for an instance - a chart over time by latch class of the measure picked
    /// (wait time or waiting requests per second) with a summary grid below it.
    ///
    /// The collection is off by default, so the report is shown on its own tab next to Waits rather than in the
    /// Reports folder, and that tab is only shown where the collection is enabled or has data (see
    /// <see cref="DBADashContext.HasLatchStats"/>).
    /// </summary>
    internal class LatchStatsReport
    {
        public static SystemReport Instance => new()
        {
            SchemaName = "dbo",
            ProcedureName = "LatchStats_Get",
            QualifiedProcedureName = "dbo.LatchStats_Get",
            ReportVisibilityRole = "public",
            ReportName = "Latches",
            Description = "Latch wait time by latch class (sys.dm_os_latch_stats).",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string> { "LatchStats" },
            ChartVisible = true,
            TableVisible = true,
            ChartLayoutColumns = 1,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = 1,
                    Title = "Latches",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.StackedColumn,
                        ChartTitle = "Latches",
                        XColumn = "SnapshotDate",
                        // The proc returns the measure picked in @Measure (per second) as Value.  The axis is
                        // deliberately unlabelled as the measure is a picker - see the menu bar for what's shown.
                        MetricColumn = "Value",
                        SeriesColumn = "LatchClass",
                        YAxisFormat = "0.0",
                        YAxisMin = 0
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
                        ["LatchClass"] = new ColumnMetadata { Alias = "Latch Class" },
                        // Rates are averaged over the part of the selected date range that the collection covers, so
                        // they mean the same thing whatever the length of the range.
                        ["WaitTimeMsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Wait Time (ms/sec)",
                            FormatString = "N3",
                            Description = "Wait time per second - this is also the average number of threads waiting x 1000.  Highlighted at 1,000 ms/sec or more, where on average at least one thread was waiting on this latch class the whole time.",
                            // ms/sec of waiting is the average number of waiting threads, so 1000 is a threshold with a
                            // meaning rather than a guess.  Summary only - a short burst in the over time grid is less telling.
                            Highlighting = new CellHighlightingRuleSet("WaitTimeMsPerSec")
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
                        // The average over a long range can hide a burst of contention, so the busiest chart bucket is
                        // shown too.  Same threshold - a bucket is at least a minute, so at 1000 a thread was waiting on
                        // average for the whole of it.
                        ["PeakWaitTimeMsPerSec"] = new ColumnMetadata
                        {
                            Alias = "Peak Wait Time (ms/sec)",
                            FormatString = "N3",
                            Description = "The highest wait time per second in any one interval of the chart.  The interval depends on the length of the date range (from a minute up to a day), so the peak is closer to the average on longer ranges.  Highlighted at 1,000 ms/sec or more, where on average at least one thread was waiting on this latch class for the whole interval.",
                            Highlighting = new CellHighlightingRuleSet("PeakWaitTimeMsPerSec")
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
                        // For context only - not highlighted.  Waiting threads don't use CPU, so the cost of the wait
                        // (stalled requests) doesn't shrink with more cores and the highlight stays on ms/sec.
                        ["WaitTimeMsPerCorePerSec"] = new ColumnMetadata
                        {
                            Alias = "Wait Time (ms/sec/core)",
                            FormatString = "N3",
                            Description = "Wait time per second divided by the number of schedulers (cores), for comparison with the Waits report."
                        },
                        ["PctOfTotal"] = new ColumnMetadata { Alias = "% of Total", FormatString = "P1" },
                        ["WaitingRequestsPerSec"] = new ColumnMetadata { Alias = "Waiting Requests/sec", FormatString = "N2" },
                        ["AvgWaitMs"] = new ColumnMetadata { Alias = "Avg Wait (ms)", FormatString = "N3" },
                        // Totals for the date range.  Hidden as they depend on its length - the rates are comparable
                        // across ranges - but available from the column chooser.
                        ["TotalWaitSec"] = new ColumnMetadata { Alias = "Total Wait (sec)", FormatString = "N1", Visible = false },
                        ["WaitingRequests"] = new ColumnMetadata { Alias = "Total Waiting Requests", FormatString = "N0", Visible = false },
                        ["MaxWaitMs"] = new ColumnMetadata
                        {
                            Alias = "Max Wait (ms)",
                            FormatString = "N0",
                            Description = "Longest single wait since the instance started or the latch stats were cleared.  Not limited to the selected date range."
                        },
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
                        ["LatchClass"] = new ColumnMetadata { Alias = "Latch Class" },
                        ["Value"] = new ColumnMetadata { Alias = "Selected Measure (per sec)", FormatString = "N3" },
                        ["WaitTimeMsPerSec"] = new ColumnMetadata { Alias = "Wait Time (ms/sec)", FormatString = "N3" },
                        ["WaitingRequestsPerSec"] = new ColumnMetadata { Alias = "Waiting Requests/sec", FormatString = "N2" },
                        ["AvgWaitMs"] = new ColumnMetadata { Alias = "Avg Wait (ms)", FormatString = "N3" }
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
                    // The top N latch classes are ranked by whichever is picked.
                    ParameterName = "@Measure",
                    Name = "Measure",
                    PickerItems = new Dictionary<object, string>
                    {
                        ["WaitTime"] = "Wait Time (ms/sec)",
                        ["WaitingRequests"] = "Waiting Requests/sec"
                    },
                    DefaultValue = "WaitTime",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@TopN",
                    Name = "Latch Classes",
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
