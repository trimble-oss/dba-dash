namespace DBADashGUI.LogShipping
{
    partial class LogShippingControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogShippingControl));
            dgvLogShipping = new System.Windows.Forms.DataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            RestoreDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            BackupStartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            TimeSinceLast = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LatencyOfLast = new System.Windows.Forms.DataGridViewTextBoxColumn();
            TotalTimeBehind = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LastFile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ThresholdConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            dgvSummary = new System.Windows.Forms.DataGridView();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsCopyDetail = new System.Windows.Forms.ToolStripButton();
            tsExportExcelDetail = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
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
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dgvLogShipping).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSummary).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip2.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvLogShipping
            // 
            dgvLogShipping.AllowUserToAddRows = false;
            dgvLogShipping.AllowUserToDeleteRows = false;
            dgvLogShipping.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvLogShipping.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvLogShipping.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLogShipping.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, Database, Status, RestoreDate, BackupStartDate, TimeSinceLast, LatencyOfLast, TotalTimeBehind, SnapshotAge, SnapshotDate, LastFile, ThresholdConfiguredLevel, Configure });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvLogShipping.DefaultCellStyle = dataGridViewCellStyle2;
            dgvLogShipping.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvLogShipping.Location = new System.Drawing.Point(0, 27);
            dgvLogShipping.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvLogShipping.Name = "dgvLogShipping";
            dgvLogShipping.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvLogShipping.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvLogShipping.RowHeadersVisible = false;
            dgvLogShipping.RowHeadersWidth = 51;
            dgvLogShipping.RowTemplate.Height = 24;
            dgvLogShipping.Size = new System.Drawing.Size(698, 276);
            dgvLogShipping.TabIndex = 0;
            dgvLogShipping.CellContentClick += DgvLogShipping_CellContentClick;
            dgvLogShipping.RowsAdded += DgvLogShipping_RowsAdded;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 90;
            // 
            // Database
            // 
            Database.DataPropertyName = "name";
            Database.HeaderText = "Database";
            Database.MinimumWidth = 6;
            Database.Name = "Database";
            Database.ReadOnly = true;
            Database.Width = 98;
            // 
            // Status
            // 
            Status.DataPropertyName = "StatusDescription";
            Status.HeaderText = "Status";
            Status.MinimumWidth = 6;
            Status.Name = "Status";
            Status.ReadOnly = true;
            Status.Width = 77;
            // 
            // RestoreDate
            // 
            RestoreDate.DataPropertyName = "restore_date";
            RestoreDate.HeaderText = "Restore Date";
            RestoreDate.MinimumWidth = 6;
            RestoreDate.Name = "RestoreDate";
            RestoreDate.ReadOnly = true;
            RestoreDate.Width = 111;
            // 
            // BackupStartDate
            // 
            BackupStartDate.DataPropertyName = "backup_start_date";
            BackupStartDate.HeaderText = "Backup Start Date";
            BackupStartDate.MinimumWidth = 6;
            BackupStartDate.Name = "BackupStartDate";
            BackupStartDate.ReadOnly = true;
            BackupStartDate.Width = 112;
            // 
            // TimeSinceLast
            // 
            TimeSinceLast.DataPropertyName = "TimeSinceLast";
            TimeSinceLast.HeaderText = "Time Since Last";
            TimeSinceLast.MinimumWidth = 6;
            TimeSinceLast.Name = "TimeSinceLast";
            TimeSinceLast.ReadOnly = true;
            TimeSinceLast.Width = 127;
            // 
            // LatencyOfLast
            // 
            LatencyOfLast.DataPropertyName = "LatencyOfLast";
            LatencyOfLast.HeaderText = "Latency of Last";
            LatencyOfLast.MinimumWidth = 6;
            LatencyOfLast.Name = "LatencyOfLast";
            LatencyOfLast.ReadOnly = true;
            LatencyOfLast.Width = 99;
            // 
            // TotalTimeBehind
            // 
            TotalTimeBehind.DataPropertyName = "TotalTimeBehind";
            TotalTimeBehind.HeaderText = "Total Time Behind";
            TotalTimeBehind.MinimumWidth = 6;
            TotalTimeBehind.Name = "TotalTimeBehind";
            TotalTimeBehind.ReadOnly = true;
            TotalTimeBehind.Width = 139;
            // 
            // SnapshotAge
            // 
            SnapshotAge.DataPropertyName = "SnapshotAge";
            SnapshotAge.HeaderText = "Snapshot Age";
            SnapshotAge.MinimumWidth = 6;
            SnapshotAge.Name = "SnapshotAge";
            SnapshotAge.ReadOnly = true;
            SnapshotAge.Width = 116;
            // 
            // SnapshotDate
            // 
            SnapshotDate.DataPropertyName = "SnapshotDate";
            SnapshotDate.HeaderText = "Snapshot Date";
            SnapshotDate.MinimumWidth = 6;
            SnapshotDate.Name = "SnapshotDate";
            SnapshotDate.ReadOnly = true;
            SnapshotDate.Width = 143;
            // 
            // LastFile
            // 
            LastFile.DataPropertyName = "last_file";
            LastFile.HeaderText = "Last File";
            LastFile.MinimumWidth = 6;
            LastFile.Name = "LastFile";
            LastFile.ReadOnly = true;
            LastFile.Width = 64;
            // 
            // ThresholdConfiguredLevel
            // 
            ThresholdConfiguredLevel.DataPropertyName = "ThresholdConfiguredLevel";
            ThresholdConfiguredLevel.HeaderText = "Threshold Configured Level";
            ThresholdConfiguredLevel.MinimumWidth = 6;
            ThresholdConfiguredLevel.Name = "ThresholdConfiguredLevel";
            ThresholdConfiguredLevel.ReadOnly = true;
            ThresholdConfiguredLevel.Width = 163;
            // 
            // Configure
            // 
            Configure.HeaderText = "Configure";
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            Configure.Text = "Configure";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 98;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsConfigure, tsBack, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(698, 27);
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
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureInstanceThresholdsToolStripMenuItem, configureRootThresholdsToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            configureInstanceThresholdsToolStripMenuItem.Click += ConfigureInstanceThresholdsToolStripMenuItem_Click;
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            configureRootThresholdsToolStripMenuItem.Click += ConfigureRootThresholdsToolStripMenuItem_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 24);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Visible = false;
            tsTrigger.Click += tsTrigger_Click;
            // 
            // dgvSummary
            // 
            dgvSummary.AllowUserToAddRows = false;
            dgvSummary.AllowUserToDeleteRows = false;
            dgvSummary.BackgroundColor = System.Drawing.Color.White;
            dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvSummary.Location = new System.Drawing.Point(0, 0);
            dgvSummary.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvSummary.Name = "dgvSummary";
            dgvSummary.ReadOnly = true;
            dgvSummary.RowHeadersVisible = false;
            dgvSummary.RowHeadersWidth = 51;
            dgvSummary.RowTemplate.Height = 24;
            dgvSummary.Size = new System.Drawing.Size(698, 184);
            dgvSummary.TabIndex = 3;
            dgvSummary.CellContentClick += DgvSummary_CellContentClick;
            dgvSummary.RowsAdded += DgvSummary_RowsAdded;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgvSummary);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvLogShipping);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(698, 492);
            splitContainer1.SplitterDistance = 184;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 4;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyDetail, tsExportExcelDetail, statusFilterToolStrip1 });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(698, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsCopyDetail
            // 
            tsCopyDetail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyDetail.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyDetail.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyDetail.Name = "tsCopyDetail";
            tsCopyDetail.Size = new System.Drawing.Size(29, 24);
            tsCopyDetail.Text = "Copy";
            tsCopyDetail.Click += TsCopyDetail_Click;
            // 
            // tsExportExcelDetail
            // 
            tsExportExcelDetail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExportExcelDetail.Image = Properties.Resources.excel16x16;
            tsExportExcelDetail.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExportExcelDetail.Name = "tsExportExcelDetail";
            tsExportExcelDetail.Size = new System.Drawing.Size(29, 24);
            tsExportExcelDetail.Text = "Export Excel";
            tsExportExcelDetail.Click += TsExportExcelDetail_Click;
            // 
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.Acknowledged = false;
            statusFilterToolStrip1.AcknowledgedVisible = false;
            statusFilterToolStrip1.Critical = true;
            statusFilterToolStrip1.CriticalVisible = true;
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.NA = true;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.NAVisible = true;
            statusFilterToolStrip1.OK = true;
            statusFilterToolStrip1.OKVisible = true;
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.Warning = true;
            statusFilterToolStrip1.WarningVisible = true;
            statusFilterToolStrip1.UserChangedStatusFilter += TsFilter_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "name";
            dataGridViewTextBoxColumn2.HeaderText = "Database";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.Width = 98;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "StatusDescription";
            dataGridViewTextBoxColumn3.HeaderText = "Status";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.Width = 77;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "restore_date";
            dataGridViewTextBoxColumn4.HeaderText = "Restore Date";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.Width = 111;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "backup_start_date";
            dataGridViewTextBoxColumn5.HeaderText = "Backup Start Date";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.Width = 112;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "TimeSinceLast";
            dataGridViewTextBoxColumn6.HeaderText = "Time Since Last";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.Width = 127;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "LatencyOfLast";
            dataGridViewTextBoxColumn7.HeaderText = "Latency of Last";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.Width = 99;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "TotalTimeBehind";
            dataGridViewTextBoxColumn8.HeaderText = "Total Time Behind";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.Width = 139;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "SnapshotAge";
            dataGridViewTextBoxColumn9.HeaderText = "Snapshot Age";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.Width = 116;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "LogRestoresDate";
            dataGridViewTextBoxColumn10.HeaderText = "Log Restores Date";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.Width = 143;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "last_file";
            dataGridViewTextBoxColumn11.HeaderText = "Last File";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.Width = 64;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "ThresholdConfiguredLevel";
            dataGridViewTextBoxColumn12.HeaderText = "Threshold Configured Level";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.Width = 163;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 519);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(698, 22);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // LogShippingControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "LogShippingControl";
            Size = new System.Drawing.Size(698, 541);
            ((System.ComponentModel.ISupportInitialize)dgvLogShipping).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSummary).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvLogShipping;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridView dgvSummary;
        private System.Windows.Forms.SplitContainer splitContainer1;
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
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsCopyDetail;
        private System.Windows.Forms.ToolStripButton tsExportExcelDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn RestoreDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn BackupStartDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLast;
        private System.Windows.Forms.DataGridViewTextBoxColumn LatencyOfLast;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalTimeBehind;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFile;
        private System.Windows.Forms.DataGridViewTextBoxColumn ThresholdConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
