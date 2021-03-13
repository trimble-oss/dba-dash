namespace DBADashGUI.Performance
{
    partial class WaitsSummary
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.colCriticalWait = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colHelp = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTotalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWait = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSignalWaitPct = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWaitTimeMsPerSecPerCore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSampleDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colWaitType,
            this.colTotalWait,
            this.colSignalWait,
            this.colSignalWaitPct,
            this.colWaitTimeMsPerSec,
            this.colWaitTimeMsPerSecPerCore,
            this.colSampleDuration,
            this.colCriticalWait,
            this.colHelp});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(708, 498);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            // 
            // colCriticalWait
            // 
            this.colCriticalWait.DataPropertyName = "CriticalWait";
            this.colCriticalWait.FalseValue = "";
            this.colCriticalWait.HeaderText = "Is Critical Wait?";
            this.colCriticalWait.MinimumWidth = 6;
            this.colCriticalWait.Name = "colCriticalWait";
            this.colCriticalWait.ReadOnly = true;
            this.colCriticalWait.Width = 99;
            // 
            // colHelp
            // 
            this.colHelp.HeaderText = "Help";
            this.colHelp.MinimumWidth = 6;
            this.colHelp.Name = "colHelp";
            this.colHelp.ReadOnly = true;
            this.colHelp.Text = "Help";
            this.colHelp.UseColumnTextForLinkValue = true;
            this.colHelp.Width = 43;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.tsRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(708, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "WaitType";
            this.dataGridViewTextBoxColumn1.HeaderText = "Wait Type";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 101;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle7.Format = "N3";
            dataGridViewCellStyle7.NullValue = null;
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle7;
            this.dataGridViewTextBoxColumn2.HeaderText = "Total Wait (sec)";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 126;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle8.Format = "N3";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn3.HeaderText = "Signal Wait (sec)";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle9.Format = "P1";
            this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn4.HeaderText = "Signal Wait %";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 103;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle10.Format = "N2";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn5.HeaderText = "Wait Time (ms/sec)";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 145;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle11.Format = "N2";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn6.HeaderText = "Wait Time (ms/sec/core)";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 173;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle12.Format = "N0";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn7.HeaderText = "Sample Duration (sec)";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 134;
            // 
            // colWaitType
            // 
            this.colWaitType.DataPropertyName = "WaitType";
            this.colWaitType.HeaderText = "Wait Type";
            this.colWaitType.MinimumWidth = 6;
            this.colWaitType.Name = "colWaitType";
            this.colWaitType.ReadOnly = true;
            this.colWaitType.Width = 93;
            // 
            // colTotalWait
            // 
            this.colTotalWait.DataPropertyName = "TotalWaitSec";
            dataGridViewCellStyle1.Format = "N3";
            dataGridViewCellStyle1.NullValue = null;
            this.colTotalWait.DefaultCellStyle = dataGridViewCellStyle1;
            this.colTotalWait.HeaderText = "Total Wait (sec)";
            this.colTotalWait.MinimumWidth = 6;
            this.colTotalWait.Name = "colTotalWait";
            this.colTotalWait.ReadOnly = true;
            this.colTotalWait.Width = 126;
            // 
            // colSignalWait
            // 
            this.colSignalWait.DataPropertyName = "SignalWaitSec";
            dataGridViewCellStyle2.Format = "N3";
            this.colSignalWait.DefaultCellStyle = dataGridViewCellStyle2;
            this.colSignalWait.HeaderText = "Signal Wait (sec)";
            this.colSignalWait.MinimumWidth = 6;
            this.colSignalWait.Name = "colSignalWait";
            this.colSignalWait.ReadOnly = true;
            this.colSignalWait.Width = 103;
            // 
            // colSignalWaitPct
            // 
            this.colSignalWaitPct.DataPropertyName = "SignalWaitPct";
            dataGridViewCellStyle3.Format = "P1";
            this.colSignalWaitPct.DefaultCellStyle = dataGridViewCellStyle3;
            this.colSignalWaitPct.HeaderText = "Signal Wait %";
            this.colSignalWaitPct.MinimumWidth = 6;
            this.colSignalWaitPct.Name = "colSignalWaitPct";
            this.colSignalWaitPct.ReadOnly = true;
            this.colSignalWaitPct.Width = 103;
            // 
            // colWaitTimeMsPerSec
            // 
            this.colWaitTimeMsPerSec.DataPropertyName = "WaitTimeMsPerSec";
            dataGridViewCellStyle4.Format = "N2";
            this.colWaitTimeMsPerSec.DefaultCellStyle = dataGridViewCellStyle4;
            this.colWaitTimeMsPerSec.HeaderText = "Wait Time (ms/sec)";
            this.colWaitTimeMsPerSec.MinimumWidth = 6;
            this.colWaitTimeMsPerSec.Name = "colWaitTimeMsPerSec";
            this.colWaitTimeMsPerSec.ReadOnly = true;
            this.colWaitTimeMsPerSec.Width = 145;
            // 
            // colWaitTimeMsPerSecPerCore
            // 
            this.colWaitTimeMsPerSecPerCore.DataPropertyName = "WaitTimeMsPerCorePerSec";
            dataGridViewCellStyle5.Format = "N2";
            this.colWaitTimeMsPerSecPerCore.DefaultCellStyle = dataGridViewCellStyle5;
            this.colWaitTimeMsPerSecPerCore.HeaderText = "Wait Time (ms/sec/core)";
            this.colWaitTimeMsPerSecPerCore.MinimumWidth = 6;
            this.colWaitTimeMsPerSecPerCore.Name = "colWaitTimeMsPerSecPerCore";
            this.colWaitTimeMsPerSecPerCore.ReadOnly = true;
            this.colWaitTimeMsPerSecPerCore.Width = 173;
            // 
            // colSampleDuration
            // 
            this.colSampleDuration.DataPropertyName = "SampleDurationSec";
            dataGridViewCellStyle6.Format = "N0";
            this.colSampleDuration.DefaultCellStyle = dataGridViewCellStyle6;
            this.colSampleDuration.HeaderText = "Sample Duration (sec)";
            this.colSampleDuration.MinimumWidth = 6;
            this.colSampleDuration.Name = "colSampleDuration";
            this.colSampleDuration.ReadOnly = true;
            this.colSampleDuration.Width = 134;
            // 
            // WaitsSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "WaitsSummary";
            this.Size = new System.Drawing.Size(708, 525);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWait;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSignalWaitPct;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWaitTimeMsPerSecPerCore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSampleDuration;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCriticalWait;
        private System.Windows.Forms.DataGridViewLinkColumn colHelp;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsRefresh;
    }
}
