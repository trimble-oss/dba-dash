namespace DBAChecksGUI
{
    partial class HardwareChanges
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgvHardware = new System.Windows.Forms.DataGridView();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colManufacturer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProcessor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCoresPerSocket = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSockets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCPUCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPhysicalCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHTRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNUMA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSoftNUMA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAffinity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPhysicalMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBufferPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPctMemoryBufferPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMemNotAllocated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMemoryModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOfflineSchedulers = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPowerPlan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstantFileInitialization = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsRefreshHardware = new System.Windows.Forms.ToolStripButton();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SystemManufacturerOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SystemManufacturerNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SystemProductNameOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SystemProductNameNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Processor_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Processor_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cpu_count_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cpu_count_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cores_per_socket_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cores_per_socket_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.socket_count_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.socket_count_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.hyperthread_ratio_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.hyperthread_ratio_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.physical_memory_gb_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.physical_memory_gb_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHardware)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.ChangeDate,
            this.SystemManufacturerOld,
            this.SystemManufacturerNew,
            this.SystemProductNameOld,
            this.SystemProductNameNew,
            this.Processor_old,
            this.Processor_new,
            this.cpu_count_old,
            this.cpu_count_new,
            this.cores_per_socket_old,
            this.cores_per_socket_new,
            this.socket_count_old,
            this.socket_count_new,
            this.hyperthread_ratio_old,
            this.hyperthread_ratio_new,
            this.physical_memory_gb_old,
            this.physical_memory_gb_new});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(850, 284);
            this.dgv.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvHardware);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer1.Size = new System.Drawing.Size(850, 629);
            this.splitContainer1.SplitterDistance = 314;
            this.splitContainer1.TabIndex = 1;
            // 
            // dgvHardware
            // 
            this.dgvHardware.AllowUserToAddRows = false;
            this.dgvHardware.AllowUserToDeleteRows = false;
            this.dgvHardware.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvHardware.BackgroundColor = System.Drawing.Color.White;
            this.dgvHardware.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgvHardware.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHardware.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colManufacturer,
            this.colModel,
            this.colProcessor,
            this.colCoresPerSocket,
            this.colSockets,
            this.colCPUs,
            this.colCPUCores,
            this.colPhysicalCPUs,
            this.colHTRatio,
            this.colNUMA,
            this.colSoftNUMA,
            this.colAffinity,
            this.colPhysicalMemory,
            this.colBufferPool,
            this.colPctMemoryBufferPool,
            this.colMemNotAllocated,
            this.colMemoryModel,
            this.colOfflineSchedulers,
            this.colPowerPlan,
            this.colPriority,
            this.colInstantFileInitialization});
            this.dgvHardware.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHardware.Location = new System.Drawing.Point(0, 31);
            this.dgvHardware.Name = "dgvHardware";
            this.dgvHardware.ReadOnly = true;
            this.dgvHardware.RowHeadersVisible = false;
            this.dgvHardware.RowHeadersWidth = 51;
            this.dgvHardware.RowTemplate.Height = 24;
            this.dgvHardware.Size = new System.Drawing.Size(850, 283);
            this.dgvHardware.TabIndex = 0;
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
            // colManufacturer
            // 
            this.colManufacturer.DataPropertyName = "SystemManufacturer";
            this.colManufacturer.HeaderText = "Manufacturer";
            this.colManufacturer.MinimumWidth = 6;
            this.colManufacturer.Name = "colManufacturer";
            this.colManufacturer.ReadOnly = true;
            this.colManufacturer.Width = 121;
            // 
            // colModel
            // 
            this.colModel.DataPropertyName = "SystemProductName";
            this.colModel.HeaderText = "Model";
            this.colModel.MinimumWidth = 6;
            this.colModel.Name = "colModel";
            this.colModel.ReadOnly = true;
            this.colModel.Width = 75;
            // 
            // colProcessor
            // 
            this.colProcessor.DataPropertyName = "ProcessorNameString";
            this.colProcessor.HeaderText = "Processor";
            this.colProcessor.MinimumWidth = 6;
            this.colProcessor.Name = "colProcessor";
            this.colProcessor.ReadOnly = true;
            this.colProcessor.Width = 101;
            // 
            // colCoresPerSocket
            // 
            this.colCoresPerSocket.DataPropertyName = "cores_per_socket";
            this.colCoresPerSocket.HeaderText = "Cores Per Socket";
            this.colCoresPerSocket.MinimumWidth = 6;
            this.colCoresPerSocket.Name = "colCoresPerSocket";
            this.colCoresPerSocket.ReadOnly = true;
            this.colCoresPerSocket.Width = 135;
            // 
            // colSockets
            // 
            this.colSockets.DataPropertyName = "socket_count";
            this.colSockets.HeaderText = "Sockets";
            this.colSockets.MinimumWidth = 6;
            this.colSockets.Name = "colSockets";
            this.colSockets.ReadOnly = true;
            this.colSockets.Width = 87;
            // 
            // colCPUs
            // 
            this.colCPUs.DataPropertyName = "cpu_count";
            this.colCPUs.HeaderText = "CPUs";
            this.colCPUs.MinimumWidth = 6;
            this.colCPUs.Name = "colCPUs";
            this.colCPUs.ReadOnly = true;
            this.colCPUs.Width = 72;
            // 
            // colCPUCores
            // 
            this.colCPUCores.DataPropertyName = "cpu_core_count";
            this.colCPUCores.HeaderText = "CPU Cores";
            this.colCPUCores.MinimumWidth = 6;
            this.colCPUCores.Name = "colCPUCores";
            this.colCPUCores.ReadOnly = true;
            this.colCPUCores.Width = 98;
            // 
            // colPhysicalCPUs
            // 
            this.colPhysicalCPUs.DataPropertyName = "physical_cpu_count";
            this.colPhysicalCPUs.HeaderText = "Physical CPUs";
            this.colPhysicalCPUs.MinimumWidth = 6;
            this.colPhysicalCPUs.Name = "colPhysicalCPUs";
            this.colPhysicalCPUs.ReadOnly = true;
            this.colPhysicalCPUs.Width = 118;
            // 
            // colHTRatio
            // 
            this.colHTRatio.DataPropertyName = "hyperthread_ratio";
            this.colHTRatio.HeaderText = "HT Ratio";
            this.colHTRatio.MinimumWidth = 6;
            this.colHTRatio.Name = "colHTRatio";
            this.colHTRatio.ReadOnly = true;
            this.colHTRatio.ToolTipText = "Hyperthread Ratio";
            this.colHTRatio.Width = 86;
            // 
            // colNUMA
            // 
            this.colNUMA.DataPropertyName = "numa_node_count";
            this.colNUMA.HeaderText = "NUMA nodes";
            this.colNUMA.MinimumWidth = 6;
            this.colNUMA.Name = "colNUMA";
            this.colNUMA.ReadOnly = true;
            this.colNUMA.Width = 110;
            // 
            // colSoftNUMA
            // 
            this.colSoftNUMA.DataPropertyName = "softnuma_configuration_desc";
            this.colSoftNUMA.HeaderText = "Soft NUMA";
            this.colSoftNUMA.MinimumWidth = 6;
            this.colSoftNUMA.Name = "colSoftNUMA";
            this.colSoftNUMA.ReadOnly = true;
            this.colSoftNUMA.Width = 98;
            // 
            // colAffinity
            // 
            this.colAffinity.DataPropertyName = "affinity_type_desc";
            this.colAffinity.HeaderText = "Affinity";
            this.colAffinity.MinimumWidth = 6;
            this.colAffinity.Name = "colAffinity";
            this.colAffinity.ReadOnly = true;
            this.colAffinity.Width = 79;
            // 
            // colPhysicalMemory
            // 
            this.colPhysicalMemory.DataPropertyName = "PhysicalMemoryGB";
            dataGridViewCellStyle3.Format = "N1";
            this.colPhysicalMemory.DefaultCellStyle = dataGridViewCellStyle3;
            this.colPhysicalMemory.HeaderText = "Physical Memory (GB)";
            this.colPhysicalMemory.MinimumWidth = 6;
            this.colPhysicalMemory.Name = "colPhysicalMemory";
            this.colPhysicalMemory.ReadOnly = true;
            this.colPhysicalMemory.Width = 135;
            // 
            // colBufferPool
            // 
            this.colBufferPool.DataPropertyName = "BufferPoolMB";
            dataGridViewCellStyle4.Format = "N0";
            this.colBufferPool.DefaultCellStyle = dataGridViewCellStyle4;
            this.colBufferPool.HeaderText = "Buffer Pool (MB)";
            this.colBufferPool.MinimumWidth = 6;
            this.colBufferPool.Name = "colBufferPool";
            this.colBufferPool.ReadOnly = true;
            this.colBufferPool.Width = 102;
            // 
            // colPctMemoryBufferPool
            // 
            this.colPctMemoryBufferPool.DataPropertyName = "PctMemoryAllocatedToBufferPool";
            dataGridViewCellStyle5.Format = "P1";
            this.colPctMemoryBufferPool.DefaultCellStyle = dataGridViewCellStyle5;
            this.colPctMemoryBufferPool.HeaderText = "% Memory allocated to buffer pool";
            this.colPctMemoryBufferPool.MinimumWidth = 6;
            this.colPctMemoryBufferPool.Name = "colPctMemoryBufferPool";
            this.colPctMemoryBufferPool.ReadOnly = true;
            this.colPctMemoryBufferPool.Width = 168;
            // 
            // colMemNotAllocated
            // 
            this.colMemNotAllocated.DataPropertyName = "MemoryNotAllocatedToBufferPoolGB";
            dataGridViewCellStyle6.Format = "N1";
            this.colMemNotAllocated.DefaultCellStyle = dataGridViewCellStyle6;
            this.colMemNotAllocated.HeaderText = "Memory not allocated to buffer pool (GB)";
            this.colMemNotAllocated.MinimumWidth = 6;
            this.colMemNotAllocated.Name = "colMemNotAllocated";
            this.colMemNotAllocated.ReadOnly = true;
            this.colMemNotAllocated.Width = 175;
            // 
            // colMemoryModel
            // 
            this.colMemoryModel.DataPropertyName = "sql_memory_model_desc";
            this.colMemoryModel.HeaderText = "Memory Model";
            this.colMemoryModel.MinimumWidth = 6;
            this.colMemoryModel.Name = "colMemoryModel";
            this.colMemoryModel.ReadOnly = true;
            this.colMemoryModel.Width = 118;
            // 
            // colOfflineSchedulers
            // 
            this.colOfflineSchedulers.DataPropertyName = "OfflineSchedulers";
            this.colOfflineSchedulers.HeaderText = "Offline Schedulers";
            this.colOfflineSchedulers.MinimumWidth = 6;
            this.colOfflineSchedulers.Name = "colOfflineSchedulers";
            this.colOfflineSchedulers.ReadOnly = true;
            this.colOfflineSchedulers.Width = 140;
            // 
            // colPowerPlan
            // 
            this.colPowerPlan.DataPropertyName = "ActivePowerPlan";
            this.colPowerPlan.HeaderText = "Power Plan";
            this.colPowerPlan.MinimumWidth = 6;
            this.colPowerPlan.Name = "colPowerPlan";
            this.colPowerPlan.ReadOnly = true;
            // 
            // colPriority
            // 
            this.colPriority.DataPropertyName = "os_priority_class_desc";
            this.colPriority.HeaderText = "Priority";
            this.colPriority.MinimumWidth = 6;
            this.colPriority.Name = "colPriority";
            this.colPriority.ReadOnly = true;
            this.colPriority.Width = 81;
            // 
            // colInstantFileInitialization
            // 
            this.colInstantFileInitialization.DataPropertyName = "InstantFileInitializationEnabled";
            this.colInstantFileInitialization.HeaderText = "Instant File Initialization";
            this.colInstantFileInitialization.MinimumWidth = 6;
            this.colInstantFileInitialization.Name = "colInstantFileInitialization";
            this.colInstantFileInitialization.ReadOnly = true;
            this.colInstantFileInitialization.Width = 166;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsRefreshHardware,
            this.tsCopy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(850, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(77, 28);
            this.toolStripLabel1.Text = "Hardware";
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.tsRefreshHistory,
            this.tsCopyHistory});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(850, 27);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(61, 24);
            this.toolStripLabel2.Text = "History";
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
            // tsRefreshHardware
            // 
            this.tsRefreshHardware.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHardware.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHardware.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHardware.Name = "tsRefreshHardware";
            this.tsRefreshHardware.Size = new System.Drawing.Size(29, 28);
            this.tsRefreshHardware.Text = "toolStripButton1";
            this.tsRefreshHardware.Click += new System.EventHandler(this.tsRefreshHardware_Click);
            // 
            // tsRefreshHistory
            // 
            this.tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHistory.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHistory.Name = "tsRefreshHistory";
            this.tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshHistory.Text = "Refresh";
            this.tsRefreshHistory.Click += new System.EventHandler(this.tsRefreshHistory_Click);
            // 
            // tsCopyHistory
            // 
            this.tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyHistory.Image = global::DBAChecksGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyHistory.Name = "tsCopyHistory";
            this.tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            this.tsCopyHistory.Text = "Copy";
            this.tsCopyHistory.Click += new System.EventHandler(this.tsCopyHistory_Click);
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
            // ChangeDate
            // 
            this.ChangeDate.DataPropertyName = "ChangeDate";
            this.ChangeDate.HeaderText = "Change Date";
            this.ChangeDate.MinimumWidth = 6;
            this.ChangeDate.Name = "ChangeDate";
            this.ChangeDate.ReadOnly = true;
            this.ChangeDate.Width = 120;
            // 
            // SystemManufacturerOld
            // 
            this.SystemManufacturerOld.DataPropertyName = "SystemManufacturerOld";
            this.SystemManufacturerOld.HeaderText = "Manufacturer (Old)";
            this.SystemManufacturerOld.MinimumWidth = 6;
            this.SystemManufacturerOld.Name = "SystemManufacturerOld";
            this.SystemManufacturerOld.ReadOnly = true;
            this.SystemManufacturerOld.Width = 144;
            // 
            // SystemManufacturerNew
            // 
            this.SystemManufacturerNew.DataPropertyName = "SystemManufacturerNew";
            this.SystemManufacturerNew.HeaderText = "Manufacturer (New)";
            this.SystemManufacturerNew.MinimumWidth = 6;
            this.SystemManufacturerNew.Name = "SystemManufacturerNew";
            this.SystemManufacturerNew.ReadOnly = true;
            this.SystemManufacturerNew.Width = 148;
            // 
            // SystemProductNameOld
            // 
            this.SystemProductNameOld.DataPropertyName = "SystemProductNameOld";
            this.SystemProductNameOld.HeaderText = "Model (Old)";
            this.SystemProductNameOld.MinimumWidth = 6;
            this.SystemProductNameOld.Name = "SystemProductNameOld";
            this.SystemProductNameOld.ReadOnly = true;
            this.SystemProductNameOld.Width = 102;
            // 
            // SystemProductNameNew
            // 
            this.SystemProductNameNew.DataPropertyName = "SystemProductNameNew";
            this.SystemProductNameNew.HeaderText = "Model (New)";
            this.SystemProductNameNew.MinimumWidth = 6;
            this.SystemProductNameNew.Name = "SystemProductNameNew";
            this.SystemProductNameNew.ReadOnly = true;
            this.SystemProductNameNew.Width = 107;
            // 
            // Processor_old
            // 
            this.Processor_old.DataPropertyName = "Processor_old";
            this.Processor_old.HeaderText = "Processor (Old)";
            this.Processor_old.MinimumWidth = 6;
            this.Processor_old.Name = "Processor_old";
            this.Processor_old.ReadOnly = true;
            this.Processor_old.Width = 126;
            // 
            // Processor_new
            // 
            this.Processor_new.DataPropertyName = "Processor_new";
            this.Processor_new.HeaderText = "Processor (New)";
            this.Processor_new.MinimumWidth = 6;
            this.Processor_new.Name = "Processor_new";
            this.Processor_new.ReadOnly = true;
            this.Processor_new.Width = 130;
            // 
            // cpu_count_old
            // 
            this.cpu_count_old.DataPropertyName = "cpu_count_old";
            this.cpu_count_old.HeaderText = "CPUs (Old)";
            this.cpu_count_old.MinimumWidth = 6;
            this.cpu_count_old.Name = "cpu_count_old";
            this.cpu_count_old.ReadOnly = true;
            // 
            // cpu_count_new
            // 
            this.cpu_count_new.DataPropertyName = "cpu_count_new";
            this.cpu_count_new.HeaderText = "CPUs (new)";
            this.cpu_count_new.MinimumWidth = 6;
            this.cpu_count_new.Name = "cpu_count_new";
            this.cpu_count_new.ReadOnly = true;
            this.cpu_count_new.Width = 102;
            // 
            // cores_per_socket_old
            // 
            this.cores_per_socket_old.DataPropertyName = "cores_per_socket_old";
            this.cores_per_socket_old.HeaderText = "Core per Socket (Old)";
            this.cores_per_socket_old.MinimumWidth = 6;
            this.cores_per_socket_old.Name = "cores_per_socket_old";
            this.cores_per_socket_old.ReadOnly = true;
            this.cores_per_socket_old.Width = 131;
            // 
            // cores_per_socket_new
            // 
            this.cores_per_socket_new.DataPropertyName = "cores_per_socket_new";
            this.cores_per_socket_new.HeaderText = "Cores per Socket (New)";
            this.cores_per_socket_new.MinimumWidth = 6;
            this.cores_per_socket_new.Name = "cores_per_socket_new";
            this.cores_per_socket_new.ReadOnly = true;
            this.cores_per_socket_new.Width = 137;
            // 
            // socket_count_old
            // 
            this.socket_count_old.DataPropertyName = "socket_count_old";
            this.socket_count_old.HeaderText = "Socket Count (Old)";
            this.socket_count_old.MinimumWidth = 6;
            this.socket_count_old.Name = "socket_count_old";
            this.socket_count_old.ReadOnly = true;
            this.socket_count_old.Width = 115;
            // 
            // socket_count_new
            // 
            this.socket_count_new.DataPropertyName = "socket_count_new";
            this.socket_count_new.HeaderText = "Socket Count (New)";
            this.socket_count_new.MinimumWidth = 6;
            this.socket_count_new.Name = "socket_count_new";
            this.socket_count_new.ReadOnly = true;
            this.socket_count_new.Width = 115;
            // 
            // hyperthread_ratio_old
            // 
            this.hyperthread_ratio_old.DataPropertyName = "hyperthread_ratio_old";
            this.hyperthread_ratio_old.HeaderText = "HT Ratio (Old)";
            this.hyperthread_ratio_old.MinimumWidth = 6;
            this.hyperthread_ratio_old.Name = "hyperthread_ratio_old";
            this.hyperthread_ratio_old.ReadOnly = true;
            this.hyperthread_ratio_old.Width = 118;
            // 
            // hyperthread_ratio_new
            // 
            this.hyperthread_ratio_new.DataPropertyName = "hyperthread_ratio_new";
            this.hyperthread_ratio_new.HeaderText = "HT Ratio (New)";
            this.hyperthread_ratio_new.MinimumWidth = 6;
            this.hyperthread_ratio_new.Name = "hyperthread_ratio_new";
            this.hyperthread_ratio_new.ReadOnly = true;
            this.hyperthread_ratio_new.Width = 123;
            // 
            // physical_memory_gb_old
            // 
            this.physical_memory_gb_old.DataPropertyName = "physical_memory_gb_old";
            dataGridViewCellStyle1.Format = "N1";
            this.physical_memory_gb_old.DefaultCellStyle = dataGridViewCellStyle1;
            this.physical_memory_gb_old.HeaderText = "Physical Memory (Old)";
            this.physical_memory_gb_old.MinimumWidth = 6;
            this.physical_memory_gb_old.Name = "physical_memory_gb_old";
            this.physical_memory_gb_old.ReadOnly = true;
            this.physical_memory_gb_old.Width = 135;
            // 
            // physical_memory_gb_new
            // 
            this.physical_memory_gb_new.DataPropertyName = "physical_memory_gb_new";
            dataGridViewCellStyle2.Format = "N1";
            this.physical_memory_gb_new.DefaultCellStyle = dataGridViewCellStyle2;
            this.physical_memory_gb_new.HeaderText = "Physical Memory (New)";
            this.physical_memory_gb_new.MinimumWidth = 6;
            this.physical_memory_gb_new.Name = "physical_memory_gb_new";
            this.physical_memory_gb_new.ReadOnly = true;
            this.physical_memory_gb_new.Width = 135;
            // 
            // HardwareChanges
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "HardwareChanges";
            this.Size = new System.Drawing.Size(850, 629);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHardware)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvHardware;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colManufacturer;
        private System.Windows.Forms.DataGridViewTextBoxColumn colModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProcessor;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCoresPerSocket;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSockets;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCPUs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCPUCores;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPhysicalCPUs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHTRatio;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNUMA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSoftNUMA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAffinity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPhysicalMemory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBufferPool;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPctMemoryBufferPool;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMemNotAllocated;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMemoryModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOfflineSchedulers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPowerPlan;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstantFileInitialization;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsRefreshHardware;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChangeDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn SystemManufacturerOld;
        private System.Windows.Forms.DataGridViewTextBoxColumn SystemManufacturerNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn SystemProductNameOld;
        private System.Windows.Forms.DataGridViewTextBoxColumn SystemProductNameNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn Processor_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn Processor_new;
        private System.Windows.Forms.DataGridViewTextBoxColumn cpu_count_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn cpu_count_new;
        private System.Windows.Forms.DataGridViewTextBoxColumn cores_per_socket_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn cores_per_socket_new;
        private System.Windows.Forms.DataGridViewTextBoxColumn socket_count_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn socket_count_new;
        private System.Windows.Forms.DataGridViewTextBoxColumn hyperthread_ratio_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn hyperthread_ratio_new;
        private System.Windows.Forms.DataGridViewTextBoxColumn physical_memory_gb_old;
        private System.Windows.Forms.DataGridViewTextBoxColumn physical_memory_gb_new;
    }
}
