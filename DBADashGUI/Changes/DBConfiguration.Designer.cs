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
            this.dgvConfig = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.configuredOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgvConfigHistory = new System.Windows.Forms.DataGridView();
            this.colHInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHNewValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHValueForSecondary = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHValueForSecondaryNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfig)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfigHistory)).BeginInit();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvConfig
            // 
            this.dgvConfig.AllowUserToAddRows = false;
            this.dgvConfig.AllowUserToDeleteRows = false;
            this.dgvConfig.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvConfig.BackgroundColor = System.Drawing.Color.White;
            this.dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvConfig.Location = new System.Drawing.Point(0, 31);
            this.dgvConfig.Name = "dgvConfig";
            this.dgvConfig.ReadOnly = true;
            this.dgvConfig.RowHeadersVisible = false;
            this.dgvConfig.RowHeadersWidth = 51;
            this.dgvConfig.RowTemplate.Height = 24;
            this.dgvConfig.Size = new System.Drawing.Size(994, 223);
            this.dgvConfig.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsFilter,
            this.toolStripLabel1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(994, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 28);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 28);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsFilter
            // 
            this.tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configuredOnlyToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 28);
            this.tsFilter.Text = "Filter";
            // 
            // configuredOnlyToolStripMenuItem
            // 
            this.configuredOnlyToolStripMenuItem.Checked = true;
            this.configuredOnlyToolStripMenuItem.CheckOnClick = true;
            this.configuredOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.configuredOnlyToolStripMenuItem.Name = "configuredOnlyToolStripMenuItem";
            this.configuredOnlyToolStripMenuItem.Size = new System.Drawing.Size(200, 26);
            this.configuredOnlyToolStripMenuItem.Text = "Configured Only";
            this.configuredOnlyToolStripMenuItem.Click += new System.EventHandler(this.configuredOnlyToolStripMenuItem_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(185, 28);
            this.toolStripLabel1.Text = "DB Scoped Configuration";
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
            this.splitContainer1.Panel1.Controls.Add(this.dgvConfig);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvConfigHistory);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer1.Size = new System.Drawing.Size(994, 508);
            this.splitContainer1.SplitterDistance = 254;
            this.splitContainer1.TabIndex = 2;
            // 
            // dgvConfigHistory
            // 
            this.dgvConfigHistory.AllowUserToAddRows = false;
            this.dgvConfigHistory.AllowUserToDeleteRows = false;
            this.dgvConfigHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvConfigHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvConfigHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvConfigHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConfigHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHInstance,
            this.colHDB,
            this.colHName,
            this.colHValue,
            this.colHNewValue,
            this.colHValueForSecondary,
            this.colHValueForSecondaryNew,
            this.colHValidTo});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvConfigHistory.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvConfigHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvConfigHistory.Location = new System.Drawing.Point(0, 31);
            this.dgvConfigHistory.Name = "dgvConfigHistory";
            this.dgvConfigHistory.ReadOnly = true;
            this.dgvConfigHistory.RowHeadersVisible = false;
            this.dgvConfigHistory.RowHeadersWidth = 51;
            this.dgvConfigHistory.RowTemplate.Height = 24;
            this.dgvConfigHistory.Size = new System.Drawing.Size(994, 219);
            this.dgvConfigHistory.TabIndex = 1;
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
            // colHName
            // 
            this.colHName.DataPropertyName = "name";
            this.colHName.HeaderText = "Name";
            this.colHName.MinimumWidth = 6;
            this.colHName.Name = "colHName";
            this.colHName.ReadOnly = true;
            this.colHName.Width = 74;
            // 
            // colHValue
            // 
            this.colHValue.DataPropertyName = "value";
            this.colHValue.HeaderText = "Value (Old)";
            this.colHValue.MinimumWidth = 6;
            this.colHValue.Name = "colHValue";
            this.colHValue.ReadOnly = true;
            // 
            // colHNewValue
            // 
            this.colHNewValue.DataPropertyName = "new_value";
            this.colHNewValue.HeaderText = "Value (New)";
            this.colHNewValue.MinimumWidth = 6;
            this.colHNewValue.Name = "colHNewValue";
            this.colHNewValue.ReadOnly = true;
            this.colHNewValue.Width = 105;
            // 
            // colHValueForSecondary
            // 
            this.colHValueForSecondary.DataPropertyName = "value_for_secondary";
            this.colHValueForSecondary.HeaderText = "Value for Secondary (Old)";
            this.colHValueForSecondary.MinimumWidth = 6;
            this.colHValueForSecondary.Name = "colHValueForSecondary";
            this.colHValueForSecondary.ReadOnly = true;
            this.colHValueForSecondary.Width = 155;
            // 
            // colHValueForSecondaryNew
            // 
            this.colHValueForSecondaryNew.DataPropertyName = "new_value_for_secondary";
            this.colHValueForSecondaryNew.HeaderText = "Value for secondary (New)";
            this.colHValueForSecondaryNew.MinimumWidth = 6;
            this.colHValueForSecondaryNew.Name = "colHValueForSecondaryNew";
            this.colHValueForSecondaryNew.ReadOnly = true;
            this.colHValueForSecondaryNew.Width = 154;
            // 
            // colHValidTo
            // 
            this.colHValidTo.DataPropertyName = "ValidTo";
            this.colHValidTo.HeaderText = "Change Date";
            this.colHValidTo.MinimumWidth = 6;
            this.colHValidTo.Name = "colHValidTo";
            this.colHValidTo.ReadOnly = true;
            this.colHValidTo.Width = 110;
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefreshHistory,
            this.tsCopyHistory,
            this.toolStripLabel2});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(994, 31);
            this.toolStrip2.TabIndex = 0;
            this.toolStrip2.Text = "toolStrip2";
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
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(61, 28);
            this.toolStripLabel2.Text = "History";
            // 
            // DBConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "DBConfiguration";
            this.Size = new System.Drawing.Size(994, 508);
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfig)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvConfigHistory)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvConfig;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem configuredOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvConfigHistory;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHNewValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueForSecondary;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValueForSecondaryNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHValidTo;
    }
}
