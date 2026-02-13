namespace DBADashGUI.Performance
{
    partial class Waits
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Waits));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            waitChart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblWaits = new System.Windows.Forms.ToolStripLabel();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            tsMetric = new System.Windows.Forms.ToolStripDropDownButton();
            avgWaitTimemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            signalWaitsecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            signalWaitMssecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            signalWaitMsseccoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            totalWaitsecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            waitmssecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            msseccoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            waitingTasksCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            criticalWaitsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stringFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTopWaits = new System.Windows.Forms.ToolStripButton();
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // waitChart
            // 
            waitChart.AutoUpdateEnabled = true;
            waitChart.ChartTheme = null;
            waitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            waitChart.ForceGPU = false;
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
            waitChart.Legend = skDefaultLegend1;
            waitChart.Location = new System.Drawing.Point(0, 27);
            waitChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            waitChart.MatchAxesScreenDataRatio = false;
            waitChart.Name = "waitChart";
            waitChart.Size = new System.Drawing.Size(623, 350);
            waitChart.TabIndex = 0;
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
            waitChart.Tooltip = skDefaultTooltip1;
            waitChart.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblWaits, tsDateGrouping, tsMetric, tsLegend, tsFilter, tsTopWaits });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(623, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
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
            // lblWaits
            // 
            lblWaits.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblWaits.Name = "lblWaits";
            lblWaits.Size = new System.Drawing.Size(119, 24);
            lblWaits.Text = "Waits : Instance";
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.Image = Properties.Resources.Time_16x;
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(76, 24);
            tsDateGrouping.Text = "1min";
            // 
            // tsMetric
            // 
            tsMetric.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { avgWaitTimemsToolStripMenuItem, signalWaitsecToolStripMenuItem, signalWaitMssecToolStripMenuItem, signalWaitMsseccoreToolStripMenuItem, totalWaitsecToolStripMenuItem, waitmssecToolStripMenuItem, msseccoreToolStripMenuItem, waitingTasksCountToolStripMenuItem });
            tsMetric.Image = Properties.Resources.AddComputedField_16x;
            tsMetric.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMetric.Name = "tsMetric";
            tsMetric.Size = new System.Drawing.Size(133, 24);
            tsMetric.Tag = "WaitTimeMsPerSec";
            tsMetric.Text = "Wait (ms/sec)";
            // 
            // avgWaitTimemsToolStripMenuItem
            // 
            avgWaitTimemsToolStripMenuItem.Name = "avgWaitTimemsToolStripMenuItem";
            avgWaitTimemsToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            avgWaitTimemsToolStripMenuItem.Tag = "AvgWaitTimeMs";
            avgWaitTimemsToolStripMenuItem.Text = "Avg Wait Time (ms)";
            avgWaitTimemsToolStripMenuItem.Click += SelectMetric;
            // 
            // signalWaitsecToolStripMenuItem
            // 
            signalWaitsecToolStripMenuItem.Name = "signalWaitsecToolStripMenuItem";
            signalWaitsecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            signalWaitsecToolStripMenuItem.Tag = "SignalWaitSec";
            signalWaitsecToolStripMenuItem.Text = "Signal Wait (sec)";
            signalWaitsecToolStripMenuItem.Click += SelectMetric;
            // 
            // signalWaitMssecToolStripMenuItem
            // 
            signalWaitMssecToolStripMenuItem.Name = "signalWaitMssecToolStripMenuItem";
            signalWaitMssecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            signalWaitMssecToolStripMenuItem.Tag = "SignalWaitMsPerSec";
            signalWaitMssecToolStripMenuItem.Text = "Signal Wait ms/sec";
            signalWaitMssecToolStripMenuItem.Click += SelectMetric;
            // 
            // signalWaitMsseccoreToolStripMenuItem
            // 
            signalWaitMsseccoreToolStripMenuItem.Name = "signalWaitMsseccoreToolStripMenuItem";
            signalWaitMsseccoreToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            signalWaitMsseccoreToolStripMenuItem.Tag = "SignalWaitMsPerCorePerSec";
            signalWaitMsseccoreToolStripMenuItem.Text = "Signal Wait ms/sec/core";
            signalWaitMsseccoreToolStripMenuItem.Click += SelectMetric;
            // 
            // totalWaitsecToolStripMenuItem
            // 
            totalWaitsecToolStripMenuItem.Name = "totalWaitsecToolStripMenuItem";
            totalWaitsecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            totalWaitsecToolStripMenuItem.Tag = "TotalWaitSec";
            totalWaitsecToolStripMenuItem.Text = "Total Wait (sec)";
            totalWaitsecToolStripMenuItem.Click += SelectMetric;
            // 
            // waitmssecToolStripMenuItem
            // 
            waitmssecToolStripMenuItem.Checked = true;
            waitmssecToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            waitmssecToolStripMenuItem.Name = "waitmssecToolStripMenuItem";
            waitmssecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            waitmssecToolStripMenuItem.Tag = "WaitTimeMsPerSec";
            waitmssecToolStripMenuItem.Text = "Wait (ms/sec)";
            waitmssecToolStripMenuItem.Click += SelectMetric;
            // 
            // msseccoreToolStripMenuItem
            // 
            msseccoreToolStripMenuItem.Name = "msseccoreToolStripMenuItem";
            msseccoreToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            msseccoreToolStripMenuItem.Tag = "WaitTimeMsPerCorePerSec";
            msseccoreToolStripMenuItem.Text = "Wait (ms/sec/core)";
            msseccoreToolStripMenuItem.Click += SelectMetric;
            // 
            // waitingTasksCountToolStripMenuItem
            // 
            waitingTasksCountToolStripMenuItem.Name = "waitingTasksCountToolStripMenuItem";
            waitingTasksCountToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            waitingTasksCountToolStripMenuItem.Tag = "WaitingTasksCount";
            waitingTasksCountToolStripMenuItem.Text = "Waiting Tasks Count";
            waitingTasksCountToolStripMenuItem.Click += SelectMetric;
            // 
            // tsFilter
            // 
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { criticalWaitsOnlyToolStripMenuItem, stringFilterToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            // 
            // criticalWaitsOnlyToolStripMenuItem
            // 
            criticalWaitsOnlyToolStripMenuItem.CheckOnClick = true;
            criticalWaitsOnlyToolStripMenuItem.Name = "criticalWaitsOnlyToolStripMenuItem";
            criticalWaitsOnlyToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            criticalWaitsOnlyToolStripMenuItem.Text = "Critical Waits Only";
            criticalWaitsOnlyToolStripMenuItem.ToolTipText = "Wait types that can indicate serious performance problems. e.g. RESOURCE_SEMAPHORE, THREADPOOL";
            criticalWaitsOnlyToolStripMenuItem.Click += CriticalWaitsOnlyToolStripMenuItem_Click;
            // 
            // stringFilterToolStripMenuItem
            // 
            stringFilterToolStripMenuItem.Name = "stringFilterToolStripMenuItem";
            stringFilterToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            stringFilterToolStripMenuItem.Text = "String Filter";
            stringFilterToolStripMenuItem.Click += StringFilterToolStripMenuItem_Click;
            // 
            // tsTopWaits
            // 
            tsTopWaits.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsTopWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            tsTopWaits.Image = (System.Drawing.Image)resources.GetObject("tsTopWaits.Image");
            tsTopWaits.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTopWaits.Name = "tsTopWaits";
            tsTopWaits.Size = new System.Drawing.Size(78, 24);
            tsTopWaits.Text = "Top Waits";
            tsTopWaits.Click += TsTopWaits_Click;
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
            // Waits
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(waitChart);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Waits";
            Size = new System.Drawing.Size(623, 377);
            Load += Waits_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart waitChart;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblWaits;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalWaitsOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stringFilterToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private System.Windows.Forms.ToolStripDropDownButton tsMetric;
        private System.Windows.Forms.ToolStripMenuItem msseccoreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalWaitsecToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgWaitTimemsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signalWaitMssecToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signalWaitMsseccoreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signalWaitsecToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem waitingTasksCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsTopWaits;
        private System.Windows.Forms.ToolStripMenuItem waitmssecToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsLegend;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hiddenToolStripMenuItem;
    }
}
