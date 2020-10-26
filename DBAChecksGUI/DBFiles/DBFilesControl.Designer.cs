namespace DBAChecksGUI.DBFiles
{
    partial class DBFilesControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBFilesControl));
            this.dgvFiles = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_FileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_PhysicalName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_FileSizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_FileUsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_FileFreeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileLevel_FilePctFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FilegroupUsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FilegroupFreeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FilegroupPctFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NumberOfFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ExcludedReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DBReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Standby = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.State = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileSnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.History = new System.Windows.Forms.DataGridViewLinkColumn();
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
            this.configureDatabaseThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsLevel = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsFilegroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tsFile = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvFiles
            // 
            this.dgvFiles.AllowUserToAddRows = false;
            this.dgvFiles.AllowUserToDeleteRows = false;
            this.dgvFiles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvFiles.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvFiles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Database,
            this.FileLevel_FileName,
            this.FileLevel_PhysicalName,
            this.FileLevel_FileSizeMB,
            this.FileLevel_FileUsedMB,
            this.FileLevel_FileFreeMB,
            this.FileLevel_FilePctFree,
            this.FileGroup,
            this.SizeMB,
            this.FilegroupUsedMB,
            this.FilegroupFreeMB,
            this.FilegroupPctFree,
            this.NumberOfFiles,
            this.ExcludedReason,
            this.ReadOnly,
            this.DBReadOnly,
            this.Standby,
            this.State,
            this.ConfiguredLevel,
            this.FileSnapshotAge,
            this.History,
            this.Configure});
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvFiles.DefaultCellStyle = dataGridViewCellStyle10;
            this.dgvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvFiles.Location = new System.Drawing.Point(0, 31);
            this.dgvFiles.Name = "dgvFiles";
            this.dgvFiles.ReadOnly = true;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvFiles.RowHeadersDefaultCellStyle = dataGridViewCellStyle11;
            this.dgvFiles.RowHeadersVisible = false;
            this.dgvFiles.RowHeadersWidth = 51;
            this.dgvFiles.RowTemplate.Height = 24;
            this.dgvFiles.Size = new System.Drawing.Size(1616, 255);
            this.dgvFiles.TabIndex = 0;
            this.dgvFiles.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvFiles_CellContentClick);
            this.dgvFiles.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvFiles_RowsAdded);
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
            // FileLevel_FileName
            // 
            this.FileLevel_FileName.DataPropertyName = "file_name";
            this.FileLevel_FileName.HeaderText = "File Name";
            this.FileLevel_FileName.MinimumWidth = 6;
            this.FileLevel_FileName.Name = "FileLevel_FileName";
            this.FileLevel_FileName.ReadOnly = true;
            this.FileLevel_FileName.Visible = false;
            this.FileLevel_FileName.Width = 125;
            // 
            // FileLevel_PhysicalName
            // 
            this.FileLevel_PhysicalName.DataPropertyName = "physical_name";
            this.FileLevel_PhysicalName.HeaderText = "Physical Name";
            this.FileLevel_PhysicalName.MinimumWidth = 6;
            this.FileLevel_PhysicalName.Name = "FileLevel_PhysicalName";
            this.FileLevel_PhysicalName.ReadOnly = true;
            this.FileLevel_PhysicalName.Visible = false;
            this.FileLevel_PhysicalName.Width = 130;
            // 
            // FileLevel_FileSizeMB
            // 
            this.FileLevel_FileSizeMB.DataPropertyName = "FileSizeMB";
            dataGridViewCellStyle2.Format = "N1";
            this.FileLevel_FileSizeMB.DefaultCellStyle = dataGridViewCellStyle2;
            this.FileLevel_FileSizeMB.HeaderText = "File Size (MB)";
            this.FileLevel_FileSizeMB.MinimumWidth = 6;
            this.FileLevel_FileSizeMB.Name = "FileLevel_FileSizeMB";
            this.FileLevel_FileSizeMB.ReadOnly = true;
            this.FileLevel_FileSizeMB.Visible = false;
            this.FileLevel_FileSizeMB.Width = 124;
            // 
            // FileLevel_FileUsedMB
            // 
            this.FileLevel_FileUsedMB.DataPropertyName = "FileUsedMB";
            dataGridViewCellStyle3.Format = "N1";
            this.FileLevel_FileUsedMB.DefaultCellStyle = dataGridViewCellStyle3;
            this.FileLevel_FileUsedMB.HeaderText = "File Used (MB)";
            this.FileLevel_FileUsedMB.MinimumWidth = 6;
            this.FileLevel_FileUsedMB.Name = "FileLevel_FileUsedMB";
            this.FileLevel_FileUsedMB.ReadOnly = true;
            this.FileLevel_FileUsedMB.Visible = false;
            this.FileLevel_FileUsedMB.Width = 130;
            // 
            // FileLevel_FileFreeMB
            // 
            this.FileLevel_FileFreeMB.DataPropertyName = "FileFreeMB";
            dataGridViewCellStyle4.Format = "N1";
            this.FileLevel_FileFreeMB.DefaultCellStyle = dataGridViewCellStyle4;
            this.FileLevel_FileFreeMB.HeaderText = "File Free (MB)";
            this.FileLevel_FileFreeMB.MinimumWidth = 6;
            this.FileLevel_FileFreeMB.Name = "FileLevel_FileFreeMB";
            this.FileLevel_FileFreeMB.ReadOnly = true;
            this.FileLevel_FileFreeMB.Visible = false;
            this.FileLevel_FileFreeMB.Width = 126;
            // 
            // FileLevel_FilePctFree
            // 
            this.FileLevel_FilePctFree.DataPropertyName = "FilePctFree";
            dataGridViewCellStyle5.Format = "P1";
            this.FileLevel_FilePctFree.DefaultCellStyle = dataGridViewCellStyle5;
            this.FileLevel_FilePctFree.HeaderText = "File Free %";
            this.FileLevel_FilePctFree.MinimumWidth = 6;
            this.FileLevel_FilePctFree.Name = "FileLevel_FilePctFree";
            this.FileLevel_FilePctFree.ReadOnly = true;
            this.FileLevel_FilePctFree.Visible = false;
            this.FileLevel_FilePctFree.Width = 108;
            // 
            // FileGroup
            // 
            this.FileGroup.DataPropertyName = "filegroup_name";
            this.FileGroup.HeaderText = "Filegroup";
            this.FileGroup.MinimumWidth = 6;
            this.FileGroup.Name = "FileGroup";
            this.FileGroup.ReadOnly = true;
            this.FileGroup.Width = 96;
            // 
            // SizeMB
            // 
            this.SizeMB.DataPropertyName = "FilegroupSizeMB";
            dataGridViewCellStyle6.Format = "N1";
            this.SizeMB.DefaultCellStyle = dataGridViewCellStyle6;
            this.SizeMB.HeaderText = "Filegroup Size (MB)";
            this.SizeMB.MinimumWidth = 6;
            this.SizeMB.Name = "SizeMB";
            this.SizeMB.ReadOnly = true;
            this.SizeMB.Width = 120;
            // 
            // FilegroupUsedMB
            // 
            this.FilegroupUsedMB.DataPropertyName = "FilegroupUsedMB";
            dataGridViewCellStyle7.Format = "N1";
            dataGridViewCellStyle7.NullValue = null;
            this.FilegroupUsedMB.DefaultCellStyle = dataGridViewCellStyle7;
            this.FilegroupUsedMB.HeaderText = "Filegroup Used (MB)";
            this.FilegroupUsedMB.MinimumWidth = 6;
            this.FilegroupUsedMB.Name = "FilegroupUsedMB";
            this.FilegroupUsedMB.ReadOnly = true;
            this.FilegroupUsedMB.Width = 126;
            // 
            // FilegroupFreeMB
            // 
            this.FilegroupFreeMB.DataPropertyName = "FilegroupFreeMB";
            dataGridViewCellStyle8.Format = "N1";
            this.FilegroupFreeMB.DefaultCellStyle = dataGridViewCellStyle8;
            this.FilegroupFreeMB.HeaderText = "Filegroup Free (MB)";
            this.FilegroupFreeMB.MinimumWidth = 6;
            this.FilegroupFreeMB.Name = "FilegroupFreeMB";
            this.FilegroupFreeMB.ReadOnly = true;
            this.FilegroupFreeMB.Width = 122;
            // 
            // FilegroupPctFree
            // 
            this.FilegroupPctFree.DataPropertyName = "FilegroupPctFree";
            dataGridViewCellStyle9.Format = "P1";
            dataGridViewCellStyle9.NullValue = null;
            this.FilegroupPctFree.DefaultCellStyle = dataGridViewCellStyle9;
            this.FilegroupPctFree.HeaderText = "Filegroup Free %";
            this.FilegroupPctFree.MinimumWidth = 6;
            this.FilegroupPctFree.Name = "FilegroupPctFree";
            this.FilegroupPctFree.ReadOnly = true;
            this.FilegroupPctFree.Width = 122;
            // 
            // NumberOfFiles
            // 
            this.NumberOfFiles.DataPropertyName = "FilegroupNumberOfFiles";
            this.NumberOfFiles.HeaderText = "Filegroup Number of Files";
            this.NumberOfFiles.MinimumWidth = 6;
            this.NumberOfFiles.Name = "NumberOfFiles";
            this.NumberOfFiles.ReadOnly = true;
            this.NumberOfFiles.Width = 141;
            // 
            // ExcludedReason
            // 
            this.ExcludedReason.DataPropertyName = "ExcludedReason";
            this.ExcludedReason.HeaderText = "Excluded Reason";
            this.ExcludedReason.MinimumWidth = 6;
            this.ExcludedReason.Name = "ExcludedReason";
            this.ExcludedReason.ReadOnly = true;
            this.ExcludedReason.Width = 135;
            // 
            // ReadOnly
            // 
            this.ReadOnly.DataPropertyName = "is_read_only";
            this.ReadOnly.HeaderText = "Read Only";
            this.ReadOnly.MinimumWidth = 6;
            this.ReadOnly.Name = "ReadOnly";
            this.ReadOnly.ReadOnly = true;
            this.ReadOnly.Width = 96;
            // 
            // DBReadOnly
            // 
            this.DBReadOnly.DataPropertyName = "is_db_read_only";
            this.DBReadOnly.HeaderText = "DB ReadOnly";
            this.DBReadOnly.MinimumWidth = 6;
            this.DBReadOnly.Name = "DBReadOnly";
            this.DBReadOnly.ReadOnly = true;
            this.DBReadOnly.Width = 113;
            // 
            // Standby
            // 
            this.Standby.DataPropertyName = "is_in_standby";
            this.Standby.HeaderText = "Standby";
            this.Standby.MinimumWidth = 6;
            this.Standby.Name = "Standby";
            this.Standby.ReadOnly = true;
            this.Standby.Width = 89;
            // 
            // State
            // 
            this.State.DataPropertyName = "state_desc";
            this.State.HeaderText = "State";
            this.State.MinimumWidth = 6;
            this.State.Name = "State";
            this.State.ReadOnly = true;
            this.State.Width = 70;
            // 
            // ConfiguredLevel
            // 
            this.ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            this.ConfiguredLevel.HeaderText = "Configured Level";
            this.ConfiguredLevel.MinimumWidth = 6;
            this.ConfiguredLevel.Name = "ConfiguredLevel";
            this.ConfiguredLevel.ReadOnly = true;
            this.ConfiguredLevel.Width = 132;
            // 
            // FileSnapshotAge
            // 
            this.FileSnapshotAge.DataPropertyName = "FileSnapshotAge";
            this.FileSnapshotAge.HeaderText = "File Snapshot Age";
            this.FileSnapshotAge.MinimumWidth = 6;
            this.FileSnapshotAge.Name = "FileSnapshotAge";
            this.FileSnapshotAge.ReadOnly = true;
            this.FileSnapshotAge.Width = 139;
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
            this.toolStripFilter,
            this.tsConfigure,
            this.tsLevel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1616, 31);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 28);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBAChecksGUI.Properties.Resources.ASX_Copy_blue_16x;
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
            this.toolStripFilter.Image = global::DBAChecksGUI.Properties.Resources.FilterDropdown_16x;
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
            this.configureDatabaseThresholdsToolStripMenuItem,
            this.configureInstanceThresholdsToolStripMenuItem,
            this.configureRootThresholdsToolStripMenuItem});
            this.tsConfigure.Image = global::DBAChecksGUI.Properties.Resources.SettingsOutline_16x;
            this.tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsConfigure.Name = "tsConfigure";
            this.tsConfigure.Size = new System.Drawing.Size(34, 28);
            this.tsConfigure.Text = "Configure";
            // 
            // configureDatabaseThresholdsToolStripMenuItem
            // 
            this.configureDatabaseThresholdsToolStripMenuItem.Name = "configureDatabaseThresholdsToolStripMenuItem";
            this.configureDatabaseThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureDatabaseThresholdsToolStripMenuItem.Text = "Configure Database Thresholds";
            this.configureDatabaseThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureDatabaseThresholdsToolStripMenuItem_Click);
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            this.configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            this.configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            this.configureInstanceThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureInstanceThresholdsToolStripMenuItem_Click);
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            this.configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            this.configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            this.configureRootThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureRootThresholdsToolStripMenuItem_Click);
            // 
            // tsLevel
            // 
            this.tsLevel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsLevel.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsFilegroup,
            this.tsFile});
            this.tsLevel.Image = ((System.Drawing.Image)(resources.GetObject("tsLevel.Image")));
            this.tsLevel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsLevel.Name = "tsLevel";
            this.tsLevel.Size = new System.Drawing.Size(57, 28);
            this.tsLevel.Text = "Level";
            // 
            // tsFilegroup
            // 
            this.tsFilegroup.Checked = true;
            this.tsFilegroup.CheckOnClick = true;
            this.tsFilegroup.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsFilegroup.Name = "tsFilegroup";
            this.tsFilegroup.Size = new System.Drawing.Size(155, 26);
            this.tsFilegroup.Text = "Filegroup";
            this.tsFilegroup.Click += new System.EventHandler(this.tsFilegroup_Click);
            // 
            // tsFile
            // 
            this.tsFile.Name = "tsFile";
            this.tsFile.Size = new System.Drawing.Size(155, 26);
            this.tsFile.Text = "File";
            this.tsFile.Click += new System.EventHandler(this.tsFile_Click);
            // 
            // DBFilesControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvFiles);
            this.Controls.Add(this.toolStrip1);
            this.Name = "DBFilesControl";
            this.Size = new System.Drawing.Size(1616, 286);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsLevel;
        private System.Windows.Forms.ToolStripMenuItem tsFilegroup;
        private System.Windows.Forms.ToolStripMenuItem tsFile;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_PhysicalName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileSizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileUsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileFreeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FilePctFree;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn SizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupUsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupFreeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupPctFree;
        private System.Windows.Forms.DataGridViewTextBoxColumn NumberOfFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn ExcludedReason;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn DBReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn Standby;
        private System.Windows.Forms.DataGridViewTextBoxColumn State;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileSnapshotAge;
        private System.Windows.Forms.DataGridViewLinkColumn History;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
    }
}
