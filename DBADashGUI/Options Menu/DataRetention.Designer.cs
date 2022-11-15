namespace DBADashGUI
{
    partial class DataRetention
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataRetention));
            this.dgv = new System.Windows.Forms.DataGridView();
            this.colSchema = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTableName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRetentionDays = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colActualRetention = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDataFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.showAllTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsPurge = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSchema,
            this.colTableName,
            this.SizeMB,
            this.colRetentionDays,
            this.colActualRetention,
            this.colDataFrom});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(1028, 532);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellContentClick);
            // 
            // colSchema
            // 
            this.colSchema.DataPropertyName = "SchemaName";
            this.colSchema.HeaderText = "Schema";
            this.colSchema.MinimumWidth = 6;
            this.colSchema.Name = "colSchema";
            this.colSchema.ReadOnly = true;
            this.colSchema.Width = 88;
            // 
            // colTableName
            // 
            this.colTableName.DataPropertyName = "TableName";
            this.colTableName.HeaderText = "Table";
            this.colTableName.MinimumWidth = 6;
            this.colTableName.Name = "colTableName";
            this.colTableName.ReadOnly = true;
            this.colTableName.Width = 73;
            // 
            // SizeMB
            // 
            this.SizeMB.DataPropertyName = "SizeMB";
            dataGridViewCellStyle1.Format = "N1";
            this.SizeMB.DefaultCellStyle = dataGridViewCellStyle1;
            this.SizeMB.HeaderText = "Size (MB)";
            this.SizeMB.MinimumWidth = 6;
            this.SizeMB.Name = "SizeMB";
            this.SizeMB.ReadOnly = true;
            this.SizeMB.Width = 91;
            // 
            // colRetentionDays
            // 
            this.colRetentionDays.DataPropertyName = "RetentionDays";
            this.colRetentionDays.HeaderText = "Retention (Days)";
            this.colRetentionDays.MinimumWidth = 6;
            this.colRetentionDays.Name = "colRetentionDays";
            this.colRetentionDays.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colRetentionDays.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colRetentionDays.Width = 132;
            // 
            // colActualRetention
            // 
            this.colActualRetention.DataPropertyName = "ActualRetention";
            this.colActualRetention.HeaderText = "Actual Retention (Days)";
            this.colActualRetention.MinimumWidth = 6;
            this.colActualRetention.Name = "colActualRetention";
            this.colActualRetention.ReadOnly = true;
            this.colActualRetention.ToolTipText = "Partitions in table";
            this.colActualRetention.Width = 171;
            // 
            // colDataFrom
            // 
            this.colDataFrom.DataPropertyName = "DataFrom";
            this.colDataFrom.HeaderText = "Oldest Partition";
            this.colDataFrom.MinimumWidth = 6;
            this.colDataFrom.Name = "colDataFrom";
            this.colDataFrom.ReadOnly = true;
            this.colDataFrom.Width = 123;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.toolStripDropDownButton1,
            this.tsPurge});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1028, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.TsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showAllTablesToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            this.toolStripDropDownButton1.Text = "Filter";
            // 
            // showAllTablesToolStripMenuItem
            // 
            this.showAllTablesToolStripMenuItem.CheckOnClick = true;
            this.showAllTablesToolStripMenuItem.Name = "showAllTablesToolStripMenuItem";
            this.showAllTablesToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            this.showAllTablesToolStripMenuItem.Text = "Show All Tables";
            this.showAllTablesToolStripMenuItem.Click += new System.EventHandler(this.ShowAllTablesToolStripMenuItem_Click);
            // 
            // tsPurge
            // 
            this.tsPurge.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsPurge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPurge.Image = ((System.Drawing.Image)(resources.GetObject("tsPurge.Image")));
            this.tsPurge.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPurge.Name = "tsPurge";
            this.tsPurge.Size = new System.Drawing.Size(29, 24);
            this.tsPurge.Text = "Run Cleanup";
            this.tsPurge.Click += new System.EventHandler(this.TsPurge_Click);
            // 
            // DataRetention
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 559);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "DataRetention";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Data Retention";
            this.Load += new System.EventHandler(this.DataRetention_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem showAllTablesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsPurge;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchema;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTableName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SizeMB;
        private System.Windows.Forms.DataGridViewLinkColumn colRetentionDays;
        private System.Windows.Forms.DataGridViewTextBoxColumn colActualRetention;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDataFrom;
    }
}