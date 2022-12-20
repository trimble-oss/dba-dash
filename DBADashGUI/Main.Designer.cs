namespace DBADashGUI
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.TreeViewImageList = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tsConnect = new System.Windows.Forms.ToolStripMenuItem();
            this.diffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.databaseSchemaDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.agentJobDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureDisplayNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataRetentionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.freezeKeyColumnsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showHiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveTimeZonePreferenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.minsToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.minsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.minsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.daysToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days14toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days28ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.dateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dummyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsTimeFilter = new System.Windows.Forms.ToolStripMenuItem();
            this.tsDayOfWeek = new System.Windows.Forms.ToolStripMenuItem();
            this.cboTimeZone = new System.Windows.Forms.ToolStripComboBox();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tv1 = new System.Windows.Forms.TreeView();
            this.pnlSearch = new System.Windows.Forms.Panel();
            this.bttnSearch = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsHome = new System.Windows.Forms.ToolStripButton();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.mnuTags = new System.Windows.Forms.ToolStripDropDownButton();
            this.groupToolStripMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabSnapshotsSummary = new System.Windows.Forms.TabPage();
            this.schemaSnapshots1 = new DBADashGUI.Changes.SchemaSnapshots();
            this.tabSchema = new System.Windows.Forms.TabPage();
            this.splitSchemaSnapshot = new System.Windows.Forms.SplitContainer();
            this.label7 = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsPageNum = new System.Windows.Forms.ToolStripLabel();
            this.tsNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsPageSize = new System.Windows.Forms.ToolStripComboBox();
            this.gvHistory = new System.Windows.Forms.DataGridView();
            this.ObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ObjectType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ObjectDateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ObjectDateModified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCompare = new System.Windows.Forms.DataGridViewLinkColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.tabTags = new System.Windows.Forms.TabPage();
            this.tags1 = new DBADashGUI.Tagging.Tags();
            this.tabDrives = new System.Windows.Forms.TabPage();
            this.drivesControl1 = new DBADashGUI.Properties.DrivesControl();
            this.tabBackups = new System.Windows.Forms.TabPage();
            this.backupsControl1 = new DBADashGUI.Backups.BackupsControl();
            this.tabLogShipping = new System.Windows.Forms.TabPage();
            this.logShippingControl1 = new DBADashGUI.LogShipping.LogShippingControl();
            this.tabJobs = new System.Windows.Forms.TabPage();
            this.agentJobsControl1 = new DBADashGUI.AgentJobs.AgentJobsControl();
            this.tabSummary = new System.Windows.Forms.TabPage();
            this.summary1 = new DBADashGUI.Summary();
            this.tabFiles = new System.Windows.Forms.TabPage();
            this.dbFilesControl1 = new DBADashGUI.DBFiles.DBFilesControl();
            this.tabLastGood = new System.Windows.Forms.TabPage();
            this.lastGoodCheckDBControl1 = new DBADashGUI.LastGoodCheckDB.LastGoodCheckDBControl();
            this.tabPerformance = new System.Windows.Forms.TabPage();
            this.performance1 = new DBADashGUI.Performance.Performance();
            this.tabDBADashErrorLog = new System.Windows.Forms.TabPage();
            this.collectionErrors1 = new DBADashGUI.CollectionDates.CollectionErrors();
            this.tabCollectionDates = new System.Windows.Forms.TabPage();
            this.collectionDates1 = new DBADashGUI.CollectionDates.CollectionDates();
            this.tabPerformanceSummary = new System.Windows.Forms.TabPage();
            this.performanceSummary1 = new DBADashGUI.Performance.PerformanceSummary();
            this.tabInfo = new System.Windows.Forms.TabPage();
            this.info1 = new DBADashGUI.Info();
            this.tabHardware = new System.Windows.Forms.TabPage();
            this.hardwareChanges1 = new DBADashGUI.HardwareChanges();
            this.tabSQLPatching = new System.Windows.Forms.TabPage();
            this.sqlPatching1 = new DBADashGUI.SQLPatching();
            this.tabInstanceConfig = new System.Windows.Forms.TabPage();
            this.configurationHistory1 = new DBADashGUI.ConfigurationHistory();
            this.tabSlowQueries = new System.Windows.Forms.TabPage();
            this.slowQueries1 = new DBADashGUI.SlowQueries();
            this.tabTraceFlags = new System.Windows.Forms.TabPage();
            this.traceFlagHistory1 = new DBADashGUI.Changes.TraceFlagHistory();
            this.tabAlerts = new System.Windows.Forms.TabPage();
            this.alerts1 = new DBADashGUI.Changes.Alerts();
            this.tabDrivers = new System.Windows.Forms.TabPage();
            this.drivers1 = new DBADashGUI.Changes.Drivers();
            this.tabDBSpace = new System.Windows.Forms.TabPage();
            this.spaceTracking1 = new DBADashGUI.SpaceTracking();
            this.tabAzureSummary = new System.Windows.Forms.TabPage();
            this.azureSummary1 = new DBADashGUI.Performance.AzureSummary();
            this.tabAzureDB = new System.Windows.Forms.TabPage();
            this.azureDBResourceStats1 = new DBADashGUI.Performance.AzureDBResourceStats();
            this.tabServiceObjectives = new System.Windows.Forms.TabPage();
            this.azureServiceObjectivesHistory1 = new DBADashGUI.Changes.AzureServiceObjectivesHistory();
            this.tabDBConfiguration = new System.Windows.Forms.TabPage();
            this.dbConfiguration1 = new DBADashGUI.Changes.DBConfiguration();
            this.tabDBOptions = new System.Windows.Forms.TabPage();
            this.dbOptions1 = new DBADashGUI.Changes.DBOptions();
            this.tabTempDB = new System.Windows.Forms.TabPage();
            this.tempDBConfig1 = new DBADashGUI.DBFiles.TempDBConfig();
            this.tabCustomChecks = new System.Windows.Forms.TabPage();
            this.customChecks1 = new DBADashGUI.Checks.CustomChecks();
            this.tabPC = new System.Windows.Forms.TabPage();
            this.performanceCounterSummary1 = new DBADashGUI.Performance.PerformanceCounterSummary();
            this.tabObjectExecutionSummary = new System.Windows.Forms.TabPage();
            this.objectExecutionSummary1 = new DBADashGUI.Performance.ObjectExecutionSummary();
            this.tabWaits = new System.Windows.Forms.TabPage();
            this.waitsSummary1 = new DBADashGUI.Performance.WaitsSummary();
            this.tabMirroring = new System.Windows.Forms.TabPage();
            this.mirroring1 = new DBADashGUI.HA.Mirroring();
            this.tabJobDDL = new System.Windows.Forms.TabPage();
            this.jobDDLHistory1 = new DBADashGUI.Changes.JobDDLHistory();
            this.tabAG = new System.Windows.Forms.TabPage();
            this.ag1 = new DBADashGUI.HA.AG();
            this.tabQS = new System.Windows.Forms.TabPage();
            this.queryStore1 = new DBADashGUI.Changes.QueryStore();
            this.tabRG = new System.Windows.Forms.TabPage();
            this.resourceGovernor1 = new DBADashGUI.Changes.ResourceGovernor();
            this.tabAzureDBesourceGovernance = new System.Windows.Forms.TabPage();
            this.azureDBResourceGovernance1 = new DBADashGUI.Changes.AzureDBResourceGovernance();
            this.tabRunningQueries = new System.Windows.Forms.TabPage();
            this.runningQueries1 = new DBADashGUI.Performance.RunningQueries();
            this.tabMemory = new System.Windows.Forms.TabPage();
            this.memoryUsage1 = new DBADashGUI.Performance.MemoryUsage();
            this.tabJobStats = new System.Windows.Forms.TabPage();
            this.jobStats1 = new DBADashGUI.AgentJobs.JobStats();
            this.tabDBADash = new System.Windows.Forms.TabPage();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblSQLMonitoring = new System.Windows.Forms.Label();
            this.lblDBADash = new System.Windows.Forms.Label();
            this.tabIdentityColumns = new System.Windows.Forms.TabPage();
            this.identityColumns1 = new DBADashGUI.Checks.IdentityColumns();
            this.tabOSLoadedModules = new System.Windows.Forms.TabPage();
            this.osLoadedModules1 = new DBADashGUI.Checks.OSLoadedModules();
            this.tabJobTimeline = new System.Windows.Forms.TabPage();
            this.jobTimeline1 = new DBADashGUI.AgentJobs.JobTimeline();
            this.refresh1 = new DBADashGUI.Refresh();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlSearch.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabSnapshotsSummary.SuspendLayout();
            this.tabSchema.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitSchemaSnapshot)).BeginInit();
            this.splitSchemaSnapshot.Panel1.SuspendLayout();
            this.splitSchemaSnapshot.Panel2.SuspendLayout();
            this.splitSchemaSnapshot.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gvHistory)).BeginInit();
            this.tabTags.SuspendLayout();
            this.tabDrives.SuspendLayout();
            this.tabBackups.SuspendLayout();
            this.tabLogShipping.SuspendLayout();
            this.tabJobs.SuspendLayout();
            this.tabSummary.SuspendLayout();
            this.tabFiles.SuspendLayout();
            this.tabLastGood.SuspendLayout();
            this.tabPerformance.SuspendLayout();
            this.tabDBADashErrorLog.SuspendLayout();
            this.tabCollectionDates.SuspendLayout();
            this.tabPerformanceSummary.SuspendLayout();
            this.tabInfo.SuspendLayout();
            this.tabHardware.SuspendLayout();
            this.tabSQLPatching.SuspendLayout();
            this.tabInstanceConfig.SuspendLayout();
            this.tabSlowQueries.SuspendLayout();
            this.tabTraceFlags.SuspendLayout();
            this.tabAlerts.SuspendLayout();
            this.tabDrivers.SuspendLayout();
            this.tabDBSpace.SuspendLayout();
            this.tabAzureSummary.SuspendLayout();
            this.tabAzureDB.SuspendLayout();
            this.tabServiceObjectives.SuspendLayout();
            this.tabDBConfiguration.SuspendLayout();
            this.tabDBOptions.SuspendLayout();
            this.tabTempDB.SuspendLayout();
            this.tabCustomChecks.SuspendLayout();
            this.tabPC.SuspendLayout();
            this.tabObjectExecutionSummary.SuspendLayout();
            this.tabWaits.SuspendLayout();
            this.tabMirroring.SuspendLayout();
            this.tabJobDDL.SuspendLayout();
            this.tabAG.SuspendLayout();
            this.tabQS.SuspendLayout();
            this.tabRG.SuspendLayout();
            this.tabAzureDBesourceGovernance.SuspendLayout();
            this.tabRunningQueries.SuspendLayout();
            this.tabMemory.SuspendLayout();
            this.tabJobStats.SuspendLayout();
            this.tabDBADash.SuspendLayout();
            this.tabIdentityColumns.SuspendLayout();
            this.tabOSLoadedModules.SuspendLayout();
            this.tabJobTimeline.SuspendLayout();
            this.SuspendLayout();
            // 
            // TreeViewImageList
            // 
            this.TreeViewImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.TreeViewImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeViewImageList.ImageStream")));
            this.TreeViewImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.TreeViewImageList.Images.SetKeyName(0, "ServerProject_16x.png");
            this.TreeViewImageList.Images.SetKeyName(1, "DataServer_16x.png");
            this.TreeViewImageList.Images.SetKeyName(2, "Database_16x.png");
            this.TreeViewImageList.Images.SetKeyName(3, "FolderClosed_16x.png");
            this.TreeViewImageList.Images.SetKeyName(4, "Table_16x.png");
            this.TreeViewImageList.Images.SetKeyName(5, "StoredProcedureScript_16x.png");
            this.TreeViewImageList.Images.SetKeyName(6, "FilterFolderOpened_16x.png");
            this.TreeViewImageList.Images.SetKeyName(7, "DatabaseProperty_16x.png");
            this.TreeViewImageList.Images.SetKeyName(8, "Cloud_blue_whiteHalo_16x.png");
            this.TreeViewImageList.Images.SetKeyName(9, "CloudDatabase_16x.png");
            this.TreeViewImageList.Images.SetKeyName(10, "Checklist_16x.png");
            this.TreeViewImageList.Images.SetKeyName(11, "Tag_16x.png");
            this.TreeViewImageList.Images.SetKeyName(12, "BatchFile_16x.png");
            this.TreeViewImageList.Images.SetKeyName(13, "Cube_16x.png");
            this.TreeViewImageList.Images.SetKeyName(14, "DataSourceTarget_16x.png");
            this.TreeViewImageList.Images.SetKeyName(15, "PowerShellFile_16x.png");
            this.TreeViewImageList.Images.SetKeyName(16, "SQLFile_16x.png");
            this.TreeViewImageList.Images.SetKeyName(17, "ProcedureMissing_16x.png");
            this.TreeViewImageList.Images.SetKeyName(18, "CloudServer_16x.png");
            this.TreeViewImageList.Images.SetKeyName(19, "DataServer_16x_BWLight.png");
            this.TreeViewImageList.Images.SetKeyName(20, "FolderClosedBlue_16x.png");
            // 
            // menuStrip1
            // 
            this.menuStrip1.AutoSize = false;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsConnect,
            this.diffToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.tsTime,
            this.helpToolStripMenuItem,
            this.tsTimeFilter,
            this.tsDayOfWeek,
            this.cboTimeZone});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1983, 34);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tsConnect
            // 
            this.tsConnect.Image = global::DBADashGUI.Properties.Resources.ConnectToDatabase_16x;
            this.tsConnect.Name = "tsConnect";
            this.tsConnect.Size = new System.Drawing.Size(97, 30);
            this.tsConnect.Text = "Connect";
            this.tsConnect.ToolTipText = "Connect to a different DBA Dash repository";
            this.tsConnect.Click += new System.EventHandler(this.TsConnect_Click);
            // 
            // diffToolStripMenuItem
            // 
            this.diffToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.databaseSchemaDiffToolStripMenuItem,
            this.agentJobDiffToolStripMenuItem});
            this.diffToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.Diff_16x;
            this.diffToolStripMenuItem.Name = "diffToolStripMenuItem";
            this.diffToolStripMenuItem.Size = new System.Drawing.Size(68, 30);
            this.diffToolStripMenuItem.Text = "Diff";
            // 
            // databaseSchemaDiffToolStripMenuItem
            // 
            this.databaseSchemaDiffToolStripMenuItem.Name = "databaseSchemaDiffToolStripMenuItem";
            this.databaseSchemaDiffToolStripMenuItem.Size = new System.Drawing.Size(240, 26);
            this.databaseSchemaDiffToolStripMenuItem.Text = "Database Schema Diff";
            this.databaseSchemaDiffToolStripMenuItem.Click += new System.EventHandler(this.DatabaseSchemaDiffToolStripMenuItem_Click);
            // 
            // agentJobDiffToolStripMenuItem
            // 
            this.agentJobDiffToolStripMenuItem.Name = "agentJobDiffToolStripMenuItem";
            this.agentJobDiffToolStripMenuItem.Size = new System.Drawing.Size(240, 26);
            this.agentJobDiffToolStripMenuItem.Text = "Agent Job Diff";
            this.agentJobDiffToolStripMenuItem.Click += new System.EventHandler(this.AgentJobDiffToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureDisplayNameToolStripMenuItem,
            this.dataRetentionToolStripMenuItem,
            this.freezeKeyColumnsToolStripMenuItem,
            this.showHiddenToolStripMenuItem,
            this.manageInstancesToolStripMenuItem,
            this.saveTimeZonePreferenceToolStripMenuItem});
            this.optionsToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(95, 30);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // configureDisplayNameToolStripMenuItem
            // 
            this.configureDisplayNameToolStripMenuItem.Name = "configureDisplayNameToolStripMenuItem";
            this.configureDisplayNameToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.configureDisplayNameToolStripMenuItem.Text = "Configure Display Name";
            this.configureDisplayNameToolStripMenuItem.Click += new System.EventHandler(this.ConfigureDisplayNameToolStripMenuItem_Click);
            // 
            // dataRetentionToolStripMenuItem
            // 
            this.dataRetentionToolStripMenuItem.Name = "dataRetentionToolStripMenuItem";
            this.dataRetentionToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.dataRetentionToolStripMenuItem.Text = "Data Retention";
            this.dataRetentionToolStripMenuItem.Click += new System.EventHandler(this.DataRetentionToolStripMenuItem_Click);
            // 
            // freezeKeyColumnsToolStripMenuItem
            // 
            this.freezeKeyColumnsToolStripMenuItem.Checked = true;
            this.freezeKeyColumnsToolStripMenuItem.CheckOnClick = true;
            this.freezeKeyColumnsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.freezeKeyColumnsToolStripMenuItem.Name = "freezeKeyColumnsToolStripMenuItem";
            this.freezeKeyColumnsToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.freezeKeyColumnsToolStripMenuItem.Text = "Freeze Key Columns";
            this.freezeKeyColumnsToolStripMenuItem.ToolTipText = "Keep the key column(s) in the grid visible as you scroll to the right";
            this.freezeKeyColumnsToolStripMenuItem.Click += new System.EventHandler(this.FreezeKeyColumnsToolStripMenuItem_Click);
            // 
            // showHiddenToolStripMenuItem
            // 
            this.showHiddenToolStripMenuItem.CheckOnClick = true;
            this.showHiddenToolStripMenuItem.Name = "showHiddenToolStripMenuItem";
            this.showHiddenToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.showHiddenToolStripMenuItem.Text = "Show Hidden";
            this.showHiddenToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ShowHidden_Changed);
            // 
            // manageInstancesToolStripMenuItem
            // 
            this.manageInstancesToolStripMenuItem.Name = "manageInstancesToolStripMenuItem";
            this.manageInstancesToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.manageInstancesToolStripMenuItem.Text = "Manage Instances";
            this.manageInstancesToolStripMenuItem.Click += new System.EventHandler(this.ManageInstancesToolStripMenuItem_Click);
            // 
            // saveTimeZonePreferenceToolStripMenuItem
            // 
            this.saveTimeZonePreferenceToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.Save_16x;
            this.saveTimeZonePreferenceToolStripMenuItem.Name = "saveTimeZonePreferenceToolStripMenuItem";
            this.saveTimeZonePreferenceToolStripMenuItem.Size = new System.Drawing.Size(268, 26);
            this.saveTimeZonePreferenceToolStripMenuItem.Text = "Save time zone preference";
            this.saveTimeZonePreferenceToolStripMenuItem.Click += new System.EventHandler(this.SaveTimeZonePreferenceToolStripMenuItem_Click);
            // 
            // tsTime
            // 
            this.tsTime.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minsToolStripMenuItem2,
            this.minsToolStripMenuItem1,
            this.minsToolStripMenuItem,
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.daysToolStripMenuItem,
            this.daysToolStripMenuItem1,
            this.days7ToolStripMenuItem,
            this.days14toolStripMenuItem,
            this.days28ToolStripMenuItem,
            this.toolStripSeparator1,
            this.dateToolStripMenuItem,
            this.tsCustom});
            this.tsTime.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tsTime.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(71, 27);
            this.tsTime.Text = "1Hr";
            // 
            // minsToolStripMenuItem2
            // 
            this.minsToolStripMenuItem2.CheckOnClick = true;
            this.minsToolStripMenuItem2.Name = "minsToolStripMenuItem2";
            this.minsToolStripMenuItem2.Size = new System.Drawing.Size(153, 28);
            this.minsToolStripMenuItem2.Tag = "5";
            this.minsToolStripMenuItem2.Text = "5 Mins";
            this.minsToolStripMenuItem2.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // minsToolStripMenuItem1
            // 
            this.minsToolStripMenuItem1.CheckOnClick = true;
            this.minsToolStripMenuItem1.Name = "minsToolStripMenuItem1";
            this.minsToolStripMenuItem1.Size = new System.Drawing.Size(153, 28);
            this.minsToolStripMenuItem1.Tag = "10";
            this.minsToolStripMenuItem1.Text = "10 Mins";
            this.minsToolStripMenuItem1.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // minsToolStripMenuItem
            // 
            this.minsToolStripMenuItem.CheckOnClick = true;
            this.minsToolStripMenuItem.Name = "minsToolStripMenuItem";
            this.minsToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.minsToolStripMenuItem.Tag = "15";
            this.minsToolStripMenuItem.Text = "15 Mins";
            this.minsToolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(153, 28);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(153, 28);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(153, 28);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(153, 28);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(153, 28);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(153, 28);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // daysToolStripMenuItem
            // 
            this.daysToolStripMenuItem.Name = "daysToolStripMenuItem";
            this.daysToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.daysToolStripMenuItem.Tag = "2880";
            this.daysToolStripMenuItem.Text = "2 Days";
            this.daysToolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // daysToolStripMenuItem1
            // 
            this.daysToolStripMenuItem1.Name = "daysToolStripMenuItem1";
            this.daysToolStripMenuItem1.Size = new System.Drawing.Size(153, 28);
            this.daysToolStripMenuItem1.Tag = "4320";
            this.daysToolStripMenuItem1.Text = "3 Days";
            this.daysToolStripMenuItem1.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // days14toolStripMenuItem
            // 
            this.days14toolStripMenuItem.Name = "days14toolStripMenuItem";
            this.days14toolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days14toolStripMenuItem.Tag = "20160";
            this.days14toolStripMenuItem.Text = "14 Days";
            this.days14toolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // days28ToolStripMenuItem
            // 
            this.days28ToolStripMenuItem.Name = "days28ToolStripMenuItem";
            this.days28ToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days28ToolStripMenuItem.Tag = "40320";
            this.days28ToolStripMenuItem.Text = "28 Days";
            this.days28ToolStripMenuItem.Click += new System.EventHandler(this.TsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(150, 6);
            // 
            // dateToolStripMenuItem
            // 
            this.dateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dummyToolStripMenuItem});
            this.dateToolStripMenuItem.Name = "dateToolStripMenuItem";
            this.dateToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.dateToolStripMenuItem.Tag = "Date";
            this.dateToolStripMenuItem.Text = "Date";
            this.dateToolStripMenuItem.DropDownOpening += new System.EventHandler(this.DateToolStripMenuItem_Opening);
            // 
            // dummyToolStripMenuItem
            // 
            this.dummyToolStripMenuItem.Name = "dummyToolStripMenuItem";
            this.dummyToolStripMenuItem.Size = new System.Drawing.Size(154, 28);
            this.dummyToolStripMenuItem.Text = "Dummy";
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(153, 28);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.TsCustomTime_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.Information_blue_6227_16x16;
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(75, 30);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // tsTimeFilter
            // 
            this.tsTimeFilter.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsTimeFilter.Image = global::DBADashGUI.Properties.Resources.Filter_16x;
            this.tsTimeFilter.Name = "tsTimeFilter";
            this.tsTimeFilter.Size = new System.Drawing.Size(124, 30);
            this.tsTimeFilter.Text = "Time of Day";
            this.tsTimeFilter.Visible = false;
            this.tsTimeFilter.Click += new System.EventHandler(this.TsTimeFilter_Click);
            // 
            // tsDayOfWeek
            // 
            this.tsDayOfWeek.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsDayOfWeek.Image = global::DBADashGUI.Properties.Resources.Filter_16x;
            this.tsDayOfWeek.Name = "tsDayOfWeek";
            this.tsDayOfWeek.Size = new System.Drawing.Size(127, 30);
            this.tsDayOfWeek.Text = "Day of Week";
            this.tsDayOfWeek.Visible = false;
            this.tsDayOfWeek.Click += new System.EventHandler(this.TsDayOfWeek_Click);
            // 
            // cboTimeZone
            // 
            this.cboTimeZone.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cboTimeZone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.cboTimeZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTimeZone.DropDownWidth = 350;
            this.cboTimeZone.ForeColor = System.Drawing.Color.White;
            this.cboTimeZone.Name = "cboTimeZone";
            this.cboTimeZone.Size = new System.Drawing.Size(250, 30);
            this.cboTimeZone.SelectedIndexChanged += new System.EventHandler(this.TimeZone_Selected);
            // 
            // splitMain
            // 
            this.splitMain.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 34);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.tv1);
            this.splitMain.Panel1.Controls.Add(this.pnlSearch);
            this.splitMain.Panel1.Controls.Add(this.toolStrip2);
            this.splitMain.Panel1MinSize = 50;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.tabs);
            this.splitMain.Panel2.Controls.Add(this.refresh1);
            this.splitMain.Panel2MinSize = 100;
            this.splitMain.Size = new System.Drawing.Size(1983, 1275);
            this.splitMain.SplitterDistance = 340;
            this.splitMain.TabIndex = 3;
            // 
            // tv1
            // 
            this.tv1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tv1.ImageIndex = 0;
            this.tv1.ImageList = this.TreeViewImageList;
            this.tv1.Location = new System.Drawing.Point(0, 27);
            this.tv1.Name = "tv1";
            this.tv1.SelectedImageIndex = 0;
            this.tv1.Size = new System.Drawing.Size(340, 1196);
            this.tv1.TabIndex = 0;
            this.tv1.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tv1_BeforeExpand);
            this.tv1.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tv1_BeforeSelect);
            this.tv1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tv1_AfterSelect);
            // 
            // pnlSearch
            // 
            this.pnlSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.pnlSearch.Controls.Add(this.bttnSearch);
            this.pnlSearch.Controls.Add(this.txtSearch);
            this.pnlSearch.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlSearch.Location = new System.Drawing.Point(0, 1223);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.Size = new System.Drawing.Size(340, 52);
            this.pnlSearch.TabIndex = 1;
            // 
            // bttnSearch
            // 
            this.bttnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSearch.Location = new System.Drawing.Point(232, 13);
            this.bttnSearch.Name = "bttnSearch";
            this.bttnSearch.Size = new System.Drawing.Size(94, 29);
            this.bttnSearch.TabIndex = 2;
            this.bttnSearch.Text = "Search";
            this.bttnSearch.UseVisualStyleBackColor = true;
            this.bttnSearch.Click += new System.EventHandler(this.BttnSearch_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(12, 16);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(214, 22);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyUp);
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsHome,
            this.tsBack,
            this.mnuTags,
            this.groupToolStripMenuItem});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(340, 27);
            this.toolStrip2.TabIndex = 2;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsHome
            // 
            this.tsHome.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsHome.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsHome.Image = global::DBADashGUI.Properties.Resources.HomeHS;
            this.tsHome.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsHome.Name = "tsHome";
            this.tsHome.Size = new System.Drawing.Size(29, 24);
            this.tsHome.Text = "Home";
            this.tsHome.ToolTipText = "Go to root level summary";
            this.tsHome.Click += new System.EventHandler(this.TsHome_Click);
            // 
            // tsBack
            // 
            this.tsBack.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBack.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBack.Name = "tsBack";
            this.tsBack.Size = new System.Drawing.Size(29, 24);
            this.tsBack.Text = "Back";
            this.tsBack.ToolTipText = "Move back to the previous context";
            this.tsBack.Click += new System.EventHandler(this.TsBack_Click);
            // 
            // mnuTags
            // 
            this.mnuTags.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.mnuTags.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.mnuTags.Name = "mnuTags";
            this.mnuTags.Size = new System.Drawing.Size(76, 24);
            this.mnuTags.Text = "Filter";
            this.mnuTags.ToolTipText = "Filter instances by tag";
            // 
            // groupToolStripMenuItem
            // 
            this.groupToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.GroupBy_16x;
            this.groupToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.groupToolStripMenuItem.Name = "groupToolStripMenuItem";
            this.groupToolStripMenuItem.Size = new System.Drawing.Size(84, 24);
            this.groupToolStripMenuItem.Text = "Group";
            this.groupToolStripMenuItem.ToolTipText = "Group instances by tag";
            // 
            // tabs
            // 
            this.tabs.Controls.Add(this.tabSnapshotsSummary);
            this.tabs.Controls.Add(this.tabSchema);
            this.tabs.Controls.Add(this.tabTags);
            this.tabs.Controls.Add(this.tabDrives);
            this.tabs.Controls.Add(this.tabBackups);
            this.tabs.Controls.Add(this.tabLogShipping);
            this.tabs.Controls.Add(this.tabJobs);
            this.tabs.Controls.Add(this.tabSummary);
            this.tabs.Controls.Add(this.tabFiles);
            this.tabs.Controls.Add(this.tabLastGood);
            this.tabs.Controls.Add(this.tabPerformance);
            this.tabs.Controls.Add(this.tabDBADashErrorLog);
            this.tabs.Controls.Add(this.tabCollectionDates);
            this.tabs.Controls.Add(this.tabPerformanceSummary);
            this.tabs.Controls.Add(this.tabInfo);
            this.tabs.Controls.Add(this.tabHardware);
            this.tabs.Controls.Add(this.tabSQLPatching);
            this.tabs.Controls.Add(this.tabInstanceConfig);
            this.tabs.Controls.Add(this.tabSlowQueries);
            this.tabs.Controls.Add(this.tabTraceFlags);
            this.tabs.Controls.Add(this.tabAlerts);
            this.tabs.Controls.Add(this.tabDrivers);
            this.tabs.Controls.Add(this.tabDBSpace);
            this.tabs.Controls.Add(this.tabAzureSummary);
            this.tabs.Controls.Add(this.tabAzureDB);
            this.tabs.Controls.Add(this.tabServiceObjectives);
            this.tabs.Controls.Add(this.tabDBConfiguration);
            this.tabs.Controls.Add(this.tabDBOptions);
            this.tabs.Controls.Add(this.tabTempDB);
            this.tabs.Controls.Add(this.tabCustomChecks);
            this.tabs.Controls.Add(this.tabPC);
            this.tabs.Controls.Add(this.tabObjectExecutionSummary);
            this.tabs.Controls.Add(this.tabWaits);
            this.tabs.Controls.Add(this.tabMirroring);
            this.tabs.Controls.Add(this.tabJobDDL);
            this.tabs.Controls.Add(this.tabAG);
            this.tabs.Controls.Add(this.tabQS);
            this.tabs.Controls.Add(this.tabRG);
            this.tabs.Controls.Add(this.tabAzureDBesourceGovernance);
            this.tabs.Controls.Add(this.tabRunningQueries);
            this.tabs.Controls.Add(this.tabMemory);
            this.tabs.Controls.Add(this.tabJobStats);
            this.tabs.Controls.Add(this.tabDBADash);
            this.tabs.Controls.Add(this.tabIdentityColumns);
            this.tabs.Controls.Add(this.tabOSLoadedModules);
            this.tabs.Controls.Add(this.tabJobTimeline);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 0);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1639, 1275);
            this.tabs.TabIndex = 0;
            this.tabs.Tag = "";
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.Tabs_SelectedIndexChanged);
            // 
            // tabSnapshotsSummary
            // 
            this.tabSnapshotsSummary.Controls.Add(this.schemaSnapshots1);
            this.tabSnapshotsSummary.Location = new System.Drawing.Point(4, 25);
            this.tabSnapshotsSummary.Name = "tabSnapshotsSummary";
            this.tabSnapshotsSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSnapshotsSummary.Size = new System.Drawing.Size(1631, 1246);
            this.tabSnapshotsSummary.TabIndex = 1;
            this.tabSnapshotsSummary.Text = "Snapshot Summary";
            this.tabSnapshotsSummary.UseVisualStyleBackColor = true;
            // 
            // schemaSnapshots1
            // 
            this.schemaSnapshots1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.schemaSnapshots1.Location = new System.Drawing.Point(3, 3);
            this.schemaSnapshots1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.schemaSnapshots1.Name = "schemaSnapshots1";
            this.schemaSnapshots1.Size = new System.Drawing.Size(1625, 1240);
            this.schemaSnapshots1.TabIndex = 0;
            // 
            // tabSchema
            // 
            this.tabSchema.Controls.Add(this.splitSchemaSnapshot);
            this.tabSchema.Location = new System.Drawing.Point(4, 29);
            this.tabSchema.Name = "tabSchema";
            this.tabSchema.Padding = new System.Windows.Forms.Padding(3);
            this.tabSchema.Size = new System.Drawing.Size(192, 67);
            this.tabSchema.TabIndex = 0;
            this.tabSchema.Text = "Schema Snapshot";
            this.tabSchema.UseVisualStyleBackColor = true;
            // 
            // splitSchemaSnapshot
            // 
            this.splitSchemaSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitSchemaSnapshot.Location = new System.Drawing.Point(3, 3);
            this.splitSchemaSnapshot.Name = "splitSchemaSnapshot";
            this.splitSchemaSnapshot.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitSchemaSnapshot.Panel1
            // 
            this.splitSchemaSnapshot.Panel1.Controls.Add(this.label7);
            // 
            // splitSchemaSnapshot.Panel2
            // 
            this.splitSchemaSnapshot.Panel2.Controls.Add(this.toolStrip1);
            this.splitSchemaSnapshot.Panel2.Controls.Add(this.gvHistory);
            this.splitSchemaSnapshot.Panel2.Controls.Add(this.label1);
            this.splitSchemaSnapshot.Size = new System.Drawing.Size(186, 61);
            this.splitSchemaSnapshot.SplitterDistance = 25;
            this.splitSchemaSnapshot.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(215, 236);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(311, 16);
            this.label7.TabIndex = 1;
            this.label7.Text = "Diff (Loaded programatically due to designer issue)";
            this.label7.Visible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsPrevious,
            this.tsPageNum,
            this.tsNext,
            this.toolStripLabel1,
            this.tsPageSize});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(186, 32);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsPrevious
            // 
            this.tsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPrevious.Image = ((System.Drawing.Image)(resources.GetObject("tsPrevious.Image")));
            this.tsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPrevious.Name = "tsPrevious";
            this.tsPrevious.Size = new System.Drawing.Size(29, 29);
            this.tsPrevious.Text = "Previous";
            this.tsPrevious.Click += new System.EventHandler(this.TsPrevious_Click);
            // 
            // tsPageNum
            // 
            this.tsPageNum.Name = "tsPageNum";
            this.tsPageNum.Size = new System.Drawing.Size(53, 29);
            this.tsPageNum.Text = "Page 1";
            // 
            // tsNext
            // 
            this.tsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNext.Image = ((System.Drawing.Image)(resources.GetObject("tsNext.Image")));
            this.tsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNext.Name = "tsNext";
            this.tsNext.Size = new System.Drawing.Size(29, 29);
            this.tsNext.Text = "Next";
            this.tsNext.Click += new System.EventHandler(this.TsNext_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(75, 20);
            this.toolStripLabel1.Text = "Page Size:";
            // 
            // tsPageSize
            // 
            this.tsPageSize.Items.AddRange(new object[] {
            "100",
            "200",
            "500",
            "1000",
            "5000"});
            this.tsPageSize.Name = "tsPageSize";
            this.tsPageSize.Size = new System.Drawing.Size(121, 28);
            this.tsPageSize.Text = "100";
            this.tsPageSize.Validating += new System.ComponentModel.CancelEventHandler(this.TsPageSize_Validating);
            this.tsPageSize.Validated += new System.EventHandler(this.TsPageSize_Validated);
            // 
            // gvHistory
            // 
            this.gvHistory.AllowUserToAddRows = false;
            this.gvHistory.AllowUserToDeleteRows = false;
            this.gvHistory.AllowUserToOrderColumns = true;
            this.gvHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gvHistory.ColumnHeadersHeight = 29;
            this.gvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ObjectName,
            this.SchemaName,
            this.ObjectType,
            this.SnapshotValidFrom,
            this.SnapshotValidTo,
            this.ObjectDateCreated,
            this.ObjectDateModified,
            this.colCompare});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gvHistory.DefaultCellStyle = dataGridViewCellStyle2;
            this.gvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvHistory.Location = new System.Drawing.Point(0, 25);
            this.gvHistory.MultiSelect = false;
            this.gvHistory.Name = "gvHistory";
            this.gvHistory.RowHeadersVisible = false;
            this.gvHistory.RowHeadersWidth = 51;
            this.gvHistory.RowTemplate.Height = 24;
            this.gvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gvHistory.Size = new System.Drawing.Size(186, 7);
            this.gvHistory.TabIndex = 0;
            this.gvHistory.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GvHistory_CellContentClick);
            this.gvHistory.SelectionChanged += new System.EventHandler(this.GvHistory_SelectionChanged);
            // 
            // ObjectName
            // 
            this.ObjectName.DataPropertyName = "ObjectName";
            this.ObjectName.HeaderText = "Object Name";
            this.ObjectName.MinimumWidth = 6;
            this.ObjectName.Name = "ObjectName";
            this.ObjectName.Width = 119;
            // 
            // SchemaName
            // 
            this.SchemaName.DataPropertyName = "SchemaName";
            this.SchemaName.HeaderText = "Schema Name";
            this.SchemaName.MinimumWidth = 6;
            this.SchemaName.Name = "SchemaName";
            this.SchemaName.Width = 129;
            // 
            // ObjectType
            // 
            this.ObjectType.HeaderText = "Object Type";
            this.ObjectType.MinimumWidth = 6;
            this.ObjectType.Name = "ObjectType";
            this.ObjectType.Width = 114;
            // 
            // SnapshotValidFrom
            // 
            this.SnapshotValidFrom.DataPropertyName = "SnapshotValidFrom";
            this.SnapshotValidFrom.HeaderText = "Snapshot Valid From";
            this.SnapshotValidFrom.MinimumWidth = 6;
            this.SnapshotValidFrom.Name = "SnapshotValidFrom";
            this.SnapshotValidFrom.Width = 168;
            // 
            // SnapshotValidTo
            // 
            this.SnapshotValidTo.DataPropertyName = "SnapshotValidTo";
            this.SnapshotValidTo.HeaderText = "Snapshot Valid To";
            this.SnapshotValidTo.MinimumWidth = 6;
            this.SnapshotValidTo.Name = "SnapshotValidTo";
            this.SnapshotValidTo.Width = 153;
            // 
            // ObjectDateCreated
            // 
            this.ObjectDateCreated.DataPropertyName = "ObjectDateCreated";
            this.ObjectDateCreated.HeaderText = "Date Created";
            this.ObjectDateCreated.MinimumWidth = 6;
            this.ObjectDateCreated.Name = "ObjectDateCreated";
            this.ObjectDateCreated.Width = 121;
            // 
            // ObjectDateModified
            // 
            this.ObjectDateModified.DataPropertyName = "ObjectDateModified";
            this.ObjectDateModified.HeaderText = "Date Modified";
            this.ObjectDateModified.MinimumWidth = 6;
            this.ObjectDateModified.Name = "ObjectDateModified";
            this.ObjectDateModified.Width = 124;
            // 
            // colCompare
            // 
            this.colCompare.HeaderText = "Compare";
            this.colCompare.MinimumWidth = 6;
            this.colCompare.Name = "colCompare";
            this.colCompare.Text = "Compare";
            this.colCompare.UseColumnTextForLinkValue = true;
            this.colCompare.Width = 125;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(186, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Snapshot History";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // tabTags
            // 
            this.tabTags.Controls.Add(this.tags1);
            this.tabTags.Location = new System.Drawing.Point(4, 29);
            this.tabTags.Name = "tabTags";
            this.tabTags.Padding = new System.Windows.Forms.Padding(3);
            this.tabTags.Size = new System.Drawing.Size(192, 67);
            this.tabTags.TabIndex = 2;
            this.tabTags.Text = "Tags";
            this.tabTags.UseVisualStyleBackColor = true;
            // 
            // tags1
            // 
            this.tags1.AllTags = null;
            this.tags1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tags1.Location = new System.Drawing.Point(3, 3);
            this.tags1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tags1.Name = "tags1";
            this.tags1.Size = new System.Drawing.Size(186, 61);
            this.tags1.TabIndex = 0;
            this.tags1.TagsChanged += new System.EventHandler(this.Tags1_TagsChanged);
            // 
            // tabDrives
            // 
            this.tabDrives.AutoScroll = true;
            this.tabDrives.Controls.Add(this.drivesControl1);
            this.tabDrives.Location = new System.Drawing.Point(4, 29);
            this.tabDrives.Name = "tabDrives";
            this.tabDrives.Padding = new System.Windows.Forms.Padding(3);
            this.tabDrives.Size = new System.Drawing.Size(192, 67);
            this.tabDrives.TabIndex = 3;
            this.tabDrives.Text = "Drives";
            this.tabDrives.UseVisualStyleBackColor = true;
            // 
            // drivesControl1
            // 
            this.drivesControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drivesControl1.IncludeCritical = false;
            this.drivesControl1.IncludeNA = false;
            this.drivesControl1.IncludeOK = false;
            this.drivesControl1.IncludeWarning = false;
            this.drivesControl1.Location = new System.Drawing.Point(3, 3);
            this.drivesControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.drivesControl1.Name = "drivesControl1";
            this.drivesControl1.Size = new System.Drawing.Size(186, 61);
            this.drivesControl1.TabIndex = 0;
            // 
            // tabBackups
            // 
            this.tabBackups.Controls.Add(this.backupsControl1);
            this.tabBackups.Location = new System.Drawing.Point(4, 29);
            this.tabBackups.Name = "tabBackups";
            this.tabBackups.Padding = new System.Windows.Forms.Padding(3);
            this.tabBackups.Size = new System.Drawing.Size(192, 67);
            this.tabBackups.TabIndex = 4;
            this.tabBackups.Tag = "1";
            this.tabBackups.Text = "Backups";
            this.tabBackups.UseVisualStyleBackColor = true;
            // 
            // backupsControl1
            // 
            this.backupsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.backupsControl1.IncludeCritical = false;
            this.backupsControl1.IncludeNA = false;
            this.backupsControl1.IncludeOK = false;
            this.backupsControl1.IncludeWarning = false;
            this.backupsControl1.Location = new System.Drawing.Point(3, 3);
            this.backupsControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.backupsControl1.Name = "backupsControl1";
            this.backupsControl1.Size = new System.Drawing.Size(186, 61);
            this.backupsControl1.TabIndex = 0;
            // 
            // tabLogShipping
            // 
            this.tabLogShipping.Controls.Add(this.logShippingControl1);
            this.tabLogShipping.Location = new System.Drawing.Point(4, 29);
            this.tabLogShipping.Name = "tabLogShipping";
            this.tabLogShipping.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogShipping.Size = new System.Drawing.Size(192, 67);
            this.tabLogShipping.TabIndex = 5;
            this.tabLogShipping.Text = "Log Shipping";
            this.tabLogShipping.UseVisualStyleBackColor = true;
            // 
            // logShippingControl1
            // 
            this.logShippingControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logShippingControl1.IncludeCritical = false;
            this.logShippingControl1.IncludeNA = false;
            this.logShippingControl1.IncludeOK = false;
            this.logShippingControl1.IncludeWarning = false;
            this.logShippingControl1.Location = new System.Drawing.Point(3, 3);
            this.logShippingControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.logShippingControl1.Name = "logShippingControl1";
            this.logShippingControl1.Size = new System.Drawing.Size(186, 61);
            this.logShippingControl1.TabIndex = 0;
            // 
            // tabJobs
            // 
            this.tabJobs.Controls.Add(this.agentJobsControl1);
            this.tabJobs.Location = new System.Drawing.Point(4, 29);
            this.tabJobs.Name = "tabJobs";
            this.tabJobs.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobs.Size = new System.Drawing.Size(192, 67);
            this.tabJobs.TabIndex = 6;
            this.tabJobs.Text = "Agent Jobs";
            this.tabJobs.UseVisualStyleBackColor = true;
            // 
            // agentJobsControl1
            // 
            this.agentJobsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.agentJobsControl1.IncludeCritical = false;
            this.agentJobsControl1.IncludeNA = false;
            this.agentJobsControl1.IncludeOK = false;
            this.agentJobsControl1.IncludeWarning = false;
            this.agentJobsControl1.Location = new System.Drawing.Point(3, 3);
            this.agentJobsControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.agentJobsControl1.Name = "agentJobsControl1";
            this.agentJobsControl1.Size = new System.Drawing.Size(186, 61);
            this.agentJobsControl1.TabIndex = 0;
            // 
            // tabSummary
            // 
            this.tabSummary.Controls.Add(this.summary1);
            this.tabSummary.Location = new System.Drawing.Point(4, 29);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSummary.Size = new System.Drawing.Size(192, 67);
            this.tabSummary.TabIndex = 7;
            this.tabSummary.Text = "Summary";
            this.tabSummary.UseVisualStyleBackColor = true;
            // 
            // summary1
            // 
            this.summary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.summary1.Location = new System.Drawing.Point(3, 3);
            this.summary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.summary1.Name = "summary1";
            this.summary1.Size = new System.Drawing.Size(186, 61);
            this.summary1.TabIndex = 0;
            this.summary1.Instance_Selected += new System.EventHandler<DBADashGUI.Main.InstanceSelectedEventArgs>(this.Instance_Selected);
            // 
            // tabFiles
            // 
            this.tabFiles.Controls.Add(this.dbFilesControl1);
            this.tabFiles.Location = new System.Drawing.Point(4, 29);
            this.tabFiles.Name = "tabFiles";
            this.tabFiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabFiles.Size = new System.Drawing.Size(192, 67);
            this.tabFiles.TabIndex = 8;
            this.tabFiles.Text = "Files";
            this.tabFiles.UseVisualStyleBackColor = true;
            // 
            // dbFilesControl1
            // 
            this.dbFilesControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbFilesControl1.IncludeCritical = false;
            this.dbFilesControl1.IncludeNA = false;
            this.dbFilesControl1.IncludeOK = false;
            this.dbFilesControl1.IncludeWarning = false;
            this.dbFilesControl1.Location = new System.Drawing.Point(3, 3);
            this.dbFilesControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dbFilesControl1.Name = "dbFilesControl1";
            this.dbFilesControl1.Size = new System.Drawing.Size(186, 61);
            this.dbFilesControl1.TabIndex = 0;
            // 
            // tabLastGood
            // 
            this.tabLastGood.Controls.Add(this.lastGoodCheckDBControl1);
            this.tabLastGood.Location = new System.Drawing.Point(4, 29);
            this.tabLastGood.Name = "tabLastGood";
            this.tabLastGood.Padding = new System.Windows.Forms.Padding(3);
            this.tabLastGood.Size = new System.Drawing.Size(192, 67);
            this.tabLastGood.TabIndex = 9;
            this.tabLastGood.Text = "Last Good CheckDB";
            this.tabLastGood.UseVisualStyleBackColor = true;
            // 
            // lastGoodCheckDBControl1
            // 
            this.lastGoodCheckDBControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lastGoodCheckDBControl1.IncludeCritical = false;
            this.lastGoodCheckDBControl1.IncludeNA = false;
            this.lastGoodCheckDBControl1.IncludeOK = false;
            this.lastGoodCheckDBControl1.IncludeWarning = false;
            this.lastGoodCheckDBControl1.Location = new System.Drawing.Point(3, 3);
            this.lastGoodCheckDBControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.lastGoodCheckDBControl1.Name = "lastGoodCheckDBControl1";
            this.lastGoodCheckDBControl1.Size = new System.Drawing.Size(186, 61);
            this.lastGoodCheckDBControl1.TabIndex = 0;
            // 
            // tabPerformance
            // 
            this.tabPerformance.Controls.Add(this.performance1);
            this.tabPerformance.Location = new System.Drawing.Point(4, 29);
            this.tabPerformance.Name = "tabPerformance";
            this.tabPerformance.Padding = new System.Windows.Forms.Padding(3);
            this.tabPerformance.Size = new System.Drawing.Size(192, 67);
            this.tabPerformance.TabIndex = 10;
            this.tabPerformance.Text = "Performance";
            this.tabPerformance.UseVisualStyleBackColor = true;
            // 
            // performance1
            // 
            this.performance1.AutoScroll = true;
            this.performance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performance1.Location = new System.Drawing.Point(3, 3);
            this.performance1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.performance1.Name = "performance1";
            this.performance1.Size = new System.Drawing.Size(186, 61);
            this.performance1.TabIndex = 0;
            // 
            // tabDBADashErrorLog
            // 
            this.tabDBADashErrorLog.Controls.Add(this.collectionErrors1);
            this.tabDBADashErrorLog.Location = new System.Drawing.Point(4, 29);
            this.tabDBADashErrorLog.Name = "tabDBADashErrorLog";
            this.tabDBADashErrorLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBADashErrorLog.Size = new System.Drawing.Size(192, 67);
            this.tabDBADashErrorLog.TabIndex = 11;
            this.tabDBADashErrorLog.Text = "DBA Dash ErrorLog";
            this.tabDBADashErrorLog.UseVisualStyleBackColor = true;
            // 
            // collectionErrors1
            // 
            this.collectionErrors1.AckErrors = false;
            this.collectionErrors1.Days = 0;
            this.collectionErrors1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.collectionErrors1.Location = new System.Drawing.Point(3, 3);
            this.collectionErrors1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.collectionErrors1.Name = "collectionErrors1";
            this.collectionErrors1.Size = new System.Drawing.Size(186, 61);
            this.collectionErrors1.TabIndex = 0;
            // 
            // tabCollectionDates
            // 
            this.tabCollectionDates.Controls.Add(this.collectionDates1);
            this.tabCollectionDates.Location = new System.Drawing.Point(4, 29);
            this.tabCollectionDates.Name = "tabCollectionDates";
            this.tabCollectionDates.Padding = new System.Windows.Forms.Padding(3);
            this.tabCollectionDates.Size = new System.Drawing.Size(192, 67);
            this.tabCollectionDates.TabIndex = 12;
            this.tabCollectionDates.Text = "Collection Dates";
            this.tabCollectionDates.UseVisualStyleBackColor = true;
            // 
            // collectionDates1
            // 
            this.collectionDates1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.collectionDates1.IncludeCritical = false;
            this.collectionDates1.IncludeNA = false;
            this.collectionDates1.IncludeOK = false;
            this.collectionDates1.IncludeWarning = false;
            this.collectionDates1.Location = new System.Drawing.Point(3, 3);
            this.collectionDates1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.collectionDates1.Name = "collectionDates1";
            this.collectionDates1.Size = new System.Drawing.Size(186, 61);
            this.collectionDates1.TabIndex = 0;
            // 
            // tabPerformanceSummary
            // 
            this.tabPerformanceSummary.Controls.Add(this.performanceSummary1);
            this.tabPerformanceSummary.Location = new System.Drawing.Point(4, 29);
            this.tabPerformanceSummary.Name = "tabPerformanceSummary";
            this.tabPerformanceSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPerformanceSummary.Size = new System.Drawing.Size(192, 67);
            this.tabPerformanceSummary.TabIndex = 13;
            this.tabPerformanceSummary.Text = "Performance Summary";
            this.tabPerformanceSummary.UseVisualStyleBackColor = true;
            // 
            // performanceSummary1
            // 
            this.performanceSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceSummary1.Location = new System.Drawing.Point(3, 3);
            this.performanceSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.performanceSummary1.Name = "performanceSummary1";
            this.performanceSummary1.Size = new System.Drawing.Size(186, 61);
            this.performanceSummary1.TabIndex = 0;
            this.performanceSummary1.Instance_Selected += new System.EventHandler<DBADashGUI.Main.InstanceSelectedEventArgs>(this.Instance_Selected);
            // 
            // tabInfo
            // 
            this.tabInfo.Controls.Add(this.info1);
            this.tabInfo.Location = new System.Drawing.Point(4, 29);
            this.tabInfo.Name = "tabInfo";
            this.tabInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tabInfo.Size = new System.Drawing.Size(192, 67);
            this.tabInfo.TabIndex = 14;
            this.tabInfo.Text = "Info";
            this.tabInfo.UseVisualStyleBackColor = true;
            // 
            // info1
            // 
            this.info1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.info1.Location = new System.Drawing.Point(3, 3);
            this.info1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.info1.Name = "info1";
            this.info1.Size = new System.Drawing.Size(186, 61);
            this.info1.TabIndex = 0;
            // 
            // tabHardware
            // 
            this.tabHardware.Controls.Add(this.hardwareChanges1);
            this.tabHardware.Location = new System.Drawing.Point(4, 29);
            this.tabHardware.Name = "tabHardware";
            this.tabHardware.Padding = new System.Windows.Forms.Padding(3);
            this.tabHardware.Size = new System.Drawing.Size(192, 67);
            this.tabHardware.TabIndex = 15;
            this.tabHardware.Text = "Hardware";
            this.tabHardware.UseVisualStyleBackColor = true;
            // 
            // hardwareChanges1
            // 
            this.hardwareChanges1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hardwareChanges1.Location = new System.Drawing.Point(3, 3);
            this.hardwareChanges1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.hardwareChanges1.Name = "hardwareChanges1";
            this.hardwareChanges1.Size = new System.Drawing.Size(186, 61);
            this.hardwareChanges1.TabIndex = 0;
            // 
            // tabSQLPatching
            // 
            this.tabSQLPatching.Controls.Add(this.sqlPatching1);
            this.tabSQLPatching.Location = new System.Drawing.Point(4, 29);
            this.tabSQLPatching.Name = "tabSQLPatching";
            this.tabSQLPatching.Padding = new System.Windows.Forms.Padding(3);
            this.tabSQLPatching.Size = new System.Drawing.Size(192, 67);
            this.tabSQLPatching.TabIndex = 16;
            this.tabSQLPatching.Text = "SQL Patching";
            this.tabSQLPatching.UseVisualStyleBackColor = true;
            // 
            // sqlPatching1
            // 
            this.sqlPatching1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sqlPatching1.Location = new System.Drawing.Point(3, 3);
            this.sqlPatching1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.sqlPatching1.Name = "sqlPatching1";
            this.sqlPatching1.Size = new System.Drawing.Size(186, 61);
            this.sqlPatching1.TabIndex = 0;
            // 
            // tabInstanceConfig
            // 
            this.tabInstanceConfig.Controls.Add(this.configurationHistory1);
            this.tabInstanceConfig.Location = new System.Drawing.Point(4, 29);
            this.tabInstanceConfig.Name = "tabInstanceConfig";
            this.tabInstanceConfig.Padding = new System.Windows.Forms.Padding(3);
            this.tabInstanceConfig.Size = new System.Drawing.Size(192, 67);
            this.tabInstanceConfig.TabIndex = 17;
            this.tabInstanceConfig.Text = "Configuration";
            this.tabInstanceConfig.UseVisualStyleBackColor = true;
            // 
            // configurationHistory1
            // 
            this.configurationHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configurationHistory1.Location = new System.Drawing.Point(3, 3);
            this.configurationHistory1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.configurationHistory1.Name = "configurationHistory1";
            this.configurationHistory1.Size = new System.Drawing.Size(186, 61);
            this.configurationHistory1.TabIndex = 0;
            // 
            // tabSlowQueries
            // 
            this.tabSlowQueries.Controls.Add(this.slowQueries1);
            this.tabSlowQueries.Location = new System.Drawing.Point(4, 29);
            this.tabSlowQueries.Name = "tabSlowQueries";
            this.tabSlowQueries.Padding = new System.Windows.Forms.Padding(3);
            this.tabSlowQueries.Size = new System.Drawing.Size(192, 67);
            this.tabSlowQueries.TabIndex = 18;
            this.tabSlowQueries.Text = "Slow Queries";
            this.tabSlowQueries.UseVisualStyleBackColor = true;
            // 
            // slowQueries1
            // 
            this.slowQueries1.DBName = "";
            this.slowQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slowQueries1.Location = new System.Drawing.Point(3, 3);
            this.slowQueries1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.slowQueries1.Name = "slowQueries1";
            this.slowQueries1.Size = new System.Drawing.Size(186, 61);
            this.slowQueries1.TabIndex = 0;
            // 
            // tabTraceFlags
            // 
            this.tabTraceFlags.Controls.Add(this.traceFlagHistory1);
            this.tabTraceFlags.Location = new System.Drawing.Point(4, 29);
            this.tabTraceFlags.Name = "tabTraceFlags";
            this.tabTraceFlags.Padding = new System.Windows.Forms.Padding(3);
            this.tabTraceFlags.Size = new System.Drawing.Size(192, 67);
            this.tabTraceFlags.TabIndex = 19;
            this.tabTraceFlags.Text = "Trace Flags";
            this.tabTraceFlags.UseVisualStyleBackColor = true;
            // 
            // traceFlagHistory1
            // 
            this.traceFlagHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.traceFlagHistory1.Location = new System.Drawing.Point(3, 3);
            this.traceFlagHistory1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.traceFlagHistory1.Name = "traceFlagHistory1";
            this.traceFlagHistory1.Size = new System.Drawing.Size(186, 61);
            this.traceFlagHistory1.TabIndex = 0;
            // 
            // tabAlerts
            // 
            this.tabAlerts.Controls.Add(this.alerts1);
            this.tabAlerts.Location = new System.Drawing.Point(4, 29);
            this.tabAlerts.Name = "tabAlerts";
            this.tabAlerts.Padding = new System.Windows.Forms.Padding(3);
            this.tabAlerts.Size = new System.Drawing.Size(192, 67);
            this.tabAlerts.TabIndex = 20;
            this.tabAlerts.Text = "Alerts";
            this.tabAlerts.UseVisualStyleBackColor = true;
            // 
            // alerts1
            // 
            this.alerts1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alerts1.Location = new System.Drawing.Point(3, 3);
            this.alerts1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.alerts1.Name = "alerts1";
            this.alerts1.Size = new System.Drawing.Size(186, 61);
            this.alerts1.TabIndex = 0;
            this.alerts1.UseAlertName = false;
            // 
            // tabDrivers
            // 
            this.tabDrivers.Controls.Add(this.drivers1);
            this.tabDrivers.Location = new System.Drawing.Point(4, 29);
            this.tabDrivers.Name = "tabDrivers";
            this.tabDrivers.Padding = new System.Windows.Forms.Padding(3);
            this.tabDrivers.Size = new System.Drawing.Size(192, 67);
            this.tabDrivers.TabIndex = 21;
            this.tabDrivers.Text = "Drivers";
            this.tabDrivers.UseVisualStyleBackColor = true;
            // 
            // drivers1
            // 
            this.drivers1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drivers1.Location = new System.Drawing.Point(3, 3);
            this.drivers1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.drivers1.Name = "drivers1";
            this.drivers1.Size = new System.Drawing.Size(186, 61);
            this.drivers1.TabIndex = 0;
            // 
            // tabDBSpace
            // 
            this.tabDBSpace.Controls.Add(this.spaceTracking1);
            this.tabDBSpace.Location = new System.Drawing.Point(4, 29);
            this.tabDBSpace.Name = "tabDBSpace";
            this.tabDBSpace.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBSpace.Size = new System.Drawing.Size(192, 67);
            this.tabDBSpace.TabIndex = 22;
            this.tabDBSpace.Text = "DB Space";
            this.tabDBSpace.UseVisualStyleBackColor = true;
            // 
            // spaceTracking1
            // 
            this.spaceTracking1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spaceTracking1.Location = new System.Drawing.Point(3, 3);
            this.spaceTracking1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.spaceTracking1.Name = "spaceTracking1";
            this.spaceTracking1.Size = new System.Drawing.Size(186, 61);
            this.spaceTracking1.TabIndex = 0;
            // 
            // tabAzureSummary
            // 
            this.tabAzureSummary.Controls.Add(this.azureSummary1);
            this.tabAzureSummary.Location = new System.Drawing.Point(4, 29);
            this.tabAzureSummary.Name = "tabAzureSummary";
            this.tabAzureSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabAzureSummary.Size = new System.Drawing.Size(192, 67);
            this.tabAzureSummary.TabIndex = 23;
            this.tabAzureSummary.Text = "Azure Summary";
            this.tabAzureSummary.UseVisualStyleBackColor = true;
            // 
            // azureSummary1
            // 
            this.azureSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureSummary1.Location = new System.Drawing.Point(3, 3);
            this.azureSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.azureSummary1.Name = "azureSummary1";
            this.azureSummary1.Size = new System.Drawing.Size(186, 61);
            this.azureSummary1.TabIndex = 0;
            // 
            // tabAzureDB
            // 
            this.tabAzureDB.Controls.Add(this.azureDBResourceStats1);
            this.tabAzureDB.Location = new System.Drawing.Point(4, 29);
            this.tabAzureDB.Name = "tabAzureDB";
            this.tabAzureDB.Padding = new System.Windows.Forms.Padding(3);
            this.tabAzureDB.Size = new System.Drawing.Size(192, 67);
            this.tabAzureDB.TabIndex = 24;
            this.tabAzureDB.Text = "Azure DB";
            this.tabAzureDB.UseVisualStyleBackColor = true;
            // 
            // azureDBResourceStats1
            // 
            this.azureDBResourceStats1.DateGrouping = 0;
            this.azureDBResourceStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureDBResourceStats1.Location = new System.Drawing.Point(3, 3);
            this.azureDBResourceStats1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.azureDBResourceStats1.Name = "azureDBResourceStats1";
            this.azureDBResourceStats1.Size = new System.Drawing.Size(186, 61);
            this.azureDBResourceStats1.TabIndex = 0;
            // 
            // tabServiceObjectives
            // 
            this.tabServiceObjectives.Controls.Add(this.azureServiceObjectivesHistory1);
            this.tabServiceObjectives.Location = new System.Drawing.Point(4, 29);
            this.tabServiceObjectives.Name = "tabServiceObjectives";
            this.tabServiceObjectives.Padding = new System.Windows.Forms.Padding(3);
            this.tabServiceObjectives.Size = new System.Drawing.Size(192, 67);
            this.tabServiceObjectives.TabIndex = 25;
            this.tabServiceObjectives.Text = "Azure Service Objectives";
            this.tabServiceObjectives.UseVisualStyleBackColor = true;
            // 
            // azureServiceObjectivesHistory1
            // 
            this.azureServiceObjectivesHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureServiceObjectivesHistory1.Location = new System.Drawing.Point(3, 3);
            this.azureServiceObjectivesHistory1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.azureServiceObjectivesHistory1.Name = "azureServiceObjectivesHistory1";
            this.azureServiceObjectivesHistory1.Size = new System.Drawing.Size(186, 61);
            this.azureServiceObjectivesHistory1.TabIndex = 0;
            // 
            // tabDBConfiguration
            // 
            this.tabDBConfiguration.Controls.Add(this.dbConfiguration1);
            this.tabDBConfiguration.Location = new System.Drawing.Point(4, 29);
            this.tabDBConfiguration.Name = "tabDBConfiguration";
            this.tabDBConfiguration.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBConfiguration.Size = new System.Drawing.Size(192, 67);
            this.tabDBConfiguration.TabIndex = 26;
            this.tabDBConfiguration.Text = "DB Configuration";
            this.tabDBConfiguration.UseVisualStyleBackColor = true;
            // 
            // dbConfiguration1
            // 
            this.dbConfiguration1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbConfiguration1.Location = new System.Drawing.Point(3, 3);
            this.dbConfiguration1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dbConfiguration1.Name = "dbConfiguration1";
            this.dbConfiguration1.Size = new System.Drawing.Size(186, 61);
            this.dbConfiguration1.TabIndex = 0;
            // 
            // tabDBOptions
            // 
            this.tabDBOptions.Controls.Add(this.dbOptions1);
            this.tabDBOptions.Location = new System.Drawing.Point(4, 29);
            this.tabDBOptions.Name = "tabDBOptions";
            this.tabDBOptions.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBOptions.Size = new System.Drawing.Size(192, 67);
            this.tabDBOptions.TabIndex = 27;
            this.tabDBOptions.Text = "DB Options";
            this.tabDBOptions.UseVisualStyleBackColor = true;
            // 
            // dbOptions1
            // 
            this.dbOptions1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbOptions1.Location = new System.Drawing.Point(3, 3);
            this.dbOptions1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dbOptions1.Name = "dbOptions1";
            this.dbOptions1.Size = new System.Drawing.Size(186, 61);
            this.dbOptions1.SummaryMode = false;
            this.dbOptions1.TabIndex = 0;
            // 
            // tabTempDB
            // 
            this.tabTempDB.Controls.Add(this.tempDBConfig1);
            this.tabTempDB.Location = new System.Drawing.Point(4, 29);
            this.tabTempDB.Name = "tabTempDB";
            this.tabTempDB.Padding = new System.Windows.Forms.Padding(3);
            this.tabTempDB.Size = new System.Drawing.Size(192, 67);
            this.tabTempDB.TabIndex = 28;
            this.tabTempDB.Text = "TempDB";
            this.tabTempDB.UseVisualStyleBackColor = true;
            // 
            // tempDBConfig1
            // 
            this.tempDBConfig1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tempDBConfig1.Location = new System.Drawing.Point(3, 3);
            this.tempDBConfig1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tempDBConfig1.Name = "tempDBConfig1";
            this.tempDBConfig1.Size = new System.Drawing.Size(186, 61);
            this.tempDBConfig1.TabIndex = 0;
            // 
            // tabCustomChecks
            // 
            this.tabCustomChecks.Controls.Add(this.customChecks1);
            this.tabCustomChecks.Location = new System.Drawing.Point(4, 29);
            this.tabCustomChecks.Name = "tabCustomChecks";
            this.tabCustomChecks.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomChecks.Size = new System.Drawing.Size(192, 67);
            this.tabCustomChecks.TabIndex = 29;
            this.tabCustomChecks.Text = "Custom";
            this.tabCustomChecks.UseVisualStyleBackColor = true;
            // 
            // customChecks1
            // 
            this.customChecks1.Context = null;
            this.customChecks1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.customChecks1.IncludeCritical = true;
            this.customChecks1.IncludeNA = false;
            this.customChecks1.IncludeOK = false;
            this.customChecks1.IncludeWarning = true;
            this.customChecks1.Location = new System.Drawing.Point(3, 3);
            this.customChecks1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.customChecks1.Name = "customChecks1";
            this.customChecks1.Size = new System.Drawing.Size(186, 61);
            this.customChecks1.TabIndex = 0;
            this.customChecks1.Test = null;
            // 
            // tabPC
            // 
            this.tabPC.Controls.Add(this.performanceCounterSummary1);
            this.tabPC.Location = new System.Drawing.Point(4, 29);
            this.tabPC.Name = "tabPC";
            this.tabPC.Padding = new System.Windows.Forms.Padding(3);
            this.tabPC.Size = new System.Drawing.Size(192, 67);
            this.tabPC.TabIndex = 30;
            this.tabPC.Text = "Metrics";
            this.tabPC.UseVisualStyleBackColor = true;
            // 
            // performanceCounterSummary1
            // 
            this.performanceCounterSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceCounterSummary1.Location = new System.Drawing.Point(3, 3);
            this.performanceCounterSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.performanceCounterSummary1.Name = "performanceCounterSummary1";
            this.performanceCounterSummary1.Size = new System.Drawing.Size(186, 61);
            this.performanceCounterSummary1.TabIndex = 0;
            // 
            // tabObjectExecutionSummary
            // 
            this.tabObjectExecutionSummary.Controls.Add(this.objectExecutionSummary1);
            this.tabObjectExecutionSummary.Location = new System.Drawing.Point(4, 29);
            this.tabObjectExecutionSummary.Name = "tabObjectExecutionSummary";
            this.tabObjectExecutionSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabObjectExecutionSummary.Size = new System.Drawing.Size(192, 67);
            this.tabObjectExecutionSummary.TabIndex = 31;
            this.tabObjectExecutionSummary.Text = "Object Execution";
            this.tabObjectExecutionSummary.UseVisualStyleBackColor = true;
            // 
            // objectExecutionSummary1
            // 
            this.objectExecutionSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecutionSummary1.Location = new System.Drawing.Point(3, 3);
            this.objectExecutionSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.objectExecutionSummary1.Name = "objectExecutionSummary1";
            this.objectExecutionSummary1.Size = new System.Drawing.Size(186, 61);
            this.objectExecutionSummary1.TabIndex = 0;
            this.objectExecutionSummary1.Types = "";
            // 
            // tabWaits
            // 
            this.tabWaits.Controls.Add(this.waitsSummary1);
            this.tabWaits.Location = new System.Drawing.Point(4, 29);
            this.tabWaits.Name = "tabWaits";
            this.tabWaits.Padding = new System.Windows.Forms.Padding(3);
            this.tabWaits.Size = new System.Drawing.Size(192, 67);
            this.tabWaits.TabIndex = 32;
            this.tabWaits.Text = "Waits";
            this.tabWaits.UseVisualStyleBackColor = true;
            // 
            // waitsSummary1
            // 
            this.waitsSummary1.DateGrouping = 1;
            this.waitsSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitsSummary1.Location = new System.Drawing.Point(3, 3);
            this.waitsSummary1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.waitsSummary1.Name = "waitsSummary1";
            this.waitsSummary1.Size = new System.Drawing.Size(186, 61);
            this.waitsSummary1.TabIndex = 0;
            // 
            // tabMirroring
            // 
            this.tabMirroring.Controls.Add(this.mirroring1);
            this.tabMirroring.Location = new System.Drawing.Point(4, 29);
            this.tabMirroring.Name = "tabMirroring";
            this.tabMirroring.Padding = new System.Windows.Forms.Padding(3);
            this.tabMirroring.Size = new System.Drawing.Size(192, 67);
            this.tabMirroring.TabIndex = 33;
            this.tabMirroring.Text = "Mirroring";
            this.tabMirroring.UseVisualStyleBackColor = true;
            // 
            // mirroring1
            // 
            this.mirroring1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mirroring1.Location = new System.Drawing.Point(3, 3);
            this.mirroring1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.mirroring1.Name = "mirroring1";
            this.mirroring1.Size = new System.Drawing.Size(186, 61);
            this.mirroring1.SummaryMode = true;
            this.mirroring1.TabIndex = 0;
            // 
            // tabJobDDL
            // 
            this.tabJobDDL.Controls.Add(this.jobDDLHistory1);
            this.tabJobDDL.Location = new System.Drawing.Point(4, 29);
            this.tabJobDDL.Name = "tabJobDDL";
            this.tabJobDDL.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobDDL.Size = new System.Drawing.Size(192, 67);
            this.tabJobDDL.TabIndex = 34;
            this.tabJobDDL.Text = "Job DDL";
            this.tabJobDDL.UseVisualStyleBackColor = true;
            // 
            // jobDDLHistory1
            // 
            this.jobDDLHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobDDLHistory1.Location = new System.Drawing.Point(3, 3);
            this.jobDDLHistory1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.jobDDLHistory1.Name = "jobDDLHistory1";
            this.jobDDLHistory1.Size = new System.Drawing.Size(186, 61);
            this.jobDDLHistory1.TabIndex = 0;
            // 
            // tabAG
            // 
            this.tabAG.Controls.Add(this.ag1);
            this.tabAG.Location = new System.Drawing.Point(4, 29);
            this.tabAG.Name = "tabAG";
            this.tabAG.Padding = new System.Windows.Forms.Padding(3);
            this.tabAG.Size = new System.Drawing.Size(192, 67);
            this.tabAG.TabIndex = 35;
            this.tabAG.Text = "Availability Groups";
            this.tabAG.UseVisualStyleBackColor = true;
            // 
            // ag1
            // 
            this.ag1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ag1.Location = new System.Drawing.Point(3, 3);
            this.ag1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ag1.Name = "ag1";
            this.ag1.Size = new System.Drawing.Size(186, 61);
            this.ag1.TabIndex = 0;
            // 
            // tabQS
            // 
            this.tabQS.Controls.Add(this.queryStore1);
            this.tabQS.Location = new System.Drawing.Point(4, 29);
            this.tabQS.Name = "tabQS";
            this.tabQS.Padding = new System.Windows.Forms.Padding(3);
            this.tabQS.Size = new System.Drawing.Size(192, 67);
            this.tabQS.TabIndex = 36;
            this.tabQS.Text = "QS";
            this.tabQS.UseVisualStyleBackColor = true;
            // 
            // queryStore1
            // 
            this.queryStore1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.queryStore1.Location = new System.Drawing.Point(3, 3);
            this.queryStore1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.queryStore1.Name = "queryStore1";
            this.queryStore1.Size = new System.Drawing.Size(186, 61);
            this.queryStore1.TabIndex = 0;
            // 
            // tabRG
            // 
            this.tabRG.Controls.Add(this.resourceGovernor1);
            this.tabRG.Location = new System.Drawing.Point(4, 29);
            this.tabRG.Name = "tabRG";
            this.tabRG.Padding = new System.Windows.Forms.Padding(3);
            this.tabRG.Size = new System.Drawing.Size(192, 67);
            this.tabRG.TabIndex = 37;
            this.tabRG.Text = "Resource Governor";
            this.tabRG.UseVisualStyleBackColor = true;
            // 
            // resourceGovernor1
            // 
            this.resourceGovernor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resourceGovernor1.Location = new System.Drawing.Point(3, 3);
            this.resourceGovernor1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.resourceGovernor1.Name = "resourceGovernor1";
            this.resourceGovernor1.Size = new System.Drawing.Size(186, 61);
            this.resourceGovernor1.TabIndex = 0;
            // 
            // tabAzureDBesourceGovernance
            // 
            this.tabAzureDBesourceGovernance.Controls.Add(this.azureDBResourceGovernance1);
            this.tabAzureDBesourceGovernance.Location = new System.Drawing.Point(4, 29);
            this.tabAzureDBesourceGovernance.Name = "tabAzureDBesourceGovernance";
            this.tabAzureDBesourceGovernance.Padding = new System.Windows.Forms.Padding(3);
            this.tabAzureDBesourceGovernance.Size = new System.Drawing.Size(192, 67);
            this.tabAzureDBesourceGovernance.TabIndex = 38;
            this.tabAzureDBesourceGovernance.Text = "Azure Resource Governance";
            this.tabAzureDBesourceGovernance.UseVisualStyleBackColor = true;
            // 
            // azureDBResourceGovernance1
            // 
            this.azureDBResourceGovernance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureDBResourceGovernance1.Location = new System.Drawing.Point(3, 3);
            this.azureDBResourceGovernance1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.azureDBResourceGovernance1.Name = "azureDBResourceGovernance1";
            this.azureDBResourceGovernance1.Size = new System.Drawing.Size(186, 61);
            this.azureDBResourceGovernance1.TabIndex = 0;
            // 
            // tabRunningQueries
            // 
            this.tabRunningQueries.Controls.Add(this.runningQueries1);
            this.tabRunningQueries.Location = new System.Drawing.Point(4, 29);
            this.tabRunningQueries.Name = "tabRunningQueries";
            this.tabRunningQueries.Padding = new System.Windows.Forms.Padding(3);
            this.tabRunningQueries.Size = new System.Drawing.Size(192, 67);
            this.tabRunningQueries.TabIndex = 39;
            this.tabRunningQueries.Text = "Running Queries";
            this.tabRunningQueries.UseVisualStyleBackColor = true;
            // 
            // runningQueries1
            // 
            this.runningQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.runningQueries1.Location = new System.Drawing.Point(3, 3);
            this.runningQueries1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.runningQueries1.Name = "runningQueries1";
            this.runningQueries1.Size = new System.Drawing.Size(186, 61);
            this.runningQueries1.TabIndex = 0;
            // 
            // tabMemory
            // 
            this.tabMemory.Controls.Add(this.memoryUsage1);
            this.tabMemory.Location = new System.Drawing.Point(4, 29);
            this.tabMemory.Name = "tabMemory";
            this.tabMemory.Padding = new System.Windows.Forms.Padding(3);
            this.tabMemory.Size = new System.Drawing.Size(192, 67);
            this.tabMemory.TabIndex = 40;
            this.tabMemory.Text = "Memory";
            this.tabMemory.UseVisualStyleBackColor = true;
            // 
            // memoryUsage1
            // 
            this.memoryUsage1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memoryUsage1.Location = new System.Drawing.Point(3, 3);
            this.memoryUsage1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.memoryUsage1.Name = "memoryUsage1";
            this.memoryUsage1.Size = new System.Drawing.Size(186, 61);
            this.memoryUsage1.TabIndex = 0;
            // 
            // tabJobStats
            // 
            this.tabJobStats.Controls.Add(this.jobStats1);
            this.tabJobStats.Location = new System.Drawing.Point(4, 29);
            this.tabJobStats.Name = "tabJobStats";
            this.tabJobStats.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobStats.Size = new System.Drawing.Size(192, 67);
            this.tabJobStats.TabIndex = 41;
            this.tabJobStats.Text = "Job Stats";
            this.tabJobStats.UseVisualStyleBackColor = true;
            // 
            // jobStats1
            // 
            this.jobStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobStats1.Location = new System.Drawing.Point(3, 3);
            this.jobStats1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.jobStats1.Name = "jobStats1";
            this.jobStats1.Size = new System.Drawing.Size(186, 61);
            this.jobStats1.TabIndex = 0;
            // 
            // tabDBADash
            // 
            this.tabDBADash.Controls.Add(this.lblVersion);
            this.tabDBADash.Controls.Add(this.lblSQLMonitoring);
            this.tabDBADash.Controls.Add(this.lblDBADash);
            this.tabDBADash.Location = new System.Drawing.Point(4, 29);
            this.tabDBADash.Name = "tabDBADash";
            this.tabDBADash.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBADash.Size = new System.Drawing.Size(192, 67);
            this.tabDBADash.TabIndex = 42;
            this.tabDBADash.UseVisualStyleBackColor = true;
            // 
            // lblVersion
            // 
            this.lblVersion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.lblVersion.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblVersion.ForeColor = System.Drawing.Color.White;
            this.lblVersion.Location = new System.Drawing.Point(3, 32);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(186, 32);
            this.lblVersion.TabIndex = 29;
            this.lblVersion.Text = "{Version}";
            // 
            // lblSQLMonitoring
            // 
            this.lblSQLMonitoring.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.lblSQLMonitoring.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSQLMonitoring.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblSQLMonitoring.ForeColor = System.Drawing.Color.White;
            this.lblSQLMonitoring.Location = new System.Drawing.Point(3, 3);
            this.lblSQLMonitoring.Name = "lblSQLMonitoring";
            this.lblSQLMonitoring.Size = new System.Drawing.Size(186, 28);
            this.lblSQLMonitoring.TabIndex = 28;
            this.lblSQLMonitoring.Text = "SQL Server Monitoring";
            this.lblSQLMonitoring.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblDBADash
            // 
            this.lblDBADash.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.lblDBADash.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDBADash.Font = new System.Drawing.Font("Segoe UI", 26F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point);
            this.lblDBADash.ForeColor = System.Drawing.Color.White;
            this.lblDBADash.Location = new System.Drawing.Point(3, 3);
            this.lblDBADash.Name = "lblDBADash";
            this.lblDBADash.Size = new System.Drawing.Size(186, 61);
            this.lblDBADash.TabIndex = 0;
            this.lblDBADash.Text = "DBA Dash";
            this.lblDBADash.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabIdentityColumns
            // 
            this.tabIdentityColumns.Controls.Add(this.identityColumns1);
            this.tabIdentityColumns.Location = new System.Drawing.Point(4, 29);
            this.tabIdentityColumns.Name = "tabIdentityColumns";
            this.tabIdentityColumns.Padding = new System.Windows.Forms.Padding(3);
            this.tabIdentityColumns.Size = new System.Drawing.Size(192, 67);
            this.tabIdentityColumns.TabIndex = 43;
            this.tabIdentityColumns.Text = "Identity Columns";
            this.tabIdentityColumns.UseVisualStyleBackColor = true;
            // 
            // identityColumns1
            // 
            this.identityColumns1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.identityColumns1.IncludeCritical = true;
            this.identityColumns1.IncludeNA = false;
            this.identityColumns1.IncludeOK = false;
            this.identityColumns1.IncludeWarning = true;
            this.identityColumns1.Location = new System.Drawing.Point(3, 3);
            this.identityColumns1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.identityColumns1.Name = "identityColumns1";
            this.identityColumns1.Size = new System.Drawing.Size(186, 61);
            this.identityColumns1.TabIndex = 0;
            // 
            // tabOSLoadedModules
            // 
            this.tabOSLoadedModules.Controls.Add(this.osLoadedModules1);
            this.tabOSLoadedModules.Location = new System.Drawing.Point(4, 29);
            this.tabOSLoadedModules.Name = "tabOSLoadedModules";
            this.tabOSLoadedModules.Padding = new System.Windows.Forms.Padding(3);
            this.tabOSLoadedModules.Size = new System.Drawing.Size(192, 67);
            this.tabOSLoadedModules.TabIndex = 44;
            this.tabOSLoadedModules.Text = "OS Loaded Modules";
            this.tabOSLoadedModules.UseVisualStyleBackColor = true;
            // 
            // osLoadedModules1
            // 
            this.osLoadedModules1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.osLoadedModules1.Location = new System.Drawing.Point(3, 3);
            this.osLoadedModules1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.osLoadedModules1.Name = "osLoadedModules1";
            this.osLoadedModules1.Size = new System.Drawing.Size(186, 61);
            this.osLoadedModules1.TabIndex = 0;
            // 
            // tabJobTimeline
            // 
            this.tabJobTimeline.Controls.Add(this.jobTimeline1);
            this.tabJobTimeline.Location = new System.Drawing.Point(4, 25);
            this.tabJobTimeline.Name = "tabJobTimeline";
            this.tabJobTimeline.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobTimeline.Size = new System.Drawing.Size(1631, 1246);
            this.tabJobTimeline.TabIndex = 45;
            this.tabJobTimeline.Text = "Timeline";
            this.tabJobTimeline.UseVisualStyleBackColor = true;
            // 
            // jobTimeline1
            // 
            this.jobTimeline1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobTimeline1.Location = new System.Drawing.Point(3, 3);
            this.jobTimeline1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.jobTimeline1.Name = "jobTimeline1";
            this.jobTimeline1.Size = new System.Drawing.Size(1625, 1240);
            this.jobTimeline1.TabIndex = 0;
            // 
            // refresh1
            // 
            this.refresh1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.refresh1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refresh1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.refresh1.ForeColor = System.Drawing.Color.White;
            this.refresh1.Location = new System.Drawing.Point(0, 0);
            this.refresh1.Margin = new System.Windows.Forms.Padding(4);
            this.refresh1.Name = "refresh1";
            this.refresh1.Size = new System.Drawing.Size(1639, 1275);
            this.refresh1.TabIndex = 1;
            this.refresh1.Visible = false;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "DB";
            this.dataGridViewTextBoxColumn1.HeaderText = "DB";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "SnapshotDate";
            this.dataGridViewTextBoxColumn2.HeaderText = "Snapshot Date";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "ValidatedDate";
            this.dataGridViewTextBoxColumn3.HeaderText = "Validated Date";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn4.DataPropertyName = "ValidForDays";
            this.dataGridViewTextBoxColumn4.HeaderText = "Valid For (Days)";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn5.DataPropertyName = "DaysSinceValidation";
            this.dataGridViewTextBoxColumn5.HeaderText = "Days Since Validation";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn6.DataPropertyName = "Created";
            this.dataGridViewTextBoxColumn6.HeaderText = "Created";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn7.DataPropertyName = "Modified";
            this.dataGridViewTextBoxColumn7.HeaderText = "Modified";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn8.DataPropertyName = "Dropped";
            this.dataGridViewTextBoxColumn8.HeaderText = "Dropped";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn9.DataPropertyName = "ObjectName";
            this.dataGridViewTextBoxColumn9.HeaderText = "Object Name";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn10.DataPropertyName = "SchemaName";
            this.dataGridViewTextBoxColumn10.HeaderText = "Schema Name";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn11.DataPropertyName = "Action";
            this.dataGridViewTextBoxColumn11.HeaderText = "Action";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn12.DataPropertyName = "ObjectName";
            this.dataGridViewTextBoxColumn12.HeaderText = "Object Name";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 125;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn13.DataPropertyName = "SchemaName";
            this.dataGridViewTextBoxColumn13.HeaderText = "Schema Name";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 125;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn14.HeaderText = "Object Type";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 125;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn15.DataPropertyName = "SnapshotValidFrom";
            this.dataGridViewTextBoxColumn15.HeaderText = "Snapshot Valid From";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 125;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn16.DataPropertyName = "SnapshotValidTo";
            this.dataGridViewTextBoxColumn16.HeaderText = "Snapshot Valid To";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            this.dataGridViewTextBoxColumn16.Width = 125;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn17.DataPropertyName = "ObjectDateCreated";
            this.dataGridViewTextBoxColumn17.HeaderText = "Date Created";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.ReadOnly = true;
            this.dataGridViewTextBoxColumn17.Width = 125;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.dataGridViewTextBoxColumn18.DataPropertyName = "ObjectDateModified";
            this.dataGridViewTextBoxColumn18.HeaderText = "Date Modified";
            this.dataGridViewTextBoxColumn18.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            this.dataGridViewTextBoxColumn18.ReadOnly = true;
            this.dataGridViewTextBoxColumn18.Width = 125;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn19.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn19.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            this.dataGridViewTextBoxColumn19.ReadOnly = true;
            this.dataGridViewTextBoxColumn19.Width = 90;
            // 
            // dataGridViewTextBoxColumn20
            // 
            this.dataGridViewTextBoxColumn20.DataPropertyName = "ErrorDate";
            this.dataGridViewTextBoxColumn20.HeaderText = "Date";
            this.dataGridViewTextBoxColumn20.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            this.dataGridViewTextBoxColumn20.ReadOnly = true;
            this.dataGridViewTextBoxColumn20.Width = 67;
            // 
            // dataGridViewTextBoxColumn21
            // 
            this.dataGridViewTextBoxColumn21.DataPropertyName = "ErrorSource";
            this.dataGridViewTextBoxColumn21.HeaderText = "Source";
            this.dataGridViewTextBoxColumn21.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            this.dataGridViewTextBoxColumn21.ReadOnly = true;
            this.dataGridViewTextBoxColumn21.Width = 82;
            // 
            // dataGridViewTextBoxColumn22
            // 
            this.dataGridViewTextBoxColumn22.DataPropertyName = "ErrorContext";
            this.dataGridViewTextBoxColumn22.HeaderText = "Error Context";
            this.dataGridViewTextBoxColumn22.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            this.dataGridViewTextBoxColumn22.ReadOnly = true;
            this.dataGridViewTextBoxColumn22.Width = 120;
            // 
            // dataGridViewTextBoxColumn23
            // 
            this.dataGridViewTextBoxColumn23.DataPropertyName = "ErrorMessage";
            this.dataGridViewTextBoxColumn23.HeaderText = "Message";
            this.dataGridViewTextBoxColumn23.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            this.dataGridViewTextBoxColumn23.ReadOnly = true;
            this.dataGridViewTextBoxColumn23.Width = 94;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1983, 1309);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.Text = "DBA Dash";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Main_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.tabs.ResumeLayout(false);
            this.tabSnapshotsSummary.ResumeLayout(false);
            this.tabSchema.ResumeLayout(false);
            this.splitSchemaSnapshot.Panel1.ResumeLayout(false);
            this.splitSchemaSnapshot.Panel1.PerformLayout();
            this.splitSchemaSnapshot.Panel2.ResumeLayout(false);
            this.splitSchemaSnapshot.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitSchemaSnapshot)).EndInit();
            this.splitSchemaSnapshot.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gvHistory)).EndInit();
            this.tabTags.ResumeLayout(false);
            this.tabDrives.ResumeLayout(false);
            this.tabBackups.ResumeLayout(false);
            this.tabLogShipping.ResumeLayout(false);
            this.tabJobs.ResumeLayout(false);
            this.tabSummary.ResumeLayout(false);
            this.tabFiles.ResumeLayout(false);
            this.tabLastGood.ResumeLayout(false);
            this.tabPerformance.ResumeLayout(false);
            this.tabDBADashErrorLog.ResumeLayout(false);
            this.tabCollectionDates.ResumeLayout(false);
            this.tabPerformanceSummary.ResumeLayout(false);
            this.tabInfo.ResumeLayout(false);
            this.tabHardware.ResumeLayout(false);
            this.tabSQLPatching.ResumeLayout(false);
            this.tabInstanceConfig.ResumeLayout(false);
            this.tabSlowQueries.ResumeLayout(false);
            this.tabTraceFlags.ResumeLayout(false);
            this.tabAlerts.ResumeLayout(false);
            this.tabDrivers.ResumeLayout(false);
            this.tabDBSpace.ResumeLayout(false);
            this.tabAzureSummary.ResumeLayout(false);
            this.tabAzureDB.ResumeLayout(false);
            this.tabServiceObjectives.ResumeLayout(false);
            this.tabDBConfiguration.ResumeLayout(false);
            this.tabDBOptions.ResumeLayout(false);
            this.tabTempDB.ResumeLayout(false);
            this.tabCustomChecks.ResumeLayout(false);
            this.tabPC.ResumeLayout(false);
            this.tabObjectExecutionSummary.ResumeLayout(false);
            this.tabWaits.ResumeLayout(false);
            this.tabMirroring.ResumeLayout(false);
            this.tabJobDDL.ResumeLayout(false);
            this.tabAG.ResumeLayout(false);
            this.tabQS.ResumeLayout(false);
            this.tabRG.ResumeLayout(false);
            this.tabAzureDBesourceGovernance.ResumeLayout(false);
            this.tabRunningQueries.ResumeLayout(false);
            this.tabMemory.ResumeLayout(false);
            this.tabJobStats.ResumeLayout(false);
            this.tabDBADash.ResumeLayout(false);
            this.tabIdentityColumns.ResumeLayout(false);
            this.tabOSLoadedModules.ResumeLayout(false);
            this.tabJobTimeline.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tv1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabSchema;
        private System.Windows.Forms.TabPage tabSnapshotsSummary;
        private System.Windows.Forms.ImageList TreeViewImageList;
        private System.Windows.Forms.SplitContainer splitSchemaSnapshot;
        private System.Windows.Forms.DataGridView gvHistory;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsPrevious;
        private System.Windows.Forms.ToolStripLabel tsPageNum;
        private System.Windows.Forms.ToolStripButton tsNext;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox tsPageSize;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage tabTags;
        private System.Windows.Forms.TabPage tabDrives;
        private Properties.DrivesControl drivesControl1;
        private System.Windows.Forms.TabPage tabBackups;
        private Backups.BackupsControl backupsControl1;
        private System.Windows.Forms.TabPage tabLogShipping;
        private LogShipping.LogShippingControl logShippingControl1;
        private System.Windows.Forms.TabPage tabJobs;
        private AgentJobs.AgentJobsControl agentJobsControl1;
        private System.Windows.Forms.TabPage tabSummary;
        private Summary summary1;
        private System.Windows.Forms.TabPage tabFiles;
        private DBFiles.DBFilesControl dbFilesControl1;
        private System.Windows.Forms.TabPage tabLastGood;
        private LastGoodCheckDB.LastGoodCheckDBControl lastGoodCheckDBControl1;
        private System.Windows.Forms.TabPage tabPerformance;
        private Performance.Performance performance1;
        private System.Windows.Forms.TabPage tabDBADashErrorLog;
        private System.Windows.Forms.TabPage tabCollectionDates;
        private CollectionDates.CollectionDates collectionDates1;
        private System.Windows.Forms.TabPage tabPerformanceSummary;
        private Performance.PerformanceSummary performanceSummary1;
        private System.Windows.Forms.TabPage tabInfo;
        private Info info1;
        private System.Windows.Forms.TabPage tabHardware;
        private HardwareChanges hardwareChanges1;
        private System.Windows.Forms.TabPage tabSQLPatching;
        private SQLPatching sqlPatching1;
        private System.Windows.Forms.TabPage tabInstanceConfig;
        private ConfigurationHistory configurationHistory1;
        private System.Windows.Forms.TabPage tabSlowQueries;
        private SlowQueries slowQueries1;
        private System.Windows.Forms.TabPage tabTraceFlags;
        private Changes.TraceFlagHistory traceFlagHistory1;
        private System.Windows.Forms.TabPage tabAlerts;
        private Changes.Alerts alerts1;
        private System.Windows.Forms.TabPage tabDrivers;
        private Changes.Drivers drivers1;
        private System.Windows.Forms.TabPage tabDBSpace;
        private SpaceTracking spaceTracking1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dataRetentionToolStripMenuItem;
        private System.Windows.Forms.TabPage tabAzureSummary;
        private Performance.AzureSummary azureSummary1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private System.Windows.Forms.TabPage tabAzureDB;
        private Performance.AzureDBResourceStats azureDBResourceStats1;
        private System.Windows.Forms.TabPage tabServiceObjectives;
        private Changes.AzureServiceObjectivesHistory azureServiceObjectivesHistory1;
        private System.Windows.Forms.TabPage tabDBConfiguration;
        private Changes.DBConfiguration dbConfiguration1;
        private System.Windows.Forms.TabPage tabDBOptions;
        private Changes.DBOptions dbOptions1;
        private System.Windows.Forms.TabPage tabTempDB;
        private DBFiles.TempDBConfig tempDBConfig1;
        private System.Windows.Forms.TabPage tabCustomChecks;
        private Checks.CustomChecks customChecks1;
        private System.Windows.Forms.TabPage tabPC;
        private Performance.PerformanceCounterSummary performanceCounterSummary1;
        private CollectionDates.CollectionErrors collectionErrors1;
        private System.Windows.Forms.TabPage tabObjectExecutionSummary;
        private Performance.ObjectExecutionSummary objectExecutionSummary1;
        private Changes.SchemaSnapshots schemaSnapshots1;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem minsToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem minsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem minsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days14toolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days28ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private System.Windows.Forms.TabPage tabWaits;
        private Performance.WaitsSummary waitsSummary1;
        private System.Windows.Forms.TabPage tabMirroring;
        private HA.Mirroring mirroring1;
        private System.Windows.Forms.ToolStripMenuItem manageInstancesToolStripMenuItem;
        private System.Windows.Forms.TabPage tabJobDDL;
        private Changes.JobDDLHistory jobDDLHistory1;
        private System.Windows.Forms.TabPage tabAG;
        private HA.AG ag1;
        private System.Windows.Forms.TabPage tabQS;
        private Changes.QueryStore queryStore1;
        private System.Windows.Forms.TabPage tabRG;
        private Changes.ResourceGovernor resourceGovernor1;
        private System.Windows.Forms.TabPage tabAzureDBesourceGovernance;
        private Changes.AzureDBResourceGovernance azureDBResourceGovernance1;
        private System.Windows.Forms.ToolStripMenuItem daysToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daysToolStripMenuItem1;
        private System.Windows.Forms.TabPage tabRunningQueries;
        private Performance.RunningQueries runningQueries1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchemaName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectType;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotValidTo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectDateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectDateModified;
        private System.Windows.Forms.DataGridViewLinkColumn colCompare;
        private Tagging.Tags tags1;
        private System.Windows.Forms.TabPage tabMemory;
        private Performance.MemoryUsage memoryUsage1;
        private System.Windows.Forms.TabPage tabJobStats;
        private AgentJobs.JobStats jobStats1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.Panel pnlSearch;
        private System.Windows.Forms.Button bttnSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ToolStripMenuItem diffToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem databaseSchemaDiffToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem agentJobDiffToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureDisplayNameToolStripMenuItem;
        private System.Windows.Forms.TabPage tabDBADash;
        private System.Windows.Forms.Label lblDBADash;
        private System.Windows.Forms.Label lblSQLMonitoring;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.ToolStripMenuItem freezeKeyColumnsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dummyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsTimeFilter;
        private System.Windows.Forms.ToolStripMenuItem tsDayOfWeek;
        private System.Windows.Forms.TabPage tabIdentityColumns;
        private Checks.IdentityColumns identityColumns1;
        private System.Windows.Forms.TabPage tabOSLoadedModules;
        private Checks.OSLoadedModules osLoadedModules1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsHome;
        private System.Windows.Forms.ToolStripButton tsBack;
        private Refresh refresh1;
        private System.Windows.Forms.ToolStripMenuItem tsConnect;
        private System.Windows.Forms.ToolStripDropDownButton mnuTags;
        private System.Windows.Forms.ToolStripDropDownButton groupToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox cboTimeZone;
        private System.Windows.Forms.ToolStripMenuItem saveTimeZonePreferenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHiddenToolStripMenuItem;
        private System.Windows.Forms.TabPage tabJobTimeline;
        private AgentJobs.JobTimeline jobTimeline1;
    }
}