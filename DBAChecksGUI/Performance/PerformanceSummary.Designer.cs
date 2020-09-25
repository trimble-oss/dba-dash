namespace DBAChecksGUI.Performance
{
    partial class PerformanceSummary
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.ConnectionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AvgCPU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxCPU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CriticalWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LockWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IOWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LatchWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Latency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReadLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WriteLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxReadMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxWriteMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReadIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WriteIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxReadIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxWriteIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MaxIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ConnectionID,
            this.AvgCPU,
            this.MaxCPU,
            this.CriticalWaitMsPerSec,
            this.LockWaitMsPerSec,
            this.IOWaitMsPerSec,
            this.WaitMsPerSec,
            this.LatchWaitMsPerSec,
            this.Latency,
            this.ReadLatency,
            this.WriteLatency,
            this.MBsec,
            this.MaxReadMBsec,
            this.MaxWriteMBsec,
            this.MaxMBsec,
            this.IOPs,
            this.ReadIOPs,
            this.WriteIOPs,
            this.MaxReadIOPs,
            this.MaxWriteIOPs,
            this.MaxIOPs});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(1048, 626);
            this.dgv.TabIndex = 0;
            this.dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            // 
            // ConnectionID
            // 
            this.ConnectionID.DataPropertyName = "ConnectionID";
            this.ConnectionID.HeaderText = "Instance";
            this.ConnectionID.MinimumWidth = 6;
            this.ConnectionID.Name = "ConnectionID";
            this.ConnectionID.ReadOnly = true;
            this.ConnectionID.Width = 90;
            // 
            // AvgCPU
            // 
            this.AvgCPU.DataPropertyName = "AvgCPU";
            dataGridViewCellStyle1.Format = "0.#\\%";
            dataGridViewCellStyle1.NullValue = null;
            this.AvgCPU.DefaultCellStyle = dataGridViewCellStyle1;
            this.AvgCPU.HeaderText = "Avg CPU";
            this.AvgCPU.MinimumWidth = 6;
            this.AvgCPU.Name = "AvgCPU";
            this.AvgCPU.ReadOnly = true;
            this.AvgCPU.Width = 86;
            // 
            // MaxCPU
            // 
            this.MaxCPU.DataPropertyName = "MaxCPU";
            dataGridViewCellStyle2.Format = "0\\%";
            this.MaxCPU.DefaultCellStyle = dataGridViewCellStyle2;
            this.MaxCPU.HeaderText = "Max CPU";
            this.MaxCPU.MinimumWidth = 6;
            this.MaxCPU.Name = "MaxCPU";
            this.MaxCPU.ReadOnly = true;
            this.MaxCPU.Width = 87;
            // 
            // CriticalWaitMsPerSec
            // 
            this.CriticalWaitMsPerSec.DataPropertyName = "CriticalWaitMsPerSec";
            dataGridViewCellStyle3.Format = "N2";
            dataGridViewCellStyle3.NullValue = null;
            this.CriticalWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle3;
            this.CriticalWaitMsPerSec.HeaderText = "Critical Wait (ms/sec)";
            this.CriticalWaitMsPerSec.MinimumWidth = 6;
            this.CriticalWaitMsPerSec.Name = "CriticalWaitMsPerSec";
            this.CriticalWaitMsPerSec.ReadOnly = true;
            this.CriticalWaitMsPerSec.Width = 154;
            // 
            // LockWaitMsPerSec
            // 
            this.LockWaitMsPerSec.DataPropertyName = "LockWaitMsPerSec";
            dataGridViewCellStyle4.Format = "N2";
            this.LockWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle4;
            this.LockWaitMsPerSec.HeaderText = "Lock Wait (ms/sec)";
            this.LockWaitMsPerSec.MinimumWidth = 6;
            this.LockWaitMsPerSec.Name = "LockWaitMsPerSec";
            this.LockWaitMsPerSec.ReadOnly = true;
            this.LockWaitMsPerSec.Width = 144;
            // 
            // IOWaitMsPerSec
            // 
            this.IOWaitMsPerSec.DataPropertyName = "IOWaitMsPerSec";
            dataGridViewCellStyle5.Format = "N2";
            this.IOWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle5;
            this.IOWaitMsPerSec.HeaderText = "IO Wait (ms/sec)";
            this.IOWaitMsPerSec.MinimumWidth = 6;
            this.IOWaitMsPerSec.Name = "IOWaitMsPerSec";
            this.IOWaitMsPerSec.ReadOnly = true;
            this.IOWaitMsPerSec.Width = 129;
            // 
            // WaitMsPerSec
            // 
            this.WaitMsPerSec.DataPropertyName = "WaitMsPerSec";
            dataGridViewCellStyle6.Format = "N2";
            this.WaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle6;
            this.WaitMsPerSec.HeaderText = "Total Wait (ms/sec)";
            this.WaitMsPerSec.MinimumWidth = 6;
            this.WaitMsPerSec.Name = "WaitMsPerSec";
            this.WaitMsPerSec.ReadOnly = true;
            this.WaitMsPerSec.Width = 145;
            // 
            // LatchWaitMsPerSec
            // 
            this.LatchWaitMsPerSec.DataPropertyName = "LatchWaitMsPerSec";
            dataGridViewCellStyle7.Format = "N2";
            this.LatchWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle7;
            this.LatchWaitMsPerSec.HeaderText = "Latch Wait (ms/sec)";
            this.LatchWaitMsPerSec.MinimumWidth = 6;
            this.LatchWaitMsPerSec.Name = "LatchWaitMsPerSec";
            this.LatchWaitMsPerSec.ReadOnly = true;
            this.LatchWaitMsPerSec.Width = 148;
            // 
            // Latency
            // 
            this.Latency.DataPropertyName = "Latency";
            dataGridViewCellStyle8.Format = "N1";
            this.Latency.DefaultCellStyle = dataGridViewCellStyle8;
            this.Latency.HeaderText = "IO Latency (ms)";
            this.Latency.MinimumWidth = 6;
            this.Latency.Name = "Latency";
            this.Latency.ReadOnly = true;
            this.Latency.Visible = false;
            this.Latency.Width = 126;
            // 
            // ReadLatency
            // 
            this.ReadLatency.DataPropertyName = "ReadLatency";
            dataGridViewCellStyle9.Format = "N1";
            this.ReadLatency.DefaultCellStyle = dataGridViewCellStyle9;
            this.ReadLatency.HeaderText = "Read Latency (ms)";
            this.ReadLatency.MinimumWidth = 6;
            this.ReadLatency.Name = "ReadLatency";
            this.ReadLatency.ReadOnly = true;
            this.ReadLatency.Width = 144;
            // 
            // WriteLatency
            // 
            this.WriteLatency.DataPropertyName = "WriteLatency";
            dataGridViewCellStyle10.Format = "N1";
            this.WriteLatency.DefaultCellStyle = dataGridViewCellStyle10;
            this.WriteLatency.HeaderText = "Write Latency (ms)";
            this.WriteLatency.MinimumWidth = 6;
            this.WriteLatency.Name = "WriteLatency";
            this.WriteLatency.ReadOnly = true;
            this.WriteLatency.Width = 143;
            // 
            // MBsec
            // 
            this.MBsec.DataPropertyName = "MBsec";
            dataGridViewCellStyle11.Format = "N1";
            this.MBsec.DefaultCellStyle = dataGridViewCellStyle11;
            this.MBsec.HeaderText = "MB/sec";
            this.MBsec.MinimumWidth = 6;
            this.MBsec.Name = "MBsec";
            this.MBsec.ReadOnly = true;
            this.MBsec.Width = 83;
            // 
            // MaxReadMBsec
            // 
            this.MaxReadMBsec.DataPropertyName = "MaxReadMBsec";
            dataGridViewCellStyle12.Format = "N1";
            this.MaxReadMBsec.DefaultCellStyle = dataGridViewCellStyle12;
            this.MaxReadMBsec.HeaderText = "Max Read MB/sec";
            this.MaxReadMBsec.MinimumWidth = 6;
            this.MaxReadMBsec.Name = "MaxReadMBsec";
            this.MaxReadMBsec.ReadOnly = true;
            this.MaxReadMBsec.Visible = false;
            this.MaxReadMBsec.Width = 137;
            // 
            // MaxWriteMBsec
            // 
            this.MaxWriteMBsec.DataPropertyName = "MaxWriteMBsec";
            dataGridViewCellStyle13.Format = "N1";
            this.MaxWriteMBsec.DefaultCellStyle = dataGridViewCellStyle13;
            this.MaxWriteMBsec.HeaderText = "Max Write MB/sec";
            this.MaxWriteMBsec.MinimumWidth = 6;
            this.MaxWriteMBsec.Name = "MaxWriteMBsec";
            this.MaxWriteMBsec.ReadOnly = true;
            this.MaxWriteMBsec.Visible = false;
            this.MaxWriteMBsec.Width = 136;
            // 
            // MaxMBsec
            // 
            this.MaxMBsec.DataPropertyName = "MaxMBsec";
            dataGridViewCellStyle14.Format = "N1";
            this.MaxMBsec.DefaultCellStyle = dataGridViewCellStyle14;
            this.MaxMBsec.HeaderText = "Max MB/sec";
            this.MaxMBsec.MinimumWidth = 6;
            this.MaxMBsec.Name = "MaxMBsec";
            this.MaxMBsec.ReadOnly = true;
            this.MaxMBsec.Visible = false;
            this.MaxMBsec.Width = 103;
            // 
            // IOPs
            // 
            this.IOPs.DataPropertyName = "IOPs";
            dataGridViewCellStyle15.Format = "N0";
            this.IOPs.DefaultCellStyle = dataGridViewCellStyle15;
            this.IOPs.HeaderText = "IOPs";
            this.IOPs.MinimumWidth = 6;
            this.IOPs.Name = "IOPs";
            this.IOPs.ReadOnly = true;
            this.IOPs.Width = 67;
            // 
            // ReadIOPs
            // 
            this.ReadIOPs.DataPropertyName = "ReadIOPs";
            dataGridViewCellStyle16.Format = "N0";
            this.ReadIOPs.DefaultCellStyle = dataGridViewCellStyle16;
            this.ReadIOPs.HeaderText = "Read IOPs";
            this.ReadIOPs.MinimumWidth = 6;
            this.ReadIOPs.Name = "ReadIOPs";
            this.ReadIOPs.ReadOnly = true;
            this.ReadIOPs.Visible = false;
            this.ReadIOPs.Width = 97;
            // 
            // WriteIOPs
            // 
            this.WriteIOPs.DataPropertyName = "WriteIOPs";
            dataGridViewCellStyle17.Format = "N0";
            this.WriteIOPs.DefaultCellStyle = dataGridViewCellStyle17;
            this.WriteIOPs.HeaderText = "Write IOPs";
            this.WriteIOPs.MinimumWidth = 6;
            this.WriteIOPs.Name = "WriteIOPs";
            this.WriteIOPs.ReadOnly = true;
            this.WriteIOPs.Visible = false;
            this.WriteIOPs.Width = 96;
            // 
            // MaxReadIOPs
            // 
            this.MaxReadIOPs.DataPropertyName = "MaxReadIOPs";
            dataGridViewCellStyle18.Format = "N0";
            this.MaxReadIOPs.DefaultCellStyle = dataGridViewCellStyle18;
            this.MaxReadIOPs.HeaderText = "Max Read IOPs";
            this.MaxReadIOPs.MinimumWidth = 6;
            this.MaxReadIOPs.Name = "MaxReadIOPs";
            this.MaxReadIOPs.ReadOnly = true;
            this.MaxReadIOPs.Visible = false;
            this.MaxReadIOPs.Width = 123;
            // 
            // MaxWriteIOPs
            // 
            this.MaxWriteIOPs.DataPropertyName = "MaxWriteIOPs";
            dataGridViewCellStyle19.Format = "N0";
            this.MaxWriteIOPs.DefaultCellStyle = dataGridViewCellStyle19;
            this.MaxWriteIOPs.HeaderText = "Max Write IOPs";
            this.MaxWriteIOPs.MinimumWidth = 6;
            this.MaxWriteIOPs.Name = "MaxWriteIOPs";
            this.MaxWriteIOPs.ReadOnly = true;
            this.MaxWriteIOPs.Visible = false;
            this.MaxWriteIOPs.Width = 122;
            // 
            // MaxIOPs
            // 
            this.MaxIOPs.DataPropertyName = "MaxIOPs";
            dataGridViewCellStyle20.Format = "N0";
            this.MaxIOPs.DefaultCellStyle = dataGridViewCellStyle20;
            this.MaxIOPs.HeaderText = "Max IOPs";
            this.MaxIOPs.MinimumWidth = 6;
            this.MaxIOPs.Name = "MaxIOPs";
            this.MaxIOPs.ReadOnly = true;
            this.MaxIOPs.Visible = false;
            this.MaxIOPs.Width = 89;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsTime,
            this.tsColumns});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1048, 27);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsTime
            // 
            this.tsTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem1,
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.days7ToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBAChecksGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 24);
            this.tsTime.Text = "Time";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem2.Tag = "5";
            this.toolStripMenuItem2.Text = "5 Mins";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem1.Tag = "15";
            this.toolStripMenuItem1.Text = "15 Mins";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(143, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(143, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(143, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(143, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(143, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(143, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(140, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(143, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.tsCustom_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsColumns
            // 
            this.tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsColumns.Image = global::DBAChecksGUI.Properties.Resources.Column_16x;
            this.tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsColumns.Name = "tsColumns";
            this.tsColumns.Size = new System.Drawing.Size(34, 24);
            this.tsColumns.Text = "Columns";
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
            // PerformanceSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "PerformanceSummary";
            this.Size = new System.Drawing.Size(1048, 653);
            this.Load += new System.EventHandler(this.PerformanceSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripDropDownButton tsColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConnectionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgCPU;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxCPU;
        private System.Windows.Forms.DataGridViewTextBoxColumn CriticalWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn LockWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn IOWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn WaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn LatchWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn Latency;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReadLatency;
        private System.Windows.Forms.DataGridViewTextBoxColumn WriteLatency;
        private System.Windows.Forms.DataGridViewTextBoxColumn MBsec;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxReadMBsec;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxWriteMBsec;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxMBsec;
        private System.Windows.Forms.DataGridViewTextBoxColumn IOPs;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReadIOPs;
        private System.Windows.Forms.DataGridViewTextBoxColumn WriteIOPs;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxReadIOPs;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxWriteIOPs;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxIOPs;
        private System.Windows.Forms.ToolStripButton tsCopy;
    }
}
