namespace DBAChecksGUI.CollectionDates
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvCollectionDates = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Reference = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WarningThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CriticalThreshold = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            this.ConfigureRoot = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCollectionDates)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripFilter});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(882, 27);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripFilter
            // 
            this.toolStripFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalToolStripMenuItem,
            this.warningToolStripMenuItem,
            this.undefinedToolStripMenuItem,
            this.OKToolStripMenuItem});
            this.toolStripFilter.Image = global::DBAChecksGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripFilter.Name = "toolStripFilter";
            this.toolStripFilter.Size = new System.Drawing.Size(34, 28);
            this.toolStripFilter.Text = "Filter";
            // 
            // criticalToolStripMenuItem
            // 
            this.criticalToolStripMenuItem.CheckOnClick = true;
            this.criticalToolStripMenuItem.Name = "criticalToolStripMenuItem";
            this.criticalToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.criticalToolStripMenuItem.Text = "Critical";
            this.criticalToolStripMenuItem.Click += new System.EventHandler(this.criticalToolStripMenuItem_Click);
            // 
            // warningToolStripMenuItem
            // 
            this.warningToolStripMenuItem.CheckOnClick = true;
            this.warningToolStripMenuItem.Name = "warningToolStripMenuItem";
            this.warningToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.warningToolStripMenuItem.Text = "Warning";
            this.warningToolStripMenuItem.Click += new System.EventHandler(this.warningToolStripMenuItem_Click);
            // 
            // undefinedToolStripMenuItem
            // 
            this.undefinedToolStripMenuItem.CheckOnClick = true;
            this.undefinedToolStripMenuItem.Name = "undefinedToolStripMenuItem";
            this.undefinedToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.undefinedToolStripMenuItem.Text = "Undefined";
            this.undefinedToolStripMenuItem.Click += new System.EventHandler(this.undefinedToolStripMenuItem_Click);
            // 
            // OKToolStripMenuItem
            // 
            this.OKToolStripMenuItem.CheckOnClick = true;
            this.OKToolStripMenuItem.Name = "OKToolStripMenuItem";
            this.OKToolStripMenuItem.Size = new System.Drawing.Size(161, 26);
            this.OKToolStripMenuItem.Text = "OK";
            this.OKToolStripMenuItem.Click += new System.EventHandler(this.OKToolStripMenuItem_Click);
            // 
            // dgvCollectionDates
            // 
            this.dgvCollectionDates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCollectionDates.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Reference,
            this.WarningThreshold,
            this.CriticalThreshold,
            this.SnapshotAge,
            this.SnapshotDate,
            this.ConfiguredLevel,
            this.Configure,
            this.ConfigureRoot});
            this.dgvCollectionDates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCollectionDates.Location = new System.Drawing.Point(0, 27);
            this.dgvCollectionDates.Name = "dgvCollectionDates";
            this.dgvCollectionDates.RowHeadersVisible = false;
            this.dgvCollectionDates.RowHeadersWidth = 51;
            this.dgvCollectionDates.RowTemplate.Height = 24;
            this.dgvCollectionDates.Size = new System.Drawing.Size(882, 278);
            this.dgvCollectionDates.TabIndex = 3;
            this.dgvCollectionDates.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgvCollectionDates.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "ConnectionID";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.Width = 125;
            // 
            // Reference
            // 
            this.Reference.DataPropertyName = "Reference";
            this.Reference.HeaderText = "Reference";
            this.Reference.MinimumWidth = 6;
            this.Reference.Name = "Reference";
            this.Reference.Width = 125;
            // 
            // WarningThreshold
            // 
            this.WarningThreshold.DataPropertyName = "WarningThreshold";
            this.WarningThreshold.HeaderText = "WarningThreshold";
            this.WarningThreshold.MinimumWidth = 6;
            this.WarningThreshold.Name = "WarningThreshold";
            this.WarningThreshold.Width = 125;
            // 
            // CriticalThreshold
            // 
            this.CriticalThreshold.DataPropertyName = "CriticalThreshold";
            this.CriticalThreshold.HeaderText = "Critical Threshold";
            this.CriticalThreshold.MinimumWidth = 6;
            this.CriticalThreshold.Name = "CriticalThreshold";
            this.CriticalThreshold.Width = 125;
            // 
            // SnapshotAge
            // 
            this.SnapshotAge.DataPropertyName = "SnapshotAge";
            this.SnapshotAge.HeaderText = "SnapshotAge";
            this.SnapshotAge.MinimumWidth = 6;
            this.SnapshotAge.Name = "SnapshotAge";
            this.SnapshotAge.Width = 125;
            // 
            // SnapshotDate
            // 
            this.SnapshotDate.DataPropertyName = "SnapshotDate";
            this.SnapshotDate.HeaderText = "Snapshot Date";
            this.SnapshotDate.MinimumWidth = 6;
            this.SnapshotDate.Name = "SnapshotDate";
            this.SnapshotDate.Width = 125;
            // 
            // ConfiguredLevel
            // 
            this.ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            this.ConfiguredLevel.HeaderText = "Configured Level";
            this.ConfiguredLevel.MinimumWidth = 6;
            this.ConfiguredLevel.Name = "ConfiguredLevel";
            this.ConfiguredLevel.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.ConfiguredLevel.Width = 125;
            // 
            // Configure
            // 
            this.Configure.HeaderText = "Configure Instance";
            this.Configure.MinimumWidth = 6;
            this.Configure.Name = "Configure";
            this.Configure.Text = "Configure Instance";
            this.Configure.UseColumnTextForLinkValue = true;
            this.Configure.Width = 125;
            // 
            // ConfigureRoot
            // 
            this.ConfigureRoot.HeaderText = "Configure Root";
            this.ConfigureRoot.MinimumWidth = 6;
            this.ConfigureRoot.Name = "ConfigureRoot";
            this.ConfigureRoot.Text = "Configure Root";
            this.ConfigureRoot.UseColumnTextForLinkValue = true;
            this.ConfigureRoot.Width = 125;
            // 
            // CollectionDates
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvCollectionDates);
            this.Controls.Add(this.toolStrip1);
            this.Name = "CollectionDates";
            this.Size = new System.Drawing.Size(882, 305);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCollectionDates)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.DataGridView dgvCollectionDates;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Reference;
        private System.Windows.Forms.DataGridViewTextBoxColumn WarningThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn CriticalThreshold;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotAge;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewLinkColumn ConfigureRoot;
    }
}
