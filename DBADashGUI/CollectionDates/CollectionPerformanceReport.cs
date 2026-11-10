using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using System.Collections.Generic;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// A collection's performance over time - duration, CPU and logical reads for collecting and importing, and how the runs
    /// went - opened from the Collection Health report (<see cref="CollectionHealthView"/>).  Each run where the service keeps
    /// them (LogInternalPerformanceCounters) and the range is short enough, otherwise by the hour.
    /// </summary>
    internal static class CollectionPerformanceReport
    {
        private const int DurationTableIndex = 1;
        private const int CpuTableIndex = 2;
        private const int ReadsTableIndex = 3;
        private const int RunsTableIndex = 4;

        private static ColumnMetadata Hidden() => new() { Visible = false };

        private static CustomReportChart Chart(int tableIndex, string title, string metricColumn, string seriesColumn, ChartTypes chartType = ChartTypes.Line, Dictionary<string, ChartSeriesStyle> seriesStyles = null) => new()
        {
            TableIndex = tableIndex,
            Title = title,
            BindXAxisToDateRange = true,
            Config = new ChartConfiguration
            {
                ChartType = chartType,
                ChartTitle = title,
                XColumn = "SnapshotDate",
                MetricColumn = metricColumn,
                SeriesColumn = seriesColumn,
                SeriesStyles = seriesStyles,
                YAxisFormat = "N0",
                YAxisMin = 0
            }
        };

        private static CustomReportResult ChartData(string name, string seriesColumn, string valueColumn, string valueAlias) => new()
        {
            ResultName = name,
            ShowWithAllResults = false, // Chart data
            Columns = new Dictionary<string, ColumnMetadata>
            {
                ["SnapshotDate"] = new() { Alias = "Date" },
                [seriesColumn] = new(),
                [valueColumn] = new() { Alias = valueAlias, FormatString = "N0" }
            }
        };

        public static SystemReport ReportInstance => new()
        {
            ReportName = "Collection Performance",
            Description = "Duration, CPU and logical reads for collecting and importing a collection over time, and how its runs went.",
            SchemaName = "dbo",
            ProcedureName = "CollectionPerformance_Get",
            QualifiedProcedureName = "dbo.CollectionPerformance_Get",
            CanEditReport = false,
            ChartVisible = true,
            TableVisible = true,
            ChartLayoutColumns = 2, // 2 x 2.  No import lag chart - it's the batch's, the same for every collection in it (Collection Health charts it)
            Charts = new List<CustomReportChart>
            {
                Chart(DurationTableIndex, "Duration (ms)", "Value", "Measure"),
                Chart(CpuTableIndex, "CPU (ms)", "Value", "Measure"),
                Chart(ReadsTableIndex, "Logical Reads", "Value", "Measure"),
                Chart(RunsTableIndex, "Runs", "RunCount", "Outcome", ChartTypes.StackedColumn, CollectionHealthView.RunOutcomeStyles)
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Runs",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = Hidden(),
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["Name"] = new() { Alias = "Collection" },
                        ["SnapshotDate"] = new() { Alias = "Date", Description = "When the run started - or the hour, for hours where each run wasn't kept" },
                        ["Period"] = new() { Description = "Run: a single run, kept with Log Internal Performance Counters on.  Hour: the hourly rollup, for hours where each run wasn't kept." },
                        ["Outcome"] = new() { Highlighting = new CellHighlightingRuleSet("Status") { IsStatusColumn = true } },
                        ["Status"] = Hidden(),
                        ["RunCount"] = new() { Alias = "Runs", FormatString = "N0" },
                        ["FailedCount"] = new() { Alias = "Failed", FormatString = "N0", Highlighting = new CellHighlightingRuleSet("Status") { IsStatusColumn = true } },
                        ["RetriedOrPartialCount"] = new() { Alias = "Retried / Partial", FormatString = "N0" },
                        ["AvgDurationMs"] = new() { Alias = "Duration (ms)", FormatString = "N0", Description = "The run's duration, or the average for an hour" },
                        ["MaxDurationMs"] = new() { Alias = "Max Duration (ms)", FormatString = "N0" },
                        ["AvgCpuMs"] = new() { Alias = "CPU (ms)", FormatString = "N0", Description = "CPU on the monitored instance" },
                        ["MaxCpuMs"] = new() { Alias = "Max CPU (ms)", FormatString = "N0", Visible = false },
                        ["AvgLogicalReads"] = new() { Alias = "Logical Reads", FormatString = "N0" },
                        ["AvgImportMs"] = new() { Alias = "Import (ms)", FormatString = "N0" },
                        ["MaxImportMs"] = new() { Alias = "Max Import (ms)", FormatString = "N0", Visible = false },
                        ["AvgImportCpuMs"] = new() { Alias = "Import CPU (ms)", FormatString = "N0", Description = "CPU in the repository" },
                        ["AvgImportLogicalReads"] = new() { Alias = "Import Logical Reads", FormatString = "N0" },
                        ["AvgImportLagSec"] = new() { Alias = "Import Lag (sec)", FormatString = "N1", Description = "How long from the batch being sent to a destination to its import starting - near zero for a SQL destination.  The run's, or the average for an hour" },
                        ["MaxImportLagSec"] = new() { Alias = "Max Import Lag (sec)", FormatString = "N1", Visible = false }
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [DurationTableIndex] = ChartData("Duration Over Time", "Measure", "Value", "ms"),
                [CpuTableIndex] = ChartData("CPU Over Time", "Measure", "Value", "ms"),
                [ReadsTableIndex] = ChartData("Logical Reads Over Time", "Measure", "Value", "Reads"),
                [RunsTableIndex] = ChartData("Runs Over Time", "Outcome", "RunCount", "Runs")
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@Reference", ParamType = "VARCHAR" },
                    new() { ParamName = "@Step", ParamType = "VARCHAR" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@DetailMaxHours", ParamType = "INT" },
                    new() { ParamName = "@MaxRows", ParamType = "INT" }
                }
            }
        };
    }
}
