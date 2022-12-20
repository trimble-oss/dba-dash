using DBADash;
using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class Main : Form, IMessageFilter
    {
        public class InstanceSelectedEventArgs : EventArgs
        {
            public int InstanceID = -1;
            public string Instance;
            public string Tab;
        }

        private readonly CommandLineOptions commandLine;
        private readonly List<int> commandLineTags = new();

        public Main(CommandLineOptions opts)
        {
            Application.AddMessageFilter(this);
            this.FormClosed += (s, e) => Application.RemoveMessageFilter(this);
            InitializeComponent();
            commandLine = opts;
        }

        private readonly string jsonPath = Common.JsonConfigPath;

        private Int64 currentObjectID;
        private Int32 currentPage = 1;
        private Int32 currentPageSize = 100;
        private readonly DiffControl diffSchemaSnapshot = new();
        private bool suppressLoadTab = false;
        private bool CurrentTabSupportsDayOfWeekFilter { get => (new List<TabPage>() { tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabWaits }).Contains(tabs.SelectedTab); }
        private bool CurrentTabSupportsTimeOfDayFilter { get => (new List<TabPage>() { tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabWaits }).Contains(tabs.SelectedTab); }
        private bool GlobalTimeIsVisible { get => (new List<TabPage>() { tabPerformanceSummary, tabPerformance, tabSlowQueries, tabAzureDB, tabAzureSummary, tabPC, tabObjectExecutionSummary, tabWaits, tabRunningQueries, tabMemory, tabJobStats, tabJobTimeline }).Contains(tabs.SelectedTab); }

        private bool IsAzureOnly;
        private bool ShowCounts = false;
        private string GroupByTag = String.Empty;
        private readonly List<TreeContext> VisitedNodes = new();
        private bool suppressSaveContext = false;

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
                string searchString = String.Empty;
                if (txtSearch.Text.Trim().Length > 0)
                {
                    searchString = "%" + txtSearch.Text.Trim() + "%";
                }
                return searchString;
            }
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.SettingsUpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            lblVersion.Text = "Version: " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            tabs.TabPages.Clear();
            tabs.TabPages.Add(tabDBADash);
            Common.StyleGrid(ref gvHistory);
            dbOptions1.SummaryMode = true;
            splitSchemaSnapshot.Panel1.Controls.Add(diffSchemaSnapshot);
            diffSchemaSnapshot.Dock = DockStyle.Fill;

            string connectionString;
            if (System.IO.File.Exists(jsonPath))
            {
                string jsonConfig = System.IO.File.ReadAllText(jsonPath);
                var cfg = BasicConfig.Deserialize(jsonConfig);
                if (cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL)
                {
                    connectionString = cfg.DestinationConnection.ConnectionString;
                }
                else
                {
                    MessageBox.Show("This GUI client needs a connection to the repository database.  The destination connection type is " + cfg.DestinationConnection.Type.ToString() + ".  To configure the service, please use DBADashServiceConfigTool.exe or edit the ServiceConfig.json file.", "DBADashGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                    return;
                }
            }
            else
            {
                using (var frm = new ConnectionOptions())
                {
                    frm.ShowDialog();
                    if (frm.cfg == null)
                    {
                        Application.Exit();
                        return;
                    }
                    connectionString = frm.cfg.DestinationConnection.ConnectionString;
                }
            }
            try
            {
                await SetConnection(connectionString);
            }
            catch (Exception ex) when (ex.Message is "Version check" or "Error checking repository DB connection")
            {
                Application.Exit();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking repository DB version.  The application will close.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }
        }

        public async Task SetConnection(string connection)
        {
            var builder = new SqlConnectionStringBuilder(connection)
            {
                ApplicationName = "DBADashGUI"
            };
            Common.SetConnectionString(builder.ConnectionString);
            mnuTags.Visible = !commandLine.NoTagMenu;

            if (!CheckRepositoryDBConnection())
            {
                throw new Exception("Error checking repository DB connection");
            }

            await CheckVersion();

            DBADashUser.GetUser();
            GetCommandLineTags();
            GetTreeLayout();
            BuildTagMenu(commandLineTags);
            AddInstanes();
            AddTimeZoneMenus();
        }

        /// <summary>
        /// Check connection to DBA Dash repository DB.  User is prompted to retry or cancel on failure.
        /// </summary>
        /// <returns>True if connection succeeded</returns>
        private static bool CheckRepositoryDBConnection()
        {
            bool connectionCheckPassed = false;
            while (!connectionCheckPassed)
            {
                try
                {
                    using (var cn = new SqlConnection(Common.ConnectionString))
                    {
                        cn.Open();
                    }
                    connectionCheckPassed = true;
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show("Error connecting to repository database\n" + ex.Message, "Connection Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                    {
                        break;
                    }
                }
            }
            return connectionCheckPassed;
        }

        private static async Task CheckVersion()
        {
            var dbVersion = DBValidations.GetDBVersion(Common.ConnectionString);
            var appVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            var compare = (new Version(appVersion.Major, appVersion.Minor)).CompareTo(new Version(dbVersion.Major, dbVersion.Minor));

            if (compare < 0)
            {
                var promptUpgrade = MessageBox.Show(String.Format("The version of this GUI app ({0}.{1}) is OLDER than the repository database. Please upgrade to version {2}.{3}{4}Would you like to run the upgrade script now?", appVersion.Major, appVersion.Minor, dbVersion.Major, dbVersion.Minor, Environment.NewLine), "Upgrade GUI", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (promptUpgrade == DialogResult.Yes)
                {
                    string tag = dbVersion.ToString(3);
                    await Upgrade.UpgradeDBADashAsync(tag, true);
                    Application.Exit();
                }
                else if (promptUpgrade == DialogResult.No)
                {
                    throw new Exception("Version check");
                }
                else
                {
                    MessageBox.Show("The GUI might be unstable as it's not designed to run against this version of the repository database.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (compare > 0)
            {
                if (MessageBox.Show(String.Format("The version of this GUI app ({0}.{1}) is NEWER than the repository database ({2}.{3}). Please upgrade the repository database.", appVersion.Major, appVersion.Minor, dbVersion.Major, dbVersion.Minor), "Upgrade Agent", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    throw new Exception("Version check");
                }
            }
        }

        private void GetCommandLineTags()
        {
            if (commandLine.TagFilters != null && commandLine.TagFilters.Length > 0)
            {
                foreach (var t in DBADashTag.GetTags(commandLine.TagFilters))
                {
                    commandLineTags.Add(t.TagID);
                }
            }
        }

        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedTab();
        }

        private void LoadSelectedTab()
        {
            if (suppressLoadTab)
            {
                return;
            }
            SaveContext();

            SQLTreeItem n = tv1.SelectedSQLTreeItem();
            if (n == null)
            {
                return;
            }

            if (tabs.SelectedTab == tabDBADashErrorLog)
            {
                collectionErrors1.AckErrors = SelectedTags().Count == 0 && n.SQLTreeItemParent.Type == SQLTreeItem.TreeType.DBADashRoot;
            }

            if (tabs.SelectedTab == tabSchema)
            {
                GetHistory(n.ObjectID);
            }
            else
            {
                foreach (ISetContext ctrl in tabs.SelectedTab.Controls.OfType<ISetContext>())
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
            var mnuRootRefresh = new ToolStripMenuItem("Refresh");
            ctxMnu.Items.Add(mnuRootRefresh);
            mnuRootRefresh.Click += MnuRootRefresh_Click;
            return ctxMnu;
        }

        private void MnuRootRefresh_Click(object sender, EventArgs e)
        {
            AddInstanes();
        }

        private void AddInstanes()
        {
            VisitedNodes.Clear();
            tsBack.Enabled = false;
            tv1.Nodes.Clear();
            var root = new SQLTreeItem("DBA Dash", SQLTreeItem.TreeType.DBADashRoot) { ContextMenuStrip = RootRefreshContextMenu() };
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            var hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
            var checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
            root.Nodes.Add(changes);
            root.Nodes.Add(checks);
            root.Nodes.Add(hadr);
            SQLTreeItem parentNode = root;

            var tags = String.Join(",", SelectedTags());

            CommonData.UpdateInstancesList(tagIDs: tags, searchString: SearchString, groupByTag: GroupByTag);

            SQLTreeItem AzureNode = null;
            string currentTagGroup = string.Empty;
            foreach (DataRow row in CommonData.Instances.Rows)
            {
                string instance = (string)row["Instance"];
                string displayName = (string)row["InstanceDisplayName"];
                Int32 instanceID = (Int32)row["InstanceID"];
                bool showInSummary = (bool)row["ShowInSummary"];
                string tagGroup = Convert.ToString(row["TagGroup"]);

                if (currentTagGroup != tagGroup && !string.IsNullOrEmpty(tagGroup))
                {
                    parentNode = new SQLTreeItem(GroupByTag + ": " + tagGroup, SQLTreeItem.TreeType.InstanceFolder);
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
                    string db = (string)row["AzureDBName"];
                    if (AzureNode == null || AzureNode.InstanceName != instance)
                    {
                        AzureNode = new SQLTreeItem(instance, SQLTreeItem.TreeType.AzureInstance)
                        {
                            EngineEdition = edition
                        };
                        parentNode.Nodes.Add(AzureNode);
                        AzureNode.Nodes.Add(new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration));
                        AzureNode.Nodes.Add(new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks));
                        AzureNode.Nodes.Add(new SQLTreeItem("Tags", SQLTreeItem.TreeType.Tags));
                    }
                    var azureDBNode = new SQLTreeItem(db, SQLTreeItem.TreeType.AzureDatabase)
                    {
                        DatabaseID = (Int32)row["AzureDatabaseID"],
                        InstanceID = instanceID,
                        DatabaseName = db
                    };
                    azureDBNode.Nodes.Add(new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration));
                    azureDBNode.Nodes.Add(new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks));
                    azureDBNode.AddDatabaseFolders();
                    azureDBNode.EngineEdition = edition;
                    AzureNode.Nodes.Add(azureDBNode);
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
                    parentNode.Nodes.Add(n);
                }
            }
            if (ShowCounts) // Show count of instances for the grouping
            {
                foreach (SQLTreeItem n in root.Nodes.Cast<SQLTreeItem>().Where(t => t.Type == SQLTreeItem.TreeType.InstanceFolder))
                {
                    n.Text += "    {" + n.Nodes.Count + "}";
                }
            }
            IsAzureOnly = root.InstanceIDs.Count == root.AzureInstanceIDs.Count;
            if (IsAzureOnly)
            {
                root.Nodes.Remove(hadr);
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
                var jobItem = new SQLTreeItem((string)job["name"], SQLTreeItem.TreeType.AgentJob, attributes) { InstanceID = jobsNode.InstanceID, Tag = (Guid)job["job_id"] };
                jobItem.AddDummyNode();
                jobsNode.Nodes.Add(jobItem);
            }
        }

        private static void AddDatabases(SQLTreeItem instanceNode)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesByInstance_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                var changesNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration)
                {
                    InstanceID = instanceNode.InstanceID
                };
                instanceNode.Nodes.Add(changesNode);
                instanceNode.Nodes.Add(new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks));
                instanceNode.Nodes.Add(new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR));

                cmd.Parameters.AddWithValue("InstanceID", instanceNode.InstanceID);
                var systemNode = new SQLTreeItem("System Databases", SQLTreeItem.TreeType.Folder);
                instanceNode.Nodes.Add(systemNode);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string db = (string)rdr["name"];
                        string status = (string)rdr["Status"];
                        string nodeName = string.IsNullOrEmpty(status) ? db : db + " (" + status + ")";

                        var n = new SQLTreeItem(nodeName, SQLTreeItem.TreeType.Database)
                        {
                            DatabaseID = (Int32)rdr["DatabaseID"],
                            InstanceID = (Int32)rdr["InstanceID"],
                            DatabaseName = db
                        };
                        if (rdr["ObjectID"] != DBNull.Value)
                        {
                            n.ObjectID = (Int64)rdr["ObjectID"];
                        }
                        n.AddDatabaseFolders();
                        if ((new string[] { "master", "model", "msdb", "tempdb" }).Contains((string)rdr[1]))
                        {
                            systemNode.Nodes.Add(n);
                        }
                        else
                        {
                            instanceNode.Nodes.Add(n);
                        }
                    }
                }
                var jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs) { InstanceID = instanceNode.InstanceID };
                jobs.AddDummyNode();
                instanceNode.Nodes.Add(jobs);
            }
        }

        private static void ExpandObjects(SQLTreeItem n)
        {
            DataTable dbobj = CommonData.GetDBObjects(n.DatabaseID, (string)n.Tag);
            foreach (DataRow r in dbobj.Rows)
            {
                string type = ((string)r[1]).Trim();
                var objN = new SQLTreeItem((string)r[3], (string)r[2], type)
                {
                    ObjectID = (Int64)r[0]
                };
                n.Nodes.Add(objN);
            }
        }

        private List<TabPage> GetAllowedTabs()
        {
            List<TabPage> allowedTabs = new();
            var n = tv1.SelectedSQLTreeItem();
            var parent = n.SQLTreeItemParent;
            bool hasAzureDBs = n.AzureInstanceIDs.Count > 0;
            if (n.Type is SQLTreeItem.TreeType.DBADashRoot or SQLTreeItem.TreeType.InstanceFolder)
            {
                allowedTabs.AddRange(new TabPage[] { tabSummary, tabPerformanceSummary });
                if (hasAzureDBs)
                {
                    allowedTabs.Add(tabAzureSummary);
                }
                allowedTabs.AddRange(new TabPage[] { tabSlowQueries, tabRunningQueries });
            }
            else if (n.Type == SQLTreeItem.TreeType.Database)
            {
                allowedTabs.AddRange(new TabPage[] { tabPerformance, tabObjectExecutionSummary, tabSlowQueries, tabFiles, tabSnapshotsSummary, tabDBSpace, tabDBConfiguration, tabDBOptions });
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureDatabase)
            {
                allowedTabs.AddRange(new TabPage[] { tabPerformance, tabAzureSummary, tabAzureDB, tabPC, tabSlowQueries, tabObjectExecutionSummary, tabWaits, tabRunningQueries });
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.AddRange(new TabPage[] { tabPerformanceSummary, tabPerformance, tabPC, tabObjectExecutionSummary, tabSlowQueries, tabWaits, tabRunningQueries, tabMemory });
            }
            else if (n.Type == SQLTreeItem.TreeType.AzureInstance)
            {
                allowedTabs.AddRange(new TabPage[] { tabPerformanceSummary, tabAzureSummary, tabSlowQueries, tabObjectExecutionSummary, tabRunningQueries });
            }
            else if (n.Type == SQLTreeItem.TreeType.Configuration)
            {
                if (parent.Type == SQLTreeItem.TreeType.Instance)
                {
                    allowedTabs.Add(tabInfo);
                }
                if (parent.Type != SQLTreeItem.TreeType.AzureDatabase && parent.Type != SQLTreeItem.TreeType.AzureInstance && !IsAzureOnly)
                {
                    allowedTabs.AddRange(new TabPage[] { tabInstanceConfig, tabTraceFlags, tabHardware, tabSQLPatching, tabAlerts, tabDrivers, tabTempDB, tabRG });
                }
                if (parent.Type != SQLTreeItem.TreeType.Instance && hasAzureDBs)
                {
                    allowedTabs.AddRange(new TabPage[] { tabServiceObjectives, tabAzureDBesourceGovernance });
                }
                allowedTabs.AddRange(new TabPage[] { tabDBConfiguration, tabDBOptions, tabQS, tabTags });
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJobs)
            {
                allowedTabs.AddRange(new TabPage[] { tabJobs, tabJobStats, tabJobTimeline });
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJob)
            {
                allowedTabs.AddRange(new TabPage[] { tabJobs, tabJobDDL, tabJobStats, tabJobTimeline });
            }
            else if (n.Type == SQLTreeItem.TreeType.HADR)
            {
                allowedTabs.AddRange(new TabPage[] { tabBackups, tabLogShipping, tabMirroring, tabAG });
            }
            else if (n.Type == SQLTreeItem.TreeType.DBAChecks)
            {
                allowedTabs.Add(tabSummary);
                if (!IsAzureOnly && parent.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.AddRange(new TabPage[] { tabDrives, tabJobs, tabLastGood, tabOSLoadedModules });
                }
                allowedTabs.AddRange(new TabPage[] { tabFiles, tabDBADashErrorLog, tabCollectionDates, tabDBSpace, tabCustomChecks, tabSnapshotsSummary, tabIdentityColumns });
            }
            else if (n.Type == SQLTreeItem.TreeType.Tags)
            {
                allowedTabs.Add(tabTags);
            }
            else if (n.Type == SQLTreeItem.TreeType.AgentJobStep)
            {
                allowedTabs.AddRange(new TabPage[] { tabJobs, tabJobStats });
            }

            if (n.ObjectID > 0)
            {
                if (n.Type is SQLTreeItem.TreeType.StoredProcedure or SQLTreeItem.TreeType.CLRProcedure or SQLTreeItem.TreeType.ScalarFunction or SQLTreeItem.TreeType.CLRScalarFunction or SQLTreeItem.TreeType.Trigger or SQLTreeItem.TreeType.CLRTrigger)
                {
                    allowedTabs.AddRange(new TabPage[] { tabObjectExecutionSummary, tabPerformance });
                }
                allowedTabs.Add(tabSchema);
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

            List<TabPage> allowedTabs = GetAllowedTabs();

            this.Text = n.FullPath.Replace("\\", " \\ ");

            // Check if Tab pages match tabs currently loaded
            bool validatedTabs = true;
            if (allowedTabs.Count == tabs.TabPages.Count)
            {
                validatedTabs = !tabs.TabPages.Cast<TabPage>().Any(t => !allowedTabs.Contains(t));
            }
            else
            {
                validatedTabs = false;
            }
            // If tab pages don't match, clear tabs and reload
            if (!validatedTabs)
            {
                tabs.TabPages.Clear();
                tabs.TabPages.AddRange(allowedTabs.ToArray());
            }
            suppressLoadTab = suppress; // Return tab load supression back to previous value
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
                    AddDatabases(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.AgentJobs)
                {
                    ExpandJobs(n);
                }
                else if (n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    ExpandJobSteps(n);
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

                n.Nodes.Add(new SQLTreeItem(stepName, SQLTreeItem.TreeType.AgentJobStep, attributes) { InstanceID = n.InstanceID, Tag = stepID });
            }
        }

        #endregion Tree

        #region SchemaSnapshots

        private void LoadDDL(Int64 DDLID, Int64 DDLIDOld)
        {
            string newText = Common.DDL(DDLID);
            string oldText = Common.DDL(DDLIDOld);
            diffSchemaSnapshot.OldText = oldText;
            diffSchemaSnapshot.NewText = newText;
        }

        private void GetHistory(Int64 ObjectID, Int32 PageNum = 1)
        {
            diffSchemaSnapshot.OldText = "";
            diffSchemaSnapshot.NewText = "";
            currentPageSize = Int32.Parse(tsPageSize.Text);
            DataTable dt = CommonData.GetDDLHistoryForObject(ObjectID, PageNum, currentPageSize);

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
                Int64 ddlID;
                Int64 ddlIDOld;
                if (row["DDLID"] == DBNull.Value)
                {
                    ddlID = -1;
                }
                else
                {
                    ddlID = (Int64)row["DDLID"];
                }
                if (row["DDLIDOld"] == DBNull.Value)
                {
                    ddlIDOld = -1;
                }
                else
                {
                    ddlIDOld = (Int64)row["DDLIDOld"];
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
            if (Int32.Parse(tsPageSize.Text) != currentPageSize)
            {
                GetHistory(currentObjectID, 1);
            }
        }

        private void TsPageSize_Validating(object sender, CancelEventArgs e)
        {
            _ = int.TryParse(tsPageSize.Text, out int i);
            if (i <= 0)
            {
                tsPageSize.Text = currentPageSize.ToString();
            }
        }

        #endregion SchemaSnapshots

        #region Tagging

        private bool isClearTags = false;

        private void BuildTagMenu(List<int> selected = null)
        {
            mnuTags.DropDownItems.Clear();

            string currentTag = String.Empty;
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

                    if (tag.TagName.StartsWith("{"))
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

            //SetFont(mnuTags);

            BuildGroupByTagMenu(ref tags);
        }

        private void BuildGroupByTagMenu(ref List<DBADashTag> tags)
        {
            groupToolStripMenuItem.DropDownItems.Clear();
            IEnumerable<string> tagNames = tags.Select(t => t.TagName).Distinct();
            ToolStripMenuItem mSystemTags = new("System Tags") { Font = new Font(groupToolStripMenuItem.Font, FontStyle.Italic) };
            ToolStripMenuItem mNone = new("[None]") { Tag = string.Empty };
            ToolStripMenuItem mShowCounts = new("Show Counts") { CheckOnClick = true, Checked = ShowCounts };
            ToolStripMenuItem mSave = new("Save") { Image = Properties.Resources.Save_16x };
            mNone.Click += GroupByTag_Click;
            mShowCounts.Click += ShowCounts_Click;
            mSave.Click += SaveTreeLayout;
            foreach (string tag in tagNames)
            {
                ToolStripMenuItem mnu = new(tag) { Tag = tag };
                mnu.Click += GroupByTag_Click;
                if (tag.StartsWith("{"))
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
            TreeSavedView treeView = new() { GroupByTag = GroupByTag, ShowCounts = ShowCounts, Name = SavedView.DefaultViewName };
            treeView.Save();
            MessageBox.Show("Tree layout saved", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetTreeLayout()
        {
            GroupByTag = string.Empty;
            try
            {
                var savedView = TreeSavedView.GetSavedViews(SavedView.ViewTypes.Tree, DBADashUser.UserID).Where(sv => sv.Key == SavedView.DefaultViewName);
                if (savedView.Count() == 1)
                {
                    TreeSavedView treeView = TreeSavedView.Deserialize(savedView.First().Value);
                    GroupByTag = treeView.GroupByTag;
                    ShowCounts = treeView.ShowCounts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error loading tree layout\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowCounts_Click(object sender, EventArgs e)
        {
            ShowCounts = ((ToolStripMenuItem)sender).Checked;
            AddInstanes();
        }

        private void GroupByTag_Click(object sender, EventArgs e)
        {
            GroupByTag = (string)((ToolStripMenuItem)sender).Tag;
            AddInstanes();
        }

        private void RefreshTag_Click(object sender, EventArgs e)
        {
            BuildTagMenu();
        }

        private void ClearTag_Click(object sender, EventArgs e)
        {
            isClearTags = true;
            mnuTags.Font = Font = new Font(mnuTags.Font, mnuTags.Font.Style & ~FontStyle.Bold);
            ClearTags(mnuTags.DropDownItems);
            isClearTags = false;
            AddInstanes();
            this.Font = new Font(this.Font, FontStyle.Regular);
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

        private List<int> SelectedTags(ToolStripItemCollection items)
        {
            var selected = new List<int>();
            foreach (ToolStripItem mnuTag in items)
            {
                if (mnuTag.GetType() == typeof(ToolStripMenuItem))
                {
                    if (((ToolStripMenuItem)mnuTag).Checked)
                    {
                        selected.Add((int)mnuTag.Tag);
                    }
                    if (((ToolStripMenuItem)mnuTag).DropDownItems.Count > 0)
                    {
                        selected.AddRange(SelectedTags(((ToolStripMenuItem)mnuTag).DropDownItems));
                    }
                }
            }
            return selected;
        }

        private void MTagValue_CheckedChanged(object sender, EventArgs e)
        {
            if (!isClearTags)
            {
                AddInstanes();
                var mnuTag = (ToolStripMenuItem)sender;
                while (mnuTag.OwnerItem is not null and ToolStripMenuItem)
                {
                    mnuTag = (ToolStripMenuItem)mnuTag.OwnerItem;
                }
                SetFont(mnuTag);
            }
        }

        private void SetFont(ToolStripMenuItem mnu)
        {
            if (mnu.Checked)
            {
                mnu.Font = new Font(mnu.Font, mnu.Font.Style | FontStyle.Bold);
            }
            else if (SelectedTags(mnu.DropDownItems).Count > 0)
            {
                mnu.Font = new Font(mnu.Font, mnu.Font.Style | FontStyle.Bold);
            }
            else
            {
                mnu.Font = new Font(mnu.Font, mnu.Font.Style & ~FontStyle.Bold);
            }
            foreach (ToolStripItem itm in mnu.DropDownItems)
            {
                if (itm.GetType() == typeof(ToolStripMenuItem))
                {
                    SetFont((ToolStripMenuItem)itm);
                }
            }
        }

        #endregion Tagging

        private void DataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using DataRetention frm = new();
            frm.ShowDialog();
        }

        private void Instance_Selected(object sender, InstanceSelectedEventArgs e)
        {
            var root = tv1.SelectedSQLTreeItem();

            SQLTreeItem nInstance;

            if (e.InstanceID <= 0 && string.IsNullOrEmpty(e.Instance)) // No Instance - Use root Level
            {
                nInstance = tv1.Nodes[0].AsSQLTreeItem();
            }
            else
            {
                nInstance = e.InstanceID > 0 ? root.FindInstance(e.InstanceID) : root.FindInstance(e.Instance);
            }

            if (nInstance == null)
            {
                MessageBox.Show("Instance not found: " + (e.InstanceID > 0 ? e.InstanceID.ToString() : e.Instance), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                suppressLoadTab = true;
                try
                {
                    var parent = nInstance.Parent;
                    if (parent != null && !parent.IsExpanded)
                    {
                        parent.Expand();
                    }
                    if (e.Tab is "tabAlerts" or "tabQS") // Configuration Node
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.Nodes[0];
                    }
                    else if (e.Tab is "tabMirroring" or "tabLogShipping" or "tabBackups" or "tabAG")
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.Nodes[2];
                    }
                    else if (e.Tab == "tabJobs" && parent == null) // Root Level (Jobs Uner Checks)
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.Nodes[1];
                    }
                    else if (e.Tab == "tabJobs" && parent != null) // Instance Level Jobs tab
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.LastNode;
                    }
                    else if (e.Tab is "tabAzureSummary" or "tabPerformance")
                    {
                        tv1.SelectedNode = nInstance;
                    }
                    else
                    {
                        nInstance.Expand();
                        tv1.SelectedNode = nInstance.Nodes[1];
                    }

                    if (e.Tab != null && e.Tab.Length > 0)
                    {
                        if (tabs.TabPages.ContainsKey(e.Tab))
                        {
                            tabs.SelectedTab = tabs.TabPages[e.Tab];
                        }
                        else
                        {
                            NavigateBack();
                            MessageBox.Show("Selected tab page is not available for this context", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error selecting instance: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string tag = (string)((ToolStripMenuItem)sender).Tag;
            CheckTime(tag);
            Performance.DateRange.SetMins(Convert.ToInt32(tag));
            DateRangeChanged();
        }

        private void CheckTime(string tag)
        {
            if (tag != "60")
            {
                tsTime.Font = new Font(tsTime.Font, FontStyle.Bold);
            }
            else
            {
                tsTime.Font = new Font(tsTime.Font, FontStyle.Regular);
            }
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
            tsDayOfWeek.Visible = DateRange.CurrentDateRangeSupportsDayOfWeekFilter && CurrentTabSupportsDayOfWeekFilter && GlobalTimeIsVisible;
            tsTimeFilter.Visible = DateRange.CurrentDateRangeSupportsTimeOfDayFilter && CurrentTabSupportsTimeOfDayFilter && GlobalTimeIsVisible;
            tsDayOfWeek.Font = new Font(tsDayOfWeek.Font, DateRange.HasDayOfWeekFilter ? FontStyle.Bold : FontStyle.Regular);
            tsTimeFilter.Font = new Font(tsTimeFilter.Font, DateRange.HasTimeOfDayFilter ? FontStyle.Bold : FontStyle.Regular);
        }

        private void DateRangeChanged()
        {
            UpdateTimeFilterVisibility();

            foreach (IRefreshData ctrl in tabs.SelectedTab.Controls.OfType<IRefreshData>())
            {
                ctrl.RefreshData();
            }
        }

        private void TsCustomTime_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker() { FromDate = DateRange.FromUTC.ToAppTimeZone(), ToDate = DateRange.ToUTC.ToAppTimeZone() };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                DateRange.SetCustom(frm.FromDate.AppTimeZoneToUtc(), frm.ToDate.AppTimeZoneToUtc());
                CheckTime((string)((ToolStripMenuItem)sender).Tag);
                DateRangeChanged();
            }
        }

        private void ManageInstancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new ManageInstances
            {
                Tags = String.Join(",", SelectedTags())
            };
            frm.ShowDialog();
            if (frm.InstanceActiveFlagChanged || frm.InstanceSummaryVisibleChanged)
            {
                AddInstanes(); // refresh the tree if instances deleted/restored
                if (tabs.SelectedTab == tabSummary)
                {
                    summary1.RefreshData();
                }
            }
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
            AddInstanes();
        }

        private void DatabaseSchemaDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new DBDiff { SelectedTags = SelectedTags() })
            {
                var n = tv1.SelectedSQLTreeItem();
                frm.SelectedInstanceA = n.InstanceName;
                frm.SelectedDatabaseA = new DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName };
                frm.ShowDialog(this);
            }
        }

        private void AgentJobDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new JobDiff())
            {
                var selected = tv1.SelectedSQLTreeItem();
                frm.InstanceID_A = selected.InstanceID;
                frm.ShowDialog(this);
            }
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddInstanes();
            }
        }

        private void ConfigureDisplayNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new ConfigureDisplayName() { TagIDs = String.Join(",", SelectedTags()), SearchString = SearchString })
            {
                frm.ShowDialog(this);
                if (frm.EditCount > 0)
                {
                    AddInstanes();
                }
            }
        }

        private void FreezeKeyColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.FreezeKeyColumn = freezeKeyColumnsToolStripMenuItem.Checked;
            LoadSelectedTab();
        }

        private void DateToolStripMenuItem_Opening(object sender, EventArgs e)
        {
            dateToolStripMenuItem.DropDownItems.Clear();
            for (int i = 0; i <= 14; i++)
            {
                var date = DateHelper.AppNow.AddDays(-i).Date;
                var daysAgo = i == 0 ? "Today" : i == 1 ? "Yesterday" : i.ToString() + " days ago";
                var humanDateString = date.ToShortDateString() + " (" + date.DayOfWeek.ToString()[..3] + " - " + daysAgo + ")";
                var dateItem = new ToolStripMenuItem(humanDateString) { Tag = date, Checked = DateRange.FromUTC == date.AppTimeZoneToUtc() && DateRange.ToUTC == date.AddDays(1).AppTimeZoneToUtc() };
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
            bool bHandled = false;
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
            if (node != null && TabIndex >= 0 & !suppressSaveContext) // Save context if we have a current node/tab and save context is not supressed (e.g. When navigating back)
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
                    if (node == tv1.Nodes[0] && TabIndex == 0) // If context we are saving is root, we don't need to add it again
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
                suppressSaveContext = true; // Don't save the context change when moving back.  Otherwise we'd flip flop between two contexts.
                var context = VisitedNodes[^1];
                VisitedNodes.RemoveAt(VisitedNodes.Count - 1);
                if (context.Node == tv1.SelectedNode && context.TabIndex == tabs.SelectedIndex) // If move back location is set to current location, navigate to the next saved location
                {
                    NavigateBack();
                }
                else
                {
                    suppressLoadTab = true; // Avoid potential double refresh when changing tree node then moving to different tab
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
                int lowWord = (m.WParam.ToInt32() << 16) >> 16;
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
            string oldConnection = Common.ConnectionString;
            using (var frm = new DBConnection() { ConnectionString = Common.ConnectionString })
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    try
                    {
                        await SetConnection(frm.ConnectionString);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error switching to DBA Dash repository database:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        await SetConnection(oldConnection);
                    }
                }
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
            if (MessageBox.Show("Save time zone: " + DateHelper.AppTimeZone.DisplayName, "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DBADashUser.SetUserTimeZone(DateHelper.AppTimeZone);
                MessageBox.Show($"Time zone will be set to {DateHelper.AppTimeZone.DisplayName} on application start.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowHidden_Changed(object sender, EventArgs e)
        {
            Common.ShowHidden = showHiddenToolStripMenuItem.Checked;
            LoadSelectedTab();
        }
    }
}