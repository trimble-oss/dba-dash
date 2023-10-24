using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.HA
{
    public partial class AG : UserControl, INavigation, ISetContext
    {
        public AG()
        {
            InitializeComponent();
        }

        private List<Int32> InstanceIDs;
        private int instanceId = -1;

        public bool CanNavigateBack { get => tsBack.Enabled; }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            instanceId = -1;
            if (InstanceIDs.Count == 1)
            {
                instanceId = InstanceIDs[0];
            }
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            tsBack.Enabled = instanceId > 0 && InstanceIDs.Count > 1;
            dgv.DataSource = null;
            dgv.Columns.Clear();
            DataTable dt;

            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "InstanceID", Visible = false, Name = "colInstanceID", Frozen = Common.FreezeKeyColumn });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "Snapshot Status", Name = "colSnapshotStatus", Visible = false, Frozen = Common.FreezeKeyColumn });
            if (instanceId > 0)
            {
                dt = GetAvailabilityGroup(instanceId);
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = "Database", Name = "colDatabase", HeaderText = "Database", Frozen = Common.FreezeKeyColumn });
            }
            else
            {
                dt = GetAvailabilityGroupSummary(InstanceIDs);
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn, SortMode = DataGridViewColumnSortMode.Automatic });
            }
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgv.DataSource = dt;
            dgv.ApplyTheme();
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        public static DataTable GetAvailabilityGroup(Int32 InstanceID)
        {
            using (SqlConnection cn = new(Common.ConnectionString))
            using (SqlCommand cmd = new("AvailabilityGroup_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static DataTable GetAvailabilityGroupSummary(List<int> InstanceIDs)
        {
            using (SqlConnection cn = new(Common.ConnectionString))
            using (SqlCommand cmd = new("AvailabilityGroupSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataLocal();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "colInstance")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                instanceId = (int)row["InstanceID"];
                RefreshDataLocal();
            }
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                RefreshData();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
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
                        var syncStateStatus = Convert.ToString(row["Sync Health"]) == "HEALTHY" ? DBADashStatusEnum.OK :
                                 Convert.ToString(row["Sync Health"]) == "PARTIALLY_HEALTHY" ? DBADashStatusEnum.Warning : DBADashStatusEnum.Critical;
                        r.Cells["Sync State"].SetStatusColor(syncStateStatus);
                    }
                    else
                    {
                        var secondaryReplicas = (int)row["Secondary Replicas"];
                        var primaryReplicas = (int)row["Primary Replicas"];
                        var totalReplicas = primaryReplicas+secondaryReplicas;

                        r.Cells["Not Synchronizing"].SetStatusColor((int)row["Not Synchronizing"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Remote Not Synchronizing"].SetStatusColor((int)row["Remote Not Synchronizing"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Synchronized"].SetStatusColor((int)row["Synchronized"] == totalReplicas ? DBADashStatusEnum.OK : DBADashStatusEnum.WarningLow);
                        r.Cells["Remote Synchronized"].SetStatusColor((int)row["Remote Synchronized"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Reverting"].SetStatusColor((int)row["Reverting"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Remote Reverting"].SetStatusColor((int)row["Remote Reverting"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Initializing"].SetStatusColor((int)row["Initializing"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                        r.Cells["Remote Initializing"].SetStatusColor((int)row["Remote Initializing"] > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.OK);
                    }
                    var syncHealthStatus = Convert.ToString(row["Sync Health"]) == "HEALTHY" ? DBADashStatusEnum.OK :
                                                     Convert.ToString(row["Sync Health"]) == "PARTIALLY_HEALTHY" ? DBADashStatusEnum.Warning : DBADashStatusEnum.Critical;
                    dgv.Rows[idx].Cells["Sync Health"].SetStatusColor(syncHealthStatus);
                }
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}