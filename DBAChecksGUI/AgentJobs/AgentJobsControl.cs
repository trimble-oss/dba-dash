using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DBAChecksGUI.AgentJobs
{
    public partial class AgentJobsControl : UserControl
    {
        public List<Int32> InstanceIDs;
        public string ConnectionString;

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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AgentJobs_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.CommandType = CommandType.StoredProcedure;
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                dgvJobs.AutoGenerateColumns = false;
                dgvJobs.DataSource = new DataView(dt);
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
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
            var frm = new AgentJobThresholdsConfig();
            frm.InstanceID = InstanceID;
            frm.JobID = jobID;
            frm.connectionString = ConnectionString;
            frm.ShowDialog();
            if(frm.DialogResult== DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void dgvJobs_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvJobs.Columns[e.ColumnIndex].HeaderText == "Configure")
            {
                var row = (DataRowView)dgvJobs.Rows[e.RowIndex].DataBoundItem;
                configureThresholds((Int32)row["InstanceID"], (Guid)row["job_id"]);
            }
        }

        private void dgvJobs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvJobs.Rows[idx].DataBoundItem;
                var jobStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["JobStatus"];
                var lastFailStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["LastFailStatus"];
                var timeSinceLastStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["TimeSinceLastFailureStatus"];
                var timeSinceLastSucceededStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["TimeSinceLastSucceededStatus"];
                var failCount24HrsStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["FailCount24HrsStatus"];
                var failCount7DaysStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["FailCount7DaysStatus"];
                var stepFailCount24HrsStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["JobStepFail24HrsStatus"];
                var stepFailCount7DaysStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["JobStepFail7DaysStatus"];
                dgvJobs.Rows[idx].Cells["name"].Style.BackColor = DBAChecksStatus.GetStatusColour(jobStatus);
                dgvJobs.Rows[idx].Cells["IsLastFail"].Style.BackColor = DBAChecksStatus.GetStatusColour(lastFailStatus);
                dgvJobs.Rows[idx].Cells["LastFail"].Style.BackColor = DBAChecksStatus.GetStatusColour(lastFailStatus);
                dgvJobs.Rows[idx].Cells["TimeSinceLastFail"].Style.BackColor = DBAChecksStatus.GetStatusColour(timeSinceLastStatus);
                dgvJobs.Rows[idx].Cells["TimeSinceLastSucceeded"].Style.BackColor = DBAChecksStatus.GetStatusColour(timeSinceLastSucceededStatus);
                dgvJobs.Rows[idx].Cells["FailCount24Hrs"].Style.BackColor = DBAChecksStatus.GetStatusColour(failCount24HrsStatus);
                dgvJobs.Rows[idx].Cells["FailCount7Days"].Style.BackColor = DBAChecksStatus.GetStatusColour(failCount7DaysStatus);
                dgvJobs.Rows[idx].Cells["JobStepFails24Hrs"].Style.BackColor = DBAChecksStatus.GetStatusColour(stepFailCount24HrsStatus);
                dgvJobs.Rows[idx].Cells["JobStepFails7Days"].Style.BackColor = DBAChecksStatus.GetStatusColour(stepFailCount7DaysStatus);
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
    }
    }

