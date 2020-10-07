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

namespace DBAChecksGUI.Backups
{
    public partial class BackupsControl : UserControl
    {

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

        public List<Int32> InstanceIDs { get; set; }
        public List<Int32> DatabaseID { get; set; }
        public string ConnectionString;


        public void RefreshBackups()
        {
            UseWaitCursor = true;
            if (ConnectionString != null)
            {
                configureInstanceThresholdsToolStripMenuItem.Enabled = (InstanceIDs.Count == 1);
                SqlConnection cn = new SqlConnection(ConnectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.Backups_Get", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                    cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                    cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                    cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dtBackups = new DataTable();
                    da.Fill(dtBackups);
                    dgvBackups.AutoGenerateColumns = false;
                    dgvBackups.DataSource = new DataView(dtBackups);
                }
                
            }
            UseWaitCursor = false;
        }

        public BackupsControl()
        {
            InitializeComponent();
        }



        private void dgvBackups_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvBackups.Rows[idx].DataBoundItem;
                var fullBackupStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["FullBackupStatus"];
                var diffBackupStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["DiffBackupStatus"];
                var logBackupStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["LogBackupStatus"];
                var snapshotStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["SnapshotAgeStatus"];
                dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor = DBAChecksStatus.GetStatusColour(fullBackupStatus);
                dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor = DBAChecksStatus.GetStatusColour(diffBackupStatus);
                dgvBackups.Rows[idx].Cells["LastLog"].Style.BackColor = DBAChecksStatus.GetStatusColour(logBackupStatus);
                if ((bool)row["ConsiderPartialBackups"])
                {
                    dgvBackups.Rows[idx].Cells["LastPartial"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor;
                    dgvBackups.Rows[idx].Cells["LastPartialDiff"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor;
                }
                if ((bool)row["ConsiderFGBackups"])
                {
                    dgvBackups.Rows[idx].Cells["LastFG"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor;
                    dgvBackups.Rows[idx].Cells["LastFGDiff"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor;
                }
                dgvBackups.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBAChecksStatus.GetStatusColour(snapshotStatus);
                if ((string)row["ThresholdsConfiguredLevel"] == "Database")
                {
                    dgvBackups.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvBackups.Font, FontStyle.Bold);
                }
                else
                {
                    dgvBackups.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvBackups.Font, FontStyle.Regular);
                }
            }

        }

        private void dgvBackups_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvBackups.Columns[e.ColumnIndex].HeaderText == "Configure")
            {
                var row = (DataRowView)dgvBackups.Rows[e.RowIndex].DataBoundItem;
                ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"]);
            }
        }

        private void ConfigureThresholds(Int32 InstanceID, Int32 DatabaseID)
        {
            var frm = new BackupThresholdsConfig();
            frm.InstanceID = InstanceID;
            frm.DatabaseID = DatabaseID;
            frm.ConnectionString = ConnectionString;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshBackups();
            }

        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void criticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshBackups();

        }

        private void warningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshBackups();
        }

        private void undefinedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshBackups();
        }

        private void OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshBackups();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshBackups();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvBackups);
            Configure.Visible = true;
        }
    }
}
