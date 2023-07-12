using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class HardwareChanges : UserControl, ISetContext
    {
        public HardwareChanges()
        {
            InitializeComponent();
        }

        private List<Int32> InstanceIDs;

        public void SetContext(DBADashContext context)
        {
            this.InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvHardware.Columns[0].Frozen = Common.FreezeKeyColumn;
            RefreshHistory();
            RefreshHardware();
        }

        private void RefreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.HostUpgradeHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void RefreshHardware()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Hardware_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                dgvHardware.AutoGenerateColumns = false;
                dgvHardware.DataSource = dt;
                dgvHardware.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHardware);
        }

        private void TsRefreshHardware_Click(object sender, EventArgs e)
        {
            RefreshHardware();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void DgvHardware_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx++)
            {
                var r = dgvHardware.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var ifiStatus = row["InstantFileInitializationEnabled"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((bool)row["InstantFileInitializationEnabled"] ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var offlineSchedulerStatus = row["OfflineSchedulers"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((Int32)row["OfflineSchedulers"] == 0 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Critical);
                var ppStatus = row["ActivePowerPlanGUID"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((Guid)row["ActivePowerPlanGUID"] == Common.HighPerformancePowerPlanGUID ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var priorityStatus = row["os_priority_class"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((Int32)row["os_priority_class"] == 32 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var affinityStatus = row["affinity_type_desc"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((string)row["affinity_type_desc"] == "AUTO" ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);

                r.Cells[colAffinity.Index].SetStatusColor(affinityStatus);
                r.Cells[colPriority.Index].SetStatusColor(priorityStatus);
                r.Cells[colOfflineSchedulers.Index].SetStatusColor(offlineSchedulerStatus);
                r.Cells[colPowerPlan.Index].SetStatusColor(ppStatus);
                r.Cells[colInstantFileInitialization.Index].SetStatusColor(ifiStatus);
            }
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHardware);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvHardware.PromptColumnSelection();
        }
    }
}