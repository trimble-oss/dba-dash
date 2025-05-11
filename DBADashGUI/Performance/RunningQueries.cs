using DBADash;
using DBADash.Messaging;
using DBADashGUI.AgentJobs;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeUnit = Humanizer.Localisation.TimeUnit;

namespace DBADashGUI.Performance
{
    public partial class RunningQueries : UserControl, INavigation, ISetContext, IRefreshData, ISetStatus
    {
        public RunningQueries()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
            dgv.GridFilterChanged += (_, _) =>
            {
                if (!dgv.HasFilter)
                {
                    ResetBlockingFilterText();
                    tsGroupByFilter.Visible = false;
                }
            };
            var openInNewWindow = new ToolStripMenuItem("Open In New Window", Properties.Resources.NewWindow_16x);
            var contextRowInNewWindow = new ToolStripMenuItem("Context Row", Properties.Resources.Select,
                OpenInNewWindow);
            var selectedInNewWindow = new ToolStripMenuItem("Selected", Properties.Resources.SelectRows,
                OpenSelectedInNewWindow);
            openInNewWindow.DropDownItems.AddRange(new ToolStripItem[] { contextRowInNewWindow, selectedInNewWindow });
            dgv.CellContextMenu.Items.Insert(0, openInNewWindow);
            dgv.CellContextMenu.Items.Insert(1, new ToolStripSeparator());
        }

        private const int MaxSnapshotsToLoadBeforePrompt = 20;
        private const int MaxSnapshotTabs = 100; // Tabs wil be loaded on demand after first few tabs

        public void AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode mode = DataGridViewAutoSizeColumnsMode.AllCells) => dgv.AutoResizeColumnsWithMaxColumnWidth(mode);

        private void OpenSelectedInNewWindow(object sender, EventArgs e)
        {
            var snapshots = dgv.SelectedCellRows
                .Select(row => (DataRowView)row.DataBoundItem)
                .Select(row =>
                    new RunningQueriesSnapshotInfo(((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc(),
                            (int)row["InstanceID"],
                            InstanceID > 0 ?
                            ((DateTime)row["SnapshotDate"]).ToString(CultureInfo.CurrentCulture) : (string)row["InstanceDisplayName"])
                        )
                        .Distinct().ToList();

            if (snapshots.Count > MaxSnapshotTabs)
            {
                MessageBox.Show($"Too many snapshots selected ({snapshots.Count}).", "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            if (snapshots.Count > MaxSnapshotsToLoadBeforePrompt)
            {
                if (MessageBox.Show(
                        $"Are you sure you want to open {snapshots.Count} snapshots?",
                        "Open Snapshots", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }

            if (snapshots.Count() == 1)
            {
                OpenInNewWindow(snapshots[0].SnapshotDateUtc, snapshots[0].InstanceID);
            }
            else
            {
                var viewer = new RunningQueriesViewer();
                viewer.LoadSnapshots(snapshots);
                viewer.Show();
            }
        }

        private void OpenInNewWindow(object sender, EventArgs e)
        {
            var row = (DataRowView)dgv.Rows[dgv.ClickedRowIndex].DataBoundItem;
            var snapshotDate = ((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc();
            var instanceId = (int)row["InstanceID"];
            OpenInNewWindow(snapshotDate, instanceId);
        }

        private DBADashContext CurrentContext;
        private string PersistedSort;
        private string PersistedFilter;
        private bool IsForceDetail;

        public int InstanceID;
        private HashSet<int> InstanceIDs => InstanceID > 0 ? new HashSet<int>(InstanceID) : CurrentContext.InstanceIDs;
        private DateTime currentSnapshotDate;
        private DataTable snapshotDT;
        public DateTime SnapshotDateFrom;
        public DateTime SnapshotDateTo;
        public int SessionID = 0;
        public Guid JobId = Guid.Empty;
        private int blockedCount;
        private int idleCount;
        private int runningJobCount;
        private bool hasWaitResource;
        private long blockedWait;
        private bool hasContextInfo;
        private int skip = 0;
        private bool IsGroupBy => dgv.Columns.Contains("colGroup");
        private bool IsInstanceDrillDown => CurrentContext != null && InstanceID != CurrentContext.InstanceID;
        private RunningQueriesFilters forceDetailFilters;
        private bool hasImplicitTran;

        private static string IdleThresholdInfo =>
            $"Red = Sleeping session with an open transaction that has been idle for longer than {TimeSpan.FromSeconds(Config.IdleCriticalThresholdForSleepingSessionWithOpenTran).Humanize(maxUnit: TimeUnit.Year, precision: 3)}.\nYellow=Sleeping session with an open transaction that has been idle for longer than {TimeSpan.FromSeconds(Config.IdleWarningThresholdForSleepingSessionWithOpenTran).Humanize(maxUnit: TimeUnit.Year, precision: 3)}.";

        private DataGridViewColumn[] RunningQueryColumns =>
            new DataGridViewColumn[]
            {
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", Name = "colInstance",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, Frozen = Common.FreezeKeyColumn
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Session ID", DataPropertyName = "session_id", Name = "colSessionID",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, Frozen = Common.FreezeKeyColumn
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Batch Text", DataPropertyName = "batch_text", Name = "colBatchText",
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Text", DataPropertyName = "text", Name = "colText",
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Plan", DataPropertyName = "has_plan", Name = "colQueryPlan",
                    SortMode = DataGridViewColumnSortMode.Automatic, Visible = true,
                    ToolTipText = "Click link to view query plan"
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocking Session ID", DataPropertyName = "blocking_session_id",
                    Name = "colBlockingSessionID", SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60,
                    ToolTipText = "ID of the session directly blocking the current query.  0 = Not blocked.",
                    DefaultCellStyle = new DataGridViewCellStyle() { ForeColor = Color.White }
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocking Hierarchy", DataPropertyName = "BlockingHierarchy",
                    SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 70,
                    ToolTipText =
                        "Identifies all the session IDs that are involved in the blocking chain for each query starting with the root blocker."
                },
                new DataGridViewCheckBoxColumn
                {
                    HeaderText = "Root Blocker", DataPropertyName = "IsRootBlocker", Name = "colIsRootBlocker",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, Visible = blockedCount > 0,
                    ToolTipText =
                        "Root blocker is the query at the head of the blocking chain. This query is not blocked but it's holding locks that other queries are waiting for."
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Blocked Count", DataPropertyName = "BlockCount", Name = "colBlockCount",
                    SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60,
                    ToolTipText = "Count of sessions blocked directly by the current query.", LinkColor = Color.White
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Blocked Count Recursive", DataPropertyName = "BlockCountRecursive",
                    Name = "colBlockedCountRecursive", SortMode = DataGridViewColumnSortMode.Automatic,
                    Visible = blockedCount > 0, MinimumWidth = 60,
                    ToolTipText = "Count of sessions blocked by the current query - directly or indirectly",
                    LinkColor = Color.White
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocked Wait Time", DataPropertyName = "BlockWaitTime", Name = "colBlockedWaitTime",
                    SortMode = DataGridViewColumnSortMode.Automatic, Visible = blockedCount > 0, MinimumWidth = 60,
                    ToolTipText = "Wait time associated with sessions blocked directly by the current session."
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocked Wait Time Recursive", DataPropertyName = "BlockWaitTimeRecursive",
                    Name = "colBlockedWaitTimeRecursive", SortMode = DataGridViewColumnSortMode.Automatic,
                    Visible = blockedCount > 0, MinimumWidth = 60,
                    ToolTipText =
                        "Wait time associated with sessions blocked directly or indirectly by the current session"
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Duration", DataPropertyName = "Duration", Name = "colDuration",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Duration (ms)", DataPropertyName = "Duration (ms)",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Transaction Duration", DataPropertyName = "transaction_duration", Name = "colTranDuration",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60
                },
                new DataGridViewCheckBoxColumn()
                {
                    HeaderText = "Implicit Tran", DataPropertyName = "is_implicit_transaction", Name = "colImplicitTran",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60,Visible = hasImplicitTran,
                    ToolTipText = "Implicit transactions are something that should be avoided.  When on, transactions will be started without an explicit BEGIN TRAN statement."
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Transaction Duration (ms)", DataPropertyName = "transaction_duration_ms", Name = "colTranDurationMs",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60, Visible = false
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "CPU Time", DataPropertyName = "cpu_time",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Logical Reads", DataPropertyName = "logical_reads",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Reads", DataPropertyName = "reads", SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Writes", DataPropertyName = "writes", SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Granted Query Memory (Kb)", DataPropertyName = "granted_query_memory_kb",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Command", DataPropertyName = "Command", Name = "colCommand",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Status", DataPropertyName = "Status", Name = "colStatus",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40,
                    ToolTipText =
                        "Running - Query is currently running and consuming CPU cycles.\nRunnable - Ready to run, waiting for time on the CPU\nSuspended - Waiting on a resource.  e.g IO, lock\nPending - Waiting for a worker thread to become available.\nRollback - Transaction rollback is in progress.\nSleeping - No work to perform, waiting for next query from client application.\n\nRed = Sleeping session that is causing blocking."
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "colIdleTime", HeaderText = "Idle Time", DataPropertyName = "sleeping_session_idle_time",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40,
                    ToolTipText =
                        $"Sleeping session idle time.  \nA sleeping session is waiting for work from the client application.  \nThis can be a problem if the session is sleeping for a long time and has an open transaction. \ne.g. Blocking or log growth due to the open transaction preventing log truncation.\nApplication code changes are required to fix sleeping sessions with open transactions.\n\n{IdleThresholdInfo}",
                    Visible = idleCount > 0
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "colIdleTimeSec", HeaderText = "Idle Time (sec)",
                    DataPropertyName = "sleeping_session_idle_time_sec",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40,
                    ToolTipText = "Sleeping session idle time (seconds)", Visible = false
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time", Name = "colWaitTime",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Type", DataPropertyName = "wait_type",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Top Session Waits", DataPropertyName = "TopSessionWaits", Name = "colTopSessionWaits",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50,
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Object ID", DataPropertyName = "object_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Object Name", DataPropertyName = "object_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewLinkColumn
                {
                    Name = "colSnapshotDateLink", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "% Complete", DataPropertyName = "percent_complete",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "colOpenTransactionCount", HeaderText = "Open Transaction Count",
                    DataPropertyName = "open_transaction_count", SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Transaction Isolation Level", DataPropertyName = "transaction_isolation_level",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Login Name", DataPropertyName = "login_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Host Name", DataPropertyName = "host_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Database ID", DataPropertyName = "database_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 60
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Database Name", DataPropertyName = "database_names",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Program Name", DataPropertyName = "program_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Job ID", DataPropertyName = "job_id", SortMode = DataGridViewColumnSortMode.Automatic,
                    Visible = runningJobCount > 0, MinimumWidth = 40, Name = "colJobID"
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Job Name", DataPropertyName = "job_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, Visible = runningJobCount > 0, MinimumWidth = 40, Name = "colJobName"
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Client Interface Name", DataPropertyName = "client_interface_name",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Start Time", DataPropertyName = "start_time",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = false
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Last Request Start Time", DataPropertyName = "last_request_start_time",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = false
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Last Request End Time", DataPropertyName = "last_request_end_time",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = false
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Last Request Duration", DataPropertyName = "last_request_duration",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = false
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Wait Resource", DataPropertyName = "wait_resource",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Name = "colWaitResource"
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Resource Type", DataPropertyName = "wait_resource_type",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Database ID", DataPropertyName = "wait_database_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Database", DataPropertyName = "wait_db",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait File ID", DataPropertyName = "wait_file_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait File", DataPropertyName = "wait_file",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Page ID", DataPropertyName = "wait_page_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Object ID", DataPropertyName = "wait_object_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Object", DataPropertyName = "wait_object",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Index ID", DataPropertyName = "wait_index_id",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait HOBT", DataPropertyName = "wait_hobt",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Hash", DataPropertyName = "wait_hash",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Wait Slot", DataPropertyName = "wait_slot",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewCheckBoxColumn()
                {
                    HeaderText = "Wait Is Compile", DataPropertyName = "wait_is_compile",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Page Type", DataPropertyName = "page_type",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40, Visible = hasWaitResource
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Login Time", DataPropertyName = "login_time",
                    SortMode = DataGridViewColumnSortMode.Automatic, MinimumWidth = 40,
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "SQL Handle", DataPropertyName = "sql_handle",
                    SortMode = DataGridViewColumnSortMode.Automatic, Name = "colSQLHandle"
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Plan Handle", DataPropertyName = "plan_handle",
                    SortMode = DataGridViewColumnSortMode.Automatic, Name = "colPlanHandle"
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Query Hash", DataPropertyName = "query_hash",
                    SortMode = DataGridViewColumnSortMode.Automatic, Name = "colQueryHash"
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Query Plan Hash", DataPropertyName = "query_plan_hash",
                    SortMode = DataGridViewColumnSortMode.Automatic, Name = "colQueryPlanHash"
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Context Info", DataPropertyName = "context_info",
                    SortMode = DataGridViewColumnSortMode.Automatic, Name = "colContextInfo", Visible = hasContextInfo
                },
                new DataGridViewTextBoxColumn
                {
                    HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false
                },
            };

        private readonly DataGridViewColumn[] sessionWaitColumns = new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Session ID", DataPropertyName = "session_id", Name = "colSessionID", Visible = false,
                Frozen = Common.FreezeKeyColumn
            },
            new DataGridViewTextBoxColumn()
                { HeaderText = "Wait Type", DataPropertyName = "WaitType", Name = "colWaitType" },
            new DataGridViewTextBoxColumn()
                { HeaderText = "Waiting Tasks Count", DataPropertyName = "waiting_tasks_count" },
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Wait Time %", DataPropertyName = "wait_pct",
                DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" }
            },
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Max Wait Time (ms)", DataPropertyName = "max_wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Signal Wait Time (ms)", DataPropertyName = "signal_wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn()
            {
                HeaderText = "Signal Wait %", DataPropertyName = "signal_wait_pct",
                DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" }
            },
            new DataGridViewLinkColumn()
                { HeaderText = "Help", Text = "help", UseColumnTextForLinkValue = true, Name = "colHelp" }
        };

        public void SetContext(DBADashContext _context)
        {
            if (CurrentContext == _context) return; // Context hasn't changed, don't refresh
            ShowLatestOnNextExecution = false;
            CurrentContext = _context;
            currentSnapshotDate = DateTime.MinValue;
            dgv.DataSource = null;
            InstanceID = _context.InstanceID;
            IsForceDetail = false;
            if (InstanceIDs is { Count: 1 })
            {
                InstanceID = InstanceIDs.First();
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
            else if (dgv.RowCount > 0 && dgv.Columns.Contains("colSnapshotDate") &&
                     dgv.FirstDisplayedScrollingRowIndex >
                     0) // Note: Only maintain scroll position if we have scrolled (If index is 0, we want new rows to display rather than scrolling down)
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
        private void SetVisibility()
        {
            UpdateRowLimit();
            tsEditLimit.Visible = false;
            lblRowLimit.Visible = false;
            tsWaitsFilter.Enabled = SessionID == 0 && JobId == Guid.Empty;
            tsGroupByFilter.Visible = false;
            splitContainer1.Panel2Collapsed = true;
            dgvSessionWaits.DataSource = null;
            tsBack.Enabled = IsGroupBy || IsInstanceDrillDown || SnapshotDateFrom == SnapshotDateTo || IsForceDetail;
            tsNext.Visible = SnapshotDateFrom == SnapshotDateTo && !IsForceDetail;
            tsPrevious.Visible = SnapshotDateFrom == SnapshotDateTo && !IsForceDetail;
            tsGroupBy.Enabled = false;
            tsGetLatest.Visible = InstanceID > 0 && JobId == Guid.Empty;
            tsTriggerCollection.Visible = DBADashUser.AllowMessaging && !IsServerLevelSummary && JobId == Guid.Empty &&
                                          SessionID == 0 && CollectionMessaging.IsMessagingEnabled(InstanceID);
            lblSnapshotDate.Visible = SnapshotDateFrom == SnapshotDateTo && !IsForceDetail;
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }

            if (ShowLatestOnNextExecution)
            {
                SnapshotDateFrom = DateTime.MaxValue;
                SnapshotDateTo = DateTime.MaxValue;
                ShowLatestOnNextExecution = false;
            }

            SetHighlightBeforeRefresh(out var highlightSnapshot, out var highlight);
            dgv.DataSource = null;
            snapshotDT = null;
            SetVisibility();
            ClearBlocking();

            var detail = InstanceID > 0 && (SessionID != 0 || JobId != Guid.Empty || IsForceDetail ||
                                            (SnapshotDateFrom == SnapshotDateTo &&
                                             SnapshotDateFrom != DateTime.MinValue));

            try
            {
                if (detail) // Show a specific blocking snapshot (e.g. Blocking chart drill down)
                {
                    var filters = IsForceDetail
                        ? forceDetailFilters
                        : new RunningQueriesFilters()
                        {
                            InstanceID = InstanceID,
                            From = SnapshotDateFrom,
                            To = SnapshotDateTo,
                            Skip = skip,
                            SessionID = SessionID,
                            JobID = JobId
                        };
                    snapshotDT = RunningQueriesSnapshot(ref filters);
                    SnapshotDateFrom = filters.From;
                    skip = 0;
                    GetCounts();
                    lblSnapshotDate.Text = "Snapshot Date: " +
                                           SnapshotDateFrom.ToAppTimeZone().ToString(CultureInfo.CurrentCulture);
                    try
                    {
                        LoadSnapshot(new DataView(snapshotDT, PersistedFilter, PersistedSort,
                            DataViewRowState.CurrentRows));
                    }
                    catch // Previous filter might not be valid
                    {
                        LoadSnapshot(new DataView(snapshotDT));
                    }

                    PersistedFilter = string.Empty;
                    PersistedSort = string.Empty;
                    tsViewALL.Visible = false;
                    currentSnapshotDate = SnapshotDateFrom;
                    lblRowLimit.Visible = snapshotDT.Rows.Count == filters.Top;
                }
                else // List of snapshots for an instance or last snapshot for all instances
                {
                    tsViewALL.Visible = InstanceID > 0;
                    LoadSummaryData();
                    if (highlightSnapshot.HasValue)
                    {
                        HighlightSnapshot(highlightSnapshot.Value, highlight);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            dgv.ApplyTheme(DBADashUser.SelectedTheme);
            tsEditLimit.LinkColor = DBADashUser.SelectedTheme.LinkColor;
        }

        private void LoadSnapshot(DateTime snapshotDate, int _skip = 0)
        {
            SnapshotDateFrom = snapshotDate;
            SnapshotDateTo = snapshotDate;
            skip = _skip;
            RefreshData();
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
                tsBack.Enabled = CurrentContext?.InstanceIDs is { Count: > 1 };
                lblRowLimit.Visible = dt.Rows.Count == Properties.Settings.Default.RunningQueriesSummaryMaxRows;
                tsEditLimit.Visible = true;
            }

            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "InstanceID",
                    DataPropertyName = "InstanceID",
                    Name = "colInstanceID",
                    Visible = false,
                    Frozen = Common.FreezeKeyColumn
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Instance",
                    DataPropertyName = "InstanceDisplayName",
                    Name = "colInstance",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    Visible = IsServerLevelSummary,
                    Frozen = Common.FreezeKeyColumn
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Instance ",
                    DataPropertyName = "InstanceDisplayName",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    Visible = !IsServerLevelSummary,
                    Frozen = Common.FreezeKeyColumn
                },
                new DataGridViewLinkColumn()
                {
                    HeaderText = "Snapshot Date",
                    DataPropertyName = "SnapshotDate",
                    Name = "colSnapshotDate",
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Running Queries",
                    DataPropertyName = "RunningQueries",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocked Queries",
                    DataPropertyName = "BlockedQueries",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Blocked Queries Wait",
                    DataPropertyName = "BlockedQueriesWait",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Max Memory Grant KB",
                    DataPropertyName = "MaxMemoryGrantKB",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Sum Memory Grant KB",
                    DataPropertyName = "SumMemoryGrantKB",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Longest Running Query",
                    DataPropertyName = "LongestRunningQuery",
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Critical Wait Count",
                    DataPropertyName = "CriticalWaitCount",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Critical Wait Time",
                    DataPropertyName = "CriticalWaitTime",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "TempDB Wait Count",
                    DataPropertyName = "TempDBWaitCount",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "TempDB Wait Time",
                    DataPropertyName = "TempDBWaitTime",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Sleeping Sessions Count",
                    DataPropertyName = "SleepingSessionsCount",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle,
                    ToolTipText =
                        "Count of sleeping sessions with open transactions. Sleeping sessions are waiting for input from the client application."
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "colMaxIdleTime",
                    HeaderText = "Max Idle Time",
                    DataPropertyName = "MaxIdleTime",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle,
                    ToolTipText = $"Max idle time for sleeping sessions with open transactions.\n\n{IdleThresholdInfo}"
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "colOldestTran",
                    HeaderText = "Oldest Transaction",
                    DataPropertyName = "OldestTransaction",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                }
            );
            dgv.DataSource = new DataView(dt);
            dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
            dgv.AutoResizeColumnsWithMaxColumnWidth();
        }

        private static void ApplyTableModifications(DataTable dt)
        {
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
            dt.Columns["start_time_utc"].ColumnName = "start_time";
            dt.Columns["last_request_start_time_utc"].ColumnName = "last_request_start_time";
            dt.Columns["last_request_end_time_utc"].ColumnName = "last_request_end_time";
            dt.Columns["login_time_utc"].ColumnName = "login_time";
            Common.ReplaceBinaryContextInfoColumn(ref dt);
        }

        /// <summary>Get last running query snapshot summary data for all servers</summary>
        private DataTable RunningQueriesServerSummary()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningQueriesServerSummary_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
            return dt;
        }

        /// <summary>Get a list of running query snapshots for the specified instance</summary>
        private DataTable RunningQueriesSummary()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningQueriesSummary_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
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

        /// <summary>Get running queries snapshot data for the specified snapshot date. skip parameter is used to return next snapshot (1) or previous snapshot (-1)</summary>
        private static DataTable RunningQueriesSnapshot(ref RunningQueriesFilters filters)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningQueries_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            var parameters = filters.GetNonDefaultParameters();
            cmd.Parameters.AddRange(parameters);
            var pSnapshotDateFrom = parameters.First(p => p.ParameterName == "SnapshotDateFrom");
            da.Fill(dt);

            // Empty column to cache query plans when they're requested
            dt.Columns.Add("query_plan_text");

            filters.From = Convert.ToDateTime(pSnapshotDateFrom.Value);
            ApplyTableModifications(dt);
            return dt;
        }

        /// <summary>Load a running queries snapshot</summary>
        private void LoadSnapshot(DataView source)
        {
            dgv.DataSource = null;
            dgv.Columns.Clear();
            if (source.Table == null)
            {
                return;
            }

            dgv.AutoGenerateColumns = false;
            hasContextInfo = source.Table.Columns.Contains("context_info") && source.Cast<DataRowView>()
                .Any(row => (row["context_info"] as string ?? "0x") != "0x");
            hasImplicitTran = source.Table.Columns.Contains("is_implicit_transaction") && source.Cast<DataRowView>()
                .Any(row => (bool?)row["is_implicit_transaction"].DBNullToNull() == true);

            dgv.Columns.AddRange(RunningQueryColumns);
            dgv.DataSource = source;
            dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
            dgv.AutoResizeColumnsWithMaxColumnWidth();
            tsGroupBy.Enabled = dgv.Rows.Count > 1;
            tsBlockingFilter.Visible = SessionID == 0 && JobId == Guid.Empty;
        }

        /// <summary>Get counts from the running queries snapshot table. e.g. Blocking counts</summary>
        private void GetCounts()
        {
            runningJobCount = snapshotDT.AsEnumerable().Count(r => r["job_id"] != DBNull.Value);
            blockedCount = snapshotDT.AsEnumerable().Count(r => Convert.ToInt16(r["blocking_session_id"]) != 0);
            idleCount = snapshotDT.AsEnumerable()
                .Count(r => Convert.ToInt64(r["sleeping_session_idle_time_sec"].DBNullToNull()) > 0);
            blockedWait = snapshotDT.AsEnumerable()
                .Where(r => Convert.ToInt16(r["blocking_session_id"]) != 0 && r["wait_time"] != DBNull.Value)
                .Sum(r => Convert.ToInt64(r["wait_time"]));
            hasWaitResource = snapshotDT.AsEnumerable().Any(r =>
                r["wait_resource"] != DBNull.Value && !string.IsNullOrEmpty((string)r["wait_resource"]));

            tsBlockingFilter.Text = $"Blocking ({blockedCount} Blocked)";
            tsBlockingFilter.Enabled = blockedCount > 0;
            var status = SessionID == 0 && JobId == Guid.Empty ?
                $"Blocked Sessions: {blockedCount}, Blocked Wait Time: {TimeSpan.FromMilliseconds(blockedWait):dd\\ hh\\:mm\\:ss}, Running Jobs {runningJobCount}" : string.Empty;
            var statusColor = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ?
                DBADashUser.SelectedTheme.ForegroundColor
                : blockedCount > 0
                    ? DashColors.Fail
                    : DashColors.Success;
            SetStatus(status, string.Empty, statusColor);
            tsStatus.Font = blockedCount > 0
                ? new Font(tsStatus.Font, FontStyle.Bold)
                : new Font(tsStatus.Font, FontStyle.Regular);
        }

        private void ShowPlan(DataRowView row)
        {
            if (!(bool)row["has_plan"])
            {
                var context = CommonData.GetDBADashContext((int)row["InstanceID"]);
                if (context.CanMessage)
                {
                    CollectPlan(row, context);
                    return;
                }
                FindPlanScript(row);
            }
            else
            {
                if (row["query_plan_text"] == DBNull.Value)
                {
                    row["query_plan_text"] = GetPlan(row);
                }
                var plan = (string)row["query_plan_text"];
                Common.ShowQueryPlan(plan);
            }
        }

        private static string GetPlan(DataRowView row)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.QueryPlan_Get", cn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.AddWithValue("plan_handle", row.GetHexStringColumnAsByteArray("plan_handle"));
            cmd.Parameters.AddWithValue("query_plan_hash", row.GetHexStringColumnAsByteArray("query_plan_hash"));
            cmd.Parameters.AddWithValue("statement_start_offset", row["statement_start_offset"]);
            cmd.Parameters.AddWithValue("statement_end_offset", row["statement_end_offset"]);

            cn.Open();
            var result = cmd.ExecuteScalar();

            return Encoding.Unicode.GetString(((byte[])result).Decompress().ToArray());
        }

        private static QueryPlanCollectionMessage GetPlanCollectionMessage(DataRowView row, DBADashContext context)
        {
            if (row["plan_handle"] == DBNull.Value)
            {
                throw new Exception("No plan handle");
            }
            if (row["query_plan_hash"] == DBNull.Value)
            {
                throw new Exception("No plan hash");
            }
            var plan = new Plan(row.GetHexStringColumnAsByteArray("plan_handle"),
                row.GetHexStringColumnAsByteArray("query_plan_hash"),
                (int)row["statement_start_offset"],
                (int)row["statement_end_offset"]);
            return new QueryPlanCollectionMessage()
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                PlansToCollect = new List<Plan>() { plan }
            };
        }

        private async Task ProcessPlanCollectionMessageReply(ResponseMessage reply, Guid messageGroup)
        {
            try
            {
                var request = PlanCollectionRequests[messageGroup];
                PlanCollectionRequests.Remove(messageGroup);

                if (reply.Type != ResponseMessage.ResponseTypes.Success)
                {
                    SetStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                    return;
                }
                SetStatus("Loading Plan...", string.Empty, DashColors.Information);

                var context = CommonData.GetDBADashContext(request.ConnectionID);

                var dtPlan = reply.Data.Tables["QueryPlans"];
                if (dtPlan == null || dtPlan.Rows.Count == 0)
                {
                    SetStatus("Query plan was not found", string.Empty, DashColors.Fail);
                    MessageBox.Show("Query plan was not found", "Warning", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var status = "Plan collected.  Loading in default app.";
                var tooltip = string.Empty;
                var statusColor = DashColors.Green;
                try
                {
                    await DBImporter.UpdateCollectionAsync(dtPlan, context.InstanceID, DateTime.UtcNow, Common.ConnectionString);
                }
                catch (Exception ex)
                {
                    status = "Plan collected, but error saving to repository database.";
                    tooltip = ex.Message;
                    statusColor = DashColors.Warning;
                }

                var planBin = dtPlan.Rows[0].Field<byte[]>("query_plan_compressed");
                var planHandle = dtPlan.Rows[0].Field<byte[]>("plan_handle").ToHexString(true);
                var planHash = dtPlan.Rows[0].Field<byte[]>("query_plan_hash").ToHexString(true);
                var startOffset = dtPlan.Rows[0].Field<int>("statement_start_offset");
                var endOffset = dtPlan.Rows[0].Field<int>("statement_end_offset");
                var planText = SMOBaseClass.Unzip(planBin);
                var dtGrid = ((DataView)dgv.DataSource).Table;
                if (dtGrid == null)
                {
                    throw new Exception("DataTable is null");
                }
                foreach (var row in dtGrid.Rows.Cast<DataRow>().Where(r =>
                             r.Field<string>("plan_handle") == planHandle
                             && r.Field<string>("query_plan_hash") == planHash
                             && r.Field<int>("statement_start_offset") == startOffset
                             && r.Field<int>("statement_end_offset") == endOffset))
                {
                    row["has_plan"] = true;
                    row["query_plan_text"] = planText;
                }

                Common.ShowQueryPlan(planText);
                SetStatus(status, tooltip, statusColor);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Red);
            }
        }

        private readonly Dictionary<Guid, QueryPlanCollectionMessage> PlanCollectionRequests = new();

        private void CollectPlan(DataRowView row, DBADashContext context)
        {
            if (context.CanMessage)
            {
                try
                {
                    var planCollectionMessage = GetPlanCollectionMessage(row, context);
                    var id = Guid.NewGuid();
                    PlanCollectionRequests.Add(id, planCollectionMessage);
                    _ = MessagingHelper.SendMessageAndProcessReply((MessageBase)planCollectionMessage, context,
                        tsStatus, ProcessPlanCollectionMessageReply, id);
                    SetStatus("Plan collection message SENT", string.Empty, DashColors.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Plan Collection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return;
            }
        }

        private static void FindPlanScript(DataRowView row)
        {
            var db = Convert.ToString(row["database_name"]);
            var planHandle = Convert.ToString(row["plan_handle"]);
            var planHash = Convert.ToString(row["query_plan_hash"]);
            var queryHash = Convert.ToString(row["query_hash"]);
            var sqlHandle = Convert.ToString(row["sql_handle"]);
            var statementStartOffset =
                Convert.ToInt32(row["statement_start_offset"] == DBNull.Value ? -1 : row["statement_start_offset"]);
            var statementEndOffset =
                Convert.ToInt32(row["statement_end_offset"] == DBNull.Value ? -1 : row["statement_end_offset"]);
            var instance = Convert.ToString(row["InstanceDisplayName"]);
            var sql = SqlStrings.GetFindPlan(planHash, queryHash, planHandle, sqlHandle, db, statementStartOffset,
                statementEndOffset, instance);

            Common.ShowCodeViewer(sql, "Find Plan");
        }

        private void OpenInNewWindow(DateTime snapshotDate, int instanceId)
        {
            var viewer = new RunningQueriesViewer()
            {
                InstanceID = instanceId,
                SnapshotDateFrom = snapshotDate,
                SnapshotDateTo = snapshotDate
            };
            viewer.Show();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
            var colName = dgv.Columns[e.ColumnIndex].Name;
            switch (colName)
            {
                // Drill down to view a snapshot from summary
                case "colSnapshotDate":
                    {
                        var snapshotDate = ((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc();
                        InstanceID = (int)row["InstanceID"];
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            OpenInNewWindow(snapshotDate, InstanceID);
                            return;
                        }
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
                        var sql = (string)row["batch_text"];
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

                case "colQueryPlanHash":
                    LoadQueryStore(row, false, true);
                    break;

                case "colQueryHash":
                    LoadQueryStore(row, true, false);
                    break;

                case "colPlanHandle":
                case "colSQLHandle":
                    FindPlanScript(row);
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
                    tsBack.Enabled = true;
                    ShowBlocking(Convert.ToInt16(dgv.Rows[e.RowIndex].Cells["colSessionID"].Value));
                    break;
                // Filter to show queries blocked directly by the selected session
                case "colBlockedCountRecursive":
                    tsBack.Enabled = true;
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

                case "colJobName":
                case "colJobID":
                    ShowJob(row);
                    break;
            }
        }

        private static void ShowJob(DataRowView row)
        {
            try
            {
                if (row["job_id"] == DBNull.Value)
                {
                    MessageBox.Show("Job ID is null", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var instanceId = (int)row["InstanceID"];
                var jobId = (Guid)row["job_id"];
                var jobName = (string)(row["job_name"].DBNullToNull());
                var jobContext = CommonData.GetDBADashContext(instanceId);
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = jobId;
                jobContext.ObjectName = jobName;
                var frm = new JobInfoForm() { DBADashContext = jobContext };
                frm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LoadQueryStore(DataRowView row, bool isQueryHash, bool isPlanHash)
        {
            var db = Convert.ToString(row["database_name"].DBNullToNull());
            var planHash = row.GetHexStringColumnAsByteArray("query_plan_hash");
            var queryHash = row.GetHexStringColumnAsByteArray("query_hash");
            var instanceID = Convert.ToInt32(row["InstanceID"]);
            var isQueryStoreOn = row["is_query_store_on"] != DBNull.Value && Convert.ToBoolean(row["is_query_store_on"]);
            if (!isQueryStoreOn)
            {
                MessageBox.Show("Query store is not enabled for this database", "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var context = CommonData.GetDBADashContext(instanceID);
            if (!context.CanMessage)
            {
                MessageBox.Show("You don't have access to the messaging feature required to access query store",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            context.DatabaseName = db;

            var planViewer = new QueryStoreViewer()
            {
                PlanHash = isPlanHash ? planHash : null,
                QueryHash = isQueryHash ? queryHash : null,
                Context = context
            };
            planViewer.Show();
        }

        private static void DecipherWaitResource(DataRowView row)
        {
            var waitResource = Convert.ToString(row["wait_resource"]);
            var instance = Convert.ToString(row["InstanceDisplayName"]);
            var sql = SqlStrings.GetDecipherWaitResource(waitResource, instance);
            Common.ShowCodeViewer(sql, "Decipher Wait Resource");
        }

        private void GroupByFilter(DataGridViewCellEventArgs e, DataRowView row)
        {
            var filter = dgv.Columns[e.ColumnIndex].DataPropertyName + "='" +
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

            var sessionId = (short)row["session_id"];
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, sessionId,
                Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc(),
                Convert.ToDateTime(row["login_time"]).AppTimeZoneToUtc());
            dgvSessionWaits.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            sessionToolStripMenuItem.Tag = sessionId;
            lblWaitsForSession.Text = "Waits For Session ID: " + sessionId;
            dgvSessionWaits.ApplyTheme(DBADashUser.SelectedTheme);
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
            dgv.ExportToExcel();
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
                SetVisibility();
            }
            else if (IsForceDetail)
            {
                IsForceDetail = false;
                RefreshData();
            }
            else if (!dgv.Columns.Contains("colSessionID"))
            {
                InstanceID = -1;
                RefreshData();
            }
            else
            {
                SnapshotDateFrom = DateTime.MinValue;
                SnapshotDateTo = DateTime.MinValue;
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
                if (row.Cells["colSnapshotDate"].Value != null &&
                    (DateTime)row.Cells["colSnapshotDate"].Value == snapshotDate)
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
            IsForceDetail = false;
            PersistedFilter = dgv.RowFilter;
            PersistedSort = dgv.SortString;
            LoadSnapshot(DateTime.MaxValue);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            IsForceDetail = false;
            RefreshData();
            if (dgv.RowCount == 0) return;
            dgv.FirstDisplayedScrollingRowIndex =
                0; // Reset the scroll position if we click refresh as it's likely we are interested in new snapshots.
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == "colQueryPlan")
            {
                e.Value = Convert.ToBoolean(e.Value) ? "View Plan" : dgv.Rows[e.RowIndex].Cells["colPlanHandle"].Value == DBNull.Value || dgv.Rows[e.RowIndex].Cells["colQueryPlanHash"].Value == DBNull.Value ? "" : "Find Plan";
            }
            else if (Convert.ToString(e.Value)?.Length > 1000)
            {
                e.Value = Convert.ToString(e.Value).Truncate(997) + "...";
            }
            else if (new[] { "colBlockCount", "colBlockedCountRecursive", "colBlockingSessionID" }.Contains(
                         dgv.Columns[e.ColumnIndex].Name))
            {
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(Convert.ToInt32(e.Value) == 0
                    ? DBADashStatus.DBADashStatusEnum.OK
                    : DBADashStatus.DBADashStatusEnum.Critical);
            }
            else if (new[] { "colIdleTimeSec", "colIdleTime" }.Contains(dgv.Columns[e.ColumnIndex].Name))
            {
                var idleSec = Convert.ToDouble(dgv.Rows[e.RowIndex].Cells["colIdleTimeSec"].Value.DBNullToNull());
                var openTranCnt =
                    Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["colOpenTransactionCount"].Value.DBNullToNull());

                var status = openTranCnt switch
                {
                    > 0 when idleSec > Config.IdleCriticalThresholdForSleepingSessionWithOpenTran => DBADashStatus
                        .DBADashStatusEnum.Critical,
                    > 0 when idleSec > Config.IdleWarningThresholdForSleepingSessionWithOpenTran => DBADashStatus
                        .DBADashStatusEnum.Warning,
                    _ => DBADashStatus.DBADashStatusEnum.NA
                };
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(status);
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "colMaxIdleTime")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var idleSec = Convert.ToDouble(row["SleepingSessionsMaxIdleTimeMs"].DBNullToNull()) / 1000;
                var status =
                    idleSec > Config.IdleCriticalThresholdForSleepingSessionWithOpenTran
                        ?
                        DBADashStatus.DBADashStatusEnum.Critical
                        :
                        idleSec > Config.IdleWarningThresholdForSleepingSessionWithOpenTran
                            ? DBADashStatus.DBADashStatusEnum.Warning
                            :
                            DBADashStatus.DBADashStatusEnum.NA;
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(status);
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "colStatus")
            {
                var blockedCnt = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["colBlockCount"].Value.DBNullToNull());
                if (e.Value != null && e.Value.ToString() == "sleeping" && blockedCnt > 0)
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                        "Sleeping session with open transaction causing blocking";
                }
                else
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }
            }
            else if (dgv.Columns[e.ColumnIndex].Name == "colImplicitTran")
            {
                var isImplicit = (bool?)dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.DBNullToNull();
                if (!isImplicit.HasValue)
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = "Unknown";
                }
                else if (isImplicit.Value)
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = "WARNING: This session is using implicit transactions";
                }
            }
        }

        /// <summary>Takes the running query snapshot and groups the data by a specified column</summary>
        private void GroupSnapshot(string group)
        {
            if (snapshotDT == null || snapshotDT.Rows.Count <= 0) return;
            ClearBlocking();
            DataTable groupedDT = new();
            groupedDT.Columns.AddRange(
                new[]
                {
                    new DataColumn(group, typeof(string)),
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
                    row["sum_logical_reads"] = g.Sum(r =>
                        r["logical_reads"] == DBNull.Value ? 0 : r.Field<long>("logical_reads"));
                    row["sum_writes"] = g.Sum(r => r["writes"] == DBNull.Value ? 0 : r.Field<long>("writes"));
                    row["sum_cpu_time"] = g.Sum(r => r["cpu_time"] == DBNull.Value ? 0 : r.Field<int>("cpu_time"));
                    row["sum_granted_query_memory_kb"] = g.Sum(r =>
                        r["granted_query_memory_kb"] == DBNull.Value
                            ? 0
                            : Convert.ToInt64(r["granted_query_memory_kb"]));
                    row["blocked_count"] = g.Count(r => r.Field<short>("blocking_session_id") != 0);
                    row["blocking_count"] = g.Count(r => r.Field<int>("BlockCount") > 0);
                    row["root_blockers"] = g.Count(r => r.Field<bool>("IsRootBlocker"));
                    return row;
                }).CopyToDataTable();

            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewLinkColumn()
                {
                    DataPropertyName = group,
                    HeaderText = group,
                    Name = "colGroup",
                    SortMode = DataGridViewColumnSortMode.Automatic
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "execution_count",
                    HeaderText = "Execution Count",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "sum_reads",
                    HeaderText = "Sum Reads",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "sum_logical_reads",
                    HeaderText = "Sum Logical Reads",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "sum_writes",
                    HeaderText = "Sum Writes",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "sum_cpu_time",
                    HeaderText = "Sum CPU",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "sum_granted_query_memory_kb",
                    HeaderText = "Sum Granted Query Memory (KB)",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "blocked_count",
                    HeaderText = "Count of Queries Blocked",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "blocking_count",
                    HeaderText = "Count of Queries Blocking",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                },
                new DataGridViewTextBoxColumn()
                {
                    DataPropertyName = "root_blockers",
                    HeaderText = "Count of Root Blockers",
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    DefaultCellStyle = Common.DataGridViewNumericCellStyle
                }
            );
            dgv.DataSource = new DataView(groupedDT);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.ApplyTheme();
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
            PersistedFilter = dgv.RowFilter;
            PersistedSort = dgv.SortString;
            LoadSnapshot(currentSnapshotDate, -1);
        }

        private void TsNext_Click(object sender, EventArgs e)
        {
            PersistedFilter = dgv.RowFilter;
            PersistedSort = dgv.SortString;
            LoadSnapshot(currentSnapshotDate, 1);
        }

        /// <summary>Get session level wait stats for a specified session or for all sessions</summary>
        private static DataTable GetSessionWaits(int InstanceID, short? SessionID, DateTime? SnapshotDateUTC,
            DateTime? LoginTimeUTC)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.SessionWaits_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("SessionID", SessionID);
            cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
            cmd.Parameters.AddWithValue("LoginTimeUTC", LoginTimeUTC);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        /// <summary>Get session level wait stats for the specified snapshot over all sessions.</summary>
        private static DataTable GetSessionWaitSummary(int InstanceID, DateTime? SnapshotDateUTC)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.SessionWaitsSummary_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private void TsSessionWaitCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSessionWaits);
        }

        private void TsSessionWaitExcel_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.ExportToExcel();
        }

        private void AllSessionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"]!.Visible = true;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, null, currentSnapshotDate, null);
            lblWaitsForSession.Text = "All Sessions";
        }

        private void SummaryViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"]!.Visible = false;
            dgvSessionWaits.DataSource = GetSessionWaitSummary(InstanceID, currentSnapshotDate);
            lblWaitsForSession.Text = "Session Wait Summary";
        }

        private void SessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"]!.Visible = false;
            var sessionId = (short)sessionToolStripMenuItem.Tag;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, sessionId, currentSnapshotDate, null);
            lblWaitsForSession.Text = "Waits For Session ID: " + sessionId;
        }

        private void DgvSessionWaits_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvSessionWaits.Columns[e.ColumnIndex].Name != "colHelp") return;
            var wait = (string)dgvSessionWaits.Rows[e.RowIndex].Cells["colWaitType"].Value;
            var psi = new ProcessStartInfo("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/")
            { UseShellExecute = true };
            Process.Start(psi);
        }

        private void ShowRootBlockersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowRootBlockers();
        }

        private void BlockedQueriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsGroupByFilter.Visible = false;
            dgv.SetFilter("blocking_session_id > 0");
            tsBlockingFilter.Text = "Blocking : All Blocked Queries";
            tsBack.Enabled = true;
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Bold);
        }

        private void BlockedOrBlockingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsGroupByFilter.Visible = false;
            dgv.SetFilter("(blocking_session_id > 0 OR BlockCount>0)");
            tsBlockingFilter.Text = "Blocking : All Blocked or Blocking Queries";
            tsBack.Enabled = true;
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Bold);
        }

        private void BlockingQueriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tsGroupByFilter.Visible = false;
            dgv.SetFilter("BlockCount>0");
            tsBlockingFilter.Text = "Blocking : All Blocking Queries";
            tsBack.Enabled = true;
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Bold);
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
            if (sessionID == 0) // Root blockers
            {
                dgv.SetFilter($"blocking_session_id = {sessionID} AND BlockCount > 0 ");
                tsBlockingFilter.Text = "Blocking : Root Blockers";
            }
            else if (recursive)
            {
                dgv.SetFilter(
                    $"(blocking_session_id = {sessionID} OR BlockingHierarchy LIKE '{sessionID} \\%' OR BlockingHierarchy LIKE '%\\ {sessionID}' OR BlockingHierarchy LIKE '%\\ {sessionID} \\%')");
                tsBlockingFilter.Text = $"Blocking : Blocked By {sessionID} (Recursive)";
            }
            else
            {
                dgv.SetFilter($"blocking_session_id = {sessionID}");
                tsBlockingFilter.Text = $"Blocking : Blocked By {sessionID}";
            }

            tsBack.Enabled = true;
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Bold);
        }

        /// <summary>Remove blocking filter applied to grid with showBlocking()</summary>
        private void ClearBlocking()
        {
            tsGroupByFilter.Visible = false;
            if (dgv.DataSource != null)
            {
                dgv.ClearFilter();
            }

            ResetBlockingFilterText();
        }

        private void ResetBlockingFilterText()
        {
            tsBlockingFilter.Text = $"Blocking ({blockedCount} Blocked)";
            tsBlockingFilter.Font = new Font(tsBlockingFilter.Font, FontStyle.Regular);
        }

        private void ClearBlockingFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearBlocking();
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgv.PromptColumnSelection();
        }

        private void TsEditLimit_Click(object sender, EventArgs e)
        {
            var limit = Properties.Settings.Default.RunningQueriesSummaryMaxRows.ToString();
            if (CommonShared.ShowInputDialog(ref limit, "Enter row limit") != DialogResult.OK) return;
            if (int.TryParse(limit, out var maxRows) && maxRows > 0)
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
            Common.AddContextInfoDisplayAsMenu(dgv, "colContextInfo");
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

        private bool ShowLatestOnNextExecution;

        private async void TsTriggerCollection_Click(object sender, EventArgs e)
        {
            IsForceDetail = false;
            PersistedFilter = dgv.RowFilter;
            PersistedSort = dgv.SortString;
            ShowLatestOnNextExecution = true;
            await CollectionMessaging.TriggerCollection(InstanceID, CollectionType.RunningQueries, this);
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            Invoke(() =>
            {
                tsStatus.Visible = true;
                tsStatus.Text = message;
                tsStatus.ToolTipText = tooltip;
                tsStatus.IsLink = !string.IsNullOrEmpty(tooltip);
                tsStatus.ForeColor = color;
                tsStatus.LinkColor = color;
            });
        }

        private const int DefaultViewAllTop = 5000;

        private void tsViewALL_Click(object sender, EventArgs e)
        {
            var filters = new RunningQueriesFilters()
            {
                From = DateRange.FromUTC.ToAppTimeZone(),
                To = DateRange.ToUTC.ToAppTimeZone(),
                InstanceID = InstanceID,
                Top = DefaultViewAllTop
            };
            var frm = new PropertyGridDialog()
            {
                Title = "Set Filters",
                SelectedObject = filters
            };
            if (frm.ShowDialog() != DialogResult.OK) return;
            filters.From = filters.From.AppTimeZoneToUtc();
            filters.To = filters.To.AppTimeZoneToUtc();
            forceDetailFilters = filters;
            IsForceDetail = true;
            RefreshData();
        }
    }
}