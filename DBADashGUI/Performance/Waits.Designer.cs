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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Waits));
            this.waitChart = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsClose = new System.Windows.Forms.ToolStripButton();
            this.tsUp = new System.Windows.Forms.ToolStripButton();
            this.lblWaits = new System.Windows.Forms.ToolStripLabel();
            this.tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsMetric = new System.Windows.Forms.ToolStripDropDownButton();
            this.avgWaitTimemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signalWaitsecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signalWaitMssecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signalWaitMsseccoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.totalWaitsecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitmssecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.msseccoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitingTasksCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalWaitsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stringFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsTopWaits = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // waitChart
            // 
            this.waitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitChart.Location = new System.Drawing.Point(0, 27);
            this.waitChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.waitChart.Name = "waitChart";
            this.waitChart.Size = new System.Drawing.Size(623, 350);
            this.waitChart.TabIndex = 0;
            this.waitChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsClose,
            this.tsUp,
            this.lblWaits,
            this.tsDateGrouping,
            this.tsMetric,
            this.tsFilter,
            this.tsTopWaits});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(623, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsClose
            // 
            this.tsClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsClose.Image = global::DBADashGUI.Properties.Resources.Close_red_16x;
            this.tsClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsClose.Name = "tsClose";
            this.tsClose.Size = new System.Drawing.Size(29, 24);
            this.tsClose.Text = "Close";
            this.tsClose.Visible = false;
            this.tsClose.Click += new System.EventHandler(this.TsClose_Click);
            // 
            // tsUp
            // 
            this.tsUp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsUp.Image = global::DBADashGUI.Properties.Resources.arrow_Up_16xLG;
            this.tsUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsUp.Name = "tsUp";
            this.tsUp.Size = new System.Drawing.Size(29, 24);
            this.tsUp.Text = "Move Up";
            this.tsUp.Visible = false;
            this.tsUp.Click += new System.EventHandler(this.TsUp_Click);
            // 
            // lblWaits
            // 
            this.lblWaits.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblWaits.Name = "lblWaits";
            this.lblWaits.Size = new System.Drawing.Size(119, 24);
            this.lblWaits.Text = "Waits : Instance";
            // 
            // tsDateGrouping
            // 
            this.tsDateGrouping.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGrouping.Name = "tsDateGrouping";
            this.tsDateGrouping.Size = new System.Drawing.Size(76, 24);
            this.tsDateGrouping.Text = "1min";
            // 
            // tsMetric
            // 
            this.tsMetric.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.avgWaitTimemsToolStripMenuItem,
            this.signalWaitsecToolStripMenuItem,
            this.signalWaitMssecToolStripMenuItem,
            this.signalWaitMsseccoreToolStripMenuItem,
            this.totalWaitsecToolStripMenuItem,
            this.waitmssecToolStripMenuItem,
            this.msseccoreToolStripMenuItem,
            this.waitingTasksCountToolStripMenuItem});
            this.tsMetric.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMetric.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMetric.Name = "tsMetric";
            this.tsMetric.Size = new System.Drawing.Size(133, 24);
            this.tsMetric.Tag = "WaitTimeMsPerSec";
            this.tsMetric.Text = "Wait (ms/sec)";
            // 
            // avgWaitTimemsToolStripMenuItem
            // 
            this.avgWaitTimemsToolStripMenuItem.Name = "avgWaitTimemsToolStripMenuItem";
            this.avgWaitTimemsToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.avgWaitTimemsToolStripMenuItem.Tag = "AvgWaitTimeMs";
            this.avgWaitTimemsToolStripMenuItem.Text = "Avg Wait Time (ms)";
            this.avgWaitTimemsToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // signalWaitsecToolStripMenuItem
            // 
            this.signalWaitsecToolStripMenuItem.Name = "signalWaitsecToolStripMenuItem";
            this.signalWaitsecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.signalWaitsecToolStripMenuItem.Tag = "SignalWaitSec";
            this.signalWaitsecToolStripMenuItem.Text = "Signal Wait (sec)";
            this.signalWaitsecToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // signalWaitMssecToolStripMenuItem
            // 
            this.signalWaitMssecToolStripMenuItem.Name = "signalWaitMssecToolStripMenuItem";
            this.signalWaitMssecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.signalWaitMssecToolStripMenuItem.Tag = "SignalWaitMsPerSec";
            this.signalWaitMssecToolStripMenuItem.Text = "Signal Wait ms/sec";
            this.signalWaitMssecToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // signalWaitMsseccoreToolStripMenuItem
            // 
            this.signalWaitMsseccoreToolStripMenuItem.Name = "signalWaitMsseccoreToolStripMenuItem";
            this.signalWaitMsseccoreToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.signalWaitMsseccoreToolStripMenuItem.Tag = "SignalWaitMsPerCorePerSec";
            this.signalWaitMsseccoreToolStripMenuItem.Text = "Signal Wait ms/sec/core";
            this.signalWaitMsseccoreToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // totalWaitsecToolStripMenuItem
            // 
            this.totalWaitsecToolStripMenuItem.Name = "totalWaitsecToolStripMenuItem";
            this.totalWaitsecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.totalWaitsecToolStripMenuItem.Tag = "TotalWaitSec";
            this.totalWaitsecToolStripMenuItem.Text = "Total Wait (sec)";
            this.totalWaitsecToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // waitmssecToolStripMenuItem
            // 
            this.waitmssecToolStripMenuItem.Checked = true;
            this.waitmssecToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.waitmssecToolStripMenuItem.Name = "waitmssecToolStripMenuItem";
            this.waitmssecToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.waitmssecToolStripMenuItem.Tag = "WaitTimeMsPerSec";
            this.waitmssecToolStripMenuItem.Text = "Wait (ms/sec)";
            this.waitmssecToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // msseccoreToolStripMenuItem
            // 
            this.msseccoreToolStripMenuItem.Name = "msseccoreToolStripMenuItem";
            this.msseccoreToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.msseccoreToolStripMenuItem.Tag = "WaitTimeMsPerCorePerSec";
            this.msseccoreToolStripMenuItem.Text = "Wait (ms/sec/core)";
            this.msseccoreToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // waitingTasksCountToolStripMenuItem
            // 
            this.waitingTasksCountToolStripMenuItem.Name = "waitingTasksCountToolStripMenuItem";
            this.waitingTasksCountToolStripMenuItem.Size = new System.Drawing.Size(252, 26);
            this.waitingTasksCountToolStripMenuItem.Tag = "WaitingTasksCount";
            this.waitingTasksCountToolStripMenuItem.Text = "Waiting Tasks Count";
            this.waitingTasksCountToolStripMenuItem.Click += new System.EventHandler(this.SelectMetric);
            // 
            // tsFilter
            // 
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalWaitsOnlyToolStripMenuItem,
            this.stringFilterToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 24);
            // 
            // criticalWaitsOnlyToolStripMenuItem
            // 
            this.criticalWaitsOnlyToolStripMenuItem.CheckOnClick = true;
            this.criticalWaitsOnlyToolStripMenuItem.Name = "criticalWaitsOnlyToolStripMenuItem";
            this.criticalWaitsOnlyToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.criticalWaitsOnlyToolStripMenuItem.Text = "Critical Waits Only";
            this.criticalWaitsOnlyToolStripMenuItem.ToolTipText = "Wait types that can indicate serious performance problems. e.g. RESOURCE_SEMAPHOR" +
    "E, THREADPOOL";
            this.criticalWaitsOnlyToolStripMenuItem.Click += new System.EventHandler(this.CriticalWaitsOnlyToolStripMenuItem_Click);
            // 
            // stringFilterToolStripMenuItem
            // 
            this.stringFilterToolStripMenuItem.Name = "stringFilterToolStripMenuItem";
            this.stringFilterToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.stringFilterToolStripMenuItem.Text = "String Filter";
            this.stringFilterToolStripMenuItem.Click += new System.EventHandler(this.StringFilterToolStripMenuItem_Click);
            // 
            // tsTopWaits
            // 
            this.tsTopWaits.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsTopWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.tsTopWaits.Image = ((System.Drawing.Image)(resources.GetObject("tsTopWaits.Image")));
            this.tsTopWaits.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTopWaits.Name = "tsTopWaits";
            this.tsTopWaits.Size = new System.Drawing.Size(78, 24);
            this.tsTopWaits.Text = "Top Waits";
            this.tsTopWaits.Click += new System.EventHandler(this.TsTopWaits_Click);
            // 
            // Waits
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.waitChart);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Waits";
            this.Size = new System.Drawing.Size(623, 377);
            this.Load += new System.EventHandler(this.Waits_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart waitChart;
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
    }
}
