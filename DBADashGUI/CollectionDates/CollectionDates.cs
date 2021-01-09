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

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDates : UserControl
    {
        public CollectionDates()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs { get; set; }
        public string ConnectionString { get; set; }

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
            UseWaitCursor = true;
            if (ConnectionString != null)
            {
                SqlConnection cn = new SqlConnection(ConnectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.CollectionDates_Get", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                    cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                    cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                    cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvCollectionDates.AutoGenerateColumns = false;
                    dgvCollectionDates.DataSource = new DataView(dt);
                }

            }
            UseWaitCursor = false;
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

        private void ConfigureThresholds(Int32 InstanceID, DataRowView row)
        {
        
            var frm = new CollectionDatesThresholds();
            frm.InstanceID = InstanceID;
            if (row["WarningThreshold"] == DBNull.Value || row["CriticalThreshold"] == DBNull.Value)
            {
                frm.Disabled = true;
            }
            else
            {
                frm.WarningThreshold = (Int32)row["WarningThreshold"];
                frm.CriticalThreshold = (Int32)row["CriticalThreshold"];
            }
            if ((string)row["ConfiguredLevel"] != "Instance")
            {
                frm.Inherit = true;
            }
            frm.Reference = (string)row["Reference"];
            frm.ConnectionString = ConnectionString;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[e.RowIndex].DataBoundItem;
                if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Instance")
                {

                    var InstanceID = (Int32)row["InstanceID"];
                    ConfigureThresholds(InstanceID, row);
                }
                else if (dgvCollectionDates.Columns[e.ColumnIndex].HeaderText == "Configure Root")
                {
                    ConfigureThresholds(-1, row);
                }
            }
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvCollectionDates.Rows[idx].DataBoundItem;
                if (row != null)
                {
                    var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];

                    dgvCollectionDates.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(Status);
                    dgvCollectionDates.Rows[idx].Cells["SnapshotDate"].Value = ((DateTime)row["SnapshotDate"]).ToLocalTime();

                    if ((string)row["ConfiguredLevel"] == "Instance")
                    {
                        dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvCollectionDates.Font, FontStyle.Bold);
                    }
                    else
                    {
                        dgvCollectionDates.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvCollectionDates.Font, FontStyle.Regular);
                    }
                }
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            ConfigureRoot.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvCollectionDates);
            Configure.Visible = true;
            ConfigureRoot.Visible = true;
        }
    }
}
