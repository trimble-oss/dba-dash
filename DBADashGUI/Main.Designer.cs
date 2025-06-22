using DBADashGUI.Drives;
using DBADashGUI.Theme;

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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            TreeViewImageList = new System.Windows.Forms.ImageList(components);
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            tsConnect = new System.Windows.Forms.ToolStripMenuItem();
            diffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            databaseSchemaDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            agentJobDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureDisplayNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            externalDiffToolToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dataRetentionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            desktopNotificationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            freezeKeyColumnsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            manageInstancesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            repoSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dateTimeFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setTimeFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setDateTimeFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveTimeZonePreferenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setAutoRefreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            secondsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minuteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minutesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minutesToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            customToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showHiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            themeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            defaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            darkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            whiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsAlert = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDateRange = new DateRangeToolStripMenuItem();
            tsTimeFilter = new System.Windows.Forms.ToolStripMenuItem();
            tsDayOfWeek = new System.Windows.Forms.ToolStripMenuItem();
            cboTimeZone = new System.Windows.Forms.ToolStripComboBox();
            splitMain = new System.Windows.Forms.SplitContainer();
            tv1 = new System.Windows.Forms.TreeView();
            pnlSearch = new System.Windows.Forms.Panel();
            searchLayout = new System.Windows.Forms.TableLayoutPanel();
            txtSearch = new System.Windows.Forms.TextBox();
            bttnSearch = new System.Windows.Forms.Button();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsHome = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            mnuTags = new System.Windows.Forms.ToolStripDropDownButton();
            groupToolStripMenuItem = new System.Windows.Forms.ToolStripDropDownButton();
            tabs = new ThemedTabControl();
            tabSnapshotsSummary = new System.Windows.Forms.TabPage();
            schemaSnapshots1 = new DBADashGUI.Changes.SchemaSnapshots();
            tabSchema = new System.Windows.Forms.TabPage();
            splitSchemaSnapshot = new System.Windows.Forms.SplitContainer();
            label7 = new System.Windows.Forms.Label();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsPrevious = new System.Windows.Forms.ToolStripButton();
            tsPageNum = new System.Windows.Forms.ToolStripLabel();
            tsNext = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsPageSize = new System.Windows.Forms.ToolStripComboBox();
            gvHistory = new System.Windows.Forms.DataGridView();
            ObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ObjectType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ObjectDateCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ObjectDateModified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCompare = new System.Windows.Forms.DataGridViewLinkColumn();
            label1 = new System.Windows.Forms.Label();
            tabTags = new System.Windows.Forms.TabPage();
            tags1 = new DBADashGUI.Tagging.Tags();
            tabDrives = new System.Windows.Forms.TabPage();
            drivesControl1 = new DrivesControl();
            tabBackups = new System.Windows.Forms.TabPage();
            backupsControl1 = new DBADashGUI.Backups.BackupsControl();
            tabLogShipping = new System.Windows.Forms.TabPage();
            logShippingControl1 = new DBADashGUI.LogShipping.LogShippingControl();
            tabJobs = new System.Windows.Forms.TabPage();
            agentJobsControl1 = new DBADashGUI.AgentJobs.AgentJobsControl();
            tabSummary = new System.Windows.Forms.TabPage();
            summary1 = new Summary();
            tabFiles = new System.Windows.Forms.TabPage();
            dbFilesControl1 = new DBADashGUI.DBFiles.DBFilesControl();
            tabLastGood = new System.Windows.Forms.TabPage();
            lastGoodCheckDBControl1 = new DBADashGUI.LastGoodCheckDB.LastGoodCheckDBControl();
            tabPerformance = new System.Windows.Forms.TabPage();
            performance1 = new DBADashGUI.Performance.Performance();
            tabDBADashErrorLog = new System.Windows.Forms.TabPage();
            collectionErrors1 = new DBADashGUI.CollectionDates.CollectionErrors();
            tabCollectionDates = new System.Windows.Forms.TabPage();
            collectionDates1 = new DBADashGUI.CollectionDates.CollectionDates();
            tabPerformanceSummary = new System.Windows.Forms.TabPage();
            performanceSummary1 = new DBADashGUI.Performance.PerformanceSummary();
            tabInfo = new System.Windows.Forms.TabPage();
            info1 = new Info();
            tabHardware = new System.Windows.Forms.TabPage();
            hardwareChanges1 = new HardwareChanges();
            tabSQLPatching = new System.Windows.Forms.TabPage();
            sqlPatching1 = new SQLPatching();
            tabInstanceConfig = new System.Windows.Forms.TabPage();
            configurationHistory1 = new ConfigurationHistory();
            tabSlowQueries = new System.Windows.Forms.TabPage();
            slowQueries1 = new SlowQueries();
            tabTraceFlags = new System.Windows.Forms.TabPage();
            traceFlagHistory1 = new DBADashGUI.Changes.TraceFlagHistory();
            tabSQLAgentAlerts = new System.Windows.Forms.TabPage();
            alerts1 = new DBADashGUI.Changes.SQLAgentAlerts();
            tabDrivers = new System.Windows.Forms.TabPage();
            drivers1 = new DBADashGUI.Changes.Drivers();
            tabDBSpace = new System.Windows.Forms.TabPage();
            spaceTracking1 = new SpaceTracking();
            tabAzureSummary = new System.Windows.Forms.TabPage();
            azureSummary1 = new DBADashGUI.Performance.AzureSummary();
            tabAzureDB = new System.Windows.Forms.TabPage();
            azureDBResourceStats1 = new DBADashGUI.Performance.AzureDBResourceStats();
            tabServiceObjectives = new System.Windows.Forms.TabPage();
            azureServiceObjectivesHistory1 = new DBADashGUI.Changes.AzureServiceObjectivesHistory();
            tabDBConfiguration = new System.Windows.Forms.TabPage();
            dbConfiguration1 = new DBADashGUI.Changes.DBConfiguration();
            tabDBOptions = new System.Windows.Forms.TabPage();
            dbOptions1 = new DBADashGUI.Changes.DBOptions();
            tabTempDB = new System.Windows.Forms.TabPage();
            tempDBConfig1 = new DBADashGUI.DBFiles.TempDBConfig();
            tabCustomChecks = new System.Windows.Forms.TabPage();
            customChecks1 = new DBADashGUI.Checks.CustomChecks();
            tabPC = new System.Windows.Forms.TabPage();
            performanceCounterSummary1 = new DBADashGUI.Performance.PerformanceCounterSummary();
            tabObjectExecutionSummary = new System.Windows.Forms.TabPage();
            objectExecutionSummary1 = new DBADashGUI.Performance.ObjectExecutionSummary();
            tabWaits = new System.Windows.Forms.TabPage();
            waitsSummary1 = new DBADashGUI.Performance.WaitsSummary();
            tabMirroring = new System.Windows.Forms.TabPage();
            mirroring1 = new DBADashGUI.HA.Mirroring();
            tabJobDDL = new System.Windows.Forms.TabPage();
            jobDDLHistory1 = new DBADashGUI.Changes.JobDDLHistory();
            tabAG = new System.Windows.Forms.TabPage();
            ag1 = new DBADashGUI.HA.AG();
            tabQS = new System.Windows.Forms.TabPage();
            queryStore1 = new DBADashGUI.Changes.QueryStore();
            tabRG = new System.Windows.Forms.TabPage();
            resourceGovernor1 = new DBADashGUI.Changes.ResourceGovernor();
            tabAzureDBesourceGovernance = new System.Windows.Forms.TabPage();
            azureDBResourceGovernance1 = new DBADashGUI.Changes.AzureDBResourceGovernance();
            tabRunningQueries = new System.Windows.Forms.TabPage();
            runningQueries1 = new DBADashGUI.Performance.RunningQueries();
            tabMemory = new System.Windows.Forms.TabPage();
            memoryUsage1 = new DBADashGUI.Performance.MemoryUsage();
            tabJobStats = new System.Windows.Forms.TabPage();
            jobStats1 = new DBADashGUI.AgentJobs.JobStats();
            tabDBADash = new System.Windows.Forms.TabPage();
            lblVersion = new System.Windows.Forms.Label();
            lblSQLMonitoring = new System.Windows.Forms.Label();
            lblDBADash = new System.Windows.Forms.Label();
            tabIdentityColumns = new System.Windows.Forms.TabPage();
            identityColumns1 = new DBADashGUI.Checks.IdentityColumns();
            tabOSLoadedModules = new System.Windows.Forms.TabPage();
            osLoadedModules1 = new DBADashGUI.Checks.OSLoadedModules();
            tabJobTimeline = new System.Windows.Forms.TabPage();
            jobTimeline1 = new DBADashGUI.AgentJobs.JobTimeline();
            tabDrivePerformance = new System.Windows.Forms.TabPage();
            drivePerformance1 = new DBADashGUI.Performance.DrivePerformance();
            tabRunningJobs = new System.Windows.Forms.TabPage();
            runningJobs1 = new DBADashGUI.AgentJobs.RunningJobs();
            tabCustomReport = new System.Windows.Forms.TabPage();
            customReportView1 = new DBADashGUI.CustomReports.CustomReportView();
            tabTableSize = new System.Windows.Forms.TabPage();
            tableSize1 = new DBADashGUI.DBFiles.TableSize();
            tabTopQueries = new System.Windows.Forms.TabPage();
            queryStoreTop = new DBADashGUI.Performance.QueryStoreTopQueries();
            tabQueryStoreForcedPlans = new System.Windows.Forms.TabPage();
            queryStoreForcedPlans1 = new DBADashGUI.Performance.QueryStoreForcedPlans();
            tabServerServices = new System.Windows.Forms.TabPage();
            serverServices1 = new DBADashGUI.CustomReports.ServerServices();
            tabDeletedInstances = new System.Windows.Forms.TabPage();
            deletedInstances1 = new DeletedInstances();
            refresh1 = new Refresh();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            autoRefreshTimer = new System.Windows.Forms.Timer(components);
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            pnlSearch.SuspendLayout();
            searchLayout.SuspendLayout();
            toolStrip2.SuspendLayout();
            tabs.SuspendLayout();
            tabSnapshotsSummary.SuspendLayout();
            tabSchema.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitSchemaSnapshot).BeginInit();
            splitSchemaSnapshot.Panel1.SuspendLayout();
            splitSchemaSnapshot.Panel2.SuspendLayout();
            splitSchemaSnapshot.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gvHistory).BeginInit();
            tabTags.SuspendLayout();
            tabDrives.SuspendLayout();
            tabBackups.SuspendLayout();
            tabLogShipping.SuspendLayout();
            tabJobs.SuspendLayout();
            tabSummary.SuspendLayout();
            tabFiles.SuspendLayout();
            tabLastGood.SuspendLayout();
            tabPerformance.SuspendLayout();
            tabDBADashErrorLog.SuspendLayout();
            tabCollectionDates.SuspendLayout();
            tabPerformanceSummary.SuspendLayout();
            tabInfo.SuspendLayout();
            tabHardware.SuspendLayout();
            tabSQLPatching.SuspendLayout();
            tabInstanceConfig.SuspendLayout();
            tabSlowQueries.SuspendLayout();
            tabTraceFlags.SuspendLayout();
            tabSQLAgentAlerts.SuspendLayout();
            tabDrivers.SuspendLayout();
            tabDBSpace.SuspendLayout();
            tabAzureSummary.SuspendLayout();
            tabAzureDB.SuspendLayout();
            tabServiceObjectives.SuspendLayout();
            tabDBConfiguration.SuspendLayout();
            tabDBOptions.SuspendLayout();
            tabTempDB.SuspendLayout();
            tabCustomChecks.SuspendLayout();
            tabPC.SuspendLayout();
            tabObjectExecutionSummary.SuspendLayout();
            tabWaits.SuspendLayout();
            tabMirroring.SuspendLayout();
            tabJobDDL.SuspendLayout();
            tabAG.SuspendLayout();
            tabQS.SuspendLayout();
            tabRG.SuspendLayout();
            tabAzureDBesourceGovernance.SuspendLayout();
            tabRunningQueries.SuspendLayout();
            tabMemory.SuspendLayout();
            tabJobStats.SuspendLayout();
            tabDBADash.SuspendLayout();
            tabIdentityColumns.SuspendLayout();
            tabOSLoadedModules.SuspendLayout();
            tabJobTimeline.SuspendLayout();
            tabDrivePerformance.SuspendLayout();
            tabRunningJobs.SuspendLayout();
            tabCustomReport.SuspendLayout();
            tabTableSize.SuspendLayout();
            tabTopQueries.SuspendLayout();
            tabQueryStoreForcedPlans.SuspendLayout();
            tabServerServices.SuspendLayout();
            tabDeletedInstances.SuspendLayout();
            SuspendLayout();
            // 
            // TreeViewImageList
            // 
            TreeViewImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            TreeViewImageList.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("TreeViewImageList.ImageStream");
            TreeViewImageList.TransparentColor = System.Drawing.Color.Transparent;
            TreeViewImageList.Images.SetKeyName(0, "ServerProject_16x.png");
            TreeViewImageList.Images.SetKeyName(1, "DataServer_16x.png");
            TreeViewImageList.Images.SetKeyName(2, "Database_16x.png");
            TreeViewImageList.Images.SetKeyName(3, "FolderClosed_16x.png");
            TreeViewImageList.Images.SetKeyName(4, "Table_16x.png");
            TreeViewImageList.Images.SetKeyName(5, "StoredProcedureScript_16x.png");
            TreeViewImageList.Images.SetKeyName(6, "FilterFolderOpened_16x.png");
            TreeViewImageList.Images.SetKeyName(7, "DatabaseProperty_16x.png");
            TreeViewImageList.Images.SetKeyName(8, "Cloud_blue_whiteHalo_16x.png");
            TreeViewImageList.Images.SetKeyName(9, "CloudDatabase_16x.png");
            TreeViewImageList.Images.SetKeyName(10, "Checklist_16x.png");
            TreeViewImageList.Images.SetKeyName(11, "Tag_16x.png");
            TreeViewImageList.Images.SetKeyName(12, "BatchFile_16x.png");
            TreeViewImageList.Images.SetKeyName(13, "Cube_16x.png");
            TreeViewImageList.Images.SetKeyName(14, "DataSourceTarget_16x.png");
            TreeViewImageList.Images.SetKeyName(15, "PowerShellFile_16x.png");
            TreeViewImageList.Images.SetKeyName(16, "SQLFile_16x.png");
            TreeViewImageList.Images.SetKeyName(17, "ProcedureMissing_16x.png");
            TreeViewImageList.Images.SetKeyName(18, "CloudServer_16x.png");
            TreeViewImageList.Images.SetKeyName(19, "DataServer_16x_BWLight.png");
            TreeViewImageList.Images.SetKeyName(20, "FolderClosedBlue_16x.png");
            TreeViewImageList.Images.SetKeyName(21, "");
            TreeViewImageList.Images.SetKeyName(22, "MonthCalendar_16x.png");
            TreeViewImageList.Images.SetKeyName(23, "LogicalDataCenterDiagram.ico");
            TreeViewImageList.Images.SetKeyName(24, "System_Report_16x.png");
            TreeViewImageList.Images.SetKeyName(25, "FileDialogReport_16x.png");
            TreeViewImageList.Images.SetKeyName(26, "DatabaseGroup.png");
            TreeViewImageList.Images.SetKeyName(27, "User_Report_16x.png");
            TreeViewImageList.Images.SetKeyName(28, "Report_16x.png");
            TreeViewImageList.Images.SetKeyName(29, "VSO_TeamProjectRepositoryFolder_hoverblue_16x.png");
            TreeViewImageList.Images.SetKeyName(30, "VSO_TeamProject_16x.png");
            // 
            // menuStrip1
            // 
            menuStrip1.GripMargin = new System.Windows.Forms.Padding(2);
            menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsConnect, diffToolStripMenuItem, optionsToolStripMenuItem, tsAlert, helpToolStripMenuItem, tsDateRange, tsTimeFilter, tsDayOfWeek, cboTimeZone });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new System.Windows.Forms.Padding(3, 1, 0, 1);
            menuStrip1.Size = new System.Drawing.Size(1955, 30);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // tsConnect
            // 
            tsConnect.Image = Properties.Resources.ConnectToDatabase_16x;
            tsConnect.Name = "tsConnect";
            tsConnect.Size = new System.Drawing.Size(97, 28);
            tsConnect.Text = "Connect";
            tsConnect.ToolTipText = "Connect to a different DBA Dash repository";
            // 
            // diffToolStripMenuItem
            // 
            diffToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { databaseSchemaDiffToolStripMenuItem, agentJobDiffToolStripMenuItem });
            diffToolStripMenuItem.Enabled = false;
            diffToolStripMenuItem.Image = Properties.Resources.Diff_16x;
            diffToolStripMenuItem.Name = "diffToolStripMenuItem";
            diffToolStripMenuItem.Size = new System.Drawing.Size(68, 28);
            diffToolStripMenuItem.Text = "Diff";
            // 
            // databaseSchemaDiffToolStripMenuItem
            // 
            databaseSchemaDiffToolStripMenuItem.Name = "databaseSchemaDiffToolStripMenuItem";
            databaseSchemaDiffToolStripMenuItem.Size = new System.Drawing.Size(240, 26);
            databaseSchemaDiffToolStripMenuItem.Text = "Database Schema Diff";
            databaseSchemaDiffToolStripMenuItem.Click += DatabaseSchemaDiffToolStripMenuItem_Click;
            // 
            // agentJobDiffToolStripMenuItem
            // 
            agentJobDiffToolStripMenuItem.Name = "agentJobDiffToolStripMenuItem";
            agentJobDiffToolStripMenuItem.Size = new System.Drawing.Size(240, 26);
            agentJobDiffToolStripMenuItem.Text = "Agent Job Diff";
            agentJobDiffToolStripMenuItem.Click += AgentJobDiffToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureDisplayNameToolStripMenuItem, externalDiffToolToolStripMenuItem, dataRetentionToolStripMenuItem, desktopNotificationsToolStripMenuItem, freezeKeyColumnsToolStripMenuItem, manageInstancesToolStripMenuItem, repoSettingsToolStripMenuItem, dateTimeFormatToolStripMenuItem, saveTimeZonePreferenceToolStripMenuItem, setAutoRefreshToolStripMenuItem, showHiddenToolStripMenuItem, themeToolStripMenuItem });
            optionsToolStripMenuItem.Enabled = false;
            optionsToolStripMenuItem.Image = Properties.Resources.SettingsOutline_16x;
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new System.Drawing.Size(95, 28);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // configureDisplayNameToolStripMenuItem
            // 
            configureDisplayNameToolStripMenuItem.Name = "configureDisplayNameToolStripMenuItem";
            configureDisplayNameToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            configureDisplayNameToolStripMenuItem.Text = "Configure Display Name";
            configureDisplayNameToolStripMenuItem.Click += ConfigureDisplayNameToolStripMenuItem_Click;
            // 
            // externalDiffToolToolStripMenuItem
            // 
            externalDiffToolToolStripMenuItem.Image = Properties.Resources.Diff_16x;
            externalDiffToolToolStripMenuItem.Name = "externalDiffToolToolStripMenuItem";
            externalDiffToolToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            externalDiffToolToolStripMenuItem.Text = "Configure External Diff Tool";
            externalDiffToolToolStripMenuItem.Click += externalDiffToolToolStripMenuItem_Click;
            // 
            // dataRetentionToolStripMenuItem
            // 
            dataRetentionToolStripMenuItem.Name = "dataRetentionToolStripMenuItem";
            dataRetentionToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            dataRetentionToolStripMenuItem.Text = "Data Retention";
            dataRetentionToolStripMenuItem.Click += DataRetentionToolStripMenuItem_Click;
            // 
            // desktopNotificationsToolStripMenuItem
            // 
            desktopNotificationsToolStripMenuItem.CheckOnClick = true;
            desktopNotificationsToolStripMenuItem.Name = "desktopNotificationsToolStripMenuItem";
            desktopNotificationsToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            desktopNotificationsToolStripMenuItem.Text = "Desktop Notifications";
            desktopNotificationsToolStripMenuItem.Click += DesktopNotificationsToolStripMenuItem_Click;
            // 
            // freezeKeyColumnsToolStripMenuItem
            // 
            freezeKeyColumnsToolStripMenuItem.Checked = true;
            freezeKeyColumnsToolStripMenuItem.CheckOnClick = true;
            freezeKeyColumnsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            freezeKeyColumnsToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            freezeKeyColumnsToolStripMenuItem.Name = "freezeKeyColumnsToolStripMenuItem";
            freezeKeyColumnsToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            freezeKeyColumnsToolStripMenuItem.Text = "Freeze Key Columns";
            freezeKeyColumnsToolStripMenuItem.ToolTipText = "Keep the key column(s) in the grid visible as you scroll to the right";
            freezeKeyColumnsToolStripMenuItem.Click += FreezeKeyColumnsToolStripMenuItem_Click;
            // 
            // manageInstancesToolStripMenuItem
            // 
            manageInstancesToolStripMenuItem.Name = "manageInstancesToolStripMenuItem";
            manageInstancesToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            manageInstancesToolStripMenuItem.Text = "Manage Instances";
            manageInstancesToolStripMenuItem.Click += ManageInstancesToolStripMenuItem_Click;
            // 
            // repoSettingsToolStripMenuItem
            // 
            repoSettingsToolStripMenuItem.Image = Properties.Resources.DatabaseSettings_16x;
            repoSettingsToolStripMenuItem.Name = "repoSettingsToolStripMenuItem";
            repoSettingsToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            repoSettingsToolStripMenuItem.Text = "Repository Settings";
            repoSettingsToolStripMenuItem.Click += RepositorySettings_Click;
            // 
            // dateTimeFormatToolStripMenuItem
            // 
            dateTimeFormatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { setTimeFormatToolStripMenuItem, setDateTimeFormatToolStripMenuItem });
            dateTimeFormatToolStripMenuItem.Name = "dateTimeFormatToolStripMenuItem";
            dateTimeFormatToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            dateTimeFormatToolStripMenuItem.Text = "Date/Time Format";
            // 
            // setTimeFormatToolStripMenuItem
            // 
            setTimeFormatToolStripMenuItem.Name = "setTimeFormatToolStripMenuItem";
            setTimeFormatToolStripMenuItem.Size = new System.Drawing.Size(253, 26);
            setTimeFormatToolStripMenuItem.Text = "Set Time Format";
            setTimeFormatToolStripMenuItem.Click += setTimeFormatToolStripMenuItem_Click;
            // 
            // setDateTimeFormatToolStripMenuItem
            // 
            setDateTimeFormatToolStripMenuItem.Name = "setDateTimeFormatToolStripMenuItem";
            setDateTimeFormatToolStripMenuItem.Size = new System.Drawing.Size(253, 26);
            setDateTimeFormatToolStripMenuItem.Text = "Set Date && Time Format";
            setDateTimeFormatToolStripMenuItem.Click += setDateTimeFormatToolStripMenuItem_Click;
            // 
            // saveTimeZonePreferenceToolStripMenuItem
            // 
            saveTimeZonePreferenceToolStripMenuItem.Image = Properties.Resources.Save_16x;
            saveTimeZonePreferenceToolStripMenuItem.Name = "saveTimeZonePreferenceToolStripMenuItem";
            saveTimeZonePreferenceToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            saveTimeZonePreferenceToolStripMenuItem.Text = "Save time zone preference";
            saveTimeZonePreferenceToolStripMenuItem.Click += SaveTimeZonePreferenceToolStripMenuItem_Click;
            // 
            // setAutoRefreshToolStripMenuItem
            // 
            setAutoRefreshToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { secondsToolStripMenuItem, minuteToolStripMenuItem, minutesToolStripMenuItem, minutesToolStripMenuItem1, customToolStripMenuItem, noneToolStripMenuItem });
            setAutoRefreshToolStripMenuItem.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            setAutoRefreshToolStripMenuItem.Name = "setAutoRefreshToolStripMenuItem";
            setAutoRefreshToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            setAutoRefreshToolStripMenuItem.Text = "Set Auto Refresh";
            // 
            // secondsToolStripMenuItem
            // 
            secondsToolStripMenuItem.Name = "secondsToolStripMenuItem";
            secondsToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            secondsToolStripMenuItem.Tag = "30";
            secondsToolStripMenuItem.Text = "30 seconds";
            secondsToolStripMenuItem.Click += SetAutoRefresh;
            // 
            // minuteToolStripMenuItem
            // 
            minuteToolStripMenuItem.Name = "minuteToolStripMenuItem";
            minuteToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            minuteToolStripMenuItem.Tag = "60";
            minuteToolStripMenuItem.Text = "1 minute";
            minuteToolStripMenuItem.Click += SetAutoRefresh;
            // 
            // minutesToolStripMenuItem
            // 
            minutesToolStripMenuItem.Name = "minutesToolStripMenuItem";
            minutesToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            minutesToolStripMenuItem.Tag = "120";
            minutesToolStripMenuItem.Text = "2 minutes";
            minutesToolStripMenuItem.Click += SetAutoRefresh;
            // 
            // minutesToolStripMenuItem1
            // 
            minutesToolStripMenuItem1.Name = "minutesToolStripMenuItem1";
            minutesToolStripMenuItem1.Size = new System.Drawing.Size(165, 26);
            minutesToolStripMenuItem1.Tag = "300";
            minutesToolStripMenuItem1.Text = "5 minutes";
            minutesToolStripMenuItem1.Click += SetAutoRefresh;
            // 
            // customToolStripMenuItem
            // 
            customToolStripMenuItem.Name = "customToolStripMenuItem";
            customToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            customToolStripMenuItem.Tag = "0";
            customToolStripMenuItem.Text = "Custom";
            customToolStripMenuItem.Click += SetAutoRefresh;
            // 
            // noneToolStripMenuItem
            // 
            noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            noneToolStripMenuItem.Size = new System.Drawing.Size(165, 26);
            noneToolStripMenuItem.Tag = "-1";
            noneToolStripMenuItem.Text = "None";
            noneToolStripMenuItem.Click += SetAutoRefresh;
            // 
            // showHiddenToolStripMenuItem
            // 
            showHiddenToolStripMenuItem.CheckOnClick = true;
            showHiddenToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            showHiddenToolStripMenuItem.Name = "showHiddenToolStripMenuItem";
            showHiddenToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            showHiddenToolStripMenuItem.Text = "Show Hidden";
            showHiddenToolStripMenuItem.CheckStateChanged += ShowHidden_Changed;
            // 
            // themeToolStripMenuItem
            // 
            themeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { defaultToolStripMenuItem, darkToolStripMenuItem, whiteToolStripMenuItem });
            themeToolStripMenuItem.Image = Properties.Resources.ColorPalette;
            themeToolStripMenuItem.Name = "themeToolStripMenuItem";
            themeToolStripMenuItem.Size = new System.Drawing.Size(276, 26);
            themeToolStripMenuItem.Text = "Theme";
            // 
            // defaultToolStripMenuItem
            // 
            defaultToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            defaultToolStripMenuItem.Name = "defaultToolStripMenuItem";
            defaultToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            defaultToolStripMenuItem.Text = "Default";
            defaultToolStripMenuItem.Click += DefaultToolStripMenuItem_Click;
            // 
            // darkToolStripMenuItem
            // 
            darkToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            darkToolStripMenuItem.Name = "darkToolStripMenuItem";
            darkToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            darkToolStripMenuItem.Text = "Dark";
            darkToolStripMenuItem.Click += DarkToolStripMenuItem_Click;
            // 
            // whiteToolStripMenuItem
            // 
            whiteToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            whiteToolStripMenuItem.Name = "whiteToolStripMenuItem";
            whiteToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            whiteToolStripMenuItem.Text = "White";
            whiteToolStripMenuItem.Click += LightToolStripMenuItem_Click;
            // 
            // tsAlert
            // 
            tsAlert.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsAlert.Image = Properties.Resources.Alert_OK;
            tsAlert.Name = "tsAlert";
            tsAlert.Size = new System.Drawing.Size(51, 28);
            tsAlert.Text = "0";
            tsAlert.Click += TsAlert_Click;
            tsAlert.MouseHover += ShowAlertToolTip;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Image = Properties.Resources.Information_blue_6227_16x16;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(75, 28);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItem_Click;
            // 
            // tsDateRange
            // 
            tsDateRange.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDateRange.DefaultTimeSpan = System.TimeSpan.Parse("01:00:00");
            tsDateRange.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            tsDateRange.Font = new System.Drawing.Font("Segoe UI", 9F);
            tsDateRange.Image = (System.Drawing.Image)resources.GetObject("tsDateRange.Image");
            tsDateRange.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateRange.MaximumTimeSpan = System.TimeSpan.Parse("10675199.02:48:05.4775807");
            tsDateRange.MinimumTimeSpan = System.TimeSpan.Parse("-10675199.02:48:05.4775808");
            tsDateRange.Name = "tsDateRange";
            tsDateRange.SelectedTimeSpan = System.TimeSpan.Parse("01:00:00");
            tsDateRange.Size = new System.Drawing.Size(71, 25);
            tsDateRange.Text = "1 Hr";
            tsDateRange.DateRangeChanged += DateRangeChanged;
            // 
            // tsTimeFilter
            // 
            tsTimeFilter.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTimeFilter.Image = Properties.Resources.Filter_16x;
            tsTimeFilter.Name = "tsTimeFilter";
            tsTimeFilter.Size = new System.Drawing.Size(124, 28);
            tsTimeFilter.Text = "Time of Day";
            tsTimeFilter.Visible = false;
            tsTimeFilter.Click += TsTimeFilter_Click;
            // 
            // tsDayOfWeek
            // 
            tsDayOfWeek.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDayOfWeek.Image = Properties.Resources.Filter_16x;
            tsDayOfWeek.Name = "tsDayOfWeek";
            tsDayOfWeek.Size = new System.Drawing.Size(127, 28);
            tsDayOfWeek.Text = "Day of Week";
            tsDayOfWeek.Visible = false;
            tsDayOfWeek.Click += TsDayOfWeek_Click;
            // 
            // cboTimeZone
            // 
            cboTimeZone.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            cboTimeZone.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            cboTimeZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboTimeZone.DropDownWidth = 350;
            cboTimeZone.ForeColor = System.Drawing.Color.White;
            cboTimeZone.Name = "cboTimeZone";
            cboTimeZone.Size = new System.Drawing.Size(221, 28);
            cboTimeZone.SelectedIndexChanged += TimeZone_Selected;
            // 
            // splitMain
            // 
            splitMain.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            splitMain.Location = new System.Drawing.Point(0, 30);
            splitMain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(tv1);
            splitMain.Panel1.Controls.Add(pnlSearch);
            splitMain.Panel1.Controls.Add(toolStrip2);
            splitMain.Panel1MinSize = 50;
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(tabs);
            splitMain.Panel2.Controls.Add(refresh1);
            splitMain.Panel2MinSize = 100;
            splitMain.Size = new System.Drawing.Size(1955, 998);
            splitMain.SplitterDistance = 365;
            splitMain.TabIndex = 3;
            // 
            // tv1
            // 
            tv1.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            tv1.Dock = System.Windows.Forms.DockStyle.Fill;
            tv1.ImageIndex = 0;
            tv1.ImageList = TreeViewImageList;
            tv1.Location = new System.Drawing.Point(0, 27);
            tv1.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            tv1.Name = "tv1";
            tv1.SelectedImageIndex = 0;
            tv1.Size = new System.Drawing.Size(365, 921);
            tv1.TabIndex = 0;
            tv1.BeforeExpand += Tv1_BeforeExpand;
            tv1.BeforeSelect += Tv1_BeforeSelect;
            tv1.AfterSelect += Tv1_AfterSelect;
            // 
            // pnlSearch
            // 
            pnlSearch.AutoSize = true;
            pnlSearch.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            pnlSearch.Controls.Add(searchLayout);
            pnlSearch.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            pnlSearch.Location = new System.Drawing.Point(0, 948);
            pnlSearch.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new System.Drawing.Size(365, 50);
            pnlSearch.TabIndex = 1;
            // 
            // searchLayout
            // 
            searchLayout.AutoSize = true;
            searchLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            searchLayout.ColumnCount = 2;
            searchLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            searchLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            searchLayout.Controls.Add(txtSearch, 0, 0);
            searchLayout.Controls.Add(bttnSearch, 1, 0);
            searchLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            searchLayout.Location = new System.Drawing.Point(0, 0);
            searchLayout.Name = "searchLayout";
            searchLayout.RowCount = 1;
            searchLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            searchLayout.Size = new System.Drawing.Size(365, 50);
            searchLayout.TabIndex = 3;
            // 
            // txtSearch
            // 
            txtSearch.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtSearch.Location = new System.Drawing.Point(10, 8);
            txtSearch.Margin = new System.Windows.Forms.Padding(10, 8, 10, 8);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(236, 27);
            txtSearch.TabIndex = 0;
            txtSearch.KeyUp += TxtSearch_KeyUp;
            // 
            // bttnSearch
            // 
            bttnSearch.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnSearch.AutoSize = true;
            bttnSearch.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            bttnSearch.Enabled = false;
            bttnSearch.ForeColor = System.Drawing.Color.Black;
            bttnSearch.Location = new System.Drawing.Point(259, 8);
            bttnSearch.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            bttnSearch.Name = "bttnSearch";
            bttnSearch.Padding = new System.Windows.Forms.Padding(20, 2, 20, 2);
            bttnSearch.Size = new System.Drawing.Size(103, 34);
            bttnSearch.TabIndex = 2;
            bttnSearch.Text = "Search";
            bttnSearch.UseVisualStyleBackColor = true;
            bttnSearch.Click += BttnSearch_Click;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsHome, tsBack, mnuTags, groupToolStripMenuItem });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip2.Size = new System.Drawing.Size(365, 27);
            toolStrip2.TabIndex = 2;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsHome
            // 
            tsHome.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsHome.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHome.Image = Properties.Resources.HomeHS;
            tsHome.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHome.Name = "tsHome";
            tsHome.Size = new System.Drawing.Size(29, 24);
            tsHome.Text = "Home";
            tsHome.ToolTipText = "Go to root level summary";
            tsHome.Click += TsHome_Click;
            // 
            // tsBack
            // 
            tsBack.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.ToolTipText = "Move back to the previous context";
            tsBack.Click += TsBack_Click;
            // 
            // mnuTags
            // 
            mnuTags.Image = Properties.Resources.FilterDropdown_16x;
            mnuTags.ImageTransparentColor = System.Drawing.Color.Magenta;
            mnuTags.Name = "mnuTags";
            mnuTags.Size = new System.Drawing.Size(76, 24);
            mnuTags.Text = "Filter";
            mnuTags.ToolTipText = "Filter instances by tag";
            // 
            // groupToolStripMenuItem
            // 
            groupToolStripMenuItem.Image = Properties.Resources.GroupBy_16x;
            groupToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            groupToolStripMenuItem.Name = "groupToolStripMenuItem";
            groupToolStripMenuItem.Size = new System.Drawing.Size(84, 24);
            groupToolStripMenuItem.Text = "Group";
            groupToolStripMenuItem.ToolTipText = "Group instances by tag";
            // 
            // tabs
            // 
            tabs.Controls.Add(tabSnapshotsSummary);
            tabs.Controls.Add(tabSchema);
            tabs.Controls.Add(tabTags);
            tabs.Controls.Add(tabDrives);
            tabs.Controls.Add(tabBackups);
            tabs.Controls.Add(tabLogShipping);
            tabs.Controls.Add(tabJobs);
            tabs.Controls.Add(tabSummary);
            tabs.Controls.Add(tabFiles);
            tabs.Controls.Add(tabLastGood);
            tabs.Controls.Add(tabPerformance);
            tabs.Controls.Add(tabDBADashErrorLog);
            tabs.Controls.Add(tabCollectionDates);
            tabs.Controls.Add(tabPerformanceSummary);
            tabs.Controls.Add(tabInfo);
            tabs.Controls.Add(tabHardware);
            tabs.Controls.Add(tabSQLPatching);
            tabs.Controls.Add(tabInstanceConfig);
            tabs.Controls.Add(tabSlowQueries);
            tabs.Controls.Add(tabTraceFlags);
            tabs.Controls.Add(tabSQLAgentAlerts);
            tabs.Controls.Add(tabDrivers);
            tabs.Controls.Add(tabDBSpace);
            tabs.Controls.Add(tabAzureSummary);
            tabs.Controls.Add(tabAzureDB);
            tabs.Controls.Add(tabServiceObjectives);
            tabs.Controls.Add(tabDBConfiguration);
            tabs.Controls.Add(tabDBOptions);
            tabs.Controls.Add(tabTempDB);
            tabs.Controls.Add(tabCustomChecks);
            tabs.Controls.Add(tabPC);
            tabs.Controls.Add(tabObjectExecutionSummary);
            tabs.Controls.Add(tabWaits);
            tabs.Controls.Add(tabMirroring);
            tabs.Controls.Add(tabJobDDL);
            tabs.Controls.Add(tabAG);
            tabs.Controls.Add(tabQS);
            tabs.Controls.Add(tabRG);
            tabs.Controls.Add(tabAzureDBesourceGovernance);
            tabs.Controls.Add(tabRunningQueries);
            tabs.Controls.Add(tabMemory);
            tabs.Controls.Add(tabJobStats);
            tabs.Controls.Add(tabDBADash);
            tabs.Controls.Add(tabIdentityColumns);
            tabs.Controls.Add(tabOSLoadedModules);
            tabs.Controls.Add(tabJobTimeline);
            tabs.Controls.Add(tabDrivePerformance);
            tabs.Controls.Add(tabRunningJobs);
            tabs.Controls.Add(tabCustomReport);
            tabs.Controls.Add(tabTableSize);
            tabs.Controls.Add(tabTopQueries);
            tabs.Controls.Add(tabQueryStoreForcedPlans);
            tabs.Controls.Add(tabServerServices);
            tabs.Controls.Add(tabDeletedInstances);
            tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tabs.Location = new System.Drawing.Point(0, 0);
            tabs.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            tabs.Name = "tabs";
            tabs.Padding = new System.Drawing.Point(20, 8);
            tabs.SelectedIndex = 0;
            tabs.Size = new System.Drawing.Size(1586, 998);
            tabs.TabIndex = 0;
            tabs.Tag = "";
            tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
            // 
            // tabSnapshotsSummary
            // 
            tabSnapshotsSummary.Controls.Add(schemaSnapshots1);
            tabSnapshotsSummary.Location = new System.Drawing.Point(4, 39);
            tabSnapshotsSummary.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            tabSnapshotsSummary.Name = "tabSnapshotsSummary";
            tabSnapshotsSummary.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
            tabSnapshotsSummary.Size = new System.Drawing.Size(1578, 955);
            tabSnapshotsSummary.TabIndex = 1;
            tabSnapshotsSummary.Text = "Snapshot Summary";
            tabSnapshotsSummary.UseVisualStyleBackColor = true;
            // 
            // schemaSnapshots1
            // 
            schemaSnapshots1.Dock = System.Windows.Forms.DockStyle.Fill;
            schemaSnapshots1.Location = new System.Drawing.Point(3, 5);
            schemaSnapshots1.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            schemaSnapshots1.Name = "schemaSnapshots1";
            schemaSnapshots1.Size = new System.Drawing.Size(1572, 945);
            schemaSnapshots1.TabIndex = 0;
            // 
            // tabSchema
            // 
            tabSchema.Controls.Add(splitSchemaSnapshot);
            tabSchema.Location = new System.Drawing.Point(4, 39);
            tabSchema.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            tabSchema.Name = "tabSchema";
            tabSchema.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
            tabSchema.Size = new System.Drawing.Size(192, 57);
            tabSchema.TabIndex = 0;
            tabSchema.Text = "Schema Snapshot";
            tabSchema.UseVisualStyleBackColor = true;
            // 
            // splitSchemaSnapshot
            // 
            splitSchemaSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            splitSchemaSnapshot.Location = new System.Drawing.Point(3, 5);
            splitSchemaSnapshot.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            splitSchemaSnapshot.Name = "splitSchemaSnapshot";
            splitSchemaSnapshot.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitSchemaSnapshot.Panel1
            // 
            splitSchemaSnapshot.Panel1.Controls.Add(label7);
            // 
            // splitSchemaSnapshot.Panel2
            // 
            splitSchemaSnapshot.Panel2.Controls.Add(toolStrip1);
            splitSchemaSnapshot.Panel2.Controls.Add(gvHistory);
            splitSchemaSnapshot.Panel2.Controls.Add(label1);
            splitSchemaSnapshot.Size = new System.Drawing.Size(186, 47);
            splitSchemaSnapshot.SplitterDistance = 25;
            splitSchemaSnapshot.SplitterWidth = 2;
            splitSchemaSnapshot.TabIndex = 1;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(101, 281);
            label7.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(351, 20);
            label7.TabIndex = 1;
            label7.Text = "Diff (Loaded programatically due to designer issue)";
            label7.Visible = false;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsPrevious, tsPageNum, tsNext, toolStripLabel1, tsPageSize });
            toolStrip1.Location = new System.Drawing.Point(0, -5);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0);
            toolStrip1.Size = new System.Drawing.Size(186, 32);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsPrevious
            // 
            tsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPrevious.Name = "tsPrevious";
            tsPrevious.Size = new System.Drawing.Size(29, 29);
            tsPrevious.Text = "Previous";
            tsPrevious.Click += TsPrevious_Click;
            // 
            // tsPageNum
            // 
            tsPageNum.Name = "tsPageNum";
            tsPageNum.Size = new System.Drawing.Size(53, 29);
            tsPageNum.Text = "Page 1";
            // 
            // tsNext
            // 
            tsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsNext.Name = "tsNext";
            tsNext.Size = new System.Drawing.Size(29, 29);
            tsNext.Text = "Next";
            tsNext.Click += TsNext_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(75, 20);
            toolStripLabel1.Text = "Page Size:";
            // 
            // tsPageSize
            // 
            tsPageSize.Items.AddRange(new object[] { "100", "200", "500", "1000", "5000" });
            tsPageSize.Name = "tsPageSize";
            tsPageSize.Size = new System.Drawing.Size(75, 28);
            tsPageSize.Text = "100";
            tsPageSize.Validating += TsPageSize_Validating;
            tsPageSize.Validated += TsPageSize_Validated;
            // 
            // gvHistory
            // 
            gvHistory.AllowUserToAddRows = false;
            gvHistory.AllowUserToDeleteRows = false;
            gvHistory.AllowUserToOrderColumns = true;
            gvHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            gvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            gvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { ObjectName, SchemaName, ObjectType, SnapshotValidFrom, SnapshotValidTo, ObjectDateCreated, ObjectDateModified, colCompare });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            gvHistory.DefaultCellStyle = dataGridViewCellStyle2;
            gvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            gvHistory.Location = new System.Drawing.Point(0, 30);
            gvHistory.Margin = new System.Windows.Forms.Padding(1, 4, 1, 4);
            gvHistory.MultiSelect = false;
            gvHistory.Name = "gvHistory";
            gvHistory.RowHeadersVisible = false;
            gvHistory.RowHeadersWidth = 51;
            gvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            gvHistory.Size = new System.Drawing.Size(186, 0);
            gvHistory.TabIndex = 0;
            gvHistory.CellContentClick += GvHistory_CellContentClick;
            gvHistory.SelectionChanged += GvHistory_SelectionChanged;
            // 
            // ObjectName
            // 
            ObjectName.DataPropertyName = "ObjectName";
            ObjectName.HeaderText = "Object Name";
            ObjectName.MinimumWidth = 6;
            ObjectName.Name = "ObjectName";
            ObjectName.Width = 119;
            // 
            // SchemaName
            // 
            SchemaName.DataPropertyName = "SchemaName";
            SchemaName.HeaderText = "Schema Name";
            SchemaName.MinimumWidth = 6;
            SchemaName.Name = "SchemaName";
            SchemaName.Width = 129;
            // 
            // ObjectType
            // 
            ObjectType.HeaderText = "Object Type";
            ObjectType.MinimumWidth = 6;
            ObjectType.Name = "ObjectType";
            ObjectType.Width = 114;
            // 
            // SnapshotValidFrom
            // 
            SnapshotValidFrom.DataPropertyName = "SnapshotValidFrom";
            SnapshotValidFrom.HeaderText = "Snapshot Valid From";
            SnapshotValidFrom.MinimumWidth = 6;
            SnapshotValidFrom.Name = "SnapshotValidFrom";
            SnapshotValidFrom.Width = 168;
            // 
            // SnapshotValidTo
            // 
            SnapshotValidTo.DataPropertyName = "SnapshotValidTo";
            SnapshotValidTo.HeaderText = "Snapshot Valid To";
            SnapshotValidTo.MinimumWidth = 6;
            SnapshotValidTo.Name = "SnapshotValidTo";
            SnapshotValidTo.Width = 153;
            // 
            // ObjectDateCreated
            // 
            ObjectDateCreated.DataPropertyName = "ObjectDateCreated";
            ObjectDateCreated.HeaderText = "Date Created";
            ObjectDateCreated.MinimumWidth = 6;
            ObjectDateCreated.Name = "ObjectDateCreated";
            ObjectDateCreated.Width = 121;
            // 
            // ObjectDateModified
            // 
            ObjectDateModified.DataPropertyName = "ObjectDateModified";
            ObjectDateModified.HeaderText = "Date Modified";
            ObjectDateModified.MinimumWidth = 6;
            ObjectDateModified.Name = "ObjectDateModified";
            ObjectDateModified.Width = 124;
            // 
            // colCompare
            // 
            colCompare.HeaderText = "Compare";
            colCompare.MinimumWidth = 6;
            colCompare.Name = "colCompare";
            colCompare.Text = "Compare";
            colCompare.UseColumnTextForLinkValue = true;
            colCompare.Width = 125;
            // 
            // label1
            // 
            label1.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.ForeColor = System.Drawing.Color.White;
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(186, 30);
            label1.TabIndex = 2;
            label1.Text = "Snapshot History";
            label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // tabTags
            // 
            tabTags.Controls.Add(tags1);
            tabTags.Location = new System.Drawing.Point(4, 39);
            tabTags.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTags.Name = "tabTags";
            tabTags.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTags.Size = new System.Drawing.Size(192, 57);
            tabTags.TabIndex = 2;
            tabTags.Text = "Tags";
            tabTags.UseVisualStyleBackColor = true;
            // 
            // tags1
            // 
            tags1.Dock = System.Windows.Forms.DockStyle.Fill;
            tags1.Location = new System.Drawing.Point(1, 2);
            tags1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            tags1.Name = "tags1";
            tags1.Size = new System.Drawing.Size(190, 53);
            tags1.TabIndex = 0;
            tags1.TagsChanged += Tags1_TagsChanged;
            // 
            // tabDrives
            // 
            tabDrives.AutoScroll = true;
            tabDrives.Controls.Add(drivesControl1);
            tabDrives.Location = new System.Drawing.Point(4, 39);
            tabDrives.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrives.Name = "tabDrives";
            tabDrives.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrives.Size = new System.Drawing.Size(192, 57);
            tabDrives.TabIndex = 3;
            tabDrives.Text = "Drives";
            tabDrives.UseVisualStyleBackColor = true;
            // 
            // drivesControl1
            // 
            drivesControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            drivesControl1.IncludeCritical = true;
            drivesControl1.IncludeNA = true;
            drivesControl1.IncludeOK = true;
            drivesControl1.IncludeWarning = true;
            drivesControl1.Location = new System.Drawing.Point(1, 2);
            drivesControl1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            drivesControl1.Name = "drivesControl1";
            drivesControl1.Size = new System.Drawing.Size(190, 53);
            drivesControl1.TabIndex = 0;
            // 
            // tabBackups
            // 
            tabBackups.Controls.Add(backupsControl1);
            tabBackups.Location = new System.Drawing.Point(4, 39);
            tabBackups.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabBackups.Name = "tabBackups";
            tabBackups.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabBackups.Size = new System.Drawing.Size(192, 57);
            tabBackups.TabIndex = 4;
            tabBackups.Tag = "1";
            tabBackups.Text = "Backups";
            tabBackups.UseVisualStyleBackColor = true;
            // 
            // backupsControl1
            // 
            backupsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            backupsControl1.IncludeCritical = true;
            backupsControl1.IncludeNA = true;
            backupsControl1.IncludeOK = true;
            backupsControl1.IncludeWarning = true;
            backupsControl1.Location = new System.Drawing.Point(1, 2);
            backupsControl1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            backupsControl1.Name = "backupsControl1";
            backupsControl1.Size = new System.Drawing.Size(190, 53);
            backupsControl1.TabIndex = 0;
            // 
            // tabLogShipping
            // 
            tabLogShipping.Controls.Add(logShippingControl1);
            tabLogShipping.Location = new System.Drawing.Point(4, 39);
            tabLogShipping.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabLogShipping.Name = "tabLogShipping";
            tabLogShipping.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabLogShipping.Size = new System.Drawing.Size(192, 57);
            tabLogShipping.TabIndex = 5;
            tabLogShipping.Text = "Log Shipping";
            tabLogShipping.UseVisualStyleBackColor = true;
            // 
            // logShippingControl1
            // 
            logShippingControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            logShippingControl1.IncludeCritical = true;
            logShippingControl1.IncludeNA = true;
            logShippingControl1.IncludeOK = true;
            logShippingControl1.IncludeWarning = true;
            logShippingControl1.Location = new System.Drawing.Point(1, 2);
            logShippingControl1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            logShippingControl1.Name = "logShippingControl1";
            logShippingControl1.Size = new System.Drawing.Size(190, 53);
            logShippingControl1.TabIndex = 0;
            // 
            // tabJobs
            // 
            tabJobs.Controls.Add(agentJobsControl1);
            tabJobs.Location = new System.Drawing.Point(4, 39);
            tabJobs.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobs.Name = "tabJobs";
            tabJobs.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobs.Size = new System.Drawing.Size(192, 57);
            tabJobs.TabIndex = 6;
            tabJobs.Text = "Job Status";
            tabJobs.UseVisualStyleBackColor = true;
            // 
            // agentJobsControl1
            // 
            agentJobsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            agentJobsControl1.IncludeAcknowledged = true;
            agentJobsControl1.IncludeCritical = false;
            agentJobsControl1.IncludeNA = false;
            agentJobsControl1.IncludeOK = false;
            agentJobsControl1.IncludeWarning = false;
            agentJobsControl1.Location = new System.Drawing.Point(1, 2);
            agentJobsControl1.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            agentJobsControl1.Name = "agentJobsControl1";
            agentJobsControl1.ShowSteps = false;
            agentJobsControl1.Size = new System.Drawing.Size(190, 53);
            agentJobsControl1.TabIndex = 0;
            // 
            // tabSummary
            // 
            tabSummary.Controls.Add(summary1);
            tabSummary.Location = new System.Drawing.Point(4, 39);
            tabSummary.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSummary.Name = "tabSummary";
            tabSummary.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSummary.Size = new System.Drawing.Size(192, 57);
            tabSummary.TabIndex = 7;
            tabSummary.Text = "Summary";
            tabSummary.UseVisualStyleBackColor = true;
            // 
            // summary1
            // 
            summary1.Dock = System.Windows.Forms.DockStyle.Fill;
            summary1.Location = new System.Drawing.Point(1, 2);
            summary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            summary1.Name = "summary1";
            summary1.Size = new System.Drawing.Size(190, 53);
            summary1.TabIndex = 0;
            summary1.Instance_Selected += Instance_Selected;
            // 
            // tabFiles
            // 
            tabFiles.Controls.Add(dbFilesControl1);
            tabFiles.Location = new System.Drawing.Point(4, 39);
            tabFiles.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabFiles.Name = "tabFiles";
            tabFiles.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabFiles.Size = new System.Drawing.Size(192, 57);
            tabFiles.TabIndex = 8;
            tabFiles.Text = "Files";
            tabFiles.UseVisualStyleBackColor = true;
            // 
            // dbFilesControl1
            // 
            dbFilesControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            dbFilesControl1.IncludeCritical = true;
            dbFilesControl1.IncludeNA = true;
            dbFilesControl1.IncludeOK = true;
            dbFilesControl1.IncludeWarning = true;
            dbFilesControl1.Location = new System.Drawing.Point(1, 2);
            dbFilesControl1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            dbFilesControl1.Name = "dbFilesControl1";
            dbFilesControl1.Size = new System.Drawing.Size(190, 53);
            dbFilesControl1.TabIndex = 0;
            // 
            // tabLastGood
            // 
            tabLastGood.Controls.Add(lastGoodCheckDBControl1);
            tabLastGood.Location = new System.Drawing.Point(4, 39);
            tabLastGood.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabLastGood.Name = "tabLastGood";
            tabLastGood.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabLastGood.Size = new System.Drawing.Size(192, 57);
            tabLastGood.TabIndex = 9;
            tabLastGood.Text = "Last Good CheckDB";
            tabLastGood.UseVisualStyleBackColor = true;
            // 
            // lastGoodCheckDBControl1
            // 
            lastGoodCheckDBControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            lastGoodCheckDBControl1.IncludeCritical = true;
            lastGoodCheckDBControl1.IncludeNA = true;
            lastGoodCheckDBControl1.IncludeOK = true;
            lastGoodCheckDBControl1.IncludeWarning = true;
            lastGoodCheckDBControl1.Location = new System.Drawing.Point(1, 2);
            lastGoodCheckDBControl1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            lastGoodCheckDBControl1.Name = "lastGoodCheckDBControl1";
            lastGoodCheckDBControl1.Size = new System.Drawing.Size(190, 53);
            lastGoodCheckDBControl1.TabIndex = 0;
            // 
            // tabPerformance
            // 
            tabPerformance.Controls.Add(performance1);
            tabPerformance.Location = new System.Drawing.Point(4, 39);
            tabPerformance.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPerformance.Name = "tabPerformance";
            tabPerformance.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPerformance.Size = new System.Drawing.Size(192, 57);
            tabPerformance.TabIndex = 10;
            tabPerformance.Text = "Performance";
            tabPerformance.UseVisualStyleBackColor = true;
            // 
            // performance1
            // 
            performance1.AutoScroll = true;
            performance1.Dock = System.Windows.Forms.DockStyle.Fill;
            performance1.Location = new System.Drawing.Point(1, 2);
            performance1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            performance1.Name = "performance1";
            performance1.Size = new System.Drawing.Size(190, 53);
            performance1.TabIndex = 0;
            // 
            // tabDBADashErrorLog
            // 
            tabDBADashErrorLog.Controls.Add(collectionErrors1);
            tabDBADashErrorLog.Location = new System.Drawing.Point(4, 39);
            tabDBADashErrorLog.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBADashErrorLog.Name = "tabDBADashErrorLog";
            tabDBADashErrorLog.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBADashErrorLog.Size = new System.Drawing.Size(192, 57);
            tabDBADashErrorLog.TabIndex = 11;
            tabDBADashErrorLog.Text = "DBA Dash ErrorLog";
            tabDBADashErrorLog.UseVisualStyleBackColor = true;
            // 
            // collectionErrors1
            // 
            collectionErrors1.AckErrors = false;
            collectionErrors1.Days = 0;
            collectionErrors1.Dock = System.Windows.Forms.DockStyle.Fill;
            collectionErrors1.Location = new System.Drawing.Point(1, 2);
            collectionErrors1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            collectionErrors1.Name = "collectionErrors1";
            collectionErrors1.Size = new System.Drawing.Size(190, 53);
            collectionErrors1.TabIndex = 0;
            // 
            // tabCollectionDates
            // 
            tabCollectionDates.Controls.Add(collectionDates1);
            tabCollectionDates.Location = new System.Drawing.Point(4, 39);
            tabCollectionDates.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCollectionDates.Name = "tabCollectionDates";
            tabCollectionDates.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCollectionDates.Size = new System.Drawing.Size(192, 57);
            tabCollectionDates.TabIndex = 12;
            tabCollectionDates.Text = "Collection Dates";
            tabCollectionDates.UseVisualStyleBackColor = true;
            // 
            // collectionDates1
            // 
            collectionDates1.Dock = System.Windows.Forms.DockStyle.Fill;
            collectionDates1.IncludeCritical = true;
            collectionDates1.IncludeNA = true;
            collectionDates1.IncludeOK = true;
            collectionDates1.IncludeWarning = true;
            collectionDates1.Location = new System.Drawing.Point(1, 2);
            collectionDates1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            collectionDates1.Name = "collectionDates1";
            collectionDates1.Size = new System.Drawing.Size(190, 53);
            collectionDates1.TabIndex = 0;
            // 
            // tabPerformanceSummary
            // 
            tabPerformanceSummary.Controls.Add(performanceSummary1);
            tabPerformanceSummary.Location = new System.Drawing.Point(4, 39);
            tabPerformanceSummary.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPerformanceSummary.Name = "tabPerformanceSummary";
            tabPerformanceSummary.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPerformanceSummary.Size = new System.Drawing.Size(192, 57);
            tabPerformanceSummary.TabIndex = 13;
            tabPerformanceSummary.Text = "Performance Summary";
            tabPerformanceSummary.UseVisualStyleBackColor = true;
            // 
            // performanceSummary1
            // 
            performanceSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            performanceSummary1.Location = new System.Drawing.Point(1, 2);
            performanceSummary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            performanceSummary1.Name = "performanceSummary1";
            performanceSummary1.Size = new System.Drawing.Size(190, 53);
            performanceSummary1.TabIndex = 0;
            performanceSummary1.Instance_Selected += Instance_Selected;
            // 
            // tabInfo
            // 
            tabInfo.Controls.Add(info1);
            tabInfo.Location = new System.Drawing.Point(4, 39);
            tabInfo.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabInfo.Name = "tabInfo";
            tabInfo.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabInfo.Size = new System.Drawing.Size(192, 57);
            tabInfo.TabIndex = 14;
            tabInfo.Text = "Info";
            tabInfo.UseVisualStyleBackColor = true;
            // 
            // info1
            // 
            info1.Dock = System.Windows.Forms.DockStyle.Fill;
            info1.Location = new System.Drawing.Point(1, 2);
            info1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            info1.Name = "info1";
            info1.Size = new System.Drawing.Size(190, 53);
            info1.TabIndex = 0;
            // 
            // tabHardware
            // 
            tabHardware.Controls.Add(hardwareChanges1);
            tabHardware.Location = new System.Drawing.Point(4, 39);
            tabHardware.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabHardware.Name = "tabHardware";
            tabHardware.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabHardware.Size = new System.Drawing.Size(192, 57);
            tabHardware.TabIndex = 15;
            tabHardware.Text = "Hardware";
            tabHardware.UseVisualStyleBackColor = true;
            // 
            // hardwareChanges1
            // 
            hardwareChanges1.Dock = System.Windows.Forms.DockStyle.Fill;
            hardwareChanges1.Location = new System.Drawing.Point(1, 2);
            hardwareChanges1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            hardwareChanges1.Name = "hardwareChanges1";
            hardwareChanges1.Size = new System.Drawing.Size(190, 53);
            hardwareChanges1.TabIndex = 0;
            // 
            // tabSQLPatching
            // 
            tabSQLPatching.Controls.Add(sqlPatching1);
            tabSQLPatching.Location = new System.Drawing.Point(4, 39);
            tabSQLPatching.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSQLPatching.Name = "tabSQLPatching";
            tabSQLPatching.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSQLPatching.Size = new System.Drawing.Size(192, 57);
            tabSQLPatching.TabIndex = 16;
            tabSQLPatching.Text = "SQL Patching";
            tabSQLPatching.UseVisualStyleBackColor = true;
            // 
            // sqlPatching1
            // 
            sqlPatching1.Dock = System.Windows.Forms.DockStyle.Fill;
            sqlPatching1.Location = new System.Drawing.Point(1, 2);
            sqlPatching1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            sqlPatching1.Name = "sqlPatching1";
            sqlPatching1.Size = new System.Drawing.Size(190, 53);
            sqlPatching1.TabIndex = 0;
            // 
            // tabInstanceConfig
            // 
            tabInstanceConfig.Controls.Add(configurationHistory1);
            tabInstanceConfig.Location = new System.Drawing.Point(4, 39);
            tabInstanceConfig.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabInstanceConfig.Name = "tabInstanceConfig";
            tabInstanceConfig.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabInstanceConfig.Size = new System.Drawing.Size(192, 57);
            tabInstanceConfig.TabIndex = 17;
            tabInstanceConfig.Text = "Configuration";
            tabInstanceConfig.UseVisualStyleBackColor = true;
            // 
            // configurationHistory1
            // 
            configurationHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            configurationHistory1.Location = new System.Drawing.Point(1, 2);
            configurationHistory1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            configurationHistory1.Name = "configurationHistory1";
            configurationHistory1.Size = new System.Drawing.Size(190, 53);
            configurationHistory1.TabIndex = 0;
            // 
            // tabSlowQueries
            // 
            tabSlowQueries.Controls.Add(slowQueries1);
            tabSlowQueries.Location = new System.Drawing.Point(4, 39);
            tabSlowQueries.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSlowQueries.Name = "tabSlowQueries";
            tabSlowQueries.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSlowQueries.Size = new System.Drawing.Size(192, 57);
            tabSlowQueries.TabIndex = 18;
            tabSlowQueries.Text = "Slow Queries";
            tabSlowQueries.UseVisualStyleBackColor = true;
            // 
            // slowQueries1
            // 
            slowQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            slowQueries1.Location = new System.Drawing.Point(1, 2);
            slowQueries1.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            slowQueries1.Name = "slowQueries1";
            slowQueries1.Size = new System.Drawing.Size(190, 53);
            slowQueries1.TabIndex = 0;
            // 
            // tabTraceFlags
            // 
            tabTraceFlags.Controls.Add(traceFlagHistory1);
            tabTraceFlags.Location = new System.Drawing.Point(4, 39);
            tabTraceFlags.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTraceFlags.Name = "tabTraceFlags";
            tabTraceFlags.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTraceFlags.Size = new System.Drawing.Size(192, 57);
            tabTraceFlags.TabIndex = 19;
            tabTraceFlags.Text = "Trace Flags";
            tabTraceFlags.UseVisualStyleBackColor = true;
            // 
            // traceFlagHistory1
            // 
            traceFlagHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            traceFlagHistory1.Location = new System.Drawing.Point(1, 2);
            traceFlagHistory1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            traceFlagHistory1.Name = "traceFlagHistory1";
            traceFlagHistory1.Size = new System.Drawing.Size(190, 53);
            traceFlagHistory1.TabIndex = 0;
            // 
            // tabSQLAgentAlerts
            // 
            tabSQLAgentAlerts.Controls.Add(alerts1);
            tabSQLAgentAlerts.Location = new System.Drawing.Point(4, 39);
            tabSQLAgentAlerts.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSQLAgentAlerts.Name = "tabSQLAgentAlerts";
            tabSQLAgentAlerts.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabSQLAgentAlerts.Size = new System.Drawing.Size(192, 57);
            tabSQLAgentAlerts.TabIndex = 20;
            tabSQLAgentAlerts.Text = "SQL Agent Alerts";
            tabSQLAgentAlerts.UseVisualStyleBackColor = true;
            // 
            // alerts1
            // 
            alerts1.Dock = System.Windows.Forms.DockStyle.Fill;
            alerts1.Location = new System.Drawing.Point(1, 2);
            alerts1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            alerts1.Name = "alerts1";
            alerts1.Size = new System.Drawing.Size(190, 53);
            alerts1.TabIndex = 0;
            alerts1.UseAlertName = false;
            // 
            // tabDrivers
            // 
            tabDrivers.Controls.Add(drivers1);
            tabDrivers.Location = new System.Drawing.Point(4, 39);
            tabDrivers.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrivers.Name = "tabDrivers";
            tabDrivers.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrivers.Size = new System.Drawing.Size(192, 57);
            tabDrivers.TabIndex = 21;
            tabDrivers.Text = "Drivers";
            tabDrivers.UseVisualStyleBackColor = true;
            // 
            // drivers1
            // 
            drivers1.Dock = System.Windows.Forms.DockStyle.Fill;
            drivers1.Location = new System.Drawing.Point(1, 2);
            drivers1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            drivers1.Name = "drivers1";
            drivers1.Size = new System.Drawing.Size(190, 53);
            drivers1.TabIndex = 0;
            // 
            // tabDBSpace
            // 
            tabDBSpace.Controls.Add(spaceTracking1);
            tabDBSpace.Location = new System.Drawing.Point(4, 39);
            tabDBSpace.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBSpace.Name = "tabDBSpace";
            tabDBSpace.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBSpace.Size = new System.Drawing.Size(192, 57);
            tabDBSpace.TabIndex = 22;
            tabDBSpace.Text = "DB Space";
            tabDBSpace.UseVisualStyleBackColor = true;
            // 
            // spaceTracking1
            // 
            spaceTracking1.Dock = System.Windows.Forms.DockStyle.Fill;
            spaceTracking1.Location = new System.Drawing.Point(1, 2);
            spaceTracking1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            spaceTracking1.Name = "spaceTracking1";
            spaceTracking1.Size = new System.Drawing.Size(190, 53);
            spaceTracking1.TabIndex = 0;
            // 
            // tabAzureSummary
            // 
            tabAzureSummary.Controls.Add(azureSummary1);
            tabAzureSummary.Location = new System.Drawing.Point(4, 39);
            tabAzureSummary.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureSummary.Name = "tabAzureSummary";
            tabAzureSummary.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureSummary.Size = new System.Drawing.Size(192, 57);
            tabAzureSummary.TabIndex = 23;
            tabAzureSummary.Text = "Azure Summary";
            tabAzureSummary.UseVisualStyleBackColor = true;
            // 
            // azureSummary1
            // 
            azureSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            azureSummary1.Location = new System.Drawing.Point(1, 2);
            azureSummary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            azureSummary1.Name = "azureSummary1";
            azureSummary1.Size = new System.Drawing.Size(190, 53);
            azureSummary1.TabIndex = 0;
            // 
            // tabAzureDB
            // 
            tabAzureDB.Controls.Add(azureDBResourceStats1);
            tabAzureDB.Location = new System.Drawing.Point(4, 39);
            tabAzureDB.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureDB.Name = "tabAzureDB";
            tabAzureDB.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureDB.Size = new System.Drawing.Size(192, 57);
            tabAzureDB.TabIndex = 24;
            tabAzureDB.Text = "Azure DB";
            tabAzureDB.UseVisualStyleBackColor = true;
            // 
            // azureDBResourceStats1
            // 
            azureDBResourceStats1.DateGrouping = 0;
            azureDBResourceStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            azureDBResourceStats1.Location = new System.Drawing.Point(1, 2);
            azureDBResourceStats1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            azureDBResourceStats1.Name = "azureDBResourceStats1";
            azureDBResourceStats1.Size = new System.Drawing.Size(190, 53);
            azureDBResourceStats1.TabIndex = 0;
            // 
            // tabServiceObjectives
            // 
            tabServiceObjectives.Controls.Add(azureServiceObjectivesHistory1);
            tabServiceObjectives.Location = new System.Drawing.Point(4, 39);
            tabServiceObjectives.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabServiceObjectives.Name = "tabServiceObjectives";
            tabServiceObjectives.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabServiceObjectives.Size = new System.Drawing.Size(192, 57);
            tabServiceObjectives.TabIndex = 25;
            tabServiceObjectives.Text = "Azure Service Objectives";
            tabServiceObjectives.UseVisualStyleBackColor = true;
            // 
            // azureServiceObjectivesHistory1
            // 
            azureServiceObjectivesHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            azureServiceObjectivesHistory1.Location = new System.Drawing.Point(1, 2);
            azureServiceObjectivesHistory1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            azureServiceObjectivesHistory1.Name = "azureServiceObjectivesHistory1";
            azureServiceObjectivesHistory1.Size = new System.Drawing.Size(190, 53);
            azureServiceObjectivesHistory1.TabIndex = 0;
            // 
            // tabDBConfiguration
            // 
            tabDBConfiguration.Controls.Add(dbConfiguration1);
            tabDBConfiguration.Location = new System.Drawing.Point(4, 39);
            tabDBConfiguration.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBConfiguration.Name = "tabDBConfiguration";
            tabDBConfiguration.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBConfiguration.Size = new System.Drawing.Size(192, 57);
            tabDBConfiguration.TabIndex = 26;
            tabDBConfiguration.Text = "DB Configuration";
            tabDBConfiguration.UseVisualStyleBackColor = true;
            // 
            // dbConfiguration1
            // 
            dbConfiguration1.Dock = System.Windows.Forms.DockStyle.Fill;
            dbConfiguration1.Location = new System.Drawing.Point(1, 2);
            dbConfiguration1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            dbConfiguration1.Name = "dbConfiguration1";
            dbConfiguration1.Size = new System.Drawing.Size(190, 53);
            dbConfiguration1.TabIndex = 0;
            // 
            // tabDBOptions
            // 
            tabDBOptions.Controls.Add(dbOptions1);
            tabDBOptions.Location = new System.Drawing.Point(4, 39);
            tabDBOptions.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBOptions.Name = "tabDBOptions";
            tabDBOptions.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBOptions.Size = new System.Drawing.Size(192, 57);
            tabDBOptions.TabIndex = 27;
            tabDBOptions.Text = "DB Options";
            tabDBOptions.UseVisualStyleBackColor = true;
            // 
            // dbOptions1
            // 
            dbOptions1.Dock = System.Windows.Forms.DockStyle.Fill;
            dbOptions1.Location = new System.Drawing.Point(1, 2);
            dbOptions1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            dbOptions1.Name = "dbOptions1";
            dbOptions1.Size = new System.Drawing.Size(190, 53);
            dbOptions1.SummaryMode = false;
            dbOptions1.TabIndex = 0;
            // 
            // tabTempDB
            // 
            tabTempDB.Controls.Add(tempDBConfig1);
            tabTempDB.Location = new System.Drawing.Point(4, 39);
            tabTempDB.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTempDB.Name = "tabTempDB";
            tabTempDB.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTempDB.Size = new System.Drawing.Size(192, 57);
            tabTempDB.TabIndex = 28;
            tabTempDB.Text = "TempDB";
            tabTempDB.UseVisualStyleBackColor = true;
            // 
            // tempDBConfig1
            // 
            tempDBConfig1.Dock = System.Windows.Forms.DockStyle.Fill;
            tempDBConfig1.Location = new System.Drawing.Point(1, 2);
            tempDBConfig1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            tempDBConfig1.Name = "tempDBConfig1";
            tempDBConfig1.Size = new System.Drawing.Size(190, 53);
            tempDBConfig1.TabIndex = 0;
            // 
            // tabCustomChecks
            // 
            tabCustomChecks.Controls.Add(customChecks1);
            tabCustomChecks.Location = new System.Drawing.Point(4, 39);
            tabCustomChecks.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCustomChecks.Name = "tabCustomChecks";
            tabCustomChecks.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCustomChecks.Size = new System.Drawing.Size(192, 57);
            tabCustomChecks.TabIndex = 29;
            tabCustomChecks.Text = "Custom";
            tabCustomChecks.UseVisualStyleBackColor = true;
            // 
            // customChecks1
            // 
            customChecks1.CheckContext = null;
            customChecks1.Dock = System.Windows.Forms.DockStyle.Fill;
            customChecks1.IncludeCritical = true;
            customChecks1.IncludeNA = false;
            customChecks1.IncludeOK = false;
            customChecks1.IncludeWarning = true;
            customChecks1.Location = new System.Drawing.Point(1, 2);
            customChecks1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            customChecks1.Name = "customChecks1";
            customChecks1.Size = new System.Drawing.Size(190, 53);
            customChecks1.TabIndex = 0;
            customChecks1.Test = null;
            // 
            // tabPC
            // 
            tabPC.Controls.Add(performanceCounterSummary1);
            tabPC.Location = new System.Drawing.Point(4, 39);
            tabPC.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPC.Name = "tabPC";
            tabPC.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabPC.Size = new System.Drawing.Size(192, 57);
            tabPC.TabIndex = 30;
            tabPC.Text = "Metrics";
            tabPC.UseVisualStyleBackColor = true;
            // 
            // performanceCounterSummary1
            // 
            performanceCounterSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            performanceCounterSummary1.Location = new System.Drawing.Point(1, 2);
            performanceCounterSummary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            performanceCounterSummary1.Name = "performanceCounterSummary1";
            performanceCounterSummary1.Size = new System.Drawing.Size(190, 53);
            performanceCounterSummary1.TabIndex = 0;
            // 
            // tabObjectExecutionSummary
            // 
            tabObjectExecutionSummary.Controls.Add(objectExecutionSummary1);
            tabObjectExecutionSummary.Location = new System.Drawing.Point(4, 39);
            tabObjectExecutionSummary.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabObjectExecutionSummary.Name = "tabObjectExecutionSummary";
            tabObjectExecutionSummary.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabObjectExecutionSummary.Size = new System.Drawing.Size(192, 57);
            tabObjectExecutionSummary.TabIndex = 31;
            tabObjectExecutionSummary.Text = "Object Execution";
            tabObjectExecutionSummary.UseVisualStyleBackColor = true;
            // 
            // objectExecutionSummary1
            // 
            objectExecutionSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            objectExecutionSummary1.Location = new System.Drawing.Point(1, 2);
            objectExecutionSummary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            objectExecutionSummary1.Name = "objectExecutionSummary1";
            objectExecutionSummary1.Size = new System.Drawing.Size(190, 53);
            objectExecutionSummary1.TabIndex = 0;
            objectExecutionSummary1.Types = "";
            objectExecutionSummary1.UseGlobalTime = true;
            // 
            // tabWaits
            // 
            tabWaits.Controls.Add(waitsSummary1);
            tabWaits.Location = new System.Drawing.Point(4, 39);
            tabWaits.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabWaits.Name = "tabWaits";
            tabWaits.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabWaits.Size = new System.Drawing.Size(192, 57);
            tabWaits.TabIndex = 32;
            tabWaits.Text = "Waits";
            tabWaits.UseVisualStyleBackColor = true;
            // 
            // waitsSummary1
            // 
            waitsSummary1.DateGrouping = 1;
            waitsSummary1.Dock = System.Windows.Forms.DockStyle.Fill;
            waitsSummary1.Location = new System.Drawing.Point(1, 2);
            waitsSummary1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            waitsSummary1.Name = "waitsSummary1";
            waitsSummary1.Size = new System.Drawing.Size(190, 53);
            waitsSummary1.TabIndex = 0;
            // 
            // tabMirroring
            // 
            tabMirroring.Controls.Add(mirroring1);
            tabMirroring.Location = new System.Drawing.Point(4, 39);
            tabMirroring.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabMirroring.Name = "tabMirroring";
            tabMirroring.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabMirroring.Size = new System.Drawing.Size(192, 57);
            tabMirroring.TabIndex = 33;
            tabMirroring.Text = "Mirroring";
            tabMirroring.UseVisualStyleBackColor = true;
            // 
            // mirroring1
            // 
            mirroring1.Dock = System.Windows.Forms.DockStyle.Fill;
            mirroring1.Location = new System.Drawing.Point(1, 2);
            mirroring1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            mirroring1.Name = "mirroring1";
            mirroring1.Size = new System.Drawing.Size(190, 53);
            mirroring1.SummaryMode = true;
            mirroring1.TabIndex = 0;
            // 
            // tabJobDDL
            // 
            tabJobDDL.Controls.Add(jobDDLHistory1);
            tabJobDDL.Location = new System.Drawing.Point(4, 39);
            tabJobDDL.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobDDL.Name = "tabJobDDL";
            tabJobDDL.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobDDL.Size = new System.Drawing.Size(192, 57);
            tabJobDDL.TabIndex = 34;
            tabJobDDL.Text = "Job DDL";
            tabJobDDL.UseVisualStyleBackColor = true;
            // 
            // jobDDLHistory1
            // 
            jobDDLHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobDDLHistory1.Location = new System.Drawing.Point(1, 2);
            jobDDLHistory1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            jobDDLHistory1.Name = "jobDDLHistory1";
            jobDDLHistory1.Size = new System.Drawing.Size(190, 53);
            jobDDLHistory1.TabIndex = 0;
            // 
            // tabAG
            // 
            tabAG.Controls.Add(ag1);
            tabAG.Location = new System.Drawing.Point(4, 39);
            tabAG.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAG.Name = "tabAG";
            tabAG.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAG.Size = new System.Drawing.Size(192, 57);
            tabAG.TabIndex = 35;
            tabAG.Text = "Availability Groups";
            tabAG.UseVisualStyleBackColor = true;
            // 
            // ag1
            // 
            ag1.Dock = System.Windows.Forms.DockStyle.Fill;
            ag1.Location = new System.Drawing.Point(1, 2);
            ag1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            ag1.Name = "ag1";
            ag1.Size = new System.Drawing.Size(190, 53);
            ag1.TabIndex = 0;
            // 
            // tabQS
            // 
            tabQS.Controls.Add(queryStore1);
            tabQS.Location = new System.Drawing.Point(4, 39);
            tabQS.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabQS.Name = "tabQS";
            tabQS.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabQS.Size = new System.Drawing.Size(192, 57);
            tabQS.TabIndex = 36;
            tabQS.Text = "QS";
            tabQS.UseVisualStyleBackColor = true;
            // 
            // queryStore1
            // 
            queryStore1.Dock = System.Windows.Forms.DockStyle.Fill;
            queryStore1.Location = new System.Drawing.Point(1, 2);
            queryStore1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            queryStore1.Name = "queryStore1";
            queryStore1.Size = new System.Drawing.Size(190, 53);
            queryStore1.TabIndex = 0;
            // 
            // tabRG
            // 
            tabRG.Controls.Add(resourceGovernor1);
            tabRG.Location = new System.Drawing.Point(4, 39);
            tabRG.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRG.Name = "tabRG";
            tabRG.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRG.Size = new System.Drawing.Size(192, 57);
            tabRG.TabIndex = 37;
            tabRG.Text = "Resource Governor";
            tabRG.UseVisualStyleBackColor = true;
            // 
            // resourceGovernor1
            // 
            resourceGovernor1.Dock = System.Windows.Forms.DockStyle.Fill;
            resourceGovernor1.Location = new System.Drawing.Point(1, 2);
            resourceGovernor1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            resourceGovernor1.Name = "resourceGovernor1";
            resourceGovernor1.Size = new System.Drawing.Size(190, 53);
            resourceGovernor1.TabIndex = 0;
            // 
            // tabAzureDBesourceGovernance
            // 
            tabAzureDBesourceGovernance.Controls.Add(azureDBResourceGovernance1);
            tabAzureDBesourceGovernance.Location = new System.Drawing.Point(4, 39);
            tabAzureDBesourceGovernance.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureDBesourceGovernance.Name = "tabAzureDBesourceGovernance";
            tabAzureDBesourceGovernance.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabAzureDBesourceGovernance.Size = new System.Drawing.Size(192, 57);
            tabAzureDBesourceGovernance.TabIndex = 38;
            tabAzureDBesourceGovernance.Text = "Azure Resource Governance";
            tabAzureDBesourceGovernance.UseVisualStyleBackColor = true;
            // 
            // azureDBResourceGovernance1
            // 
            azureDBResourceGovernance1.Dock = System.Windows.Forms.DockStyle.Fill;
            azureDBResourceGovernance1.Location = new System.Drawing.Point(1, 2);
            azureDBResourceGovernance1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            azureDBResourceGovernance1.Name = "azureDBResourceGovernance1";
            azureDBResourceGovernance1.Size = new System.Drawing.Size(190, 53);
            azureDBResourceGovernance1.TabIndex = 0;
            // 
            // tabRunningQueries
            // 
            tabRunningQueries.Controls.Add(runningQueries1);
            tabRunningQueries.Location = new System.Drawing.Point(4, 39);
            tabRunningQueries.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRunningQueries.Name = "tabRunningQueries";
            tabRunningQueries.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRunningQueries.Size = new System.Drawing.Size(192, 57);
            tabRunningQueries.TabIndex = 39;
            tabRunningQueries.Text = "Running Queries";
            tabRunningQueries.UseVisualStyleBackColor = true;
            // 
            // runningQueries1
            // 
            runningQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            runningQueries1.Location = new System.Drawing.Point(1, 2);
            runningQueries1.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            runningQueries1.Name = "runningQueries1";
            runningQueries1.Size = new System.Drawing.Size(190, 53);
            runningQueries1.TabIndex = 0;
            // 
            // tabMemory
            // 
            tabMemory.Controls.Add(memoryUsage1);
            tabMemory.Location = new System.Drawing.Point(4, 39);
            tabMemory.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabMemory.Name = "tabMemory";
            tabMemory.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabMemory.Size = new System.Drawing.Size(192, 57);
            tabMemory.TabIndex = 40;
            tabMemory.Text = "Memory";
            tabMemory.UseVisualStyleBackColor = true;
            // 
            // memoryUsage1
            // 
            memoryUsage1.Dock = System.Windows.Forms.DockStyle.Fill;
            memoryUsage1.Location = new System.Drawing.Point(1, 2);
            memoryUsage1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            memoryUsage1.Name = "memoryUsage1";
            memoryUsage1.Size = new System.Drawing.Size(190, 53);
            memoryUsage1.TabIndex = 0;
            // 
            // tabJobStats
            // 
            tabJobStats.Controls.Add(jobStats1);
            tabJobStats.Location = new System.Drawing.Point(4, 39);
            tabJobStats.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobStats.Name = "tabJobStats";
            tabJobStats.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobStats.Size = new System.Drawing.Size(192, 57);
            tabJobStats.TabIndex = 41;
            tabJobStats.Text = "Job Stats";
            tabJobStats.UseVisualStyleBackColor = true;
            // 
            // jobStats1
            // 
            jobStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobStats1.Location = new System.Drawing.Point(1, 2);
            jobStats1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            jobStats1.Name = "jobStats1";
            jobStats1.Size = new System.Drawing.Size(190, 53);
            jobStats1.TabIndex = 0;
            jobStats1.UseGlobalTime = true;
            // 
            // tabDBADash
            // 
            tabDBADash.Controls.Add(lblVersion);
            tabDBADash.Controls.Add(lblSQLMonitoring);
            tabDBADash.Controls.Add(lblDBADash);
            tabDBADash.Location = new System.Drawing.Point(4, 39);
            tabDBADash.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBADash.Name = "tabDBADash";
            tabDBADash.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDBADash.Size = new System.Drawing.Size(192, 57);
            tabDBADash.TabIndex = 42;
            tabDBADash.UseVisualStyleBackColor = true;
            // 
            // lblVersion
            // 
            lblVersion.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            lblVersion.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblVersion.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Italic);
            lblVersion.ForeColor = System.Drawing.Color.White;
            lblVersion.Location = new System.Drawing.Point(1, 35);
            lblVersion.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new System.Drawing.Size(190, 20);
            lblVersion.TabIndex = 29;
            lblVersion.Text = "{Version}";
            // 
            // lblSQLMonitoring
            // 
            lblSQLMonitoring.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            lblSQLMonitoring.Dock = System.Windows.Forms.DockStyle.Top;
            lblSQLMonitoring.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Italic);
            lblSQLMonitoring.ForeColor = System.Drawing.Color.White;
            lblSQLMonitoring.Location = new System.Drawing.Point(1, 2);
            lblSQLMonitoring.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            lblSQLMonitoring.Name = "lblSQLMonitoring";
            lblSQLMonitoring.Size = new System.Drawing.Size(190, 17);
            lblSQLMonitoring.TabIndex = 28;
            lblSQLMonitoring.Text = "SQL Server Monitoring";
            lblSQLMonitoring.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblDBADash
            // 
            lblDBADash.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            lblDBADash.Dock = System.Windows.Forms.DockStyle.Fill;
            lblDBADash.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
            lblDBADash.ForeColor = System.Drawing.Color.White;
            lblDBADash.Location = new System.Drawing.Point(1, 2);
            lblDBADash.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            lblDBADash.Name = "lblDBADash";
            lblDBADash.Size = new System.Drawing.Size(190, 53);
            lblDBADash.TabIndex = 0;
            lblDBADash.Text = "DBA Dash";
            lblDBADash.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tabIdentityColumns
            // 
            tabIdentityColumns.Controls.Add(identityColumns1);
            tabIdentityColumns.Location = new System.Drawing.Point(4, 39);
            tabIdentityColumns.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabIdentityColumns.Name = "tabIdentityColumns";
            tabIdentityColumns.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabIdentityColumns.Size = new System.Drawing.Size(192, 57);
            tabIdentityColumns.TabIndex = 43;
            tabIdentityColumns.Text = "Identity Columns";
            tabIdentityColumns.UseVisualStyleBackColor = true;
            // 
            // identityColumns1
            // 
            identityColumns1.Dock = System.Windows.Forms.DockStyle.Fill;
            identityColumns1.IncludeCritical = true;
            identityColumns1.IncludeNA = false;
            identityColumns1.IncludeOK = false;
            identityColumns1.IncludeWarning = true;
            identityColumns1.Location = new System.Drawing.Point(1, 2);
            identityColumns1.Margin = new System.Windows.Forms.Padding(1);
            identityColumns1.Name = "identityColumns1";
            identityColumns1.Size = new System.Drawing.Size(190, 53);
            identityColumns1.TabIndex = 0;
            // 
            // tabOSLoadedModules
            // 
            tabOSLoadedModules.Controls.Add(osLoadedModules1);
            tabOSLoadedModules.Location = new System.Drawing.Point(4, 39);
            tabOSLoadedModules.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabOSLoadedModules.Name = "tabOSLoadedModules";
            tabOSLoadedModules.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabOSLoadedModules.Size = new System.Drawing.Size(192, 57);
            tabOSLoadedModules.TabIndex = 44;
            tabOSLoadedModules.Text = "OS Loaded Modules";
            tabOSLoadedModules.UseVisualStyleBackColor = true;
            // 
            // osLoadedModules1
            // 
            osLoadedModules1.Dock = System.Windows.Forms.DockStyle.Fill;
            osLoadedModules1.Location = new System.Drawing.Point(1, 2);
            osLoadedModules1.Margin = new System.Windows.Forms.Padding(1);
            osLoadedModules1.Name = "osLoadedModules1";
            osLoadedModules1.Size = new System.Drawing.Size(190, 53);
            osLoadedModules1.TabIndex = 0;
            // 
            // tabJobTimeline
            // 
            tabJobTimeline.Controls.Add(jobTimeline1);
            tabJobTimeline.Location = new System.Drawing.Point(4, 39);
            tabJobTimeline.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobTimeline.Name = "tabJobTimeline";
            tabJobTimeline.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabJobTimeline.Size = new System.Drawing.Size(192, 57);
            tabJobTimeline.TabIndex = 45;
            tabJobTimeline.Text = "Timeline";
            tabJobTimeline.UseVisualStyleBackColor = true;
            // 
            // jobTimeline1
            // 
            jobTimeline1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobTimeline1.IsActive = false;
            jobTimeline1.Location = new System.Drawing.Point(1, 2);
            jobTimeline1.Margin = new System.Windows.Forms.Padding(1);
            jobTimeline1.Name = "jobTimeline1";
            jobTimeline1.Size = new System.Drawing.Size(190, 53);
            jobTimeline1.TabIndex = 0;
            jobTimeline1.UseGlobalTime = true;
            // 
            // tabDrivePerformance
            // 
            tabDrivePerformance.Controls.Add(drivePerformance1);
            tabDrivePerformance.Location = new System.Drawing.Point(4, 39);
            tabDrivePerformance.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrivePerformance.Name = "tabDrivePerformance";
            tabDrivePerformance.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDrivePerformance.Size = new System.Drawing.Size(192, 57);
            tabDrivePerformance.TabIndex = 46;
            tabDrivePerformance.Text = "Drive Performance";
            tabDrivePerformance.UseVisualStyleBackColor = true;
            // 
            // drivePerformance1
            // 
            drivePerformance1.Dock = System.Windows.Forms.DockStyle.Fill;
            drivePerformance1.Location = new System.Drawing.Point(1, 2);
            drivePerformance1.Margin = new System.Windows.Forms.Padding(1);
            drivePerformance1.Name = "drivePerformance1";
            drivePerformance1.Size = new System.Drawing.Size(190, 53);
            drivePerformance1.TabIndex = 0;
            // 
            // tabRunningJobs
            // 
            tabRunningJobs.Controls.Add(runningJobs1);
            tabRunningJobs.Location = new System.Drawing.Point(4, 39);
            tabRunningJobs.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRunningJobs.Name = "tabRunningJobs";
            tabRunningJobs.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabRunningJobs.Size = new System.Drawing.Size(192, 57);
            tabRunningJobs.TabIndex = 47;
            tabRunningJobs.Text = "Running Jobs";
            tabRunningJobs.UseVisualStyleBackColor = true;
            // 
            // runningJobs1
            // 
            runningJobs1.Dock = System.Windows.Forms.DockStyle.Fill;
            runningJobs1.Location = new System.Drawing.Point(1, 2);
            runningJobs1.Margin = new System.Windows.Forms.Padding(1);
            runningJobs1.MinimumDuration = 60;
            runningJobs1.Name = "runningJobs1";
            runningJobs1.Size = new System.Drawing.Size(190, 53);
            runningJobs1.TabIndex = 0;
            // 
            // tabCustomReport
            // 
            tabCustomReport.Controls.Add(customReportView1);
            tabCustomReport.Location = new System.Drawing.Point(4, 39);
            tabCustomReport.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCustomReport.Name = "tabCustomReport";
            tabCustomReport.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabCustomReport.Size = new System.Drawing.Size(192, 57);
            tabCustomReport.TabIndex = 48;
            tabCustomReport.Text = "Report";
            tabCustomReport.UseVisualStyleBackColor = true;
            // 
            // customReportView1
            // 
            customReportView1.AutoScroll = true;
            customReportView1.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            customReportView1.Dock = System.Windows.Forms.DockStyle.Fill;
            customReportView1.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            customReportView1.Location = new System.Drawing.Point(1, 2);
            customReportView1.Margin = new System.Windows.Forms.Padding(1);
            customReportView1.Name = "customReportView1";
            customReportView1.Report = null;
            customReportView1.Size = new System.Drawing.Size(190, 53);
            customReportView1.TabIndex = 0;
            // 
            // tabTableSize
            // 
            tabTableSize.Controls.Add(tableSize1);
            tabTableSize.Location = new System.Drawing.Point(4, 39);
            tabTableSize.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTableSize.Name = "tabTableSize";
            tabTableSize.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTableSize.Size = new System.Drawing.Size(192, 57);
            tabTableSize.TabIndex = 49;
            tabTableSize.Text = "Table Size";
            tabTableSize.UseVisualStyleBackColor = true;
            // 
            // tableSize1
            // 
            tableSize1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableSize1.Location = new System.Drawing.Point(1, 2);
            tableSize1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tableSize1.Name = "tableSize1";
            tableSize1.Size = new System.Drawing.Size(190, 53);
            tableSize1.TabIndex = 0;
            // 
            // tabTopQueries
            // 
            tabTopQueries.Controls.Add(queryStoreTop);
            tabTopQueries.Location = new System.Drawing.Point(4, 39);
            tabTopQueries.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTopQueries.Name = "tabTopQueries";
            tabTopQueries.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabTopQueries.Size = new System.Drawing.Size(192, 57);
            tabTopQueries.TabIndex = 50;
            tabTopQueries.Text = "Top Queries (Query Store)";
            tabTopQueries.UseVisualStyleBackColor = true;
            // 
            // queryStoreTop
            // 
            queryStoreTop.Dock = System.Windows.Forms.DockStyle.Fill;
            queryStoreTop.Location = new System.Drawing.Point(1, 2);
            queryStoreTop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            queryStoreTop.Name = "queryStoreTop";
            queryStoreTop.PlanHash = null;
            queryStoreTop.QueryHash = null;
            queryStoreTop.Size = new System.Drawing.Size(190, 53);
            queryStoreTop.TabIndex = 0;
            queryStoreTop.UseGlobalTime = true;
            // 
            // tabQueryStoreForcedPlans
            // 
            tabQueryStoreForcedPlans.Controls.Add(queryStoreForcedPlans1);
            tabQueryStoreForcedPlans.Location = new System.Drawing.Point(4, 39);
            tabQueryStoreForcedPlans.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabQueryStoreForcedPlans.Name = "tabQueryStoreForcedPlans";
            tabQueryStoreForcedPlans.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabQueryStoreForcedPlans.Size = new System.Drawing.Size(192, 57);
            tabQueryStoreForcedPlans.TabIndex = 51;
            tabQueryStoreForcedPlans.Text = "Forced Plans (Query Store)";
            tabQueryStoreForcedPlans.UseVisualStyleBackColor = true;
            // 
            // queryStoreForcedPlans1
            // 
            queryStoreForcedPlans1.Dock = System.Windows.Forms.DockStyle.Fill;
            queryStoreForcedPlans1.Location = new System.Drawing.Point(1, 2);
            queryStoreForcedPlans1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            queryStoreForcedPlans1.Name = "queryStoreForcedPlans1";
            queryStoreForcedPlans1.Size = new System.Drawing.Size(190, 53);
            queryStoreForcedPlans1.TabIndex = 0;
            // 
            // tabServerServices
            // 
            tabServerServices.Controls.Add(serverServices1);
            tabServerServices.Location = new System.Drawing.Point(4, 39);
            tabServerServices.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabServerServices.Name = "tabServerServices";
            tabServerServices.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabServerServices.Size = new System.Drawing.Size(192, 57);
            tabServerServices.TabIndex = 52;
            tabServerServices.Text = "Server Services";
            tabServerServices.UseVisualStyleBackColor = true;
            // 
            // serverServices1
            // 
            serverServices1.Dock = System.Windows.Forms.DockStyle.Fill;
            serverServices1.Location = new System.Drawing.Point(1, 2);
            serverServices1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            serverServices1.Name = "serverServices1";
            serverServices1.Size = new System.Drawing.Size(190, 53);
            serverServices1.TabIndex = 0;
            // 
            // tabDeletedInstances
            // 
            tabDeletedInstances.Controls.Add(deletedInstances1);
            tabDeletedInstances.Location = new System.Drawing.Point(4, 39);
            tabDeletedInstances.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDeletedInstances.Name = "tabDeletedInstances";
            tabDeletedInstances.Padding = new System.Windows.Forms.Padding(1, 2, 1, 2);
            tabDeletedInstances.Size = new System.Drawing.Size(192, 57);
            tabDeletedInstances.TabIndex = 53;
            tabDeletedInstances.Text = "Recycle Bin";
            tabDeletedInstances.UseVisualStyleBackColor = true;
            // 
            // deletedInstances1
            // 
            deletedInstances1.Dock = System.Windows.Forms.DockStyle.Fill;
            deletedInstances1.Location = new System.Drawing.Point(1, 2);
            deletedInstances1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            deletedInstances1.Name = "deletedInstances1";
            deletedInstances1.Size = new System.Drawing.Size(190, 53);
            deletedInstances1.TabIndex = 0;
            // 
            // refresh1
            // 
            refresh1.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            refresh1.Dock = System.Windows.Forms.DockStyle.Fill;
            refresh1.Font = new System.Drawing.Font("Segoe UI", 13.8F);
            refresh1.ForeColor = System.Drawing.Color.White;
            refresh1.Location = new System.Drawing.Point(0, 0);
            refresh1.Margin = new System.Windows.Forms.Padding(2);
            refresh1.Name = "refresh1";
            refresh1.Size = new System.Drawing.Size(1586, 998);
            refresh1.TabIndex = 1;
            refresh1.Visible = false;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn1.DataPropertyName = "DB";
            dataGridViewTextBoxColumn1.HeaderText = "DB";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn2.DataPropertyName = "SnapshotDate";
            dataGridViewTextBoxColumn2.HeaderText = "Snapshot Date";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn3.DataPropertyName = "ValidatedDate";
            dataGridViewTextBoxColumn3.HeaderText = "Validated Date";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn4.DataPropertyName = "ValidForDays";
            dataGridViewTextBoxColumn4.HeaderText = "Valid For (Days)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn5.DataPropertyName = "DaysSinceValidation";
            dataGridViewTextBoxColumn5.HeaderText = "Days Since Validation";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn6.DataPropertyName = "Created";
            dataGridViewTextBoxColumn6.HeaderText = "Created";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn7.DataPropertyName = "Modified";
            dataGridViewTextBoxColumn7.HeaderText = "Modified";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn8.DataPropertyName = "Dropped";
            dataGridViewTextBoxColumn8.HeaderText = "Dropped";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn9.DataPropertyName = "ObjectName";
            dataGridViewTextBoxColumn9.HeaderText = "Object Name";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn10.DataPropertyName = "SchemaName";
            dataGridViewTextBoxColumn10.HeaderText = "Schema Name";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn11.DataPropertyName = "Action";
            dataGridViewTextBoxColumn11.HeaderText = "Action";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn12.DataPropertyName = "ObjectName";
            dataGridViewTextBoxColumn12.HeaderText = "Object Name";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 125;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn13.DataPropertyName = "SchemaName";
            dataGridViewTextBoxColumn13.HeaderText = "Schema Name";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 125;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn14.HeaderText = "Object Type";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 125;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn15.DataPropertyName = "SnapshotValidFrom";
            dataGridViewTextBoxColumn15.HeaderText = "Snapshot Valid From";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 125;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn16.DataPropertyName = "SnapshotValidTo";
            dataGridViewTextBoxColumn16.HeaderText = "Snapshot Valid To";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 125;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn17.DataPropertyName = "ObjectDateCreated";
            dataGridViewTextBoxColumn17.HeaderText = "Date Created";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            dataGridViewTextBoxColumn17.Width = 125;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn18.DataPropertyName = "ObjectDateModified";
            dataGridViewTextBoxColumn18.HeaderText = "Date Modified";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            dataGridViewTextBoxColumn18.Width = 125;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn19.HeaderText = "Instance";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            dataGridViewTextBoxColumn19.Width = 90;
            // 
            // dataGridViewTextBoxColumn20
            // 
            dataGridViewTextBoxColumn20.DataPropertyName = "ErrorDate";
            dataGridViewTextBoxColumn20.HeaderText = "Date";
            dataGridViewTextBoxColumn20.MinimumWidth = 6;
            dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            dataGridViewTextBoxColumn20.ReadOnly = true;
            dataGridViewTextBoxColumn20.Width = 67;
            // 
            // dataGridViewTextBoxColumn21
            // 
            dataGridViewTextBoxColumn21.DataPropertyName = "ErrorSource";
            dataGridViewTextBoxColumn21.HeaderText = "Source";
            dataGridViewTextBoxColumn21.MinimumWidth = 6;
            dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            dataGridViewTextBoxColumn21.ReadOnly = true;
            dataGridViewTextBoxColumn21.Width = 82;
            // 
            // dataGridViewTextBoxColumn22
            // 
            dataGridViewTextBoxColumn22.DataPropertyName = "ErrorContext";
            dataGridViewTextBoxColumn22.HeaderText = "Error Context";
            dataGridViewTextBoxColumn22.MinimumWidth = 6;
            dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            dataGridViewTextBoxColumn22.ReadOnly = true;
            dataGridViewTextBoxColumn22.Width = 120;
            // 
            // dataGridViewTextBoxColumn23
            // 
            dataGridViewTextBoxColumn23.DataPropertyName = "ErrorMessage";
            dataGridViewTextBoxColumn23.HeaderText = "Message";
            dataGridViewTextBoxColumn23.MinimumWidth = 6;
            dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            dataGridViewTextBoxColumn23.ReadOnly = true;
            dataGridViewTextBoxColumn23.Width = 94;
            // 
            // autoRefreshTimer
            // 
            autoRefreshTimer.Interval = 60000;
            autoRefreshTimer.Tick += AutoRefresh;
            // 
            // Main
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1955, 1028);
            Controls.Add(splitMain);
            Controls.Add(menuStrip1);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Main";
            Text = "DBA Dash";
            WindowState = System.Windows.Forms.FormWindowState.Minimized;
            Load += Main_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            searchLayout.ResumeLayout(false);
            searchLayout.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            tabs.ResumeLayout(false);
            tabSnapshotsSummary.ResumeLayout(false);
            tabSchema.ResumeLayout(false);
            splitSchemaSnapshot.Panel1.ResumeLayout(false);
            splitSchemaSnapshot.Panel1.PerformLayout();
            splitSchemaSnapshot.Panel2.ResumeLayout(false);
            splitSchemaSnapshot.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitSchemaSnapshot).EndInit();
            splitSchemaSnapshot.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gvHistory).EndInit();
            tabTags.ResumeLayout(false);
            tabDrives.ResumeLayout(false);
            tabBackups.ResumeLayout(false);
            tabLogShipping.ResumeLayout(false);
            tabJobs.ResumeLayout(false);
            tabSummary.ResumeLayout(false);
            tabFiles.ResumeLayout(false);
            tabLastGood.ResumeLayout(false);
            tabPerformance.ResumeLayout(false);
            tabDBADashErrorLog.ResumeLayout(false);
            tabCollectionDates.ResumeLayout(false);
            tabPerformanceSummary.ResumeLayout(false);
            tabInfo.ResumeLayout(false);
            tabHardware.ResumeLayout(false);
            tabSQLPatching.ResumeLayout(false);
            tabInstanceConfig.ResumeLayout(false);
            tabSlowQueries.ResumeLayout(false);
            tabTraceFlags.ResumeLayout(false);
            tabSQLAgentAlerts.ResumeLayout(false);
            tabDrivers.ResumeLayout(false);
            tabDBSpace.ResumeLayout(false);
            tabAzureSummary.ResumeLayout(false);
            tabAzureDB.ResumeLayout(false);
            tabServiceObjectives.ResumeLayout(false);
            tabDBConfiguration.ResumeLayout(false);
            tabDBOptions.ResumeLayout(false);
            tabTempDB.ResumeLayout(false);
            tabCustomChecks.ResumeLayout(false);
            tabPC.ResumeLayout(false);
            tabObjectExecutionSummary.ResumeLayout(false);
            tabWaits.ResumeLayout(false);
            tabMirroring.ResumeLayout(false);
            tabJobDDL.ResumeLayout(false);
            tabAG.ResumeLayout(false);
            tabQS.ResumeLayout(false);
            tabRG.ResumeLayout(false);
            tabAzureDBesourceGovernance.ResumeLayout(false);
            tabRunningQueries.ResumeLayout(false);
            tabMemory.ResumeLayout(false);
            tabJobStats.ResumeLayout(false);
            tabDBADash.ResumeLayout(false);
            tabIdentityColumns.ResumeLayout(false);
            tabOSLoadedModules.ResumeLayout(false);
            tabJobTimeline.ResumeLayout(false);
            tabDrivePerformance.ResumeLayout(false);
            tabRunningJobs.ResumeLayout(false);
            tabCustomReport.ResumeLayout(false);
            tabTableSize.ResumeLayout(false);
            tabTopQueries.ResumeLayout(false);
            tabQueryStoreForcedPlans.ResumeLayout(false);
            tabServerServices.ResumeLayout(false);
            tabDeletedInstances.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TreeView tv1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.SplitContainer splitMain;
        private ThemedTabControl tabs;
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
        private DrivesControl drivesControl1;
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
        private System.Windows.Forms.TabPage tabSQLAgentAlerts;
        private Changes.SQLAgentAlerts alerts1;
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
        private System.Windows.Forms.TabPage tabDrivePerformance;
        private Performance.DrivePerformance drivePerformance1;
        private System.Windows.Forms.ToolStripMenuItem setAutoRefreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minuteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minutesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minutesToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem customToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.Timer autoRefreshTimer;
        private System.Windows.Forms.ToolStripMenuItem secondsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem themeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem defaultToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem darkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem whiteToolStripMenuItem;
        private System.Windows.Forms.TabPage tabRunningJobs;
        private AgentJobs.RunningJobs runningJobs1;
        private System.Windows.Forms.TabPage tabCustomReport;
        private CustomReports.CustomReportView customReportView1;
        private System.Windows.Forms.TabPage tabTableSize;
        private DBFiles.TableSize tableSize1;
        private System.Windows.Forms.TabPage tabTopQueries;
        private Performance.QueryStoreTopQueries queryStoreTop;
        private System.Windows.Forms.TabPage tabQueryStoreForcedPlans;
        private Performance.QueryStoreForcedPlans queryStoreForcedPlans1;
        private System.Windows.Forms.TabPage tabServerServices;
        private CustomReports.ServerServices serverServices1;
        private System.Windows.Forms.ToolStripMenuItem repoSettingsToolStripMenuItem;
        private System.Windows.Forms.TabPage tabDeletedInstances;
        private DeletedInstances deletedInstances1;
        private System.Windows.Forms.ToolStripMenuItem externalDiffToolToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel searchLayout;
        private System.Windows.Forms.ToolStripMenuItem tsAlert;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ToolStripMenuItem desktopNotificationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dateTimeFormatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setTimeFormatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setDateTimeFormatToolStripMenuItem;
        private Performance.Performance performance1;
        private DateRangeToolStripMenuItem tsDateRange;
    }
}