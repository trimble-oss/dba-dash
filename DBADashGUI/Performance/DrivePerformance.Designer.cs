namespace DBADashGUI.Performance
{
    partial class DrivePerformance
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
            layout1 = new System.Windows.Forms.TableLayoutPanel();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            lblNotice = new System.Windows.Forms.ToolStripLabel();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            synchronizeAxisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dataPointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            latencyLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency10 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency50 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency100 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency200 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency500 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency1000 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency2000 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency5000 = new System.Windows.Forms.ToolStripMenuItem();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsDrives = new System.Windows.Forms.ToolStripButton();
            tsSummary = new System.Windows.Forms.ToolStripButton();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // layout1
            // 
            layout1.ColumnCount = 1;
            layout1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layout1.Dock = System.Windows.Forms.DockStyle.Fill;
            layout1.Location = new System.Drawing.Point(0, 27);
            layout1.Name = "layout1";
            layout1.RowCount = 1;
            layout1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            layout1.Size = new System.Drawing.Size(400, 257);
            layout1.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGroup, tsRefresh, lblNotice, toolStripDropDownButton1, tsCols, tsDrives, tsSummary });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(400, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsDateGroup
            // 
            tsDateGroup.Image = Properties.Resources.Time_16x;
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(76, 24);
            tsDateGroup.Text = "1min";
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
            // lblNotice
            // 
            lblNotice.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblNotice.Name = "lblNotice";
            lblNotice.Size = new System.Drawing.Size(0, 24);
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { synchronizeAxisToolStripMenuItem, smoothLinesToolStripMenuItem, dataPointsToolStripMenuItem, latencyLimitToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.SettingsOutline_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Options";
            // 
            // synchronizeAxisToolStripMenuItem
            // 
            synchronizeAxisToolStripMenuItem.Checked = true;
            synchronizeAxisToolStripMenuItem.CheckOnClick = true;
            synchronizeAxisToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            synchronizeAxisToolStripMenuItem.Name = "synchronizeAxisToolStripMenuItem";
            synchronizeAxisToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            synchronizeAxisToolStripMenuItem.Text = "Synchronize axis";
            synchronizeAxisToolStripMenuItem.Click += SynchronizeAxisToolStripMenuItem_Click;
            // 
            // smoothLinesToolStripMenuItem
            // 
            smoothLinesToolStripMenuItem.Checked = true;
            smoothLinesToolStripMenuItem.CheckOnClick = true;
            smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            smoothLinesToolStripMenuItem.Text = "Smooth Lines";
            smoothLinesToolStripMenuItem.Click += SmoothLinesToolStripMenuItem_Click;
            // 
            // dataPointsToolStripMenuItem
            // 
            dataPointsToolStripMenuItem.CheckOnClick = true;
            dataPointsToolStripMenuItem.Name = "dataPointsToolStripMenuItem";
            dataPointsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            dataPointsToolStripMenuItem.Text = "Data Points";
            dataPointsToolStripMenuItem.Click += DataPointsToolStripMenuItem_Click;
            // 
            // latencyLimitToolStripMenuItem
            // 
            latencyLimitToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsLatency10, tsLatency50, tsLatency100, tsLatency200, tsLatency500, tsLatency1000, tsLatency2000, tsLatency5000 });
            latencyLimitToolStripMenuItem.Name = "latencyLimitToolStripMenuItem";
            latencyLimitToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            latencyLimitToolStripMenuItem.Text = "Latency Limit";
            // 
            // tsLatency10
            // 
            tsLatency10.Name = "tsLatency10";
            tsLatency10.Size = new System.Drawing.Size(124, 26);
            tsLatency10.Text = "10";
            tsLatency10.Click += SetLatencyLimit;
            // 
            // tsLatency50
            // 
            tsLatency50.Name = "tsLatency50";
            tsLatency50.Size = new System.Drawing.Size(124, 26);
            tsLatency50.Text = "50";
            tsLatency50.Click += SetLatencyLimit;
            // 
            // tsLatency100
            // 
            tsLatency100.Name = "tsLatency100";
            tsLatency100.Size = new System.Drawing.Size(124, 26);
            tsLatency100.Text = "100";
            tsLatency100.Click += SetLatencyLimit;
            // 
            // tsLatency200
            // 
            tsLatency200.Checked = true;
            tsLatency200.CheckState = System.Windows.Forms.CheckState.Checked;
            tsLatency200.Name = "tsLatency200";
            tsLatency200.Size = new System.Drawing.Size(124, 26);
            tsLatency200.Text = "200";
            tsLatency200.Click += SetLatencyLimit;
            // 
            // tsLatency500
            // 
            tsLatency500.Name = "tsLatency500";
            tsLatency500.Size = new System.Drawing.Size(124, 26);
            tsLatency500.Text = "500";
            tsLatency500.Click += SetLatencyLimit;
            // 
            // tsLatency1000
            // 
            tsLatency1000.Name = "tsLatency1000";
            tsLatency1000.Size = new System.Drawing.Size(124, 26);
            tsLatency1000.Text = "1000";
            tsLatency1000.Click += SetLatencyLimit;
            // 
            // tsLatency2000
            // 
            tsLatency2000.Name = "tsLatency2000";
            tsLatency2000.Size = new System.Drawing.Size(124, 26);
            tsLatency2000.Text = "2000";
            tsLatency2000.Click += SetLatencyLimit;
            // 
            // tsLatency5000
            // 
            tsLatency5000.Name = "tsLatency5000";
            tsLatency5000.Size = new System.Drawing.Size(124, 26);
            tsLatency5000.Text = "5000";
            tsLatency5000.Click += SetLatencyLimit;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.AddComputedField_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Measures";
            tsCols.Click += TsCols_Click;
            // 
            // tsDrives
            // 
            tsDrives.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsDrives.Image = Properties.Resources.Hard_Drive;
            tsDrives.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDrives.Name = "tsDrives";
            tsDrives.Size = new System.Drawing.Size(29, 24);
            tsDrives.Text = "Drives";
            tsDrives.Click += TsDrives_Click;
            // 
            // tsSummary
            // 
            tsSummary.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSummary.Image = Properties.Resources.Table_16x;
            tsSummary.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSummary.Name = "tsSummary";
            tsSummary.Size = new System.Drawing.Size(29, 24);
            tsSummary.Text = "Show Summary";
            tsSummary.Click += TsSummary_Click;
            // 
            // DrivePerformance
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(layout1);
            Controls.Add(toolStrip1);
            Name = "DrivePerformance";
            Size = new System.Drawing.Size(400, 284);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel layout1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripLabel lblNotice;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem synchronizeAxisToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripButton tsDrives;
        private System.Windows.Forms.ToolStripButton tsSummary;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem latencyLimitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsLatency10;
        private System.Windows.Forms.ToolStripMenuItem tsLatency50;
        private System.Windows.Forms.ToolStripMenuItem tsLatency100;
        private System.Windows.Forms.ToolStripMenuItem tsLatency200;
        private System.Windows.Forms.ToolStripMenuItem tsLatency500;
        private System.Windows.Forms.ToolStripMenuItem tsLatency1000;
        private System.Windows.Forms.ToolStripMenuItem tsLatency2000;
        private System.Windows.Forms.ToolStripMenuItem tsLatency5000;
        private System.Windows.Forms.ToolStripMenuItem dataPointsToolStripMenuItem;
    }
}
