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
            this.dgv = new System.Windows.Forms.DataGridView();
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
            this.colPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaxWorkerCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSchedulerCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOfflineSchedulers = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPowerPlan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstantFileInitialization = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshHardware = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
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
            this.dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn28 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn29 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn30 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn31 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn32 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn33 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn34 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn35 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn36 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn37 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn38 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn39 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn40 = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.ChangeDate.Width = 110;
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
            this.colPriority,
            this.colMaxWorkerCount,
            this.colSchedulerCount,
            this.colOfflineSchedulers,
            this.colPowerPlan,
            this.colInstantFileInitialization});
            this.dgvHardware.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHardware.Location = new System.Drawing.Point(0, 27);
            this.dgvHardware.Name = "dgvHardware";
            this.dgvHardware.ReadOnly = true;
            this.dgvHardware.RowHeadersVisible = false;
            this.dgvHardware.RowHeadersWidth = 51;
            this.dgvHardware.RowTemplate.Height = 24;
            this.dgvHardware.Size = new System.Drawing.Size(850, 287);
            this.dgvHardware.TabIndex = 0;
            this.dgvHardware.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvHardware_RowsAdded);
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
            // colPriority
            // 
            this.colPriority.DataPropertyName = "os_priority_class_desc";
            this.colPriority.HeaderText = "Priority";
            this.colPriority.MinimumWidth = 6;
            this.colPriority.Name = "colPriority";
            this.colPriority.ReadOnly = true;
            this.colPriority.Width = 81;
            // 
            // colMaxWorkerCount
            // 
            this.colMaxWorkerCount.DataPropertyName = "max_workers_count";
            this.colMaxWorkerCount.HeaderText = "Max Workers";
            this.colMaxWorkerCount.MinimumWidth = 6;
            this.colMaxWorkerCount.Name = "colMaxWorkerCount";
            this.colMaxWorkerCount.ReadOnly = true;
            this.colMaxWorkerCount.Width = 109;
            // 
            // colSchedulerCount
            // 
            this.colSchedulerCount.DataPropertyName = "scheduler_count";
            this.colSchedulerCount.HeaderText = "Scheduler Count";
            this.colSchedulerCount.MinimumWidth = 6;
            this.colSchedulerCount.Name = "colSchedulerCount";
            this.colSchedulerCount.ReadOnly = true;
            this.colSchedulerCount.Width = 130;
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
            this.toolStrip1.Size = new System.Drawing.Size(850, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(77, 24);
            this.toolStripLabel1.Text = "Hardware";
            // 
            // tsRefreshHardware
            // 
            this.tsRefreshHardware.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHardware.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHardware.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHardware.Name = "tsRefreshHardware";
            this.tsRefreshHardware.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshHardware.Text = "Refresh";
            this.tsRefreshHardware.Click += new System.EventHandler(this.tsRefreshHardware_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBAChecksGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
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
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "SystemManufacturer";
            this.dataGridViewTextBoxColumn2.HeaderText = "Manufacturer";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 121;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "SystemProductName";
            this.dataGridViewTextBoxColumn3.HeaderText = "Model";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 75;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "ProcessorNameString";
            this.dataGridViewTextBoxColumn4.HeaderText = "Processor";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 101;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "cores_per_socket";
            this.dataGridViewTextBoxColumn5.HeaderText = "Cores Per Socket";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 135;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "socket_count";
            this.dataGridViewTextBoxColumn6.HeaderText = "Sockets";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 87;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "cpu_count";
            this.dataGridViewTextBoxColumn7.HeaderText = "CPUs";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 72;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "cpu_core_count";
            this.dataGridViewTextBoxColumn8.HeaderText = "CPU Cores";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 98;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.DataPropertyName = "physical_cpu_count";
            this.dataGridViewTextBoxColumn9.HeaderText = "Physical CPUs";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 118;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.DataPropertyName = "hyperthread_ratio";
            this.dataGridViewTextBoxColumn10.HeaderText = "HT Ratio";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.ToolTipText = "Hyperthread Ratio";
            this.dataGridViewTextBoxColumn10.Width = 86;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.DataPropertyName = "numa_node_count";
            this.dataGridViewTextBoxColumn11.HeaderText = "NUMA nodes";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 110;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.DataPropertyName = "softnuma_configuration_desc";
            this.dataGridViewTextBoxColumn12.HeaderText = "Soft NUMA";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 98;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.DataPropertyName = "affinity_type_desc";
            this.dataGridViewTextBoxColumn13.HeaderText = "Affinity";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 79;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.DataPropertyName = "PhysicalMemoryGB";
            dataGridViewCellStyle7.Format = "N1";
            this.dataGridViewTextBoxColumn14.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridViewTextBoxColumn14.HeaderText = "Physical Memory (GB)";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 135;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.DataPropertyName = "BufferPoolMB";
            dataGridViewCellStyle8.Format = "N0";
            this.dataGridViewTextBoxColumn15.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn15.HeaderText = "Buffer Pool (MB)";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 102;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.DataPropertyName = "PctMemoryAllocatedToBufferPool";
            dataGridViewCellStyle9.Format = "P1";
            this.dataGridViewTextBoxColumn16.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn16.HeaderText = "% Memory allocated to buffer pool";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            this.dataGridViewTextBoxColumn16.Width = 168;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.DataPropertyName = "MemoryNotAllocatedToBufferPoolGB";
            dataGridViewCellStyle10.Format = "N1";
            this.dataGridViewTextBoxColumn17.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn17.HeaderText = "Memory not allocated to buffer pool (GB)";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.ReadOnly = true;
            this.dataGridViewTextBoxColumn17.Width = 175;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.DataPropertyName = "sql_memory_model_desc";
            this.dataGridViewTextBoxColumn18.HeaderText = "Memory Model";
            this.dataGridViewTextBoxColumn18.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            this.dataGridViewTextBoxColumn18.ReadOnly = true;
            this.dataGridViewTextBoxColumn18.Width = 118;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.DataPropertyName = "OfflineSchedulers";
            this.dataGridViewTextBoxColumn19.HeaderText = "Offline Schedulers";
            this.dataGridViewTextBoxColumn19.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            this.dataGridViewTextBoxColumn19.ReadOnly = true;
            this.dataGridViewTextBoxColumn19.Width = 140;
            // 
            // dataGridViewTextBoxColumn20
            // 
            this.dataGridViewTextBoxColumn20.DataPropertyName = "ActivePowerPlan";
            this.dataGridViewTextBoxColumn20.HeaderText = "Power Plan";
            this.dataGridViewTextBoxColumn20.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            this.dataGridViewTextBoxColumn20.ReadOnly = true;
            this.dataGridViewTextBoxColumn20.Width = 125;
            // 
            // dataGridViewTextBoxColumn21
            // 
            this.dataGridViewTextBoxColumn21.DataPropertyName = "os_priority_class_desc";
            this.dataGridViewTextBoxColumn21.HeaderText = "Priority";
            this.dataGridViewTextBoxColumn21.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            this.dataGridViewTextBoxColumn21.ReadOnly = true;
            this.dataGridViewTextBoxColumn21.Width = 81;
            // 
            // dataGridViewTextBoxColumn22
            // 
            this.dataGridViewTextBoxColumn22.DataPropertyName = "InstantFileInitializationEnabled";
            this.dataGridViewTextBoxColumn22.HeaderText = "Instant File Initialization";
            this.dataGridViewTextBoxColumn22.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            this.dataGridViewTextBoxColumn22.ReadOnly = true;
            this.dataGridViewTextBoxColumn22.Width = 166;
            // 
            // dataGridViewTextBoxColumn23
            // 
            this.dataGridViewTextBoxColumn23.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn23.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn23.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            this.dataGridViewTextBoxColumn23.ReadOnly = true;
            this.dataGridViewTextBoxColumn23.Width = 90;
            // 
            // dataGridViewTextBoxColumn24
            // 
            this.dataGridViewTextBoxColumn24.DataPropertyName = "ChangeDate";
            this.dataGridViewTextBoxColumn24.HeaderText = "Change Date";
            this.dataGridViewTextBoxColumn24.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            this.dataGridViewTextBoxColumn24.ReadOnly = true;
            this.dataGridViewTextBoxColumn24.Width = 110;
            // 
            // dataGridViewTextBoxColumn25
            // 
            this.dataGridViewTextBoxColumn25.DataPropertyName = "SystemManufacturerOld";
            this.dataGridViewTextBoxColumn25.HeaderText = "Manufacturer (Old)";
            this.dataGridViewTextBoxColumn25.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            this.dataGridViewTextBoxColumn25.ReadOnly = true;
            this.dataGridViewTextBoxColumn25.Width = 144;
            // 
            // dataGridViewTextBoxColumn26
            // 
            this.dataGridViewTextBoxColumn26.DataPropertyName = "SystemManufacturerNew";
            this.dataGridViewTextBoxColumn26.HeaderText = "Manufacturer (New)";
            this.dataGridViewTextBoxColumn26.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            this.dataGridViewTextBoxColumn26.ReadOnly = true;
            this.dataGridViewTextBoxColumn26.Width = 148;
            // 
            // dataGridViewTextBoxColumn27
            // 
            this.dataGridViewTextBoxColumn27.DataPropertyName = "SystemProductNameOld";
            this.dataGridViewTextBoxColumn27.HeaderText = "Model (Old)";
            this.dataGridViewTextBoxColumn27.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            this.dataGridViewTextBoxColumn27.ReadOnly = true;
            this.dataGridViewTextBoxColumn27.Width = 102;
            // 
            // dataGridViewTextBoxColumn28
            // 
            this.dataGridViewTextBoxColumn28.DataPropertyName = "SystemProductNameNew";
            this.dataGridViewTextBoxColumn28.HeaderText = "Model (New)";
            this.dataGridViewTextBoxColumn28.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            this.dataGridViewTextBoxColumn28.ReadOnly = true;
            this.dataGridViewTextBoxColumn28.Width = 107;
            // 
            // dataGridViewTextBoxColumn29
            // 
            this.dataGridViewTextBoxColumn29.DataPropertyName = "Processor_old";
            this.dataGridViewTextBoxColumn29.HeaderText = "Processor (Old)";
            this.dataGridViewTextBoxColumn29.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn29.Name = "dataGridViewTextBoxColumn29";
            this.dataGridViewTextBoxColumn29.ReadOnly = true;
            this.dataGridViewTextBoxColumn29.Width = 126;
            // 
            // dataGridViewTextBoxColumn30
            // 
            this.dataGridViewTextBoxColumn30.DataPropertyName = "Processor_new";
            this.dataGridViewTextBoxColumn30.HeaderText = "Processor (New)";
            this.dataGridViewTextBoxColumn30.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn30.Name = "dataGridViewTextBoxColumn30";
            this.dataGridViewTextBoxColumn30.ReadOnly = true;
            this.dataGridViewTextBoxColumn30.Width = 130;
            // 
            // dataGridViewTextBoxColumn31
            // 
            this.dataGridViewTextBoxColumn31.DataPropertyName = "cpu_count_old";
            this.dataGridViewTextBoxColumn31.HeaderText = "CPUs (Old)";
            this.dataGridViewTextBoxColumn31.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn31.Name = "dataGridViewTextBoxColumn31";
            this.dataGridViewTextBoxColumn31.ReadOnly = true;
            this.dataGridViewTextBoxColumn31.Width = 125;
            // 
            // dataGridViewTextBoxColumn32
            // 
            this.dataGridViewTextBoxColumn32.DataPropertyName = "cpu_count_new";
            this.dataGridViewTextBoxColumn32.HeaderText = "CPUs (new)";
            this.dataGridViewTextBoxColumn32.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn32.Name = "dataGridViewTextBoxColumn32";
            this.dataGridViewTextBoxColumn32.ReadOnly = true;
            this.dataGridViewTextBoxColumn32.Width = 102;
            // 
            // dataGridViewTextBoxColumn33
            // 
            this.dataGridViewTextBoxColumn33.DataPropertyName = "cores_per_socket_old";
            this.dataGridViewTextBoxColumn33.HeaderText = "Core per Socket (Old)";
            this.dataGridViewTextBoxColumn33.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn33.Name = "dataGridViewTextBoxColumn33";
            this.dataGridViewTextBoxColumn33.ReadOnly = true;
            this.dataGridViewTextBoxColumn33.Width = 131;
            // 
            // dataGridViewTextBoxColumn34
            // 
            this.dataGridViewTextBoxColumn34.DataPropertyName = "cores_per_socket_new";
            this.dataGridViewTextBoxColumn34.HeaderText = "Cores per Socket (New)";
            this.dataGridViewTextBoxColumn34.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn34.Name = "dataGridViewTextBoxColumn34";
            this.dataGridViewTextBoxColumn34.ReadOnly = true;
            this.dataGridViewTextBoxColumn34.Width = 137;
            // 
            // dataGridViewTextBoxColumn35
            // 
            this.dataGridViewTextBoxColumn35.DataPropertyName = "socket_count_old";
            this.dataGridViewTextBoxColumn35.HeaderText = "Socket Count (Old)";
            this.dataGridViewTextBoxColumn35.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn35.Name = "dataGridViewTextBoxColumn35";
            this.dataGridViewTextBoxColumn35.ReadOnly = true;
            this.dataGridViewTextBoxColumn35.Width = 115;
            // 
            // dataGridViewTextBoxColumn36
            // 
            this.dataGridViewTextBoxColumn36.DataPropertyName = "socket_count_new";
            this.dataGridViewTextBoxColumn36.HeaderText = "Socket Count (New)";
            this.dataGridViewTextBoxColumn36.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn36.Name = "dataGridViewTextBoxColumn36";
            this.dataGridViewTextBoxColumn36.ReadOnly = true;
            this.dataGridViewTextBoxColumn36.Width = 115;
            // 
            // dataGridViewTextBoxColumn37
            // 
            this.dataGridViewTextBoxColumn37.DataPropertyName = "hyperthread_ratio_old";
            this.dataGridViewTextBoxColumn37.HeaderText = "HT Ratio (Old)";
            this.dataGridViewTextBoxColumn37.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn37.Name = "dataGridViewTextBoxColumn37";
            this.dataGridViewTextBoxColumn37.ReadOnly = true;
            this.dataGridViewTextBoxColumn37.Width = 118;
            // 
            // dataGridViewTextBoxColumn38
            // 
            this.dataGridViewTextBoxColumn38.DataPropertyName = "hyperthread_ratio_new";
            this.dataGridViewTextBoxColumn38.HeaderText = "HT Ratio (New)";
            this.dataGridViewTextBoxColumn38.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn38.Name = "dataGridViewTextBoxColumn38";
            this.dataGridViewTextBoxColumn38.ReadOnly = true;
            this.dataGridViewTextBoxColumn38.Width = 123;
            // 
            // dataGridViewTextBoxColumn39
            // 
            this.dataGridViewTextBoxColumn39.DataPropertyName = "physical_memory_gb_old";
            dataGridViewCellStyle11.Format = "N1";
            this.dataGridViewTextBoxColumn39.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn39.HeaderText = "Physical Memory (Old)";
            this.dataGridViewTextBoxColumn39.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn39.Name = "dataGridViewTextBoxColumn39";
            this.dataGridViewTextBoxColumn39.ReadOnly = true;
            this.dataGridViewTextBoxColumn39.Width = 135;
            // 
            // dataGridViewTextBoxColumn40
            // 
            this.dataGridViewTextBoxColumn40.DataPropertyName = "physical_memory_gb_new";
            dataGridViewCellStyle12.Format = "N1";
            this.dataGridViewTextBoxColumn40.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn40.HeaderText = "Physical Memory (New)";
            this.dataGridViewTextBoxColumn40.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn40.Name = "dataGridViewTextBoxColumn40";
            this.dataGridViewTextBoxColumn40.ReadOnly = true;
            this.dataGridViewTextBoxColumn40.Width = 135;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxWorkerCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchedulerCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOfflineSchedulers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPowerPlan;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstantFileInitialization;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn29;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn30;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn31;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn32;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn33;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn34;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn35;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn36;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn37;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn38;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn39;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn40;
    }
}
