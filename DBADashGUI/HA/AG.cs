using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
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
            HookupNavigationButtons(this); // Handle mouse back button
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

        private void HandlePreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.XButton1:
                    NavigateBack();
                    break;
            }
        }
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.XButton1:
                    NavigateBack();
                    break;
            }
        }
        // https://stackoverflow.com/questions/41637248/how-do-i-capture-mouse-back-button-and-cause-it-to-do-something-else
        private void HookupNavigationButtons(Control ctrl)
        {
            for (int t = ctrl.Controls.Count - 1; t >= 0; t--)
            {
                Control c = ctrl.Controls[t];
                c.PreviewKeyDown -= HandlePreviewKeyDown;
                c.PreviewKeyDown += HandlePreviewKeyDown;
                c.MouseDown -= HandleMouseDown;
                c.MouseDown += HandleMouseDown;
                HookupNavigationButtons(c);
            }
        }


        private void refreshData()
        {
            tsBack.Enabled = instanceId > 0;
            dgv.DataSource = null;
            dgv.Columns.Clear();
            DataTable dt;

            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "InstanceID", Visible = false, Name="colInstanceID",Frozen = Common.FreezeKeyColumn });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "Snapshot Status",Name="colSnapshotStatus", Visible = false, Frozen= Common.FreezeKeyColumn });
            if (instanceId > 0)
            {                
                dt = GetAvailabilityGroup(instanceId);
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "Database", Name = "colDatabase", HeaderText ="Database", Frozen = Common.FreezeKeyColumn });
            }
            else
            {
                dt = GetAvailabilityGroupSummary(InstanceIDs);
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance", LinkColor=DashColors.LinkColor, Frozen = Common.FreezeKeyColumn });
            }
            Common.ConvertUTCToLocal(ref dt);
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
            NavigateBack();
        }

        private void NavigateBack()
        {
            if (tsBack.Enabled)
            {
                RefreshData();
            }
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dgv.Columns.Contains("Snapshot Date"))
            {
                for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                    var r = dgv.Rows[idx];
                    var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["Snapshot Status"];
                    r.Cells["Snapshot Date"].SetStatusColor(snapshotStatus);
                    r.Cells["Snapshot Age"].SetStatusColor(snapshotStatus);
                    if (instanceId > 0)
                    {
                        var syncStateStatus = Convert.ToString(row["Sync Health"]) == "HEALTHY" ? DashColors.Success :
                                 Convert.ToString(row["Sync Health"]) == "PARTIALLY_HEALTHY" ? DashColors.Warning : DashColors.Fail;
                        r.Cells["Sync State"].SetStatusColor(syncStateStatus);
                    }
                    else
                    {
                        r.Cells["Not Synchronizing"].SetStatusColor((int)row["Not Synchronizing"] >0 ? DashColors.Fail: Color.White);
                        r.Cells["Remote Not Synchronizing"].SetStatusColor((int)row["Remote Not Synchronizing"] > 0 ? DashColors.Fail : Color.White);
                        r.Cells["Synchronized"].SetStatusColor((int)row["Synchronized"] > 0 ? DashColors.Success : Color.White);
                        r.Cells["Remote Synchronized"].SetStatusColor((int)row["Remote Synchronized"] > 0 ? DashColors.Success : Color.White);
                        r.Cells["Reverting"].SetStatusColor((int)row["Reverting"] > 0 ? DashColors.Warning : Color.White);
                        r.Cells["Remote Reverting"].SetStatusColor((int)row["Remote Reverting"] > 0 ? DashColors.Warning : Color.White);
                        r.Cells["Initializing"].SetStatusColor((int)row["Initializing"] > 0 ? DashColors.Warning : Color.White);
                        r.Cells["Remote Initializing"].SetStatusColor((int)row["Remote Initializing"] > 0 ? DashColors.Warning : Color.White);

                    }
                    var syncHealthStatus = Convert.ToString(row["Sync Health"]) == "HEALTHY" ? DashColors.Success :
                                                     Convert.ToString(row["Sync Health"]) == "PARTIALLY_HEALTHY" ? DashColors.Warning : DashColors.Fail;
                    dgv.Rows[idx].Cells["Sync Health"].SetStatusColor(syncHealthStatus); 
                }
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}
