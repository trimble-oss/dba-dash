using DBADashGUI.CustomReports;

namespace DBADashGUI.LastGoodCheckDB
{
    partial class LastGoodCheckDBControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LastGoodCheckDBControl));
            dgvLastGoodCheckDB = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LastGoodCheckDbTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DaysSinceLastGoodCheckDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCreateDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ExcludedFromCheck = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvLastGoodCheckDB).BeginInit();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvLastGoodCheckDB
            // 
            dgvLastGoodCheckDB.AllowUserToAddRows = false;
            dgvLastGoodCheckDB.AllowUserToDeleteRows = false;
            dgvLastGoodCheckDB.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvLastGoodCheckDB.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvLastGoodCheckDB.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLastGoodCheckDB.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, Database, LastGoodCheckDbTime, DaysSinceLastGoodCheckDB, colCreateDate, ExcludedFromCheck, Configure });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvLastGoodCheckDB.DefaultCellStyle = dataGridViewCellStyle2;
            dgvLastGoodCheckDB.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvLastGoodCheckDB.EnableHeadersVisualStyles = false;
            dgvLastGoodCheckDB.Location = new System.Drawing.Point(0, 27);
            dgvLastGoodCheckDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvLastGoodCheckDB.Name = "dgvLastGoodCheckDB";
            dgvLastGoodCheckDB.ReadOnly = true;
            dgvLastGoodCheckDB.ResultSetID = 0;
            dgvLastGoodCheckDB.ResultSetName = null;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvLastGoodCheckDB.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvLastGoodCheckDB.RowHeadersVisible = false;
            dgvLastGoodCheckDB.RowHeadersWidth = 51;
            dgvLastGoodCheckDB.Size = new System.Drawing.Size(1250, 300);
            dgvLastGoodCheckDB.TabIndex = 0;
            dgvLastGoodCheckDB.CellContentClick += DgvLastGoodCheckDB_CellContentClick;
            dgvLastGoodCheckDB.RowsAdded += DgvLastGoodCheckDB_RowsAdded;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 125;
            // 
            // Database
            // 
            Database.DataPropertyName = "Name";
            Database.HeaderText = "Database";
            Database.MinimumWidth = 6;
            Database.Name = "Database";
            Database.ReadOnly = true;
            Database.Width = 125;
            // 
            // LastGoodCheckDbTime
            // 
            LastGoodCheckDbTime.DataPropertyName = "LastGoodCheckDbTimeUTC";
            LastGoodCheckDbTime.HeaderText = "Last Good Check DB";
            LastGoodCheckDbTime.MinimumWidth = 6;
            LastGoodCheckDbTime.Name = "LastGoodCheckDbTime";
            LastGoodCheckDbTime.ReadOnly = true;
            LastGoodCheckDbTime.Width = 125;
            // 
            // DaysSinceLastGoodCheckDB
            // 
            DaysSinceLastGoodCheckDB.DataPropertyName = "DaysSinceLastGoodCheckDB";
            DaysSinceLastGoodCheckDB.HeaderText = "Days Since Last Good CheckDB";
            DaysSinceLastGoodCheckDB.MinimumWidth = 6;
            DaysSinceLastGoodCheckDB.Name = "DaysSinceLastGoodCheckDB";
            DaysSinceLastGoodCheckDB.ReadOnly = true;
            DaysSinceLastGoodCheckDB.Width = 125;
            // 
            // colCreateDate
            // 
            colCreateDate.DataPropertyName = "create_date_utc";
            colCreateDate.HeaderText = "Create Date";
            colCreateDate.MinimumWidth = 6;
            colCreateDate.Name = "colCreateDate";
            colCreateDate.ReadOnly = true;
            colCreateDate.Width = 125;
            // 
            // ExcludedFromCheck
            // 
            ExcludedFromCheck.DataPropertyName = "LastGoodCheckDBExcludedReason";
            ExcludedFromCheck.HeaderText = "Excluded Reason";
            ExcludedFromCheck.MinimumWidth = 6;
            ExcludedFromCheck.Name = "ExcludedFromCheck";
            ExcludedFromCheck.ReadOnly = true;
            ExcludedFromCheck.Width = 125;
            // 
            // Configure
            // 
            Configure.HeaderText = "Configure";
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Text = "Configure";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 125;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsClearFilter, tsConfigure, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1250, 27);
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
            statusFilterToolStrip1.UserChangedStatusFilter += Status_Selected;
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureInstanceThresholdsToolStripMenuItem, configureRootThresholdsToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            configureInstanceThresholdsToolStripMenuItem.Click += ConfigureInstanceThresholdsToolStripMenuItem_Click;
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            configureRootThresholdsToolStripMenuItem.Click += ConfigureRootThresholdsToolStripMenuItem_Click;
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
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 327);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1250, 22);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Filter_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // LastGoodCheckDBControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvLastGoodCheckDB);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "LastGoodCheckDBControl";
            Size = new System.Drawing.Size(1250, 349);
            Load += LastGoodCheckDB_Load;
            ((System.ComponentModel.ISupportInitialize)dgvLastGoodCheckDB).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvLastGoodCheckDB;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastGoodCheckDbTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn DaysSinceLastGoodCheckDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCreateDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ExcludedFromCheck;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
