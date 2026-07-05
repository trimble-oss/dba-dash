using DBADashGUI.CustomReports;

namespace DBADashGUI.CollectionDates
{
    partial class CollectionDates
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionDates));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsTriggerMenu = new System.Windows.Forms.ToolStripDropDownButton();
            tsTriggerWarningAndCritical = new System.Windows.Forms.ToolStripMenuItem();
            tsTriggerSelected = new System.Windows.Forms.ToolStripMenuItem();
            tsTriggerAll = new System.Windows.Forms.ToolStripMenuItem();
            dgvCollectionDates = new DBADashDataGridView();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Reference = new System.Windows.Forms.DataGridViewTextBoxColumn();
            WarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IsScheduleThreshold = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NextFireTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ScheduleLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CronExpression = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ScheduleDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            RunOnServiceStart = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            MaxIntervalMinutes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            ConfigureRoot = new System.Windows.Forms.DataGridViewLinkColumn();
            colRun = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCollectionDates).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsClearFilter, tsTriggerMenu });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1357, 27);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.UserChangedStatusFilter += Status_Selected;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Filter_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // tsTriggerMenu
            // 
            tsTriggerMenu.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTriggerMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsTriggerWarningAndCritical, tsTriggerSelected, tsTriggerAll });
            tsTriggerMenu.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTriggerMenu.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTriggerMenu.Name = "tsTriggerMenu";
            tsTriggerMenu.Size = new System.Drawing.Size(167, 24);
            tsTriggerMenu.Text = "Trigger Collections";
            // 
            // tsTriggerWarningAndCritical
            // 
            tsTriggerWarningAndCritical.Name = "tsTriggerWarningAndCritical";
            tsTriggerWarningAndCritical.Size = new System.Drawing.Size(213, 26);
            tsTriggerWarningAndCritical.Text = "Warning && Critical";
            tsTriggerWarningAndCritical.Click += tsTriggerWarningAndCritical_Click;
            // 
            // tsTriggerSelected
            // 
            tsTriggerSelected.Name = "tsTriggerSelected";
            tsTriggerSelected.Size = new System.Drawing.Size(213, 26);
            tsTriggerSelected.Text = "Selected";
            tsTriggerSelected.Click += tsTriggerSelected_Click;
            // 
            // tsTriggerAll
            // 
            tsTriggerAll.Name = "tsTriggerAll";
            tsTriggerAll.Size = new System.Drawing.Size(213, 26);
            tsTriggerAll.Text = "All";
            tsTriggerAll.Click += tsTriggerAll_Click;
            // 
            // dgvCollectionDates
            // 
            dgvCollectionDates.AllowUserToAddRows = false;
            dgvCollectionDates.AllowUserToDeleteRows = false;
            dgvCollectionDates.AllowUserToOrderColumns = true;
            dgvCollectionDates.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvCollectionDates.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvCollectionDates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCollectionDates.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, Reference, WarningThreshold, CriticalThreshold, IsScheduleThreshold, SnapshotAge, SnapshotDate, NextFireTime, ScheduleLevel, CronExpression, ScheduleDescription, RunOnServiceStart, MaxIntervalMinutes, ConfiguredLevel, Configure, ConfigureRoot, colRun });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvCollectionDates.DefaultCellStyle = dataGridViewCellStyle2;
            dgvCollectionDates.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvCollectionDates.EnableHeadersVisualStyles = false;
            dgvCollectionDates.Location = new System.Drawing.Point(0, 27);
            dgvCollectionDates.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvCollectionDates.Name = "dgvCollectionDates";
            dgvCollectionDates.ReadOnly = true;
            dgvCollectionDates.RowHeadersVisible = false;
            dgvCollectionDates.RowHeadersWidth = 51;
            dgvCollectionDates.Size = new System.Drawing.Size(1357, 457);
            dgvCollectionDates.TabIndex = 3;
            dgvCollectionDates.CellContentClick += Dgv_CellContentClick;
            dgvCollectionDates.RowsAdded += Dgv_RowsAdded;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 484);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1357, 26);
            statusStrip1.TabIndex = 4;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(50, 20);
            lblStatus.Text = "Ready";
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.ToolTipText = "The name of the instance";
            Instance.Width = 90;
            // 
            // Reference
            // 
            Reference.DataPropertyName = "Reference";
            Reference.HeaderText = "Reference";
            Reference.MinimumWidth = 6;
            Reference.Name = "Reference";
            Reference.ReadOnly = true;
            Reference.ToolTipText = "The name of the collection";
            Reference.Width = 103;
            // 
            // WarningThreshold
            // 
            WarningThreshold.DataPropertyName = "WarningThreshold";
            WarningThreshold.HeaderText = "Warning Threshold";
            WarningThreshold.MinimumWidth = 6;
            WarningThreshold.Name = "WarningThreshold";
            WarningThreshold.ReadOnly = true;
            WarningThreshold.ToolTipText = "If the collection hasn't run in this number of minutes, it will be highlighted in yellow";
            WarningThreshold.Width = 154;
            // 
            // CriticalThreshold
            // 
            CriticalThreshold.DataPropertyName = "CriticalThreshold";
            CriticalThreshold.HeaderText = "Critical Threshold";
            CriticalThreshold.MinimumWidth = 6;
            CriticalThreshold.Name = "CriticalThreshold";
            CriticalThreshold.ReadOnly = true;
            CriticalThreshold.ToolTipText = "If the collection hasn't run in this number of minutes, it will be highlighted in red";
            CriticalThreshold.Width = 135;
            // 
            // IsScheduleThreshold
            // 
            IsScheduleThreshold.DataPropertyName = "IsScheduleThreshold";
            IsScheduleThreshold.HeaderText = "Scheduled Threshold";
            IsScheduleThreshold.MinimumWidth = 6;
            IsScheduleThreshold.Name = "IsScheduleThreshold";
            IsScheduleThreshold.ReadOnly = true;
            IsScheduleThreshold.ToolTipText = "Checked when the thresholds shown are computed from the collection schedule rather than explicitly configured";
            IsScheduleThreshold.Width = 110;
            // 
            // SnapshotAge
            // 
            SnapshotAge.DataPropertyName = "HumanSnapshotAge";
            SnapshotAge.HeaderText = "Snapshot Age";
            SnapshotAge.MinimumWidth = 6;
            SnapshotAge.Name = "SnapshotAge";
            SnapshotAge.ReadOnly = true;
            SnapshotAge.ToolTipText = "How long since the collection last ran (at last report refresh)";
            SnapshotAge.Width = 122;
            // 
            // SnapshotDate
            // 
            SnapshotDate.DataPropertyName = "SnapshotDate";
            SnapshotDate.HeaderText = "Snapshot Date";
            SnapshotDate.MinimumWidth = 6;
            SnapshotDate.Name = "SnapshotDate";
            SnapshotDate.ReadOnly = true;
            SnapshotDate.ToolTipText = "The date/time the collection last ran";
            SnapshotDate.Width = 120;
            // 
            // NextFireTime
            // 
            NextFireTime.DataPropertyName = "NextFireTime";
            NextFireTime.HeaderText = "Next Fire Time";
            NextFireTime.MinimumWidth = 6;
            NextFireTime.Name = "NextFireTime";
            NextFireTime.ReadOnly = true;
            NextFireTime.ToolTipText = "The time the collection is next scheduled to run, calculated from the schedule (cron expression or interval)";
            NextFireTime.Width = 120;
            // 
            // ScheduleLevel
            // 
            ScheduleLevel.DataPropertyName = "ScheduleLevel";
            ScheduleLevel.HeaderText = "Schedule Level";
            ScheduleLevel.MinimumWidth = 6;
            ScheduleLevel.Name = "ScheduleLevel";
            ScheduleLevel.ReadOnly = true;
            ScheduleLevel.ToolTipText = "Indicates whether the schedule is configured at service level or customized for this instance. Schedules are configured using the service configuration tool.";
            ScheduleLevel.Width = 110;
            // 
            // CronExpression
            // 
            CronExpression.DataPropertyName = "Schedule";
            CronExpression.HeaderText = "Schedule";
            CronExpression.MinimumWidth = 6;
            CronExpression.Name = "CronExpression";
            CronExpression.ReadOnly = true;
            CronExpression.ToolTipText = "Cron expression or interval (in seconds) used to calculate the frequency of the collection";
            CronExpression.Width = 130;
            // 
            // ScheduleDescription
            // 
            ScheduleDescription.DataPropertyName = "ScheduleDescription";
            ScheduleDescription.HeaderText = "Schedule Description";
            ScheduleDescription.MinimumWidth = 6;
            ScheduleDescription.Name = "ScheduleDescription";
            ScheduleDescription.ReadOnly = true;
            ScheduleDescription.ToolTipText = "Description of the collection schedule calculated from the cron expression";
            ScheduleDescription.Width = 160;
            // 
            // RunOnServiceStart
            // 
            RunOnServiceStart.DataPropertyName = "RunOnServiceStart";
            RunOnServiceStart.HeaderText = "Run On Service Start";
            RunOnServiceStart.MinimumWidth = 6;
            RunOnServiceStart.Name = "RunOnServiceStart";
            RunOnServiceStart.ReadOnly = true;
            RunOnServiceStart.ToolTipText = "Indicates whether the collection runs when the service is started instead of waiting for the next scheduled execution";
            RunOnServiceStart.Width = 130;
            // 
            // MaxIntervalMinutes
            // 
            MaxIntervalMinutes.DataPropertyName = "MaxIntervalMinutes";
            MaxIntervalMinutes.HeaderText = "Max Interval (mins)";
            MaxIntervalMinutes.MinimumWidth = 6;
            MaxIntervalMinutes.Name = "MaxIntervalMinutes";
            MaxIntervalMinutes.ReadOnly = true;
            MaxIntervalMinutes.ToolTipText = resources.GetString("MaxIntervalMinutes.ToolTipText");
            MaxIntervalMinutes.Width = 140;
            // 
            // ConfiguredLevel
            // 
            ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            ConfiguredLevel.HeaderText = "Configured Level";
            ConfiguredLevel.MinimumWidth = 6;
            ConfiguredLevel.Name = "ConfiguredLevel";
            ConfiguredLevel.ReadOnly = true;
            ConfiguredLevel.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ConfiguredLevel.ToolTipText = "Indicates whether the warning/critical thresholds use the application defaults, or have been overridden at root or instance level.";
            ConfiguredLevel.Width = 132;
            // 
            // Configure
            // 
            Configure.HeaderText = "Configure Instance";
            Configure.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Text = "Configure Instance";
            Configure.ToolTipText = "Configure the warning/critical thresholds for this collection at the instance level. The collection is highlighted when it hasn't run successfully within the specified period of time.";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 119;
            // 
            // ConfigureRoot
            // 
            ConfigureRoot.HeaderText = "Configure Root";
            ConfigureRoot.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            ConfigureRoot.MinimumWidth = 6;
            ConfigureRoot.Name = "ConfigureRoot";
            ConfigureRoot.ReadOnly = true;
            ConfigureRoot.Text = "Configure Root";
            ConfigureRoot.ToolTipText = "Configure the warning/critical thresholds for this collection at the root level, applying to all instances. The collection is highlighted when it hasn't run successfully within the specified period of time.";
            ConfigureRoot.UseColumnTextForLinkValue = true;
            ConfigureRoot.Width = 98;
            // 
            // colRun
            // 
            colRun.HeaderText = "Run";
            colRun.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colRun.MinimumWidth = 6;
            colRun.Name = "colRun";
            colRun.ReadOnly = true;
            colRun.Text = "Run Now";
            colRun.ToolTipText = "Trigger the collection to run now";
            colRun.UseColumnTextForLinkValue = true;
            colRun.Width = 125;
            // 
            // CollectionDates
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvCollectionDates);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CollectionDates";
            Size = new System.Drawing.Size(1357, 510);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCollectionDates).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private DBADashDataGridView dgvCollectionDates;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripDropDownButton tsTriggerMenu;
        private System.Windows.Forms.ToolStripMenuItem tsTriggerWarningAndCritical;
        private System.Windows.Forms.ToolStripMenuItem tsTriggerSelected;
        private System.Windows.Forms.ToolStripMenuItem tsTriggerAll;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Reference;
        private System.Windows.Forms.DataGridViewTextBoxColumn WarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn CriticalThreshold;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsScheduleThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn NextFireTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScheduleLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn CronExpression;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScheduleDescription;
        private System.Windows.Forms.DataGridViewCheckBoxColumn RunOnServiceStart;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxIntervalMinutes;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewLinkColumn ConfigureRoot;
        private System.Windows.Forms.DataGridViewLinkColumn colRun;
    }
}
