using DBADashGUI.CustomReports;

namespace DBADashGUI.DBFiles
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBFilesControl));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle25 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle26 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle27 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle28 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle29 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle30 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle31 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle32 = new System.Windows.Forms.DataGridViewCellStyle();
            dgvFiles = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_FileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_PhysicalName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_FileSizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_FileUsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_FileFreeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_FilePctFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_Growth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_GrowthPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileLevel_MaxSizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupUsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupFreeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupPctFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupMaxSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupPctMaxSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupUsedPctMaxSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NumberOfFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FilegroupAutogrow = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ExcludedReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxSizeExcludedReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DBReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Standby = new System.Windows.Forms.DataGridViewTextBoxColumn();
            State = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FileSnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            History = new System.Windows.Forms.DataGridViewLinkColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            configureDatabaseThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsLevel = new System.Windows.Forms.ToolStripDropDownButton();
            tsFilegroup = new System.Windows.Forms.ToolStripMenuItem();
            tsFile = new System.Windows.Forms.ToolStripMenuItem();
            tsType = new System.Windows.Forms.ToolStripDropDownButton();
            rOWSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lOGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            fILESTREAMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            fULLTEXTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn28 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn29 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn30 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblInfo = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dgvFiles).BeginInit();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvFiles
            // 
            dgvFiles.AllowUserToAddRows = false;
            dgvFiles.AllowUserToDeleteRows = false;
            dgvFiles.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvFiles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, Database, FileLevel_FileName, colType, FileLevel_PhysicalName, FileLevel_FileSizeMB, FileLevel_FileUsedMB, FileLevel_FileFreeMB, FileLevel_FilePctFree, FileLevel_Growth, FileLevel_GrowthPct, FileLevel_MaxSizeMB, FileGroup, SizeMB, FilegroupUsedMB, FilegroupFreeMB, FilegroupPctFree, FilegroupMaxSize, FilegroupPctMaxSize, FilegroupUsedPctMaxSize, NumberOfFiles, FilegroupAutogrow, ExcludedReason, MaxSizeExcludedReason, ReadOnly, DBReadOnly, Standby, State, ConfiguredLevel, FileSnapshotAge, History, Configure });
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle15.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle15.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle15.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle15.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle15.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvFiles.DefaultCellStyle = dataGridViewCellStyle15;
            dgvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvFiles.EnableHeadersVisualStyles = false;
            dgvFiles.Location = new System.Drawing.Point(0, 27);
            dgvFiles.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvFiles.Name = "dgvFiles";
            dgvFiles.ReadOnly = true;
            dgvFiles.ResultSetID = 0;
            dgvFiles.ResultSetName = null;
            dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle16.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle16.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle16.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle16.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle16.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle16.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvFiles.RowHeadersDefaultCellStyle = dataGridViewCellStyle16;
            dgvFiles.RowHeadersVisible = false;
            dgvFiles.RowHeadersWidth = 51;
            dgvFiles.RowTemplate.Height = 24;
            dgvFiles.Size = new System.Drawing.Size(1616, 308);
            dgvFiles.TabIndex = 0;
            dgvFiles.CellContentClick += DgvFiles_CellContentClick;
            dgvFiles.RowsAdded += DgvFiles_RowsAdded;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 90;
            // 
            // Database
            // 
            Database.DataPropertyName = "name";
            Database.HeaderText = "Database";
            Database.MinimumWidth = 6;
            Database.Name = "Database";
            Database.ReadOnly = true;
            Database.Width = 98;
            // 
            // FileLevel_FileName
            // 
            FileLevel_FileName.DataPropertyName = "file_name";
            FileLevel_FileName.HeaderText = "File Name";
            FileLevel_FileName.MinimumWidth = 6;
            FileLevel_FileName.Name = "FileLevel_FileName";
            FileLevel_FileName.ReadOnly = true;
            FileLevel_FileName.Visible = false;
            FileLevel_FileName.Width = 125;
            // 
            // colType
            // 
            colType.DataPropertyName = "type_desc";
            colType.HeaderText = "Type";
            colType.MinimumWidth = 6;
            colType.Name = "colType";
            colType.ReadOnly = true;
            colType.Width = 69;
            // 
            // FileLevel_PhysicalName
            // 
            FileLevel_PhysicalName.DataPropertyName = "physical_name";
            FileLevel_PhysicalName.HeaderText = "Physical Name";
            FileLevel_PhysicalName.MinimumWidth = 6;
            FileLevel_PhysicalName.Name = "FileLevel_PhysicalName";
            FileLevel_PhysicalName.ReadOnly = true;
            FileLevel_PhysicalName.Visible = false;
            FileLevel_PhysicalName.Width = 130;
            // 
            // FileLevel_FileSizeMB
            // 
            FileLevel_FileSizeMB.DataPropertyName = "FileSizeMB";
            dataGridViewCellStyle2.Format = "N1";
            FileLevel_FileSizeMB.DefaultCellStyle = dataGridViewCellStyle2;
            FileLevel_FileSizeMB.HeaderText = "File Size (MB)";
            FileLevel_FileSizeMB.MinimumWidth = 6;
            FileLevel_FileSizeMB.Name = "FileLevel_FileSizeMB";
            FileLevel_FileSizeMB.ReadOnly = true;
            FileLevel_FileSizeMB.Visible = false;
            FileLevel_FileSizeMB.Width = 124;
            // 
            // FileLevel_FileUsedMB
            // 
            FileLevel_FileUsedMB.DataPropertyName = "FileUsedMB";
            dataGridViewCellStyle3.Format = "N1";
            FileLevel_FileUsedMB.DefaultCellStyle = dataGridViewCellStyle3;
            FileLevel_FileUsedMB.HeaderText = "File Used (MB)";
            FileLevel_FileUsedMB.MinimumWidth = 6;
            FileLevel_FileUsedMB.Name = "FileLevel_FileUsedMB";
            FileLevel_FileUsedMB.ReadOnly = true;
            FileLevel_FileUsedMB.Visible = false;
            FileLevel_FileUsedMB.Width = 130;
            // 
            // FileLevel_FileFreeMB
            // 
            FileLevel_FileFreeMB.DataPropertyName = "FileFreeMB";
            dataGridViewCellStyle4.Format = "N1";
            FileLevel_FileFreeMB.DefaultCellStyle = dataGridViewCellStyle4;
            FileLevel_FileFreeMB.HeaderText = "File Free (MB)";
            FileLevel_FileFreeMB.MinimumWidth = 6;
            FileLevel_FileFreeMB.Name = "FileLevel_FileFreeMB";
            FileLevel_FileFreeMB.ReadOnly = true;
            FileLevel_FileFreeMB.Visible = false;
            FileLevel_FileFreeMB.Width = 126;
            // 
            // FileLevel_FilePctFree
            // 
            FileLevel_FilePctFree.DataPropertyName = "FilePctFree";
            dataGridViewCellStyle5.Format = "P1";
            FileLevel_FilePctFree.DefaultCellStyle = dataGridViewCellStyle5;
            FileLevel_FilePctFree.HeaderText = "File Free %";
            FileLevel_FilePctFree.MinimumWidth = 6;
            FileLevel_FilePctFree.Name = "FileLevel_FilePctFree";
            FileLevel_FilePctFree.ReadOnly = true;
            FileLevel_FilePctFree.Visible = false;
            FileLevel_FilePctFree.Width = 108;
            // 
            // FileLevel_Growth
            // 
            FileLevel_Growth.DataPropertyName = "GrowthMB";
            dataGridViewCellStyle6.Format = "#,##0.#";
            FileLevel_Growth.DefaultCellStyle = dataGridViewCellStyle6;
            FileLevel_Growth.HeaderText = "Growth (MB)";
            FileLevel_Growth.MinimumWidth = 6;
            FileLevel_Growth.Name = "FileLevel_Growth";
            FileLevel_Growth.ReadOnly = true;
            FileLevel_Growth.Visible = false;
            FileLevel_Growth.Width = 116;
            // 
            // FileLevel_GrowthPct
            // 
            FileLevel_GrowthPct.DataPropertyName = "GrowthPct";
            FileLevel_GrowthPct.HeaderText = "Growth %";
            FileLevel_GrowthPct.MinimumWidth = 6;
            FileLevel_GrowthPct.Name = "FileLevel_GrowthPct";
            FileLevel_GrowthPct.ReadOnly = true;
            FileLevel_GrowthPct.Visible = false;
            FileLevel_GrowthPct.Width = 98;
            // 
            // FileLevel_MaxSizeMB
            // 
            FileLevel_MaxSizeMB.DataPropertyName = "MaxSizeMB";
            dataGridViewCellStyle7.Format = "#,##0.#";
            FileLevel_MaxSizeMB.DefaultCellStyle = dataGridViewCellStyle7;
            FileLevel_MaxSizeMB.HeaderText = "Max Size (MB)";
            FileLevel_MaxSizeMB.MinimumWidth = 6;
            FileLevel_MaxSizeMB.Name = "FileLevel_MaxSizeMB";
            FileLevel_MaxSizeMB.ReadOnly = true;
            FileLevel_MaxSizeMB.Visible = false;
            FileLevel_MaxSizeMB.Width = 127;
            // 
            // FileGroup
            // 
            FileGroup.DataPropertyName = "filegroup_name";
            FileGroup.HeaderText = "Filegroup";
            FileGroup.MinimumWidth = 6;
            FileGroup.Name = "FileGroup";
            FileGroup.ReadOnly = true;
            FileGroup.Width = 96;
            // 
            // SizeMB
            // 
            SizeMB.DataPropertyName = "FilegroupSizeMB";
            dataGridViewCellStyle8.Format = "N1";
            SizeMB.DefaultCellStyle = dataGridViewCellStyle8;
            SizeMB.HeaderText = "Filegroup Size (MB)";
            SizeMB.MinimumWidth = 6;
            SizeMB.Name = "SizeMB";
            SizeMB.ReadOnly = true;
            SizeMB.Width = 120;
            // 
            // FilegroupUsedMB
            // 
            FilegroupUsedMB.DataPropertyName = "FilegroupUsedMB";
            dataGridViewCellStyle9.Format = "N1";
            dataGridViewCellStyle9.NullValue = null;
            FilegroupUsedMB.DefaultCellStyle = dataGridViewCellStyle9;
            FilegroupUsedMB.HeaderText = "Filegroup Used (MB)";
            FilegroupUsedMB.MinimumWidth = 6;
            FilegroupUsedMB.Name = "FilegroupUsedMB";
            FilegroupUsedMB.ReadOnly = true;
            FilegroupUsedMB.Width = 126;
            // 
            // FilegroupFreeMB
            // 
            FilegroupFreeMB.DataPropertyName = "FilegroupFreeMB";
            dataGridViewCellStyle10.Format = "N1";
            FilegroupFreeMB.DefaultCellStyle = dataGridViewCellStyle10;
            FilegroupFreeMB.HeaderText = "Filegroup Free (MB)";
            FilegroupFreeMB.MinimumWidth = 6;
            FilegroupFreeMB.Name = "FilegroupFreeMB";
            FilegroupFreeMB.ReadOnly = true;
            FilegroupFreeMB.Width = 122;
            // 
            // FilegroupPctFree
            // 
            FilegroupPctFree.DataPropertyName = "FilegroupPctFree";
            dataGridViewCellStyle11.Format = "P1";
            dataGridViewCellStyle11.NullValue = null;
            FilegroupPctFree.DefaultCellStyle = dataGridViewCellStyle11;
            FilegroupPctFree.HeaderText = "Filegroup Free %";
            FilegroupPctFree.MinimumWidth = 6;
            FilegroupPctFree.Name = "FilegroupPctFree";
            FilegroupPctFree.ReadOnly = true;
            FilegroupPctFree.Width = 122;
            // 
            // FilegroupMaxSize
            // 
            FilegroupMaxSize.DataPropertyName = "FilegroupMaxSizeMB";
            dataGridViewCellStyle12.Format = "N0";
            FilegroupMaxSize.DefaultCellStyle = dataGridViewCellStyle12;
            FilegroupMaxSize.HeaderText = "Filegroup Max Size (MB)";
            FilegroupMaxSize.MinimumWidth = 6;
            FilegroupMaxSize.Name = "FilegroupMaxSize";
            FilegroupMaxSize.ReadOnly = true;
            FilegroupMaxSize.Width = 146;
            // 
            // FilegroupPctMaxSize
            // 
            FilegroupPctMaxSize.DataPropertyName = "FilegroupPctOfMaxSize";
            dataGridViewCellStyle13.Format = "P1";
            FilegroupPctMaxSize.DefaultCellStyle = dataGridViewCellStyle13;
            FilegroupPctMaxSize.HeaderText = "Filegroup % Of Max Size";
            FilegroupPctMaxSize.MinimumWidth = 6;
            FilegroupPctMaxSize.Name = "FilegroupPctMaxSize";
            FilegroupPctMaxSize.ReadOnly = true;
            FilegroupPctMaxSize.Width = 124;
            // 
            // FilegroupUsedPctMaxSize
            // 
            FilegroupUsedPctMaxSize.DataPropertyName = "FilegroupUsedPctOfMaxSize";
            dataGridViewCellStyle14.Format = "P1";
            FilegroupUsedPctMaxSize.DefaultCellStyle = dataGridViewCellStyle14;
            FilegroupUsedPctMaxSize.HeaderText = "Filegroup Used % Max Size";
            FilegroupUsedPctMaxSize.MinimumWidth = 6;
            FilegroupUsedPctMaxSize.Name = "FilegroupUsedPctMaxSize";
            FilegroupUsedPctMaxSize.ReadOnly = true;
            FilegroupUsedPctMaxSize.Width = 140;
            // 
            // NumberOfFiles
            // 
            NumberOfFiles.DataPropertyName = "FilegroupNumberOfFiles";
            NumberOfFiles.HeaderText = "Filegroup Number of Files";
            NumberOfFiles.MinimumWidth = 6;
            NumberOfFiles.Name = "NumberOfFiles";
            NumberOfFiles.ReadOnly = true;
            NumberOfFiles.Width = 141;
            // 
            // FilegroupAutogrow
            // 
            FilegroupAutogrow.DataPropertyName = "FilegroupAutogrowFileCount";
            FilegroupAutogrow.HeaderText = "Filegroup Autogrow File Count";
            FilegroupAutogrow.MinimumWidth = 6;
            FilegroupAutogrow.Name = "FilegroupAutogrow";
            FilegroupAutogrow.ReadOnly = true;
            FilegroupAutogrow.Width = 172;
            // 
            // ExcludedReason
            // 
            ExcludedReason.DataPropertyName = "ExcludedReason";
            ExcludedReason.HeaderText = "Excluded Reason";
            ExcludedReason.MinimumWidth = 6;
            ExcludedReason.Name = "ExcludedReason";
            ExcludedReason.ReadOnly = true;
            ExcludedReason.Width = 135;
            // 
            // MaxSizeExcludedReason
            // 
            MaxSizeExcludedReason.DataPropertyName = "MaxSizeExcludedReason";
            MaxSizeExcludedReason.HeaderText = "Max Size Excluded Reason";
            MaxSizeExcludedReason.MinimumWidth = 6;
            MaxSizeExcludedReason.Name = "MaxSizeExcludedReason";
            MaxSizeExcludedReason.ReadOnly = true;
            MaxSizeExcludedReason.Width = 189;
            // 
            // ReadOnly
            // 
            ReadOnly.DataPropertyName = "is_read_only";
            ReadOnly.HeaderText = "Read Only";
            ReadOnly.MinimumWidth = 6;
            ReadOnly.Name = "ReadOnly";
            ReadOnly.ReadOnly = true;
            ReadOnly.Width = 96;
            // 
            // DBReadOnly
            // 
            DBReadOnly.DataPropertyName = "is_db_read_only";
            DBReadOnly.HeaderText = "DB ReadOnly";
            DBReadOnly.MinimumWidth = 6;
            DBReadOnly.Name = "DBReadOnly";
            DBReadOnly.ReadOnly = true;
            DBReadOnly.Width = 113;
            // 
            // Standby
            // 
            Standby.DataPropertyName = "is_in_standby";
            Standby.HeaderText = "Standby";
            Standby.MinimumWidth = 6;
            Standby.Name = "Standby";
            Standby.ReadOnly = true;
            Standby.Width = 89;
            // 
            // State
            // 
            State.DataPropertyName = "state_desc";
            State.HeaderText = "State";
            State.MinimumWidth = 6;
            State.Name = "State";
            State.ReadOnly = true;
            State.Width = 70;
            // 
            // ConfiguredLevel
            // 
            ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            ConfiguredLevel.HeaderText = "Configured Level";
            ConfiguredLevel.MinimumWidth = 6;
            ConfiguredLevel.Name = "ConfiguredLevel";
            ConfiguredLevel.ReadOnly = true;
            ConfiguredLevel.Width = 132;
            // 
            // FileSnapshotAge
            // 
            FileSnapshotAge.DataPropertyName = "FileSnapshotAge";
            FileSnapshotAge.HeaderText = "File Snapshot Age";
            FileSnapshotAge.MinimumWidth = 6;
            FileSnapshotAge.Name = "FileSnapshotAge";
            FileSnapshotAge.ReadOnly = true;
            FileSnapshotAge.Width = 139;
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
            // Configure
            // 
            Configure.HeaderText = "Configure";
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            Configure.Text = "Configure";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 98;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsConfigure, tsLevel, tsType, tsTrigger, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1616, 27);
            toolStrip1.TabIndex = 3;
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
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureDatabaseThresholdsToolStripMenuItem, configureInstanceThresholdsToolStripMenuItem, configureRootThresholdsToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // configureDatabaseThresholdsToolStripMenuItem
            // 
            configureDatabaseThresholdsToolStripMenuItem.Name = "configureDatabaseThresholdsToolStripMenuItem";
            configureDatabaseThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureDatabaseThresholdsToolStripMenuItem.Text = "Configure Database Thresholds";
            configureDatabaseThresholdsToolStripMenuItem.Click += ConfigureDatabaseThresholdsToolStripMenuItem_Click;
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            configureInstanceThresholdsToolStripMenuItem.Click += ConfigureInstanceThresholdsToolStripMenuItem_Click;
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            configureRootThresholdsToolStripMenuItem.Click += ConfigureRootThresholdsToolStripMenuItem_Click;
            // 
            // tsLevel
            // 
            tsLevel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsLevel.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsFilegroup, tsFile });
            tsLevel.Image = (System.Drawing.Image)resources.GetObject("tsLevel.Image");
            tsLevel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLevel.Name = "tsLevel";
            tsLevel.Size = new System.Drawing.Size(57, 24);
            tsLevel.Text = "Level";
            // 
            // tsFilegroup
            // 
            tsFilegroup.Checked = true;
            tsFilegroup.CheckOnClick = true;
            tsFilegroup.CheckState = System.Windows.Forms.CheckState.Checked;
            tsFilegroup.Name = "tsFilegroup";
            tsFilegroup.Size = new System.Drawing.Size(155, 26);
            tsFilegroup.Text = "Filegroup";
            tsFilegroup.Click += TsFilegroup_Click;
            // 
            // tsFile
            // 
            tsFile.Name = "tsFile";
            tsFile.Size = new System.Drawing.Size(155, 26);
            tsFile.Text = "File";
            tsFile.Click += TsFile_Click;
            // 
            // tsType
            // 
            tsType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { rOWSToolStripMenuItem, lOGToolStripMenuItem, fILESTREAMToolStripMenuItem, fULLTEXTToolStripMenuItem });
            tsType.Image = (System.Drawing.Image)resources.GetObject("tsType.Image");
            tsType.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsType.Name = "tsType";
            tsType.Size = new System.Drawing.Size(54, 24);
            tsType.Text = "Type";
            // 
            // rOWSToolStripMenuItem
            // 
            rOWSToolStripMenuItem.Checked = true;
            rOWSToolStripMenuItem.CheckOnClick = true;
            rOWSToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            rOWSToolStripMenuItem.Name = "rOWSToolStripMenuItem";
            rOWSToolStripMenuItem.Size = new System.Drawing.Size(174, 26);
            rOWSToolStripMenuItem.Tag = "0";
            rOWSToolStripMenuItem.Text = "ROWS";
            rOWSToolStripMenuItem.CheckedChanged += TsTypes_Click;
            // 
            // lOGToolStripMenuItem
            // 
            lOGToolStripMenuItem.Checked = true;
            lOGToolStripMenuItem.CheckOnClick = true;
            lOGToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            lOGToolStripMenuItem.Name = "lOGToolStripMenuItem";
            lOGToolStripMenuItem.Size = new System.Drawing.Size(174, 26);
            lOGToolStripMenuItem.Tag = "1";
            lOGToolStripMenuItem.Text = "LOG";
            lOGToolStripMenuItem.CheckedChanged += TsTypes_Click;
            // 
            // fILESTREAMToolStripMenuItem
            // 
            fILESTREAMToolStripMenuItem.Checked = true;
            fILESTREAMToolStripMenuItem.CheckOnClick = true;
            fILESTREAMToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            fILESTREAMToolStripMenuItem.Name = "fILESTREAMToolStripMenuItem";
            fILESTREAMToolStripMenuItem.Size = new System.Drawing.Size(174, 26);
            fILESTREAMToolStripMenuItem.Tag = "2";
            fILESTREAMToolStripMenuItem.Text = "FILESTREAM";
            fILESTREAMToolStripMenuItem.CheckedChanged += TsTypes_Click;
            // 
            // fULLTEXTToolStripMenuItem
            // 
            fULLTEXTToolStripMenuItem.Checked = true;
            fULLTEXTToolStripMenuItem.CheckOnClick = true;
            fULLTEXTToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            fULLTEXTToolStripMenuItem.Name = "fULLTEXTToolStripMenuItem";
            fULLTEXTToolStripMenuItem.Size = new System.Drawing.Size(174, 26);
            fULLTEXTToolStripMenuItem.Tag = "4";
            fULLTEXTToolStripMenuItem.Text = "FULLTEXT";
            fULLTEXTToolStripMenuItem.CheckedChanged += TsTypes_Click;
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
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
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
            dataGridViewTextBoxColumn2.DataPropertyName = "name";
            dataGridViewTextBoxColumn2.HeaderText = "Database";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 98;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "file_name";
            dataGridViewTextBoxColumn3.HeaderText = "File Name";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Visible = false;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "physical_name";
            dataGridViewTextBoxColumn4.HeaderText = "Physical Name";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Visible = false;
            dataGridViewTextBoxColumn4.Width = 130;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "FileSizeMB";
            dataGridViewCellStyle17.Format = "N1";
            dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle17;
            dataGridViewTextBoxColumn5.HeaderText = "File Size (MB)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Visible = false;
            dataGridViewTextBoxColumn5.Width = 124;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "FileUsedMB";
            dataGridViewCellStyle18.Format = "N1";
            dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle18;
            dataGridViewTextBoxColumn6.HeaderText = "File Used (MB)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Visible = false;
            dataGridViewTextBoxColumn6.Width = 130;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "FileFreeMB";
            dataGridViewCellStyle19.Format = "N1";
            dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle19;
            dataGridViewTextBoxColumn7.HeaderText = "File Free (MB)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Visible = false;
            dataGridViewTextBoxColumn7.Width = 126;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "FilePctFree";
            dataGridViewCellStyle20.Format = "P1";
            dataGridViewTextBoxColumn8.DefaultCellStyle = dataGridViewCellStyle20;
            dataGridViewTextBoxColumn8.HeaderText = "File Free %";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Visible = false;
            dataGridViewTextBoxColumn8.Width = 108;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "GrowthMB";
            dataGridViewCellStyle21.Format = "#,##0.#";
            dataGridViewTextBoxColumn9.DefaultCellStyle = dataGridViewCellStyle21;
            dataGridViewTextBoxColumn9.HeaderText = "Growth (MB)";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Visible = false;
            dataGridViewTextBoxColumn9.Width = 107;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "MaxSizeMB";
            dataGridViewCellStyle22.Format = "#,##0.#";
            dataGridViewTextBoxColumn10.DefaultCellStyle = dataGridViewCellStyle22;
            dataGridViewTextBoxColumn10.HeaderText = "Max Size (MB)";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Visible = false;
            dataGridViewTextBoxColumn10.Width = 117;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "filegroup_name";
            dataGridViewCellStyle23.Format = "#,##0.#";
            dataGridViewTextBoxColumn11.DefaultCellStyle = dataGridViewCellStyle23;
            dataGridViewTextBoxColumn11.HeaderText = "Filegroup";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Visible = false;
            dataGridViewTextBoxColumn11.Width = 96;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "FilegroupSizeMB";
            dataGridViewCellStyle24.Format = "N1";
            dataGridViewTextBoxColumn12.DefaultCellStyle = dataGridViewCellStyle24;
            dataGridViewTextBoxColumn12.HeaderText = "Filegroup Size (MB)";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Visible = false;
            dataGridViewTextBoxColumn12.Width = 120;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "FilegroupUsedMB";
            dataGridViewCellStyle25.Format = "N1";
            dataGridViewCellStyle25.NullValue = null;
            dataGridViewTextBoxColumn13.DefaultCellStyle = dataGridViewCellStyle25;
            dataGridViewTextBoxColumn13.HeaderText = "Filegroup Used (MB)";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 126;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "FilegroupFreeMB";
            dataGridViewCellStyle26.Format = "N1";
            dataGridViewTextBoxColumn14.DefaultCellStyle = dataGridViewCellStyle26;
            dataGridViewTextBoxColumn14.HeaderText = "Filegroup Free (MB)";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 122;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "FilegroupPctFree";
            dataGridViewCellStyle27.Format = "P1";
            dataGridViewCellStyle27.NullValue = null;
            dataGridViewTextBoxColumn15.DefaultCellStyle = dataGridViewCellStyle27;
            dataGridViewTextBoxColumn15.HeaderText = "Filegroup Free %";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 122;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "FilegroupNumberOfFiles";
            dataGridViewCellStyle28.Format = "P1";
            dataGridViewCellStyle28.NullValue = null;
            dataGridViewTextBoxColumn16.DefaultCellStyle = dataGridViewCellStyle28;
            dataGridViewTextBoxColumn16.HeaderText = "Filegroup Number of Files";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 141;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "ExcludedReason";
            dataGridViewCellStyle29.Format = "N0";
            dataGridViewTextBoxColumn17.DefaultCellStyle = dataGridViewCellStyle29;
            dataGridViewTextBoxColumn17.HeaderText = "Excluded Reason";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            dataGridViewTextBoxColumn17.Width = 135;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "is_read_only";
            dataGridViewCellStyle30.Format = "P1";
            dataGridViewTextBoxColumn18.DefaultCellStyle = dataGridViewCellStyle30;
            dataGridViewTextBoxColumn18.HeaderText = "Read Only";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            dataGridViewTextBoxColumn18.Width = 96;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "is_db_read_only";
            dataGridViewCellStyle31.Format = "P1";
            dataGridViewTextBoxColumn19.DefaultCellStyle = dataGridViewCellStyle31;
            dataGridViewTextBoxColumn19.HeaderText = "DB ReadOnly";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            dataGridViewTextBoxColumn19.Width = 113;
            // 
            // dataGridViewTextBoxColumn20
            // 
            dataGridViewTextBoxColumn20.DataPropertyName = "is_in_standby";
            dataGridViewCellStyle32.Format = "P1";
            dataGridViewTextBoxColumn20.DefaultCellStyle = dataGridViewCellStyle32;
            dataGridViewTextBoxColumn20.HeaderText = "Standby";
            dataGridViewTextBoxColumn20.MinimumWidth = 6;
            dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            dataGridViewTextBoxColumn20.ReadOnly = true;
            dataGridViewTextBoxColumn20.Width = 89;
            // 
            // dataGridViewTextBoxColumn21
            // 
            dataGridViewTextBoxColumn21.DataPropertyName = "state_desc";
            dataGridViewTextBoxColumn21.HeaderText = "State";
            dataGridViewTextBoxColumn21.MinimumWidth = 6;
            dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            dataGridViewTextBoxColumn21.ReadOnly = true;
            dataGridViewTextBoxColumn21.Width = 70;
            // 
            // dataGridViewTextBoxColumn22
            // 
            dataGridViewTextBoxColumn22.DataPropertyName = "ConfiguredLevel";
            dataGridViewTextBoxColumn22.HeaderText = "Configured Level";
            dataGridViewTextBoxColumn22.MinimumWidth = 6;
            dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            dataGridViewTextBoxColumn22.ReadOnly = true;
            dataGridViewTextBoxColumn22.Width = 132;
            // 
            // dataGridViewTextBoxColumn23
            // 
            dataGridViewTextBoxColumn23.DataPropertyName = "FileSnapshotAge";
            dataGridViewTextBoxColumn23.HeaderText = "File Snapshot Age";
            dataGridViewTextBoxColumn23.MinimumWidth = 6;
            dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            dataGridViewTextBoxColumn23.ReadOnly = true;
            dataGridViewTextBoxColumn23.Width = 139;
            // 
            // dataGridViewTextBoxColumn24
            // 
            dataGridViewTextBoxColumn24.DataPropertyName = "FileSnapshotAge";
            dataGridViewTextBoxColumn24.HeaderText = "File Snapshot Age";
            dataGridViewTextBoxColumn24.MinimumWidth = 6;
            dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            dataGridViewTextBoxColumn24.ReadOnly = true;
            dataGridViewTextBoxColumn24.Width = 139;
            // 
            // dataGridViewTextBoxColumn25
            // 
            dataGridViewTextBoxColumn25.DataPropertyName = "is_in_standby";
            dataGridViewTextBoxColumn25.HeaderText = "Standby";
            dataGridViewTextBoxColumn25.MinimumWidth = 6;
            dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            dataGridViewTextBoxColumn25.Width = 89;
            // 
            // dataGridViewTextBoxColumn26
            // 
            dataGridViewTextBoxColumn26.DataPropertyName = "state_desc";
            dataGridViewTextBoxColumn26.HeaderText = "State";
            dataGridViewTextBoxColumn26.MinimumWidth = 6;
            dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            dataGridViewTextBoxColumn26.Width = 70;
            // 
            // dataGridViewTextBoxColumn27
            // 
            dataGridViewTextBoxColumn27.DataPropertyName = "ConfiguredLevel";
            dataGridViewTextBoxColumn27.HeaderText = "Configured Level";
            dataGridViewTextBoxColumn27.MinimumWidth = 6;
            dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            dataGridViewTextBoxColumn27.Width = 132;
            // 
            // dataGridViewTextBoxColumn28
            // 
            dataGridViewTextBoxColumn28.DataPropertyName = "FileSnapshotAge";
            dataGridViewTextBoxColumn28.HeaderText = "File Snapshot Age";
            dataGridViewTextBoxColumn28.MinimumWidth = 6;
            dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            dataGridViewTextBoxColumn28.Width = 139;
            // 
            // dataGridViewTextBoxColumn29
            // 
            dataGridViewTextBoxColumn29.DataPropertyName = "ConfiguredLevel";
            dataGridViewTextBoxColumn29.HeaderText = "Configured Level";
            dataGridViewTextBoxColumn29.MinimumWidth = 6;
            dataGridViewTextBoxColumn29.Name = "dataGridViewTextBoxColumn29";
            dataGridViewTextBoxColumn29.Width = 132;
            // 
            // dataGridViewTextBoxColumn30
            // 
            dataGridViewTextBoxColumn30.DataPropertyName = "FileSnapshotAge";
            dataGridViewTextBoxColumn30.HeaderText = "File Snapshot Age";
            dataGridViewTextBoxColumn30.MinimumWidth = 6;
            dataGridViewTextBoxColumn30.Name = "dataGridViewTextBoxColumn30";
            dataGridViewTextBoxColumn30.Width = 139;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblInfo });
            statusStrip1.Location = new System.Drawing.Point(0, 335);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            statusStrip1.Size = new System.Drawing.Size(1616, 22);
            statusStrip1.TabIndex = 4;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblInfo
            // 
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new System.Drawing.Size(0, 16);
            // 
            // DBFilesControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvFiles);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DBFilesControl";
            Size = new System.Drawing.Size(1616, 357);
            ((System.ComponentModel.ISupportInitialize)dgvFiles).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvFiles;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsLevel;
        private System.Windows.Forms.ToolStripMenuItem tsFilegroup;
        private System.Windows.Forms.ToolStripMenuItem tsFile;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn24;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn25;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn26;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn27;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn28;
        private System.Windows.Forms.ToolStripDropDownButton tsType;
        private System.Windows.Forms.ToolStripMenuItem rOWSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lOGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fILESTREAMToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fULLTEXTToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn29;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn30;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_PhysicalName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileSizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileUsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FileFreeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_FilePctFree;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_Growth;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_GrowthPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileLevel_MaxSizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn SizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupUsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupFreeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupPctFree;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupMaxSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupPctMaxSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupUsedPctMaxSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn NumberOfFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilegroupAutogrow;
        private System.Windows.Forms.DataGridViewTextBoxColumn ExcludedReason;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxSizeExcludedReason;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn DBReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn Standby;
        private System.Windows.Forms.DataGridViewTextBoxColumn State;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileSnapshotAge;
        private System.Windows.Forms.DataGridViewLinkColumn History;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblInfo;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
