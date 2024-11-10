using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Options_Menu;
using DBADashGUI.Performance;
using DBADashGUI.Properties;
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
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.CommunityTools;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.SqlServer.Management.XEvent;
using Version = System.Version;

namespace DBADashGUI
{
    public partial class Main : Form, IMessageFilter, IThemedControl
    {
        public class InstanceSelectedEventArgs : EventArgs
        {
            public int InstanceID = -1;
            public string Instance;
            public string Tab;
        }

        private readonly CommandLineOptions commandLine;
        private readonly List<int> commandLineTags = new();
        private TabPage[] AllTabs;
        private CustomReports.CustomReports customReports = new();
        private readonly Dictionary<ProcedureExecutionMessage.CommandNames, TabPage> CommunityToolsTabPages = new Dictionary<ProcedureExecutionMessage.CommandNames, TabPage>();
        private TabPage tabBlitzIndex => CommunityToolsTabPages[ProcedureExecutionMessage.CommandNames.sp_BlitzIndex];

        public Main(CommandLineOptions opts)
        {
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => Application.RemoveMessageFilter(this);
            InitializeComponent();
            commandLine = opts;
            Disposed += OnDispose;
            AddTabs();
        }

        private void AddTabs()
        {
            foreach (var proc in Enum.GetValues<ProcedureExecutionMessage.CommandNames>())
            {
                var tab = GetCommunityToolsTabPage(proc);
                CommunityToolsTabPages.Add(proc, tab);
                tabs.TabPages.Add(tab);
            }
        }

        public TabPage GetCommunityToolsTabPage(ProcedureExecutionMessage.CommandNames proc)
        {
            var tab = new TabPage(proc.ToString());
            var report = CommunityTools.CommunityTools.CommunityToolsList.First(report => report.ProcedureName == proc.ToString());
            tab.Controls.Add(new CustomReportView() { Dock = DockStyle.Fill, Report = report });
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

        private bool CurrentTabSupportsDayOfWeekFilter =>
            (new List<TabPage>()
                { tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabWaits })
            .Contains(tabs.SelectedTab);

        private bool CurrentTabSupportsTimeOfDayFilter =>
            (new List<TabPage>()
                { tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabWaits })
            .Contains(tabs.SelectedTab);

        private bool GlobalTimeIsVisible =>
            (new List<TabPage>()
            {
                tabPerformanceSummary, tabPerformance, tabSlowQueries, tabAzureDB, tabAzureSummary, tabPC,
                tabObjectExecutionSummary, tabWaits, tabRunningQueries, tabMemory, tabJobStats, tabJobTimeline, tabDrivePerformance, tabTopQueries
            }).Contains(tabs.SelectedTab) || (tabs.SelectedTab == tabCustomReport && ((SQLTreeItem)tv1.SelectedNode).Report.TimeFilterSupported);

        private bool IsAzureOnly;
        private bool ShowCounts;
        private string GroupByTag = string.Empty;
        private readonly List<TreeContext> VisitedNodes = new();
        private bool suppressSaveContext;

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
                }

                // Prompt the user to connect to an existing DBA Dash repository DB.
                await AddConnection();
                if (repositories.Count == 0)
                {
                    Application.Exit();
                    return;
                }
            }

            await SetConnection(repositories.GetDefaultConnection());
            this.ApplyTheme();
            CheckTheme();
            WindowState = FormWindowState.Maximized;
            if (splitMain.SplitterDistance > 400)
            {
                splitMain.SplitterDistance = 400;
            }

            customReportView1.ReportNameChanged += CustomReport_ReportNameChanged;
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

        public async Task SetConnection(RepositoryConnection connection)
        {
            SetConnectionState(false);
            if (connection == null) return;

            try
            {
                if (!CheckRepositoryDBConnection(connection.ConnectionString))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                await CheckVersion(connection.ConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Common.SetConnectionString(connection);
            try
            {
                Config.RefreshConfig();
                DBADashUser.GetUser();
                customReports = CustomReports.CustomReports.GetCustomReports();
                GetCommandLineTags();
                GetTreeLayout();
                BuildTagMenu(commandLineTags);
                AddInstances();
                AddTimeZoneMenus();
                SetConnectionState(true);
                repoSettingsToolStripMenuItem.Enabled = DBADashUser.IsAdmin;
                ThemeExtensions.CellToolTipMaxLength = Config.CellToolTipMaxLength;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetConnectionState(bool isGood)
        {
            bttnSearch.Enabled = isGood;
            optionsToolStripMenuItem.Enabled = isGood;
            diffToolStripMenuItem.Enabled = isGood;
            if (!isGood)
            {
                tv1.Nodes.Clear();
                tabs.TabPages.Clear();
                tabs.TabPages.Add(tabDBADash);
            }
        }

        /// <summary>
        /// Check connection to DBA Dash repository DB.  User is prompted to retry or cancel on failure.
        /// </summary>
        /// <returns>True if connection succeeded</returns>
        private static bool CheckRepositoryDBConnection(string connectionString)
        {
            var connectionCheckPassed = false;
            while (!connectionCheckPassed)
            {
                try
                {
                    using (var cn = new SqlConnection(connectionString))
                    {
                        cn.Open();
                    }

                    connectionCheckPassed = true;
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show("Error connecting to repository database\n" + ex.Message, "Connection Error",
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                    {
                        break;
                    }
                }
            }

            return connectionCheckPassed;
        }

        private static async Task CheckVersion(string connectionString)
        {
            var dbVersion = DBValidations.GetDBVersion(connectionString);
            var appVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            var compare =
                (new Version(appVersion.Major, appVersion.Minor)).CompareTo(new Version(dbVersion.Major,
                    dbVersion.Minor));

            if (compare < 0)
            {
                var promptUpgrade = MessageBox.Show(
                    $"The version of this GUI app ({appVersion.Major}.{appVersion.Minor}) is OLDER than the repository database. Please upgrade to version {dbVersion.Major}.{dbVersion.Minor}{Environment.NewLine}Would you like to run the upgrade script now?",
                    "Upgrade GUI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (promptUpgrade == DialogResult.Yes)
                {
                    var tag = dbVersion.ToString(3);
                    await Upgrade.UpgradeDBADashAsync(tag, true);
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show(
                        "The GUI might be unstable as it's not designed to run against this version of the repository database.",
                        "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (compare > 0)
            {
                MessageBox.Show(
                    $"The version of this GUI app ({appVersion.Major}.{appVersion.Minor}) is NEWER than the repository database ({dbVersion.Major}.{dbVersion.Minor}). You might experience issues using the GUI.  Please upgrade the repository database.", "Upgrade Agent",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void GetCommandLineTags()
        {
            if (string.IsNullOrEmpty(commandLine.TagFilters)) return;
            foreach (var t in DBADashTag.GetTags(commandLine.TagFilters))
            {
                commandLineTags.Add(t.TagID);
            }
        }

        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedTab();
        }

        private void LoadSelectedTab()
        {
            DisableAutoRefresh();
            setAutoRefreshToolStripMenuItem.Enabled = IsAutoRefreshApplicable;

            jobTimeline1.IsActive = false; // Don't re-draw timeline on resize unless control is active
            if (suppressLoadTab)
            {
                return;
            }
            // tabs.SelectedTab?.ApplyTheme();
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
                GetHistory(n.ObjectID);
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

        private ContextMenuStrip RootRefreshContextMenu()
        {
            var ctxMnu = new ContextMenuStrip();
            var mnuRootRefresh = new ToolStripMenuItem("Refresh") { Image = Resources._112_RefreshArrow_Green_16x16_72 };
            ctxMnu.Items.Add(mnuRootRefresh);
            mnuRootRefresh.Click += MnuRootRefresh_Click;
            return ctxMnu;
        }

        private void MnuRootRefresh_Click(object sender, EventArgs e)
        {
            AddInstances();
        }

        private void AddInstances()
        {
            VisitedNodes.Clear();
            tsBack.Enabled = false;
            tv1.Nodes.Clear();
            var root = new SQLTreeItem(Common.RepositoryDBConnection.Name, SQLTreeItem.TreeType.DBADashRoot)
            { ContextMenuStrip = RootRefreshContextMenu() };
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            var hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
            var checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
            var storage = new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage);
            var jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs);

            root.Nodes.AddRange(new TreeNode[] { changes, checks, hadr, storage, jobs });

            root.AddReportsFolder(customReports.RootLevelReports);

            var parentNode = root;

            var tags = string.Join(",", SelectedTags());

            CommonData.UpdateInstancesList(tagIDs: tags, searchString: SearchString, groupByTag: GroupByTag);

            SQLTreeItem AzureNode = null;
            var currentTagGroup = string.Empty;
            foreach (DataRow row in CommonData.Instances.Rows)
            {
                var instance = (string)row["Instance"];
                var displayName = (string)row["InstanceDisplayName"];
                var instanceID = (int)row["InstanceID"];
                var showInSummary = (bool)row["ShowInSummary"];
                var tagGroup = Convert.ToString(row["TagGroup"]);

                if (currentTagGroup != tagGroup && !string.IsNullOrEmpty(tagGroup))
                {
                    parentNode = new SQLTreeItem(GroupByTag + ": " + tagGroup, SQLTreeItem.TreeType.InstanceFolder);
                    changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
                    hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
                    checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
                    storage = new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage);
                    jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs);
                    parentNode.Nodes.AddRange(new TreeNode[] { changes, checks, hadr, storage, jobs });
                    root.Nodes.Add(parentNode);
                    currentTagGroup = tagGroup;
                }

                DatabaseEngineEdition edition;
                try
                {
                    edition = (DatabaseEngineEdition)Convert.ToInt32(row["EngineEdition"]);
                }
                catch
                {
                    edition = DatabaseEngineEdition.Unknown;
                }

                if ((bool)row["IsAzure"])
                {
                    var db = (string)row["AzureDBName"];
                    if (AzureNode == null || AzureNode.InstanceName != instance)
                    {
                        AzureNode = new SQLTreeItem(instance, SQLTreeItem.TreeType.AzureInstance)
                        {
                            EngineEdition = edition
                        };
                        parentNode.Nodes.Add(AzureNode);
                        AzureNode.Nodes.AddRange(
                            new TreeNode[]
                            {
                                new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration),
                                new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks),
                                new SQLTreeItem("Tags", SQLTreeItem.TreeType.Tags),
                                new SQLTreeItem("Storage", SQLTreeItem.TreeType.Storage)
                            }
                        );

                        AzureNode.AddReportsFolder(customReports.InstanceLevelReports);
                        var poolNodes = CommonData.Instances.Rows.Cast<DataRow>()
                            .Where(r => (string)r["Instance"] == instance && r["elastic_pool_name"] != DBNull.Value)
                            .Select(r => (string)r["elastic_pool_name"])
                            .Distinct()
                            .OrderBy(r => r)
                            .Select(poolName => (TreeNode)new SQLTreeItem(poolName, SQLTreeItem.TreeType.ElasticPool) { ElasticPoolName = poolName })
                            .ToArray();

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
                    azureDBNode.Nodes.AddRange(
                        new TreeNode[]
                        {
                            new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration),
                            new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks)
                            }
                        );
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

            if (ShowCounts) // Show count of instances for the grouping
            {
                foreach (var n in root.Nodes.Cast<SQLTreeItem>()
                             .Where(t => t.Type == SQLTreeItem.TreeType.InstanceFolder))
                {
                    n.Text += "    {" + n.Nodes.Cast<SQLTreeItem>().Count(child => child.Type is SQLTreeItem.TreeType.Instance or SQLTreeItem.TreeType.AzureInstance) + "}";
                }
            }

            IsAzureOnly = root.InstanceIDs.Count == root.AzureInstanceIDs.Count;
            if (IsAzureOnly)
            {
                root.Nodes.Remove(hadr);
            }
            if (DBADashUser.IsAdmin)
            {
                var recycleBin = new SQLTreeItem("Recycle Bin", SQLTreeItem.TreeType.RecycleBin);
                root.Nodes.Add(recycleBin);
            }

            tv1.Nodes.Add(root);
            root.Expand();
            tv1.SelectedNode = root;
        }

        private static void ExpandJobs(SQLTreeItem jobsNode)
        {
            var jobs = CommonData.GetJobs(jobsNode.InstanceID);
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

        private static void ExpandDatabases(SQLTreeItem dbFolder)
        {
            List<TreeNode> nodesToAdd = new();
            var systemNode = new SQLTreeItem("System Databases", SQLTreeItem.TreeType.Folder);
            nodesToAdd.Add(systemNode);

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesByInstance_Get", cn)
            { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", dbFolder.InstanceID);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
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
                }
            }

            dbFolder.Nodes.AddRange(nodesToAdd.ToArray());
        }

        private static void ExpandObjects(SQLTreeItem n)
        {
            var dbObj = CommonData.GetDBObjects(n.DatabaseID, (string)n.Tag);
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

        private List<TabPage> GetAllowedTabs()
        {
            List<TabPage> allowedTabs = new();
            var n = tv1.SelectedSQLTreeItem();
            var parent = n.SQLTreeItemParent;
            var hasAzureDBs = n.AzureInstanceIDs.Count > 0;

            if (n.Type is SQLTreeItem.TreeType.DBADashRoot or SQLTreeItem.TreeType.InstanceFolder)
            {
                allowedTabs.AddRange(new[] { tabSummary, tabPerformanceSummary });
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
                    tabPerformance, tabObjectExecutionSummary, tabSlowQueries, tabFiles, tabSnapshotsSummary,
                    tabDBSpace, tabDBConfiguration, tabDBOptions,  tabQS,tabTopQueries, tabQueryStoreForcedPlans
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureDatabase)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformance, tabAzureSummary, tabAzureDB, tabPC, tabSlowQueries, tabObjectExecutionSummary,
                    tabWaits, tabRunningQueries, tabFiles, tabTopQueries, tabQueryStoreForcedPlans
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabSlowQueries, tabWaits,
                    tabRunningQueries, tabMemory,tabTopQueries, tabQueryStoreForcedPlans
                });
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureInstance)
            {
                allowedTabs.AddRange(new[]
                {
                    tabPerformanceSummary, tabAzureSummary, tabSlowQueries, tabObjectExecutionSummary, tabRunningQueries
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

                if (parent.Type != SQLTreeItem.TreeType.AzureDatabase &&
                    parent.Type != SQLTreeItem.TreeType.AzureInstance && !IsAzureOnly)
                {
                    allowedTabs.AddRange(new[]
                    {
                        tabInstanceConfig, tabTraceFlags, tabHardware, tabSQLPatching, tabAlerts, tabDrivers, tabTempDB,
                        tabRG, tabServerServices
                    });
                }

                if (parent.Type != SQLTreeItem.TreeType.Instance && hasAzureDBs)
                {
                    allowedTabs.AddRange(new[] { tabServiceObjectives, tabAzureDBesourceGovernance });
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
                allowedTabs.AddRange(new[] { tabJobs, tabJobDDL, tabJobStats, tabJobTimeline });
            }
            else if (n.Type == SQLTreeItem.TreeType.HADR)
            {
                allowedTabs.AddRange(new[] { tabBackups, tabLogShipping, tabMirroring, tabAG });
            }
            else if (n.Type == SQLTreeItem.TreeType.DBAChecks)
            {
                allowedTabs.Add(tabSummary);
                if (!IsAzureOnly && parent.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.AddRange(new[] { tabLastGood, tabOSLoadedModules });
                }

                allowedTabs.AddRange(new[]
                {
                    tabDBADashErrorLog, tabCollectionDates, tabCustomChecks, tabSnapshotsSummary,
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
            else if (n.Type is SQLTreeItem.TreeType.CustomReport or SQLTreeItem.TreeType.SystemReport)
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
            else if (n.Type == SQLTreeItem.TreeType.CommunityTool)
            {
                tabCustomReport.Text = n.Text;
                if (Enum.TryParse(n.Text, out ProcedureExecutionMessage.CommandNames proc))
                {
                    allowedTabs.Add(CommunityToolsTabPages[proc]);
                }
            }

            if (n.ObjectID > 0)
            {
                switch (n.Type)
                {
                    case SQLTreeItem.TreeType.StoredProcedure or SQLTreeItem.TreeType.CLRProcedure
                        or SQLTreeItem.TreeType.ScalarFunction or SQLTreeItem.TreeType.CLRScalarFunction
                        or SQLTreeItem.TreeType.Trigger or SQLTreeItem.TreeType.CLRTrigger:
                        allowedTabs.AddRange(new[] { tabObjectExecutionSummary, tabPerformance });
                        break;

                    case SQLTreeItem.TreeType.Table:
                        allowedTabs.Add(tabTableSize);
                        if (n.Context.IsScriptAllowed(ProcedureExecutionMessage.CommandNames.sp_BlitzIndex))
                        {
                            allowedTabs.Add(tabBlitzIndex);
                        }
                        break;
                }

                allowedTabs.Add(tabSchema);
            }

            if (allowedTabs.Contains(tabSlowQueries) && n.Context.ProductVersion?.Major < 10)
            {
                allowedTabs.Remove(tabSlowQueries);
            }
            if (!n.Context.CanMessage || (n.Context.ProductVersion?.Major < 13 && n.Context.AzureInstanceIDs.Count == 0))
            {
                allowedTabs.Remove(tabTopQueries);
                allowedTabs.Remove(tabQueryStoreForcedPlans);
            }

            if (allowedTabs.Count == 0) // Display default tab if no tabs are applicable
            {
                allowedTabs.Add(tabDBADash);
            }

            return allowedTabs;
        }

        private void Tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var suppress = suppressLoadTab;
            suppressLoadTab = true; // Don't Load tab while adding/removing tabs
            var n = tv1.SelectedSQLTreeItem();
            tabAzureDB.Text = n.Type == SQLTreeItem.TreeType.ElasticPool ? "Pool" : "Azure DB";
            var allowedTabs = GetAllowedTabs();

            Text = n.FriendlyFullPath;

            // Check if Tab pages match tabs currently loaded
            var validatedTabs = allowedTabs.Count == tabs.TabPages.Count && tabs.TabPages.Cast<TabPage>().All(t => allowedTabs.Contains(t));

            // If tab pages don't match, clear tabs and reload
            if (!validatedTabs)
            {
                tabs.TabPages.Clear();
                tabs.TabPages.AddRange(allowedTabs.ToArray());
            }

            suppressLoadTab = suppress; // Return tab load suppression back to previous value
            LoadSelectedTab();
        }

        private void Tv1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var n = e.Node.AsSQLTreeItem();
            if (n.Nodes.Count == 1 && n.Nodes[0].AsSQLTreeItem().Type == SQLTreeItem.TreeType.DummyNode)
            {
                n.Nodes.Clear();
                if (n.Type == SQLTreeItem.TreeType.Instance)
                {
                    ExpandInstance(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.DatabasesFolder)
                {
                    ExpandDatabases(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.AgentJobs)
                {
                    ExpandJobs(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    ExpandJobSteps(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.Database)
                {
                    n.AddDatabaseFolders();
                }
                else if (n.Type == SQLTreeItem.TreeType.Storage)
                {
                    ExpandStorage(n);
                }
                else
                {
                    ExpandObjects(n);
                }
            }
        }

        private static void ExpandJobSteps(SQLTreeItem n)
        {
            var dtSteps = CommonData.GetJobSteps(n.InstanceID, (Guid)n.Tag);
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

        #endregion Tree

        #region SchemaSnapshots

        private void LoadDDL(long DDLID, long DDLIDOld)
        {
            var newText = Common.DDL(DDLID);
            var oldText = Common.DDL(DDLIDOld);
            diffSchemaSnapshot.OldText = oldText;
            diffSchemaSnapshot.NewText = newText;
        }

        private void GetHistory(long ObjectID, int PageNum = 1)
        {
            diffSchemaSnapshot.OldText = "";
            diffSchemaSnapshot.NewText = "";
            currentPageSize = int.Parse(tsPageSize.Text);
            var dt = CommonData.GetDDLHistoryForObject(ObjectID, PageNum, currentPageSize);

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

        private void TsNext_Click(object sender, EventArgs e)
        {
            GetHistory(currentObjectID, currentPage + 1);
        }

        private void TsPrevious_Click(object sender, EventArgs e)
        {
            GetHistory(currentObjectID, currentPage - 1);
        }

        private void TsPageSize_Validated(object sender, EventArgs e)
        {
            if (int.Parse(tsPageSize.Text) != currentPageSize)
            {
                GetHistory(currentObjectID);
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
            mSystemTags.Font = new Font(mSystemTags.Font, FontStyle.Italic);
            var tags = DBADashTag.GetTags();
            tags1.AllTags = tags;
            foreach (var tag in tags)
            {
                if (tag.TagName != currentTag)
                {
                    mTagName = new ToolStripMenuItem(tag.TagName);

                    if (tag.TagName.StartsWith('{'))
                    {
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

            mnuTags.DropDownItems.Add(mSystemTags);
            var clearTag = new ToolStripMenuItem("Clear All");
            clearTag.Font = new Font(clearTag.Font, FontStyle.Italic);
            clearTag.Click += ClearTag_Click;
            var refreshTag = new ToolStripMenuItem("Refresh Tags");
            refreshTag.Font = new Font(refreshTag.Font, FontStyle.Italic);
            refreshTag.Click += RefreshTag_Click;
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
                MessageBox.Show("Unexpected error loading tree layout\n" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ShowCounts_Click(object sender, EventArgs e)
        {
            ShowCounts = ((ToolStripMenuItem)sender).Checked;
            AddInstances();
        }

        private void GroupByTag_Click(object sender, EventArgs e)
        {
            GroupByTag = (string)((ToolStripMenuItem)sender).Tag;
            AddInstances();
        }

        private void RefreshTag_Click(object sender, EventArgs e)
        {
            BuildTagMenu(SelectedTags());
        }

        private void ClearTag_Click(object sender, EventArgs e)
        {
            customReports = CustomReports.CustomReports.GetCustomReports(true);
            isClearTags = true;
            mnuTags.Font = Font = new Font(mnuTags.Font, mnuTags.Font.Style & ~FontStyle.Bold);
            ClearTags(mnuTags.DropDownItems);
            isClearTags = false;
            AddInstances();
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

        private void MTagValue_CheckedChanged(object sender, EventArgs e)
        {
            if (isClearTags) return;
            AddInstances();
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

        private DataRetention DataRetentionForm;

        private void DataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DataRetentionForm == null)
            {
                DataRetentionForm = new();
                DataRetentionForm.FormClosed += delegate { DataRetentionForm = null; };
            }

            DataRetentionForm.Show();
            DataRetentionForm.Focus();
        }

        private void Instance_Selected(object sender, InstanceSelectedEventArgs e)
        {
            var root = tv1.SelectedSQLTreeItem();

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

                    if (e.Tab is "tabAlerts" or "tabQS" or "tabDBOptions") // Configuration Node
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.Configuration);
                    }
                    else if (e.Tab is "tabMirroring" or "tabLogShipping" or "tabBackups" or "tabAG")
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.HADR);
                    }
                    else if (e.Tab == "tabJobs" && parent == null) // Root Level
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.AgentJobs);
                    }
                    else if (e.Tab == "tabJobs" && parent != null) // Instance Level Jobs tab
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.AgentJobs);
                    }
                    else if (e.Tab is "tabAzureSummary" or "tabPerformance")
                    {
                        tv1.SelectedNode = nInstance;
                    }
                    else if (e.Tab is "tabFiles" or "tabDrives")
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.FindChildOfType(SQLTreeItem.TreeType.Storage);
                    }
                    else
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.Nodes[1];
                    }

                    if (e.Tab is { Length: > 0 })
                    {
                        if (tabs.TabPages.ContainsKey(e.Tab))
                        {
                            tabs.SelectedTab = tabs.TabPages[e.Tab];
                        }
                        else
                        {
                            NavigateBack();
                            MessageBox.Show("Selected tab page is not available for this context", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error selecting instance: " + ex.Message, "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    suppressLoadTab = false;
                }

                LoadSelectedTab();
            }
        }

        private void TsTime_Click(object sender, EventArgs e)
        {
            var tag = (string)((ToolStripMenuItem)sender).Tag;
            CheckTime(tag);
            DateRange.SetMins(Convert.ToInt32(tag));
            DateRangeChanged();
        }

        private void CheckTime(string tag)
        {
            tsTime.Font = tag != "60" ? new Font(tsTime.Font, FontStyle.Bold) : new Font(tsTime.Font, FontStyle.Regular);

            foreach (var itm in tsTime.DropDownItems)
            {
                if (itm.GetType() == typeof(ToolStripMenuItem))
                {
                    var ts = (ToolStripMenuItem)itm;
                    ts.Font = new Font(ts.Font, FontStyle.Regular);
                    ts.Checked = (string)ts.Tag == tag;
                    if (ts.Checked)
                    {
                        tsTime.Text = ts.Text;
                    }
                }
            }
        }

        private void UpdateTimeFilterVisibility()
        {
            tsTime.Visible = GlobalTimeIsVisible;
            tsDayOfWeek.Visible = DateRange.CurrentDateRangeSupportsDayOfWeekFilter &&
                                  CurrentTabSupportsDayOfWeekFilter && GlobalTimeIsVisible;
            tsTimeFilter.Visible = DateRange.CurrentDateRangeSupportsTimeOfDayFilter &&
                                   CurrentTabSupportsTimeOfDayFilter && GlobalTimeIsVisible;
            tsDayOfWeek.Font = new Font(tsDayOfWeek.Font,
                DateRange.HasDayOfWeekFilter ? FontStyle.Bold : FontStyle.Regular);
            tsTimeFilter.Font = new Font(tsTimeFilter.Font,
                DateRange.HasTimeOfDayFilter ? FontStyle.Bold : FontStyle.Regular);
        }

        private void DateRangeChanged()
        {
            UpdateTimeFilterVisibility();

            foreach (var ctrl in tabs.SelectedTab.Controls.OfType<IRefreshData>())
            {
                ctrl.RefreshData();
            }
        }

        private void TsCustomTime_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker()
            { FromDate = DateRange.FromUTC.ToAppTimeZone(), ToDate = DateRange.ToUTC.ToAppTimeZone() };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                DateRange.SetCustom(frm.FromDate.AppTimeZoneToUtc(), frm.ToDate.AppTimeZoneToUtc());
                CheckTime((string)((ToolStripMenuItem)sender).Tag);
                DateRangeChanged();
            }
        }

        private static ManageInstances ManageInstancesForm;

        private void ManageInstancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ManageInstancesForm?.Close();
            ManageInstancesForm = new ManageInstances
            {
                Tags = string.Join(",", SelectedTags())
            };
            ManageInstancesForm.FormClosing += delegate
            {
                if (ManageInstancesForm.InstanceActiveFlagChanged || ManageInstancesForm.InstanceSummaryVisibleChanged)
                {
                    AddInstances(); // refresh the tree if instances deleted/restored
                    if (tabs.SelectedTab == tabSummary)
                    {
                        summary1.RefreshData();
                    }
                }

                ManageInstancesForm = null;
            };
            ManageInstancesForm.Show();
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

        private void BttnSearch_Click(object sender, EventArgs e)
        {
            AddInstances();
        }

        private static DBDiff DBDiffForm;

        private void DatabaseSchemaDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DBDiffForm?.Close();
            var n = tv1.SelectedSQLTreeItem();
            DBDiffForm = new DBDiff
            {
                SelectedTags = SelectedTags(),
                SelectedInstanceA = n.InstanceName,
                SelectedDatabaseA = new DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName }
            };
            DBDiffForm.ApplyTheme();
            DBDiffForm.FormClosed += delegate { DBDiffForm = null; };
            DBDiffForm.Show();
        }

        private static JobDiff JobDiffForm;

        private void AgentJobDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            JobDiffForm?.Close();
            var selected = tv1.SelectedSQLTreeItem();
            JobDiffForm = new()
            {
                InstanceID_A = selected.InstanceID
            };
            JobDiffForm.FormClosed += delegate { JobDiffForm = null; };
            JobDiffForm.ApplyTheme();
            JobDiffForm.Show();
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddInstances();
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
                    AddInstances();
                }

                ConfigureDisplayNameForm = null;
            };
            ConfigureDisplayNameForm.Show();
        }

        private void FreezeKeyColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.FreezeKeyColumn = freezeKeyColumnsToolStripMenuItem.Checked;
            LoadSelectedTab();
        }

        private void DateToolStripMenuItem_Opening(object sender, EventArgs e)
        {
            dateToolStripMenuItem.DropDownItems.Clear();
            for (var i = 0; i <= 14; i++)
            {
                var date = DateHelper.AppNow.AddDays(-i).Date;
                var daysAgo = i == 0 ? "Today" : i == 1 ? "Yesterday" : i + " days ago";
                var humanDateString = date.ToShortDateString() + " (" + date.DayOfWeek.ToString()[..3] + " - " +
                                      daysAgo + ")";
                var dateItem = new ToolStripMenuItem(humanDateString)
                {
                    Tag = date,
                    Checked = DateRange.FromUTC == date.AppTimeZoneToUtc() &&
                              DateRange.ToUTC == date.AddDays(1).AppTimeZoneToUtc()
                };
                dateItem.Click += DateItem_Click;
                dateToolStripMenuItem.DropDownItems.Add(dateItem);
            }
        }

        private void DateItem_Click(object sender, EventArgs e)
        {
            var dateItem = (ToolStripMenuItem)sender;
            var selectedDate = (DateTime)dateItem.Tag;
            DateRange.SetCustom(selectedDate.AppTimeZoneToUtc(), selectedDate.AppTimeZoneToUtc().AddDays(1));
            CheckTime("Date");
            tsTime.Text = dateItem.Text;
            DateRangeChanged();
        }

        // F5 to refresh
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
                        LoadSelectedTab();
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
            SaveContext(tv1.SelectedNode, tabs.SelectedIndex);
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

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        /// <summary>
        /// Navigate back.  If current control supports INavigation, invoke the NavigateBack for the control.  If move back was not performed on control, take user to previous selected node/tab in tree.
        /// </summary>
        private void NavigateBack()
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
                    NavigateBack();
                }
                else
                {
                    suppressLoadTab =
                        true; // Avoid potential double refresh when changing tree node then moving to different tab
                    ShowRefresh();
                    tv1.SelectedNode = context.Node;
                    tabs.SelectedIndex = context.TabIndex;
                    suppressLoadTab = false;
                    LoadSelectedTab(); // Need to refresh data.  In some cases it could be OK not to refresh - in others, data could be shown from the wrong context.
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
                        NavigateBack();
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
            var oldConnection = Common.RepositoryDBConnection;
            using var frm = new DBConnection() { ConnectionString = GetNewConnectionString() };
            frm.ShowDialog();
            if (frm.DialogResult != DialogResult.OK) return;
            try
            {
                var connection = new RepositoryConnection() { ConnectionString = frm.ConnectionString };
                connection.SetDefaultName();
                await SetConnection(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error switching to DBA Dash repository database:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                await SetConnection(oldConnection);
            }
        }

        private bool IsLoadingTimeZones = true;

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
                ShowRefresh();
                LoadSelectedTab();
                ShowRefresh(false);
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

        private void ShowHidden_Changed(object sender, EventArgs e)
        {
            Common.ShowHidden = showHiddenToolStripMenuItem.Checked;
            LoadSelectedTab();
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
            await SetConnection(connection);
            tsConnect.HideDropDown();
        }

        private async void TsAddConnection_Click(object sender, EventArgs e)
        {
            await AddConnection();
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

        private async Task AddConnection()
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
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            connection.Name = connection.GetDefaultName();
            repositories.Add(connection);
            LoadRepositoryConnections();
            repositories.Save();
            await SetConnection(connection);
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

        private void SetTheme(BaseTheme theme)
        {
            DBADashUser.SelectedTheme = theme;
            DBADashUser.Update();
            CheckTheme();
            this.ApplyTheme(theme);
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

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            Controls.ApplyTheme(theme);

            foreach (var tab in AllTabs)
            {
                tab.ApplyTheme();
            }
            pnlSearch.BackColor = theme.SearchBackColor;
            cboTimeZone.BackColor = theme.TimeZoneBackColor;
            cboTimeZone.ForeColor = theme.TimeZoneForeColor;
            cboTimeZone.FlatStyle = FlatStyle.Flat;
        }
    }
}