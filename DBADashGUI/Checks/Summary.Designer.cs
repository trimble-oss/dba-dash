using DBADashGUI.CustomReports;

namespace DBADashGUI
{
    partial class Summary
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
            components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            dgvSummary = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopyGrid = new System.Windows.Forms.ToolStripDropDownButton();
            copySummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsExportToExcel = new System.Windows.Forms.ToolStripDropDownButton();
            exportSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsView = new System.Windows.Forms.ToolStripDropDownButton();
            focusedViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lblRefreshTime = new System.Windows.Forms.ToolStripLabel();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            memoryDumpsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            acknowledgeDumpsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            configureThresholdsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            instanceUptimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            acknowledgeUptimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureThresholdsToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
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
            timer1 = new System.Windows.Forms.Timer(components);
            refresh1 = new Refresh();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvTests = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            MemoryDumpStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CorruptionStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            LastGoodCheckDBStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            AlertStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            FullBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            DiffBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            LogBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            DriveStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            JobStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            IsAgentRunningStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LogShippingStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            MirroringStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            AGStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            LogFreeSpaceStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            FileFreeSpaceStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            PctMaxSizeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            ElasticPoolStorageStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            QueryStoreStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            CustomCheckStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            CollectionErrorStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            SnapshotAgeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            DBMailStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IdentityStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            DatabaseStateStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            UptimeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)dgvSummary).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTests).BeginInit();
            SuspendLayout();
            // 
            // dgvSummary
            // 
            dgvSummary.AllowUserToAddRows = false;
            dgvSummary.AllowUserToDeleteRows = false;
            dgvSummary.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvSummary.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, colHidden, MemoryDumpStatus, CorruptionStatus, LastGoodCheckDBStatus, AlertStatus, FullBackupStatus, DiffBackupStatus, LogBackupStatus, DriveStatus, JobStatus, IsAgentRunningStatus, LogShippingStatus, MirroringStatus, AGStatus, LogFreeSpaceStatus, FileFreeSpaceStatus, PctMaxSizeStatus, ElasticPoolStorageStatus, QueryStoreStatus, CustomCheckStatus, CollectionErrorStatus, SnapshotAgeStatus, DBMailStatus, IdentityStatus, DatabaseStateStatus, UptimeStatus });
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvSummary.DefaultCellStyle = dataGridViewCellStyle7;
            dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvSummary.EnableHeadersVisualStyles = false;
            dgvSummary.Location = new System.Drawing.Point(0, 0);
            dgvSummary.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvSummary.Name = "dgvSummary";
            dgvSummary.ReadOnly = true;
            dgvSummary.ResultSetID = 0;
            dgvSummary.ResultSetName = null;
            dgvSummary.RowHeadersVisible = false;
            dgvSummary.RowHeadersWidth = 51;
            dgvSummary.Size = new System.Drawing.Size(1800, 119);
            dgvSummary.TabIndex = 0;
            dgvSummary.CellContentClick += DgvSummary_CellContentClick;
            dgvSummary.ColumnHeaderMouseClick += DgvSummary_ColumnHeaderMouseClick;
            dgvSummary.RowsAdded += DgvSummary_RowAdded;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopyGrid, tsExportToExcel, tsView, lblRefreshTime, tsOptions, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1800, 27);
            toolStrip1.TabIndex = 1;
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
            // tsCopyGrid
            // 
            tsCopyGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyGrid.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { copySummaryToolStripMenuItem, copyTestSummaryToolStripMenuItem });
            tsCopyGrid.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyGrid.Name = "tsCopyGrid";
            tsCopyGrid.Size = new System.Drawing.Size(34, 24);
            tsCopyGrid.Text = "Copy";
            // 
            // copySummaryToolStripMenuItem
            // 
            copySummaryToolStripMenuItem.Name = "copySummaryToolStripMenuItem";
            copySummaryToolStripMenuItem.Size = new System.Drawing.Size(222, 26);
            copySummaryToolStripMenuItem.Text = "Copy Summary";
            copySummaryToolStripMenuItem.Click += CopySummaryToolStripMenuItem_Click;
            // 
            // copyTestSummaryToolStripMenuItem
            // 
            copyTestSummaryToolStripMenuItem.Name = "copyTestSummaryToolStripMenuItem";
            copyTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(222, 26);
            copyTestSummaryToolStripMenuItem.Text = "Copy Test Summary";
            copyTestSummaryToolStripMenuItem.Click += CopyTestSummaryToolStripMenuItem_Click;
            // 
            // tsExportToExcel
            // 
            tsExportToExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExportToExcel.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exportSummaryToolStripMenuItem, exportTestSummaryToolStripMenuItem });
            tsExportToExcel.Image = Properties.Resources.excel16x16;
            tsExportToExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExportToExcel.Name = "tsExportToExcel";
            tsExportToExcel.Size = new System.Drawing.Size(34, 24);
            tsExportToExcel.Text = "Export to Excel";
            // 
            // exportSummaryToolStripMenuItem
            // 
            exportSummaryToolStripMenuItem.Name = "exportSummaryToolStripMenuItem";
            exportSummaryToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            exportSummaryToolStripMenuItem.Text = "Export Summary";
            exportSummaryToolStripMenuItem.Click += ExportSummaryToolStripMenuItem_Click;
            // 
            // exportTestSummaryToolStripMenuItem
            // 
            exportTestSummaryToolStripMenuItem.Name = "exportTestSummaryToolStripMenuItem";
            exportTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            exportTestSummaryToolStripMenuItem.Text = "Export Test Summary";
            exportTestSummaryToolStripMenuItem.Click += ExportTestSummaryToolStripMenuItem_Click;
            // 
            // tsView
            // 
            tsView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { focusedViewToolStripMenuItem, showTestSummaryToolStripMenuItem, saveToolStripMenuItem });
            tsView.Image = Properties.Resources.Table_16x;
            tsView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsView.Name = "tsView";
            tsView.Size = new System.Drawing.Size(75, 24);
            tsView.Text = "View";
            // 
            // focusedViewToolStripMenuItem
            // 
            focusedViewToolStripMenuItem.CheckOnClick = true;
            focusedViewToolStripMenuItem.Name = "focusedViewToolStripMenuItem";
            focusedViewToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            focusedViewToolStripMenuItem.Text = "Focused View";
            focusedViewToolStripMenuItem.ToolTipText = "Show only instances and checks that are warning or critical status";
            focusedViewToolStripMenuItem.Click += FocusedViewToolStripMenuItem_Click;
            // 
            // showTestSummaryToolStripMenuItem
            // 
            showTestSummaryToolStripMenuItem.Checked = true;
            showTestSummaryToolStripMenuItem.CheckOnClick = true;
            showTestSummaryToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            showTestSummaryToolStripMenuItem.Name = "showTestSummaryToolStripMenuItem";
            showTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            showTestSummaryToolStripMenuItem.Text = "Show Test Summary";
            showTestSummaryToolStripMenuItem.Click += ShowTestSummaryToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = Properties.Resources.Save_16x;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += SaveToolStripMenuItem_Click;
            // 
            // lblRefreshTime
            // 
            lblRefreshTime.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblRefreshTime.Name = "lblRefreshTime";
            lblRefreshTime.Size = new System.Drawing.Size(98, 24);
            lblRefreshTime.Text = "Refresh Time:";
            // 
            // tsOptions
            // 
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { memoryDumpsToolStripMenuItem1, instanceUptimeToolStripMenuItem });
            tsOptions.Image = Properties.Resources.SettingsOutline_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(95, 24);
            tsOptions.Text = "Options";
            tsOptions.ToolTipText = "Options";
            // 
            // memoryDumpsToolStripMenuItem1
            // 
            memoryDumpsToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { acknowledgeDumpsToolStripMenuItem1, configureThresholdsToolStripMenuItem1 });
            memoryDumpsToolStripMenuItem1.Name = "memoryDumpsToolStripMenuItem1";
            memoryDumpsToolStripMenuItem1.Size = new System.Drawing.Size(199, 26);
            memoryDumpsToolStripMenuItem1.Text = "Memory Dumps";
            // 
            // acknowledgeDumpsToolStripMenuItem1
            // 
            acknowledgeDumpsToolStripMenuItem1.Name = "acknowledgeDumpsToolStripMenuItem1";
            acknowledgeDumpsToolStripMenuItem1.Size = new System.Drawing.Size(233, 26);
            acknowledgeDumpsToolStripMenuItem1.Text = "Acknowledge Dumps";
            acknowledgeDumpsToolStripMenuItem1.Click += AcknowledgeDumpsToolStripMenuItem_Click;
            // 
            // configureThresholdsToolStripMenuItem1
            // 
            configureThresholdsToolStripMenuItem1.Name = "configureThresholdsToolStripMenuItem1";
            configureThresholdsToolStripMenuItem1.Size = new System.Drawing.Size(233, 26);
            configureThresholdsToolStripMenuItem1.Text = "Configure Thresholds";
            configureThresholdsToolStripMenuItem1.Click += ConfigureThresholdsToolStripMenuItem_Click;
            // 
            // instanceUptimeToolStripMenuItem
            // 
            instanceUptimeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { acknowledgeUptimeToolStripMenuItem, configureThresholdsToolStripMenuItem2 });
            instanceUptimeToolStripMenuItem.Name = "instanceUptimeToolStripMenuItem";
            instanceUptimeToolStripMenuItem.Size = new System.Drawing.Size(199, 26);
            instanceUptimeToolStripMenuItem.Text = "Instance Uptime";
            // 
            // acknowledgeUptimeToolStripMenuItem
            // 
            acknowledgeUptimeToolStripMenuItem.Name = "acknowledgeUptimeToolStripMenuItem";
            acknowledgeUptimeToolStripMenuItem.Size = new System.Drawing.Size(235, 26);
            acknowledgeUptimeToolStripMenuItem.Text = "Acknowledge Uptime";
            acknowledgeUptimeToolStripMenuItem.Click += AcknowledgeUptimeToolStripMenuItem_Click;
            // 
            // configureThresholdsToolStripMenuItem2
            // 
            configureThresholdsToolStripMenuItem2.Name = "configureThresholdsToolStripMenuItem2";
            configureThresholdsToolStripMenuItem2.Size = new System.Drawing.Size(235, 26);
            configureThresholdsToolStripMenuItem2.Text = "Configure Thresholds";
            configureThresholdsToolStripMenuItem2.Click += ConfigureUptimeThresholdsToolStripMenuItem_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            tsClearFilter.Click += TsClearFilter_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Memory Dump";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 128;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "DetectedCorruptionDate ";
            dataGridViewTextBoxColumn3.HeaderText = "Corruption";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "Last Good Check DB";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 137;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewCellStyle8.NullValue = "View";
            dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle8;
            dataGridViewTextBoxColumn5.HeaderText = "Alerts";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            dataGridViewTextBoxColumn5.Width = 73;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewCellStyle9.NullValue = "View";
            dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle9;
            dataGridViewTextBoxColumn6.HeaderText = "Full Backup";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            dataGridViewTextBoxColumn6.Width = 101;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewCellStyle10.NullValue = "View";
            dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle10;
            dataGridViewTextBoxColumn7.HeaderText = "Diff Backup";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.HeaderText = "Log Backup";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 103;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.HeaderText = "Log Shipping";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 110;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.HeaderText = "Drive Space";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 105;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.HeaderText = "Agent Jobs";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.HeaderText = "Availability Groups";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 141;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.HeaderText = "File FreeSpace";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 121;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.HeaderText = "Custom Checks";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 123;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.HeaderText = "DBADash Errors";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 141;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.HeaderText = "Snapshot Age";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 116;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.HeaderText = "Instance Uptime";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.Width = 127;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 10000;
            timer1.Tick += Timer1_Tick;
            // 
            // refresh1
            // 
            refresh1.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            refresh1.Dock = System.Windows.Forms.DockStyle.Fill;
            refresh1.Font = new System.Drawing.Font("Segoe UI", 11F);
            refresh1.ForeColor = System.Drawing.Color.White;
            refresh1.Location = new System.Drawing.Point(0, 27);
            refresh1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            refresh1.Name = "refresh1";
            refresh1.Size = new System.Drawing.Size(1800, 240);
            refresh1.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgvTests);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvSummary);
            splitContainer1.Size = new System.Drawing.Size(1800, 240);
            splitContainer1.SplitterDistance = 117;
            splitContainer1.TabIndex = 3;
            // 
            // dgvTests
            // 
            dgvTests.AllowUserToAddRows = false;
            dgvTests.AllowUserToDeleteRows = false;
            dgvTests.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvTests.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle11;
            dgvTests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvTests.DefaultCellStyle = dataGridViewCellStyle12;
            dgvTests.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvTests.EnableHeadersVisualStyles = false;
            dgvTests.Location = new System.Drawing.Point(0, 0);
            dgvTests.Name = "dgvTests";
            dgvTests.ReadOnly = true;
            dgvTests.ResultSetID = 0;
            dgvTests.ResultSetName = null;
            dgvTests.RowHeadersVisible = false;
            dgvTests.RowHeadersWidth = 51;
            dgvTests.Size = new System.Drawing.Size(1800, 117);
            dgvTests.TabIndex = 0;
            dgvTests.CellContentClick += DgvTests_CellContentClick;
            dgvTests.RowsAdded += DgvTests_RowsAdded;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceGroupName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            Instance.Width = 125;
            // 
            // colHidden
            // 
            colHidden.DataPropertyName = "IsHidden";
            colHidden.HeaderText = "Hidden";
            colHidden.MinimumWidth = 6;
            colHidden.Name = "colHidden";
            colHidden.ReadOnly = true;
            colHidden.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colHidden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            colHidden.ToolTipText = "Checked if instance is hidden by default at root level";
            colHidden.Visible = false;
            colHidden.Width = 70;
            // 
            // MemoryDumpStatus
            // 
            MemoryDumpStatus.HeaderText = "Memory Dump";
            MemoryDumpStatus.MinimumWidth = 6;
            MemoryDumpStatus.Name = "MemoryDumpStatus";
            MemoryDumpStatus.ReadOnly = true;
            MemoryDumpStatus.Width = 70;
            // 
            // CorruptionStatus
            // 
            CorruptionStatus.HeaderText = "Corruption";
            CorruptionStatus.MinimumWidth = 6;
            CorruptionStatus.Name = "CorruptionStatus";
            CorruptionStatus.ReadOnly = true;
            CorruptionStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            CorruptionStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            CorruptionStatus.Width = 75;
            // 
            // LastGoodCheckDBStatus
            // 
            LastGoodCheckDBStatus.HeaderText = "Last Good Check DB";
            LastGoodCheckDBStatus.LinkColor = System.Drawing.Color.Black;
            LastGoodCheckDBStatus.MinimumWidth = 6;
            LastGoodCheckDBStatus.Name = "LastGoodCheckDBStatus";
            LastGoodCheckDBStatus.ReadOnly = true;
            LastGoodCheckDBStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            LastGoodCheckDBStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            LastGoodCheckDBStatus.Width = 70;
            // 
            // AlertStatus
            // 
            AlertStatus.HeaderText = "Alerts";
            AlertStatus.LinkColor = System.Drawing.Color.Black;
            AlertStatus.MinimumWidth = 6;
            AlertStatus.Name = "AlertStatus";
            AlertStatus.ReadOnly = true;
            AlertStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            AlertStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            AlertStatus.Width = 70;
            // 
            // FullBackupStatus
            // 
            FullBackupStatus.HeaderText = "Backup FULL";
            FullBackupStatus.LinkColor = System.Drawing.Color.Black;
            FullBackupStatus.MinimumWidth = 6;
            FullBackupStatus.Name = "FullBackupStatus";
            FullBackupStatus.ReadOnly = true;
            FullBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            FullBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            FullBackupStatus.Width = 70;
            // 
            // DiffBackupStatus
            // 
            DiffBackupStatus.HeaderText = "Backup DIFF";
            DiffBackupStatus.LinkColor = System.Drawing.Color.Black;
            DiffBackupStatus.MinimumWidth = 6;
            DiffBackupStatus.Name = "DiffBackupStatus";
            DiffBackupStatus.ReadOnly = true;
            DiffBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            DiffBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            DiffBackupStatus.Width = 70;
            // 
            // LogBackupStatus
            // 
            LogBackupStatus.HeaderText = "Backup LOG";
            LogBackupStatus.LinkColor = System.Drawing.Color.Black;
            LogBackupStatus.MinimumWidth = 6;
            LogBackupStatus.Name = "LogBackupStatus";
            LogBackupStatus.ReadOnly = true;
            LogBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            LogBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            LogBackupStatus.Width = 70;
            // 
            // DriveStatus
            // 
            DriveStatus.HeaderText = "Drive Space";
            DriveStatus.LinkColor = System.Drawing.Color.Black;
            DriveStatus.MinimumWidth = 6;
            DriveStatus.Name = "DriveStatus";
            DriveStatus.ReadOnly = true;
            DriveStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            DriveStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            DriveStatus.Width = 70;
            // 
            // JobStatus
            // 
            JobStatus.HeaderText = "Agent Jobs";
            JobStatus.LinkColor = System.Drawing.Color.Black;
            JobStatus.MinimumWidth = 6;
            JobStatus.Name = "JobStatus";
            JobStatus.ReadOnly = true;
            JobStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            JobStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            JobStatus.Width = 70;
            // 
            // IsAgentRunningStatus
            // 
            IsAgentRunningStatus.HeaderText = "Is Agent Running";
            IsAgentRunningStatus.MinimumWidth = 6;
            IsAgentRunningStatus.Name = "IsAgentRunningStatus";
            IsAgentRunningStatus.ReadOnly = true;
            IsAgentRunningStatus.Width = 70;
            // 
            // LogShippingStatus
            // 
            LogShippingStatus.HeaderText = "Log Shipping";
            LogShippingStatus.LinkColor = System.Drawing.Color.Black;
            LogShippingStatus.MinimumWidth = 6;
            LogShippingStatus.Name = "LogShippingStatus";
            LogShippingStatus.ReadOnly = true;
            LogShippingStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            LogShippingStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            LogShippingStatus.Width = 70;
            // 
            // MirroringStatus
            // 
            MirroringStatus.HeaderText = "Mirroring";
            MirroringStatus.MinimumWidth = 6;
            MirroringStatus.Name = "MirroringStatus";
            MirroringStatus.ReadOnly = true;
            MirroringStatus.Width = 70;
            // 
            // AGStatus
            // 
            AGStatus.HeaderText = "Availability Groups";
            AGStatus.LinkColor = System.Drawing.Color.Black;
            AGStatus.MinimumWidth = 6;
            AGStatus.Name = "AGStatus";
            AGStatus.ReadOnly = true;
            AGStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            AGStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            AGStatus.Text = "";
            AGStatus.Width = 80;
            // 
            // LogFreeSpaceStatus
            // 
            LogFreeSpaceStatus.HeaderText = "Log Space";
            LogFreeSpaceStatus.LinkColor = System.Drawing.Color.Black;
            LogFreeSpaceStatus.MinimumWidth = 6;
            LogFreeSpaceStatus.Name = "LogFreeSpaceStatus";
            LogFreeSpaceStatus.ReadOnly = true;
            LogFreeSpaceStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            LogFreeSpaceStatus.Text = "View";
            LogFreeSpaceStatus.UseColumnTextForLinkValue = true;
            LogFreeSpaceStatus.Width = 70;
            // 
            // FileFreeSpaceStatus
            // 
            dataGridViewCellStyle2.NullValue = "View";
            FileFreeSpaceStatus.DefaultCellStyle = dataGridViewCellStyle2;
            FileFreeSpaceStatus.HeaderText = "File Space";
            FileFreeSpaceStatus.LinkColor = System.Drawing.Color.Black;
            FileFreeSpaceStatus.MinimumWidth = 6;
            FileFreeSpaceStatus.Name = "FileFreeSpaceStatus";
            FileFreeSpaceStatus.ReadOnly = true;
            FileFreeSpaceStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            FileFreeSpaceStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            FileFreeSpaceStatus.Width = 70;
            // 
            // PctMaxSizeStatus
            // 
            dataGridViewCellStyle3.Format = "P1";
            PctMaxSizeStatus.DefaultCellStyle = dataGridViewCellStyle3;
            PctMaxSizeStatus.HeaderText = "% Max Size";
            PctMaxSizeStatus.LinkColor = System.Drawing.Color.Black;
            PctMaxSizeStatus.MinimumWidth = 6;
            PctMaxSizeStatus.Name = "PctMaxSizeStatus";
            PctMaxSizeStatus.ReadOnly = true;
            PctMaxSizeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            PctMaxSizeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            PctMaxSizeStatus.Text = "View";
            PctMaxSizeStatus.UseColumnTextForLinkValue = true;
            PctMaxSizeStatus.Width = 70;
            // 
            // ElasticPoolStorageStatus
            // 
            ElasticPoolStorageStatus.HeaderText = "Elastic Pool Storage";
            ElasticPoolStorageStatus.LinkColor = System.Drawing.Color.Black;
            ElasticPoolStorageStatus.MinimumWidth = 6;
            ElasticPoolStorageStatus.Name = "ElasticPoolStorageStatus";
            ElasticPoolStorageStatus.ReadOnly = true;
            ElasticPoolStorageStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ElasticPoolStorageStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            ElasticPoolStorageStatus.Width = 70;
            // 
            // QueryStoreStatus
            // 
            QueryStoreStatus.HeaderText = "QS";
            QueryStoreStatus.LinkColor = System.Drawing.Color.Black;
            QueryStoreStatus.MinimumWidth = 6;
            QueryStoreStatus.Name = "QueryStoreStatus";
            QueryStoreStatus.ReadOnly = true;
            QueryStoreStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            QueryStoreStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            QueryStoreStatus.Text = "View";
            QueryStoreStatus.ToolTipText = "Query Store";
            QueryStoreStatus.UseColumnTextForLinkValue = true;
            QueryStoreStatus.Width = 70;
            // 
            // CustomCheckStatus
            // 
            dataGridViewCellStyle4.NullValue = "View";
            CustomCheckStatus.DefaultCellStyle = dataGridViewCellStyle4;
            CustomCheckStatus.HeaderText = "Custom Checks";
            CustomCheckStatus.LinkColor = System.Drawing.Color.Black;
            CustomCheckStatus.MinimumWidth = 6;
            CustomCheckStatus.Name = "CustomCheckStatus";
            CustomCheckStatus.ReadOnly = true;
            CustomCheckStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            CustomCheckStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            CustomCheckStatus.Width = 70;
            // 
            // CollectionErrorStatus
            // 
            CollectionErrorStatus.DataPropertyName = "CollectionErrorCount";
            dataGridViewCellStyle5.NullValue = "View";
            CollectionErrorStatus.DefaultCellStyle = dataGridViewCellStyle5;
            CollectionErrorStatus.HeaderText = "DBA Dash Errors (24hrs)";
            CollectionErrorStatus.LinkColor = System.Drawing.Color.Black;
            CollectionErrorStatus.MinimumWidth = 6;
            CollectionErrorStatus.Name = "CollectionErrorStatus";
            CollectionErrorStatus.ReadOnly = true;
            CollectionErrorStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            CollectionErrorStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            CollectionErrorStatus.Width = 70;
            // 
            // SnapshotAgeStatus
            // 
            SnapshotAgeStatus.HeaderText = "Snapshot Age";
            SnapshotAgeStatus.LinkColor = System.Drawing.Color.Black;
            SnapshotAgeStatus.MinimumWidth = 6;
            SnapshotAgeStatus.Name = "SnapshotAgeStatus";
            SnapshotAgeStatus.ReadOnly = true;
            SnapshotAgeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            SnapshotAgeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            SnapshotAgeStatus.Width = 110;
            // 
            // DBMailStatus
            // 
            DBMailStatus.HeaderText = "DB Mail";
            DBMailStatus.MinimumWidth = 6;
            DBMailStatus.Name = "DBMailStatus";
            DBMailStatus.ReadOnly = true;
            DBMailStatus.Width = 70;
            // 
            // IdentityStatus
            // 
            dataGridViewCellStyle6.Format = "P1";
            IdentityStatus.DefaultCellStyle = dataGridViewCellStyle6;
            IdentityStatus.HeaderText = "Identity Columns";
            IdentityStatus.LinkColor = System.Drawing.Color.Black;
            IdentityStatus.MinimumWidth = 6;
            IdentityStatus.Name = "IdentityStatus";
            IdentityStatus.ReadOnly = true;
            IdentityStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            IdentityStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            IdentityStatus.ToolTipText = "Check for identity columns running out of values.  Show Max % used";
            IdentityStatus.Width = 70;
            // 
            // DatabaseStateStatus
            // 
            DatabaseStateStatus.HeaderText = "Database State";
            DatabaseStateStatus.MinimumWidth = 6;
            DatabaseStateStatus.Name = "DatabaseStateStatus";
            DatabaseStateStatus.ReadOnly = true;
            DatabaseStateStatus.Text = "View";
            DatabaseStateStatus.ToolTipText = "Check for databases in Recovery Pending, Suspect or Emergency state";
            DatabaseStateStatus.UseColumnTextForLinkValue = true;
            DatabaseStateStatus.Width = 70;
            // 
            // UptimeStatus
            // 
            UptimeStatus.HeaderText = "Instance Uptime";
            UptimeStatus.LinkColor = System.Drawing.Color.Black;
            UptimeStatus.MinimumWidth = 6;
            UptimeStatus.Name = "UptimeStatus";
            UptimeStatus.ReadOnly = true;
            UptimeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            UptimeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            UptimeStatus.Width = 110;
            // 
            // Summary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(refresh1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Summary";
            Size = new System.Drawing.Size(1800, 267);
            ((System.ComponentModel.ISupportInitialize)dgvSummary).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTests).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvSummary;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
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
        private System.Windows.Forms.ToolStripDropDownButton tsView;
        private System.Windows.Forms.ToolStripMenuItem focusedViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel lblRefreshTime;
        private System.Windows.Forms.Timer timer1;
        private Refresh refresh1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvTests;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripMenuItem showTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsExportToExcel;
        private System.Windows.Forms.ToolStripMenuItem exportSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsCopyGrid;
        private System.Windows.Forms.ToolStripMenuItem copySummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem memoryDumpsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem acknowledgeDumpsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem configureThresholdsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem instanceUptimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem acknowledgeUptimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureThresholdsToolStripMenuItem2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colHidden;
        private System.Windows.Forms.DataGridViewTextBoxColumn MemoryDumpStatus;
        private System.Windows.Forms.DataGridViewLinkColumn CorruptionStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LastGoodCheckDBStatus;
        private System.Windows.Forms.DataGridViewLinkColumn AlertStatus;
        private System.Windows.Forms.DataGridViewLinkColumn FullBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn DiffBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LogBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn DriveStatus;
        private System.Windows.Forms.DataGridViewLinkColumn JobStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn IsAgentRunningStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LogShippingStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn MirroringStatus;
        private System.Windows.Forms.DataGridViewLinkColumn AGStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LogFreeSpaceStatus;
        private System.Windows.Forms.DataGridViewLinkColumn FileFreeSpaceStatus;
        private System.Windows.Forms.DataGridViewLinkColumn PctMaxSizeStatus;
        private System.Windows.Forms.DataGridViewLinkColumn ElasticPoolStorageStatus;
        private System.Windows.Forms.DataGridViewLinkColumn QueryStoreStatus;
        private System.Windows.Forms.DataGridViewLinkColumn CustomCheckStatus;
        private System.Windows.Forms.DataGridViewLinkColumn CollectionErrorStatus;
        private System.Windows.Forms.DataGridViewLinkColumn SnapshotAgeStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn DBMailStatus;
        private System.Windows.Forms.DataGridViewLinkColumn IdentityStatus;
        private System.Windows.Forms.DataGridViewLinkColumn DatabaseStateStatus;
        private System.Windows.Forms.DataGridViewLinkColumn UptimeStatus;
    }
}
