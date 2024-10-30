using DBADashGUI.CustomReports;

namespace DBADashGUI
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle26 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle24 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle25 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle27 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle28 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle29 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle30 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle31 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle32 = new System.Windows.Forms.DataGridViewCellStyle();
            dgvHistory = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SystemManufacturerOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SystemManufacturerNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SystemProductNameOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SystemProductNameNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Processor_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Processor_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            cpu_count_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            cpu_count_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            cores_per_socket_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            cores_per_socket_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            socket_count_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            socket_count_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            hyperthread_ratio_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            hyperthread_ratio_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            physical_memory_gb_old = new System.Windows.Forms.DataGridViewTextBoxColumn();
            physical_memory_gb_new = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvHardware = new DBADashDataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colManufacturer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProcessor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SQLVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCoresPerSocket = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSockets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCPUCores = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPhysicalCPUs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colHTRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colNUMA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSoftNUMA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAffinity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPhysicalMemory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colBufferPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPctMemoryBufferPool = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMemNotAllocated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMemoryModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxWorkerCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSchedulerCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colOfflineSchedulers = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPowerPlan = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInstantFileInitialization = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshHardware = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            tsClearFilterHistory = new System.Windows.Forms.ToolStripButton();
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
            dataGridViewTextBoxColumn31 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn32 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn33 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn34 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn35 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn36 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn37 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn38 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn39 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn40 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tsHistoryCols = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHardware).BeginInit();
            toolStrip1.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgvHistory
            // 
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle17.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle17.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle17.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle17.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle17.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle17.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle17;
            dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, ChangeDate, SystemManufacturerOld, SystemManufacturerNew, SystemProductNameOld, SystemProductNameNew, Processor_old, Processor_new, cpu_count_old, cpu_count_new, cores_per_socket_old, cores_per_socket_new, socket_count_old, socket_count_new, hyperthread_ratio_old, hyperthread_ratio_new, physical_memory_gb_old, physical_memory_gb_new });
            dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle20.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle20.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle20.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle20.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle20.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle20.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvHistory.DefaultCellStyle = dataGridViewCellStyle20;
            dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.Location = new System.Drawing.Point(0, 27);
            dgvHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvHistory.Name = "dgvHistory";
            dgvHistory.ReadOnly = true;
            dgvHistory.ResultSetID = 0;
            dgvHistory.ResultSetName = null;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.RowHeadersWidth = 51;
            dgvHistory.RowTemplate.Height = 24;
            dgvHistory.Size = new System.Drawing.Size(850, 364);
            dgvHistory.TabIndex = 0;
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
            // ChangeDate
            // 
            ChangeDate.DataPropertyName = "ChangeDate";
            ChangeDate.HeaderText = "Change Date";
            ChangeDate.MinimumWidth = 6;
            ChangeDate.Name = "ChangeDate";
            ChangeDate.ReadOnly = true;
            ChangeDate.Width = 110;
            // 
            // SystemManufacturerOld
            // 
            SystemManufacturerOld.DataPropertyName = "SystemManufacturerOld";
            SystemManufacturerOld.HeaderText = "Manufacturer (Old)";
            SystemManufacturerOld.MinimumWidth = 6;
            SystemManufacturerOld.Name = "SystemManufacturerOld";
            SystemManufacturerOld.ReadOnly = true;
            SystemManufacturerOld.Width = 144;
            // 
            // SystemManufacturerNew
            // 
            SystemManufacturerNew.DataPropertyName = "SystemManufacturerNew";
            SystemManufacturerNew.HeaderText = "Manufacturer (New)";
            SystemManufacturerNew.MinimumWidth = 6;
            SystemManufacturerNew.Name = "SystemManufacturerNew";
            SystemManufacturerNew.ReadOnly = true;
            SystemManufacturerNew.Width = 148;
            // 
            // SystemProductNameOld
            // 
            SystemProductNameOld.DataPropertyName = "SystemProductNameOld";
            SystemProductNameOld.HeaderText = "Model (Old)";
            SystemProductNameOld.MinimumWidth = 6;
            SystemProductNameOld.Name = "SystemProductNameOld";
            SystemProductNameOld.ReadOnly = true;
            SystemProductNameOld.Width = 102;
            // 
            // SystemProductNameNew
            // 
            SystemProductNameNew.DataPropertyName = "SystemProductNameNew";
            SystemProductNameNew.HeaderText = "Model (New)";
            SystemProductNameNew.MinimumWidth = 6;
            SystemProductNameNew.Name = "SystemProductNameNew";
            SystemProductNameNew.ReadOnly = true;
            SystemProductNameNew.Width = 107;
            // 
            // Processor_old
            // 
            Processor_old.DataPropertyName = "Processor_old";
            Processor_old.HeaderText = "Processor (Old)";
            Processor_old.MinimumWidth = 6;
            Processor_old.Name = "Processor_old";
            Processor_old.ReadOnly = true;
            Processor_old.Width = 126;
            // 
            // Processor_new
            // 
            Processor_new.DataPropertyName = "Processor_new";
            Processor_new.HeaderText = "Processor (New)";
            Processor_new.MinimumWidth = 6;
            Processor_new.Name = "Processor_new";
            Processor_new.ReadOnly = true;
            Processor_new.Width = 130;
            // 
            // cpu_count_old
            // 
            cpu_count_old.DataPropertyName = "cpu_count_old";
            cpu_count_old.HeaderText = "CPUs (Old)";
            cpu_count_old.MinimumWidth = 6;
            cpu_count_old.Name = "cpu_count_old";
            cpu_count_old.ReadOnly = true;
            cpu_count_old.Width = 125;
            // 
            // cpu_count_new
            // 
            cpu_count_new.DataPropertyName = "cpu_count_new";
            cpu_count_new.HeaderText = "CPUs (new)";
            cpu_count_new.MinimumWidth = 6;
            cpu_count_new.Name = "cpu_count_new";
            cpu_count_new.ReadOnly = true;
            cpu_count_new.Width = 102;
            // 
            // cores_per_socket_old
            // 
            cores_per_socket_old.DataPropertyName = "cores_per_socket_old";
            cores_per_socket_old.HeaderText = "Core per Socket (Old)";
            cores_per_socket_old.MinimumWidth = 6;
            cores_per_socket_old.Name = "cores_per_socket_old";
            cores_per_socket_old.ReadOnly = true;
            cores_per_socket_old.Width = 131;
            // 
            // cores_per_socket_new
            // 
            cores_per_socket_new.DataPropertyName = "cores_per_socket_new";
            cores_per_socket_new.HeaderText = "Cores per Socket (New)";
            cores_per_socket_new.MinimumWidth = 6;
            cores_per_socket_new.Name = "cores_per_socket_new";
            cores_per_socket_new.ReadOnly = true;
            cores_per_socket_new.Width = 137;
            // 
            // socket_count_old
            // 
            socket_count_old.DataPropertyName = "socket_count_old";
            socket_count_old.HeaderText = "Socket Count (Old)";
            socket_count_old.MinimumWidth = 6;
            socket_count_old.Name = "socket_count_old";
            socket_count_old.ReadOnly = true;
            socket_count_old.Width = 115;
            // 
            // socket_count_new
            // 
            socket_count_new.DataPropertyName = "socket_count_new";
            socket_count_new.HeaderText = "Socket Count (New)";
            socket_count_new.MinimumWidth = 6;
            socket_count_new.Name = "socket_count_new";
            socket_count_new.ReadOnly = true;
            socket_count_new.Width = 115;
            // 
            // hyperthread_ratio_old
            // 
            hyperthread_ratio_old.DataPropertyName = "hyperthread_ratio_old";
            hyperthread_ratio_old.HeaderText = "HT Ratio (Old)";
            hyperthread_ratio_old.MinimumWidth = 6;
            hyperthread_ratio_old.Name = "hyperthread_ratio_old";
            hyperthread_ratio_old.ReadOnly = true;
            hyperthread_ratio_old.Width = 118;
            // 
            // hyperthread_ratio_new
            // 
            hyperthread_ratio_new.DataPropertyName = "hyperthread_ratio_new";
            hyperthread_ratio_new.HeaderText = "HT Ratio (New)";
            hyperthread_ratio_new.MinimumWidth = 6;
            hyperthread_ratio_new.Name = "hyperthread_ratio_new";
            hyperthread_ratio_new.ReadOnly = true;
            hyperthread_ratio_new.Width = 123;
            // 
            // physical_memory_gb_old
            // 
            physical_memory_gb_old.DataPropertyName = "physical_memory_gb_old";
            dataGridViewCellStyle18.Format = "N1";
            physical_memory_gb_old.DefaultCellStyle = dataGridViewCellStyle18;
            physical_memory_gb_old.HeaderText = "Physical Memory (Old)";
            physical_memory_gb_old.MinimumWidth = 6;
            physical_memory_gb_old.Name = "physical_memory_gb_old";
            physical_memory_gb_old.ReadOnly = true;
            physical_memory_gb_old.Width = 135;
            // 
            // physical_memory_gb_new
            // 
            physical_memory_gb_new.DataPropertyName = "physical_memory_gb_new";
            dataGridViewCellStyle19.Format = "N1";
            physical_memory_gb_new.DefaultCellStyle = dataGridViewCellStyle19;
            physical_memory_gb_new.HeaderText = "Physical Memory (New)";
            physical_memory_gb_new.MinimumWidth = 6;
            physical_memory_gb_new.Name = "physical_memory_gb_new";
            physical_memory_gb_new.ReadOnly = true;
            physical_memory_gb_new.Width = 135;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgvHardware);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvHistory);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(850, 786);
            splitContainer1.SplitterDistance = 390;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // dgvHardware
            // 
            dgvHardware.AllowUserToAddRows = false;
            dgvHardware.AllowUserToDeleteRows = false;
            dgvHardware.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle21.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle21.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle21.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle21.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle21.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle21.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvHardware.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle21;
            dgvHardware.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHardware.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colManufacturer, colModel, colProcessor, SQLVersion, colCoresPerSocket, colSockets, colCPUs, colCPUCores, colPhysicalCPUs, colHTRatio, colNUMA, colSoftNUMA, colAffinity, colPhysicalMemory, colBufferPool, colPctMemoryBufferPool, colMemNotAllocated, colMemoryModel, colPriority, colMaxWorkerCount, colSchedulerCount, colOfflineSchedulers, colPowerPlan, colInstantFileInitialization });
            dataGridViewCellStyle26.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle26.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle26.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle26.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle26.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle26.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle26.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvHardware.DefaultCellStyle = dataGridViewCellStyle26;
            dgvHardware.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvHardware.EnableHeadersVisualStyles = false;
            dgvHardware.Location = new System.Drawing.Point(0, 27);
            dgvHardware.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvHardware.Name = "dgvHardware";
            dgvHardware.ReadOnly = true;
            dgvHardware.ResultSetID = 0;
            dgvHardware.ResultSetName = null;
            dgvHardware.RowHeadersVisible = false;
            dgvHardware.RowHeadersWidth = 51;
            dgvHardware.RowTemplate.Height = 24;
            dgvHardware.Size = new System.Drawing.Size(850, 363);
            dgvHardware.TabIndex = 0;
            dgvHardware.RowsAdded += DgvHardware_RowsAdded;
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
            // colManufacturer
            // 
            colManufacturer.DataPropertyName = "SystemManufacturer";
            colManufacturer.HeaderText = "Manufacturer";
            colManufacturer.MinimumWidth = 6;
            colManufacturer.Name = "colManufacturer";
            colManufacturer.ReadOnly = true;
            colManufacturer.Width = 121;
            // 
            // colModel
            // 
            colModel.DataPropertyName = "SystemProductName";
            colModel.HeaderText = "Model";
            colModel.MinimumWidth = 6;
            colModel.Name = "colModel";
            colModel.ReadOnly = true;
            colModel.Width = 75;
            // 
            // colProcessor
            // 
            colProcessor.DataPropertyName = "ProcessorNameString";
            colProcessor.HeaderText = "Processor";
            colProcessor.MinimumWidth = 6;
            colProcessor.Name = "colProcessor";
            colProcessor.ReadOnly = true;
            colProcessor.Width = 101;
            // 
            // SQLVersion
            // 
            SQLVersion.DataPropertyName = "SQLVersion";
            SQLVersion.HeaderText = "SQL Version";
            SQLVersion.MinimumWidth = 6;
            SQLVersion.Name = "SQLVersion";
            SQLVersion.ReadOnly = true;
            SQLVersion.Width = 125;
            // 
            // colCoresPerSocket
            // 
            colCoresPerSocket.DataPropertyName = "cores_per_socket";
            colCoresPerSocket.HeaderText = "Cores Per Socket";
            colCoresPerSocket.MinimumWidth = 6;
            colCoresPerSocket.Name = "colCoresPerSocket";
            colCoresPerSocket.ReadOnly = true;
            colCoresPerSocket.Width = 135;
            // 
            // colSockets
            // 
            colSockets.DataPropertyName = "socket_count";
            colSockets.HeaderText = "Sockets";
            colSockets.MinimumWidth = 6;
            colSockets.Name = "colSockets";
            colSockets.ReadOnly = true;
            colSockets.Width = 87;
            // 
            // colCPUs
            // 
            colCPUs.DataPropertyName = "cpu_count";
            colCPUs.HeaderText = "CPUs";
            colCPUs.MinimumWidth = 6;
            colCPUs.Name = "colCPUs";
            colCPUs.ReadOnly = true;
            colCPUs.Width = 72;
            // 
            // colCPUCores
            // 
            colCPUCores.DataPropertyName = "cpu_core_count";
            colCPUCores.HeaderText = "CPU Cores";
            colCPUCores.MinimumWidth = 6;
            colCPUCores.Name = "colCPUCores";
            colCPUCores.ReadOnly = true;
            colCPUCores.Width = 98;
            // 
            // colPhysicalCPUs
            // 
            colPhysicalCPUs.DataPropertyName = "physical_cpu_count";
            colPhysicalCPUs.HeaderText = "Physical CPUs";
            colPhysicalCPUs.MinimumWidth = 6;
            colPhysicalCPUs.Name = "colPhysicalCPUs";
            colPhysicalCPUs.ReadOnly = true;
            colPhysicalCPUs.Width = 118;
            // 
            // colHTRatio
            // 
            colHTRatio.DataPropertyName = "hyperthread_ratio";
            colHTRatio.HeaderText = "HT Ratio";
            colHTRatio.MinimumWidth = 6;
            colHTRatio.Name = "colHTRatio";
            colHTRatio.ReadOnly = true;
            colHTRatio.ToolTipText = "Hyperthread Ratio";
            colHTRatio.Width = 86;
            // 
            // colNUMA
            // 
            colNUMA.DataPropertyName = "numa_node_count";
            colNUMA.HeaderText = "NUMA nodes";
            colNUMA.MinimumWidth = 6;
            colNUMA.Name = "colNUMA";
            colNUMA.ReadOnly = true;
            colNUMA.Width = 110;
            // 
            // colSoftNUMA
            // 
            colSoftNUMA.DataPropertyName = "softnuma_configuration_desc";
            colSoftNUMA.HeaderText = "Soft NUMA";
            colSoftNUMA.MinimumWidth = 6;
            colSoftNUMA.Name = "colSoftNUMA";
            colSoftNUMA.ReadOnly = true;
            colSoftNUMA.Width = 98;
            // 
            // colAffinity
            // 
            colAffinity.DataPropertyName = "affinity_type_desc";
            colAffinity.HeaderText = "Affinity";
            colAffinity.MinimumWidth = 6;
            colAffinity.Name = "colAffinity";
            colAffinity.ReadOnly = true;
            colAffinity.Width = 79;
            // 
            // colPhysicalMemory
            // 
            colPhysicalMemory.DataPropertyName = "PhysicalMemoryGB";
            dataGridViewCellStyle22.Format = "N1";
            colPhysicalMemory.DefaultCellStyle = dataGridViewCellStyle22;
            colPhysicalMemory.HeaderText = "Physical Memory (GB)";
            colPhysicalMemory.MinimumWidth = 6;
            colPhysicalMemory.Name = "colPhysicalMemory";
            colPhysicalMemory.ReadOnly = true;
            colPhysicalMemory.Width = 135;
            // 
            // colBufferPool
            // 
            colBufferPool.DataPropertyName = "BufferPoolMB";
            dataGridViewCellStyle23.Format = "N0";
            colBufferPool.DefaultCellStyle = dataGridViewCellStyle23;
            colBufferPool.HeaderText = "Buffer Pool (MB)";
            colBufferPool.MinimumWidth = 6;
            colBufferPool.Name = "colBufferPool";
            colBufferPool.ReadOnly = true;
            colBufferPool.Width = 102;
            // 
            // colPctMemoryBufferPool
            // 
            colPctMemoryBufferPool.DataPropertyName = "PctMemoryAllocatedToBufferPool";
            dataGridViewCellStyle24.Format = "P1";
            colPctMemoryBufferPool.DefaultCellStyle = dataGridViewCellStyle24;
            colPctMemoryBufferPool.HeaderText = "% Memory allocated to buffer pool";
            colPctMemoryBufferPool.MinimumWidth = 6;
            colPctMemoryBufferPool.Name = "colPctMemoryBufferPool";
            colPctMemoryBufferPool.ReadOnly = true;
            colPctMemoryBufferPool.Width = 168;
            // 
            // colMemNotAllocated
            // 
            colMemNotAllocated.DataPropertyName = "MemoryNotAllocatedToBufferPoolGB";
            dataGridViewCellStyle25.Format = "N1";
            colMemNotAllocated.DefaultCellStyle = dataGridViewCellStyle25;
            colMemNotAllocated.HeaderText = "Memory not allocated to buffer pool (GB)";
            colMemNotAllocated.MinimumWidth = 6;
            colMemNotAllocated.Name = "colMemNotAllocated";
            colMemNotAllocated.ReadOnly = true;
            colMemNotAllocated.Width = 175;
            // 
            // colMemoryModel
            // 
            colMemoryModel.DataPropertyName = "sql_memory_model_desc";
            colMemoryModel.HeaderText = "Memory Model";
            colMemoryModel.MinimumWidth = 6;
            colMemoryModel.Name = "colMemoryModel";
            colMemoryModel.ReadOnly = true;
            colMemoryModel.Width = 118;
            // 
            // colPriority
            // 
            colPriority.DataPropertyName = "os_priority_class_desc";
            colPriority.HeaderText = "Priority";
            colPriority.MinimumWidth = 6;
            colPriority.Name = "colPriority";
            colPriority.ReadOnly = true;
            colPriority.Width = 81;
            // 
            // colMaxWorkerCount
            // 
            colMaxWorkerCount.DataPropertyName = "max_workers_count";
            colMaxWorkerCount.HeaderText = "Max Workers";
            colMaxWorkerCount.MinimumWidth = 6;
            colMaxWorkerCount.Name = "colMaxWorkerCount";
            colMaxWorkerCount.ReadOnly = true;
            colMaxWorkerCount.Width = 109;
            // 
            // colSchedulerCount
            // 
            colSchedulerCount.DataPropertyName = "scheduler_count";
            colSchedulerCount.HeaderText = "Scheduler Count";
            colSchedulerCount.MinimumWidth = 6;
            colSchedulerCount.Name = "colSchedulerCount";
            colSchedulerCount.ReadOnly = true;
            colSchedulerCount.Width = 130;
            // 
            // colOfflineSchedulers
            // 
            colOfflineSchedulers.DataPropertyName = "OfflineSchedulers";
            colOfflineSchedulers.HeaderText = "Offline Schedulers";
            colOfflineSchedulers.MinimumWidth = 6;
            colOfflineSchedulers.Name = "colOfflineSchedulers";
            colOfflineSchedulers.ReadOnly = true;
            colOfflineSchedulers.Width = 140;
            // 
            // colPowerPlan
            // 
            colPowerPlan.DataPropertyName = "ActivePowerPlan";
            colPowerPlan.HeaderText = "Power Plan";
            colPowerPlan.MinimumWidth = 6;
            colPowerPlan.Name = "colPowerPlan";
            colPowerPlan.ReadOnly = true;
            colPowerPlan.Width = 125;
            // 
            // colInstantFileInitialization
            // 
            colInstantFileInitialization.DataPropertyName = "InstantFileInitializationEnabled";
            colInstantFileInitialization.HeaderText = "Instant File Initialization";
            colInstantFileInitialization.MinimumWidth = 6;
            colInstantFileInitialization.Name = "colInstantFileInitialization";
            colInstantFileInitialization.ReadOnly = true;
            colInstantFileInitialization.Width = 166;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefreshHardware, tsCopy, tsExcel, tsCols, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(850, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(77, 24);
            toolStripLabel1.Text = "Hardware";
            // 
            // tsRefreshHardware
            // 
            tsRefreshHardware.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHardware.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHardware.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHardware.Name = "tsRefreshHardware";
            tsRefreshHardware.Size = new System.Drawing.Size(29, 24);
            tsRefreshHardware.Text = "Refresh";
            tsRefreshHardware.Click += TsRefreshHardware_Click;
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
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel2, tsRefreshHistory, tsCopyHistory, tsExcelHistory, tsHistoryCols, tsClearFilterHistory });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(850, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(61, 24);
            toolStripLabel2.Text = "History";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHistory.Name = "tsRefreshHistory";
            tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            tsRefreshHistory.Text = "Refresh";
            tsRefreshHistory.Click += TsRefreshHistory_Click;
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // tsClearFilterHistory
            // 
            tsClearFilterHistory.Image = Properties.Resources.Eraser_16x;
            tsClearFilterHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterHistory.Name = "tsClearFilterHistory";
            tsClearFilterHistory.Size = new System.Drawing.Size(104, 24);
            tsClearFilterHistory.Text = "Clear Filter";
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "SystemManufacturer";
            dataGridViewTextBoxColumn2.HeaderText = "Manufacturer";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 121;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "SystemProductName";
            dataGridViewTextBoxColumn3.HeaderText = "Model";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 75;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "ProcessorNameString";
            dataGridViewTextBoxColumn4.HeaderText = "Processor";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 101;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "cores_per_socket";
            dataGridViewTextBoxColumn5.HeaderText = "Cores Per Socket";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 135;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "socket_count";
            dataGridViewTextBoxColumn6.HeaderText = "Sockets";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 87;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "cpu_count";
            dataGridViewTextBoxColumn7.HeaderText = "CPUs";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 72;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "cpu_core_count";
            dataGridViewTextBoxColumn8.HeaderText = "CPU Cores";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 98;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "physical_cpu_count";
            dataGridViewTextBoxColumn9.HeaderText = "Physical CPUs";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 118;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "hyperthread_ratio";
            dataGridViewTextBoxColumn10.HeaderText = "HT Ratio";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.ToolTipText = "Hyperthread Ratio";
            dataGridViewTextBoxColumn10.Width = 86;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "numa_node_count";
            dataGridViewTextBoxColumn11.HeaderText = "NUMA nodes";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 110;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "softnuma_configuration_desc";
            dataGridViewTextBoxColumn12.HeaderText = "Soft NUMA";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 98;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "affinity_type_desc";
            dataGridViewTextBoxColumn13.HeaderText = "Affinity";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 79;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "PhysicalMemoryGB";
            dataGridViewCellStyle27.Format = "N1";
            dataGridViewTextBoxColumn14.DefaultCellStyle = dataGridViewCellStyle27;
            dataGridViewTextBoxColumn14.HeaderText = "Physical Memory (GB)";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 135;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "BufferPoolMB";
            dataGridViewCellStyle28.Format = "N0";
            dataGridViewTextBoxColumn15.DefaultCellStyle = dataGridViewCellStyle28;
            dataGridViewTextBoxColumn15.HeaderText = "Buffer Pool (MB)";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 102;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "PctMemoryAllocatedToBufferPool";
            dataGridViewCellStyle29.Format = "P1";
            dataGridViewTextBoxColumn16.DefaultCellStyle = dataGridViewCellStyle29;
            dataGridViewTextBoxColumn16.HeaderText = "% Memory allocated to buffer pool";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.ReadOnly = true;
            dataGridViewTextBoxColumn16.Width = 168;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "MemoryNotAllocatedToBufferPoolGB";
            dataGridViewCellStyle30.Format = "N1";
            dataGridViewTextBoxColumn17.DefaultCellStyle = dataGridViewCellStyle30;
            dataGridViewTextBoxColumn17.HeaderText = "Memory not allocated to buffer pool (GB)";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.ReadOnly = true;
            dataGridViewTextBoxColumn17.Width = 175;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "sql_memory_model_desc";
            dataGridViewTextBoxColumn18.HeaderText = "Memory Model";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.ReadOnly = true;
            dataGridViewTextBoxColumn18.Width = 118;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "OfflineSchedulers";
            dataGridViewTextBoxColumn19.HeaderText = "Offline Schedulers";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.ReadOnly = true;
            dataGridViewTextBoxColumn19.Width = 140;
            // 
            // dataGridViewTextBoxColumn20
            // 
            dataGridViewTextBoxColumn20.DataPropertyName = "ActivePowerPlan";
            dataGridViewTextBoxColumn20.HeaderText = "Power Plan";
            dataGridViewTextBoxColumn20.MinimumWidth = 6;
            dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            dataGridViewTextBoxColumn20.ReadOnly = true;
            dataGridViewTextBoxColumn20.Width = 125;
            // 
            // dataGridViewTextBoxColumn21
            // 
            dataGridViewTextBoxColumn21.DataPropertyName = "os_priority_class_desc";
            dataGridViewTextBoxColumn21.HeaderText = "Priority";
            dataGridViewTextBoxColumn21.MinimumWidth = 6;
            dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            dataGridViewTextBoxColumn21.ReadOnly = true;
            dataGridViewTextBoxColumn21.Width = 81;
            // 
            // dataGridViewTextBoxColumn22
            // 
            dataGridViewTextBoxColumn22.DataPropertyName = "InstantFileInitializationEnabled";
            dataGridViewTextBoxColumn22.HeaderText = "Instant File Initialization";
            dataGridViewTextBoxColumn22.MinimumWidth = 6;
            dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            dataGridViewTextBoxColumn22.ReadOnly = true;
            dataGridViewTextBoxColumn22.Width = 166;
            // 
            // dataGridViewTextBoxColumn23
            // 
            dataGridViewTextBoxColumn23.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn23.HeaderText = "Instance";
            dataGridViewTextBoxColumn23.MinimumWidth = 6;
            dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            dataGridViewTextBoxColumn23.ReadOnly = true;
            dataGridViewTextBoxColumn23.Width = 90;
            // 
            // dataGridViewTextBoxColumn24
            // 
            dataGridViewTextBoxColumn24.DataPropertyName = "ChangeDate";
            dataGridViewTextBoxColumn24.HeaderText = "Change Date";
            dataGridViewTextBoxColumn24.MinimumWidth = 6;
            dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            dataGridViewTextBoxColumn24.ReadOnly = true;
            dataGridViewTextBoxColumn24.Width = 110;
            // 
            // dataGridViewTextBoxColumn25
            // 
            dataGridViewTextBoxColumn25.DataPropertyName = "SystemManufacturerOld";
            dataGridViewTextBoxColumn25.HeaderText = "Manufacturer (Old)";
            dataGridViewTextBoxColumn25.MinimumWidth = 6;
            dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            dataGridViewTextBoxColumn25.ReadOnly = true;
            dataGridViewTextBoxColumn25.Width = 144;
            // 
            // dataGridViewTextBoxColumn26
            // 
            dataGridViewTextBoxColumn26.DataPropertyName = "SystemManufacturerNew";
            dataGridViewTextBoxColumn26.HeaderText = "Manufacturer (New)";
            dataGridViewTextBoxColumn26.MinimumWidth = 6;
            dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            dataGridViewTextBoxColumn26.ReadOnly = true;
            dataGridViewTextBoxColumn26.Width = 148;
            // 
            // dataGridViewTextBoxColumn27
            // 
            dataGridViewTextBoxColumn27.DataPropertyName = "SystemProductNameOld";
            dataGridViewTextBoxColumn27.HeaderText = "Model (Old)";
            dataGridViewTextBoxColumn27.MinimumWidth = 6;
            dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            dataGridViewTextBoxColumn27.ReadOnly = true;
            dataGridViewTextBoxColumn27.Width = 102;
            // 
            // dataGridViewTextBoxColumn28
            // 
            dataGridViewTextBoxColumn28.DataPropertyName = "SystemProductNameNew";
            dataGridViewTextBoxColumn28.HeaderText = "Model (New)";
            dataGridViewTextBoxColumn28.MinimumWidth = 6;
            dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            dataGridViewTextBoxColumn28.ReadOnly = true;
            dataGridViewTextBoxColumn28.Width = 107;
            // 
            // dataGridViewTextBoxColumn29
            // 
            dataGridViewTextBoxColumn29.DataPropertyName = "Processor_old";
            dataGridViewTextBoxColumn29.HeaderText = "Processor (Old)";
            dataGridViewTextBoxColumn29.MinimumWidth = 6;
            dataGridViewTextBoxColumn29.Name = "dataGridViewTextBoxColumn29";
            dataGridViewTextBoxColumn29.ReadOnly = true;
            dataGridViewTextBoxColumn29.Width = 126;
            // 
            // dataGridViewTextBoxColumn30
            // 
            dataGridViewTextBoxColumn30.DataPropertyName = "Processor_new";
            dataGridViewTextBoxColumn30.HeaderText = "Processor (New)";
            dataGridViewTextBoxColumn30.MinimumWidth = 6;
            dataGridViewTextBoxColumn30.Name = "dataGridViewTextBoxColumn30";
            dataGridViewTextBoxColumn30.ReadOnly = true;
            dataGridViewTextBoxColumn30.Width = 130;
            // 
            // dataGridViewTextBoxColumn31
            // 
            dataGridViewTextBoxColumn31.DataPropertyName = "cpu_count_old";
            dataGridViewTextBoxColumn31.HeaderText = "CPUs (Old)";
            dataGridViewTextBoxColumn31.MinimumWidth = 6;
            dataGridViewTextBoxColumn31.Name = "dataGridViewTextBoxColumn31";
            dataGridViewTextBoxColumn31.ReadOnly = true;
            dataGridViewTextBoxColumn31.Width = 125;
            // 
            // dataGridViewTextBoxColumn32
            // 
            dataGridViewTextBoxColumn32.DataPropertyName = "cpu_count_new";
            dataGridViewTextBoxColumn32.HeaderText = "CPUs (new)";
            dataGridViewTextBoxColumn32.MinimumWidth = 6;
            dataGridViewTextBoxColumn32.Name = "dataGridViewTextBoxColumn32";
            dataGridViewTextBoxColumn32.ReadOnly = true;
            dataGridViewTextBoxColumn32.Width = 102;
            // 
            // dataGridViewTextBoxColumn33
            // 
            dataGridViewTextBoxColumn33.DataPropertyName = "cores_per_socket_old";
            dataGridViewTextBoxColumn33.HeaderText = "Core per Socket (Old)";
            dataGridViewTextBoxColumn33.MinimumWidth = 6;
            dataGridViewTextBoxColumn33.Name = "dataGridViewTextBoxColumn33";
            dataGridViewTextBoxColumn33.ReadOnly = true;
            dataGridViewTextBoxColumn33.Width = 131;
            // 
            // dataGridViewTextBoxColumn34
            // 
            dataGridViewTextBoxColumn34.DataPropertyName = "cores_per_socket_new";
            dataGridViewTextBoxColumn34.HeaderText = "Cores per Socket (New)";
            dataGridViewTextBoxColumn34.MinimumWidth = 6;
            dataGridViewTextBoxColumn34.Name = "dataGridViewTextBoxColumn34";
            dataGridViewTextBoxColumn34.ReadOnly = true;
            dataGridViewTextBoxColumn34.Width = 137;
            // 
            // dataGridViewTextBoxColumn35
            // 
            dataGridViewTextBoxColumn35.DataPropertyName = "socket_count_old";
            dataGridViewTextBoxColumn35.HeaderText = "Socket Count (Old)";
            dataGridViewTextBoxColumn35.MinimumWidth = 6;
            dataGridViewTextBoxColumn35.Name = "dataGridViewTextBoxColumn35";
            dataGridViewTextBoxColumn35.ReadOnly = true;
            dataGridViewTextBoxColumn35.Width = 115;
            // 
            // dataGridViewTextBoxColumn36
            // 
            dataGridViewTextBoxColumn36.DataPropertyName = "socket_count_new";
            dataGridViewTextBoxColumn36.HeaderText = "Socket Count (New)";
            dataGridViewTextBoxColumn36.MinimumWidth = 6;
            dataGridViewTextBoxColumn36.Name = "dataGridViewTextBoxColumn36";
            dataGridViewTextBoxColumn36.ReadOnly = true;
            dataGridViewTextBoxColumn36.Width = 115;
            // 
            // dataGridViewTextBoxColumn37
            // 
            dataGridViewTextBoxColumn37.DataPropertyName = "hyperthread_ratio_old";
            dataGridViewTextBoxColumn37.HeaderText = "HT Ratio (Old)";
            dataGridViewTextBoxColumn37.MinimumWidth = 6;
            dataGridViewTextBoxColumn37.Name = "dataGridViewTextBoxColumn37";
            dataGridViewTextBoxColumn37.ReadOnly = true;
            dataGridViewTextBoxColumn37.Width = 118;
            // 
            // dataGridViewTextBoxColumn38
            // 
            dataGridViewTextBoxColumn38.DataPropertyName = "hyperthread_ratio_new";
            dataGridViewTextBoxColumn38.HeaderText = "HT Ratio (New)";
            dataGridViewTextBoxColumn38.MinimumWidth = 6;
            dataGridViewTextBoxColumn38.Name = "dataGridViewTextBoxColumn38";
            dataGridViewTextBoxColumn38.ReadOnly = true;
            dataGridViewTextBoxColumn38.Width = 123;
            // 
            // dataGridViewTextBoxColumn39
            // 
            dataGridViewTextBoxColumn39.DataPropertyName = "physical_memory_gb_old";
            dataGridViewCellStyle31.Format = "N1";
            dataGridViewTextBoxColumn39.DefaultCellStyle = dataGridViewCellStyle31;
            dataGridViewTextBoxColumn39.HeaderText = "Physical Memory (Old)";
            dataGridViewTextBoxColumn39.MinimumWidth = 6;
            dataGridViewTextBoxColumn39.Name = "dataGridViewTextBoxColumn39";
            dataGridViewTextBoxColumn39.ReadOnly = true;
            dataGridViewTextBoxColumn39.Width = 135;
            // 
            // dataGridViewTextBoxColumn40
            // 
            dataGridViewTextBoxColumn40.DataPropertyName = "physical_memory_gb_new";
            dataGridViewCellStyle32.Format = "N1";
            dataGridViewTextBoxColumn40.DefaultCellStyle = dataGridViewCellStyle32;
            dataGridViewTextBoxColumn40.HeaderText = "Physical Memory (New)";
            dataGridViewTextBoxColumn40.MinimumWidth = 6;
            dataGridViewTextBoxColumn40.Name = "dataGridViewTextBoxColumn40";
            dataGridViewTextBoxColumn40.ReadOnly = true;
            dataGridViewTextBoxColumn40.Width = 135;
            // 
            // tsHistoryCols
            // 
            tsHistoryCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHistoryCols.Image = Properties.Resources.Column_16x;
            tsHistoryCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHistoryCols.Name = "tsHistoryCols";
            tsHistoryCols.Size = new System.Drawing.Size(29, 24);
            tsHistoryCols.Text = "Columns";
            tsHistoryCols.Click += TsHistoryCols_Click;
            // 
            // HardwareChanges
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "HardwareChanges";
            Size = new System.Drawing.Size(850, 786);
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvHardware).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgvHistory;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvHardware;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsRefreshHardware;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
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
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.ToolStripButton tsCols;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn SQLVersion;
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
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripButton tsClearFilterHistory;
        private System.Windows.Forms.ToolStripButton tsHistoryCols;
    }
}
