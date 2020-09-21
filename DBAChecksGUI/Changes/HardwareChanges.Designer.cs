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
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
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
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(850, 629);
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
            this.ChangeDate.HeaderText = "ChangeDate";
            this.ChangeDate.MinimumWidth = 6;
            this.ChangeDate.Name = "ChangeDate";
            this.ChangeDate.ReadOnly = true;
            this.ChangeDate.Width = 116;
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
            this.Controls.Add(this.dgv);
            this.Name = "HardwareChanges";
            this.Size = new System.Drawing.Size(850, 629);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
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
