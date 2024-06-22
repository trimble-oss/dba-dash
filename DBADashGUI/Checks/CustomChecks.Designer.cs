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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomChecks));
            dgvCustom = new System.Windows.Forms.DataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTest = new System.Windows.Forms.DataGridViewLinkColumn();
            colContext = new System.Windows.Forms.DataGridViewLinkColumn();
            colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSSDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            History = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClear = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dgvCustom).BeginInit();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvCustom
            // 
            dgvCustom.AllowUserToAddRows = false;
            dgvCustom.AllowUserToDeleteRows = false;
            dgvCustom.BackgroundColor = System.Drawing.Color.White;
            dgvCustom.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCustom.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colTest, colContext, colStatus, colInfo, colSSDate, History });
            dgvCustom.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvCustom.Location = new System.Drawing.Point(0, 27);
            dgvCustom.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvCustom.Name = "dgvCustom";
            dgvCustom.ReadOnly = true;
            dgvCustom.RowHeadersVisible = false;
            dgvCustom.RowHeadersWidth = 51;
            dgvCustom.RowTemplate.Height = 24;
            dgvCustom.Size = new System.Drawing.Size(801, 505);
            dgvCustom.TabIndex = 0;
            dgvCustom.CellContentClick += DgvCustom_CellContentClick;
            dgvCustom.RowsAdded += DgvCustom_RowsAdded;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "InstanceDisplayName";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Width = 90;
            // 
            // colTest
            // 
            colTest.DataPropertyName = "Test";
            colTest.HeaderText = "Test";
            colTest.MinimumWidth = 6;
            colTest.Name = "colTest";
            colTest.ReadOnly = true;
            colTest.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colTest.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colTest.Width = 65;
            // 
            // colContext
            // 
            colContext.DataPropertyName = "Context";
            colContext.HeaderText = "Context";
            colContext.MinimumWidth = 6;
            colContext.Name = "colContext";
            colContext.ReadOnly = true;
            colContext.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colContext.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colContext.Width = 84;
            // 
            // colStatus
            // 
            colStatus.DataPropertyName = "Status";
            colStatus.HeaderText = "Status";
            colStatus.MinimumWidth = 6;
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            colStatus.Width = 77;
            // 
            // colInfo
            // 
            colInfo.DataPropertyName = "Info";
            colInfo.HeaderText = "Info";
            colInfo.MinimumWidth = 6;
            colInfo.Name = "colInfo";
            colInfo.ReadOnly = true;
            colInfo.Width = 60;
            // 
            // colSSDate
            // 
            colSSDate.DataPropertyName = "SnapshotDate";
            colSSDate.HeaderText = "Snapshot Date";
            colSSDate.MinimumWidth = 6;
            colSSDate.Name = "colSSDate";
            colSSDate.ReadOnly = true;
            colSSDate.Width = 120;
            // 
            // History
            // 
            History.HeaderText = "History";
            History.MinimumWidth = 6;
            History.Name = "History";
            History.ReadOnly = true;
            History.Text = "History";
            History.UseColumnTextForLinkValue = true;
            History.Width = 58;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsBack, tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsFilter, tsClear, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(801, 27);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Visible = false;
            tsBack.Click += TsBack_Click;
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
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.Acknowledged = false;
            statusFilterToolStrip1.AcknowledgedVisible = false;
            statusFilterToolStrip1.Critical = true;
            statusFilterToolStrip1.CriticalVisible = true;
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.NA = true;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.NAVisible = true;
            statusFilterToolStrip1.OK = true;
            statusFilterToolStrip1.OKVisible = true;
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.Warning = true;
            statusFilterToolStrip1.WarningVisible = true;
            statusFilterToolStrip1.UserChangedStatusFilter += StatusToolStripMenuItem_Click;
            // 
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { testToolStripMenuItem, contextToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // testToolStripMenuItem
            // 
            testToolStripMenuItem.Name = "testToolStripMenuItem";
            testToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            testToolStripMenuItem.Text = "Test";
            // 
            // contextToolStripMenuItem
            // 
            contextToolStripMenuItem.Name = "contextToolStripMenuItem";
            contextToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            contextToolStripMenuItem.Text = "Context";
            // 
            // tsClear
            // 
            tsClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClear.Image = Properties.Resources.Eraser_16x;
            tsClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClear.Name = "tsClear";
            tsClear.Size = new System.Drawing.Size(29, 24);
            tsClear.Text = "Clear Filters";
            tsClear.Click += TsClear_Click;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 24);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Visible = false;
            tsTrigger.Click += TsTrigger_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "Test";
            dataGridViewTextBoxColumn2.HeaderText = "Test";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 65;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "Context";
            dataGridViewTextBoxColumn3.HeaderText = "Context";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 84;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "Status";
            dataGridViewTextBoxColumn4.HeaderText = "Status";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 77;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "Info";
            dataGridViewTextBoxColumn5.HeaderText = "Info";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 60;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "SnapshotDate";
            dataGridViewTextBoxColumn6.HeaderText = "Snapshot Date";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 120;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 532);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(801, 22);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // CustomChecks
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvCustom);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CustomChecks";
            Size = new System.Drawing.Size(801, 554);
            Load += CustomChecks_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCustom).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClear;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewLinkColumn colTest;
        private System.Windows.Forms.DataGridViewLinkColumn colContext;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInfo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSSDate;
        private System.Windows.Forms.DataGridViewLinkColumn History;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
