using DBADash;
using ICSharpCode.TextEditor.Document;
using Microsoft.Reporting.WinForms;
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
        public Main()
        {
            InitializeComponent();
        }

        string connectionString = "";

        int currentSummaryPage = 1;
        readonly Int32 currentSummaryPageSize = 100;
        private Int64 currentObjectID;
        private Int32 currentPage=1;
        private Int32 currentPageSize=100;
        bool isTagPopulation = false;
        private readonly DiffControl diffSchemaSnapshot = new DiffControl();
        private bool suppressLoadTab=false;

        private void Main_Load(object sender, EventArgs e)
        {
            
            splitSchemaSnapshot.Panel1.Controls.Add(diffSchemaSnapshot);
            diffSchemaSnapshot.Dock = DockStyle.Fill;

            string jsonPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
            if (System.IO.File.Exists(jsonPath))
            {
                string jsonConfig = System.IO.File.ReadAllText(jsonPath);
                var cfg= CollectionConfig.Deserialize(jsonConfig);
                if(cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL)
                {
                    connectionString = cfg.DestinationConnection.ConnectionString;                }
           
            }
            if (connectionString == "")
            {
                MessageBox.Show("Please use " + Properties.Resources.ServiceConfigToolName + " to configure the service" + Environment.NewLine + "This GUI tool requires a destination connection string pointing to the DBADash repository database.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (File.Exists(Properties.Resources.ServiceConfigToolName))
                {
                    Process.Start(Properties.Resources.ServiceConfigToolName);
                }
                Application.Exit();
            }
            else
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ApplicationName = "DBADashGUI"
                };
                connectionString = builder.ConnectionString;
                Common.ConnectionString = connectionString;

                addInstanes();
                buildTagMenu();
                loadSelectedTab();
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
                else if(n.Type == SQLTreeItem.TreeType.Configuration && parent.Type == SQLTreeItem.TreeType.AzureInstance)
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
                getTags();
            }
            if (tabs.SelectedTab == tabDrives)
            {
                drivesControl1.ConnectionString = connectionString;
                drivesControl1.InstanceIDs = instanceIDs;
                drivesControl1.IncludeNA = (n.Type != SQLTreeItem.TreeType.DBADashRoot);
                drivesControl1.IncludeOK = (n.Type != SQLTreeItem.TreeType.DBADashRoot);
                drivesControl1.IncludeWarning = true;
                drivesControl1.IncludeCritical = true;
                drivesControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabBackups)
            {
                backupsControl1.InstanceIDs = instanceIDs;
                backupsControl1.ConnectionString = connectionString;          
                backupsControl1.IncludeNA = (n.Type != SQLTreeItem.TreeType.DBADashRoot);
                backupsControl1.IncludeOK = (n.Type != SQLTreeItem.TreeType.DBADashRoot);
                backupsControl1.IncludeWarning = true;
                backupsControl1.IncludeCritical = true;
                backupsControl1.RefreshBackups();
            }
            if(tabs.SelectedTab== tabLogShipping)
            {
                logShippingControl1.IncludeNA = n.Type != SQLTreeItem.TreeType.DBADashRoot;
                logShippingControl1.IncludeOK = n.Type != SQLTreeItem.TreeType.DBADashRoot;
                logShippingControl1.IncludeWarning = true;
                logShippingControl1.IncludeCritical = true;
                logShippingControl1.InstanceIDs = instanceIDs;
                logShippingControl1.ConnectionString = connectionString;
                logShippingControl1.RefreshData();
            }
            if(tabs.SelectedTab== tabJobs)
            {
                agentJobsControl1.IncludeNA = n.Type != SQLTreeItem.TreeType.DBADashRoot;
                agentJobsControl1.IncludeOK = n.Type != SQLTreeItem.TreeType.DBADashRoot;
                agentJobsControl1.IncludeWarning = true;
                agentJobsControl1.IncludeCritical = true;
                agentJobsControl1.ConnectionString = connectionString;
                agentJobsControl1.InstanceIDs = instanceIDs;
                agentJobsControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabSummary)
            {
                summary1.ConnectionString = connectionString;
                summary1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                summary1.RefreshData();
            }
            if(tabs.SelectedTab == tabFiles)
            {
                dbFilesControl1.ConnectionString = connectionString;
                dbFilesControl1.InstanceIDs = instanceIDs;
                dbFilesControl1.DatabaseID = (n.DatabaseID > 0 ? (Int32?)n.DatabaseID : null);
                dbFilesControl1.IncludeCritical = true;
                dbFilesControl1.IncludeWarning = true;
                dbFilesControl1.IncludeNA = dbFilesControl1.DatabaseID != null;
                dbFilesControl1.IncludeOK = dbFilesControl1.DatabaseID != null;
                dbFilesControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabSnapshotsSummary)
            {
                loadSnapshots();
            }
            if( tabs.SelectedTab == tabSchema)
            {
                getHistory(n.ObjectID);
            }
            if(tabs.SelectedTab == tabLastGood)
            {
                lastGoodCheckDBControl1.InstanceIDs = instanceIDs;
                lastGoodCheckDBControl1.connectionString = connectionString;
                lastGoodCheckDBControl1.IncludeCritical = true;
                lastGoodCheckDBControl1.IncludeWarning = true;
                lastGoodCheckDBControl1.IncludeNA = n.InstanceID>0;
                lastGoodCheckDBControl1.IncludeOK = n.InstanceID > 0;
                lastGoodCheckDBControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabPerformance)
            {
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
            if(tabs.SelectedTab== tabDBADashErrorLog)
            {
                collectionErrors1.InstanceID = n.InstanceID;
                collectionErrors1.InstanceName = n.InstanceName;
                collectionErrors1.Days = 1;
                collectionErrors1.RefreshData();
            }
            if(tabs.SelectedTab== tabCollectionDates)
            {
                collectionDates1.InstanceIDs =  n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                collectionDates1.ConnectionString = connectionString;
                collectionDates1.IncludeCritical = true;
                collectionDates1.IncludeWarning = true;
                collectionDates1.IncludeNA = n.InstanceID > 0;
                collectionDates1.IncludeOK = n.InstanceID > 0;
                collectionDates1.RefreshData();

            }
            if(tabs.SelectedTab== tabPerformanceSummary)
            {
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
                performanceSummary1.ConnectionString = connectionString;
                performanceSummary1.RefreshData();
            }
            if(tabs.SelectedTab== tabInfo)
            {
                info1.InstanceID = n.InstanceID;
                info1.ConnectionString = connectionString;
                info1.RefreshData();
            }
            if(tabs.SelectedTab== tabHardware)
            {
                hardwareChanges1.InstanceIDs = instanceIDs;
                hardwareChanges1.ConnectionString = connectionString;
                hardwareChanges1.RefreshData();
            }
            if (tabs.SelectedTab == tabSQLPatching)
            {
                sqlPatching1.InstanceIDs = instanceIDs;
                sqlPatching1.ConnectionString = connectionString;
                sqlPatching1.RefreshData();
            }
            if(tabs.SelectedTab == tabInstanceConfig)
            {                
                configurationHistory1.InstanceIDs = instanceIDs;
                configurationHistory1.ConnectionString = connectionString;
                configurationHistory1.RefreshData();
            }
            if(tabs.SelectedTab== tabSlowQueries)
            {
                slowQueries1.InstanceIDs = instanceIDs;
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
            if(tabs.SelectedTab== tabTraceFlags)
            {
                traceFlagHistory1.InstanceIDs = instanceIDs;
                traceFlagHistory1.ConnectionString = connectionString;
                traceFlagHistory1.RefreshData();
            }
            if(tabs.SelectedTab == tabAlerts)
            {
                alerts1.InstanceIDs = instanceIDs;
                alerts1.ConnectionString = connectionString;
                alerts1.RefreshData();
            }
            if (tabs.SelectedTab == tabDrivers)
            {
                drivers1.InstanceIDs = instanceIDs;
                drivers1.ConnectionString = connectionString;
                drivers1.RefreshData();
            }
            if (tabs.SelectedTab == tabDBSpace)
            {
                spaceTracking1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                spaceTracking1.DatabaseID = n.DatabaseID;
                spaceTracking1.Instance = n.InstanceName;
                spaceTracking1.DBName = n.DatabaseName;
                spaceTracking1.RefreshData();
            }
            if(tabs.SelectedTab == tabAzureSummary)
            {
                azureSummary1.InstanceIDs =n.Type == SQLTreeItem.TreeType.DBADashRoot ? AzureInstanceIDs : instanceIDs;
                azureSummary1.RefreshData();
            }
            if(tabs.SelectedTab== tabAzureDB)
            {
                azureDBResourceStats1.InstanceID = n.InstanceID;
                azureDBResourceStats1.RefreshData();
            }
            if(tabs.SelectedTab == tabServiceObjectives)
            {
                azureServiceObjectivesHistory1.InstanceIDs = ((SQLTreeItem)n.Parent).Type == SQLTreeItem.TreeType.DBADashRoot ? AzureInstanceIDs : instanceIDs;
                azureServiceObjectivesHistory1.RefreshData();
            }
            if (tabs.SelectedTab == tabDBConfiguration)
            {
                dbConfiguration1.InstanceIDs = instanceIDs;
                dbConfiguration1.DatabaseID = n.DatabaseID;
                dbConfiguration1.RefreshData();
            }
            if(tabs.SelectedTab == tabDBOptions)
            {
                dbOptions1.InstanceIDs = parent.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
                dbOptions1.DatabaseID = n.DatabaseID;
                dbOptions1.RefreshData();
            }
            if(tabs.SelectedTab == tabTempDB)
            {
                tempDBConfig1.InstanceIDs = instanceIDs;
                tempDBConfig1.RefreshData();
            }
            if (tabs.SelectedTab == tabCustomChecks)
            {
               customChecks1.InstanceIDs = n.Type == SQLTreeItem.TreeType.DBADashRoot ? AllInstanceIDs : instanceIDs;
               customChecks1.IncludeCritical = true;
               customChecks1.IncludeWarning = true;
               customChecks1.IncludeNA = n.InstanceID > 0;
               customChecks1.IncludeOK = n.InstanceID > 0;
               customChecks1.RefreshData();
            }
            if (tabs.SelectedTab == tabPC)
            {
                performanceCounterSummary1.InstanceID = n.InstanceID;
                performanceCounterSummary1.RefreshData();
            }
        }

 

        private readonly List<Int32> InstanceIDs = new List<Int32>();
        private readonly List<Int32> AllInstanceIDs=new List<Int32>();
        private readonly List<Int32> AzureInstanceIDs= new List<Int32>();
    

        #region Tree

        private void addInstanes()
        {
            tv1.Nodes.Clear();
            InstanceIDs.Clear();
            AzureInstanceIDs.Clear();
            AllInstanceIDs.Clear();
            var root = new SQLTreeItem("DBA Dash", SQLTreeItem.TreeType.DBADashRoot);
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            root.Nodes.Add(changes);
            
            var tags = String.Join(",", SelectedTags());

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"dbo.Instances_Get", cn);

                cmd.Parameters.AddWithValue("TagIDs", tags);
                cmd.CommandType = CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                SQLTreeItem AzureNode=null;
                while (rdr.Read())
                {
                    string instance = (string)rdr["Instance"];
                    Int32 instanceID = (Int32)rdr["InstanceID"];
                    if ((bool)rdr["IsAzure"])
                    {
                        string db = (string)rdr["AzureDBName"];
                        if (AzureNode == null || AzureNode.InstanceName != instance)
                        {
                            AzureNode = new SQLTreeItem(instance, SQLTreeItem.TreeType.AzureInstance);
                            root.Nodes.Add(AzureNode);
                            var cfgNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
                            AzureNode.Nodes.Add(cfgNode);
                        }
                        var azureDBNode = new SQLTreeItem(db, SQLTreeItem.TreeType.AzureDatabase)
                        {
                            DatabaseID = (Int32)rdr["AzureDatabaseID"],
                            InstanceID = instanceID
                        };
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
            }
            tv1.Nodes.Add(root);
            root.Expand();
            tv1.SelectedNode = root;
        }



        private void addDatabases(SQLTreeItem instanceNode)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT D.DatabaseID,D.name,O.ObjectID,D.InstanceID
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.DBObjects O ON O.DatabaseID = D.DatabaseID AND O.ObjectType='DB'
WHERE I.IsActive=1
AND D.IsActive=1
AND D.source_database_id IS NULL
AND I.Instance = @Instance
ORDER BY D.Name
", cn);

                var changesNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration)
                {
                    InstanceID = instanceNode.InstanceID
                };
                instanceNode.Nodes.Add(changesNode);
                
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
                    if ((new string[] { "master", "model", "msdb" ,"tempdb"}).Contains((string)rdr[1]))
                    {
                        systemNode.Nodes.Add(n);
                    }
                    else
                    {
                        instanceNode.Nodes.Add(n);
                    }
                }
            }
        }


        private void ExpandObjects(SQLTreeItem n)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT ObjectID,ObjectType,SchemaName,ObjectName
FROM dbo.DBObjects
WHERE DatabaseID=@DatabaseID
AND IsActive=1
AND ObjectType IN(SELECT value FROM STRING_SPLIT(@Types,','))
ORDER BY SchemaName,ObjectName
", cn);

                cmd.Parameters.AddWithValue("DatabaseID", n.DatabaseID);
                cmd.Parameters.AddWithValue("Types", n.Tag);
                var rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    string type = ((string)rdr[1]).Trim();
                    var objN = new SQLTreeItem((string)rdr[3], (string)rdr[2], type)
                    {
                        ObjectID = (Int64)rdr[0]
                    };
                    n.Nodes.Add(objN);
                }
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
                allowedTabs.Add(tabBackups);
                allowedTabs.Add(tabDrives);
                allowedTabs.Add(tabLogShipping);
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabLastGood);
                allowedTabs.Add(tabDBADashErrorLog);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabCustomChecks);
            }
            else if(n.Type== SQLTreeItem.TreeType.Database)
            {
                allowedTabs.Add(tabPerformance);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabSnapshotsSummary);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabDBConfiguration);
                allowedTabs.Add(tabDBOptions);
            }
            else if(n.Type == SQLTreeItem.TreeType.AzureDatabase)
            {
                allowedTabs.Add(tabPerformance);
                allowedTabs.Add(tabAzureSummary);
                allowedTabs.Add(tabAzureDB);
                allowedTabs.Add(tabDBADashErrorLog);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabSnapshotsSummary);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabServiceObjectives);
                allowedTabs.Add(tabDBConfiguration);
                allowedTabs.Add(tabDBOptions);
                allowedTabs.Add(tabCustomChecks);
                allowedTabs.Add(tabPC);
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.Add(tabPerformanceSummary);
                allowedTabs.Add(tabPerformance);                  
                allowedTabs.Add(tabSummary);
                allowedTabs.Add(tabBackups);
                allowedTabs.Add(tabDrives);              
                allowedTabs.Add(tabLogShipping);
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabSnapshotsSummary);
                allowedTabs.Add(tabLastGood);
                allowedTabs.Add(tabDBADashErrorLog);
                allowedTabs.Add(tabInfo);                   
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabTags);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabCustomChecks);
                allowedTabs.Add(tabPC);
            }
            else if(n.Type == SQLTreeItem.TreeType.AzureInstance)
            {
                allowedTabs.Add(tabPerformanceSummary);         
                allowedTabs.Add(tabAzureSummary);              
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabTags);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabDBSpace);
                allowedTabs.Add(tabCustomChecks);
                allowedTabs.Add(tabDBADashErrorLog);
            }
            else if (n.Type == SQLTreeItem.TreeType.Configuration)
            {
                if(parent.Type != SQLTreeItem.TreeType.AzureDatabase && parent.Type != SQLTreeItem.TreeType.AzureInstance)
                {
                    allowedTabs.Add(tabInstanceConfig);
                    allowedTabs.Add(tabTraceFlags);
                    allowedTabs.Add(tabHardware);
                    allowedTabs.Add(tabSQLPatching);
                    allowedTabs.Add(tabAlerts);
                    allowedTabs.Add(tabDrivers);
                    allowedTabs.Add(tabTempDB);
                }
                if (parent.Type != SQLTreeItem.TreeType.Instance && hasAzureDBs)
                {
                    allowedTabs.Add(tabServiceObjectives);
                }
                allowedTabs.Add(tabDBConfiguration);
                allowedTabs.Add(tabDBOptions);
            }
            if (n.ObjectID > 0)
            {
                if (n.Type == SQLTreeItem.TreeType.StoredProcedure || n.Type == SQLTreeItem.TreeType.CLRProcedure || n.Type == SQLTreeItem.TreeType.ScalarFunction || n.Type == SQLTreeItem.TreeType.CLRScalarFunction || n.Type == SQLTreeItem.TreeType.Trigger || n.Type == SQLTreeItem.TreeType.CLRTrigger)
                {
                    allowedTabs.Add(tabPerformance);
                }
                allowedTabs.Add(tabSchema);               
            }
            this.Text ="DBA Dash" + (n.Type== SQLTreeItem.TreeType.DBADashRoot ? "" : " - " + n.InstanceName);

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
                else
                {
                    ExpandObjects(n);
                }

            }
        }

        #endregion

        #region SchemaSnapshots
        private void loadDDL(Int64 DDLID, Int64 DDLIDOld)
        {
            string newText = Common.DDL(DDLID, connectionString);
            string oldText = Common.DDL(DDLIDOld, connectionString);
            diffSchemaSnapshot.OldText = oldText;
            diffSchemaSnapshot.NewText = newText;
        }

        private void getHistory(Int64 ObjectID, Int32 PageNum = 1)
        {
            diffSchemaSnapshot.OldText = "";
            diffSchemaSnapshot.NewText = "";
            currentPageSize = Int32.Parse(tsPageSize.Text);
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DDLHistoryForObject_Get", cn);
                cmd.Parameters.AddWithValue("ObjectID", ObjectID);
                cmd.Parameters.AddWithValue("PageSize", currentPageSize);
                cmd.Parameters.AddWithValue("PageNumber", PageNum);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                gvHistory.AutoGenerateColumns = false;
                gvHistory.DataSource = ds.Tables[0];
                currentObjectID = ObjectID;
                currentPage = PageNum;
                tsPageNum.Text = "Page " + PageNum;

                tsPrevious.Enabled = (PageNum > 1);
                tsNext.Enabled = ds.Tables[0].Rows.Count == currentPageSize;

            }
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

        private void gvSnapshots_SelectionChanged(object sender, EventArgs e)
        {
            if (gvSnapshots.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvSnapshots.SelectedRows[0].DataBoundItem;
                DateTime SnapshotDate = (DateTime)row["SnapshotDate"];
                Int32 DatabaseID = (Int32)row["DatabaseID"];
                SqlConnection cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();

                    SqlCommand cmd = new SqlCommand("dbo.DDLSnapshotDiff_Get", cn);
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    cmd.CommandType = CommandType.StoredProcedure;
                    var p = cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    p.DbType = DbType.DateTime2;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    gvSnapshotsDetail.AutoGenerateColumns = false;
                    gvSnapshotsDetail.DataSource = ds.Tables[0];




                }
            }
        }

        private void gvSnapshotsDetail_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var colCount = gvSnapshotsDetail.Columns.Count;

            if (e.ColumnIndex == colCount - 1 || e.ColumnIndex == colCount - 2)
            {
                var row = (DataRowView)gvSnapshotsDetail.Rows[e.RowIndex].DataBoundItem;
                string ddl = "";
                if (row["NewDDLID"] != DBNull.Value)
                {
                    ddl = Common.DDL((Int64)row["NewDDLID"], connectionString);
                }
                string ddlOld = "";
                if (row["OldDDLID"] != DBNull.Value)
                {
                    ddlOld = Common.DDL((Int64)row["OldDDLID"], connectionString);
                }
                ViewMode mode = ViewMode.Diff;
                if (e.ColumnIndex == colCount - 2)
                {
                    mode = ViewMode.Code;
                }
                var frm = new Diff();
                frm.setText(ddlOld, ddl, mode);
                frm.Show();
            }
        }

        private void loadSnapshots(Int32 pageNum = 1)
        {
            gvSnapshotsDetail.DataSource = null;
            var n = (SQLTreeItem)tv1.SelectedNode;
            currentSummaryPage = Int32.Parse(tsSummaryPageSize.Text);
            if (n.Type == SQLTreeItem.TreeType.Database || n.Type == SQLTreeItem.TreeType.Instance)
            {
                SqlConnection cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();

                    SqlCommand cmd = new SqlCommand("dbo.DDLSnapshots_Get", cn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("DatabaseID", n.DatabaseID);
                    cmd.Parameters.AddWithValue("Instance", n.InstanceName);
                    cmd.Parameters.AddWithValue("PageSize", currentSummaryPage);
                    cmd.Parameters.AddWithValue("PageNumber", pageNum);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    gvSnapshots.AutoGenerateColumns = false;
                    gvSnapshots.DataSource = ds.Tables[0];

                    tsSummaryPageNum.Text = "Page " + pageNum;
                    tsSummaryBack.Enabled = (pageNum > 1);
                    tsSummaryNext.Enabled = ds.Tables[0].Rows.Count == currentSummaryPage;
                    currentSummaryPage = pageNum;

                }
            }
        }
        private void tsSummaryBack_Click(object sender, EventArgs e)
        {
            loadSnapshots(currentSummaryPage - 1);
        }

        private void tsSummaryNext_Click(object sender, EventArgs e)
        {
            loadSnapshots(currentSummaryPage + 1);
        }

        private void tsSummaryPageSize_Validated(object sender, EventArgs e)
        {
            if (Int32.Parse(tsSummaryPageSize.Text) != currentPageSize)
            {
                loadSnapshots(1);
            }
        }

        private void tsSummaryPageSize_Validating(object sender, CancelEventArgs e)
        {
            int.TryParse(tsSummaryPageSize.Text, out int i);
            if (i <= 0)
            {
                tsSummaryPageSize.Text = currentSummaryPageSize.ToString();
            }
        }


        private void dBDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new DBDiff
            {
                ConnectionString = connectionString
            };
            var n = (SQLTreeItem)tv1.SelectedNode;
            frm.SelectedInstanceA = n.InstanceName;
            frm.SelectedDatabaseA = new DBDiff.DatabaseItem() { DatabaseID = n.DatabaseID, DatabaseName = n.DatabaseName };
            frm.ShowDialog();
        }


        #endregion

        #region Tagging

        bool isClearTags = false;

        private void buildTagMenu(List<Int16> selected = null)
        {
            mnuTags.DropDownItems.Clear();
            cboTagName.Items.Clear();
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Tags_Get", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                var rdr = cmd.ExecuteReader();
                string currentTag = String.Empty, tag, tagValue;
                ToolStripMenuItem mTagName = new ToolStripMenuItem();
                ToolStripMenuItem mSystemTags = new ToolStripMenuItem("System Tags");
                mSystemTags.Font = new Font(mSystemTags.Font, FontStyle.Italic);
                Int16 tagID;
                while (rdr.Read())
                {
                    tag = (string)rdr[1];
                    tagValue = (string)rdr[2];
                    tagID = (Int16)rdr[0];
                    if (tag != currentTag)
                    {
                        mTagName = new ToolStripMenuItem(tag);
                        
                        if (tag.StartsWith("{"))
                        {
                            mSystemTags.DropDownItems.Add(mTagName);
                        }
                        else
                        {
                            mnuTags.DropDownItems.Add(mTagName);
                            cboTagName.Items.Add(tag);
                        }
                       
                        currentTag = tag;
                    }
                    var mTagValue = new ToolStripMenuItem(tagValue)
                    {
                        Tag = tagID,
                        CheckOnClick = true
                    };

                    if (selected != null && selected.Contains(tagID))
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
            }
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


        private void getTags()
        {
            isTagPopulation = true;
            SQLTreeItem n = (SQLTreeItem)tv1.SelectedNode;
            chkTags.Items.Clear();
            var tags = InstanceTag.GetInstanceTags(connectionString, n.InstanceName);
            foreach (var t in tags)
            {
                if (!t.TagName.StartsWith("{")) { chkTags.Items.Add(t, t.IsTagged); }
            }
            isTagPopulation = false;
        }


        private void chkTags_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!isTagPopulation)
            {
                var InstanceTag = (InstanceTag)chkTags.Items[e.Index];
                if (e.NewValue == CheckState.Checked)
                {
                    InstanceTag.Save(connectionString);
                }
                else
                {
                    InstanceTag.Delete(connectionString);
                }
            }
        }

        private void bttnAdd_Click(object sender, EventArgs e)
        {
            if (cboTagName.Text.StartsWith("{")){
                MessageBox.Show("Invalid TagName.  TagNames starting with { are system reserved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            SQLTreeItem n = (SQLTreeItem)tv1.SelectedNode;
            InstanceTag newTag = new InstanceTag() { Instance = n.InstanceName, TagName = cboTagName.Text, TagValue = cboTagValue.Text };
            newTag.Save(connectionString);
            getTags();
            buildTagMenu(SelectedTags());
        }

        private void cboTagName_SelectedValueChanged(object sender, EventArgs e)
        {
            cboTagValue.Items.Clear();
            foreach(ToolStripMenuItem mnuName in mnuTags.DropDownItems)
            {
                if(mnuName.Text == cboTagName.Text)
                {
                    foreach (ToolStripMenuItem mnuValue in mnuName.DropDownItems) {
                        cboTagValue.Items.Add(mnuValue.Text);
                    }
                    break;
                }
            }
        }

        private List<Int16> SelectedTags(ToolStripMenuItem mnu = null)
        {
            if (mnu == null)
            {
                mnu = mnuTags;
                
            }
            var selected = new List<Int16>();
            foreach (ToolStripItem mnuTag in mnu.DropDownItems)
            {
                if (mnuTag.GetType() == typeof(ToolStripMenuItem))
                {
                    if (((ToolStripMenuItem)mnuTag).Checked)
                    {
                        selected.Add((Int16)mnuTag.Tag);                 
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
                    if (e.Tab == "tabAlerts") // Configuration Node
                    {
                        child.Expand();
                        tv1.SelectedNode = child.Nodes[0];
                    }
                    else
                    {
                        tv1.SelectedNode = child;
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

        private void tsErrorsDays_Click(object sender, EventArgs e)
        {

        }
    }
}
