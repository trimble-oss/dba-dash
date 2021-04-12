namespace DBADashGUI.Performance
{
    partial class BlockingViewer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BlockingViewer));
            this.gvBlocking = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.bttnBack = new System.Windows.Forms.Button();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblBlockedWaitTime = new System.Windows.Forms.Label();
            this.lblBlockedSessions = new System.Windows.Forms.Label();
            this.lblSnapshotDate = new System.Windows.Forms.Label();
            this.lblInstance = new System.Windows.Forms.Label();
            this.bttnRootBlockers = new System.Windows.Forms.Button();
            this.lblBlockers = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SessionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBlockedBy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BlockedSessions = new System.Windows.Forms.DataGridViewLinkColumn();
            this.BlockedWaitTimeRecursive = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HostName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LoginName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSessionStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColIsolationLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WaitResource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WaitTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Txt = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.gvBlocking)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gvBlocking
            // 
            this.gvBlocking.AllowUserToAddRows = false;
            this.gvBlocking.AllowUserToDeleteRows = false;
            this.gvBlocking.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.gvBlocking.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvBlocking.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SessionID,
            this.colBlockedBy,
            this.BlockedSessions,
            this.BlockedWaitTimeRecursive,
            this.StartTime,
            this.Database,
            this.HostName,
            this.LoginName,
            this.Status,
            this.colSessionStatus,
            this.ColIsolationLevel,
            this.WaitResource,
            this.WaitTime,
            this.Txt});
            this.gvBlocking.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvBlocking.Location = new System.Drawing.Point(0, 74);
            this.gvBlocking.Name = "gvBlocking";
            this.gvBlocking.ReadOnly = true;
            this.gvBlocking.RowHeadersVisible = false;
            this.gvBlocking.RowHeadersWidth = 51;
            this.gvBlocking.RowTemplate.Height = 96;
            this.gvBlocking.Size = new System.Drawing.Size(1490, 376);
            this.gvBlocking.TabIndex = 0;
            this.gvBlocking.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gvBlocking_CellContentClick);
            this.gvBlocking.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gvBlocking_ColumnHeaderMouseClick);
            this.gvBlocking.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.gvBlocking_RowsAdded);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.bttnBack);
            this.panel1.Controls.Add(this.lblPath);
            this.panel1.Controls.Add(this.lblBlockedWaitTime);
            this.panel1.Controls.Add(this.lblBlockedSessions);
            this.panel1.Controls.Add(this.lblSnapshotDate);
            this.panel1.Controls.Add(this.lblInstance);
            this.panel1.Controls.Add(this.bttnRootBlockers);
            this.panel1.Controls.Add(this.lblBlockers);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1490, 74);
            this.panel1.TabIndex = 1;
            // 
            // bttnBack
            // 
            this.bttnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnBack.Location = new System.Drawing.Point(1222, 12);
            this.bttnBack.Name = "bttnBack";
            this.bttnBack.Size = new System.Drawing.Size(125, 29);
            this.bttnBack.TabIndex = 7;
            this.bttnBack.Text = "Back";
            this.bttnBack.UseVisualStyleBackColor = true;
            this.bttnBack.Click += new System.EventHandler(this.bttnBack_Click);
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.lblPath.Location = new System.Drawing.Point(14, 41);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(43, 17);
            this.lblPath.TabIndex = 6;
            this.lblPath.Text = "{root}";
            // 
            // lblBlockedWaitTime
            // 
            this.lblBlockedWaitTime.AutoSize = true;
            this.lblBlockedWaitTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBlockedWaitTime.Location = new System.Drawing.Point(635, 36);
            this.lblBlockedWaitTime.Name = "lblBlockedWaitTime";
            this.lblBlockedWaitTime.Size = new System.Drawing.Size(147, 17);
            this.lblBlockedWaitTime.TabIndex = 5;
            this.lblBlockedWaitTime.Text = "Blocked Wait Time:";
            // 
            // lblBlockedSessions
            // 
            this.lblBlockedSessions.AutoSize = true;
            this.lblBlockedSessions.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBlockedSessions.Location = new System.Drawing.Point(635, 9);
            this.lblBlockedSessions.Name = "lblBlockedSessions";
            this.lblBlockedSessions.Size = new System.Drawing.Size(140, 17);
            this.lblBlockedSessions.TabIndex = 4;
            this.lblBlockedSessions.Text = "Blocked Sessions:";
            // 
            // lblSnapshotDate
            // 
            this.lblSnapshotDate.AutoSize = true;
            this.lblSnapshotDate.Location = new System.Drawing.Point(355, 36);
            this.lblSnapshotDate.Name = "lblSnapshotDate";
            this.lblSnapshotDate.Size = new System.Drawing.Size(106, 17);
            this.lblSnapshotDate.TabIndex = 3;
            this.lblSnapshotDate.Text = "Snapshot Date:";
            // 
            // lblInstance
            // 
            this.lblInstance.AutoSize = true;
            this.lblInstance.Location = new System.Drawing.Point(355, 9);
            this.lblInstance.Name = "lblInstance";
            this.lblInstance.Size = new System.Drawing.Size(65, 17);
            this.lblInstance.TabIndex = 2;
            this.lblInstance.Text = "Instance:";
            // 
            // bttnRootBlockers
            // 
            this.bttnRootBlockers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnRootBlockers.Location = new System.Drawing.Point(1353, 12);
            this.bttnRootBlockers.Name = "bttnRootBlockers";
            this.bttnRootBlockers.Size = new System.Drawing.Size(125, 29);
            this.bttnRootBlockers.TabIndex = 1;
            this.bttnRootBlockers.Text = "Root Blockers";
            this.bttnRootBlockers.UseVisualStyleBackColor = true;
            this.bttnRootBlockers.Click += new System.EventHandler(this.bttnRootBlockers_Click);
            // 
            // lblBlockers
            // 
            this.lblBlockers.AutoSize = true;
            this.lblBlockers.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBlockers.Location = new System.Drawing.Point(12, 12);
            this.lblBlockers.Name = "lblBlockers";
            this.lblBlockers.Size = new System.Drawing.Size(177, 29);
            this.lblBlockers.TabIndex = 0;
            this.lblBlockers.Text = "Root Blockers";
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "session_id";
            this.dataGridViewTextBoxColumn1.HeaderText = "Session ID";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 104;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewCellStyle3.Format = "N0";
            dataGridViewCellStyle3.NullValue = null;
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridViewTextBoxColumn2.HeaderText = "Blocked Sessions Wait Time (ms)";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn2.Width = 168;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "start_time_utc";
            this.dataGridViewTextBoxColumn3.HeaderText = "Start Time";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Width = 94;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "database_name";
            this.dataGridViewTextBoxColumn4.HeaderText = "Database";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.Width = 98;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "host_name";
            this.dataGridViewTextBoxColumn5.HeaderText = "Host";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.Width = 66;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "login_name";
            this.dataGridViewTextBoxColumn6.HeaderText = "Login";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.Width = 72;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "Status";
            this.dataGridViewTextBoxColumn7.HeaderText = "Status";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.Width = 77;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "wait_resource";
            this.dataGridViewTextBoxColumn8.HeaderText = "Wait Resource";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.Width = 119;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "Wait Time (ms)";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dataGridViewTextBoxColumn9.Width = 121;
            // 
            // SessionID
            // 
            this.SessionID.DataPropertyName = "session_id";
            this.SessionID.HeaderText = "Session ID";
            this.SessionID.MinimumWidth = 6;
            this.SessionID.Name = "SessionID";
            this.SessionID.ReadOnly = true;
            this.SessionID.Width = 104;
            // 
            // colBlockedBy
            // 
            this.colBlockedBy.DataPropertyName = "blocking_session_id";
            this.colBlockedBy.HeaderText = "Blocked By Session";
            this.colBlockedBy.MinimumWidth = 6;
            this.colBlockedBy.Name = "colBlockedBy";
            this.colBlockedBy.ReadOnly = true;
            this.colBlockedBy.Width = 147;
            // 
            // BlockedSessions
            // 
            this.BlockedSessions.DataPropertyName = "BlockCountRecursive";
            this.BlockedSessions.HeaderText = "Blocked Sessions";
            this.BlockedSessions.MinimumWidth = 6;
            this.BlockedSessions.Name = "BlockedSessions";
            this.BlockedSessions.ReadOnly = true;
            this.BlockedSessions.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.BlockedSessions.Width = 136;
            // 
            // BlockedWaitTimeRecursive
            // 
            dataGridViewCellStyle1.Format = "N0";
            dataGridViewCellStyle1.NullValue = null;
            this.BlockedWaitTimeRecursive.DefaultCellStyle = dataGridViewCellStyle1;
            this.BlockedWaitTimeRecursive.HeaderText = "Blocked Sessions Wait Time (ms)";
            this.BlockedWaitTimeRecursive.MinimumWidth = 6;
            this.BlockedWaitTimeRecursive.Name = "BlockedWaitTimeRecursive";
            this.BlockedWaitTimeRecursive.ReadOnly = true;
            this.BlockedWaitTimeRecursive.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.BlockedWaitTimeRecursive.Width = 168;
            // 
            // StartTime
            // 
            this.StartTime.DataPropertyName = "start_time_utc";
            this.StartTime.HeaderText = "Start Time";
            this.StartTime.MinimumWidth = 6;
            this.StartTime.Name = "StartTime";
            this.StartTime.ReadOnly = true;
            this.StartTime.Width = 94;
            // 
            // Database
            // 
            this.Database.DataPropertyName = "database_name";
            this.Database.HeaderText = "Database";
            this.Database.MinimumWidth = 6;
            this.Database.Name = "Database";
            this.Database.ReadOnly = true;
            this.Database.Width = 98;
            // 
            // HostName
            // 
            this.HostName.DataPropertyName = "host_name";
            this.HostName.HeaderText = "Host";
            this.HostName.MinimumWidth = 6;
            this.HostName.Name = "HostName";
            this.HostName.ReadOnly = true;
            this.HostName.Width = 66;
            // 
            // LoginName
            // 
            this.LoginName.DataPropertyName = "login_name";
            this.LoginName.HeaderText = "Login";
            this.LoginName.MinimumWidth = 6;
            this.LoginName.Name = "LoginName";
            this.LoginName.ReadOnly = true;
            this.LoginName.Width = 72;
            // 
            // Status
            // 
            this.Status.DataPropertyName = "Status";
            this.Status.HeaderText = "Status";
            this.Status.MinimumWidth = 6;
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            this.Status.Width = 77;
            // 
            // colSessionStatus
            // 
            this.colSessionStatus.DataPropertyName = "session_status";
            this.colSessionStatus.HeaderText = "Session Status";
            this.colSessionStatus.MinimumWidth = 6;
            this.colSessionStatus.Name = "colSessionStatus";
            this.colSessionStatus.ReadOnly = true;
            this.colSessionStatus.Width = 120;
            // 
            // ColIsolationLevel
            // 
            this.ColIsolationLevel.DataPropertyName = "transaction_isolation_level";
            this.ColIsolationLevel.HeaderText = "Isolation Level";
            this.ColIsolationLevel.MinimumWidth = 6;
            this.ColIsolationLevel.Name = "ColIsolationLevel";
            this.ColIsolationLevel.ReadOnly = true;
            this.ColIsolationLevel.Width = 117;
            // 
            // WaitResource
            // 
            this.WaitResource.DataPropertyName = "wait_resource";
            this.WaitResource.HeaderText = "Wait Resource";
            this.WaitResource.MinimumWidth = 6;
            this.WaitResource.Name = "WaitResource";
            this.WaitResource.ReadOnly = true;
            this.WaitResource.Width = 119;
            // 
            // WaitTime
            // 
            this.WaitTime.HeaderText = "Wait Time (ms)";
            this.WaitTime.MinimumWidth = 6;
            this.WaitTime.Name = "WaitTime";
            this.WaitTime.ReadOnly = true;
            this.WaitTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.WaitTime.Width = 121;
            // 
            // Txt
            // 
            this.Txt.DataPropertyName = "Txt";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.Txt.DefaultCellStyle = dataGridViewCellStyle2;
            this.Txt.HeaderText = "Txt";
            this.Txt.MinimumWidth = 6;
            this.Txt.Name = "Txt";
            this.Txt.ReadOnly = true;
            this.Txt.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Txt.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Txt.Width = 56;
            // 
            // BlockingViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1490, 450);
            this.Controls.Add(this.gvBlocking);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BlockingViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Blocking Snapshot Viewer";
            this.Load += new System.EventHandler(this.BlockingViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gvBlocking)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView gvBlocking;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblBlockers;
        private System.Windows.Forms.Button bttnRootBlockers;
        private System.Windows.Forms.Label lblBlockedWaitTime;
        private System.Windows.Forms.Label lblBlockedSessions;
        private System.Windows.Forms.Label lblSnapshotDate;
        private System.Windows.Forms.Label lblInstance;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Button bttnBack;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn SessionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBlockedBy;
        private System.Windows.Forms.DataGridViewLinkColumn BlockedSessions;
        private System.Windows.Forms.DataGridViewTextBoxColumn BlockedWaitTimeRecursive;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn HostName;
        private System.Windows.Forms.DataGridViewTextBoxColumn LoginName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSessionStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColIsolationLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn WaitResource;
        private System.Windows.Forms.DataGridViewTextBoxColumn WaitTime;
        private System.Windows.Forms.DataGridViewLinkColumn Txt;
    }
}