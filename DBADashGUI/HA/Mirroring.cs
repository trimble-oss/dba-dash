using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.HA
{
    public partial class Mirroring : UserControl
    {
        public Mirroring()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;

        public bool SummaryMode
        {
            get
            {
                return tsDetailSummary.Text != "Summary";
            }
            set
            {
                tsDetailSummary.Text = value ? "Detail" : "Summary";
            }
        }

        public void RefreshData()
        {
            if (SummaryMode)
            {
                var dt = GetMirroringSummary();
                dgv.DataSource = null;
                dgv.Columns.Clear();
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name="Instance" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Principal Count", DataPropertyName = "PrincipalCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Mirror Count", DataPropertyName = "MirrorCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Synchronized Count", DataPropertyName = "SynchronizedCount", Name = "SynchronizedCount"});
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Suspended Count", DataPropertyName = "SuspendedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Disconnected Count", DataPropertyName = "DisconnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Synchronizing Count", DataPropertyName = "SynchronizingCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Pending Failover Count", DataPropertyName = "PendingFailoverCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Not Synchronized Count", DataPropertyName = "NotSynchronizedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Unknown Count", DataPropertyName = "WitnessUnknownCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Connected Count", DataPropertyName = "WitnessConnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Witness Disconnected Count", DataPropertyName = "WitnessDisconnectedCount" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age (min)", DataPropertyName = "SnapshotAge" , Name="SnapshotAge"});
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            else
            {
                var dt = GetMirroringDetail();
                dgv.DataSource = null;
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = true;
                dgv.DataSource = dt;
                dgv.Columns.Remove("CollectionDateStatus");
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        DataTable GetMirroringSummary()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseMirroringSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));               
                var dt = new DataTable();
                da.Fill(dt);
                return dt;             
            }
            
        }
        DataTable GetMirroringDetail()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseMirroring_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));                                 
                var dt = new DataTable();
                da.Fill(dt);
                return dt;             
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsDetailSummary_Click(object sender, EventArgs e)
        {
            SummaryMode = !SummaryMode;
            RefreshData();
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            bool isSummary = SummaryMode;
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["CollectionDateStatus"];
                dgv.Rows[idx].Cells["SnapshotAge"].SetStatusColor(snapshotStatus);
                if (isSummary)
                {
                    var mirroringStatus =  (DBADashStatus.DBADashStatusEnum)(row["MirroringStatus"] == DBNull.Value ? 3 : Convert.ToInt16(row["MirroringStatus"]));
                    dgv.Rows[idx].Cells["SynchronizedCount"].SetStatusColor(mirroringStatus);
                }
                else
                {
                    var mirroringState = row["mirroring_state"] == DBNull.Value ? Int16.MinValue : Convert.ToInt16(row["mirroring_state"]);
                    var witnessState = row["mirroring_witness_state"] == DBNull.Value ? Int16.MinValue : Convert.ToInt16(row["mirroring_witness_state"]);
                    var colour = DashColors.Fail;
                    if (mirroringState==4|| mirroringState == 6)
                    {
                        colour = DashColors.Success;
                    }
                    else if (mirroringState == 2)
                    {
                        colour = DashColors.Warning;
                    }
                    var witnessColour = DashColors.NotApplicable;
                    if (witnessState == 1)
                    {
                        witnessColour = DashColors.Success;
                    }
                    else if (witnessState == 2)
                    {
                        witnessColour= DashColors.Fail;
                    }
                    dgv.Rows[idx].Cells["mirroring_witness_state"].SetStatusColor(witnessColour);
                    dgv.Rows[idx].Cells["mirroring_state"].SetStatusColor(colour);
                }
                
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}
