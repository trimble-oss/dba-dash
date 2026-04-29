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
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PerformanceCounters));
            LiveChartsCore.Drawing.Padding padding3 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding4 = new LiveChartsCore.Drawing.Padding();
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
            tsTitle = new System.Windows.Forms.ToolStripLabel();
            chart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGrouping, tsAgg, tsClose, tsUp, tsTitle, tsLegend });
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
            // tsTitle
            // 
            tsTitle.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsTitle.Name = "tsTitle";
            tsTitle.Size = new System.Drawing.Size(54, 24);
            tsTitle.Text = "Metric";
            // 
            // chart1
            // 
            chart1.AutoUpdateEnabled = true;
            chart1.ChartTheme = null;
            chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            skDefaultLegend2.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend2.Content = null;
            skDefaultLegend2.IsValid = false;
            skDefaultLegend2.Opacity = 1F;
            padding3.Bottom = 0F;
            padding3.Left = 0F;
            padding3.Right = 0F;
            padding3.Top = 0F;
            skDefaultLegend2.Padding = padding3;
            skDefaultLegend2.RemoveOnCompleted = false;
            skDefaultLegend2.RotateTransform = 0F;
            skDefaultLegend2.X = 0F;
            skDefaultLegend2.Y = 0F;
            chart1.Legend = skDefaultLegend2;
            chart1.Location = new System.Drawing.Point(0, 27);
            chart1.MatchAxesScreenDataRatio = false;
            chart1.Name = "chart1";
            chart1.Size = new System.Drawing.Size(1299, 965);
            chart1.TabIndex = 2;
            skDefaultTooltip2.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip2.Content = null;
            skDefaultTooltip2.IsValid = false;
            skDefaultTooltip2.Opacity = 1F;
            padding4.Bottom = 0F;
            padding4.Left = 0F;
            padding4.Right = 0F;
            padding4.Top = 0F;
            skDefaultTooltip2.Padding = padding4;
            skDefaultTooltip2.RemoveOnCompleted = false;
            skDefaultTooltip2.RotateTransform = 0F;
            skDefaultTooltip2.Wedge = 10;
            skDefaultTooltip2.X = 0F;
            skDefaultTooltip2.Y = 0F;
            chart1.Tooltip = skDefaultTooltip2;
            chart1.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
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
        private System.Windows.Forms.ToolStripDropDownButton tsLegend;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hiddenToolStripMenuItem;
    }
}
