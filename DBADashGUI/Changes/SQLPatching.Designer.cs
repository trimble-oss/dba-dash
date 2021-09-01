namespace DBADashGUI
{
    partial class SQLPatching
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChangedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgvVersion = new System.Windows.Forms.DataGridView();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSQLVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPatchDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEngineEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEditionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductUpdateReference = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductMajorVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBuildType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProductBuild = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResourceVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResourceLastUpdateDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLicenseType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumLicences = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWindowsCaption = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWindowsRelease = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWindowsSKU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshVersion = new System.Windows.Forms.ToolStripButton();
            this.tsCopyVersion = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            this.tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            this.tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVersion)).BeginInit();
            this.toolStrip2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.ChangedDate,
            this.OldVersion,
            this.NewVersion,
            this.OldProductLevel,
            this.NewProductLevel,
            this.OldProductUpdateLevel,
            this.NewProductUpdateLevel,
            this.OldEdition,
            this.NewEdition});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(1204, 317);
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
            // ChangedDate
            // 
            this.ChangedDate.DataPropertyName = "ChangedDate";
            this.ChangedDate.HeaderText = "Changed Date";
            this.ChangedDate.MinimumWidth = 6;
            this.ChangedDate.Name = "ChangedDate";
            this.ChangedDate.ReadOnly = true;
            this.ChangedDate.Width = 118;
            // 
            // OldVersion
            // 
            this.OldVersion.DataPropertyName = "OldVersion";
            this.OldVersion.HeaderText = "Old Version";
            this.OldVersion.MinimumWidth = 6;
            this.OldVersion.Name = "OldVersion";
            this.OldVersion.ReadOnly = true;
            this.OldVersion.Width = 102;
            // 
            // NewVersion
            // 
            this.NewVersion.DataPropertyName = "NewVersion";
            this.NewVersion.HeaderText = "New Version";
            this.NewVersion.MinimumWidth = 6;
            this.NewVersion.Name = "NewVersion";
            this.NewVersion.ReadOnly = true;
            this.NewVersion.Width = 107;
            // 
            // OldProductLevel
            // 
            this.OldProductLevel.DataPropertyName = "OldProductLevel";
            this.OldProductLevel.HeaderText = "Old Product Level";
            this.OldProductLevel.MinimumWidth = 6;
            this.OldProductLevel.Name = "OldProductLevel";
            this.OldProductLevel.ReadOnly = true;
            this.OldProductLevel.Width = 137;
            // 
            // NewProductLevel
            // 
            this.NewProductLevel.DataPropertyName = "NewProductLevel";
            this.NewProductLevel.HeaderText = "New Product Level";
            this.NewProductLevel.MinimumWidth = 6;
            this.NewProductLevel.Name = "NewProductLevel";
            this.NewProductLevel.ReadOnly = true;
            this.NewProductLevel.Width = 142;
            // 
            // OldProductUpdateLevel
            // 
            this.OldProductUpdateLevel.DataPropertyName = "OldProductUpdateLevel";
            this.OldProductUpdateLevel.HeaderText = "Old Product Update Level";
            this.OldProductUpdateLevel.MinimumWidth = 6;
            this.OldProductUpdateLevel.Name = "OldProductUpdateLevel";
            this.OldProductUpdateLevel.ReadOnly = true;
            this.OldProductUpdateLevel.Width = 152;
            // 
            // NewProductUpdateLevel
            // 
            this.NewProductUpdateLevel.DataPropertyName = "NewProductUpdateLevel";
            this.NewProductUpdateLevel.HeaderText = "New Product Update Level";
            this.NewProductUpdateLevel.MinimumWidth = 6;
            this.NewProductUpdateLevel.Name = "NewProductUpdateLevel";
            this.NewProductUpdateLevel.ReadOnly = true;
            this.NewProductUpdateLevel.Width = 156;
            // 
            // OldEdition
            // 
            this.OldEdition.DataPropertyName = "OldEdition";
            this.OldEdition.HeaderText = "Old Edition";
            this.OldEdition.MinimumWidth = 6;
            this.OldEdition.Name = "OldEdition";
            this.OldEdition.ReadOnly = true;
            this.OldEdition.Width = 98;
            // 
            // NewEdition
            // 
            this.NewEdition.DataPropertyName = "NewEdition";
            this.NewEdition.HeaderText = "New Edition";
            this.NewEdition.MinimumWidth = 6;
            this.NewEdition.Name = "NewEdition";
            this.NewEdition.ReadOnly = true;
            this.NewEdition.Width = 102;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvVersion);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(1204, 696);
            this.splitContainer1.SplitterDistance = 348;
            this.splitContainer1.TabIndex = 1;
            // 
            // dgvVersion
            // 
            this.dgvVersion.AllowUserToAddRows = false;
            this.dgvVersion.AllowUserToDeleteRows = false;
            this.dgvVersion.BackgroundColor = System.Drawing.Color.White;
            this.dgvVersion.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dgvVersion.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvVersion.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colSQLVersion,
            this.colPatchDate,
            this.colProductVersion,
            this.colEdition,
            this.colEngineEdition,
            this.colEditionID,
            this.colProductLevel,
            this.colProductUpdateLevel,
            this.colProductUpdateReference,
            this.colProductMajorVersion,
            this.colBuildType,
            this.colProductBuild,
            this.colResourceVersion,
            this.colResourceLastUpdateDateTime,
            this.colLicenseType,
            this.colNumLicences,
            this.colWindowsCaption,
            this.colWindowsRelease,
            this.colWindowsSKU});
            this.dgvVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvVersion.Location = new System.Drawing.Point(0, 27);
            this.dgvVersion.Name = "dgvVersion";
            this.dgvVersion.ReadOnly = true;
            this.dgvVersion.RowHeadersVisible = false;
            this.dgvVersion.RowHeadersWidth = 51;
            this.dgvVersion.RowTemplate.Height = 24;
            this.dgvVersion.Size = new System.Drawing.Size(1204, 321);
            this.dgvVersion.TabIndex = 0;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "ConnectionID";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Width = 90;
            // 
            // colSQLVersion
            // 
            this.colSQLVersion.DataPropertyName = "SQLVersion";
            this.colSQLVersion.HeaderText = "Version";
            this.colSQLVersion.MinimumWidth = 6;
            this.colSQLVersion.Name = "colSQLVersion";
            this.colSQLVersion.ReadOnly = true;
            this.colSQLVersion.Width = 85;
            // 
            // colPatchDate
            // 
            this.colPatchDate.DataPropertyName = "PatchDate";
            this.colPatchDate.HeaderText = "SQL Patch Date";
            this.colPatchDate.MinimumWidth = 6;
            this.colPatchDate.Name = "colPatchDate";
            this.colPatchDate.ReadOnly = true;
            this.colPatchDate.Width = 127;
            // 
            // colProductVersion
            // 
            this.colProductVersion.DataPropertyName = "ProductVersion";
            this.colProductVersion.HeaderText = "Product Version";
            this.colProductVersion.MinimumWidth = 6;
            this.colProductVersion.Name = "colProductVersion";
            this.colProductVersion.ReadOnly = true;
            this.colProductVersion.Width = 127;
            // 
            // colEdition
            // 
            this.colEdition.DataPropertyName = "Edition";
            this.colEdition.HeaderText = "Edition";
            this.colEdition.MinimumWidth = 6;
            this.colEdition.Name = "colEdition";
            this.colEdition.ReadOnly = true;
            this.colEdition.Width = 80;
            // 
            // colEngineEdition
            // 
            this.colEngineEdition.DataPropertyName = "EngineEdition";
            this.colEngineEdition.HeaderText = "Engine Edition";
            this.colEngineEdition.MinimumWidth = 6;
            this.colEngineEdition.Name = "colEngineEdition";
            this.colEngineEdition.ReadOnly = true;
            this.colEngineEdition.Width = 118;
            // 
            // colEditionID
            // 
            this.colEditionID.DataPropertyName = "EditionID";
            this.colEditionID.HeaderText = "Edition ID";
            this.colEditionID.MinimumWidth = 6;
            this.colEditionID.Name = "colEditionID";
            this.colEditionID.ReadOnly = true;
            this.colEditionID.Width = 90;
            // 
            // colProductLevel
            // 
            this.colProductLevel.DataPropertyName = "ProductLevel";
            this.colProductLevel.HeaderText = "Product Level";
            this.colProductLevel.MinimumWidth = 6;
            this.colProductLevel.Name = "colProductLevel";
            this.colProductLevel.ReadOnly = true;
            this.colProductLevel.Width = 114;
            // 
            // colProductUpdateLevel
            // 
            this.colProductUpdateLevel.DataPropertyName = "ProductUpdateLevel";
            this.colProductUpdateLevel.HeaderText = "Product Update Level";
            this.colProductUpdateLevel.MinimumWidth = 6;
            this.colProductUpdateLevel.Name = "colProductUpdateLevel";
            this.colProductUpdateLevel.ReadOnly = true;
            this.colProductUpdateLevel.Width = 128;
            // 
            // colProductUpdateReference
            // 
            this.colProductUpdateReference.DataPropertyName = "ProductUpdateReference";
            this.colProductUpdateReference.HeaderText = "Product Update Reference";
            this.colProductUpdateReference.MinimumWidth = 6;
            this.colProductUpdateReference.Name = "colProductUpdateReference";
            this.colProductUpdateReference.ReadOnly = true;
            this.colProductUpdateReference.Width = 188;
            // 
            // colProductMajorVersion
            // 
            this.colProductMajorVersion.DataPropertyName = "ProductMajorVersion";
            this.colProductMajorVersion.HeaderText = "Major Version";
            this.colProductMajorVersion.MinimumWidth = 6;
            this.colProductMajorVersion.Name = "colProductMajorVersion";
            this.colProductMajorVersion.ReadOnly = true;
            this.colProductMajorVersion.Width = 114;
            // 
            // colBuildType
            // 
            this.colBuildType.DataPropertyName = "ProductBuildType";
            this.colBuildType.HeaderText = "Build Type";
            this.colBuildType.MinimumWidth = 6;
            this.colBuildType.Name = "colBuildType";
            this.colBuildType.ReadOnly = true;
            this.colBuildType.Width = 96;
            // 
            // colProductBuild
            // 
            this.colProductBuild.DataPropertyName = "ProductBuild";
            this.colProductBuild.HeaderText = "Product Build";
            this.colProductBuild.MinimumWidth = 6;
            this.colProductBuild.Name = "colProductBuild";
            this.colProductBuild.ReadOnly = true;
            this.colProductBuild.Width = 111;
            // 
            // colResourceVersion
            // 
            this.colResourceVersion.DataPropertyName = "ResourceVersion";
            this.colResourceVersion.HeaderText = "Resource Version";
            this.colResourceVersion.MinimumWidth = 6;
            this.colResourceVersion.Name = "colResourceVersion";
            this.colResourceVersion.ReadOnly = true;
            this.colResourceVersion.Width = 137;
            // 
            // colResourceLastUpdateDateTime
            // 
            this.colResourceLastUpdateDateTime.DataPropertyName = "ResourceLastUpdateDateTime";
            this.colResourceLastUpdateDateTime.HeaderText = "Resource Last Update";
            this.colResourceLastUpdateDateTime.MinimumWidth = 6;
            this.colResourceLastUpdateDateTime.Name = "colResourceLastUpdateDateTime";
            this.colResourceLastUpdateDateTime.ReadOnly = true;
            this.colResourceLastUpdateDateTime.Width = 122;
            // 
            // colLicenseType
            // 
            this.colLicenseType.DataPropertyName = "LicenseType";
            this.colLicenseType.HeaderText = "License Type";
            this.colLicenseType.MinimumWidth = 6;
            this.colLicenseType.Name = "colLicenseType";
            this.colLicenseType.ReadOnly = true;
            this.colLicenseType.Width = 112;
            // 
            // colNumLicences
            // 
            this.colNumLicences.DataPropertyName = "NumLicenses";
            this.colNumLicences.HeaderText = "Num Licenses";
            this.colNumLicences.MinimumWidth = 6;
            this.colNumLicences.Name = "colNumLicences";
            this.colNumLicences.ReadOnly = true;
            this.colNumLicences.Width = 116;
            // 
            // colWindowsCaption
            // 
            this.colWindowsCaption.DataPropertyName = "WindowsCaption";
            this.colWindowsCaption.HeaderText = "Windows Caption";
            this.colWindowsCaption.MinimumWidth = 6;
            this.colWindowsCaption.Name = "colWindowsCaption";
            this.colWindowsCaption.ReadOnly = true;
            this.colWindowsCaption.Width = 133;
            // 
            // colWindowsRelease
            // 
            this.colWindowsRelease.DataPropertyName = "WindowsRelease";
            this.colWindowsRelease.HeaderText = "Windows Release";
            this.colWindowsRelease.MinimumWidth = 6;
            this.colWindowsRelease.Name = "colWindowsRelease";
            this.colWindowsRelease.ReadOnly = true;
            this.colWindowsRelease.Width = 136;
            // 
            // colWindowsSKU
            // 
            this.colWindowsSKU.DataPropertyName = "WindowsSKU";
            this.colWindowsSKU.HeaderText = "Windows SKU";
            this.colWindowsSKU.MinimumWidth = 6;
            this.colWindowsSKU.Name = "colWindowsSKU";
            this.colWindowsSKU.ReadOnly = true;
            this.colWindowsSKU.Width = 115;
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.tsRefreshVersion,
            this.tsCopyVersion,
            this.tsExcel});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(1204, 27);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(94, 24);
            this.toolStripLabel2.Text = "Version Info";
            // 
            // tsRefreshVersion
            // 
            this.tsRefreshVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshVersion.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshVersion.Name = "tsRefreshVersion";
            this.tsRefreshVersion.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshVersion.Text = "Refresh";
            this.tsRefreshVersion.Click += new System.EventHandler(this.tsRefreshVersion_Click);
            // 
            // tsCopyVersion
            // 
            this.tsCopyVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyVersion.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyVersion.Name = "tsCopyVersion";
            this.tsCopyVersion.Size = new System.Drawing.Size(29, 24);
            this.tsCopyVersion.Text = "Copy";
            this.tsCopyVersion.Click += new System.EventHandler(this.tsCopyVersion_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsRefreshHistory,
            this.tsCopyHistory,
            this.tsExcelHistory});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1204, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(61, 24);
            this.toolStripLabel1.Text = "History";
            // 
            // tsRefreshHistory
            // 
            this.tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshHistory.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshHistory.Name = "tsRefreshHistory";
            this.tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshHistory.Text = "Refresh";
            this.tsRefreshHistory.Click += new System.EventHandler(this.tsRefreshHistory_Click);
            // 
            // tsCopyHistory
            // 
            this.tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyHistory.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyHistory.Name = "tsCopyHistory";
            this.tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            this.tsCopyHistory.Text = "Copy";
            this.tsCopyHistory.Click += new System.EventHandler(this.tsCopyHistory_Click);
            // 
            // tsExcelHistory
            // 
            this.tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcelHistory.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcelHistory.Name = "tsExcelHistory";
            this.tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            this.tsExcelHistory.Text = "Export Excel";
            this.tsExcelHistory.Click += new System.EventHandler(this.tsExcelHistory_Click);
            // 
            // SQLPatching
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "SQLPatching";
            this.Size = new System.Drawing.Size(1204, 696);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvVersion)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChangedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldProductLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewProductLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldProductUpdateLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewProductUpdateLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewEdition;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSQLVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPatchDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEngineEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEditionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductUpdateLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductUpdateReference;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductMajorVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBuildType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProductBuild;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResourceVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResourceLastUpdateDateTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLicenseType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumLicences;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWindowsCaption;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWindowsRelease;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWindowsSKU;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsRefreshVersion;
        private System.Windows.Forms.ToolStripButton tsCopyVersion;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
    }
}
