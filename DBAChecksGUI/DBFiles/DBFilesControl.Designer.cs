namespace DBAChecksGUI.DBFiles
{
    partial class DBFilesControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvFiles = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Database = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SizeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UsedMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FreeMB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PctFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NumberOfFiles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DBReadOnly = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Standby = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.State = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileSnapshotAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.warningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undefinedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OKToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            this.configureDatabaseThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvFiles
            // 
            this.dgvFiles.AllowUserToAddRows = false;
            this.dgvFiles.AllowUserToDeleteRows = false;
            this.dgvFiles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvFiles.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvFiles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Database,
            this.FileGroup,
            this.SizeMB,
            this.UsedMB,
            this.FreeMB,
            this.PctFree,
            this.NumberOfFiles,
            this.ReadOnly,
            this.DBReadOnly,
            this.Standby,
            this.State,
            this.ConfiguredLevel,
            this.FileSnapshotAge,
            this.Configure});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvFiles.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvFiles.Location = new System.Drawing.Point(0, 27);
            this.dgvFiles.Name = "dgvFiles";
            this.dgvFiles.ReadOnly = true;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvFiles.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvFiles.RowHeadersVisible = false;
            this.dgvFiles.RowHeadersWidth = 51;
            this.dgvFiles.RowTemplate.Height = 24;
            this.dgvFiles.Size = new System.Drawing.Size(1616, 259);
            this.dgvFiles.TabIndex = 0;
            this.dgvFiles.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvFiles_CellContentClick);
            this.dgvFiles.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvFiles_RowsAdded);
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
            // Database
            // 
            this.Database.DataPropertyName = "name";
            this.Database.HeaderText = "Database";
            this.Database.MinimumWidth = 6;
            this.Database.Name = "Database";
            this.Database.ReadOnly = true;
            this.Database.Width = 98;
            // 
            // FileGroup
            // 
            this.FileGroup.DataPropertyName = "filegroup_name";
            this.FileGroup.HeaderText = "FileGroup";
            this.FileGroup.MinimumWidth = 6;
            this.FileGroup.Name = "FileGroup";
            this.FileGroup.ReadOnly = true;
            this.FileGroup.Width = 99;
            // 
            // SizeMB
            // 
            this.SizeMB.DataPropertyName = "SizeMB";
            dataGridViewCellStyle2.Format = "0.0";
            this.SizeMB.DefaultCellStyle = dataGridViewCellStyle2;
            this.SizeMB.HeaderText = "SizeMB";
            this.SizeMB.MinimumWidth = 6;
            this.SizeMB.Name = "SizeMB";
            this.SizeMB.ReadOnly = true;
            this.SizeMB.Width = 84;
            // 
            // UsedMB
            // 
            this.UsedMB.DataPropertyName = "UsedMB";
            dataGridViewCellStyle3.Format = "N1";
            dataGridViewCellStyle3.NullValue = null;
            this.UsedMB.DefaultCellStyle = dataGridViewCellStyle3;
            this.UsedMB.HeaderText = "UsedMB";
            this.UsedMB.MinimumWidth = 6;
            this.UsedMB.Name = "UsedMB";
            this.UsedMB.ReadOnly = true;
            this.UsedMB.Width = 90;
            // 
            // FreeMB
            // 
            this.FreeMB.DataPropertyName = "FreeMB";
            dataGridViewCellStyle4.Format = "N1";
            this.FreeMB.DefaultCellStyle = dataGridViewCellStyle4;
            this.FreeMB.HeaderText = "Free (MB)";
            this.FreeMB.MinimumWidth = 6;
            this.FreeMB.Name = "FreeMB";
            this.FreeMB.ReadOnly = true;
            this.FreeMB.Width = 92;
            // 
            // PctFree
            // 
            this.PctFree.DataPropertyName = "PctFree";
            dataGridViewCellStyle5.Format = "P1";
            dataGridViewCellStyle5.NullValue = null;
            this.PctFree.DefaultCellStyle = dataGridViewCellStyle5;
            this.PctFree.HeaderText = "Free %";
            this.PctFree.MinimumWidth = 6;
            this.PctFree.Name = "PctFree";
            this.PctFree.ReadOnly = true;
            this.PctFree.Width = 76;
            // 
            // NumberOfFiles
            // 
            this.NumberOfFiles.DataPropertyName = "NumberOfFiles";
            this.NumberOfFiles.HeaderText = "Number of Files";
            this.NumberOfFiles.MinimumWidth = 6;
            this.NumberOfFiles.Name = "NumberOfFiles";
            this.NumberOfFiles.ReadOnly = true;
            this.NumberOfFiles.Width = 99;
            // 
            // ReadOnly
            // 
            this.ReadOnly.DataPropertyName = "is_read_only";
            this.ReadOnly.HeaderText = "Read Only";
            this.ReadOnly.MinimumWidth = 6;
            this.ReadOnly.Name = "ReadOnly";
            this.ReadOnly.ReadOnly = true;
            this.ReadOnly.Width = 96;
            // 
            // DBReadOnly
            // 
            this.DBReadOnly.DataPropertyName = "is_db_readonly";
            this.DBReadOnly.HeaderText = "DB ReadOnly";
            this.DBReadOnly.MinimumWidth = 6;
            this.DBReadOnly.Name = "DBReadOnly";
            this.DBReadOnly.ReadOnly = true;
            this.DBReadOnly.Width = 113;
            // 
            // Standby
            // 
            this.Standby.DataPropertyName = "is_in_standby";
            this.Standby.HeaderText = "Standby";
            this.Standby.MinimumWidth = 6;
            this.Standby.Name = "Standby";
            this.Standby.ReadOnly = true;
            this.Standby.Width = 89;
            // 
            // State
            // 
            this.State.DataPropertyName = "state_desc";
            this.State.HeaderText = "State";
            this.State.MinimumWidth = 6;
            this.State.Name = "State";
            this.State.ReadOnly = true;
            this.State.Width = 70;
            // 
            // ConfiguredLevel
            // 
            this.ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            this.ConfiguredLevel.HeaderText = "Configured Level";
            this.ConfiguredLevel.MinimumWidth = 6;
            this.ConfiguredLevel.Name = "ConfiguredLevel";
            this.ConfiguredLevel.ReadOnly = true;
            this.ConfiguredLevel.Width = 132;
            // 
            // FileSnapshotAge
            // 
            this.FileSnapshotAge.DataPropertyName = "FileSnapshotAge";
            this.FileSnapshotAge.HeaderText = "File Snapshot Age";
            this.FileSnapshotAge.MinimumWidth = 6;
            this.FileSnapshotAge.Name = "FileSnapshotAge";
            this.FileSnapshotAge.ReadOnly = true;
            this.FileSnapshotAge.Width = 139;
            // 
            // Configure
            // 
            this.Configure.HeaderText = "Configure";
            this.Configure.MinimumWidth = 6;
            this.Configure.Name = "Configure";
            this.Configure.ReadOnly = true;
            this.Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Configure.Text = "Configure";
            this.Configure.UseColumnTextForLinkValue = true;
            this.Configure.Width = 98;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.toolStripFilter,
            this.tsConfigure});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1616, 27);
            this.toolStrip1.TabIndex = 3;
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
            this.toolStripFilter.Size = new System.Drawing.Size(34, 24);
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
            // tsConfigure
            // 
            this.tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureDatabaseThresholdsToolStripMenuItem,
            this.configureInstanceThresholdsToolStripMenuItem,
            this.configureRootThresholdsToolStripMenuItem});
            this.tsConfigure.Image = global::DBAChecksGUI.Properties.Resources.SettingsOutline_16x;
            this.tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsConfigure.Name = "tsConfigure";
            this.tsConfigure.Size = new System.Drawing.Size(34, 24);
            this.tsConfigure.Text = "Configure";
            // 
            // configureDatabaseThresholdsToolStripMenuItem
            // 
            this.configureDatabaseThresholdsToolStripMenuItem.Name = "configureDatabaseThresholdsToolStripMenuItem";
            this.configureDatabaseThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureDatabaseThresholdsToolStripMenuItem.Text = "Configure Database Thresholds";
            this.configureDatabaseThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureDatabaseThresholdsToolStripMenuItem_Click);
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            this.configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            this.configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            this.configureInstanceThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureInstanceThresholdsToolStripMenuItem_Click);
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            this.configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            this.configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            this.configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            this.configureRootThresholdsToolStripMenuItem.Click += new System.EventHandler(this.configureRootThresholdsToolStripMenuItem_Click);
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
            // DBFilesControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvFiles);
            this.Controls.Add(this.toolStrip1);
            this.Name = "DBFilesControl";
            this.Size = new System.Drawing.Size(1616, 286);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem warningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undefinedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OKToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Database;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn SizeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn UsedMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn FreeMB;
        private System.Windows.Forms.DataGridViewTextBoxColumn PctFree;
        private System.Windows.Forms.DataGridViewTextBoxColumn NumberOfFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn DBReadOnly;
        private System.Windows.Forms.DataGridViewTextBoxColumn Standby;
        private System.Windows.Forms.DataGridViewTextBoxColumn State;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileSnapshotAge;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
    }
}
