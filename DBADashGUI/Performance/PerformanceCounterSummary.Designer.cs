namespace DBADashGUI.Performance
{
    partial class PerformanceCounterSummary
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PerformanceCounterSummary));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            txtSearch = new System.Windows.Forms.ToolStripTextBox();
            tsClear = new System.Windows.Forms.ToolStripButton();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            blockingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            CPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            IOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ObjectExecutionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            WaitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsToggleGrid = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            tsSaveView = new System.Windows.Forms.ToolStripButton();
            tsDeleteView = new System.Windows.Forms.ToolStripButton();
            savedViewMenuItem1 = new SavedViewMenuItem();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            layout1 = new System.Windows.Forms.TableLayoutPanel();
            performanceCounterSummaryGrid1 = new PerformanceCounterSummaryGrid();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)performanceCounterSummaryGrid1).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, toolStripLabel1, txtSearch, tsClear, toolStripDropDownButton1, tsToggleGrid, toolStripSeparator1, tsSaveView, tsDeleteView, savedViewMenuItem1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(978, 27);
            toolStrip1.TabIndex = 4;
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
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(53, 24);
            toolStripLabel1.Text = "Search";
            // 
            // txtSearch
            // 
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(100, 27);
            txtSearch.KeyPress += TxtSearch_KeyPress;
            // 
            // tsClear
            // 
            tsClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClear.Image = Properties.Resources.Eraser_16x;
            tsClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClear.Name = "tsClear";
            tsClear.Size = new System.Drawing.Size(29, 24);
            tsClear.Text = "Clear";
            tsClear.Click += TsClear_Click;
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { blockingToolStripMenuItem, CPUToolStripMenuItem, IOToolStripMenuItem, ObjectExecutionToolStripMenuItem, WaitsToolStripMenuItem });
            toolStripDropDownButton1.Image = (System.Drawing.Image)resources.GetObject("toolStripDropDownButton1.Image");
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(151, 24);
            toolStripDropDownButton1.Text = "Add Other Chart";
            // 
            // blockingToolStripMenuItem
            // 
            blockingToolStripMenuItem.Name = "blockingToolStripMenuItem";
            blockingToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            blockingToolStripMenuItem.Text = "Blocking";
            blockingToolStripMenuItem.Click += BlockingToolStripMenuItem_Click;
            // 
            // CPUToolStripMenuItem
            // 
            CPUToolStripMenuItem.Name = "CPUToolStripMenuItem";
            CPUToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            CPUToolStripMenuItem.Text = "CPU";
            CPUToolStripMenuItem.Click += CPUToolStripMenuItem_Click;
            // 
            // IOToolStripMenuItem
            // 
            IOToolStripMenuItem.Name = "IOToolStripMenuItem";
            IOToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            IOToolStripMenuItem.Text = "IO ";
            IOToolStripMenuItem.Click += IOToolStripMenuItem_Click;
            // 
            // ObjectExecutionToolStripMenuItem
            // 
            ObjectExecutionToolStripMenuItem.Name = "ObjectExecutionToolStripMenuItem";
            ObjectExecutionToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            ObjectExecutionToolStripMenuItem.Text = "Object Execution";
            ObjectExecutionToolStripMenuItem.Click += ObjectExecutionToolStripMenuItem_Click;
            // 
            // WaitsToolStripMenuItem
            // 
            WaitsToolStripMenuItem.Name = "WaitsToolStripMenuItem";
            WaitsToolStripMenuItem.Size = new System.Drawing.Size(204, 26);
            WaitsToolStripMenuItem.Text = "Waits";
            WaitsToolStripMenuItem.Click += WaitsToolStripMenuItem_Click;
            // 
            // tsToggleGrid
            // 
            tsToggleGrid.Image = Properties.Resources.Table_16x;
            tsToggleGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsToggleGrid.Name = "tsToggleGrid";
            tsToggleGrid.Size = new System.Drawing.Size(97, 24);
            tsToggleGrid.Text = "Hide Grid";
            tsToggleGrid.Click += TsToggleGrid_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // tsSaveView
            // 
            tsSaveView.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsSaveView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSaveView.Image = Properties.Resources.Save_16x;
            tsSaveView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSaveView.Name = "tsSaveView";
            tsSaveView.Size = new System.Drawing.Size(29, 24);
            tsSaveView.Text = "Save View";
            tsSaveView.Click += TsSaveView_Click;
            // 
            // tsDeleteView
            // 
            tsDeleteView.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDeleteView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsDeleteView.Image = Properties.Resources.Close_red_16x;
            tsDeleteView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDeleteView.Name = "tsDeleteView";
            tsDeleteView.Size = new System.Drawing.Size(29, 24);
            tsDeleteView.Text = "Delete View";
            tsDeleteView.Visible = false;
            tsDeleteView.Click += TsDeleteView_Click;
            // 
            // savedViewMenuItem1
            // 
            savedViewMenuItem1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            savedViewMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            savedViewMenuItem1.Name = "savedViewMenuItem1";
            savedViewMenuItem1.Size = new System.Drawing.Size(55, 24);
            savedViewMenuItem1.Text = "View";
            savedViewMenuItem1.Type = SavedView.ViewTypes.Metric;
            savedViewMenuItem1.SavedViewSelected += SavedViewSelected;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(layout1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(performanceCounterSummaryGrid1);
            splitContainer1.Size = new System.Drawing.Size(978, 878);
            splitContainer1.SplitterDistance = 435;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 6;
            // 
            // layout1
            // 
            layout1.ColumnCount = 1;
            layout1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layout1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            layout1.Dock = System.Windows.Forms.DockStyle.Fill;
            layout1.Location = new System.Drawing.Point(0, 0);
            layout1.Name = "layout1";
            layout1.RowCount = 1;
            layout1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            layout1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            layout1.Size = new System.Drawing.Size(978, 435);
            layout1.TabIndex = 0;
            // 
            // performanceCounterSummaryGrid1
            // 
            performanceCounterSummaryGrid1.AllowUserToAddRows = false;
            performanceCounterSummaryGrid1.AllowUserToDeleteRows = false;
            performanceCounterSummaryGrid1.BackgroundColor = System.Drawing.Color.White;
            performanceCounterSummaryGrid1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            performanceCounterSummaryGrid1.Counters = null;
            performanceCounterSummaryGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            performanceCounterSummaryGrid1.InstanceID = 0;
            performanceCounterSummaryGrid1.Location = new System.Drawing.Point(0, 0);
            performanceCounterSummaryGrid1.Name = "performanceCounterSummaryGrid1";
            performanceCounterSummaryGrid1.ReadOnly = true;
            performanceCounterSummaryGrid1.RowHeadersVisible = false;
            performanceCounterSummaryGrid1.RowHeadersWidth = 51;
            performanceCounterSummaryGrid1.SearchText = null;
            performanceCounterSummaryGrid1.Size = new System.Drawing.Size(978, 438);
            performanceCounterSummaryGrid1.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "MaxValue";
            dataGridViewCellStyle1.Format = "#,##0.########";
            dataGridViewTextBoxColumn8.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewTextBoxColumn8.HeaderText = "MaxValue";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "MinValue";
            dataGridViewTextBoxColumn9.HeaderText = "MinValue";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "AvgValue";
            dataGridViewTextBoxColumn10.HeaderText = "AvgValue";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "Total";
            dataGridViewTextBoxColumn11.HeaderText = "Total";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "SampleCount";
            dataGridViewTextBoxColumn12.HeaderText = "SampleCount";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.Width = 125;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "CurrentValue";
            dataGridViewTextBoxColumn13.HeaderText = "CurrentValue";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "CurrentValue";
            dataGridViewTextBoxColumn7.HeaderText = "Current Value";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 124;
            // 
            // PerformanceCounterSummary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "PerformanceCounterSummary";
            Size = new System.Drawing.Size(978, 905);
            Load += PerformanceCounterSummary_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)performanceCounterSummaryGrid1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.ToolStripButton tsClear;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private PerformanceCounterSummaryGrid performanceCounterSummaryGrid1;
        private System.Windows.Forms.TableLayoutPanel layout1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem CPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem IOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem WaitsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ObjectExecutionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blockingToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsToggleGrid;
        private System.Windows.Forms.ToolStripButton tsSaveView;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsDeleteView;
        private SavedViewMenuItem savedViewMenuItem1;
    }
}
