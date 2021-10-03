using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DiffControl;
using System.Diagnostics;
using System.IO;

namespace DBADashGUI
{

    public partial class Main : Form
    {

        public class InstanceSelectedEventArgs : EventArgs
        {
            public int InstanceID=-1;
            public string Instance;
            public string Tab;
        }

        private readonly CommandLineOptions commandLine;
        private readonly List<int> commandLineTags = new List<int>();

        public Main(CommandLineOptions opts)
        {
            InitializeComponent();
            commandLine = opts;
        }

        string connectionString = "";
        readonly string jsonPath = Common.JsonConfigPath;


        private Int64 currentObjectID;
        private Int32 currentPage = 1;
        private Int32 currentPageSize = 100;
        private readonly DiffControl diffSchemaSnapshot = new DiffControl();
        private bool suppressLoadTab=false;

        private void Main_Load(object sender, EventArgs e)
        {
            dbOptions1.SummaryMode = true;
            splitSchemaSnapshot.Panel1.Controls.Add(diffSchemaSnapshot);
            diffSchemaSnapshot.Dock = DockStyle.Fill;

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

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = "DBADashGUI"
            };
            connectionString = builder.ConnectionString;
            Common.ConnectionString = connectionString;
            mnuTags.Visible = !commandLine.NoTagMenu;
            getCommandLineTags();
            buildTagMenu(commandLineTags);
            addInstanes();
            loadSelectedTab();
            
        }

        private void getCommandLineTags()
        {
            if (commandLine.TagFilters !=null && commandLine.TagFilters.Length > 0)
            {
                foreach (var t in DBADashTag.GetTags(commandLine.TagFilters))
                {
                    commandLineTags.Add(t.TagID);
                }
            }
        }


        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadSelectedTab();
        }

        private void loadSelectedTab()
        {
            if (suppressLoadTab)
            {
                return;
            }
            bool globalTimeisVisible = false;
            List<Int32> instanceIDs;         
            var n = (SQLTreeItem) tv1.SelectedNode;
            var parent = (SQLTreeItem)n.Parent;
            if (n.InstanceID > 0)
            {
                instanceIDs = new List<Int32>() { n.InstanceID };
            }
            else
            {
            
                if (n.Type == SQLTreeItem.TreeType.AzureInstance)
                {
                    instanceIDs = n.ChildInstanceIDs;
                }
                else if(parent!=null && parent.Type == SQLTreeItem.TreeType.AzureInstance && n.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    instanceIDs = parent.ChildInstanceIDs;
                }
                else
                {
                    instanceIDs = InstanceIDs;
                }
               
            }
            if (tabs.SelectedTab == tabTags)
            {
                tags1.InstanceName = n.InstanceName;
                tags1.InstanceIDs = AllInstanceIDs;
                tags1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDrives)
            {
                drivesControl1.ConnectionString = connectionString;
                drivesControl1.InstanceIDs = instanceIDs;
                drivesControl1.IncludeNA = instanceIDs.Count == 1;
                drivesControl1.IncludeOK = instanceIDs.Count == 1;
                drivesControl1.IncludeWarning = true;
                drivesControl1.IncludeCritical = true;
                drivesControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabBackups)
            {
                backupsControl1.InstanceIDs = instanceIDs;    
                backupsControl1.IncludeNA = instanceIDs.Count==1;
                backupsControl1.IncludeOK = instanceIDs.Count == 1;
                backupsControl1.IncludeWarning = true;
                backupsControl1.IncludeCritical = true;
                backupsControl1.RefreshBackups();
            }
            else if(tabs.SelectedTab== tabLogShipping)
            {
                logShippingControl1.IncludeNA = instanceIDs.Count==1;
                logShippingControl1.IncludeOK = instanceIDs.Count==1;
                logShippingControl1.IncludeWarning = true;
                logShippingControl1.IncludeCritical = true;
                logShippingControl1.InstanceIDs = instanceIDs;
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
                agentJobsControl1.IncludeNA = instanceIDs.Count == 1;
                agentJobsControl1.IncludeOK = instanceIDs.Count == 1;
                agentJobsControl1.IncludeWarning = true;
                agentJobsControl1.IncludeCritical = true;
                agentJobsControl1.InstanceIDs = instanceIDs;
                agentJobsControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabSummary)
            {
                summary1.ConnectionString = connectionString;
                summary1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                summary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabFiles)
            {
                dbFilesControl1.ConnectionString = connectionString;
                dbFilesControl1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
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
                schemaSnapshots1.InstanceIDs = AllInstanceIDs;
                schemaSnapshots1.RefreshData();
            }
            else if ( tabs.SelectedTab == tabSchema)
            {
                getHistory(n.ObjectID);
            }
            else if(tabs.SelectedTab == tabLastGood)
            {
                lastGoodCheckDBControl1.InstanceIDs = instanceIDs;
                lastGoodCheckDBControl1.connectionString = connectionString;
                lastGoodCheckDBControl1.IncludeCritical = true;
                lastGoodCheckDBControl1.IncludeWarning = true;
                lastGoodCheckDBControl1.IncludeNA = n.InstanceID>0;
                lastGoodCheckDBControl1.IncludeOK = n.InstanceID > 0;
                lastGoodCheckDBControl1.RefreshData();
            }
            else if(tabs.SelectedTab == tabPerformance)
            {
                globalTimeisVisible = true;
                performance1.InstanceID = n.InstanceID;
                performance1.DatabaseID = n.DatabaseID;
                performance1.ConnectionString = connectionString;
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
                collectionErrors1.InstanceName = n.InstanceName;
                collectionErrors1.Days = 1;
                collectionErrors1.AckErrors =  SelectedTags().Count == 0 && parent.Type == SQLTreeItem.TreeType.DBADashRoot;
                collectionErrors1.RefreshData();
            }
            else if(tabs.SelectedTab== tabCollectionDates)
            {
                collectionDates1.InstanceIDs =  n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                collectionDates1.ConnectionString = connectionString;
                collectionDates1.IncludeCritical = true;
                collectionDates1.IncludeWarning = true;
                collectionDates1.IncludeNA = n.InstanceID > 0;
                collectionDates1.IncludeOK = n.InstanceID > 0;
                collectionDates1.RefreshData();

            }
            else if(tabs.SelectedTab== tabPerformanceSummary)
            {
                globalTimeisVisible = true;
                if (n.Type == SQLTreeItem.TreeType.DBADashRoot)
                {
                    performanceSummary1.TagIDs = String.Join(",", SelectedTags());
                    performanceSummary1.InstanceIDs = new List<int>();
                }
                else
                {
                    performanceSummary1.InstanceIDs = instanceIDs;                
                    performanceSummary1.TagIDs = "";
                }
                performanceSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabInfo)
            {
                info1.InstanceID = n.InstanceID;
                info1.ConnectionString = connectionString;
                info1.RefreshData();
            }
            else if(tabs.SelectedTab== tabHardware)
            {
                hardwareChanges1.InstanceIDs = instanceIDs;
                hardwareChanges1.ConnectionString = connectionString;
                hardwareChanges1.RefreshData();
            }
            else if (tabs.SelectedTab == tabSQLPatching)
            {
                sqlPatching1.InstanceIDs = instanceIDs;
                sqlPatching1.ConnectionString = connectionString;
                sqlPatching1.RefreshData();
            }
            else if(tabs.SelectedTab == tabInstanceConfig)
            {                
                configurationHistory1.InstanceIDs = instanceIDs;
                configurationHistory1.ConnectionString = connectionString;
                configurationHistory1.RefreshData();
            }
            else if(tabs.SelectedTab== tabSlowQueries)
            {
                globalTimeisVisible = true;
                slowQueries1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                slowQueries1.ConnectionString = connectionString;
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
                traceFlagHistory1.InstanceIDs = instanceIDs;
                traceFlagHistory1.ConnectionString = connectionString;
                traceFlagHistory1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAlerts)
            {
                alerts1.InstanceIDs = instanceIDs;
                alerts1.ConnectionString = connectionString;
                alerts1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDrivers)
            {
                drivers1.InstanceIDs = instanceIDs;
                drivers1.ConnectionString = connectionString;
                drivers1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDBSpace)
            {
                spaceTracking1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                spaceTracking1.DatabaseID = n.DatabaseID;
                spaceTracking1.Instance = n.InstanceName;
                spaceTracking1.DBName = n.DatabaseName;
                spaceTracking1.RefreshData();
            }
            else if(tabs.SelectedTab == tabAzureSummary)
            {
                globalTimeisVisible = true;
                azureSummary1.InstanceIDs =n.Type == SQLTreeItem.TreeType.DBADashRoot ? AzureInstanceIDs : instanceIDs;
                azureSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabAzureDB)
            {
                globalTimeisVisible = true;
                azureDBResourceStats1.InstanceID = n.InstanceID;
                azureDBResourceStats1.RefreshData();
            }
            else if(tabs.SelectedTab == tabServiceObjectives)
            {
                azureServiceObjectivesHistory1.InstanceIDs = ((SQLTreeItem)n.Parent).Type == SQLTreeItem.TreeType.DBADashRoot ? AzureInstanceIDs : instanceIDs;
                azureServiceObjectivesHistory1.RefreshData();
            }
            else if (tabs.SelectedTab == tabAzureDBesourceGovernance)
            {
                azureDBResourceGovernance1.InstanceIDs = ((SQLTreeItem)n.Parent).Type == SQLTreeItem.TreeType.DBADashRoot ? AzureInstanceIDs : instanceIDs;
                azureDBResourceGovernance1.RefreshData();
            }
            else if (tabs.SelectedTab == tabDBConfiguration)
            {
                dbConfiguration1.InstanceIDs = ((SQLTreeItem)n.Parent).Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                dbConfiguration1.DatabaseID = n.DatabaseID;
                dbConfiguration1.RefreshData();
            }
            else if(tabs.SelectedTab == tabDBOptions)
            {
                dbOptions1.InstanceIDs = parent.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                dbOptions1.DatabaseID = n.DatabaseID;
                dbOptions1.RefreshData();
            }
            else if(tabs.SelectedTab == tabTempDB)
            {
                tempDBConfig1.InstanceIDs = instanceIDs;
                tempDBConfig1.RefreshData();
            }
            else if (tabs.SelectedTab == tabCustomChecks)
            {
               customChecks1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
               customChecks1.IncludeCritical = true;
               customChecks1.IncludeWarning = true;
               customChecks1.IncludeNA = n.InstanceID > 0 || n.Type == SQLTreeItem.TreeType.AzureInstance;
               customChecks1.IncludeOK = n.InstanceID > 0 || n.Type == SQLTreeItem.TreeType.AzureInstance;
               customChecks1.RefreshData();
            }
            else if (tabs.SelectedTab == tabPC)
            {
                globalTimeisVisible = true;
                performanceCounterSummary1.InstanceID = n.InstanceID;
                performanceCounterSummary1.RefreshData();
            }
            else if(tabs.SelectedTab== tabObjectExecutionSummary)
            {
                globalTimeisVisible = true;
                objectExecutionSummary1.Instance = n.InstanceName;
                objectExecutionSummary1.InstanceID = n.InstanceID;
                objectExecutionSummary1.DatabaseID = n.DatabaseID;
                objectExecutionSummary1.ObjectID = (n.Type == SQLTreeItem.TreeType.Database || n.Type == SQLTreeItem.TreeType.AzureDatabase) ? -1 :  n.ObjectID;
                objectExecutionSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabWaits)
            {
                globalTimeisVisible = true;
                waitsSummary1.InstanceID = n.InstanceID;
                waitsSummary1.RefreshData();
            }
            else if(tabs.SelectedTab == tabMirroring)
            {
                mirroring1.InstanceIDs = instanceIDs;
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
                ag1.InstanceIDs = instanceIDs;
                ag1.RefreshData();
            }
            else if(tabs.SelectedTab == tabQS)
            {
                queryStore1.InstanceIDs = AllInstanceIDs;
                queryStore1.Instance = n.InstanceName;
                queryStore1.DatabaseID = n.DatabaseID;
                queryStore1.RefreshData();
            }
            else if(tabs.SelectedTab== tabRG)
            {
                resourceGovernor1.InstanceIDs = instanceIDs;
                resourceGovernor1.RefreshData();
            }
            else if (tabs.SelectedTab == tabRunningQueries)
            {
                globalTimeisVisible = true;
                runningQueries1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                runningQueries1.InstanceID = n.InstanceID;
                runningQueries1.RefreshData();
            }
            else if(tabs.SelectedTab == tabMemory)
            {
                globalTimeisVisible = true;
                memoryUsage1.InstanceID = n.InstanceID;
                memoryUsage1.RefreshData();
            }
            tsTime.Visible = globalTimeisVisible;
        }

 

        private readonly List<Int32> InstanceIDs = new List<Int32>();
        private readonly List<Int32> AllInstanceIDs=new List<Int32>();
        private readonly List<Int32> AzureInstanceIDs= new List<Int32>();

        private bool IsAzureOnly;

        #region Tree

        private void addInstanes()
        {
            tv1.Nodes.Clear();
            InstanceIDs.Clear();
            AzureInstanceIDs.Clear();
            AllInstanceIDs.Clear();
            var root = new SQLTreeItem("DBA Dash", SQLTreeItem.TreeType.DBADashRoot);
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            var hadr = new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR);
            var checks = new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks);
            root.Nodes.Add(changes);
            root.Nodes.Add(checks);
            root.Nodes.Add(hadr);

            var tags = String.Join(",", SelectedTags());

            var dtInstances = CommonData.GetInstances(tags);

            SQLTreeItem AzureNode = null;
            foreach (DataRow row in dtInstances.Rows)
            {
                string instance = (string)row["Instance"];
                Int32 instanceID = (Int32)row["InstanceID"];
                if ((bool)row["IsAzure"])
                {
                    string db = (string)row["AzureDBName"];
                    if (AzureNode == null || AzureNode.InstanceName != instance)
                    {
                        AzureNode = new SQLTreeItem(instance, SQLTreeItem.TreeType.AzureInstance);
                        root.Nodes.Add(AzureNode);
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
                    AzureNode.Nodes.Add(azureDBNode);
                    AzureInstanceIDs.Add(instanceID);
                }
                else
                {
                    var n = new SQLTreeItem(instance, SQLTreeItem.TreeType.Instance)
                    {
                        InstanceID = instanceID
                    };
                    n.AddDummyNode();
                    root.Nodes.Add(n);
                    InstanceIDs.Add(instanceID);
                }
                AllInstanceIDs.Add(instanceID);
            }
            IsAzureOnly=AllInstanceIDs.Count() == AzureInstanceIDs.Count();
            if (IsAzureOnly)
            {
                root.Nodes.Remove(hadr);
            }
  
            tv1.Nodes.Add(root);
            root.Expand();
            tv1.SelectedNode = root;
        }

        private void expandJobs(SQLTreeItem jobsNode)
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



        private void addDatabases(SQLTreeItem instanceNode)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DatabasesByInstance_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();

                    var changesNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration)
                    {
                        InstanceID = instanceNode.InstanceID
                    };
                    instanceNode.Nodes.Add(changesNode);                  
                    instanceNode.Nodes.Add(new SQLTreeItem("Checks", SQLTreeItem.TreeType.DBAChecks));
                    instanceNode.Nodes.Add(new SQLTreeItem("HA/DR", SQLTreeItem.TreeType.HADR));
                    // instanceNode.Nodes.Add(new SQLTreeItem("Tags", SQLTreeItem.TreeType.Tags));

                    cmd.Parameters.AddWithValue("Instance", instanceNode.ObjectName);
                    var systemNode = new SQLTreeItem("System Databases", SQLTreeItem.TreeType.Folder);
                    instanceNode.Nodes.Add(systemNode);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var n = new SQLTreeItem((string)rdr[1], SQLTreeItem.TreeType.Database)
                        {
                            DatabaseID = (Int32)rdr[0],
                            InstanceID = (Int32)rdr[3]
                        };
                        if (!rdr.IsDBNull(2))
                        {
                            n.ObjectID = (Int64)rdr[2];
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
                    var jobs = new SQLTreeItem("Jobs", SQLTreeItem.TreeType.AgentJobs) { InstanceID = instanceNode.InstanceID };
                    jobs.AddDummyNode();
                    instanceNode.Nodes.Add(jobs);
                }
            }
        }


        private void ExpandObjects(SQLTreeItem n)
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
 
        private void tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var n = (SQLTreeItem)e.Node;
            var parent = (SQLTreeItem)n.Parent;

            List<TabPage> allowedTabs = new List<TabPage>();         
            bool hasAzureDBs = AzureInstanceIDs.Count > 0;
            var suppress = suppressLoadTab;
            suppressLoadTab = true;

            if(n.Type== SQLTreeItem.TreeType.DBADashRoot)
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
            }
            else if(n.Type== SQLTreeItem.TreeType.AgentJob) {
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabJobDDL);
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
            }
            else if(n.Type== SQLTreeItem.TreeType.Tags)
            {
                allowedTabs.Add(tabTags);
            }
            else if(n.Type == SQLTreeItem.TreeType.AgentJobStep)
            {
                allowedTabs.Add(tabJobs);
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
            this.Text ="DBA Dash" + (n.Type== SQLTreeItem.TreeType.DBADashRoot || n.InstanceName == null ? "" : " - " + n.InstanceName) + ((n.DatabaseName ==null || n.DatabaseName=="") ? "" : "\\" + n.DatabaseName);

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
            }
            suppressLoadTab = suppress;
             loadSelectedTab();
            
           
        }

        private void tv1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var n = (SQLTreeItem)e.Node;
            if (n.Nodes.Count == 1 && ((SQLTreeItem)n.Nodes[0]).Type == SQLTreeItem.TreeType.DummyNode)
            {
                n.Nodes.Clear();
                if (n.Type == SQLTreeItem.TreeType.Instance)
                {
                    addDatabases(n);
                }
                else if(n.Type== SQLTreeItem.TreeType.AgentJobs)
                {
                    expandJobs(n);
                }
                else if(n.Type == SQLTreeItem.TreeType.AgentJob)
                {
                    expandJobSteps(n);
                }
                else
                {
                    ExpandObjects(n);
                }

            }
        }

        private void expandJobSteps(SQLTreeItem n)
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
        private void loadDDL(Int64 DDLID, Int64 DDLIDOld)
        {
            string newText = Common.DDL(DDLID);
            string oldText = Common.DDL(DDLIDOld);
            diffSchemaSnapshot.OldText = oldText;
            diffSchemaSnapshot.NewText = newText;
        }

        private void getHistory(Int64 ObjectID, Int32 PageNum = 1)
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

        private void gvHistory_SelectionChanged(object sender, EventArgs e)
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
                loadDDL(ddlID, ddlIDOld);
            }
        }

        private void tsNext_Click(object sender, EventArgs e)
        {
            getHistory(currentObjectID, currentPage + 1);
        }

        private void tsPrevious_Click(object sender, EventArgs e)
        {
            getHistory(currentObjectID, currentPage - 1);
        }

        private void tsPageSize_Validated(object sender, EventArgs e)
        {
            if (Int32.Parse(tsPageSize.Text) != currentPageSize)
            {
                getHistory(currentObjectID, 1);
            }
        }

        private void tsPageSize_Validating(object sender, CancelEventArgs e)
        {
            int.TryParse(tsPageSize.Text, out int i);
            if (i <= 0)
            {
                tsPageSize.Text = currentPageSize.ToString();
            }
        }


        private void dBDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new DBDiff
            {
                SelectedTags = SelectedTags()
            };
            var n = (SQLTreeItem)tv1.SelectedNode;
            frm.SelectedInstanceA = n.InstanceName;
            frm.SelectedDatabaseA = new DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName };
            frm.ShowDialog();
        }


        #endregion

        #region Tagging

        bool isClearTags = false;


        private void buildTagMenu(List<int> selected = null)
        {
            mnuTags.DropDownItems.Clear();


            string currentTag = String.Empty;
            ToolStripMenuItem mTagName = new ToolStripMenuItem();
            ToolStripMenuItem mSystemTags = new ToolStripMenuItem("System Tags");
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

            setFont(mnuTags);
        }

        private void RefreshTag_Click(object sender, EventArgs e)
        {
            buildTagMenu();
        }


        private void ClearTag_Click(object sender, EventArgs e)
        {
            isClearTags = true;
            mnuTags.Font = Font = new Font(mnuTags.Font, mnuTags.Font.Style & ~FontStyle.Bold);
            clearTags(mnuTags);
            isClearTags = false;
            addInstanes();
            this.Font = new Font(this.Font, FontStyle.Regular);
        }

        private void clearTags(ToolStripMenuItem rootMnu)
        {
            
            foreach (ToolStripItem mnu in rootMnu.DropDownItems)
            {
                if (mnu.GetType() == typeof(ToolStripMenuItem))
                {
                    ((ToolStripMenuItem)mnu).Checked = false;
                    clearTags((ToolStripMenuItem)mnu);
                    if (mnu.Font.Bold)
                    {
                        mnu.Font = Font = new Font(mnu.Font, mnu.Font.Style & ~FontStyle.Bold);
                    }
                }
            }
        }



        private List<int> SelectedTags(ToolStripMenuItem mnu = null)
        {
            if (mnu == null)
            {
                mnu = mnuTags;
                
            }
            var selected = new List<int>();
            foreach (ToolStripItem mnuTag in mnu.DropDownItems)
            {
                if (mnuTag.GetType() == typeof(ToolStripMenuItem))
                {
                    if (((ToolStripMenuItem)mnuTag).Checked)
                    {
                        selected.Add((int)mnuTag.Tag);                 
                    }
                    if (((ToolStripMenuItem)mnuTag).DropDownItems.Count > 0)
                    {
                        selected.AddRange(SelectedTags((ToolStripMenuItem)mnuTag));
                    }
                }
            }
            return selected;
        }

        private void MTagValue_CheckedChanged(object sender, EventArgs e)
        {
            if (!isClearTags)
            {
                addInstanes();
                var mnuTag = (ToolStripMenuItem)sender;
                while (mnuTag.OwnerItem != null) {
                    mnuTag = (ToolStripMenuItem)mnuTag.OwnerItem;
                }
                setFont(mnuTag);
                
            }
        }


        private void setFont(ToolStripMenuItem mnu)
        {
            if (mnu.Checked)
            {
                mnu.Font = new Font(mnu.Font, mnu.Font.Style | FontStyle.Bold);
            }
            else if (SelectedTags(mnu).Count > 0)
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
                    setFont((ToolStripMenuItem)itm);
                }

            }
        }

        #endregion

         

        private void DataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataRetention frm = new DataRetention
            {
                ConnectionString = connectionString
            };
            frm.ShowDialog();
        }

        private void Instance_Selected(object sender, InstanceSelectedEventArgs e)
        {
            suppressLoadTab = true;
            var root = tv1.Nodes[0];
            foreach(SQLTreeItem child in root.Nodes)
            {
                if((child.InstanceID  == e.InstanceID && e.InstanceID>0 ) || (child.InstanceName == e.Instance && e.Instance!=null))
                {
                    if (e.Tab == "tabAlerts" || e.Tab == "tabQS") // Configuration Node
                    {
                        child.Expand();
                        tv1.SelectedNode = child.Nodes[0];
                    }
                    else if(e.Tab=="tabMirroring" || e.Tab== "tabLogShipping" || e.Tab=="tabBackups" || e.Tab=="tabAG")
                    {
                        child.Expand();
                        tv1.SelectedNode = child.Nodes[2];
                    }
                    else if (e.Tab == "tabJobs")
                    {
                        child.Expand();
                        tv1.SelectedNode = child.LastNode;
                    }
                    else if(e.Tab == "tabAzureSummary" || e.Tab=="tabPerformance")
                    {
                        tv1.SelectedNode = child;
                    }
                    else
                    {
                        child.Expand();
                        tv1.SelectedNode = child.Nodes[1];
                    }
                    break;
                }
                if (child.InstanceID <= 0)
                {
                    foreach(SQLTreeItem azureDB in child.Nodes)
                    {
                        if(azureDB.InstanceID == e.InstanceID)
                        {
                            tv1.SelectedNode = azureDB;
                            break;
                        }
                    }
                }
            }
            if (e.Tab != null && e.Tab.Length > 0)
            {
                tabs.SelectedTab = tabs.TabPages[e.Tab];
            }
            suppressLoadTab = false;
            loadSelectedTab();
        }

        private void tsTime_Click(object sender, EventArgs e)
        {
            string tag = (string)((ToolStripMenuItem)sender).Tag;
            checkTime(tag);
            Performance.DateRange.SetMins(Convert.ToInt32(tag));
            DateRangeChanged();
        }

        private void checkTime(string tag)
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

        private void DateRangeChanged()
        {
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

        }

        private void tsCustomTime_Click(object sender, EventArgs e)
        {
            var frm = new Performance.CustomTimePicker() { FromDate = Performance.DateRange.FromUTC.ToLocalTime(), ToDate = Performance.DateRange.ToUTC.ToLocalTime()};
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                Performance.DateRange.SetCustom(frm.FromDate.ToUniversalTime(), frm.ToDate.ToUniversalTime());
                checkTime((string)((ToolStripMenuItem)sender).Tag);
                DateRangeChanged();
            }            
        }

        private void manageInstancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new ManageInstances
            {
                Tags = String.Join(",", SelectedTags())
            };
            frm.ShowDialog();
            if (frm.InstanceActiveFlagChanged)
            {
                addInstanes(); // refresh the tree if instances deleted/restored
            }
        }

        private void tsJobDiff_Click(object sender, EventArgs e)
        {
           using(var frm = new JobDiff())
            {
                var selected = (SQLTreeItem)tv1.SelectedNode;
                frm.InstanceID_A = selected.InstanceID;
                frm.ShowDialog();
            }
        }

        private void gvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var node = (SQLTreeItem)tv1.SelectedNode;
            if (e.RowIndex>=0 && e.ColumnIndex == colCompare.Index)
            {
                var row = (DataRowView)gvHistory.Rows[e.RowIndex].DataBoundItem;
                var frm = new DDLCompareTo() { Instance_A = node.InstanceName, DatabaseID_A = node.DatabaseID, ObjectType_A = (string)row["ObjectType"], ObjectID_A = node.ObjectID, SnapshotDate_A = (DateTime)row["SnapshotValidFrom"] } ;   
                frm.SelectedTags = SelectedTags();
                frm.ShowDialog();
            }
        }

        private void tags1_TagsChanged(object sender, EventArgs e)
        {
            buildTagMenu(SelectedTags());
        }
    }
}
