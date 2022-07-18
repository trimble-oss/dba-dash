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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.colWaitType = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colTotalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWaitPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSecPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitingTasksCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvgWaitTimeMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSampleDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCriticalWait = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colHelp = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.WaitChart1 = new DBADashGUI.Performance.CartesianChartWithDataTable();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsWaitType = new System.Windows.Forms.ToolStripLabel();
            this.tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            this.lineSmoothnesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmoothPoint5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth0 = new System.Windows.Forms.ToolStripMenuItem();
            this.pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mediumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.largeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tsMetrics = new System.Windows.Forms.ToolStripDropDownButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colWaitType,
            this.colTotalWait,
            this.colSignalWait,
            this.colSignalWaitPct,
            this.colWaitTimeMsPerSec,
            this.colWaitTimeMsPerSecPerCore,
            this.colWaitingTasksCount,
            this.colAvgWaitTimeMs,
            this.colSampleDuration,
            this.colCriticalWait,
            this.colHelp,
            this.colDescription});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(708, 318);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellContentClick);
            // 
            // colWaitType
            // 
            this.colWaitType.DataPropertyName = "WaitType";
            this.colWaitType.HeaderText = "Wait Type";
            this.colWaitType.MinimumWidth = 6;
            this.colWaitType.Name = "colWaitType";
            this.colWaitType.ReadOnly = true;
            this.colWaitType.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colWaitType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colWaitType.Width = 93;
            // 
            // colTotalWait
            // 
            this.colTotalWait.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle1.Format = "N3";
            dataGridViewCellStyle1.NullValue = null;
            this.colTotalWait.DefaultCellStyle = dataGridViewCellStyle1;
            this.colTotalWait.HeaderText = "Total Wait (sec)";
            this.colTotalWait.MinimumWidth = 6;
            this.colTotalWait.Name = "colTotalWait";
            this.colTotalWait.ReadOnly = true;
            this.colTotalWait.Width = 126;
            // 
            // colSignalWait
            // 
            this.colSignalWait.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle2.Format = "N3";
            this.colSignalWait.DefaultCellStyle = dataGridViewCellStyle2;
            this.colSignalWait.HeaderText = "Signal Wait (sec)";
            this.colSignalWait.MinimumWidth = 6;
            this.colSignalWait.Name = "colSignalWait";
            this.colSignalWait.ReadOnly = true;
            this.colSignalWait.Width = 103;
            // 
            // colSignalWaitPct
            // 
            this.colSignalWaitPct.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle3.Format = "P1";
            this.colSignalWaitPct.DefaultCellStyle = dataGridViewCellStyle3;
            this.colSignalWaitPct.HeaderText = "Signal Wait %";
            this.colSignalWaitPct.MinimumWidth = 6;
            this.colSignalWaitPct.Name = "colSignalWaitPct";
            this.colSignalWaitPct.ReadOnly = true;
            this.colSignalWaitPct.Width = 103;
            // 
            // colWaitTimeMsPerSec
            // 
            this.colWaitTimeMsPerSec.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle4.Format = "N2";
            this.colWaitTimeMsPerSec.DefaultCellStyle = dataGridViewCellStyle4;
            this.colWaitTimeMsPerSec.HeaderText = "Wait Time (ms/sec)";
            this.colWaitTimeMsPerSec.MinimumWidth = 6;
            this.colWaitTimeMsPerSec.Name = "colWaitTimeMsPerSec";
            this.colWaitTimeMsPerSec.ReadOnly = true;
            this.colWaitTimeMsPerSec.Width = 145;
            // 
            // colWaitTimeMsPerSecPerCore
            // 
            this.colWaitTimeMsPerSecPerCore.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle5.Format = "N2";
            this.colWaitTimeMsPerSecPerCore.DefaultCellStyle = dataGridViewCellStyle5;
            this.colWaitTimeMsPerSecPerCore.HeaderText = "Wait Time (ms/sec/core)";
            this.colWaitTimeMsPerSecPerCore.MinimumWidth = 6;
            this.colWaitTimeMsPerSecPerCore.Name = "colWaitTimeMsPerSecPerCore";
            this.colWaitTimeMsPerSecPerCore.ReadOnly = true;
            this.colWaitTimeMsPerSecPerCore.Width = 173;
            // 
            // colWaitingTasksCount
            // 
            this.colWaitingTasksCount.DataPropertyName = "WaitingTasksCount";
            this.colWaitingTasksCount.HeaderText = "Waiting Tasks Count";
            this.colWaitingTasksCount.MinimumWidth = 6;
            this.colWaitingTasksCount.Name = "colWaitingTasksCount";
            this.colWaitingTasksCount.ReadOnly = true;
            this.colWaitingTasksCount.Width = 125;
            // 
            // colAvgWaitTimeMs
            // 
            this.colAvgWaitTimeMs.DataPropertyName = "AvgWaitTimeMs";
            dataGridViewCellStyle6.Format = "N3";
            this.colAvgWaitTimeMs.DefaultCellStyle = dataGridViewCellStyle6;
            this.colAvgWaitTimeMs.HeaderText = "Avg Wait Time (ms)";
            this.colAvgWaitTimeMs.MinimumWidth = 6;
            this.colAvgWaitTimeMs.Name = "colAvgWaitTimeMs";
            this.colAvgWaitTimeMs.ReadOnly = true;
            this.colAvgWaitTimeMs.Width = 125;
            // 
            // colSampleDuration
            // 
            this.colSampleDuration.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle7.Format = "N0";
            this.colSampleDuration.DefaultCellStyle = dataGridViewCellStyle7;
            this.colSampleDuration.HeaderText = "Sample Duration (sec)";
            this.colSampleDuration.MinimumWidth = 6;
            this.colSampleDuration.Name = "colSampleDuration";
            this.colSampleDuration.ReadOnly = true;
            this.colSampleDuration.Width = 134;
            // 
            // colCriticalWait
            // 
            this.colCriticalWait.DataPropertyName = "CriticalWait";
            this.colCriticalWait.FalseValue = "";
            this.colCriticalWait.HeaderText = "Is Critical Wait?";
            this.colCriticalWait.MinimumWidth = 6;
            this.colCriticalWait.Name = "colCriticalWait";
            this.colCriticalWait.ReadOnly = true;
            this.colCriticalWait.Width = 99;
            // 
            // colHelp
            // 
            this.colHelp.HeaderText = "Help";
            this.colHelp.MinimumWidth = 6;
            this.colHelp.Name = "colHelp";
            this.colHelp.ReadOnly = true;
            this.colHelp.Text = "Help";
            this.colHelp.UseColumnTextForLinkValue = true;
            this.colHelp.Width = 43;
            // 
            // colDescription
            // 
            this.colDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDescription.DataPropertyName = "Description";
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.colDescription.DefaultCellStyle = dataGridViewCellStyle8;
            this.colDescription.HeaderText = "Description";
            this.colDescription.MinimumWidth = 6;
            this.colDescription.Name = "colDescription";
            this.colDescription.ReadOnly = true;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.tsExcel,
            this.tsColumns,
            this.tsRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(708, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.TsExcel_Click);
            // 
            // tsColumns
            // 
            this.tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsColumns.Image = global::DBADashGUI.Properties.Resources.Column_16x;
            this.tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsColumns.Name = "tsColumns";
            this.tsColumns.Size = new System.Drawing.Size(34, 24);
            this.tsColumns.Text = "Columns";
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
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.WaitChart1);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(708, 656);
            this.splitContainer1.SplitterDistance = 306;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 2;
            // 
            // WaitChart1
            // 
            this.WaitChart1.DefaultLineSmoothness = 0.5D;
            this.WaitChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WaitChart1.Location = new System.Drawing.Point(0, 27);
            this.WaitChart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.WaitChart1.Name = "WaitChart1";
            this.WaitChart1.Size = new System.Drawing.Size(708, 279);
            this.WaitChart1.TabIndex = 0;
            this.WaitChart1.Text = "cartesianChartWithDataTable1";
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsWaitType,
            this.tsOptions,
            this.tsDateGroup,
            this.tsMetrics});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(708, 27);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsWaitType
            // 
            this.tsWaitType.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsWaitType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tsWaitType.Name = "tsWaitType";
            this.tsWaitType.Size = new System.Drawing.Size(89, 24);
            this.tsWaitType.Text = "WAIT_TYPE";
            // 
            // tsOptions
            // 
            this.tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lineSmoothnesToolStripMenuItem,
            this.pointsToolStripMenuItem});
            this.tsOptions.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsOptions.Name = "tsOptions";
            this.tsOptions.Size = new System.Drawing.Size(34, 24);
            this.tsOptions.Text = "Options";
            // 
            // lineSmoothnesToolStripMenuItem
            // 
            this.lineSmoothnesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsSmooth1,
            this.toolStripMenuItem3,
            this.tsSmoothPoint5,
            this.toolStripMenuItem2,
            this.tsSmooth0});
            this.lineSmoothnesToolStripMenuItem.Name = "lineSmoothnesToolStripMenuItem";
            this.lineSmoothnesToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.lineSmoothnesToolStripMenuItem.Text = "Line Smoothnes";
            // 
            // tsSmooth1
            // 
            this.tsSmooth1.Name = "tsSmooth1";
            this.tsSmooth1.Size = new System.Drawing.Size(128, 26);
            this.tsSmooth1.Tag = "1.0";
            this.tsSmooth1.Text = "100%";
            this.tsSmooth1.Click += new System.EventHandler(this.TsSmooth_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(128, 26);
            this.toolStripMenuItem3.Tag = "0.75";
            this.toolStripMenuItem3.Text = "75%";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.TsSmooth_Click);
            // 
            // tsSmoothPoint5
            // 
            this.tsSmoothPoint5.Name = "tsSmoothPoint5";
            this.tsSmoothPoint5.Size = new System.Drawing.Size(128, 26);
            this.tsSmoothPoint5.Tag = "0.5";
            this.tsSmoothPoint5.Text = "50%";
            this.tsSmoothPoint5.Click += new System.EventHandler(this.TsSmooth_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(128, 26);
            this.toolStripMenuItem2.Tag = "0.25";
            this.toolStripMenuItem2.Text = "25%";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.TsSmooth_Click);
            // 
            // tsSmooth0
            // 
            this.tsSmooth0.Name = "tsSmooth0";
            this.tsSmooth0.Size = new System.Drawing.Size(128, 26);
            this.tsSmooth0.Tag = "0";
            this.tsSmooth0.Text = "0%";
            this.tsSmooth0.Click += new System.EventHandler(this.TsSmooth_Click);
            // 
            // pointsToolStripMenuItem
            // 
            this.pointsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.smallToolStripMenuItem,
            this.mediumToolStripMenuItem,
            this.largeToolStripMenuItem});
            this.pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            this.pointsToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.pointsToolStripMenuItem.Text = "Points";
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            this.noneToolStripMenuItem.Tag = "0";
            this.noneToolStripMenuItem.Text = "None";
            this.noneToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // smallToolStripMenuItem
            // 
            this.smallToolStripMenuItem.Name = "smallToolStripMenuItem";
            this.smallToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            this.smallToolStripMenuItem.Tag = "5";
            this.smallToolStripMenuItem.Text = "Small";
            this.smallToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // mediumToolStripMenuItem
            // 
            this.mediumToolStripMenuItem.Name = "mediumToolStripMenuItem";
            this.mediumToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            this.mediumToolStripMenuItem.Tag = "10";
            this.mediumToolStripMenuItem.Text = "Medium";
            this.mediumToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // largeToolStripMenuItem
            // 
            this.largeToolStripMenuItem.Name = "largeToolStripMenuItem";
            this.largeToolStripMenuItem.Size = new System.Drawing.Size(147, 26);
            this.largeToolStripMenuItem.Tag = "15";
            this.largeToolStripMenuItem.Text = "Large";
            this.largeToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // tsDateGroup
            // 
            this.tsDateGroup.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGroup.Name = "tsDateGroup";
            this.tsDateGroup.Size = new System.Drawing.Size(76, 24);
            this.tsDateGroup.Tag = "1";
            this.tsDateGroup.Text = "1Min";
            this.tsDateGroup.ToolTipText = "Date Group";
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "WaitType";
            dataGridViewCellStyle9.Format = "N3";
            dataGridViewCellStyle9.NullValue = null;
            this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn1.HeaderText = "Wait Type";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 101;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle10.Format = "N3";
            dataGridViewCellStyle10.NullValue = null;
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn2.HeaderText = "Total Wait (sec)";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 126;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle11.Format = "N3";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn3.HeaderText = "Signal Wait (sec)";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle12.Format = "P1";
            this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn4.HeaderText = "Signal Wait %";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 103;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle13.Format = "N2";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle13;
            this.dataGridViewTextBoxColumn5.HeaderText = "Wait Time (ms/sec)";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 145;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle14.Format = "N2";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle14;
            this.dataGridViewTextBoxColumn6.HeaderText = "Wait Time (ms/sec/core)";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 173;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle15.Format = "N0";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle15;
            this.dataGridViewTextBoxColumn7.HeaderText = "Sample Duration (sec)";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 134;
            // 
            // tsMetrics
            // 
            this.tsMetrics.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMetrics.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMetrics.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMetrics.Name = "tsMetrics";
            this.tsMetrics.Size = new System.Drawing.Size(34, 24);
            this.tsMetrics.Text = "Metrics";
            // 
            // WaitsSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "WaitsSummary";
            this.Size = new System.Drawing.Size(708, 656);
            this.Load += new System.EventHandler(this.WaitsSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
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
        private System.Windows.Forms.SplitContainer splitContainer1;
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
        private System.Windows.Forms.ToolStripDropDownButton tsMetrics;
    }
}
