using DBADashGUI.CustomReports;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    partial class QueryStoreTopQueries
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueryStoreTopQueries));
            dgv = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            TsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            tsNearestInterval = new System.Windows.Forms.ToolStripMenuItem();
            includeWaitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsReset = new System.Windows.Forms.ToolStripButton();
            tsColumns = new System.Windows.Forms.ToolStripButton();
            tsExecute = new System.Windows.Forms.ToolStripButton();
            tsTop = new System.Windows.Forms.ToolStripDropDownButton();
            tsTop10 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop25 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop50 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop100 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            tsTopCustom = new System.Windows.Forms.ToolStripMenuItem();
            tsGroupBy = new System.Windows.Forms.ToolStripDropDownButton();
            queryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            planToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            planHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            queryHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            objectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsSort = new System.Windows.Forms.ToolStripDropDownButton();
            totalCPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgCPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            totalDurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgDurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            executionCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            memoryGrantToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            physicalReadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            avgPhysicalReadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            parallelPlansOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minimumPlanCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            txtObjectName = new System.Windows.Forms.ToolStripTextBox();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            txtPlan = new System.Windows.Forms.ToolStripTextBox();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            tabDrillDown = new ThemedTabControl();
            tabSummary = new System.Windows.Forms.TabPage();
            dgvDrillDown = new DBADashDataGridView();
            tabChart = new System.Windows.Forms.TabPage();
            queryStorePlanChart1 = new QueryStorePlanChart();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tabDrillDown.SuspendLayout();
            tabSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDrillDown).BeginInit();
            tabChart.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1408, 267);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellFormatting += Dgv_CellFormatting;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { TsCopy, tsExcel, tsOptions, tsReset, tsColumns, tsExecute, tsTop, tsGroupBy, tsSort, tsFilter, toolStripLabel1, txtObjectName, toolStripLabel2, txtPlan });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1408, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // TsCopy
            // 
            TsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            TsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            TsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            TsCopy.Name = "TsCopy";
            TsCopy.Size = new System.Drawing.Size(29, 24);
            TsCopy.Text = "Copy";
            TsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsOptions
            // 
            tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsNearestInterval, includeWaitsToolStripMenuItem });
            tsOptions.Image = Properties.Resources.SettingsOutline_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(34, 24);
            tsOptions.Text = "Options";
            // 
            // tsNearestInterval
            // 
            tsNearestInterval.Checked = true;
            tsNearestInterval.CheckOnClick = true;
            tsNearestInterval.CheckState = System.Windows.Forms.CheckState.Checked;
            tsNearestInterval.Name = "tsNearestInterval";
            tsNearestInterval.Size = new System.Drawing.Size(221, 26);
            tsNearestInterval.Text = "Use nearest interval";
            tsNearestInterval.ToolTipText = "Use the nearest query store interval.  Uncheck to filter on first/last execution time";
            // 
            // includeWaitsToolStripMenuItem
            // 
            includeWaitsToolStripMenuItem.Checked = true;
            includeWaitsToolStripMenuItem.CheckOnClick = true;
            includeWaitsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            includeWaitsToolStripMenuItem.Name = "includeWaitsToolStripMenuItem";
            includeWaitsToolStripMenuItem.Size = new System.Drawing.Size(221, 26);
            includeWaitsToolStripMenuItem.Text = "Include Waits";
            includeWaitsToolStripMenuItem.Click += IncludeWaitsToolStripMenuItem_Click;
            // 
            // tsReset
            // 
            tsReset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsReset.Image = Properties.Resources.Undo_grey_16x;
            tsReset.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsReset.Name = "tsReset";
            tsReset.Size = new System.Drawing.Size(29, 24);
            tsReset.Text = "Reset";
            tsReset.Click += TsReset_Click;
            // 
            // tsColumns
            // 
            tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsColumns.Image = Properties.Resources.Column_16x;
            tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsColumns.Name = "tsColumns";
            tsColumns.Size = new System.Drawing.Size(29, 24);
            tsColumns.Text = "Columns";
            tsColumns.Click += TsColumns_Click;
            // 
            // tsExecute
            // 
            tsExecute.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsExecute.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExecute.Name = "tsExecute";
            tsExecute.Size = new System.Drawing.Size(84, 24);
            tsExecute.Text = "Execute";
            tsExecute.Click += TsRefresh;
            // 
            // tsTop
            // 
            tsTop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsTop.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsTop10, tsTop25, tsTop50, tsTop100, toolStripMenuItem1, tsTopCustom });
            tsTop.Image = (System.Drawing.Image)resources.GetObject("tsTop.Image");
            tsTop.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTop.Name = "tsTop";
            tsTop.Size = new System.Drawing.Size(68, 24);
            tsTop.Text = "Top 25";
            // 
            // tsTop10
            // 
            tsTop10.Name = "tsTop10";
            tsTop10.Size = new System.Drawing.Size(142, 26);
            tsTop10.Tag = "10";
            tsTop10.Text = "10";
            tsTop10.Click += Top_Select;
            // 
            // tsTop25
            // 
            tsTop25.Checked = true;
            tsTop25.CheckState = System.Windows.Forms.CheckState.Checked;
            tsTop25.Name = "tsTop25";
            tsTop25.Size = new System.Drawing.Size(142, 26);
            tsTop25.Tag = "25";
            tsTop25.Text = "25";
            tsTop25.Click += Top_Select;
            // 
            // tsTop50
            // 
            tsTop50.Name = "tsTop50";
            tsTop50.Size = new System.Drawing.Size(142, 26);
            tsTop50.Tag = "50";
            tsTop50.Text = "50";
            tsTop50.Click += Top_Select;
            // 
            // tsTop100
            // 
            tsTop100.Name = "tsTop100";
            tsTop100.Size = new System.Drawing.Size(142, 26);
            tsTop100.Tag = "100";
            tsTop100.Text = "100";
            tsTop100.Click += Top_Select;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(139, 6);
            // 
            // tsTopCustom
            // 
            tsTopCustom.Name = "tsTopCustom";
            tsTopCustom.Size = new System.Drawing.Size(142, 26);
            tsTopCustom.Text = "Custom";
            tsTopCustom.Click += Top_Select;
            // 
            // tsGroupBy
            // 
            tsGroupBy.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { queryToolStripMenuItem, planToolStripMenuItem, planHashToolStripMenuItem, queryHashToolStripMenuItem, objectToolStripMenuItem });
            tsGroupBy.Image = Properties.Resources.GroupBy_16x;
            tsGroupBy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGroupBy.Name = "tsGroupBy";
            tsGroupBy.Size = new System.Drawing.Size(147, 24);
            tsGroupBy.Text = "Group by Query";
            // 
            // queryToolStripMenuItem
            // 
            queryToolStripMenuItem.Checked = true;
            queryToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            queryToolStripMenuItem.Name = "queryToolStripMenuItem";
            queryToolStripMenuItem.Size = new System.Drawing.Size(168, 26);
            queryToolStripMenuItem.Tag = "query_id";
            queryToolStripMenuItem.Text = "Query";
            queryToolStripMenuItem.Click += Select_GroupBy;
            // 
            // planToolStripMenuItem
            // 
            planToolStripMenuItem.Name = "planToolStripMenuItem";
            planToolStripMenuItem.Size = new System.Drawing.Size(168, 26);
            planToolStripMenuItem.Tag = "plan_id";
            planToolStripMenuItem.Text = "Plan";
            planToolStripMenuItem.Click += Select_GroupBy;
            // 
            // planHashToolStripMenuItem
            // 
            planHashToolStripMenuItem.Name = "planHashToolStripMenuItem";
            planHashToolStripMenuItem.Size = new System.Drawing.Size(168, 26);
            planHashToolStripMenuItem.Tag = "query_plan_hash";
            planHashToolStripMenuItem.Text = "Plan Hash";
            planHashToolStripMenuItem.Click += Select_GroupBy;
            // 
            // queryHashToolStripMenuItem
            // 
            queryHashToolStripMenuItem.Name = "queryHashToolStripMenuItem";
            queryHashToolStripMenuItem.Size = new System.Drawing.Size(168, 26);
            queryHashToolStripMenuItem.Tag = "query_hash";
            queryHashToolStripMenuItem.Text = "Query Hash";
            queryHashToolStripMenuItem.Click += Select_GroupBy;
            // 
            // objectToolStripMenuItem
            // 
            objectToolStripMenuItem.Name = "objectToolStripMenuItem";
            objectToolStripMenuItem.Size = new System.Drawing.Size(168, 26);
            objectToolStripMenuItem.Tag = "object_id";
            objectToolStripMenuItem.Text = "Object";
            objectToolStripMenuItem.Click += Select_GroupBy;
            // 
            // tsSort
            // 
            tsSort.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsSort.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { totalCPUToolStripMenuItem, avgCPUToolStripMenuItem, totalDurationToolStripMenuItem, avgDurationToolStripMenuItem, executionCountToolStripMenuItem, memoryGrantToolStripMenuItem, physicalReadsToolStripMenuItem, avgPhysicalReadsToolStripMenuItem });
            tsSort.Image = (System.Drawing.Image)resources.GetObject("tsSort.Image");
            tsSort.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSort.Name = "tsSort";
            tsSort.Size = new System.Drawing.Size(138, 24);
            tsSort.Text = "Sort by Total CPU";
            // 
            // totalCPUToolStripMenuItem
            // 
            totalCPUToolStripMenuItem.Checked = true;
            totalCPUToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            totalCPUToolStripMenuItem.Name = "totalCPUToolStripMenuItem";
            totalCPUToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            totalCPUToolStripMenuItem.Tag = "total_cpu_time_ms";
            totalCPUToolStripMenuItem.Text = "Total CPU";
            totalCPUToolStripMenuItem.Click += Sort_Select;
            // 
            // avgCPUToolStripMenuItem
            // 
            avgCPUToolStripMenuItem.Name = "avgCPUToolStripMenuItem";
            avgCPUToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            avgCPUToolStripMenuItem.Tag = "avg_cpu_time_ms";
            avgCPUToolStripMenuItem.Text = "Avg CPU";
            avgCPUToolStripMenuItem.Click += Sort_Select;
            // 
            // totalDurationToolStripMenuItem
            // 
            totalDurationToolStripMenuItem.Name = "totalDurationToolStripMenuItem";
            totalDurationToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            totalDurationToolStripMenuItem.Tag = "total_duration_ms";
            totalDurationToolStripMenuItem.Text = "Total Duration";
            totalDurationToolStripMenuItem.Click += Sort_Select;
            // 
            // avgDurationToolStripMenuItem
            // 
            avgDurationToolStripMenuItem.Name = "avgDurationToolStripMenuItem";
            avgDurationToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            avgDurationToolStripMenuItem.Tag = "avg_duration_ms";
            avgDurationToolStripMenuItem.Text = "Avg Duration";
            avgDurationToolStripMenuItem.Click += Sort_Select;
            // 
            // executionCountToolStripMenuItem
            // 
            executionCountToolStripMenuItem.Name = "executionCountToolStripMenuItem";
            executionCountToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            executionCountToolStripMenuItem.Tag = "count_executions";
            executionCountToolStripMenuItem.Text = "Execution Count";
            executionCountToolStripMenuItem.Click += Sort_Select;
            // 
            // memoryGrantToolStripMenuItem
            // 
            memoryGrantToolStripMenuItem.Name = "memoryGrantToolStripMenuItem";
            memoryGrantToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            memoryGrantToolStripMenuItem.Tag = "max_memory_grant_kb";
            memoryGrantToolStripMenuItem.Text = "Max Memory Grant";
            memoryGrantToolStripMenuItem.Click += Sort_Select;
            // 
            // physicalReadsToolStripMenuItem
            // 
            physicalReadsToolStripMenuItem.Name = "physicalReadsToolStripMenuItem";
            physicalReadsToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            physicalReadsToolStripMenuItem.Tag = "total_physical_io_reads_kb";
            physicalReadsToolStripMenuItem.Text = "Total Physical Reads";
            physicalReadsToolStripMenuItem.Click += Sort_Select;
            // 
            // avgPhysicalReadsToolStripMenuItem
            // 
            avgPhysicalReadsToolStripMenuItem.Name = "avgPhysicalReadsToolStripMenuItem";
            avgPhysicalReadsToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            avgPhysicalReadsToolStripMenuItem.Tag = "avg_physical_io_reads_kb";
            avgPhysicalReadsToolStripMenuItem.Text = "Avg Physical Reads";
            avgPhysicalReadsToolStripMenuItem.Click += Sort_Select;
            // 
            // tsFilter
            // 
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { parallelPlansOnlyToolStripMenuItem, minimumPlanCountToolStripMenuItem });
            tsFilter.Image = Properties.Resources.Filter_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(76, 24);
            tsFilter.Text = "Filter";
            // 
            // parallelPlansOnlyToolStripMenuItem
            // 
            parallelPlansOnlyToolStripMenuItem.CheckOnClick = true;
            parallelPlansOnlyToolStripMenuItem.Name = "parallelPlansOnlyToolStripMenuItem";
            parallelPlansOnlyToolStripMenuItem.Size = new System.Drawing.Size(242, 26);
            parallelPlansOnlyToolStripMenuItem.Text = "Parallel Plans Only";
            parallelPlansOnlyToolStripMenuItem.CheckedChanged += ParallelPlans_CheckChanged;
            // 
            // minimumPlanCountToolStripMenuItem
            // 
            minimumPlanCountToolStripMenuItem.Name = "minimumPlanCountToolStripMenuItem";
            minimumPlanCountToolStripMenuItem.Size = new System.Drawing.Size(242, 26);
            minimumPlanCountToolStripMenuItem.Text = "Minimum Plan Count 1";
            minimumPlanCountToolStripMenuItem.Click += MinimumPlanCountToolStripMenuItem_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(203, 24);
            toolStripLabel1.Text = "Object Name/Query ID/Hash:";
            // 
            // txtObjectName
            // 
            txtObjectName.Name = "txtObjectName";
            txtObjectName.Size = new System.Drawing.Size(100, 27);
            txtObjectName.KeyDown += RefreshOn_KeyDown;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(98, 24);
            toolStripLabel2.Text = "Plan ID/Hash:";
            // 
            // txtPlan
            // 
            txtPlan.Name = "txtPlan";
            txtPlan.Size = new System.Drawing.Size(100, 27);
            txtPlan.KeyDown += RefreshOn_KeyDown;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 640);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1408, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 16);
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
            splitContainer1.Panel1.Controls.Add(dgv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabDrillDown);
            splitContainer1.Size = new System.Drawing.Size(1408, 613);
            splitContainer1.SplitterDistance = 267;
            splitContainer1.TabIndex = 3;
            // 
            // tabDrillDown
            // 
            tabDrillDown.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            tabDrillDown.Controls.Add(tabSummary);
            tabDrillDown.Controls.Add(tabChart);
            tabDrillDown.Dock = System.Windows.Forms.DockStyle.Fill;
            tabDrillDown.Location = new System.Drawing.Point(0, 0);
            tabDrillDown.Name = "tabDrillDown";
            tabDrillDown.SelectedIndex = 0;
            tabDrillDown.Size = new System.Drawing.Size(1408, 342);
            tabDrillDown.TabIndex = 1;
            tabDrillDown.SelectedIndexChanged += DrillDownTabIndexChanged;
            // 
            // tabSummary
            // 
            tabSummary.Controls.Add(dgvDrillDown);
            tabSummary.Location = new System.Drawing.Point(4, 4);
            tabSummary.Name = "tabSummary";
            tabSummary.Padding = new System.Windows.Forms.Padding(3);
            tabSummary.Size = new System.Drawing.Size(1400, 309);
            tabSummary.TabIndex = 0;
            tabSummary.Text = "Summary";
            tabSummary.UseVisualStyleBackColor = true;
            // 
            // dgvDrillDown
            // 
            dgvDrillDown.AllowUserToAddRows = false;
            dgvDrillDown.AllowUserToDeleteRows = false;
            dgvDrillDown.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDrillDown.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvDrillDown.Location = new System.Drawing.Point(3, 3);
            dgvDrillDown.Name = "dgvDrillDown";
            dgvDrillDown.RowHeadersVisible = false;
            dgvDrillDown.RowHeadersWidth = 51;
            dgvDrillDown.Size = new System.Drawing.Size(1394, 303);
            dgvDrillDown.TabIndex = 0;
            dgvDrillDown.CellContentClick += Dgv_CellContentClick;
            dgvDrillDown.CellFormatting += Dgv_CellFormatting;
            dgvDrillDown.RowsAdded += DgvDrillDown_RowsAdded;
            // 
            // tabChart
            // 
            tabChart.Controls.Add(queryStorePlanChart1);
            tabChart.Location = new System.Drawing.Point(4, 4);
            tabChart.Name = "tabChart";
            tabChart.Padding = new System.Windows.Forms.Padding(3);
            tabChart.Size = new System.Drawing.Size(1400, 309);
            tabChart.TabIndex = 1;
            tabChart.Text = "Chart";
            tabChart.UseVisualStyleBackColor = true;
            // 
            // queryStorePlanChart1
            // 
            queryStorePlanChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            queryStorePlanChart1.Location = new System.Drawing.Point(3, 3);
            queryStorePlanChart1.Name = "queryStorePlanChart1";
            queryStorePlanChart1.Size = new System.Drawing.Size(1394, 303);
            queryStorePlanChart1.TabIndex = 0;
            // 
            // QueryStoreTopQueries
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "QueryStoreTopQueries";
            Size = new System.Drawing.Size(1408, 662);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tabDrillDown.ResumeLayout(false);
            tabSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDrillDown).EndInit();
            tabChart.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsExecute;
        private System.Windows.Forms.ToolStripDropDownButton tsTop;
        private System.Windows.Forms.ToolStripMenuItem tsTop10;
        private System.Windows.Forms.ToolStripMenuItem tsTop25;
        private System.Windows.Forms.ToolStripMenuItem tsTop50;
        private System.Windows.Forms.ToolStripMenuItem tsTop100;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tsTopCustom;
        private System.Windows.Forms.ToolStripDropDownButton tsSort;
        private System.Windows.Forms.ToolStripMenuItem totalCPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgCPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem totalDurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgDurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem executionCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem memoryGrantToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem physicalReadsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avgPhysicalReadsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton TsCopy;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox txtObjectName;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem tsNearestInterval;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripTextBox txtPlan;
        private System.Windows.Forms.ToolStripDropDownButton tsGroupBy;
        private System.Windows.Forms.ToolStripMenuItem queryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem planToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem planHashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem queryHashToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvDrillDown;
        private System.Windows.Forms.ToolStripButton tsReset;
        private System.Windows.Forms.ToolStripMenuItem includeWaitsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem parallelPlansOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minimumPlanCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsColumns;
        private DBADashGUI.Theme.ThemedTabControl tabDrillDown;
        private System.Windows.Forms.TabPage tabSummary;
        private System.Windows.Forms.TabPage tabChart;
        private QueryStorePlanChart queryStorePlanChart1;
    }
}
