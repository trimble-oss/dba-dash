namespace DBADashGUI
{
    partial class Summary
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvSummary = new System.Windows.Forms.DataGridView();
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
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MemoryDumpStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CorruptionStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastGoodCheckDBStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.AlertStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.FullBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DiffBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.LogBackupStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.LogShippingStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.DriveStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.JobStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.AGStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileFreeSpaceStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.CustomCheckStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.CollectionErrorStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.SnapshotAgeStatus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.UptimeStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSummary
            // 
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.MemoryDumpStatus,
            this.CorruptionStatus,
            this.LastGoodCheckDBStatus,
            this.AlertStatus,
            this.FullBackupStatus,
            this.DiffBackupStatus,
            this.LogBackupStatus,
            this.LogShippingStatus,
            this.DriveStatus,
            this.JobStatus,
            this.AGStatus,
            this.FileFreeSpaceStatus,
            this.CustomCheckStatus,
            this.CollectionErrorStatus,
            this.SnapshotAgeStatus,
            this.UptimeStatus});
            this.dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSummary.Location = new System.Drawing.Point(0, 31);
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.RowHeadersVisible = false;
            this.dgvSummary.RowHeadersWidth = 51;
            this.dgvSummary.RowTemplate.Height = 24;
            this.dgvSummary.Size = new System.Drawing.Size(1800, 182);
            this.dgvSummary.TabIndex = 0;
            this.dgvSummary.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSummary_CellContentClick);
            this.dgvSummary.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvSummary_ColumnHeaderMouseClick);
            this.dgvSummary.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvSummary_RowAdded);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1800, 31);
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
            this.dataGridViewTextBoxColumn2.HeaderText = "Memory Dump";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 128;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "DetectedCorruptionDate ";
            this.dataGridViewTextBoxColumn3.HeaderText = "Corruption";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 103;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Last Good Check DB";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 137;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewCellStyle4.NullValue = "View";
            this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle4;
            this.dataGridViewTextBoxColumn5.HeaderText = "Alerts";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn5.Width = 73;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewCellStyle5.NullValue = "View";
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle5;
            this.dataGridViewTextBoxColumn6.HeaderText = "Full Backup";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn6.Width = 101;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewCellStyle6.NullValue = "View";
            this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle6;
            this.dataGridViewTextBoxColumn7.HeaderText = "Diff Backup";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.HeaderText = "Log Backup";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 103;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "Log Shipping";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 110;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.HeaderText = "Drive Space";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 105;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.HeaderText = "Agent Jobs";
            this.dataGridViewTextBoxColumn11.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.HeaderText = "Availability Groups";
            this.dataGridViewTextBoxColumn12.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            this.dataGridViewTextBoxColumn12.Width = 141;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.HeaderText = "File FreeSpace";
            this.dataGridViewTextBoxColumn13.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            this.dataGridViewTextBoxColumn13.Width = 121;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.HeaderText = "Custom Checks";
            this.dataGridViewTextBoxColumn14.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.Width = 123;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.HeaderText = "DBADash Errors";
            this.dataGridViewTextBoxColumn15.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.Width = 141;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.HeaderText = "Snapshot Age";
            this.dataGridViewTextBoxColumn16.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            this.dataGridViewTextBoxColumn16.Width = 116;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.HeaderText = "Instance Uptime";
            this.dataGridViewTextBoxColumn17.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            this.dataGridViewTextBoxColumn17.Width = 127;
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
            // MemoryDumpStatus
            // 
            this.MemoryDumpStatus.HeaderText = "Memory Dump";
            this.MemoryDumpStatus.MinimumWidth = 6;
            this.MemoryDumpStatus.Name = "MemoryDumpStatus";
            this.MemoryDumpStatus.ReadOnly = true;
            this.MemoryDumpStatus.Width = 128;
            // 
            // CorruptionStatus
            // 
            this.CorruptionStatus.DataPropertyName = "DetectedCorruptionDate ";
            this.CorruptionStatus.HeaderText = "Corruption";
            this.CorruptionStatus.MinimumWidth = 6;
            this.CorruptionStatus.Name = "CorruptionStatus";
            this.CorruptionStatus.ReadOnly = true;
            this.CorruptionStatus.Width = 103;
            // 
            // LastGoodCheckDBStatus
            // 
            this.LastGoodCheckDBStatus.HeaderText = "Last Good Check DB";
            this.LastGoodCheckDBStatus.LinkColor = System.Drawing.Color.Black;
            this.LastGoodCheckDBStatus.MinimumWidth = 6;
            this.LastGoodCheckDBStatus.Name = "LastGoodCheckDBStatus";
            this.LastGoodCheckDBStatus.ReadOnly = true;
            this.LastGoodCheckDBStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LastGoodCheckDBStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LastGoodCheckDBStatus.Width = 137;
            // 
            // AlertStatus
            // 
            this.AlertStatus.HeaderText = "Alerts";
            this.AlertStatus.LinkColor = System.Drawing.Color.Black;
            this.AlertStatus.MinimumWidth = 6;
            this.AlertStatus.Name = "AlertStatus";
            this.AlertStatus.ReadOnly = true;
            this.AlertStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.AlertStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.AlertStatus.Width = 73;
            // 
            // FullBackupStatus
            // 
            this.FullBackupStatus.HeaderText = "Full Backup";
            this.FullBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.FullBackupStatus.MinimumWidth = 6;
            this.FullBackupStatus.Name = "FullBackupStatus";
            this.FullBackupStatus.ReadOnly = true;
            this.FullBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FullBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.FullBackupStatus.Width = 101;
            // 
            // DiffBackupStatus
            // 
            this.DiffBackupStatus.HeaderText = "Diff Backup";
            this.DiffBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.DiffBackupStatus.MinimumWidth = 6;
            this.DiffBackupStatus.Name = "DiffBackupStatus";
            this.DiffBackupStatus.ReadOnly = true;
            this.DiffBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DiffBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // LogBackupStatus
            // 
            this.LogBackupStatus.HeaderText = "Log Backup";
            this.LogBackupStatus.LinkColor = System.Drawing.Color.Black;
            this.LogBackupStatus.MinimumWidth = 6;
            this.LogBackupStatus.Name = "LogBackupStatus";
            this.LogBackupStatus.ReadOnly = true;
            this.LogBackupStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogBackupStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LogBackupStatus.Width = 103;
            // 
            // LogShippingStatus
            // 
            this.LogShippingStatus.HeaderText = "Log Shipping";
            this.LogShippingStatus.LinkColor = System.Drawing.Color.Black;
            this.LogShippingStatus.MinimumWidth = 6;
            this.LogShippingStatus.Name = "LogShippingStatus";
            this.LogShippingStatus.ReadOnly = true;
            this.LogShippingStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.LogShippingStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.LogShippingStatus.Width = 110;
            // 
            // DriveStatus
            // 
            this.DriveStatus.HeaderText = "Drive Space";
            this.DriveStatus.LinkColor = System.Drawing.Color.Black;
            this.DriveStatus.MinimumWidth = 6;
            this.DriveStatus.Name = "DriveStatus";
            this.DriveStatus.ReadOnly = true;
            this.DriveStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.DriveStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.DriveStatus.Width = 105;
            // 
            // JobStatus
            // 
            this.JobStatus.HeaderText = "Agent Jobs";
            this.JobStatus.LinkColor = System.Drawing.Color.Black;
            this.JobStatus.MinimumWidth = 6;
            this.JobStatus.Name = "JobStatus";
            this.JobStatus.ReadOnly = true;
            this.JobStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.JobStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // AGStatus
            // 
            this.AGStatus.HeaderText = "Availability Groups";
            this.AGStatus.MinimumWidth = 6;
            this.AGStatus.Name = "AGStatus";
            this.AGStatus.ReadOnly = true;
            this.AGStatus.Width = 141;
            // 
            // FileFreeSpaceStatus
            // 
            dataGridViewCellStyle1.NullValue = "View";
            this.FileFreeSpaceStatus.DefaultCellStyle = dataGridViewCellStyle1;
            this.FileFreeSpaceStatus.HeaderText = "File FreeSpace";
            this.FileFreeSpaceStatus.LinkColor = System.Drawing.Color.Black;
            this.FileFreeSpaceStatus.MinimumWidth = 6;
            this.FileFreeSpaceStatus.Name = "FileFreeSpaceStatus";
            this.FileFreeSpaceStatus.ReadOnly = true;
            this.FileFreeSpaceStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.FileFreeSpaceStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.FileFreeSpaceStatus.Width = 121;
            // 
            // CustomCheckStatus
            // 
            dataGridViewCellStyle2.NullValue = "View";
            this.CustomCheckStatus.DefaultCellStyle = dataGridViewCellStyle2;
            this.CustomCheckStatus.HeaderText = "Custom Checks";
            this.CustomCheckStatus.LinkColor = System.Drawing.Color.Black;
            this.CustomCheckStatus.MinimumWidth = 6;
            this.CustomCheckStatus.Name = "CustomCheckStatus";
            this.CustomCheckStatus.ReadOnly = true;
            this.CustomCheckStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CustomCheckStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.CustomCheckStatus.Width = 123;
            // 
            // CollectionErrorStatus
            // 
            dataGridViewCellStyle3.NullValue = "View";
            this.CollectionErrorStatus.DefaultCellStyle = dataGridViewCellStyle3;
            this.CollectionErrorStatus.HeaderText = "DBADash Errors";
            this.CollectionErrorStatus.LinkColor = System.Drawing.Color.Black;
            this.CollectionErrorStatus.MinimumWidth = 6;
            this.CollectionErrorStatus.Name = "CollectionErrorStatus";
            this.CollectionErrorStatus.ReadOnly = true;
            this.CollectionErrorStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.CollectionErrorStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.CollectionErrorStatus.Width = 141;
            // 
            // SnapshotAgeStatus
            // 
            this.SnapshotAgeStatus.HeaderText = "Snapshot Age";
            this.SnapshotAgeStatus.LinkColor = System.Drawing.Color.Black;
            this.SnapshotAgeStatus.MinimumWidth = 6;
            this.SnapshotAgeStatus.Name = "SnapshotAgeStatus";
            this.SnapshotAgeStatus.ReadOnly = true;
            this.SnapshotAgeStatus.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SnapshotAgeStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SnapshotAgeStatus.Width = 116;
            // 
            // UptimeStatus
            // 
            this.UptimeStatus.HeaderText = "Instance Uptime";
            this.UptimeStatus.MinimumWidth = 6;
            this.UptimeStatus.Name = "UptimeStatus";
            this.UptimeStatus.ReadOnly = true;
            this.UptimeStatus.Width = 127;
            // 
            // Summary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvSummary);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Summary";
            this.Size = new System.Drawing.Size(1800, 213);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSummary;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn MemoryDumpStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn CorruptionStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LastGoodCheckDBStatus;
        private System.Windows.Forms.DataGridViewLinkColumn AlertStatus;
        private System.Windows.Forms.DataGridViewLinkColumn FullBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn DiffBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LogBackupStatus;
        private System.Windows.Forms.DataGridViewLinkColumn LogShippingStatus;
        private System.Windows.Forms.DataGridViewLinkColumn DriveStatus;
        private System.Windows.Forms.DataGridViewLinkColumn JobStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn AGStatus;
        private System.Windows.Forms.DataGridViewLinkColumn FileFreeSpaceStatus;
        private System.Windows.Forms.DataGridViewLinkColumn CustomCheckStatus;
        private System.Windows.Forms.DataGridViewLinkColumn CollectionErrorStatus;
        private System.Windows.Forms.DataGridViewLinkColumn SnapshotAgeStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn UptimeStatus;
    }
}
