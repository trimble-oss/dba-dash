using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class DBConfiguration
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
            dgvConfig = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            configuredOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvConfigHistory = new DBADashDataGridView();
            colHInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHNewValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHValueForSecondary = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHValueForSecondaryNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            tsClearFilterHistory = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tsColumnsHistory = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvConfig).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConfigHistory).BeginInit();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgvConfig
            // 
            dgvConfig.AllowUserToAddRows = false;
            dgvConfig.AllowUserToDeleteRows = false;
            dgvConfig.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvConfig.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvConfig.DefaultCellStyle = dataGridViewCellStyle2;
            dgvConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvConfig.EnableHeadersVisualStyles = false;
            dgvConfig.Location = new System.Drawing.Point(0, 27);
            dgvConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvConfig.Name = "dgvConfig";
            dgvConfig.ReadOnly = true;
            dgvConfig.ResultSetID = 0;
            dgvConfig.ResultSetName = null;
            dgvConfig.RowHeadersVisible = false;
            dgvConfig.RowHeadersWidth = 51;
            dgvConfig.Size = new System.Drawing.Size(994, 290);
            dgvConfig.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsFilter, toolStripLabel1, tsExcel, tsCols });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(994, 27);
            toolStrip1.TabIndex = 1;
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
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configuredOnlyToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // configuredOnlyToolStripMenuItem
            // 
            configuredOnlyToolStripMenuItem.Checked = true;
            configuredOnlyToolStripMenuItem.CheckOnClick = true;
            configuredOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            configuredOnlyToolStripMenuItem.Name = "configuredOnlyToolStripMenuItem";
            configuredOnlyToolStripMenuItem.Size = new System.Drawing.Size(200, 26);
            configuredOnlyToolStripMenuItem.Text = "Configured Only";
            configuredOnlyToolStripMenuItem.Click += ConfiguredOnlyToolStripMenuItem_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(185, 24);
            toolStripLabel1.Text = "DB Scoped Configuration";
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
            splitContainer1.Panel1.Controls.Add(dgvConfig);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvConfigHistory);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(994, 635);
            splitContainer1.SplitterDistance = 317;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // dgvConfigHistory
            // 
            dgvConfigHistory.AllowUserToAddRows = false;
            dgvConfigHistory.AllowUserToDeleteRows = false;
            dgvConfigHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvConfigHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvConfigHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConfigHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colHInstance, colHDB, colHName, colHValue, colHNewValue, colHValueForSecondary, colHValueForSecondaryNew, colHValidTo });
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvConfigHistory.DefaultCellStyle = dataGridViewCellStyle4;
            dgvConfigHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvConfigHistory.EnableHeadersVisualStyles = false;
            dgvConfigHistory.Location = new System.Drawing.Point(0, 27);
            dgvConfigHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvConfigHistory.Name = "dgvConfigHistory";
            dgvConfigHistory.ReadOnly = true;
            dgvConfigHistory.ResultSetID = 0;
            dgvConfigHistory.ResultSetName = null;
            dgvConfigHistory.RowHeadersVisible = false;
            dgvConfigHistory.RowHeadersWidth = 51;
            dgvConfigHistory.Size = new System.Drawing.Size(994, 286);
            dgvConfigHistory.TabIndex = 1;
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
            // colHName
            // 
            colHName.DataPropertyName = "name";
            colHName.HeaderText = "Name";
            colHName.MinimumWidth = 6;
            colHName.Name = "colHName";
            colHName.ReadOnly = true;
            colHName.Width = 74;
            // 
            // colHValue
            // 
            colHValue.DataPropertyName = "value";
            colHValue.HeaderText = "Value (Old)";
            colHValue.MinimumWidth = 6;
            colHValue.Name = "colHValue";
            colHValue.ReadOnly = true;
            colHValue.Width = 125;
            // 
            // colHNewValue
            // 
            colHNewValue.DataPropertyName = "new_value";
            colHNewValue.HeaderText = "Value (New)";
            colHNewValue.MinimumWidth = 6;
            colHNewValue.Name = "colHNewValue";
            colHNewValue.ReadOnly = true;
            colHNewValue.Width = 105;
            // 
            // colHValueForSecondary
            // 
            colHValueForSecondary.DataPropertyName = "value_for_secondary";
            colHValueForSecondary.HeaderText = "Value for Secondary (Old)";
            colHValueForSecondary.MinimumWidth = 6;
            colHValueForSecondary.Name = "colHValueForSecondary";
            colHValueForSecondary.ReadOnly = true;
            colHValueForSecondary.Width = 155;
            // 
            // colHValueForSecondaryNew
            // 
            colHValueForSecondaryNew.DataPropertyName = "new_value_for_secondary";
            colHValueForSecondaryNew.HeaderText = "Value for secondary (New)";
            colHValueForSecondaryNew.MinimumWidth = 6;
            colHValueForSecondaryNew.Name = "colHValueForSecondaryNew";
            colHValueForSecondaryNew.ReadOnly = true;
            colHValueForSecondaryNew.Width = 154;
            // 
            // colHValidTo
            // 
            colHValidTo.DataPropertyName = "ValidTo";
            colHValidTo.HeaderText = "Change Date";
            colHValidTo.MinimumWidth = 6;
            colHValidTo.Name = "colHValidTo";
            colHValidTo.ReadOnly = true;
            colHValidTo.Width = 110;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefreshHistory, tsCopyHistory, toolStripLabel2, tsExcelHistory, tsColumnsHistory, tsClearFilterHistory });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(994, 27);
            toolStrip2.TabIndex = 0;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
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
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(61, 24);
            toolStripLabel2.Text = "History";
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // tsClearFilterHistory
            // 
            tsClearFilterHistory.Image = Properties.Resources.Eraser_16x;
            tsClearFilterHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterHistory.Name = "tsClearFilterHistory";
            tsClearFilterHistory.Size = new System.Drawing.Size(104, 24);
            tsClearFilterHistory.Text = "Clear Filter";
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
            dataGridViewTextBoxColumn3.DataPropertyName = "name";
            dataGridViewTextBoxColumn3.HeaderText = "Name";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 74;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "value";
            dataGridViewTextBoxColumn4.HeaderText = "Value (Old)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "new_value";
            dataGridViewTextBoxColumn5.HeaderText = "Value (New)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 105;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "value_for_secondary";
            dataGridViewTextBoxColumn6.HeaderText = "Value for Secondary (Old)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 155;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "new_value_for_secondary";
            dataGridViewTextBoxColumn7.HeaderText = "Value for secondary (New)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 154;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "ValidTo";
            dataGridViewTextBoxColumn8.HeaderText = "Change Date";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 110;
            // 
            // tsColumnsHistory
            // 
            tsColumnsHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsColumnsHistory.Image = Properties.Resources.Column_16x;
            tsColumnsHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsColumnsHistory.Name = "tsColumnsHistory";
            tsColumnsHistory.Size = new System.Drawing.Size(29, 24);
            tsColumnsHistory.Text = "Columns";
            tsColumnsHistory.Click += TsColumnsHistory_Click;
            // 
            // DBConfiguration
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DBConfiguration";
            Size = new System.Drawing.Size(994, 635);
            ((System.ComponentModel.ISupportInitialize)dgvConfig).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConfigHistory).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgvConfig;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem configuredOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvConfigHistory;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHNewValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueForSecondary;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueForSecondaryNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValidTo;
        private System.Windows.Forms.ToolStripButton tsClearFilterHistory;
        private System.Windows.Forms.ToolStripButton tsColumnsHistory;
    }
}
