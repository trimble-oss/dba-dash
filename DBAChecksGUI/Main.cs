using DBAChecks;
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
using static DBAChecksGUI.DiffControl;

namespace DBAChecksGUI
{


    public partial class Main : Form
    {

        public Main()
        {
            InitializeComponent();
        }

        string connectionString = "Data Source=.;Initial Catalog=DBAChecksDB;Integrated Security=SSPI";

        Int32 currentSummaryPage = 1;
        Int32 currentSummaryPageSize = 100;
        private Int64 currentObjectID;
        private Int32 currentPage=1;
        private Int32 currentPageSize=100;
        bool isTagPopulation = false;
        private DiffControl diffSchemaSnapshot = new DiffControl();


        private void Main_Load(object sender, EventArgs e)
        {
            
            splitSchemaSnapshot.Panel1.Controls.Add(diffSchemaSnapshot);
            diffSchemaSnapshot.Dock = DockStyle.Fill;

            string jsonPath = System.IO.Path.Combine(Application.StartupPath, "ServiceConfig.json");
            if (System.IO.File.Exists(jsonPath))
            {
                string jsonConfig = System.IO.File.ReadAllText(jsonPath);
                var cfg= CollectionConfig.Deserialize(jsonConfig);
                connectionString = cfg.DestinationConnection.ConnectionString;
            }
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            builder.ApplicationName = "DBAChecksGUI";
            connectionString = builder.ConnectionString;

            addInstanes();
            buildTagMenu();
            loadSelectedTab();

        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadSelectedTab();
        }

        private void loadSelectedTab()
        {
            performance1.StopTimer();
            List<Int32> instanceIDs;         
            var n = (SQLTreeItem) tv1.SelectedNode;
            if (n.InstanceID > 0)
            {
                instanceIDs = new List<Int32>() { n.InstanceID };
            }
            else
            {

               
                if (n.Type == SQLTreeItem.TreeType.Instance && !(n.InstanceID>0))
                {
                    instanceIDs = ChildInstanceIDs(n);
                }
                else
                {
                    instanceIDs = InstanceIDs();
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
                drivesControl1.IncludeNA = (n.Type != SQLTreeItem.TreeType.DBAChecksRoot);
                drivesControl1.IncludeOK = (n.Type != SQLTreeItem.TreeType.DBAChecksRoot);
                drivesControl1.IncludeWarning = true;
                drivesControl1.IncludeCritical = true;
                drivesControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabBackups)
            {
                backupsControl1.InstanceIDs = instanceIDs;
                backupsControl1.ConnectionString = connectionString;          
                backupsControl1.IncludeNA = (n.Type != SQLTreeItem.TreeType.DBAChecksRoot);
                backupsControl1.IncludeOK = (n.Type != SQLTreeItem.TreeType.DBAChecksRoot);
                backupsControl1.IncludeWarning = true;
                backupsControl1.IncludeCritical = true;
                backupsControl1.RefreshBackups();
            }
            if(tabs.SelectedTab== tabLogShipping)
            {
                logShippingControl1.IncludeNA = n.Type != SQLTreeItem.TreeType.DBAChecksRoot;
                logShippingControl1.IncludeOK = n.Type != SQLTreeItem.TreeType.DBAChecksRoot;
                logShippingControl1.IncludeWarning = true;
                logShippingControl1.IncludeCritical = true;
                logShippingControl1.InstanceIDs = instanceIDs;
                logShippingControl1.ConnectionString = connectionString;
                logShippingControl1.RefreshData();
            }
            if(tabs.SelectedTab== tabJobs)
            {
                agentJobsControl1.IncludeNA = n.Type != SQLTreeItem.TreeType.DBAChecksRoot;
                agentJobsControl1.IncludeOK = n.Type != SQLTreeItem.TreeType.DBAChecksRoot;
                agentJobsControl1.IncludeWarning = true;
                agentJobsControl1.IncludeCritical = true;
                agentJobsControl1.ConnectionString = connectionString;
                agentJobsControl1.InstanceIDs = instanceIDs;
                agentJobsControl1.RefreshData();
            }
            if(tabs.SelectedTab == tabSummary)
            {
                summary1.ConnectionString = connectionString;
                summary1.InstanceIDs = instanceIDs;
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
            if(tabs.SelectedTab== tabDBAChecksErrorLog)
            {
                loadDBAChecksErrorLog(n.InstanceID);
            }
            if(tabs.SelectedTab== tabCollectionDates)
            {
                collectionDates1.InstanceIDs = instanceIDs;
                collectionDates1.ConnectionString = connectionString;
                collectionDates1.IncludeCritical = true;
                collectionDates1.IncludeWarning = true;
                collectionDates1.IncludeNA = n.InstanceID > 0;
                collectionDates1.IncludeOK = n.InstanceID > 0;
                collectionDates1.RefreshData();

            }
            if(tabs.SelectedTab== tabPerformanceSummary)
            {
                if (n.Type == SQLTreeItem.TreeType.DBAChecksRoot)
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
                spaceTracking1.InstanceIDs = instanceIDs;
                spaceTracking1.DatabaseID = n.DatabaseID;
                spaceTracking1.ConnectionString = connectionString;
                spaceTracking1.RefreshData();
            }
        }

        private void loadDBAChecksErrorLog(Int32 InstanceID)
        {
            SqlConnection cn = new SqlConnection(connectionString);

            using (cn)
            {
                SqlCommand cmd = new SqlCommand("dbo.CollectionErrorLog_Get",cn);
                cmd.CommandType = CommandType.StoredProcedure;
                if (InstanceID > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
               
                da.Fill(dt);
                dgvDBAChecksErrors.AutoGenerateColumns = false;
                dgvDBAChecksErrors.DataSource = dt;
            }

        }

    

        private List<Int32> InstanceIDs()
        {
            var instanceIDs = new List<Int32>();
            foreach(SQLTreeItem itm in tv1.Nodes[0].Nodes)
            {
                if (itm.InstanceID > 0)
                {
                    instanceIDs.Add(itm.InstanceID);
                }
            }
            return instanceIDs;
        }

        private List<Int32> ChildInstanceIDs(SQLTreeItem n)
        {
            var instanceIDs = new List<Int32>();
            if(n.Nodes.Count==1 && n.Type == SQLTreeItem.TreeType.Instance && ((SQLTreeItem)n.Nodes[0]).Type == SQLTreeItem.TreeType.DummyNode)
            {
                n.Nodes.Clear();
                addDatabases(n);
            }
            foreach (SQLTreeItem itm in n.Nodes)
            {
                if (itm.InstanceID > 0)
                {
                    instanceIDs.Add(itm.InstanceID);
                }
            }
            return instanceIDs;
        }


        #region Tree

        private void addInstanes()
        {
            tv1.Nodes.Clear();
            var root = new SQLTreeItem("DBAChecks", SQLTreeItem.TreeType.DBAChecksRoot);
            var changes = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
            root.Nodes.Add(changes);
            
            var tags = String.Join(",", SelectedTags());

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT Instance,CASE WHEN EditionID <> 1674378470 THEN InstanceID ELSE NULL END as InstanceID
FROM dbo.InstancesMatchingTags(@TagIDs) I
WHERE I.IsActive=1
GROUP BY Instance,CASE WHEN EditionID <> 1674378470 THEN InstanceID ELSE NULL END
ORDER BY Instance", cn);

                cmd.Parameters.AddWithValue("TagIDs", tags);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var n = new SQLTreeItem((string)rdr[0], SQLTreeItem.TreeType.Instance);
                    if (rdr["InstanceID"] != DBNull.Value)
                    {
                        n.InstanceID = (Int32)rdr["InstanceID"];
                    }
                    n.AddDummyNode();
                    root.Nodes.Add(n);
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
                //    cmd.CommandType = CommandType.StoredProcedure;
                if (instanceNode.InstanceID > 0)
                {
                    var changesNode = new SQLTreeItem("Configuration", SQLTreeItem.TreeType.Configuration);
                    changesNode.InstanceID = instanceNode.InstanceID;
                    instanceNode.Nodes.Add(changesNode);
                }
                cmd.Parameters.AddWithValue("Instance", instanceNode.ObjectName);
                var systemNode = new SQLTreeItem("System Databases", SQLTreeItem.TreeType.Folder);
                instanceNode.Nodes.Add(systemNode);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var n = new SQLTreeItem((string)rdr[1], SQLTreeItem.TreeType.Database);
                    n.DatabaseID = (Int32)rdr[0];
                    n.InstanceID = (Int32)rdr[3];
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
                    var objN = new SQLTreeItem((string)rdr[3], (string)rdr[2], type);
                    objN.ObjectID = (Int64)rdr[0];
                    n.Nodes.Add(objN);
                }
            }

        }

        private SQLTreeItem selectedItem;

        private void tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var n = (SQLTreeItem)e.Node;

            List<TabPage> allowedTabs = new List<TabPage>();         
            selectedItem = n;


            if(n.Type== SQLTreeItem.TreeType.DBAChecksRoot)
            {
                allowedTabs.Add(tabSummary);
                allowedTabs.Add(tabPerformanceSummary);
                allowedTabs.Add(tabBackups);
                allowedTabs.Add(tabDrives);
                allowedTabs.Add(tabLogShipping);
                allowedTabs.Add(tabJobs);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabLastGood);
                allowedTabs.Add(tabDBAChecksErrorLog);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabDBSpace);
            }
            else if(n.Type== SQLTreeItem.TreeType.Database)
            {
                allowedTabs.Add(tabPerformance);
                if (((SQLTreeItem)n.Parent).InstanceID == 0) //azure
                {                   
                    allowedTabs.Add(tabDBAChecksErrorLog);
                }
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabSnapshotsSummary);
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabDBSpace);
            }
            else if (n.Type == SQLTreeItem.TreeType.Instance)
            {
                allowedTabs.Add(tabPerformanceSummary);
                if (n.InstanceID > 0){
                    allowedTabs.Add(tabPerformance);                  
                    allowedTabs.Add(tabSummary);
                    allowedTabs.Add(tabBackups);
                    allowedTabs.Add(tabDrives);              
                    allowedTabs.Add(tabLogShipping);
                    allowedTabs.Add(tabJobs);
                    allowedTabs.Add(tabSnapshotsSummary);
                    allowedTabs.Add(tabLastGood);
                    allowedTabs.Add(tabDBAChecksErrorLog);
                    allowedTabs.Add(tabInfo);                   
                }
                allowedTabs.Add(tabSlowQueries);
                allowedTabs.Add(tabFiles);
                allowedTabs.Add(tabTags);
                allowedTabs.Add(tabCollectionDates);
                allowedTabs.Add(tabDBSpace);
            }
            else if (n.Type == SQLTreeItem.TreeType.Configuration)
            {
                allowedTabs.Add(tabInstanceConfig);
                allowedTabs.Add(tabTraceFlags);
                allowedTabs.Add(tabHardware);
                allowedTabs.Add(tabSQLPatching);
                allowedTabs.Add(tabAlerts);
                allowedTabs.Add(tabDrivers);
            }
            if (n.ObjectID > 0)
            {
                if (n.Type == SQLTreeItem.TreeType.StoredProcedure || n.Type == SQLTreeItem.TreeType.CLRProcedure || n.Type == SQLTreeItem.TreeType.ScalarFunction || n.Type == SQLTreeItem.TreeType.CLRScalarFunction || n.Type == SQLTreeItem.TreeType.Trigger || n.Type == SQLTreeItem.TreeType.CLRTrigger)
                {
                    allowedTabs.Add(tabPerformance);
                }
                allowedTabs.Add(tabSchema);               
            }
            this.Text ="DBAChecks" + (n.Type== SQLTreeItem.TreeType.DBAChecksRoot ? "" : " - " + n.InstanceName);

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
            Int32 i;
            Int32.TryParse(tsPageSize.Text, out i);
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

                    SqlCommand cmd = new SqlCommand("dbo.DDLSnapshots_Get", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
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
            Int32 i;
            Int32.TryParse(tsSummaryPageSize.Text, out i);
            if (i <= 0)
            {
                tsSummaryPageSize.Text = currentSummaryPageSize.ToString();
            }
        }


        private void dBDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new DBDiff();
            frm.ConnectionString = connectionString;
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
                SqlCommand cmd = new SqlCommand("Tags_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
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
                    var mTagValue = new ToolStripMenuItem(tagValue);
                    mTagValue.Tag = tagID;
                    mTagValue.CheckOnClick = true;

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
            SqlConnection cn = new SqlConnection(connectionString);
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
                SQLTreeItem n = (SQLTreeItem)tv1.SelectedNode;
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

        private void tsRefreshErrors_Click(object sender, EventArgs e)
        {
            SQLTreeItem n = (SQLTreeItem)tv1.SelectedNode;
            loadDBAChecksErrorLog(n.InstanceID);
        }

        private void tsCopyErrors_Click(object sender, EventArgs e)
        {
            
            Common.CopyDataGridViewToClipboard(dgvDBAChecksErrors);

        }

        private void dataRetentionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new DataRetention();
            frm.ConnectionString = connectionString;
            frm.ShowDialog();
        }
    }
}
