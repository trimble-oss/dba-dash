using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.HA
{
    public partial class AG : UserControl
    {
        public AG()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        private int instanceId=-1;

        public void RefreshData()
        {
            instanceId = -1;
            if (InstanceIDs.Count == 1)
            {
               instanceId= InstanceIDs[0];
            }
            refreshData();
        }

        private void refreshData()
        {
            tsBack.Enabled = instanceId > 0;
            dgv.DataSource = null;
            dgv.Columns.Clear();
            DataTable dt;

            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "InstanceID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "Snapshot Status",Name="colSnapshotStatus", Visible = false });
            if (instanceId > 0)
            {
                dt = GetAvailabilityGroup(instanceId);                
            }
            else
            {
                dt = GetAvailabilityGroupSummary(InstanceIDs);
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance" });
            }

            dgv.DataSource = dt;

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        public DataTable GetAvailabilityGroup(Int32 InstanceID)
        {
            using (SqlConnection cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("AvailabilityGroup_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetAvailabilityGroupSummary(List<int> InstanceIDs)
        {
            using(SqlConnection cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("AvailabilityGroupSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && dgv.Columns[e.ColumnIndex].Name=="colInstance")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                instanceId = (int)row["InstanceID"];
                refreshData();
            }
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dgv.Columns.Contains("Snapshot Date"))
            {
                for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                    var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["Snapshot Status"];
                    dgv.Rows[idx].Cells["Snapshot Date"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                    dgv.Rows[idx].Cells["Snapshot Age"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                    if (instanceId > 0)
                    {
                        dgv.Rows[idx].Cells["Sync State"].Style.BackColor = (string)row["Sync Health"] == "HEALTHY" ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.OK) :
                                 (string)row["Sync Health"] == "PARTIALLY_HEALTHY" ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Critical);
                    }
                    else
                    {
                        dgv.Rows[idx].Cells["Not Synchronizing"].Style.BackColor =(int)row["Not Synchronizing"] >0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Critical) : Color.White;
                        dgv.Rows[idx].Cells["Remote Not Synchronizing"].Style.BackColor = (int)row["Remote Not Synchronizing"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Critical) : Color.White;
                        dgv.Rows[idx].Cells["Synchronized"].Style.BackColor = (int)row["Synchronized"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.OK) : Color.White;
                        dgv.Rows[idx].Cells["Remote Synchronized"].Style.BackColor = (int)row["Remote Synchronized"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.OK) : Color.White;
                        dgv.Rows[idx].Cells["Reverting"].Style.BackColor = (int)row["Reverting"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : Color.White;
                        dgv.Rows[idx].Cells["Remote Reverting"].Style.BackColor = (int)row["Remote Reverting"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : Color.White;
                        dgv.Rows[idx].Cells["Initializing"].Style.BackColor = (int)row["Initializing"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : Color.White;
                        dgv.Rows[idx].Cells["Remote Initializing"].Style.BackColor = (int)row["Remote Initializing"] > 0 ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : Color.White;

                    }
                    dgv.Rows[idx].Cells["Sync Health"].Style.BackColor = (string)row["Sync Health"] == "HEALTHY" ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.OK) :
                                                     (string)row["Sync Health"] == "PARTIALLY_HEALTHY" ? DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Warning) : DBADashStatus.GetStatusColour(DBADashStatus.DBADashStatusEnum.Critical);
                }
            }
        }
    }
}
