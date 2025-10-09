using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class QueryStore : UserControl, INavigation, ISetContext
    {
        public QueryStore()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private string Instance = string.Empty;
        private List<int> InstanceIDs;
        private int DatabaseID = -1;

        public bool CanNavigateBack => tsBack.Enabled;

        public bool CanNavigateForward => throw new NotImplementedException();

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            Instance = _context.InstanceName;
            DatabaseID = _context.DatabaseID;
            RefreshData();
        }

        public void RefreshData()
        {
            DataTable dt;
            tsBack.Visible = InstanceIDs.Count > 0;
            dgv.DataSource = null;
            if (!string.IsNullOrEmpty(Instance))
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
            dgv.DataSource = new DataView(dt);
            if (dt.Rows.Count == 1 && DatabaseID > 0)
            {
                Common.PivotDGV(ref dgv);
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.ApplyTheme();
        }

        private void SetCols()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colDB", HeaderText = "DB", DataPropertyName = "name", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Database name" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Desired State", DataPropertyName = "desired_state_desc", ToolTipText = "The desired operation mode of query store, explicitly set by user" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Actual State", DataPropertyName = "actual_state_desc", ToolTipText = "The actual operation mode of query store.  \nThis can be different from the actual state as it can be put into a READ_ONLY or ERROR state in certain situations." });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_ReadOnlyReason", HeaderText = "Read Only Reason", DataPropertyName = "readonly_reason_desc", ToolTipText = "Returns the reason query store is READ_ONLY when the desired state is READ_WRITE." });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Query Capture Mode", DataPropertyName = "query_capture_mode_desc", ToolTipText = "Query Store capture mode.  \ne.g. ALL, AUTO, NONE, CUSTOM" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Sized Based Cleanup", DataPropertyName = "size_based_cleanup_mode_desc", ToolTipText = "Controls whether cleanup is automatically triggered when query store is close to it's maximum size" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Wait Stats Capture", DataPropertyName = "wait_stats_capture_mode_desc", ToolTipText = "Controls if wait stats are captured by query store. \nApplies to SQL 2017 and later" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Stale Query Threshold (Days)", DataPropertyName = "stale_query_threshold_days", ToolTipText = "Query store retention policy in days. \nA value of 0 disables the retention policy. " });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Plans Per Query", DataPropertyName = "max_plans_per_query", ToolTipText = "Limit on the maximum number of plans stored per query.  \nIf the maximum is reached, query store stops collecting new plans for the query. \nA value of 0 disables this limit." });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Current Size MB", DataPropertyName = "current_storage_size_mb", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "Current size of query store in megabytes" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Size MB", DataPropertyName = "max_storage_size_mb", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "Maximum size of query store in megabytes." });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Actual State Additional Info", DataPropertyName = "actual_state_additional_info", ToolTipText = "Not currently used", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Flush Interval (Sec)", DataPropertyName = "flush_interval_seconds", ToolTipText = "The frequency query store data is flushed to disk in seconds.  Default is 900 (15min)" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Execution Count", DataPropertyName = "capture_policy_execution_count", ToolTipText = "The execution count threshold a query must reach within the stale threshold of the custom capture policy.\nApplies to SQL 2019 and later" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Stale Threshold (Hrs)", DataPropertyName = "capture_policy_stale_threshold_hours", ToolTipText = "The interval used for custom capture policy.\nApplies to SQL 2019 and later" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Total Compile Time (ms)", DataPropertyName = "capture_policy_total_compile_cpu_time_ms", ToolTipText = "The total compile time a query must accumulate within the stale threshold of the custom capture policy.\nApplies to SQL 2019 and later" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Policy Total Execution CPU Time (ms)", DataPropertyName = "capture_policy_total_execution_cpu_time_ms", ToolTipText = "The total execution CPU time a query must accumulate within the stale threshold of the custom capture policy.\nApplies to SQL 2019 and later" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_SnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", ToolTipText = "Date of last collection for DatabaseQueryStoreOptions (sys.database_query_store_options)" });
        }

        private void SetSummaryCols()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn { Name = "colInstance", HeaderText = "Instance", DataPropertyName = "InstanceGroupName", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "OFF", DataPropertyName = "QS_OFF", ToolTipText = "Count of databases where actual state (from sys.database_query_store_options) is 0 (OFF)" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_READ_ONLY", HeaderText = "READ_ONLY", DataPropertyName = "QS_READ_ONLY", ToolTipText = "Count of databases where actual state (from sys.database_query_store_options) is 1 (READ_ONLY)" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "READ_WRITE", DataPropertyName = "QS_READ_WRITE", ToolTipText = "Count of databases where actual state (from sys.database_query_store_options) is 2 (READ_WRITE)" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_ERROR", HeaderText = "ERROR", DataPropertyName = "QS_ERROR", ToolTipText = "Count of databases where actual state (from sys.database_query_store_options) is 3 (ERROR)" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Total Size (MB)", DataPropertyName = "TotalCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "SUM of current_storage_size_mb from sys.database_query_store_options" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Avg Size (MB)", DataPropertyName = "AvgCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "AVG of current_storage_size_mb from sys.database_query_store_options" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Size (MB)", DataPropertyName = "MaxCurrentStorageSizeMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "MAX of current_storage_size_mb from sys.database_query_store_options" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Limit (MB)", DataPropertyName = "MaxSizeLimitMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "MAX of max_storage_size_mb from sys.database_query_store_options" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Min Limit (MB)", DataPropertyName = "MinSizeLimitMB", DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" }, ToolTipText = "MIN of max_storage_size_mb from sys.database_query_store_options" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "col_SnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", ToolTipText = "Date of last collection for DatabaseQueryStoreOptions (sys.database_query_store_options)" });
        }

        private DataTable GetDatabaseQueryStoreOptions()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DatabaseQueryStoreOptions_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceGroupName", Instance);
            cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private DataTable GetDatabaseQueryStoreOptionsSummary()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DatabaseQueryStoreOptionsSummary_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private void Dgv_CellContent_Click(object sender, DataGridViewCellEventArgs e)
        {
            switch (dgv.Columns[e.ColumnIndex].Name)
            {
                case "colInstance" when e.RowIndex >= 0:
                    Instance = (string)dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    RefreshData();
                    break;

                case "colDB" when e.RowIndex >= 0:
                    {
                        var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                        DatabaseID = (int)row["DatabaseID"];
                        RefreshData();
                        break;
                    }
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
                Instance = string.Empty;
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
            var summaryMode = dgv.Columns.Contains("col_READ_ONLY");
            var dbsMode = dgv.Columns.Contains("colDB)");
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                if (summaryMode)
                {
                    var readOnlyAttention = (int)row["QS_READ_ONLY_ATT"];
                    dgv.Rows[idx].Cells["col_READ_ONLY"].SetStatusColor(readOnlyAttention > 0 ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.OK);

                    var qsErrorCount = (int)row["QS_ERROR"];
                    dgv.Rows[idx].Cells["col_ERROR"].SetStatusColor(qsErrorCount > 0 ? DBADashStatus.DBADashStatusEnum.Critical : DBADashStatus.DBADashStatusEnum.OK);
                }
                else if (dbsMode)
                {
                    var readOnlyErrorState = (bool)row["IsReadOnlyErrorState"];
                    dgv.Rows[idx].Cells["col_ReadOnlyReason"].SetStatusColor(readOnlyErrorState ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.OK);
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
            dgv.ExportToExcel();
        }
    }
}