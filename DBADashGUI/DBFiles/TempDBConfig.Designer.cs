using DBADashGUI.CustomReports;

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
            dgvTempDB = new DBADashDataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colNumberOfDataFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInsufficientFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMinimumRecommendedFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colNumberOfLogFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEvenSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEvenGrowth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTotalSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colLogMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colFileSizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxGrowthMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxLogGrowth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxGrowthPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxLogGrowthPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTempDBVolumes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCPUCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colT1117 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colT1118 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colIsTraceFlagRequired = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTempDBMemoryOpt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
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
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvTempDB).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvTempDB
            // 
            dgvTempDB.AllowUserToAddRows = false;
            dgvTempDB.AllowUserToDeleteRows = false;
            dgvTempDB.BackgroundColor = System.Drawing.Color.White;
            dgvTempDB.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTempDB.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colNumberOfDataFiles, colInsufficientFiles, colMinimumRecommendedFiles, colNumberOfLogFiles, colEvenSize, colEvenGrowth, colTotalSize, colLogMB, colFileSizeMB, colMaxGrowthMB, colMaxLogGrowth, colMaxGrowthPct, colMaxLogGrowthPct, colTempDBVolumes, colCPUCores, colT1117, colT1118, colIsTraceFlagRequired, colTempDBMemoryOpt });
            dgvTempDB.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvTempDB.Location = new System.Drawing.Point(0, 27);
            dgvTempDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvTempDB.Name = "dgvTempDB";
            dgvTempDB.ReadOnly = true;
            dgvTempDB.RowHeadersVisible = false;
            dgvTempDB.RowHeadersWidth = 51;
            dgvTempDB.Size = new System.Drawing.Size(959, 549);
            dgvTempDB.TabIndex = 0;
            dgvTempDB.RowsAdded += DgvTempDB_RowsAdded;
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
            // colNumberOfDataFiles
            // 
            colNumberOfDataFiles.DataPropertyName = "NumberOfDataFiles";
            colNumberOfDataFiles.HeaderText = "#Data Files";
            colNumberOfDataFiles.MinimumWidth = 6;
            colNumberOfDataFiles.Name = "colNumberOfDataFiles";
            colNumberOfDataFiles.ReadOnly = true;
            colNumberOfDataFiles.Width = 125;
            // 
            // colInsufficientFiles
            // 
            colInsufficientFiles.DataPropertyName = "InsufficientFiles";
            colInsufficientFiles.HeaderText = "Insufficient Files";
            colInsufficientFiles.MinimumWidth = 6;
            colInsufficientFiles.Name = "colInsufficientFiles";
            colInsufficientFiles.ReadOnly = true;
            colInsufficientFiles.Width = 126;
            // 
            // colMinimumRecommendedFiles
            // 
            colMinimumRecommendedFiles.DataPropertyName = "MinimumRecommendedFiles";
            colMinimumRecommendedFiles.HeaderText = "Recommended Files (Minimum)";
            colMinimumRecommendedFiles.MinimumWidth = 6;
            colMinimumRecommendedFiles.Name = "colMinimumRecommendedFiles";
            colMinimumRecommendedFiles.ReadOnly = true;
            colMinimumRecommendedFiles.Width = 154;
            // 
            // colNumberOfLogFiles
            // 
            colNumberOfLogFiles.DataPropertyName = "NumberOfLogFiles";
            colNumberOfLogFiles.HeaderText = "#Log Files";
            colNumberOfLogFiles.MinimumWidth = 6;
            colNumberOfLogFiles.Name = "colNumberOfLogFiles";
            colNumberOfLogFiles.ReadOnly = true;
            colNumberOfLogFiles.Width = 94;
            // 
            // colEvenSize
            // 
            colEvenSize.DataPropertyName = "IsEvenlySized";
            colEvenSize.HeaderText = "Even Sized?";
            colEvenSize.MinimumWidth = 6;
            colEvenSize.Name = "colEvenSize";
            colEvenSize.ReadOnly = true;
            colEvenSize.ToolTipText = "Should be evenly sized";
            colEvenSize.Width = 107;
            // 
            // colEvenGrowth
            // 
            colEvenGrowth.DataPropertyName = "IsEvenGrowth";
            colEvenGrowth.HeaderText = "Even Growth";
            colEvenGrowth.MinimumWidth = 6;
            colEvenGrowth.Name = "colEvenGrowth";
            colEvenGrowth.ReadOnly = true;
            colEvenGrowth.ToolTipText = "Should be even growth";
            colEvenGrowth.Width = 109;
            // 
            // colTotalSize
            // 
            colTotalSize.DataPropertyName = "TotalSizeMB";
            dataGridViewCellStyle1.Format = "N0";
            colTotalSize.DefaultCellStyle = dataGridViewCellStyle1;
            colTotalSize.HeaderText = "Total Size (MB)";
            colTotalSize.MinimumWidth = 6;
            colTotalSize.Name = "colTotalSize";
            colTotalSize.ReadOnly = true;
            colTotalSize.Width = 123;
            // 
            // colLogMB
            // 
            colLogMB.DataPropertyName = "LogMB";
            dataGridViewCellStyle2.Format = "N0";
            colLogMB.DefaultCellStyle = dataGridViewCellStyle2;
            colLogMB.HeaderText = "Log MB";
            colLogMB.MinimumWidth = 6;
            colLogMB.Name = "colLogMB";
            colLogMB.ReadOnly = true;
            colLogMB.Width = 61;
            // 
            // colFileSizeMB
            // 
            colFileSizeMB.DataPropertyName = "FileSizeMB";
            dataGridViewCellStyle3.Format = "N0";
            colFileSizeMB.DefaultCellStyle = dataGridViewCellStyle3;
            colFileSizeMB.HeaderText = "File Size (MB)";
            colFileSizeMB.MinimumWidth = 6;
            colFileSizeMB.Name = "colFileSizeMB";
            colFileSizeMB.ReadOnly = true;
            colFileSizeMB.Width = 114;
            // 
            // colMaxGrowthMB
            // 
            colMaxGrowthMB.DataPropertyName = "MaxGrowthMB";
            dataGridViewCellStyle4.Format = "N0";
            colMaxGrowthMB.DefaultCellStyle = dataGridViewCellStyle4;
            colMaxGrowthMB.HeaderText = "Max Growth (MB)";
            colMaxGrowthMB.MinimumWidth = 6;
            colMaxGrowthMB.Name = "colMaxGrowthMB";
            colMaxGrowthMB.ReadOnly = true;
            colMaxGrowthMB.Width = 133;
            // 
            // colMaxLogGrowth
            // 
            colMaxLogGrowth.DataPropertyName = "MaxLogGrowthMB";
            dataGridViewCellStyle5.Format = "N0";
            colMaxLogGrowth.DefaultCellStyle = dataGridViewCellStyle5;
            colMaxLogGrowth.HeaderText = "Max Log Growth (MB)";
            colMaxLogGrowth.MinimumWidth = 6;
            colMaxLogGrowth.Name = "colMaxLogGrowth";
            colMaxLogGrowth.ReadOnly = true;
            colMaxLogGrowth.Width = 131;
            // 
            // colMaxGrowthPct
            // 
            colMaxGrowthPct.DataPropertyName = "MaxGrowthPct";
            colMaxGrowthPct.HeaderText = "Max Growth %";
            colMaxGrowthPct.MinimumWidth = 6;
            colMaxGrowthPct.Name = "colMaxGrowthPct";
            colMaxGrowthPct.ReadOnly = true;
            colMaxGrowthPct.Width = 106;
            // 
            // colMaxLogGrowthPct
            // 
            colMaxLogGrowthPct.DataPropertyName = "MaxLogGrowthPct";
            colMaxLogGrowthPct.HeaderText = "Max Log Growth %";
            colMaxLogGrowthPct.MinimumWidth = 6;
            colMaxLogGrowthPct.Name = "colMaxLogGrowthPct";
            colMaxLogGrowthPct.ReadOnly = true;
            colMaxLogGrowthPct.Width = 127;
            // 
            // colTempDBVolumes
            // 
            colTempDBVolumes.DataPropertyName = "TempDBVolumes";
            colTempDBVolumes.HeaderText = "TempDB Volume(s)";
            colTempDBVolumes.MinimumWidth = 6;
            colTempDBVolumes.Name = "colTempDBVolumes";
            colTempDBVolumes.ReadOnly = true;
            colTempDBVolumes.Width = 146;
            // 
            // colCPUCores
            // 
            colCPUCores.DataPropertyName = "cpu_core_count";
            colCPUCores.HeaderText = "CPU Core Count";
            colCPUCores.MinimumWidth = 6;
            colCPUCores.Name = "colCPUCores";
            colCPUCores.ReadOnly = true;
            colCPUCores.ToolTipText = "Up to 8 cores tempdb should match core count.  Then start at 8 files and add more if needed.";
            colCPUCores.Width = 128;
            // 
            // colT1117
            // 
            colT1117.DataPropertyName = "T1117";
            colT1117.HeaderText = "T1117";
            colT1117.MinimumWidth = 6;
            colT1117.Name = "colT1117";
            colT1117.ReadOnly = true;
            colT1117.ToolTipText = "Recommended trace flag prior to SQL 2016";
            colT1117.Width = 78;
            // 
            // colT1118
            // 
            colT1118.DataPropertyName = "T1118";
            colT1118.HeaderText = "T1118";
            colT1118.MinimumWidth = 6;
            colT1118.Name = "colT1118";
            colT1118.ReadOnly = true;
            colT1118.ToolTipText = "Recommended trace flag prior to SQL 2016";
            colT1118.Width = 78;
            // 
            // colIsTraceFlagRequired
            // 
            colIsTraceFlagRequired.DataPropertyName = "IsTraceFlagRequired";
            colIsTraceFlagRequired.HeaderText = "Trace Flag Required?";
            colIsTraceFlagRequired.MinimumWidth = 6;
            colIsTraceFlagRequired.Name = "colIsTraceFlagRequired";
            colIsTraceFlagRequired.ReadOnly = true;
            colIsTraceFlagRequired.ToolTipText = "SQL 2016 and later don't require T1118 & T1117";
            colIsTraceFlagRequired.Width = 160;
            // 
            // colTempDBMemoryOpt
            // 
            colTempDBMemoryOpt.DataPropertyName = "IsTempDBMetadataMemoryOptimized";
            dataGridViewCellStyle6.NullValue = "N/A";
            colTempDBMemoryOpt.DefaultCellStyle = dataGridViewCellStyle6;
            colTempDBMemoryOpt.HeaderText = "Memory Optimized TempDB?";
            colTempDBMemoryOpt.MinimumWidth = 6;
            colTempDBMemoryOpt.Name = "colTempDBMemoryOpt";
            colTempDBMemoryOpt.ReadOnly = true;
            colTempDBMemoryOpt.ToolTipText = "Valid from SQL 2019";
            colTempDBMemoryOpt.Width = 201;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(959, 27);
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
            dataGridViewTextBoxColumn2.DataPropertyName = "NumberOfDataFiles";
            dataGridViewTextBoxColumn2.HeaderText = "#Data Files";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 108;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "MinimumRecommendedFiles";
            dataGridViewTextBoxColumn3.HeaderText = "Recommended Files (Minimum)";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 154;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "NumberOfLogFiles";
            dataGridViewTextBoxColumn4.HeaderText = "#Log Files";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 94;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "IsEvenlySized";
            dataGridViewTextBoxColumn5.HeaderText = "Even Sized?";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 107;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "IsEvenGrowth";
            dataGridViewTextBoxColumn6.HeaderText = "Even Growth";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 109;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "TotalSizeMB";
            dataGridViewTextBoxColumn7.HeaderText = "Total Size (MB)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 123;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "LogMB";
            dataGridViewTextBoxColumn8.HeaderText = "Log MB";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 61;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "FileSizeMB";
            dataGridViewTextBoxColumn9.HeaderText = "File Size (MB)";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 114;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "MaxGrowthMB";
            dataGridViewTextBoxColumn10.HeaderText = "Max Growth (MB)";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 133;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "MaxLogGrowthMB";
            dataGridViewTextBoxColumn11.HeaderText = "Max Log Growth (MB)";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 131;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "MaxGrowthPct";
            dataGridViewTextBoxColumn12.HeaderText = "Max Growth %";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 106;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "MaxLogGrowthPct";
            dataGridViewTextBoxColumn13.HeaderText = "Max Log Growth %";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 127;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "TempDBVolumes";
            dataGridViewTextBoxColumn14.HeaderText = "TempDB Volume(s)";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 146;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "cpu_core_count";
            dataGridViewTextBoxColumn15.HeaderText = "CPU Core Count";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 128;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "T1117";
            dataGridViewTextBoxColumn16.HeaderText = "T1117";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 78;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "T1118";
            dataGridViewTextBoxColumn17.HeaderText = "T1118";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            dataGridViewTextBoxColumn17.Width = 78;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "IsTraceFlagRequired";
            dataGridViewTextBoxColumn18.HeaderText = "Trace Flag Required?";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            dataGridViewTextBoxColumn18.Width = 160;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "IsTempDBMetadataMemoryOptimized";
            dataGridViewCellStyle7.NullValue = "N/A";
            dataGridViewTextBoxColumn19.DefaultCellStyle = dataGridViewCellStyle7;
            dataGridViewTextBoxColumn19.HeaderText = "Memory Optimized TempDB?";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            dataGridViewTextBoxColumn19.Width = 201;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // TempDBConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvTempDB);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "TempDBConfig";
            Size = new System.Drawing.Size(959, 576);
            ((System.ComponentModel.ISupportInitialize)dgvTempDB).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvTempDB;
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
        private System.Windows.Forms.ToolStripButton tsExcel;
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
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
