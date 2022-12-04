using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class QueryStore : UserControl, INavigation, ISetContext
    {
        public QueryStore()
        {
            InitializeComponent();
        }

        private string Instance = string.Empty;
        private List<Int32> InstanceIDs;
        private int DatabaseID = -1;

        public bool CanNavigateBack => tsBack.Enabled;

        public bool CanNavigateForward => throw new NotImplementedException();

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            Instance = context.InstanceName;
            DatabaseID = context.DatabaseID;
            RefreshData();
        }

        public void RefreshData()
        {
            DataTable dt;
            tsBack.Visible = InstanceIDs.Count > 0;
            dgv.DataSource = null;
            if (Instance != string.Empty && Instance != null)
            {
                tsBack.Enabled = true;
                dt = GetDatabaseQueryStoreOptions();
                SetCols();

            }
            else
            {
                tsBack.Enabled = false;
                dt = GetDatabaseQueryStoreOptionsSummary();
                SetSummaryCols();
            }
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            if (dt.Rows.Count == 1 && DatabaseID > 0)
            {
                Common.PivotDGV(ref dgv);

            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void SetCols()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colDB", HeaderText = "DB", DataPropertyName = "name", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Desired State", DataPropertyName = "desired_state_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Actual State", DataPropertyName = "actual_state_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_ReadOnlyReason", HeaderText = "Read Only Reason", DataPropertyName = "readonly_reason_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Query Capture Mode", DataPropertyName = "query_capture_mode_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Sized Based Cleanup", DataPropertyName = "size_based_cleanup_mode_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Wait Stats Capture", DataPropertyName = "wait_stats_capture_mode_desc" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Stale Query Threshold (Days)", DataPropertyName = "stale_query_threshold_days" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Plans Per Query", DataPropertyName = "max_plans_per_query" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Current Size MB", DataPropertyName = "current_storage_size_mb", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Size MB", DataPropertyName = "max_storage_size_mb", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Actual State Additional Info", DataPropertyName = "actual_state_additional_info" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Flush Interval (Sec)", DataPropertyName = "flush_interval_seconds" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Execution Count", DataPropertyName = "capture_policy_execution_count" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Stale Threshold (Hrs)", DataPropertyName = "capture_policy_stale_threshold_hours" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Total Compile Time (ms)", DataPropertyName = "capture_policy_total_compile_cpu_time_ms" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Total Execution Time (ms)", DataPropertyName = "capture_policy_total_execution_cpu_time_ms" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_SnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
        }

        private void SetSummaryCols()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn { Name = "colInstance", HeaderText = "Instance", DataPropertyName = "InstanceGroupName", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "OFF", DataPropertyName = "QS_OFF" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_READ_ONLY", HeaderText = "READ_ONLY", DataPropertyName = "QS_READ_ONLY" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "READ_WRITE", DataPropertyName = "QS_READ_WRITE" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_ERROR", HeaderText = "ERROR", DataPropertyName = "QS_ERROR" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Total Size (MB)", DataPropertyName = "TotalCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Avg Size (MB)", DataPropertyName = "AvgCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Size (MB)", DataPropertyName = "MaxCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Limit (MB)", DataPropertyName = "MaxSizeLimitMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Min Limit (MB)", DataPropertyName = "MinSizeLimitMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_SnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
        }

        private DataTable GetDatabaseQueryStoreOptions()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseQueryStoreOptions_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceGroupName", Instance);
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable GetDatabaseQueryStoreOptionsSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabaseQueryStoreOptionsSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void Dgv_CellContent_Click(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "colInstance" && e.RowIndex >= 0)
            {
                Instance = (string)dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                RefreshData();
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "colDB" && e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                DatabaseID = (int)row["DatabaseID"];
                RefreshData();
            }
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (!tsBack.Enabled)
            {
                return false;
            }
            if (DatabaseID > 0)
            {
                DatabaseID = -1;
            }
            else
            {
                Instance = String.Empty;
            }
            RefreshData();
            return true;
        }

        public bool NavigateForward()
        {
            throw new NotImplementedException();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            bool summaryMode = dgv.Columns.Contains("col_READ_ONLY");
            bool dbsMode = dgv.Columns.Contains("colDB)");
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                if (summaryMode)
                {
                    var readOnlyAttention = (int)row["QS_READ_ONLY_ATT"];
                    dgv.Rows[idx].Cells["col_READ_ONLY"].SetStatusColor(readOnlyAttention > 0 ? DashColors.Warning : Color.White);

                    var qsErrorCount = (int)row["QS_ERROR"];
                    dgv.Rows[idx].Cells["col_ERROR"].SetStatusColor(qsErrorCount > 0 ? DashColors.Fail : Color.White);
                }
                else if (dbsMode)
                {
                    var readOnlyErrorState = (bool)row["IsReadOnlyErrorState"];
                    dgv.Rows[idx].Cells["col_ReadOnlyReason"].SetStatusColor(readOnlyErrorState ? DashColors.Warning : Color.White);
                }
                if (dbsMode || summaryMode)
                {
                    var snapshotStatus = (int)row["CollectionDateStatus"];
                    dgv.Rows[idx].Cells["col_SnapshotDate"].SetStatusColor((DBADashStatus.DBADashStatusEnum)snapshotStatus);
                }
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

    }
}
