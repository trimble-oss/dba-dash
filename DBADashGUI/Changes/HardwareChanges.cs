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

namespace DBADashGUI
{
    public partial class HardwareChanges : UserControl
    {
        public HardwareChanges()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            refreshHistory();
            refreshHardware();
        }

        private void refreshHistory()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.HostUpgradeHistory_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();

                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void refreshHardware()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.Hardware_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvHardware.AutoGenerateColumns = false;
                    dgvHardware.DataSource = dt;
                    dgvHardware.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHardware);
        }

        private void tsRefreshHardware_Click(object sender, EventArgs e)
        {
            refreshHardware();
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void dgvHardware_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx ++)
            {
                var r = dgvHardware.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var ifiStatus = row["InstantFileInitializationEnabled"] == DBNull.Value ?  DBADashStatus.DBADashStatusEnum.NA : ((bool)row["InstantFileInitializationEnabled"] ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var offlineSchedulerStatus = row["OfflineSchedulers"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((Int32)row["OfflineSchedulers"]==0 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Critical);
                var ppStatus = row["ActivePowerPlanGUID"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((Guid)row["ActivePowerPlanGUID"] == Common.HighPerformancePowerPlanGUID ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var priorityStatus = row["os_priority_class"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA: ((Int32)row["os_priority_class"] == 32 ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);
                var affinityStatus = row["affinity_type_desc"] == DBNull.Value ? DBADashStatus.DBADashStatusEnum.NA : ((string)row["affinity_type_desc"] == "AUTO" ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.Warning);

                r.Cells[colAffinity.Index].Style.BackColor = DBADashStatus.GetStatusColour(affinityStatus);
                r.Cells[colPriority.Index].Style.BackColor = DBADashStatus.GetStatusColour(priorityStatus);
                r.Cells[colOfflineSchedulers.Index].Style.BackColor = DBADashStatus.GetStatusColour(offlineSchedulerStatus);
                r.Cells[colPowerPlan.Index].Style.BackColor = DBADashStatus.GetStatusColour(ppStatus);
                r.Cells[colInstantFileInitialization.Index].Style.BackColor = DBADashStatus.GetStatusColour(ifiStatus);
            }

        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHardware);
        }
    }
}
