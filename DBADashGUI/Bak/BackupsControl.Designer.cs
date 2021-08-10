namespace DBADashGUI.Backups
{
    partial class BackupsControl
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
            this.dgvBackups = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CreatedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RecoveryModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastFull = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastLog = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastFG = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastFGDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastPartial = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastPartialDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FullCriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FullWarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiffCriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiffWarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogCriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LogWarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ThresholdsConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvBackups
            // 
            this.dgvBackups.AllowUserToAddRows = false;
            this.dgvBackups.AllowUserToDeleteRows = false;
            this.dgvBackups.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvBackups.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvBackups.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvBackups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBackups.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Database,
            this.CreatedDate,
            this.RecoveryModel,
            this.LastFull,
            this.LastDiff,
            this.LastLog,
            this.LastFG,
            this.LastFGDiff,
            this.LastPartial,
            this.LastPartialDiff,
            this.SnapshotAge,
            this.FullCriticalThreshold,
            this.FullWarningThreshold,
            this.DiffCriticalThreshold,
            this.DiffWarningThreshold,
            this.LogCriticalThreshold,
            this.LogWarningThreshold,
            this.ThresholdsConfiguredLevel,
            this.Configure});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvBackups.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvBackups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvBackups.Location = new System.Drawing.Point(0, 31);
            this.dgvBackups.Name = "dgvBackups";
            this.dgvBackups.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvBackups.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvBackups.RowHeadersVisible = false;
            this.dgvBackups.RowHeadersWidth = 51;
            this.dgvBackups.Size = new System.Drawing.Size(1947, 619);
            this.dgvBackups.TabIndex = 0;
            this.dgvBackups.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvBackups_CellContentClick);
            this.dgvBackups.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvBackups_RowsAdded);
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
            this.toolStrip1.Size = new System.Drawing.Size(1947, 31);
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
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "name";
            this.dataGridViewTextBoxColumn2.HeaderText = "Database";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 98;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "create_date_utc";
            dataGridViewCellStyle4.Format = "yyyy-MM-dd HH:mm";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle4;
            this.dataGridViewTextBoxColumn3.HeaderText = "Created";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Width = 87;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "recovery_model_desc";
            this.dataGridViewTextBoxColumn4.HeaderText = "Recovery Model";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Width = 127;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn5.DataPropertyName = "LastFull";
            this.dataGridViewTextBoxColumn5.HeaderText = "Last Full";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.Width = 64;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn6.DataPropertyName = "LastDiff";
            this.dataGridViewTextBoxColumn6.HeaderText = "Last Diff";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.Width = 64;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn7.DataPropertyName = "LastLog";
            this.dataGridViewTextBoxColumn7.HeaderText = "Last Log";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.Width = 64;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn8.DataPropertyName = "LastFG";
            this.dataGridViewTextBoxColumn8.HeaderText = "Last Filegroup Backup";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn9.DataPropertyName = "LastFGDiff";
            this.dataGridViewTextBoxColumn9.HeaderText = "Last Filegroup Diff Backup";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn10.DataPropertyName = "LastPartial";
            this.dataGridViewTextBoxColumn10.HeaderText = "Last Partial Backup";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn11.DataPropertyName = "LastPartialDiff";
            this.dataGridViewTextBoxColumn11.HeaderText = "Last Partial Diff Backup";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.DataPropertyName = "SnapshotAge";
            this.dataGridViewTextBoxColumn12.HeaderText = "Snapshot Age (mins)";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.Width = 97;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn13.DataPropertyName = "FullBackupCriticalThreshold";
            this.dataGridViewTextBoxColumn13.HeaderText = "Full Critical Threshold";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.Width = 80;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn14.DataPropertyName = "FullBackupWarningThreshold";
            this.dataGridViewTextBoxColumn14.HeaderText = "Full Warning Threshold";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.Width = 80;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn15.DataPropertyName = "DiffBackupCriticalThreshold";
            this.dataGridViewTextBoxColumn15.HeaderText = "Diff Critical Threshold";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.Width = 80;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn16.DataPropertyName = "DiffBackupWarningThreshold";
            this.dataGridViewTextBoxColumn16.HeaderText = "Diff Warning Threshold";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.Width = 80;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn17.DataPropertyName = "LogBackupCriticalThreshold";
            this.dataGridViewTextBoxColumn17.HeaderText = "Log Critical Threshold";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.Width = 80;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dataGridViewTextBoxColumn18.DataPropertyName = "LogBackupWarningThreshold";
            this.dataGridViewTextBoxColumn18.HeaderText = "Log Warning Threshold";
            this.dataGridViewTextBoxColumn18.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            this.dataGridViewTextBoxColumn18.Width = 80;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.DataPropertyName = "ThresholdsConfiguredLevel";
            this.dataGridViewTextBoxColumn19.HeaderText = "Thresholds Configured Level";
            this.dataGridViewTextBoxColumn19.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            this.dataGridViewTextBoxColumn19.Width = 132;
            // 
            // Instance
            // 
            this.Instance.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Instance.DataPropertyName = "Instance";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // Database
            // 
            this.Database.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Database.DataPropertyName = "name";
            this.Database.HeaderText = "Database";
            this.Database.MinimumWidth = 6;
            this.Database.Name = "Database";
            this.Database.ReadOnly = true;
            this.Database.Width = 98;
            // 
            // CreatedDate
            // 
            this.CreatedDate.DataPropertyName = "create_date_utc";
            this.CreatedDate.HeaderText = "Created";
            this.CreatedDate.MinimumWidth = 6;
            this.CreatedDate.Name = "CreatedDate";
            this.CreatedDate.ReadOnly = true;
            this.CreatedDate.Width = 87;
            // 
            // RecoveryModel
            // 
            this.RecoveryModel.DataPropertyName = "recovery_model_desc";
            this.RecoveryModel.HeaderText = "Recovery Model";
            this.RecoveryModel.MinimumWidth = 6;
            this.RecoveryModel.Name = "RecoveryModel";
            this.RecoveryModel.ReadOnly = true;
            this.RecoveryModel.Width = 127;
            // 
            // LastFull
            // 
            this.LastFull.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.LastFull.DataPropertyName = "LastFull";
            this.LastFull.HeaderText = "Last Full";
            this.LastFull.MinimumWidth = 6;
            this.LastFull.Name = "LastFull";
            this.LastFull.ReadOnly = true;
            this.LastFull.Width = 64;
            // 
            // LastDiff
            // 
            this.LastDiff.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.LastDiff.DataPropertyName = "LastDiff";
            this.LastDiff.HeaderText = "Last Diff";
            this.LastDiff.MinimumWidth = 6;
            this.LastDiff.Name = "LastDiff";
            this.LastDiff.ReadOnly = true;
            this.LastDiff.Width = 64;
            // 
            // LastLog
            // 
            this.LastLog.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.LastLog.DataPropertyName = "LastLog";
            this.LastLog.HeaderText = "Last Log";
            this.LastLog.MinimumWidth = 6;
            this.LastLog.Name = "LastLog";
            this.LastLog.ReadOnly = true;
            this.LastLog.Width = 64;
            // 
            // LastFG
            // 
            this.LastFG.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LastFG.DataPropertyName = "LastFG";
            this.LastFG.HeaderText = "Last Filegroup Backup";
            this.LastFG.MinimumWidth = 6;
            this.LastFG.Name = "LastFG";
            this.LastFG.ReadOnly = true;
            this.LastFG.Width = 125;
            // 
            // LastFGDiff
            // 
            this.LastFGDiff.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LastFGDiff.DataPropertyName = "LastFGDiff";
            this.LastFGDiff.HeaderText = "Last Filegroup Diff Backup";
            this.LastFGDiff.MinimumWidth = 6;
            this.LastFGDiff.Name = "LastFGDiff";
            this.LastFGDiff.ReadOnly = true;
            this.LastFGDiff.Width = 125;
            // 
            // LastPartial
            // 
            this.LastPartial.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LastPartial.DataPropertyName = "LastPartial";
            this.LastPartial.HeaderText = "Last Partial Backup";
            this.LastPartial.MinimumWidth = 6;
            this.LastPartial.Name = "LastPartial";
            this.LastPartial.ReadOnly = true;
            this.LastPartial.Width = 125;
            // 
            // LastPartialDiff
            // 
            this.LastPartialDiff.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LastPartialDiff.DataPropertyName = "LastPartialDiff";
            this.LastPartialDiff.HeaderText = "Last Partial Diff Backup";
            this.LastPartialDiff.MinimumWidth = 6;
            this.LastPartialDiff.Name = "LastPartialDiff";
            this.LastPartialDiff.ReadOnly = true;
            this.LastPartialDiff.Width = 125;
            // 
            // SnapshotAge
            // 
            this.SnapshotAge.DataPropertyName = "SnapshotAge";
            this.SnapshotAge.HeaderText = "Snapshot Age (mins)";
            this.SnapshotAge.MinimumWidth = 6;
            this.SnapshotAge.Name = "SnapshotAge";
            this.SnapshotAge.ReadOnly = true;
            this.SnapshotAge.Width = 119;
            // 
            // FullCriticalThreshold
            // 
            this.FullCriticalThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.FullCriticalThreshold.DataPropertyName = "FullBackupCriticalThreshold";
            this.FullCriticalThreshold.HeaderText = "Full Critical Threshold";
            this.FullCriticalThreshold.MinimumWidth = 6;
            this.FullCriticalThreshold.Name = "FullCriticalThreshold";
            this.FullCriticalThreshold.ReadOnly = true;
            this.FullCriticalThreshold.Width = 80;
            // 
            // FullWarningThreshold
            // 
            this.FullWarningThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.FullWarningThreshold.DataPropertyName = "FullBackupWarningThreshold";
            this.FullWarningThreshold.HeaderText = "Full Warning Threshold";
            this.FullWarningThreshold.MinimumWidth = 6;
            this.FullWarningThreshold.Name = "FullWarningThreshold";
            this.FullWarningThreshold.ReadOnly = true;
            this.FullWarningThreshold.Width = 80;
            // 
            // DiffCriticalThreshold
            // 
            this.DiffCriticalThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.DiffCriticalThreshold.DataPropertyName = "DiffBackupCriticalThreshold";
            this.DiffCriticalThreshold.HeaderText = "Diff Critical Threshold";
            this.DiffCriticalThreshold.MinimumWidth = 6;
            this.DiffCriticalThreshold.Name = "DiffCriticalThreshold";
            this.DiffCriticalThreshold.ReadOnly = true;
            this.DiffCriticalThreshold.Width = 80;
            // 
            // DiffWarningThreshold
            // 
            this.DiffWarningThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.DiffWarningThreshold.DataPropertyName = "DiffBackupWarningThreshold";
            this.DiffWarningThreshold.HeaderText = "Diff Warning Threshold";
            this.DiffWarningThreshold.MinimumWidth = 6;
            this.DiffWarningThreshold.Name = "DiffWarningThreshold";
            this.DiffWarningThreshold.ReadOnly = true;
            this.DiffWarningThreshold.Width = 80;
            // 
            // LogCriticalThreshold
            // 
            this.LogCriticalThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LogCriticalThreshold.DataPropertyName = "LogBackupCriticalThreshold";
            this.LogCriticalThreshold.HeaderText = "Log Critical Threshold";
            this.LogCriticalThreshold.MinimumWidth = 6;
            this.LogCriticalThreshold.Name = "LogCriticalThreshold";
            this.LogCriticalThreshold.ReadOnly = true;
            this.LogCriticalThreshold.Width = 80;
            // 
            // LogWarningThreshold
            // 
            this.LogWarningThreshold.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.LogWarningThreshold.DataPropertyName = "LogBackupWarningThreshold";
            this.LogWarningThreshold.HeaderText = "Log Warning Threshold";
            this.LogWarningThreshold.MinimumWidth = 6;
            this.LogWarningThreshold.Name = "LogWarningThreshold";
            this.LogWarningThreshold.ReadOnly = true;
            this.LogWarningThreshold.Width = 80;
            // 
            // ThresholdsConfiguredLevel
            // 
            this.ThresholdsConfiguredLevel.DataPropertyName = "ThresholdsConfiguredLevel";
            this.ThresholdsConfiguredLevel.HeaderText = "Thresholds Configured Level";
            this.ThresholdsConfiguredLevel.MinimumWidth = 6;
            this.ThresholdsConfiguredLevel.Name = "ThresholdsConfiguredLevel";
            this.ThresholdsConfiguredLevel.ReadOnly = true;
            this.ThresholdsConfiguredLevel.Width = 132;
            // 
            // Configure
            // 
            this.Configure.HeaderText = "Configure";
            this.Configure.MinimumWidth = 6;
            this.Configure.Name = "Configure";
            this.Configure.ReadOnly = true;
            this.Configure.Text = "Configure";
            this.Configure.UseColumnTextForLinkValue = true;
            this.Configure.Width = 75;
            // 
            // BackupsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvBackups);
            this.Controls.Add(this.toolStrip1);
            this.Name = "BackupsControl";
            this.Size = new System.Drawing.Size(1947, 650);
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackups)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvBackups;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn CreatedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn RecoveryModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFull;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastDiff;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastLog;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFG;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFGDiff;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastPartial;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastPartialDiff;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullCriticalThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullWarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiffCriticalThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiffWarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogCriticalThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn LogWarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn ThresholdsConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
    }
}
