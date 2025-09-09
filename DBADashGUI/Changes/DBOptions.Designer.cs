using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class DBOptions
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
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgv = new DBADashDataGridView();
            toolStripDBInfo = new System.Windows.Forms.ToolStrip();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshInfo = new System.Windows.Forms.ToolStripButton();
            tsCopyInfo = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsSummary = new System.Windows.Forms.ToolStripButton();
            tsDetail = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsConfigureMetrics = new System.Windows.Forms.ToolStripDropDownButton();
            tsConfigureRoot = new System.Windows.Forms.ToolStripMenuItem();
            tsConfigureInstance = new System.Windows.Forms.ToolStripMenuItem();
            dgvHistory = new DBADashDataGridView();
            colHInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHSetting = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHOldValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHValueNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            tsHistoryCols = new System.Windows.Forms.ToolStripButton();
            tsClearFilterHistory = new System.Windows.Forms.ToolStripButton();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            excludeStateChangesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStripDBInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
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
            splitContainer1.Panel1.Controls.Add(dgv);
            splitContainer1.Panel1.Controls.Add(toolStripDBInfo);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvHistory);
            splitContainer1.Panel2.Controls.Add(toolStrip1);
            splitContainer1.Size = new System.Drawing.Size(839, 605);
            splitContainer1.SplitterDistance = 348;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
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
            dgv.Location = new System.Drawing.Point(0, 31);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(839, 317);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // toolStripDBInfo
            // 
            toolStripDBInfo.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStripDBInfo.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel2, tsRefreshInfo, tsCopyInfo, tsExcel, tsCols, tsSummary, tsDetail, tsClearFilter, tsConfigureMetrics });
            toolStripDBInfo.Location = new System.Drawing.Point(0, 0);
            toolStripDBInfo.Name = "toolStripDBInfo";
            toolStripDBInfo.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStripDBInfo.Size = new System.Drawing.Size(839, 31);
            toolStripDBInfo.TabIndex = 1;
            toolStripDBInfo.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            toolStripLabel2.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(107, 24);
            toolStripLabel2.Text = "Database Info";
            // 
            // tsRefreshInfo
            // 
            tsRefreshInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshInfo.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshInfo.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsRefreshInfo.Name = "tsRefreshInfo";
            tsRefreshInfo.Size = new System.Drawing.Size(29, 24);
            tsRefreshInfo.Text = "Refresh";
            tsRefreshInfo.Click += TsRefreshInfo_Click;
            // 
            // tsCopyInfo
            // 
            tsCopyInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyInfo.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyInfo.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCopyInfo.Name = "tsCopyInfo";
            tsCopyInfo.Size = new System.Drawing.Size(29, 24);
            tsCopyInfo.Text = "Copy";
            tsCopyInfo.Click += TsCopyInfo_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsSummary
            // 
            tsSummary.Image = Properties.Resources.Table_16x;
            tsSummary.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSummary.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsSummary.Name = "tsSummary";
            tsSummary.Size = new System.Drawing.Size(95, 24);
            tsSummary.Text = "Summary";
            tsSummary.Visible = false;
            tsSummary.Click += TsSummary_Click;
            // 
            // tsDetail
            // 
            tsDetail.Image = Properties.Resources.Table_16x;
            tsDetail.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDetail.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsDetail.Name = "tsDetail";
            tsDetail.Size = new System.Drawing.Size(73, 24);
            tsDetail.Text = "Detail";
            tsDetail.Click += TsDetail_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Filter_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // tsConfigureMetrics
            // 
            tsConfigureMetrics.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsConfigureRoot, tsConfigureInstance });
            tsConfigureMetrics.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigureMetrics.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigureMetrics.Name = "tsConfigureMetrics";
            tsConfigureMetrics.Size = new System.Drawing.Size(160, 28);
            tsConfigureMetrics.Text = "Configure Metrics";
            // 
            // tsConfigureRoot
            // 
            tsConfigureRoot.Name = "tsConfigureRoot";
            tsConfigureRoot.Size = new System.Drawing.Size(224, 26);
            tsConfigureRoot.Text = "Root Level";
            tsConfigureRoot.Click += ConfigureRoot_Click;
            // 
            // tsConfigureInstance
            // 
            tsConfigureInstance.Name = "tsConfigureInstance";
            tsConfigureInstance.Size = new System.Drawing.Size(224, 26);
            tsConfigureInstance.Text = "Instance Level";
            tsConfigureInstance.Click += ConfigureInstance_Click;
            // 
            // dgvHistory
            // 
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.AllowUserToOrderColumns = true;
            dgvHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colHInstance, colHDB, colHSetting, colHOldValue, colHValueNew, colHChangeDate });
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvHistory.DefaultCellStyle = dataGridViewCellStyle4;
            dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.Location = new System.Drawing.Point(0, 31);
            dgvHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvHistory.Name = "dgvHistory";
            dgvHistory.ReadOnly = true;
            dgvHistory.ResultSetID = 0;
            dgvHistory.ResultSetName = null;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.RowHeadersWidth = 51;
            dgvHistory.Size = new System.Drawing.Size(839, 221);
            dgvHistory.TabIndex = 0;
            // 
            // colHInstance
            // 
            colHInstance.DataPropertyName = "InstanceGroupName";
            colHInstance.HeaderText = "Instance";
            colHInstance.MinimumWidth = 6;
            colHInstance.Name = "colHInstance";
            colHInstance.ReadOnly = true;
            colHInstance.Width = 90;
            // 
            // colHDB
            // 
            colHDB.DataPropertyName = "DB";
            colHDB.HeaderText = "DB";
            colHDB.MinimumWidth = 6;
            colHDB.Name = "colHDB";
            colHDB.ReadOnly = true;
            colHDB.Width = 56;
            // 
            // colHSetting
            // 
            colHSetting.DataPropertyName = "Setting";
            colHSetting.HeaderText = "Setting";
            colHSetting.MinimumWidth = 6;
            colHSetting.Name = "colHSetting";
            colHSetting.ReadOnly = true;
            colHSetting.Width = 81;
            // 
            // colHOldValue
            // 
            colHOldValue.DataPropertyName = "OldValue";
            colHOldValue.HeaderText = "Value (Old)";
            colHOldValue.MinimumWidth = 6;
            colHOldValue.Name = "colHOldValue";
            colHOldValue.ReadOnly = true;
            colHOldValue.Width = 109;
            // 
            // colHValueNew
            // 
            colHValueNew.DataPropertyName = "NewValue";
            colHValueNew.HeaderText = "Value (New)";
            colHValueNew.MinimumWidth = 6;
            colHValueNew.Name = "colHValueNew";
            colHValueNew.ReadOnly = true;
            colHValueNew.Width = 114;
            // 
            // colHChangeDate
            // 
            colHChangeDate.DataPropertyName = "ChangeDate";
            colHChangeDate.HeaderText = "Change Date";
            colHChangeDate.MinimumWidth = 6;
            colHChangeDate.Name = "colHChangeDate";
            colHChangeDate.ReadOnly = true;
            colHChangeDate.Width = 120;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefreshHistory, tsCopyHistory, tsExcelHistory, tsHistoryCols, tsClearFilterHistory, tsFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip1.Size = new System.Drawing.Size(839, 31);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            toolStripLabel1.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(61, 24);
            toolStripLabel1.Text = "History";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHistory.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsRefreshHistory.Name = "tsRefreshHistory";
            tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            tsRefreshHistory.Text = "Refresh";
            tsRefreshHistory.Click += TsRefreshHistory_Click;
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // tsHistoryCols
            // 
            tsHistoryCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHistoryCols.Image = Properties.Resources.Column_16x;
            tsHistoryCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHistoryCols.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsHistoryCols.Name = "tsHistoryCols";
            tsHistoryCols.Size = new System.Drawing.Size(29, 24);
            tsHistoryCols.Text = "Columns";
            tsHistoryCols.Click += HistoryCols_Click;
            // 
            // tsClearFilterHistory
            // 
            tsClearFilterHistory.Enabled = false;
            tsClearFilterHistory.Image = Properties.Resources.Filter_16x;
            tsClearFilterHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterHistory.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsClearFilterHistory.Name = "tsClearFilterHistory";
            tsClearFilterHistory.Size = new System.Drawing.Size(104, 24);
            tsClearFilterHistory.Text = "Clear Filter";
            // 
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { excludeStateChangesToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // excludeStateChangesToolStripMenuItem
            // 
            excludeStateChangesToolStripMenuItem.Checked = true;
            excludeStateChangesToolStripMenuItem.CheckOnClick = true;
            excludeStateChangesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            excludeStateChangesToolStripMenuItem.Name = "excludeStateChangesToolStripMenuItem";
            excludeStateChangesToolStripMenuItem.Size = new System.Drawing.Size(241, 26);
            excludeStateChangesToolStripMenuItem.Text = "Exclude State Changes";
            excludeStateChangesToolStripMenuItem.Click += ExcludeStateChangesToolStripMenuItem_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "DB";
            dataGridViewTextBoxColumn2.HeaderText = "DB";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 56;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "Setting";
            dataGridViewTextBoxColumn3.HeaderText = "Setting";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 81;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "OldValue";
            dataGridViewTextBoxColumn4.HeaderText = "Value (Old)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 109;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "NewValue";
            dataGridViewTextBoxColumn5.HeaderText = "Value (New)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 114;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "ChangeDate";
            dataGridViewTextBoxColumn6.HeaderText = "Change Date";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 120;
            // 
            // DBOptions
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DBOptions";
            Size = new System.Drawing.Size(839, 605);
            Load += DBOptions_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStripDBInfo.ResumeLayout(false);
            toolStripDBInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvHistory;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStripDBInfo;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsRefreshInfo;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.ToolStripButton tsCopyInfo;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem excludeStateChangesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsSummary;
        private System.Windows.Forms.ToolStripButton tsDetail;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHSetting;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHOldValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHChangeDate;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripButton tsClearFilterHistory;
        private System.Windows.Forms.ToolStripButton tsHistoryCols;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigureMetrics;
        private System.Windows.Forms.ToolStripMenuItem tsConfigureRoot;
        private System.Windows.Forms.ToolStripMenuItem tsConfigureInstance;
    }
}
