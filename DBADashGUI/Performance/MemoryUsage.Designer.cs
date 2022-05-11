
namespace DBADashGUI.Performance
{
    partial class MemoryUsage
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.performanceCounters1 = new DBADashGUI.Performance.PerformanceCounters();
            this.pieChart1 = new LiveCharts.WinForms.PieChart();
            this.chartHistory = new DBADashGUI.Performance.CartesianChartWithDataTable();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tab1 = new System.Windows.Forms.TabControl();
            this.tabClerks = new System.Windows.Forms.TabPage();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.dgvConfig = new System.Windows.Forms.DataGridView();
            this.tabCounters = new System.Windows.Forms.TabPage();
            this.performanceCounterSummaryGrid1 = new DBADashGUI.Performance.PerformanceCounterSummaryGrid();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.tab1.SuspendLayout();
            this.tabClerks.SuspendLayout();
            this.tabConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfig)).BeginInit();
            this.tabCounters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.performanceCounterSummaryGrid1)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(3, 4);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.ShowCellToolTips = false;
            this.dgv.Size = new System.Drawing.Size(870, 402);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellMouseEnter);
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
            this.splitContainer1.Panel1.Controls.Add(this.performanceCounters1);
            this.splitContainer1.Panel1.Controls.Add(this.pieChart1);
            this.splitContainer1.Panel1.Controls.Add(this.chartHistory);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tab1);
            this.splitContainer1.Size = new System.Drawing.Size(884, 895);
            this.splitContainer1.SplitterDistance = 447;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            // 
            // performanceCounters1
            // 
            this.performanceCounters1.CounterID = 0;
            this.performanceCounters1.CounterName = null;
            this.performanceCounters1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceCounters1.FromDate = new System.DateTime(((long)(0)));
            this.performanceCounters1.InstanceID = 0;
            this.performanceCounters1.Location = new System.Drawing.Point(0, 27);
            this.performanceCounters1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.performanceCounters1.Name = "performanceCounters1";
            this.performanceCounters1.Size = new System.Drawing.Size(884, 420);
            this.performanceCounters1.TabIndex = 3;
            this.performanceCounters1.ToDate = new System.DateTime(((long)(0)));
            this.performanceCounters1.Visible = false;
            // 
            // pieChart1
            // 
            this.pieChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pieChart1.Location = new System.Drawing.Point(0, 27);
            this.pieChart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pieChart1.Name = "pieChart1";
            this.pieChart1.Size = new System.Drawing.Size(884, 420);
            this.pieChart1.TabIndex = 0;
            this.pieChart1.Text = "pieChart1";
            // 
            // chartHistory
            // 
            this.chartHistory.DefaultLineSmoothness = 0.5D;
            this.chartHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartHistory.Location = new System.Drawing.Point(0, 27);
            this.chartHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chartHistory.Name = "chartHistory";
            this.chartHistory.Size = new System.Drawing.Size(884, 420);
            this.chartHistory.TabIndex = 1;
            this.chartHistory.Text = "cartesianChartWithDataTable1";
            this.chartHistory.Visible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(884, 27);
            this.toolStrip1.TabIndex = 2;
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
            this.tsExcel.Text = "Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // tab1
            // 
            this.tab1.Controls.Add(this.tabClerks);
            this.tab1.Controls.Add(this.tabConfig);
            this.tab1.Controls.Add(this.tabCounters);
            this.tab1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab1.Location = new System.Drawing.Point(0, 0);
            this.tab1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tab1.Name = "tab1";
            this.tab1.SelectedIndex = 0;
            this.tab1.Size = new System.Drawing.Size(884, 443);
            this.tab1.TabIndex = 1;
            this.tab1.SelectedIndexChanged += new System.EventHandler(this.tab1_SelectedIndexChanged);
            // 
            // tabClerks
            // 
            this.tabClerks.Controls.Add(this.dgv);
            this.tabClerks.Location = new System.Drawing.Point(4, 29);
            this.tabClerks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabClerks.Name = "tabClerks";
            this.tabClerks.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabClerks.Size = new System.Drawing.Size(876, 410);
            this.tabClerks.TabIndex = 0;
            this.tabClerks.Text = "Memory Clerks";
            this.tabClerks.UseVisualStyleBackColor = true;
            // 
            // tabConfig
            // 
            this.tabConfig.Controls.Add(this.dgvConfig);
            this.tabConfig.Location = new System.Drawing.Point(4, 29);
            this.tabConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabConfig.Size = new System.Drawing.Size(876, 410);
            this.tabConfig.TabIndex = 1;
            this.tabConfig.Text = "Config";
            this.tabConfig.UseVisualStyleBackColor = true;
            // 
            // dgvConfig
            // 
            this.dgvConfig.AllowUserToAddRows = false;
            this.dgvConfig.AllowUserToDeleteRows = false;
            this.dgvConfig.BackgroundColor = System.Drawing.Color.White;
            this.dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvConfig.Location = new System.Drawing.Point(3, 4);
            this.dgvConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvConfig.Name = "dgvConfig";
            this.dgvConfig.ReadOnly = true;
            this.dgvConfig.RowHeadersVisible = false;
            this.dgvConfig.RowHeadersWidth = 51;
            this.dgvConfig.RowTemplate.Height = 24;
            this.dgvConfig.Size = new System.Drawing.Size(870, 402);
            this.dgvConfig.TabIndex = 0;
            // 
            // tabCounters
            // 
            this.tabCounters.Controls.Add(this.performanceCounterSummaryGrid1);
            this.tabCounters.Location = new System.Drawing.Point(4, 29);
            this.tabCounters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabCounters.Name = "tabCounters";
            this.tabCounters.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabCounters.Size = new System.Drawing.Size(876, 410);
            this.tabCounters.TabIndex = 2;
            this.tabCounters.Text = "Memory Performance Counters";
            this.tabCounters.UseVisualStyleBackColor = true;
            // 
            // performanceCounterSummaryGrid1
            // 
            this.performanceCounterSummaryGrid1.AllowUserToAddRows = false;
            this.performanceCounterSummaryGrid1.AllowUserToDeleteRows = false;
            this.performanceCounterSummaryGrid1.BackgroundColor = System.Drawing.Color.White;
            this.performanceCounterSummaryGrid1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.performanceCounterSummaryGrid1.Counters = null;
            this.performanceCounterSummaryGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceCounterSummaryGrid1.InstanceID = 0;
            this.performanceCounterSummaryGrid1.Location = new System.Drawing.Point(3, 4);
            this.performanceCounterSummaryGrid1.Name = "performanceCounterSummaryGrid1";
            this.performanceCounterSummaryGrid1.ReadOnly = true;
            this.performanceCounterSummaryGrid1.RowHeadersVisible = false;
            this.performanceCounterSummaryGrid1.RowHeadersWidth = 51;
            this.performanceCounterSummaryGrid1.RowTemplate.Height = 29;
            this.performanceCounterSummaryGrid1.SearchText = null;
            this.performanceCounterSummaryGrid1.Size = new System.Drawing.Size(870, 402);
            this.performanceCounterSummaryGrid1.TabIndex = 0;
            // 
            // MemoryUsage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MemoryUsage";
            this.Size = new System.Drawing.Size(884, 895);
            this.Load += new System.EventHandler(this.MemoryUsage_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tab1.ResumeLayout(false);
            this.tabClerks.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfig)).EndInit();
            this.tabCounters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.performanceCounterSummaryGrid1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private LiveCharts.WinForms.PieChart pieChart1;
        private CartesianChartWithDataTable chartHistory;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.TabControl tab1;
        private System.Windows.Forms.TabPage tabClerks;
        private System.Windows.Forms.TabPage tabConfig;
        private System.Windows.Forms.DataGridView dgvConfig;
        private System.Windows.Forms.TabPage tabCounters;
        private PerformanceCounters performanceCounters1;
        private PerformanceCounterSummaryGrid performanceCounterSummaryGrid1;
    }
}
