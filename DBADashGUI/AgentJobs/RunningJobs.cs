﻿using DBADash;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Performance;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class RunningJobs : UserControl, IRefreshData, ISetContext, ISetStatus
    {
        private DBADashContext context;

        private string PersistedFilter;
        private string PersistedSort;

        public RunningJobs()
        {
            InitializeComponent();
            dgvRunningJobs.RegisterClearFilter(tsClearFilter);
            AddColsToDGV();
            AgentJobsControl.AddContextMenuItems(dgvRunningJobs, this);
        }

        private void AddColsToDGV()
        {
            dgvRunningJobs.AutoGenerateColumns = false;
            dgvRunningJobs.Columns.Clear();
            dgvRunningJobs.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "colInstanceID", HeaderText = "Instance ID", DataPropertyName = "InstanceID", Visible = false, Width = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName" },
                new DataGridViewTextBoxColumn() { HeaderText = "Job ID", DataPropertyName = "job_id", Visible = false, Width = 260 },
                new DataGridViewLinkColumn() { Name = "colJobName", HeaderText = "Job Name", DataPropertyName = "job_name" },
                new DataGridViewTextBoxColumn() { Name = "colStatus", HeaderText = "Status", DataPropertyName = "current_execution_status_description", Width = 130 },
                new DataGridViewTextBoxColumn() { HeaderText = "Current Step ID", DataPropertyName = "current_execution_step_id", Visible = false, ToolTipText = "Current step been executed", Width = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Current Step Name", DataPropertyName = "current_execution_step_name", Visible = false, ToolTipText = "Current step been executed" },
                new DataGridViewTextBoxColumn() { HeaderText = "Current Step", DataPropertyName = "current_execution_step", ToolTipText = "Current step been executed" },
                new DataGridViewTextBoxColumn() { Name = "colRetryAttempt", HeaderText = "Retry Attempt", DataPropertyName = "current_retry_attempt", Width = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step ID", DataPropertyName = "last_executed_step_id", Visible = false, ToolTipText = "Previous step executed", Width = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step Name", DataPropertyName = "last_executed_step_name", Visible = false, ToolTipText = "Previous step executed" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step", DataPropertyName = "last_executed_step", Visible = false, ToolTipText = "Previous step executed" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step Start", DataPropertyName = "LastStepStartDate", Visible = false, ToolTipText = "Date the previous step started", Width = 110 },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step Finish", DataPropertyName = "LastStepFinishDate", Visible = false, ToolTipText = "Date the previous step finished", Width = 110 },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Step Duration", DataPropertyName = "LastStepDuration", Width = 100, Visible = false },
                new DataGridViewTextBoxColumn() { Name = "colStartTime", HeaderText = "Start Time", DataPropertyName = "start_execution_date", ToolTipText = "Time the job started executing", Width = 110 },
                new DataGridViewLinkColumn() { Name = "colRunningTime", HeaderText = "Running Time", DataPropertyName = "RunningTime", Width = 100, ToolTipText = "How long the job has been running at the time the data was captured\n\nGreen: < Avg Duration + 10%\nWarning: < Max Duration + 10%\nCritical: >=Max Duration + 10%" },
                new DataGridViewTextBoxColumn() { HeaderText = "Avg Duration", DataPropertyName = "AvgDuration", Width = 100, ToolTipText = "Average running time for the job over the last 30 days" },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Duration", DataPropertyName = "MaxDuration", Width = 100, ToolTipText = "Max running time for the job over the last 30 days" },
                new DataGridViewTextBoxColumn() { HeaderText = "Current Step Duration", DataPropertyName = "CurrentStepDuration", Width = 100 },
                new DataGridViewTextBoxColumn() { Name = "colEstimatedCompletion", HeaderText = "Estimated Completion", DataPropertyName = "EstimatedCompletionTime", Width = 270, ToolTipText = "Estimated time the job will be completed based on average running time\n\nWarning=Estimated date has passed" },
                new DataGridViewTextBoxColumn() { HeaderText = "Running Time (sec)", DataPropertyName = "RunningTimeSec", Visible = false, DefaultCellStyle = Common.DataGridViewNumericCellStyle, Width = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Avg Duration (sec)", DataPropertyName = "AvgRunDurationSec", Visible = false, DefaultCellStyle = Common.DataGridViewNumericCellStyle, Width = 60, ToolTipText = "Average running time for the job over the last 30 days" },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Duration (sec)", DataPropertyName = "MaxRunDurationSec", Visible = false, DefaultCellStyle = Common.DataGridViewNumericCellStyle, Width = 60, ToolTipText = "Max running time for the job over the last 30 days" },
                new DataGridViewTextBoxColumn() { HeaderText = "Step Duration (sec)", DataPropertyName = "CurrentStepDurationSec", Visible = false, DefaultCellStyle = Common.DataGridViewNumericCellStyle, Width = 60, ToolTipText = "Duration job has been on the current step at the time the data was collected" },
                new DataGridViewTextBoxColumn() { Name = "colTimeSinceSnapshot", HeaderText = "Time Since Snapshot", DataPropertyName = "TimeSinceSnapshot", ToolTipText = "Time since this data was collected", Width = 100 },
                new DataGridViewTextBoxColumn() { Name = "colSnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", ToolTipText = "Time the data was collected", Width = 110 }
            );
            dgvRunningJobs.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }
            var dt = GetRunningJobs();
            dgvRunningJobs.DataSource = new DataView(dt, PersistedFilter, PersistedSort, DataViewRowState.CurrentRows);
            dgvRunningJobs.AutoResizeColumnsWithMaxColumnWidth();
            PersistedFilter = null;
            PersistedSort = null;
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            tsStatus.InvokeSetStatus(message, tooltip, color);
        }

        private DataTable GetRunningJobs()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningJobs_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", context.RegularInstanceIDs.AsDataTable());
            cmd.Parameters.AddWithValue("MinimumDurationSec", MinimumDuration);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dt.Columns["LastStepFinishDateUtc"]!.ColumnName = "LastStepFinishDate";
            dt.Columns["LastStepStartDateUtc"]!.ColumnName = "LastStepStartDate";
            dt.Columns["start_execution_date_utc"]!.ColumnName = "start_execution_date";

            return dt;
        }

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;
            tsStatus.InvokeSetStatus(string.Empty, string.Empty, DashColors.Information);
            tsTrigger.Visible = context.InstanceID > 0 && context.CanMessage;
            RefreshData();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            PersistedFilter = dgvRunningJobs.RowFilter;
            PersistedSort = dgvRunningJobs.SortString;
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgvRunningJobs.CopyGrid();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvRunningJobs.ExportToExcel();
        }

        private void DgvRunningJobs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvRunningJobs.Rows[idx].DataBoundItem;
                var snapshotStatus = row["SnapshotAgeStatus"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.Critical : (DBADashStatus.DBADashStatusEnum)(int)row["SnapshotAgeStatus"];
                var retryStatus = dgvRunningJobs.Rows[idx].Cells["colRetryAttempt"].Value == DBNull.Value
                    ? DBADashStatus.DBADashStatusEnum.NA
                    : Convert.ToInt32(dgvRunningJobs.Rows[idx].Cells["colRetryAttempt"].Value) > 0
                    ? DBADashStatus.DBADashStatusEnum.Warning
                    : DBADashStatus.DBADashStatusEnum.OK;
                var avgRunDuration = (int?)(row["AvgRunDurationSec"].DBNullToNull());
                var maxRunDuration = (int?)(row["MaxRunDurationSec"].DBNullToNull());
                var runningTime = (int?)row["RunningTimeSec"].DBNullToNull();
                var executionStatus = row["current_execution_status"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : (int)row["current_execution_status"] == 1 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning;

                var runningTimeStatus = runningTime switch
                {
                    null => DBADashStatus.DBADashStatusEnum.NA,
                    _ when runningTime < (avgRunDuration * 1.10) => DBADashStatus.DBADashStatusEnum.OK,
                    _ when runningTime < (maxRunDuration * 1.10) => DBADashStatus.DBADashStatusEnum.Warning,
                    _ when avgRunDuration == null || maxRunDuration == null => DBADashStatus.DBADashStatusEnum.NA,
                    _ => DBADashStatus.DBADashStatusEnum.Critical
                };
                dgvRunningJobs.Rows[idx].Cells["colStatus"].SetStatusColor(executionStatus);
                dgvRunningJobs.Rows[idx].Cells["colRunningTime"].SetStatusColor(runningTimeStatus);
                dgvRunningJobs.Rows[idx].Cells["colSnapshotDate"].SetStatusColor(snapshotStatus);
                dgvRunningJobs.Rows[idx].Cells["colTimeSinceSnapshot"].SetStatusColor(snapshotStatus);
                dgvRunningJobs.Rows[idx].Cells["colRetryAttempt"].SetStatusColor(retryStatus);
            }
        }

        private void DgvRunningJobs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvRunningJobs.Columns["colEstimatedCompletion"]!.Index) return;
            if (e.Value == DBNull.Value || e.Value == null)
            {
                return;
            }
            var estimatedCompletion = (DateTime)e.Value;
            var ts = estimatedCompletion.Subtract(DateTime.Now);

            e.Value = estimatedCompletion + " (" + (ts.Ticks > 0 ? "+" : "-") + ts.Humanize(2) + ")";
            e.CellStyle.SetStatusColor(ts.Ticks > 0 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
            e.FormattingApplied = true;
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvRunningJobs.PromptColumnSelection();
        }

        private void SetMinimumDuration(object sender, EventArgs e)
        {
            MinimumDuration = int.Parse(((ToolStripMenuItem)sender).Tag!.ToString()!);
            RefreshData();
        }

        private int _minimumDuration = 60;

        private static JobStatusAndHistory JobHistoryForm;
        private static RunningQueriesViewer RunningViewer;

        private void DgvRunningJobs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvRunningJobs.Rows[e.RowIndex].DataBoundItem;
            var instanceId = (int)row["InstanceID"];
            var jobId = (Guid)row["job_id"];
            var jobName = (string)row["job_name"];
            if (e.ColumnIndex == dgvRunningJobs.Columns["colJobName"]!.Index)
            {
                JobHistoryForm?.Close();
                JobHistoryForm = new();
                var jobContext = new DBADashContext()
                {
                    InstanceID = instanceId,
                    JobID = jobId,
                    RegularInstanceIDsWithHidden = new HashSet<int>() { instanceId },
                    JobStepID = -1
                };
                JobHistoryForm.ShowSteps = true;
                JobHistoryForm.FormClosed += delegate { JobHistoryForm = null; };
                JobHistoryForm.Show();
                JobHistoryForm.SetContext(jobContext);
            }
            else if (e.ColumnIndex == dgvRunningJobs.Columns["colRunningTime"]!.Index)
            {
                var from = ((DateTime)row["start_execution_date"]).ToUniversalTime();
                var to = DateTime.UtcNow;
                RunningViewer?.Close();
                RunningViewer = new()
                { SnapshotDateFrom = from, SnapshotDateTo = to, InstanceID = instanceId, JobId = jobId };

                RunningViewer.FormClosed += delegate { RunningViewer = null; };
                RunningViewer.Show();
            }
        }

        private async void tsTrigger_Click(object sender, EventArgs e)
        {
            await TriggerCollection(context);
        }

        private async Task TriggerCollection(DBADashContext triggerContext)
        {
            var types = new List<CollectionType>()
            {
                CollectionType.RunningJobs
            };
            if (triggerContext.CollectAgentID == null || triggerContext.ImportAgentID == null) return;
            tsStatus.InvokeSetStatus("Message Sent", string.Empty, DashColors.Information);
            await CollectionMessaging.TriggerCollection(triggerContext.ConnectionID, types, triggerContext.CollectAgentID.Value, triggerContext.ImportAgentID.Value, this);
        }

        public int MinimumDuration
        {
            get => _minimumDuration;
            set
            {
                _minimumDuration = value;
                minimumDurationToolStripMenuItem.DropDownItems.Cast<ToolStripMenuItem>().ToList().ForEach(x => x.Checked = int.Parse(x.Tag?.ToString()!) == value);
            }
        }
    }
}