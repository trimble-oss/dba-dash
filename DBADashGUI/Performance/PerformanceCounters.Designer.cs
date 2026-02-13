namespace DBADashGUI.Performance
{
    partial class PerformanceCounters
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PerformanceCounters));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            totalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            maxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sampleCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            chart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            tsTitle = new System.Windows.Forms.ToolStripLabel();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGrouping, tsAgg, tsClose, tsUp, tsTitle });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1299, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.Image = Properties.Resources.Time_16x;
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(120, 24);
            tsDateGrouping.Text = "Date Group";
            // 
            // tsAgg
            // 
            tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { totalToolStripMenuItem, minToolStripMenuItem, maxToolStripMenuItem, avgToolStripMenuItem, sampleCountToolStripMenuItem });
            tsAgg.Image = Properties.Resources.AddComputedField_16x;
            tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsAgg.Name = "tsAgg";
            tsAgg.Size = new System.Drawing.Size(69, 24);
            tsAgg.Text = "Avg";
            // 
            // totalToolStripMenuItem
            // 
            totalToolStripMenuItem.Name = "totalToolStripMenuItem";
            totalToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            totalToolStripMenuItem.Tag = "Total";
            totalToolStripMenuItem.Text = "Total";
            totalToolStripMenuItem.Click += TsAgg_Click;
            // 
            // minToolStripMenuItem
            // 
            minToolStripMenuItem.Name = "minToolStripMenuItem";
            minToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            minToolStripMenuItem.Tag = "Min";
            minToolStripMenuItem.Text = "Min";
            minToolStripMenuItem.Click += TsAgg_Click;
            // 
            // maxToolStripMenuItem
            // 
            maxToolStripMenuItem.Name = "maxToolStripMenuItem";
            maxToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            maxToolStripMenuItem.Tag = "Max";
            maxToolStripMenuItem.Text = "Max";
            maxToolStripMenuItem.Click += TsAgg_Click;
            // 
            // avgToolStripMenuItem
            // 
            avgToolStripMenuItem.Checked = true;
            avgToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            avgToolStripMenuItem.Name = "avgToolStripMenuItem";
            avgToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            avgToolStripMenuItem.Tag = "Avg";
            avgToolStripMenuItem.Text = "Avg";
            avgToolStripMenuItem.Click += TsAgg_Click;
            // 
            // sampleCountToolStripMenuItem
            // 
            sampleCountToolStripMenuItem.Name = "sampleCountToolStripMenuItem";
            sampleCountToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            sampleCountToolStripMenuItem.Tag = "SampleCount";
            sampleCountToolStripMenuItem.Text = "Sample Count";
            sampleCountToolStripMenuItem.Click += TsAgg_Click;
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
            tsUp.Visible = false;
            tsUp.Click += TsUp_Click;
            // 
            // chart1
            // 
            chart1.AutoUpdateEnabled = true;
            chart1.ChartTheme = null;
            chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            chart1.ForceGPU = false;
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
            chart1.Legend = skDefaultLegend1;
            chart1.Location = new System.Drawing.Point(0, 27);
            chart1.MatchAxesScreenDataRatio = false;
            chart1.Name = "chart1";
            chart1.Size = new System.Drawing.Size(1299, 965);
            chart1.TabIndex = 2;
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
            chart1.Tooltip = skDefaultTooltip1;
            chart1.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // tsTitle
            // 
            tsTitle.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsTitle.Name = "tsTitle";
            tsTitle.Size = new System.Drawing.Size(54, 24);
            tsTitle.Text = "Metric";
            // 
            // PerformanceCounters
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(chart1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "PerformanceCounters";
            Size = new System.Drawing.Size(1299, 992);
            Load += PerformanceCounters_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private System.Windows.Forms.ToolStripDropDownButton tsAgg;
        private System.Windows.Forms.ToolStripMenuItem totalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem maxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sampleCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chart1;
        private System.Windows.Forms.ToolStripLabel tsTitle;
    }
}
