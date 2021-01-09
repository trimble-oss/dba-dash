namespace DBADashGUI.Checks
{
    partial class CustomChecks
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
            this.dgvCustom = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsClear = new System.Windows.Forms.ToolStripButton();
            this.colTest = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colContext = new System.Windows.Forms.DataGridViewLinkColumn();
            this.History = new System.Windows.Forms.DataGridViewLinkColumn();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSSDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCustom)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvCustom
            // 
            this.dgvCustom.AllowUserToAddRows = false;
            this.dgvCustom.AllowUserToDeleteRows = false;
            this.dgvCustom.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvCustom.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCustom.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colTest,
            this.colContext,
            this.colStatus,
            this.colInfo,
            this.colSSDate,
            this.History});
            this.dgvCustom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCustom.Location = new System.Drawing.Point(0, 27);
            this.dgvCustom.Name = "dgvCustom";
            this.dgvCustom.ReadOnly = true;
            this.dgvCustom.RowHeadersVisible = false;
            this.dgvCustom.RowHeadersWidth = 51;
            this.dgvCustom.RowTemplate.Height = 24;
            this.dgvCustom.Size = new System.Drawing.Size(801, 416);
            this.dgvCustom.TabIndex = 0;
            this.dgvCustom.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCustom_CellContentClick);
            this.dgvCustom.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvCustom_RowsAdded);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsBack,
            this.tsRefresh,
            this.tsCopy,
            this.tsFilter,
            this.tsClear});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(801, 27);
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
            // tsFilter
            // 
            this.tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalToolStripMenuItem,
            this.warningToolStripMenuItem,
            this.undefinedToolStripMenuItem,
            this.OKToolStripMenuItem,
            this.toolStripMenuItem1,
            this.testToolStripMenuItem,
            this.contextToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 24);
            this.tsFilter.Text = "Filter";
            // 
            // criticalToolStripMenuItem
            // 
            this.criticalToolStripMenuItem.Checked = true;
            this.criticalToolStripMenuItem.CheckOnClick = true;
            this.criticalToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.criticalToolStripMenuItem.Name = "criticalToolStripMenuItem";
            this.criticalToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.criticalToolStripMenuItem.Text = "Critical";
            this.criticalToolStripMenuItem.Click += new System.EventHandler(this.statusToolStripMenuItem_Click);
            // 
            // warningToolStripMenuItem
            // 
            this.warningToolStripMenuItem.Checked = true;
            this.warningToolStripMenuItem.CheckOnClick = true;
            this.warningToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.warningToolStripMenuItem.Name = "warningToolStripMenuItem";
            this.warningToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.warningToolStripMenuItem.Text = "Warning";
            this.warningToolStripMenuItem.Click += new System.EventHandler(this.statusToolStripMenuItem_Click);
            // 
            // undefinedToolStripMenuItem
            // 
            this.undefinedToolStripMenuItem.CheckOnClick = true;
            this.undefinedToolStripMenuItem.Name = "undefinedToolStripMenuItem";
            this.undefinedToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.undefinedToolStripMenuItem.Text = "Undefined";
            this.undefinedToolStripMenuItem.Click += new System.EventHandler(this.statusToolStripMenuItem_Click);
            // 
            // OKToolStripMenuItem
            // 
            this.OKToolStripMenuItem.CheckOnClick = true;
            this.OKToolStripMenuItem.Name = "OKToolStripMenuItem";
            this.OKToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.OKToolStripMenuItem.Text = "OK";
            this.OKToolStripMenuItem.Click += new System.EventHandler(this.statusToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(158, 6);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.testToolStripMenuItem.Text = "Test";
            // 
            // contextToolStripMenuItem
            // 
            this.contextToolStripMenuItem.Name = "contextToolStripMenuItem";
            this.contextToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.contextToolStripMenuItem.Text = "Context";
            // 
            // tsClear
            // 
            this.tsClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsClear.Image = global::DBADashGUI.Properties.Resources.Eraser_16x;
            this.tsClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsClear.Name = "tsClear";
            this.tsClear.Size = new System.Drawing.Size(29, 24);
            this.tsClear.Text = "Clear Filters";
            this.tsClear.Click += new System.EventHandler(this.tsClear_Click);
            // 
            // colTest
            // 
            this.colTest.DataPropertyName = "Test";
            this.colTest.HeaderText = "Test";
            this.colTest.MinimumWidth = 6;
            this.colTest.Name = "colTest";
            this.colTest.ReadOnly = true;
            this.colTest.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colTest.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colTest.Width = 65;
            // 
            // colContext
            // 
            this.colContext.DataPropertyName = "Context";
            this.colContext.HeaderText = "Context";
            this.colContext.MinimumWidth = 6;
            this.colContext.Name = "colContext";
            this.colContext.ReadOnly = true;
            this.colContext.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colContext.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colContext.Width = 84;
            // 
            // History
            // 
            this.History.HeaderText = "History";
            this.History.MinimumWidth = 6;
            this.History.Name = "History";
            this.History.ReadOnly = true;
            this.History.Text = "History";
            this.History.UseColumnTextForLinkValue = true;
            this.History.Width = 58;
            // 
            // tsBack
            // 
            this.tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBack.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBack.Name = "tsBack";
            this.tsBack.Size = new System.Drawing.Size(29, 24);
            this.tsBack.Text = "Back";
            this.tsBack.Visible = false;
            this.tsBack.Click += new System.EventHandler(this.tsBack_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Test";
            this.dataGridViewTextBoxColumn2.HeaderText = "Test";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 65;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "Context";
            this.dataGridViewTextBoxColumn3.HeaderText = "Context";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 84;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "Status";
            this.dataGridViewTextBoxColumn4.HeaderText = "Status";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 77;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "ConnectionID";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Width = 90;
            // 
            // colStatus
            // 
            this.colStatus.DataPropertyName = "Status";
            this.colStatus.HeaderText = "Status";
            this.colStatus.MinimumWidth = 6;
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 77;
            // 
            // colInfo
            // 
            this.colInfo.DataPropertyName = "Info";
            this.colInfo.HeaderText = "Info";
            this.colInfo.MinimumWidth = 6;
            this.colInfo.Name = "colInfo";
            this.colInfo.ReadOnly = true;
            this.colInfo.Width = 60;
            // 
            // colSSDate
            // 
            this.colSSDate.DataPropertyName = "SnapshotDate";
            this.colSSDate.HeaderText = "Snapshot Date";
            this.colSSDate.MinimumWidth = 6;
            this.colSSDate.Name = "colSSDate";
            this.colSSDate.ReadOnly = true;
            this.colSSDate.Width = 120;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "Info";
            this.dataGridViewTextBoxColumn5.HeaderText = "Info";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 60;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "SnapshotDate";
            this.dataGridViewTextBoxColumn6.HeaderText = "Snapshot Date";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 120;
            // 
            // CustomChecks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvCustom);
            this.Controls.Add(this.toolStrip1);
            this.Name = "CustomChecks";
            this.Size = new System.Drawing.Size(801, 443);
            this.Load += new System.EventHandler(this.CustomChecks_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCustom)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvCustom;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClear;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewLinkColumn colTest;
        private System.Windows.Forms.DataGridViewLinkColumn colContext;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInfo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSSDate;
        private System.Windows.Forms.DataGridViewLinkColumn History;
        private System.Windows.Forms.ToolStripButton tsBack;
    }
}
