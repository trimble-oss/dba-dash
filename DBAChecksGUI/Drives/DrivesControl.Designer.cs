namespace DBAChecksGUI.Properties
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
            this.driveControl1 = new DBAChecksGUI.DriveControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
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
            this.pnlSpacing = new System.Windows.Forms.Panel();
            this.pnlDrives.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlDrives
            // 
            this.pnlDrives.AutoScroll = true;
            this.pnlDrives.Controls.Add(this.driveControl1);
            this.pnlDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDrives.Location = new System.Drawing.Point(0, 47);
            this.pnlDrives.Name = "pnlDrives";
            this.pnlDrives.Size = new System.Drawing.Size(735, 210);
            this.pnlDrives.TabIndex = 3;
            // 
            // driveControl1
            // 
            this.driveControl1.DisplayInstanceName = false;
            this.driveControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.driveControl1.Drive.ConnectionString = null;
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
            this.driveControl1.Drive.DriveCheckType = DBAChecksGUI.DriveThreshold.DriveCheckTypeEnum.None;
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
            this.driveControl1.Drive.WarningThreshold = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Location = new System.Drawing.Point(0, 0);
            this.driveControl1.Name = "driveControl1";
            this.driveControl1.Size = new System.Drawing.Size(735, 210);
            this.driveControl1.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripFilter,
            this.tsConfigure,
            this.tsGridView,
            this.tsDrivesView});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(735, 31);
            this.toolStrip1.TabIndex = 7;
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
            this.toolStripFilter.Size = new System.Drawing.Size(34, 28);
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
            this.tsConfigure.Size = new System.Drawing.Size(34, 28);
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
            // tsGridView
            // 
            this.tsGridView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsGridView.Image = global::DBAChecksGUI.Properties.Resources.Table_16x;
            this.tsGridView.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGridView.Name = "tsGridView";
            this.tsGridView.Size = new System.Drawing.Size(29, 28);
            this.tsGridView.Text = "Table View";
            this.tsGridView.Click += new System.EventHandler(this.tsGridView_Click);
            // 
            // tsDrivesView
            // 
            this.tsDrivesView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDrivesView.Image = global::DBAChecksGUI.Properties.Resources.Hard_Drive;
            this.tsDrivesView.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDrivesView.Name = "tsDrivesView";
            this.tsDrivesView.Size = new System.Drawing.Size(29, 28);
            this.tsDrivesView.Text = "Drives View";
            this.tsDrivesView.Click += new System.EventHandler(this.tsDrivesView_Click);
            // 
            // pnlSpacing
            // 
            this.pnlSpacing.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSpacing.Location = new System.Drawing.Point(0, 31);
            this.pnlSpacing.Name = "pnlSpacing";
            this.pnlSpacing.Size = new System.Drawing.Size(735, 16);
            this.pnlSpacing.TabIndex = 8;
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
    }
}
