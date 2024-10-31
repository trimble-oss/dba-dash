using DBADashGUI.CustomReports;

namespace DBADashGUI.Performance
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle23 = new System.Windows.Forms.DataGridViewCellStyle();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PerformanceSummary));
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle41 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle42 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle43 = new System.Windows.Forms.DataGridViewCellStyle();
            dgv = new DBADashDataGridView();
            ConnectionID = new System.Windows.Forms.DataGridViewLinkColumn();
            AvgCPU = new CustomProgressControl.DataGridViewProgressBarColumn();
            MaxCPU = new CustomProgressControl.DataGridViewProgressBarColumn();
            colCPUHistogram = new System.Windows.Forms.DataGridViewImageColumn();
            CriticalWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LockWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IOWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            WaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSignalWaitPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LatchWaitMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Latency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ReadLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            WriteLatency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxReadMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxWriteMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxMBsec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ReadIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            WriteIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxReadIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxWriteIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxIOPs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripDropDownButton();
            standardColumnsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            performanceCounterColumnsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsSaveView = new System.Windows.Forms.ToolStripButton();
            tsDeleteView = new System.Windows.Forms.ToolStripButton();
            savedViewMenuItem1 = new SavedViewMenuItem();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewProgressBarColumn1 = new CustomProgressControl.DataGridViewProgressBarColumn();
            dataGridViewProgressBarColumn2 = new CustomProgressControl.DataGridViewProgressBarColumn();
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
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
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
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeight = 70;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { ConnectionID, AvgCPU, MaxCPU, colCPUHistogram, CriticalWaitMsPerSec, LockWaitMsPerSec, IOWaitMsPerSec, WaitMsPerSec, colSignalWaitPct, LatchWaitMsPerSec, Latency, ReadLatency, WriteLatency, MBsec, MaxReadMBsec, MaxWriteMBsec, MaxMBsec, IOPs, ReadIOPs, WriteIOPs, MaxReadIOPs, MaxWriteIOPs, MaxIOPs });
            dataGridViewCellStyle23.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle23.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle23.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle23.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle23.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle23.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle23.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle23;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1048, 789);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // ConnectionID
            // 
            ConnectionID.DataPropertyName = "InstanceDisplayName";
            ConnectionID.HeaderText = "Instance";
            ConnectionID.MinimumWidth = 6;
            ConnectionID.Name = "ConnectionID";
            ConnectionID.ReadOnly = true;
            ConnectionID.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ConnectionID.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            ConnectionID.Width = 250;
            // 
            // AvgCPU
            // 
            AvgCPU.DataPropertyName = "AvgCPU";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.Format = "0.#\\%";
            dataGridViewCellStyle2.NullValue = null;
            AvgCPU.DefaultCellStyle = dataGridViewCellStyle2;
            AvgCPU.HeaderText = "Avg CPU";
            AvgCPU.MinimumWidth = 6;
            AvgCPU.Name = "AvgCPU";
            AvgCPU.ReadOnly = true;
            AvgCPU.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            AvgCPU.Width = 80;
            // 
            // MaxCPU
            // 
            MaxCPU.DataPropertyName = "MaxCPU";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.Format = "0\\%";
            MaxCPU.DefaultCellStyle = dataGridViewCellStyle3;
            MaxCPU.HeaderText = "Max CPU";
            MaxCPU.MinimumWidth = 6;
            MaxCPU.Name = "MaxCPU";
            MaxCPU.ReadOnly = true;
            MaxCPU.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            MaxCPU.Width = 80;
            // 
            // colCPUHistogram
            // 
            colCPUHistogram.DataPropertyName = "CPUHistogram";
            colCPUHistogram.HeaderText = "CPU Histogram";
            colCPUHistogram.MinimumWidth = 6;
            colCPUHistogram.Name = "colCPUHistogram";
            colCPUHistogram.ReadOnly = true;
            colCPUHistogram.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colCPUHistogram.Visible = false;
            colCPUHistogram.Width = 110;
            // 
            // CriticalWaitMsPerSec
            // 
            CriticalWaitMsPerSec.DataPropertyName = "CriticalWaitMsPerSec";
            dataGridViewCellStyle4.Format = "N2";
            dataGridViewCellStyle4.NullValue = null;
            CriticalWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle4;
            CriticalWaitMsPerSec.HeaderText = "Critical Wait (ms/sec)";
            CriticalWaitMsPerSec.MinimumWidth = 6;
            CriticalWaitMsPerSec.Name = "CriticalWaitMsPerSec";
            CriticalWaitMsPerSec.ReadOnly = true;
            CriticalWaitMsPerSec.Width = 70;
            // 
            // LockWaitMsPerSec
            // 
            LockWaitMsPerSec.DataPropertyName = "LockWaitMsPerSec";
            dataGridViewCellStyle5.Format = "N2";
            LockWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle5;
            LockWaitMsPerSec.HeaderText = "Lock Wait (ms/sec)";
            LockWaitMsPerSec.MinimumWidth = 6;
            LockWaitMsPerSec.Name = "LockWaitMsPerSec";
            LockWaitMsPerSec.ReadOnly = true;
            LockWaitMsPerSec.Width = 70;
            // 
            // IOWaitMsPerSec
            // 
            IOWaitMsPerSec.DataPropertyName = "IOWaitMsPerSec";
            dataGridViewCellStyle6.Format = "N2";
            IOWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle6;
            IOWaitMsPerSec.HeaderText = "IO Wait (ms/sec)";
            IOWaitMsPerSec.MinimumWidth = 6;
            IOWaitMsPerSec.Name = "IOWaitMsPerSec";
            IOWaitMsPerSec.ReadOnly = true;
            IOWaitMsPerSec.Width = 70;
            // 
            // WaitMsPerSec
            // 
            WaitMsPerSec.DataPropertyName = "WaitMsPerSec";
            dataGridViewCellStyle7.Format = "N2";
            WaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle7;
            WaitMsPerSec.HeaderText = "Total Wait (ms/sec)";
            WaitMsPerSec.MinimumWidth = 6;
            WaitMsPerSec.Name = "WaitMsPerSec";
            WaitMsPerSec.ReadOnly = true;
            WaitMsPerSec.Width = 70;
            // 
            // colSignalWaitPct
            // 
            colSignalWaitPct.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle8.Format = "P1";
            dataGridViewCellStyle8.NullValue = null;
            colSignalWaitPct.DefaultCellStyle = dataGridViewCellStyle8;
            colSignalWaitPct.HeaderText = "Signal Wait %";
            colSignalWaitPct.MinimumWidth = 6;
            colSignalWaitPct.Name = "colSignalWaitPct";
            colSignalWaitPct.ReadOnly = true;
            colSignalWaitPct.Width = 70;
            // 
            // LatchWaitMsPerSec
            // 
            LatchWaitMsPerSec.DataPropertyName = "LatchWaitMsPerSec";
            dataGridViewCellStyle9.Format = "N2";
            LatchWaitMsPerSec.DefaultCellStyle = dataGridViewCellStyle9;
            LatchWaitMsPerSec.HeaderText = "Latch Wait (ms/sec)";
            LatchWaitMsPerSec.MinimumWidth = 6;
            LatchWaitMsPerSec.Name = "LatchWaitMsPerSec";
            LatchWaitMsPerSec.ReadOnly = true;
            LatchWaitMsPerSec.Width = 70;
            // 
            // Latency
            // 
            Latency.DataPropertyName = "Latency";
            dataGridViewCellStyle10.Format = "N1";
            Latency.DefaultCellStyle = dataGridViewCellStyle10;
            Latency.HeaderText = "IO Latency (ms)";
            Latency.MinimumWidth = 6;
            Latency.Name = "Latency";
            Latency.ReadOnly = true;
            Latency.Visible = false;
            Latency.Width = 70;
            // 
            // ReadLatency
            // 
            ReadLatency.DataPropertyName = "ReadLatency";
            dataGridViewCellStyle11.Format = "N1";
            ReadLatency.DefaultCellStyle = dataGridViewCellStyle11;
            ReadLatency.HeaderText = "Read Latency (ms)";
            ReadLatency.MinimumWidth = 6;
            ReadLatency.Name = "ReadLatency";
            ReadLatency.ReadOnly = true;
            ReadLatency.Width = 70;
            // 
            // WriteLatency
            // 
            WriteLatency.DataPropertyName = "WriteLatency";
            dataGridViewCellStyle12.Format = "N1";
            WriteLatency.DefaultCellStyle = dataGridViewCellStyle12;
            WriteLatency.HeaderText = "Write Latency (ms)";
            WriteLatency.MinimumWidth = 6;
            WriteLatency.Name = "WriteLatency";
            WriteLatency.ReadOnly = true;
            WriteLatency.Width = 70;
            // 
            // MBsec
            // 
            MBsec.DataPropertyName = "MBsec";
            dataGridViewCellStyle13.Format = "N1";
            MBsec.DefaultCellStyle = dataGridViewCellStyle13;
            MBsec.HeaderText = "MB/sec";
            MBsec.MinimumWidth = 6;
            MBsec.Name = "MBsec";
            MBsec.ReadOnly = true;
            MBsec.Width = 70;
            // 
            // MaxReadMBsec
            // 
            MaxReadMBsec.DataPropertyName = "MaxReadMBsec";
            dataGridViewCellStyle14.Format = "N1";
            MaxReadMBsec.DefaultCellStyle = dataGridViewCellStyle14;
            MaxReadMBsec.HeaderText = "Max Read MB/sec";
            MaxReadMBsec.MinimumWidth = 6;
            MaxReadMBsec.Name = "MaxReadMBsec";
            MaxReadMBsec.ReadOnly = true;
            MaxReadMBsec.Visible = false;
            MaxReadMBsec.Width = 70;
            // 
            // MaxWriteMBsec
            // 
            MaxWriteMBsec.DataPropertyName = "MaxWriteMBsec";
            dataGridViewCellStyle15.Format = "N1";
            MaxWriteMBsec.DefaultCellStyle = dataGridViewCellStyle15;
            MaxWriteMBsec.HeaderText = "Max Write MB/sec";
            MaxWriteMBsec.MinimumWidth = 6;
            MaxWriteMBsec.Name = "MaxWriteMBsec";
            MaxWriteMBsec.ReadOnly = true;
            MaxWriteMBsec.Visible = false;
            MaxWriteMBsec.Width = 70;
            // 
            // MaxMBsec
            // 
            MaxMBsec.DataPropertyName = "MaxMBsec";
            dataGridViewCellStyle16.Format = "N1";
            MaxMBsec.DefaultCellStyle = dataGridViewCellStyle16;
            MaxMBsec.HeaderText = "Max MB/sec";
            MaxMBsec.MinimumWidth = 6;
            MaxMBsec.Name = "MaxMBsec";
            MaxMBsec.ReadOnly = true;
            MaxMBsec.Visible = false;
            MaxMBsec.Width = 70;
            // 
            // IOPs
            // 
            IOPs.DataPropertyName = "IOPs";
            dataGridViewCellStyle17.Format = "N0";
            IOPs.DefaultCellStyle = dataGridViewCellStyle17;
            IOPs.HeaderText = "IOPs";
            IOPs.MinimumWidth = 6;
            IOPs.Name = "IOPs";
            IOPs.ReadOnly = true;
            IOPs.Width = 70;
            // 
            // ReadIOPs
            // 
            ReadIOPs.DataPropertyName = "ReadIOPs";
            dataGridViewCellStyle18.Format = "N0";
            ReadIOPs.DefaultCellStyle = dataGridViewCellStyle18;
            ReadIOPs.HeaderText = "Read IOPs";
            ReadIOPs.MinimumWidth = 6;
            ReadIOPs.Name = "ReadIOPs";
            ReadIOPs.ReadOnly = true;
            ReadIOPs.Visible = false;
            ReadIOPs.Width = 70;
            // 
            // WriteIOPs
            // 
            WriteIOPs.DataPropertyName = "WriteIOPs";
            dataGridViewCellStyle19.Format = "N0";
            WriteIOPs.DefaultCellStyle = dataGridViewCellStyle19;
            WriteIOPs.HeaderText = "Write IOPs";
            WriteIOPs.MinimumWidth = 6;
            WriteIOPs.Name = "WriteIOPs";
            WriteIOPs.ReadOnly = true;
            WriteIOPs.Visible = false;
            WriteIOPs.Width = 70;
            // 
            // MaxReadIOPs
            // 
            MaxReadIOPs.DataPropertyName = "MaxReadIOPs";
            dataGridViewCellStyle20.Format = "N0";
            MaxReadIOPs.DefaultCellStyle = dataGridViewCellStyle20;
            MaxReadIOPs.HeaderText = "Max Read IOPs";
            MaxReadIOPs.MinimumWidth = 6;
            MaxReadIOPs.Name = "MaxReadIOPs";
            MaxReadIOPs.ReadOnly = true;
            MaxReadIOPs.Visible = false;
            MaxReadIOPs.Width = 70;
            // 
            // MaxWriteIOPs
            // 
            MaxWriteIOPs.DataPropertyName = "MaxWriteIOPs";
            dataGridViewCellStyle21.Format = "N0";
            MaxWriteIOPs.DefaultCellStyle = dataGridViewCellStyle21;
            MaxWriteIOPs.HeaderText = "Max Write IOPs";
            MaxWriteIOPs.MinimumWidth = 6;
            MaxWriteIOPs.Name = "MaxWriteIOPs";
            MaxWriteIOPs.ReadOnly = true;
            MaxWriteIOPs.Visible = false;
            MaxWriteIOPs.Width = 70;
            // 
            // MaxIOPs
            // 
            MaxIOPs.DataPropertyName = "MaxIOPs";
            dataGridViewCellStyle22.Format = "N0";
            MaxIOPs.DefaultCellStyle = dataGridViewCellStyle22;
            MaxIOPs.HeaderText = "Max IOPs";
            MaxIOPs.MinimumWidth = 6;
            MaxIOPs.Name = "MaxIOPs";
            MaxIOPs.ReadOnly = true;
            MaxIOPs.Visible = false;
            MaxIOPs.Width = 70;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsCols, tsSaveView, tsDeleteView, savedViewMenuItem1, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1048, 27);
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
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { standardColumnsToolStripMenuItem, performanceCounterColumnsToolStripMenuItem });
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(34, 24);
            tsCols.Text = "Columns";
            // 
            // standardColumnsToolStripMenuItem
            // 
            standardColumnsToolStripMenuItem.Image = Properties.Resources.Column_16x;
            standardColumnsToolStripMenuItem.Name = "standardColumnsToolStripMenuItem";
            standardColumnsToolStripMenuItem.Size = new System.Drawing.Size(292, 26);
            standardColumnsToolStripMenuItem.Text = "Standard Columns";
            standardColumnsToolStripMenuItem.ToolTipText = "Select columns from the standard column selection";
            standardColumnsToolStripMenuItem.Click += StandardColumnsToolStripMenuItem_Click;
            // 
            // performanceCounterColumnsToolStripMenuItem
            // 
            performanceCounterColumnsToolStripMenuItem.Image = Properties.Resources.LineChart_16x;
            performanceCounterColumnsToolStripMenuItem.Name = "performanceCounterColumnsToolStripMenuItem";
            performanceCounterColumnsToolStripMenuItem.Size = new System.Drawing.Size(292, 26);
            performanceCounterColumnsToolStripMenuItem.Text = "Performance Counter Columns";
            performanceCounterColumnsToolStripMenuItem.ToolTipText = "Select performance counter columns to add to the grid";
            performanceCounterColumnsToolStripMenuItem.Click += PerformanceCounterColumnsToolStripMenuItem_Click;
            // 
            // tsSaveView
            // 
            tsSaveView.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsSaveView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSaveView.Image = Properties.Resources.Save_16x;
            tsSaveView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSaveView.Name = "tsSaveView";
            tsSaveView.Size = new System.Drawing.Size(29, 24);
            tsSaveView.Text = "Save Layout";
            tsSaveView.Click += TsSave_Click;
            // 
            // tsDeleteView
            // 
            tsDeleteView.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDeleteView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsDeleteView.Image = Properties.Resources.Close_red_16x;
            tsDeleteView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDeleteView.Name = "tsDeleteView";
            tsDeleteView.Size = new System.Drawing.Size(29, 24);
            tsDeleteView.Text = "Delete Layout";
            tsDeleteView.Click += TsDeleteView_Click;
            // 
            // savedViewMenuItem1
            // 
            savedViewMenuItem1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            savedViewMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            savedViewMenuItem1.Image = (System.Drawing.Image)resources.GetObject("savedViewMenuItem1.Image");
            savedViewMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta;
            savedViewMenuItem1.Name = "savedViewMenuItem1";
            savedViewMenuItem1.Size = new System.Drawing.Size(55, 24);
            savedViewMenuItem1.Text = "View";
            savedViewMenuItem1.Type = SavedView.ViewTypes.PerformanceSummary;
            savedViewMenuItem1.SavedViewSelected += SavedViewSelected;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewProgressBarColumn1
            // 
            dataGridViewProgressBarColumn1.DataPropertyName = "AvgCPU";
            dataGridViewCellStyle24.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle24.Format = "0.#\\%";
            dataGridViewCellStyle24.NullValue = null;
            dataGridViewProgressBarColumn1.DefaultCellStyle = dataGridViewCellStyle24;
            dataGridViewProgressBarColumn1.HeaderText = "Avg CPU";
            dataGridViewProgressBarColumn1.MinimumWidth = 6;
            dataGridViewProgressBarColumn1.Name = "dataGridViewProgressBarColumn1";
            dataGridViewProgressBarColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn1.Width = 93;
            // 
            // dataGridViewProgressBarColumn2
            // 
            dataGridViewProgressBarColumn2.DataPropertyName = "MaxCPU";
            dataGridViewCellStyle25.Format = "0\\%";
            dataGridViewProgressBarColumn2.DefaultCellStyle = dataGridViewCellStyle25;
            dataGridViewProgressBarColumn2.HeaderText = "Max CPU";
            dataGridViewProgressBarColumn2.MinimumWidth = 6;
            dataGridViewProgressBarColumn2.Name = "dataGridViewProgressBarColumn2";
            dataGridViewProgressBarColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            dataGridViewProgressBarColumn2.Width = 94;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "CriticalWaitMsPerSec";
            dataGridViewCellStyle26.Format = "N2";
            dataGridViewCellStyle26.NullValue = null;
            dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle26;
            dataGridViewTextBoxColumn2.HeaderText = "Critical Wait (ms/sec)";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.Width = 154;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "LockWaitMsPerSec";
            dataGridViewCellStyle27.Format = "N2";
            dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle27;
            dataGridViewTextBoxColumn3.HeaderText = "Lock Wait (ms/sec)";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.Width = 144;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "IOWaitMsPerSec";
            dataGridViewCellStyle28.Format = "N2";
            dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle28;
            dataGridViewTextBoxColumn4.HeaderText = "IO Wait (ms/sec)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.Width = 129;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "WaitMsPerSec";
            dataGridViewCellStyle29.Format = "N2";
            dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle29;
            dataGridViewTextBoxColumn5.HeaderText = "Total Wait (ms/sec)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.Width = 145;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "LatchWaitMsPerSec";
            dataGridViewCellStyle30.Format = "N2";
            dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle30;
            dataGridViewTextBoxColumn6.HeaderText = "Latch Wait (ms/sec)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.Width = 148;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "Latency";
            dataGridViewCellStyle31.Format = "N1";
            dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle31;
            dataGridViewTextBoxColumn7.HeaderText = "IO Latency (ms)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.Visible = false;
            dataGridViewTextBoxColumn7.Width = 126;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "ReadLatency";
            dataGridViewCellStyle32.Format = "N1";
            dataGridViewTextBoxColumn8.DefaultCellStyle = dataGridViewCellStyle32;
            dataGridViewTextBoxColumn8.HeaderText = "Read Latency (ms)";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.Width = 144;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "WriteLatency";
            dataGridViewCellStyle33.Format = "N1";
            dataGridViewTextBoxColumn9.DefaultCellStyle = dataGridViewCellStyle33;
            dataGridViewTextBoxColumn9.HeaderText = "Write Latency (ms)";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.Width = 143;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "MBsec";
            dataGridViewCellStyle34.Format = "N1";
            dataGridViewTextBoxColumn10.DefaultCellStyle = dataGridViewCellStyle34;
            dataGridViewTextBoxColumn10.HeaderText = "MB/sec";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.Width = 83;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "MaxReadMBsec";
            dataGridViewCellStyle35.Format = "N1";
            dataGridViewTextBoxColumn11.DefaultCellStyle = dataGridViewCellStyle35;
            dataGridViewTextBoxColumn11.HeaderText = "Max Read MB/sec";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.Visible = false;
            dataGridViewTextBoxColumn11.Width = 137;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "MaxWriteMBsec";
            dataGridViewCellStyle36.Format = "N1";
            dataGridViewTextBoxColumn12.DefaultCellStyle = dataGridViewCellStyle36;
            dataGridViewTextBoxColumn12.HeaderText = "Max Write MB/sec";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.Visible = false;
            dataGridViewTextBoxColumn12.Width = 136;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "MaxMBsec";
            dataGridViewCellStyle37.Format = "N1";
            dataGridViewTextBoxColumn13.DefaultCellStyle = dataGridViewCellStyle37;
            dataGridViewTextBoxColumn13.HeaderText = "Max MB/sec";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.Visible = false;
            dataGridViewTextBoxColumn13.Width = 103;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "IOPs";
            dataGridViewCellStyle38.Format = "N0";
            dataGridViewTextBoxColumn14.DefaultCellStyle = dataGridViewCellStyle38;
            dataGridViewTextBoxColumn14.HeaderText = "IOPs";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.Width = 67;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "ReadIOPs";
            dataGridViewCellStyle39.Format = "N0";
            dataGridViewTextBoxColumn15.DefaultCellStyle = dataGridViewCellStyle39;
            dataGridViewTextBoxColumn15.HeaderText = "Read IOPs";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.Visible = false;
            dataGridViewTextBoxColumn15.Width = 97;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "WriteIOPs";
            dataGridViewCellStyle40.Format = "N0";
            dataGridViewTextBoxColumn16.DefaultCellStyle = dataGridViewCellStyle40;
            dataGridViewTextBoxColumn16.HeaderText = "Write IOPs";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.Visible = false;
            dataGridViewTextBoxColumn16.Width = 96;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "MaxReadIOPs";
            dataGridViewCellStyle41.Format = "N0";
            dataGridViewTextBoxColumn17.DefaultCellStyle = dataGridViewCellStyle41;
            dataGridViewTextBoxColumn17.HeaderText = "Max Read IOPs";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.Visible = false;
            dataGridViewTextBoxColumn17.Width = 123;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "MaxWriteIOPs";
            dataGridViewCellStyle42.Format = "N0";
            dataGridViewTextBoxColumn18.DefaultCellStyle = dataGridViewCellStyle42;
            dataGridViewTextBoxColumn18.HeaderText = "Max Write IOPs";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.Visible = false;
            dataGridViewTextBoxColumn18.Width = 122;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "MaxIOPs";
            dataGridViewCellStyle43.Format = "N0";
            dataGridViewTextBoxColumn19.DefaultCellStyle = dataGridViewCellStyle43;
            dataGridViewTextBoxColumn19.HeaderText = "Max IOPs";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.Visible = false;
            dataGridViewTextBoxColumn19.Width = 89;
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
            // PerformanceSummary
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "PerformanceSummary";
            Size = new System.Drawing.Size(1048, 816);
            Load += PerformanceSummary_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn1;
        private CustomProgressControl.DataGridViewProgressBarColumn dataGridViewProgressBarColumn2;
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
        private System.Windows.Forms.ToolStripDropDownButton tsCols;
        private System.Windows.Forms.ToolStripMenuItem standardColumnsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem performanceCounterColumnsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewLinkColumn ConnectionID;
        private CustomProgressControl.DataGridViewProgressBarColumn AvgCPU;
        private CustomProgressControl.DataGridViewProgressBarColumn MaxCPU;
        private System.Windows.Forms.DataGridViewImageColumn colCPUHistogram;
        private System.Windows.Forms.DataGridViewTextBoxColumn CriticalWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn LockWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn IOWaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn WaitMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWaitPct;
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
        private System.Windows.Forms.ToolStripButton tsSaveView;
        private System.Windows.Forms.ToolStripButton tsDeleteView;
        private SavedViewMenuItem savedViewMenuItem1;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
