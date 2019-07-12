namespace DBAChecksConfigTool
{
    partial class DBAChecksConfigTool
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.bttAddTag = new System.Windows.Forms.Button();
            this.txtTag = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkTags = new System.Windows.Forms.CheckedListBox();
            this.cboInstances = new System.Windows.Forms.ComboBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.optInherit = new System.Windows.Forms.RadioButton();
            this.pnlThresholds = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.lblDriveCritical = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDriveWarning = new System.Windows.Forms.Label();
            this.numDriveWarning = new System.Windows.Forms.NumericUpDown();
            this.numDriveCritical = new System.Windows.Forms.NumericUpDown();
            this.bttnAddDriveThreshold = new System.Windows.Forms.Button();
            this.dgvDriveThresholds = new System.Windows.Forms.DataGridView();
            this.OptDisabled = new System.Windows.Forms.RadioButton();
            this.optGB = new System.Windows.Forms.RadioButton();
            this.optPercent = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.cboDrives = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboDrivesInstances = new System.Windows.Forms.ComboBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pnlBackupThresholds = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.numFullWarning = new System.Windows.Forms.NumericUpDown();
            this.numFullCritical = new System.Windows.Forms.NumericUpDown();
            this.bttnUpdateBackup = new System.Windows.Forms.Button();
            this.dgvBackup = new System.Windows.Forms.DataGridView();
            this.label11 = new System.Windows.Forms.Label();
            this.cboBackupDatabase = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.cboBackupInstance = new System.Windows.Forms.ComboBox();
            this.chkFull = new System.Windows.Forms.CheckBox();
            this.chkDiff = new System.Windows.Forms.CheckBox();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.numDiffWarning = new System.Windows.Forms.NumericUpDown();
            this.numDiffCritical = new System.Windows.Forms.NumericUpDown();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.numLogWarning = new System.Windows.Forms.NumericUpDown();
            this.numLogCritical = new System.Windows.Forms.NumericUpDown();
            this.chkBackupInherit = new System.Windows.Forms.CheckBox();
            this.chkUsePartial = new System.Windows.Forms.CheckBox();
            this.chkUseFG = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDriveThresholds)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.pnlBackupThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFullWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackup)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(16, 15);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1195, 680);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.bttAddTag);
            this.tabPage1.Controls.Add(this.txtTag);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.chkTags);
            this.tabPage1.Controls.Add(this.cboInstances);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Size = new System.Drawing.Size(1187, 651);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Tags";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 76);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Tag";
            // 
            // bttAddTag
            // 
            this.bttAddTag.Location = new System.Drawing.Point(475, 73);
            this.bttAddTag.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bttAddTag.Name = "bttAddTag";
            this.bttAddTag.Size = new System.Drawing.Size(100, 28);
            this.bttAddTag.TabIndex = 4;
            this.bttAddTag.Text = "Add";
            this.bttAddTag.UseVisualStyleBackColor = true;
            this.bttAddTag.Click += new System.EventHandler(this.bttAddTag_Click);
            // 
            // txtTag
            // 
            this.txtTag.Location = new System.Drawing.Point(103, 73);
            this.txtTag.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtTag.Name = "txtTag";
            this.txtTag.Size = new System.Drawing.Size(363, 22);
            this.txtTag.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 31);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Instance:";
            // 
            // chkTags
            // 
            this.chkTags.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.chkTags.FormattingEnabled = true;
            this.chkTags.Location = new System.Drawing.Point(103, 117);
            this.chkTags.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chkTags.Name = "chkTags";
            this.chkTags.Size = new System.Drawing.Size(363, 514);
            this.chkTags.TabIndex = 1;
            this.chkTags.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chkTags_ItemCheck);
            this.chkTags.SelectedIndexChanged += new System.EventHandler(this.chkTags_SelectedIndexChanged);
            // 
            // cboInstances
            // 
            this.cboInstances.DisplayMember = "Instance";
            this.cboInstances.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstances.FormattingEnabled = true;
            this.cboInstances.Location = new System.Drawing.Point(103, 27);
            this.cboInstances.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cboInstances.Name = "cboInstances";
            this.cboInstances.Size = new System.Drawing.Size(363, 24);
            this.cboInstances.TabIndex = 0;
            this.cboInstances.ValueMember = "InstanceID";
            this.cboInstances.SelectedIndexChanged += new System.EventHandler(this.cboInstances_SelectedIndexChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.optInherit);
            this.tabPage2.Controls.Add(this.pnlThresholds);
            this.tabPage2.Controls.Add(this.bttnAddDriveThreshold);
            this.tabPage2.Controls.Add(this.dgvDriveThresholds);
            this.tabPage2.Controls.Add(this.OptDisabled);
            this.tabPage2.Controls.Add(this.optGB);
            this.tabPage2.Controls.Add(this.optPercent);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.cboDrives);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.cboDrivesInstances);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Size = new System.Drawing.Size(1187, 651);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Drives";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // optInherit
            // 
            this.optInherit.AutoSize = true;
            this.optInherit.Location = new System.Drawing.Point(388, 114);
            this.optInherit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.optInherit.Name = "optInherit";
            this.optInherit.Size = new System.Drawing.Size(68, 21);
            this.optInherit.TabIndex = 27;
            this.optInherit.Text = "Inherit";
            this.optInherit.UseVisualStyleBackColor = true;
            this.optInherit.CheckedChanged += new System.EventHandler(this.optInherit_CheckedChanged);
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Controls.Add(this.label6);
            this.pnlThresholds.Controls.Add(this.lblDriveCritical);
            this.pnlThresholds.Controls.Add(this.label5);
            this.pnlThresholds.Controls.Add(this.lblDriveWarning);
            this.pnlThresholds.Controls.Add(this.numDriveWarning);
            this.pnlThresholds.Controls.Add(this.numDriveCritical);
            this.pnlThresholds.Location = new System.Drawing.Point(96, 156);
            this.pnlThresholds.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(364, 86);
            this.pnlThresholds.TabIndex = 26;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 52);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 17);
            this.label6.TabIndex = 20;
            this.label6.Text = "Critical:";
            // 
            // lblDriveCritical
            // 
            this.lblDriveCritical.AutoSize = true;
            this.lblDriveCritical.Location = new System.Drawing.Point(244, 50);
            this.lblDriveCritical.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveCritical.Name = "lblDriveCritical";
            this.lblDriveCritical.Size = new System.Drawing.Size(20, 17);
            this.lblDriveCritical.TabIndex = 25;
            this.lblDriveCritical.Text = "%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 16);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 17);
            this.label5.TabIndex = 19;
            this.label5.Text = "Warning:";
            // 
            // lblDriveWarning
            // 
            this.lblDriveWarning.AutoSize = true;
            this.lblDriveWarning.Location = new System.Drawing.Point(244, 18);
            this.lblDriveWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveWarning.Name = "lblDriveWarning";
            this.lblDriveWarning.Size = new System.Drawing.Size(20, 17);
            this.lblDriveWarning.TabIndex = 24;
            this.lblDriveWarning.Text = "%";
            // 
            // numDriveWarning
            // 
            this.numDriveWarning.DecimalPlaces = 1;
            this.numDriveWarning.Location = new System.Drawing.Point(76, 16);
            this.numDriveWarning.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.numDriveWarning.Name = "numDriveWarning";
            this.numDriveWarning.Size = new System.Drawing.Size(160, 22);
            this.numDriveWarning.TabIndex = 22;
            this.numDriveWarning.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // numDriveCritical
            // 
            this.numDriveCritical.DecimalPlaces = 1;
            this.numDriveCritical.Location = new System.Drawing.Point(76, 48);
            this.numDriveCritical.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.numDriveCritical.Name = "numDriveCritical";
            this.numDriveCritical.Size = new System.Drawing.Size(160, 22);
            this.numDriveCritical.TabIndex = 23;
            this.numDriveCritical.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // bttnAddDriveThreshold
            // 
            this.bttnAddDriveThreshold.Location = new System.Drawing.Point(513, 201);
            this.bttnAddDriveThreshold.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.bttnAddDriveThreshold.Name = "bttnAddDriveThreshold";
            this.bttnAddDriveThreshold.Size = new System.Drawing.Size(144, 28);
            this.bttnAddDriveThreshold.TabIndex = 21;
            this.bttnAddDriveThreshold.Text = "Add/Update";
            this.bttnAddDriveThreshold.UseVisualStyleBackColor = true;
            this.bttnAddDriveThreshold.Click += new System.EventHandler(this.bttnAddDriveThreshold_Click);
            // 
            // dgvDriveThresholds
            // 
            this.dgvDriveThresholds.AllowUserToAddRows = false;
            this.dgvDriveThresholds.AllowUserToDeleteRows = false;
            this.dgvDriveThresholds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDriveThresholds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDriveThresholds.Location = new System.Drawing.Point(8, 265);
            this.dgvDriveThresholds.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvDriveThresholds.Name = "dgvDriveThresholds";
            this.dgvDriveThresholds.ReadOnly = true;
            this.dgvDriveThresholds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDriveThresholds.Size = new System.Drawing.Size(1171, 378);
            this.dgvDriveThresholds.TabIndex = 16;
            this.dgvDriveThresholds.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDriveThresholds_CellContentClick);
            this.dgvDriveThresholds.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvDriveThresholds_CellFormatting);
            this.dgvDriveThresholds.CurrentCellChanged += new System.EventHandler(this.dgvDriveThresholds_CurrentCellChaned);
            // 
            // OptDisabled
            // 
            this.OptDisabled.AutoSize = true;
            this.OptDisabled.Location = new System.Drawing.Point(272, 114);
            this.OptDisabled.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.OptDisabled.Name = "OptDisabled";
            this.OptDisabled.Size = new System.Drawing.Size(84, 21);
            this.OptDisabled.TabIndex = 14;
            this.OptDisabled.Text = "Disabled";
            this.OptDisabled.UseVisualStyleBackColor = true;
            this.OptDisabled.CheckedChanged += new System.EventHandler(this.OptDisabled_CheckedChanged);
            // 
            // optGB
            // 
            this.optGB.AutoSize = true;
            this.optGB.Location = new System.Drawing.Point(201, 114);
            this.optGB.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.optGB.Name = "optGB";
            this.optGB.Size = new System.Drawing.Size(49, 21);
            this.optGB.TabIndex = 12;
            this.optGB.Text = "GB";
            this.optGB.UseVisualStyleBackColor = true;
            // 
            // optPercent
            // 
            this.optPercent.AutoSize = true;
            this.optPercent.Checked = true;
            this.optPercent.Location = new System.Drawing.Point(96, 114);
            this.optPercent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.optPercent.Name = "optPercent";
            this.optPercent.Size = new System.Drawing.Size(94, 21);
            this.optPercent.TabIndex = 11;
            this.optPercent.TabStop = true;
            this.optPercent.Text = "Percent %";
            this.optPercent.UseVisualStyleBackColor = true;
            this.optPercent.CheckedChanged += new System.EventHandler(this.optPercent_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 73);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "Drive:";
            // 
            // cboDrives
            // 
            this.cboDrives.DisplayMember = "Instance";
            this.cboDrives.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDrives.FormattingEnabled = true;
            this.cboDrives.Location = new System.Drawing.Point(96, 69);
            this.cboDrives.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cboDrives.Name = "cboDrives";
            this.cboDrives.Size = new System.Drawing.Size(363, 24);
            this.cboDrives.TabIndex = 5;
            this.cboDrives.ValueMember = "InstanceID";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 39);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Instance:";
            // 
            // cboDrivesInstances
            // 
            this.cboDrivesInstances.DisplayMember = "Instance";
            this.cboDrivesInstances.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDrivesInstances.FormattingEnabled = true;
            this.cboDrivesInstances.Location = new System.Drawing.Point(96, 36);
            this.cboDrivesInstances.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cboDrivesInstances.Name = "cboDrivesInstances";
            this.cboDrivesInstances.Size = new System.Drawing.Size(363, 24);
            this.cboDrivesInstances.TabIndex = 3;
            this.cboDrivesInstances.ValueMember = "InstanceID";
            this.cboDrivesInstances.SelectedIndexChanged += new System.EventHandler(this.cboDrivesInstances_SelectedIndexChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.chkUseFG);
            this.tabPage3.Controls.Add(this.chkUsePartial);
            this.tabPage3.Controls.Add(this.chkBackupInherit);
            this.tabPage3.Controls.Add(this.pnlBackupThresholds);
            this.tabPage3.Controls.Add(this.bttnUpdateBackup);
            this.tabPage3.Controls.Add(this.dgvBackup);
            this.tabPage3.Controls.Add(this.label11);
            this.tabPage3.Controls.Add(this.cboBackupDatabase);
            this.tabPage3.Controls.Add(this.label12);
            this.tabPage3.Controls.Add(this.cboBackupInstance);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(1187, 651);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Backup";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // pnlBackupThresholds
            // 
            this.pnlBackupThresholds.Controls.Add(this.label15);
            this.pnlBackupThresholds.Controls.Add(this.label16);
            this.pnlBackupThresholds.Controls.Add(this.numLogWarning);
            this.pnlBackupThresholds.Controls.Add(this.numLogCritical);
            this.pnlBackupThresholds.Controls.Add(this.label13);
            this.pnlBackupThresholds.Controls.Add(this.label14);
            this.pnlBackupThresholds.Controls.Add(this.numDiffWarning);
            this.pnlBackupThresholds.Controls.Add(this.numDiffCritical);
            this.pnlBackupThresholds.Controls.Add(this.label10);
            this.pnlBackupThresholds.Controls.Add(this.label8);
            this.pnlBackupThresholds.Controls.Add(this.chkLog);
            this.pnlBackupThresholds.Controls.Add(this.chkDiff);
            this.pnlBackupThresholds.Controls.Add(this.chkFull);
            this.pnlBackupThresholds.Controls.Add(this.label7);
            this.pnlBackupThresholds.Controls.Add(this.label9);
            this.pnlBackupThresholds.Controls.Add(this.numFullWarning);
            this.pnlBackupThresholds.Controls.Add(this.numFullCritical);
            this.pnlBackupThresholds.Location = new System.Drawing.Point(95, 122);
            this.pnlBackupThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlBackupThresholds.Name = "pnlBackupThresholds";
            this.pnlBackupThresholds.Size = new System.Drawing.Size(509, 141);
            this.pnlBackupThresholds.TabIndex = 37;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(295, 9);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 17);
            this.label7.TabIndex = 20;
            this.label7.Text = "Critical:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(119, 10);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 17);
            this.label9.TabIndex = 19;
            this.label9.Text = "Warning:";
            // 
            // numFullWarning
            // 
            this.numFullWarning.Location = new System.Drawing.Point(122, 40);
            this.numFullWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numFullWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numFullWarning.Name = "numFullWarning";
            this.numFullWarning.Size = new System.Drawing.Size(100, 22);
            this.numFullWarning.TabIndex = 22;
            this.numFullWarning.Value = new decimal(new int[] {
            10080,
            0,
            0,
            0});
            // 
            // numFullCritical
            // 
            this.numFullCritical.Location = new System.Drawing.Point(296, 40);
            this.numFullCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numFullCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numFullCritical.Name = "numFullCritical";
            this.numFullCritical.Size = new System.Drawing.Size(100, 22);
            this.numFullCritical.TabIndex = 23;
            this.numFullCritical.Value = new decimal(new int[] {
            14400,
            0,
            0,
            0});
            // 
            // bttnUpdateBackup
            // 
            this.bttnUpdateBackup.Location = new System.Drawing.Point(631, 205);
            this.bttnUpdateBackup.Margin = new System.Windows.Forms.Padding(4);
            this.bttnUpdateBackup.Name = "bttnUpdateBackup";
            this.bttnUpdateBackup.Size = new System.Drawing.Size(144, 28);
            this.bttnUpdateBackup.TabIndex = 36;
            this.bttnUpdateBackup.Text = "Add/Update";
            this.bttnUpdateBackup.UseVisualStyleBackColor = true;
            this.bttnUpdateBackup.Click += new System.EventHandler(this.bttnUpdateBackup_Click);
            // 
            // dgvBackup
            // 
            this.dgvBackup.AllowUserToAddRows = false;
            this.dgvBackup.AllowUserToDeleteRows = false;
            this.dgvBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvBackup.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBackup.Location = new System.Drawing.Point(7, 271);
            this.dgvBackup.Margin = new System.Windows.Forms.Padding(4);
            this.dgvBackup.Name = "dgvBackup";
            this.dgvBackup.ReadOnly = true;
            this.dgvBackup.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBackup.Size = new System.Drawing.Size(1173, 373);
            this.dgvBackup.TabIndex = 35;
            this.dgvBackup.CurrentCellChanged += new System.EventHandler(this.dgvBackup_CurrentCellChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(30, 64);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(73, 17);
            this.label11.TabIndex = 31;
            this.label11.Text = "Database:";
            // 
            // cboBackupDatabase
            // 
            this.cboBackupDatabase.DisplayMember = "Instance";
            this.cboBackupDatabase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBackupDatabase.FormattingEnabled = true;
            this.cboBackupDatabase.Location = new System.Drawing.Point(106, 60);
            this.cboBackupDatabase.Margin = new System.Windows.Forms.Padding(4);
            this.cboBackupDatabase.Name = "cboBackupDatabase";
            this.cboBackupDatabase.Size = new System.Drawing.Size(363, 24);
            this.cboBackupDatabase.TabIndex = 30;
            this.cboBackupDatabase.ValueMember = "InstanceID";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(30, 30);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 17);
            this.label12.TabIndex = 29;
            this.label12.Text = "Instance:";
            // 
            // cboBackupInstance
            // 
            this.cboBackupInstance.DisplayMember = "Instance";
            this.cboBackupInstance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBackupInstance.FormattingEnabled = true;
            this.cboBackupInstance.Location = new System.Drawing.Point(106, 27);
            this.cboBackupInstance.Margin = new System.Windows.Forms.Padding(4);
            this.cboBackupInstance.Name = "cboBackupInstance";
            this.cboBackupInstance.Size = new System.Drawing.Size(363, 24);
            this.cboBackupInstance.TabIndex = 28;
            this.cboBackupInstance.ValueMember = "InstanceID";
            this.cboBackupInstance.SelectedIndexChanged += new System.EventHandler(this.cboBackupInstance_SelectedIndexChanged);
            // 
            // chkFull
            // 
            this.chkFull.AutoSize = true;
            this.chkFull.Checked = true;
            this.chkFull.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFull.Location = new System.Drawing.Point(13, 41);
            this.chkFull.Name = "chkFull";
            this.chkFull.Size = new System.Drawing.Size(52, 21);
            this.chkFull.TabIndex = 41;
            this.chkFull.Text = "Full";
            this.chkFull.UseVisualStyleBackColor = true;
            this.chkFull.CheckedChanged += new System.EventHandler(this.chkFull_CheckedChanged);
            // 
            // chkDiff
            // 
            this.chkDiff.AutoSize = true;
            this.chkDiff.Location = new System.Drawing.Point(13, 71);
            this.chkDiff.Name = "chkDiff";
            this.chkDiff.Size = new System.Drawing.Size(51, 21);
            this.chkDiff.TabIndex = 42;
            this.chkDiff.Text = "Diff";
            this.chkDiff.UseVisualStyleBackColor = true;
            this.chkDiff.CheckedChanged += new System.EventHandler(this.chkDiff_CheckedChanged);
            // 
            // chkLog
            // 
            this.chkLog.AutoSize = true;
            this.chkLog.Checked = true;
            this.chkLog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLog.Location = new System.Drawing.Point(14, 101);
            this.chkLog.Name = "chkLog";
            this.chkLog.Size = new System.Drawing.Size(54, 21);
            this.chkLog.TabIndex = 43;
            this.chkLog.Text = "Log";
            this.chkLog.UseVisualStyleBackColor = true;
            this.chkLog.CheckedChanged += new System.EventHandler(this.chkLog_CheckedChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(404, 41);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 17);
            this.label8.TabIndex = 44;
            this.label8.Text = "mins";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(229, 41);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 17);
            this.label10.TabIndex = 45;
            this.label10.Text = "mins";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(229, 71);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(37, 17);
            this.label13.TabIndex = 49;
            this.label13.Text = "mins";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(404, 71);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(37, 17);
            this.label14.TabIndex = 48;
            this.label14.Text = "mins";
            // 
            // numDiffWarning
            // 
            this.numDiffWarning.Enabled = false;
            this.numDiffWarning.Location = new System.Drawing.Point(122, 70);
            this.numDiffWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numDiffWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numDiffWarning.Name = "numDiffWarning";
            this.numDiffWarning.Size = new System.Drawing.Size(100, 22);
            this.numDiffWarning.TabIndex = 46;
            this.numDiffWarning.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // numDiffCritical
            // 
            this.numDiffCritical.Enabled = false;
            this.numDiffCritical.Location = new System.Drawing.Point(296, 70);
            this.numDiffCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numDiffCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numDiffCritical.Name = "numDiffCritical";
            this.numDiffCritical.Size = new System.Drawing.Size(100, 22);
            this.numDiffCritical.TabIndex = 47;
            this.numDiffCritical.Value = new decimal(new int[] {
            2880,
            0,
            0,
            0});
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(229, 101);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(37, 17);
            this.label15.TabIndex = 53;
            this.label15.Text = "mins";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(404, 101);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(37, 17);
            this.label16.TabIndex = 52;
            this.label16.Text = "mins";
            // 
            // numLogWarning
            // 
            this.numLogWarning.Location = new System.Drawing.Point(122, 100);
            this.numLogWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numLogWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLogWarning.Name = "numLogWarning";
            this.numLogWarning.Size = new System.Drawing.Size(100, 22);
            this.numLogWarning.TabIndex = 50;
            this.numLogWarning.Value = new decimal(new int[] {
            720,
            0,
            0,
            0});
            // 
            // numLogCritical
            // 
            this.numLogCritical.Location = new System.Drawing.Point(296, 100);
            this.numLogCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numLogCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLogCritical.Name = "numLogCritical";
            this.numLogCritical.Size = new System.Drawing.Size(100, 22);
            this.numLogCritical.TabIndex = 51;
            this.numLogCritical.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // chkBackupInherit
            // 
            this.chkBackupInherit.AutoSize = true;
            this.chkBackupInherit.Location = new System.Drawing.Point(106, 94);
            this.chkBackupInherit.Name = "chkBackupInherit";
            this.chkBackupInherit.Size = new System.Drawing.Size(69, 21);
            this.chkBackupInherit.TabIndex = 38;
            this.chkBackupInherit.Text = "Inherit";
            this.chkBackupInherit.UseVisualStyleBackColor = true;
            this.chkBackupInherit.CheckedChanged += new System.EventHandler(this.chkBackupInherit_CheckedChanged);
            // 
            // chkUsePartial
            // 
            this.chkUsePartial.AutoSize = true;
            this.chkUsePartial.Location = new System.Drawing.Point(191, 94);
            this.chkUsePartial.Name = "chkUsePartial";
            this.chkUsePartial.Size = new System.Drawing.Size(157, 21);
            this.chkUsePartial.TabIndex = 39;
            this.chkUsePartial.Text = "Use Partial Backups";
            this.chkUsePartial.UseVisualStyleBackColor = true;
            // 
            // chkUseFG
            // 
            this.chkUseFG.AutoSize = true;
            this.chkUseFG.Location = new System.Drawing.Point(360, 94);
            this.chkUseFG.Name = "chkUseFG";
            this.chkUseFG.Size = new System.Drawing.Size(176, 21);
            this.chkUseFG.TabIndex = 40;
            this.chkUseFG.Text = "Use Filegroup Backups";
            this.chkUseFG.UseVisualStyleBackColor = true;
            // 
            // DBAChecksConfigTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1224, 737);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "DBAChecksConfigTool";
            this.Text = "DBAChecks Config Tool";
            this.Load += new System.EventHandler(this.DBAChecksConfigTool_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDriveThresholds)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.pnlBackupThresholds.ResumeLayout(false);
            this.pnlBackupThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFullWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBackup)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogCritical)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ComboBox cboInstances;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.CheckedListBox chkTags;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttAddTag;
        private System.Windows.Forms.TextBox txtTag;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton OptDisabled;
        private System.Windows.Forms.RadioButton optGB;
        private System.Windows.Forms.RadioButton optPercent;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboDrives;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboDrivesInstances;
        private System.Windows.Forms.DataGridView dgvDriveThresholds;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numDriveWarning;
        private System.Windows.Forms.Button bttnAddDriveThreshold;
        private System.Windows.Forms.NumericUpDown numDriveCritical;
        private System.Windows.Forms.Label lblDriveCritical;
        private System.Windows.Forms.Label lblDriveWarning;
        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.RadioButton optInherit;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.CheckBox chkUseFG;
        private System.Windows.Forms.CheckBox chkUsePartial;
        private System.Windows.Forms.CheckBox chkBackupInherit;
        private System.Windows.Forms.Panel pnlBackupThresholds;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.NumericUpDown numLogWarning;
        private System.Windows.Forms.NumericUpDown numLogCritical;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.NumericUpDown numDiffWarning;
        private System.Windows.Forms.NumericUpDown numDiffCritical;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkLog;
        private System.Windows.Forms.CheckBox chkDiff;
        private System.Windows.Forms.CheckBox chkFull;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numFullWarning;
        private System.Windows.Forms.NumericUpDown numFullCritical;
        private System.Windows.Forms.Button bttnUpdateBackup;
        private System.Windows.Forms.DataGridView dgvBackup;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cboBackupDatabase;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cboBackupInstance;
    }
}