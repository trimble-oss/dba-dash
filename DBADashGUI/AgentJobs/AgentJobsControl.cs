﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.AgentJobs
{
    public partial class AgentJobsControl : UserControl
    {
        public List<Int32> InstanceIDs;
        public Guid JobID;

        private int? instance_id = null;
        private int instanceId;
        private Guid jobID;
        public int StepID=-1;

        public bool IncludeCritical
        {
            get
            {
                return criticalToolStripMenuItem.Checked;
            }
            set
            {
                criticalToolStripMenuItem.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return warningToolStripMenuItem.Checked;
            }
            set
            {
                warningToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return undefinedToolStripMenuItem.Checked;
            }
            set
            {
                undefinedToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return OKToolStripMenuItem.Checked;
            }
            set
            {
                OKToolStripMenuItem.Checked = value;
            }
        }



        public void RefreshData()
        {
            dgvJobs.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvJobs.Columns[1].Frozen = Common.FreezeKeyColumn;
            failedOnlyToolStripMenuItem.Checked = false;
            splitContainer1.Panel2Collapsed = true;
            dgvJobHistory.DataSource = null;
            if (StepID > 0 && InstanceIDs.Count==1)
            {
                tsJobs.Visible = false;
                dgvJobs.Visible = false;
                jobStep1.Visible = true;
                jobStep1.JobID = JobID;
                jobID = JobID;
                instanceId = InstanceIDs[0];
                jobStep1.InstanceID = InstanceIDs[0];
                jobStep1.StepID = StepID;
                jobStep1.RefreshData();
                showHistory();
            }
            else
            {
                tsJobs.Visible = true;
                dgvJobs.Visible = true;
                jobStep1.Visible = false;
                var dt = GetJobs();
                dgvJobs.AutoGenerateColumns = false;
                dgvJobs.DataSource = new DataView(dt);
                dgvJobs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
                if (JobID != Guid.Empty && dt.Rows.Count == 1)
                {
                    showHistory(dt.Rows[0]);
                }
            }
          
      
        }

        private DataTable GetJobs()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd= new SqlCommand("dbo.AgentJobs_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                if(JobID!= Guid.Empty)
                {
                    cmd.Parameters.AddWithValue("JobID", JobID);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }


        public AgentJobsControl()
        {
            InitializeComponent();
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            configureThresholds(-1, Guid.Empty);
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                configureThresholds(InstanceIDs[0], Guid.Empty);
            }
        }

        private void configureThresholds(Int32 InstanceID,Guid jobID)
        {
            var frm = new AgentJobThresholdsConfig
            {
                InstanceID = InstanceID,
                JobID = jobID,
                connectionString = Common.ConnectionString
            };
            frm.ShowDialog();
            if(frm.DialogResult== DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void dgvJobs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvJobs.Rows[e.RowIndex].DataBoundItem;
                if (dgvJobs.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    
                    configureThresholds((Int32)row["InstanceID"], (Guid)row["job_id"]);
                }
                if (dgvJobs.Columns[e.ColumnIndex] == colHistory)
                {
                    failedOnlyToolStripMenuItem.Checked = false;
                    showHistory(row.Row);
                }
            }
        }

        private void showHistory(DataRow row)
        {

            if (row["job_id"] != DBNull.Value)
            {
                instanceId = (Int32)row["InstanceID"];
                jobID = (Guid)row["job_id"];
                tsJobName.Text = (string)row["Instance"] + " | " + (string)row["name"];
                instance_id = null;
                showHistory();
            }
            else
            {
                MessageBox.Show("job_id IS NULL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void showHistory()
        {
            int? stepID = StepID;
            if (StepID < 0)
            {
                stepID = showJobStepsToolStripMenuItem.Checked ? (int?)null : 0;
            }
            stepID = instance_id == null ? stepID : null;
            colStepName.Visible = stepID !=0;
            colStepID.Visible = stepID != 0;
            tsFilter.Visible = instance_id == null;
            tsBack.Visible = instance_id != null;
            colViewSteps.Visible=instance_id== null && stepID==0;
            splitContainer1.Panel2Collapsed = false;
            dgvJobHistory.AutoGenerateColumns = false;
            dgvJobHistory.DataSource = GetJobHistory(instanceId, jobID,stepID,instance_id, failedOnlyToolStripMenuItem.Checked);
            dgvJobHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetJobHistory(int InstanceID,Guid JobID, int? StepID=0,int? instance_id=null,bool failedOnly=false)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))         
            using (SqlCommand cmd = new SqlCommand("dbo.JobHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", JobID);
                if (StepID != null)
                {
                    cmd.Parameters.AddWithValue("StepID", StepID);
                }
                else
                {
                    cmd.Parameters.AddWithValue("StepID", DBNull.Value);
                }
                if (instance_id != null)
                {
                    cmd.Parameters.AddWithValue("instance_id", instance_id);
                }
                cmd.Parameters.AddWithValue("FailedOnly", failedOnly);
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
          
        }

        private void dgvJobs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
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
                }
            }

        private void criticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void warningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void undefinedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();

        }

        private void OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            colHistory.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvJobs);
            Configure.Visible = true;
            colHistory.Visible = true;
        }

        private void dgvJobHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvJobHistory.Rows[e.RowIndex].DataBoundItem;
                if(e.ColumnIndex == colViewSteps.Index)
                {
                    instance_id = (int)row["instance_id"];
                    showHistory();
                }
                if (e.ColumnIndex == colMessage.Index)
                {
                    string message = Convert.ToString(row["Message"]);
                    NotepadHelper.ShowMessage(message, "DBA Dash Job History");                                     
                }
            }
       }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            colViewSteps.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvJobHistory);
            colViewSteps.Visible = true;
        }

        private void showJobStepsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showHistory();
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            instance_id = null;
            showHistory();
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            showHistory();
        }

        private void failedOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showHistory();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            colHistory.Visible = false;
            Common.PromptSaveDataGridView(ref dgvJobs);
            Configure.Visible = true;
            colHistory.Visible = true;
        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            colViewSteps.Visible = false;
            Common.PromptSaveDataGridView(ref dgvJobHistory);
            colViewSteps.Visible = true;
        }

        private void AgentJobsControl_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgvJobHistory);
            Common.StyleGrid(ref dgvJobs);
        }
    }
    }

