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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshInfo = new System.Windows.Forms.ToolStripButton();
            this.tsCopyInfo = new System.Windows.Forms.ToolStripButton();
            this.tsSummary = new System.Windows.Forms.ToolStripButton();
            this.tsDetail = new System.Windows.Forms.ToolStripButton();
            this.dgvHistory = new System.Windows.Forms.DataGridView();
            this.colHInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHSetting = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHOldValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHValueNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.excludeStateChangesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgv);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvHistory);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(839, 484);
            this.splitContainer1.SplitterDistance = 279;
            this.splitContainer1.TabIndex = 0;
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 31);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(839, 248);
            this.dgv.TabIndex = 0;
            this.dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.tsRefreshInfo,
            this.tsCopyInfo,
            this.tsSummary,
            this.tsDetail});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(839, 31);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(107, 28);
            this.toolStripLabel2.Text = "Database Info";
            // 
            // tsRefreshInfo
            // 
            this.tsRefreshInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshInfo.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshInfo.Name = "tsRefreshInfo";
            this.tsRefreshInfo.Size = new System.Drawing.Size(29, 28);
            this.tsRefreshInfo.Text = "Refresh";
            this.tsRefreshInfo.Click += new System.EventHandler(this.tsRefreshInfo_Click);
            // 
            // tsCopyInfo
            // 
            this.tsCopyInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyInfo.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyInfo.Name = "tsCopyInfo";
            this.tsCopyInfo.Size = new System.Drawing.Size(29, 28);
            this.tsCopyInfo.Text = "Copy";
            this.tsCopyInfo.Click += new System.EventHandler(this.tsCopyInfo_Click);
            // 
            // tsSummary
            // 
            this.tsSummary.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsSummary.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSummary.Name = "tsSummary";
            this.tsSummary.Size = new System.Drawing.Size(95, 24);
            this.tsSummary.Text = "Summary";
            this.tsSummary.Visible = false;
            this.tsSummary.Click += new System.EventHandler(this.tsSummary_Click);
            // 
            // tsDetail
            // 
            this.tsDetail.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsDetail.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDetail.Name = "tsDetail";
            this.tsDetail.Size = new System.Drawing.Size(73, 28);
            this.tsDetail.Text = "Detail";
            this.tsDetail.Click += new System.EventHandler(this.tsDetail_Click);
            // 
            // dgvHistory
            // 
            this.dgvHistory.AllowUserToAddRows = false;
            this.dgvHistory.AllowUserToDeleteRows = false;
            this.dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvHistory.BackgroundColor = System.Drawing.Color.White;
            this.dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHInstance,
            this.colHDB,
            this.colHSetting,
            this.colHOldValue,
            this.colHValueNew,
            this.colHChangeDate});
            this.dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistory.Location = new System.Drawing.Point(0, 31);
            this.dgvHistory.Name = "dgvHistory";
            this.dgvHistory.ReadOnly = true;
            this.dgvHistory.RowHeadersVisible = false;
            this.dgvHistory.RowHeadersWidth = 51;
            this.dgvHistory.RowTemplate.Height = 24;
            this.dgvHistory.Size = new System.Drawing.Size(839, 170);
            this.dgvHistory.TabIndex = 0;
            // 
            // colHInstance
            // 
            this.colHInstance.DataPropertyName = "Instance";
            this.colHInstance.HeaderText = "Instance";
            this.colHInstance.MinimumWidth = 6;
            this.colHInstance.Name = "colHInstance";
            this.colHInstance.ReadOnly = true;
            this.colHInstance.Width = 90;
            // 
            // colHDB
            // 
            this.colHDB.DataPropertyName = "DB";
            this.colHDB.HeaderText = "DB";
            this.colHDB.MinimumWidth = 6;
            this.colHDB.Name = "colHDB";
            this.colHDB.ReadOnly = true;
            this.colHDB.Width = 56;
            // 
            // colHSetting
            // 
            this.colHSetting.DataPropertyName = "Setting";
            this.colHSetting.HeaderText = "Setting";
            this.colHSetting.MinimumWidth = 6;
            this.colHSetting.Name = "colHSetting";
            this.colHSetting.ReadOnly = true;
            this.colHSetting.Width = 81;
            // 
            // colHOldValue
            // 
            this.colHOldValue.DataPropertyName = "OldValue";
            this.colHOldValue.HeaderText = "Value (Old)";
            this.colHOldValue.MinimumWidth = 6;
            this.colHOldValue.Name = "colHOldValue";
            this.colHOldValue.ReadOnly = true;
            this.colHOldValue.Width = 109;
            // 
            // colHValueNew
            // 
            this.colHValueNew.DataPropertyName = "NewValue";
            this.colHValueNew.HeaderText = "Value (New)";
            this.colHValueNew.MinimumWidth = 6;
            this.colHValueNew.Name = "colHValueNew";
            this.colHValueNew.ReadOnly = true;
            this.colHValueNew.Width = 114;
            // 
            // colHChangeDate
            // 
            this.colHChangeDate.DataPropertyName = "ChangeDate";
            this.colHChangeDate.HeaderText = "Change Date";
            this.colHChangeDate.MinimumWidth = 6;
            this.colHChangeDate.Name = "colHChangeDate";
            this.colHChangeDate.ReadOnly = true;
            this.colHChangeDate.Width = 120;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsRefreshHistory,
            this.tsCopyHistory,
            this.tsFilter});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(839, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(61, 28);
            this.toolStripLabel1.Text = "History";
            // 
            // tsRefreshHistory
            // 
            this.tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHistory.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHistory.Name = "tsRefreshHistory";
            this.tsRefreshHistory.Size = new System.Drawing.Size(29, 28);
            this.tsRefreshHistory.Text = "Refresh";
            this.tsRefreshHistory.Click += new System.EventHandler(this.tsRefreshHistory_Click);
            // 
            // tsCopyHistory
            // 
            this.tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyHistory.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyHistory.Name = "tsCopyHistory";
            this.tsCopyHistory.Size = new System.Drawing.Size(29, 28);
            this.tsCopyHistory.Text = "Copy";
            this.tsCopyHistory.Click += new System.EventHandler(this.tsCopyHistory_Click);
            // 
            // tsFilter
            // 
            this.tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.excludeStateChangesToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 28);
            this.tsFilter.Text = "Filter";
            // 
            // excludeStateChangesToolStripMenuItem
            // 
            this.excludeStateChangesToolStripMenuItem.Checked = true;
            this.excludeStateChangesToolStripMenuItem.CheckOnClick = true;
            this.excludeStateChangesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.excludeStateChangesToolStripMenuItem.Name = "excludeStateChangesToolStripMenuItem";
            this.excludeStateChangesToolStripMenuItem.Size = new System.Drawing.Size(241, 26);
            this.excludeStateChangesToolStripMenuItem.Text = "Exclude State Changes";
            this.excludeStateChangesToolStripMenuItem.Click += new System.EventHandler(this.excludeStateChangesToolStripMenuItem_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "DB";
            this.dataGridViewTextBoxColumn2.HeaderText = "DB";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 56;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "Setting";
            this.dataGridViewTextBoxColumn3.HeaderText = "Setting";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 81;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "OldValue";
            this.dataGridViewTextBoxColumn4.HeaderText = "Value (Old)";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 109;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "NewValue";
            this.dataGridViewTextBoxColumn5.HeaderText = "Value (New)";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 114;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "ChangeDate";
            this.dataGridViewTextBoxColumn6.HeaderText = "Change Date";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 120;
            // 
            // DBOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "DBOptions";
            this.Size = new System.Drawing.Size(839, 484);
            this.Load += new System.EventHandler(this.DBOptions_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistory)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHSetting;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHOldValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHChangeDate;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip2;
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
    }
}
