using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;
using SortOrder = System.Windows.Forms.SortOrder;

namespace DBADashGUI.AgentJobs
{
    public partial class AgentJobsControl : UserControl, ISetContext
    {
        private List<Int32> InstanceIDs;
        private int? instance_id = null;
        private int instanceId;
        private Guid jobID;
        private int StepID = -1;
        private DBADashContext context;

        public bool IncludeCritical { get => statusFilterToolStrip1.Critical; set => statusFilterToolStrip1.Critical = value; }

        public bool IncludeWarning { get => statusFilterToolStrip1.Warning; set => statusFilterToolStrip1.Warning = value; }

        public bool IncludeNA { get => statusFilterToolStrip1.NA; set => statusFilterToolStrip1.NA = value; }

        public bool IncludeOK { get => statusFilterToolStrip1.OK; set => statusFilterToolStrip1.OK = value; }

        public bool IncludeAcknowledged { get => statusFilterToolStrip1.Acknowledged; set => statusFilterToolStrip1.Acknowledged = value; }

        private bool DoAutoSize = true;

        public void SetContext(DBADashContext context)
        {
            this.context = context;

            StepID = context.JobStepID;
            IncludeNA = context.RegularInstanceIDs.Count == 1;
            IncludeOK = context.RegularInstanceIDs.Count == 1;
            IncludeWarning = true;
            IncludeCritical = true;
            IncludeAcknowledged = true;
            InstanceIDs = context.RegularInstanceIDs.ToList();

            RefreshData();
        }

        public void RefreshData()
        {
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
                    sort = dgvJobs.SortedColumn.DataPropertyName + (dgvJobs.SortOrder == SortOrder.Descending ? " DESC" : " ASC");
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
                    ShowHistory(dt.Rows[0]);
                }
            }
        }

        private DataTable GetJobs()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AgentJobs_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
                cmd.Parameters.AddWithValue("ShowHidden", context.RegularInstanceIDs.Count == 1 || Common.ShowHidden);
                cmd.Parameters.AddGuidIfNotEmpty("JobID", context.JobID);

                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        public AgentJobsControl()
        {
            InitializeComponent();
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, Guid.Empty);
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], Guid.Empty);
            }
        }

        private void ConfigureThresholds(Int32 InstanceID, Guid jobID)
        {
            var frm = new AgentJobThresholdsConfig
            {
                InstanceID = InstanceID,
                JobID = jobID,
                connectionString = Common.ConnectionString
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void DgvJobs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvJobs.Rows[e.RowIndex].DataBoundItem;
                if (dgvJobs.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    ConfigureThresholds((Int32)row["InstanceID"], (Guid)row["job_id"]);
                }
                else if (dgvJobs.Columns[e.ColumnIndex] == colHistory)
                {
                    failedOnlyToolStripMenuItem.Checked = false;
                    ShowHistory(row.Row);
                }
                else if (e.ColumnIndex == Acknowledge.Index)
                {
                    bool clear = (DBADashStatus.DBADashStatusEnum)row["JobStatus"] == DBADashStatus.DBADashStatusEnum.Acknowledged;
                    AcknowledgeJobErrors((int)row["InstanceID"], (Guid)row["job_id"], clear);
                    RefreshData();
                }
            }
        }

        private void ShowHistory(DataRow row)
        {
            if (row["job_id"] != DBNull.Value)
            {
                instanceId = (Int32)row["InstanceID"];
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
                stepID = showJobStepsToolStripMenuItem.Checked ? (int?)null : 0;
            }
            stepID = instance_id == null ? stepID : null;
            colStepName.Visible = stepID != 0;
            colStepID.Visible = stepID != 0;
            tsFilter.Visible = instance_id == null;
            tsBack.Visible = instance_id != null;
            colViewSteps.Visible = instance_id == null && stepID == 0;
            splitContainer1.Panel2Collapsed = false;
            dgvJobHistory.AutoGenerateColumns = false;
            dgvJobHistory.DataSource = GetJobHistory(instanceId, jobID, stepID, instance_id, failedOnlyToolStripMenuItem.Checked);
            dgvJobHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private static DataTable GetJobHistory(int InstanceID, Guid JobID, int? StepID = 0, int? instance_id = null, bool failedOnly = false)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.JobHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new(cmd))
            {
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
        }

        private void DgvJobs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
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
                if ((string)row["ConfiguredLevel"] == "Job")
                {
                    dgvJobs.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvJobs.Font, FontStyle.Bold);
                }
                else
                {
                    dgvJobs.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvJobs.Font, FontStyle.Regular);
                }
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
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.JobErrorAck", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddGuidIfNotEmpty("job_id", JobID);
                cmd.Parameters.AddWithValue("Clear", clear);
                cmd.ExecuteNonQuery();
            }
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

        private void DgvJobHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
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
            Common.PromptSaveDataGridView(ref dgvJobs);
            Configure.Visible = true;
            colHistory.Visible = true;
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            colViewSteps.Visible = false;
            Common.PromptSaveDataGridView(ref dgvJobHistory);
            colViewSteps.Visible = true;
        }

        private void AgentJobsControl_Load(object sender, EventArgs e)
        {
            dgvJobHistory.ApplyTheme();
            dgvJobs.ApplyTheme();
        }

        private void UserChangedStatusFilter(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void AcknowledgeErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataView dt = (DataView)dgvJobs.DataSource;
            var warningsAndFailures = dt.Table.Select("JobStatus IN(1,2)");
            if (warningsAndFailures.Length == 0)
            {
                MessageBox.Show("No warnings/failures to acknowledge", "Acknowledge Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (MessageBox.Show(string.Format("Are you sure you want to acknowledge {0} job failure(s)?", warningsAndFailures.Length), "Acknowledge Failures", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (DataRow row in warningsAndFailures)
                {
                    Guid jobID = (Guid)row["job_id"];
                    int instanceID = (int)row["InstanceID"];
                    AcknowledgeJobErrors(instanceID, jobID, false);
                }
                RefreshData();
            }
        }
    }
}