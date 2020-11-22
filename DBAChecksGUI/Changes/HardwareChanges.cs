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

namespace DBAChecksGUI
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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.HostUpgradeHistory_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
            }
        }

        private void refreshHardware()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Hardware_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvHardware.AutoGenerateColumns = false;
                dgvHardware.DataSource = dt;
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
                var ifiStatus = row["InstantFileInitializationEnabled"] == DBNull.Value ?  DBAChecksStatus.DBAChecksStatusEnum.NA : ((bool)row["InstantFileInitializationEnabled"] ? DBAChecksStatus.DBAChecksStatusEnum.OK : DBAChecksStatus.DBAChecksStatusEnum.Warning);
                var offlineSchedulerStatus = row["OfflineSchedulers"] == DBNull.Value ? DBAChecksStatus.DBAChecksStatusEnum.NA : ((Int32)row["OfflineSchedulers"]==0 ? DBAChecksStatus.DBAChecksStatusEnum.OK : DBAChecksStatus.DBAChecksStatusEnum.Critical);
                var ppStatus = row["ActivePowerPlanGUID"] == DBNull.Value ? DBAChecksStatus.DBAChecksStatusEnum.NA : ((Guid)row["ActivePowerPlanGUID"] == Common.HighPerformancePowerPlanGUID ? DBAChecksStatus.DBAChecksStatusEnum.OK : DBAChecksStatus.DBAChecksStatusEnum.Warning);
                var priorityStatus = row["os_priority_class"] == DBNull.Value ? DBAChecksStatus.DBAChecksStatusEnum.NA: ((Int32)row["os_priority_class"] == 32 ? DBAChecksStatus.DBAChecksStatusEnum.OK : DBAChecksStatus.DBAChecksStatusEnum.Warning);
                var affinityStatus = row["affinity_type_desc"] == DBNull.Value ? DBAChecksStatus.DBAChecksStatusEnum.NA : ((string)row["affinity_type_desc"] == "AUTO" ? DBAChecksStatus.DBAChecksStatusEnum.OK : DBAChecksStatus.DBAChecksStatusEnum.Warning);

                r.Cells[colAffinity.Index].Style.BackColor = DBAChecksStatus.GetStatusColour(affinityStatus);
                r.Cells[colPriority.Index].Style.BackColor = DBAChecksStatus.GetStatusColour(priorityStatus);
                r.Cells[colOfflineSchedulers.Index].Style.BackColor = DBAChecksStatus.GetStatusColour(offlineSchedulerStatus);
                r.Cells[colPowerPlan.Index].Style.BackColor = DBAChecksStatus.GetStatusColour(ppStatus);
                r.Cells[colInstantFileInitialization.Index].Style.BackColor = DBAChecksStatus.GetStatusColour(ifiStatus);
            }

        }
    }
}
