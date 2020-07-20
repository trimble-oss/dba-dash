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

namespace DBAChecksGUI.DBFiles
{
    public partial class DBFilesControl : UserControl
    {
        public DBFilesControl()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        public string ConnectionString;
        public Int32? DatabaseID;

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
                SqlCommand cmd = new SqlCommand("dbo.DBFiles_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",",InstanceIDs));
                if (DatabaseID != null) { cmd.Parameters.AddWithValue("DatabaseID", DatabaseID); }
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvFiles.AutoGenerateColumns = false;
                dgvFiles.DataSource = new DataView(dt);
            }

            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
            configureDatabaseThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1 && DatabaseID > 0;
   
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

        private void dgvFiles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvFiles.Columns[e.ColumnIndex].HeaderText == "Configure")
            {
                var row = (DataRowView)dgvFiles.Rows[e.RowIndex].DataBoundItem;
                ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"],(Int32)row["data_space_id"]);
            }
        }

        public void ConfigureThresholds(Int32 InstanceID, Int32 DatabaseID,Int32 DataSpaceID)
        {
            var threshold = FileThreshold.GetFileThreshold(InstanceID, DatabaseID, DataSpaceID, ConnectionString);
            var frm = new FileThresholdConfig();
            frm.FileThreshold = threshold;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void dgvFiles_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvFiles.Rows[idx].DataBoundItem;
                var Status = (DBAChecksStatus.DBAChecksStatusEnum)row["FreeSpaceStatus"];
                var snapshotStatus = (DBAChecksStatus.DBAChecksStatusEnum)row["SnapshotAgeStatus"];
                string checkType = row["FreeSpaceCheckType"] == DBNull.Value ? "-" : (string)row["FreeSpaceCheckType"];
                dgvFiles.Rows[idx].Cells["FileSnapshotAge"].Style.BackColor = DBAChecksStatus.GetStatusColour(snapshotStatus);
                dgvFiles.Rows[idx].Cells["PctFree"].Style.BackColor = Color.White;
                dgvFiles.Rows[idx].Cells["FreeMB"].Style.BackColor = Color.White;
                if (checkType != "M")
                {
                    dgvFiles.Rows[idx].Cells["PctFree"].Style.BackColor = DBAChecksStatus.GetStatusColour(Status);
                }
                if (checkType != "%")
                {
                    dgvFiles.Rows[idx].Cells["FreeMB"].Style.BackColor = DBAChecksStatus.GetStatusColour(Status);
                }

                if (row["ConfiguredLevel"]!=DBNull.Value && (string)row["ConfiguredLevel"] == "FG")
                {
                    dgvFiles.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvFiles.Font, FontStyle.Bold);
                }
                else
                {
                    dgvFiles.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvFiles.Font, FontStyle.Regular);
                }
            }
        }

        private void configureDatabaseThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(InstanceIDs.Count==1 && DatabaseID > 0)
            {
                ConfigureThresholds(InstanceIDs[0], (Int32)DatabaseID, -1);
            }
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1) {
                ConfigureThresholds(InstanceIDs[0], -1, -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1, -1);
        }
    }
}
