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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.colObject = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colCounter = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colMaxValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMinValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAVGValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Total = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SampleCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrentValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colView = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.txtSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tsClear = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.performanceCounters1 = new DBADashGUI.Performance.PerformanceCounters();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colObject,
            this.colCounter,
            this.colInstance,
            this.colMaxValue,
            this.colMinValue,
            this.colAVGValue,
            this.Total,
            this.SampleCount,
            this.colCurrentValue,
            this.colView});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(759, 438);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            // 
            // colObject
            // 
            this.colObject.DataPropertyName = "object_name";
            this.colObject.HeaderText = "Object";
            this.colObject.MinimumWidth = 6;
            this.colObject.Name = "colObject";
            this.colObject.ReadOnly = true;
            this.colObject.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colObject.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colObject.Width = 78;
            // 
            // colCounter
            // 
            this.colCounter.DataPropertyName = "counter_name";
            this.colCounter.HeaderText = "Counter";
            this.colCounter.MinimumWidth = 6;
            this.colCounter.Name = "colCounter";
            this.colCounter.ReadOnly = true;
            this.colCounter.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colCounter.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colCounter.Width = 87;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "instance_name";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colInstance.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colInstance.Width = 90;
            // 
            // colMaxValue
            // 
            this.colMaxValue.DataPropertyName = "MaxValue";
            dataGridViewCellStyle1.Format = "#,##0.########";
            dataGridViewCellStyle1.NullValue = null;
            this.colMaxValue.DefaultCellStyle = dataGridViewCellStyle1;
            this.colMaxValue.HeaderText = "Max Value";
            this.colMaxValue.MinimumWidth = 6;
            this.colMaxValue.Name = "colMaxValue";
            this.colMaxValue.ReadOnly = true;
            this.colMaxValue.Width = 102;
            // 
            // colMinValue
            // 
            this.colMinValue.DataPropertyName = "MinValue";
            dataGridViewCellStyle2.Format = "#,##0.########";
            this.colMinValue.DefaultCellStyle = dataGridViewCellStyle2;
            this.colMinValue.HeaderText = "Min Value";
            this.colMinValue.MinimumWidth = 6;
            this.colMinValue.Name = "colMinValue";
            this.colMinValue.ReadOnly = true;
            this.colMinValue.Width = 99;
            // 
            // colAVGValue
            // 
            this.colAVGValue.DataPropertyName = "AvgValue";
            dataGridViewCellStyle3.Format = "#,##0.########";
            this.colAVGValue.DefaultCellStyle = dataGridViewCellStyle3;
            this.colAVGValue.HeaderText = "Avg Value";
            this.colAVGValue.MinimumWidth = 6;
            this.colAVGValue.Name = "colAVGValue";
            this.colAVGValue.ReadOnly = true;
            this.colAVGValue.Width = 101;
            // 
            // Total
            // 
            this.Total.DataPropertyName = "TotalValue";
            dataGridViewCellStyle4.Format = "#,##0.########";
            this.Total.DefaultCellStyle = dataGridViewCellStyle4;
            this.Total.HeaderText = "Total";
            this.Total.MinimumWidth = 6;
            this.Total.Name = "Total";
            this.Total.ReadOnly = true;
            this.Total.Width = 69;
            // 
            // SampleCount
            // 
            this.SampleCount.DataPropertyName = "SampleCount";
            dataGridViewCellStyle5.Format = "#,##0";
            this.SampleCount.DefaultCellStyle = dataGridViewCellStyle5;
            this.SampleCount.HeaderText = "Sample Count";
            this.SampleCount.MinimumWidth = 6;
            this.SampleCount.Name = "SampleCount";
            this.SampleCount.ReadOnly = true;
            this.SampleCount.Width = 125;
            // 
            // colCurrentValue
            // 
            this.colCurrentValue.DataPropertyName = "CurrentValue";
            dataGridViewCellStyle6.Format = "#,##0.########";
            this.colCurrentValue.DefaultCellStyle = dataGridViewCellStyle6;
            this.colCurrentValue.HeaderText = "Current Value";
            this.colCurrentValue.MinimumWidth = 6;
            this.colCurrentValue.Name = "colCurrentValue";
            this.colCurrentValue.ReadOnly = true;
            this.colCurrentValue.Width = 124;
            // 
            // colView
            // 
            this.colView.HeaderText = "";
            this.colView.MinimumWidth = 6;
            this.colView.Name = "colView";
            this.colView.ReadOnly = true;
            this.colView.Text = "View";
            this.colView.UseColumnTextForLinkValue = true;
            this.colView.Width = 6;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.toolStripLabel1,
            this.txtSearch,
            this.tsClear});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(759, 27);
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
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(53, 24);
            this.toolStripLabel1.Text = "Search";
            // 
            // txtSearch
            // 
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(100, 27);
            this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
            // 
            // tsClear
            // 
            this.tsClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsClear.Image = global::DBADashGUI.Properties.Resources.Eraser_16x;
            this.tsClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsClear.Name = "tsClear";
            this.tsClear.Size = new System.Drawing.Size(29, 24);
            this.tsClear.Text = "Clear";
            this.tsClear.Click += new System.EventHandler(this.tsClear_Click);
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
            this.splitContainer1.Panel1.Controls.Add(this.performanceCounters1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Size = new System.Drawing.Size(759, 878);
            this.splitContainer1.SplitterDistance = 435;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 6;
            // 
            // performanceCounters1
            // 
            this.performanceCounters1.CounterID = 0;
            this.performanceCounters1.CounterName = null;
            this.performanceCounters1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.performanceCounters1.FromDate = new System.DateTime(((long)(0)));
            this.performanceCounters1.InstanceID = 0;
            this.performanceCounters1.Location = new System.Drawing.Point(0, 0);
            this.performanceCounters1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.performanceCounters1.Name = "performanceCounters1";
            this.performanceCounters1.Size = new System.Drawing.Size(759, 435);
            this.performanceCounters1.TabIndex = 5;
            this.performanceCounters1.ToDate = new System.DateTime(((long)(0)));
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "object_name";
            dataGridViewCellStyle7.Format = "#,##0.########";
            dataGridViewCellStyle7.NullValue = null;
            this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridViewTextBoxColumn1.HeaderText = "Object";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 78;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "counter_name";
            dataGridViewCellStyle8.Format = "#,##0.########";
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn2.HeaderText = "Counter";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 87;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "instance";
            dataGridViewCellStyle9.Format = "#,##0.########";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn3.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 90;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "MaxValue";
            dataGridViewCellStyle10.Format = "#,##0.########";
            dataGridViewCellStyle10.NullValue = null;
            this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn4.HeaderText = "Max Value";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 102;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "MinValue";
            dataGridViewCellStyle11.Format = "#,##0.########";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn5.HeaderText = "Min Value";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 99;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "AvgValue";
            dataGridViewCellStyle12.Format = "#,##0.########";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn6.HeaderText = "Avg Value";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 101;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "CurrentValue";
            dataGridViewCellStyle13.Format = "#,##0.########";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle13;
            this.dataGridViewTextBoxColumn7.HeaderText = "Current Value";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 124;
            // 
            // PerformanceCounterSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "PerformanceCounterSummary";
            this.Size = new System.Drawing.Size(759, 905);
            this.Load += new System.EventHandler(this.PerformanceCounterSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private PerformanceCounters performanceCounters1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.ToolStripButton tsClear;
        private System.Windows.Forms.DataGridViewLinkColumn colObject;
        private System.Windows.Forms.DataGridViewLinkColumn colCounter;
        private System.Windows.Forms.DataGridViewLinkColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMinValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAVGValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn Total;
        private System.Windows.Forms.DataGridViewTextBoxColumn SampleCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentValue;
        private System.Windows.Forms.DataGridViewLinkColumn colView;
        private System.Windows.Forms.ToolStripButton tsExcel;
    }
}
