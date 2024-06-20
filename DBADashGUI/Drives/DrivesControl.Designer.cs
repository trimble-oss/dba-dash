namespace DBADashGUI.Drives
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DrivesControl));
            pnlDrives = new System.Windows.Forms.Panel();
            driveControl1 = new DriveControl();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsGridView = new System.Windows.Forms.ToolStripButton();
            tsDrivesView = new System.Windows.Forms.ToolStripButton();
            tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            includeAllMetricsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            pnlSpacing = new System.Windows.Forms.Panel();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            pnlDrives.SuspendLayout();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlDrives
            // 
            pnlDrives.AutoScroll = true;
            pnlDrives.Controls.Add(driveControl1);
            pnlDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlDrives.Location = new System.Drawing.Point(0, 47);
            pnlDrives.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlDrives.Name = "pnlDrives";
            pnlDrives.Size = new System.Drawing.Size(735, 250);
            pnlDrives.TabIndex = 3;
            // 
            // driveControl1
            // 
            driveControl1.DisplayInstanceName = false;
            driveControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            driveControl1.Drive.CriticalThreshold = new decimal(new int[] { 0, 0, 0, 0 });
            driveControl1.Drive.DriveCapacity = 0L;
            driveControl1.Drive.DriveCapacityGB = new decimal(new int[] { 0, 0, 0, 0 });
            driveControl1.Drive.DriveCheckType = DriveThreshold.DriveCheckTypeEnum.None;
            driveControl1.Drive.DriveCheckTypeChar = '-';
            driveControl1.Drive.DriveID = 0;
            driveControl1.Drive.DriveLabel = null;
            driveControl1.Drive.DriveLetter = null;
            driveControl1.Drive.FreeSpace = 0L;
            driveControl1.Drive.FreeSpaceGB = new decimal(new int[] { 0, 0, 0, 0 });
            driveControl1.Drive.Inherited = false;
            driveControl1.Drive.InstanceID = 0;
            driveControl1.Drive.SnapshotDate = new System.DateTime(0L);
            driveControl1.Drive.WarningThreshold = new decimal(new int[] { 0, 0, 0, 0 });
            driveControl1.Location = new System.Drawing.Point(0, 0);
            driveControl1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            driveControl1.Name = "driveControl1";
            driveControl1.Size = new System.Drawing.Size(735, 250);
            driveControl1.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsConfigure, tsGridView, tsDrivesView, tsColumns, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(735, 27);
            toolStrip1.TabIndex = 7;
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
            statusFilterToolStrip1.UserChangedStatusFilter += Status_Selected;
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
            // tsGridView
            // 
            tsGridView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsGridView.Image = Properties.Resources.Table_16x;
            tsGridView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGridView.Name = "tsGridView";
            tsGridView.Size = new System.Drawing.Size(29, 24);
            tsGridView.Text = "Table View";
            tsGridView.Click += TsGridView_Click;
            // 
            // tsDrivesView
            // 
            tsDrivesView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsDrivesView.Image = Properties.Resources.Hard_Drive;
            tsDrivesView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDrivesView.Name = "tsDrivesView";
            tsDrivesView.Size = new System.Drawing.Size(29, 24);
            tsDrivesView.Text = "Drives View";
            tsDrivesView.Click += TsDrivesView_Click;
            // 
            // tsColumns
            // 
            tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsColumns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { includeAllMetricsToolStripMenuItem });
            tsColumns.Image = Properties.Resources.Column_16x;
            tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsColumns.Name = "tsColumns";
            tsColumns.Size = new System.Drawing.Size(34, 24);
            tsColumns.Text = "Columns";
            // 
            // includeAllMetricsToolStripMenuItem
            // 
            includeAllMetricsToolStripMenuItem.CheckOnClick = true;
            includeAllMetricsToolStripMenuItem.Name = "includeAllMetricsToolStripMenuItem";
            includeAllMetricsToolStripMenuItem.Size = new System.Drawing.Size(214, 26);
            includeAllMetricsToolStripMenuItem.Text = "Include All Metrics";
            includeAllMetricsToolStripMenuItem.Click += IncludeAllMetricsToolStripMenuItem_Click;
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
            tsTrigger.Click += TsTrigger_Click;
            // 
            // pnlSpacing
            // 
            pnlSpacing.Dock = System.Windows.Forms.DockStyle.Top;
            pnlSpacing.Location = new System.Drawing.Point(0, 27);
            pnlSpacing.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlSpacing.Name = "pnlSpacing";
            pnlSpacing.Size = new System.Drawing.Size(735, 20);
            pnlSpacing.TabIndex = 8;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 297);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(735, 24);
            statusStrip1.TabIndex = 9;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 18);
            // 
            // DrivesControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlDrives);
            Controls.Add(pnlSpacing);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DrivesControl";
            Size = new System.Drawing.Size(735, 321);
            pnlDrives.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Panel pnlDrives;
        private DriveControl driveControl1;
        private System.Windows.Forms.ToolStrip toolStrip1;
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
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
