﻿namespace DBADashGUI.Performance
{
    partial class Performance
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
            this.components = new System.ComponentModel.Container();
            DBADashGUI.CPUMetric cpuMetric1 = new DBADashGUI.CPUMetric();
            DBADashGUI.ObjectExecutionMetric objectExecutionMetric1 = new DBADashGUI.ObjectExecutionMetric();
            DBADashGUI.IOMetric ioMetric1 = new DBADashGUI.IOMetric();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Performance));
            DBADashGUI.BlockingMetric blockingMetric1 = new DBADashGUI.BlockingMetric();
            DBADashGUI.WaitMetric waitMetric1 = new DBADashGUI.WaitMetric();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataPointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cpu1 = new DBADashGUI.Performance.CPU();
            this.objectExecution1 = new DBADashGUI.Performance.ObjectExecution();
            this.ioPerformance1 = new DBADashGUI.Performance.IOPerformance();
            this.blocking1 = new DBADashGUI.Performance.Blocking();
            this.waits1 = new DBADashGUI.Performance.Waits();
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1,
            this.tsRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1757, 27);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smoothLinesToolStripMenuItem,
            this.dataPointsToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.LineChart_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            this.toolStripDropDownButton1.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            this.smoothLinesToolStripMenuItem.Checked = true;
            this.smoothLinesToolStripMenuItem.CheckOnClick = true;
            this.smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            this.smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.smoothLinesToolStripMenuItem.Text = "Smooth Lines";
            this.smoothLinesToolStripMenuItem.Click += new System.EventHandler(this.SmoothLinesToolStripMenuItem_Click);
            // 
            // dataPointsToolStripMenuItem
            // 
            this.dataPointsToolStripMenuItem.Checked = true;
            this.dataPointsToolStripMenuItem.CheckOnClick = true;
            this.dataPointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dataPointsToolStripMenuItem.Name = "dataPointsToolStripMenuItem";
            this.dataPointsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.dataPointsToolStripMenuItem.Text = "Data Points";
            this.dataPointsToolStripMenuItem.Click += new System.EventHandler(this.DataPointsToolStripMenuItem_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.TsRefresh_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.cpu1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.objectExecution1, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.ioPerformance1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.blocking1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.waits1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 27);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1757, 2338);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // cpu1
            // 
            this.cpu1.CloseVisible = false;
            this.cpu1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cpu1.InstanceID = 0;
            this.cpu1.Location = new System.Drawing.Point(3, 5);
            this.cpu1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            cpuMetric1.AggregateType = DBADashGUI.Performance.IMetric.AggregateTypes.Avg;
            this.cpu1.Metric = cpuMetric1;
            this.cpu1.MoveUpVisible = false;
            this.cpu1.Name = "cpu1";
            this.cpu1.Size = new System.Drawing.Size(1751, 457);
            this.cpu1.SmoothLines = true;
            this.cpu1.TabIndex = 4;
            // 
            // objectExecution1
            // 
            this.objectExecution1.CloseVisible = false;
            this.objectExecution1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecution1.Location = new System.Drawing.Point(3, 1873);
            this.objectExecution1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            objectExecutionMetric1.Measure = "TotalDuration";
            this.objectExecution1.Metric = objectExecutionMetric1;
            this.objectExecution1.MoveUpVisible = false;
            this.objectExecution1.Name = "objectExecution1";
            this.objectExecution1.Size = new System.Drawing.Size(1751, 460);
            this.objectExecution1.TabIndex = 7;
            // 
            // ioPerformance1
            // 
            this.ioPerformance1.CloseVisible = false;
            this.ioPerformance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioPerformance1.Drive = "";
            this.ioPerformance1.FileGroup = "";
            this.ioPerformance1.Location = new System.Drawing.Point(3, 472);
            this.ioPerformance1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            ioMetric1.Drive = "";
            ioMetric1.VisibleMetrics = ((System.Collections.Generic.List<string>)(resources.GetObject("ioMetric1.VisibleMetrics")));
            this.ioPerformance1.Metric = ioMetric1;
            this.ioPerformance1.MoveUpVisible = false;
            this.ioPerformance1.Name = "ioPerformance1";
            this.ioPerformance1.Size = new System.Drawing.Size(1751, 457);
            this.ioPerformance1.SmoothLines = true;
            this.ioPerformance1.TabIndex = 3;
            // 
            // blocking1
            // 
            this.blocking1.CloseVisible = false;
            this.blocking1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.blocking1.Location = new System.Drawing.Point(3, 1406);
            this.blocking1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.blocking1.Metric = blockingMetric1;
            this.blocking1.MoveUpVisible = false;
            this.blocking1.Name = "blocking1";
            this.blocking1.Size = new System.Drawing.Size(1751, 457);
            this.blocking1.TabIndex = 6;
            // 
            // waits1
            // 
            this.waits1.CloseVisible = false;
            this.waits1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waits1.Location = new System.Drawing.Point(3, 939);
            this.waits1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            waitMetric1.CriticalWaitsOnly = false;
            waitMetric1.WaitType = null;
            this.waits1.Metric = waitMetric1;
            this.waits1.MoveUpVisible = false;
            this.waits1.Name = "waits1";
            this.waits1.Size = new System.Drawing.Size(1751, 457);
            this.waits1.TabIndex = 5;
            this.waits1.WaitType = null;
            // 
            // Performance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Performance";
            this.Size = new System.Drawing.Size(1757, 2365);
            this.Load += new System.EventHandler(this.Performance_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.Timer timer1;
        private DBADashGUI.Performance.IOPerformance ioPerformance1;
        private CPU cpu1;
        private Waits waits1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private Blocking blocking1;
        private ObjectExecution objectExecution1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem dataPointsToolStripMenuItem;
    }
}
