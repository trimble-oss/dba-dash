namespace DBADashGUI.Performance
{
    partial class QueryStorePlanChart
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
            planChart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            tsCopyImage = new System.Windows.Forms.ToolStripMenuItem();
            tsCopyData = new System.Windows.Forms.ToolStripMenuItem();
            tsSave = new System.Windows.Forms.ToolStripMenuItem();
            refresh1 = new Refresh();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            setYAxisMaxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setYAxisMinToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsMeasure = new System.Windows.Forms.ToolStripDropDownButton();
            totalCPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgCPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            totalDurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgDurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            executionCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            maxMemoryGrantToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            totalPhysicalReadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgPhysicalReadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsCopyImage2 = new System.Windows.Forms.ToolStripButton();
            tsCopyData2 = new System.Windows.Forms.ToolStripButton();
            contextMenuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // planChart
            // 
            planChart.ContextMenuStrip = contextMenuStrip1;
            planChart.Dock = System.Windows.Forms.DockStyle.Fill;
            planChart.Location = new System.Drawing.Point(0, 27);
            planChart.Name = "planChart";
            planChart.Size = new System.Drawing.Size(1189, 530);
            planChart.TabIndex = 0;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyImage, tsCopyData, tsSave });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(163, 82);
            // 
            // tsCopyImage
            // 
            tsCopyImage.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyImage.Name = "tsCopyImage";
            tsCopyImage.Size = new System.Drawing.Size(162, 26);
            tsCopyImage.Text = "Copy Image";
            tsCopyImage.Click += CopyImage;
            // 
            // tsCopyData
            // 
            tsCopyData.Image = Properties.Resources.ASX_Copy_grey_16x;
            tsCopyData.Name = "tsCopyData";
            tsCopyData.Size = new System.Drawing.Size(162, 26);
            tsCopyData.Text = "Copy Data";
            tsCopyData.Click += CopyData;
            // 
            // tsSave
            // 
            tsSave.Image = Properties.Resources.Save_16x;
            tsSave.Name = "tsSave";
            tsSave.Size = new System.Drawing.Size(162, 26);
            tsSave.Text = "Save As";
            tsSave.Click += SaveChart;
            // 
            // refresh1
            // 
            refresh1.Dock = System.Windows.Forms.DockStyle.Fill;
            refresh1.Font = new System.Drawing.Font("Segoe UI", 11F);
            refresh1.Location = new System.Drawing.Point(0, 27);
            refresh1.Margin = new System.Windows.Forms.Padding(4);
            refresh1.Name = "refresh1";
            refresh1.Size = new System.Drawing.Size(1189, 530);
            refresh1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyImage2, tsCopyData2, tsConfigure, tsMeasure, toolStripLabel1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1189, 27);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { setYAxisMaxToolStripMenuItem, setYAxisMinToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // setYAxisMaxToolStripMenuItem
            // 
            setYAxisMaxToolStripMenuItem.Name = "setYAxisMaxToolStripMenuItem";
            setYAxisMaxToolStripMenuItem.Size = new System.Drawing.Size(188, 26);
            setYAxisMaxToolStripMenuItem.Text = "Set Y Axis Max";
            setYAxisMaxToolStripMenuItem.Click += SetYAxisMaxToolStripMenuItem_Click;
            // 
            // setYAxisMinToolStripMenuItem
            // 
            setYAxisMinToolStripMenuItem.Name = "setYAxisMinToolStripMenuItem";
            setYAxisMinToolStripMenuItem.Size = new System.Drawing.Size(188, 26);
            setYAxisMinToolStripMenuItem.Text = "Set Y Axis Min";
            setYAxisMinToolStripMenuItem.Click += SetYAxisMinToolStripMenuItem_Click;
            // 
            // tsMeasure
            // 
            tsMeasure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { totalCPUToolStripMenuItem, avgCPUToolStripMenuItem, totalDurationToolStripMenuItem, avgDurationToolStripMenuItem, executionCountToolStripMenuItem, maxMemoryGrantToolStripMenuItem, totalPhysicalReadsToolStripMenuItem, avgPhysicalReadsToolStripMenuItem });
            tsMeasure.Image = Properties.Resources.AddComputedField_16x;
            tsMeasure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasure.Name = "tsMeasure";
            tsMeasure.Size = new System.Drawing.Size(133, 24);
            tsMeasure.Tag = "avg_cpu_time_ms";
            tsMeasure.Text = "Avg CPU (ms)";
            // 
            // totalCPUToolStripMenuItem
            // 
            totalCPUToolStripMenuItem.Name = "totalCPUToolStripMenuItem";
            totalCPUToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            totalCPUToolStripMenuItem.Tag = "total_cpu_time_ms";
            totalCPUToolStripMenuItem.Text = "Total CPU (ms)";
            totalCPUToolStripMenuItem.Click += Select_Measure;
            // 
            // avgCPUToolStripMenuItem
            // 
            avgCPUToolStripMenuItem.Name = "avgCPUToolStripMenuItem";
            avgCPUToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            avgCPUToolStripMenuItem.Tag = "avg_cpu_time_ms";
            avgCPUToolStripMenuItem.Text = "Avg CPU (ms)";
            avgCPUToolStripMenuItem.Click += Select_Measure;
            // 
            // totalDurationToolStripMenuItem
            // 
            totalDurationToolStripMenuItem.Name = "totalDurationToolStripMenuItem";
            totalDurationToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            totalDurationToolStripMenuItem.Tag = "total_duration_ms";
            totalDurationToolStripMenuItem.Text = "Total Duration (ms)";
            totalDurationToolStripMenuItem.Click += Select_Measure;
            // 
            // avgDurationToolStripMenuItem
            // 
            avgDurationToolStripMenuItem.Name = "avgDurationToolStripMenuItem";
            avgDurationToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            avgDurationToolStripMenuItem.Tag = "avg_duration_ms";
            avgDurationToolStripMenuItem.Text = "Avg Duration (ms)";
            avgDurationToolStripMenuItem.Click += Select_Measure;
            // 
            // executionCountToolStripMenuItem
            // 
            executionCountToolStripMenuItem.Name = "executionCountToolStripMenuItem";
            executionCountToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            executionCountToolStripMenuItem.Tag = "count_executions";
            executionCountToolStripMenuItem.Text = "Execution Count";
            executionCountToolStripMenuItem.Click += Select_Measure;
            // 
            // maxMemoryGrantToolStripMenuItem
            // 
            maxMemoryGrantToolStripMenuItem.Name = "maxMemoryGrantToolStripMenuItem";
            maxMemoryGrantToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            maxMemoryGrantToolStripMenuItem.Tag = "max_memory_grant_kb";
            maxMemoryGrantToolStripMenuItem.Text = "Max Memory Grant (KB)";
            maxMemoryGrantToolStripMenuItem.Click += Select_Measure;
            // 
            // totalPhysicalReadsToolStripMenuItem
            // 
            totalPhysicalReadsToolStripMenuItem.Name = "totalPhysicalReadsToolStripMenuItem";
            totalPhysicalReadsToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            totalPhysicalReadsToolStripMenuItem.Tag = "total_physical_io_reads_kb";
            totalPhysicalReadsToolStripMenuItem.Text = "Total Physical Reads (KB)";
            totalPhysicalReadsToolStripMenuItem.Click += Select_Measure;
            // 
            // avgPhysicalReadsToolStripMenuItem
            // 
            avgPhysicalReadsToolStripMenuItem.Name = "avgPhysicalReadsToolStripMenuItem";
            avgPhysicalReadsToolStripMenuItem.Size = new System.Drawing.Size(257, 26);
            avgPhysicalReadsToolStripMenuItem.Tag = "avg_physical_io_reads_kb";
            avgPhysicalReadsToolStripMenuItem.Text = "Avg Physical Reads (KB)";
            avgPhysicalReadsToolStripMenuItem.Click += Select_Measure;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(0, 24);
            // 
            // tsCopyImage2
            // 
            tsCopyImage2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyImage2.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyImage2.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyImage2.Name = "tsCopyImage2";
            tsCopyImage2.Size = new System.Drawing.Size(29, 24);
            tsCopyImage2.Text = "Copy Image";
            tsCopyImage2.Click += CopyImage;
            // 
            // tsCopyData2
            // 
            tsCopyData2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyData2.Image = Properties.Resources.ASX_Copy_grey_16x;
            tsCopyData2.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyData2.Name = "tsCopyData2";
            tsCopyData2.Size = new System.Drawing.Size(29, 24);
            tsCopyData2.Text = "Copy Data";
            tsCopyData2.Click += CopyData;
            // 
            // QueryStorePlanChart
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(planChart);
            Controls.Add(refresh1);
            Controls.Add(toolStrip1);
            Name = "QueryStorePlanChart";
            Size = new System.Drawing.Size(1189, 557);
            contextMenuStrip1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart planChart;
        private Refresh refresh1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasure;
        private System.Windows.Forms.ToolStripMenuItem totalCPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgCPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalDurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgDurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem executionCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem maxMemoryGrantToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalPhysicalReadsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgPhysicalReadsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem setYAxisMaxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setYAxisMinToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsSave;
        private System.Windows.Forms.ToolStripMenuItem tsCopyImage;
        private System.Windows.Forms.ToolStripMenuItem tsCopyData;
        private System.Windows.Forms.ToolStripButton tsCopyImage2;
        private System.Windows.Forms.ToolStripButton tsCopyData2;
    }
}
