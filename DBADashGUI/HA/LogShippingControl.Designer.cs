namespace DBADashGUI.LogShipping
{
    partial class LogShippingControl
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
            this.dgvLogShipping = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RestoreDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BackupStartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TimeSinceLast = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LatencyOfLast = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalTimeBehind = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogRestoresDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastFile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ThresholdConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogShipping)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvLogShipping
            // 
            this.dgvLogShipping.AllowUserToAddRows = false;
            this.dgvLogShipping.AllowUserToDeleteRows = false;
            this.dgvLogShipping.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvLogShipping.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvLogShipping.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvLogShipping.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLogShipping.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Database,
            this.Status,
            this.RestoreDate,
            this.BackupStartDate,
            this.TimeSinceLast,
            this.LatencyOfLast,
            this.TotalTimeBehind,
            this.SnapshotAge,
            this.LogRestoresDate,
            this.LastFile,
            this.ThresholdConfiguredLevel,
            this.Configure});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvLogShipping.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvLogShipping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLogShipping.Location = new System.Drawing.Point(0, 31);
            this.dgvLogShipping.Name = "dgvLogShipping";
            this.dgvLogShipping.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvLogShipping.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvLogShipping.RowHeadersVisible = false;
            this.dgvLogShipping.RowHeadersWidth = 51;
            this.dgvLogShipping.RowTemplate.Height = 24;
            this.dgvLogShipping.Size = new System.Drawing.Size(698, 402);
            this.dgvLogShipping.TabIndex = 0;
            this.dgvLogShipping.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvLogShipping_CellContentClick);
            this.dgvLogShipping.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvLogShipping_RowsAdded);
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "Instance";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // Database
            // 
            this.Database.DataPropertyName = "name";
            this.Database.HeaderText = "Database";
            this.Database.MinimumWidth = 6;
            this.Database.Name = "Database";
            this.Database.ReadOnly = true;
            this.Database.Width = 98;
            // 
            // Status
            // 
            this.Status.DataPropertyName = "StatusDescription";
            this.Status.HeaderText = "Status";
            this.Status.MinimumWidth = 6;
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            this.Status.Width = 77;
            // 
            // RestoreDate
            // 
            this.RestoreDate.DataPropertyName = "restore_date";
            this.RestoreDate.HeaderText = "Restore Date";
            this.RestoreDate.MinimumWidth = 6;
            this.RestoreDate.Name = "RestoreDate";
            this.RestoreDate.ReadOnly = true;
            this.RestoreDate.Width = 111;
            // 
            // BackupStartDate
            // 
            this.BackupStartDate.DataPropertyName = "backup_start_date";
            this.BackupStartDate.HeaderText = "Backup Start Date";
            this.BackupStartDate.MinimumWidth = 6;
            this.BackupStartDate.Name = "BackupStartDate";
            this.BackupStartDate.ReadOnly = true;
            this.BackupStartDate.Width = 112;
            // 
            // TimeSinceLast
            // 
            this.TimeSinceLast.DataPropertyName = "TimeSinceLast";
            this.TimeSinceLast.HeaderText = "Time Since Last";
            this.TimeSinceLast.MinimumWidth = 6;
            this.TimeSinceLast.Name = "TimeSinceLast";
            this.TimeSinceLast.ReadOnly = true;
            this.TimeSinceLast.Width = 127;
            // 
            // LatencyOfLast
            // 
            this.LatencyOfLast.DataPropertyName = "LatencyOfLast";
            this.LatencyOfLast.HeaderText = "Latency of Last";
            this.LatencyOfLast.MinimumWidth = 6;
            this.LatencyOfLast.Name = "LatencyOfLast";
            this.LatencyOfLast.ReadOnly = true;
            this.LatencyOfLast.Width = 99;
            // 
            // TotalTimeBehind
            // 
            this.TotalTimeBehind.DataPropertyName = "TotalTimeBehind";
            this.TotalTimeBehind.HeaderText = "Total Time Behind";
            this.TotalTimeBehind.MinimumWidth = 6;
            this.TotalTimeBehind.Name = "TotalTimeBehind";
            this.TotalTimeBehind.ReadOnly = true;
            this.TotalTimeBehind.Width = 139;
            // 
            // SnapshotAge
            // 
            this.SnapshotAge.DataPropertyName = "SnapshotAge";
            this.SnapshotAge.HeaderText = "Snapshot Age";
            this.SnapshotAge.MinimumWidth = 6;
            this.SnapshotAge.Name = "SnapshotAge";
            this.SnapshotAge.ReadOnly = true;
            this.SnapshotAge.Width = 116;
            // 
            // LogRestoresDate
            // 
            this.LogRestoresDate.DataPropertyName = "LogRestoresDate";
            this.LogRestoresDate.HeaderText = "Log Restores Date";
            this.LogRestoresDate.MinimumWidth = 6;
            this.LogRestoresDate.Name = "LogRestoresDate";
            this.LogRestoresDate.ReadOnly = true;
            this.LogRestoresDate.Width = 143;
            // 
            // LastFile
            // 
            this.LastFile.DataPropertyName = "last_file";
            this.LastFile.HeaderText = "Last File";
            this.LastFile.MinimumWidth = 6;
            this.LastFile.Name = "LastFile";
            this.LastFile.ReadOnly = true;
            this.LastFile.Width = 64;
            // 
            // ThresholdConfiguredLevel
            // 
            this.ThresholdConfiguredLevel.DataPropertyName = "ThresholdConfiguredLevel";
            this.ThresholdConfiguredLevel.HeaderText = "Threshold Configured Level";
            this.ThresholdConfiguredLevel.MinimumWidth = 6;
            this.ThresholdConfiguredLevel.Name = "ThresholdConfiguredLevel";
            this.ThresholdConfiguredLevel.ReadOnly = true;
            this.ThresholdConfiguredLevel.Width = 163;
            // 
            // Configure
            // 
            this.Configure.HeaderText = "Configure";
            this.Configure.MinimumWidth = 6;
            this.Configure.Name = "Configure";
            this.Configure.ReadOnly = true;
            this.Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Configure.Text = "Configure";
            this.Configure.UseColumnTextForLinkValue = true;
            this.Configure.Width = 98;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.toolStripFilter,
            this.tsConfigure});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(698, 31);
            this.toolStrip1.TabIndex = 2;
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
            // toolStripFilter
            // 
            this.toolStripFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalToolStripMenuItem,
            this.warningToolStripMenuItem,
            this.undefinedToolStripMenuItem,
            this.OKToolStripMenuItem});
            this.toolStripFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripFilter.Name = "toolStripFilter";
            this.toolStripFilter.Size = new System.Drawing.Size(34, 28);
            this.toolStripFilter.Text = "Filter";
            // 
            // criticalToolStripMenuItem
            // 
            this.criticalToolStripMenuItem.CheckOnClick = true;
            this.criticalToolStripMenuItem.Name = "criticalToolStripMenuItem";
            this.criticalToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.criticalToolStripMenuItem.Text = "Critical";
            this.criticalToolStripMenuItem.Click += new System.EventHandler(this.criticalToolStripMenuItem_Click);
            // 
            // warningToolStripMenuItem
            // 
            this.warningToolStripMenuItem.CheckOnClick = true;
            this.warningToolStripMenuItem.Name = "warningToolStripMenuItem";
            this.warningToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.warningToolStripMenuItem.Text = "Warning";
            this.warningToolStripMenuItem.Click += new System.EventHandler(this.warningToolStripMenuItem_Click);
            // 
            // undefinedToolStripMenuItem
            // 
            this.undefinedToolStripMenuItem.CheckOnClick = true;
            this.undefinedToolStripMenuItem.Name = "undefinedToolStripMenuItem";
            this.undefinedToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.undefinedToolStripMenuItem.Text = "Undefined";
            this.undefinedToolStripMenuItem.Click += new System.EventHandler(this.undefinedToolStripMenuItem_Click);
            // 
            // OKToolStripMenuItem
            // 
            this.OKToolStripMenuItem.CheckOnClick = true;
            this.OKToolStripMenuItem.Name = "OKToolStripMenuItem";
            this.OKToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.OKToolStripMenuItem.Text = "OK";
            this.OKToolStripMenuItem.Click += new System.EventHandler(this.OKToolStripMenuItem_Click);
            // 
            // tsConfigure
            // 
            this.tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureInstanceThresholdsToolStripMenuItem,
            this.configureRootThresholdsToolStripMenuItem});
            this.tsConfigure.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsConfigure.Name = "tsConfigure";
            this.tsConfigure.Size = new System.Drawing.Size(34, 28);
            this.tsConfigure.Text = "Configure";
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            this.configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            this.configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            this.configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            this.configureInstanceThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureInstanceThresholdsToolStripMenuItem_Click);
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            this.configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            this.configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            this.configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            this.configureRootThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureRootThresholdsToolStripMenuItem_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 28);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // LogShippingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvLogShipping);
            this.Controls.Add(this.toolStrip1);
            this.Name = "LogShippingControl";
            this.Size = new System.Drawing.Size(698, 433);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogShipping)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvLogShipping;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn RestoreDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn BackupStartDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLast;
        private System.Windows.Forms.DataGridViewTextBoxColumn LatencyOfLast;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalTimeBehind;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogRestoresDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFile;
        private System.Windows.Forms.DataGridViewTextBoxColumn ThresholdConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
    }
}
