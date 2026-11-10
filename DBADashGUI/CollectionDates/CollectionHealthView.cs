using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// How well each collection is working over the selected period - success rate, failures in a row, retries and partial
    /// failures - and what it costs: duration, and CPU and logical reads on the monitored instance where the service measures
    /// them (LogInternalPerformanceCounters).  Read from the hourly rollup of collection runs (dbo.CollectionRuns_60MIN).
    ///
    /// <para>Status is from the failures in a row now and the failure rate over the period, with thresholds configured from the
    /// grid (<see cref="CollectionHealthThresholdsDialog"/>) - the same as the summary's DBA Dash Errors status
    /// (dbo.CollectionHealthStatus).  The summary rates the last 24hrs whatever period is selected here, so that rate has a
    /// column of its own, and a collection failing now is listed even with no runs in the period.</para>
    ///
    /// <para>What to do about the instances' errors, where there's something to suggest (WMI failing for several collections),
    /// is a card at the top rather than a column repeated on each collection's row.</para>
    /// </summary>
    internal class CollectionHealthView : CustomReportView
    {
        private const int RunsTableIndex = 1;
        private const int CostTableIndex = 2;
        private const int LagTableIndex = 3;

        /// <summary>
        /// Colours for the Runs charts' Outcome series.  Left to the theme palette, the series take its colours in the
        /// order they sort - Failed first - so failures came out in the palette's friendly blue and Retried / Partial in red.
        /// </summary>
        internal static Dictionary<string, ChartSeriesStyle> RunOutcomeStyles => new()
        {
            ["Failed"] = ChartSeriesStyle.FromColor(DashColors.Fail),
            ["Retried / Partial"] = ChartSeriesStyle.FromColor(DashColors.Warning),
            ["Succeeded"] = ChartSeriesStyle.FromColor(DashColors.Success)
        };

        public CollectionHealthView()
        {
            Report = ReportInstance;
            PreventReportOverwrite = true;
        }

        /// <summary>
        /// The errors logged for the collection on the row (on its instance, or every instance where the grid shows each
        /// collection), over the report's period - in the DBA Dash ErrorLog report.  Steps log their errors against the
        /// collection they follow, so a step's row shows the collection's errors.  Without retries, the errors the Errors
        /// count is of - Retries: Shown in the ErrorLog adds them.  With retries for the Retried count.
        /// </summary>
        private static SystemDrillDownLinkColumnInfo ErrorsLink(bool includeRetries) => ErrorLogView.ErrorsFor(includeRetries);

        /// <summary>
        /// The thresholds for the collection on the row - for its instance, or every instance where the grid shows each
        /// collection.  Refreshes the report once they're saved.
        /// </summary>
        private sealed class ThresholdsLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                var columns = row.DataGridView.Columns;
                var instanceID = columns.Contains("InstanceID") && row.Cells["InstanceID"].Value is int id ? id : -1;
                var instance = columns.Contains("InstanceDisplayName") ? row.Cells["InstanceDisplayName"].Value as string : null;
                var reference = row.Cells["Reference"].Value as string;
                using var frm = new CollectionHealthThresholdsDialog(instanceID, instance, reference);
                frm.ShowDialog(sender);
                if (frm.Saved && sender is IRefreshData refresh)
                {
                    refresh.RefreshData();
                }
            }
        }

        private static ColumnMetadata Hidden() => new() { Visible = false };

        /// <summary>
        /// The collection's performance over time (<see cref="CollectionPerformanceReport"/>) - for the instance on the row, or the
        /// instances selected when the grid shows each collection.  A new window, over the same date range.
        /// </summary>
        private static SystemDrillDownLinkColumnInfo PerformanceLink() => new()
        {
            ReportFactory = () => CollectionPerformanceReport.ReportInstance,
            DrillDownMode = DrillDownMode.NewWindow,
            ColumnToParameterMap = new Dictionary<string, string>
            {
                ["@Reference"] = "Reference",
                ["@Step"] = "Step",
                ["@InstanceIDs"] = "InstanceID"
            }
        };

        private const string PerformanceLinkDescription = "  Click to see the collection's performance over time.";

        private static CellHighlightingRuleSet Status() => new("Status") { IsStatusColumn = true };

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(CollectionHealthView),
            ReportName = "Collection Health",
            Description = "How often each collection succeeds, fails or needs a retry over a period, and what it costs to run.",
            SchemaName = "dbo",
            ProcedureName = "CollectionHealth_Get",
            QualifiedProcedureName = "dbo.CollectionHealth_Get",
            CanEditReport = false,
            ShowInsights = true, // What to do about the instances' errors (dbo.CollectionErrorInsight)
            ShowStatusFilter = true,
            ChartVisible = true,
            TableVisible = true,
            ChartLayoutColumns = 3,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = RunsTableIndex,
                    Title = "Runs",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.StackedColumn,
                        ChartTitle = "Runs",
                        XColumn = "SnapshotDate",
                        MetricColumn = "RunCount",
                        SeriesColumn = "Outcome",
                        SeriesStyles = RunOutcomeStyles,
                        YAxisFormat = "N0",
                        YAxisMin = 0
                    }
                },
                new()
                {
                    TableIndex = CostTableIndex,
                    Title = "Cost",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Line,
                        ChartTitle = "Time spent collecting and importing (sec)",
                        XColumn = "SnapshotDate",
                        MetricColumn = "Value",
                        SeriesColumn = "Measure",
                        YAxisFormat = "N1",
                        YAxisMin = 0
                    }
                },
                new()
                {
                    TableIndex = LagTableIndex,
                    Title = "Import Lag",
                    BindXAxisToDateRange = true,
                    Config = new ChartConfiguration
                    {
                        ChartType = ChartTypes.Line,
                        ChartTitle = "Collection to import (sec)",
                        XColumn = "SnapshotDate",
                        MetricColumn = "Value",
                        SeriesColumn = "Measure",
                        YAxisFormat = "N1",
                        YAxisMin = 0,
                        /* At least a second.  Near zero for a SQL destination, where a few milliseconds' difference would
                           otherwise fill the chart - a backlog at an S3 or folder destination is seconds or more, and still scales. */
                        YAxisMinimumMax = 1
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Collections",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = Hidden(),
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["Name"] = new() { Alias = "Collection", Description = "The collection, or a step that follows it (fetching text and plans for QueryStats, say)." + PerformanceLinkDescription, Highlighting = Status(), Link = PerformanceLink() },
                        ["Reference"] = Hidden(),
                        ["Step"] = Hidden(),
                        ["Status"] = Hidden(),
                        ["StatusReason"] = new() { Alias = "Status Reason", Description = "Why the collection is a warning or critical - failures in a row now, or the failure rate over the selected period.  The summary rates the last 24hrs instead (Failed % 24hrs), so a short period can show a collection as OK that the summary counts.  A collection failing now is listed even with no runs in the period.", Highlighting = Status() },
                        ["InstanceCount"] = new()
                        {
                            Alias = "Instances",
                            FormatString = "N0",
                            Description = "Instances the collection ran on.  Click to see each instance.",
                            RequiresDataColumn = true, // Only a row per collection has it - otherwise the link is added as a column of its own
                            Link = new SystemDrillDownLinkColumnInfo
                            {
                                ReportFactory = () => ReportInstance,
                                DrillDownMode = DrillDownMode.NewWindow,
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    ["@Reference"] = "Reference",
                                    ["@Step"] = "Step",
                                    ["@GroupByInstance"] = "DrillGroupByInstance"
                                }
                            }
                        },
                        ["FailingInstanceCount"] = new() { Alias = "Failing Instances", FormatString = "N0", Description = "Instances where the last run failed", Highlighting = Status() },
                        ["RunCount"] = new() { Alias = "Runs", FormatString = "N0", Description = PerformanceLinkDescription.Trim(), Link = PerformanceLink() },
                        ["SucceededPct"] = new() { Alias = "Succeeded %", FormatString = "0.##", Description = "Runs that got the data in - including after a retry, or with part of the collection failing.  Runs reported by an older service version have no status and aren't counted.", Highlighting = Status() },
                        ["FailedPct"] = new() { Alias = "Failed %", FormatString = "0.##", Description = "Runs over the selected period that failed to collect, or whose data couldn't be imported", Highlighting = Status() },
                        ["FailedPct24Hrs"] = new() { Alias = "Failed % 24hrs", FormatString = "0.##", Description = "Runs that failed over the period the summary rates - the last 24hrs, or since errors were acknowledged if that's sooner, from the start of the next hour.  The summary's status is from this rate and the failures in a row now, whatever period is selected here.", Highlighting = Status() },
                        ["SucceededCount"] = new() { Alias = "Succeeded", FormatString = "N0" },
                        ["WarningCount"] = new() { Alias = "Warnings", FormatString = "N0", Description = "Succeeded with a warning logged (a TableSize threshold, say)" },
                        ["RetriedCount"] = new() { Alias = "Retried", FormatString = "N0", Description = "Succeeded after one or more attempts failed.  Click to see the errors, retries included.", Link = ErrorsLink(includeRetries: true) },
                        ["PartialCount"] = new() { Alias = "Partial", FormatString = "N0", Description = "Succeeded, but part of the collection logged an error (one WMI query out of several, say)" },
                        ["FailedCount"] = new() { Alias = "Failed", FormatString = "N0", Description = "Failed to collect.  Click to see the errors.", Link = ErrorsLink(includeRetries: false) },
                        ["ImportFailedCount"] = new() { Alias = "Import Failed", FormatString = "N0", Description = "Collected, but the data couldn't be imported into the repository.  Click to see the errors.", Link = ErrorsLink(includeRetries: false) },
                        ["UnknownCount"] = new() { Alias = "No Status", FormatString = "N0", Description = "Runs reported by an older service version, which only reported a duration", Visible = false },
                        ["ConsecutiveFailures"] = new() { Alias = "Failures In A Row", FormatString = "N0", Description = "How many of the latest runs have failed in a row (the most for any instance, over all instances)", Highlighting = Status() },
                        ["LastStatus"] = Hidden(),
                        ["LastRunDate"] = new() { Alias = "Last Run" },
                        ["LastSuccessDate"] = new() { Alias = "Last Success" },
                        ["LastFailureDate"] = new() { Alias = "Last Failure", Description = "When the collection last failed - whenever that was, not just in the selected period.  Click to see the errors: every failure in a row while it's failing now, otherwise around the last failure.", Link = ErrorLogView.LastFailureErrors() },
                        ["LastDurationMs"] = new() { Alias = "Last Duration (ms)", FormatString = "N0", Visible = false },
                        ["AvgDurationMs"] = new() { Alias = "Avg Duration (ms)", FormatString = "N0", Description = "How long the collection took - the last attempt where it was retried." + PerformanceLinkDescription, Link = PerformanceLink() },
                        ["MaxDurationMs"] = new() { Alias = "Max Duration (ms)", FormatString = "N0", Description = PerformanceLinkDescription.Trim(), Link = PerformanceLink() },
                        ["TotalDurationMs"] = new() { Alias = "Total Duration (ms)", FormatString = "N0", Visible = false },
                        ["AvgCpuMs"] = new() { Alias = "Avg CPU (ms)", FormatString = "N0", Description = "CPU used on the monitored instance by the collection's own sessions.  Measured with Log Internal Performance Counters on in the service config tool.  Blank for collections that don't query SQL Server (WMI) or aren't measured." + PerformanceLinkDescription, Link = PerformanceLink() },
                        ["MaxCpuMs"] = new() { Alias = "Max CPU (ms)", FormatString = "N0" },
                        ["TotalCpuMs"] = new() { Alias = "Total CPU (ms)", FormatString = "N0", Description = "The CPU the collection cost the monitored instance over the period." + PerformanceLinkDescription, Link = PerformanceLink() },
                        ["AvgLogicalReads"] = new() { Alias = "Avg Logical Reads", FormatString = "N0", Description = "Logical reads on the monitored instance by the collection's own sessions.  Measured with Log Internal Performance Counters on." },
                        ["MaxLogicalReads"] = new() { Alias = "Max Logical Reads", FormatString = "N0", Visible = false },
                        ["TotalLogicalReads"] = new() { Alias = "Total Logical Reads", FormatString = "N0", Visible = false },
                        ["AvgImportMs"] = new() { Alias = "Avg Import (ms)", FormatString = "N0", Description = "Time spent importing the collection's data into the repository, retries included.  Measured by the service that imports it." + PerformanceLinkDescription, Link = PerformanceLink() },
                        ["MaxImportMs"] = new() { Alias = "Max Import (ms)", FormatString = "N0" },
                        ["TotalImportMs"] = new() { Alias = "Total Import (ms)", FormatString = "N0", Visible = false },
                        ["AvgImportCpuMs"] = new() { Alias = "Avg Import CPU (ms)", FormatString = "N0", Description = "CPU the import used in the repository.  Measured with Log Internal Performance Counters on for the service that collected the data." },
                        ["TotalImportCpuMs"] = new() { Alias = "Total Import CPU (ms)", FormatString = "N0", Description = "The CPU importing the collection cost the repository over the period" },
                        ["AvgImportLogicalReads"] = new() { Alias = "Avg Import Logical Reads", FormatString = "N0", Visible = false },
                        ["TotalImportLogicalReads"] = new() { Alias = "Total Import Logical Reads", FormatString = "N0", Visible = false },
                        ["AvgImportLagSec"] = new() { Alias = "Avg Import Lag (sec)", FormatString = "N1", Description = "How long the batch the collection came in waited from being sent to a destination - once everything in it was collected - to its import starting.  Near zero for a SQL destination; a backlog at an S3 or folder destination shows here.  The importer's clock against the collector's.  Also a counter (Collection Import Lag (ms)) that can be alerted on." + PerformanceLinkDescription, Link = PerformanceLink() },
                        ["MaxImportLagSec"] = new() { Alias = "Max Import Lag (sec)", FormatString = "N1", Description = "The longest the collection's data waited at the destination to be imported" },
                        ["ErrorCount"] = new() { Alias = "Errors", FormatString = "N0", Description = "Errors logged for the collection over the period, retries excluded.  Click to see them - set Retries to Shown in the ErrorLog to add the errors from attempts that were retried.", Link = ErrorsLink(includeRetries: false) },
                        ["ThresholdLevel"] = new() { Alias = "Thresholds", Description = "Where the collection's health thresholds come from - Default, All, Collection (every instance), Instance (all collections) or Instance.  Over all the instances, Varies by instance where some instances have thresholds of their own.  Click to configure them.", Link = new ThresholdsLink() },
                        ["ErrorsFrom"] = Hidden(),
                        ["ErrorsTo"] = Hidden(),
                        ["DrillGroupByInstance"] = Hidden()
                    }
                }.SetDisplayIndexBasedOnColumnOrder(),
                [RunsTableIndex] = new CustomReportResult
                {
                    ResultName = "Runs Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Date" },
                        ["Outcome"] = new(),
                        ["RunCount"] = new() { Alias = "Runs", FormatString = "N0" }
                    }
                },
                [CostTableIndex] = new CustomReportResult
                {
                    ResultName = "Cost Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Date" },
                        ["Measure"] = new(),
                        ["Value"] = new() { Alias = "Seconds", FormatString = "N1" }
                    }
                },
                [LagTableIndex] = new CustomReportResult
                {
                    ResultName = "Import Lag Over Time",
                    ShowWithAllResults = false, // Chart data
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["SnapshotDate"] = new() { Alias = "Date Collected" },
                        ["Measure"] = new(),
                        ["Value"] = new() { Alias = "Seconds", FormatString = "N1" }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@FromDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@ToDate", ParamType = "DATETIME2" },
                    new() { ParamName = "@DateGroupingMin", ParamType = "INT" },
                    new() { ParamName = "@DetailMaxHours", ParamType = "INT" },
                    new() { ParamName = "@GroupByInstance", ParamType = "BIT" },
                    new() { ParamName = "@IncludeSteps", ParamType = "BIT" },
                    new() { ParamName = "@Reference", ParamType = "VARCHAR" },
                    new() { ParamName = "@Step", ParamType = "VARCHAR" },
                    new() { ParamName = "@IncludeCritical", ParamType = "BIT" },
                    new() { ParamName = "@IncludeWarning", ParamType = "BIT" },
                    new() { ParamName = "@IncludeNA", ParamType = "BIT" },
                    new() { ParamName = "@IncludeOK", ParamType = "BIT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@GroupByInstance",
                    Name = "Show",
                    Description = "Row per collection per instance: each collection on each instance, with its status and last run there.\n" +
                                  "Row per collection: each collection once, over all the instances selected - how many it runs on and fails on, and the worst status.",
                    PickerItems = new Dictionary<object, string>
                    {
                        [true] = "Row per collection per instance",
                        [false] = "Row per collection"
                    },
                    DefaultValue = true,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
                new()
                {
                    ParameterName = "@IncludeSteps",
                    Name = "Steps",
                    Description = "Some collections are followed by steps of their own - fetching the text, plans or live profiles for RunningQueries and QueryStats, say.\n" +
                                  "Row per collection + step detail: a row for each step too, so a slow or failing step shows as itself.\n" +
                                  "Row per collection: the collections only.",
                    PickerItems = new Dictionary<object, string>
                    {
                        [false] = "Row per collection",
                        [true] = "Row per collection + step detail"
                    },
                    DefaultValue = true,
                    MenuBar = true,
                    DataType = typeof(bool)
                }
            }
        };
    }
}
