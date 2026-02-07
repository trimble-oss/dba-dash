
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;

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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MemoryUsage));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            dgv = new DBADashDataGridView();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            chartClerk = new CartesianChartWithDataTable();
            performanceCounters1 = new PerformanceCounters();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            avgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            maxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsPieChart = new System.Windows.Forms.ToolStripButton();
            tab1 = new ThemedTabControl();
            tabClerks = new System.Windows.Forms.TabPage();
            tabConfig = new System.Windows.Forms.TabPage();
            dgvConfig = new DBADashDataGridView();
            tabCounters = new System.Windows.Forms.TabPage();
            performanceCounterSummaryGrid1 = new PerformanceCounterSummaryGrid();
            pieChart1 = new LiveChartsCore.SkiaSharpView.WinForms.PieChart();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            tab1.SuspendLayout();
            tabClerks.SuspendLayout();
            tabConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConfig).BeginInit();
            tabCounters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)performanceCounterSummaryGrid1).BeginInit();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
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
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle2;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(3, 4);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.ShowCellToolTips = false;
            dgv.Size = new System.Drawing.Size(870, 392);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellMouseEnter += Dgv_CellMouseEnter;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(pieChart1);
            splitContainer1.Panel1.Controls.Add(chartClerk);
            splitContainer1.Panel1.Controls.Add(performanceCounters1);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tab1);
            splitContainer1.Size = new System.Drawing.Size(884, 895);
            splitContainer1.SplitterDistance = 447;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // chartClerk
            // 
            chartClerk.DefaultLineSmoothness = 0.5D;
            chartClerk.Dock = System.Windows.Forms.DockStyle.Fill;
            chartClerk.Location = new System.Drawing.Point(0, 31);
            chartClerk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chartClerk.Name = "chartClerk";
            chartClerk.Size = new System.Drawing.Size(884, 416);
            chartClerk.TabIndex = 1;
            chartClerk.Text = "cartesianChartWithDataTable1";
            chartClerk.Visible = false;
            // 
            // performanceCounters1
            // 
            performanceCounters1.Dock = System.Windows.Forms.DockStyle.Fill;
            performanceCounters1.Location = new System.Drawing.Point(0, 31);
            performanceCounters1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            performanceCounters1.Name = "performanceCounters1";
            performanceCounters1.Size = new System.Drawing.Size(884, 416);
            performanceCounters1.TabIndex = 3;
            performanceCounters1.Visible = false;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsDateGroup, tsAgg, tsPieChart });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip1.Size = new System.Drawing.Size(884, 31);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
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
            tsCopy.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
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
            tsExcel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsDateGroup
            // 
            tsDateGroup.Image = Properties.Resources.Time_16x;
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(79, 24);
            tsDateGroup.Text = "None";
            tsDateGroup.ToolTipText = "Date Group";
            // 
            // tsAgg
            // 
            tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { avgToolStripMenuItem, minToolStripMenuItem, maxToolStripMenuItem });
            tsAgg.Image = Properties.Resources.AddComputedField_16x;
            tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsAgg.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsAgg.Name = "tsAgg";
            tsAgg.Size = new System.Drawing.Size(71, 24);
            tsAgg.Text = "Max";
            tsAgg.ToolTipText = "Aggregate Type";
            // 
            // avgToolStripMenuItem
            // 
            avgToolStripMenuItem.Name = "avgToolStripMenuItem";
            avgToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            avgToolStripMenuItem.Text = "Avg";
            avgToolStripMenuItem.Click += TsAGG_Click;
            // 
            // minToolStripMenuItem
            // 
            minToolStripMenuItem.Name = "minToolStripMenuItem";
            minToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            minToolStripMenuItem.Text = "Min";
            minToolStripMenuItem.Click += TsAGG_Click;
            // 
            // maxToolStripMenuItem
            // 
            maxToolStripMenuItem.Checked = true;
            maxToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            maxToolStripMenuItem.Name = "maxToolStripMenuItem";
            maxToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            maxToolStripMenuItem.Text = "Max";
            maxToolStripMenuItem.Click += TsAGG_Click;
            // 
            // tsPieChart
            // 
            tsPieChart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPieChart.Image = Properties.Resources.PieChartHS;
            tsPieChart.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPieChart.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsPieChart.Name = "tsPieChart";
            tsPieChart.Size = new System.Drawing.Size(29, 24);
            tsPieChart.Text = "Pie Chart";
            tsPieChart.Click += TsPieChart_Click;
            // 
            // tab1
            // 
            tab1.Controls.Add(tabClerks);
            tab1.Controls.Add(tabConfig);
            tab1.Controls.Add(tabCounters);
            tab1.Dock = System.Windows.Forms.DockStyle.Fill;
            tab1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tab1.Location = new System.Drawing.Point(0, 0);
            tab1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tab1.Name = "tab1";
            tab1.Padding = new System.Drawing.Point(20, 8);
            tab1.SelectedIndex = 0;
            tab1.Size = new System.Drawing.Size(884, 443);
            tab1.TabIndex = 1;
            tab1.SelectedIndexChanged += Tab1_SelectedIndexChanged;
            // 
            // tabClerks
            // 
            tabClerks.Controls.Add(dgv);
            tabClerks.Location = new System.Drawing.Point(4, 39);
            tabClerks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabClerks.Name = "tabClerks";
            tabClerks.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabClerks.Size = new System.Drawing.Size(876, 400);
            tabClerks.TabIndex = 0;
            tabClerks.Text = "Memory Clerks";
            tabClerks.UseVisualStyleBackColor = true;
            // 
            // tabConfig
            // 
            tabConfig.Controls.Add(dgvConfig);
            tabConfig.Location = new System.Drawing.Point(4, 39);
            tabConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabConfig.Name = "tabConfig";
            tabConfig.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabConfig.Size = new System.Drawing.Size(876, 400);
            tabConfig.TabIndex = 1;
            tabConfig.Text = "Config";
            tabConfig.UseVisualStyleBackColor = true;
            // 
            // dgvConfig
            // 
            dgvConfig.AllowUserToAddRows = false;
            dgvConfig.AllowUserToDeleteRows = false;
            dgvConfig.AllowUserToOrderColumns = true;
            dgvConfig.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvConfig.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvConfig.DefaultCellStyle = dataGridViewCellStyle4;
            dgvConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvConfig.EnableHeadersVisualStyles = false;
            dgvConfig.Location = new System.Drawing.Point(3, 4);
            dgvConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvConfig.Name = "dgvConfig";
            dgvConfig.ReadOnly = true;
            dgvConfig.RowHeadersVisible = false;
            dgvConfig.RowHeadersWidth = 51;
            dgvConfig.Size = new System.Drawing.Size(870, 392);
            dgvConfig.TabIndex = 0;
            // 
            // tabCounters
            // 
            tabCounters.Controls.Add(performanceCounterSummaryGrid1);
            tabCounters.Location = new System.Drawing.Point(4, 39);
            tabCounters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabCounters.Name = "tabCounters";
            tabCounters.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabCounters.Size = new System.Drawing.Size(876, 400);
            tabCounters.TabIndex = 2;
            tabCounters.Text = "Memory Performance Counters";
            tabCounters.UseVisualStyleBackColor = true;
            // 
            // performanceCounterSummaryGrid1
            // 
            performanceCounterSummaryGrid1.AllowUserToAddRows = false;
            performanceCounterSummaryGrid1.AllowUserToDeleteRows = false;
            performanceCounterSummaryGrid1.AllowUserToOrderColumns = true;
            performanceCounterSummaryGrid1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            performanceCounterSummaryGrid1.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            performanceCounterSummaryGrid1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            performanceCounterSummaryGrid1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            performanceCounterSummaryGrid1.DefaultCellStyle = dataGridViewCellStyle6;
            performanceCounterSummaryGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            performanceCounterSummaryGrid1.EnableHeadersVisualStyles = false;
            performanceCounterSummaryGrid1.Location = new System.Drawing.Point(3, 4);
            performanceCounterSummaryGrid1.Name = "performanceCounterSummaryGrid1";
            performanceCounterSummaryGrid1.ReadOnly = true;
            performanceCounterSummaryGrid1.RowHeadersVisible = false;
            performanceCounterSummaryGrid1.RowHeadersWidth = 51;
            performanceCounterSummaryGrid1.Size = new System.Drawing.Size(870, 392);
            performanceCounterSummaryGrid1.TabIndex = 0;
            // 
            // pieChart1
            // 
            pieChart1.AutoUpdateEnabled = true;
            pieChart1.ChartTheme = null;
            pieChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            pieChart1.ForceGPU = false;
            skDefaultLegend1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend1.Content = null;
            skDefaultLegend1.IsValid = true;
            skDefaultLegend1.Opacity = 1F;
            padding1.Bottom = 0F;
            padding1.Left = 0F;
            padding1.Right = 0F;
            padding1.Top = 0F;
            skDefaultLegend1.Padding = padding1;
            skDefaultLegend1.RemoveOnCompleted = false;
            skDefaultLegend1.RotateTransform = 0F;
            skDefaultLegend1.X = 0F;
            skDefaultLegend1.Y = 0F;
            pieChart1.Legend = skDefaultLegend1;
            pieChart1.Location = new System.Drawing.Point(0, 31);
            pieChart1.Name = "pieChart1";
            pieChart1.Size = new System.Drawing.Size(884, 416);
            pieChart1.TabIndex = 4;
            skDefaultTooltip1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip1.Content = null;
            skDefaultTooltip1.IsValid = true;
            skDefaultTooltip1.Opacity = 1F;
            padding2.Bottom = 0F;
            padding2.Left = 0F;
            padding2.Right = 0F;
            padding2.Top = 0F;
            skDefaultTooltip1.Padding = padding2;
            skDefaultTooltip1.RemoveOnCompleted = false;
            skDefaultTooltip1.RotateTransform = 0F;
            skDefaultTooltip1.Wedge = 10;
            skDefaultTooltip1.X = 0F;
            skDefaultTooltip1.Y = 0F;
            pieChart1.Tooltip = skDefaultTooltip1;
            pieChart1.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // MemoryUsage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "MemoryUsage";
            Size = new System.Drawing.Size(884, 895);
            Load += MemoryUsage_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            tab1.ResumeLayout(false);
            tabClerks.ResumeLayout(false);
            tabConfig.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConfig).EndInit();
            tabCounters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)performanceCounterSummaryGrid1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private CartesianChartWithDataTable chartClerk;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private ThemedTabControl tab1;
        private System.Windows.Forms.TabPage tabClerks;
        private System.Windows.Forms.TabPage tabConfig;
        private DBADashDataGridView dgvConfig;
        private System.Windows.Forms.TabPage tabCounters;
        private PerformanceCounters performanceCounters1;
        private PerformanceCounterSummaryGrid performanceCounterSummaryGrid1;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsAgg;
        private System.Windows.Forms.ToolStripMenuItem avgToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem maxToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsPieChart;
        private LiveChartsCore.SkiaSharpView.WinForms.PieChart pieChart1;
    }
}
