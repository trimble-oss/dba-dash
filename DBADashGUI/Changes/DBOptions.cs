using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Changes
{
    public partial class DBOptions : UserControl, ISetContext, INavigation
    {
        private List<int> InstanceIDs;
        private int DatabaseID;
        private string InstanceGroupName;
        private string _RowFilter;

        private string RowFilter
        {
            get => _RowFilter;
            set
            {
                _RowFilter = value;
                tsClearFilter.Text = "Clear Filter" + (string.IsNullOrEmpty(value) ? "" : " : " + RowFilter);
            }
        }

        private const int MAX_VLF_WARNING_THRESHOLD = 1000;
        private const int MAX_VLF_CRITICAL_THRESHOLD = 10000;

        public DBOptions()
        {
            InitializeComponent();
        }

        private static readonly DataGridViewColumn[] SummaryCols =
        {           new DataGridViewLinkColumn(){ Name="Instance", HeaderText="Instance", DataPropertyName="Instance", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Page Verify Not Optimal", HeaderText="Page Verify Not Optimal", DataPropertyName="Page Verify Not Optimal", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Page verify should be set to CHECKSUM which can help detect corruption"},
                    new DataGridViewLinkColumn(){ Name="Auto Close", HeaderText="Auto Close", DataPropertyName="Auto Close", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Auto close should be set to OFF.  Opening and closing the database after each connection can result in performance issues"},
                    new DataGridViewLinkColumn(){ Name="Auto Shrink", HeaderText="Auto Shrink", DataPropertyName="Auto Shrink", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText ="Auto shrink should be set to OFF.  Constant growing and shrinking of database files will result in performance issues."},
                    new DataGridViewLinkColumn(){ Name="Auto Create Stats Disabled", HeaderText="Auto Create Stats Disabled", DataPropertyName="Auto Create Stats Disabled", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Auto create statistics should be enabled. Statistics enable the query optimizer to make better decisions about how to process your query."},
                    new DataGridViewLinkColumn(){ Name="Auto Update Stats Disabled", HeaderText="Auto Update Stats Disabled", DataPropertyName="Auto Update Stats Disabled", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText ="Auto update statistics should be enabled. Up-to-date statistics enable the query optimizer to make better decisions about how to process your query." },
                    new DataGridViewLinkColumn(){ Name="Old Compat Level", HeaderText="Old Compat Level", DataPropertyName="Old Compat Level", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Use the latest compatibility level to benefit from new performance improvements and features. \nWARNING: Changing the database compatibility level has some risk associated with it and it can also result in performance degradation."},
                    new DataGridViewLinkColumn(){ Name="Trustworthy", HeaderText="Trustworthy", DataPropertyName="Trustworthy", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Trustworthy should be set to OFF.  This setting has security risks associated with it."},
                    new DataGridViewLinkColumn(){ Name="Online", HeaderText="Online", DataPropertyName="Online", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database is online ready for queries."},
                    new DataGridViewLinkColumn(){ Name="Offline", HeaderText="Offline", DataPropertyName="Offline", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database has been taken offline and is unavailable"},
                    new DataGridViewLinkColumn(){ Name="Restoring", HeaderText="Restoring", DataPropertyName="Restoring", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database is restoring."},
                    new DataGridViewLinkColumn(){ Name="Recovering", HeaderText="Recovering", DataPropertyName="Recovering", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database is waiting for recovery process to complete before it becomes online"},
                    new DataGridViewLinkColumn(){ Name="Recovery Pending", HeaderText="Recovery Pending", DataPropertyName="Recovery Pending", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "SQL Server encountered a resource related error during recovery.  Files could be missing or system resource limitations might be preventing the database from starting."},
                    new DataGridViewLinkColumn(){ Name="Suspect", HeaderText="Suspect", DataPropertyName="Suspect", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database couldn't be recovered and might be damaged."},
                    new DataGridViewLinkColumn(){ Name="Emergency", HeaderText="Emergency", DataPropertyName="Emergency", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "User has changed the database state to EMERGENCY.  The database is in single-user mode to allow repair."},
                    new DataGridViewLinkColumn(){ Name="Standby", HeaderText="Standby", DataPropertyName="Standby", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Database is restoring and has been brought online with STANDBY for read-only access."},
                    new DataGridViewLinkColumn(){ Name="Max VLF Count", HeaderText="Max VLF Count", DataPropertyName="Max VLF Count", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Too many virtual log files (VLF) can slow down log backups and database recovery."},
                    new DataGridViewLinkColumn(){ Name="Not Using Indirect Checkpoints", HeaderText="Not Using Indirect Checkpoints", DataPropertyName="Not Using Indirect Checkpoints", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Indirect checkpoints can improve database recovery time and reduce checkpoint related I/O spiking.\nIntroduced in SQL 2012 and became the default in SQL 2016.\nNote: In some cases indirect checkpoints might cause performance degradation."},
                    new DataGridViewLinkColumn(){ Name="None-Default Target Recovery Time", HeaderText="None-Default Target Recovery Time", DataPropertyName="None-Default Target Recovery Time", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Defaults are:\n0 - Automatic checkpoints.  Old default.\n60 - Indirect checkpoints. Default from SQL 2016.  Can improve recovery time and reduce checkpoint related I/O spiking."},
                    new DataGridViewLinkColumn(){ Name="RCSI Count", HeaderText="RCSI Count", DataPropertyName="RCSI Count", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Count of user databases using read committed snapshot isolation level.\nRead Committed Isolation Level (RCSI) allows read and write queries to run concurrently without blocking each other."},
                    new DataGridViewLinkColumn(){ Name="User Database Count", HeaderText="User Database Count", DataPropertyName="User Database Count", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, ToolTipText = "Count of databases excluding system databases."},
        };

        public bool SummaryMode
        {
            get => DatabaseID <= 0 && tsDetail.Visible;
            set
            {
                tsDetail.Visible = value;
                tsSummary.Visible = !value;
                if (value)
                {
                    RowFilter = string.Empty;
                    InstanceGroupName = string.Empty;
                }
            }
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            DatabaseID = _context.DatabaseID;
            RowFilter = string.Empty;
            InstanceGroupName = string.Empty;
            SummaryMode = true;
            RefreshData();
        }

        private void RefreshData()
        {
            if (InstanceIDs == null) return;
            RefreshHistory();
            if (SummaryMode)
            {
                RefreshDBSummary();
            }
            else
            {
                RefreshDBInfo();
            }
        }

        private void Pivot(ref DataTable dt)
        {
            var pivotDT = new DataTable();
            pivotDT.Columns.Add("Setting");
            pivotDT.Columns.Add("Value");
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName is not "InstanceID" and not "DatabaseID")
                {
                    var r = pivotDT.NewRow();
                    r[0] = col.ColumnName;
                    r[1] = dt.Rows[0][col];
                    pivotDT.Rows.Add(r);
                }
            }

            dgv.AutoGenerateColumns = true;
            dgv.DataSource = pivotDT;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void RefreshDBSummary()
        {
            tsClearFilter.Visible = false;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                dgv.DataSource = null;
                dgv.AutoGenerateColumns = false;
                dgv.Columns.Clear();
                dgv.Columns.AddRange(SummaryCols);
                dgv.ApplyTheme();
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
        }

        public DataTable GetDBInfo()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", InstanceGroupName);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void RefreshDBInfo()
        {
            var dt = GetDBInfo();
            tsClearFilter.Enabled = RowFilter != string.Empty;
            if (dt.Rows.Count == 1 && DatabaseID > 0)
            {
                tsSummary.Visible = false;
                tsClearFilter.Visible = false;
                tsDetail.Visible = false;
                Pivot(ref dt);
            }
            else
            {
                tsSummary.Visible = true;
                tsClearFilter.Visible = true;
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = true;
                var dv = new DataView(dt, RowFilter, string.Empty, DataViewRowState.CurrentRows);
                dgv.DataSource = dv;
                dgv.Columns["InstanceID"].Visible = false;
                dgv.Columns["DatabaseID"].Visible = false;
                dgv.Columns["LastGoodCheckDBStatus"].Visible = false;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.HeaderText = col.HeaderText.Titleize();
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
                dgv.Columns[1].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[2].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[3].Frozen = Common.FreezeKeyColumn;
                dgv.ApplyTheme();
            }
        }

        private void RefreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBOptionsHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("ExcludeStateChanges", excludeStateChangesToolStripMenuItem.Checked);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", InstanceGroupName);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                foreach (DataRow r in dt.Rows)
                {
                    if (r["OldValue"].GetType() == typeof(byte[]))
                    {
                        r["OldValue"] = Common.ByteArrayToString((byte[])r["OldValue"]);
                    }
                    if (r["NewValue"].GetType() == typeof(byte[]))
                    {
                        r["NewValue"] = Common.ByteArrayToString((byte[])r["NewValue"]);
                    }
                }
                dgvHistory.AutoGenerateColumns = false;
                dgvHistory.DataSource = dt;
                dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dgvHistory.ApplyTheme();
            }
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHistory);
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsRefreshInfo_Click(object sender, EventArgs e)
        {
            if (SummaryMode)
            {
                RefreshDBSummary();
            }
            else
            {
                RefreshDBInfo();
            }
        }

        private void TsCopyInfo_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void ExcludeStateChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsSummary_Click(object sender, EventArgs e)
        {
            ShowSummary();
        }

        private void ShowSummary()
        {
            var historyRefresh = !string.IsNullOrEmpty(InstanceGroupName);
            dgv.DataSource = null;
            SummaryMode = true;
            RefreshDBSummary();
            if (historyRefresh)
            {
                RefreshHistory();
            }
        }

        private void TsDetail_Click(object sender, EventArgs e)
        {
            var historyRefresh = string.IsNullOrEmpty(InstanceGroupName);
            RowFilter = string.Empty;
            SummaryMode = false;
            RefreshDBInfo();
            if (historyRefresh)
            {
                RefreshHistory();
            }
        }

        private void DBOptions_Load(object sender, EventArgs e)
        {
            if (DatabaseID > 0)
            {
                SummaryMode = false;
            }
        }

        private void Detail_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!dgv.Columns.Contains("database_id")) return;

            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];
                var maxCompatLevel = Convert.ToInt16(r.Cells["MaxSupportedCompatibilityLevel"].Value);
                r.Cells["compatibility_level"].SetStatusColor(Convert.ToInt16(r.Cells["compatibility_level"].Value) < maxCompatLevel
                    ? DBADashStatus.DBADashStatusEnum.Warning
                    : DBADashStatus.DBADashStatusEnum.OK);

                r.Cells["page_verify_option_desc"].SetStatusColor(r.Cells["page_verify_option_desc"].Value as string == "CHECKSUM"
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical);
                r.Cells["page_verify_option"].SetStatusColor(r.Cells["page_verify_option_desc"].Value as string == "CHECKSUM"
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical);
                r.Cells["is_auto_create_stats_on"].SetStatusColor(r.Cells["is_auto_create_stats_on"].Value as bool? == true
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Warning);
                r.Cells["is_auto_update_stats_on"].SetStatusColor(r.Cells["is_auto_update_stats_on"].Value as bool? == true
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Warning);
                r.Cells["is_auto_close_on"].SetStatusColor(r.Cells["is_auto_close_on"].Value as bool? == false
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical);
                r.Cells["is_auto_shrink_on"].SetStatusColor(r.Cells["is_auto_shrink_on"].Value as bool? == false
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical);
                r.Cells["LastGoodCheckDBStatus"].SetStatusColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(r.Cells["LastGoodCheckDBStatus"].Value));
                r.Cells["LastGoodCheckDbTime"].SetStatusColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(r.Cells["LastGoodCheckDBStatus"].Value));

                if (Convert.ToInt32(r.Cells["database_id"].Value) == 4) // msdb
                {
                    r.Cells["is_trustworthy_on"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }
                else
                {
                    r.Cells["is_trustworthy_on"].SetStatusColor(r.Cells["is_trustworthy_on"].Value as bool? == false
                        ? DBADashStatus.DBADashStatusEnum.OK
                        : DBADashStatus.DBADashStatusEnum.Critical);
                }

                if (r.Cells["target_recovery_time_in_seconds"].Value == DBNull.Value)
                {
                    r.Cells["target_recovery_time_in_seconds"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }
                else
                {
                    r.Cells["target_recovery_time_in_seconds"].SetStatusColor(
                        r.Cells["target_recovery_time_in_seconds"].Value as int? == 60
                            ? DBADashStatus.DBADashStatusEnum.OK
                            : DBADashStatus.DBADashStatusEnum.Warning);
                }

                switch (Convert.ToInt32(r.Cells["state"].Value))
                {
                    case 0: // Online
                        r.Cells["state"].SetStatusColor(DBADashStatus.DBADashStatusEnum.OK);
                        r.Cells["state_desc"].SetStatusColor(DBADashStatus.DBADashStatusEnum.OK); break;

                    case 1: // Restoring
                    case 6: // Offline
                    case 7: // Copying
                    case 10: // Offline Secondary
                        r.Cells["state"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                        r.Cells["state_desc"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                        break;

                    case 2: // Recovering
                        r.Cells["state"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Warning);
                        r.Cells["state_desc"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Warning);
                        break;

                    case 3: // Recovery Pending
                    case 4: // Suspect
                    case 5: // Emergency
                        r.Cells["state"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
                        r.Cells["state_desc"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
                        break;
                }

                if (r.Cells["VLFCount"].Value == DBNull.Value)
                {
                    r.Cells["VLFCount"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }
                else
                {
                    switch (Convert.ToInt32(r.Cells["VLFCount"].Value))
                    {
                        case > MAX_VLF_CRITICAL_THRESHOLD:
                            r.Cells["VLFCount"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
                            break;

                        case > MAX_VLF_WARNING_THRESHOLD:
                            r.Cells["VLFCount"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Warning);
                            break;

                        default:
                            r.Cells["VLFCount"].SetStatusColor(DBADashStatus.DBADashStatusEnum.OK);
                            break;
                    }
                }
            }
        }

        private void Summary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var warningCols = new[] { "Auto Create Stats Disabled", "Auto Update Stats Disabled", "Old Compat Level", "Recovering", "Offline", "Trustworthy", "Not Using Indirect Checkpoints", "None-Default Target Recovery Time" };
            var criticalCols = new[] { "Page Verify Not Optimal", "Auto Close", "Auto Shrink", "Suspect", "Emergency", "Recovery Pending" };
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];

                foreach (var col in warningCols)
                {
                    if (r.Cells[col].Value == DBNull.Value)
                    {
                        r.Cells[col].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                    }
                    else
                    {
                        r.Cells[col].SetStatusColor((int)r.Cells[col].Value > 0 ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.OK);
                    }
                }
                foreach (var col in criticalCols)
                {
                    if (r.Cells[col].Value == DBNull.Value)
                    {
                        r.Cells[col].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                    }
                    else
                    {
                        r.Cells[col].SetStatusColor((int)r.Cells[col].Value > 0 ? DBADashStatus.DBADashStatusEnum.Critical : DBADashStatus.DBADashStatusEnum.OK);
                    }
                }
                var vlfStatus = DBADashStatus.DBADashStatusEnum.NA;
                if (r.Cells["Max VLF Count"].Value != DBNull.Value)
                {
                    vlfStatus = (int)r.Cells["Max VLF Count"].Value > MAX_VLF_CRITICAL_THRESHOLD ? DBADashStatus.DBADashStatusEnum.Critical : ((int)r.Cells["Max VLF Count"].Value > MAX_VLF_WARNING_THRESHOLD ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.OK);
                }

                r.Cells["Max VLF Count"].SetStatusColor(vlfStatus);
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (SummaryMode)
            {
                Summary_RowsAdded(sender, e);
            }
            else
            {
                Detail_RowsAdded(sender, e);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHistory);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgv.PromptColumnSelection();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!SummaryMode || e.RowIndex < 0) return;
            var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
            InstanceGroupName = (string)row["Instance"];
            SummaryMode = false;
            if (e.ColumnIndex == dgv.Columns["Instance"]?.Index)
            {
                RowFilter = string.Empty;
            }
            else if (e.ColumnIndex == dgv.Columns["Page Verify Not Optimal"]?.Index)
            {
                RowFilter = "page_verify_option <> 2";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Close"]?.Index)
            {
                RowFilter = "is_auto_close_on = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Shrink"]?.Index)
            {
                RowFilter = "is_auto_shrink_on = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Create Stats Disabled"]?.Index)
            {
                RowFilter = "is_auto_create_stats_on = 0";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Update Stats Disabled"]?.Index)
            {
                RowFilter = "is_auto_update_stats_on = 0";
            }
            else if (e.ColumnIndex == dgv.Columns["Trustworthy"]?.Index)
            {
                RowFilter = "is_trustworthy_on = 1 AND DatabaseName<> 'msdb'";
            }
            else if (e.ColumnIndex == dgv.Columns["Online"]?.Index)
            {
                RowFilter = "state = 0";
            }
            else if (e.ColumnIndex == dgv.Columns["Restoring"]?.Index)
            {
                RowFilter = "state = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["Recovering"]?.Index)
            {
                RowFilter = "state = 2";
            }
            else if (e.ColumnIndex == dgv.Columns["Recovery Pending"]?.Index)
            {
                RowFilter = "state = 3";
            }
            else if (e.ColumnIndex == dgv.Columns["Suspect"]?.Index)
            {
                RowFilter = "state = 4";
            }
            else if (e.ColumnIndex == dgv.Columns["Emergency"]?.Index)
            {
                RowFilter = "state = 5";
            }
            else if (e.ColumnIndex == dgv.Columns["Offline"]?.Index)
            {
                RowFilter = "state IN(6, 10)";
            }
            else if (e.ColumnIndex == dgv.Columns["Not Using Indirect Checkpoints"]?.Index)
            {
                RowFilter = "target_recovery_time_in_seconds=0 and database_id>4";
            }
            else if (e.ColumnIndex == dgv.Columns["None-Default Target Recovery Time"]?.Index)
            {
                RowFilter = "target_recovery_time_in_seconds NOT IN(0,60)";
            }
            else if (e.ColumnIndex == dgv.Columns["Old Compat Level"]?.Index)
            {
                RowFilter = "compatibility_level < " + Convert.ToInt32(row["Max Supported Compatibility Level"]);
            }
            else if (e.ColumnIndex == dgv.Columns["Max VLF Count"]?.Index)
            {
                RowFilter = "VLFCount >= " + Math.Min(MAX_VLF_WARNING_THRESHOLD + 1, Convert.ToInt32(row["Max VLF Count"]));
            }
            else if (e.ColumnIndex == dgv.Columns["User Database Count"]?.Index)
            {
                RowFilter = "database_id > 4";
            }
            else if (e.ColumnIndex == dgv.Columns["Standby"]?.Index)
            {
                RowFilter = "is_in_standby = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["RCSI Count"]?.Index)
            {
                RowFilter = "is_read_committed_snapshot_on=1";
            }
            else
            {
                RowFilter = "";
            }
            RefreshData();
        }

        public bool CanNavigateBack => InstanceGroupName != string.Empty;

        public bool NavigateBack()
        {
            if (!CanNavigateBack) return false;

            ShowSummary();
            return true;
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            RowFilter = string.Empty;
            ((DataView)dgv.DataSource).RowFilter = RowFilter;
            tsClearFilter.Enabled = false;
        }
    }
}