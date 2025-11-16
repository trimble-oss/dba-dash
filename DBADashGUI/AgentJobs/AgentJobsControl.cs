using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SortOrder = System.Windows.Forms.SortOrder;

namespace DBADashGUI.AgentJobs
{
    public partial class AgentJobsControl : UserControl, ISetContext, ISetStatus
    {
        private List<int> InstanceIDs;
        private int? instance_id;
        private int instanceId;
        private Guid jobID;
        private int StepID = -1;
        private DBADashContext context;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeCritical { get => statusFilterToolStrip1.Critical; set => statusFilterToolStrip1.Critical = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeWarning { get => statusFilterToolStrip1.Warning; set => statusFilterToolStrip1.Warning = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeNA { get => statusFilterToolStrip1.NA; set => statusFilterToolStrip1.NA = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeOK { get => statusFilterToolStrip1.OK; set => statusFilterToolStrip1.OK = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IncludeAcknowledged { get => statusFilterToolStrip1.Acknowledged; set => statusFilterToolStrip1.Acknowledged = value; }

        private bool DoAutoSize = true;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowSteps { get => showJobStepsToolStripMenuItem.Checked; set => showJobStepsToolStripMenuItem.Checked = value; }

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;

            StepID = _context.JobStepID;
            IncludeNA = _context.RegularInstanceIDs.Count == 1;
            IncludeOK = _context.RegularInstanceIDs.Count == 1;
            IncludeWarning = true;
            IncludeCritical = true;
            IncludeAcknowledged = true;
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            tsTriggerCollection.Visible = _context.CanMessage;
            SetStatus("", "", DashColors.Information);
            SetSplitterDistance();
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(RefreshData);
                    return;
                }
                dgvJobs.Columns[0].Frozen = Common.FreezeKeyColumn;
                dgvJobs.Columns[1].Frozen = Common.FreezeKeyColumn;
                failedOnlyToolStripMenuItem.Checked = false;
                splitContainer1.Panel2Collapsed = true;
                dgvJobHistory.DataSource = null;
                if (StepID > 0 && InstanceIDs.Count == 1)
                {
                    tsJobs.Visible = false;
                    dgvJobs.Visible = false;
                    jobStep1.Visible = true;
                    jobStep1.JobID = context.JobID;
                    jobID = context.JobID;
                    instanceId = InstanceIDs[0];
                    jobStep1.InstanceID = InstanceIDs[0];
                    jobStep1.StepID = StepID;
                    jobStep1.RefreshData();
                    ShowHistory();
                }
                else
                {
                    tsJobs.Visible = true;
                    dgvJobs.Visible = true;
                    jobStep1.Visible = false;
                    var dt = GetJobs();
                    dgvJobs.AutoGenerateColumns = false;
                    var sort = "";
                    if (dgvJobs.SortedColumn != null)
                    {
                        sort = dgvJobs.SortedColumn.DataPropertyName +
                               (dgvJobs.SortOrder == SortOrder.Descending ? " DESC" : " ASC");
                    }

                    dgvJobs.DataSource = new DataView(dt, "", sort, DataViewRowState.CurrentRows);
                    if (DoAutoSize) // AutoResize on first refresh only.  Persist user column widths after that.
                    {
                        dgvJobs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                        DoAutoSize = false;
                    }

                    configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
                    if (context.JobID != Guid.Empty && dt.Rows.Count == 1)
                    {
                        ShowHistory(dt.Rows[0], null);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private DataTable GetJobs()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.AgentJobs_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
            cmd.Parameters.AddWithValue("ShowHidden", context.RegularInstanceIDs.Count == 1 || Common.ShowHidden);
            cmd.Parameters.AddGuidIfNotEmpty("JobID", context.JobID);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public AgentJobsControl()
        {
            InitializeComponent();
            dgvJobs.RegisterClearFilter(tsClearFilter);
            dgvJobHistory.RegisterClearFilter(tsClearFilterHistory);
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, Guid.Empty, this);
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], Guid.Empty, this);
            }
        }

        private static void ConfigureThresholds(int InstanceID, Guid _jobID, IRefreshData control)
        {
            var frm = new AgentJobThresholdsConfig
            {
                InstanceID = InstanceID,
                JobID = _jobID,
                connectionString = Common.ConnectionString
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                control.RefreshData();
            }
        }

        private void DgvJobs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvJobs.Rows[e.RowIndex].DataBoundItem;
            if (dgvJobs.Columns[e.ColumnIndex].HeaderText == "Configure")
            {
                ConfigureThresholds(GetInstanceID(row), GetJobId(row), this);
            }
            else if (dgvJobs.Columns[e.ColumnIndex] == colHistory)
            {
                ShowHistory(row.Row, false);
            }
            else if (e.ColumnIndex == Acknowledge.Index)
            {
                AcknowledgeRow(row, this);
            }
            else if (e.ColumnIndex == name.Index)
            {
                ShowJobInfoForm(row);
            }
        }

        private static void AcknowledgeRow(DataRowView row, IRefreshData control)
        {
            try
            {
                var clear = (DBADashStatus.DBADashStatusEnum)row["JobStatus"] ==
                            DBADashStatus.DBADashStatusEnum.Acknowledged;
                AcknowledgeJobErrors(GetInstanceID(row), GetJobId(row), clear);
                control.RefreshData();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        public void ShowHistory(DataRow row, bool? failedOnly)
        {
            failedOnlyToolStripMenuItem.Checked = failedOnly ?? failedOnlyToolStripMenuItem.Checked;
            if (row["job_id"] != DBNull.Value)
            {
                instanceId = (int)row["InstanceID"];
                jobID = (Guid)row["job_id"];
                tsJobName.Text = (string)row["Instance"] + " | " + (string)row["name"];
                instance_id = null;
                ShowHistory();
            }
            else
            {
                MessageBox.Show("job_id IS NULL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowHistory()
        {
            int? stepID = StepID;
            if (StepID < 0)
            {
                stepID = showJobStepsToolStripMenuItem.Checked ? null : 0;
            }
            stepID = instance_id == null ? stepID : null;
            colStepName.Visible = stepID != 0;
            colStepID.Visible = stepID != 0;
            tsFilter.Visible = instance_id == null;
            tsBack.Visible = instance_id != null;
            colViewSteps.Visible = instance_id == null && stepID == 0;
            splitContainer1.Panel2Collapsed = false;
            dgvJobHistory.AutoGenerateColumns = false;
            var dtHistory = GetJobHistory(instanceId, jobID, stepID, instance_id, failedOnlyToolStripMenuItem.Checked);
            dgvJobHistory.DataSource = new DataView(dtHistory);
            dgvJobHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private static DataTable GetJobHistory(int InstanceID, Guid JobID, int? StepID = 0, int? instance_id = null, bool failedOnly = false)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.JobHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using SqlDataAdapter da = new(cmd);
            var dt = new DataTable();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("JobID", JobID);
            cmd.Parameters.AddWithNullableValue("StepID", StepID);
            cmd.Parameters.AddWithNullableValue("instance_id", instance_id);
            cmd.Parameters.AddWithValue("FailedOnly", failedOnly);
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void DgvJobs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvJobs.Rows[idx].DataBoundItem;
                var jobStatus = (DBADashStatus.DBADashStatusEnum)row["JobStatus"];
                var lastFailStatus = (DBADashStatus.DBADashStatusEnum)row["LastFailStatus"];
                var timeSinceLastStatus = (DBADashStatus.DBADashStatusEnum)row["TimeSinceLastFailureStatus"];
                var timeSinceLastSucceededStatus = (DBADashStatus.DBADashStatusEnum)row["TimeSinceLastSucceededStatus"];
                var failCount24HrsStatus = (DBADashStatus.DBADashStatusEnum)row["FailCount24HrsStatus"];
                var failCount7DaysStatus = (DBADashStatus.DBADashStatusEnum)row["FailCount7DaysStatus"];
                var stepFailCount24HrsStatus = (DBADashStatus.DBADashStatusEnum)row["JobStepFail24HrsStatus"];
                var stepFailCount7DaysStatus = (DBADashStatus.DBADashStatusEnum)row["JobStepFail7DaysStatus"];
                dgvJobs.Rows[idx].Cells["name"].SetStatusColor(jobStatus);
                dgvJobs.Rows[idx].Cells["IsLastFail"].SetStatusColor(lastFailStatus);
                dgvJobs.Rows[idx].Cells["LastFail"].SetStatusColor(lastFailStatus);
                dgvJobs.Rows[idx].Cells["TimeSinceLastFail"].SetStatusColor(timeSinceLastStatus);
                dgvJobs.Rows[idx].Cells["TimeSinceLastSucceeded"].SetStatusColor(timeSinceLastSucceededStatus);
                dgvJobs.Rows[idx].Cells["FailCount24Hrs"].SetStatusColor(failCount24HrsStatus);
                dgvJobs.Rows[idx].Cells["FailCount7Days"].SetStatusColor(failCount7DaysStatus);
                dgvJobs.Rows[idx].Cells["JobStepFails24Hrs"].SetStatusColor(stepFailCount24HrsStatus);
                dgvJobs.Rows[idx].Cells["JobStepFails7Days"].SetStatusColor(stepFailCount7DaysStatus);
                dgvJobs.Rows[idx].Cells["Configure"].Style.Font = (string)row["ConfiguredLevel"] == "Job" ? new Font(dgvJobs.Font, FontStyle.Bold) : new Font(dgvJobs.Font, FontStyle.Regular);
                dgvJobs.Rows[idx].Cells["Acknowledge"].Value = jobStatus switch
                {
                    DBADashStatus.DBADashStatusEnum.Critical or DBADashStatus.DBADashStatusEnum.Warning => "Acknowledge",
                    DBADashStatus.DBADashStatusEnum.Acknowledged => "Clear",
                    _ => "",
                };
            }
        }

        private static void AcknowledgeJobErrors(int InstanceID, Guid JobID, bool clear)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.JobErrorAck", cn) { CommandType = CommandType.StoredProcedure };
            using SqlDataAdapter da = new(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddGuidIfNotEmpty("job_id", JobID);
            cmd.Parameters.AddWithValue("Clear", clear);
            cmd.ExecuteNonQuery();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            colHistory.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvJobs);
            Configure.Visible = true;
            colHistory.Visible = true;
        }

        private RunningQueriesViewer RunningViewer;

        private void DgvJobHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvJobHistory.Rows[e.RowIndex].DataBoundItem;
            if (e.ColumnIndex == colViewSteps.Index)
            {
                instance_id = (int)row["instance_id"];
                ShowHistory();
            }
            else if (e.ColumnIndex == colMessage.Index)
            {
                Convert.ToString(row["Message"]).OpenAsTextFile();
            }
            else if (e.ColumnIndex == colRunDuration.Index || e.ColumnIndex == colRunDurationSec.Index)
            {
                var from = ((DateTime)dgvJobHistory.Rows[e.RowIndex].Cells[colRunDateTime.Index].Value).ToUniversalTime();
                var to = from.AddSeconds(
                    Convert.ToDouble(dgvJobHistory.Rows[e.RowIndex].Cells[colRunDurationSec.Index].Value));
                var jobId = (Guid)row["job_id"];
                var id = (int)row["InstanceID"];
                RunningViewer?.Close();
                RunningViewer = new() { SnapshotDateFrom = from, SnapshotDateTo = to, InstanceID = id, JobId = jobId };

                RunningViewer.FormClosed += delegate { RunningViewer = null; };
                RunningViewer.Show();
            }
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            colViewSteps.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvJobHistory);
            colViewSteps.Visible = true;
        }

        private void ShowJobStepsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHistory();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            instance_id = null;
            ShowHistory();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            ShowHistory();
        }

        private void FailedOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHistory();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            colHistory.Visible = false;
            dgvJobs.ExportToExcel();
            Configure.Visible = true;
            colHistory.Visible = true;
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            colViewSteps.Visible = false;
            dgvJobHistory.ExportToExcel();
            colViewSteps.Visible = true;
        }

        private void AgentJobsControl_Load(object sender, EventArgs e)
        {
            dgvJobHistory.ApplyTheme();
            dgvJobs.ApplyTheme();
            AddContextMenuItems(dgvJobs, this);
        }

        #region Shared Context Menu

        public static void AddContextMenuItems(DBADashDataGridView grid, IRefreshData control)
        {
            grid.CellContextMenu ??= new ContextMenuStrip();
            var strip = grid.CellContextMenu;
            var tsStartJob = new ToolStripMenuItem("Start/Stop Job", Properties.Resources.ProjectSystemModelRefresh_16x);
            var tsConfig = new ToolStripMenuItem("Configure Thresholds", Properties.Resources.SettingsOutline_16x);
            var tsConfigJob = new ToolStripMenuItem("Job Level", Properties.Resources.TableScript_16x);
            var tsConfigInstance = new ToolStripMenuItem("Instance Level", Properties.Resources.Database_16x);
            var tsConfigRoot = new ToolStripMenuItem("Root Level", Properties.Resources.StrongHierarchy_16x);
            tsConfig.DropDownItems.AddRange(new ToolStripItem[] { tsConfigJob, tsConfigInstance, tsConfigRoot });
            var tsJobInfo = new ToolStripMenuItem("Job Info", Properties.Resources.Information_blue_6227_16x16);
            var tsJobHistory = new ToolStripMenuItem("Show History", Properties.Resources.Time_16x);
            var tsAcknowledge = new ToolStripMenuItem("Acknowledge", Properties.Resources.Tick_Blue_32x32_72);
            var lblJobName = new ToolStripLabel();
            lblJobName.Font = new Font(lblJobName.Font, FontStyle.Italic);
            tsJobHistory.Visible = control is AgentJobsControl;
            tsAcknowledge.Visible = control is AgentJobsControl;
            var tsJobActions = new ToolStripMenuItem("Agent Job Actions", Properties.Resources.MonthCalendar_16x);
            tsJobActions.DropDownItems.AddRange(new ToolStripItem[]
                {
                    lblJobName,
                    new ToolStripSeparator(),
                    tsJobInfo,
                    tsJobHistory,
                    tsAcknowledge,
                    tsConfig,
                    tsStartJob
                }
                );

            tsStartJob.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                StartJob(row);
            };
            tsConfigJob.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ConfigureThresholds(GetInstanceID(row), GetJobId(row), control);
            };
            tsConfigInstance.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ConfigureThresholds(GetInstanceID(row), Guid.Empty, control);
            };
            tsConfigRoot.Click += (_, _) =>
            {
                ConfigureThresholds(-1, Guid.Empty, control);
            };
            tsJobInfo.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ShowJobInfoForm(row);
            };
            tsJobHistory.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ((AgentJobsControl)control).ShowHistory(row.Row, false);
            };
            tsAcknowledge.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                AcknowledgeRow(row, control);
            };
            strip.Opening += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                lblJobName.Text = GetJobName(row);
                lblJobName.Visible = !string.IsNullOrEmpty(lblJobName.Text);

                if (tsAcknowledge.Visible)
                {
                    var jobStatus = (DBADashStatus.DBADashStatusEnum)row["JobStatus"];
                    tsAcknowledge.Text =
                        jobStatus == DBADashStatus.DBADashStatusEnum.Acknowledged ? "Clear Acknowledgement" : "Acknowledge";
                    tsAcknowledge.Enabled = jobStatus is DBADashStatus.DBADashStatusEnum.Critical
                        or DBADashStatus.DBADashStatusEnum.Warning or DBADashStatus.DBADashStatusEnum.Acknowledged;
                }

                tsStartJob.Visible = DBADashUser.AllowJobExecution;
            };
            strip.Items.Insert(0, new ToolStripSeparator());
            strip.Items.Insert(0, tsJobActions);
        }

        private static string GetJobName(DataRowView row)
        {
            if (row.Row.Table.Columns.Contains("name"))
            {
                return (string)row["name"];
            }
            else if (row.Row.Table.Columns.Contains("JobName"))
            {
                return (string)row["JobName"];
            }
            else if (row.Row.Table.Columns.Contains("job_name"))
            {
                return (string)row["job_name"];
            }
            else
            {
                return string.Empty;
            }
        }

        private static int GetInstanceID(DataRowView row)
        {
            return (int)row["InstanceID"];
        }

        private static Guid GetJobId(DataRowView row)
        {
            return (Guid)row["job_id"];
        }

        private static void StartJob(DataRowView row)
        {
            RunJob(GetJobId(row), GetInstanceID(row), GetJobName(row));
        }

        private static void ShowJobInfoForm(DataRowView row)
        {
            try
            {
                var jobContext = CommonData.GetDBADashContext(GetInstanceID(row));
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = GetJobId(row);
                jobContext.ObjectName = GetJobName(row);
                var frm = new JobInfoForm() { DBADashContext = jobContext };
                frm.Show();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private static void RunJob(Guid jobId, int _instanceId, string jobName)
        {
            var runJobDialog = new JobExecutionDialog()
            {
                InstanceId = _instanceId,
                JobId = jobId,
                JobName = jobName,
            };
            runJobDialog.Show();
        }

        #endregion Shared Context Menu

        private void UserChangedStatusFilter(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void AcknowledgeErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dt = (DataView)dgvJobs.DataSource;
            var warningsAndFailures = dt.Table!.Select("JobStatus IN(1,2)");
            if (warningsAndFailures.Length == 0)
            {
                MessageBox.Show("No warnings/failures to acknowledge", "Acknowledge Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (MessageBox.Show(
                         $"Are you sure you want to acknowledge {warningsAndFailures.Length} job failure(s)?", "Acknowledge Failures", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var row in warningsAndFailures)
                {
                    var id = (Guid)row["job_id"];
                    var instanceID = (int)row["InstanceID"];
                    AcknowledgeJobErrors(instanceID, id, false);
                }
                RefreshData();
            }
        }

        private void ResizeForm(object sender, EventArgs e)
        {
            SetSplitterDistance();
        }

        private void SetSplitterDistance()
        {
            var newSplitterDistance = (context == null || context.JobID == Guid.Empty)
                ? splitContainer1.Height / 2
                : splitContainer1.Height / 4;
            if (newSplitterDistance < 100) return;
            try
            {
                splitContainer1.SplitterDistance = newSplitterDistance;
            }
            catch (InvalidOperationException)
            {
                // Ignore error that can occur if you resize the control to too small a size
            }
            catch (ArgumentOutOfRangeException)
            {
                // Ignore error that can occur if you resize the control to too small a size
            }
        }

        private void DgvJobHistory_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvJobHistory.Rows[idx].DataBoundItem;
                var statusString = row["run_status_description"] as string;
                var stepId = (int)row["step_id"];
                var status = statusString switch
                {
                    "Succeeded" => DBADashStatus.DBADashStatusEnum.OK,
                    "Failed" => DBADashStatus.DBADashStatusEnum.Critical,
                    _ => DBADashStatus.DBADashStatusEnum.Warning
                };
                dgvJobHistory.Rows[idx].Cells["colRunStatus"].SetStatusColor(status);
                if (stepId == 0 && ShowSteps)
                {
                    dgvJobHistory.Rows[idx].DefaultCellStyle.Font = new Font(dgvJobHistory.Font, FontStyle.Bold);
                }
                else if (ShowSteps)
                {
                    dgvJobHistory.Rows[idx].DefaultCellStyle.Font = new Font(dgvJobHistory.Font, FontStyle.Italic);
                }
            }
        }

        private async void TriggerCollection_Click(object sender, EventArgs e)
        {
            var types = new List<CollectionType>()
            {
                CollectionType.JobHistory, CollectionType.Jobs, CollectionType.RunningJobs
            };
            if (context.CollectAgentID == null || context.ImportAgentID == null) return;
            await CollectionMessaging.TriggerCollection(context.ConnectionID, types, context.CollectAgentID.Value, context.ImportAgentID.Value, this);
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            tsStatus.InvokeSetStatus(message, tooltip, color);
            if (message.Contains("completed", StringComparison.OrdinalIgnoreCase))
            {
                _ = Task.Run(() =>
                {
                    Task.Delay(3000);
                    RefreshData();
                });
            }
        }
    }
}