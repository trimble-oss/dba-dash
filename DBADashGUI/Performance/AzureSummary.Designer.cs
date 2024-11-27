using DBADashGUI.CustomReports;

namespace DBADashGUI.Performance
{
    partial class AzureSummary
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle33 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle34 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle35 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle36 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle37 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle38 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle39 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle40 = new System.Windows.Forms.DataGridViewCellStyle();
            dgv = new DBADashDataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDB = new System.Windows.Forms.DataGridViewLinkColumn();
            colEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colServiceObjective = new System.Windows.Forms.DataGridViewLinkColumn();
            colElasticPool = new System.Windows.Forms.DataGridViewLinkColumn();
            dgv_DTUHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgv_CPUHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgv_DataHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgv_LogHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            colMaxDTU = new CustomProgressControl.DataGridViewProgressBarColumn();
            colMaxDTUUsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colUnusedDTUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAvgDTUPercent = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAVGDTUsUsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMinDTULimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDTULimitMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxCPUPercent = new CustomProgressControl.DataGridViewProgressBarColumn();
            colMaxDataPct = new CustomProgressControl.DataGridViewProgressBarColumn();
            colMaxLog = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAvgCPUPct = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAvgDataPct = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAvgLogWrite = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAllocatedSpaceMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colUsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxStorageSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAllocatedPctMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colUsedPctMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colFileSnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewProgressBarColumn1 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewProgressBarColumn2 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewProgressBarColumn3 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn4 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn5 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn6 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn7 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn8 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dgvPool = new DBADashDataGridView();
            poolInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolName = new System.Windows.Forms.DataGridViewLinkColumn();
            dgvPool_DTUHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgvPool_CPUHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgvPool_DataHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            dgvPool_LogHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            poolMaxDTUPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolMaxDTU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolUnusedDTU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolAVGDTUPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolAVGDTU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolMinDTULimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolDTULimitMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCPULimitMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCPULimitMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            poolMaxCPU = new CustomProgressControl.DataGridViewProgressBarColumn();
            poolMaxDataPct = new CustomProgressControl.DataGridViewProgressBarColumn();
            poolMaxLogWrite = new CustomProgressControl.DataGridViewProgressBarColumn();
            poolAvgCPU = new CustomProgressControl.DataGridViewProgressBarColumn();
            poolAvgData = new CustomProgressControl.DataGridViewProgressBarColumn();
            poolAvgLogWrite = new CustomProgressControl.DataGridViewProgressBarColumn();
            colAllocatedStoragePct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colStorageLimit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCurrentUsedGB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCurrentFreeGB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsRefreshPool = new System.Windows.Forms.ToolStripButton();
            tsCopyPool = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsExcelPool = new System.Windows.Forms.ToolStripButton();
            tsPoolCols = new System.Windows.Forms.ToolStripButton();
            tsClearFilterPool = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPool).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colDB, colEdition, colServiceObjective, colElasticPool, dgv_DTUHistogram, dgv_CPUHistogram, dgv_DataHistogram, dgv_LogHistogram, colMaxDTU, colMaxDTUUsed, colUnusedDTUs, colAvgDTUPercent, colAVGDTUsUsed, colMinDTULimit, colDTULimitMax, colMaxCPUPercent, colMaxDataPct, colMaxLog, colAvgCPUPct, colAvgDataPct, colAvgLogWrite, colAllocatedSpaceMB, colUsedMB, colMaxStorageSize, colAllocatedPctMax, colUsedPctMax, colFileSnapshotAge });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1455, 749);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            dgv.Sorted += Dgv_Sorted;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "Instance";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colInstance.Width = 90;
            // 
            // colDB
            // 
            colDB.DataPropertyName = "DB";
            colDB.HeaderText = "DB";
            colDB.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colDB.MinimumWidth = 6;
            colDB.Name = "colDB";
            colDB.ReadOnly = true;
            colDB.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colDB.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colDB.Width = 56;
            // 
            // colEdition
            // 
            colEdition.DataPropertyName = "edition";
            colEdition.HeaderText = "Edition";
            colEdition.MinimumWidth = 6;
            colEdition.Name = "colEdition";
            colEdition.ReadOnly = true;
            colEdition.Width = 80;
            // 
            // colServiceObjective
            // 
            colServiceObjective.DataPropertyName = "service_objective";
            colServiceObjective.HeaderText = "Service Objective";
            colServiceObjective.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colServiceObjective.MinimumWidth = 6;
            colServiceObjective.Name = "colServiceObjective";
            colServiceObjective.ReadOnly = true;
            colServiceObjective.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colServiceObjective.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colServiceObjective.Width = 135;
            // 
            // colElasticPool
            // 
            colElasticPool.DataPropertyName = "elastic_pool_name";
            colElasticPool.HeaderText = "Elastic Pool";
            colElasticPool.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colElasticPool.MinimumWidth = 6;
            colElasticPool.Name = "colElasticPool";
            colElasticPool.ReadOnly = true;
            colElasticPool.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colElasticPool.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colElasticPool.Width = 101;
            // 
            // dgv_DTUHistogram
            // 
            dgv_DTUHistogram.HeaderText = "DTU Histogram";
            dgv_DTUHistogram.MinimumWidth = 6;
            dgv_DTUHistogram.Name = "dgv_DTUHistogram";
            dgv_DTUHistogram.ReadOnly = true;
            dgv_DTUHistogram.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dgv_DTUHistogram.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            dgv_DTUHistogram.Visible = false;
            dgv_DTUHistogram.Width = 123;
            // 
            // dgv_CPUHistogram
            // 
            dgv_CPUHistogram.HeaderText = "CPU Histogram";
            dgv_CPUHistogram.MinimumWidth = 6;
            dgv_CPUHistogram.Name = "dgv_CPUHistogram";
            dgv_CPUHistogram.ReadOnly = true;
            dgv_CPUHistogram.Visible = false;
            dgv_CPUHistogram.Width = 99;
            // 
            // dgv_DataHistogram
            // 
            dgv_DataHistogram.HeaderText = "Data Histogram";
            dgv_DataHistogram.MinimumWidth = 6;
            dgv_DataHistogram.Name = "dgv_DataHistogram";
            dgv_DataHistogram.ReadOnly = true;
            dgv_DataHistogram.Visible = false;
            dgv_DataHistogram.Width = 101;
            // 
            // dgv_LogHistogram
            // 
            dgv_LogHistogram.HeaderText = "Log Write Histogram";
            dgv_LogHistogram.MinimumWidth = 6;
            dgv_LogHistogram.Name = "dgv_LogHistogram";
            dgv_LogHistogram.ReadOnly = true;
            dgv_LogHistogram.Visible = false;
            dgv_LogHistogram.Width = 129;
            // 
            // colMaxDTU
            // 
            colMaxDTU.DataPropertyName = "Max_DTUPercent";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "0.#\\%";
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(4);
            colMaxDTU.DefaultCellStyle = dataGridViewCellStyle2;
            colMaxDTU.HeaderText = "Max DTU (%)";
            colMaxDTU.MinimumWidth = 6;
            colMaxDTU.Name = "colMaxDTU";
            colMaxDTU.ReadOnly = true;
            colMaxDTU.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colMaxDTU.Width = 111;
            // 
            // colMaxDTUUsed
            // 
            colMaxDTUUsed.DataPropertyName = "Max_DTUsUsed";
            dataGridViewCellStyle3.Format = "N0";
            colMaxDTUUsed.DefaultCellStyle = dataGridViewCellStyle3;
            colMaxDTUUsed.HeaderText = "Max DTUs Used";
            colMaxDTUUsed.MinimumWidth = 6;
            colMaxDTUUsed.Name = "colMaxDTUUsed";
            colMaxDTUUsed.ReadOnly = true;
            colMaxDTUUsed.Width = 127;
            // 
            // colUnusedDTUs
            // 
            colUnusedDTUs.DataPropertyName = "UnusedDTU";
            dataGridViewCellStyle4.Format = "N0";
            colUnusedDTUs.DefaultCellStyle = dataGridViewCellStyle4;
            colUnusedDTUs.HeaderText = "Unused DTUs";
            colUnusedDTUs.MinimumWidth = 6;
            colUnusedDTUs.Name = "colUnusedDTUs";
            colUnusedDTUs.ReadOnly = true;
            colUnusedDTUs.Width = 116;
            // 
            // colAvgDTUPercent
            // 
            colAvgDTUPercent.DataPropertyName = "Avg_DTUPercent";
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle5.Format = "0.#\\%";
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(4);
            colAvgDTUPercent.DefaultCellStyle = dataGridViewCellStyle5;
            colAvgDTUPercent.HeaderText = "Avg DTU %";
            colAvgDTUPercent.MinimumWidth = 6;
            colAvgDTUPercent.Name = "colAvgDTUPercent";
            colAvgDTUPercent.ReadOnly = true;
            colAvgDTUPercent.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colAvgDTUPercent.Width = 91;
            // 
            // colAVGDTUsUsed
            // 
            colAVGDTUsUsed.DataPropertyName = "Avg_DTUsUsed";
            dataGridViewCellStyle6.Format = "N0";
            colAVGDTUsUsed.DefaultCellStyle = dataGridViewCellStyle6;
            colAVGDTUsUsed.HeaderText = "Avg DTUs Used";
            colAVGDTUsUsed.MinimumWidth = 6;
            colAVGDTUsUsed.Name = "colAVGDTUsUsed";
            colAVGDTUsUsed.ReadOnly = true;
            colAVGDTUsUsed.Width = 127;
            // 
            // colMinDTULimit
            // 
            colMinDTULimit.DataPropertyName = "Min_DTULimit";
            dataGridViewCellStyle7.Format = "N0";
            colMinDTULimit.DefaultCellStyle = dataGridViewCellStyle7;
            colMinDTULimit.HeaderText = "DTU Limit (Min)";
            colMinDTULimit.MinimumWidth = 6;
            colMinDTULimit.Name = "colMinDTULimit";
            colMinDTULimit.ReadOnly = true;
            colMinDTULimit.Width = 124;
            // 
            // colDTULimitMax
            // 
            colDTULimitMax.DataPropertyName = "Max_DTULimit";
            dataGridViewCellStyle8.Format = "N0";
            colDTULimitMax.DefaultCellStyle = dataGridViewCellStyle8;
            colDTULimitMax.HeaderText = "DTU Limit (Max)";
            colDTULimitMax.MinimumWidth = 6;
            colDTULimitMax.Name = "colDTULimitMax";
            colDTULimitMax.ReadOnly = true;
            colDTULimitMax.Width = 127;
            // 
            // colMaxCPUPercent
            // 
            colMaxCPUPercent.DataPropertyName = "MaxCPUPercent";
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle9.Format = "0.#\\%";
            dataGridViewCellStyle9.Padding = new System.Windows.Forms.Padding(4);
            colMaxCPUPercent.DefaultCellStyle = dataGridViewCellStyle9;
            colMaxCPUPercent.HeaderText = "Max CPU %";
            colMaxCPUPercent.MinimumWidth = 6;
            colMaxCPUPercent.Name = "colMaxCPUPercent";
            colMaxCPUPercent.ReadOnly = true;
            colMaxCPUPercent.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colMaxCPUPercent.Width = 91;
            // 
            // colMaxDataPct
            // 
            colMaxDataPct.DataPropertyName = "MaxDataPercent";
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle10.Format = "0.#\\%";
            dataGridViewCellStyle10.Padding = new System.Windows.Forms.Padding(4);
            colMaxDataPct.DefaultCellStyle = dataGridViewCellStyle10;
            colMaxDataPct.HeaderText = "Max Data %";
            colMaxDataPct.MinimumWidth = 6;
            colMaxDataPct.Name = "colMaxDataPct";
            colMaxDataPct.ReadOnly = true;
            colMaxDataPct.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colMaxDataPct.Width = 92;
            // 
            // colMaxLog
            // 
            colMaxLog.DataPropertyName = "MaxLogWritePercent";
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle11.Format = "0.#\\%";
            dataGridViewCellStyle11.Padding = new System.Windows.Forms.Padding(4);
            colMaxLog.DefaultCellStyle = dataGridViewCellStyle11;
            colMaxLog.HeaderText = "Max Log Write %";
            colMaxLog.MinimumWidth = 6;
            colMaxLog.Name = "colMaxLog";
            colMaxLog.ReadOnly = true;
            colMaxLog.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colMaxLog.Width = 120;
            // 
            // colAvgCPUPct
            // 
            colAvgCPUPct.DataPropertyName = "AvgCPUPercent";
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle12.Format = "0.#\\%";
            dataGridViewCellStyle12.Padding = new System.Windows.Forms.Padding(4);
            colAvgCPUPct.DefaultCellStyle = dataGridViewCellStyle12;
            colAvgCPUPct.HeaderText = "Avg CPU %";
            colAvgCPUPct.MinimumWidth = 6;
            colAvgCPUPct.Name = "colAvgCPUPct";
            colAvgCPUPct.ReadOnly = true;
            colAvgCPUPct.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colAvgCPUPct.Width = 90;
            // 
            // colAvgDataPct
            // 
            colAvgDataPct.DataPropertyName = "AvgDataPercent";
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle13.Format = "0.#\\%";
            dataGridViewCellStyle13.Padding = new System.Windows.Forms.Padding(4);
            colAvgDataPct.DefaultCellStyle = dataGridViewCellStyle13;
            colAvgDataPct.HeaderText = "Avg Data %";
            colAvgDataPct.MinimumWidth = 6;
            colAvgDataPct.Name = "colAvgDataPct";
            colAvgDataPct.ReadOnly = true;
            colAvgDataPct.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colAvgDataPct.Width = 91;
            // 
            // colAvgLogWrite
            // 
            colAvgLogWrite.DataPropertyName = "AvgLogWritePercent";
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle14.Format = "0.#\\%";
            dataGridViewCellStyle14.Padding = new System.Windows.Forms.Padding(4);
            colAvgLogWrite.DefaultCellStyle = dataGridViewCellStyle14;
            colAvgLogWrite.HeaderText = "Avg Log Write %";
            colAvgLogWrite.MinimumWidth = 6;
            colAvgLogWrite.Name = "colAvgLogWrite";
            colAvgLogWrite.ReadOnly = true;
            colAvgLogWrite.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colAvgLogWrite.Width = 119;
            // 
            // colAllocatedSpaceMB
            // 
            colAllocatedSpaceMB.DataPropertyName = "AllocatedSpaceMB";
            dataGridViewCellStyle15.Format = "N0";
            colAllocatedSpaceMB.DefaultCellStyle = dataGridViewCellStyle15;
            colAllocatedSpaceMB.HeaderText = "Allocated Space MB";
            colAllocatedSpaceMB.MinimumWidth = 6;
            colAllocatedSpaceMB.Name = "colAllocatedSpaceMB";
            colAllocatedSpaceMB.ReadOnly = true;
            colAllocatedSpaceMB.Visible = false;
            colAllocatedSpaceMB.Width = 125;
            // 
            // colUsedMB
            // 
            colUsedMB.DataPropertyName = "UsedSpaceMB";
            dataGridViewCellStyle16.Format = "N0";
            colUsedMB.DefaultCellStyle = dataGridViewCellStyle16;
            colUsedMB.HeaderText = "Used MB";
            colUsedMB.MinimumWidth = 6;
            colUsedMB.Name = "colUsedMB";
            colUsedMB.ReadOnly = true;
            colUsedMB.Visible = false;
            colUsedMB.Width = 125;
            // 
            // colMaxStorageSize
            // 
            colMaxStorageSize.DataPropertyName = "MaxStorageSizeMB";
            dataGridViewCellStyle17.Format = "N0";
            colMaxStorageSize.DefaultCellStyle = dataGridViewCellStyle17;
            colMaxStorageSize.HeaderText = "Max Storage Size MB";
            colMaxStorageSize.MinimumWidth = 6;
            colMaxStorageSize.Name = "colMaxStorageSize";
            colMaxStorageSize.ReadOnly = true;
            colMaxStorageSize.Visible = false;
            colMaxStorageSize.Width = 125;
            // 
            // colAllocatedPctMax
            // 
            colAllocatedPctMax.DataPropertyName = "AllocatedPctOfMaxSize";
            dataGridViewCellStyle18.Format = "P1";
            colAllocatedPctMax.DefaultCellStyle = dataGridViewCellStyle18;
            colAllocatedPctMax.HeaderText = "Allocated % of Max Size";
            colAllocatedPctMax.MinimumWidth = 6;
            colAllocatedPctMax.Name = "colAllocatedPctMax";
            colAllocatedPctMax.ReadOnly = true;
            colAllocatedPctMax.Width = 125;
            // 
            // colUsedPctMax
            // 
            colUsedPctMax.DataPropertyName = "UsedPctOfMaxSize";
            dataGridViewCellStyle19.Format = "P1";
            colUsedPctMax.DefaultCellStyle = dataGridViewCellStyle19;
            colUsedPctMax.HeaderText = "Used % Max Size";
            colUsedPctMax.MinimumWidth = 6;
            colUsedPctMax.Name = "colUsedPctMax";
            colUsedPctMax.ReadOnly = true;
            colUsedPctMax.Width = 125;
            // 
            // colFileSnapshotAge
            // 
            colFileSnapshotAge.DataPropertyName = "FileSnapshotAge";
            colFileSnapshotAge.HeaderText = "File Snapshot Age";
            colFileSnapshotAge.MinimumWidth = 6;
            colFileSnapshotAge.Name = "colFileSnapshotAge";
            colFileSnapshotAge.ReadOnly = true;
            colFileSnapshotAge.Width = 125;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, toolStripLabel2, tsCols, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1455, 31);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 28);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 28);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 28);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(75, 28);
            toolStripLabel2.Text = "Azure DB";
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 28);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 28);
            tsClearFilter.Text = "Clear Filter";
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "DB";
            dataGridViewTextBoxColumn2.HeaderText = "DB";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "edition";
            dataGridViewTextBoxColumn3.HeaderText = "Edition";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "service_objective";
            dataGridViewTextBoxColumn4.HeaderText = "Service Objective";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "elastic_pool_name";
            dataGridViewTextBoxColumn5.HeaderText = "Elastic Pool";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewProgressBarColumn1
            // 
            dataGridViewProgressBarColumn1.DataPropertyName = "Max_DTUPercent";
            dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle20.Format = "P1";
            dataGridViewCellStyle20.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn1.DefaultCellStyle = dataGridViewCellStyle20;
            dataGridViewProgressBarColumn1.HeaderText = "Max DPU (%)";
            dataGridViewProgressBarColumn1.MinimumWidth = 6;
            dataGridViewProgressBarColumn1.Name = "dataGridViewProgressBarColumn1";
            dataGridViewProgressBarColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "Max_DTUPercent";
            dataGridViewTextBoxColumn6.HeaderText = "Max DPU (%)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "Max_DTUsUsed";
            dataGridViewTextBoxColumn7.HeaderText = "Max DTUs Used";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewProgressBarColumn2
            // 
            dataGridViewProgressBarColumn2.DataPropertyName = "Avg_DTUPercent";
            dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle21.Format = "P1";
            dataGridViewCellStyle21.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn2.DefaultCellStyle = dataGridViewCellStyle21;
            dataGridViewProgressBarColumn2.HeaderText = "Avg DTU %";
            dataGridViewProgressBarColumn2.MinimumWidth = 6;
            dataGridViewProgressBarColumn2.Name = "dataGridViewProgressBarColumn2";
            dataGridViewProgressBarColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "UnusedDTU";
            dataGridViewTextBoxColumn8.HeaderText = "Unused DTUs";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "Avg_DTUPercent";
            dataGridViewTextBoxColumn9.HeaderText = "Avg DTU %";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "Avg_DTUsUsed";
            dataGridViewTextBoxColumn10.HeaderText = "AVG DTUs Used";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewProgressBarColumn3
            // 
            dataGridViewProgressBarColumn3.DataPropertyName = "MaxCPUPercent";
            dataGridViewCellStyle22.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle22.Format = "P1";
            dataGridViewCellStyle22.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn3.DefaultCellStyle = dataGridViewCellStyle22;
            dataGridViewProgressBarColumn3.HeaderText = "Max CPU %";
            dataGridViewProgressBarColumn3.MinimumWidth = 6;
            dataGridViewProgressBarColumn3.Name = "dataGridViewProgressBarColumn3";
            dataGridViewProgressBarColumn3.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn3.Width = 125;
            // 
            // dataGridViewProgressBarColumn4
            // 
            dataGridViewProgressBarColumn4.DataPropertyName = "MaxDataPercent";
            dataGridViewCellStyle23.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle23.Format = "P1";
            dataGridViewCellStyle23.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn4.DefaultCellStyle = dataGridViewCellStyle23;
            dataGridViewProgressBarColumn4.HeaderText = "Max Data %";
            dataGridViewProgressBarColumn4.MinimumWidth = 6;
            dataGridViewProgressBarColumn4.Name = "dataGridViewProgressBarColumn4";
            dataGridViewProgressBarColumn4.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn4.Width = 125;
            // 
            // dataGridViewProgressBarColumn5
            // 
            dataGridViewProgressBarColumn5.DataPropertyName = "MaxLogWritePercent";
            dataGridViewCellStyle24.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle24.Format = "P1";
            dataGridViewCellStyle24.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn5.DefaultCellStyle = dataGridViewCellStyle24;
            dataGridViewProgressBarColumn5.HeaderText = "Max Log Write %";
            dataGridViewProgressBarColumn5.MinimumWidth = 6;
            dataGridViewProgressBarColumn5.Name = "dataGridViewProgressBarColumn5";
            dataGridViewProgressBarColumn5.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn5.Width = 125;
            // 
            // dataGridViewProgressBarColumn6
            // 
            dataGridViewProgressBarColumn6.DataPropertyName = "AvgCPUPercent";
            dataGridViewCellStyle25.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle25.Format = "P1";
            dataGridViewCellStyle25.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn6.DefaultCellStyle = dataGridViewCellStyle25;
            dataGridViewProgressBarColumn6.HeaderText = "Avg CPU %";
            dataGridViewProgressBarColumn6.MinimumWidth = 6;
            dataGridViewProgressBarColumn6.Name = "dataGridViewProgressBarColumn6";
            dataGridViewProgressBarColumn6.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn6.Width = 125;
            // 
            // dataGridViewProgressBarColumn7
            // 
            dataGridViewProgressBarColumn7.DataPropertyName = "AvgDataPercent";
            dataGridViewCellStyle26.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle26.Format = "P1";
            dataGridViewCellStyle26.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn7.DefaultCellStyle = dataGridViewCellStyle26;
            dataGridViewProgressBarColumn7.HeaderText = "Avg Data %";
            dataGridViewProgressBarColumn7.MinimumWidth = 6;
            dataGridViewProgressBarColumn7.Name = "dataGridViewProgressBarColumn7";
            dataGridViewProgressBarColumn7.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn7.Width = 125;
            // 
            // dataGridViewProgressBarColumn8
            // 
            dataGridViewProgressBarColumn8.DataPropertyName = "AvgLogWritePercent";
            dataGridViewCellStyle27.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle27.Format = "P1";
            dataGridViewCellStyle27.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewProgressBarColumn8.DefaultCellStyle = dataGridViewCellStyle27;
            dataGridViewProgressBarColumn8.HeaderText = "Avg Log Write %";
            dataGridViewProgressBarColumn8.MinimumWidth = 6;
            dataGridViewProgressBarColumn8.Name = "dataGridViewProgressBarColumn8";
            dataGridViewProgressBarColumn8.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "Min_DTULimit";
            dataGridViewTextBoxColumn11.HeaderText = "DTU Limit (Min)";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "Max_DTULimit";
            dataGridViewTextBoxColumn12.HeaderText = "DTU Limit (Max)";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 125;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "MaxCPUPercent";
            dataGridViewTextBoxColumn13.HeaderText = "Max CPU %";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 125;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "MaxDataPercent";
            dataGridViewTextBoxColumn14.HeaderText = "Max Data %";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 125;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "MaxLogWritePercent";
            dataGridViewTextBoxColumn15.HeaderText = "Max Log Write %";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 125;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "AvgCPUPercent";
            dataGridViewTextBoxColumn16.HeaderText = "Avg CPU %";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 125;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "AvgDataPercent";
            dataGridViewTextBoxColumn17.HeaderText = "Avg Data %";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            dataGridViewTextBoxColumn17.Width = 125;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "AvgLogWritePercent";
            dataGridViewTextBoxColumn18.HeaderText = "Avg Log Write %";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            dataGridViewTextBoxColumn18.Width = 125;
            // 
            // dgvPool
            // 
            dgvPool.AllowUserToAddRows = false;
            dgvPool.AllowUserToDeleteRows = false;
            dgvPool.AllowUserToOrderColumns = true;
            dgvPool.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle28.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle28.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle28.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle28.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle28.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle28.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle28.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvPool.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle28;
            dgvPool.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPool.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { poolInstance, colPoolName, dgvPool_DTUHistogram, dgvPool_CPUHistogram, dgvPool_DataHistogram, dgvPool_LogHistogram, poolMaxDTUPct, poolMaxDTU, poolUnusedDTU, poolAVGDTUPct, poolAVGDTU, poolMinDTULimit, poolDTULimitMax, colCPULimitMin, colCPULimitMax, poolMaxCPU, poolMaxDataPct, poolMaxLogWrite, poolAvgCPU, poolAvgData, poolAvgLogWrite, colAllocatedStoragePct, colStorageLimit, colCurrentUsedGB, colCurrentFreeGB });
            dgvPool.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvPool.EnableHeadersVisualStyles = false;
            dgvPool.Location = new System.Drawing.Point(0, 31);
            dgvPool.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvPool.Name = "dgvPool";
            dgvPool.ReadOnly = true;
            dgvPool.ResultSetID = 0;
            dgvPool.ResultSetName = null;
            dgvPool.RowHeadersVisible = false;
            dgvPool.RowHeadersWidth = 51;
            dgvPool.Size = new System.Drawing.Size(1455, 435);
            dgvPool.TabIndex = 5;
            dgvPool.CellContentClick += DgvPool_CellContentClick;
            dgvPool.RowsAdded += DgvPol_RowsAdded;
            dgvPool.Sorted += DgvPool_Sorted;
            // 
            // poolInstance
            // 
            poolInstance.DataPropertyName = "Instance";
            poolInstance.HeaderText = "Instance";
            poolInstance.MinimumWidth = 6;
            poolInstance.Name = "poolInstance";
            poolInstance.ReadOnly = true;
            poolInstance.Width = 90;
            // 
            // colPoolName
            // 
            colPoolName.DataPropertyName = "elastic_pool_name";
            colPoolName.HeaderText = "Elastic Pool";
            colPoolName.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colPoolName.MinimumWidth = 6;
            colPoolName.Name = "colPoolName";
            colPoolName.ReadOnly = true;
            colPoolName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colPoolName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colPoolName.Width = 101;
            // 
            // dgvPool_DTUHistogram
            // 
            dgvPool_DTUHistogram.HeaderText = "DTU Histogram";
            dgvPool_DTUHistogram.MinimumWidth = 6;
            dgvPool_DTUHistogram.Name = "dgvPool_DTUHistogram";
            dgvPool_DTUHistogram.ReadOnly = true;
            dgvPool_DTUHistogram.Visible = false;
            dgvPool_DTUHistogram.Width = 111;
            // 
            // dgvPool_CPUHistogram
            // 
            dgvPool_CPUHistogram.HeaderText = "CPU Histogram";
            dgvPool_CPUHistogram.MinimumWidth = 6;
            dgvPool_CPUHistogram.Name = "dgvPool_CPUHistogram";
            dgvPool_CPUHistogram.ReadOnly = true;
            dgvPool_CPUHistogram.Visible = false;
            dgvPool_CPUHistogram.Width = 110;
            // 
            // dgvPool_DataHistogram
            // 
            dgvPool_DataHistogram.HeaderText = "Data Histogram";
            dgvPool_DataHistogram.MinimumWidth = 6;
            dgvPool_DataHistogram.Name = "dgvPool_DataHistogram";
            dgvPool_DataHistogram.ReadOnly = true;
            dgvPool_DataHistogram.Visible = false;
            dgvPool_DataHistogram.Width = 112;
            // 
            // dgvPool_LogHistogram
            // 
            dgvPool_LogHistogram.HeaderText = "Log Histogram";
            dgvPool_LogHistogram.MinimumWidth = 6;
            dgvPool_LogHistogram.Name = "dgvPool_LogHistogram";
            dgvPool_LogHistogram.ReadOnly = true;
            dgvPool_LogHistogram.Visible = false;
            dgvPool_LogHistogram.Width = 106;
            // 
            // poolMaxDTUPct
            // 
            poolMaxDTUPct.DataPropertyName = "Max_DTUPercent";
            poolMaxDTUPct.HeaderText = "Max DTU %";
            poolMaxDTUPct.MinimumWidth = 6;
            poolMaxDTUPct.Name = "poolMaxDTUPct";
            poolMaxDTUPct.ReadOnly = true;
            poolMaxDTUPct.Width = 91;
            // 
            // poolMaxDTU
            // 
            poolMaxDTU.DataPropertyName = "Max_DTUsUsed";
            poolMaxDTU.HeaderText = "Max DTUs Used";
            poolMaxDTU.MinimumWidth = 6;
            poolMaxDTU.Name = "poolMaxDTU";
            poolMaxDTU.ReadOnly = true;
            poolMaxDTU.Width = 127;
            // 
            // poolUnusedDTU
            // 
            poolUnusedDTU.DataPropertyName = "UnusedDTU";
            poolUnusedDTU.HeaderText = "Unused DTUs";
            poolUnusedDTU.MinimumWidth = 6;
            poolUnusedDTU.Name = "poolUnusedDTU";
            poolUnusedDTU.ReadOnly = true;
            poolUnusedDTU.Width = 116;
            // 
            // poolAVGDTUPct
            // 
            poolAVGDTUPct.DataPropertyName = "Avg_DTUPercent";
            poolAVGDTUPct.HeaderText = "Avg DTU %";
            poolAVGDTUPct.MinimumWidth = 6;
            poolAVGDTUPct.Name = "poolAVGDTUPct";
            poolAVGDTUPct.ReadOnly = true;
            poolAVGDTUPct.Width = 91;
            // 
            // poolAVGDTU
            // 
            poolAVGDTU.DataPropertyName = "Avg_DTUsUsed";
            poolAVGDTU.HeaderText = "Avg DTUs Used";
            poolAVGDTU.MinimumWidth = 6;
            poolAVGDTU.Name = "poolAVGDTU";
            poolAVGDTU.ReadOnly = true;
            poolAVGDTU.Width = 127;
            // 
            // poolMinDTULimit
            // 
            poolMinDTULimit.DataPropertyName = "Min_DTULimit";
            poolMinDTULimit.HeaderText = "DTU Limit (Min)";
            poolMinDTULimit.MinimumWidth = 6;
            poolMinDTULimit.Name = "poolMinDTULimit";
            poolMinDTULimit.ReadOnly = true;
            poolMinDTULimit.Visible = false;
            poolMinDTULimit.Width = 124;
            // 
            // poolDTULimitMax
            // 
            poolDTULimitMax.DataPropertyName = "Max_DTULimit";
            poolDTULimitMax.HeaderText = "DTU Limit (Max)";
            poolDTULimitMax.MinimumWidth = 6;
            poolDTULimitMax.Name = "poolDTULimitMax";
            poolDTULimitMax.ReadOnly = true;
            poolDTULimitMax.Width = 127;
            // 
            // colCPULimitMin
            // 
            colCPULimitMin.DataPropertyName = "MinCPULimit";
            dataGridViewCellStyle29.Format = "N2";
            colCPULimitMin.DefaultCellStyle = dataGridViewCellStyle29;
            colCPULimitMin.HeaderText = "CPU Limit (Min)";
            colCPULimitMin.MinimumWidth = 6;
            colCPULimitMin.Name = "colCPULimitMin";
            colCPULimitMin.ReadOnly = true;
            colCPULimitMin.Visible = false;
            colCPULimitMin.Width = 123;
            // 
            // colCPULimitMax
            // 
            colCPULimitMax.DataPropertyName = "MaxCPULimit";
            dataGridViewCellStyle30.Format = "N2";
            colCPULimitMax.DefaultCellStyle = dataGridViewCellStyle30;
            colCPULimitMax.HeaderText = "CPU Limit (Max)";
            colCPULimitMax.MinimumWidth = 6;
            colCPULimitMax.Name = "colCPULimitMax";
            colCPULimitMax.ReadOnly = true;
            colCPULimitMax.Width = 126;
            // 
            // poolMaxCPU
            // 
            poolMaxCPU.DataPropertyName = "MaxCPUPercent";
            dataGridViewCellStyle31.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle31.Format = "0.#\\%";
            dataGridViewCellStyle31.Padding = new System.Windows.Forms.Padding(4);
            poolMaxCPU.DefaultCellStyle = dataGridViewCellStyle31;
            poolMaxCPU.HeaderText = "Max CPU %";
            poolMaxCPU.MinimumWidth = 6;
            poolMaxCPU.Name = "poolMaxCPU";
            poolMaxCPU.ReadOnly = true;
            poolMaxCPU.Width = 91;
            // 
            // poolMaxDataPct
            // 
            poolMaxDataPct.DataPropertyName = "MaxDataPercent";
            dataGridViewCellStyle32.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle32.Format = "0.#\\%";
            dataGridViewCellStyle32.Padding = new System.Windows.Forms.Padding(4);
            poolMaxDataPct.DefaultCellStyle = dataGridViewCellStyle32;
            poolMaxDataPct.HeaderText = "Max Data %";
            poolMaxDataPct.MinimumWidth = 6;
            poolMaxDataPct.Name = "poolMaxDataPct";
            poolMaxDataPct.ReadOnly = true;
            poolMaxDataPct.Width = 92;
            // 
            // poolMaxLogWrite
            // 
            poolMaxLogWrite.DataPropertyName = "MaxLogWritePercent";
            dataGridViewCellStyle33.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle33.Format = "0.#\\%";
            dataGridViewCellStyle33.Padding = new System.Windows.Forms.Padding(4);
            poolMaxLogWrite.DefaultCellStyle = dataGridViewCellStyle33;
            poolMaxLogWrite.HeaderText = "Max Log Write %";
            poolMaxLogWrite.MinimumWidth = 6;
            poolMaxLogWrite.Name = "poolMaxLogWrite";
            poolMaxLogWrite.ReadOnly = true;
            poolMaxLogWrite.Width = 120;
            // 
            // poolAvgCPU
            // 
            poolAvgCPU.DataPropertyName = "AvgCPUPercent";
            dataGridViewCellStyle34.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle34.Format = "0.#\\%";
            dataGridViewCellStyle34.Padding = new System.Windows.Forms.Padding(4);
            poolAvgCPU.DefaultCellStyle = dataGridViewCellStyle34;
            poolAvgCPU.HeaderText = "Avg CPU %";
            poolAvgCPU.MinimumWidth = 6;
            poolAvgCPU.Name = "poolAvgCPU";
            poolAvgCPU.ReadOnly = true;
            poolAvgCPU.Width = 90;
            // 
            // poolAvgData
            // 
            poolAvgData.DataPropertyName = "AvgDataPercent";
            dataGridViewCellStyle35.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle35.Format = "0.#\\%";
            dataGridViewCellStyle35.Padding = new System.Windows.Forms.Padding(4);
            poolAvgData.DefaultCellStyle = dataGridViewCellStyle35;
            poolAvgData.HeaderText = "Avg Data %";
            poolAvgData.MinimumWidth = 6;
            poolAvgData.Name = "poolAvgData";
            poolAvgData.ReadOnly = true;
            poolAvgData.Width = 91;
            // 
            // poolAvgLogWrite
            // 
            poolAvgLogWrite.DataPropertyName = "AvgLogWritePercent";
            dataGridViewCellStyle36.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle36.Format = "0.#\\%";
            dataGridViewCellStyle36.Padding = new System.Windows.Forms.Padding(4);
            poolAvgLogWrite.DefaultCellStyle = dataGridViewCellStyle36;
            poolAvgLogWrite.HeaderText = "Avg Log Write %";
            poolAvgLogWrite.MinimumWidth = 6;
            poolAvgLogWrite.Name = "poolAvgLogWrite";
            poolAvgLogWrite.ReadOnly = true;
            poolAvgLogWrite.Width = 119;
            // 
            // colAllocatedStoragePct
            // 
            colAllocatedStoragePct.DataPropertyName = "current_allocated_storage_percent";
            dataGridViewCellStyle37.Format = "###.#\\%";
            dataGridViewCellStyle37.NullValue = null;
            colAllocatedStoragePct.DefaultCellStyle = dataGridViewCellStyle37;
            colAllocatedStoragePct.HeaderText = "Current Allocated Storage %";
            colAllocatedStoragePct.MinimumWidth = 6;
            colAllocatedStoragePct.Name = "colAllocatedStoragePct";
            colAllocatedStoragePct.ReadOnly = true;
            colAllocatedStoragePct.Width = 197;
            // 
            // colStorageLimit
            // 
            colStorageLimit.DataPropertyName = "current_elastic_pool_storage_limit_gb";
            dataGridViewCellStyle38.Format = "#,##0.0";
            dataGridViewCellStyle38.NullValue = null;
            colStorageLimit.DefaultCellStyle = dataGridViewCellStyle38;
            colStorageLimit.HeaderText = "Current Storage Limit (GB)";
            colStorageLimit.MinimumWidth = 6;
            colStorageLimit.Name = "colStorageLimit";
            colStorageLimit.ReadOnly = true;
            colStorageLimit.Width = 160;
            // 
            // colCurrentUsedGB
            // 
            colCurrentUsedGB.DataPropertyName = "current_elastic_pool_storage_used_gb";
            dataGridViewCellStyle39.Format = "#,##0.0";
            colCurrentUsedGB.DefaultCellStyle = dataGridViewCellStyle39;
            colCurrentUsedGB.HeaderText = "Current Used GB";
            colCurrentUsedGB.MinimumWidth = 6;
            colCurrentUsedGB.Name = "colCurrentUsedGB";
            colCurrentUsedGB.ReadOnly = true;
            colCurrentUsedGB.Width = 115;
            // 
            // colCurrentFreeGB
            // 
            colCurrentFreeGB.DataPropertyName = "current_elastic_pool_storage_free_gb";
            dataGridViewCellStyle40.Format = "#,##0.0";
            colCurrentFreeGB.DefaultCellStyle = dataGridViewCellStyle40;
            colCurrentFreeGB.HeaderText = "Current Free GB";
            colCurrentFreeGB.MinimumWidth = 6;
            colCurrentFreeGB.Name = "colCurrentFreeGB";
            colCurrentFreeGB.ReadOnly = true;
            colCurrentFreeGB.Width = 111;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 31);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvPool);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(1455, 1220);
            splitContainer1.SplitterDistance = 749;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 6;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefreshPool, tsCopyPool, toolStripLabel1, tsExcelPool, tsPoolCols, tsClearFilterPool });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(1455, 31);
            toolStrip2.TabIndex = 6;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsRefreshPool
            // 
            tsRefreshPool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshPool.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshPool.Name = "tsRefreshPool";
            tsRefreshPool.Size = new System.Drawing.Size(29, 28);
            tsRefreshPool.Text = "Refresh";
            tsRefreshPool.Click += TsRefreshPool_Click;
            // 
            // tsCopyPool
            // 
            tsCopyPool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyPool.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyPool.Name = "tsCopyPool";
            tsCopyPool.Size = new System.Drawing.Size(29, 28);
            tsCopyPool.Text = "Copy";
            tsCopyPool.Click += TsCopyPool_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(88, 28);
            toolStripLabel1.Text = "Elastic Pool";
            // 
            // tsExcelPool
            // 
            tsExcelPool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelPool.Image = Properties.Resources.excel16x16;
            tsExcelPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelPool.Name = "tsExcelPool";
            tsExcelPool.Size = new System.Drawing.Size(29, 28);
            tsExcelPool.Text = "Export Excel";
            tsExcelPool.Click += TsExcelPool_Click;
            // 
            // tsPoolCols
            // 
            tsPoolCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPoolCols.Image = Properties.Resources.Column_16x;
            tsPoolCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPoolCols.Name = "tsPoolCols";
            tsPoolCols.Size = new System.Drawing.Size(29, 28);
            tsPoolCols.Text = "Columns";
            tsPoolCols.Click += TsPoolCols_Click;
            // 
            // tsClearFilterPool
            // 
            tsClearFilterPool.Enabled = false;
            tsClearFilterPool.Image = Properties.Resources.Eraser_16x;
            tsClearFilterPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterPool.Name = "tsClearFilterPool";
            tsClearFilterPool.Size = new System.Drawing.Size(104, 28);
            tsClearFilterPool.Text = "Clear Filter";
            // 
            // AzureSummary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AzureSummary";
            Size = new System.Drawing.Size(1455, 1251);
            Load += AzureSummary_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPool).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgv;
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
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn1;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn2;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn3;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn4;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn5;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn6;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn7;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn8;
        private DBADashDataGridView dgvPool;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsCopyPool;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefreshPool;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelPool;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripButton tsPoolCols;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolInstance;
        private System.Windows.Forms.DataGridViewLinkColumn colPoolName;
        private System.Windows.Forms.DataGridViewImageColumn dgvPool_DTUHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgvPool_CPUHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgvPool_DataHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgvPool_LogHistogram;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolMaxDTUPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolMaxDTU;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolUnusedDTU;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolAVGDTUPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolAVGDTU;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolMinDTULimit;
        private System.Windows.Forms.DataGridViewTextBoxColumn poolDTULimitMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCPULimitMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCPULimitMax;
        private CustomProgressControl.DataGridViewProgressBarColumn poolMaxCPU;
        private CustomProgressControl.DataGridViewProgressBarColumn poolMaxDataPct;
        private CustomProgressControl.DataGridViewProgressBarColumn poolMaxLogWrite;
        private CustomProgressControl.DataGridViewProgressBarColumn poolAvgCPU;
        private CustomProgressControl.DataGridViewProgressBarColumn poolAvgData;
        private CustomProgressControl.DataGridViewProgressBarColumn poolAvgLogWrite;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAllocatedStoragePct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStorageLimit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentUsedGB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrentFreeGB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewLinkColumn colDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEdition;
        private System.Windows.Forms.DataGridViewLinkColumn colServiceObjective;
        private System.Windows.Forms.DataGridViewLinkColumn colElasticPool;
        private System.Windows.Forms.DataGridViewImageColumn dgv_DTUHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgv_CPUHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgv_DataHistogram;
        private System.Windows.Forms.DataGridViewImageColumn dgv_LogHistogram;
        private CustomProgressControl.DataGridViewProgressBarColumn colMaxDTU;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxDTUUsed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUnusedDTUs;
        private CustomProgressControl.DataGridViewProgressBarColumn colAvgDTUPercent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAVGDTUsUsed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMinDTULimit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDTULimitMax;
        private CustomProgressControl.DataGridViewProgressBarColumn colMaxCPUPercent;
        private CustomProgressControl.DataGridViewProgressBarColumn colMaxDataPct;
        private CustomProgressControl.DataGridViewProgressBarColumn colMaxLog;
        private CustomProgressControl.DataGridViewProgressBarColumn colAvgCPUPct;
        private CustomProgressControl.DataGridViewProgressBarColumn colAvgDataPct;
        private CustomProgressControl.DataGridViewProgressBarColumn colAvgLogWrite;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAllocatedSpaceMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxStorageSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAllocatedPctMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsedPctMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFileSnapshotAge;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripButton tsClearFilterPool;
    }
}
