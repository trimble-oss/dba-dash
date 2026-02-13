namespace DBADashGUI.Performance
{
    partial class AzureDBResourceStats
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureDBResourceStats));
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            LiveChartsCore.Drawing.Padding padding3 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding4 = new LiveChartsCore.Drawing.Padding();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            chartDB = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip3 = new System.Windows.Forms.ToolStrip();
            tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            tsDB = new System.Windows.Forms.ToolStripLabel();
            chartPool = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsPool = new System.Windows.Forms.ToolStripLabel();
            tsPoolMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip3.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsOptions, tsDateGrouping });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(882, 27);
            toolStrip1.TabIndex = 4;
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
            // tsOptions
            // 
            tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { smoothLinesToolStripMenuItem, pointsToolStripMenuItem });
            tsOptions.Image = Properties.Resources.LineChart_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(34, 24);
            tsOptions.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            smoothLinesToolStripMenuItem.Checked = true;
            smoothLinesToolStripMenuItem.CheckOnClick = true;
            smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            smoothLinesToolStripMenuItem.Text = "Smooth lines";
            smoothLinesToolStripMenuItem.Click += SmoothLinesToolStripMenuItem_Click;
            // 
            // pointsToolStripMenuItem
            // 
            pointsToolStripMenuItem.Checked = true;
            pointsToolStripMenuItem.CheckOnClick = true;
            pointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            pointsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            pointsToolStripMenuItem.Text = "Points";
            pointsToolStripMenuItem.Click += PointsToolStripMenuItem_Click;
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsDateGrouping.Image = (System.Drawing.Image)resources.GetObject("tsDateGrouping.Image");
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(59, 24);
            tsDateGrouping.Text = "None";
            tsDateGrouping.ToolTipText = "Date Grouping";
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
            splitContainer1.Panel1.Controls.Add(chartDB);
            splitContainer1.Panel1.Controls.Add(toolStrip3);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(chartPool);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(882, 629);
            splitContainer1.SplitterDistance = 311;
            splitContainer1.TabIndex = 6;
            // 
            // chartDB
            // 
            chartDB.AutoUpdateEnabled = true;
            chartDB.ChartTheme = null;
            chartDB.Dock = System.Windows.Forms.DockStyle.Fill;
            chartDB.ForceGPU = false;
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
            chartDB.Legend = skDefaultLegend1;
            chartDB.Location = new System.Drawing.Point(0, 27);
            chartDB.MatchAxesScreenDataRatio = false;
            chartDB.Name = "chartDB";
            chartDB.Size = new System.Drawing.Size(882, 284);
            chartDB.TabIndex = 2;
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
            chartDB.Tooltip = skDefaultTooltip1;
            chartDB.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // toolStrip3
            // 
            toolStrip3.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsMeasures, tsDB });
            toolStrip3.Location = new System.Drawing.Point(0, 0);
            toolStrip3.Name = "toolStrip3";
            toolStrip3.Size = new System.Drawing.Size(882, 27);
            toolStrip3.TabIndex = 1;
            toolStrip3.Text = "toolStrip3";
            // 
            // tsMeasures
            // 
            tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasures.Name = "tsMeasures";
            tsMeasures.Size = new System.Drawing.Size(34, 24);
            tsMeasures.Text = "Columns";
            // 
            // tsDB
            // 
            tsDB.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDB.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsDB.Name = "tsDB";
            tsDB.Size = new System.Drawing.Size(38, 24);
            tsDB.Text = "DB: ";
            // 
            // chartPool
            // 
            chartPool.AutoUpdateEnabled = true;
            chartPool.ChartTheme = null;
            chartPool.Dock = System.Windows.Forms.DockStyle.Fill;
            chartPool.ForceGPU = false;
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
            chartPool.Legend = skDefaultLegend2;
            chartPool.Location = new System.Drawing.Point(0, 27);
            chartPool.MatchAxesScreenDataRatio = false;
            chartPool.Name = "chartPool";
            chartPool.Size = new System.Drawing.Size(882, 287);
            chartPool.TabIndex = 7;
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
            chartPool.Tooltip = skDefaultTooltip2;
            chartPool.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsPool, tsPoolMeasures });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(882, 27);
            toolStrip2.TabIndex = 6;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsPool
            // 
            tsPool.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsPool.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsPool.Name = "tsPool";
            tsPool.Size = new System.Drawing.Size(88, 24);
            tsPool.Text = "Elastic Pool";
            // 
            // tsPoolMeasures
            // 
            tsPoolMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPoolMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsPoolMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPoolMeasures.Name = "tsPoolMeasures";
            tsPoolMeasures.Size = new System.Drawing.Size(34, 24);
            tsPoolMeasures.Text = "Columns";
            // 
            // AzureDBResourceStats
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AzureDBResourceStats";
            Size = new System.Drawing.Size(882, 656);
            Load += AzureDBResourceStats_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip3.ResumeLayout(false);
            toolStrip3.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pointsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel tsPool;
        private System.Windows.Forms.ToolStripDropDownButton tsPoolMeasures;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripLabel tsDB;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chartDB;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chartPool;
    }
}
