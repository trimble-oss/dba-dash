namespace DBAChecksGUI.AgentJobs
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
            this.LastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsLastFail = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.TimeSinceLastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TimeSinceLastSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FailCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SucceedCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FailCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SucceedCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JobStepFails24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JobStepFails7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvJobs
            // 
            this.dgvJobs.AllowUserToAddRows = false;
            this.dgvJobs.AllowUserToDeleteRows = false;
            this.dgvJobs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvJobs.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgvJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvJobs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.name,
            this.LastFail,
            this.IsLastFail,
            this.TimeSinceLastFail,
            this.TimeSinceLastSucceeded,
            this.FailCount24Hrs,
            this.SucceedCount24Hrs,
            this.FailCount7Days,
            this.SucceedCount7Days,
            this.JobStepFails24Hrs,
            this.JobStepFails7Days,
            this.MaxDurationSec,
            this.AvgDurationSec,
            this.ConfiguredLevel,
            this.Configure});
            this.dgvJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvJobs.Location = new System.Drawing.Point(0, 31);
            this.dgvJobs.Name = "dgvJobs";
            this.dgvJobs.ReadOnly = true;
            this.dgvJobs.RowHeadersVisible = false;
            this.dgvJobs.RowHeadersWidth = 51;
            this.dgvJobs.RowTemplate.Height = 24;
            this.dgvJobs.Size = new System.Drawing.Size(606, 349);
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
            this.name.Width = 93;
            // 
            // LastFail
            // 
            this.LastFail.DataPropertyName = "LastFail";
            this.LastFail.HeaderText = "Last Fail";
            this.LastFail.MinimumWidth = 6;
            this.LastFail.Name = "LastFail";
            this.LastFail.ReadOnly = true;
            this.LastFail.Width = 64;
            // 
            // IsLastFail
            // 
            this.IsLastFail.DataPropertyName = "IsLastFail";
            this.IsLastFail.HeaderText = "Is Last Fail?";
            this.IsLastFail.MinimumWidth = 6;
            this.IsLastFail.Name = "IsLastFail";
            this.IsLastFail.ReadOnly = true;
            this.IsLastFail.Width = 80;
            // 
            // TimeSinceLastFail
            // 
            this.TimeSinceLastFail.DataPropertyName = "TimeSinceLastFail";
            this.TimeSinceLastFail.HeaderText = "Time Since Last Fail";
            this.TimeSinceLastFail.MinimumWidth = 6;
            this.TimeSinceLastFail.Name = "TimeSinceLastFail";
            this.TimeSinceLastFail.ReadOnly = true;
            this.TimeSinceLastFail.Width = 130;
            // 
            // TimeSinceLastSucceeded
            // 
            this.TimeSinceLastSucceeded.DataPropertyName = "TimeSinceLastSucceeded";
            this.TimeSinceLastSucceeded.HeaderText = "TimeSinceLastSucceded";
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
            // AvgDurationSec
            // 
            this.AvgDurationSec.DataPropertyName = "AvgDurationSec";
            this.AvgDurationSec.HeaderText = "Avg Duration (sec)";
            this.AvgDurationSec.MinimumWidth = 6;
            this.AvgDurationSec.Name = "AvgDurationSec";
            this.AvgDurationSec.ReadOnly = true;
            this.AvgDurationSec.Width = 142;
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
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.toolStripFilter,
            this.tsConfigure});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(606, 31);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripFilter
            // 
            this.toolStripFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalToolStripMenuItem,
            this.warningToolStripMenuItem,
            this.undefinedToolStripMenuItem,
            this.OKToolStripMenuItem});
            this.toolStripFilter.Image = global::DBAChecksGUI.Properties.Resources.FilterDropdown_16x;
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
            this.tsConfigure.Image = global::DBAChecksGUI.Properties.Resources.SettingsOutline_16x;
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
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBAChecksGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "toolStripButton1";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // AgentJobsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvJobs);
            this.Controls.Add(this.toolStrip1);
            this.Name = "AgentJobsControl";
            this.Size = new System.Drawing.Size(606, 380);
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvJobs;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFail;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
    }
}
