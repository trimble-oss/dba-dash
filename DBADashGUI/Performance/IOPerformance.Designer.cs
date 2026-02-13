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
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IOPerformance));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
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
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsFileGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsIOSummary = new System.Windows.Forms.ToolStripButton();
            chartIO = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGroup, tsClose, tsUp, lblIOPerformance, tsMeasures, tsDrives, tsOptions, tsLegend, tsFileGroup, tsIOSummary });
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
            lblIOPerformance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
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
            // tsLegend
            // 
            tsLegend.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsLegend.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { leftToolStripMenuItem, rightToolStripMenuItem, topToolStripMenuItem, bottomToolStripMenuItem, hiddenToolStripMenuItem });
            tsLegend.Image = Properties.Resources.LegendHS;
            tsLegend.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLegend.Name = "tsLegend";
            tsLegend.Size = new System.Drawing.Size(34, 24);
            tsLegend.Text = "Legend Position";
            // 
            // leftToolStripMenuItem
            // 
            leftToolStripMenuItem.Name = "leftToolStripMenuItem";
            leftToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            leftToolStripMenuItem.Tag = "Left";
            leftToolStripMenuItem.Text = "Left";
            leftToolStripMenuItem.Click += SetLegendPosition;
            // 
            // rightToolStripMenuItem
            // 
            rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            rightToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            rightToolStripMenuItem.Tag = "Right";
            rightToolStripMenuItem.Text = "Right";
            rightToolStripMenuItem.Click += SetLegendPosition;
            // 
            // topToolStripMenuItem
            // 
            topToolStripMenuItem.Name = "topToolStripMenuItem";
            topToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            topToolStripMenuItem.Tag = "Top";
            topToolStripMenuItem.Text = "Top";
            topToolStripMenuItem.Click += SetLegendPosition;
            // 
            // bottomToolStripMenuItem
            // 
            bottomToolStripMenuItem.Name = "bottomToolStripMenuItem";
            bottomToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            bottomToolStripMenuItem.Tag = "Bottom";
            bottomToolStripMenuItem.Text = "Bottom";
            bottomToolStripMenuItem.Click += SetLegendPosition;
            // 
            // hiddenToolStripMenuItem
            // 
            hiddenToolStripMenuItem.Checked = true;
            hiddenToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            hiddenToolStripMenuItem.Name = "hiddenToolStripMenuItem";
            hiddenToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            hiddenToolStripMenuItem.Tag = "Hidden";
            hiddenToolStripMenuItem.Text = "Hidden";
            hiddenToolStripMenuItem.Click += SetLegendPosition;
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
            // chartIO
            // 
            chartIO.AutoUpdateEnabled = true;
            chartIO.ChartTheme = null;
            chartIO.Dock = System.Windows.Forms.DockStyle.Fill;
            chartIO.ForceGPU = false;
            skDefaultLegend1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend1.Content = null;
            skDefaultLegend1.IsValid = false;
            skDefaultLegend1.Opacity = 1F;
            padding1.Bottom = 0F;
            padding1.Left = 0F;
            padding1.Right = 0F;
            padding1.Top = 0F;
            skDefaultLegend1.Padding = padding1;
            skDefaultLegend1.RemoveOnCompleted = false;
            skDefaultLegend1.RotateTransform = 0F;
            skDefaultLegend1.X = 0F;
            skDefaultLegend1.Y = 0F;
            chartIO.Legend = skDefaultLegend1;
            chartIO.Location = new System.Drawing.Point(0, 27);
            chartIO.MatchAxesScreenDataRatio = false;
            chartIO.Name = "chartIO";
            chartIO.Size = new System.Drawing.Size(773, 341);
            chartIO.TabIndex = 6;
            skDefaultTooltip1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip1.Content = null;
            skDefaultTooltip1.IsValid = false;
            skDefaultTooltip1.Opacity = 1F;
            padding2.Bottom = 0F;
            padding2.Left = 0F;
            padding2.Right = 0F;
            padding2.Top = 0F;
            skDefaultTooltip1.Padding = padding2;
            skDefaultTooltip1.RemoveOnCompleted = false;
            skDefaultTooltip1.RotateTransform = 0F;
            skDefaultTooltip1.Wedge = 10;
            skDefaultTooltip1.X = 0F;
            skDefaultTooltip1.Y = 0F;
            chartIO.Tooltip = skDefaultTooltip1;
            chartIO.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
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
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chartIO;
        private System.Windows.Forms.ToolStripDropDownButton tsLegend;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hiddenToolStripMenuItem;
    }
}
