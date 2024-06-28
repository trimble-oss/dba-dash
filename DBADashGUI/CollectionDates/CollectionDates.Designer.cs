namespace DBADashGUI.CollectionDates
{
    partial class CollectionDates
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionDates));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            dgvCollectionDates = new System.Windows.Forms.DataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Reference = new System.Windows.Forms.DataGridViewTextBoxColumn();
            WarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            ConfigureRoot = new System.Windows.Forms.DataGridViewLinkColumn();
            colRun = new System.Windows.Forms.DataGridViewLinkColumn();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCollectionDates).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, statusFilterToolStrip1, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1357, 27);
            toolStrip1.TabIndex = 2;
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
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.Acknowledged = false;
            statusFilterToolStrip1.AcknowledgedVisible = false;
            statusFilterToolStrip1.Critical = true;
            statusFilterToolStrip1.CriticalVisible = true;
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.NA = true;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.NAVisible = true;
            statusFilterToolStrip1.OK = true;
            statusFilterToolStrip1.OKVisible = true;
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.Warning = true;
            statusFilterToolStrip1.WarningVisible = true;
            statusFilterToolStrip1.UserChangedStatusFilter += Status_Selected;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(192, 24);
            tsTrigger.Text = "Trigger Warning, Critical";
            tsTrigger.Click += TsTrigger_Click;
            // 
            // dgvCollectionDates
            // 
            dgvCollectionDates.AllowUserToAddRows = false;
            dgvCollectionDates.AllowUserToDeleteRows = false;
            dgvCollectionDates.BackgroundColor = System.Drawing.Color.White;
            dgvCollectionDates.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dgvCollectionDates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCollectionDates.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, Reference, WarningThreshold, CriticalThreshold, SnapshotAge, SnapshotDate, ConfiguredLevel, Configure, ConfigureRoot, colRun });
            dgvCollectionDates.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvCollectionDates.Location = new System.Drawing.Point(0, 27);
            dgvCollectionDates.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvCollectionDates.Name = "dgvCollectionDates";
            dgvCollectionDates.ReadOnly = true;
            dgvCollectionDates.RowHeadersVisible = false;
            dgvCollectionDates.RowHeadersWidth = 51;
            dgvCollectionDates.RowTemplate.Height = 24;
            dgvCollectionDates.Size = new System.Drawing.Size(1357, 457);
            dgvCollectionDates.TabIndex = 3;
            dgvCollectionDates.CellContentClick += Dgv_CellContentClick;
            dgvCollectionDates.RowsAdded += Dgv_RowsAdded;
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
            // Reference
            // 
            Reference.DataPropertyName = "Reference";
            Reference.HeaderText = "Reference";
            Reference.MinimumWidth = 6;
            Reference.Name = "Reference";
            Reference.ReadOnly = true;
            Reference.Width = 103;
            // 
            // WarningThreshold
            // 
            WarningThreshold.DataPropertyName = "WarningThreshold";
            WarningThreshold.HeaderText = "Warning Threshold";
            WarningThreshold.MinimumWidth = 6;
            WarningThreshold.Name = "WarningThreshold";
            WarningThreshold.ReadOnly = true;
            WarningThreshold.Width = 154;
            // 
            // CriticalThreshold
            // 
            CriticalThreshold.DataPropertyName = "CriticalThreshold";
            CriticalThreshold.HeaderText = "Critical Threshold";
            CriticalThreshold.MinimumWidth = 6;
            CriticalThreshold.Name = "CriticalThreshold";
            CriticalThreshold.ReadOnly = true;
            CriticalThreshold.Width = 135;
            // 
            // SnapshotAge
            // 
            SnapshotAge.DataPropertyName = "HumanSnapshotAge";
            SnapshotAge.HeaderText = "Snapshot Age";
            SnapshotAge.MinimumWidth = 6;
            SnapshotAge.Name = "SnapshotAge";
            SnapshotAge.ReadOnly = true;
            SnapshotAge.Width = 122;
            // 
            // SnapshotDate
            // 
            SnapshotDate.DataPropertyName = "SnapshotDate";
            SnapshotDate.HeaderText = "Snapshot Date";
            SnapshotDate.MinimumWidth = 6;
            SnapshotDate.Name = "SnapshotDate";
            SnapshotDate.ReadOnly = true;
            SnapshotDate.Width = 120;
            // 
            // ConfiguredLevel
            // 
            ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            ConfiguredLevel.HeaderText = "Configured Level";
            ConfiguredLevel.MinimumWidth = 6;
            ConfiguredLevel.Name = "ConfiguredLevel";
            ConfiguredLevel.ReadOnly = true;
            ConfiguredLevel.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ConfiguredLevel.Width = 132;
            // 
            // Configure
            // 
            Configure.HeaderText = "Configure Instance";
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Text = "Configure Instance";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 119;
            // 
            // ConfigureRoot
            // 
            ConfigureRoot.HeaderText = "Configure Root";
            ConfigureRoot.MinimumWidth = 6;
            ConfigureRoot.Name = "ConfigureRoot";
            ConfigureRoot.ReadOnly = true;
            ConfigureRoot.Text = "Configure Root";
            ConfigureRoot.UseColumnTextForLinkValue = true;
            ConfigureRoot.Width = 98;
            // 
            // colRun
            // 
            colRun.HeaderText = "Run";
            colRun.MinimumWidth = 6;
            colRun.Name = "colRun";
            colRun.ReadOnly = true;
            colRun.Text = "Run Now";
            colRun.UseColumnTextForLinkValue = true;
            colRun.Width = 125;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 484);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1357, 26);
            statusStrip1.TabIndex = 4;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(1303, 20);
            lblStatus.Spring = true;
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CollectionDates
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvCollectionDates);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CollectionDates";
            Size = new System.Drawing.Size(1357, 510);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCollectionDates).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.DataGridView dgvCollectionDates;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Reference;
        private System.Windows.Forms.DataGridViewTextBoxColumn WarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn CriticalThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewLinkColumn ConfigureRoot;
        private System.Windows.Forms.DataGridViewLinkColumn colRun;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripButton tsTrigger;
    }
}
