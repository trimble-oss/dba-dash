namespace DBADashGUI.DBFiles
{
    partial class TempDBConfig
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvTempDB = new System.Windows.Forms.DataGridView();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumberOfDataFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInsufficientFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMinimumRecommendedFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumberOfLogFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEvenSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEvenGrowth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTotalSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFileSizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxGrowthMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxLogGrowth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxGrowthPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxLogGrowthPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTempDBVolumes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCPUCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colT1117 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colT1118 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIsTraceFlagRequired = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTempDBMemoryOpt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
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
            ((System.ComponentModel.ISupportInitialize)(this.dgvTempDB)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvTempDB
            // 
            this.dgvTempDB.AllowUserToAddRows = false;
            this.dgvTempDB.AllowUserToDeleteRows = false;
            this.dgvTempDB.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvTempDB.BackgroundColor = System.Drawing.Color.White;
            this.dgvTempDB.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTempDB.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colNumberOfDataFiles,
            this.colInsufficientFiles,
            this.colMinimumRecommendedFiles,
            this.colNumberOfLogFiles,
            this.colEvenSize,
            this.colEvenGrowth,
            this.colTotalSize,
            this.colLogMB,
            this.colFileSizeMB,
            this.colMaxGrowthMB,
            this.colMaxLogGrowth,
            this.colMaxGrowthPct,
            this.colMaxLogGrowthPct,
            this.colTempDBVolumes,
            this.colCPUCores,
            this.colT1117,
            this.colT1118,
            this.colIsTraceFlagRequired,
            this.colTempDBMemoryOpt});
            this.dgvTempDB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTempDB.Location = new System.Drawing.Point(0, 31);
            this.dgvTempDB.Name = "dgvTempDB";
            this.dgvTempDB.ReadOnly = true;
            this.dgvTempDB.RowHeadersVisible = false;
            this.dgvTempDB.RowHeadersWidth = 51;
            this.dgvTempDB.RowTemplate.Height = 24;
            this.dgvTempDB.Size = new System.Drawing.Size(959, 430);
            this.dgvTempDB.TabIndex = 0;
            this.dgvTempDB.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvTempDB_RowsAdded);
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "Instance";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Width = 90;
            // 
            // colNumberOfDataFiles
            // 
            this.colNumberOfDataFiles.DataPropertyName = "NumberOfDataFiles";
            this.colNumberOfDataFiles.HeaderText = "#Data Files";
            this.colNumberOfDataFiles.MinimumWidth = 6;
            this.colNumberOfDataFiles.Name = "colNumberOfDataFiles";
            this.colNumberOfDataFiles.ReadOnly = true;
            // 
            // colInsufficientFiles
            // 
            this.colInsufficientFiles.DataPropertyName = "InsufficientFiles";
            this.colInsufficientFiles.HeaderText = "Insufficient Files";
            this.colInsufficientFiles.MinimumWidth = 6;
            this.colInsufficientFiles.Name = "colInsufficientFiles";
            this.colInsufficientFiles.ReadOnly = true;
            this.colInsufficientFiles.Width = 126;
            // 
            // colMinimumRecommendedFiles
            // 
            this.colMinimumRecommendedFiles.DataPropertyName = "MinimumRecommendedFiles";
            this.colMinimumRecommendedFiles.HeaderText = "Recommended Files (Minimum)";
            this.colMinimumRecommendedFiles.MinimumWidth = 6;
            this.colMinimumRecommendedFiles.Name = "colMinimumRecommendedFiles";
            this.colMinimumRecommendedFiles.ReadOnly = true;
            this.colMinimumRecommendedFiles.Width = 154;
            // 
            // colNumberOfLogFiles
            // 
            this.colNumberOfLogFiles.DataPropertyName = "NumberOfLogFiles";
            this.colNumberOfLogFiles.HeaderText = "#Log Files";
            this.colNumberOfLogFiles.MinimumWidth = 6;
            this.colNumberOfLogFiles.Name = "colNumberOfLogFiles";
            this.colNumberOfLogFiles.ReadOnly = true;
            this.colNumberOfLogFiles.Width = 94;
            // 
            // colEvenSize
            // 
            this.colEvenSize.DataPropertyName = "IsEvenlySized";
            this.colEvenSize.HeaderText = "Even Sized?";
            this.colEvenSize.MinimumWidth = 6;
            this.colEvenSize.Name = "colEvenSize";
            this.colEvenSize.ReadOnly = true;
            this.colEvenSize.ToolTipText = "Should be evenly sized";
            this.colEvenSize.Width = 107;
            // 
            // colEvenGrowth
            // 
            this.colEvenGrowth.DataPropertyName = "IsEvenGrowth";
            this.colEvenGrowth.HeaderText = "Even Growth";
            this.colEvenGrowth.MinimumWidth = 6;
            this.colEvenGrowth.Name = "colEvenGrowth";
            this.colEvenGrowth.ReadOnly = true;
            this.colEvenGrowth.ToolTipText = "Should be even growth";
            this.colEvenGrowth.Width = 109;
            // 
            // colTotalSize
            // 
            this.colTotalSize.DataPropertyName = "TotalSizeMB";
            dataGridViewCellStyle1.Format = "N0";
            this.colTotalSize.DefaultCellStyle = dataGridViewCellStyle1;
            this.colTotalSize.HeaderText = "Total Size (MB)";
            this.colTotalSize.MinimumWidth = 6;
            this.colTotalSize.Name = "colTotalSize";
            this.colTotalSize.ReadOnly = true;
            this.colTotalSize.Width = 123;
            // 
            // colLogMB
            // 
            this.colLogMB.DataPropertyName = "LogMB";
            dataGridViewCellStyle2.Format = "N0";
            this.colLogMB.DefaultCellStyle = dataGridViewCellStyle2;
            this.colLogMB.HeaderText = "Log MB";
            this.colLogMB.MinimumWidth = 6;
            this.colLogMB.Name = "colLogMB";
            this.colLogMB.ReadOnly = true;
            this.colLogMB.Width = 61;
            // 
            // colFileSizeMB
            // 
            this.colFileSizeMB.DataPropertyName = "FileSizeMB";
            dataGridViewCellStyle3.Format = "N0";
            this.colFileSizeMB.DefaultCellStyle = dataGridViewCellStyle3;
            this.colFileSizeMB.HeaderText = "File Size (MB)";
            this.colFileSizeMB.MinimumWidth = 6;
            this.colFileSizeMB.Name = "colFileSizeMB";
            this.colFileSizeMB.ReadOnly = true;
            this.colFileSizeMB.Width = 114;
            // 
            // colMaxGrowthMB
            // 
            this.colMaxGrowthMB.DataPropertyName = "MaxGrowthMB";
            dataGridViewCellStyle4.Format = "N0";
            this.colMaxGrowthMB.DefaultCellStyle = dataGridViewCellStyle4;
            this.colMaxGrowthMB.HeaderText = "Max Growth (MB)";
            this.colMaxGrowthMB.MinimumWidth = 6;
            this.colMaxGrowthMB.Name = "colMaxGrowthMB";
            this.colMaxGrowthMB.ReadOnly = true;
            this.colMaxGrowthMB.Width = 133;
            // 
            // colMaxLogGrowth
            // 
            this.colMaxLogGrowth.DataPropertyName = "MaxLogGrowthMB";
            dataGridViewCellStyle5.Format = "N0";
            this.colMaxLogGrowth.DefaultCellStyle = dataGridViewCellStyle5;
            this.colMaxLogGrowth.HeaderText = "Max Log Growth (MB)";
            this.colMaxLogGrowth.MinimumWidth = 6;
            this.colMaxLogGrowth.Name = "colMaxLogGrowth";
            this.colMaxLogGrowth.ReadOnly = true;
            this.colMaxLogGrowth.Width = 131;
            // 
            // colMaxGrowthPct
            // 
            this.colMaxGrowthPct.DataPropertyName = "MaxGrowthPct";
            this.colMaxGrowthPct.HeaderText = "Max Growth %";
            this.colMaxGrowthPct.MinimumWidth = 6;
            this.colMaxGrowthPct.Name = "colMaxGrowthPct";
            this.colMaxGrowthPct.ReadOnly = true;
            this.colMaxGrowthPct.Width = 106;
            // 
            // colMaxLogGrowthPct
            // 
            this.colMaxLogGrowthPct.DataPropertyName = "MaxLogGrowthPct";
            this.colMaxLogGrowthPct.HeaderText = "Max Log Growth %";
            this.colMaxLogGrowthPct.MinimumWidth = 6;
            this.colMaxLogGrowthPct.Name = "colMaxLogGrowthPct";
            this.colMaxLogGrowthPct.ReadOnly = true;
            this.colMaxLogGrowthPct.Width = 127;
            // 
            // colTempDBVolumes
            // 
            this.colTempDBVolumes.DataPropertyName = "TempDBVolumes";
            this.colTempDBVolumes.HeaderText = "TempDB Volume(s)";
            this.colTempDBVolumes.MinimumWidth = 6;
            this.colTempDBVolumes.Name = "colTempDBVolumes";
            this.colTempDBVolumes.ReadOnly = true;
            this.colTempDBVolumes.Width = 146;
            // 
            // colCPUCores
            // 
            this.colCPUCores.DataPropertyName = "cpu_core_count";
            this.colCPUCores.HeaderText = "CPU Core Count";
            this.colCPUCores.MinimumWidth = 6;
            this.colCPUCores.Name = "colCPUCores";
            this.colCPUCores.ReadOnly = true;
            this.colCPUCores.ToolTipText = "Up to 8 cores tempdb should match core count.  Then start at 8 files and add more" +
    " if needed.";
            this.colCPUCores.Width = 128;
            // 
            // colT1117
            // 
            this.colT1117.DataPropertyName = "T1117";
            this.colT1117.HeaderText = "T1117";
            this.colT1117.MinimumWidth = 6;
            this.colT1117.Name = "colT1117";
            this.colT1117.ReadOnly = true;
            this.colT1117.ToolTipText = "Recommended trace flag prior to SQL 2016";
            this.colT1117.Width = 78;
            // 
            // colT1118
            // 
            this.colT1118.DataPropertyName = "T1118";
            this.colT1118.HeaderText = "T1118";
            this.colT1118.MinimumWidth = 6;
            this.colT1118.Name = "colT1118";
            this.colT1118.ReadOnly = true;
            this.colT1118.ToolTipText = "Recommended trace flag prior to SQL 2016";
            this.colT1118.Width = 78;
            // 
            // colIsTraceFlagRequired
            // 
            this.colIsTraceFlagRequired.DataPropertyName = "IsTraceFlagRequired";
            this.colIsTraceFlagRequired.HeaderText = "Trace Flag Required?";
            this.colIsTraceFlagRequired.MinimumWidth = 6;
            this.colIsTraceFlagRequired.Name = "colIsTraceFlagRequired";
            this.colIsTraceFlagRequired.ReadOnly = true;
            this.colIsTraceFlagRequired.ToolTipText = "SQL 2016 and later don\'t require T1118 & T1117";
            this.colIsTraceFlagRequired.Width = 160;
            // 
            // colTempDBMemoryOpt
            // 
            this.colTempDBMemoryOpt.DataPropertyName = "IsTempDBMetadataMemoryOptimized";
            dataGridViewCellStyle6.NullValue = "N/A";
            this.colTempDBMemoryOpt.DefaultCellStyle = dataGridViewCellStyle6;
            this.colTempDBMemoryOpt.HeaderText = "Memory Optimized TempDB?";
            this.colTempDBMemoryOpt.MinimumWidth = 6;
            this.colTempDBMemoryOpt.Name = "colTempDBMemoryOpt";
            this.colTempDBMemoryOpt.ReadOnly = true;
            this.colTempDBMemoryOpt.ToolTipText = "Valid from SQL 2019";
            this.colTempDBMemoryOpt.Width = 201;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(959, 31);
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
            this.dataGridViewTextBoxColumn2.DataPropertyName = "NumberOfDataFiles";
            this.dataGridViewTextBoxColumn2.HeaderText = "#Data Files";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 108;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "MinimumRecommendedFiles";
            this.dataGridViewTextBoxColumn3.HeaderText = "Recommended Files (Minimum)";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 154;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "NumberOfLogFiles";
            this.dataGridViewTextBoxColumn4.HeaderText = "#Log Files";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 94;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "IsEvenlySized";
            this.dataGridViewTextBoxColumn5.HeaderText = "Even Sized?";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 107;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "IsEvenGrowth";
            this.dataGridViewTextBoxColumn6.HeaderText = "Even Growth";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 109;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "TotalSizeMB";
            this.dataGridViewTextBoxColumn7.HeaderText = "Total Size (MB)";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 123;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "LogMB";
            this.dataGridViewTextBoxColumn8.HeaderText = "Log MB";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 61;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.DataPropertyName = "FileSizeMB";
            this.dataGridViewTextBoxColumn9.HeaderText = "File Size (MB)";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 114;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.DataPropertyName = "MaxGrowthMB";
            this.dataGridViewTextBoxColumn10.HeaderText = "Max Growth (MB)";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 133;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.DataPropertyName = "MaxLogGrowthMB";
            this.dataGridViewTextBoxColumn11.HeaderText = "Max Log Growth (MB)";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 131;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.DataPropertyName = "MaxGrowthPct";
            this.dataGridViewTextBoxColumn12.HeaderText = "Max Growth %";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 106;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.DataPropertyName = "MaxLogGrowthPct";
            this.dataGridViewTextBoxColumn13.HeaderText = "Max Log Growth %";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 127;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.DataPropertyName = "TempDBVolumes";
            this.dataGridViewTextBoxColumn14.HeaderText = "TempDB Volume(s)";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 146;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.DataPropertyName = "cpu_core_count";
            this.dataGridViewTextBoxColumn15.HeaderText = "CPU Core Count";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 128;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.DataPropertyName = "T1117";
            this.dataGridViewTextBoxColumn16.HeaderText = "T1117";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            this.dataGridViewTextBoxColumn16.Width = 78;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.DataPropertyName = "T1118";
            this.dataGridViewTextBoxColumn17.HeaderText = "T1118";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.ReadOnly = true;
            this.dataGridViewTextBoxColumn17.Width = 78;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.DataPropertyName = "IsTraceFlagRequired";
            this.dataGridViewTextBoxColumn18.HeaderText = "Trace Flag Required?";
            this.dataGridViewTextBoxColumn18.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            this.dataGridViewTextBoxColumn18.ReadOnly = true;
            this.dataGridViewTextBoxColumn18.Width = 160;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.DataPropertyName = "IsTempDBMetadataMemoryOptimized";
            dataGridViewCellStyle7.NullValue = "N/A";
            this.dataGridViewTextBoxColumn19.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridViewTextBoxColumn19.HeaderText = "Memory Optimized TempDB?";
            this.dataGridViewTextBoxColumn19.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            this.dataGridViewTextBoxColumn19.ReadOnly = true;
            this.dataGridViewTextBoxColumn19.Width = 201;
            // 
            // TempDBConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvTempDB);
            this.Controls.Add(this.toolStrip1);
            this.Name = "TempDBConfig";
            this.Size = new System.Drawing.Size(959, 461);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTempDB)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvTempDB;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumberOfDataFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInsufficientFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMinimumRecommendedFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumberOfLogFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEvenSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEvenGrowth;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileSizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxGrowthMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxLogGrowth;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxGrowthPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxLogGrowthPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTempDBVolumes;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCPUCores;
        private System.Windows.Forms.DataGridViewTextBoxColumn colT1117;
        private System.Windows.Forms.DataGridViewTextBoxColumn colT1118;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIsTraceFlagRequired;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTempDBMemoryOpt;
    }
}
