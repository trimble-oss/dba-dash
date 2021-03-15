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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.colWaitType = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colCriticalWait = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colHelp = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsWaitType = new System.Windows.Forms.ToolStripLabel();
            this.tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            this.lineSmoothnesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmoothPoint5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth0 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mediumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.largeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WaitChart1 = new DBADashGUI.Performance.CartesianChartWithDataTable();
            this.colTotalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWaitPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSecPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSampleDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colWaitType,
            this.colTotalWait,
            this.colSignalWait,
            this.colSignalWaitPct,
            this.colWaitTimeMsPerSec,
            this.colWaitTimeMsPerSecPerCore,
            this.colSampleDuration,
            this.colCriticalWait,
            this.colHelp});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(885, 318);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
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
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.tsRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(885, 39);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 36);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 36);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 49);
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
            this.splitContainer1.Size = new System.Drawing.Size(885, 608);
            this.splitContainer1.SplitterDistance = 286;
            this.splitContainer1.TabIndex = 2;
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsWaitType,
            this.tsOptions,
            this.tsDateGroup});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(1106, 39);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsWaitType
            // 
            this.tsWaitType.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsWaitType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.tsWaitType.Name = "tsWaitType";
            this.tsWaitType.Size = new System.Drawing.Size(89, 36);
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
            this.tsOptions.Size = new System.Drawing.Size(34, 36);
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
            this.lineSmoothnesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.lineSmoothnesToolStripMenuItem.Text = "Line Smoothnes";
            // 
            // tsSmooth1
            // 
            this.tsSmooth1.Name = "tsSmooth1";
            this.tsSmooth1.Size = new System.Drawing.Size(128, 26);
            this.tsSmooth1.Tag = "1.0";
            this.tsSmooth1.Text = "100%";
            this.tsSmooth1.Click += new System.EventHandler(this.tsSmooth_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(128, 26);
            this.toolStripMenuItem3.Tag = "0.75";
            this.toolStripMenuItem3.Text = "75%";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.tsSmooth_Click);
            // 
            // tsSmoothPoint5
            // 
            this.tsSmoothPoint5.Name = "tsSmoothPoint5";
            this.tsSmoothPoint5.Size = new System.Drawing.Size(128, 26);
            this.tsSmoothPoint5.Tag = "0.5";
            this.tsSmoothPoint5.Text = "50%";
            this.tsSmoothPoint5.Click += new System.EventHandler(this.tsSmooth_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(128, 26);
            this.toolStripMenuItem2.Tag = "0.25";
            this.toolStripMenuItem2.Text = "25%";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsSmooth_Click);
            // 
            // tsSmooth0
            // 
            this.tsSmooth0.Name = "tsSmooth0";
            this.tsSmooth0.Size = new System.Drawing.Size(128, 26);
            this.tsSmooth0.Tag = "0";
            this.tsSmooth0.Text = "0%";
            this.tsSmooth0.Click += new System.EventHandler(this.tsSmooth_Click);
            // 
            // tsDateGroup
            // 
            this.tsDateGroup.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGroup.Name = "tsDateGroup";
            this.tsDateGroup.Size = new System.Drawing.Size(76, 36);
            this.tsDateGroup.Tag = "1";
            this.tsDateGroup.Text = "1Min";
            this.tsDateGroup.ToolTipText = "Date Group";
            // 
            // pointsToolStripMenuItem
            // 
            this.pointsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.smallToolStripMenuItem,
            this.mediumToolStripMenuItem,
            this.largeToolStripMenuItem});
            this.pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            this.pointsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.pointsToolStripMenuItem.Text = "Points";
            // 
            // smallToolStripMenuItem
            // 
            this.smallToolStripMenuItem.Name = "smallToolStripMenuItem";
            this.smallToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.smallToolStripMenuItem.Tag = "5";
            this.smallToolStripMenuItem.Text = "Small";
            this.smallToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // mediumToolStripMenuItem
            // 
            this.mediumToolStripMenuItem.Name = "mediumToolStripMenuItem";
            this.mediumToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.mediumToolStripMenuItem.Tag = "10";
            this.mediumToolStripMenuItem.Text = "Medium";
            this.mediumToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // largeToolStripMenuItem
            // 
            this.largeToolStripMenuItem.Name = "largeToolStripMenuItem";
            this.largeToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.largeToolStripMenuItem.Tag = "15";
            this.largeToolStripMenuItem.Text = "Large";
            this.largeToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.noneToolStripMenuItem.Tag = "0";
            this.noneToolStripMenuItem.Text = "None";
            this.noneToolStripMenuItem.Click += new System.EventHandler(this.PointSize_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "WaitType";
            dataGridViewCellStyle7.Format = "N3";
            dataGridViewCellStyle7.NullValue = null;
            this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridViewTextBoxColumn1.HeaderText = "Wait Type";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 101;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle8.Format = "N3";
            dataGridViewCellStyle8.NullValue = null;
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn2.HeaderText = "Total Wait (sec)";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 126;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle9.Format = "N3";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn3.HeaderText = "Signal Wait (sec)";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle10.Format = "P1";
            this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn4.HeaderText = "Signal Wait %";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 103;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle11.Format = "N2";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn5.HeaderText = "Wait Time (ms/sec)";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 145;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle12.Format = "N2";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn6.HeaderText = "Wait Time (ms/sec/core)";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 173;
            // 
            // WaitChart1
            // 
            this.WaitChart1.DefaultLineSmoothness = 0.5D;
            this.WaitChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WaitChart1.Location = new System.Drawing.Point(0, 49);
            this.WaitChart1.Name = "WaitChart1";
            this.WaitChart1.Size = new System.Drawing.Size(1106, 309);
            this.WaitChart1.TabIndex = 0;
            this.WaitChart1.Text = "cartesianChartWithDataTable1";
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
            // colSampleDuration
            // 
            this.colSampleDuration.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle6.Format = "N0";
            this.colSampleDuration.DefaultCellStyle = dataGridViewCellStyle6;
            this.colSampleDuration.HeaderText = "Sample Duration (sec)";
            this.colSampleDuration.MinimumWidth = 6;
            this.colSampleDuration.Name = "colSampleDuration";
            this.colSampleDuration.ReadOnly = true;
            this.colSampleDuration.Width = 134;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle13.Format = "N0";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle13;
            this.dataGridViewTextBoxColumn7.HeaderText = "Sample Duration (sec)";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 134;
            // 
            // WaitsSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "WaitsSummary";
            this.Size = new System.Drawing.Size(708, 525);
            this.Load += new System.EventHandler(this.WaitsSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.DataGridViewLinkColumn colWaitType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWaitPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSecPerCore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSampleDuration;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCriticalWait;
        private System.Windows.Forms.DataGridViewLinkColumn colHelp;
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
    }
}
