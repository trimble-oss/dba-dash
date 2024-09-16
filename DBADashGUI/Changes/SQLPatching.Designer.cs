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
            dgv = new System.Windows.Forms.DataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ChangedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            OldVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NewVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            OldProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NewProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            OldProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NewProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            OldEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            NewEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvVersion = new System.Windows.Forms.DataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSQLVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPatchDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEngineEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEditionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductUpdateReference = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductMajorVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colBuildType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colProductBuild = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colResourceVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colResourceLastUpdateDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colLicenseType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colNumLicences = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWindowsCaption = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWindowsRelease = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colWindowsSKU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshVersion = new System.Windows.Forms.ToolStripButton();
            tsCopyVersion = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsConfig = new System.Windows.Forms.ToolStripDropDownButton();
            thresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sPBehindWarningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sPBehindCriticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cUBehindWarningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cUBehindCriticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            daysUntilSupportEndsWarningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            daysUntilSupportEndsCriticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            buildReferenceAgeWarningThresholdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            buildReferenceAgeCriticalThresholdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            buildReferenceUpdateExclusionPeriodToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            resetToDefaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsViewBuildReference = new System.Windows.Forms.ToolStripButton();
            tsUpdateBuildReference = new System.Windows.Forms.ToolStripButton();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvVersion).BeginInit();
            toolStrip2.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, ChangedDate, OldVersion, NewVersion, OldProductLevel, NewProductLevel, OldProductUpdateLevel, NewProductUpdateLevel, OldEdition, NewEdition });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1205, 407);
            dgv.TabIndex = 0;
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
            // ChangedDate
            // 
            ChangedDate.DataPropertyName = "ChangedDate";
            ChangedDate.HeaderText = "Changed Date";
            ChangedDate.MinimumWidth = 6;
            ChangedDate.Name = "ChangedDate";
            ChangedDate.ReadOnly = true;
            ChangedDate.Width = 118;
            // 
            // OldVersion
            // 
            OldVersion.DataPropertyName = "OldVersion";
            OldVersion.HeaderText = "Old Version";
            OldVersion.MinimumWidth = 6;
            OldVersion.Name = "OldVersion";
            OldVersion.ReadOnly = true;
            OldVersion.Width = 102;
            // 
            // NewVersion
            // 
            NewVersion.DataPropertyName = "NewVersion";
            NewVersion.HeaderText = "New Version";
            NewVersion.MinimumWidth = 6;
            NewVersion.Name = "NewVersion";
            NewVersion.ReadOnly = true;
            NewVersion.Width = 107;
            // 
            // OldProductLevel
            // 
            OldProductLevel.DataPropertyName = "OldProductLevel";
            OldProductLevel.HeaderText = "Old Product Level";
            OldProductLevel.MinimumWidth = 6;
            OldProductLevel.Name = "OldProductLevel";
            OldProductLevel.ReadOnly = true;
            OldProductLevel.Width = 137;
            // 
            // NewProductLevel
            // 
            NewProductLevel.DataPropertyName = "NewProductLevel";
            NewProductLevel.HeaderText = "New Product Level";
            NewProductLevel.MinimumWidth = 6;
            NewProductLevel.Name = "NewProductLevel";
            NewProductLevel.ReadOnly = true;
            NewProductLevel.Width = 142;
            // 
            // OldProductUpdateLevel
            // 
            OldProductUpdateLevel.DataPropertyName = "OldProductUpdateLevel";
            OldProductUpdateLevel.HeaderText = "Old Product Update Level";
            OldProductUpdateLevel.MinimumWidth = 6;
            OldProductUpdateLevel.Name = "OldProductUpdateLevel";
            OldProductUpdateLevel.ReadOnly = true;
            OldProductUpdateLevel.Width = 152;
            // 
            // NewProductUpdateLevel
            // 
            NewProductUpdateLevel.DataPropertyName = "NewProductUpdateLevel";
            NewProductUpdateLevel.HeaderText = "New Product Update Level";
            NewProductUpdateLevel.MinimumWidth = 6;
            NewProductUpdateLevel.Name = "NewProductUpdateLevel";
            NewProductUpdateLevel.ReadOnly = true;
            NewProductUpdateLevel.Width = 156;
            // 
            // OldEdition
            // 
            OldEdition.DataPropertyName = "OldEdition";
            OldEdition.HeaderText = "Old Edition";
            OldEdition.MinimumWidth = 6;
            OldEdition.Name = "OldEdition";
            OldEdition.ReadOnly = true;
            OldEdition.Width = 98;
            // 
            // NewEdition
            // 
            NewEdition.DataPropertyName = "NewEdition";
            NewEdition.HeaderText = "New Edition";
            NewEdition.MinimumWidth = 6;
            NewEdition.Name = "NewEdition";
            NewEdition.ReadOnly = true;
            NewEdition.Width = 102;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgvVersion);
            splitContainer1.Panel1.Controls.Add(toolStrip2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Panel2.Controls.Add(toolStrip1);
            splitContainer1.Size = new System.Drawing.Size(1205, 869);
            splitContainer1.SplitterDistance = 430;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // dgvVersion
            // 
            dgvVersion.AllowUserToAddRows = false;
            dgvVersion.AllowUserToDeleteRows = false;
            dgvVersion.AllowUserToOrderColumns = true;
            dgvVersion.BackgroundColor = System.Drawing.Color.White;
            dgvVersion.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvVersion.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colSQLVersion, colPatchDate, colProductVersion, colEdition, colEngineEdition, colEditionID, colProductLevel, colProductUpdateLevel, colProductUpdateReference, colProductMajorVersion, colBuildType, colProductBuild, colResourceVersion, colResourceLastUpdateDateTime, colLicenseType, colNumLicences, colWindowsCaption, colWindowsRelease, colWindowsSKU });
            dgvVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvVersion.Location = new System.Drawing.Point(0, 27);
            dgvVersion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvVersion.Name = "dgvVersion";
            dgvVersion.ReadOnly = true;
            dgvVersion.RowHeadersVisible = false;
            dgvVersion.RowHeadersWidth = 51;
            dgvVersion.RowTemplate.Height = 24;
            dgvVersion.Size = new System.Drawing.Size(1205, 403);
            dgvVersion.TabIndex = 0;
            dgvVersion.CellContentClick += DgvVersion_CellContentClick;
            dgvVersion.ColumnHeaderMouseClick += DgvVersion_ColumnHeaderMouseClick;
            dgvVersion.RowsAdded += DgvVersion_RowsAdded;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "InstanceDisplayName";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Width = 90;
            // 
            // colSQLVersion
            // 
            colSQLVersion.DataPropertyName = "SQLVersion";
            colSQLVersion.HeaderText = "Version";
            colSQLVersion.MinimumWidth = 6;
            colSQLVersion.Name = "colSQLVersion";
            colSQLVersion.ReadOnly = true;
            colSQLVersion.Width = 85;
            // 
            // colPatchDate
            // 
            colPatchDate.DataPropertyName = "PatchDate";
            colPatchDate.HeaderText = "SQL Patch Date";
            colPatchDate.MinimumWidth = 6;
            colPatchDate.Name = "colPatchDate";
            colPatchDate.ReadOnly = true;
            colPatchDate.Width = 127;
            // 
            // colProductVersion
            // 
            colProductVersion.DataPropertyName = "ProductVersion";
            colProductVersion.HeaderText = "Product Version";
            colProductVersion.MinimumWidth = 6;
            colProductVersion.Name = "colProductVersion";
            colProductVersion.ReadOnly = true;
            colProductVersion.Width = 127;
            // 
            // colEdition
            // 
            colEdition.DataPropertyName = "Edition";
            colEdition.HeaderText = "Edition";
            colEdition.MinimumWidth = 6;
            colEdition.Name = "colEdition";
            colEdition.ReadOnly = true;
            colEdition.Width = 80;
            // 
            // colEngineEdition
            // 
            colEngineEdition.DataPropertyName = "EngineEdition";
            colEngineEdition.HeaderText = "Engine Edition";
            colEngineEdition.MinimumWidth = 6;
            colEngineEdition.Name = "colEngineEdition";
            colEngineEdition.ReadOnly = true;
            colEngineEdition.Width = 118;
            // 
            // colEditionID
            // 
            colEditionID.DataPropertyName = "EditionID";
            colEditionID.HeaderText = "Edition ID";
            colEditionID.MinimumWidth = 6;
            colEditionID.Name = "colEditionID";
            colEditionID.ReadOnly = true;
            colEditionID.Width = 90;
            // 
            // colProductLevel
            // 
            colProductLevel.DataPropertyName = "ProductLevel";
            colProductLevel.HeaderText = "Product Level";
            colProductLevel.MinimumWidth = 6;
            colProductLevel.Name = "colProductLevel";
            colProductLevel.ReadOnly = true;
            colProductLevel.Width = 114;
            // 
            // colProductUpdateLevel
            // 
            colProductUpdateLevel.DataPropertyName = "ProductUpdateLevel";
            colProductUpdateLevel.HeaderText = "Product Update Level";
            colProductUpdateLevel.MinimumWidth = 6;
            colProductUpdateLevel.Name = "colProductUpdateLevel";
            colProductUpdateLevel.ReadOnly = true;
            colProductUpdateLevel.Width = 128;
            // 
            // colProductUpdateReference
            // 
            colProductUpdateReference.DataPropertyName = "ProductUpdateReference";
            colProductUpdateReference.HeaderText = "Product Update Reference";
            colProductUpdateReference.MinimumWidth = 6;
            colProductUpdateReference.Name = "colProductUpdateReference";
            colProductUpdateReference.ReadOnly = true;
            colProductUpdateReference.Width = 188;
            // 
            // colProductMajorVersion
            // 
            colProductMajorVersion.DataPropertyName = "ProductMajorVersion";
            colProductMajorVersion.HeaderText = "Major Version";
            colProductMajorVersion.MinimumWidth = 6;
            colProductMajorVersion.Name = "colProductMajorVersion";
            colProductMajorVersion.ReadOnly = true;
            colProductMajorVersion.Width = 114;
            // 
            // colBuildType
            // 
            colBuildType.DataPropertyName = "ProductBuildType";
            colBuildType.HeaderText = "Build Type";
            colBuildType.MinimumWidth = 6;
            colBuildType.Name = "colBuildType";
            colBuildType.ReadOnly = true;
            colBuildType.Width = 96;
            // 
            // colProductBuild
            // 
            colProductBuild.DataPropertyName = "ProductBuild";
            colProductBuild.HeaderText = "Product Build";
            colProductBuild.MinimumWidth = 6;
            colProductBuild.Name = "colProductBuild";
            colProductBuild.ReadOnly = true;
            colProductBuild.Width = 111;
            // 
            // colResourceVersion
            // 
            colResourceVersion.DataPropertyName = "ResourceVersion";
            colResourceVersion.HeaderText = "Resource Version";
            colResourceVersion.MinimumWidth = 6;
            colResourceVersion.Name = "colResourceVersion";
            colResourceVersion.ReadOnly = true;
            colResourceVersion.Width = 137;
            // 
            // colResourceLastUpdateDateTime
            // 
            colResourceLastUpdateDateTime.DataPropertyName = "ResourceLastUpdateDateTime";
            colResourceLastUpdateDateTime.HeaderText = "Resource Last Update";
            colResourceLastUpdateDateTime.MinimumWidth = 6;
            colResourceLastUpdateDateTime.Name = "colResourceLastUpdateDateTime";
            colResourceLastUpdateDateTime.ReadOnly = true;
            colResourceLastUpdateDateTime.Width = 122;
            // 
            // colLicenseType
            // 
            colLicenseType.DataPropertyName = "LicenseType";
            colLicenseType.HeaderText = "License Type";
            colLicenseType.MinimumWidth = 6;
            colLicenseType.Name = "colLicenseType";
            colLicenseType.ReadOnly = true;
            colLicenseType.Width = 112;
            // 
            // colNumLicences
            // 
            colNumLicences.DataPropertyName = "NumLicenses";
            colNumLicences.HeaderText = "Num Licenses";
            colNumLicences.MinimumWidth = 6;
            colNumLicences.Name = "colNumLicences";
            colNumLicences.ReadOnly = true;
            colNumLicences.Width = 116;
            // 
            // colWindowsCaption
            // 
            colWindowsCaption.DataPropertyName = "WindowsCaption";
            colWindowsCaption.HeaderText = "Windows Caption";
            colWindowsCaption.MinimumWidth = 6;
            colWindowsCaption.Name = "colWindowsCaption";
            colWindowsCaption.ReadOnly = true;
            colWindowsCaption.Width = 133;
            // 
            // colWindowsRelease
            // 
            colWindowsRelease.DataPropertyName = "WindowsRelease";
            colWindowsRelease.HeaderText = "Windows Release";
            colWindowsRelease.MinimumWidth = 6;
            colWindowsRelease.Name = "colWindowsRelease";
            colWindowsRelease.ReadOnly = true;
            colWindowsRelease.Width = 136;
            // 
            // colWindowsSKU
            // 
            colWindowsSKU.DataPropertyName = "WindowsSKU";
            colWindowsSKU.HeaderText = "Windows SKU";
            colWindowsSKU.MinimumWidth = 6;
            colWindowsSKU.Name = "colWindowsSKU";
            colWindowsSKU.ReadOnly = true;
            colWindowsSKU.Width = 115;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel2, tsRefreshVersion, tsCopyVersion, tsExcel, tsCols, tsConfig, tsViewBuildReference, tsUpdateBuildReference });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(1205, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(94, 24);
            toolStripLabel2.Text = "Version Info";
            // 
            // tsRefreshVersion
            // 
            tsRefreshVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshVersion.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshVersion.Name = "tsRefreshVersion";
            tsRefreshVersion.Size = new System.Drawing.Size(29, 24);
            tsRefreshVersion.Text = "Refresh";
            tsRefreshVersion.Click += TsRefreshVersion_Click;
            // 
            // tsCopyVersion
            // 
            tsCopyVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyVersion.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyVersion.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyVersion.Name = "tsCopyVersion";
            tsCopyVersion.Size = new System.Drawing.Size(29, 24);
            tsCopyVersion.Text = "Copy";
            tsCopyVersion.Click += TsCopyVersion_Click;
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
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsConfig
            // 
            tsConfig.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfig.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { thresholdsToolStripMenuItem });
            tsConfig.Image = Properties.Resources.SettingsOutline_16x;
            tsConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfig.Name = "tsConfig";
            tsConfig.Size = new System.Drawing.Size(34, 24);
            tsConfig.Text = "Config";
            // 
            // thresholdsToolStripMenuItem
            // 
            thresholdsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { sPBehindWarningToolStripMenuItem, sPBehindCriticalToolStripMenuItem, cUBehindWarningToolStripMenuItem, cUBehindCriticalToolStripMenuItem, daysUntilSupportEndsWarningToolStripMenuItem, daysUntilSupportEndsCriticalToolStripMenuItem, daysUntilMainstreamSupportEndsWarningToolStripMenuItem, daysUntilMainstreamSupportEndsCriticalToolStripMenuItem, buildReferenceAgeWarningThresholdToolStripMenuItem, buildReferenceAgeCriticalThresholdToolStripMenuItem, buildReferenceUpdateExclusionPeriodToolStripMenuItem, toolStripSeparator1, resetToDefaultToolStripMenuItem });
            thresholdsToolStripMenuItem.Name = "thresholdsToolStripMenuItem";
            thresholdsToolStripMenuItem.Size = new System.Drawing.Size(163, 26);
            thresholdsToolStripMenuItem.Text = "Thresholds";
            // 
            // sPBehindWarningToolStripMenuItem
            // 
            sPBehindWarningToolStripMenuItem.Name = "sPBehindWarningToolStripMenuItem";
            sPBehindWarningToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            sPBehindWarningToolStripMenuItem.Tag = "GUISPBehindWarningThreshold";
            sPBehindWarningToolStripMenuItem.Text = "SP Behind Warning";
            sPBehindWarningToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // sPBehindCriticalToolStripMenuItem
            // 
            sPBehindCriticalToolStripMenuItem.Name = "sPBehindCriticalToolStripMenuItem";
            sPBehindCriticalToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            sPBehindCriticalToolStripMenuItem.Tag = "GUISPBehindCriticalThreshold";
            sPBehindCriticalToolStripMenuItem.Text = "SP Behind Critical";
            sPBehindCriticalToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // cUBehindWarningToolStripMenuItem
            // 
            cUBehindWarningToolStripMenuItem.Name = "cUBehindWarningToolStripMenuItem";
            cUBehindWarningToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            cUBehindWarningToolStripMenuItem.Tag = "GUICUBehindWarningThreshold";
            cUBehindWarningToolStripMenuItem.Text = "CU Behind Warning";
            cUBehindWarningToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // cUBehindCriticalToolStripMenuItem
            // 
            cUBehindCriticalToolStripMenuItem.Name = "cUBehindCriticalToolStripMenuItem";
            cUBehindCriticalToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            cUBehindCriticalToolStripMenuItem.Tag = "GUICUBehindCriticalThreshold";
            cUBehindCriticalToolStripMenuItem.Text = "CU Behind Critical";
            cUBehindCriticalToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // daysUntilSupportEndsWarningToolStripMenuItem
            // 
            daysUntilSupportEndsWarningToolStripMenuItem.Name = "daysUntilSupportEndsWarningToolStripMenuItem";
            daysUntilSupportEndsWarningToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            daysUntilSupportEndsWarningToolStripMenuItem.Tag = "GUIDaysUntilSupportEndsWarningThreshold";
            daysUntilSupportEndsWarningToolStripMenuItem.Text = "Days Until Support Ends Warning";
            daysUntilSupportEndsWarningToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // daysUntilSupportEndsCriticalToolStripMenuItem
            // 
            daysUntilSupportEndsCriticalToolStripMenuItem.Name = "daysUntilSupportEndsCriticalToolStripMenuItem";
            daysUntilSupportEndsCriticalToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            daysUntilSupportEndsCriticalToolStripMenuItem.Tag = "GUIDaysUntilSupportEndsCriticalThreshold";
            daysUntilSupportEndsCriticalToolStripMenuItem.Text = "Days Until Support Ends Critical";
            daysUntilSupportEndsCriticalToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // daysUntilMainstreamSupportEndsWarningToolStripMenuItem
            // 
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Name = "daysUntilMainstreamSupportEndsWarningToolStripMenuItem";
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Tag = "GUIDaysUntilMainstreamSupportEndsWarningThreshold";
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Text = "Days Until Mainstream Support Ends Warning";
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // daysUntilMainstreamSupportEndsCriticalToolStripMenuItem
            // 
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Name = "daysUntilMainstreamSupportEndsCriticalToolStripMenuItem";
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Tag = "GUIDaysUntilMainstreamSupportEndsCriticalThreshold";
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Text = "Days Until Mainstream Support Ends Critical";
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // buildReferenceAgeWarningThresholdToolStripMenuItem
            // 
            buildReferenceAgeWarningThresholdToolStripMenuItem.Name = "buildReferenceAgeWarningThresholdToolStripMenuItem";
            buildReferenceAgeWarningThresholdToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            buildReferenceAgeWarningThresholdToolStripMenuItem.Tag = "GUIBuildReferenceAgeWarningThreshold";
            buildReferenceAgeWarningThresholdToolStripMenuItem.Text = "Build Reference Age Warning Threshold";
            buildReferenceAgeWarningThresholdToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // buildReferenceAgeCriticalThresholdToolStripMenuItem
            // 
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Name = "buildReferenceAgeCriticalThresholdToolStripMenuItem";
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Tag = "GUIBuildReferenceAgeCriticalThreshold";
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Text = "Build Reference Age Critical Threshold";
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // buildReferenceUpdateExclusionPeriodToolStripMenuItem
            // 
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Name = "buildReferenceUpdateExclusionPeriodToolStripMenuItem";
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Tag = "GUIBuildReferenceUpdateExclusionPeriod";
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Text = "Build Reference Update Exclusion Period";
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.ToolTipText = "Don't show a build reference is out of date warning if we have checked for updates in X days";
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Click += SetThreshold_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(389, 6);
            // 
            // resetToDefaultToolStripMenuItem
            // 
            resetToDefaultToolStripMenuItem.Name = "resetToDefaultToolStripMenuItem";
            resetToDefaultToolStripMenuItem.Size = new System.Drawing.Size(392, 26);
            resetToDefaultToolStripMenuItem.Text = "Reset to Default";
            resetToDefaultToolStripMenuItem.Click += ResetToDefaultToolStripMenuItem_Click;
            // 
            // tsViewBuildReference
            // 
            tsViewBuildReference.Image = Properties.Resources.Table_16x;
            tsViewBuildReference.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsViewBuildReference.Name = "tsViewBuildReference";
            tsViewBuildReference.Size = new System.Drawing.Size(137, 24);
            tsViewBuildReference.Text = "Build Reference";
            tsViewBuildReference.Click += TsViewBuildReference_Click;
            // 
            // tsUpdateBuildReference
            // 
            tsUpdateBuildReference.Image = Properties.Resources.CloudDownload_16x;
            tsUpdateBuildReference.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsUpdateBuildReference.Name = "tsUpdateBuildReference";
            tsUpdateBuildReference.Size = new System.Drawing.Size(190, 24);
            tsUpdateBuildReference.Text = "Update Build Reference";
            tsUpdateBuildReference.Click += TsUpdateBuildReference_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefreshHistory, tsCopyHistory, tsExcelHistory });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1205, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(61, 24);
            toolStripLabel1.Text = "History";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHistory.Name = "tsRefreshHistory";
            tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            tsRefreshHistory.Text = "Refresh";
            tsRefreshHistory.Click += TsRefreshHistory_Click;
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // SQLPatching
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "SQLPatching";
            Size = new System.Drawing.Size(1205, 869);
            Load += SQLPatching_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvVersion).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvVersion;
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
        private System.Windows.Forms.ToolStripButton tsCols;
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
        private System.Windows.Forms.ToolStripButton tsUpdateBuildReference;
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
        private System.Windows.Forms.ToolStripButton tsViewBuildReference;
        private System.Windows.Forms.ToolStripDropDownButton tsConfig;
        private System.Windows.Forms.ToolStripMenuItem thresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sPBehindWarningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sPBehindCriticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cUBehindWarningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cUBehindCriticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daysUntilSupportEndsWarningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daysUntilSupportEndsCriticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daysUntilMainstreamSupportEndsWarningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem daysUntilMainstreamSupportEndsCriticalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetToDefaultToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem buildReferenceAgeWarningThresholdToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem buildReferenceAgeCriticalThresholdToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem buildReferenceUpdateExclusionPeriodToolStripMenuItem;
    }
}
