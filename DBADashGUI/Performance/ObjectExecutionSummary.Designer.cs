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
            components = new System.ComponentModel.Container();
            dgv = new System.Windows.Forms.DataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCompare = new System.Windows.Forms.ToolStripDropDownButton();
            tsPreviousPeriod = new System.Windows.Forms.ToolStripMenuItem();
            ts24Hrs = new System.Windows.Forms.ToolStripMenuItem();
            ts7Days = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            tsCustomCompare = new System.Windows.Forms.ToolStripMenuItem();
            tsNoCompare = new System.Windows.Forms.ToolStripMenuItem();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsType = new System.Windows.Forms.ToolStripDropDownButton();
            procedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            triggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cLRProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cLRTriggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            scalarFunctionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            extendedStoredProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            lblSearch = new System.Windows.Forms.ToolStripLabel();
            txtSearch = new System.Windows.Forms.ToolStripTextBox();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            splitChart = new System.Windows.Forms.SplitContainer();
            objectExecutionLineChart1 = new ObjectExecutionLineChart();
            compareObjectExecutionLineChart = new ObjectExecutionLineChart();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            tmrSearch = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitChart).BeginInit();
            splitChart.Panel1.SuspendLayout();
            splitChart.Panel2.SuspendLayout();
            splitChart.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.RowTemplate.Height = 24;
            dgv.Size = new System.Drawing.Size(1262, 297);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsCompare, tsCols, tsType, toolStripSeparator1, lblSearch, txtSearch });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1262, 27);
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
            // tsCompare
            // 
            tsCompare.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsPreviousPeriod, ts24Hrs, ts7Days, toolStripSeparator2, tsCustomCompare, tsNoCompare });
            tsCompare.Image = Properties.Resources.Time_16x;
            tsCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCompare.Name = "tsCompare";
            tsCompare.Size = new System.Drawing.Size(124, 24);
            tsCompare.Text = "Compare To";
            // 
            // tsPreviousPeriod
            // 
            tsPreviousPeriod.Font = new System.Drawing.Font("Segoe UI", 9F);
            tsPreviousPeriod.Name = "tsPreviousPeriod";
            tsPreviousPeriod.Size = new System.Drawing.Size(224, 26);
            tsPreviousPeriod.Tag = "-1";
            tsPreviousPeriod.Text = "Previous Period";
            tsPreviousPeriod.Click += TsSetOffset_Click;
            // 
            // ts24Hrs
            // 
            ts24Hrs.Font = new System.Drawing.Font("Segoe UI", 9F);
            ts24Hrs.Name = "ts24Hrs";
            ts24Hrs.Size = new System.Drawing.Size(224, 26);
            ts24Hrs.Tag = "1440";
            ts24Hrs.Text = "-24hrs offset";
            ts24Hrs.Click += TsSetOffset_Click;
            // 
            // ts7Days
            // 
            ts7Days.Font = new System.Drawing.Font("Segoe UI", 9F);
            ts7Days.Name = "ts7Days";
            ts7Days.Size = new System.Drawing.Size(224, 26);
            ts7Days.Tag = "10080";
            ts7Days.Text = "-7 days offset";
            ts7Days.Click += TsSetOffset_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(221, 6);
            // 
            // tsCustomCompare
            // 
            tsCustomCompare.Font = new System.Drawing.Font("Segoe UI", 9F);
            tsCustomCompare.Name = "tsCustomCompare";
            tsCustomCompare.Size = new System.Drawing.Size(224, 26);
            tsCustomCompare.Tag = "-1";
            tsCustomCompare.Text = "Custom";
            tsCustomCompare.Click += TsCustomCompare_Click;
            // 
            // tsNoCompare
            // 
            tsNoCompare.Checked = true;
            tsNoCompare.CheckState = System.Windows.Forms.CheckState.Checked;
            tsNoCompare.Font = new System.Drawing.Font("Segoe UI", 9F);
            tsNoCompare.Name = "tsNoCompare";
            tsNoCompare.Size = new System.Drawing.Size(224, 26);
            tsNoCompare.Tag = "-1";
            tsNoCompare.Text = "None";
            tsNoCompare.Click += TsSetOffset_Click;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsType
            // 
            tsType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { procedureToolStripMenuItem, triggerToolStripMenuItem, cLRProcedureToolStripMenuItem, cLRTriggerToolStripMenuItem, scalarFunctionToolStripMenuItem, extendedStoredProcedureToolStripMenuItem });
            tsType.Image = Properties.Resources.FilterDropdown_16x;
            tsType.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsType.Name = "tsType";
            tsType.Size = new System.Drawing.Size(74, 24);
            tsType.Text = "Type";
            // 
            // procedureToolStripMenuItem
            // 
            procedureToolStripMenuItem.CheckOnClick = true;
            procedureToolStripMenuItem.Name = "procedureToolStripMenuItem";
            procedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            procedureToolStripMenuItem.Tag = "P";
            procedureToolStripMenuItem.Text = "Procedure";
            procedureToolStripMenuItem.Click += TsType_Click;
            // 
            // triggerToolStripMenuItem
            // 
            triggerToolStripMenuItem.CheckOnClick = true;
            triggerToolStripMenuItem.Name = "triggerToolStripMenuItem";
            triggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            triggerToolStripMenuItem.Tag = "TR";
            triggerToolStripMenuItem.Text = "Trigger";
            triggerToolStripMenuItem.Click += TsType_Click;
            // 
            // cLRProcedureToolStripMenuItem
            // 
            cLRProcedureToolStripMenuItem.CheckOnClick = true;
            cLRProcedureToolStripMenuItem.Name = "cLRProcedureToolStripMenuItem";
            cLRProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            cLRProcedureToolStripMenuItem.Tag = "PC";
            cLRProcedureToolStripMenuItem.Text = "CLR Procedure";
            cLRProcedureToolStripMenuItem.Click += TsType_Click;
            // 
            // cLRTriggerToolStripMenuItem
            // 
            cLRTriggerToolStripMenuItem.CheckOnClick = true;
            cLRTriggerToolStripMenuItem.Name = "cLRTriggerToolStripMenuItem";
            cLRTriggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            cLRTriggerToolStripMenuItem.Tag = "TA";
            cLRTriggerToolStripMenuItem.Text = "CLR Trigger";
            cLRTriggerToolStripMenuItem.Click += TsType_Click;
            // 
            // scalarFunctionToolStripMenuItem
            // 
            scalarFunctionToolStripMenuItem.CheckOnClick = true;
            scalarFunctionToolStripMenuItem.Name = "scalarFunctionToolStripMenuItem";
            scalarFunctionToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            scalarFunctionToolStripMenuItem.Tag = "FN";
            scalarFunctionToolStripMenuItem.Text = "Scalar Function";
            scalarFunctionToolStripMenuItem.Click += TsType_Click;
            // 
            // extendedStoredProcedureToolStripMenuItem
            // 
            extendedStoredProcedureToolStripMenuItem.CheckOnClick = true;
            extendedStoredProcedureToolStripMenuItem.Name = "extendedStoredProcedureToolStripMenuItem";
            extendedStoredProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            extendedStoredProcedureToolStripMenuItem.Tag = "X";
            extendedStoredProcedureToolStripMenuItem.Text = "Extended Stored Procedure";
            extendedStoredProcedureToolStripMenuItem.Click += TsType_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // lblSearch
            // 
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(56, 24);
            lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(200, 27);
            txtSearch.TextChanged += TxtSearch_TextChanged;
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
            splitContainer1.Panel1.Controls.Add(splitChart);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Panel2.Controls.Add(statusStrip1);
            splitContainer1.Size = new System.Drawing.Size(1262, 1117);
            splitContainer1.SplitterDistance = 789;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 6;
            // 
            // splitChart
            // 
            splitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            splitChart.Location = new System.Drawing.Point(0, 0);
            splitChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitChart.Name = "splitChart";
            splitChart.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitChart.Panel1
            // 
            splitChart.Panel1.Controls.Add(objectExecutionLineChart1);
            // 
            // splitChart.Panel2
            // 
            splitChart.Panel2.Controls.Add(compareObjectExecutionLineChart);
            splitChart.Size = new System.Drawing.Size(1262, 789);
            splitChart.SplitterDistance = 391;
            splitChart.SplitterWidth = 5;
            splitChart.TabIndex = 7;
            // 
            // objectExecutionLineChart1
            // 
            objectExecutionLineChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            objectExecutionLineChart1.Location = new System.Drawing.Point(0, 0);
            objectExecutionLineChart1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            objectExecutionLineChart1.Name = "objectExecutionLineChart1";
            objectExecutionLineChart1.Size = new System.Drawing.Size(1262, 391);
            objectExecutionLineChart1.TabIndex = 5;
            objectExecutionLineChart1.Title = "abc";
            // 
            // compareObjectExecutionLineChart
            // 
            compareObjectExecutionLineChart.Dock = System.Windows.Forms.DockStyle.Fill;
            compareObjectExecutionLineChart.Location = new System.Drawing.Point(0, 0);
            compareObjectExecutionLineChart.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            compareObjectExecutionLineChart.Name = "compareObjectExecutionLineChart";
            compareObjectExecutionLineChart.Size = new System.Drawing.Size(1262, 393);
            compareObjectExecutionLineChart.TabIndex = 6;
            compareObjectExecutionLineChart.Title = "abc";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 297);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1262, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(124, 20);
            lblStatus.Text = "Refreshing Data...";
            // 
            // tmrSearch
            // 
            tmrSearch.Interval = 1000;
            tmrSearch.Tick += TmrSearch_Tick;
            // 
            // ObjectExecutionSummary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ObjectExecutionSummary";
            Size = new System.Drawing.Size(1262, 1144);
            Load += ObjectExecutionSummary_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitChart.Panel1.ResumeLayout(false);
            splitChart.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitChart).EndInit();
            splitChart.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.ToolStripMenuItem tsPreviousPeriod;
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
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
