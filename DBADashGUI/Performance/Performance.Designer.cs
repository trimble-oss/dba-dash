namespace DBADashGUI.Performance
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
            components = new System.ComponentModel.Container();
            CPUMetric cpuMetric1 = new CPUMetric();
            ObjectExecutionMetric objectExecutionMetric1 = new ObjectExecutionMetric();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Performance));
            IOMetric ioMetric1 = new IOMetric();
            BlockingMetric blockingMetric1 = new BlockingMetric();
            WaitMetric waitMetric1 = new WaitMetric();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dataPointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            timer1 = new System.Windows.Forms.Timer(components);
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            cpu1 = new CPU();
            objectExecution1 = new ObjectExecution();
            ioPerformance1 = new IOPerformance();
            blocking1 = new Blocking();
            waits1 = new Waits();
            toolStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripDropDownButton1, tsRefresh });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1757, 27);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { smoothLinesToolStripMenuItem, dataPointsToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.LineChart_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            smoothLinesToolStripMenuItem.Checked = true;
            smoothLinesToolStripMenuItem.CheckOnClick = true;
            smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            smoothLinesToolStripMenuItem.Text = "Smooth Lines";
            smoothLinesToolStripMenuItem.Click += SmoothLinesToolStripMenuItem_Click;
            // 
            // dataPointsToolStripMenuItem
            // 
            dataPointsToolStripMenuItem.Checked = true;
            dataPointsToolStripMenuItem.CheckOnClick = true;
            dataPointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            dataPointsToolStripMenuItem.Name = "dataPointsToolStripMenuItem";
            dataPointsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            dataPointsToolStripMenuItem.Text = "Data Points";
            dataPointsToolStripMenuItem.Click += DataPointsToolStripMenuItem_Click;
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
            // timer1
            // 
            timer1.Interval = 10000;
            timer1.Tick += Timer1_Tick;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(cpu1, 0, 0);
            tableLayoutPanel1.Controls.Add(objectExecution1, 0, 4);
            tableLayoutPanel1.Controls.Add(blocking1, 0, 3);
            tableLayoutPanel1.Controls.Add(ioPerformance1, 0, 2);
            tableLayoutPanel1.Controls.Add(waits1, 0, 1);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 27);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.Size = new System.Drawing.Size(1757, 2338);
            tableLayoutPanel1.TabIndex = 8;
            // 
            // cpu1
            // 
            cpu1.CloseVisible = false;
            cpu1.Dock = System.Windows.Forms.DockStyle.Fill;
            cpu1.InstanceID = 0;
            cpu1.Location = new System.Drawing.Point(3, 5);
            cpu1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            cpuMetric1.AggregateType = IMetric.AggregateTypes.Avg;
            cpu1.Metric = cpuMetric1;
            cpu1.MoveUpVisible = false;
            cpu1.Name = "cpu1";
            cpu1.Size = new System.Drawing.Size(1751, 457);
            cpu1.SmoothLines = true;
            cpu1.TabIndex = 4;
            // 
            // objectExecution1
            // 
            objectExecution1.CloseVisible = false;
            objectExecution1.Dock = System.Windows.Forms.DockStyle.Fill;
            objectExecution1.Location = new System.Drawing.Point(3, 1873);
            objectExecution1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            objectExecutionMetric1.Measure = "TotalDuration";
            objectExecution1.Metric = objectExecutionMetric1;
            objectExecution1.MoveUpVisible = false;
            objectExecution1.Name = "objectExecution1";
            objectExecution1.Size = new System.Drawing.Size(1751, 460);
            objectExecution1.TabIndex = 7;
            // 
            // ioPerformance1
            // 
            ioPerformance1.CloseVisible = false;
            ioPerformance1.DateGrouping = 0;
            ioPerformance1.DateGroupingVisible = true;
            ioPerformance1.Dock = System.Windows.Forms.DockStyle.Fill;
            ioPerformance1.Drive = "";
            ioPerformance1.DriveVisible = true;
            ioPerformance1.FileGroup = "";
            ioPerformance1.FileGroupVisible = false;
            ioPerformance1.Location = new System.Drawing.Point(3, 939);
            ioPerformance1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            ioPerformance1.MeasuresVisible = true;
            ioMetric1.Drive = "";
            ioMetric1.VisibleMetrics = (System.Collections.Generic.List<string>)resources.GetObject("ioMetric1.VisibleMetrics");
            ioPerformance1.Metric = ioMetric1;
            ioPerformance1.MoveUpVisible = false;
            ioPerformance1.Name = "ioPerformance1";
            ioPerformance1.OptionsVisible = true;
            ioPerformance1.Size = new System.Drawing.Size(1751, 457);
            ioPerformance1.SmoothLines = true;
            ioPerformance1.SummaryVisible = true;
            ioPerformance1.TabIndex = 3;
            // 
            // blocking1
            // 
            blocking1.CloseVisible = false;
            blocking1.Dock = System.Windows.Forms.DockStyle.Fill;
            blocking1.Location = new System.Drawing.Point(3, 1406);
            blocking1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            blocking1.Metric = blockingMetric1;
            blocking1.MoveUpVisible = false;
            blocking1.Name = "blocking1";
            blocking1.Size = new System.Drawing.Size(1751, 457);
            blocking1.TabIndex = 6;
            // 
            // waits1
            // 
            waits1.CloseVisible = false;
            waits1.Dock = System.Windows.Forms.DockStyle.Fill;
            waits1.Location = new System.Drawing.Point(3, 472);
            waits1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            waitMetric1.CriticalWaitsOnly = false;
            waitMetric1.WaitType = null;
            waits1.Metric = waitMetric1;
            waits1.MoveUpVisible = false;
            waits1.Name = "waits1";
            waits1.Size = new System.Drawing.Size(1751, 457);
            waits1.TabIndex = 5;
            waits1.WaitType = null;
            // 
            // Performance
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(tableLayoutPanel1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Performance";
            Size = new System.Drawing.Size(1757, 2365);
            Load += Performance_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
