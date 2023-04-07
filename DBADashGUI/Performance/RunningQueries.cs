using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class RunningQueries : UserControl, INavigation, ISetContext, IRefreshData
    {
        public RunningQueries()
        {
            InitializeComponent();
        }

        public int InstanceID;
        private List<int> InstanceIDs;
        private DateTime currentSnapshotDate;
        private DataTable snapshotDT;
        public DateTime SnapshotDateFrom;
        public DateTime SnapshotDateTo;
        public int SessionID = 0;

        private int blockedCount;
        private int runningJobCount;
        private bool hasWaitResource;
        private long blockedWait;

        private DataGridViewColumn[] RunningQueryColumns =>
            new DataGridViewColumn[] {
                new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", Name = "colInstance", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn},
                new DataGridViewLinkColumn() { HeaderText = "Session ID", DataPropertyName = "session_id", Name = "colSessionID", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn },
                new DataGridViewLinkColumn() { HeaderText = "Batch Text", DataPropertyName = "batch_text", Name = "colBatchText", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                new DataGridViewLinkColumn() { HeaderText = "Text", DataPropertyName = "text", Name = "colText", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                new DataGridViewLinkColumn() { HeaderText = "Plan", DataPropertyName="has_plan", Name = "colQueryPlan", SortMode = DataGridViewColumnSortMode.Automatic, Visible = true, LinkColor = DashColors.LinkColor, ToolTipText="Click link to view query plan" },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocking Session ID", DataPropertyName = "blocking_session_id", Name = "colBlockingSessionID", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, ToolTipText = "ID of the session directly blocking the current query.  0 = Not blocked.", DefaultCellStyle = new DataGridViewCellStyle(){ ForeColor=Color.White } },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocking Hierarchy", DataPropertyName = "BlockingHierarchy", SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 70, ToolTipText = "Identifies all the session IDs that are involved in the blocking chain for each query starting with the root blocker." },
                new DataGridViewCheckBoxColumn { HeaderText = "Root Blocker", DataPropertyName = "IsRootBlocker", Name = "colIsRootBlocker", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, Visible = blockedCount > 0, ToolTipText = "Root blocker is the query at the head of the blocking chain. This query is not blocked but it's holding locks that other queries are waiting for." },
                new DataGridViewLinkColumn() { HeaderText = "Blocked Count", DataPropertyName = "BlockCount", Name = "colBlockCount", SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60, ToolTipText = "Count of sessions blocked directly by the current query." , LinkColor = Color.White },
                new DataGridViewLinkColumn() { HeaderText = "Blocked Count Recursive", DataPropertyName = "BlockCountRecursive", Name = "colBlockedCountRecursive", SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60, ToolTipText = "Count of sessions blocked by the current query - directly or indirectly", LinkColor = Color.White },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocked Wait Time", DataPropertyName = "BlockWaitTime", Name = "colBlockedWaitTime", SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60, ToolTipText = "Wait time associated with sessions blocked directly by the current session." },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocked Wait Time Recursive", DataPropertyName = "BlockWaitTimeRecursive", Name = "colBlockedWaitTimeRecursive", SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60, ToolTipText = "Wait time associated with sessions blocked directly or indirectly by the current session" },
                new DataGridViewTextBoxColumn() { HeaderText = "Duration", DataPropertyName = "Duration", Name = "colDuration", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Duration (ms)", DataPropertyName = "Duration (ms)", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "CPU Time", DataPropertyName = "cpu_time", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Logical Reads", DataPropertyName = "logical_reads", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Reads", DataPropertyName = "reads", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Writes", DataPropertyName = "writes", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Granted Query Memory (Kb)", DataPropertyName = "granted_query_memory_kb", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Command", DataPropertyName = "Command", Name = "colCommand", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Status", DataPropertyName = "Status", Name = "colStatus", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time", Name = "colWaitTime", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Type", DataPropertyName = "wait_type", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewLinkColumn() { HeaderText = "Top Session Waits", DataPropertyName = "TopSessionWaits", Name = "colTopSessionWaits", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.Automatic , LinkColor = DashColors.LinkColor},
                new DataGridViewTextBoxColumn() { HeaderText = "Object ID", DataPropertyName = "object_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Object Name", DataPropertyName = "object_name", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { Name="colSnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewLinkColumn { Name="colSnapshotDateLink", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible=false,LinkColor=DashColors.LinkColor },
                new DataGridViewTextBoxColumn() { HeaderText = "% Complete", DataPropertyName = "percent_complete", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Open Transaction Count", DataPropertyName = "open_transaction_count", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Transaction Isolation Level", DataPropertyName = "transaction_isolation_level", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40  },
                new DataGridViewTextBoxColumn() { HeaderText = "Login Name", DataPropertyName = "login_name", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Host Name", DataPropertyName = "host_name", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Database ID", DataPropertyName = "database_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60 },
                new DataGridViewTextBoxColumn() { HeaderText = "Database Name", DataPropertyName = "database_names", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Program Name", DataPropertyName = "program_name", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Job ID", DataPropertyName = "job_id", SortMode = DataGridViewColumnSortMode.Automatic, Visible = runningJobCount > 0, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Job Name", DataPropertyName = "job_name", SortMode = DataGridViewColumnSortMode.Automatic, Visible = runningJobCount > 0, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Client Interface Name", DataPropertyName = "client_interface_name", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Start Time", DataPropertyName = "start_time", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Request Start Time", DataPropertyName = "last_request_start_time", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40 },
                new DataGridViewLinkColumn()   { HeaderText = "Wait Resource", DataPropertyName = "wait_resource", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, LinkColor = DashColors.LinkColor, Name="colWaitResource"},
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Resource Type", DataPropertyName = "wait_resource_type", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Database ID", DataPropertyName = "wait_database_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Database", DataPropertyName = "wait_db", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait File ID", DataPropertyName = "wait_file_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait File", DataPropertyName = "wait_file", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Page ID", DataPropertyName = "wait_page_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Object ID", DataPropertyName = "wait_object_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Object", DataPropertyName = "wait_object", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Index ID", DataPropertyName = "wait_index_id", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait HOBT", DataPropertyName = "wait_hobt", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Hash", DataPropertyName = "wait_hash", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Slot", DataPropertyName = "wait_slot", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewCheckBoxColumn() { HeaderText = "Wait Is Compile", DataPropertyName = "wait_is_compile", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Page Type", DataPropertyName = "page_type", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource },
                new DataGridViewTextBoxColumn() { HeaderText = "Login Time", DataPropertyName = "login_time", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, },
                new DataGridViewLinkColumn()  { HeaderText = "SQL Handle", DataPropertyName = "sql_handle", SortMode = DataGridViewColumnSortMode.Automatic, Name = "colSQLHandle" ,LinkColor=DashColors.LinkColor },
                new DataGridViewLinkColumn()  { HeaderText = "Plan Handle", DataPropertyName = "plan_handle", SortMode = DataGridViewColumnSortMode.Automatic, Name = "colPlanHandle",LinkColor=DashColors.LinkColor  },
                new DataGridViewLinkColumn()  { HeaderText = "Query Hash", DataPropertyName = "query_hash", SortMode = DataGridViewColumnSortMode.Automatic, Name = "colQueryHash" ,LinkColor=DashColors.LinkColor },
                new DataGridViewLinkColumn()  { HeaderText = "Query Plan Hash", DataPropertyName = "query_plan_hash", SortMode = DataGridViewColumnSortMode.Automatic, Name = "colQueryPlanHash" ,LinkColor=DashColors.LinkColor },
                new DataGridViewTextBoxColumn { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false },
            };

        private readonly DataGridViewColumn[] sessionWaitColumns = new DataGridViewColumn[]  {
                new DataGridViewTextBoxColumn() { HeaderText = "Session ID", DataPropertyName = "session_id", Name = "colSessionID", Visible=false, Frozen = Common.FreezeKeyColumn },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Type", DataPropertyName = "WaitType", Name = "colWaitType" },
                new DataGridViewTextBoxColumn() { HeaderText = "Waiting Tasks Count", DataPropertyName = "waiting_tasks_count" },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Wait Time %", DataPropertyName = "wait_pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Wait Time (ms)", DataPropertyName = "max_wait_time_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Signal Wait Time (ms)", DataPropertyName = "signal_wait_time_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Signal Wait %", DataPropertyName = "signal_wait_pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                new DataGridViewLinkColumn() { HeaderText = "Help", Text = "help", UseColumnTextForLinkValue = true, Name = "colHelp", LinkColor = DashColors.LinkColor}
        };

        public void SetContext(DBADashContext context)
        {
            if (InstanceIDs != null && context.InstanceIDs.SetEquals(InstanceIDs) && InstanceID == context.InstanceID) // Context hasn't changed
            {
                return;
            }
            currentSnapshotDate = DateTime.MinValue;
            dgv.DataSource = null;
            InstanceIDs = context.InstanceIDs.ToList();
            InstanceID = context.InstanceID;
            if (InstanceIDs is { Count: 1 })
            {
                InstanceID = InstanceIDs[0];
            }
            RefreshData();
        }

        /// <summary>
        /// Get the date of the snapshot to highlight in the snapshot list.  Either the current snapshot we are viewing or the first displayed row.  In the first case the row will be highlighted otherwise just the scroll position is set.
        /// </summary>
        /// <param name="highlightSnapshot">DateTime of snapshot to be highlighted and set as first displayed row</param>
        /// <param name="highlight">Value is true (row to be highlighted) if we are currently viewing the snapshot</param>
        private void SetHighlightBeforeRefresh(out DateTime? highlightSnapshot, out bool highlight)
        {
            if (currentSnapshotDate > DateTime.MinValue)
            {
                highlightSnapshot = currentSnapshotDate.ToLocalTime();
                highlight = true;
            }
            else if (dgv.RowCount > 0 && dgv.Columns.Contains("colSnapshotDate") && dgv.FirstDisplayedScrollingRowIndex > 0) // Note: Only maintain scroll position if we have scrolled (If index is 0, we want new rows to display rather than scrolling down)
            {
                highlight = false;
                highlightSnapshot =
                    (DateTime)dgv.Rows[dgv.FirstDisplayedScrollingRowIndex].Cells["colSnapshotDate"].Value;
            }
            else
            {
                highlight = false;
                highlightSnapshot = null;
            }
            currentSnapshotDate = DateTime.MinValue;
        }

        /// <summary>
        /// Set things back to defaults (control visibility, enabled etc)
        /// </summary>
        private void Reset()
        {
            UpdateRowLimit();
            tsEditLimit.Visible = false;
            lblRowLimit.Visible = false;
            tsWaitsFilter.Enabled = SessionID == 0;
            tsGroupByFilter.Visible = false;
            splitContainer1.Panel2Collapsed = true;
            dgvSessionWaits.DataSource = null;
            snapshotDT = null;
            tsBack.Enabled = false;
            dgv.DataSource = null;
            tsGroupBy.Enabled = false;
            lblSnapshotDate.Visible = false;
            tsPrevious.Visible = false;
            tsNext.Visible = false;
            tsGetLatest.Visible = true;
        }

        public void RefreshData()
        {
            SetHighlightBeforeRefresh(out var highlightSnapshot, out var highlight);
            Reset();
            ClearBlocking();

            if (SessionID != 0) // Show the running query snapshots for a specific session ID between specified dates
            {
                tsGetLatest.Visible = false;
                snapshotDT = RunningQueriesForSession(SessionID, SnapshotDateFrom, SnapshotDateTo, InstanceID);
                GetCounts();
                LoadSnapshot(new DataView(snapshotDT));
                dgv.Columns["colSnapshotDate"].Visible = false;
                dgv.Columns["colSnapshotDateLink"].Visible = true;
            }
            else if (SnapshotDateFrom == SnapshotDateTo && InstanceID > 0 && SnapshotDateFrom > DateTime.MinValue) // Show a specific blocking snapshot (e.g. Blocking chart drill down)
            {
                LoadSnapshot(SnapshotDateFrom);
            }
            else // List of snapshots for an instance or last snapshot for all instances
            {
                LoadSummaryData();
                if (highlightSnapshot.HasValue)
                {
                    HighlightSnapshot(highlightSnapshot.Value, highlight);
                }
            }
        }

        /// <summary>If we are not filtered for a specific instance then show server level summary</summary>
        private bool IsServerLevelSummary => !(InstanceID > 0);

        public bool CanNavigateBack => tsBack.Enabled;

        /// <summary>Get a list of running queries snapshots for an instance or last snapshot for each instance</summary>
        private void LoadSummaryData()
        {
            DataTable dt;
            tsBlockingFilter.Visible = false;
            tsGetLatest.Visible = !IsServerLevelSummary;
            tsStatus.Visible = false;
            if (IsServerLevelSummary) // Show a list of snapshots for the selected database instance
            {
                dt = RunningQueriesServerSummary();
                tsBack.Enabled = false;
            }
            else // Show the last snapshot for all instances
            {
                dt = RunningQueriesSummary();
                tsBack.Enabled = InstanceIDs is { Count: > 1 };
                lblRowLimit.Visible = dt.Rows.Count == Properties.Settings.Default.RunningQueriesSummaryMaxRows;
                tsEditLimit.Visible = true;
            }
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false, Frozen = Common.FreezeKeyColumn },
                new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", Name = "colInstance", SortMode = DataGridViewColumnSortMode.Automatic, Visible = IsServerLevelSummary, LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn },
                new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", SortMode = DataGridViewColumnSortMode.Automatic, Visible = !IsServerLevelSummary, Frozen = Common.FreezeKeyColumn },
                new DataGridViewLinkColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", Name = "colSnapshotDate", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor },
                new DataGridViewTextBoxColumn() { HeaderText = "Running Queries", DataPropertyName = "RunningQueries", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocked Queries", DataPropertyName = "BlockedQueries", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Blocked Queries Wait", DataPropertyName = "BlockedQueriesWait", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Memory Grant KB", DataPropertyName = "MaxMemoryGrantKB", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Sum Memory Grant KB", DataPropertyName = "SumMemoryGrantKB", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Longest Running Query", DataPropertyName = "LongestRunningQuery", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Critical Wait Count", DataPropertyName = "CriticalWaitCount", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "Critical Wait Time", DataPropertyName = "CriticalWaitTime", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "TempDB Wait Count", DataPropertyName = "TempDBWaitCount", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { HeaderText = "TempDB Wait Time", DataPropertyName = "TempDBWaitTime", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle }
            );
            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        /// <summary>Get running query snapshots associated with a specified session id between two dates (for associating RPC/Batch completed events with running queries)</summary>
        private static DataTable RunningQueriesForSession(int sessionID, DateTime fromDate, DateTime toDate, int instanceID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("RunningQueriesForSession_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("SessionID", sessionID);
                cmd.Parameters.Add(new SqlParameter("SnapshotDateFrom", fromDate) { DbType = DbType.DateTime2 });
                cmd.Parameters.Add(new SqlParameter("SnapshotDateTo", toDate) { DbType = DbType.DateTime2 });
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                dt.Columns["start_time_utc"].ColumnName = "start_time";
                dt.Columns["last_request_start_time_utc"].ColumnName = "last_request_start_time";
                dt.Columns["login_time_utc"].ColumnName = "login_time";
                return dt;
            }
        }

        /// <summary>Get last running query snapshot summary data for all servers</summary>
        private DataTable RunningQueriesServerSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueriesServerSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                return dt;
            }
        }

        /// <summary>Get a list of running query snapshots for the specified instance</summary>
        private DataTable RunningQueriesSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueriesSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("MaxRows", Properties.Settings.Default.RunningQueriesSummaryMaxRows);
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                return dt;
            }
        }

        /// <summary>Get running queries snapshot data for the specified snapshot date. skip parameter is used to return next snapshot (1) or previous snapshot (-1)</summary>
        private DataTable RunningQueriesSnapshot(ref DateTime snapshotDate, int skip = 0)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueries_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var pSnapshotDate = cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("Skip", skip);
                pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
                pSnapshotDate.Direction = ParameterDirection.InputOutput;
                if (snapshotDate == DateTime.MaxValue)
                {
                    pSnapshotDate.Value = DBNull.Value;
                }
                da.Fill(dt);
                snapshotDate = Convert.ToDateTime(pSnapshotDate.Value);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                dt.Columns["start_time_utc"].ColumnName = "start_time";
                dt.Columns["last_request_start_time_utc"].ColumnName = "last_request_start_time";
                dt.Columns["login_time_utc"].ColumnName = "login_time";
                return dt;
            }
        }

        /// <summary>Load a running queries snapshot for the specified date. skip parameter is used to return next snapshot (1) or previous snapshot (-1) </summary>
        private void LoadSnapshot(DateTime snapshotDate, int skip = 0)
        {
            lblRowLimit.Visible = false;
            tsEditLimit.Visible = false;
            tsGroupByFilter.Visible = false;
            snapshotDT = RunningQueriesSnapshot(ref snapshotDate, skip);
            GetCounts();
            lblSnapshotDate.Text = "Snapshot Date: " + snapshotDate.ToAppTimeZone().ToString(CultureInfo.CurrentCulture);
            LoadSnapshot(new DataView(snapshotDT));
            lblSnapshotDate.Visible = true;
            tsGetLatest.Visible = true;
            tsPrevious.Visible = true;
            tsNext.Visible = true;

            currentSnapshotDate = snapshotDate;
            tsBack.Enabled = SnapshotDateFrom == DateTime.MinValue;
        }

        /// <summary>Load a running queries snapshot</summary>
        private void LoadSnapshot(DataView source)
        {
            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(RunningQueryColumns);
            dgv.DataSource = source;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCellsExceptHeader);
            dgv.Columns["colBatchText"].Width = 200;
            dgv.Columns["colText"].Width = 200;
            dgv.Columns["colTopSessionWaits"].Width = 200;
            dgv.Columns["colQueryPlan"].Width = 90;
            dgv.Columns["colSQLHandle"].Width = 70;
            dgv.Columns["colPlanHandle"].Width = 70;
            dgv.Columns["colQueryHash"].Width = 70;
            dgv.Columns["colQueryPlanHash"].Width = 70;
            tsGroupBy.Enabled = dgv.Rows.Count > 1;
            tsBlockingFilter.Visible = SessionID == 0;
        }

        /// <summary>Get counts from the running queries snapshot table. e.g. Blocking counts</summary>
        private void GetCounts()
        {
            runningJobCount = snapshotDT.AsEnumerable().Count(r => r["job_id"] != DBNull.Value);
            blockedCount = snapshotDT.AsEnumerable().Count(r => Convert.ToInt16(r["blocking_session_id"]) != 0);
            blockedWait = snapshotDT.AsEnumerable().Where(r => Convert.ToInt16(r["blocking_session_id"]) != 0 && r["wait_time"] != DBNull.Value).Sum(r => Convert.ToInt64(r["wait_time"]));
            hasWaitResource = snapshotDT.AsEnumerable().Any(r => r["wait_resource"] != DBNull.Value && !string.IsNullOrEmpty((string)r["wait_resource"]));

            tsBlockingFilter.Text = $"Blocking ({blockedCount} Blocked)";
            tsBlockingFilter.Enabled = blockedCount > 0;
            tsStatus.Visible = SessionID == 0;
            tsStatus.Text =
                $"Blocked Sessions: {blockedCount}, Blocked Wait Time: {TimeSpan.FromMilliseconds(blockedWait):dd\\ hh\\:mm\\:ss}, Running Jobs {runningJobCount}";
            tsStatus.Font = blockedCount > 0 ? new Font(tsStatus.Font, FontStyle.Bold) : new Font(tsStatus.Font, FontStyle.Regular);
            tsStatus.ForeColor = blockedCount > 0 ? DashColors.Fail : DashColors.Success;
        }

        private static void ShowPlan(DataRowView row)
        {
            if (row["query_plan"] == DBNull.Value)
            {
                FindPlan(row);
            }
            else
            {
                var plan = (string)row["query_plan"];
                var path = System.IO.Path.GetTempFileName() + ".sqlplan";
                System.IO.File.WriteAllText(path, plan);
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                Process.Start(psi);
            }
        }

        private static void FindPlan(DataRowView row)
        {
            var db = Convert.ToString(row["database_name"]);
            var planHandle = Convert.ToString(row["plan_handle"]);
            var planHash = Convert.ToString(row["query_plan_hash"]);
            var queryHash = Convert.ToString(row["query_hash"]);
            var sqlHandle = Convert.ToString(row["sql_handle"]);
            var statementStartOffset = Convert.ToInt32(row["statement_start_offset"] == DBNull.Value ? -1 : row["statement_start_offset"]);
            var statementEndOffset = Convert.ToInt32(row["statement_end_offset"] == DBNull.Value ? -1 : row["statement_end_offset"]);
            var instance = Convert.ToString(row["InstanceDisplayName"]);
            var sql = SqlStrings.GetFindPlan(planHash, queryHash, planHandle, sqlHandle, db, statementStartOffset, statementEndOffset, instance);

            Common.ShowCodeViewer(sql, "Find Plan");
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
            string colName = dgv.Columns[e.ColumnIndex].Name;
            switch (colName)
            {
                // Drill down to view a snapshot from summary
                case "colSnapshotDate":
                    {
                        var snapshotDate = ((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc();
                        InstanceID = (int)row["InstanceID"];
                        LoadSnapshot(snapshotDate);
                        tsBack.Enabled = true;
                        break;
                    }
                // Drill down to view snapshot summary for a specific instance
                case "colInstance":
                    InstanceID = (int)row["InstanceID"];
                    RefreshData();
                    break;
                // New window with full batch text for query
                case "colBatchText":
                    {
                        string sql = (string)row["batch_text"];
                        var title = "SPID: " + Convert.ToString(row["session_id"]) + " Batch Text";
                        Common.ShowCodeViewer(sql, title);
                        break;
                    }
                // New window with full query text
                case "colText":
                    {
                        var sql = (string)row["text"];
                        var title = "SPID: " + Convert.ToString(row["session_id"]) + " Text";
                        Common.ShowCodeViewer(sql, title);
                        break;
                    }
                // save query plan and open in default tool
                case "colQueryPlan":
                    ShowPlan(row);
                    break;

                case "colPlanHandle":
                case "colQueryPlanHash":
                case "colQueryHash":
                case "colSQLHandle":
                    FindPlan(row);
                    break;
                // Running query snapshot has been grouped by some value and the user has selected to drill down - filter the grid by the selected value for the column we have grouped by
                case "colGroup":
                    {
                        GroupByFilter(e, row);
                        break;
                    }
                // Load the associated RPC/Batch completed event when the user clicks the session ID column
                case "colSessionID":
                    {
                        ShowCompletedBatchRPC(row);
                        break;
                    }
                // Show a summary of the waits for the session
                case "colTopSessionWaits":
                    {
                        ShowSessionWaits(row);
                        break;
                    }
                // Filter to show queries blocked directly by the selected session
                case "colBlockCount":
                    ShowBlocking(Convert.ToInt16(dgv.Rows[e.RowIndex].Cells["colSessionID"].Value));
                    break;
                // Filter to show queries blocked directly by the selected session
                case "colBlockedCountRecursive":
                    ShowBlocking(Convert.ToInt16(dgv.Rows[e.RowIndex].Cells["colSessionID"].Value), true);
                    break;

                case "colSnapshotDateLink":
                    {
                        ShowRunningQueriesForSnapshotDate(row);
                        break;
                    }
                case "colWaitResource":
                    DecipherWaitResource(row);
                    break;
            }
        }

        private void DecipherWaitResource(DataRowView row)
        {
            var waitResource = Convert.ToString(row["wait_resource"]);
            var instance = Convert.ToString(row["InstanceDisplayName"]);
            var sql = SqlStrings.GetDecipherWaitResource(waitResource, instance);
            Common.ShowCodeViewer(sql, "Decipher Wait Resource");
        }

        private void GroupByFilter(DataGridViewCellEventArgs e, DataRowView row)
        {
            string filter = dgv.Columns[e.ColumnIndex].DataPropertyName + "='" +
                            Convert.ToString(row[dgv.Columns[e.ColumnIndex].DataPropertyName]).Replace("'", "''") + "'";
            tsGroupByFilter.Text = filter;
            tsGroupByFilter.Visible = true;
            DataView dv;
            try
            {
                dv = new DataView(snapshotDT, filter, "", DataViewRowState.CurrentRows);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Filter error: " + filter + Environment.NewLine + ex.Message);
                return;
            }

            LoadSnapshot(dv);
            tsBack.Enabled = true;
        }

        private void ShowCompletedBatchRPC(DataRowView row)
        {
            var frm = new CompletedRPCBatchEvent()
            {
                SessionID = Convert.ToInt32(row["session_id"]),
                InstanceID = Convert.ToInt32(row["InstanceID"]),
                SnapshotDateUTC = Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc(),
                StartTimeUTC = row["start_time"] == DBNull.Value
                    ? Convert.ToDateTime(row["last_request_start_time"]).AppTimeZoneToUtc()
                    : Convert.ToDateTime(row["start_time"]).AppTimeZoneToUtc(),
                IsSleeping = Convert.ToString(row["status"]) == "sleeping"
            };
            frm.Show(this);
        }

        private void ShowSessionWaits(DataRowView row)
        {
            splitContainer1.Panel2Collapsed = false;
            if (dgvSessionWaits.Columns.Count == 0)
            {
                dgvSessionWaits.AutoGenerateColumns = false;
                dgvSessionWaits.Columns.AddRange(sessionWaitColumns);
            }

            var sessionid = (short)row["session_id"];
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, sessionid,
                Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc(),
                Convert.ToDateTime(row["login_time"]).AppTimeZoneToUtc());
            dgvSessionWaits.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            sessionToolStripMenuItem.Tag = sessionid;
            lblWaitsForSession.Text = "Waits For Session ID: " + sessionid.ToString();
        }

        private void ShowRunningQueriesForSnapshotDate(DataRowView row)
        {
            var frm = new RunningQueriesViewer()
            {
                InstanceID = InstanceID,
                SnapshotDateFrom = Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc(),
                SnapshotDateTo = Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc()
            };
            frm.Show(this);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (!CanNavigateBack)
            {
                return false;
            }
            var rowFilter = string.Empty;
            if (dgv.DataSource.GetType() == typeof(DataView))
            {
                rowFilter = ((DataView)dgv.DataSource).RowFilter;
            }
            if (dgv.Columns.Contains("colGroup") || !string.IsNullOrEmpty(rowFilter)) // Remove filter
            {
                ClearBlocking();
                LoadSnapshot(new DataView(snapshotDT));
                tsBack.Enabled = SnapshotDateFrom == DateTime.MinValue;
                tsGroupByFilter.Visible = false;
            }
            else if (!dgv.Columns.Contains("colSessionID"))
            {
                InstanceID = -1;
                RefreshData();
            }
            else
            {
                RefreshData();
            }
            return true;
        }

        /// <summary>
        /// Highlight a snapshot in the grid by setting the scroll position to the specified snapshot so that it is visible.  Row is also selected when highlight is true
        /// </summary>
        /// <param name="snapshotDate">Snapshot date to find</param>
        /// <param name="highlight">Option to select the row in addition to setting the scroll position</param>
        private void HighlightSnapshot(DateTime snapshotDate, bool highlight)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["colSnapshotDate"].Value != null && (DateTime)row.Cells["colSnapshotDate"].Value == snapshotDate)
                {
                    dgv.ClearSelection();
                    row.Selected = highlight;
                    dgv.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        private void TsGetLatest_Click(object sender, EventArgs e)
        {
            LoadSnapshot(DateTime.MaxValue);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
            dgv.FirstDisplayedScrollingRowIndex = 0; // Reset the scroll position if we click refresh as it's likely we are interested in new snapshots.
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "colQueryPlan")
            {
                e.Value = Convert.ToBoolean(e.Value) ? "View Plan" : "Find Plan";
            }
            else if (Convert.ToString(e.Value)?.Length > 1000)
            {
                e.Value = Convert.ToString(e.Value).Truncate(997) + "...";
            }
            else if ((new[] { "colBlockCount", "colBlockedCountRecursive", "colBlockingSessionID" }).Contains(dgv.Columns[e.ColumnIndex].Name))
            {
                e.CellStyle.BackColor = Convert.ToInt32(e.Value) == 0 ? DashColors.Success : DashColors.Fail;
            }
        }

        /// <summary>Takes the running query snapshot and groups the data by a specified column</summary>
        private void GroupSnapshot(string group)
        {
            if (snapshotDT == null || snapshotDT.Rows.Count <= 0) return;
            ClearBlocking();
            DataTable groupedDT = new();
            groupedDT.Columns.AddRange(
                new[] {
                    new DataColumn(group,typeof(string)),
                    new DataColumn("execution_count", typeof(long)),
                    new DataColumn("sum_cpu_time", typeof(long)),
                    new DataColumn("sum_reads", typeof(long)),
                    new DataColumn("sum_logical_reads", typeof(long)),
                    new DataColumn("sum_writes", typeof(long)),
                    new DataColumn("sum_granted_query_memory_kb", typeof(long)),
                    new DataColumn("blocked_count", typeof(long)),
                    new DataColumn("blocking_count", typeof(long)),
                    new DataColumn("root_blockers", typeof(long))
                }
            );
            groupedDT = snapshotDT.AsEnumerable()
                .GroupBy(r => r.Field<string>(group))
                .Select(g =>
                {
                    var row = groupedDT.NewRow();
                    row[group] = g.Key;
                    row["execution_count"] = g.Count();
                    row["sum_reads"] = g.Sum(r => r["reads"] == DBNull.Value ? 0 : r.Field<long>("reads"));
                    row["sum_logical_reads"] = g.Sum(r => r["logical_reads"] == DBNull.Value ? 0 : r.Field<long>("logical_reads"));
                    row["sum_writes"] = g.Sum(r => r["writes"] == DBNull.Value ? 0 : r.Field<long>("writes"));
                    row["sum_cpu_time"] = g.Sum(r => r["cpu_time"] == DBNull.Value ? 0 : r.Field<int>("cpu_time"));
                    row["sum_granted_query_memory_kb"] = g.Sum(r => r["granted_query_memory_kb"] == DBNull.Value ? 0 : Convert.ToInt64(r["granted_query_memory_kb"]));
                    row["blocked_count"] = g.Count(r => r.Field<short>("blocking_session_id") != 0);
                    row["blocking_count"] = g.Count(r => r.Field<int>("BlockCount") > 0);
                    row["root_blockers"] = g.Count(r => r.Field<bool>("IsRootBlocker"));
                    return row;
                }).CopyToDataTable();

            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewLinkColumn() { DataPropertyName = group, HeaderText = group, Name = "colGroup", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor },
                new DataGridViewTextBoxColumn() { DataPropertyName = "execution_count", HeaderText = "Execution Count", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "sum_reads", HeaderText = "Sum Reads", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "sum_logical_reads", HeaderText = "Sum Logical Reads", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "sum_writes", HeaderText = "Sum Writes", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "sum_cpu_time", HeaderText = "Sum CPU", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "sum_granted_query_memory_kb", HeaderText = "Sum Granted Query Memory (KB)", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "blocked_count", HeaderText = "Count of Queries Blocked", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "blocking_count", HeaderText = "Count of Queries Blocking", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle },
                new DataGridViewTextBoxColumn() { DataPropertyName = "root_blockers", HeaderText = "Count of Root Blockers", SortMode = DataGridViewColumnSortMode.Automatic, DefaultCellStyle = Common.DataGridViewNumericCellStyle }
            );
            dgv.DataSource = new DataView(groupedDT);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            tsBlockingFilter.Visible = false;
        }

        private void TsGroupBy_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            if (ts.Tag == null)
            {
                LoadSnapshot(new DataView(snapshotDT));
            }
            else
            {
                GroupSnapshot((string)ts.Tag);
                tsBack.Enabled = true;
            }
        }

        private void TsPrevious_Click(object sender, EventArgs e)
        {
            LoadSnapshot(currentSnapshotDate, -1);
        }

        private void TsNext_Click(object sender, EventArgs e)
        {
            LoadSnapshot(currentSnapshotDate, 1);
        }

        /// <summary>Get session level wait stats for a specified session or for all sessions</summary>
        private static DataTable GetSessionWaits(int InstanceID, short? SessionID, DateTime? SnapshotDateUTC, DateTime? LoginTimeUTC)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SessionWaits_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("SessionID", SessionID);
                cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
                cmd.Parameters.AddWithValue("LoginTimeUTC", LoginTimeUTC);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        /// <summary>Get session level wait stats for the specified snapshot over all sessions.</summary>
        private static DataTable GetSessionWaitSummary(int InstanceID, DateTime? SnapshotDateUTC)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SessionWaitsSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void TsSessionWaitCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSessionWaits);
        }

        private void TsSessionWaitExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSessionWaits);
        }

        private void AllSessionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = true;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, null, currentSnapshotDate, null);
            lblWaitsForSession.Text = "All Sessions";
        }

        private void SummaryViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = false;
            dgvSessionWaits.DataSource = GetSessionWaitSummary(InstanceID, currentSnapshotDate);
            lblWaitsForSession.Text = "Session Wait Summary";
        }

        private void SessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = false;
            var sessionid = (short)sessionToolStripMenuItem.Tag;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, sessionid, currentSnapshotDate, null);
            lblWaitsForSession.Text = "Waits For Session ID: " + sessionid.ToString();
        }

        private void DgvSessionWaits_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvSessionWaits.Columns[e.ColumnIndex].Name != "colHelp") return;
            var wait = (string)dgvSessionWaits.Rows[e.RowIndex].Cells["colWaitType"].Value;
            var psi = new ProcessStartInfo("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/") { UseShellExecute = true };
            Process.Start(psi);
        }

        private void ShowRootBlockersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowRootBlockers();
        }

        /// <summary>Filter to show root blockers - queries holding locks needed by other queries that are not blocked themselves</summary>
        public void ShowRootBlockers()
        {
            ShowBlocking(0);
        }

        /// <summary>Filter to show the queries blocked by a particular session.  For SessionID=0, show root blockers.</summary>
        private void ShowBlocking(short sessionID, bool recursive = false) //
        {
            tsGroupByFilter.Visible = false;
            var dv = (DataView)dgv.DataSource;
            if (sessionID == 0) // Root blockers
            {
                dv.RowFilter = $"blocking_session_id = {sessionID} AND BlockCount > 0 ";
                tsBlockingFilter.Text = "Blocking : Root Blockers";
            }
            else if (recursive)
            {
                dv.RowFilter =
                    $"(blocking_session_id = {sessionID} OR BlockingHierarchy LIKE '{sessionID} \\%' OR BlockingHierarchy LIKE '%\\ {sessionID}' OR BlockingHierarchy LIKE '%\\ {sessionID} \\%')";
                tsBlockingFilter.Text = $"Blocking : Blocked By {sessionID} (Recursive)";
            }
            else
            {
                dv.RowFilter = $"blocking_session_id = {sessionID}";
                tsBlockingFilter.Text = $"Blocking : Blocked By {sessionID}";
            }

            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Bold);
        }

        /// <summary>Remove blocking filter applied to grid with showBlocking()</summary>
        private void ClearBlocking()
        {
            tsGroupByFilter.Visible = false;
            if (dgv.DataSource != null)
            {
                var dv = (DataView)dgv.DataSource;
                dv.RowFilter = "";
            }
            tsBlockingFilter.Text = $"Blocking ({blockedCount} Blocked)";
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Regular);
        }

        private void ClearBlockingFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearBlocking();
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            PromptColumnSelection(ref dgv);
        }

        private void PromptColumnSelection(ref DataGridView gv)
        {
            using var frm = new SelectColumns();
            frm.Columns = gv.Columns;
            frm.ShowDialog(this);
        }

        private void TsEditLimit_Click(object sender, EventArgs e)
        {
            string limit = Properties.Settings.Default.RunningQueriesSummaryMaxRows.ToString();
            if (Common.ShowInputDialog(ref limit, "Enter row limit") != DialogResult.OK) return;
            if (int.TryParse(limit, out int maxRows) && maxRows > 0)
            {
                Properties.Settings.Default.RunningQueriesSummaryMaxRows = maxRows;
                Properties.Settings.Default.Save();
                UpdateRowLimit();
                LoadSummaryData();
            }
            else
            {
                MessageBox.Show("Invalid value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunningQueries_Load(object sender, EventArgs e)
        {
            tsEditLimit.LinkColor = DashColors.LinkColor;
        }

        private void UpdateRowLimit()
        {
            // Ensure max rows is set to a value greater than 0
            if (Properties.Settings.Default.RunningQueriesSummaryMaxRows <= 0)
            {
                Properties.Settings.Default.RunningQueriesSummaryMaxRows = 100;
                Properties.Settings.Default.Save();
            }
            tsEditLimit.Text = $"Row Limit {Properties.Settings.Default.RunningQueriesSummaryMaxRows}";
        }
    }
}