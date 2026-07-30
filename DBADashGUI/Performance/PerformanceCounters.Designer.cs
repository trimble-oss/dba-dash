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
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsCounters = new System.Windows.Forms.ToolStripDropDownButton();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            tsChartType = new System.Windows.Forms.ToolStripMenuItem();
            fillMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pointsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            smoothLinesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lineChartTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            areaChartTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            columnChartTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stackedColumnChartTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scatterChartTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsYAxis = new System.Windows.Forms.ToolStripMenuItem();
            autoYAxisMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            percentYAxisMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            customYAxisMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editTitleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            tsTitle = new System.Windows.Forms.ToolStripLabel();
            chart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateGrouping, tsLegend, tsCounters, tsConfigure, tsClose, tsUp, tsTitle });
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
            leftToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            leftToolStripMenuItem.Tag = "Left";
            leftToolStripMenuItem.Text = "Left";
            leftToolStripMenuItem.Click += SetLegendPosition;
            // 
            // rightToolStripMenuItem
            // 
            rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            rightToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            rightToolStripMenuItem.Tag = "Right";
            rightToolStripMenuItem.Text = "Right";
            rightToolStripMenuItem.Click += SetLegendPosition;
            // 
            // topToolStripMenuItem
            // 
            topToolStripMenuItem.Name = "topToolStripMenuItem";
            topToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            topToolStripMenuItem.Tag = "Top";
            topToolStripMenuItem.Text = "Top";
            topToolStripMenuItem.Click += SetLegendPosition;
            // 
            // bottomToolStripMenuItem
            // 
            bottomToolStripMenuItem.Name = "bottomToolStripMenuItem";
            bottomToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            bottomToolStripMenuItem.Tag = "Bottom";
            bottomToolStripMenuItem.Text = "Bottom";
            bottomToolStripMenuItem.Click += SetLegendPosition;
            // 
            // hiddenToolStripMenuItem
            // 
            hiddenToolStripMenuItem.Checked = true;
            hiddenToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            hiddenToolStripMenuItem.Name = "hiddenToolStripMenuItem";
            hiddenToolStripMenuItem.Size = new System.Drawing.Size(142, 26);
            hiddenToolStripMenuItem.Tag = "Hidden";
            hiddenToolStripMenuItem.Text = "Hidden";
            hiddenToolStripMenuItem.Click += SetLegendPosition;
            // 
            // tsCounters
            // 
            tsCounters.Image = Properties.Resources.AddComputedField_16x;
            tsCounters.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCounters.Name = "tsCounters";
            tsCounters.Size = new System.Drawing.Size(91, 24);
            tsCounters.Text = "Metrics";
            tsCounters.ToolTipText = "Show, hide or remove counters on this chart";
            tsCounters.DropDownOpening += TsCounters_DropDownOpening;
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsChartType, fillMenuItem, pointsMenuItem, smoothLinesMenuItem, tsYAxis, editTitleToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            tsConfigure.ToolTipText = "Add metrics, manage counters and axis settings";
            //
            // tsChartType
            //
            tsChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { lineChartTypeMenuItem, areaChartTypeMenuItem, columnChartTypeMenuItem, stackedColumnChartTypeMenuItem, scatterChartTypeMenuItem });
            tsChartType.Image = Properties.Resources.LineChart_16x;
            tsChartType.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsChartType.Name = "tsChartType";
            tsChartType.Size = new System.Drawing.Size(172, 26);
            tsChartType.Text = "Chart Type: Line";
            tsChartType.ToolTipText = "Change how the data is plotted";
            //
            // lineChartTypeMenuItem
            //
            lineChartTypeMenuItem.Checked = true;
            lineChartTypeMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            lineChartTypeMenuItem.Name = "lineChartTypeMenuItem";
            lineChartTypeMenuItem.Size = new System.Drawing.Size(180, 26);
            lineChartTypeMenuItem.Tag = "Line";
            lineChartTypeMenuItem.Text = "Line";
            lineChartTypeMenuItem.Click += TsChartType_Click;
            //
            // areaChartTypeMenuItem
            //
            areaChartTypeMenuItem.Name = "areaChartTypeMenuItem";
            areaChartTypeMenuItem.Size = new System.Drawing.Size(180, 26);
            areaChartTypeMenuItem.Tag = "StackedArea";
            areaChartTypeMenuItem.Text = "Stacked Area";
            areaChartTypeMenuItem.Click += TsChartType_Click;
            //
            // columnChartTypeMenuItem
            //
            columnChartTypeMenuItem.Name = "columnChartTypeMenuItem";
            columnChartTypeMenuItem.Size = new System.Drawing.Size(180, 26);
            columnChartTypeMenuItem.Tag = "Column";
            columnChartTypeMenuItem.Text = "Column";
            columnChartTypeMenuItem.Click += TsChartType_Click;
            //
            // stackedColumnChartTypeMenuItem
            //
            stackedColumnChartTypeMenuItem.Name = "stackedColumnChartTypeMenuItem";
            stackedColumnChartTypeMenuItem.Size = new System.Drawing.Size(180, 26);
            stackedColumnChartTypeMenuItem.Tag = "StackedColumn";
            stackedColumnChartTypeMenuItem.Text = "Stacked Column";
            stackedColumnChartTypeMenuItem.Click += TsChartType_Click;
            //
            // scatterChartTypeMenuItem
            //
            scatterChartTypeMenuItem.Name = "scatterChartTypeMenuItem";
            scatterChartTypeMenuItem.Size = new System.Drawing.Size(180, 26);
            scatterChartTypeMenuItem.Tag = "Scatter";
            scatterChartTypeMenuItem.Text = "Scatter";
            scatterChartTypeMenuItem.Click += TsChartType_Click;
            //
            // fillMenuItem
            //
            fillMenuItem.CheckOnClick = true;
            fillMenuItem.Name = "fillMenuItem";
            fillMenuItem.Size = new System.Drawing.Size(172, 26);
            fillMenuItem.Text = "Fill";
            fillMenuItem.ToolTipText = "Fill the area under the line (Line charts only)";
            fillMenuItem.Click += TsFill_Click;
            //
            // pointsMenuItem
            //
            pointsMenuItem.CheckOnClick = true;
            pointsMenuItem.Name = "pointsMenuItem";
            pointsMenuItem.Size = new System.Drawing.Size(172, 26);
            pointsMenuItem.Text = "Points";
            pointsMenuItem.ToolTipText = "Show data points on line/area charts (auto-hidden on dense data)";
            pointsMenuItem.Click += TsPoints_Click;
            //
            // smoothLinesMenuItem
            //
            smoothLinesMenuItem.CheckOnClick = true;
            smoothLinesMenuItem.Name = "smoothLinesMenuItem";
            smoothLinesMenuItem.Size = new System.Drawing.Size(172, 26);
            smoothLinesMenuItem.Text = "Smooth Lines";
            smoothLinesMenuItem.ToolTipText = "Use curved lines on line/area charts";
            smoothLinesMenuItem.Click += TsSmoothLines_Click;
            //
            // tsYAxis
            //
            tsYAxis.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { autoYAxisMenuItem, percentYAxisMenuItem, customYAxisMenuItem });
            tsYAxis.Image = Properties.Resources.Percentage_16x;
            tsYAxis.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsYAxis.Name = "tsYAxis";
            tsYAxis.Size = new System.Drawing.Size(172, 26);
            tsYAxis.Text = "Y-Axis: Auto";
            tsYAxis.ToolTipText = "Configure the Y-axis scale";
            // 
            // autoYAxisMenuItem
            // 
            autoYAxisMenuItem.Checked = true;
            autoYAxisMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            autoYAxisMenuItem.Name = "autoYAxisMenuItem";
            autoYAxisMenuItem.Size = new System.Drawing.Size(164, 26);
            autoYAxisMenuItem.Text = "Auto";
            autoYAxisMenuItem.Click += TsYAxisAuto_Click;
            // 
            // percentYAxisMenuItem
            // 
            percentYAxisMenuItem.Name = "percentYAxisMenuItem";
            percentYAxisMenuItem.Size = new System.Drawing.Size(164, 26);
            percentYAxisMenuItem.Text = "0 - 100 (%)";
            percentYAxisMenuItem.Click += TsYAxisPercent_Click;
            // 
            // customYAxisMenuItem
            // 
            customYAxisMenuItem.Name = "customYAxisMenuItem";
            customYAxisMenuItem.Size = new System.Drawing.Size(164, 26);
            customYAxisMenuItem.Text = "Custom...";
            customYAxisMenuItem.Click += TsYAxisCustom_Click;
            // 
            // editTitleToolStripMenuItem
            // 
            editTitleToolStripMenuItem.Image = Properties.Resources.Rename_16x;
            editTitleToolStripMenuItem.Name = "editTitleToolStripMenuItem";
            editTitleToolStripMenuItem.Size = new System.Drawing.Size(172, 26);
            editTitleToolStripMenuItem.Text = "Edit Title";
            editTitleToolStripMenuItem.Click += TsTitle_Click;
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
            tsTitle.ToolTipText = "Click to edit the chart label";
            // 
            // chart1
            // 
            chart1.AutoUpdateEnabled = true;
            chart1.ChartTheme = null;
            chart1.Dock = System.Windows.Forms.DockStyle.Fill;
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
        private System.Windows.Forms.ToolStripDropDownButton tsCounters;
        private System.Windows.Forms.ToolStripMenuItem tsChartType;
        private System.Windows.Forms.ToolStripMenuItem lineChartTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem areaChartTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem columnChartTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stackedColumnChartTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scatterChartTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fillMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pointsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsYAxis;
        private System.Windows.Forms.ToolStripMenuItem autoYAxisMenuItem;
        private System.Windows.Forms.ToolStripMenuItem percentYAxisMenuItem;
        private System.Windows.Forms.ToolStripMenuItem customYAxisMenuItem;
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
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem editTitleToolStripMenuItem;
    }
}
