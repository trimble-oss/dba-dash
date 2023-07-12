namespace DBADashGUI.Performance
{
    partial class IOPerformance
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
            chartIO = new LiveCharts.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblIOPerformance = new System.Windows.Forms.ToolStripLabel();
            tsMeasures = new System.Windows.Forms.ToolStripButton();
            tsDrives = new System.Windows.Forms.ToolStripDropDownButton();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            latencyLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency10 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency50 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency100 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency200 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency500 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency1000 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency2000 = new System.Windows.Forms.ToolStripMenuItem();
            tsLatency5000 = new System.Windows.Forms.ToolStripMenuItem();
            tsFileGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsIOSummary = new System.Windows.Forms.ToolStripButton();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // chartIO
            // 
            chartIO.Dock = System.Windows.Forms.DockStyle.Fill;
            chartIO.Location = new System.Drawing.Point(0, 27);
            chartIO.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chartIO.Name = "chartIO";
            chartIO.Size = new System.Drawing.Size(773, 341);
            chartIO.TabIndex = 4;
            chartIO.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGroup, tsClose, tsUp, lblIOPerformance, tsMeasures, tsDrives, tsOptions, tsFileGroup, tsIOSummary });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(773, 27);
            toolStrip1.TabIndex = 5;
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
            // tsClose
            // 
            tsClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClose.Image = Properties.Resources.Close_red_16x;
            tsClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClose.Name = "tsClose";
            tsClose.Size = new System.Drawing.Size(29, 24);
            tsClose.Text = "Close";
            tsClose.Visible = false;
            tsClose.Click += TsClose_Click;
            // 
            // tsUp
            // 
            tsUp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsUp.Image = Properties.Resources.arrow_Up_16xLG;
            tsUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsUp.Name = "tsUp";
            tsUp.Size = new System.Drawing.Size(29, 24);
            tsUp.Text = "Move Up";
            tsUp.Click += TsUp_Click;
            // 
            // lblIOPerformance
            // 
            lblIOPerformance.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblIOPerformance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblIOPerformance.Name = "lblIOPerformance";
            lblIOPerformance.Size = new System.Drawing.Size(119, 24);
            lblIOPerformance.Text = "IO Performance";
            // 
            // tsMeasures
            // 
            tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasures.Name = "tsMeasures";
            tsMeasures.Size = new System.Drawing.Size(29, 24);
            tsMeasures.Text = "Measures";
            tsMeasures.Click += TsMeasures_Click;
            // 
            // tsDrives
            // 
            tsDrives.Image = Properties.Resources.Hard_Drive;
            tsDrives.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDrives.Name = "tsDrives";
            tsDrives.Size = new System.Drawing.Size(34, 24);
            // 
            // tsOptions
            // 
            tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { latencyLimitToolStripMenuItem });
            tsOptions.Image = Properties.Resources.SettingsOutline_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(34, 24);
            tsOptions.Text = "Options";
            // 
            // latencyLimitToolStripMenuItem
            // 
            latencyLimitToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsLatency10, tsLatency50, tsLatency100, tsLatency200, tsLatency500, tsLatency1000, tsLatency2000, tsLatency5000 });
            latencyLimitToolStripMenuItem.Name = "latencyLimitToolStripMenuItem";
            latencyLimitToolStripMenuItem.Size = new System.Drawing.Size(179, 26);
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
            // tsFileGroup
            // 
            tsFileGroup.Image = Properties.Resources.FilterDropdown_16x;
            tsFileGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFileGroup.Name = "tsFileGroup";
            tsFileGroup.Size = new System.Drawing.Size(106, 24);
            tsFileGroup.Text = "Filegroup";
            tsFileGroup.Visible = false;
            // 
            // tsIOSummary
            // 
            tsIOSummary.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsIOSummary.Image = Properties.Resources.Table_16x;
            tsIOSummary.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsIOSummary.Name = "tsIOSummary";
            tsIOSummary.Size = new System.Drawing.Size(29, 24);
            tsIOSummary.Text = "View Table Summary";
            tsIOSummary.Click += TsIOSummary_Click;
            // 
            // IOPerformance
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(chartIO);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "IOPerformance";
            Size = new System.Drawing.Size(773, 368);
            Load += IOPerformance_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartIO;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblIOPerformance;
        private System.Windows.Forms.ToolStripDropDownButton tsDrives;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsFileGroup;
        private System.Windows.Forms.ToolStripButton tsIOSummary;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem latencyLimitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsLatency10;
        private System.Windows.Forms.ToolStripMenuItem tsLatency50;
        private System.Windows.Forms.ToolStripMenuItem tsLatency100;
        private System.Windows.Forms.ToolStripMenuItem tsLatency200;
        private System.Windows.Forms.ToolStripMenuItem tsLatency500;
        private System.Windows.Forms.ToolStripMenuItem tsLatency1000;
        private System.Windows.Forms.ToolStripMenuItem tsLatency2000;
        private System.Windows.Forms.ToolStripMenuItem tsLatency5000;
        private System.Windows.Forms.ToolStripButton tsMeasures;
    }
}
