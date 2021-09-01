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

namespace DBADashGUI.LogShipping
{
    public partial class LogShippingControl : UserControl
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
            if (ConnectionString != null)
            {
                using (var cn = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.LogShipping_Get", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cn.Open();
                        cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                        cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                        cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                        cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                        cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dgvLogShipping.AutoGenerateColumns = false;
                        dgvLogShipping.DataSource = new DataView(dt);
                        dgvLogShipping.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    }
                }
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
        }


        public LogShippingControl()
        {
            InitializeComponent();
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

        private void dgvLogShipping_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvLogShipping.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    var row = (DataRowView)dgvLogShipping.Rows[e.RowIndex].DataBoundItem;
                    ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"]);
                }
            }
        }

        public void ConfigureThresholds(Int32 InstanceID,Int32 DatabaseID)
        {
            var frm = new LogShippingThresholdsConfig
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID,
                ConnectionString = ConnectionString
            };
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void dgvLogShipping_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvLogShipping.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotAgeStatus"];
                dgvLogShipping.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                dgvLogShipping.Rows[idx].Cells["Status"].Style.BackColor = DBADashStatus.GetStatusColour(Status);
                if ((string)row["ThresholdConfiguredLevel"] == "Database")
                {
                    dgvLogShipping.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLogShipping.Font, FontStyle.Bold);
                }
                else
                {
                    dgvLogShipping.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLogShipping.Font, FontStyle.Regular);
                }
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvLogShipping);
            Configure.Visible = true;
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.PromptSaveDataGridView(ref dgvLogShipping);
            Configure.Visible = true;
        }
    }
}
