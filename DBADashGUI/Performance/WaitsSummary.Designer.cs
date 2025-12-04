using DBADashGUI.CustomReports;

namespace DBADashGUI.Performance
{
    partial class WaitsSummary
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            WaitMetric waitMetric1 = new WaitMetric();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            dgv = new DBADashDataGridView();
            colWaitType = new System.Windows.Forms.DataGridViewLinkColumn();
            colTotalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSignalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSignalWaitPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWaitTimeMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWaitTimeMsPerSecPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWaitingTasksCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAvgWaitTimeMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSampleDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCriticalWait = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colHelp = new System.Windows.Forms.DataGridViewLinkColumn();
            colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsBar2 = new System.Windows.Forms.ToolStripButton();
            splitGrid = new System.Windows.Forms.SplitContainer();
            splitChart = new System.Windows.Forms.SplitContainer();
            WaitChart1 = new CartesianChartWithDataTable();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsCloseLineChart = new System.Windows.Forms.ToolStripButton();
            tsWaitType = new System.Windows.Forms.ToolStripLabel();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            lineSmoothnesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsSmooth1 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            tsSmoothPoint5 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            tsSmooth0 = new System.Windows.Forms.ToolStripMenuItem();
            pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            smallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mediumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            largeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsMetrics = new System.Windows.Forms.ToolStripDropDownButton();
            waits1 = new Waits();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitGrid).BeginInit();
            splitGrid.Panel1.SuspendLayout();
            splitGrid.Panel2.SuspendLayout();
            splitGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitChart).BeginInit();
            splitChart.Panel1.SuspendLayout();
            splitChart.Panel2.SuspendLayout();
            splitChart.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colWaitType, colTotalWait, colSignalWait, colSignalWaitPct, colWaitTimeMsPerSec, colWaitTimeMsPerSecPerCore, colWaitingTasksCount, colAvgWaitTimeMs, colSampleDuration, colCriticalWait, colHelp, colDescription });
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle10;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1423, 395);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            // 
            // colWaitType
            // 
            colWaitType.DataPropertyName = "WaitType";
            colWaitType.HeaderText = "Wait Type";
            colWaitType.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colWaitType.MinimumWidth = 100;
            colWaitType.Name = "colWaitType";
            colWaitType.ReadOnly = true;
            colWaitType.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colWaitType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colWaitType.Width = 125;
            // 
            // colTotalWait
            // 
            colTotalWait.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle2.Format = "N3";
            dataGridViewCellStyle2.NullValue = null;
            colTotalWait.DefaultCellStyle = dataGridViewCellStyle2;
            colTotalWait.HeaderText = "Total Wait (sec)";
            colTotalWait.MinimumWidth = 50;
            colTotalWait.Name = "colTotalWait";
            colTotalWait.ReadOnly = true;
            colTotalWait.Width = 50;
            // 
            // colSignalWait
            // 
            colSignalWait.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle3.Format = "N3";
            colSignalWait.DefaultCellStyle = dataGridViewCellStyle3;
            colSignalWait.HeaderText = "Signal Wait (sec)";
            colSignalWait.MinimumWidth = 55;
            colSignalWait.Name = "colSignalWait";
            colSignalWait.ReadOnly = true;
            colSignalWait.Width = 55;
            // 
            // colSignalWaitPct
            // 
            colSignalWaitPct.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle4.Format = "P1";
            colSignalWaitPct.DefaultCellStyle = dataGridViewCellStyle4;
            colSignalWaitPct.HeaderText = "Signal Wait %";
            colSignalWaitPct.MinimumWidth = 55;
            colSignalWaitPct.Name = "colSignalWaitPct";
            colSignalWaitPct.ReadOnly = true;
            colSignalWaitPct.Width = 55;
            // 
            // colWaitTimeMsPerSec
            // 
            colWaitTimeMsPerSec.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle5.Format = "N2";
            colWaitTimeMsPerSec.DefaultCellStyle = dataGridViewCellStyle5;
            colWaitTimeMsPerSec.HeaderText = "Wait Time (ms / sec)";
            colWaitTimeMsPerSec.MinimumWidth = 70;
            colWaitTimeMsPerSec.Name = "colWaitTimeMsPerSec";
            colWaitTimeMsPerSec.ReadOnly = true;
            colWaitTimeMsPerSec.Width = 70;
            // 
            // colWaitTimeMsPerSecPerCore
            // 
            colWaitTimeMsPerSecPerCore.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle6.Format = "N2";
            colWaitTimeMsPerSecPerCore.DefaultCellStyle = dataGridViewCellStyle6;
            colWaitTimeMsPerSecPerCore.HeaderText = "Wait Time (ms / sec / core)";
            colWaitTimeMsPerSecPerCore.MinimumWidth = 70;
            colWaitTimeMsPerSecPerCore.Name = "colWaitTimeMsPerSecPerCore";
            colWaitTimeMsPerSecPerCore.ReadOnly = true;
            colWaitTimeMsPerSecPerCore.Width = 70;
            // 
            // colWaitingTasksCount
            // 
            colWaitingTasksCount.DataPropertyName = "WaitingTasksCount";
            colWaitingTasksCount.HeaderText = "Waiting Tasks Count";
            colWaitingTasksCount.MinimumWidth = 65;
            colWaitingTasksCount.Name = "colWaitingTasksCount";
            colWaitingTasksCount.ReadOnly = true;
            colWaitingTasksCount.Width = 65;
            // 
            // colAvgWaitTimeMs
            // 
            colAvgWaitTimeMs.DataPropertyName = "AvgWaitTimeMs";
            dataGridViewCellStyle7.Format = "N3";
            colAvgWaitTimeMs.DefaultCellStyle = dataGridViewCellStyle7;
            colAvgWaitTimeMs.HeaderText = "Avg Wait Time (ms)";
            colAvgWaitTimeMs.MinimumWidth = 50;
            colAvgWaitTimeMs.Name = "colAvgWaitTimeMs";
            colAvgWaitTimeMs.ReadOnly = true;
            colAvgWaitTimeMs.Width = 50;
            // 
            // colSampleDuration
            // 
            colSampleDuration.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle8.Format = "N0";
            colSampleDuration.DefaultCellStyle = dataGridViewCellStyle8;
            colSampleDuration.HeaderText = "Sample Duration (sec)";
            colSampleDuration.MinimumWidth = 70;
            colSampleDuration.Name = "colSampleDuration";
            colSampleDuration.ReadOnly = true;
            colSampleDuration.Width = 70;
            // 
            // colCriticalWait
            // 
            colCriticalWait.DataPropertyName = "CriticalWait";
            colCriticalWait.FalseValue = "";
            colCriticalWait.HeaderText = "Is Critical Wait?";
            colCriticalWait.MinimumWidth = 60;
            colCriticalWait.Name = "colCriticalWait";
            colCriticalWait.ReadOnly = true;
            colCriticalWait.Width = 60;
            // 
            // colHelp
            // 
            colHelp.HeaderText = "Help";
            colHelp.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colHelp.MinimumWidth = 50;
            colHelp.Name = "colHelp";
            colHelp.ReadOnly = true;
            colHelp.Text = "Help";
            colHelp.UseColumnTextForLinkValue = true;
            colHelp.Width = 50;
            // 
            // colDescription
            // 
            colDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colDescription.DataPropertyName = "Description";
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            colDescription.DefaultCellStyle = dataGridViewCellStyle9;
            colDescription.HeaderText = "Description";
            colDescription.MinimumWidth = 50;
            colDescription.Name = "colDescription";
            colDescription.ReadOnly = true;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsColumns, tsClearFilter, tsBar2 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1423, 27);
            toolStrip1.TabIndex = 1;
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
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsColumns
            // 
            tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsColumns.Image = Properties.Resources.Column_16x;
            tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsColumns.Name = "tsColumns";
            tsColumns.Size = new System.Drawing.Size(34, 24);
            tsColumns.Text = "Columns";
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // tsBar2
            // 
            tsBar2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBar2.Image = Properties.Resources.StackedColumnChart_24x;
            tsBar2.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBar2.Name = "tsBar2";
            tsBar2.Size = new System.Drawing.Size(29, 24);
            tsBar2.Text = "Waits bar chart";
            tsBar2.Click += ToggleBarChart_Click;
            // 
            // splitGrid
            // 
            splitGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            splitGrid.Location = new System.Drawing.Point(0, 0);
            splitGrid.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitGrid.Name = "splitGrid";
            splitGrid.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitGrid.Panel1
            // 
            splitGrid.Panel1.Controls.Add(splitChart);
            // 
            // splitGrid.Panel2
            // 
            splitGrid.Panel2.Controls.Add(dgv);
            splitGrid.Panel2.Controls.Add(toolStrip1);
            splitGrid.Size = new System.Drawing.Size(1423, 800);
            splitGrid.SplitterDistance = 373;
            splitGrid.SplitterWidth = 5;
            splitGrid.TabIndex = 2;
            // 
            // splitChart
            // 
            splitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            splitChart.Location = new System.Drawing.Point(0, 0);
            splitChart.Name = "splitChart";
            splitChart.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitChart.Panel1
            // 
            splitChart.Panel1.Controls.Add(WaitChart1);
            splitChart.Panel1.Controls.Add(toolStrip2);
            // 
            // splitChart.Panel2
            // 
            splitChart.Panel2.Controls.Add(waits1);
            splitChart.Panel2Collapsed = true;
            splitChart.Size = new System.Drawing.Size(1423, 373);
            splitChart.SplitterDistance = 185;
            splitChart.TabIndex = 3;
            // 
            // WaitChart1
            // 
            WaitChart1.DefaultLineSmoothness = 0.5D;
            WaitChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            WaitChart1.Location = new System.Drawing.Point(0, 27);
            WaitChart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            WaitChart1.Name = "WaitChart1";
            WaitChart1.Size = new System.Drawing.Size(1423, 346);
            WaitChart1.TabIndex = 0;
            WaitChart1.Text = "cartesianChartWithDataTable1";
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCloseLineChart, tsWaitType, tsOptions, tsDateGroup, tsMetrics });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(1423, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsCloseLineChart
            // 
            tsCloseLineChart.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsCloseLineChart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCloseLineChart.Image = Properties.Resources.Close_red_16x;
            tsCloseLineChart.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCloseLineChart.Name = "tsCloseLineChart";
            tsCloseLineChart.Size = new System.Drawing.Size(29, 24);
            tsCloseLineChart.Text = "toolStripButton1";
            tsCloseLineChart.Click += CloseLineChart_Click;
            // 
            // tsWaitType
            // 
            tsWaitType.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsWaitType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsWaitType.Name = "tsWaitType";
            tsWaitType.Size = new System.Drawing.Size(89, 24);
            tsWaitType.Text = "WAIT_TYPE";
            // 
            // tsOptions
            // 
            tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { lineSmoothnesToolStripMenuItem, pointsToolStripMenuItem });
            tsOptions.Image = Properties.Resources.SettingsOutline_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(34, 24);
            tsOptions.Text = "Options";
            // 
            // lineSmoothnesToolStripMenuItem
            // 
            lineSmoothnesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsSmooth1, toolStripMenuItem3, tsSmoothPoint5, toolStripMenuItem2, tsSmooth0 });
            lineSmoothnesToolStripMenuItem.Name = "lineSmoothnesToolStripMenuItem";
            lineSmoothnesToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            lineSmoothnesToolStripMenuItem.Text = "Line Smoothnes";
            // 
            // tsSmooth1
            // 
            tsSmooth1.Name = "tsSmooth1";
            tsSmooth1.Size = new System.Drawing.Size(128, 26);
            tsSmooth1.Tag = "1.0";
            tsSmooth1.Text = "100%";
            tsSmooth1.Click += TsSmooth_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(128, 26);
            toolStripMenuItem3.Tag = "0.75";
            toolStripMenuItem3.Text = "75%";
            toolStripMenuItem3.Click += TsSmooth_Click;
            // 
            // tsSmoothPoint5
            // 
            tsSmoothPoint5.Name = "tsSmoothPoint5";
            tsSmoothPoint5.Size = new System.Drawing.Size(128, 26);
            tsSmoothPoint5.Tag = "0.5";
            tsSmoothPoint5.Text = "50%";
            tsSmoothPoint5.Click += TsSmooth_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(128, 26);
            toolStripMenuItem2.Tag = "0.25";
            toolStripMenuItem2.Text = "25%";
            toolStripMenuItem2.Click += TsSmooth_Click;
            // 
            // tsSmooth0
            // 
            tsSmooth0.Name = "tsSmooth0";
            tsSmooth0.Size = new System.Drawing.Size(128, 26);
            tsSmooth0.Tag = "0";
            tsSmooth0.Text = "0%";
            tsSmooth0.Click += TsSmooth_Click;
            // 
            // pointsToolStripMenuItem
            // 
            pointsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { noneToolStripMenuItem, smallToolStripMenuItem, mediumToolStripMenuItem, largeToolStripMenuItem });
            pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            pointsToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            pointsToolStripMenuItem.Text = "Points";
            // 
            // noneToolStripMenuItem
            // 
            noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            noneToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            noneToolStripMenuItem.Tag = "0";
            noneToolStripMenuItem.Text = "None";
            noneToolStripMenuItem.Click += PointSize_Click;
            // 
            // smallToolStripMenuItem
            // 
            smallToolStripMenuItem.Name = "smallToolStripMenuItem";
            smallToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            smallToolStripMenuItem.Tag = "5";
            smallToolStripMenuItem.Text = "Small";
            smallToolStripMenuItem.Click += PointSize_Click;
            // 
            // mediumToolStripMenuItem
            // 
            mediumToolStripMenuItem.Name = "mediumToolStripMenuItem";
            mediumToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            mediumToolStripMenuItem.Tag = "10";
            mediumToolStripMenuItem.Text = "Medium";
            mediumToolStripMenuItem.Click += PointSize_Click;
            // 
            // largeToolStripMenuItem
            // 
            largeToolStripMenuItem.Name = "largeToolStripMenuItem";
            largeToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            largeToolStripMenuItem.Tag = "15";
            largeToolStripMenuItem.Text = "Large";
            largeToolStripMenuItem.Click += PointSize_Click;
            // 
            // tsDateGroup
            // 
            tsDateGroup.Image = Properties.Resources.Time_16x;
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(76, 24);
            tsDateGroup.Tag = "1";
            tsDateGroup.Text = "1Min";
            tsDateGroup.ToolTipText = "Date Group";
            // 
            // tsMetrics
            // 
            tsMetrics.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMetrics.Image = Properties.Resources.AddComputedField_16x;
            tsMetrics.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMetrics.Name = "tsMetrics";
            tsMetrics.Size = new System.Drawing.Size(34, 24);
            tsMetrics.Text = "Metrics";
            // 
            // waits1
            // 
            waits1.Dock = System.Windows.Forms.DockStyle.Fill;
            waits1.Location = new System.Drawing.Point(0, 0);
            waits1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            waitMetric1.CriticalWaitsOnly = false;
            waitMetric1.WaitType = null;
            waits1.Metric = waitMetric1;
            waits1.Name = "waits1";
            waits1.Size = new System.Drawing.Size(1423, 184);
            waits1.TabIndex = 2;
            waits1.Close += ToggleBarChart_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "WaitType";
            dataGridViewCellStyle11.Format = "N3";
            dataGridViewCellStyle11.NullValue = null;
            dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle11;
            dataGridViewTextBoxColumn1.HeaderText = "Wait Type";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 101;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle12.Format = "N3";
            dataGridViewCellStyle12.NullValue = null;
            dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle12;
            dataGridViewTextBoxColumn2.HeaderText = "Total Wait (sec)";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 126;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle13.Format = "N3";
            dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle13;
            dataGridViewTextBoxColumn3.HeaderText = "Signal Wait (sec)";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle14.Format = "P1";
            dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle14;
            dataGridViewTextBoxColumn4.HeaderText = "Signal Wait %";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 103;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle15.Format = "N2";
            dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle15;
            dataGridViewTextBoxColumn5.HeaderText = "Wait Time (ms/sec)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 145;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle16.Format = "N2";
            dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle16;
            dataGridViewTextBoxColumn6.HeaderText = "Wait Time (ms/sec/core)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 173;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle17.Format = "N0";
            dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle17;
            dataGridViewTextBoxColumn7.HeaderText = "Sample Duration (sec)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 134;
            // 
            // WaitsSummary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitGrid);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "WaitsSummary";
            Size = new System.Drawing.Size(1423, 800);
            Load += WaitsSummary_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitGrid.Panel1.ResumeLayout(false);
            splitGrid.Panel2.ResumeLayout(false);
            splitGrid.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitGrid).EndInit();
            splitGrid.ResumeLayout(false);
            splitChart.Panel1.ResumeLayout(false);
            splitChart.Panel1.PerformLayout();
            splitChart.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitChart).EndInit();
            splitChart.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.SplitContainer splitGrid;
        private CartesianChartWithDataTable WaitChart1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel tsWaitType;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem lineSmoothnesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsSmooth1;
        private System.Windows.Forms.ToolStripMenuItem tsSmoothPoint5;
        private System.Windows.Forms.ToolStripMenuItem tsSmooth0;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem pointsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mediumToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem largeToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripDropDownButton tsColumns;
        private System.Windows.Forms.ToolStripDropDownButton tsMetrics;
        private System.Windows.Forms.DataGridViewLinkColumn colWaitType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWaitPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSecPerCore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitingTasksCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgWaitTimeMs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSampleDuration;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCriticalWait;
        private System.Windows.Forms.DataGridViewLinkColumn colHelp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDescription;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.SplitContainer splitChart;
        private Waits waits1;
        private System.Windows.Forms.ToolStripButton tsBar2;
        private System.Windows.Forms.ToolStripButton tsCloseLineChart;
    }
}
