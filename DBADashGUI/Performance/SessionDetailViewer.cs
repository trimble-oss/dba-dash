using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Controls;
using DBADashGUI.Messaging;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Tabbed viewer for a single running queries session.  Opened when the Session ID link is clicked.
    /// Each tab is loaded on demand (the first time it is selected) and is not reloaded when switching tabs.
    /// </summary>
    public partial class SessionDetailViewer : Form, Interface.ISetStatus
    {
        private DataRowView Row;
        private readonly DBADashContext Context;
        private readonly int InstanceID;
        private readonly int SessionID;
        private DateTime SnapshotDateUtc;
        private DateTime StartTimeUtc;
        private DateTime HistoryFromUtc;

        // Previously-viewed snapshots (row + stale warning) for Back navigation.
        private readonly Stack<(DataRowView Row, string StaleWarning)> navHistory = new();

        // True while a user-triggered "Collect Now" is in flight, so the collection reply reloads to the latest snapshot.
        private bool collectRequested;

        /// <summary>
        /// Optional warning shown in the status bar when the viewer opens - used by the "Latest Snapshot" navigation
        /// to flag that the session is no longer active (the shown snapshot is the last one it appeared in).
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string StaleWarning { get; set; }

        // Number of other sessions in the same snapshot queued on RESOURCE_SEMAPHORE (waiting for a memory grant).
        // Computed lazily from the full snapshot (in-memory when available, otherwise queried) when the Overview loads.
        private int ResourceSemaphoreWaiterCount;

        // Number of other sessions in the same snapshot also experiencing allocation-page (PFS/GAM/SGAM) contention
        // in the same database as this session. Used to judge whether allocation contention is widespread or an isolated blip.
        private int AllocationContentionPeerCount;

        // Number of sessions blocked (directly) by this session that are readers running under Read Committed and would
        // therefore benefit from RCSI. Used to decide whether to recommend RCSI when this session is the blocker.
        private int BlockedReaderPeerCount;

        // True when this session is blocked by a reader (SELECT/CONDITIONAL) running under Read Committed - under RCSI
        // that reader wouldn't take shared locks, so it wouldn't block this query (even when this query is a writer).
        private bool IsBlockedByReadCommittedReader;

        // True once the peer counts above have been computed (avoids recomputing / re-querying on tab reload).
        private bool peerCountsComputed;

        private readonly Dictionary<TabPage, Func<TabPage, Task>> loaders = new();
        private readonly HashSet<TabPage> loadedTabs = new();

        // Independent copy of the whole snapshot (all sessions). Retained so drill-down (e.g. opening a blocking
        // session) keeps context about other queries, and so peer counts survive a grid refresh.
        private DataTable Snapshot;

        // Cached fonts for insight cards, shared across labels and disposed when the form closes.
        private Font insightBoldFont;
        private Font insightRegularFont;

        /// <param name="sourceRow">The running queries snapshot row (from the grid) to display.</param>
        /// <param name="context">The current context - used for object execution drill down.</param>
        public SessionDetailViewer(DataRowView sourceRow, DBADashContext context)
        {
            InitializeComponent();
            Context = context;
            InstanceID = Convert.ToInt32(sourceRow["InstanceID"]);
            SessionID = Convert.ToInt32(sourceRow["session_id"]);
            LoadSnapshotRow(sourceRow, null);
        }

        /// <summary>
        /// (Re)load the viewer for a running-queries snapshot row. Called from the constructor and when navigating to
        /// a different snapshot of the same session in place (Latest Snapshot / Back / after Collect Now).
        /// </summary>
        private void LoadSnapshotRow(DataRowView sourceRow, string staleWarning)
        {
            // Take independent copies so the viewer isn't affected by the source grid being refreshed.
            var table = sourceRow.Row.Table.Clone();
            table.ImportRow(sourceRow.Row);
            Row = new DataView(table)[0];
            Snapshot = sourceRow.Row.Table.Copy();

            SnapshotDateUtc = Convert.ToDateTime(Row["SnapshotDate"]).AppTimeZoneToUtc();
            StartTimeUtc = Row["start_time"] == DBNull.Value
                ? Convert.ToDateTime(Row["last_request_start_time"]).AppTimeZoneToUtc()
                : Convert.ToDateTime(Row["start_time"]).AppTimeZoneToUtc();
            HistoryFromUtc = GetHistoryFromUtc();
            StaleWarning = staleWarning;

            // Reset lazily-computed peer state so it recomputes for the new snapshot.
            peerCountsComputed = false;
            ResourceSemaphoreWaiterCount = 0;
            AllocationContentionPeerCount = 0;
            BlockedReaderPeerCount = 0;
            IsBlockedByReadCommittedReader = false;

            var instanceName = Convert.ToString(Row["InstanceDisplayName"]);
            Text = $"Session {SessionID} - {instanceName} - {SnapshotDateUtc.ToAppTimeZone().ToString(CultureInfo.CurrentCulture)}";

            SetupPlanButton();
            SetupJobInfoButton();
            SetupKillButton();
            SetupCollectButton();
            UpdateNavButtons();
            BuildTabs();
        }

        /// <summary>Show the Trigger Collection button only when messaging is enabled and the user has access to it.</summary>
        private void SetupCollectButton()
        {
            tsCollectNow.Visible = CommonData.GetDBADashContext(InstanceID).CanMessage;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            insightBoldFont?.Dispose();
            insightRegularFont?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>The start of the session history: transaction start if there is an open transaction, otherwise the query start time.</summary>
        private DateTime GetHistoryFromUtc()
        {
            var openTran = Convert.ToInt32(Row["open_transaction_count"].DBNullToNull() ?? 0);
            var tranMs = Row["transaction_duration_ms"].DBNullToNull();
            if (openTran > 0 && tranMs != null)
            {
                return SnapshotDateUtc.AddMilliseconds(-Convert.ToDouble(tranMs));
            }
            return StartTimeUtc;
        }

        private void SetupPlanButton()
        {
            var action = QueryPlanActions.Determine(Row, out _);
            tsPlan.Text = QueryPlanActions.ActionText(action);
            tsPlan.Enabled = action != QueryPlanActions.PlanAction.None;
            tsPlan.ToolTipText = action switch
            {
                QueryPlanActions.PlanAction.View => "View the query plan captured for this query.",
                QueryPlanActions.PlanAction.Collect => "Collect the query plan from the source instance (via messaging) and open it.",
                QueryPlanActions.PlanAction.FindScript => "Show a script to find the query plan in the plan cache / query store on the source instance.",
                _ => "No plan information is available for this query."
            };
        }

        /// <summary>True if this session is running a SQL Agent job.</summary>
        private bool IsAgentJob() =>
            Row.Row.Table.Columns.Contains("job_id") && Row["job_id"] != DBNull.Value;

        private void SetupJobInfoButton()
        {
            tsJobInfo.Visible = IsAgentJob();
        }

        private void TsJobInfo_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsAgentJob())
                {
                    return;
                }

                var jobId = (Guid)Row["job_id"];
                var jobName = Convert.ToString(Row.Row.Table.Columns.Contains("job_name")
                    ? Row["job_name"].DBNullToNull()
                    : null);
                var jobContext = CommonData.GetDBADashContext(InstanceID);
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = jobId;
                jobContext.ObjectName = jobName;
                var frm = new AgentJobs.JobInfoForm() { DBADashContext = jobContext };
                frm.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        #region Kill session

        // A snapshot older than this can't be used to kill a session - the data is too likely to be stale
        // (the request may have finished and the SPID been reused). Enforced in the GUI; the service also
        // re-validates the live session before killing.
        private static readonly TimeSpan KillRecencyWindow = TimeSpan.FromMinutes(5);

        /// <summary>True for a user session (system sessions - session_id &lt;= 50 - are never killed).</summary>
        private bool IsUserSpid() => SessionID > 50;

        /// <summary>True when this snapshot is recent enough to act on (see <see cref="KillRecencyWindow"/>).</summary>
        private bool IsRecentSnapshot() => DateTime.UtcNow - SnapshotDateUtc <= KillRecencyWindow;

        /// <summary>
        /// Show/enable the Kill button. Hidden unless the user is permitted (<see cref="DBADashUser.AllowKillSession"/>),
        /// messaging is available and it's a user SPID. Disabled (with an explanatory tooltip) for stale snapshots.
        /// </summary>
        private void SetupKillButton()
        {
            var context = CommonData.GetDBADashContext(InstanceID);
            // NB: don't re-read tsKill.Visible here as the guard - its getter returns Available && parent.Visible,
            // which is false while the form hasn't been shown yet (constructor), so the Enabled line below would be
            // skipped and the button would keep its designer default (Enabled = true). Use the computed value.
            killButtonAvailable = DBADashUser.AllowKillSession && context.CanKillSession && IsUserSpid();
            tsKill.Visible = killButtonAvailable;
            if (!killButtonAvailable) return;

            var recent = IsRecentSnapshot();
            tsKill.Enabled = recent;
            tsKill.ToolTipText = recent
                ? "Kill this session (SPID) on the source instance."
                : $"Kill is only available for recent snapshots (within {KillRecencyWindow.TotalMinutes:0} minutes). This snapshot is too old to act on safely.";
        }

        // Whether the Kill button is applicable at all (user permitted, messaging + kill enabled, user SPID).
        // Tracked separately from tsKill.Visible because that getter is unreliable before the form is shown.
        private bool killButtonAvailable;

        private async void TsKill_Click(object sender, EventArgs e)
        {
            try
            {
                // Re-check recency at click time - the viewer may have been left open until the snapshot went stale.
                if (!IsRecentSnapshot())
                {
                    tsKill.Enabled = false;
                    tsKill.ToolTipText = $"Kill is only available for recent snapshots (within {KillRecencyWindow.TotalMinutes:0} minutes). This snapshot is too old to act on safely.";
                    MessageBox.Show(
                        $"This snapshot is more than {KillRecencyWindow.TotalMinutes:0} minutes old. Killing is only allowed for recent snapshots to avoid acting on stale data.",
                        "Snapshot too old", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var login = RowStr("login_name");
                var db = RowStr("database_name");
                var snippet = RowStr("text");
                if (string.IsNullOrEmpty(snippet)) snippet = RowStr("command");
                if (snippet.Length > 200) snippet = snippet[..200] + "...";

                var confirm = MessageBox.Show(
                    $"Kill session {SessionID} on {Convert.ToString(Row["InstanceDisplayName"])}?\r\n\r\n" +
                    $"Login: {login}\r\nDatabase: {db}\r\n\r\n{snippet}\r\n\r\n" +
                    "This terminates the session and rolls back any open transaction. This action cannot be undone.",
                    "Confirm Kill Session", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes) return;

                var context = CommonData.GetDBADashContext(InstanceID);
                if (!context.CanMessage)
                {
                    MessageBox.Show("Messaging is not available for this instance.", "Kill Session",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                tsKill.Enabled = false;
                var messageGroup = Guid.NewGuid();
                await LogKillRequestAsync(messageGroup); // Audit who killed what before we send the request
                var message = new KillSessionMessage
                {
                    ConnectionID = context.ConnectionID,
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent,
                    SessionID = SessionID,
                    ExpectedLoginName = login,
                    ExpectedStartTimeUtc = StartTimeUtc
                };
                tsStatus.InvokeSetStatus("Killing session...", string.Empty, DashColors.Information);
                await MessagingHelper.SendMessageAndProcessReply(message, context, tsStatus, HandleKillReply, messageGroup);
            }
            catch (Exception ex)
            {
                tsKill.Enabled = IsRecentSnapshot(); // Allow a retry
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private async Task HandleKillReply(ResponseMessage reply, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                var msg = ExtractKillMessage(reply) ?? $"Session {SessionID} killed.";
                setStatus(msg, string.Empty, DashColors.Green);
                await UpdateKillLogAsync(messageGroup, "KILLED");
                MessageBox.Show(msg, "Kill Session", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Leave the button disabled - the session has been killed.
            }
            else
            {
                setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                await UpdateKillLogAsync(messageGroup, Truncate(reply.Message, 200));
                tsKill.Enabled = IsRecentSnapshot(); // Allow a retry (e.g. transient failure)
            }
        }

        private static string ExtractKillMessage(ResponseMessage reply)
        {
            var table = reply.Data != null && reply.Data.Tables.Count > 0 ? reply.Data.Tables[0] : null;
            if (table == null || table.Rows.Count == 0 || !table.Columns.Contains("Message")) return null;
            return Convert.ToString(table.Rows[0]["Message"]);
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        /// <summary>
        /// Record the kill request in the repo (audit) before it is sent. Uses the message group as the key so the
        /// outcome can be updated on reply. Only the keys back to the RunningQueries snapshot are stored - the session
        /// detail is joined from dbo.RunningQueries when the log is read.
        /// </summary>
        private async Task LogKillRequestAsync(Guid messageGroup)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.KillSessionLog_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@session_id", SessionID);
            cmd.Parameters.Add("@SnapshotDate", SqlDbType.DateTime2).Value = SnapshotDateUtc;
            cmd.Parameters.AddWithValue("@killed_by", Environment.UserName);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Update the audit record with the outcome of the kill request.</summary>
        private static async Task UpdateKillLogAsync(Guid messageGroup, string status)
        {
            try
            {
                await using var cn = new SqlConnection(Common.ConnectionString);
                await using var cmd = new SqlCommand("dbo.KillSessionLog_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
                cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // The kill itself has already happened - a failure to record the outcome shouldn't surface as an error.
                Serilog.Log.Warning(ex, "Failed to update KillSessionLog for message group {messageGroup}", messageGroup);
            }
        }

        #endregion

        #region Snapshot navigation

        /// <summary>Navigate to the most recent snapshot that contains this session (in place).</summary>
        private async void TsLatest_Click(object sender, EventArgs e)
        {
            try
            {
                tsLatest.Enabled = false;
                await NavigateToLatestAsync();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
            finally
            {
                tsLatest.Enabled = true;
            }
        }

        /// <summary>Return to the previously viewed snapshot.</summary>
        private async void TsBack_Click(object sender, EventArgs e)
        {
            if (navHistory.Count == 0) return;
            try
            {
                var (row, stale) = navHistory.Pop();
                LoadSnapshotRow(row, stale);
                ApplyStaleWarning();
                await LoadTab(tabs.SelectedTab);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        /// <summary>Trigger a RunningQueries collection on the source instance (via messaging) and, on completion, load the latest snapshot.</summary>
        private async void TsCollectNow_Click(object sender, EventArgs e)
        {
            try
            {
                if (!CollectionMessaging.IsMessagingEnabled(InstanceID))
                {
                    MessageBox.Show("Messaging is not enabled for this instance, so a collection can't be triggered.",
                        "Collect Now", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                tsCollectNow.Enabled = false;
                collectRequested = true;
                await CollectionMessaging.TriggerCollection(InstanceID, DBADash.CollectionType.RunningQueries, this);
            }
            catch (Exception ex)
            {
                collectRequested = false;
                CommonShared.ShowExceptionDialog(ex);
            }
            finally
            {
                tsCollectNow.Enabled = true;
            }
        }

        /// <summary>
        /// Load the most recent snapshot containing this session. If that snapshot isn't the instance's latest (the
        /// session has dropped out of newer snapshots) the reload is flagged as no longer active.
        /// </summary>
        private async Task NavigateToLatestAsync()
        {
            var (sessionLatest, instanceLatest) = await Task.Run(GetLatestSnapshots);
            if (sessionLatest == null)
            {
                MessageBox.Show(
                    $"Session {SessionID} was not found in any retained snapshot for this instance. It may have completed and aged out of the running queries history.",
                    "Get Latest", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var stale = instanceLatest != null && sessionLatest < instanceLatest;
            var alreadyViewing = Math.Abs((sessionLatest.Value - SnapshotDateUtc).TotalSeconds) < 1;
            var lastSeen = sessionLatest.Value.ToAppTimeZone().ToString(CultureInfo.CurrentCulture);
            var latestInstance = instanceLatest?.ToAppTimeZone().ToString(CultureInfo.CurrentCulture);
            var warning = stale
                ? $"Session {SessionID} is no longer active - showing the most recent snapshot it appeared in ({lastSeen}). Latest snapshot for the instance: {latestInstance}."
                : null;

            // Load the session's most recent snapshot if we aren't already on it.
            if (!alreadyViewing)
            {
                var row = await Task.Run(() => RunningQueries.GetSessionSnapshotRow(InstanceID, SessionID, sessionLatest.Value));
                if (row == null)
                {
                    tsStatus.InvokeSetStatus("The latest snapshot for the session could not be loaded.", string.Empty, DashColors.Warning);
                    return;
                }
                await NavigateToSnapshotAsync(row, warning);
            }

            // Clear, unmissable feedback: the session has ended (Get Latest / Trigger Collection found nothing newer for it).
            if (stale)
            {
                tsStatus.InvokeSetStatus(warning, string.Empty, DashColors.Warning);
                MessageBox.Show(
                    $"Session {SessionID} is no longer active.\r\n\r\nIt last appeared in the snapshot taken at {lastSeen}.\r\nThe latest snapshot for the instance is {latestInstance}.\r\n\r\nShowing the most recent snapshot the session appeared in.",
                    "Session no longer active", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (alreadyViewing)
            {
                tsStatus.InvokeSetStatus("This is already the latest snapshot for the session.", string.Empty, DashColors.Information);
            }
        }

        /// <summary>Load a different snapshot in place, pushing the current one onto the Back history.</summary>
        private async Task NavigateToSnapshotAsync(DataRowView row, string staleWarning)
        {
            navHistory.Push((Row, StaleWarning));
            LoadSnapshotRow(row, staleWarning);
            ApplyStaleWarning();
            await LoadTab(tabs.SelectedTab);
        }

        /// <summary>Show the stale-session warning in the status bar, or clear the status bar when there is none.</summary>
        private void ApplyStaleWarning()
        {
            if (!string.IsNullOrEmpty(StaleWarning))
            {
                tsStatus.InvokeSetStatus(StaleWarning, string.Empty, DashColors.Warning);
            }
            else
            {
                tsStatus.InvokeSetStatus(string.Empty, string.Empty, DashColors.Information);
            }
        }

        private void UpdateNavButtons()
        {
            tsBack.Enabled = navHistory.Count > 0;
        }

        /// <summary>Get the most recent snapshot containing this session and the most recent snapshot for the instance (both UTC).</summary>
        private (DateTime? sessionLatestUtc, DateTime? instanceLatestUtc) GetLatestSnapshots()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.RunningQueriesSessionLatestSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@session_id", SessionID);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return (null, null);
            DateTime? sessionLatest = rdr["SessionLatestSnapshotUTC"] == DBNull.Value ? null : Convert.ToDateTime(rdr["SessionLatestSnapshotUTC"]);
            DateTime? instanceLatest = rdr["InstanceLatestSnapshotUTC"] == DBNull.Value ? null : Convert.ToDateTime(rdr["InstanceLatestSnapshotUTC"]);
            return (sessionLatest, instanceLatest);
        }

        // ISetStatus - used by the Collect Now trigger to report progress and, on completion, reload to the latest snapshot.
        public void SetStatus(string message, string tooltip, System.Drawing.Color color) =>
            tsStatus.InvokeSetStatus(message, tooltip, color);

        public void RefreshData()
        {
            if (!collectRequested) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshData));
                return;
            }
            collectRequested = false;
            _ = NavigateToLatestAsync();
        }

        #endregion

        private void BuildTabs()
        {
            // Support in-place reload: unhook the change handler and dispose any existing tabs/loaders first
            // (dispose rather than Clear so nested controls - grids, code editors, RunningQueries controls - are freed).
            tabs.SelectedIndexChanged -= Tabs_SelectedIndexChanged;
            while (tabs.TabPages.Count > 0)
            {
                var old = tabs.TabPages[tabs.TabPages.Count - 1];
                tabs.TabPages.RemoveAt(tabs.TabPages.Count - 1);
                old.Dispose();
            }
            loaders.Clear();
            loadedTabs.Clear();

            AddTab("Overview", LoadOverview);
            AddTab("Slow Query Capture", LoadSlowQueryCapture);
            AddTab("Batch Text", LoadBatchText);
            AddTab("Text", LoadText);

            if (HasObject())
            {
                AddTab("Object Execution", LoadObjectExecution);
            }

            AddTab("Session History", LoadSessionHistory);
            AddTab("Waits", LoadWaits);

            if (HasWaitResource())
            {
                AddTab("Wait Resource", LoadWaitResource);
            }

            if (IsBlocking())
            {
                AddTab("Blocked Sessions", LoadBlockedSessions);
            }

            if (IsBlocked())
            {
                AddTab("Blocking Chain", LoadBlockingChain);
            }

            if (ShowQueryStore())
            {
                AddTab("Query Store", LoadQueryStore);
            }

            tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
        }

        private void AddTab(string title, Func<TabPage, Task> loader)
        {
            var page = new TabPage(title) { Name = title };
            tabs.TabPages.Add(page);
            loaders.Add(page, loader);
        }

        private bool HasObject() =>
            Row.Row.Table.Columns.Contains("DBADashObjectID") && Row["DBADashObjectID"] != DBNull.Value &&
            Row["ObjectName"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(Row["ObjectName"]));

        /// <summary>True if this query is blocking one or more other sessions (directly or indirectly).</summary>
        private bool IsBlocking() =>
            Row.Row.Table.Columns.Contains("BlockCountRecursive") &&
            Convert.ToInt32(Row["BlockCountRecursive"].DBNullToNull() ?? 0) > 0;

        /// <summary>True if this query is blocked by another session.</summary>
        private bool IsBlocked() =>
            Row.Row.Table.Columns.Contains("blocking_session_id") &&
            Convert.ToInt32(Row["blocking_session_id"].DBNullToNull() ?? 0) != 0;

        /// <summary>True if this query has a wait resource that can be deciphered.</summary>
        private bool HasWaitResource() =>
            Row.Row.Table.Columns.Contains("wait_resource") && Row["wait_resource"] != DBNull.Value &&
            !string.IsNullOrEmpty(Convert.ToString(Row["wait_resource"]));

        private bool ShowQueryStore()
        {
            var isQueryStoreOn = Row["is_query_store_on"] != DBNull.Value && Convert.ToBoolean(Row["is_query_store_on"]);
            if (!isQueryStoreOn || Row["query_hash"] == DBNull.Value) return false;
            var context = CommonData.GetDBADashContext(InstanceID);
            return context.CanMessage;
        }

        private async void SessionDetailViewer_Load(object sender, EventArgs e)
        {
            this.ApplyTheme();
            tabs.ApplyTheme();
            ApplyStaleWarning();
            await DisableKillIfAlreadyKilledAsync();
            await LoadTab(tabs.SelectedTab);
        }

        /// <summary>
        /// Disable the Kill button when this session (in this snapshot) has already been killed from DBA Dash - e.g.
        /// when the viewer is opened from the Killed Sessions report. The server-side re-validation would reject a
        /// repeat kill anyway, but this avoids offering an action that's already been done.
        /// </summary>
        private async Task DisableKillIfAlreadyKilledAsync()
        {
            if (!killButtonAvailable || !IsRecentSnapshot()) return;
            try
            {
                await using var cn = new SqlConnection(Common.ConnectionString);
                await using var cmd = new SqlCommand("dbo.KillSessionLog_IsKilled", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("@session_id", SessionID);
                cmd.Parameters.Add("@SnapshotDate", SqlDbType.DateTime2).Value = SnapshotDateUtc;
                await cn.OpenAsync();
                if (await cmd.ExecuteScalarAsync() != null)
                {
                    tsKill.Enabled = false;
                    tsKill.ToolTipText = "This session has already been killed from DBA Dash (see the Killed Sessions report).";
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to check KillSessionLog for session {session}", SessionID);
            }
        }

        private async void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadTab(tabs.SelectedTab);
        }

        private async Task LoadTab(TabPage page)
        {
            if (page == null || loadedTabs.Contains(page)) return;
            loadedTabs.Add(page); // Add up front to prevent re-entry / reload on subsequent tab switches
            if (!loaders.TryGetValue(page, out var loader)) return;

            var loading = ShowLoading(page);
            try
            {
                await loader(page);
            }
            catch (Exception ex)
            {
                loadedTabs.Remove(page); // Allow a retry by selecting the tab again
                ShowTabMessage(page, ex.Message);
            }
            finally
            {
                page.Controls.Remove(loading);
                loading.Dispose();
            }
        }

        private static Label ShowLoading(TabPage page)
        {
            var loading = new Label
            {
                Text = "Loading...",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 30,
                Padding = new Padding(6, 0, 0, 0)
            };
            page.Controls.Add(loading);
            loading.BringToFront();
            return loading;
        }

        private void ShowTabMessage(TabPage page, string message)
        {
            var lbl = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            page.Controls.Add(lbl);
            page.ApplyTheme();
        }

        /// <summary>Show the "not found" message for the slow query capture with a Refresh button - the event may not have been captured yet.</summary>
        private void ShowSlowQueryNotFound(TabPage page)
        {
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = string.Format(
                    "No RPC or Batch completed event was found for session {0} running at {1}.\n\nThis could occur for a number of reasons:\n* The session is still running or hasn't been processed yet. (Try Refresh later)\n* Slow query capture isn't configured\n* It did not meet the threshold for collection.\n* The event was lost for some reason",
                    SessionID, SnapshotDateUtc.ToAppTimeZone())
            };
            var refresh = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Top,
                Height = 32
            };
            refresh.Click += async (_, _) => await ReloadTab(page);
            page.Controls.Add(lbl);
            page.Controls.Add(refresh);
            page.ApplyTheme();
        }

        /// <summary>Clear a tab and run its loader again.</summary>
        private async Task ReloadTab(TabPage page)
        {
            page.Controls.Clear();
            loadedTabs.Remove(page);
            await LoadTab(page);
        }

        #region Tab loaders

        private async Task LoadOverview(TabPage page)
        {
            var dgv = NewGrid();
            dgv.AutoGenerateColumns = true;
            dgv.DataSource = BuildOverviewTable(SingleRowClone());
            page.Controls.Add(dgv);

            // Only relevant when there is blocking - used to avoid recommending RCSI when it's already enabled.
            bool? rcsiEnabled = null;
            if (IsBlockingScenario())
            {
                try
                {
                    await Task.Run(() => rcsiEnabled = GetRcsiEnabled());
                }
                catch
                {
                    // RCSI state is a nice-to-have - if we can't determine it, fall back to showing the recommendation
                }
            }

            // Peer counts (memory-grant waiters, allocation-contention peers) come from the full snapshot.
            // Use the in-memory snapshot when it's complete; otherwise (e.g. opened from session history, which
            // spans multiple snapshots) query the full snapshot for this date off the UI thread.
            await Task.Run(EnsurePeerCounts);

            var insightsPanel = BuildInsightsPanel(BuildInsights(rcsiEnabled, ResourceSemaphoreWaiterCount, AllocationContentionPeerCount));
            page.Controls.Add(insightsPanel);

            SizeOverviewGrid(dgv); // Size the Attribute column to its content and let Value fill the rest
            page.ApplyTheme();
            ApplyInsightColors(insightsPanel); // Re-apply severity colours after theming overwrites label ForeColor
            StyleOverviewSections(dgv); // Re-apply section header styling after theming
            HighlightMemoryGrant(dgv); // Draw attention to a large memory grant
        }

        #region Overview insights

        private enum InsightSeverity
        {
            Info,
            Warning,
            Critical
        }

        private sealed class Insight
        {
            public InsightSeverity Severity { get; }
            public string Text { get; }

            /// <summary>Optional in-app actions keyed by the <c>action:&lt;key&gt;</c> links used in <see cref="Text"/>.</summary>
            public IReadOnlyDictionary<string, Action> Actions { get; }

            /// <summary>Optional hover tooltips keyed by the full link url (e.g. <c>action:&lt;key&gt;</c>) used in <see cref="Text"/>.</summary>
            public IReadOnlyDictionary<string, string> Tooltips { get; }

            public Insight(InsightSeverity severity, string text, IReadOnlyDictionary<string, Action> actions = null, IReadOnlyDictionary<string, string> tooltips = null)
            {
                Severity = severity;
                Text = text;
                Actions = actions;
                Tooltips = tooltips;
            }
        }

        /// <summary>Inspect the row and surface any notable issues (blocking, sleeping open transactions, memory waits, etc.).</summary>
        private List<Insight> BuildInsights(bool? rcsiEnabled = null, int resourceSemaphoreWaiters = 0, int allocationContentionPeers = 0)
        {
            var list = new List<Insight>();

            var blockingSessionId = RowInt("blocking_session_id");
            var blockCount = RowInt("BlockCount");
            var blockCountRecursive = Math.Max(RowInt("BlockCountRecursive"), blockCount);
            var openTran = RowInt("open_transaction_count");
            var isSleeping = string.Equals(RowStr("status"), "sleeping", StringComparison.OrdinalIgnoreCase);
            var isSleepingBlocking = isSleeping && openTran > 0 && blockCountRecursive > 0;
            var waitType = RowStr("wait_type").ToUpperInvariant();

            // Allocation-page (PFS/GAM/SGAM) latch contention. Detected early because RCSI advice doesn't apply to it -
            // RCSI only helps reader/writer lock blocking, not allocation-page latch contention.
            var pageType = RowStr("page_type");
            var isAllocationPage = pageType is "PFS" or "GAM" or "SGAM";

            // The current wait time for this query.  A longer wait is a stronger signal, so it feeds both the wording and the severity.
            // The point at which a wait becomes "critical" depends on the wait type (see WaitSeverity thresholds).
            var waitMs = RowDouble("wait_time");
            var waited = WaitedSuffix(waitMs);

            // This query is blocked
            if (blockingSessionId != 0)
            {
                var actions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
                {
                    ["open-blocker"] = () => OpenSession(blockingSessionId)
                };
                list.Add(new Insight(InsightSeverity.Critical,
                    $"This query is blocked by session [{blockingSessionId}](action:open-blocker){waited}.",
                    actions));
                // RCSI helps in both directions: when this query is a reader blocked by a writer (this query is a
                // Read Committed read), and when this query is blocked by a reader (the blocker is a Read Committed
                // read that under RCSI wouldn't take shared locks) - so a writer blocked by a SELECT/CONDITIONAL qualifies too.
                var thisQueryIsBlockedReader = IsReadingCommand() && IsReadCommittedIsolation();
                if ((thisQueryIsBlockedReader || IsBlockedByReadCommittedReader) && rcsiEnabled != true && !isAllocationPage)
                {
                    list.Add(RcsiInsight());
                }
            }

            // This query is blocking others (the sleeping + blocking case is described in the sleeping section below)
            if (blockCountRecursive > 0)
            {
                var blockWait = BlockWaitSuffix();
                if (!isSleepingBlocking)
                {
                    var rootNote = RowBool("IsRootBlocker") == true
                        ? " This query is the root blocker (it is not itself blocked)."
                        : string.Empty;
                    list.Add(new Insight(InsightSeverity.Critical,
                        $"This query is blocking {Pluralize(blockCountRecursive, "query", "queries")}{blockWait}.{rootNote}"));
                }

                // The RCSI advice depends on the blocked victims being readers
                if (BlockedReaderPeerCount > 0 && rcsiEnabled != true && !isAllocationPage)
                {
                    list.Add(RcsiInsight());
                }
            }

            // Sleeping session - always report how long it's been sleeping, escalating emphasis the longer it has been idle
            if (isSleeping)
            {
                var idleSec = RowDouble("sleeping_session_idle_time_sec");
                var idleText = RowStr("sleeping_session_idle_time");
                if (string.IsNullOrEmpty(idleText))
                {
                    idleText = TimeSpan.FromSeconds(idleSec).Humanize(precision: 2, maxUnit: TimeUnit.Day);
                }

                var idleSeverity = idleSec >= Config.IdleCriticalThresholdForSleepingSessionWithOpenTran
                    ? InsightSeverity.Critical
                    : idleSec >= Config.IdleWarningThresholdForSleepingSessionWithOpenTran
                        ? InsightSeverity.Warning
                        : InsightSeverity.Info;

                if (isSleepingBlocking)
                {
                    list.Add(new Insight(InsightSeverity.Critical,
                        $"This session is sleeping (idle for {idleText}) and blocking {Pluralize(blockCountRecursive, "query", "queries")}{BlockWaitSuffix()}. SQL Server is waiting for the application to submit more work while a transaction has been left open, causing blocking for other queries."));
                }
                else if (openTran > 0)
                {
                    // Idle + open transaction is a risk regardless, but emphasise when it's been idle a long time
                    var severity = idleSeverity == InsightSeverity.Info ? InsightSeverity.Warning : idleSeverity;
                    list.Add(new Insight(severity,
                        $"This session is sleeping with an open transaction and has been idle for {idleText}. This can cause blocking or prevent transaction log truncation. Sleeping sessions with open transactions usually indicate an application issue."));
                }
                else
                {
                    list.Add(new Insight(idleSeverity,
                        $"This session is sleeping and has been idle for {idleText}."));
                }
            }

            // Wait resource: tempdb / allocation page contention
            if (isAllocationPage)
            {
                var isTempDb = IsTempDbWait();
                var db = isTempDb ? "tempdb" : string.IsNullOrEmpty(RowStr("wait_db")) ? "a database" : RowStr("wait_db");
                // Allocation contention happens in user databases too (heavy concurrent inserts), so the remediation differs from tempdb.
                var remedy = isTempDb
                    ? " Common solutions include ensuring tempdb has multiple, evenly-sized data files, tuning the workload to write less data to tempdb, and updating to a later CU / version of SQL Server."
                    : " Common solutions include adding more, evenly-sized data files to the filegroup, spreading the inserts across multiple objects, and avoiding sequential/hotspot inserts into a single heap or clustered index.";

                // Judge how significant this looks. A single allocation-page hit with no actual wait time and no other
                // sessions hitting allocation contention in the same database is most likely a transient blip.
                var widespread = allocationContentionPeers >= AllocationContentionWaiterThreshold;
                var otherWaitersNote = allocationContentionPeers > 0
                    ? $" {(allocationContentionPeers == 1 ? "1 other query in this snapshot is" : $"{allocationContentionPeers} other queries in this snapshot are")} also waiting on allocation pages in {db}."
                    : $" No other queries in this snapshot are waiting on allocation pages in {db}.";

                if (waitMs <= 0 && !widespread)
                {
                    // Not actually waiting and not widespread - surface for awareness only.
                    list.Add(new Insight(InsightSeverity.Info,
                        $"This query is on a {pageType} allocation page in {db} but has no recorded wait time.{otherWaitersNote} This looks like a transient blip rather than sustained allocation contention and can probably be ignored."));
                }
                else
                {
                    // Escalate to critical for a long wait; otherwise warn (widespread or a measurable wait).
                    var severity = waitMs >= AllocationCriticalMs ? InsightSeverity.Critical : InsightSeverity.Warning;
                    list.Add(new Insight(severity,
                        $"This query is waiting on a {pageType} allocation page in {db}{waited}, which looks like allocation contention.{otherWaitersNote}{remedy}"));
                }
            }
            else if (IsTempDbWait())
            {
                list.Add(new Insight(WaitSeverity(waitMs, TempDbCriticalMs),
                    $"This query is waiting on a resource in tempdb (database_id 2){waited}. This isn't allocation contention (the wait isn't on a GAM/SGAM/PFS allocation page) - it could be metadata contention. Use the 'Decipher Wait Resource' tab to inspect the exact resource."));
            }

            // Compilation waits
            if (RowBool("wait_is_compile") == true)
            {
                list.Add(new Insight(WaitSeverity(waitMs, CompileLockCriticalMs),
                    $"This query is blocked waiting to compile a query plan (a compile lock){waited}. Compile locks serialize when multiple sessions try to compile the same object at the same time - often seen with unparameterized ad-hoc queries."));
            }
            if (waitType == "RESOURCE_SEMAPHORE_QUERY_COMPILE")
            {
                list.Add(new Insight(WaitSeverity(waitMs, CompileMemoryCriticalMs),
                    $"This query is waiting for memory to compile its plan ({WaitTypeLink(waitType)}){waited}. This often indicates memory pressure or a large volume of ad-hoc/uncached queries."));
            }

            // Waiting for a memory grant to run
            if (waitType == "RESOURCE_SEMAPHORE")
            {
                list.Add(new Insight(WaitSeverity(waitMs, MemoryGrantCriticalMs),
                    $"This query is in a queue waiting for a memory grant before it can run ({WaitTypeLink(waitType)}){waited}. Look for queries running with large memory grants and reduce them (e.g. eliminate unnecessary sorts) or consider adding more memory."));
            }

            // Running with a large memory grant - a common cause of RESOURCE_SEMAPHORE waits for other queries.
            // This is only a warning unless other queries are actually queued waiting for memory (RESOURCE_SEMAPHORE).
            if (HasLargeMemoryGrant())
            {
                var grantKb = RowDouble("granted_query_memory_kb");
                var grantText = HumanizeKb(grantKb);
                var waiterNote = resourceSemaphoreWaiters > 0
                    ? $" {(resourceSemaphoreWaiters == 1 ? "1 other query is" : $"{resourceSemaphoreWaiters} other queries are")} currently queued waiting for a memory grant ({WaitTypeLink("RESOURCE_SEMAPHORE")}), so this grant may be contributing to memory pressure."
                    : string.Empty;
                list.Add(new Insight(resourceSemaphoreWaiters > 0 ? InsightSeverity.Critical : InsightSeverity.Warning,
                    $"This query has a large memory grant of {grantText}.{waiterNote} Right-sizing a memory grant is a balance: too small a grant can slow this query (sorts and hashes spill to tempdb), while too large a grant reserves memory that other queries can't use and can lead to RESOURCE_SEMAPHORE waits. If the grant is over-sized, consider reducing it by removing unnecessary sorts (an appropriate index can allow SQL Server to return rows in order without a sort operator), improving cardinality estimates, or adding more memory. As a last resort, query hints (e.g. MAX_GRANT_PERCENT) or Resource Governor can be used to cap memory grants."));
            }

            // Client not consuming results fast enough
            if (waitType == "ASYNC_NETWORK_IO")
            {
                list.Add(new Insight(WaitSeverity(waitMs, AsyncNetworkIoCriticalMs),
                    $"This query is waiting on {WaitTypeLink(waitType)}{waited}. This usually means the client application isn't consuming the results fast enough (for example processing rows one at a time, or a slow/overloaded client) rather than a SQL Server problem. Review how the application reads results and consider returning less data."));
            }

            // Service Broker RECEIVE waiting on an empty queue - expected idle behaviour, not a problem
            if (waitType == "BROKER_RECEIVE_WAITFOR")
            {
                list.Add(new Insight(InsightSeverity.Info,
                    $"This is a Service Broker query waiting for a message to arrive on the queue ({WaitTypeLink(waitType)}){waited}. It occurs when the queue is empty and the RECEIVE is waiting for the next message - this is normal idle behaviour and doesn't indicate a problem that needs fixing."));
            }

            // WAITFOR - a deliberate, user-initiated delay in the query itself
            if (waitType == "WAITFOR")
            {
                list.Add(new Insight(InsightSeverity.Info,
                    $"This query is executing a WAITFOR statement ({WaitTypeLink(waitType)}){waited} - a deliberate, user-initiated delay coded into the query (WAITFOR DELAY / WAITFOR TIME). It isn't consuming CPU while it waits and is usually intentional, so it often needs no action. However, it could be worth reviewing whether the wait is needed."));
            }

            // Implicit transactions
            if (RowBool("is_implicit_transaction") == true)
            {
                list.Add(new Insight(InsightSeverity.Warning,
                    "This session is using implicit transactions, which are best avoided. Transactions can be started without an explicit BEGIN TRAN and may be left open unintentionally, causing blocking or log growth."));
            }

            // When nothing notable was detected, describe the current state of the query instead of a generic
            // "no issues" message so the user still gets useful context about what the session is doing.
            if (list.Count == 0)
            {
                var statusInsight = BuildStatusInsight(waitType, waitMs, waited);
                if (statusInsight != null)
                {
                    list.Add(statusInsight);
                }
            }

            return list;
        }

        /// <summary>
        /// Describe the current execution state of the session (running / runnable / suspended / etc.) as an
        /// informational insight. Used when no specific issue was detected so the user still gets context.
        /// See https://dba.stackexchange.com/questions/211034 for the meaning of these statuses.
        /// </summary>
        private Insight BuildStatusInsight(string waitType, double waitMs, string waited)
        {
            var status = RowStr("status").ToLowerInvariant();
            switch (status)
            {
                case "running":
                    return new Insight(InsightSeverity.Info,
                        "The query is currently running - it is actively executing on a scheduler (CPU).");

                case "runnable":
                    return new Insight(InsightSeverity.Info,
                        "The query is currently in a runnable state. It has everything it needs to run and is briefly waiting in the runnable queue for its turn (a quantum) on the scheduler (CPU). This is a normal part of query execution.");

                case "suspended":
                    if (!string.IsNullOrEmpty(waitType))
                    {
                        return new Insight(InsightSeverity.Info,
                            $"The query is currently suspended, waiting on {WaitTypeLink(waitType)}{waited}.");
                    }
                    return new Insight(InsightSeverity.Info,
                        $"The query is currently suspended{waited}. It is waiting for a resource to become available before it can continue.");

                case "background":
                    return new Insight(InsightSeverity.Info,
                        "This is a background task run by SQL Server itself rather than a user query.");

                default:
                    return new Insight(InsightSeverity.Info, "No issues were automatically detected for this query.");
            }
        }

        /// <summary>
        /// Render a wait type as a markdown link to its sqlskills.com reference page (used by CreateContentLabel).
        /// </summary>
        private static string WaitTypeLink(string waitType)
        {
            if (string.IsNullOrEmpty(waitType)) return waitType;
            return $"[{waitType}](https://www.sqlskills.com/help/waits/{waitType.ToLowerInvariant()}/)";
        }

        // Current SPID is performing a pure read that RCSI can unblock.
        private bool IsReadingCommand() => IsReadCommand(RowStr("command"));

        // Prefix check shared by IsReadingCommand and the blocked-reader peer count. SELECT is the obvious read;
        // CONDITIONAL is a control-flow predicate (e.g. IF EXISTS(SELECT ...)) that reads under the session's
        // isolation level and takes no modification locks, so RCSI helps it the same way.
        private static bool IsReadCommand(string command)
        {
            var c = command?.Trim();
            return c?.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) == true ||
                   c?.StartsWith("CONDITIONAL", StringComparison.OrdinalIgnoreCase) == true;
        }

        // RCSI only changes the behaviour of the default Read Committed isolation level, so the advice only applies
        // when the session is running under Read Committed. Note SQL Server still reports isolation level 'ReadCommitted'
        // when RCSI is enabled, so this correctly gates the recommendation. Higher levels (Repeatable Read / Serializable),
        // Snapshot, or locking hints (HOLDLOCK/UPDLOCK/XLOCK) are not helped by RCSI.
        private bool IsReadCommittedIsolation() =>
            IsReadCommittedIsolation(RowStr("transaction_isolation_level"));

        private static bool IsReadCommittedIsolation(string isolationLevel) =>
            string.Equals(isolationLevel, "ReadCommitted", StringComparison.OrdinalIgnoreCase);


        private static string Pluralize(int count, string singular, string plural) =>
            count + " " + (count == 1 ? singular : plural);

        // Wait time (ms) at or above which each kind of wait is escalated from a warning to a critical issue.
        // Thresholds vary by wait type - e.g. an allocation-page latch should never take long, whereas queuing
        // for a memory grant can legitimately take longer.
        private const double AllocationCriticalMs = 1_000;      // PFS/GAM/SGAM latch - should be sub-second
        private const double TempDbCriticalMs = 5_000;          // Other tempdb contention
        private const double CompileLockCriticalMs = 5_000;     // Compile lock (serialized compilation)
        private const double CompileMemoryCriticalMs = 10_000;  // Waiting for memory to compile
        private const double MemoryGrantCriticalMs = 30_000;    // Queued for a memory grant to run
        private const double AsyncNetworkIoCriticalMs = 30_000; // Client not consuming results (rarely a server problem)

        // Memory grant (KB) at or above which a running query is flagged as having a large grant.
        private const double LargeMemoryGrantKB = 512d * 1024;      // 512 MB

        // Number of other sessions also hitting allocation-page contention in the same database at or above which the
        // wait is treated as widespread contention (rather than an isolated blip) even when this query's wait time is 0.
        private const int AllocationContentionWaiterThreshold = 3;

        // Shared RCSI recommendation, used both when this query is a blocked reader and when it is a writer blocking
        // readers. Kept in one place so the wording stays consistent. The message carries a clickable footnote link
        // that discloses the cases the gating logic can't detect (e.g. SELECTs with locking hints such as
        // HOLDLOCK/UPDLOCK/XLOCK, which still block under RCSI) - detecting those reliably would require parsing the
        // query text, so we disclose instead of over-engineering the detection.
        private const string RcsiRecommendation =
            "Read Committed Snapshot Isolation level (RCSI) can eliminate reader/writer blocking. It's not enabled for this database and it could prevent blocking occurrences similar to this one.\u207D[\u00B9](action:rcsi-caveat)\u207E Although RCSI is beneficial for most workloads, it does have a cost associated with it and for existing databases it will require careful evaluation.";

        private const string RcsiCaveat =
            "The detection logic for this recommendation isn't accurate in all scenarios. In particular, RCSI won't relieve blocking where a query uses locking hints (e.g. HOLDLOCK, UPDLOCK, XLOCK), as these aren't visible to the detection logic.";

        // The RCSI insight and its footnote tooltip, shared by both blocking directions.
        private static Insight RcsiInsight() => new(
            InsightSeverity.Warning,
            RcsiRecommendation,
            tooltips: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["action:rcsi-caveat"] = RcsiCaveat
            });

        /// <summary>True when this query is running with a memory grant large enough to be worth highlighting.</summary>
        private bool HasLargeMemoryGrant() => RowDouble("granted_query_memory_kb") >= LargeMemoryGrantKB;

        /// <summary>Format a size given in KB as a human-readable string (MB/GB).</summary>
        private static string HumanizeKb(double kb)
        {
            if (kb >= 1024 * 1024)
            {
                return $"{kb / (1024 * 1024):0.##} GB";
            }
            return kb >= 1024 ? $"{kb / 1024:0.##} MB" : $"{kb:0.##} KB";
        }

        private static InsightSeverity WaitSeverity(double waitMs, double criticalThresholdMs) =>
            waitMs >= criticalThresholdMs ? InsightSeverity.Critical : InsightSeverity.Warning;

        private static string HumanizeMs(double ms) =>
            ms <= 0 ? null : TimeSpan.FromMilliseconds(ms).Humanize(precision: 2, maxUnit: TimeUnit.Day);

        /// <summary>" (waiting X)" suffix for the current wait time, or empty when there is no meaningful wait.</summary>
        private static string WaitedSuffix(double waitMs)
        {
            var human = HumanizeMs(waitMs);
            return human == null ? string.Empty : $" (waiting {human})";
        }

        /// <summary>" for X" suffix describing the total wait time of the sessions blocked by this query.</summary>
        private string BlockWaitSuffix()
        {
            var ms = RowDouble("BlockWaitTimeRecursiveMs");
            var human = HumanizeMs(ms);
            return human == null ? string.Empty : $" for a total of {human}";
        }

        private bool IsTempDbWait() =>
            RowInt("wait_database_id") == 2 ||
            string.Equals(RowStr("wait_db"), "tempdb", StringComparison.OrdinalIgnoreCase);


        /// <summary>True when this session is involved in blocking (blocked and/or blocking others).</summary>
        private bool IsBlockingScenario() =>
            RowInt("blocking_session_id") != 0 ||
            RowInt("BlockCount") > 0 ||
            RowInt("BlockCountRecursive") > 0;

        /// <summary>
        /// Look up whether Read Committed Snapshot Isolation is enabled for the session's database
        /// (from the most recent dbo.Databases snapshot). Returns null when it can't be determined.
        /// </summary>
        private bool? GetRcsiEnabled()
        {
            var dbName = RowStr("database_name");
            if (string.IsNullOrEmpty(dbName)) return null;

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceIDs", InstanceID.ToString());
            cmd.Parameters.AddWithValue("@DatabaseName", dbName);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (!rdr.Read()) return null;
            var result = rdr["is_read_committed_snapshot_on"];
            if (result == null || result == DBNull.Value) return null;
            return Convert.ToBoolean(result);
        }

        private FlowLayoutPanel BuildInsightsPanel(List<Insight> insights)
        {
            if (insights.Count == 0)
            {
                insights = new List<Insight>
                {
                    new(InsightSeverity.Info, "No issues were automatically detected for this query.")
                };
            }

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 8, 10, 10)
            };

            foreach (var insight in insights)
            {
                var textColor = InsightTextColor(insight.Severity);
                var backColor = InsightBackColor(insight.Severity);

                var card = new InsightCard
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(InsightCard.AccentWidth + InsightCard.IconGutter, 12, 16, 12),
                    FillColor = backColor,
                    AccentColor = InsightColor(insight.Severity),
                    Icon = InsightIcon(insight.Severity)
                };

                var lbl = InsightCard.CreateContentLabel(insight.Text, insight.Actions, insight.Tooltips);
                lbl.Dock = DockStyle.Top;
                lbl.BackColor = backColor;
                lbl.ForeColor = textColor;
                lbl.Tag = (textColor, backColor);
                lbl.Font = insight.Severity == InsightSeverity.Critical
                    ? (insightBoldFont ??= new Font(Font, FontStyle.Bold))
                    : (insightRegularFont ??= new Font(Font, FontStyle.Regular));
                card.Controls.Add(lbl);
                panel.Controls.Add(card);
            }

            panel.SizeChanged += (_, _) => UpdateInsightLabelWidths(panel);
            UpdateInsightLabelWidths(panel);
            return panel;
        }

        private static void UpdateInsightLabelWidths(FlowLayoutPanel panel)
        {
            var available = panel.ClientSize.Width - panel.Padding.Horizontal;
            if (available <= 0) return;
            foreach (Control card in panel.Controls)
            {
                var cardWidth = available - card.Margin.Horizontal;
                if (cardWidth <= 0) continue;
                // Stretch each card to fill the panel width instead of shrinking to its text.
                card.MinimumSize = new Size(cardWidth, 0);
                card.MaximumSize = new Size(cardWidth, 0);
                var w = cardWidth - card.Padding.Horizontal;
                if (w <= 0) continue;
                foreach (Control c in card.Controls)
                {
                    c.MaximumSize = new Size(w, 0);
                }
            }
        }

        // Cards keep their pale severity background regardless of theme, so re-apply the label
        // fore/back colours after theming overwrites them with the generic theme colours.
        private static void ApplyInsightColors(Control panel)
        {
            foreach (Control card in panel.Controls)
            {
                foreach (Control c in card.Controls)
                {
                    if (c is Label { Tag: ValueTuple<Color, Color> colors } lbl)
                    {
                        lbl.ForeColor = colors.Item1;
                        lbl.BackColor = colors.Item2;
                        if (lbl is LinkLabel link)
                        {
                            // Keep links legible against the pale severity background in either theme.
                            link.LinkColor = DashColors.LinkColor;
                        }
                    }
                }
            }
        }

        private static Color InsightColor(InsightSeverity severity) => severity switch
        {
            InsightSeverity.Critical => DashColors.Fail,
            InsightSeverity.Warning => DashColors.Warning,
            _ => DashColors.Information
        };

        // Pale card background per severity (from the Modus palette).
        private static Color InsightBackColor(InsightSeverity severity) => severity switch
        {
            InsightSeverity.Critical => DashColors.RedPale,
            InsightSeverity.Warning => DashColors.YellowPale,
            _ => DashColors.BluePale
        };

        // Dark, readable text so the message stays legible on the pale card background in either theme.
        private static Color InsightTextColor(InsightSeverity severity) => severity switch
        {
            InsightSeverity.Critical => DashColors.RedDark,
            InsightSeverity.Warning => DashColors.Gray10,
            _ => DashColors.Gray10
        };

        private static InsightCard.CardIcon InsightIcon(InsightSeverity severity) => severity switch
        {
            InsightSeverity.Critical => InsightCard.CardIcon.Critical,
            InsightSeverity.Warning => InsightCard.CardIcon.Warning,
            _ => InsightCard.CardIcon.Information
        };

        /// <summary>Open another session from the same snapshot in a new viewer (used by insight drill-down links).</summary>
        private void OpenSession(int sessionId)
        {
            try
            {
                // Prefer opening from the retained snapshot so the new viewer keeps context about the rest of the
                // snapshot (peer contention, memory grant waiters, etc.). Only trust the in-memory snapshot when it's
                // the complete set of sessions for this date; otherwise fall back to a fresh query.
                var peerRow = InMemorySnapshotIsComplete() ? FindSessionInSnapshot(sessionId) : null;
                if (peerRow != null)
                {
                    var frm = new SessionDetailViewer(peerRow, Context);
                    frm.ShowSingleInstance();
                    return;
                }

                var context = CommonData.GetDBADashContext(InstanceID);
                if (!RunningQueries.ShowSessionDetail(InstanceID, sessionId, SnapshotDateUtc, context))
                {
                    MessageBox.Show(
                        $"Session {sessionId} was not found in this snapshot. It may have completed before the snapshot was taken.",
                        "Session not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error opening session detail");
            }
        }

        /// <summary>Locate a session row within the retained snapshot, or null if it isn't present.</summary>
        private DataRowView FindSessionInSnapshot(int sessionId)
        {
            if (Snapshot == null || !Snapshot.Columns.Contains("session_id"))
            {
                return null;
            }

            var match = Snapshot.AsEnumerable().FirstOrDefault(r =>
                Convert.ToInt32(r["session_id"].DBNullToNull() ?? 0) == sessionId);
            if (match == null)
            {
                return null;
            }

            return new DataView(Snapshot)[Snapshot.Rows.IndexOf(match)];
        }

        /// <summary>
        /// Compute the peer counts used by the insights (memory-grant waiters and allocation-contention peers).
        /// Uses the in-memory snapshot when it's complete, otherwise queries the full snapshot for this date.
        /// Runs once; safe to call from a background thread.
        /// </summary>
        private void EnsurePeerCounts()
        {
            if (peerCountsComputed)
            {
                return;
            }

            var snapshot = InMemorySnapshotIsComplete() ? Snapshot : TryLoadFullSnapshot();
            if (snapshot != null)
            {
                ResourceSemaphoreWaiterCount = CountResourceSemaphoreWaiters(snapshot, SessionID);
                AllocationContentionPeerCount = CountAllocationContentionPeers(snapshot, RowInt("wait_database_id"), RowStr("wait_db"), SessionID);
                BlockedReaderPeerCount = CountBlockedReaderPeers(snapshot, SessionID);
                IsBlockedByReadCommittedReader = BlockerIsReadCommittedReader(snapshot, RowInt("blocking_session_id"));
            }
            peerCountsComputed = true;
        }

        /// <summary>
        /// True when the retained snapshot is the complete set of sessions for this snapshot date - i.e. a single
        /// snapshot with more than one session. A session-history table (multiple snapshots) or a single-row
        /// drill-down copy returns false so the caller queries the full snapshot instead.
        /// </summary>
        private bool InMemorySnapshotIsComplete()
        {
            if (Snapshot == null ||
                !Snapshot.Columns.Contains("session_id") ||
                !Snapshot.Columns.Contains("SnapshotDate"))
            {
                return false;
            }

            var distinctSnapshots = Snapshot.AsEnumerable()
                .Where(r => r["SnapshotDate"] != DBNull.Value)
                .Select(r => Convert.ToDateTime(r["SnapshotDate"]))
                .Distinct()
                .Count();
            if (distinctSnapshots != 1)
            {
                return false; // e.g. session history spans multiple snapshots
            }

            var distinctSessions = Snapshot.AsEnumerable()
                .Select(r => Convert.ToInt32(r["session_id"].DBNullToNull() ?? 0))
                .Distinct()
                .Count();
            return distinctSessions > 1;
        }

        /// <summary>Query the full snapshot (all sessions) for this instance and date; null on failure (peer context is a nice-to-have).</summary>
        private DataTable TryLoadFullSnapshot()
        {
            try
            {
                return RunningQueries.GetFullSnapshot(InstanceID, SnapshotDateUtc);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Count other sessions in the snapshot that are waiting on RESOURCE_SEMAPHORE (queued for a memory grant).
        /// </summary>
        private static int CountResourceSemaphoreWaiters(DataTable snapshot, int excludeSessionId)
        {
            if (snapshot == null ||
                !snapshot.Columns.Contains("wait_type") ||
                !snapshot.Columns.Contains("session_id"))
            {
                return 0;
            }

            return snapshot.AsEnumerable().Count(r =>
                string.Equals(r.CellStr("wait_type"), "RESOURCE_SEMAPHORE", StringComparison.OrdinalIgnoreCase) &&
                r.CellInt("session_id") != excludeSessionId);
        }

        /// <summary>
        /// Count other sessions in the snapshot also experiencing allocation-page (PFS/GAM/SGAM) contention
        /// in the same database as this session. Uses the already-loaded snapshot table so no extra DB query is needed.
        /// </summary>
        private static int CountAllocationContentionPeers(DataTable snapshot, int waitDatabaseId, string waitDb, int excludeSessionId)
        {
            if (snapshot == null ||
                !snapshot.Columns.Contains("page_type") ||
                !snapshot.Columns.Contains("session_id"))
            {
                return 0;
            }

            var hasWaitDbId = snapshot.Columns.Contains("wait_database_id");
            var hasWaitDb = snapshot.Columns.Contains("wait_db");

            return snapshot.AsEnumerable().Count(r =>
            {
                if (r.CellInt("session_id") == excludeSessionId)
                {
                    return false;
                }

                // Peer must also be waiting on an allocation page (PFS/GAM/SGAM).
                if (r.CellStr("page_type") is not ("PFS" or "GAM" or "SGAM"))
                {
                    return false;
                }

                // And in the same database as this query (prefer the database_id, falling back to the name).
                if (hasWaitDbId && waitDatabaseId > 0)
                {
                    return r.CellInt("wait_database_id") == waitDatabaseId;
                }
                if (hasWaitDb && !string.IsNullOrEmpty(waitDb))
                {
                    return string.Equals(r.CellStr("wait_db"), waitDb, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            });
        }

        /// <summary>
        /// Count sessions directly blocked by this session that are readers running under the default Read Committed
        /// isolation level - i.e. the blocking victims that RCSI could unblock. Used to decide whether to recommend
        /// RCSI when this session is the blocker.
        /// </summary>
        private static int CountBlockedReaderPeers(DataTable snapshot, int blockerSessionId)
        {
            if (snapshot == null ||
                !snapshot.Columns.Contains("blocking_session_id") ||
                !snapshot.Columns.Contains("command"))
            {
                return 0;
            }

            // If the isolation level isn't available, don't exclude on it (SQL Server reports 'ReadCommitted' even when RCSI is on).
            var hasIsolation = snapshot.Columns.Contains("transaction_isolation_level");

            return snapshot.AsEnumerable().Count(r =>
                r.CellInt("blocking_session_id") == blockerSessionId &&
                IsReadCommand(r.CellStr("command")) &&
                (!hasIsolation || IsReadCommittedIsolation(r.CellStr("transaction_isolation_level"))));
        }

        /// <summary>
        /// True when the blocking session is itself a reader (SELECT/CONDITIONAL) running under Read Committed. Under
        /// RCSI that reader wouldn't take shared locks, so it wouldn't block this session - which is why the advice is
        /// relevant even when this session is a writer being blocked by a reader.
        /// </summary>
        private static bool BlockerIsReadCommittedReader(DataTable snapshot, int blockingSessionId)
        {
            if (blockingSessionId == 0 ||
                snapshot == null ||
                !snapshot.Columns.Contains("session_id") ||
                !snapshot.Columns.Contains("command"))
            {
                return false;
            }

            // If the isolation level isn't available, don't exclude on it (SQL Server reports 'ReadCommitted' even when RCSI is on).
            var hasIsolation = snapshot.Columns.Contains("transaction_isolation_level");

            return snapshot.AsEnumerable().Any(r =>
                r.CellInt("session_id") == blockingSessionId &&
                IsReadCommand(r.CellStr("command")) &&
                (!hasIsolation || IsReadCommittedIsolation(r.CellStr("transaction_isolation_level"))));
        }

        private int RowInt(string column) =>
            Row.Row.Table.Columns.Contains(column) ? Convert.ToInt32(Row[column].DBNullToNull() ?? 0) : 0;

        private double RowDouble(string column) =>
            Row.Row.Table.Columns.Contains(column) ? Convert.ToDouble(Row[column].DBNullToNull() ?? 0) : 0;

        private string RowStr(string column) =>
            Row.Row.Table.Columns.Contains(column) ? Convert.ToString(Row[column].DBNullToNull()) ?? string.Empty : string.Empty;

        private bool? RowBool(string column)
        {
            if (!Row.Row.Table.Columns.Contains(column)) return null;
            var value = Row[column].DBNullToNull();
            return value == null ? null : Convert.ToBoolean(value);
        }

        #endregion

        private async Task LoadSlowQueryCapture(TabPage page)
        {
            DataTable dt = null;
            var notFound = false;
            await Task.Run(() =>
            {
                try
                {
                    dt = GetCompletedRPCBatch();
                }
                catch (Exception ex) when (ex.Message == NotFoundMessage)
                {
                    notFound = true;
                }
            });

            if (notFound || dt == null || dt.Rows.Count == 0)
            {
                ShowSlowQueryNotFound(page);
                return;
            }

            var text = Convert.ToString(dt.Rows[0]["text"]);
            dt.Columns.Remove("text");
            if (dt.Columns.Contains("InstanceID")) dt.Columns.Remove("InstanceID");
            if (dt.Columns.Contains("DatabaseID")) dt.Columns.Remove("DatabaseID");

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            page.Controls.Add(split);
            var editor = AddCodeEditor(split.Panel1, text);
            var dgv = NewGrid();
            dgv.AutoGenerateColumns = true;
            dgv.DataSource = PivotSingleRow(dt);
            dgv.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(dgv);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            TrySetSplitterDistance(split, page.ClientSize.Width * 2 / 3);
            page.ApplyTheme();
            editor.ApplyTheme(DBADashUser.SelectedTheme);
        }

        private Task LoadBatchText(TabPage page)
        {
            AddCodeEditor(page, Convert.ToString(Row["batch_text"].DBNullToNull()));
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        private Task LoadText(TabPage page)
        {
            AddCodeEditor(page, Convert.ToString(Row["text"].DBNullToNull()));
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        private Task LoadObjectExecution(TabPage page)
        {
            var context = Context.DeepCopy();
            context.ObjectID = Row.Row.Field<long>("DBADashObjectID");
            context.ObjectName = Row.Row.Field<string>("ObjectName");
            context.InstanceID = InstanceID;
            context.Type = SQLTreeItem.TreeType.StoredProcedure;
            var oes = new ObjectExecutionSummary { Dock = DockStyle.Fill, UseGlobalTime = false };
            page.Controls.Add(oes);
            oes.SetContext(context);
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        private Task LoadSessionHistory(TabPage page)
        {
            var rq = new RunningQueries
            {
                Dock = DockStyle.Fill,
                InstanceID = InstanceID,
                SessionID = SessionID,
                SnapshotDateFrom = HistoryFromUtc,
                SnapshotDateTo = SnapshotDateUtc.AddSeconds(1) // Include the current snapshot
            };
            page.Controls.Add(rq);
            rq.RefreshData();
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        private Task LoadBlockedSessions(TabPage page)
        {
            var rq = NewSnapshotControl();
            page.Controls.Add(rq);
            rq.RefreshData(); // Load the full snapshot (all sessions) before filtering
            rq.ShowSessionsBlockedBy((short)SessionID);
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        private Task LoadBlockingChain(TabPage page)
        {
            var rq = NewSnapshotControl();
            page.Controls.Add(rq);
            rq.RefreshData(); // Load the full snapshot (all sessions) before filtering
            rq.ShowBlockingChain((short)SessionID, Convert.ToString(Row["BlockingHierarchy"].DBNullToNull()));
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        /// <summary>A RunningQueries control loaded with the full snapshot (all sessions) this session belongs to.</summary>
        private RunningQueries NewSnapshotControl() => new()
        {
            Dock = DockStyle.Fill,
            InstanceID = InstanceID,
            SessionID = 0,
            SnapshotDateFrom = SnapshotDateUtc,
            SnapshotDateTo = SnapshotDateUtc
        };

        private Task LoadWaitResource(TabPage page)
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

            // Wait resource info from the running query row (top)
            var infoGrid = NewGrid();
            infoGrid.DataSource = PivotSingleRow(WaitResourceInfoTable());
            split.Panel1.Controls.Add(infoGrid);

            // Decipher results (bottom) - populated on demand
            var placeholder = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Click 'Decipher Wait Resource' to resolve the wait resource on the source instance."
            };
            split.Panel2.Controls.Add(placeholder);

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            var btnDecipher = new ToolStripButton("Decipher Wait Resource")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            btnDecipher.Click += async (_, _) => await DecipherWaitResource(split, btnDecipher);
            toolbar.Items.Add(btnDecipher);

            page.Controls.Add(split);
            page.Controls.Add(toolbar);
            infoGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            TrySetSplitterDistance(split, page.ClientSize.Height / 2);
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        /// <summary>Build a single-row table of the wait resource columns available on the running query row.</summary>
        private DataTable WaitResourceInfoTable()
        {
            var cols = new[]
            {
                "wait_type", "wait_time", "wait_resource", "wait_resource_type", "wait_database_id", "wait_db",
                "wait_file_id", "wait_file", "wait_page_id", "wait_object_id", "wait_object", "wait_index_id",
                "wait_hobt", "wait_hash", "wait_slot", "wait_is_compile", "page_type"
            };
            var dt = new DataTable();
            foreach (var c in cols)
            {
                if (Row.Row.Table.Columns.Contains(c)) dt.Columns.Add(c, typeof(string));
            }
            var r = dt.NewRow();
            foreach (DataColumn c in dt.Columns)
            {
                r[c.ColumnName] = Convert.ToString(Row[c.ColumnName].DBNullToNull());
            }
            dt.Rows.Add(r);
            return dt;
        }

        private async Task DecipherWaitResource(SplitContainer split, ToolStripButton button)
        {
            var waitResource = Convert.ToString(Row["wait_resource"].DBNullToNull());
            if (string.IsNullOrEmpty(waitResource)) return;

            var context = CommonData.GetDBADashContext(InstanceID);
            if (!context.CanMessage)
            {
                // No messaging - fall back to the manual script
                ShowDecipherScript(waitResource);
                return;
            }

            try
            {
                button.Enabled = false;
                var message = new DecipherWaitResourceMessage
                {
                    ConnectionID = context.ConnectionID,
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent,
                    WaitResource = waitResource
                };

                Task ProcessReply(ResponseMessage reply, Guid group, MessagingHelper.SetStatusDelegate setStatus)
                    => HandleDecipherReply(reply, split, waitResource, button, setStatus);

                tsStatus.InvokeSetStatus("Deciphering wait resource...", string.Empty, DashColors.Information);
                await MessagingHelper.SendMessageAndProcessReply((MessageBase)message, context, tsStatus, ProcessReply,
                    Guid.NewGuid());
            }
            catch (Exception ex)
            {
                button.Enabled = true; // Allow a retry
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private Task HandleDecipherReply(ResponseMessage reply, SplitContainer split, string waitResource,
            ToolStripButton button, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (reply.Type != ResponseMessage.ResponseTypes.Success)
            {
                setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                button.Enabled = true; // Allow a retry
                return Task.CompletedTask;
            }

            var table = reply.Data != null && reply.Data.Tables.Count > 0 ? reply.Data.Tables[0] : null;
            if (table == null)
            {
                setStatus("No result returned", string.Empty, DashColors.Warning);
                button.Enabled = true; // Allow a retry
                return Task.CompletedTask;
            }

            // The DMV isn't available on this SQL version - fall back to the manual (DBCC PAGE) script
            if (table.Columns.Contains("RequiresScript") && table.Rows.Count > 0 &&
                Convert.ToBoolean(table.Rows[0]["RequiresScript"]))
            {
                setStatus("sys.dm_db_page_info not available - showing script instead", string.Empty, DashColors.Warning);
                ShowDecipherScript(waitResource);
                button.Enabled = true; // Allow the script to be reopened
                return Task.CompletedTask;
            }

            var resultGrid = NewGrid();
            resultGrid.DataSource = table;
            split.Panel2.Controls.Clear();
            split.Panel2.Controls.Add(resultGrid);
            resultGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            resultGrid.ApplyTheme(DBADashUser.SelectedTheme);
            setStatus("Wait resource deciphered", string.Empty, DashColors.Green);
            button.ToolTipText = "Wait resource already deciphered."; // Stays disabled - results are shown below
            return Task.CompletedTask;
        }

        /// <summary>Show the manual decipher script (uses DBCC PAGE) for the user to run on the source instance.</summary>
        private void ShowDecipherScript(string waitResource)
        {
            var instance = Convert.ToString(Row["InstanceDisplayName"]);
            var sql = SqlStrings.GetDecipherWaitResource(waitResource, instance);
            Common.ShowCodeViewer(sql, "Decipher Wait Resource");
        }

        private async Task LoadWaits(TabPage page)
        {
            var loginTimeUtc = Row["login_time"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(Row["login_time"]).AppTimeZoneToUtc();

            DataTable dt = null;
            await Task.Run(() =>
                dt = RunningQueries.GetSessionWaits(InstanceID, (short)SessionID, SnapshotDateUtc, loginTimeUtc));

            var dgv = NewGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(SessionWaitColumns());
            dgv.DataSource = dt;

            var header = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 66,
                Padding = new Padding(6, 4, 6, 4),
                Text = BuildCurrentWaitsText()
            };

            page.Controls.Add(dgv);
            page.Controls.Add(header);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            page.ApplyTheme();
        }

        private Task LoadQueryStore(TabPage page)
        {
            var context = CommonData.GetDBADashContext(InstanceID);
            context.DatabaseName = Convert.ToString(Row["database_name"].DBNullToNull());
            var qs = new QueryStoreTopQueries
            {
                Dock = DockStyle.Fill,
                UseGlobalTime = false,
                QueryHash = Row.GetHexStringColumnAsByteArray("query_hash")
            };
            page.Controls.Add(qs);
            qs.SetContext(context);
            qs.RefreshData();
            page.ApplyTheme();
            return Task.CompletedTask;
        }

        #endregion

        #region Helpers

        private string BuildCurrentWaitsText()
        {
            var waitType = Convert.ToString(Row["wait_type"].DBNullToNull());
            var waitTime = Row["wait_time"].DBNullToNull();
            var taskWaits = Convert.ToString(Row["TaskWaits"].DBNullToNull());
            return "Current Wait Type: " + (string.IsNullOrEmpty(waitType) ? "(none)" : waitType) +
                   "    Wait Time (ms): " + (waitTime == null ? "0" : Convert.ToString(waitTime)) +
                   "\nTask Waits: " + (string.IsNullOrEmpty(taskWaits) ? "(none)" : taskWaits) +
                   "\n\nCumulative session wait stats (dm_exec_session_wait_stats):";
        }

        private static DataGridViewColumn[] SessionWaitColumns() => new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { HeaderText = "Wait Type", DataPropertyName = "WaitType" },
            new DataGridViewTextBoxColumn { HeaderText = "Waiting Tasks Count", DataPropertyName = "waiting_tasks_count" },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Wait Time %", DataPropertyName = "wait_pct",
                DefaultCellStyle = Common.DataGridViewPercentCellStyle
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Max Wait Time (ms)", DataPropertyName = "max_wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Signal Wait Time (ms)", DataPropertyName = "signal_wait_time_ms",
                DefaultCellStyle = Common.DataGridViewNumericCellStyle
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Signal Wait %", DataPropertyName = "signal_wait_pct",
                DefaultCellStyle = Common.DataGridViewPercentCellStyle
            }
        };

        // Ordered grouping of overview columns into logical sections.  Columns not listed here fall into an "Other" section;
        // columns listed in OverviewExcludedColumns are omitted (large text shown on their own tabs, internal keys, raw duplicates, etc.).
        private static readonly (string Title, string[] Columns)[] OverviewSections =
        {
            ("General", new[] { "InstanceDisplayName", "SnapshotDate" }),
            ("Query", new[] { "text", "batch_text", "cursor_text", "command", "status", "percent_complete" }),
            ("Resource Usage", new[] { "Duration", "Duration (ms)", "logical_reads", "reads", "writes", "granted_query_memory_kb", "tempdb_current_mb", "tempdb_allocations_mb", "transaction_duration_ms","total_elapsed_time", "cpu_time", "dop" }),
            ("Object", new[] { "database_name", "database_names", "SchemaName", "ObjectName", "object_name", "object_id" }),
            ("Blocking", new[] { "blocking_session_id", "IsRootBlocker", "BlockCount", "BlockCountRecursive", "BlockWaitTime", "BlockWaitTimeRecursive", "BlockingHierarchy" }),
            ("Waits", new[] { "wait_type", "wait_time", "TopSessionWaits", "TaskWaits", "wait_resource", "wait_resource_type", "wait_db", "wait_object", "wait_file", "page_type", "wait_database_id", "wait_file_id", "wait_page_id", "wait_object_id", "wait_index_id", "wait_hobt", "wait_hash", "wait_slot" }),   
            ("Transaction", new[] { "open_transaction_count", "transaction_isolation_level", "transaction_duration", "is_implicit_transaction", "is_query_store_on" }),
            ("Session / Client", new[] { "session_id", "login_name", "host_name", "program_name", "client_interface_name", "login_time", "start_time", "last_request_start_time", "last_request_end_time", "last_request_duration", "sleeping_session_idle_time", "sleeping_session_idle_time_sec", "workload_group", "resource_pool", "job_name", "context_info" }),
            ("Identifiers", new[] { "sql_handle", "plan_handle", "query_hash", "query_plan_hash",  "statement_start_offset", "statement_end_offset", })
        };

        private static readonly HashSet<string> OverviewExcludedColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "query_plan_text", "InstanceID", "job_id","BlockWaitTimeMs", "BlockWaitTimeRecursiveMs" , "context_info_bin","DBADashObjectID", "has_plan", "wait_is_compile", "database_id"
        };

        private const string OverviewSectionColumn = "__IsSection";

        /// <summary>
        /// Build the overview Attribute/Value table, grouped into <see cref="OverviewSections"/> and omitting null/empty values.
        /// The hidden <see cref="OverviewSectionColumn"/> flags section header rows for styling.
        /// </summary>
        private static DataTable BuildOverviewTable(DataTable source)
        {
            var table = new DataTable();
            table.Columns.Add("Attribute");
            table.Columns.Add("Value");
            table.Columns.Add(OverviewSectionColumn, typeof(bool));
            if (source.Rows.Count == 0) return table;

            var row = source.Rows[0];

            static bool HasValue(DataTable dt, DataRow r, string columnName)
            {
                if (!dt.Columns.Contains(columnName)) return false;
                var value = r[columnName];
                return value != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(value));
            }

            void AddSection(string title, IEnumerable<string> columnNames)
            {
                var populated = columnNames.Where(c => HasValue(source, row, c)).ToList();
                if (populated.Count == 0) return;

                var header = table.Rows.Add();
                header["Attribute"] = title;
                header["Value"] = string.Empty;
                header[OverviewSectionColumn] = true;

                foreach (var columnName in populated)
                {
                    table.Rows.Add(columnName.Titleize(), Convert.ToString(row[columnName]), false);
                }
            }

            var grouped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, columns) in OverviewSections)
            {
                foreach (var column in columns) grouped.Add(column);
            }

            foreach (var (title, columns) in OverviewSections)
            {
                AddSection(title, columns);
            }

            // Anything not explicitly placed or excluded still gets shown so nothing is silently lost.
            var otherColumns = source.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .Where(name => !grouped.Contains(name) && !OverviewExcludedColumns.Contains(name))
                .ToList();
            AddSection("Other", otherColumns);

            return table;
        }

        /// <summary>Size the Attribute column to its content and let the Value column fill the remaining width.</summary>
        private static void SizeOverviewGrid(DataGridView dgv)
        {
            if (dgv.Columns.Contains(OverviewSectionColumn))
            {
                dgv.Columns[OverviewSectionColumn].Visible = false;
            }
            if (dgv.Columns.Contains("Attribute"))
            {
                dgv.Columns["Attribute"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            if (dgv.Columns.Contains("Value"))
            {
                dgv.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        /// <summary>Style section header rows in the overview grid.</summary>
        private void StyleOverviewSections(DataGridView dgv)
        {
            var boldFont = insightBoldFont ??= new Font(Font, FontStyle.Bold);
            foreach (DataGridViewRow gridRow in dgv.Rows)
            {
                if (gridRow.DataBoundItem is not DataRowView drv) continue;
                if (drv[OverviewSectionColumn] is bool isSection && isSection)
                {
                    gridRow.DefaultCellStyle.Font = boldFont;
                    gridRow.DefaultCellStyle.BackColor = DashColors.BluePale;
                    gridRow.DefaultCellStyle.ForeColor = DashColors.Gray10;
                    gridRow.DefaultCellStyle.SelectionBackColor = DashColors.BluePale;
                    gridRow.DefaultCellStyle.SelectionForeColor = DashColors.Gray10;
                }
            }
        }

        /// <summary>Highlight the memory grant row in the overview grid when the grant is large.</summary>
        private void HighlightMemoryGrant(DataGridView dgv)
        {
            if (!HasLargeMemoryGrant()) return;

            // Red only when the large grant is actually causing other queries to queue for memory; otherwise yellow.
            var backColor = ResourceSemaphoreWaiterCount > 0 ? DashColors.RedPale : DashColors.YellowPale;
            var attribute = "granted_query_memory_kb".Titleize();

            foreach (DataGridViewRow gridRow in dgv.Rows)
            {
                if (gridRow.DataBoundItem is not DataRowView drv) continue;
                if (drv[OverviewSectionColumn] is bool isSection && isSection) continue;
                if (!string.Equals(Convert.ToString(drv["Attribute"]), attribute, StringComparison.Ordinal)) continue;

                gridRow.DefaultCellStyle.Font = insightBoldFont ??= new Font(Font, FontStyle.Bold);
                gridRow.DefaultCellStyle.BackColor = backColor;
                gridRow.DefaultCellStyle.ForeColor = DashColors.Gray10;
                gridRow.DefaultCellStyle.SelectionBackColor = backColor;
                gridRow.DefaultCellStyle.SelectionForeColor = DashColors.Gray10;
                break;
            }
        }

        /// <summary>Pivot a single-row DataTable into an Attribute/Value table (independent of DataGridView row materialization).</summary>
        private static DataTable PivotSingleRow(DataTable source)
        {
            var pivot = new DataTable();
            pivot.Columns.Add("Attribute");
            pivot.Columns.Add("Value");
            if (source.Rows.Count == 0) return pivot;
            var row = source.Rows[0];
            foreach (DataColumn col in source.Columns)
            {
                pivot.Rows.Add(col.ColumnName.Titleize(),
                    row[col] == DBNull.Value ? string.Empty : Convert.ToString(row[col]));
            }
            return pivot;
        }

        private static void TrySetSplitterDistance(SplitContainer split, int distance)
        {
            try
            {
                split.SplitterDistance = distance;
            }
            catch (ArgumentException)
            {
                // Ignore - panel min sizes / control not yet sized. Default splitter position is fine.
            }
            catch (InvalidOperationException)
            {
                // Ignore - as above.
            }
        }

        private static DBADashDataGridView NewGrid() => new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoGenerateColumns = true
        };

        private DataTable SingleRowClone()
        {
            var dt = Row.Row.Table.Clone();
            dt.ImportRow(Row.Row);
            return dt;
        }

        private static CodeEditor AddCodeEditor(Control parent, string text)
        {
            var host = new ElementHost { Dock = DockStyle.Fill };
            var editor = new CodeEditor { IsReadOnly = true, Text = text ?? string.Empty };
            host.Child = editor;
            parent.Controls.Add(host);
            return editor;
        }

        private const string NotFoundMessage = "Unable to find completed event that was running at this time.";
        private bool IsSleeping => Convert.ToString(Row["status"]) == "sleeping";

        /// <summary>Get the RPC / Batch completed event associated with this running query (slow query capture).</summary>
        private DataTable GetCompletedRPCBatch()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("SlowQueriesDetail_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", InstanceID.ToString());
            cmd.Parameters.AddWithValue("SessionID", SessionID);
            cmd.Parameters.AddWithValue("Sort", "timestamp");
            cmd.Parameters.AddWithValue("Top", 1);

            if (IsSleeping)
            {
                cmd.Parameters.AddWithValue("FromDate", StartTimeUtc);
                cmd.Parameters.AddWithValue("ToDate", SnapshotDateUtc);
                cmd.Parameters.AddWithValue("SortDesc", true);
            }
            else
            {
                cmd.Parameters.Add("FromDate", SqlDbType.DateTime2).Value = SnapshotDateUtc;
                cmd.Parameters.Add("ToDate", SqlDbType.DateTime2).Value = SnapshotDateUtc.AddDays(2); // For performance
                cmd.Parameters.AddWithValue("SortDesc", false);
            }

            var dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count != 1)
            {
                throw new Exception(NotFoundMessage);
            }

            var row = dt.Rows[0];
            if ((DateTime)row["start_time"] > SnapshotDateUtc && !IsSleeping)
            {
                throw new Exception(NotFoundMessage);
            }

            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        #endregion

        #region Plan buttons

        private async void TsPlan_Click(object sender, EventArgs e)
        {
            try
            {
                tsPlan.Enabled = false;
                await QueryPlanActions.Execute(Row, tsStatus);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
            finally
            {
                SetupPlanButton(); // Action may have changed (e.g. Collect -> View once the plan is captured)
            }
        }

        #endregion
    }
}
