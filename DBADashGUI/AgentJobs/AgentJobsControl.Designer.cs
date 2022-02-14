namespace DBADashGUI.AgentJobs
{
    partial class AgentJobsControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dgvJobs = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsLastFail = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.TimeSinceLastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TimeSinceLastSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FailCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SucceedCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FailCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SucceedCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JobStepFails24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JobStepFails7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvgDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colHistory = new System.Windows.Forms.DataGridViewLinkColumn();
            this.tsJobs = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvJobHistory = new System.Windows.Forms.DataGridView();
            this.colRunDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStepID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStepName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMessageID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRunStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRunDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRunDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRetries = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colViewSteps = new System.Windows.Forms.DataGridViewLinkColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.jobStep1 = new DBADashGUI.AgentJobs.JobStep();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            this.tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.showJobStepsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.failedOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.tsJobName = new System.Windows.Forms.ToolStripLabel();
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
            this.dataGridViewTextBoxColumn24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn28 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).BeginInit();
            this.tsJobs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobHistory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvJobs
            // 
            this.dgvJobs.AllowUserToAddRows = false;
            this.dgvJobs.AllowUserToDeleteRows = false;
            this.dgvJobs.BackgroundColor = System.Drawing.Color.White;
            this.dgvJobs.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgvJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvJobs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.name,
            this.colEnabled,
            this.Description,
            this.LastFail,
            this.IsLastFail,
            this.TimeSinceLastFail,
            this.LastSucceeded,
            this.TimeSinceLastSucceeded,
            this.FailCount24Hrs,
            this.SucceedCount24Hrs,
            this.FailCount7Days,
            this.SucceedCount7Days,
            this.JobStepFails24Hrs,
            this.JobStepFails7Days,
            this.MaxDurationSec,
            this.colMaxDuration,
            this.AvgDurationSec,
            this.colAvgDuration,
            this.ConfiguredLevel,
            this.Configure,
            this.colHistory});
            this.dgvJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvJobs.Location = new System.Drawing.Point(0, 0);
            this.dgvJobs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvJobs.Name = "dgvJobs";
            this.dgvJobs.ReadOnly = true;
            this.dgvJobs.RowHeadersVisible = false;
            this.dgvJobs.RowHeadersWidth = 51;
            this.dgvJobs.RowTemplate.Height = 24;
            this.dgvJobs.Size = new System.Drawing.Size(2197, 216);
            this.dgvJobs.TabIndex = 0;
            this.dgvJobs.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvJobs_CellContentClick);
            this.dgvJobs.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvJobs_RowsAdded);
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "Instance";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // name
            // 
            this.name.DataPropertyName = "name";
            this.name.HeaderText = "Job Name";
            this.name.MinimumWidth = 6;
            this.name.Name = "name";
            this.name.ReadOnly = true;
            this.name.Width = 101;
            // 
            // colEnabled
            // 
            this.colEnabled.DataPropertyName = "enabled";
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.MinimumWidth = 6;
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.ReadOnly = true;
            this.colEnabled.Width = 125;
            // 
            // Description
            // 
            this.Description.DataPropertyName = "description";
            this.Description.HeaderText = "Description";
            this.Description.MinimumWidth = 6;
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 108;
            // 
            // LastFail
            // 
            this.LastFail.DataPropertyName = "LastFailed";
            this.LastFail.HeaderText = "Last Failed";
            this.LastFail.MinimumWidth = 6;
            this.LastFail.Name = "LastFail";
            this.LastFail.ReadOnly = true;
            this.LastFail.Width = 106;
            // 
            // IsLastFail
            // 
            this.IsLastFail.DataPropertyName = "IsLastFail";
            this.IsLastFail.HeaderText = "Is Last Fail?";
            this.IsLastFail.MinimumWidth = 6;
            this.IsLastFail.Name = "IsLastFail";
            this.IsLastFail.ReadOnly = true;
            this.IsLastFail.Width = 89;
            // 
            // TimeSinceLastFail
            // 
            this.TimeSinceLastFail.DataPropertyName = "TimeSinceLastFailed";
            this.TimeSinceLastFail.HeaderText = "Time Since Last Fail";
            this.TimeSinceLastFail.MinimumWidth = 6;
            this.TimeSinceLastFail.Name = "TimeSinceLastFail";
            this.TimeSinceLastFail.ReadOnly = true;
            this.TimeSinceLastFail.Width = 130;
            // 
            // LastSucceeded
            // 
            this.LastSucceeded.DataPropertyName = "LastSucceeded";
            this.LastSucceeded.HeaderText = "Last Succeeded";
            this.LastSucceeded.MinimumWidth = 6;
            this.LastSucceeded.Name = "LastSucceeded";
            this.LastSucceeded.ReadOnly = true;
            this.LastSucceeded.Width = 127;
            // 
            // TimeSinceLastSucceeded
            // 
            this.TimeSinceLastSucceeded.DataPropertyName = "TimeSinceLastSucceeded";
            this.TimeSinceLastSucceeded.HeaderText = "Time Since Last Succeded";
            this.TimeSinceLastSucceeded.MinimumWidth = 6;
            this.TimeSinceLastSucceeded.Name = "TimeSinceLastSucceeded";
            this.TimeSinceLastSucceeded.ReadOnly = true;
            this.TimeSinceLastSucceeded.Width = 193;
            // 
            // FailCount24Hrs
            // 
            this.FailCount24Hrs.DataPropertyName = "FailCount24Hrs";
            this.FailCount24Hrs.HeaderText = "Fail Count (24Hrs)";
            this.FailCount24Hrs.MinimumWidth = 6;
            this.FailCount24Hrs.Name = "FailCount24Hrs";
            this.FailCount24Hrs.ReadOnly = true;
            this.FailCount24Hrs.Width = 139;
            // 
            // SucceedCount24Hrs
            // 
            this.SucceedCount24Hrs.DataPropertyName = "SucceedCount24Hrs";
            this.SucceedCount24Hrs.HeaderText = "Succeed Count (24Hrs)";
            this.SucceedCount24Hrs.MinimumWidth = 6;
            this.SucceedCount24Hrs.Name = "SucceedCount24Hrs";
            this.SucceedCount24Hrs.ReadOnly = true;
            this.SucceedCount24Hrs.Width = 126;
            // 
            // FailCount7Days
            // 
            this.FailCount7Days.DataPropertyName = "FailCount7Days";
            this.FailCount7Days.HeaderText = "Fail Count (7 Days)";
            this.FailCount7Days.MinimumWidth = 6;
            this.FailCount7Days.Name = "FailCount7Days";
            this.FailCount7Days.ReadOnly = true;
            this.FailCount7Days.Width = 111;
            // 
            // SucceedCount7Days
            // 
            this.SucceedCount7Days.DataPropertyName = "SucceedCount7Days";
            this.SucceedCount7Days.HeaderText = "Succeed Count (7 Days)";
            this.SucceedCount7Days.MinimumWidth = 6;
            this.SucceedCount7Days.Name = "SucceedCount7Days";
            this.SucceedCount7Days.ReadOnly = true;
            this.SucceedCount7Days.Width = 141;
            // 
            // JobStepFails24Hrs
            // 
            this.JobStepFails24Hrs.DataPropertyName = "JobStepFails24Hrs";
            this.JobStepFails24Hrs.HeaderText = "Job Step Fails (24Hrs)";
            this.JobStepFails24Hrs.MinimumWidth = 6;
            this.JobStepFails24Hrs.Name = "JobStepFails24Hrs";
            this.JobStepFails24Hrs.ReadOnly = true;
            this.JobStepFails24Hrs.Width = 119;
            // 
            // JobStepFails7Days
            // 
            this.JobStepFails7Days.DataPropertyName = "JobStepFails7Days";
            this.JobStepFails7Days.HeaderText = "Job Step Fails (7 Days)";
            this.JobStepFails7Days.MinimumWidth = 6;
            this.JobStepFails7Days.Name = "JobStepFails7Days";
            this.JobStepFails7Days.ReadOnly = true;
            this.JobStepFails7Days.Width = 135;
            // 
            // MaxDurationSec
            // 
            this.MaxDurationSec.DataPropertyName = "MaxDurationSec";
            this.MaxDurationSec.HeaderText = "Max Duration (sec)";
            this.MaxDurationSec.MinimumWidth = 6;
            this.MaxDurationSec.Name = "MaxDurationSec";
            this.MaxDurationSec.ReadOnly = true;
            this.MaxDurationSec.Width = 143;
            // 
            // colMaxDuration
            // 
            this.colMaxDuration.DataPropertyName = "MaxDuration";
            this.colMaxDuration.HeaderText = "Max Duration";
            this.colMaxDuration.MinimumWidth = 6;
            this.colMaxDuration.Name = "colMaxDuration";
            this.colMaxDuration.ReadOnly = true;
            this.colMaxDuration.Width = 125;
            // 
            // AvgDurationSec
            // 
            this.AvgDurationSec.DataPropertyName = "AvgDurationSec";
            this.AvgDurationSec.HeaderText = "Avg Duration (sec)";
            this.AvgDurationSec.MinimumWidth = 6;
            this.AvgDurationSec.Name = "AvgDurationSec";
            this.AvgDurationSec.ReadOnly = true;
            this.AvgDurationSec.Width = 142;
            // 
            // colAvgDuration
            // 
            this.colAvgDuration.DataPropertyName = "AvgDuration";
            this.colAvgDuration.HeaderText = "Avg Duration";
            this.colAvgDuration.MinimumWidth = 6;
            this.colAvgDuration.Name = "colAvgDuration";
            this.colAvgDuration.ReadOnly = true;
            this.colAvgDuration.Width = 125;
            // 
            // ConfiguredLevel
            // 
            this.ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            this.ConfiguredLevel.HeaderText = "Configured Level";
            this.ConfiguredLevel.MinimumWidth = 6;
            this.ConfiguredLevel.Name = "ConfiguredLevel";
            this.ConfiguredLevel.ReadOnly = true;
            this.ConfiguredLevel.Width = 132;
            // 
            // Configure
            // 
            this.Configure.HeaderText = "Configure";
            this.Configure.MinimumWidth = 6;
            this.Configure.Name = "Configure";
            this.Configure.ReadOnly = true;
            this.Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Configure.Text = "Configure";
            this.Configure.UseColumnTextForLinkValue = true;
            this.Configure.Width = 98;
            // 
            // colHistory
            // 
            this.colHistory.HeaderText = "History";
            this.colHistory.MinimumWidth = 6;
            this.colHistory.Name = "colHistory";
            this.colHistory.ReadOnly = true;
            this.colHistory.Text = "View History";
            this.colHistory.UseColumnTextForLinkValue = true;
            this.colHistory.Width = 58;
            // 
            // tsJobs
            // 
            this.tsJobs.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tsJobs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.toolStripFilter,
            this.tsConfigure});
            this.tsJobs.Location = new System.Drawing.Point(0, 0);
            this.tsJobs.Name = "tsJobs";
            this.tsJobs.Size = new System.Drawing.Size(2197, 27);
            this.tsJobs.TabIndex = 3;
            this.tsJobs.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // toolStripFilter
            // 
            this.toolStripFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalToolStripMenuItem,
            this.warningToolStripMenuItem,
            this.undefinedToolStripMenuItem,
            this.OKToolStripMenuItem});
            this.toolStripFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripFilter.Name = "toolStripFilter";
            this.toolStripFilter.Size = new System.Drawing.Size(34, 24);
            this.toolStripFilter.Text = "Filter";
            // 
            // criticalToolStripMenuItem
            // 
            this.criticalToolStripMenuItem.CheckOnClick = true;
            this.criticalToolStripMenuItem.Name = "criticalToolStripMenuItem";
            this.criticalToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.criticalToolStripMenuItem.Text = "Critical";
            this.criticalToolStripMenuItem.Click += new System.EventHandler(this.criticalToolStripMenuItem_Click);
            // 
            // warningToolStripMenuItem
            // 
            this.warningToolStripMenuItem.CheckOnClick = true;
            this.warningToolStripMenuItem.Name = "warningToolStripMenuItem";
            this.warningToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.warningToolStripMenuItem.Text = "Warning";
            this.warningToolStripMenuItem.Click += new System.EventHandler(this.warningToolStripMenuItem_Click);
            // 
            // undefinedToolStripMenuItem
            // 
            this.undefinedToolStripMenuItem.CheckOnClick = true;
            this.undefinedToolStripMenuItem.Name = "undefinedToolStripMenuItem";
            this.undefinedToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.undefinedToolStripMenuItem.Text = "Undefined";
            this.undefinedToolStripMenuItem.Click += new System.EventHandler(this.undefinedToolStripMenuItem_Click);
            // 
            // OKToolStripMenuItem
            // 
            this.OKToolStripMenuItem.CheckOnClick = true;
            this.OKToolStripMenuItem.Name = "OKToolStripMenuItem";
            this.OKToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.OKToolStripMenuItem.Text = "OK";
            this.OKToolStripMenuItem.Click += new System.EventHandler(this.OKToolStripMenuItem_Click);
            // 
            // tsConfigure
            // 
            this.tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureInstanceThresholdsToolStripMenuItem,
            this.configureRootThresholdsToolStripMenuItem});
            this.tsConfigure.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsConfigure.Name = "tsConfigure";
            this.tsConfigure.Size = new System.Drawing.Size(34, 24);
            this.tsConfigure.Text = "Configure";
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            this.configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            this.configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            this.configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            this.configureInstanceThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureInstanceThresholdsToolStripMenuItem_Click);
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            this.configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            this.configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            this.configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            this.configureRootThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureRootThresholdsToolStripMenuItem_Click);
            // 
            // dgvJobHistory
            // 
            this.dgvJobHistory.AllowUserToAddRows = false;
            this.dgvJobHistory.AllowUserToDeleteRows = false;
            this.dgvJobHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvJobHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvJobHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRunDateTime,
            this.colStepID,
            this.colStepName,
            this.colMessageID,
            this.colSeverity,
            this.colMessage,
            this.colRunStatus,
            this.colRunDurationSec,
            this.colRunDuration,
            this.colRetries,
            this.colViewSteps});
            this.dgvJobHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvJobHistory.Location = new System.Drawing.Point(0, 27);
            this.dgvJobHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvJobHistory.Name = "dgvJobHistory";
            this.dgvJobHistory.ReadOnly = true;
            this.dgvJobHistory.RowHeadersVisible = false;
            this.dgvJobHistory.RowHeadersWidth = 51;
            this.dgvJobHistory.RowTemplate.Height = 24;
            this.dgvJobHistory.Size = new System.Drawing.Size(2197, 200);
            this.dgvJobHistory.TabIndex = 4;
            this.dgvJobHistory.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvJobHistory_CellContentClick);
            // 
            // colRunDateTime
            // 
            this.colRunDateTime.DataPropertyName = "RunDateTime";
            this.colRunDateTime.HeaderText = "Date/Time";
            this.colRunDateTime.MinimumWidth = 6;
            this.colRunDateTime.Name = "colRunDateTime";
            this.colRunDateTime.ReadOnly = true;
            this.colRunDateTime.Width = 125;
            // 
            // colStepID
            // 
            this.colStepID.DataPropertyName = "step_id";
            this.colStepID.HeaderText = "Step ID";
            this.colStepID.MinimumWidth = 6;
            this.colStepID.Name = "colStepID";
            this.colStepID.ReadOnly = true;
            this.colStepID.Width = 125;
            // 
            // colStepName
            // 
            this.colStepName.DataPropertyName = "step_name";
            this.colStepName.HeaderText = "Step Name";
            this.colStepName.MinimumWidth = 6;
            this.colStepName.Name = "colStepName";
            this.colStepName.ReadOnly = true;
            this.colStepName.Width = 125;
            // 
            // colMessageID
            // 
            this.colMessageID.DataPropertyName = "sql_message_id";
            this.colMessageID.HeaderText = "Message ID";
            this.colMessageID.MinimumWidth = 6;
            this.colMessageID.Name = "colMessageID";
            this.colMessageID.ReadOnly = true;
            this.colMessageID.Width = 125;
            // 
            // colSeverity
            // 
            this.colSeverity.DataPropertyName = "sql_severity";
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.MinimumWidth = 6;
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.ReadOnly = true;
            this.colSeverity.Width = 125;
            // 
            // colMessage
            // 
            this.colMessage.DataPropertyName = "message";
            this.colMessage.HeaderText = "Message";
            this.colMessage.MinimumWidth = 6;
            this.colMessage.Name = "colMessage";
            this.colMessage.ReadOnly = true;
            this.colMessage.Width = 125;
            // 
            // colRunStatus
            // 
            this.colRunStatus.DataPropertyName = "run_status_description";
            this.colRunStatus.HeaderText = "Status";
            this.colRunStatus.MinimumWidth = 6;
            this.colRunStatus.Name = "colRunStatus";
            this.colRunStatus.ReadOnly = true;
            this.colRunStatus.Width = 125;
            // 
            // colRunDurationSec
            // 
            this.colRunDurationSec.DataPropertyName = "RunDurationSec";
            this.colRunDurationSec.HeaderText = "Duration (sec)";
            this.colRunDurationSec.MinimumWidth = 6;
            this.colRunDurationSec.Name = "colRunDurationSec";
            this.colRunDurationSec.ReadOnly = true;
            this.colRunDurationSec.Width = 125;
            // 
            // colRunDuration
            // 
            this.colRunDuration.DataPropertyName = "RunDuration";
            this.colRunDuration.HeaderText = "Run Duration";
            this.colRunDuration.MinimumWidth = 6;
            this.colRunDuration.Name = "colRunDuration";
            this.colRunDuration.ReadOnly = true;
            this.colRunDuration.Width = 125;
            // 
            // colRetries
            // 
            this.colRetries.DataPropertyName = "retries_attempted";
            this.colRetries.HeaderText = "Retries";
            this.colRetries.MinimumWidth = 6;
            this.colRetries.Name = "colRetries";
            this.colRetries.ReadOnly = true;
            this.colRetries.Width = 125;
            // 
            // colViewSteps
            // 
            this.colViewSteps.HeaderText = "Steps";
            this.colViewSteps.MinimumWidth = 6;
            this.colViewSteps.Name = "colViewSteps";
            this.colViewSteps.ReadOnly = true;
            this.colViewSteps.Text = "View Steps";
            this.colViewSteps.UseColumnTextForLinkValue = true;
            this.colViewSteps.Width = 125;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvJobs);
            this.splitContainer1.Panel1.Controls.Add(this.jobStep1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvJobHistory);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer1.Size = new System.Drawing.Size(2197, 448);
            this.splitContainer1.SplitterDistance = 216;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 5;
            // 
            // jobStep1
            // 
            this.jobStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobStep1.Location = new System.Drawing.Point(0, 0);
            this.jobStep1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.jobStep1.Name = "jobStep1";
            this.jobStep1.Size = new System.Drawing.Size(2197, 216);
            this.jobStep1.TabIndex = 1;
            this.jobStep1.Visible = false;
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefreshHistory,
            this.tsCopyHistory,
            this.tsExcelHistory,
            this.tsFilter,
            this.tsBack,
            this.tsJobName});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(2197, 27);
            this.toolStrip2.TabIndex = 5;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsRefreshHistory
            // 
            this.tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHistory.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHistory.Name = "tsRefreshHistory";
            this.tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshHistory.Text = "Refresh";
            this.tsRefreshHistory.Click += new System.EventHandler(this.tsRefreshHistory_Click);
            // 
            // tsCopyHistory
            // 
            this.tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyHistory.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyHistory.Name = "tsCopyHistory";
            this.tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            this.tsCopyHistory.Text = "Copy";
            this.tsCopyHistory.Click += new System.EventHandler(this.tsCopyHistory_Click);
            // 
            // tsExcelHistory
            // 
            this.tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcelHistory.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcelHistory.Name = "tsExcelHistory";
            this.tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            this.tsExcelHistory.Text = "Export Excel";
            this.tsExcelHistory.Click += new System.EventHandler(this.tsExcelHistory_Click);
            // 
            // tsFilter
            // 
            this.tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showJobStepsToolStripMenuItem,
            this.failedOnlyToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 24);
            this.tsFilter.Text = "Filter";
            // 
            // showJobStepsToolStripMenuItem
            // 
            this.showJobStepsToolStripMenuItem.CheckOnClick = true;
            this.showJobStepsToolStripMenuItem.Name = "showJobStepsToolStripMenuItem";
            this.showJobStepsToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            this.showJobStepsToolStripMenuItem.Text = "Show Job Steps";
            this.showJobStepsToolStripMenuItem.Click += new System.EventHandler(this.showJobStepsToolStripMenuItem_Click);
            // 
            // failedOnlyToolStripMenuItem
            // 
            this.failedOnlyToolStripMenuItem.CheckOnClick = true;
            this.failedOnlyToolStripMenuItem.Name = "failedOnlyToolStripMenuItem";
            this.failedOnlyToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            this.failedOnlyToolStripMenuItem.Text = "Failed Only";
            this.failedOnlyToolStripMenuItem.Click += new System.EventHandler(this.failedOnlyToolStripMenuItem_Click);
            // 
            // tsBack
            // 
            this.tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBack.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBack.Name = "tsBack";
            this.tsBack.Size = new System.Drawing.Size(29, 24);
            this.tsBack.Text = "Back";
            this.tsBack.Click += new System.EventHandler(this.tsBack_Click);
            // 
            // tsJobName
            // 
            this.tsJobName.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsJobName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tsJobName.ForeColor = System.Drawing.Color.Black;
            this.tsJobName.Name = "tsJobName";
            this.tsJobName.Size = new System.Drawing.Size(80, 24);
            this.tsJobName.Text = "Job Name";
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "name";
            this.dataGridViewTextBoxColumn2.HeaderText = "Job Name";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 101;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "LastFailed";
            this.dataGridViewTextBoxColumn3.HeaderText = "Last Failed";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 106;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "TimeSinceLastFailed";
            this.dataGridViewTextBoxColumn4.HeaderText = "Time Since Last Fail";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 130;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "LastSucceeded";
            this.dataGridViewTextBoxColumn5.HeaderText = "Last Succeeded";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 127;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "TimeSinceLastSucceeded";
            this.dataGridViewTextBoxColumn6.HeaderText = "TimeSinceLastSucceded";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 193;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "FailCount24Hrs";
            this.dataGridViewTextBoxColumn7.HeaderText = "Fail Count (24Hrs)";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 139;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "SucceedCount24Hrs";
            this.dataGridViewTextBoxColumn8.HeaderText = "Succeed Count (24Hrs)";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 126;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.DataPropertyName = "FailCount7Days";
            this.dataGridViewTextBoxColumn9.HeaderText = "Fail Count (7 Days)";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 111;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.DataPropertyName = "SucceedCount7Days";
            this.dataGridViewTextBoxColumn10.HeaderText = "Succeed Count (7 Days)";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 141;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.DataPropertyName = "JobStepFails24Hrs";
            this.dataGridViewTextBoxColumn11.HeaderText = "Job Step Fails (24Hrs)";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 119;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.DataPropertyName = "JobStepFails7Days";
            this.dataGridViewTextBoxColumn12.HeaderText = "Job Step Fails (7 Days)";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 135;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.DataPropertyName = "MaxDurationSec";
            this.dataGridViewTextBoxColumn13.HeaderText = "Max Duration (sec)";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 143;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.DataPropertyName = "AvgDurationSec";
            this.dataGridViewTextBoxColumn14.HeaderText = "Avg Duration (sec)";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 142;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.DataPropertyName = "ConfiguredLevel";
            this.dataGridViewTextBoxColumn15.HeaderText = "Configured Level";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 132;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.DataPropertyName = "RunDateTime";
            this.dataGridViewTextBoxColumn16.HeaderText = "Date/Time";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.Width = 125;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.DataPropertyName = "step_id";
            this.dataGridViewTextBoxColumn17.HeaderText = "Step ID";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.Width = 125;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.DataPropertyName = "step_name";
            this.dataGridViewTextBoxColumn18.HeaderText = "Step Name";
            this.dataGridViewTextBoxColumn18.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            this.dataGridViewTextBoxColumn18.Width = 125;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.DataPropertyName = "sql_message_id";
            this.dataGridViewTextBoxColumn19.HeaderText = "Message ID";
            this.dataGridViewTextBoxColumn19.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            this.dataGridViewTextBoxColumn19.Width = 125;
            // 
            // dataGridViewTextBoxColumn20
            // 
            this.dataGridViewTextBoxColumn20.DataPropertyName = "sql_severity";
            this.dataGridViewTextBoxColumn20.HeaderText = "Severity";
            this.dataGridViewTextBoxColumn20.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            this.dataGridViewTextBoxColumn20.Width = 125;
            // 
            // dataGridViewTextBoxColumn21
            // 
            this.dataGridViewTextBoxColumn21.DataPropertyName = "message";
            this.dataGridViewTextBoxColumn21.HeaderText = "Message";
            this.dataGridViewTextBoxColumn21.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            this.dataGridViewTextBoxColumn21.Width = 125;
            // 
            // dataGridViewTextBoxColumn22
            // 
            this.dataGridViewTextBoxColumn22.DataPropertyName = "run_status_description";
            this.dataGridViewTextBoxColumn22.HeaderText = "Status";
            this.dataGridViewTextBoxColumn22.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            this.dataGridViewTextBoxColumn22.Width = 125;
            // 
            // dataGridViewTextBoxColumn23
            // 
            this.dataGridViewTextBoxColumn23.DataPropertyName = "RunDurationSec";
            this.dataGridViewTextBoxColumn23.HeaderText = "Duration (sec)";
            this.dataGridViewTextBoxColumn23.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            this.dataGridViewTextBoxColumn23.Width = 125;
            // 
            // dataGridViewTextBoxColumn24
            // 
            this.dataGridViewTextBoxColumn24.DataPropertyName = "retries_attempted";
            this.dataGridViewTextBoxColumn24.HeaderText = "Retries";
            this.dataGridViewTextBoxColumn24.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            this.dataGridViewTextBoxColumn24.Width = 125;
            // 
            // dataGridViewTextBoxColumn25
            // 
            this.dataGridViewTextBoxColumn25.DataPropertyName = "retries_attempted";
            this.dataGridViewTextBoxColumn25.HeaderText = "Retries";
            this.dataGridViewTextBoxColumn25.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            this.dataGridViewTextBoxColumn25.Width = 125;
            // 
            // dataGridViewTextBoxColumn26
            // 
            this.dataGridViewTextBoxColumn26.DataPropertyName = "RunDurationSec";
            this.dataGridViewTextBoxColumn26.HeaderText = "Duration (sec)";
            this.dataGridViewTextBoxColumn26.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            this.dataGridViewTextBoxColumn26.Width = 125;
            // 
            // dataGridViewTextBoxColumn27
            // 
            this.dataGridViewTextBoxColumn27.DataPropertyName = "RunDuration";
            this.dataGridViewTextBoxColumn27.HeaderText = "Run Duration";
            this.dataGridViewTextBoxColumn27.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            this.dataGridViewTextBoxColumn27.Width = 125;
            // 
            // dataGridViewTextBoxColumn28
            // 
            this.dataGridViewTextBoxColumn28.DataPropertyName = "retries_attempted";
            this.dataGridViewTextBoxColumn28.HeaderText = "Retries";
            this.dataGridViewTextBoxColumn28.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            this.dataGridViewTextBoxColumn28.Width = 125;
            // 
            // AgentJobsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tsJobs);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "AgentJobsControl";
            this.Size = new System.Drawing.Size(2197, 475);
            this.Load += new System.EventHandler(this.AgentJobsControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).EndInit();
            this.tsJobs.ResumeLayout(false);
            this.tsJobs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobHistory)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvJobs;
        private System.Windows.Forms.ToolStrip tsJobs;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
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
        private System.Windows.Forms.DataGridView dgvJobHistory;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem showJobStepsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn24;
        private System.Windows.Forms.ToolStripLabel tsJobName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn25;
        private System.Windows.Forms.ToolStripMenuItem failedOnlyToolStripMenuItem;
        private JobStep jobStep1;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunDateTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStepID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStepName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMessageID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMessage;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRetries;
        private System.Windows.Forms.DataGridViewLinkColumn colViewSteps;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn26;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn27;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn28;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFail;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewLinkColumn colHistory;
    }
}
