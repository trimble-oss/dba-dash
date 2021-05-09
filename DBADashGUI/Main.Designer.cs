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
            this.mnuTags = new System.Windows.Forms.ToolStripMenuItem();
            this.dBDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsJobDiff = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataRetentionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days14toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days28ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tv1 = new System.Windows.Forms.TreeView();
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
            this.label1 = new System.Windows.Forms.Label();
            this.tabTags = new System.Windows.Forms.TabPage();
            this.chkTags = new System.Windows.Forms.CheckedListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.cboTagName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboTagValue = new System.Windows.Forms.ComboBox();
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
            this.panel1.SuspendLayout();
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
            this.SuspendLayout();
            // 
            // TreeViewImageList
            // 
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
            // 
            // menuStrip1
            // 
            this.menuStrip1.AutoSize = false;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuTags,
            this.dBDiffToolStripMenuItem,
            this.tsJobDiff,
            this.optionsToolStripMenuItem,
            this.tsTime});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1983, 34);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuTags
            // 
            this.mnuTags.Name = "mnuTags";
            this.mnuTags.Size = new System.Drawing.Size(52, 30);
            this.mnuTags.Text = "Tags";
            // 
            // dBDiffToolStripMenuItem
            // 
            this.dBDiffToolStripMenuItem.Name = "dBDiffToolStripMenuItem";
            this.dBDiffToolStripMenuItem.Size = new System.Drawing.Size(72, 30);
            this.dBDiffToolStripMenuItem.Text = "DB Diff";
            this.dBDiffToolStripMenuItem.Click += new System.EventHandler(this.dBDiffToolStripMenuItem_Click);
            // 
            // tsJobDiff
            // 
            this.tsJobDiff.Name = "tsJobDiff";
            this.tsJobDiff.Size = new System.Drawing.Size(75, 30);
            this.tsJobDiff.Text = "Job Diff";
            this.tsJobDiff.Click += new System.EventHandler(this.tsJobDiff_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dataRetentionToolStripMenuItem,
            this.manageInstancesToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(75, 30);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // dataRetentionToolStripMenuItem
            // 
            this.dataRetentionToolStripMenuItem.Name = "dataRetentionToolStripMenuItem";
            this.dataRetentionToolStripMenuItem.Size = new System.Drawing.Size(210, 26);
            this.dataRetentionToolStripMenuItem.Text = "Data Retention";
            this.dataRetentionToolStripMenuItem.Click += new System.EventHandler(this.DataRetentionToolStripMenuItem_Click);
            // 
            // manageInstancesToolStripMenuItem
            // 
            this.manageInstancesToolStripMenuItem.Name = "manageInstancesToolStripMenuItem";
            this.manageInstancesToolStripMenuItem.Size = new System.Drawing.Size(210, 26);
            this.manageInstancesToolStripMenuItem.Text = "Manage Instances";
            this.manageInstancesToolStripMenuItem.Click += new System.EventHandler(this.manageInstancesToolStripMenuItem_Click);
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
            this.days7ToolStripMenuItem,
            this.days14toolStripMenuItem,
            this.days28ToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Font = new System.Drawing.Font("Segoe UI", 10F);
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
            this.minsToolStripMenuItem2.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // minsToolStripMenuItem1
            // 
            this.minsToolStripMenuItem1.CheckOnClick = true;
            this.minsToolStripMenuItem1.Name = "minsToolStripMenuItem1";
            this.minsToolStripMenuItem1.Size = new System.Drawing.Size(153, 28);
            this.minsToolStripMenuItem1.Tag = "10";
            this.minsToolStripMenuItem1.Text = "10 Mins";
            this.minsToolStripMenuItem1.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // minsToolStripMenuItem
            // 
            this.minsToolStripMenuItem.CheckOnClick = true;
            this.minsToolStripMenuItem.Name = "minsToolStripMenuItem";
            this.minsToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.minsToolStripMenuItem.Tag = "15";
            this.minsToolStripMenuItem.Text = "15 Mins";
            this.minsToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(153, 28);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(153, 28);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(153, 28);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(153, 28);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(153, 28);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(153, 28);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days14toolStripMenuItem
            // 
            this.days14toolStripMenuItem.Name = "days14toolStripMenuItem";
            this.days14toolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days14toolStripMenuItem.Tag = "20160";
            this.days14toolStripMenuItem.Text = "14 Days";
            this.days14toolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days28ToolStripMenuItem
            // 
            this.days28ToolStripMenuItem.Name = "days28ToolStripMenuItem";
            this.days28ToolStripMenuItem.Size = new System.Drawing.Size(153, 28);
            this.days28ToolStripMenuItem.Tag = "40320";
            this.days28ToolStripMenuItem.Text = "28 Days";
            this.days28ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(150, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(153, 28);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.tsCustomTime_Click);
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
            this.splitMain.Panel1MinSize = 50;
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.tabs);
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
            this.tv1.Location = new System.Drawing.Point(0, 0);
            this.tv1.Name = "tv1";
            this.tv1.SelectedImageIndex = 0;
            this.tv1.Size = new System.Drawing.Size(340, 1275);
            this.tv1.TabIndex = 0;
            this.tv1.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tv1_BeforeExpand);
            this.tv1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tv1_AfterSelect);
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
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 0);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1639, 1275);
            this.tabs.TabIndex = 0;
            this.tabs.Tag = "";
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
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
            this.schemaSnapshots1.Name = "schemaSnapshots1";
            this.schemaSnapshots1.Size = new System.Drawing.Size(1625, 1240);
            this.schemaSnapshots1.TabIndex = 0;
            // 
            // tabSchema
            // 
            this.tabSchema.Controls.Add(this.splitSchemaSnapshot);
            this.tabSchema.Location = new System.Drawing.Point(4, 25);
            this.tabSchema.Name = "tabSchema";
            this.tabSchema.Padding = new System.Windows.Forms.Padding(3);
            this.tabSchema.Size = new System.Drawing.Size(1631, 1246);
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
            this.splitSchemaSnapshot.Size = new System.Drawing.Size(1625, 1240);
            this.splitSchemaSnapshot.SplitterDistance = 566;
            this.splitSchemaSnapshot.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(215, 236);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(331, 17);
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
            this.toolStrip1.Location = new System.Drawing.Point(0, 642);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1625, 28);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsPrevious
            // 
            this.tsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPrevious.Image = ((System.Drawing.Image)(resources.GetObject("tsPrevious.Image")));
            this.tsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPrevious.Name = "tsPrevious";
            this.tsPrevious.Size = new System.Drawing.Size(29, 25);
            this.tsPrevious.Text = "Previous";
            this.tsPrevious.Click += new System.EventHandler(this.tsPrevious_Click);
            // 
            // tsPageNum
            // 
            this.tsPageNum.Name = "tsPageNum";
            this.tsPageNum.Size = new System.Drawing.Size(53, 25);
            this.tsPageNum.Text = "Page 1";
            // 
            // tsNext
            // 
            this.tsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNext.Image = ((System.Drawing.Image)(resources.GetObject("tsNext.Image")));
            this.tsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNext.Name = "tsNext";
            this.tsNext.Size = new System.Drawing.Size(29, 25);
            this.tsNext.Text = "Next";
            this.tsNext.Click += new System.EventHandler(this.tsNext_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(75, 25);
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
            this.tsPageSize.Validating += new System.ComponentModel.CancelEventHandler(this.tsPageSize_Validating);
            this.tsPageSize.Validated += new System.EventHandler(this.tsPageSize_Validated);
            // 
            // gvHistory
            // 
            this.gvHistory.AllowUserToAddRows = false;
            this.gvHistory.AllowUserToDeleteRows = false;
            this.gvHistory.AllowUserToOrderColumns = true;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ObjectName,
            this.SchemaName,
            this.ObjectType,
            this.SnapshotValidFrom,
            this.SnapshotValidTo,
            this.ObjectDateCreated,
            this.ObjectDateModified});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gvHistory.DefaultCellStyle = dataGridViewCellStyle2;
            this.gvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvHistory.Location = new System.Drawing.Point(0, 25);
            this.gvHistory.MultiSelect = false;
            this.gvHistory.Name = "gvHistory";
            this.gvHistory.ReadOnly = true;
            this.gvHistory.RowHeadersWidth = 51;
            this.gvHistory.RowTemplate.Height = 24;
            this.gvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gvHistory.Size = new System.Drawing.Size(1625, 645);
            this.gvHistory.TabIndex = 0;
            this.gvHistory.SelectionChanged += new System.EventHandler(this.gvHistory_SelectionChanged);
            // 
            // ObjectName
            // 
            this.ObjectName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.ObjectName.DataPropertyName = "ObjectName";
            this.ObjectName.HeaderText = "Object Name";
            this.ObjectName.MinimumWidth = 6;
            this.ObjectName.Name = "ObjectName";
            this.ObjectName.ReadOnly = true;
            this.ObjectName.Width = 109;
            // 
            // SchemaName
            // 
            this.SchemaName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.SchemaName.DataPropertyName = "SchemaName";
            this.SchemaName.HeaderText = "Schema Name";
            this.SchemaName.MinimumWidth = 6;
            this.SchemaName.Name = "SchemaName";
            this.SchemaName.ReadOnly = true;
            this.SchemaName.Width = 118;
            // 
            // ObjectType
            // 
            this.ObjectType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.ObjectType.HeaderText = "Object Type";
            this.ObjectType.MinimumWidth = 6;
            this.ObjectType.Name = "ObjectType";
            this.ObjectType.ReadOnly = true;
            this.ObjectType.Width = 105;
            // 
            // SnapshotValidFrom
            // 
            this.SnapshotValidFrom.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.SnapshotValidFrom.DataPropertyName = "SnapshotValidFrom";
            this.SnapshotValidFrom.HeaderText = "Snapshot Valid From";
            this.SnapshotValidFrom.MinimumWidth = 6;
            this.SnapshotValidFrom.Name = "SnapshotValidFrom";
            this.SnapshotValidFrom.ReadOnly = true;
            this.SnapshotValidFrom.Width = 125;
            // 
            // SnapshotValidTo
            // 
            this.SnapshotValidTo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.SnapshotValidTo.DataPropertyName = "SnapshotValidTo";
            this.SnapshotValidTo.HeaderText = "Snapshot Valid To";
            this.SnapshotValidTo.MinimumWidth = 6;
            this.SnapshotValidTo.Name = "SnapshotValidTo";
            this.SnapshotValidTo.ReadOnly = true;
            this.SnapshotValidTo.Width = 125;
            // 
            // ObjectDateCreated
            // 
            this.ObjectDateCreated.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.ObjectDateCreated.DataPropertyName = "ObjectDateCreated";
            this.ObjectDateCreated.HeaderText = "Date Created";
            this.ObjectDateCreated.MinimumWidth = 6;
            this.ObjectDateCreated.Name = "ObjectDateCreated";
            this.ObjectDateCreated.ReadOnly = true;
            this.ObjectDateCreated.Width = 111;
            // 
            // ObjectDateModified
            // 
            this.ObjectDateModified.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.ObjectDateModified.DataPropertyName = "ObjectDateModified";
            this.ObjectDateModified.HeaderText = "Date Modified";
            this.ObjectDateModified.MinimumWidth = 6;
            this.ObjectDateModified.Name = "ObjectDateModified";
            this.ObjectDateModified.ReadOnly = true;
            this.ObjectDateModified.Width = 114;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1625, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Snapshot History";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // tabTags
            // 
            this.tabTags.Controls.Add(this.chkTags);
            this.tabTags.Controls.Add(this.panel1);
            this.tabTags.Location = new System.Drawing.Point(4, 25);
            this.tabTags.Name = "tabTags";
            this.tabTags.Padding = new System.Windows.Forms.Padding(3);
            this.tabTags.Size = new System.Drawing.Size(1631, 1246);
            this.tabTags.TabIndex = 2;
            this.tabTags.Text = "Tags";
            this.tabTags.UseVisualStyleBackColor = true;
            // 
            // chkTags
            // 
            this.chkTags.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkTags.FormattingEnabled = true;
            this.chkTags.Location = new System.Drawing.Point(3, 136);
            this.chkTags.Name = "chkTags";
            this.chkTags.Size = new System.Drawing.Size(1625, 1107);
            this.chkTags.TabIndex = 0;
            this.chkTags.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chkTags_ItemCheck);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.bttnAdd);
            this.panel1.Controls.Add(this.cboTagName);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cboTagValue);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1625, 133);
            this.panel1.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Tag Name:";
            // 
            // bttnAdd
            // 
            this.bttnAdd.Location = new System.Drawing.Point(640, 17);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(75, 23);
            this.bttnAdd.TabIndex = 5;
            this.bttnAdd.Text = "Add";
            this.bttnAdd.UseVisualStyleBackColor = true;
            this.bttnAdd.Click += new System.EventHandler(this.bttnAdd_Click);
            // 
            // cboTagName
            // 
            this.cboTagName.FormattingEnabled = true;
            this.cboTagName.Location = new System.Drawing.Point(102, 17);
            this.cboTagName.Name = "cboTagName";
            this.cboTagName.Size = new System.Drawing.Size(177, 24);
            this.cboTagName.TabIndex = 1;
            this.cboTagName.SelectedValueChanged += new System.EventHandler(this.cboTagName_SelectedValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(311, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Tag Value:";
            // 
            // cboTagValue
            // 
            this.cboTagValue.FormattingEnabled = true;
            this.cboTagValue.Location = new System.Drawing.Point(408, 17);
            this.cboTagValue.Name = "cboTagValue";
            this.cboTagValue.Size = new System.Drawing.Size(178, 24);
            this.cboTagValue.TabIndex = 3;
            // 
            // tabDrives
            // 
            this.tabDrives.AutoScroll = true;
            this.tabDrives.Controls.Add(this.drivesControl1);
            this.tabDrives.Location = new System.Drawing.Point(4, 25);
            this.tabDrives.Name = "tabDrives";
            this.tabDrives.Padding = new System.Windows.Forms.Padding(3);
            this.tabDrives.Size = new System.Drawing.Size(1631, 1246);
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
            this.drivesControl1.Name = "drivesControl1";
            this.drivesControl1.Size = new System.Drawing.Size(1625, 1240);
            this.drivesControl1.TabIndex = 0;
            // 
            // tabBackups
            // 
            this.tabBackups.Controls.Add(this.backupsControl1);
            this.tabBackups.Location = new System.Drawing.Point(4, 25);
            this.tabBackups.Name = "tabBackups";
            this.tabBackups.Padding = new System.Windows.Forms.Padding(3);
            this.tabBackups.Size = new System.Drawing.Size(1631, 1246);
            this.tabBackups.TabIndex = 4;
            this.tabBackups.Tag = "1";
            this.tabBackups.Text = "Backups";
            this.tabBackups.UseVisualStyleBackColor = true;
            // 
            // backupsControl1
            // 
            this.backupsControl1.DatabaseID = null;
            this.backupsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.backupsControl1.IncludeCritical = false;
            this.backupsControl1.IncludeNA = false;
            this.backupsControl1.IncludeOK = false;
            this.backupsControl1.IncludeWarning = false;
            this.backupsControl1.InstanceIDs = null;
            this.backupsControl1.Location = new System.Drawing.Point(3, 3);
            this.backupsControl1.Name = "backupsControl1";
            this.backupsControl1.Size = new System.Drawing.Size(1625, 1240);
            this.backupsControl1.TabIndex = 0;
            // 
            // tabLogShipping
            // 
            this.tabLogShipping.Controls.Add(this.logShippingControl1);
            this.tabLogShipping.Location = new System.Drawing.Point(4, 25);
            this.tabLogShipping.Name = "tabLogShipping";
            this.tabLogShipping.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogShipping.Size = new System.Drawing.Size(1631, 1246);
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
            this.logShippingControl1.Name = "logShippingControl1";
            this.logShippingControl1.Size = new System.Drawing.Size(1625, 1240);
            this.logShippingControl1.TabIndex = 0;
            // 
            // tabJobs
            // 
            this.tabJobs.Controls.Add(this.agentJobsControl1);
            this.tabJobs.Location = new System.Drawing.Point(4, 25);
            this.tabJobs.Name = "tabJobs";
            this.tabJobs.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobs.Size = new System.Drawing.Size(1631, 1246);
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
            this.agentJobsControl1.Name = "agentJobsControl1";
            this.agentJobsControl1.Size = new System.Drawing.Size(1625, 1240);
            this.agentJobsControl1.TabIndex = 0;
            // 
            // tabSummary
            // 
            this.tabSummary.Controls.Add(this.summary1);
            this.tabSummary.Location = new System.Drawing.Point(4, 25);
            this.tabSummary.Name = "tabSummary";
            this.tabSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabSummary.Size = new System.Drawing.Size(1631, 1246);
            this.tabSummary.TabIndex = 7;
            this.tabSummary.Text = "Summary";
            this.tabSummary.UseVisualStyleBackColor = true;
            // 
            // summary1
            // 
            this.summary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.summary1.Location = new System.Drawing.Point(3, 3);
            this.summary1.Name = "summary1";
            this.summary1.Size = new System.Drawing.Size(1625, 1240);
            this.summary1.TabIndex = 0;
            this.summary1.Instance_Selected += new System.EventHandler<DBADashGUI.Main.InstanceSelectedEventArgs>(this.Instance_Selected);
            // 
            // tabFiles
            // 
            this.tabFiles.Controls.Add(this.dbFilesControl1);
            this.tabFiles.Location = new System.Drawing.Point(4, 25);
            this.tabFiles.Name = "tabFiles";
            this.tabFiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabFiles.Size = new System.Drawing.Size(1631, 1246);
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
            this.dbFilesControl1.Name = "dbFilesControl1";
            this.dbFilesControl1.Size = new System.Drawing.Size(1625, 1240);
            this.dbFilesControl1.TabIndex = 0;
            // 
            // tabLastGood
            // 
            this.tabLastGood.Controls.Add(this.lastGoodCheckDBControl1);
            this.tabLastGood.Location = new System.Drawing.Point(4, 25);
            this.tabLastGood.Name = "tabLastGood";
            this.tabLastGood.Padding = new System.Windows.Forms.Padding(3);
            this.tabLastGood.Size = new System.Drawing.Size(1631, 1246);
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
            this.lastGoodCheckDBControl1.Name = "lastGoodCheckDBControl1";
            this.lastGoodCheckDBControl1.Size = new System.Drawing.Size(1625, 1240);
            this.lastGoodCheckDBControl1.TabIndex = 0;
            // 
            // tabPerformance
            // 
            this.tabPerformance.Controls.Add(this.performance1);
            this.tabPerformance.Location = new System.Drawing.Point(4, 25);
            this.tabPerformance.Name = "tabPerformance";
            this.tabPerformance.Padding = new System.Windows.Forms.Padding(3);
            this.tabPerformance.Size = new System.Drawing.Size(1631, 1246);
            this.tabPerformance.TabIndex = 10;
            this.tabPerformance.Text = "Performance";
            this.tabPerformance.UseVisualStyleBackColor = true;
            // 
            // performance1
            // 
            this.performance1.AutoScroll = true;
            this.performance1.DatabaseID = 0;
            this.performance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performance1.InstanceID = 0;
            this.performance1.Location = new System.Drawing.Point(3, 3);
            this.performance1.Name = "performance1";
            this.performance1.ObjectID = ((long)(0));
            this.performance1.Size = new System.Drawing.Size(1625, 1240);
            this.performance1.TabIndex = 0;
            // 
            // tabDBADashErrorLog
            // 
            this.tabDBADashErrorLog.Controls.Add(this.collectionErrors1);
            this.tabDBADashErrorLog.Location = new System.Drawing.Point(4, 25);
            this.tabDBADashErrorLog.Name = "tabDBADashErrorLog";
            this.tabDBADashErrorLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBADashErrorLog.Size = new System.Drawing.Size(1631, 1246);
            this.tabDBADashErrorLog.TabIndex = 11;
            this.tabDBADashErrorLog.Text = "DBA Dash ErrorLog";
            this.tabDBADashErrorLog.UseVisualStyleBackColor = true;
            // 
            // collectionErrors1
            // 
            this.collectionErrors1.AckErrors = false;
            this.collectionErrors1.Days = 0;
            this.collectionErrors1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.collectionErrors1.InstanceID = 0;
            this.collectionErrors1.InstanceName = null;
            this.collectionErrors1.Location = new System.Drawing.Point(3, 3);
            this.collectionErrors1.Name = "collectionErrors1";
            this.collectionErrors1.Size = new System.Drawing.Size(1625, 1240);
            this.collectionErrors1.TabIndex = 0;
            // 
            // tabCollectionDates
            // 
            this.tabCollectionDates.Controls.Add(this.collectionDates1);
            this.tabCollectionDates.Location = new System.Drawing.Point(4, 25);
            this.tabCollectionDates.Name = "tabCollectionDates";
            this.tabCollectionDates.Padding = new System.Windows.Forms.Padding(3);
            this.tabCollectionDates.Size = new System.Drawing.Size(1631, 1246);
            this.tabCollectionDates.TabIndex = 12;
            this.tabCollectionDates.Text = "Collection Dates";
            this.tabCollectionDates.UseVisualStyleBackColor = true;
            // 
            // collectionDates1
            // 
            this.collectionDates1.ConnectionString = null;
            this.collectionDates1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.collectionDates1.IncludeCritical = false;
            this.collectionDates1.IncludeNA = false;
            this.collectionDates1.IncludeOK = false;
            this.collectionDates1.IncludeWarning = false;
            this.collectionDates1.InstanceIDs = null;
            this.collectionDates1.Location = new System.Drawing.Point(3, 3);
            this.collectionDates1.Name = "collectionDates1";
            this.collectionDates1.Size = new System.Drawing.Size(1625, 1240);
            this.collectionDates1.TabIndex = 0;
            // 
            // tabPerformanceSummary
            // 
            this.tabPerformanceSummary.Controls.Add(this.performanceSummary1);
            this.tabPerformanceSummary.Location = new System.Drawing.Point(4, 25);
            this.tabPerformanceSummary.Name = "tabPerformanceSummary";
            this.tabPerformanceSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPerformanceSummary.Size = new System.Drawing.Size(1631, 1246);
            this.tabPerformanceSummary.TabIndex = 13;
            this.tabPerformanceSummary.Text = "Performance Summary";
            this.tabPerformanceSummary.UseVisualStyleBackColor = true;
            // 
            // performanceSummary1
            // 
            this.performanceSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceSummary1.Location = new System.Drawing.Point(3, 3);
            this.performanceSummary1.Name = "performanceSummary1";
            this.performanceSummary1.Size = new System.Drawing.Size(1625, 1240);
            this.performanceSummary1.TabIndex = 0;
            this.performanceSummary1.Instance_Selected += new System.EventHandler<DBADashGUI.Main.InstanceSelectedEventArgs>(this.Instance_Selected);
            // 
            // tabInfo
            // 
            this.tabInfo.Controls.Add(this.info1);
            this.tabInfo.Location = new System.Drawing.Point(4, 25);
            this.tabInfo.Name = "tabInfo";
            this.tabInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tabInfo.Size = new System.Drawing.Size(1631, 1246);
            this.tabInfo.TabIndex = 14;
            this.tabInfo.Text = "Info";
            this.tabInfo.UseVisualStyleBackColor = true;
            // 
            // info1
            // 
            this.info1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.info1.Location = new System.Drawing.Point(3, 3);
            this.info1.Name = "info1";
            this.info1.Size = new System.Drawing.Size(1625, 1240);
            this.info1.TabIndex = 0;
            // 
            // tabHardware
            // 
            this.tabHardware.Controls.Add(this.hardwareChanges1);
            this.tabHardware.Location = new System.Drawing.Point(4, 25);
            this.tabHardware.Name = "tabHardware";
            this.tabHardware.Padding = new System.Windows.Forms.Padding(3);
            this.tabHardware.Size = new System.Drawing.Size(1631, 1246);
            this.tabHardware.TabIndex = 15;
            this.tabHardware.Text = "Hardware";
            this.tabHardware.UseVisualStyleBackColor = true;
            // 
            // hardwareChanges1
            // 
            this.hardwareChanges1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hardwareChanges1.Location = new System.Drawing.Point(3, 3);
            this.hardwareChanges1.Name = "hardwareChanges1";
            this.hardwareChanges1.Size = new System.Drawing.Size(1625, 1240);
            this.hardwareChanges1.TabIndex = 0;
            // 
            // tabSQLPatching
            // 
            this.tabSQLPatching.Controls.Add(this.sqlPatching1);
            this.tabSQLPatching.Location = new System.Drawing.Point(4, 25);
            this.tabSQLPatching.Name = "tabSQLPatching";
            this.tabSQLPatching.Padding = new System.Windows.Forms.Padding(3);
            this.tabSQLPatching.Size = new System.Drawing.Size(1631, 1246);
            this.tabSQLPatching.TabIndex = 16;
            this.tabSQLPatching.Text = "SQL Patching";
            this.tabSQLPatching.UseVisualStyleBackColor = true;
            // 
            // sqlPatching1
            // 
            this.sqlPatching1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sqlPatching1.Location = new System.Drawing.Point(3, 3);
            this.sqlPatching1.Name = "sqlPatching1";
            this.sqlPatching1.Size = new System.Drawing.Size(1625, 1240);
            this.sqlPatching1.TabIndex = 0;
            // 
            // tabInstanceConfig
            // 
            this.tabInstanceConfig.Controls.Add(this.configurationHistory1);
            this.tabInstanceConfig.Location = new System.Drawing.Point(4, 25);
            this.tabInstanceConfig.Name = "tabInstanceConfig";
            this.tabInstanceConfig.Padding = new System.Windows.Forms.Padding(3);
            this.tabInstanceConfig.Size = new System.Drawing.Size(1631, 1246);
            this.tabInstanceConfig.TabIndex = 17;
            this.tabInstanceConfig.Text = "Configuration";
            this.tabInstanceConfig.UseVisualStyleBackColor = true;
            // 
            // configurationHistory1
            // 
            this.configurationHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configurationHistory1.Location = new System.Drawing.Point(3, 3);
            this.configurationHistory1.Name = "configurationHistory1";
            this.configurationHistory1.Size = new System.Drawing.Size(1625, 1240);
            this.configurationHistory1.TabIndex = 0;
            // 
            // tabSlowQueries
            // 
            this.tabSlowQueries.Controls.Add(this.slowQueries1);
            this.tabSlowQueries.Location = new System.Drawing.Point(4, 25);
            this.tabSlowQueries.Name = "tabSlowQueries";
            this.tabSlowQueries.Padding = new System.Windows.Forms.Padding(3);
            this.tabSlowQueries.Size = new System.Drawing.Size(1631, 1246);
            this.tabSlowQueries.TabIndex = 18;
            this.tabSlowQueries.Text = "Slow Queries";
            this.tabSlowQueries.UseVisualStyleBackColor = true;
            // 
            // slowQueries1
            // 
            this.slowQueries1.DBName = "";
            this.slowQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slowQueries1.Location = new System.Drawing.Point(3, 3);
            this.slowQueries1.Name = "slowQueries1";
            this.slowQueries1.Size = new System.Drawing.Size(1625, 1240);
            this.slowQueries1.TabIndex = 0;
            // 
            // tabTraceFlags
            // 
            this.tabTraceFlags.Controls.Add(this.traceFlagHistory1);
            this.tabTraceFlags.Location = new System.Drawing.Point(4, 25);
            this.tabTraceFlags.Name = "tabTraceFlags";
            this.tabTraceFlags.Padding = new System.Windows.Forms.Padding(3);
            this.tabTraceFlags.Size = new System.Drawing.Size(1631, 1246);
            this.tabTraceFlags.TabIndex = 19;
            this.tabTraceFlags.Text = "Trace Flags";
            this.tabTraceFlags.UseVisualStyleBackColor = true;
            // 
            // traceFlagHistory1
            // 
            this.traceFlagHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.traceFlagHistory1.Location = new System.Drawing.Point(3, 3);
            this.traceFlagHistory1.Name = "traceFlagHistory1";
            this.traceFlagHistory1.Size = new System.Drawing.Size(1625, 1240);
            this.traceFlagHistory1.TabIndex = 0;
            // 
            // tabAlerts
            // 
            this.tabAlerts.Controls.Add(this.alerts1);
            this.tabAlerts.Location = new System.Drawing.Point(4, 25);
            this.tabAlerts.Name = "tabAlerts";
            this.tabAlerts.Padding = new System.Windows.Forms.Padding(3);
            this.tabAlerts.Size = new System.Drawing.Size(1631, 1246);
            this.tabAlerts.TabIndex = 20;
            this.tabAlerts.Text = "Alerts";
            this.tabAlerts.UseVisualStyleBackColor = true;
            // 
            // alerts1
            // 
            this.alerts1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alerts1.Location = new System.Drawing.Point(3, 3);
            this.alerts1.Name = "alerts1";
            this.alerts1.Size = new System.Drawing.Size(1625, 1240);
            this.alerts1.TabIndex = 0;
            this.alerts1.UseAlertName = false;
            // 
            // tabDrivers
            // 
            this.tabDrivers.Controls.Add(this.drivers1);
            this.tabDrivers.Location = new System.Drawing.Point(4, 25);
            this.tabDrivers.Name = "tabDrivers";
            this.tabDrivers.Padding = new System.Windows.Forms.Padding(3);
            this.tabDrivers.Size = new System.Drawing.Size(1631, 1246);
            this.tabDrivers.TabIndex = 21;
            this.tabDrivers.Text = "Drivers";
            this.tabDrivers.UseVisualStyleBackColor = true;
            // 
            // drivers1
            // 
            this.drivers1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drivers1.Location = new System.Drawing.Point(3, 3);
            this.drivers1.Name = "drivers1";
            this.drivers1.Size = new System.Drawing.Size(1625, 1240);
            this.drivers1.TabIndex = 0;
            // 
            // tabDBSpace
            // 
            this.tabDBSpace.Controls.Add(this.spaceTracking1);
            this.tabDBSpace.Location = new System.Drawing.Point(4, 25);
            this.tabDBSpace.Name = "tabDBSpace";
            this.tabDBSpace.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBSpace.Size = new System.Drawing.Size(1631, 1246);
            this.tabDBSpace.TabIndex = 22;
            this.tabDBSpace.Text = "DB Space";
            this.tabDBSpace.UseVisualStyleBackColor = true;
            // 
            // spaceTracking1
            // 
            this.spaceTracking1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spaceTracking1.Location = new System.Drawing.Point(3, 3);
            this.spaceTracking1.Name = "spaceTracking1";
            this.spaceTracking1.Size = new System.Drawing.Size(1625, 1240);
            this.spaceTracking1.TabIndex = 0;
            // 
            // tabAzureSummary
            // 
            this.tabAzureSummary.Controls.Add(this.azureSummary1);
            this.tabAzureSummary.Location = new System.Drawing.Point(4, 25);
            this.tabAzureSummary.Name = "tabAzureSummary";
            this.tabAzureSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabAzureSummary.Size = new System.Drawing.Size(1631, 1246);
            this.tabAzureSummary.TabIndex = 23;
            this.tabAzureSummary.Text = "Azure Summary";
            this.tabAzureSummary.UseVisualStyleBackColor = true;
            // 
            // azureSummary1
            // 
            this.azureSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureSummary1.Location = new System.Drawing.Point(3, 3);
            this.azureSummary1.Name = "azureSummary1";
            this.azureSummary1.Size = new System.Drawing.Size(1625, 1240);
            this.azureSummary1.TabIndex = 0;
            // 
            // tabAzureDB
            // 
            this.tabAzureDB.Controls.Add(this.azureDBResourceStats1);
            this.tabAzureDB.Location = new System.Drawing.Point(4, 25);
            this.tabAzureDB.Name = "tabAzureDB";
            this.tabAzureDB.Padding = new System.Windows.Forms.Padding(3);
            this.tabAzureDB.Size = new System.Drawing.Size(1631, 1246);
            this.tabAzureDB.TabIndex = 24;
            this.tabAzureDB.Text = "Azure DB";
            this.tabAzureDB.UseVisualStyleBackColor = true;
            // 
            // azureDBResourceStats1
            // 
            this.azureDBResourceStats1.DateGrouping = 0;
            this.azureDBResourceStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureDBResourceStats1.Location = new System.Drawing.Point(3, 3);
            this.azureDBResourceStats1.Name = "azureDBResourceStats1";
            this.azureDBResourceStats1.Size = new System.Drawing.Size(1625, 1240);
            this.azureDBResourceStats1.TabIndex = 0;
            // 
            // tabServiceObjectives
            // 
            this.tabServiceObjectives.Controls.Add(this.azureServiceObjectivesHistory1);
            this.tabServiceObjectives.Location = new System.Drawing.Point(4, 25);
            this.tabServiceObjectives.Name = "tabServiceObjectives";
            this.tabServiceObjectives.Padding = new System.Windows.Forms.Padding(3);
            this.tabServiceObjectives.Size = new System.Drawing.Size(1631, 1246);
            this.tabServiceObjectives.TabIndex = 25;
            this.tabServiceObjectives.Text = "Azure Service Objectives";
            this.tabServiceObjectives.UseVisualStyleBackColor = true;
            // 
            // azureServiceObjectivesHistory1
            // 
            this.azureServiceObjectivesHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.azureServiceObjectivesHistory1.Location = new System.Drawing.Point(3, 3);
            this.azureServiceObjectivesHistory1.Name = "azureServiceObjectivesHistory1";
            this.azureServiceObjectivesHistory1.Size = new System.Drawing.Size(1625, 1240);
            this.azureServiceObjectivesHistory1.TabIndex = 0;
            // 
            // tabDBConfiguration
            // 
            this.tabDBConfiguration.Controls.Add(this.dbConfiguration1);
            this.tabDBConfiguration.Location = new System.Drawing.Point(4, 25);
            this.tabDBConfiguration.Name = "tabDBConfiguration";
            this.tabDBConfiguration.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBConfiguration.Size = new System.Drawing.Size(1631, 1246);
            this.tabDBConfiguration.TabIndex = 26;
            this.tabDBConfiguration.Text = "DB Configuration";
            this.tabDBConfiguration.UseVisualStyleBackColor = true;
            // 
            // dbConfiguration1
            // 
            this.dbConfiguration1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbConfiguration1.Location = new System.Drawing.Point(3, 3);
            this.dbConfiguration1.Name = "dbConfiguration1";
            this.dbConfiguration1.Size = new System.Drawing.Size(1625, 1240);
            this.dbConfiguration1.TabIndex = 0;
            // 
            // tabDBOptions
            // 
            this.tabDBOptions.Controls.Add(this.dbOptions1);
            this.tabDBOptions.Location = new System.Drawing.Point(4, 25);
            this.tabDBOptions.Name = "tabDBOptions";
            this.tabDBOptions.Padding = new System.Windows.Forms.Padding(3);
            this.tabDBOptions.Size = new System.Drawing.Size(1631, 1246);
            this.tabDBOptions.TabIndex = 27;
            this.tabDBOptions.Text = "DB Options";
            this.tabDBOptions.UseVisualStyleBackColor = true;
            // 
            // dbOptions1
            // 
            this.dbOptions1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dbOptions1.Location = new System.Drawing.Point(3, 3);
            this.dbOptions1.Name = "dbOptions1";
            this.dbOptions1.Size = new System.Drawing.Size(1625, 1240);
            this.dbOptions1.SummaryMode = false;
            this.dbOptions1.TabIndex = 0;
            // 
            // tabTempDB
            // 
            this.tabTempDB.Controls.Add(this.tempDBConfig1);
            this.tabTempDB.Location = new System.Drawing.Point(4, 25);
            this.tabTempDB.Name = "tabTempDB";
            this.tabTempDB.Padding = new System.Windows.Forms.Padding(3);
            this.tabTempDB.Size = new System.Drawing.Size(1631, 1246);
            this.tabTempDB.TabIndex = 28;
            this.tabTempDB.Text = "TempDB";
            this.tabTempDB.UseVisualStyleBackColor = true;
            // 
            // tempDBConfig1
            // 
            this.tempDBConfig1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tempDBConfig1.Location = new System.Drawing.Point(3, 3);
            this.tempDBConfig1.Name = "tempDBConfig1";
            this.tempDBConfig1.Size = new System.Drawing.Size(1625, 1240);
            this.tempDBConfig1.TabIndex = 0;
            // 
            // tabCustomChecks
            // 
            this.tabCustomChecks.Controls.Add(this.customChecks1);
            this.tabCustomChecks.Location = new System.Drawing.Point(4, 25);
            this.tabCustomChecks.Name = "tabCustomChecks";
            this.tabCustomChecks.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomChecks.Size = new System.Drawing.Size(1631, 1246);
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
            this.customChecks1.Name = "customChecks1";
            this.customChecks1.Size = new System.Drawing.Size(1625, 1240);
            this.customChecks1.TabIndex = 0;
            this.customChecks1.Test = null;
            // 
            // tabPC
            // 
            this.tabPC.Controls.Add(this.performanceCounterSummary1);
            this.tabPC.Location = new System.Drawing.Point(4, 25);
            this.tabPC.Name = "tabPC";
            this.tabPC.Padding = new System.Windows.Forms.Padding(3);
            this.tabPC.Size = new System.Drawing.Size(1631, 1246);
            this.tabPC.TabIndex = 30;
            this.tabPC.Text = "Metrics";
            this.tabPC.UseVisualStyleBackColor = true;
            // 
            // performanceCounterSummary1
            // 
            this.performanceCounterSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceCounterSummary1.InstanceID = 0;
            this.performanceCounterSummary1.Location = new System.Drawing.Point(3, 3);
            this.performanceCounterSummary1.Name = "performanceCounterSummary1";
            this.performanceCounterSummary1.Size = new System.Drawing.Size(1625, 1240);
            this.performanceCounterSummary1.TabIndex = 0;
            // 
            // tabObjectExecutionSummary
            // 
            this.tabObjectExecutionSummary.Controls.Add(this.objectExecutionSummary1);
            this.tabObjectExecutionSummary.Location = new System.Drawing.Point(4, 25);
            this.tabObjectExecutionSummary.Name = "tabObjectExecutionSummary";
            this.tabObjectExecutionSummary.Padding = new System.Windows.Forms.Padding(3);
            this.tabObjectExecutionSummary.Size = new System.Drawing.Size(1631, 1246);
            this.tabObjectExecutionSummary.TabIndex = 31;
            this.tabObjectExecutionSummary.Text = "Object Execution";
            this.tabObjectExecutionSummary.UseVisualStyleBackColor = true;
            // 
            // objectExecutionSummary1
            // 
            this.objectExecutionSummary1.DatabaseID = 0;
            this.objectExecutionSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecutionSummary1.Instance = null;
            this.objectExecutionSummary1.InstanceID = 0;
            this.objectExecutionSummary1.Location = new System.Drawing.Point(3, 3);
            this.objectExecutionSummary1.Name = "objectExecutionSummary1";
            this.objectExecutionSummary1.ObjectID = ((long)(0));
            this.objectExecutionSummary1.Size = new System.Drawing.Size(1625, 1240);
            this.objectExecutionSummary1.TabIndex = 0;
            this.objectExecutionSummary1.Types = "";
            // 
            // tabWaits
            // 
            this.tabWaits.Controls.Add(this.waitsSummary1);
            this.tabWaits.Location = new System.Drawing.Point(4, 25);
            this.tabWaits.Name = "tabWaits";
            this.tabWaits.Padding = new System.Windows.Forms.Padding(3);
            this.tabWaits.Size = new System.Drawing.Size(1631, 1246);
            this.tabWaits.TabIndex = 32;
            this.tabWaits.Text = "Waits";
            this.tabWaits.UseVisualStyleBackColor = true;
            // 
            // waitsSummary1
            // 
            this.waitsSummary1.DateGrouping = 1;
            this.waitsSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitsSummary1.InstanceID = 0;
            this.waitsSummary1.Location = new System.Drawing.Point(3, 3);
            this.waitsSummary1.Name = "waitsSummary1";
            this.waitsSummary1.Size = new System.Drawing.Size(1625, 1240);
            this.waitsSummary1.TabIndex = 0;
            // 
            // tabMirroring
            // 
            this.tabMirroring.Controls.Add(this.mirroring1);
            this.tabMirroring.Location = new System.Drawing.Point(4, 25);
            this.tabMirroring.Name = "tabMirroring";
            this.tabMirroring.Padding = new System.Windows.Forms.Padding(3);
            this.tabMirroring.Size = new System.Drawing.Size(1631, 1246);
            this.tabMirroring.TabIndex = 33;
            this.tabMirroring.Text = "Mirroring";
            this.tabMirroring.UseVisualStyleBackColor = true;
            // 
            // mirroring1
            // 
            this.mirroring1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mirroring1.Location = new System.Drawing.Point(3, 3);
            this.mirroring1.Name = "mirroring1";
            this.mirroring1.Size = new System.Drawing.Size(1625, 1240);
            this.mirroring1.SummaryMode = true;
            this.mirroring1.TabIndex = 0;
            // 
            // tabJobDDL
            // 
            this.tabJobDDL.Controls.Add(this.jobDDLHistory1);
            this.tabJobDDL.Location = new System.Drawing.Point(4, 25);
            this.tabJobDDL.Name = "tabJobDDL";
            this.tabJobDDL.Padding = new System.Windows.Forms.Padding(3);
            this.tabJobDDL.Size = new System.Drawing.Size(1631, 1246);
            this.tabJobDDL.TabIndex = 34;
            this.tabJobDDL.Text = "Job DDL";
            this.tabJobDDL.UseVisualStyleBackColor = true;
            // 
            // jobDDLHistory1
            // 
            this.jobDDLHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobDDLHistory1.InstanceID = 0;
            this.jobDDLHistory1.JobID = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.jobDDLHistory1.Location = new System.Drawing.Point(3, 3);
            this.jobDDLHistory1.Name = "jobDDLHistory1";
            this.jobDDLHistory1.Size = new System.Drawing.Size(1625, 1240);
            this.jobDDLHistory1.TabIndex = 0;
            // 
            // tabAG
            // 
            this.tabAG.Controls.Add(this.ag1);
            this.tabAG.Location = new System.Drawing.Point(4, 25);
            this.tabAG.Name = "tabAG";
            this.tabAG.Padding = new System.Windows.Forms.Padding(3);
            this.tabAG.Size = new System.Drawing.Size(1631, 1246);
            this.tabAG.TabIndex = 35;
            this.tabAG.Text = "Availability Groups";
            this.tabAG.UseVisualStyleBackColor = true;
            // 
            // ag1
            // 
            this.ag1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ag1.Location = new System.Drawing.Point(3, 3);
            this.ag1.Name = "ag1";
            this.ag1.Size = new System.Drawing.Size(1625, 1240);
            this.ag1.TabIndex = 0;
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
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.Text = "DBA Dash";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Main_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
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
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchemaName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectType;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotValidTo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectDateCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectDateModified;
        private System.Windows.Forms.ToolStripMenuItem dBDiffToolStripMenuItem;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage tabTags;
        private System.Windows.Forms.CheckedListBox chkTags;
        private System.Windows.Forms.ToolStripMenuItem mnuTags;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboTagValue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboTagName;
        private System.Windows.Forms.Panel panel1;
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
        private System.Windows.Forms.ToolStripMenuItem tsJobDiff;
        private System.Windows.Forms.TabPage tabAG;
        private HA.AG ag1;
    }
}