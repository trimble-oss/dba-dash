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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Summary));
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
            this.dgvSummary = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopyGrid = new System.Windows.Forms.ToolStripDropDownButton();
            this.copySummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsExportToExcel = new System.Windows.Forms.ToolStripDropDownButton();
            this.exportSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            this.focusedViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showTestSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lblRefreshTime = new System.Windows.Forms.ToolStripLabel();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.acknowledgeDumpsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsClearFilter = new System.Windows.Forms.ToolStripButton();
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.refresh1 = new DBADashGUI.Refresh();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgvTests = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.MemoryDumpStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CorruptionStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.LastGoodCheckDBStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.AlertStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.FullBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DiffBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.LogBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DriveStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.JobStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.IsAgentRunningStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogShippingStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.MirroringStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AGStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.LogFreeSpaceStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.FileFreeSpaceStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.PctMaxSizeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.ElasticPoolStorageStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.QueryStoreStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.CustomCheckStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.CollectionErrorStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.SnapshotAgeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DBMailStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IdentityStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DatabaseStateStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.UptimeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTests)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvSummary
            // 
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSummary.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.colHidden,
            this.MemoryDumpStatus,
            this.CorruptionStatus,
            this.LastGoodCheckDBStatus,
            this.AlertStatus,
            this.FullBackupStatus,
            this.DiffBackupStatus,
            this.LogBackupStatus,
            this.DriveStatus,
            this.JobStatus,
            this.IsAgentRunningStatus,
            this.LogShippingStatus,
            this.MirroringStatus,
            this.AGStatus,
            this.LogFreeSpaceStatus,
            this.FileFreeSpaceStatus,
            this.PctMaxSizeStatus,
            this.ElasticPoolStorageStatus,
            this.QueryStoreStatus,
            this.CustomCheckStatus,
            this.CollectionErrorStatus,
            this.SnapshotAgeStatus,
            this.DBMailStatus,
            this.IdentityStatus,
            this.DatabaseStateStatus,
            this.UptimeStatus});
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvSummary.DefaultCellStyle = dataGridViewCellStyle7;
            this.dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSummary.Location = new System.Drawing.Point(0, 0);
            this.dgvSummary.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.RowHeadersVisible = false;
            this.dgvSummary.RowHeadersWidth = 51;
            this.dgvSummary.RowTemplate.Height = 24;
            this.dgvSummary.Size = new System.Drawing.Size(1800, 117);
            this.dgvSummary.TabIndex = 0;
            this.dgvSummary.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvSummary_CellContentClick);
            this.dgvSummary.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DgvSummary_ColumnHeaderMouseClick);
            this.dgvSummary.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.DgvSummary_RowAdded);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopyGrid,
            this.tsExportToExcel,
            this.tsOptions,
            this.lblRefreshTime,
            this.toolStripDropDownButton1,
            this.tsClearFilter});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1800, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.TsRefresh_Click);
            // 
            // tsCopyGrid
            // 
            this.tsCopyGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyGrid.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copySummaryToolStripMenuItem,
            this.copyTestSummaryToolStripMenuItem});
            this.tsCopyGrid.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyGrid.Name = "tsCopyGrid";
            this.tsCopyGrid.Size = new System.Drawing.Size(34, 24);
            this.tsCopyGrid.Text = "Copy";
            // 
            // copySummaryToolStripMenuItem
            // 
            this.copySummaryToolStripMenuItem.Name = "copySummaryToolStripMenuItem";
            this.copySummaryToolStripMenuItem.Size = new System.Drawing.Size(222, 26);
            this.copySummaryToolStripMenuItem.Text = "Copy Summary";
            this.copySummaryToolStripMenuItem.Click += new System.EventHandler(this.CopySummaryToolStripMenuItem_Click);
            // 
            // copyTestSummaryToolStripMenuItem
            // 
            this.copyTestSummaryToolStripMenuItem.Name = "copyTestSummaryToolStripMenuItem";
            this.copyTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(222, 26);
            this.copyTestSummaryToolStripMenuItem.Text = "Copy Test Summary";
            this.copyTestSummaryToolStripMenuItem.Click += new System.EventHandler(this.CopyTestSummaryToolStripMenuItem_Click);
            // 
            // tsExportToExcel
            // 
            this.tsExportToExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExportToExcel.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportSummaryToolStripMenuItem,
            this.exportTestSummaryToolStripMenuItem});
            this.tsExportToExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExportToExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExportToExcel.Name = "tsExportToExcel";
            this.tsExportToExcel.Size = new System.Drawing.Size(34, 24);
            this.tsExportToExcel.Text = "Export to Excel";
            // 
            // exportSummaryToolStripMenuItem
            // 
            this.exportSummaryToolStripMenuItem.Name = "exportSummaryToolStripMenuItem";
            this.exportSummaryToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.exportSummaryToolStripMenuItem.Text = "Export Summary";
            this.exportSummaryToolStripMenuItem.Click += new System.EventHandler(this.ExportSummaryToolStripMenuItem_Click);
            // 
            // exportTestSummaryToolStripMenuItem
            // 
            this.exportTestSummaryToolStripMenuItem.Name = "exportTestSummaryToolStripMenuItem";
            this.exportTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.exportTestSummaryToolStripMenuItem.Text = "Export Test Summary";
            this.exportTestSummaryToolStripMenuItem.Click += new System.EventHandler(this.ExportTestSummaryToolStripMenuItem_Click);
            // 
            // tsOptions
            // 
            this.tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.focusedViewToolStripMenuItem,
            this.showTestSummaryToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.tsOptions.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsOptions.Name = "tsOptions";
            this.tsOptions.Size = new System.Drawing.Size(75, 24);
            this.tsOptions.Text = "View";
            // 
            // focusedViewToolStripMenuItem
            // 
            this.focusedViewToolStripMenuItem.CheckOnClick = true;
            this.focusedViewToolStripMenuItem.Name = "focusedViewToolStripMenuItem";
            this.focusedViewToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.focusedViewToolStripMenuItem.Text = "Focused View";
            this.focusedViewToolStripMenuItem.ToolTipText = "Show only instances and checks that are warning or critical status";
            this.focusedViewToolStripMenuItem.Click += new System.EventHandler(this.FocusedViewToolStripMenuItem_Click);
            // 
            // showTestSummaryToolStripMenuItem
            // 
            this.showTestSummaryToolStripMenuItem.Checked = true;
            this.showTestSummaryToolStripMenuItem.CheckOnClick = true;
            this.showTestSummaryToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showTestSummaryToolStripMenuItem.Name = "showTestSummaryToolStripMenuItem";
            this.showTestSummaryToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.showTestSummaryToolStripMenuItem.Text = "Show Test Summary";
            this.showTestSummaryToolStripMenuItem.Click += new System.EventHandler(this.ShowTestSummaryToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = global::DBADashGUI.Properties.Resources.Save_16x;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // lblRefreshTime
            // 
            this.lblRefreshTime.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblRefreshTime.Name = "lblRefreshTime";
            this.lblRefreshTime.Size = new System.Drawing.Size(98, 24);
            this.lblRefreshTime.Text = "Refresh Time:";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.acknowledgeDumpsToolStripMenuItem,
            this.configureThresholdsToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(129, 24);
            this.toolStripDropDownButton1.Text = "Memory Dumps";
            // 
            // acknowledgeDumpsToolStripMenuItem
            // 
            this.acknowledgeDumpsToolStripMenuItem.Name = "acknowledgeDumpsToolStripMenuItem";
            this.acknowledgeDumpsToolStripMenuItem.Size = new System.Drawing.Size(233, 26);
            this.acknowledgeDumpsToolStripMenuItem.Text = "Acknowledge Dumps";
            this.acknowledgeDumpsToolStripMenuItem.Click += new System.EventHandler(this.AcknowledgeDumpsToolStripMenuItem_Click);
            // 
            // configureThresholdsToolStripMenuItem
            // 
            this.configureThresholdsToolStripMenuItem.Name = "configureThresholdsToolStripMenuItem";
            this.configureThresholdsToolStripMenuItem.Size = new System.Drawing.Size(233, 26);
            this.configureThresholdsToolStripMenuItem.Text = "Configure Thresholds";
            this.configureThresholdsToolStripMenuItem.Click += new System.EventHandler(this.ConfigureThresholdsToolStripMenuItem_Click);
            // 
            // tsClearFilter
            // 
            this.tsClearFilter.Enabled = false;
            this.tsClearFilter.Image = global::DBADashGUI.Properties.Resources.Eraser_16x;
            this.tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsClearFilter.Name = "tsClearFilter";
            this.tsClearFilter.Size = new System.Drawing.Size(104, 24);
            this.tsClearFilter.Text = "Clear Filter";
            this.tsClearFilter.Click += new System.EventHandler(this.TsClearFilter_Click);
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
            this.dataGridViewTextBoxColumn2.HeaderText = "Memory Dump";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 128;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "DetectedCorruptionDate ";
            this.dataGridViewTextBoxColumn3.HeaderText = "Corruption";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Last Good Check DB";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 137;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewCellStyle8.NullValue = "View";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn5.HeaderText = "Alerts";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn5.Width = 73;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewCellStyle9.NullValue = "View";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn6.HeaderText = "Full Backup";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn6.Width = 101;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewCellStyle10.NullValue = "View";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn7.HeaderText = "Diff Backup";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.HeaderText = "Log Backup";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 103;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "Log Shipping";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 110;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.HeaderText = "Drive Space";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 105;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.HeaderText = "Agent Jobs";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.HeaderText = "Availability Groups";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 141;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.HeaderText = "File FreeSpace";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 121;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.HeaderText = "Custom Checks";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 123;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.HeaderText = "DBADash Errors";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 141;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.HeaderText = "Snapshot Age";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            this.dataGridViewTextBoxColumn16.Width = 116;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.HeaderText = "Instance Uptime";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.Width = 127;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // refresh1
            // 
            this.refresh1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.refresh1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refresh1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.refresh1.ForeColor = System.Drawing.Color.White;
            this.refresh1.Location = new System.Drawing.Point(0, 27);
            this.refresh1.Margin = new System.Windows.Forms.Padding(4);
            this.refresh1.Name = "refresh1";
            this.refresh1.Size = new System.Drawing.Size(1800, 239);
            this.refresh1.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvTests);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvSummary);
            this.splitContainer1.Size = new System.Drawing.Size(1800, 239);
            this.splitContainer1.SplitterDistance = 118;
            this.splitContainer1.TabIndex = 3;
            // 
            // dgvTests
            // 
            this.dgvTests.AllowUserToAddRows = false;
            this.dgvTests.AllowUserToDeleteRows = false;
            this.dgvTests.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTests.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle11;
            this.dgvTests.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvTests.DefaultCellStyle = dataGridViewCellStyle12;
            this.dgvTests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTests.Location = new System.Drawing.Point(0, 0);
            this.dgvTests.Name = "dgvTests";
            this.dgvTests.ReadOnly = true;
            this.dgvTests.RowHeadersVisible = false;
            this.dgvTests.RowHeadersWidth = 51;
            this.dgvTests.RowTemplate.Height = 29;
            this.dgvTests.Size = new System.Drawing.Size(1800, 118);
            this.dgvTests.TabIndex = 0;
            this.dgvTests.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvTests_CellContentClick);
            this.dgvTests.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.DgvTests_RowsAdded);
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "InstanceGroupName";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // colHidden
            // 
            this.colHidden.DataPropertyName = "IsHidden";
            this.colHidden.HeaderText = "Hidden";
            this.colHidden.MinimumWidth = 6;
            this.colHidden.Name = "colHidden";
            this.colHidden.ReadOnly = true;
            this.colHidden.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colHidden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colHidden.ToolTipText = "Checked if instance is hidden by default at root level";
            this.colHidden.Visible = false;
            this.colHidden.Width = 125;
            // 
            // MemoryDumpStatus
            // 
            this.MemoryDumpStatus.HeaderText = "Memory Dump";
            this.MemoryDumpStatus.MinimumWidth = 6;
            this.MemoryDumpStatus.Name = "MemoryDumpStatus";
            this.MemoryDumpStatus.ReadOnly = true;
            this.MemoryDumpStatus.Width = 118;
            // 
            // CorruptionStatus
            // 
            this.CorruptionStatus.DataPropertyName = "DetectedCorruptionDate";
            this.CorruptionStatus.HeaderText = "Corruption";
            this.CorruptionStatus.MinimumWidth = 6;
            this.CorruptionStatus.Name = "CorruptionStatus";
            this.CorruptionStatus.ReadOnly = true;
            this.CorruptionStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CorruptionStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.CorruptionStatus.Width = 103;
            // 
            // LastGoodCheckDBStatus
            // 
            this.LastGoodCheckDBStatus.HeaderText = "Last Good Check DB";
            this.LastGoodCheckDBStatus.LinkColor = System.Drawing.Color.Black;
            this.LastGoodCheckDBStatus.MinimumWidth = 6;
            this.LastGoodCheckDBStatus.Name = "LastGoodCheckDBStatus";
            this.LastGoodCheckDBStatus.ReadOnly = true;
            this.LastGoodCheckDBStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LastGoodCheckDBStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LastGoodCheckDBStatus.Width = 137;
            // 
            // AlertStatus
            // 
            this.AlertStatus.HeaderText = "Alerts";
            this.AlertStatus.LinkColor = System.Drawing.Color.Black;
            this.AlertStatus.MinimumWidth = 6;
            this.AlertStatus.Name = "AlertStatus";
            this.AlertStatus.ReadOnly = true;
            this.AlertStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.AlertStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.AlertStatus.Width = 73;
            // 
            // FullBackupStatus
            // 
            this.FullBackupStatus.HeaderText = "Full Backup";
            this.FullBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.FullBackupStatus.MinimumWidth = 6;
            this.FullBackupStatus.Name = "FullBackupStatus";
            this.FullBackupStatus.ReadOnly = true;
            this.FullBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FullBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.FullBackupStatus.Width = 101;
            // 
            // DiffBackupStatus
            // 
            this.DiffBackupStatus.HeaderText = "Diff Backup";
            this.DiffBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.DiffBackupStatus.MinimumWidth = 6;
            this.DiffBackupStatus.Name = "DiffBackupStatus";
            this.DiffBackupStatus.ReadOnly = true;
            this.DiffBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DiffBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.DiffBackupStatus.Width = 125;
            // 
            // LogBackupStatus
            // 
            this.LogBackupStatus.HeaderText = "Log Backup";
            this.LogBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.LogBackupStatus.MinimumWidth = 6;
            this.LogBackupStatus.Name = "LogBackupStatus";
            this.LogBackupStatus.ReadOnly = true;
            this.LogBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LogBackupStatus.Width = 103;
            // 
            // DriveStatus
            // 
            this.DriveStatus.HeaderText = "Drive Space";
            this.DriveStatus.LinkColor = System.Drawing.Color.Black;
            this.DriveStatus.MinimumWidth = 6;
            this.DriveStatus.Name = "DriveStatus";
            this.DriveStatus.ReadOnly = true;
            this.DriveStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DriveStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.DriveStatus.Width = 105;
            // 
            // JobStatus
            // 
            this.JobStatus.HeaderText = "Agent Jobs";
            this.JobStatus.LinkColor = System.Drawing.Color.Black;
            this.JobStatus.MinimumWidth = 6;
            this.JobStatus.Name = "JobStatus";
            this.JobStatus.ReadOnly = true;
            this.JobStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.JobStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.JobStatus.Width = 125;
            // 
            // IsAgentRunningStatus
            // 
            this.IsAgentRunningStatus.HeaderText = "Is Agent Running";
            this.IsAgentRunningStatus.MinimumWidth = 6;
            this.IsAgentRunningStatus.Name = "IsAgentRunningStatus";
            this.IsAgentRunningStatus.ReadOnly = true;
            this.IsAgentRunningStatus.Width = 125;
            // 
            // LogShippingStatus
            // 
            this.LogShippingStatus.HeaderText = "Log Shipping";
            this.LogShippingStatus.LinkColor = System.Drawing.Color.Black;
            this.LogShippingStatus.MinimumWidth = 6;
            this.LogShippingStatus.Name = "LogShippingStatus";
            this.LogShippingStatus.ReadOnly = true;
            this.LogShippingStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogShippingStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LogShippingStatus.Width = 110;
            // 
            // MirroringStatus
            // 
            this.MirroringStatus.HeaderText = "Mirroring";
            this.MirroringStatus.MinimumWidth = 6;
            this.MirroringStatus.Name = "MirroringStatus";
            this.MirroringStatus.ReadOnly = true;
            this.MirroringStatus.Width = 93;
            // 
            // AGStatus
            // 
            this.AGStatus.HeaderText = "Availability Groups";
            this.AGStatus.LinkColor = System.Drawing.Color.Black;
            this.AGStatus.MinimumWidth = 6;
            this.AGStatus.Name = "AGStatus";
            this.AGStatus.ReadOnly = true;
            this.AGStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.AGStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.AGStatus.Text = "";
            this.AGStatus.Width = 141;
            // 
            // LogFreeSpaceStatus
            // 
            this.LogFreeSpaceStatus.HeaderText = "Log Space";
            this.LogFreeSpaceStatus.LinkColor = System.Drawing.Color.Black;
            this.LogFreeSpaceStatus.MinimumWidth = 6;
            this.LogFreeSpaceStatus.Name = "LogFreeSpaceStatus";
            this.LogFreeSpaceStatus.ReadOnly = true;
            this.LogFreeSpaceStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LogFreeSpaceStatus.Text = "View";
            this.LogFreeSpaceStatus.UseColumnTextForLinkValue = true;
            this.LogFreeSpaceStatus.Width = 97;
            // 
            // FileFreeSpaceStatus
            // 
            dataGridViewCellStyle2.NullValue = "View";
            this.FileFreeSpaceStatus.DefaultCellStyle = dataGridViewCellStyle2;
            this.FileFreeSpaceStatus.HeaderText = "File Space";
            this.FileFreeSpaceStatus.LinkColor = System.Drawing.Color.Black;
            this.FileFreeSpaceStatus.MinimumWidth = 6;
            this.FileFreeSpaceStatus.Name = "FileFreeSpaceStatus";
            this.FileFreeSpaceStatus.ReadOnly = true;
            this.FileFreeSpaceStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FileFreeSpaceStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FileFreeSpaceStatus.Width = 95;
            // 
            // PctMaxSizeStatus
            // 
            dataGridViewCellStyle3.Format = "P1";
            this.PctMaxSizeStatus.DefaultCellStyle = dataGridViewCellStyle3;
            this.PctMaxSizeStatus.HeaderText = "% Max Size";
            this.PctMaxSizeStatus.LinkColor = System.Drawing.Color.Black;
            this.PctMaxSizeStatus.MinimumWidth = 6;
            this.PctMaxSizeStatus.Name = "PctMaxSizeStatus";
            this.PctMaxSizeStatus.ReadOnly = true;
            this.PctMaxSizeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.PctMaxSizeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.PctMaxSizeStatus.Text = "View";
            this.PctMaxSizeStatus.UseColumnTextForLinkValue = true;
            this.PctMaxSizeStatus.Width = 125;
            // 
            // ElasticPoolStorageStatus
            // 
            this.ElasticPoolStorageStatus.HeaderText = "Elastic Pool Storage";
            this.ElasticPoolStorageStatus.LinkColor = System.Drawing.Color.Black;
            this.ElasticPoolStorageStatus.MinimumWidth = 6;
            this.ElasticPoolStorageStatus.Name = "ElasticPoolStorageStatus";
            this.ElasticPoolStorageStatus.ReadOnly = true;
            this.ElasticPoolStorageStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.ElasticPoolStorageStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.ElasticPoolStorageStatus.Width = 150;
            // 
            // QueryStoreStatus
            // 
            this.QueryStoreStatus.HeaderText = "QS";
            this.QueryStoreStatus.LinkColor = System.Drawing.Color.Black;
            this.QueryStoreStatus.MinimumWidth = 6;
            this.QueryStoreStatus.Name = "QueryStoreStatus";
            this.QueryStoreStatus.ReadOnly = true;
            this.QueryStoreStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.QueryStoreStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.QueryStoreStatus.Text = "View";
            this.QueryStoreStatus.ToolTipText = "Query Store";
            this.QueryStoreStatus.UseColumnTextForLinkValue = true;
            this.QueryStoreStatus.Width = 57;
            // 
            // CustomCheckStatus
            // 
            dataGridViewCellStyle4.NullValue = "View";
            this.CustomCheckStatus.DefaultCellStyle = dataGridViewCellStyle4;
            this.CustomCheckStatus.HeaderText = "Custom Checks";
            this.CustomCheckStatus.LinkColor = System.Drawing.Color.Black;
            this.CustomCheckStatus.MinimumWidth = 6;
            this.CustomCheckStatus.Name = "CustomCheckStatus";
            this.CustomCheckStatus.ReadOnly = true;
            this.CustomCheckStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CustomCheckStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.CustomCheckStatus.Width = 123;
            // 
            // CollectionErrorStatus
            // 
            this.CollectionErrorStatus.DataPropertyName = "CollectionErrorCount";
            dataGridViewCellStyle5.NullValue = "View";
            this.CollectionErrorStatus.DefaultCellStyle = dataGridViewCellStyle5;
            this.CollectionErrorStatus.HeaderText = "DBA Dash Errors (24hrs)";
            this.CollectionErrorStatus.LinkColor = System.Drawing.Color.Black;
            this.CollectionErrorStatus.MinimumWidth = 6;
            this.CollectionErrorStatus.Name = "CollectionErrorStatus";
            this.CollectionErrorStatus.ReadOnly = true;
            this.CollectionErrorStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CollectionErrorStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.CollectionErrorStatus.Width = 136;
            // 
            // SnapshotAgeStatus
            // 
            this.SnapshotAgeStatus.HeaderText = "Snapshot Age";
            this.SnapshotAgeStatus.LinkColor = System.Drawing.Color.Black;
            this.SnapshotAgeStatus.MinimumWidth = 6;
            this.SnapshotAgeStatus.Name = "SnapshotAgeStatus";
            this.SnapshotAgeStatus.ReadOnly = true;
            this.SnapshotAgeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SnapshotAgeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SnapshotAgeStatus.Width = 116;
            // 
            // DBMailStatus
            // 
            this.DBMailStatus.HeaderText = "DB Mail";
            this.DBMailStatus.MinimumWidth = 6;
            this.DBMailStatus.Name = "DBMailStatus";
            this.DBMailStatus.ReadOnly = true;
            this.DBMailStatus.Width = 79;
            // 
            // IdentityStatus
            // 
            this.IdentityStatus.DataPropertyName = "MaxIdentityPctUsed";
            dataGridViewCellStyle6.Format = "P1";
            this.IdentityStatus.DefaultCellStyle = dataGridViewCellStyle6;
            this.IdentityStatus.HeaderText = "Identity Columns";
            this.IdentityStatus.LinkColor = System.Drawing.Color.Black;
            this.IdentityStatus.MinimumWidth = 6;
            this.IdentityStatus.Name = "IdentityStatus";
            this.IdentityStatus.ReadOnly = true;
            this.IdentityStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.IdentityStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.IdentityStatus.ToolTipText = "Check for identity columns running out of values.  Show Max % used";
            this.IdentityStatus.Width = 125;
            // 
            // DatabaseStateStatus
            // 
            this.DatabaseStateStatus.HeaderText = "Database State";
            this.DatabaseStateStatus.MinimumWidth = 6;
            this.DatabaseStateStatus.Name = "DatabaseStateStatus";
            this.DatabaseStateStatus.ReadOnly = true;
            this.DatabaseStateStatus.Text = "View";
            this.DatabaseStateStatus.ToolTipText = "Check for databases in Recovery Pending, Suspect or Emergency state";
            this.DatabaseStateStatus.UseColumnTextForLinkValue = true;
            this.DatabaseStateStatus.Width = 125;
            // 
            // UptimeStatus
            // 
            this.UptimeStatus.HeaderText = "Instance Uptime";
            this.UptimeStatus.LinkColor = System.Drawing.Color.Black;
            this.UptimeStatus.MinimumWidth = 6;
            this.UptimeStatus.Name = "UptimeStatus";
            this.UptimeStatus.ReadOnly = true;
            this.UptimeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.UptimeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.UptimeStatus.Width = 127;
            // 
            // Summary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.refresh1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Summary";
            this.Size = new System.Drawing.Size(1800, 266);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTests)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSummary;
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
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem focusedViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel lblRefreshTime;
        private System.Windows.Forms.Timer timer1;
        private Refresh refresh1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvTests;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripMenuItem showTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsExportToExcel;
        private System.Windows.Forms.ToolStripMenuItem exportSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsCopyGrid;
        private System.Windows.Forms.ToolStripMenuItem copySummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyTestSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem acknowledgeDumpsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureThresholdsToolStripMenuItem;
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
