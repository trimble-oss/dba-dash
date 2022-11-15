namespace DBADashGUI.Properties
{
    partial class DrivesControl
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
            this.pnlDrives = new System.Windows.Forms.Panel();
            this.driveControl1 = new DBADashGUI.DriveControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsGridView = new System.Windows.Forms.ToolStripButton();
            this.tsDrivesView = new System.Windows.Forms.ToolStripButton();
            this.tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            this.includeAllMetricsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlSpacing = new System.Windows.Forms.Panel();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.pnlDrives.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlDrives
            // 
            this.pnlDrives.AutoScroll = true;
            this.pnlDrives.Controls.Add(this.driveControl1);
            this.pnlDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDrives.Location = new System.Drawing.Point(0, 43);
            this.pnlDrives.Name = "pnlDrives";
            this.pnlDrives.Size = new System.Drawing.Size(735, 214);
            this.pnlDrives.TabIndex = 3;
            // 
            // driveControl1
            // 
            this.driveControl1.DisplayInstanceName = false;
            this.driveControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.driveControl1.Drive.CriticalThreshold = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.DriveCapacity = ((long)(0));
            this.driveControl1.Drive.DriveCapacityGB = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.DriveCheckType = DBADashGUI.DriveThreshold.DriveCheckTypeEnum.None;
            this.driveControl1.Drive.DriveCheckTypeChar = '-';
            this.driveControl1.Drive.DriveID = 0;
            this.driveControl1.Drive.DriveLabel = null;
            this.driveControl1.Drive.DriveLetter = null;
            this.driveControl1.Drive.FreeSpace = ((long)(0));
            this.driveControl1.Drive.FreeSpaceGB = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.Inherited = false;
            this.driveControl1.Drive.InstanceID = 0;
            this.driveControl1.Drive.SnapshotDate = new System.DateTime(((long)(0)));
            this.driveControl1.Drive.WarningThreshold = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Location = new System.Drawing.Point(0, 0);
            this.driveControl1.Name = "driveControl1";
            this.driveControl1.Size = new System.Drawing.Size(735, 214);
            this.driveControl1.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.toolStripFilter,
            this.tsConfigure,
            this.tsGridView,
            this.tsDrivesView,
            this.tsColumns});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(735, 27);
            this.toolStrip1.TabIndex = 7;
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
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
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
            this.criticalToolStripMenuItem.Click += new System.EventHandler(this.CriticalToolStripMenuItem_Click);
            // 
            // warningToolStripMenuItem
            // 
            this.warningToolStripMenuItem.CheckOnClick = true;
            this.warningToolStripMenuItem.Name = "warningToolStripMenuItem";
            this.warningToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.warningToolStripMenuItem.Text = "Warning";
            this.warningToolStripMenuItem.Click += new System.EventHandler(this.WarningToolStripMenuItem_Click);
            // 
            // undefinedToolStripMenuItem
            // 
            this.undefinedToolStripMenuItem.CheckOnClick = true;
            this.undefinedToolStripMenuItem.Name = "undefinedToolStripMenuItem";
            this.undefinedToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.undefinedToolStripMenuItem.Text = "Undefined";
            this.undefinedToolStripMenuItem.Click += new System.EventHandler(this.UndefinedToolStripMenuItem_Click);
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
            this.configureInstanceThresholdsToolStripMenuItem.Click += new System.EventHandler(this.ConfigureInstanceThresholdsToolStripMenuItem_Click);
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            this.configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            this.configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            this.configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            this.configureRootThresholdsToolStripMenuItem.Click += new System.EventHandler(this.ConfigureRootThresholdsToolStripMenuItem_Click);
            // 
            // tsGridView
            // 
            this.tsGridView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsGridView.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsGridView.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGridView.Name = "tsGridView";
            this.tsGridView.Size = new System.Drawing.Size(29, 24);
            this.tsGridView.Text = "Table View";
            this.tsGridView.Click += new System.EventHandler(this.TsGridView_Click);
            // 
            // tsDrivesView
            // 
            this.tsDrivesView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDrivesView.Image = global::DBADashGUI.Properties.Resources.Hard_Drive;
            this.tsDrivesView.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDrivesView.Name = "tsDrivesView";
            this.tsDrivesView.Size = new System.Drawing.Size(29, 24);
            this.tsDrivesView.Text = "Drives View";
            this.tsDrivesView.Click += new System.EventHandler(this.TsDrivesView_Click);
            // 
            // tsColumns
            // 
            this.tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsColumns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeAllMetricsToolStripMenuItem});
            this.tsColumns.Image = global::DBADashGUI.Properties.Resources.Column_16x;
            this.tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsColumns.Name = "tsColumns";
            this.tsColumns.Size = new System.Drawing.Size(34, 24);
            this.tsColumns.Text = "Columns";
            // 
            // includeAllMetricsToolStripMenuItem
            // 
            this.includeAllMetricsToolStripMenuItem.CheckOnClick = true;
            this.includeAllMetricsToolStripMenuItem.Name = "includeAllMetricsToolStripMenuItem";
            this.includeAllMetricsToolStripMenuItem.Size = new System.Drawing.Size(214, 26);
            this.includeAllMetricsToolStripMenuItem.Text = "Include All Metrics";
            this.includeAllMetricsToolStripMenuItem.Click += new System.EventHandler(this.IncludeAllMetricsToolStripMenuItem_Click);
            // 
            // pnlSpacing
            // 
            this.pnlSpacing.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSpacing.Location = new System.Drawing.Point(0, 27);
            this.pnlSpacing.Name = "pnlSpacing";
            this.pnlSpacing.Size = new System.Drawing.Size(735, 16);
            this.pnlSpacing.TabIndex = 8;
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.TsExcel_Click);
            // 
            // DrivesControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlDrives);
            this.Controls.Add(this.pnlSpacing);
            this.Controls.Add(this.toolStrip1);
            this.Name = "DrivesControl";
            this.Size = new System.Drawing.Size(735, 257);
            this.pnlDrives.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel pnlDrives;
        private DriveControl driveControl1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.Panel pnlSpacing;
        private System.Windows.Forms.ToolStripButton tsGridView;
        private System.Windows.Forms.ToolStripButton tsDrivesView;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsColumns;
        private System.Windows.Forms.ToolStripMenuItem includeAllMetricsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsExcel;
    }
}
