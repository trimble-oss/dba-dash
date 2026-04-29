using DBADash;
using DBADash.Messaging;
using DBADashGUI.AgentJobs;
using DBADashGUI.Changes;
using DBADashGUI.Checks;
using DBADashGUI.CustomReports;
using DBADashGUI.DBADashAlerts;
using DBADashGUI.Options_Menu;
using DBADashGUI.Performance;
using DBADashGUI.Properties;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Humanizer;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Version = System.Version;

namespace DBADashGUI
{
    public partial class Main : Form, IMessageFilter, IThemedControl
    {
        public class InstanceSelectedEventArgs : EventArgs
        {
            public int InstanceID = -1;
            public string Instance;
            public Tabs? Tab;
            public string Database;
            public bool SearchFromRoot = false;
        }

        // Extracted helper to build the instance tree from instance/pool DataTables
        private SQLTreeItem BuildInstanceTree(DataTable instances, DataTable pools, CustomReports.CustomReports reports, string groupByTag)
        {
            var root = new SQLTreeItem(Common.RepositoryDBConnection.Name, SQLTreeItem.TreeType.DBADashRoot);
            AddRootRefreshContextMenu(root);
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            var hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
            var checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
            var storage = new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage);
            var jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs);

            root.Nodes.AddRange(new TreeNode[] { changes, checks, hadr, storage, jobs });

            root.AddReportsFolder(reports?.RootLevelReports);

            var parentNode = root;

            var poolTable = pools ?? new DataTable();

            SQLTreeItem AzureNode = null;
            var currentTagGroup = string.Empty;
            if (instances != null)
            {
                foreach (DataRow row in instances.Rows)
                {
                    var instance = (string)row["Instance"];
                    var displayName = (string)row["InstanceDisplayName"];
                    var instanceID = (int)row["InstanceID"];
                    var showInSummary = (bool)row["ShowInSummary"];
                    var tagGroup = Convert.ToString(row["TagGroup"]);

                    if (currentTagGroup != tagGroup && !string.IsNullOrEmpty(tagGroup))
                    {
                        var folderText = string.IsNullOrEmpty(groupByTag) ? tagGroup : groupByTag + ": " + tagGroup;
                        parentNode = new SQLTreeItem(folderText, SQLTreeItem.TreeType.InstanceFolder);
                        var changesGrp = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
                        var hadrGrp = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
                        var checksGrp = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
                        var storageGrp = new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage);
                        var jobsGrp = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs);
                        parentNode.Nodes.AddRange(new TreeNode[] { changesGrp, checksGrp, hadrGrp, storageGrp, jobsGrp });
                        if (IsAzureOnly)
                        {
                            parentNode.Nodes.Remove(hadrGrp);
                        }
                        parentNode.AddReportsFolder(reports?.RootLevelReports);
                        root.Nodes.Add(parentNode);
                        currentTagGroup = tagGroup;
                        AzureNode = null;
                    }

                    DatabaseEngineEdition edition;
                    try { edition = (DatabaseEngineEdition)Convert.ToInt32(row["EngineEdition"]); } catch { edition = DatabaseEngineEdition.Unknown; }

                    if ((bool)row["IsAzure"])
                    {
                        var db = (string)row["AzureDBName"];
                        if (AzureNode == null || AzureNode.InstanceName != instance)
                        {
                            AzureNode = new SQLTreeItem(instance, SQLTreeItem.TreeType.AzureInstance)
                            { EngineEdition = edition };
                            parentNode.Nodes.Add(AzureNode);
                            AzureNode.Nodes.AddRange(new TreeNode[] {
                                new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration),
                                new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks),
                                new SQLTreeItem("Tags", SQLTreeItem.TreeType.Tags),
                                new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage)
                            });

                            AzureNode.AddReportsFolder(reports?.InstanceLevelReports);
                            var poolNodes = poolTable.Rows.Cast<DataRow>()
                                .Where(r => (string)r["InstanceGroupName"] == instance && r["elastic_pool_name"] != DBNull.Value)
                                .Select(r => (string)r["elastic_pool_name"]).Distinct().OrderBy(r => r)
                                .Select(poolName => (TreeNode)new SQLTreeItem(poolName, SQLTreeItem.TreeType.ElasticPool) { ElasticPoolName = poolName }).ToArray();
                            AzureNode.Nodes.AddRange(poolNodes);
                        }

                        var poolName = (string)row["elastic_pool_name"].DBNullToNull();
                        var azureDBNode = new SQLTreeItem(db, SQLTreeItem.TreeType.AzureDatabase)
                        {
                            DatabaseID = (int)row["AzureDatabaseID"],
                            InstanceID = instanceID,
                            DatabaseName = db,
                            ElasticPoolName = poolName,
                            EngineEdition = edition,
                        };
                        azureDBNode.Nodes.AddRange(new TreeNode[] {
                            new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration),
                            new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks)
                        });
                        AzureNode.Nodes.Add(azureDBNode);
                        azureDBNode.AddDatabaseFolders();
                        azureDBNode.AddInstanceActionsContextMenu();
                    }
                    else
                    {
                        var n = new SQLTreeItem(displayName, SQLTreeItem.TreeType.Instance)
                        {
                            InstanceID = instanceID,
                            EngineEdition = edition,
                            IsVisibleInSummary = showInSummary
                        };
                        n.AddDummyNode();
                        n.AddInstanceActionsContextMenu();
                        parentNode.Nodes.Add(n);
                    }
                }
            }

            if (ShowCounts)
            {
                foreach (var n in root.Nodes.Cast<SQLTreeItem>().Where(t => t.Type == SQLTreeItem.TreeType.InstanceFolder))
                {
                    n.Text += "    {" + n.Nodes.Cast<SQLTreeItem>().Count(child => child.Type is SQLTreeItem.TreeType.Instance or SQLTreeItem.TreeType.AzureInstance) + "}";
                }
            }

            IsAzureOnly = root.InstanceIDs.Count == root.AzureInstanceIDs.Count;
            if (IsAzureOnly) root.Nodes.Remove(hadr);
            if (DBADashUser.IsAdmin) root.Nodes.Add(new SQLTreeItem("Recycle Bin", SQLTreeItem.TreeType.RecycleBin));

            return root;
        }

        public enum Tabs
        {
            Performance,
            PerformanceSummary,
            ObjectExecutionSummary,
            RunningQueries,
            Metrics,
            SlowQueries,
            Waits,
            Memory,
            Backups,
            LogShipping,
            Drives,
            Jobs,
            DBADashErrorLog,
            AG,
            LastGood,
            CollectionDates,
            SQLAgentAlerts,
            Files,
            CustomChecks,
            Mirroring,
            AzureSummary,
            QS,
            IdentityColumns,
            DBOptions,
            OfflineInstances,
            DBSpace,
            SnapshotSummary,
            DBConfiguration,
            TopQueries,
            QueryStoreForcedPlans,
            InstanceMetadata,
            Alerts,
            RunningJobs,
            JobTimeline,
            JobStats,
            Configuration,
            TraceFlags,
            Hardware,
            SQLPatching,
            Drivers,
            TempDB,
            ResourceGovernor,
            ServerServices,
            AzureServiceObjectives,
            AzureDBResourceGovernance,
            Tags,
            TableSize,
            TuningRecommendations,
            PoolsAndGroups,
            DatabaseExtendedProperties
        }

        private static readonly List<Main.Tabs> InstanceOnlyTabs = new() { Main.Tabs.PerformanceSummary, Tabs.Metrics, Tabs.Waits, Tabs.Memory, Tabs.RunningQueries };

        public static Main MainFormInstance { get; private set; }

        private readonly CommandLineOptions commandLine;
        private readonly List<int> commandLineTags = new();
        private TabPage[] AllTabs;

        // Track which theme has been applied to each TabPage to avoid
        // re-theming tabs unnecessarily (costly and can cause flicker).
        private readonly Dictionary<TabPage, ThemeType> _tabThemeApplied = new();

        private CustomReports.CustomReports customReports = new();
        private readonly Dictionary<ProcedureExecutionMessage.CommunityProcs, TabPage> CommunityToolsTabPages = new Dictionary<ProcedureExecutionMessage.CommunityProcs, TabPage>();
        private Dictionary<string, TabPage> CustomToolsTabs = new Dictionary<string, TabPage>();
        private TabPage tabBlitzIndex => CommunityToolsTabPages[ProcedureExecutionMessage.CommunityProcs.sp_BlitzIndex];
        private TabPage tabDBADashAlerts;
        private NotifyIcon notifyIcon;
        private TabPage tabOfflineInstances;
        private TabPage tabJobInfo;
        private TabPage tabCloudMetadata;
        private TabPage tabTuningRecommendations;
        private TabPage tabPoolsAndGroups;
        private TabPage tabPerformance;
        private TabPage tabDatabaseExtendedProperties;

        public Main(CommandLineOptions opts)
        {
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => Application.RemoveMessageFilter(this);
            InitializeComponent();
            // Ensure cancel button is hidden until a cancellable connection attempt is in progress
            bttnCancel.InvokeSetEnabled(false);
            WindowState = FormWindowState.Maximized;
            commandLine = opts;
            Disposed += OnDispose;
            AddTabs();
            // Apply currently selected theme immediately so controls are
            // themed before the form is shown (prevents a short white flash).
            try { this.ApplyTheme(); } catch { }
            MainFormInstance = this;
            SetSingleInstance(Settings.Default.ChildFormSingleInstance);
        }

        private void AddTabs()
        {
            foreach (var proc in Enum.GetValues<ProcedureExecutionMessage.CommunityProcs>())
            {
                var tab = GetCommunityToolsTabPage(proc);
                if (tab == null) continue;
                CommunityToolsTabPages.Add(proc, tab);
                tabs.TabPages.Add(tab);
            }
            tabDBADashAlerts = new TabPage("Alerts") { Name = Tabs.Alerts.TabName() };
            var alertsCsControl = new ActiveAlerts() { Dock = DockStyle.Fill };
            alertsCsControl.Instance_Selected += Instance_Selected;
            tabDBADashAlerts.Controls.Add(alertsCsControl);

            tabOfflineInstances = new TabPage("Offline Instances") { Name = Tabs.OfflineInstances.TabName() };
            var offlineInstancesControl = new OfflineInstances() { Dock = DockStyle.Fill };
            tabOfflineInstances.Controls.Add(offlineInstancesControl);

            tabJobInfo = new TabPage("Job Info");
            tabJobInfo.Controls.Add(new JobInfo() { Dock = DockStyle.Fill });

            tabCloudMetadata = new TabPage("Instance Metadata") { Name = Tabs.InstanceMetadata.TabName() };
            tabCloudMetadata.Controls.Add(new InstanceMetadata() { Dock = DockStyle.Fill });

            tabTuningRecommendations = new TabPage("Tuning Recommendations") { Name = Tabs.TuningRecommendations.TabName() };
            tabTuningRecommendations.Controls.Add(new TuningRecommendationsReport() { Dock = DockStyle.Fill });

            tabPoolsAndGroups = new TabPage("Resource Governor") { Name = Tabs.PoolsAndGroups.TabName() };

            tabPoolsAndGroups.Controls.Add(new ResourceGovernorPerformance { Dock = DockStyle.Fill });

            tabPerformance = new TabPage("Performance") { Name = Tabs.Performance.TabName() };
            var perfView = new CustomReportView()
            {
                Dock = DockStyle.Fill,
                Report = PerformanceReport.Instance,
                PreventReportOverwrite = true
            };
            tabPerformance.Controls.Add(perfView);

            tabDatabaseExtendedProperties = new TabPage("Extended Properties") { Name = Tabs.DatabaseExtendedProperties.TabName() };
            var extPropView = new CustomReportView()
            {
                Dock = DockStyle.Fill,
                Report = DatabaseExtendedPropertiesReport.Instance,
                PreventReportOverwrite = true
            };
            tabDatabaseExtendedProperties.Controls.Add(extPropView);
        }

        public TabPage GetCommunityToolsTabPage(ProcedureExecutionMessage.CommunityProcs proc)
        {
            var tab = new TabPage(proc.ToString());
            var report = CommunityTools.CommunityTools.CommunityToolsList.FirstOrDefault(report => report.ProcedureName == proc.ToString());
            if (report == null) return null;
            tab.Controls.Add(new CustomReportView() { Dock = DockStyle.Fill, Report = report, PreventReportOverwrite = true });
            return tab;
        }

        private void OnDispose(object sender, EventArgs e)
        {
            Common.TryDeleteTempFiles();
        }

        private long currentObjectID;
        private int currentPage = 1;
        private int currentPageSize = 100;
        private readonly DiffControl diffSchemaSnapshot = new();
        private bool suppressLoadTab;

        // Win32 message for enabling/disabling redraw to avoid flicker when
        // making bulk updates to controls (eg. clearing and re-adding TabPages)
        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private static void SuspendDrawing(Control c)
        {
            if (c.IsHandleCreated)
            {
                SendMessage(c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static void ResumeDrawing(Control c)
        {
            if (c.IsHandleCreated)
            {
                SendMessage(c.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                c.Invalidate(true);
                c.Refresh();
            }
        }

        private bool CurrentTabSupportsDayOfWeekFilter =>
            (new List<TabPage>()
                { tabPerformanceSummary, tabPerformance, tabMetrics, tabObjectExecutionSummary, tabWaits })
            .Contains(tabs.SelectedTab);

        private bool CurrentTabSupportsTimeOfDayFilter =>
            (new List<TabPage>()
                { tabPerformanceSummary, tabPerformance, tabMetrics, tabObjectExecutionSummary, tabWaits })
            .Contains(tabs.SelectedTab);

        private bool GlobalTimeIsVisible =>
            (new List<TabPage>()
            {
                tabPerformanceSummary, tabPerformance, tabSlowQueries, tabAzureDB, tabAzureSummary, tabMetrics,
                tabObjectExecutionSummary, tabWaits, tabRunningQueries, tabMemory, tabJobStats, tabJobTimeline, tabDrivePerformance, tabTopQueries, tabOfflineInstances, tabPoolsAndGroups
            }).Contains(tabs.SelectedTab) || (tabs.SelectedTab == tabCustomReport && ((SQLTreeItem)tv1.SelectedNode).Report.TimeFilterSupported);

        private bool IsAzureOnly;
        private bool ShowCounts;
        private string GroupByTag = string.Empty;
        private readonly List<TreeContext> VisitedNodes = new();
        private bool suppressSaveContext;
        private Font tvBoldFont;

        /// <summary>
        ///  For PreFilterMessage.  Mouse down button.
        /// </summary>
        private const int WM_XBUTTONDOWN = 0x020B;

        /// <summary>
        /// For PreFilterMessage. Mouse back button
        /// </summary>
        private const int MK_XBUTTON1 = 0x0020;

        /// <summary>
        /// For PreFilterMessage. Mouse forward button
        /// </summary>
        /// private const int MK_XBUTTON2 = 0x0040;

        private string SearchString
        {
            get
            {
                var searchString = string.Empty;
                if (txtSearch.Text.Trim().Length > 0)
                {
                    searchString = "%" + txtSearch.Text.Trim() + "%";
                }

                return searchString;
            }
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            tvBoldFont = new Font(tv1.Font, FontStyle.Bold);
            AllTabs = tabs.TabPages.OfType<TabPage>().ToArray();
            await CommonShared.CheckForIncompleteUpgrade();
            if (Upgrade.IsUpgradeIncomplete) return;
            if (Properties.Settings.Default.SettingsUpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            mnuTags.Visible = !commandLine.NoTagMenu;
            lblVersion.Text = "Version: " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            tabs.TabPages.Clear();
            tabs.TabPages.Add(tabDBADash);

            dbOptions1.SummaryMode = true;
            splitSchemaSnapshot.Panel1.Controls.Add(diffSchemaSnapshot);
            diffSchemaSnapshot.Dock = DockStyle.Fill;

            LoadRepositoryConnections();
            if (repositories.Count == 0) // We don't have a connection to the repository DB yet
            {
                if (File.Exists(Properties.Resources
                        .ServiceConfigToolName)) // The service configuration tool exists (Not the GUI only package).  Give user the option to configure the service or connect to an existing repository.
                {
                    using var frm = new ConnectionOptions();
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK && frm.RequestConfigureService)
                    {
                        Common.ConfigureService();
                        return;
                    }
                    else if (frm.DialogResult == DialogResult.OK)
                    {
                        // Prompt the user to connect to an existing DBA Dash repository DB.
                        await AddConnectionAsync();
                    }
                }
            }

            try
            {
                var conn = repositories.GetDefaultConnection();
                RunSetConnection(conn);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }

            this.ApplyTheme();
            CheckTheme();
            if (splitMain.SplitterDistance > 400)
            {
                splitMain.SplitterDistance = 400;
            }

            customReportView1.ReportNameChanged += CustomReport_ReportNameChanged;
            InitializeNotifyIcon();
            desktopNotificationsToolStripMenuItem.Checked = Properties.Settings.Default.DesktopNotificationsEnabled;
            _ = Task.Run(GetNewAlertsLoop);
        }

        private void CustomReport_ReportNameChanged(object sender, EventArgs e)
        {
            UpdateReportNameInTreeView(tv1.Nodes);
            tabCustomReport.Text = ((SQLTreeItem)tv1.SelectedNode).Report.ReportName;
        }

        private static void UpdateReportNameInTreeView(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node is not SQLTreeItem sqlNode) continue;
                if (sqlNode.Type == SQLTreeItem.TreeType.CustomReport)
                {
                    sqlNode.Text = sqlNode.Report.ReportName;
                }

                // Recursively update child nodes if they exist
                if (sqlNode.Nodes.Count > 0)
                {
                    UpdateReportNameInTreeView(sqlNode.Nodes);
                }
            }
        }

        private void RunSetConnection(RepositoryConnection conn)
        {
            // Prepare cancellation token for this connection attempt and then
            // call SetConnection on the UI thread. SetConnection will perform
            // network I/O using async/await and any costly CPU-bound work will
            // be offloaded as needed. This keeps ordering and avoids cross-
            // thread races introduced by Task.Run + UI marshals.
            try { _setConnectionCts?.Cancel(); _setConnectionCts?.Dispose(); } catch { }
            _setConnectionCts = new CancellationTokenSource();
            var token = _setConnectionCts.Token;

            // Remember last attempt for Retry
            _lastConnectionAttempt = conn;

            // Clear any transient cache from previous runs
            _customReportsCache = null;

            // Call SetConnection on UI thread so its UI updates are single-threaded
            // and consistent. SetConnection will use `await` for I/O and offload
            // heavy work to background tasks explicitly where required.
            // `SetConnection` is async; fire-and-forget but ensure any fault is
            // observed to avoid unobserved task exceptions.  Use the ObserveFault
            // extension to attach a continuation that will marshal any exception
            // back to the UI thread for display.
            SetConnectionAsync(conn, token).ObserveFault(ex =>
            {
                try { this.BeginInvoke((Action)(() => CommonShared.ShowExceptionDialog(ex))); } catch { }
            });
        }

        public async Task SetConnectionAsync(RepositoryConnection connection, CancellationToken token = default)
        {
            // If caller passed a null connection, make sure we bring the UI to a
            // consistent disconnected/error state rather than silently returning.
            lblRepositoryDB.Text = connection?.Name ?? "No Connection";
            if (connection == null)
            {
                try { SetConnectionError("No repository connection specified."); } catch { }
                try { StopConnectingAnimation(); } catch { }
                return;
            }

            // Ensure we have a token to use and record it if caller didn't pass one
            if (token == default)
            {
                try { _setConnectionCts?.Cancel(); _setConnectionCts?.Dispose(); } catch { }
                _setConnectionCts = new CancellationTokenSource();
                token = _setConnectionCts.Token;
            }
            Common.SetConnectionString(null); // Clear current connection during transition
            // Start UI connection state (on UI thread)
            SetConnectionState(false);
            try { StartConnectingAnimation(); } catch { }

            // remember last attempt for retry
            _lastConnectionAttempt = connection;

            if (token.IsCancellationRequested)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Connection attempt cancelled."); } catch { }
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
                return;
            }

            // 1) Try opening SQL connection (async). This uses await so it won't
            // block the UI thread and still executes in the UI synchronization
            // context (keeps ordering simple).
            try
            {
                await using var cn = new SqlConnection(connection.ConnectionString);
                await cn.OpenAsync(token);
            }
            catch (OperationCanceledException)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Connection attempt cancelled."); } catch { }
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
                return;
            }
            catch (Exception ex)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Error connecting to repository database: " + ex.Message, ex); } catch { }
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
                return;
            }

            // 2) Version check (may display dialogs) - keep on UI thread for simplicity
            try
            {
                await CheckVersion(connection.ConnectionString);
            }
            catch (OperationCanceledException)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Version check cancelled."); } catch { }
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
                return;
            }
            catch (Exception ex)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Error checking repository database version: " + ex.Message, ex); } catch { }
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
                return;
            }

            try
            {
                // 3) Perform configuration and prepare data for UI. Offload heavy
                // data pulls to background thread, but build node objects there and
                // then apply on UI thread in one shot to avoid flicker.
                Common.SetConnectionString(connection);

                await DBADashUser.GetUserAsync(token);

                try
                {
                    GetCommandLineTags();
                }
                catch (Exception ex)
                {
                    Common.ShowExceptionDialog(ex, "Command line tags error");
                }

                // Ensure any saved tree layout and command-line tags are applied
                GetTreeLayout();

                // Capture UI-dependent values before going to background thread
                var searchStringLocal = SearchString;
                var groupByTagLocal = GroupByTag;

                // Offload data reads to background thread and capture snapshots.
                // We populate CommonData.Instances in the background and capture
                // pools/customReports for use on the UI thread. Avoid duplicating
                // the instances DataTable here.
                DataTable poolsSnapshot = null;

                Config.RefreshConfig();
                SetTheme(DBADashUser.SelectedTheme, false);
                _customReportsCache = CustomReports.CustomReports.GetCustomReports();

                // Only apply command-line tag filter if one was actually provided.
                var commandLineTagIDs = string.Empty;
                if (!string.IsNullOrEmpty(commandLine.TagFilters))
                {
                    var tags = DBADashTag.GetTags(Common.ConnectionString, commandLine.TagFilters).ToList();
                    commandLineTagIDs = string.Join(",", tags.Select(t => t.TagID));
                }

                await CommonData.UpdateInstancesListAsync(tagIDs: commandLineTagIDs, searchString: searchStringLocal, groupByTag: groupByTagLocal, token: token);
                poolsSnapshot = await CommonData.GetElasticPoolsAsync(token);

                // Apply UI updates on UI thread in one atomic block
                if (token == _setConnectionCts?.Token)
                {
                    // Marshal the UI updates to the UI thread using BeginInvoke so
                    // the background Task.Run does not directly access controls.
                    this.BeginInvoke((Action)(() =>
                    {
                        // Re-validate that this delegate still belongs to the current connection attempt.
                        if (token != _setConnectionCts?.Token)
                        {
                            return;
                        }
                        try
                        {
                            // Update cached reports reference
                            if (_customReportsCache != null)
                            {
                                customReports = _customReportsCache;
                            }

                            // Build tag menu using any command-line tag filters, else use current selections
                            var selectedForMenu = string.IsNullOrEmpty(commandLine.TagFilters)
                                ? SelectedTags()
                                : DBADashTag.GetTags(Common.ConnectionString, commandLine.TagFilters).Select(t => t.TagID).ToList();
                            BuildTagMenu(selectedForMenu);

                            // Build tree from authoritative CommonData.Instances and the
                            // pools snapshot captured earlier.
                            VisitedNodes.Clear();
                            tsBack.Enabled = false;
                            tv1.Nodes.Clear();
                            var root = BuildInstanceTree(CommonData.Instances, poolsSnapshot, customReports, groupByTagLocal);
                            tv1.Nodes.Add(root);
                            root.Expand();
                            tv1.SelectedNode = root;

                            AddTimeZoneMenus();
                            SetConnectionState(true);
                            repoSettingsToolStripMenuItem.Enabled = DBADashUser.IsAdmin;
                            ThemeExtensions.CellToolTipMaxLength = Config.CellToolTipMaxLength;
                            StopConnectingAnimation();
                        }
                        catch (Exception ex)
                        {
                            Common.ShowExceptionDialog(ex, "Error updating the UI for the new connection " + ex.Message);
                        }
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Connection cancelled."); } catch { }
            }
            catch (Exception ex)
            {
                try { if (token == _setConnectionCts?.Token) SetConnectionError("Error loading data: " + ex.Message, ex); } catch { }
            }
            finally
            {
                try { if (token == _setConnectionCts?.Token) StopConnectingAnimation(); } catch { }
            }
        }

        private void SetConnectionState(bool isGood)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => SetConnectionState(isGood))); } catch { }
                return;
            }
            picConnectPct.Image = Properties.Resources.db_connection;
            lblConnecting.Text = "Connecting to repository database";
            lblConnectionInfo.Text = "Waiting for connection...";
            txtConnectionError.Visible = false;
            txtConnectionError.Text = "";
            bttnSearch.Enabled = isGood;
            txtSearch.Enabled = isGood;
            tsTree.Enabled = isGood;
            tsAlert.Visible = isGood;
            tsAlert.Enabled = isGood;
            tsAlert.Text = string.Empty;
            tsAlert.Image = null;
            tsAlert.Tag = null;
            tsAlert.ToolTipText = string.Empty;
            optionsToolStripMenuItem.Enabled = isGood;
            diffToolStripMenuItem.Enabled = isGood;
            if (!isGood)
            {
                tv1.Nodes.Clear();
                tabs.TabPages.Clear();
                tabs.TabPages.Add(tabConnecting);
            }
        }

        private void SetConnectionError(string message, Exception ex = null)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => SetConnectionError(message, ex))); } catch { }
                return;
            }
            // Ensure any partially-applied global connection state is cleared
            try { Common.SetConnectionString(null); } catch { }
            SetConnectionState(false);
            lblConnecting.Text = "Connection Failed";
            lblConnectionInfo.Text = message;
            txtConnectionError.Text = ex?.ToString();
            txtConnectionError.Visible = ex != null;
            picConnectPct.Image = Properties.Resources.db_connection_error;
            // Enable retry so user can attempt to connect again
            bttnRetry.InvokeSetEnabled(true);
            bttnCancel.InvokeSetEnabled(false);
        }

        private void StartConnectingAnimation()
        {
            StopConnectingAnimation();
            picConnectPct.Image = Properties.Resources.database_connect_animated;
            _connectingDots = 0;
            lblConnectionInfo.Text = "Waiting for connection";
            _connectingTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _connectingTimer.Tick += (s, e) =>
            {
                _connectingDots = (_connectingDots + 1) % 20;
                lblConnectionInfo.Text = "Waiting for connection" + new string('.', _connectingDots);
            };
            bttnCancel.InvokeSetEnabled(true);
            bttnRetry.InvokeSetEnabled(false);
            _connectingTimer.Start();
        }

        private void StopConnectingAnimation()
        {
            try
            {
                if (_connectingTimer != null)
                {
                    _connectingTimer.Stop();
                    _connectingTimer.Dispose();
                    _connectingTimer = null;
                }
            }
            catch { }
            bttnCancel.InvokeSetEnabled(false);
        }

        private static void WaitForDBUpgrade(string connectionString, string caption = "Upgrade in progress", string heading = "Repository database upgrade is in progress.  ", string text = "Please wait for this to complete.  If you continue, the application might be unstable.  \n\nThis dialog will close automatically...", bool allowContinue = true)
        {
            var closeButton = new TaskDialogButton("Close");
            var page = new TaskDialogPage
            {
                Caption = caption,
                Heading = heading,
                Text = text,
                Icon = TaskDialogIcon.Information,
                Buttons = new TaskDialogButtonCollection() { closeButton },
                SizeToContent = true,
                ProgressBar = new TaskDialogProgressBar()
                {
                    State = TaskDialogProgressBarState.Marquee,
                },
                DefaultButton = closeButton,
                Expander = new TaskDialogExpander()
                {
                    Text = "If this process takes longer than expected, click the 'View Service Log' button on the service config tool to check the logs.",
                    CollapsedButtonText = "Show Help",
                    ExpandedButtonText = "Hide Help",
                    Position = TaskDialogExpanderPosition.AfterFootnote
                },
            };
            if (allowContinue)
            {
                page.Buttons.Add(TaskDialogButton.Continue);
                page.DefaultButton = TaskDialogButton.Continue;
            }

            var tmr = new System.Windows.Forms.Timer() { Interval = 1000, Enabled = true };
            tmr.Tick += (s, e) =>
            {
                try
                {
                    var dbVersion = DBValidations.GetDBVersion(connectionString);
                    if (dbVersion.DeployInProgress || dbVersion.Version == Version.Parse("0.0.0.0")) return;
                }
                catch
                {
                    return; // Timer will retry on next tick (1000ms interval)
                }
                tmr.Stop();
                tmr.Dispose();
                page.BoundDialog?.Close();
            };

            if (TaskDialog.ShowDialog(page) == closeButton)
            {
                Application.Exit();
                throw new OperationCanceledException();
            }
        }

        private async Task CheckVersion(string connectionString)
        {
            var dbVersion = DBValidations.GetDBVersion(connectionString);
            var appVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

            if (dbVersion.DeployInProgress)
            {
                WaitForDBUpgrade(connectionString);
                dbVersion = DBValidations.GetDBVersion(connectionString);
            }
            if (dbVersion.Version == Version.Parse("0.0.0.0"))
            {
                WaitForDBUpgrade(connectionString, "Database Deployment", "Waiting for first time database deployment to complete. ", "Please wait for the deployment to complete.  \nIf this doesn't occur within a few minutes, check that the service is started.  Check the log file for errors.", false);
                dbVersion = DBValidations.GetDBVersion(connectionString);
            }
            var compare = (new Version(appVersion.Major, appVersion.Minor)).CompareTo(new Version(dbVersion.Version.Major, dbVersion.Version.Minor));
            if (compare < 0)
            {
                var promptUpgrade = MessageBox.Show($"The version of this GUI app ({appVersion.Major}.{appVersion.Minor}) is OLDER than the repository database. Please upgrade to version {dbVersion.Version.Major}.{dbVersion.Version.Minor}{Environment.NewLine}Would you like to run the upgrade script now?", "Upgrade GUI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (promptUpgrade == DialogResult.Yes)
                {
                    var tag = dbVersion.Version.ToString(3);
                    await Upgrade.UpgradeDBADashAsync(tag, true);
                    Application.Exit();
                    throw new OperationCanceledException();
                }
                else
                {
                    MessageBox.Show("The GUI might be unstable as it's not designed to run against this version of the repository database.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (compare > 0)
            {
                MessageBox.Show($"The version of this GUI app ({appVersion.Major}.{appVersion.Minor}) is NEWER than the repository database ({dbVersion.Version.Major}.{dbVersion.Version.Minor}). You might experience issues using the GUI.  Please upgrade the repository database.", "Upgrade Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GetCommandLineTags()
        {
            if (string.IsNullOrEmpty(commandLine.TagFilters)) return;
            foreach (var t in DBADashTag.GetTags(Common.ConnectionString, commandLine.TagFilters))
            {
                commandLineTags.Add(t.TagID);
            }
        }

        private async void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                await LoadSelectedTabAsync();
            }
            catch (Exception ex)
            {
                Common.ShowExceptionDialog(ex, "Error loading tab: " + ex.Message);
            }
        }

        private async Task LoadSelectedTabAsync()
        {
            DisableAutoRefresh();
            setAutoRefreshToolStripMenuItem.Enabled = IsAutoRefreshApplicable;

            jobTimeline1.IsActive = false; // Don't re-draw timeline on resize unless control is active
            if (suppressLoadTab)
            {
                return;
            }
            var sel = tabs.SelectedTab;
            if (sel != null)
            {
                if (!_tabThemeApplied.TryGetValue(sel, out var applied) || applied != DBADashUser.SelectedTheme.ThemeIdentifier)
                {
                    sel.ApplyTheme();
                    _tabThemeApplied[sel] = DBADashUser.SelectedTheme.ThemeIdentifier;
                }
            }
            SaveContext();

            var n = tv1.SelectedSQLTreeItem();
            if (n == null)
            {
                return;
            }

            if (tabs.SelectedTab == tabDBADashErrorLog)
            {
                collectionErrors1.AckErrors = SelectedTags().Count == 0 &&
                                              n.SQLTreeItemParent.Type == SQLTreeItem.TreeType.DBADashRoot;
            }

            if (tabs.SelectedTab == tabSchema)
            {
                await GetHistoryAsync(n.ObjectID);
            }
            else
            {
                foreach (var ctrl in tabs.SelectedTab.Controls.OfType<ISetContext>())
                {
                    ctrl.SetContext(n.Context);
                }
            }

            UpdateTimeFilterVisibility();
        }

        #region Tree

        private void AddRootRefreshContextMenu(SQLTreeItem rootNode)
        {
            rootNode.ContextMenuStrip ??= new ContextMenuStrip();
            var mnuRootRefresh = new ToolStripMenuItem("Refresh") { Image = Properties.Resources._112_RefreshArrow_Green_16x16_72 };
            rootNode.ContextMenuStrip.Items.Insert(0, mnuRootRefresh);
            mnuRootRefresh.Click += MnuRootRefresh_Click;
        }

        private async void MnuRootRefresh_Click(object sender, EventArgs e)
        {
            await AddInstancesAsync();
        }

        private async Task AddInstancesAsync(CancellationToken token = default)
        {
            // Back-compat wrapper - calls the cache-aware implementation when
            // a cache is present.
            // Clear navigation history and rebuild the tree from live data
            VisitedNodes.Clear();
            tsBack.Enabled = false;
            tv1.Nodes.Clear();

            // Build tree using shared helper
            await CommonData.UpdateInstancesListAsync(tagIDs: string.Join(",", SelectedTags()), searchString: SearchString, groupByTag: GroupByTag, token: token);
            var pools = await CommonData.GetElasticPoolsAsync(token);
            var root = BuildInstanceTree(CommonData.Instances, pools, customReports, GroupByTag);

            tv1.Nodes.Add(root);
            root.Expand();
            tv1.SelectedNode = root;
        }

        // No per-instance cache maintained here. Tree is built from
        // CommonData.Instances and freshly-captured pool/report snapshots.

        private static async Task ExpandJobsAsync(SQLTreeItem jobsNode)
        {
            var jobs = await CommonData.GetJobsAsync(jobsNode.InstanceID);
            foreach (DataRow job in jobs.Rows)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "enabled", Convert.ToBoolean(job["enabled"]) }
                };
                var jobItem = new SQLTreeItem((string)job["name"], SQLTreeItem.TreeType.AgentJob, attributes)
                { InstanceID = jobsNode.InstanceID, Tag = (Guid)job["job_id"] };
                jobItem.AddDummyNode();
                jobsNode.Nodes.Add(jobItem);
            }
        }

        private void ExpandInstance(SQLTreeItem instanceNode)
        {
            List<TreeNode> nodesToAdd = new();
            var changesNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration)
            {
                InstanceID = instanceNode.InstanceID
            };
            nodesToAdd.Add(changesNode);
            nodesToAdd.Add(new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks));
            nodesToAdd.Add(new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR));
            var storage = new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage);
            storage.AddDummyNode();
            nodesToAdd.Add(storage);
            var dbNode = new SQLTreeItem("Databases", SQLTreeItem.TreeType.DatabasesFolder);
            dbNode.AddDummyNode();
            nodesToAdd.Add(dbNode);

            var jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs) { InstanceID = instanceNode.InstanceID };
            jobs.AddDummyNode();
            nodesToAdd.Add(jobs);
            instanceNode.Nodes.AddRange(nodesToAdd.ToArray());
            instanceNode.AddReportsFolder(customReports.InstanceLevelReports);
            instanceNode.AddCommunityTools();
            instanceNode.AddCustomToolsFolder();
        }

        private static void ExpandStorage(SQLTreeItem storage)
        {
            var drives = CommonData.GetDrives(storage.InstanceIDs, false, true, true, true, true, true, null);
            storage.Nodes.Clear();
            storage.Nodes.AddRange((from DataRow drive in drives.Rows
                                    let driveLetter = Convert.ToString(drive["Name"])
                                    let label = Convert.ToString(drive["Label"])
                                    let PctFree = Convert.ToDouble(drive["PctFreeSpace"])
                                    let TotalGB = Convert.ToDouble(drive["TotalGB"])
                                    let FreeGB = Convert.ToDouble(drive["FreeGB"])
                                    let UsedGB = TotalGB - FreeGB
                                    let PctUsed = 1 - PctFree
                                    let nameAndLetter = label + " (" + driveLetter + ")"
                                    let color = DBADashStatus.GetStatusBackColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(drive["Status"]))
                                    let name = Common.AsciiProgressBar(PctUsed) + " " + nameAndLetter + "...." + UsedGB.Gigabytes().ToString("###,#.#") + "(" + PctUsed.ToString("P1") + ") Used of " + TotalGB.Gigabytes().Humanize("###,#.#")
                                    orderby driveLetter
                                    select new SQLTreeItem(name, SQLTreeItem.TreeType.Drive)
                                    { DriveName = driveLetter, ForeColor = color }).Cast<TreeNode>().ToArray());
        }

        private static async Task ExpandStorageAsync(SQLTreeItem storage)
        {
            var drives = await CommonData.GetDrivesAsync(storage.InstanceIDs, false, true, true, true, true, true, null);
            storage.Nodes.Clear();
            storage.Nodes.AddRange((from DataRow drive in drives.Rows
                                    let driveLetter = Convert.ToString(drive["Name"])
                                    let label = Convert.ToString(drive["Label"])
                                    let PctFree = Convert.ToDouble(drive["PctFreeSpace"])
                                    let TotalGB = Convert.ToDouble(drive["TotalGB"])
                                    let FreeGB = Convert.ToDouble(drive["FreeGB"])
                                    let UsedGB = TotalGB - FreeGB
                                    let PctUsed = 1 - PctFree
                                    let nameAndLetter = label + " (" + driveLetter + ")"
                                    let color = DBADashStatus.GetStatusBackColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(drive["Status"]))
                                    let name = Common.AsciiProgressBar(PctUsed) + " " + nameAndLetter + "...." + UsedGB.Gigabytes().ToString("###,#.#") + "(" + PctUsed.ToString("P1") + ") Used of " + TotalGB.Gigabytes().Humanize("###,#.#")
                                    orderby driveLetter
                                    select new SQLTreeItem(name, SQLTreeItem.TreeType.Drive)
                                    { DriveName = driveLetter, ForeColor = color }).Cast<TreeNode>().ToArray());
        }

        private static async Task ExpandDatabasesAsync(SQLTreeItem dbFolder)
        {
            List<TreeNode> nodesToAdd = new();
            var systemNode = new SQLTreeItem("System Databases", SQLTreeItem.TreeType.Folder);
            nodesToAdd.Add(systemNode);

            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.DatabasesByInstance_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", dbFolder.InstanceID);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var db = (string)rdr["name"];
                var status = (string)rdr["Status"];
                var nodeName = string.IsNullOrEmpty(status) ? db : db + " (" + status + ")";

                var n = new SQLTreeItem(nodeName, SQLTreeItem.TreeType.Database)
                {
                    DatabaseID = (int)rdr["DatabaseID"],
                    InstanceID = (int)rdr["InstanceID"],
                    DatabaseName = db
                };
                if (rdr["ObjectID"] != DBNull.Value)
                {
                    n.ObjectID = (long)rdr["ObjectID"];
                }

                n.AddDummyNode();
                if (systemDatabases.Contains((string)rdr[1]))
                {
                    systemNode.Nodes.Add(n);
                }
                else
                {
                    nodesToAdd.Add(n);
                }
            }
            dbFolder.Nodes.AddRange(nodesToAdd.ToArray());
        }

        private List<TabPage> GetAllowedTabs()
        {
            List<TabPage> allowedTabs = new();
            var n = tv1.SelectedSQLTreeItem();
            var parent = n.SQLTreeItemParent;
            var hasAzureDBs = n.AzureInstanceIDs.Count > 0;

            if (n.Type is SQLTreeItem.TreeType.DBADashRoot or SQLTreeItem.TreeType.InstanceFolder)
            {
                allowedTabs.AddRange(new[] { tabSummary, tabDBADashAlerts, tabPerformanceSummary });
                if (hasAzureDBs)
                {
                    allowedTabs.Add(tabAzureSummary);
                }

                allowedTabs.AddRange(new[] { tabSlowQueries, tabRunningQueries });
            }
            else if (n.Type == SQLTreeItem.TreeType.Database)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformance, tabObjectExecutionSummary, tabSlowQueries, tabFiles, tabSnapshotSummary,
                    tabDBSpace, tabDBConfiguration, tabDBOptions,  tabQS,tabTopQueries, tabQueryStoreForcedPlans, tabTuningRecommendations
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureDatabase)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformance, tabAzureSummary, tabAzureDB, tabMetrics,tabDBADashAlerts, tabSlowQueries, tabObjectExecutionSummary,
                    tabWaits, tabRunningQueries, tabFiles, tabTopQueries, tabQueryStoreForcedPlans, tabTuningRecommendations
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformanceSummary, tabPerformance, tabMetrics,tabDBADashAlerts, tabObjectExecutionSummary, tabSlowQueries, tabWaits,
                    tabRunningQueries, tabMemory,tabTopQueries, tabQueryStoreForcedPlans, tabTuningRecommendations
                });
                if (n.Context.HasResourceGovernorWorkloadGroups)
                {
                    allowedTabs.Add(tabPoolsAndGroups);
                }
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureInstance)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformanceSummary, tabAzureSummary,tabDBADashAlerts, tabSlowQueries, tabObjectExecutionSummary, tabRunningQueries
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.ElasticPool)
            {
                allowedTabs.AddRange(new[]
                {
                    tabAzureDB, tabPerformanceSummary, tabAzureSummary, tabSlowQueries, tabObjectExecutionSummary, tabRunningQueries, tabDBSpace
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.Configuration)
            {
                if (parent.Type == SQLTreeItem.TreeType.Instance)
                {
                    allowedTabs.Add(tabInfo);
                }
                if (n.Context.HasInstanceMetadata == true)
                {
                    allowedTabs.Add(tabCloudMetadata);
                }

                if (parent.Type != SQLTreeItem.TreeType.AzureDatabase &&
                    parent.Type != SQLTreeItem.TreeType.AzureInstance && !IsAzureOnly)
                {
                    allowedTabs.AddRange(new[]
                    {
                        tabConfiguration, tabTraceFlags, tabHardware, tabSQLPatching, tabSQLAgentAlerts, tabDrivers, tabTempDB,
                        tabResourceGovernor, tabServerServices
                    });
                }

                if (parent.Type != SQLTreeItem.TreeType.Instance && hasAzureDBs)
                {
                    allowedTabs.AddRange(new[] { tabAzureServiceObjectives, tabAzureDBResourceGovernance });
                }

                allowedTabs.AddRange(new[] { tabDBConfiguration, tabDBOptions, tabQS, tabTags });
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJobs)
            {
                allowedTabs.AddRange(parent.Type is SQLTreeItem.TreeType.DBADashRoot or SQLTreeItem.TreeType.InstanceFolder
                    ? new[] { tabJobs, tabRunningJobs }
                    : new[] { tabJobs, tabJobStats, tabJobTimeline, tabRunningJobs });
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJob)
            {
                allowedTabs.AddRange(new[] { tabJobInfo, tabJobs, tabJobDDL, tabJobStats, tabJobTimeline });
            }
            else if (n.Type == SQLTreeItem.TreeType.HADR)
            {
                allowedTabs.AddRange(new[] { tabBackups, tabLogShipping, tabMirroring, tabAG });
            }
            else if (n.Type == SQLTreeItem.TreeType.DBAChecks)
            {
                allowedTabs.Add(tabSummary);
                allowedTabs.Add(tabOfflineInstances);
                if (!IsAzureOnly && parent.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.AddRange(new[] { tabLastGood, tabOSLoadedModules });
                }

                allowedTabs.AddRange(new[]
                {
                    tabDBADashErrorLog, tabCollectionDates, tabCustomChecks, tabSnapshotSummary,
                    tabIdentityColumns
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.Tags)
            {
                allowedTabs.Add(tabTags);
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJobStep)
            {
                allowedTabs.AddRange(new[] { tabJobs, tabJobStats });
            }
            else if (n.Type == SQLTreeItem.TreeType.DatabasesFolder)
            {
                allowedTabs.Add(tabDBSpace);
            }
            else if (n.Type == SQLTreeItem.TreeType.Storage)
            {
                if (!IsAzureOnly && parent.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.AddRange(new[] { tabDrives, tabDBSpace, tabFiles, tabTempDB, tabTableSize });
                    if (n.InstanceID > 0)
                    {
                        allowedTabs.Add(tabDrivePerformance);
                    }
                }
                else
                {
                    allowedTabs.AddRange(new[] { tabDBSpace, tabFiles, tabTableSize });
                }
            }
            else if (n.Type == SQLTreeItem.TreeType.Drive)
            {
                allowedTabs.AddRange(new[] { tabDrives, tabFiles, tabDrivePerformance });
            }
            else if (n.Type is SQLTreeItem.TreeType.CustomReport or SQLTreeItem.TreeType.SystemReport or SQLTreeItem.TreeType.DirectSystemReport)
            {
                tabCustomReport.Text = n.Report.ReportName;
                allowedTabs.Add(tabCustomReport);
            }
            else if (n.Type == SQLTreeItem.TreeType.Folder && n.Text == "Tables")
            {
                allowedTabs.Add(tabTableSize);
            }
            else if (n.Type == SQLTreeItem.TreeType.RecycleBin)
            {
                allowedTabs.Add(tabDeletedInstances);
            }
            else if (n.Type == SQLTreeItem.TreeType.DatabaseExtendedProperties)
            {
                allowedTabs.Add(tabDatabaseExtendedProperties);
            }
            else if (n.Type == SQLTreeItem.TreeType.CommunityTool)
            {
                tabCustomReport.Text = n.Text;
                if (Enum.TryParse(n.Text, out ProcedureExecutionMessage.CommunityProcs proc))
                {
                    allowedTabs.Add(CommunityToolsTabPages[proc]);
                }
            }
            else if (n.Type == SQLTreeItem.TreeType.CustomTool)
            {
                CustomToolsTabs.TryGetValue(n.ObjectName, out var tab);
                if (tab == null)
                {
                    tab = new TabPage(n.Text);
                    tab.Controls.Add(new CustomReportView() { Dock = DockStyle.Fill });
                    CustomToolsTabs.Add(n.ObjectName, tab);
                }
                allowedTabs.Add(tab);
            }

            if (n.ObjectID > 0)
            {
                if (n.Type.IsQueryStoreObjectType())
                {
                    allowedTabs.AddRange(new[] { tabObjectExecutionSummary, tabSchema, tabTopQueries });

                    foreach (var tool in CommunityTools.CommunityTools.ProcedureLevelTools)
                    {
                        if (!Enum.TryParse<ProcedureExecutionMessage.CommunityProcs>(tool.ProcedureName,
                                out var proc)) continue;
                        if (!n.Context.IsScriptAllowed(ProcedureExecutionMessage.CommunityProcs.sp_QuickieStore))
                            continue;

                        allowedTabs.Add(CommunityToolsTabPages[proc]);
                    }
                }
                switch (n.Type)
                {
                    case SQLTreeItem.TreeType.Table:
                        allowedTabs.AddRange(new[] { tabTableSize, tabSchema });

                        foreach (var tool in CommunityTools.CommunityTools.TableLevelTools)
                        {
                            if (!Enum.TryParse<ProcedureExecutionMessage.CommunityProcs>(tool.ProcedureName,
                                    out var proc)) continue;
                            if (!n.Context.IsScriptAllowed(ProcedureExecutionMessage.CommunityProcs.sp_QuickieStore))
                                continue;

                            allowedTabs.Add(CommunityToolsTabPages[proc]);
                        }

                        break;

                    default:
                        allowedTabs.Add(tabSchema);
                        if (n.Context.IsScriptAllowed(ProcedureExecutionMessage.CommunityProcs.sp_DBPermissions))
                        {
                            allowedTabs.Add(CommunityToolsTabPages[ProcedureExecutionMessage.CommunityProcs.sp_DBPermissions]);
                        }
                        break;
                }
            }

            if (allowedTabs.Contains(tabSlowQueries) && n.Context.ProductVersion?.Major < 10)
            {
                allowedTabs.Remove(tabSlowQueries);
            }
            if (!n.Context.CanMessage
                || !n.Context.IsQueryStoreSupported())
            {
                allowedTabs.Remove(tabTopQueries);
                allowedTabs.Remove(tabQueryStoreForcedPlans);
            }
            if (!n.Context.CanMessage
                || !n.Context.IsQueryTuningRecommendationsSupported())
            {
                allowedTabs.Remove(tabTuningRecommendations);
            }

            if (allowedTabs.Count == 0) // Display default tab if no tabs are applicable
            {
                allowedTabs.Add(tabDBADash);
            }

            return allowedTabs.Distinct().ToList();
        }

        private async void Tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            e.Node.TreeView.BeginUpdate();
            e.Node.NodeFont = tvBoldFont;
            e.Node.BackColor = DBADashUser.SelectedTheme.SelectedTabBackColor;
            e.Node.ForeColor = DBADashUser.SelectedTheme.SelectedTabForeColor;
            e.Node.TreeView.EndUpdate();

            var suppress = suppressLoadTab;
            suppressLoadTab = true; // Don't Load tab while adding/removing tabs
            // Capture the currently selected tab so we can tell if it was removed
            // by the allowedTabs update and choose a fallback selection.
            var prevSelectedTab = tabs.SelectedTab;
            var n = tv1.SelectedSQLTreeItem();
            tabAzureDB.Text = n.Type == SQLTreeItem.TreeType.ElasticPool ? "Pool" : "Azure DB";
            var allowedTabs = GetAllowedTabs();

            Text = n.FriendlyFullPath;

            // Update the TabControl to match the allowed tabs (diff update)
            RefreshTabPages(allowedTabs, prevSelectedTab);

            suppressLoadTab = suppress; // Return tab load suppression back to previous value
            try
            {
                await LoadSelectedTabAsync();
            }
            catch (Exception ex)
            {
                Common.ShowExceptionDialog(ex, "An error occurred while loading the selected tab: " + ex.Message);
            }
        }

        private async void Tv1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var n = e.Node.AsSQLTreeItem();
            if (n.Nodes.Count == 1 && n.Nodes[0].AsSQLTreeItem().Type == SQLTreeItem.TreeType.DummyNode)
            {
                // Show a minimal visual indicator while async expand completes.
                n.Nodes.Clear();
                var loading = new SQLTreeItem("Loading...", SQLTreeItem.TreeType.Folder) { ForeColor = Color.Gray };
                try
                {
                    loading.NodeFont = new Font(tv1.Font, FontStyle.Italic);
                }
                catch { }

                n.Nodes.Add(loading);
                tv1.Refresh();

                if (n.Type == SQLTreeItem.TreeType.Instance)
                {
                    ExpandInstance(n);
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.DatabasesFolder)
                {
                    await ExpandDatabasesAsync(n);
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.AgentJobs)
                {
                    await ExpandJobsAsync(n);
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    await ExpandJobStepsAsync(n);
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.Database)
                {
                    n.AddDatabaseFolders();
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.Storage)
                {
                    await ExpandStorageAsync(n);
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                if (n.Type == SQLTreeItem.TreeType.CustomToolsFolder)
                {
                    await n.AddCustomToolsAsync();
                    if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                    n.Expand();
                    return;
                }

                await ExpandObjectsAsync(n);
                if (n.Nodes.Contains(loading)) n.Nodes.Remove(loading);
                n.Expand();
            }
        }

        private static async Task ExpandJobStepsAsync(SQLTreeItem n)
        {
            var dtSteps = await CommonData.GetJobStepsAsync(n.InstanceID, (Guid)n.Tag);
            foreach (DataRow r in dtSteps.Rows)
            {
                var stepID = (int)r["step_id"];
                var stepName = (string)r["step_name"];
                var subsystem = (string)r["subsystem"];
                var attributes = new Dictionary<string, object>
                {
                    { "subsystem", subsystem }
                };

                n.Nodes.Add(new SQLTreeItem(stepName, SQLTreeItem.TreeType.AgentJobStep, attributes)
                { InstanceID = n.InstanceID, Tag = stepID });
            }
        }

        private static async Task ExpandObjectsAsync(SQLTreeItem n)
        {
            var dbObj = await CommonData.GetObjectsAsync(n.DatabaseID, (string)n.Tag);
            foreach (DataRow r in dbObj.Rows)
            {
                var type = ((string)r[1]).Trim();
                var objN = new SQLTreeItem((string)r[3], (string)r[2], type)
                {
                    ObjectID = (long)r[0]
                };
                n.Nodes.Add(objN);
            }
        }

        #endregion Tree

        #region SchemaSnapshots

        private void LoadDDL(long DDLID, long DDLIDOld)
        {
            var newText = Common.DDL(DDLID);
            var oldText = Common.DDL(DDLIDOld);
            diffSchemaSnapshot.OldText = oldText;
            diffSchemaSnapshot.NewText = newText;
        }

        private async Task GetHistoryAsync(long ObjectID, int PageNum = 1)
        {
            diffSchemaSnapshot.OldText = "";
            diffSchemaSnapshot.NewText = "";
            currentPageSize = int.Parse(tsPageSize.Text);
            var dt = await CommonData.GetDDLHistoryForObjectAsync(ObjectID, PageNum, currentPageSize);

            gvHistory.AutoGenerateColumns = false;
            gvHistory.DataSource = dt;
            gvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            currentObjectID = ObjectID;
            currentPage = PageNum;
            tsPageNum.Text = "Page " + PageNum;
            tsPrevious.Enabled = (PageNum > 1);
            tsNext.Enabled = dt.Rows.Count == currentPageSize;
        }

        private void GvHistory_SelectionChanged(object sender, EventArgs e)
        {
            if (gvHistory.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvHistory.SelectedRows[0].DataBoundItem;
                long ddlID;
                long ddlIDOld;
                if (row["DDLID"] == DBNull.Value)
                {
                    ddlID = -1;
                }
                else
                {
                    ddlID = (long)row["DDLID"];
                }

                if (row["DDLIDOld"] == DBNull.Value)
                {
                    ddlIDOld = -1;
                }
                else
                {
                    ddlIDOld = (long)row["DDLIDOld"];
                }

                LoadDDL(ddlID, ddlIDOld);
            }
        }

        private async void TsNext_Click(object sender, EventArgs e)
        {
            await GetHistoryAsync(currentObjectID, currentPage + 1);
        }

        private async void TsPrevious_Click(object sender, EventArgs e)
        {
            await GetHistoryAsync(currentObjectID, currentPage - 1);
        }

        private async void TsPageSize_Validated(object sender, EventArgs e)
        {
            if (int.Parse(tsPageSize.Text) != currentPageSize)
            {
                await GetHistoryAsync(currentObjectID);
            }
        }

        private void TsPageSize_Validating(object sender, CancelEventArgs e)
        {
            _ = int.TryParse(tsPageSize.Text, out var i);
            if (i <= 0)
            {
                tsPageSize.Text = currentPageSize.ToString();
            }
        }

        #endregion SchemaSnapshots

        #region Tagging

        private bool isClearTags;

        private void BuildTagMenu(List<int> selected = null)
        {
            mnuTags.DropDownItems.Clear();

            var currentTag = string.Empty;
            ToolStripMenuItem mTagName = new();
            ToolStripMenuItem mSystemTags = new("System Tags");
            mSystemTags.Font = new Font(mnuTags.Font, FontStyle.Italic);
            var tags = DBADashTag.GetTags(Common.ConnectionString);
            tags1.AllTags = tags;
            foreach (var tag in tags)
            {
                if (tag.TagName != currentTag)
                {
                    mTagName = new ToolStripMenuItem(tag.TagName);

                    if (tag.TagName.StartsWith('{'))
                    {
                        mTagName.Font = mSystemTags.Font;
                        mSystemTags.DropDownItems.Add(mTagName);
                    }
                    else
                    {
                        mnuTags.DropDownItems.Add(mTagName);
                    }

                    currentTag = tag.TagName;
                }

                var mTagValue = new ToolStripMenuItem(tag.TagValue)
                {
                    Tag = tag.TagID,
                    CheckOnClick = true
                };

                if (selected != null && selected.Contains(tag.TagID))
                {
                    mTagValue.Checked = true;
                }

                mTagValue.CheckedChanged += MTagValue_CheckedChanged;
                mTagName.DropDownItems.Add(mTagValue);
            }

            var clearTag = new ToolStripMenuItem("Clear All");
            clearTag.Font = new Font(mnuTags.Font, FontStyle.Italic);
            clearTag.Click += ClearTag_Click;
            var refreshTag = new ToolStripMenuItem("Refresh Tags");
            refreshTag.Font = new Font(mnuTags.Font, FontStyle.Italic);
            refreshTag.Click += RefreshTag_Click;

            mnuTags.DropDownItems.Add(mSystemTags);
            mnuTags.DropDownItems.Add("-");
            mnuTags.DropDownItems.Add(refreshTag);
            mnuTags.DropDownItems.Add(clearTag);

            SetCheckedItemsBold(mnuTags);

            BuildGroupByTagMenu(ref tags);
        }

        private void BuildGroupByTagMenu(ref List<DBADashTag> tags)
        {
            groupToolStripMenuItem.DropDownItems.Clear();
            var tagNames = tags.Select(t => t.TagName).Distinct();
            ToolStripMenuItem mSystemTags = new("System Tags")
            { Font = new Font(groupToolStripMenuItem.Font, FontStyle.Italic) };
            ToolStripMenuItem mNone = new("[None]") { Tag = string.Empty };
            ToolStripMenuItem mShowCounts = new("Show Counts") { CheckOnClick = true, Checked = ShowCounts };
            ToolStripMenuItem mSave = new("Save") { Image = Properties.Resources.Save_16x };
            mNone.Click += GroupByTag_Click;
            mShowCounts.Click += ShowCounts_Click;
            mSave.Click += SaveTreeLayout;
            foreach (var tag in tagNames)
            {
                ToolStripMenuItem mnu = new(tag) { Tag = tag };
                mnu.Click += GroupByTag_Click;
                if (tag.StartsWith('{'))
                {
                    mSystemTags.DropDownItems.Add(mnu);
                }
                else
                {
                    groupToolStripMenuItem.DropDownItems.Add(mnu);
                }
            }

            groupToolStripMenuItem.DropDownItems.Add(mSystemTags);
            groupToolStripMenuItem.DropDownItems.Add(mNone);
            groupToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            groupToolStripMenuItem.DropDownItems.Add(mShowCounts);
            groupToolStripMenuItem.DropDownItems.Add(mSave);
        }

        private void SaveTreeLayout(object sender, EventArgs e)
        {
            TreeSavedView treeView = new()
            { GroupByTag = GroupByTag, ShowCounts = ShowCounts, Name = SavedView.DefaultViewName };
            treeView.Save();
            MessageBox.Show("Tree layout saved", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetTreeLayout()
        {
            GroupByTag = string.Empty;
            try
            {
                var savedView = SavedView.GetSavedViews(SavedView.ViewTypes.Tree, DBADashUser.UserID)
                    .Where(sv => sv.Key == SavedView.DefaultViewName);
                if (savedView.Count() == 1)
                {
                    var treeView = TreeSavedView.Deserialize(savedView.First().Value);
                    GroupByTag = treeView.GroupByTag;
                    ShowCounts = treeView.ShowCounts;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Unexpected error loading tree layout");
            }
        }

        private async void ShowCounts_Click(object sender, EventArgs e)
        {
            ShowCounts = ((ToolStripMenuItem)sender).Checked;
            await AddInstancesAsync();
        }

        private async void GroupByTag_Click(object sender, EventArgs e)
        {
            GroupByTag = (string)((ToolStripMenuItem)sender).Tag;
            await AddInstancesAsync();
        }

        private void RefreshTag_Click(object sender, EventArgs e)
        {
            BuildTagMenu(SelectedTags());
        }

        private async void ClearTag_Click(object sender, EventArgs e)
        {
            customReports = CustomReports.CustomReports.GetCustomReports(true);
            isClearTags = true;
            mnuTags.Font = Font = new Font(mnuTags.Font, mnuTags.Font.Style & ~FontStyle.Bold);
            ClearTags(mnuTags.DropDownItems);
            isClearTags = false;
            await AddInstancesAsync();
            Font = new Font(Font, FontStyle.Regular);
        }

        private void ClearTags(ToolStripItemCollection items)
        {
            foreach (ToolStripItem mnu in items)
            {
                if (mnu.GetType() == typeof(ToolStripMenuItem))
                {
                    ((ToolStripMenuItem)mnu).Checked = false;
                    ClearTags(((ToolStripMenuItem)mnu).DropDownItems);
                    if (mnu.Font.Bold)
                    {
                        mnu.Font = Font = new Font(mnu.Font, mnu.Font.Style & ~FontStyle.Bold);
                    }
                }
            }
        }

        private List<int> SelectedTags()
        {
            return SelectedTags(mnuTags.DropDownItems);
        }

        private static List<int> SelectedTags(ToolStripItemCollection items)
        {
            var selected = new List<int>();
            foreach (var mnuTag in items.OfType<ToolStripMenuItem>())
            {
                if (mnuTag.Checked)
                {
                    selected.Add((int)mnuTag.Tag!);
                }
                if (mnuTag.DropDownItems.Count > 0)
                {
                    selected.AddRange(SelectedTags(mnuTag.DropDownItems));
                }
            }

            return selected;
        }

        private async void MTagValue_CheckedChanged(object sender, EventArgs e)
        {
            if (isClearTags) return;
            await AddInstancesAsync();
            var mnuTag = (ToolStripMenuItem)sender;
            while (mnuTag.OwnerItem is ToolStripMenuItem item)
            {
                mnuTag = item;
            }
            SetCheckedItemsBold(mnuTags);
        }

        public static void SetCheckedItemsBold(ToolStripDropDownButton dropdownButton)
        {
            var hasCheckedChild = false;

            foreach (ToolStripItem item in dropdownButton.DropDownItems)
            {
                if (item is not ToolStripMenuItem menuItem) continue;
                if (ProcessMenuItem(menuItem))
                {
                    hasCheckedChild = true;
                }
            }

            // If the dropdownButton has a checked child, make it bold while preserving existing styles.
            dropdownButton.Font = hasCheckedChild
                ? new Font(dropdownButton.Font, dropdownButton.Font.Style | FontStyle.Bold)
                : new Font(dropdownButton.Font, dropdownButton.Font.Style & ~FontStyle.Bold);
        }

        public static bool ProcessMenuItem(ToolStripMenuItem menuItem)
        {
            var isCheckedOrHasCheckedChild = menuItem.Checked;

            // If this item has children, process them first.
            if (menuItem.HasDropDownItems)
            {
                foreach (ToolStripItem child in menuItem.DropDownItems)
                {
                    if (child is not ToolStripMenuItem childMenuItem) continue;
                    if (ProcessMenuItem(childMenuItem))
                    {
                        isCheckedOrHasCheckedChild = true;
                    }
                }
            }

            // Apply the bold font style if the item is checked or has a checked child while preserving existing styles.
            menuItem.Font = isCheckedOrHasCheckedChild
                ? new Font(menuItem.Font, menuItem.Font.Style | FontStyle.Bold)
                : new Font(menuItem.Font, menuItem.Font.Style & ~FontStyle.Bold);

            return isCheckedOrHasCheckedChild;
        }

        #endregion Tagging

        private void DataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataRetention dataRetentionForm = new();
            dataRetentionForm.ShowSingleInstance();
        }

        public async void Instance_Selected(object sender, InstanceSelectedEventArgs e)
        {
            var root = e.SearchFromRoot ? tv1.Nodes[0].AsSQLTreeItem() : tv1.SelectedSQLTreeItem();

            SQLTreeItem nInstance;

            if (e.InstanceID <= 0 && string.IsNullOrEmpty(e.Instance)) // No Instance - Use root Level
            {
                nInstance = root.Type == SQLTreeItem.TreeType.DBAChecks ? root.Parent.AsSQLTreeItem() : root;
            }
            else
            {
                nInstance = e.InstanceID > 0 ? root.FindInstance(e.InstanceID) : root.FindInstance(e.Instance);
            }

            if (nInstance == null)
            {
                MessageBox.Show("Instance not found: " + (e.InstanceID > 0 ? e.InstanceID.ToString() : e.Instance),
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                suppressLoadTab = true;
                try
                {
                    var parent = nInstance.Parent;
                    if (parent is { IsExpanded: false })
                    {
                        parent.Expand();
                    }

                    var selectedNode = nInstance;
                    if (!string.IsNullOrEmpty(e.Database) && nInstance.Type != SQLTreeItem.TreeType.AzureDatabase)
                    {
                        nInstance.Expand();
                        var dbFolder = nInstance.FindChildOfType(SQLTreeItem.TreeType.DatabasesFolder);
                        dbFolder?.Expand();
                        var db = dbFolder?.Nodes.Cast<SQLTreeItem>()
                            .FirstOrDefault(d => string.Equals(d.DatabaseName, e.Database, StringComparison.CurrentCultureIgnoreCase));
                        if (db == null)
                        {
                            MessageBox.Show("Database not found: " + e.Database, "Warning", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                        selectedNode = db;
                    }
                    if (selectedNode == null) return;

                    switch (e.Tab)
                    {
                        case Tabs.Files or Tabs.DBSpace or Tabs.SnapshotSummary or Tabs.DBConfiguration or Tabs.DBOptions or Tabs.QS when selectedNode.Type == SQLTreeItem.TreeType.Database:
                            break;

                        case Tabs.SQLAgentAlerts or Tabs.QS or Tabs.DBOptions or Tabs.DBConfiguration or Tabs.InstanceMetadata or Tabs.Configuration or Tabs.TraceFlags or Tabs.Hardware or Tabs.SQLPatching or Tabs.Drivers or Tabs.TempDB
                            or Tabs.ResourceGovernor or Tabs.ServerServices or Tabs.AzureServiceObjectives or Tabs.AzureDBResourceGovernance or Tabs.Tags:
                            selectedNode.Expand();
                            selectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.Configuration);
                            break;

                        case Tabs.Mirroring or Tabs.LogShipping or Tabs.Backups or Tabs.AG:
                            selectedNode.Expand();
                            selectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.HADR);
                            break;

                        case Tabs.Jobs or Tabs.RunningJobs or Tabs.JobTimeline or Tabs.JobStats:
                            selectedNode.Expand();
                            selectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.AgentJobs);
                            break;

                        case Tabs.AzureSummary or Tabs.Performance or Tabs.Metrics or Tabs.PerformanceSummary or Tabs.SlowQueries or Tabs.Waits or Tabs.Memory or Tabs.ObjectExecutionSummary or Tabs.TopQueries or Tabs.QueryStoreForcedPlans:
                            break;

                        case Tabs.Files or Tabs.Drives or Tabs.TableSize or Tabs.DBSpace:
                            selectedNode.Expand();
                            selectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.Storage);
                            break;

                        default:
                            selectedNode.Expand();
                            selectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.DBAChecks);
                            break;
                    }

                    tv1.SelectedNode = selectedNode;

                    if (e.Tab.HasValue)
                    {
                        if (tabs.TabPages.ContainsKey(e.Tab.Value.TabName()))
                        {
                            tabs.SelectedTab = tabs.TabPages[e.Tab.Value.TabName()];
                        }
                        else
                        {
                            await NavigateBackAsync();
                            MessageBox.Show("Selected tab page is not available for this context", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error selecting instance");
                }
                finally
                {
                    suppressLoadTab = false;
                }

                await LoadSelectedTabAsync();
            }
        }

        private void UpdateTimeFilterVisibility()
        {
            tsDateRange.Visible = GlobalTimeIsVisible;
            tsDayOfWeek.Visible = DateRange.CurrentDateRangeSupportsDayOfWeekFilter &&
                                  CurrentTabSupportsDayOfWeekFilter && GlobalTimeIsVisible;
            tsTimeFilter.Visible = DateRange.CurrentDateRangeSupportsTimeOfDayFilter &&
                                   CurrentTabSupportsTimeOfDayFilter && GlobalTimeIsVisible;
            tsDayOfWeek.Font = new Font(tsDayOfWeek.Font,
                DateRange.HasDayOfWeekFilter ? FontStyle.Bold : FontStyle.Regular);
            tsTimeFilter.Font = new Font(tsTimeFilter.Font,
                DateRange.HasTimeOfDayFilter ? FontStyle.Bold : FontStyle.Regular);
        }

        private void DateRangeChanged(object sender, EventArgs e) => DateRangeChanged();

        private void DateRangeChanged()
        {
            if (tsDateRange.SelectedDateTo != null && tsDateRange.SelectedDateFrom != null)
            {
                DateRange.SetCustom(tsDateRange.DateFromUtc, tsDateRange.DateToUtc);
            }
            else if (tsDateRange.SelectedTimeSpan != null)
            {
                DateRange.SetTimeSpan(tsDateRange.SelectedTimeSpan.Value);
            }
            else
            {
                DateRange.SetTimeSpan(TimeSpan.FromMinutes(60));
            }

            UpdateTimeFilterVisibility();

            foreach (var ctrl in tabs.SelectedTab!.Controls.OfType<IRefreshData>())
            {
                ctrl.RefreshData();
            }
        }

        private void ManageInstancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ManageInstances manageInstancesForm = new()
            {
                Tags = string.Join(",", SelectedTags())
            };
            manageInstancesForm.FormClosing += delegate
            {
                if (manageInstancesForm.InstanceActiveFlagChanged || manageInstancesForm.InstanceSummaryVisibleChanged)
                {
                    // Fire-and-forget refresh - don't block UI while dialog closes
                    _ = AddInstancesAsync(); // refresh the tree if instances deleted/restored
                    if (tabs.SelectedTab == tabSummary)
                    {
                        summary1.RefreshData();
                    }
                }
            };
            manageInstancesForm.ShowSingleInstance();
        }

        private void GvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var node = tv1.SelectedSQLTreeItem();
            if (e.RowIndex >= 0 && e.ColumnIndex == colCompare.Index)
            {
                var row = (DataRowView)gvHistory.Rows[e.RowIndex].DataBoundItem;
                var frm = new DDLCompareTo
                {
                    Instance_A = node.InstanceName,
                    DatabaseID_A = node.DatabaseID,
                    ObjectType_A = (string)row["ObjectType"],
                    ObjectID_A = node.ObjectID,
                    SnapshotDate_A = (DateTime)row["SnapshotValidFrom"],
                    SelectedTags = SelectedTags()
                };
                frm.ApplyTheme();
                frm.ShowDialog();
            }
        }

        private void Tags1_TagsChanged(object sender, EventArgs e)
        {
            BuildTagMenu(SelectedTags());
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonShared.ShowAbout(Common.ConnectionString, this, true);
        }

        private async void BttnSearch_Click(object sender, EventArgs e)
        {
            await AddInstancesAsync();
        }

        private void DatabaseSchemaDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var n = tv1.SelectedSQLTreeItem();
            DBDiff dbDiffForm = new DBDiff
            {
                SelectedTags = SelectedTags(),
                SelectedInstanceA = n.InstanceName,
                SelectedDatabaseA = new DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName }
            };
            dbDiffForm.ApplyTheme();
            dbDiffForm.ShowSingleInstance();
        }

        private void AgentJobDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = tv1.SelectedSQLTreeItem();
            JobDiff jobDiffForm = new()
            {
                InstanceID_A = selected.InstanceID
            };
            jobDiffForm.ApplyTheme();
            jobDiffForm.ShowSingleInstance();
        }

        private async void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                await AddInstancesAsync();
            }
        }

        private static ConfigureDisplayName ConfigureDisplayNameForm;

        private void ConfigureDisplayNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureDisplayNameForm?.Close();
            ConfigureDisplayNameForm = new ConfigureDisplayName()
            {
                TagIDs = string.Join(",", SelectedTags()),
                SearchString = SearchString
            };
            ConfigureDisplayNameForm.FormClosing += delegate
            {
                if (ConfigureDisplayNameForm.EditCount > 0)
                {
                    // Fire-and-forget is acceptable here because the dialog is about
                    // a user edit; we don't need to block closing on tree refresh.
                    _ = AddInstancesAsync();
                }

                ConfigureDisplayNameForm = null;
            };
            ConfigureDisplayNameForm.ShowSingleInstance();
        }

        private async void FreezeKeyColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.FreezeKeyColumn = freezeKeyColumnsToolStripMenuItem.Checked;
            await LoadSelectedTabAsync();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var bHandled = false;
            switch (keyData)
            {
                case Keys.F5:
                    if (GlobalTimeIsVisible)
                    {
                        DateRangeChanged();
                    }
                    else
                    {
                        LoadSelectedTabAsync().ObserveFault();
                    }

                    bHandled = true;
                    break;
            }

            return bHandled;
        }

        private void TsDayOfWeek_Click(object sender, EventArgs e)
        {
            using (var frm = new DayOfWeekSelection() { DaysOfWeekSelected = DateRange.DayOfWeek })
            {
                frm.ShowDialog(this);
                if (frm.DialogResult == DialogResult.OK)
                {
                    DateRange.DayOfWeek = frm.DaysOfWeekSelected;
                    DateRangeChanged();
                }
            }
        }

        private void TsTimeFilter_Click(object sender, EventArgs e)
        {
            using (var frm = new HourSelection { SelectedHours = DateRange.TimeOfDay })
            {
                frm.ShowDialog(this);
                if (frm.DialogResult == DialogResult.OK)
                {
                    DateRange.TimeOfDay = frm.SelectedHours;
                    DateRangeChanged();
                }
            }
        }

        /// <summary>
        /// Navigate to first tab (Summary) at root node in the tree.
        /// </summary>
        private void TsHome_Click(object sender, EventArgs e)
        {
            if (tv1.Nodes.Count == 0) return;
            tv1.SelectedNode = tv1.Nodes[0];
            tabs.SelectedIndex = 0;
        }

        private void Tv1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (tv1.SelectedNode == null) return;
            SaveContext(tv1.SelectedNode, tabs.SelectedIndex);
            tv1.SelectedNode.NodeFont = tv1.Font;
            tv1.SelectedNode.BackColor = Color.Empty;
            tv1.SelectedNode.ForeColor = Color.Empty;
        }

        /// <summary>
        /// Save current node and tab to support NavigateBack
        /// </summary>
        private void SaveContext()
        {
            SaveContext(tv1.SelectedNode, tabs.SelectedIndex);
        }

        /// <summary>
        /// Save current node and tab to support NavigateBack
        /// </summary>
        private void SaveContext(TreeNode node, int tagIndex)
        {
            if (node != null)
            {
                SaveContext(node.AsSQLTreeItem(), tagIndex);
            }
        }

        /// <summary>
        /// Save current node and tab to support NavigateBack
        /// </summary>
        private void SaveContext(SQLTreeItem node, int TabIndex)
        {
            if (node != null &&
                TabIndex >= 0 &
                !suppressSaveContext) // Save context if we have a current node/tab and save context is not supressed (e.g. When navigating back)
            {
                if (VisitedNodes.Count > 0) // Save only if current context is different to previous context
                {
                    var previousContext = VisitedNodes[^1];
                    if (previousContext.Node == node && previousContext.TabIndex == TabIndex)
                    {
                        return;
                    }
                }
                else if (tv1.Nodes.Count > 0)
                {
                    // Ensure we have added the root context
                    VisitedNodes.Add(new TreeContext() { Node = tv1.Nodes[0].AsSQLTreeItem(), TabIndex = 0 });
                    if (node == tv1.Nodes[0] &&
                        TabIndex == 0) // If context we are saving is root, we don't need to add it again
                    {
                        return;
                    }
                }

                VisitedNodes.Add(new TreeContext() { Node = node, TabIndex = TabIndex });
                tsBack.Enabled = VisitedNodes.Count > 0;
            }
        }

        private async void TsBack_Click(object sender, EventArgs e)
        {
            await NavigateBackAsync();
        }

        /// <summary>
        /// Navigate back.  If current control supports INavigation, invoke the NavigateBack for the control.  If move back was not performed on control, take user to previous selected node/tab in tree.
        /// </summary>
        private async Task NavigateBackAsync()
        {
            foreach (var ctrl in tabs.SelectedTab.Controls)
            {
                if (ctrl is INavigation navigation)
                {
                    if (navigation.NavigateBack())
                    {
                        return;
                    }
                }
            }

            if (VisitedNodes.Count > 0)
            {
                suppressSaveContext =
                    true; // Don't save the context change when moving back.  Otherwise we'd flip flop between two contexts.
                var context = VisitedNodes[^1];
                VisitedNodes.RemoveAt(VisitedNodes.Count - 1);
                if (context.Node == tv1.SelectedNode &&
                    context.TabIndex ==
                    tabs.SelectedIndex) // If move back location is set to current location, navigate to the next saved location
                {
                    await NavigateBackAsync();
                }
                else
                {
                    suppressLoadTab =
                        true; // Avoid potential double refresh when changing tree node then moving to different tab
                    ShowRefresh();
                    tv1.SelectedNode = context.Node;
                    tabs.SelectedIndex = context.TabIndex;
                    suppressLoadTab = false;
                    await LoadSelectedTabAsync(); // Need to refresh data.  In some cases it could be OK not to refresh - in others, data could be shown from the wrong context.
                    ShowRefresh(false);
                }

                tsBack.Enabled = VisitedNodes.Count > 0;
                suppressSaveContext = false;
            }
        }

        /// <summary>
        /// Show Refresh screen
        /// </summary>
        private void ShowRefresh(bool showRefresh = true)
        {
            tabs.Visible = !showRefresh;
            refresh1.Visible = showRefresh;
            Application.DoEvents();
        }

        /// <summary>
        /// Handle mouse back button press
        /// https://stackoverflow.com/questions/67683479/process-mouse-back-and-forward-buttons-in-winforms-form-when-its-client-area-is
        /// </summary>
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_XBUTTONDOWN)
            {
                var lowWord = (m.WParam.ToInt32() << 16) >> 16;
                switch (lowWord)
                {
                    case MK_XBUTTON1:
                        // navigate backward
                        NavigateBackAsync().ObserveFault();
                        break;
                        // case MK_XBUTTON2:
                        // navigate forward
                        // break;
                }
            }

            return false; // dispatch further
        }

        private async void TsConnect_Click(object sender, EventArgs e)
        {
            using var frm = new DBConnection() { ConnectionString = GetNewConnectionString() };
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;
            try
            {
                var connection = new RepositoryConnection() { ConnectionString = frm.ConnectionString };
                connection.SetDefaultName();
                RunSetConnection(connection);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error switching to DBA Dash repository database");
            }
        }

        private bool IsLoadingTimeZones = true;

        // Cancellation token source used for background SetConnection attempts
        private CancellationTokenSource _setConnectionCts;

        // Last connection attempted (used by Retry)
        private RepositoryConnection _lastConnectionAttempt;

        private CustomReports.CustomReports _customReportsCache;

        private System.Windows.Forms.Timer _connectingTimer;
        private int _connectingDots;

        private void AddTimeZoneMenus()
        {
            cboTimeZone.Items.Clear();
            cboTimeZone.Items.Add(TimeZoneInfo.Local);
            cboTimeZone.Items.AddRange(TimeZoneInfo.GetSystemTimeZones().ToArray());
            cboTimeZone.SelectedItem = DateHelper.AppTimeZone;
            IsLoadingTimeZones = false;
        }

        private void TimeZone_Selected(object sender, EventArgs e)
        {
            if (!IsLoadingTimeZones)
            {
                DateHelper.AppTimeZone = cboTimeZone.SelectedItem as TimeZoneInfo;
                tabs.SelectedTab?.Controls.OfType<IRefreshData>().ToList().ForEach(c => c.RefreshData());
            }
        }

        private void SaveTimeZonePreferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save time zone: " + DateHelper.AppTimeZone.DisplayName, "Save",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DBADashUser.UserTimeZone = DateHelper.AppTimeZone;
                DBADashUser.Update();
                MessageBox.Show($"Time zone will be set to {DateHelper.AppTimeZone.DisplayName} on application start.",
                    "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void ShowHidden_Changed(object sender, EventArgs e)
        {
            Common.ShowHidden = showHiddenToolStripMenuItem.Checked;
            await LoadSelectedTabAsync();
        }

        private RepositoryConnectionList repositories;

        private void LoadRepositoryConnections()
        {
            tsConnect.DropDownItems.Clear();
            repositories ??= RepositoryConnectionList.GetRepositoryConnectionList();

            foreach (var connection in repositories)
            {
                var tsConnection = new ToolStripMenuItem() { Text = connection.Name, Tag = connection.Name, Image = Properties.Resources.Database_16x };
                tsConnection.Click += TsConnection_Click;
                var tsConnect2 = new ToolStripMenuItem() { Text = "Connect", Tag = connection.Name, Image = Properties.Resources.ConnectToDatabase_16x };
                tsConnect2.Click += TsConnection_Click;
                var tsRemove = new ToolStripMenuItem() { Text = "Remove", Tag = connection.Name, Image = Properties.Resources.DeleteDatabase_16x };
                tsRemove.Click += TsRemove_Click;
                var tsRename = new ToolStripMenuItem() { Text = "Rename", Tag = connection.Name, Image = Properties.Resources.Rename_16x };
                tsRename.Click += TsRename_Click;
                var tsSetDefault = new ToolStripMenuItem() { Text = "Set Default", Tag = connection.Name, Checked = connection.IsDefault };
                tsSetDefault.Click += TsSetDefault_Click;
                tsConnection.DropDownItems.AddRange(new ToolStripItem[] { tsConnect2, tsRename, tsSetDefault, tsRemove });
                tsConnect.DropDownItems.Add(tsConnection);
            }

            tsConnect.DropDownItems.Add(new ToolStripSeparator());
            var tsAddConnection = tsConnect.DropDownItems.Add("Add");
            tsAddConnection.Image = Properties.Resources.AddConnection_16x;
            tsAddConnection.Click += TsAddConnection_Click;

            var tsConn = tsConnect.DropDownItems.Add("Connect");
            tsConn.Image = Properties.Resources.ConnectToDatabase_16x;
            tsConn.Click += TsConnect_Click;
        }

        private void TsSetDefault_Click(object sender, EventArgs e)
        {
            var name = (string)((ToolStripMenuItem)sender).Tag;
            foreach (var connection in repositories)
            {
                connection.IsDefault = connection.Name == name;
            }
            repositories.Save();
            LoadRepositoryConnections();
        }

        private void TsRename_Click(object sender, EventArgs e)
        {
            var name = (string)((ToolStripMenuItem)sender).Tag;
            var connection = repositories.FindByName(name);
            if (CommonShared.ShowInputDialog(ref name, "Enter name") == DialogResult.OK)
            {
                connection.Name = name;
                repositories.Save();
                LoadRepositoryConnections();
            }
        }

        private void TsRemove_Click(object sender, EventArgs e)
        {
            var name = (string)((ToolStripItem)sender).Tag;
            if (MessageBox.Show($"Remove connection {name}?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                repositories.Remove(name);
                repositories.Save();
                LoadRepositoryConnections();
            }
        }

        private async void TsConnection_Click(object sender, EventArgs e)
        {
            var name = (string)((ToolStripItem)sender).Tag;
            var connection = repositories.FindByName(name);
            RunSetConnection(connection);
            tsConnect.HideDropDown();
        }

        private async void TsAddConnection_Click(object sender, EventArgs e)
        {
            await AddConnectionAsync();
        }

        private static string GetNewConnectionString()
        {
            var connectionString = Common.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = (new SqlConnectionStringBuilder()
                {
                    InitialCatalog = "DBADashDB",
                    IntegratedSecurity = true,
                    Encrypt = true
                }).ConnectionString;
            }
            return connectionString;
        }

        private async Task AddConnectionAsync()
        {
            using var frm = new DBConnection() { ConnectionString = GetNewConnectionString() };
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;
            var connection = repositories.FindByConnectionString(frm.ConnectionString);
            if (connection != null)
            {
                MessageBox.Show($"The connection already exists: {connection.Name}", "Add Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            connection = new RepositoryConnection() { ConnectionString = frm.ConnectionString };

            try
            {
                DBValidations.GetDBVersion(connection.ConnectionString);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return;
            }

            connection.Name = connection.GetDefaultName();
            repositories.Add(connection);
            LoadRepositoryConnections();
            repositories.Save();
            RunSetConnection(connection);
        }

        private void SetAutoRefresh(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var interval = int.Parse(((string)item.Tag)!);
            if (interval == 0)
            {
                var strInterval = (autoRefreshTimer.Interval / 1000).ToString();
                if (CommonShared.ShowInputDialog(ref strInterval, "Enter interval in seconds:") == DialogResult.OK)
                {
                    if (!int.TryParse(strInterval, out interval) || interval < 1)
                    {
                        MessageBox.Show("Invalid input", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            SetAutoRefresh(interval);
        }

        private void SetAutoRefresh(int interval)
        {
            autoRefreshTimer.Enabled = false;
            autoRefreshTimer.Stop();
            noneToolStripMenuItem.Checked = true;

            if (interval > 0)
            {
                autoRefreshTimer.Interval = interval * 1000;
                autoRefreshTimer.Enabled = true;
                autoRefreshTimer.Start();
            }

            var checkCount = 0;
            foreach (var itm in setAutoRefreshToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                itm.Checked = int.Parse(((string)itm.Tag)!) == interval;
                if (itm.Checked)
                {
                    checkCount += 1;
                }
            }

            if (checkCount == 0)
            {
                customToolStripMenuItem.Checked = true;
            }
        }

        private void DisableAutoRefresh()
        {
            SetAutoRefresh(-1);
        }

        private bool IsAutoRefreshApplicable => tabs.SelectedTab != null && tabs.SelectedTab.Controls.OfType<IRefreshData>().Any();

        private static readonly string[] systemDatabases = { "master", "model", "msdb", "tempdb" };

        private void AutoRefresh(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) // Skip auto refresh if app is minimized
            {
                return;
            }
            foreach (var ctrl in tabs.SelectedTab.Controls.OfType<IRefreshData>())
            {
                ctrl.RefreshData();
            }
        }

        private void DefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTheme(new BaseTheme());
        }

        private void DarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTheme(new DarkTheme());
        }

        private void LightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTheme(new WhiteTheme());
        }

        private void CheckTheme()
        {
            defaultToolStripMenuItem.Checked = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Default;
            darkToolStripMenuItem.Checked = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark;
            whiteToolStripMenuItem.Checked = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.White;
        }

        private void SetTheme(BaseTheme theme, bool update = true)
        {
            if (update)
            {
                DBADashUser.SelectedTheme = theme;
                DBADashUser.Update();
            }
            CheckTheme();
            this.ApplyTheme(theme);
            // Clear per-tab applied theme tracking so tabs will be re-themed
            // with the new theme when they are next selected.
            _tabThemeApplied.Clear();
            foreach (var ctrl in tabs.SelectedTab?.Controls.OfType<IRefreshData>()!)
            {
                ctrl.RefreshData();
            }
        }

        private void RepositorySettings_Click(object sender, EventArgs e)
        {
            using var options = new RepoSettings();
            options.ShowDialog();
        }

        private void externalDiffToolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var frm = new ExternalDiffConfig();
            frm.ShowDialog();
        }

        #region "Alerts"

        private async void TsAlert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Common.ConnectionString)) return;
            if (tv1.Nodes.Count == 0) return;
            tv1.SelectedNode = tv1.Nodes[0];
            tabs.SelectedTab = tabDBADashAlerts;
            await LoadSelectedTabAsync();
        }

        public async Task<(int Total, int ForAttention, short Priority)> GetAlertCounts()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("Alert.AlertCount_Get", cn) { CommandType = CommandType.StoredProcedure };
            await using var rdr = await cmd.ExecuteReaderAsync();
            await rdr.ReadAsync();
            var total = rdr.GetInt32("Total");
            var forAttention = rdr.GetInt32("ForAttention");
            var priority = rdr.GetByte("Priority");
            return (total, forAttention, priority);
        }

        /// <summary>
        /// Update bell menu icon to indicate if we have any alerts & priority of those alerts
        /// </summary>
        /// <returns></returns>
        private async Task UpdateAlertIcon()
        {
            try
            {
                var counts = await GetAlertCounts();
                // Only update GUI if it's changed since the last run
                if (tsAlert.Tag?.ToString() != counts.ToString())
                {
                    menuStrip1.Invoke(() =>
                    {
                        tsAlert.Text = counts.ForAttention.ToString() + " / " + counts.Total;
                        tsAlert.ToolTipText = $"Alerts for your attention: {counts.ForAttention}\nTotal active alerts: {counts.Total}";
                        tsAlert.Image = counts.Priority switch
                        {
                            <= 10 => Properties.Resources.Alert_Critical,
                            <= 20 => Properties.Resources.Alert_Warning,
                            <= 30 => Properties.Resources.Alert_Warning,
                            <= 40 => Properties.Resources.Alert_Information,
                            41 => Properties.Resources.Alert_OK,
                            _ => Properties.Resources.Alert_Information
                        };
                    });
                }
                tsAlert.Tag = counts.ToString();
            }
            catch (Exception ex)
            {
                menuStrip1.Invoke(() =>
                {
                    tsAlert.ToolTipText = ex.Message;
                    tsAlert.Text = @"Error";
                    tsAlert.Image = Properties.Resources.StatusCriticalError_16x;
                    tsAlert.Tag = "Error";
                });
            }
        }

        private void ShowAlertToolTip(object sender, EventArgs e)
        {
            toolTip1.Show(tsAlert.ToolTipText, tsAlert.GetCurrentParent()!);
        }

        private DateTime NewAlertsFromDate = DateTime.UtcNow;

        private async Task GetNewAlertsLoop()
        {
            while (true)
            {
                if (string.IsNullOrEmpty(Common.ConnectionString))
                {
                    await Task.Delay(10000);
                    continue;
                }
                await UpdateAlertIcon();
                if (Settings.Default.DesktopNotificationsEnabled)
                {
                    await GetNewAlerts();
                }

                await Task.Delay(10000);
            }
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true,
                Text = @"DBA Dash Alert Notification"
            };
        }

        private async Task GetNewAlerts()
        {
            try
            {
                await using var cn = new SqlConnection(Common.ConnectionString);
                await cn.OpenAsync();
                await using var cmd = new SqlCommand("Alert.NewAlerts_Get", cn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.Add(new SqlParameter("FromDate", SqlDbType.DateTime2) { Value = NewAlertsFromDate });
                await using var rdr = await cmd.ExecuteReaderAsync();
                var dt = new DataTable();
                dt.Load(rdr);
                if (dt.Rows.Count == 0) return;
                var priority = dt.Rows.Cast<DataRow>().Min(r => Convert.ToInt16(r["Priority"]));
                var alertDate = dt.Rows.Cast<DataRow>().Max(r => Convert.ToDateTime(r["AlertDate"]));
                var icon = priority switch
                {
                    <= 10 => ToolTipIcon.Error,
                    <= 20 => ToolTipIcon.Warning,
                    <= 30 => ToolTipIcon.Warning,
                    <= 40 => ToolTipIcon.Warning,
                    41 => ToolTipIcon.Info,
                    _ => ToolTipIcon.Info
                };

                StringBuilder sb = new();
                dt.Rows.Cast<DataRow>().Take(5).ToList().ForEach(r =>
                    sb.AppendLine($"{r["InstanceDisplayName"]} - {r["AlertType"]} - {r["LastMessage"]}"));
                if (dt.Rows.Count > 5) sb.AppendLine("...");
                notifyIcon.ShowBalloonTip(10000, $"{dt.Rows.Count} new alerts", sb.ToString(), icon);

                NewAlertsFromDate = alertDate;
            }
            catch (Exception ex)
            {
                notifyIcon.ShowBalloonTip(10000, $"Error getting new alerts", ex.Message, ToolTipIcon.Error);
            }
        }

        #endregion "Alerts"

        private void DesktopNotificationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.DesktopNotificationsEnabled = desktopNotificationsToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        private void setTimeFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var format = DBADashUser.TimeFormatString;
            if (CommonShared.ShowInputDialog(ref format, "Enter Time format string.  e.g 't', 'HH:mm'") !=
                DialogResult.OK) return;
            DBADashUser.TimeFormatString = format;
            MessageBox.Show(
                $"The time format has been set.\n\n The current time is {DateHelper.AppNow.ToString(DBADashUser.TimeFormatString)}.\n\nThe new format will be used when the chart is refreshed.", "Time Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void setDateTimeFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var format = DBADashUser.DateTimeFormatString;
            if (CommonShared.ShowInputDialog(ref format,
                    "Enter Date/Time format string.  e.g 'g', 'yyyy-MM-dd HH:mm'") != DialogResult.OK) return;
            DBADashUser.DateTimeFormatString = format;
            MessageBox.Show(
                $"The date/time format has been set.\n\nThe current time is {DateHelper.AppNow.ToString(DBADashUser.DateTimeFormatString)}.\n\nThe new format will be used when the chart is refreshed.", "Time Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetChartAxisFontSize_Click(object sender, EventArgs e)
        {
            SetChartFontSize(
                currentValue: DBADashUser.ChartAxisLabelFontSize,
                defaultValue: DBADashUser.DefaultChartAxisLabelFontSize,
                settingName: "axis label",
                setSetting: size => DBADashUser.ChartAxisLabelFontSize = size
            );
        }

        private void SetChartAxisNameFontSize_Click(object sender, EventArgs e)
        {
            SetChartFontSize(
                currentValue: DBADashUser.ChartAxisNameFontSize,
                defaultValue: DBADashUser.DefaultChartAxisNameFontSize,
                settingName: "axis title",
                setSetting: size => DBADashUser.ChartAxisNameFontSize = size
            );
        }

        /// <summary>
        /// Common helper for setting chart font sizes with validation and reset functionality
        /// </summary>
        private static void SetChartFontSize(float currentValue, float defaultValue, string settingName, Action<float> setSetting)
        {
            var fontSize = currentValue.ToString();

            if (CommonShared.ShowInputDialog(ref fontSize, $"Chart {settingName} font size (6-24 or blank for default:{defaultValue})") != DialogResult.OK)
                return;

            // Handle empty string as reset to default (-1 means use default constant)
            if (string.IsNullOrWhiteSpace(fontSize))
            {
                setSetting(-1);
                MessageBox.Show(
                    $"Chart {settingName} font size has been reset to default ({defaultValue}).\n\nCharts will use the new font size when refreshed.",
                    "Chart Font Size",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (float.TryParse(fontSize, out var size) && size >= 6 && size <= 24)
            {
                setSetting(size);
                MessageBox.Show(
                    $"Chart {settingName} font size has been set to {size}.\n\nCharts will use the new font size when refreshed.",
                    "Chart Font Size",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid font size between 6 and 24.", "Invalid Font Size", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ToggleSingleInstancePreference(object sender, EventArgs e)
        {
            bool singleInstance = !Settings.Default.ChildFormSingleInstance;
            SetSingleInstance(singleInstance);
            Settings.Default.ChildFormSingleInstance = singleInstance;
            Settings.Default.Save();
        }

        private void SetSingleInstance(bool singleInstance)
        {
            DBADashSharedGUI.ExtensionMethods.ChildFormSingleInstance = singleInstance;
            tsToggleSingleInstance.Image = singleInstance ? Properties.Resources.AppWindow : Properties.Resources.CascadeWindowsHS;
            tsToggleSingleInstance.ToolTipText = singleInstance ? "Single instance child form mode.  Click to change to multiple instance mode." : "Multiple instance child form mode.  Click to change to single instance mode.";
        }

        private void CloseChildWindows_Click(object sender, EventArgs e)
        {
            // Snapshot to avoid collection modification issues during iteration
            var otherForms = Application.OpenForms.Cast<Form>()
                .Where(f => f != this)
                .ToList();

            foreach (var frm in otherForms)
            {
                try
                {
                    // Attempt to close the form; parent may not be set, so close unconditionally
                    frm.Close();
                }
                catch
                {
                    // Ignore exceptions to ensure all windows are processed
                }
            }
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            Controls.ApplyTheme(theme);
            searchLayout.BackColor = theme.SearchBackColor;
            cboTimeZone.BackColor = theme.TimeZoneBackColor;
            cboTimeZone.ForeColor = theme.TimeZoneForeColor;
            cboTimeZone.FlatStyle = FlatStyle.Flat;
            if (AllTabs == null) return;
            foreach (var tab in AllTabs)
            {
                tab.ApplyTheme();
            }
        }

        // Extracted helper to keep Tv1_AfterSelect concise. Performs a minimal
        // diff update of the TabControl pages while avoiding flicker.
        private void RefreshTabPages(List<TabPage> allowedTabs, TabPage prevSelectedTab)
        {
            // Quick-check: if nothing to do, return
            var validatedTabs = allowedTabs.Count == tabs.TabPages.Count && tabs.TabPages.Cast<TabPage>().All(t => allowedTabs.Contains(t));
            if (validatedTabs) return;

            var prevVisible = tabs.Visible;
            try
            {
                ShowRefresh(true); // use existing overlay to mask updates
                tabs.SuspendLayout();
                SuspendDrawing(tabs);

                // Apply theme to TabControl itself
                var theme = DBADashUser.SelectedTheme;
                try { tabs.ApplyTheme(theme); tabs.BackColor = theme.TabBackColor; } catch { }

                // Pre-apply theme to candidate pages
                foreach (var t in allowedTabs)
                {
                    try
                    {
                        if (!_tabThemeApplied.TryGetValue(t, out var applied) || applied != DBADashUser.SelectedTheme.ThemeIdentifier)
                        {
                            t.ApplyTheme();
                            _tabThemeApplied[t] = DBADashUser.SelectedTheme.ThemeIdentifier;
                        }
                    }
                    catch { }
                }

                // Remove pages not allowed
                var current = tabs.TabPages.Cast<TabPage>().ToList();
                foreach (var p in current.Where(p => !allowedTabs.Contains(p)).ToList())
                {
                    try { tabs.TabPages.Remove(p); } catch { }
                }

                // Insert or reposition pages to match allowed order
                for (int i = 0; i < allowedTabs.Count; i++)
                {
                    var page = allowedTabs[i];
                    if (!tabs.TabPages.Contains(page))
                    {
                        try { tabs.TabPages.Insert(i, page); } catch { tabs.TabPages.Add(page); }
                    }
                    else
                    {
                        var idx = tabs.TabPages.IndexOf(page);
                        if (idx != i)
                        {
                            tabs.TabPages.Remove(page);
                            tabs.TabPages.Insert(i, page);
                        }
                    }
                }

                // Ensure handle-dependent theming (ToolStrip renderers etc.)
                foreach (TabPage t in tabs.TabPages)
                {
                    try
                    {
                        t.CreateControl();
                        t.ApplyTheme();
                        foreach (var ts in t.Controls.OfType<ToolStrip>())
                        {
                            try { ts.Invalidate(); ts.Refresh(); } catch { }
                        }
                    }
                    catch { }
                }

                // If previously selected tab was removed, pick the first available
                if (prevSelectedTab != null && !tabs.TabPages.Cast<TabPage>().Contains(prevSelectedTab))
                {
                    if (tabs.TabPages.Count > 0) tabs.SelectedIndex = 0;
                }
            }
            finally
            {
                ResumeDrawing(tabs);
                tabs.ResumeLayout();
                tabs.Visible = prevVisible;
                ShowRefresh(false);
            }
        }

        private void CancelConnection_Click(object sender, EventArgs e)
        {
            _setConnectionCts?.Cancel();
            try
            {
                // Give immediate feedback on UI
                this.Invoke((Action)(() =>
                {
                    lblConnecting.Text = "Cancelling...";
                    lblConnectionInfo.Text = "Cancelling connection...";
                }));
            }
            catch { }
        }

        private async void Retry_Click(object sender, EventArgs e)
        {
            // Disable retry to avoid rapid re-clicks
            bttnRetry.InvokeSetEnabled(false);

            // If we have a last connection attempt, try it again
            if (_lastConnectionAttempt != null)
            {
                RunSetConnection(_lastConnectionAttempt);
            }
            else
            {
                await AddConnectionAsync();
                bttnRetry.Enabled = true;
            }
        }
    }
}