using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.AgentJobs
{
    internal class RunningJobsView : CustomReportView
    {
        private const string EstimatedCompletionColumn = "EstimatedCompletionTime";

        public RunningJobsView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
        }

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                grid.CellFormatting -= Grid_CellFormatting;
                grid.CellFormatting += Grid_CellFormatting;
            }
        }

        // Shows the estimated completion date with a humanized "+/- time from now" suffix and colors it
        // based on whether the estimate has already passed - custom text composition that doesn't fit the
        // declarative ColumnMetadata/CellHighlightingRuleSet model, so it's applied here instead.
        private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView grid || e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != EstimatedCompletionColumn) return;
            if (e.Value is not DateTime estimatedCompletion) return;

            var ts = estimatedCompletion.Subtract(DateTime.Now);
            e.Value = estimatedCompletion + " (" + (ts.Ticks > 0 ? "+" : "-") + ts.Humanize(2) + ")";
            e.CellStyle.SetStatusColor(ts.Ticks > 0 ? DBADashStatusEnum.OK : DBADashStatusEnum.Warning);
            e.FormattingApplied = true;
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class RunningTimeLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.Cells["start_execution_date_utc"].Value.DBNullToNull() is not DateTime start) return;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var jobId = (Guid)row.Cells["job_id"].Value;
                var frm = new RunningQueriesViewer
                {
                    SnapshotDateFrom = start.ToUniversalTime(),
                    SnapshotDateTo = DateTime.UtcNow,
                    InstanceID = instanceId,
                    JobId = jobId
                };
                frm.ShowSingleInstance();
            }
        }

        #endregion Custom links

        #region Report definition

        private static CellHighlightingRuleSet StatusHighlight(string statusColumn) =>
            new(statusColumn) { IsStatusColumn = true };

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(RunningJobsView),
            ReportName = "Running Jobs",
            SchemaName = "dbo",
            ProcedureName = "RunningJobsReport_Get",
            QualifiedProcedureName = "dbo.RunningJobsReport_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.RunningJobs.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Running Jobs",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["job_name"] = new()
                        {
                            Alias = "Job Name",
                            Link = new SystemDrillDownLinkColumnInfo
                            {
                                ReportFactory = () => JobHistoryReport.Instance,
                                DrillDownMode = DrillDownMode.NewWindow,
                                ColumnToParameterMap = new Dictionary<string, string>
                                {
                                    { "@InstanceID", "InstanceID" },
                                    { "@JobID", "job_id" },
                                },
                            },
                            Description = "View job execution history"
                        },
                        ["current_execution_status_description"] = new() { Alias = "Status", Highlighting = StatusHighlight("ExecutionStatus") },
                        ["current_execution_step"] = new() { Alias = "Current Step", Description = "The step currently being executed" },
                        ["current_execution_step_id"] = new() { Alias = "Current Step ID", Visible = false, Description = "The step currently being executed" },
                        ["current_execution_step_name"] = new() { Alias = "Current Step Name", Visible = false, Description = "The step currently being executed" },
                        ["current_retry_attempt"] = new() { Alias = "Retry Attempt", Highlighting = StatusHighlight("RetryStatus") },
                        ["last_executed_step"] = new() { Alias = "Last Step", Visible = false, Description = "Previous step executed" },
                        ["last_executed_step_id"] = new() { Alias = "Last Step ID", Visible = false, Description = "Previous step executed" },
                        ["last_executed_step_name"] = new() { Alias = "Last Step Name", Visible = false, Description = "Previous step executed" },
                        ["LastStepStartDateUtc"] = new() { Alias = "Last Step Start", Visible = false, Description = "Date the previous step started" },
                        ["LastStepFinishDateUtc"] = new() { Alias = "Last Step Finish", Visible = false, Description = "Date the previous step finished" },
                        ["LastStepDuration"] = new() { Alias = "Last Step Duration", Visible = false },
                        ["LastStepRunDurationSec"] = new() { Alias = "Last Step Duration (sec)", Visible = false, FormatString = "N0" },
                        ["start_execution_date_utc"] = new() { Alias = "Start Time", Description = "Time the job started executing" },
                        ["RunningTime"] = new()
                        {
                            Alias = "Running Time",
                            Link = new RunningTimeLink(),
                            Highlighting = StatusHighlight("RunningTimeStatus"),
                            Description = "How long the job has been running at the time the data was captured\n\nGreen: < Avg Duration + 10%\nWarning: < Max Duration + 10%\nCritical: >=Max Duration + 10%"
                        },
                        ["AvgDuration"] = new() { Alias = "Avg Duration", Description = "Average running time for the job over the last 30 days" },
                        ["MaxDuration"] = new() { Alias = "Max Duration", Description = "Max running time for the job over the last 30 days" },
                        ["CurrentStepDuration"] = new() { Alias = "Current Step Duration" },
                        [EstimatedCompletionColumn] = new() { Alias = "Estimated Completion", Description = "Estimated time the job will be completed based on average running time\n\nWarning=Estimated date has passed" },
                        ["RunningTimeSec"] = new() { Alias = "Running Time (sec)", Visible = false, FormatString = "N0" },
                        ["AvgRunDurationSec"] = new() { Alias = "Avg Duration (sec)", Visible = false, FormatString = "N0", Description = "Average running time for the job over the last 30 days" },
                        ["MaxRunDurationSec"] = new() { Alias = "Max Duration (sec)", Visible = false, FormatString = "N0", Description = "Max running time for the job over the last 30 days" },
                        ["CurrentStepRunDurationSec"] = new() { Alias = "Step Duration (sec)", Visible = false, FormatString = "N0", Description = "Duration job has been on the current step at the time the data was collected" },
                        ["TimeSinceSnapshot"] = new() { Alias = "Time Since Snapshot", Highlighting = StatusHighlight("SnapshotAgeStatus"), Description = "Time since this data was collected" },
                        ["SnapshotDate"] = new() { Alias = "Snapshot Date", Highlighting = StatusHighlight("SnapshotAgeStatus"), Description = "Time the data was collected" },
                        // Hidden technical/status columns
                        ["InstanceID"] = Hidden(),
                        ["job_id"] = Hidden(),
                        ["current_execution_status"] = Hidden(),
                        ["ExecutionStatus"] = Hidden(),
                        ["RunningTimeStatus"] = Hidden(),
                        ["RetryStatus"] = Hidden(),
                        ["SnapshotAgeStatus"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@MinimumDurationSec", ParamType = "INT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@MinimumDurationSec",
                    Name = "Minimum Duration",
                    DefaultValue = 60,
                    DataType = typeof(int),
                    PickerItems = new Dictionary<object, string>
                    {
                        { -1, "None" },
                        { 60, "1min" },
                        { 600, "10min" },
                        { 1800, "30min" },
                        { 3600, "1hr" },
                        { 7200, "2hrs" },
                        { 14400, "4hrs" },
                        { 21600, "6hrs" },
                        { 43200, "12hrs" },
                        { 86400, "1 day" },
                    },
                    MenuBar = true
                }
            }
        };

        #endregion Report definition
    }
}
