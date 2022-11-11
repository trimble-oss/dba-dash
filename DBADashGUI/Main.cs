using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DiffControl;
using System.Diagnostics;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using DBADashSharedGUI;
using DBADashGUI.Performance;
using System.Runtime.CompilerServices;

namespace DBADashGUI
{

    public partial class Main : Form, IMessageFilter
    {

        public class InstanceSelectedEventArgs : EventArgs
        {
            public int InstanceID=-1;
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

        readonly string jsonPath = Common.JsonConfigPath;


        private Int64 currentObjectID;
        private Int32 currentPage = 1;
        private Int32 currentPageSize = 100;
        private readonly DiffControl diffSchemaSnapshot = new();
        private bool suppressLoadTab=false;
        private bool currentTabSupportsDayOfWeekFilter;
        private bool currentTabSupportsTimeOfDayFilter;
        private bool globalTimeIsVisible;
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
        private const int MK_XBUTTON2 = 0x0040;

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
                var cfg= BasicConfig.Deserialize(jsonConfig);
                if(cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL)
                {
                    connectionString = cfg.DestinationConnection.ConnectionString;                
                }
                else
                {
                    MessageBox.Show("This GUI client needs a connection to the repository database.  The destination connection type is " + cfg.DestinationConnection.Type.ToString() + ".  To configure the service, please use DBADashServiceConfigTool.exe or edit the ServiceConfig.json file.","DBADashGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                    return;
                }        
            }
            else 
            {
                using(var frm = new ConnectionOptions())
                {
                    frm.ShowDialog();
                    if(frm.cfg == null)
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
            catch (Exception ex) when (ex.Message == "Version check" || ex.Message == "Error checking repository DB connection")
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
        }


        /// <summary>
        /// Check connection to DBA Dash repository DB.  User is prompted to retry or cancel on failure.
        /// </summary>
        /// <returns>True if connection succeeded</returns>
        private bool CheckRepositoryDBConnection()
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

        private async Task CheckVersion()
        {
            var dbVersion = DBValidations.GetDBVersion(Common.ConnectionString);
            var appVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            var compare = (new Version(appVersion.Major, appVersion.Minor)).CompareTo(new Version(dbVersion.Major, dbVersion.Minor));

            if (compare < 0)
            {
                var promptUpgrade = MessageBox.Show(String.Format("The version of this GUI app ({0}.{1}) is OLDER than the repository database. Please upgrade to version {2}.{3}{4}Would you like to run the upgrade script now?", appVersion.Major, appVersion.Minor, dbVersion.Major, dbVersion.Minor, Environment.NewLine), "Upgrade GUI", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (promptUpgrade  == DialogResult.Yes)
                {
                    string tag =  dbVersion.ToString(3);
                    await Upgrade.UpgradeDBADashAsync(tag,true);
                    Application.Exit();
                }      
                else if( promptUpgrade== DialogResult.No)
                {
                    throw new Exception("Version check");
                }
                else
                {
                    MessageBox.Show("The GUI might be unstable as it's not designed to run against this version of the repository database.","WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if ( compare>0)
            {
                if(MessageBox.Show(String.Format("The version of this GUI app ({0}.{1}) is NEWER than the repository database ({2}.{3}). Please upgrade the repository database.", appVersion.Major, appVersion.Minor, dbVersion.Major, dbVersion.Minor), "Upgrade Agent", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    throw new Exception("Version check");
                }
            }
            
        }

        private void GetCommandLineTags()
        {
            if (commandLine.TagFilters !=null && commandLine.TagFilters.Length > 0)
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
            globalTimeIsVisible = false;
            currentTabSupportsDayOfWeekFilter = false;
            currentTabSupportsTimeOfDayFilter = false;
      
            var n = (SQLTreeItem) tv1.SelectedNode;
            if(n == null)
            {
                return;
            }

            var parent = (SQLTreeItem)n.Parent;

            if (tabs.SelectedTab == tabTags)
            {
                tags1.InstanceName = n.InstanceName;
                tags1.InstanceID = n.InstanceID;
                tags1.InstanceIDs = n.InstanceIDs.ToList();
                tags1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDrives)
            {
                drivesControl1.InstanceIDs = n.RegularInstanceIDs.ToList();
                drivesControl1.IncludeNA = n.RegularInstanceIDs.Count == 1;
                drivesControl1.IncludeOK = n.RegularInstanceIDs.Count == 1;
                drivesControl1.IncludeWarning = true;
                drivesControl1.IncludeCritical = true;
                drivesControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabBackups)
            {
                backupsControl1.InstanceIDs = n.RegularInstanceIDs.ToList();    
                backupsControl1.IncludeNA = n.RegularInstanceIDs.Count==1;
                backupsControl1.IncludeOK = n.RegularInstanceIDs.Count==1;
                backupsControl1.IncludeWarning = true;
                backupsControl1.IncludeCritical = true;
                backupsControl1.RefreshBackups();
            }
            else if(tabs.SelectedTab== tabLogShipping)
            {
                logShippingControl1.IncludeNA = n.RegularInstanceIDs.Count==1;
                logShippingControl1.IncludeOK = n.RegularInstanceIDs.Count==1;
                logShippingControl1.IncludeWarning = true;
                logShippingControl1.IncludeCritical = true;
                logShippingControl1.InstanceIDs = n.RegularInstanceIDs.ToList();
                logShippingControl1.RefreshData();
            }
            else if(tabs.SelectedTab== tabJobs)
            {
                agentJobsControl1.StepID = -1;
                if (n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    agentJobsControl1.JobID = (Guid)n.Tag;
                }
                else if(n.Type == SQLTreeItem.TreeType.AgentJobStep)
                {
                    agentJobsControl1.JobID = (Guid)n.Parent.Tag;
                    agentJobsControl1.StepID = (int)n.Tag;
                }
                else
                {
                    agentJobsControl1.JobID = Guid.Empty;
                }
                agentJobsControl1.IncludeNA = n.RegularInstanceIDs.Count == 1;
                agentJobsControl1.IncludeOK = n.RegularInstanceIDs.Count == 1;
                agentJobsControl1.IncludeWarning = true;
                agentJobsControl1.IncludeCritical = true;
                agentJobsControl1.InstanceIDs = n.RegularInstanceIDs.ToList();
                agentJobsControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabSummary)
            {
                summary1.InstanceIDs = n.InstanceIDs.ToList();
                summary1.RefreshDataIfStale();
            }
            else if(tabs.SelectedTab == tabFiles)
            {
                dbFilesControl1.InstanceIDs = n.InstanceIDs.ToList();
                dbFilesControl1.DatabaseID = (n.DatabaseID > 0 ? (Int32?)n.DatabaseID : null);
                dbFilesControl1.IncludeCritical = true;
                dbFilesControl1.IncludeWarning = true;
                dbFilesControl1.IncludeNA = dbFilesControl1.DatabaseID != null;
                dbFilesControl1.IncludeOK = dbFilesControl1.DatabaseID != null;
                dbFilesControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabSnapshotsSummary)
            {
                schemaSnapshots1.InstanceID = n.InstanceID;
                schemaSnapshots1.InstanceName = n.InstanceName;
                schemaSnapshots1.DatabaseID = n.DatabaseID;
                schemaSnapshots1.InstanceIDs = n.InstanceIDs.ToList();
                schemaSnapshots1.RefreshData();
            }
            else if ( tabs.SelectedTab == tabSchema)
            {
                GetHistory(n.ObjectID);
            }
            else if(tabs.SelectedTab == tabLastGood)
            {
                lastGoodCheckDBControl1.InstanceIDs = n.RegularInstanceIDs.ToList();
                lastGoodCheckDBControl1.IncludeCritical = true;
                lastGoodCheckDBControl1.IncludeWarning = true;
                lastGoodCheckDBControl1.IncludeNA = n.InstanceID>0;
                lastGoodCheckDBControl1.IncludeOK = n.InstanceID > 0;
                lastGoodCheckDBControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabPerformance)
            {
                globalTimeIsVisible = true;
                currentTabSupportsDayOfWeekFilter = true;
                currentTabSupportsTimeOfDayFilter = true;
                performance1.InstanceID = n.InstanceID;
                performance1.DatabaseID = n.DatabaseID;
                if (n.Type == SQLTreeItem.TreeType.Database || n.Type == SQLTreeItem.TreeType.Instance)
                {
                    performance1.ObjectID = 0;
               
                }
                else
                {
                    performance1.ObjectID = n.ObjectID;
                }
                performance1.RefreshData();
            }
            else if(tabs.SelectedTab== tabDBADashErrorLog)
            {
                collectionErrors1.InstanceID = n.InstanceID;
                collectionErrors1.InstanceGroupName = n.InstanceName;
                collectionErrors1.Days = 1;
                collectionErrors1.AckErrors =  SelectedTags().Count == 0 && parent.Type == SQLTreeItem.TreeType.DBADashRoot;
                collectionErrors1.RefreshData();
            }
            else if(tabs.SelectedTab== tabCollectionDates)
            {
                collectionDates1.InstanceIDs = n.InstanceIDs.ToList();
                collectionDates1.IncludeCritical = true;
                collectionDates1.IncludeWarning = true;
                collectionDates1.IncludeNA = n.InstanceID > 0;
                collectionDates1.IncludeOK = n.InstanceID > 0;
                collectionDates1.RefreshData();

            }
            else if(tabs.SelectedTab== tabPerformanceSummary)
            {
                globalTimeIsVisible = true;
                currentTabSupportsDayOfWeekFilter = true;
                currentTabSupportsTimeOfDayFilter = true;
                if (n.Type == SQLTreeItem.TreeType.DBADashRoot)
                {
                    performanceSummary1.TagIDs = String.Join(",", SelectedTags());
                    performanceSummary1.InstanceIDs = new List<int>();
                }
                else
                {
                    performanceSummary1.InstanceIDs = n.InstanceIDs.ToList();                
                    performanceSummary1.TagIDs = "";
                }
                performanceSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabInfo)
            {
                info1.InstanceID = n.InstanceID;
                info1.RefreshData();
            }
            else if(tabs.SelectedTab== tabHardware)
            {
                hardwareChanges1.InstanceIDs = n.RegularInstanceIDs.ToList();
                hardwareChanges1.RefreshData();
            }
            else if (tabs.SelectedTab == tabSQLPatching)
            {
                sqlPatching1.InstanceIDs = n.RegularInstanceIDs.ToList();
                sqlPatching1.RefreshData();
            }
            else if(tabs.SelectedTab == tabInstanceConfig)
            {                
                configurationHistory1.InstanceIDs = n.RegularInstanceIDs.ToList();
                configurationHistory1.RefreshData();
            }
            else if(tabs.SelectedTab== tabSlowQueries)
            {
                globalTimeIsVisible = true;
                slowQueries1.InstanceIDs = n.InstanceIDs.ToList();
                if(n.Type== SQLTreeItem.TreeType.Database)
                {
                    slowQueries1.DBName = n.DatabaseName;
                }
                else
                {
                    slowQueries1.DBName = "";
                }
                slowQueries1.ResetFilters();
                slowQueries1.RefreshData();
            }
            else if(tabs.SelectedTab== tabTraceFlags)
            {
                traceFlagHistory1.InstanceIDs = n.RegularInstanceIDs.ToList();
                traceFlagHistory1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAlerts)
            {
                alerts1.InstanceIDs = n.RegularInstanceIDs.ToList();
                alerts1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDrivers)
            {
                drivers1.InstanceIDs = n.RegularInstanceIDs.ToList();
                drivers1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDBSpace)
            {
                spaceTracking1.InstanceIDs = n.InstanceIDs.ToList();
                spaceTracking1.DatabaseID = n.DatabaseID;             
                spaceTracking1.InstanceGroupName = n.InstanceName;              
                spaceTracking1.DBName = n.DatabaseName;
                spaceTracking1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAzureSummary)
            {
                globalTimeIsVisible = true;
                azureSummary1.InstanceIDs =n.AzureInstanceIDs.ToList();
                azureSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabAzureDB)
            {
                globalTimeIsVisible = true;
                azureDBResourceStats1.InstanceID = n.InstanceID;
                azureDBResourceStats1.RefreshData();
            }
            else if(tabs.SelectedTab == tabServiceObjectives)
            {
                azureServiceObjectivesHistory1.InstanceIDs = n.AzureInstanceIDs.ToList();
                azureServiceObjectivesHistory1.RefreshData();
            }
            else if (tabs.SelectedTab == tabAzureDBesourceGovernance)
            {
                azureDBResourceGovernance1.InstanceIDs = n.AzureInstanceIDs.ToList();
                azureDBResourceGovernance1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDBConfiguration)
            {
                dbConfiguration1.InstanceIDs = n.AzureInstanceIDs.ToList();
                dbConfiguration1.DatabaseID = n.DatabaseID;
                dbConfiguration1.RefreshData();
            }
            else if(tabs.SelectedTab == tabDBOptions)
            {
                dbOptions1.InstanceIDs = n.AzureInstanceIDs.ToList();
                dbOptions1.DatabaseID = n.DatabaseID;
                dbOptions1.RefreshData();
            }
            else if(tabs.SelectedTab == tabTempDB)
            {
                tempDBConfig1.InstanceIDs = n.RegularInstanceIDs.ToList();
                tempDBConfig1.RefreshData();
            }
            else if (tabs.SelectedTab == tabCustomChecks)
            {
               customChecks1.InstanceIDs = n.InstanceIDs.ToList();
               customChecks1.IncludeCritical = true;
               customChecks1.IncludeWarning = true;
               customChecks1.IncludeNA = n.InstanceID > 0 || n.Type == SQLTreeItem.TreeType.AzureInstance;
               customChecks1.IncludeOK = n.InstanceID > 0 || n.Type == SQLTreeItem.TreeType.AzureInstance;
               customChecks1.RefreshData();
            }
            else if (tabs.SelectedTab == tabPC)
            {
                globalTimeIsVisible = true;
                currentTabSupportsDayOfWeekFilter = true;
                currentTabSupportsTimeOfDayFilter = true;
                performanceCounterSummary1.InstanceID = n.InstanceID;
                performanceCounterSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabObjectExecutionSummary)
            {
                globalTimeIsVisible = true;
                currentTabSupportsDayOfWeekFilter = true;
                currentTabSupportsTimeOfDayFilter = true;
                objectExecutionSummary1.Instance = n.InstanceName;
                objectExecutionSummary1.InstanceID = n.InstanceID;
                objectExecutionSummary1.DatabaseID = n.DatabaseID;
                objectExecutionSummary1.ObjectID = (n.Type == SQLTreeItem.TreeType.Database || n.Type == SQLTreeItem.TreeType.AzureDatabase) ? -1 :  n.ObjectID;
                objectExecutionSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabWaits)
            {
                globalTimeIsVisible = true;
                currentTabSupportsDayOfWeekFilter = true;
                currentTabSupportsTimeOfDayFilter = true;
                waitsSummary1.InstanceID = n.InstanceID;
                waitsSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabMirroring)
            {
                mirroring1.InstanceIDs = n.RegularInstanceIDs.ToList();
                mirroring1.RefreshData();
            }
            else if(tabs.SelectedTab == tabJobDDL)
            {
                jobDDLHistory1.InstanceID = n.InstanceID;
                jobDDLHistory1.JobID = (Guid)n.Tag;
                jobDDLHistory1.RefreshData();
            }
           else  if(tabs.SelectedTab== tabAG)
            {
                ag1.InstanceIDs = n.RegularInstanceIDs.ToList();
                ag1.RefreshData();
            }
            else if(tabs.SelectedTab == tabQS)
            {
                queryStore1.InstanceIDs = n.InstanceIDs.ToList();
                queryStore1.Instance = n.InstanceName;
                queryStore1.DatabaseID = n.DatabaseID;
                queryStore1.RefreshData();
            }
            else if(tabs.SelectedTab== tabRG)
            {
                resourceGovernor1.InstanceIDs = n.RegularInstanceIDs.ToList();
                resourceGovernor1.RefreshData();
            }
            else if (tabs.SelectedTab == tabRunningQueries)
            {
                globalTimeIsVisible = true;
                runningQueries1.InstanceIDs = n.InstanceIDs.ToList();
                runningQueries1.InstanceID = n.InstanceID;
                runningQueries1.RefreshData();
            }
            else if(tabs.SelectedTab == tabMemory)
            {
                globalTimeIsVisible = true;
                memoryUsage1.InstanceID = n.InstanceID;
                memoryUsage1.RefreshData();
            }
            else if (tabs.SelectedTab == tabJobStats)
            {
                globalTimeIsVisible = true;
                jobStats1.InstanceID = n.InstanceID;
                if (n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                   jobStats1.JobID = (Guid)n.Tag;
                   jobStats1.StepID = 0;
                }
                else if (n.Type == SQLTreeItem.TreeType.AgentJobStep)
                {
                    jobStats1.JobID = (Guid)n.Parent.Tag;
                    jobStats1.StepID = (int)n.Tag;
                }
                else if(n.Type == SQLTreeItem.TreeType.AgentJobs)
                {
                    jobStats1.JobID = Guid.Empty;
                    jobStats1.StepID = 0;
                }
                jobStats1.RefreshData();
            }
            else if (tabs.SelectedTab == tabIdentityColumns)
            {
                identityColumns1.InstanceIDs = n.InstanceIDs.ToList();
                identityColumns1.DatabaseID = n.DatabaseID;
                identityColumns1.IncludeCritical = true;
                identityColumns1.IncludeWarning = true;
                identityColumns1.IncludeOK = !(parent.Type == SQLTreeItem.TreeType.DBADashRoot);
                identityColumns1.IncludeNA = !(parent.Type == SQLTreeItem.TreeType.DBADashRoot);
                identityColumns1.RefreshData();
            }
            else if(tabs.SelectedTab == tabOSLoadedModules)
            {
                osLoadedModules1.InstanceIDs = n.RegularInstanceIDs.ToList();
                osLoadedModules1.RefreshData();
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
            var root = new SQLTreeItem("DBA Dash", SQLTreeItem.TreeType.DBADashRoot) {  ContextMenuStrip = RootRefreshContextMenu() };
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            var hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
            var checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
            root.Nodes.Add(changes);
            root.Nodes.Add(checks);
            root.Nodes.Add(hadr);
            SQLTreeItem parentNode = root;

            var tags = String.Join(",", SelectedTags());

            CommonData.UpdateInstancesList(tagIDs:tags, searchString:SearchString, groupByTag: GroupByTag );

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
                        InstanceID = instanceID
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
            foreach(DataRow job in jobs.Rows)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "enabled", Convert.ToBoolean(job["enabled"]) }
                };
                var jobItem = new SQLTreeItem((string)job["name"], SQLTreeItem.TreeType.AgentJob, attributes) { InstanceID = jobsNode.InstanceID,Tag=(Guid)job["job_id"]};               
                jobItem.AddDummyNode();
                jobsNode.Nodes.Add(jobItem);
            }
        }



        private void AddDatabases(SQLTreeItem instanceNode)
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
            foreach(DataRow r in dbobj.Rows)
            {
                string type = ((string)r[1]).Trim();
                var objN = new SQLTreeItem((string)r[3], (string)r[2], type)
                {
                    ObjectID = (Int64)r[0]
                };
                n.Nodes.Add(objN);
            }
         }
 
        private void Tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var n = (SQLTreeItem)e.Node;
            var parent = (SQLTreeItem)n.Parent;

            List<TabPage> allowedTabs = new();         
            bool hasAzureDBs = n.AzureInstanceIDs.Count > 0;
            var suppress = suppressLoadTab;
            suppressLoadTab = true;

            if(n.Type== SQLTreeItem.TreeType.DBADashRoot || n.Type == SQLTreeItem.TreeType.InstanceFolder)
            {
                allowedTabs.Add(tabSummary);
                allowedTabs.Add(tabPerformanceSummary);
                if (hasAzureDBs)
                {
                    allowedTabs.Add(tabAzureSummary);
                }
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabRunningQueries);
        
            }
            else if(n.Type== SQLTreeItem.TreeType.Database)
            {
                allowedTabs.Add(tabPerformance);
                allowedTabs.Add(tabObjectExecutionSummary);
                allowedTabs.Add(tabSlowQueries);

                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabSnapshotsSummary);             
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabDBConfiguration);
                allowedTabs.Add(tabDBOptions);
                
            }
            else if(n.Type == SQLTreeItem.TreeType.AzureDatabase)
            {
                allowedTabs.Add(tabPerformance);
                allowedTabs.Add(tabAzureSummary);
                allowedTabs.Add(tabAzureDB);
                allowedTabs.Add(tabPC);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabObjectExecutionSummary);
                allowedTabs.Add(tabWaits);
                allowedTabs.Add(tabRunningQueries);
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.Add(tabPerformanceSummary);
                allowedTabs.Add(tabPerformance);
                allowedTabs.Add(tabPC);
                allowedTabs.Add(tabObjectExecutionSummary);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabWaits);
                allowedTabs.Add(tabRunningQueries);
                allowedTabs.Add(tabMemory);
            }
            else if(n.Type == SQLTreeItem.TreeType.AzureInstance)
            {
                allowedTabs.Add(tabPerformanceSummary);         
                allowedTabs.Add(tabAzureSummary);              
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabObjectExecutionSummary);
                allowedTabs.Add(tabRunningQueries);
            }
            else if (n.Type == SQLTreeItem.TreeType.Configuration)
            {
                if (parent.Type == SQLTreeItem.TreeType.Instance)
                {
                    allowedTabs.Add(tabInfo);
                }
                if (parent.Type != SQLTreeItem.TreeType.AzureDatabase && parent.Type != SQLTreeItem.TreeType.AzureInstance && !IsAzureOnly)
                {
                    allowedTabs.Add(tabInstanceConfig);
                    allowedTabs.Add(tabTraceFlags);
                    allowedTabs.Add(tabHardware);
                    allowedTabs.Add(tabSQLPatching);
                    allowedTabs.Add(tabAlerts);
                    allowedTabs.Add(tabDrivers);
                    allowedTabs.Add(tabTempDB);
                    allowedTabs.Add(tabRG);                 
                }
                if (parent.Type != SQLTreeItem.TreeType.Instance && hasAzureDBs)
                {
                    allowedTabs.Add(tabServiceObjectives);
                    allowedTabs.Add(tabAzureDBesourceGovernance);
                }
                allowedTabs.Add(tabDBConfiguration);
                allowedTabs.Add(tabDBOptions);
                allowedTabs.Add(tabQS);
                allowedTabs.Add(tabTags);
            }
            else if(n.Type == SQLTreeItem.TreeType.AgentJobs)
            {
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabJobStats);
            }
            else if(n.Type== SQLTreeItem.TreeType.AgentJob) {
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabJobDDL);
                allowedTabs.Add(tabJobStats);
            }
            else if (n.Type== SQLTreeItem.TreeType.HADR)
            {
                allowedTabs.Add(tabBackups);
                allowedTabs.Add(tabLogShipping);
                allowedTabs.Add(tabMirroring);
                allowedTabs.Add(tabAG);
            }
            else if (n.Type == SQLTreeItem.TreeType.DBAChecks)
            {
                allowedTabs.Add(tabSummary);
                if (!IsAzureOnly && parent.Type!= SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.Add(tabDrives);
                    allowedTabs.Add(tabJobs);
                    allowedTabs.Add(tabLastGood);
                }
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabDBADashErrorLog);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabCustomChecks);
                allowedTabs.Add(tabSnapshotsSummary);
                allowedTabs.Add(tabIdentityColumns);
                allowedTabs.Add(tabOSLoadedModules);
            }
            else if(n.Type== SQLTreeItem.TreeType.Tags)
            {
                allowedTabs.Add(tabTags);
            }
            else if(n.Type == SQLTreeItem.TreeType.AgentJobStep)
            {
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabJobStats);
            }

            if (n.ObjectID > 0)
            {
                if (n.Type == SQLTreeItem.TreeType.StoredProcedure || n.Type == SQLTreeItem.TreeType.CLRProcedure || n.Type == SQLTreeItem.TreeType.ScalarFunction || n.Type == SQLTreeItem.TreeType.CLRScalarFunction || n.Type == SQLTreeItem.TreeType.Trigger || n.Type == SQLTreeItem.TreeType.CLRTrigger)
                {
                    allowedTabs.Add(tabObjectExecutionSummary);
                    allowedTabs.Add(tabPerformance);                  
                }
                allowedTabs.Add(tabSchema);               
            }
            this.Text = n.FullPath.Replace("\\"," \\ ");

            bool validatedTabs = true;
            if (allowedTabs.Count == tabs.TabPages.Count)
            {
                foreach (TabPage t in tabs.TabPages)
                {
                    if (!allowedTabs.Contains(t))
                    {
                        validatedTabs=false;
                    }
                }
            }
            else
            {
                validatedTabs = false;
            }
            if (!validatedTabs)
            {
                tabs.TabPages.Clear();
                foreach (TabPage t in allowedTabs)
                {
                    tabs.TabPages.Add(t);
                }
                if (tabs.TabPages.Count == 0)
                {
                    tabs.TabPages.Add(tabDBADash);
                }
            }
            suppressLoadTab = suppress;
            LoadSelectedTab();
            
           
        }

        private void Tv1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var n = (SQLTreeItem)e.Node;
            if (n.Nodes.Count == 1 && ((SQLTreeItem)n.Nodes[0]).Type == SQLTreeItem.TreeType.DummyNode)
            {
                n.Nodes.Clear();
                if (n.Type == SQLTreeItem.TreeType.Instance)
                {
                    AddDatabases(n);
                }
                else if(n.Type== SQLTreeItem.TreeType.AgentJobs)
                {
                    ExpandJobs(n);
                }
                else if(n.Type == SQLTreeItem.TreeType.AgentJob)
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
            var dtSteps = CommonData.GetJobSteps(n.InstanceID,(Guid)n.Tag);
            foreach(DataRow r in dtSteps.Rows)
            {
                var stepID = (int)r["step_id"];
                var stepName = (string)r["step_name"];
                var subsystem = (string)r["subsystem"];
                var attributes = new Dictionary<string, object>
                {
                    { "subsystem", subsystem }
                };

                n.Nodes.Add(new SQLTreeItem(stepName, SQLTreeItem.TreeType.AgentJobStep, attributes) { InstanceID = n.InstanceID, Tag=stepID });
            }
        }

        #endregion

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


        #endregion

        #region Tagging

        bool isClearTags = false;


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
            ToolStripMenuItem mNone =new("[None]") {  Tag=string.Empty };
            ToolStripMenuItem mShowCounts = new("Show Counts") { CheckOnClick = true,Checked = ShowCounts };
            ToolStripMenuItem mSave = new("Save") { Image = Properties.Resources.Save_16x };
            mNone.Click += GroupByTag_Click;
            mShowCounts.Click += ShowCounts_Click;
            mSave.Click += SaveTreeLayout;
            foreach (string tag in tagNames)
            {
                ToolStripMenuItem mnu = new(tag) { Tag=tag };
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
            TreeSavedView treeView = new() { GroupByTag = GroupByTag, ShowCounts = ShowCounts, Name=SavedView.DefaultViewName };
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
            catch(Exception ex)
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
                while (mnuTag.OwnerItem != null && mnuTag.OwnerItem is ToolStripMenuItem) {
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

        #endregion

         

        private void DataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using DataRetention frm = new();
            frm.ShowDialog();
        }

        /// <summary>
        /// Find instance in the tree by ID
        /// </summary>
        private SQLTreeItem FindInstance(int instanceID,SQLTreeItem node)
        {
            foreach (SQLTreeItem child in node.Nodes)
            {
                if((child.Type== SQLTreeItem.TreeType.Instance || child.Type == SQLTreeItem.TreeType.AzureDatabase) && child.InstanceID == instanceID)
                {
                    return child;
                }
                if(child.Type == SQLTreeItem.TreeType.InstanceFolder || child.Type == SQLTreeItem.TreeType.AzureInstance)
                {
                    SQLTreeItem find = FindInstance(instanceID, child);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

       
        /// <summary>
        /// Find instance in the tree by name
        /// </summary>
        private SQLTreeItem FindInstance(string instance, SQLTreeItem node)
        {
            foreach (SQLTreeItem child in node.Nodes)
            {
                if ((child.Type == SQLTreeItem.TreeType.Instance || child.Type == SQLTreeItem.TreeType.AzureInstance) && child.InstanceName == instance)
                {
                    return child;
                }
                if (child.Type == SQLTreeItem.TreeType.InstanceFolder || child.Type == SQLTreeItem.TreeType.AzureInstance)
                {
                    SQLTreeItem find = FindInstance(instance, child);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        private void Instance_Selected(object sender, InstanceSelectedEventArgs e)
        {
            suppressLoadTab = true;
            var root = (SQLTreeItem)tv1.SelectedNode;

            SQLTreeItem nInstance;
            nInstance = e.InstanceID > 0 ? FindInstance(e.InstanceID, root) : FindInstance(e.Instance, root);
            if (nInstance != null)
            {
                var parent = nInstance.Parent;
                if (!parent.IsExpanded)
                {
                    parent.Expand();
                }
                if (e.Tab == "tabAlerts" || e.Tab == "tabQS") // Configuration Node
                {
                    nInstance.Expand();
                    tv1.SelectedNode = nInstance.Nodes[0];
                }
                else if (e.Tab == "tabMirroring" || e.Tab == "tabLogShipping" || e.Tab == "tabBackups" || e.Tab == "tabAG")
                {
                    nInstance.Expand();
                    tv1.SelectedNode = nInstance.Nodes[2];
                }
                else if (e.Tab == "tabJobs")
                {
                    nInstance.Expand();
                    tv1.SelectedNode = nInstance.LastNode;
                }
                else if (e.Tab == "tabAzureSummary" || e.Tab == "tabPerformance")
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
                    tabs.SelectedTab = tabs.TabPages[e.Tab];
                }
                suppressLoadTab = false;
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
            tsTime.Visible = globalTimeIsVisible;
            tsDayOfWeek.Visible = DateRange.CurrentDateRangeSupportsDayOfWeekFilter && currentTabSupportsDayOfWeekFilter && globalTimeIsVisible;
            tsTimeFilter.Visible = DateRange.CurrentDateRangeSupportsTimeOfDayFilter && currentTabSupportsTimeOfDayFilter && globalTimeIsVisible;
            tsDayOfWeek.Font = new Font(tsDayOfWeek.Font, DateRange.HasDayOfWeekFilter ? FontStyle.Bold : FontStyle.Regular);
            tsTimeFilter.Font = new Font(tsTimeFilter.Font, DateRange.HasTimeOfDayFilter ? FontStyle.Bold : FontStyle.Regular);
        }

        private void DateRangeChanged()
        {

            UpdateTimeFilterVisibility();
                             
            if (tabs.SelectedTab == tabPerformance)
            {
                performance1.RefreshData();
            }
            else if (tabs.SelectedTab == tabPerformanceSummary)
            {
                performanceSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabPC)
            {
                performanceCounterSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabSlowQueries)
            {
                slowQueries1.RefreshData();
            }
            else if (tabs.SelectedTab == tabObjectExecutionSummary)
            {
                objectExecutionSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAzureSummary)
            {
                azureSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAzureDB)
            {
                azureDBResourceStats1.RefreshData();
            }
            else if (tabs.SelectedTab == tabWaits)
            {
                waitsSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabRunningQueries)
            {
                runningQueries1.RefreshData();
            }
            else if(tabs.SelectedTab== tabMemory)
            {
                memoryUsage1.RefreshData();
            }
            else if(tabs.SelectedTab == tabJobStats)
            {
                jobStats1.RefreshData();
            }

        }

        private void TsCustomTime_Click(object sender, EventArgs e)
        {
            var frm = new Performance.CustomTimePicker() { FromDate = Performance.DateRange.FromUTC.ToLocalTime(), ToDate = Performance.DateRange.ToUTC.ToLocalTime()};
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                Performance.DateRange.SetCustom(frm.FromDate.ToUniversalTime(), frm.ToDate.ToUniversalTime());
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
                if(tabs.SelectedTab == tabSummary)
                {
                    summary1.RefreshData();
                }
            }
        }


        private void GvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var node = (SQLTreeItem)tv1.SelectedNode;
            if (e.RowIndex>=0 && e.ColumnIndex == colCompare.Index)
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
            CommonShared.ShowAbout(Common.ConnectionString, this,true);
        }

        private void BttnSearch_Click(object sender, EventArgs e)
        {
            AddInstanes();        
        }

        private void DatabaseSchemaDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new DBDiff { SelectedTags = SelectedTags() })
            {
                var n = (SQLTreeItem)tv1.SelectedNode;
                frm.SelectedInstanceA = n.InstanceName;
                frm.SelectedDatabaseA = new DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName };
                frm.ShowDialog(this);
            }
        }

        private void AgentJobDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new JobDiff())
            {
                var selected = (SQLTreeItem)tv1.SelectedNode;
                frm.InstanceID_A = selected.InstanceID;
                frm.ShowDialog(this);
            }
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
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
                var date = DateTime.Now.AddDays(-i).Date;
                var daysAgo = i == 0 ? "Today" : i == 1 ? "Yesterday" : i.ToString() + " days ago";
                var humanDateString = date.ToShortDateString() + " (" + date.DayOfWeek.ToString()[..3] + " - " + daysAgo + ")";
                var dateItem = new ToolStripMenuItem(humanDateString) { Tag = date, Checked = DateRange.FromUTC == date.ToUniversalTime() && DateRange.ToUTC ==date.AddDays(1).ToUniversalTime() };
                dateItem.Click += DateItem_Click;
                dateToolStripMenuItem.DropDownItems.Add(dateItem);
            }           
        }

        private void DateItem_Click(object sender, EventArgs e)
        {
            var dateItem = (ToolStripMenuItem)sender;
            var selectedDate = (DateTime)dateItem.Tag;
            DateRange.SetCustom(selectedDate.ToUniversalTime(), selectedDate.ToUniversalTime().AddDays(1));
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
                    if (globalTimeIsVisible)
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
            using (var frm = new DayOfWeekSelection() { DaysOfWeekSelected = DateRange.DayOfWeek }){
                frm.ShowDialog(this);
                if(frm.DialogResult == DialogResult.OK)
                {
                    DateRange.DayOfWeek = frm.DaysOfWeekSelected;
                    DateRangeChanged();
                }
            }
        }

        private void TsTimeFilter_Click(object sender, EventArgs e)
        {
            using(var frm = new HourSelection { SelectedHours = DateRange.TimeOfDay  })
            {
                frm.ShowDialog(this);
                if(frm.DialogResult== DialogResult.OK)
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
            SaveContext(tv1.SelectedNode,tabs.SelectedIndex);
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
        private void SaveContext(TreeNode node,int tagIndex)
        {
            if (node != null)
            {
                SaveContext((SQLTreeItem)node, tagIndex);
            }
        }

        /// <summary>
        /// Save current node and tab to support NavigateBack
        /// </summary>
        private void SaveContext(SQLTreeItem node,int TabIndex)
        {
            if (node != null && TabIndex>=0 & !suppressSaveContext) // Save context if we have a current node/tab and save context is not supressed (e.g. When navigating back)
            {
                if (VisitedNodes.Count > 0) // Save only if current context is different to previous context
                {
                    var previousContext = VisitedNodes[^1];
                    if (previousContext.Node == node && previousContext.TabIndex == TabIndex)
                    {
                        return;
                    }
                }
                else if (tv1.Nodes.Count>0)
                {
                    // Ensure we have added the root context
                    VisitedNodes.Add(new TreeContext() {  Node = (SQLTreeItem)tv1.Nodes[0], TabIndex = 0 }) ;
                    if(node==tv1.Nodes[0] && TabIndex == 0) // If context we are saving is root, we don't need to add it again
                    {
                        return;
                    }
                }
                VisitedNodes.Add(new TreeContext() { Node = node, TabIndex = TabIndex });
                tsBack.Enabled = VisitedNodes.Count>0;
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
            foreach(var ctrl in tabs.SelectedTab.Controls)
            {
                if(ctrl is INavigation)
                {
                    if (((INavigation)ctrl).NavigateBack()){
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

        private async void tsConnect_Click(object sender, EventArgs e)
        {
            string oldConnection = Common.ConnectionString;
            using(var frm = new DBConnection() { ConnectionString = Common.ConnectionString })
            {
                frm.ShowDialog();
                if(frm.DialogResult== DialogResult.OK)
                {
                    try
                    {
                        await SetConnection(frm.ConnectionString);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Error switching to DBA Dash repository database:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        await SetConnection(oldConnection);
                    }
                }
            }   
        }
    }
}
