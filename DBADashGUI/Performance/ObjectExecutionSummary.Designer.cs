namespace DBADashGUI.Performance
{
    partial class ObjectExecutionSummary
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsCompare = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsTimeOffset = new System.Windows.Forms.ToolStripMenuItem();
            this.ts24Hrs = new System.Windows.Forms.ToolStripMenuItem();
            this.ts7Days = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustomCompare = new System.Windows.Forms.ToolStripMenuItem();
            this.tsNoCompare = new System.Windows.Forms.ToolStripMenuItem();
            this.tsType = new System.Windows.Forms.ToolStripDropDownButton();
            this.procedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.triggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cLRProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cLRTriggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scalarFunctionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extendedStoredProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.lblSearch = new System.Windows.Forms.ToolStripLabel();
            this.txtSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tsCols = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitChart = new System.Windows.Forms.SplitContainer();
            this.objectExecutionLineChart1 = new DBADashGUI.Performance.ObjectExecutionLineChart();
            this.compareObjectExecutionLineChart = new DBADashGUI.Performance.ObjectExecutionLineChart();
            this.tmrSearch = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitChart)).BeginInit();
            this.splitChart.Panel1.SuspendLayout();
            this.splitChart.Panel2.SuspendLayout();
            this.splitChart.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(1262, 323);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.tsCompare,
            this.tsCols,
            this.tsType,
            this.toolStripSeparator1,
            this.lblSearch,
            this.txtSearch});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1262, 27);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // tsCompare
            // 
            this.tsCompare.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTimeOffset,
            this.ts24Hrs,
            this.ts7Days,
            this.toolStripSeparator2,
            this.tsCustomCompare,
            this.tsNoCompare});
            this.tsCompare.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCompare.Name = "tsCompare";
            this.tsCompare.Size = new System.Drawing.Size(124, 24);
            this.tsCompare.Text = "Compare To";
            // 
            // tsTimeOffset
            // 
            this.tsTimeOffset.Name = "tsTimeOffset";
            this.tsTimeOffset.Size = new System.Drawing.Size(193, 26);
            this.tsTimeOffset.Tag = "60";
            this.tsTimeOffset.Text = "Previous Period";
            this.tsTimeOffset.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // ts24Hrs
            // 
            this.ts24Hrs.Name = "ts24Hrs";
            this.ts24Hrs.Size = new System.Drawing.Size(193, 26);
            this.ts24Hrs.Tag = "1440";
            this.ts24Hrs.Text = "-24hrs offset";
            this.ts24Hrs.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // ts7Days
            // 
            this.ts7Days.Name = "ts7Days";
            this.ts7Days.Size = new System.Drawing.Size(193, 26);
            this.ts7Days.Tag = "10080";
            this.ts7Days.Text = "-7 days offset";
            this.ts7Days.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(190, 6);
            // 
            // tsCustomCompare
            // 
            this.tsCustomCompare.Name = "tsCustomCompare";
            this.tsCustomCompare.Size = new System.Drawing.Size(193, 26);
            this.tsCustomCompare.Tag = "-1";
            this.tsCustomCompare.Text = "Custom";
            this.tsCustomCompare.Click += new System.EventHandler(this.tsCustomCompare_Click);
            // 
            // tsNoCompare
            // 
            this.tsNoCompare.Checked = true;
            this.tsNoCompare.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsNoCompare.Name = "tsNoCompare";
            this.tsNoCompare.Size = new System.Drawing.Size(193, 26);
            this.tsNoCompare.Tag = "0";
            this.tsNoCompare.Text = "None";
            this.tsNoCompare.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // tsType
            // 
            this.tsType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.procedureToolStripMenuItem,
            this.triggerToolStripMenuItem,
            this.cLRProcedureToolStripMenuItem,
            this.cLRTriggerToolStripMenuItem,
            this.scalarFunctionToolStripMenuItem,
            this.extendedStoredProcedureToolStripMenuItem});
            this.tsType.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsType.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsType.Name = "tsType";
            this.tsType.Size = new System.Drawing.Size(74, 24);
            this.tsType.Text = "Type";
            // 
            // procedureToolStripMenuItem
            // 
            this.procedureToolStripMenuItem.CheckOnClick = true;
            this.procedureToolStripMenuItem.Name = "procedureToolStripMenuItem";
            this.procedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.procedureToolStripMenuItem.Tag = "P";
            this.procedureToolStripMenuItem.Text = "Procedure";
            this.procedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // triggerToolStripMenuItem
            // 
            this.triggerToolStripMenuItem.CheckOnClick = true;
            this.triggerToolStripMenuItem.Name = "triggerToolStripMenuItem";
            this.triggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.triggerToolStripMenuItem.Tag = "TR";
            this.triggerToolStripMenuItem.Text = "Trigger";
            this.triggerToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // cLRProcedureToolStripMenuItem
            // 
            this.cLRProcedureToolStripMenuItem.CheckOnClick = true;
            this.cLRProcedureToolStripMenuItem.Name = "cLRProcedureToolStripMenuItem";
            this.cLRProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.cLRProcedureToolStripMenuItem.Tag = "PC";
            this.cLRProcedureToolStripMenuItem.Text = "CLR Procedure";
            this.cLRProcedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // cLRTriggerToolStripMenuItem
            // 
            this.cLRTriggerToolStripMenuItem.CheckOnClick = true;
            this.cLRTriggerToolStripMenuItem.Name = "cLRTriggerToolStripMenuItem";
            this.cLRTriggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.cLRTriggerToolStripMenuItem.Tag = "TA";
            this.cLRTriggerToolStripMenuItem.Text = "CLR Trigger";
            this.cLRTriggerToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // scalarFunctionToolStripMenuItem
            // 
            this.scalarFunctionToolStripMenuItem.CheckOnClick = true;
            this.scalarFunctionToolStripMenuItem.Name = "scalarFunctionToolStripMenuItem";
            this.scalarFunctionToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.scalarFunctionToolStripMenuItem.Tag = "FN";
            this.scalarFunctionToolStripMenuItem.Text = "Scalar Function";
            this.scalarFunctionToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // extendedStoredProcedureToolStripMenuItem
            // 
            this.extendedStoredProcedureToolStripMenuItem.CheckOnClick = true;
            this.extendedStoredProcedureToolStripMenuItem.Name = "extendedStoredProcedureToolStripMenuItem";
            this.extendedStoredProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.extendedStoredProcedureToolStripMenuItem.Tag = "X";
            this.extendedStoredProcedureToolStripMenuItem.Text = "Extended Stored Procedure";
            this.extendedStoredProcedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // lblSearch
            // 
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(56, 24);
            this.lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(200, 27);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // tsCols
            // 
            this.tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCols.Image = global::DBADashGUI.Properties.Resources.Column_16x;
            this.tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCols.Name = "tsCols";
            this.tsCols.Size = new System.Drawing.Size(29, 24);
            this.tsCols.Text = "Columns";
            this.tsCols.Click += new System.EventHandler(this.tsCols_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitChart);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Size = new System.Drawing.Size(1262, 1117);
            this.splitContainer1.SplitterDistance = 789;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 6;
            // 
            // splitChart
            // 
            this.splitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitChart.Location = new System.Drawing.Point(0, 0);
            this.splitChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitChart.Name = "splitChart";
            this.splitChart.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitChart.Panel1
            // 
            this.splitChart.Panel1.Controls.Add(this.objectExecutionLineChart1);
            // 
            // splitChart.Panel2
            // 
            this.splitChart.Panel2.Controls.Add(this.compareObjectExecutionLineChart);
            this.splitChart.Size = new System.Drawing.Size(1262, 789);
            this.splitChart.SplitterDistance = 391;
            this.splitChart.SplitterWidth = 5;
            this.splitChart.TabIndex = 7;
            // 
            // objectExecutionLineChart1
            // 
            this.objectExecutionLineChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecutionLineChart1.Location = new System.Drawing.Point(0, 0);
            this.objectExecutionLineChart1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.objectExecutionLineChart1.Name = "objectExecutionLineChart1";
            this.objectExecutionLineChart1.Size = new System.Drawing.Size(1262, 391);
            this.objectExecutionLineChart1.TabIndex = 5;
            this.objectExecutionLineChart1.Title = "abc";
            // 
            // compareObjectExecutionLineChart
            // 
            this.compareObjectExecutionLineChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.compareObjectExecutionLineChart.Location = new System.Drawing.Point(0, 0);
            this.compareObjectExecutionLineChart.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.compareObjectExecutionLineChart.Name = "compareObjectExecutionLineChart";
            this.compareObjectExecutionLineChart.Size = new System.Drawing.Size(1262, 393);
            this.compareObjectExecutionLineChart.TabIndex = 6;
            this.compareObjectExecutionLineChart.Title = "abc";
            // 
            // tmrSearch
            // 
            this.tmrSearch.Interval = 1000;
            this.tmrSearch.Tick += new System.EventHandler(this.tmrSearch_Tick);
            // 
            // ObjectExecutionSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ObjectExecutionSummary";
            this.Size = new System.Drawing.Size(1262, 1144);
            this.Load += new System.EventHandler(this.ObjectExecutionSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitChart.Panel1.ResumeLayout(false);
            this.splitChart.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitChart)).EndInit();
            this.splitChart.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsCompare;
        private System.Windows.Forms.ToolStripMenuItem ts24Hrs;
        private System.Windows.Forms.ToolStripMenuItem ts7Days;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem tsCustomCompare;
        private System.Windows.Forms.ToolStripMenuItem tsNoCompare;
        private System.Windows.Forms.ToolStripMenuItem tsTimeOffset;
        private System.Windows.Forms.ToolStripDropDownButton tsType;
        private System.Windows.Forms.ToolStripMenuItem procedureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem triggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cLRProcedureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cLRTriggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scalarFunctionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extendedStoredProcedureToolStripMenuItem;
        private ObjectExecutionLineChart objectExecutionLineChart1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitChart;
        private ObjectExecutionLineChart compareObjectExecutionLineChart;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel lblSearch;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.Timer tmrSearch;
        private System.Windows.Forms.ToolStripButton tsCols;
    }
}
