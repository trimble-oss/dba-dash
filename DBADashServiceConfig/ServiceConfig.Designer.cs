namespace DBADashServiceConfig
{
    partial class ServiceConfig
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceConfig));
            this.txtJson = new System.Windows.Forms.TextBox();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSecretKey = new System.Windows.Forms.TextBox();
            this.txtAccessKey = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAWSProfile = new System.Windows.Forms.TextBox();
            this.bttnSave = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.bttnStart = new System.Windows.Forms.Button();
            this.bttnStop = new System.Windows.Forms.Button();
            this.bttnRefresh = new System.Windows.Forms.Button();
            this.bttnInstall = new System.Windows.Forms.Button();
            this.bttnUninstall = new System.Windows.Forms.Button();
            this.cboServiceCredentials = new System.Windows.Forms.ComboBox();
            this.lblServiceCredentials = new System.Windows.Forms.Label();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.tab1 = new System.Windows.Forms.TabControl();
            this.tabDest = new System.Windows.Forms.TabPage();
            this.lnkSourceConnections = new System.Windows.Forms.LinkLabel();
            this.lnkServiceStatus = new System.Windows.Forms.LinkLabel();
            this.bttnS3 = new System.Windows.Forms.Button();
            this.bttnDestFolder = new System.Windows.Forms.Button();
            this.chkAutoUpgradeRepoDB = new System.Windows.Forms.CheckBox();
            this.bttnConnect = new System.Windows.Forms.Button();
            this.lblVersionInfo = new System.Windows.Forms.Label();
            this.bttnDeployDatabase = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtDestination = new System.Windows.Forms.TextBox();
            this.tabSource = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tabSrcOptions = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.bttnS3Src = new System.Windows.Forms.Button();
            this.bttnSrcFolder = new System.Windows.Forms.Button();
            this.bttnConnectSource = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabExtendedEvents = new System.Windows.Forms.TabPage();
            this.pnlExtendedEvents = new System.Windows.Forms.Panel();
            this.chkSlowQueryThreshold = new System.Windows.Forms.CheckBox();
            this.chkDualSession = new System.Windows.Forms.CheckBox();
            this.numSlowQueryThreshold = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.lblSlow = new System.Windows.Forms.Label();
            this.chkPersistXESession = new System.Windows.Forms.CheckBox();
            this.tabAddConnectionOther = new System.Windows.Forms.TabPage();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.lnkExample = new System.Windows.Forms.LinkLabel();
            this.lnkNone = new System.Windows.Forms.LinkLabel();
            this.lnkALL = new System.Windows.Forms.LinkLabel();
            this.label8 = new System.Windows.Forms.Label();
            this.txtSnapshotDBs = new System.Windows.Forms.TextBox();
            this.chkCollectPlans = new System.Windows.Forms.CheckBox();
            this.grpRunningQueryThreshold = new System.Windows.Forms.GroupBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtDurationThreshold = new System.Windows.Forms.TextBox();
            this.txtCountThreshold = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.txtCPUThreshold = new System.Windows.Forms.TextBox();
            this.txtGrantThreshold = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.chkNoWMI = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvConnections = new System.Windows.Forms.DataGridView();
            this.label13 = new System.Windows.Forms.Label();
            this.lblSourceConnections = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.tabService = new System.Windows.Forms.TabPage();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lnkPermissions = new System.Windows.Forms.LinkLabel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabOther = new System.Windows.Forms.TabPage();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.chkScanAzureDB = new System.Windows.Forms.CheckBox();
            this.lblHHmm = new System.Windows.Forms.Label();
            this.bttnScanNow = new System.Windows.Forms.Button();
            this.numAzureScanInterval = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.chkScanEvery = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.bttnSchedule = new System.Windows.Forms.Button();
            this.chkLogInternalPerfCounters = new System.Windows.Forms.CheckBox();
            this.tabJson = new System.Windows.Forms.TabPage();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.tab1.SuspendLayout();
            this.tabDest.SuspendLayout();
            this.tabSource.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabSrcOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.tabExtendedEvents.SuspendLayout();
            this.pnlExtendedEvents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).BeginInit();
            this.tabAddConnectionOther.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.grpRunningQueryThreshold.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnections)).BeginInit();
            this.tabService.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabOther.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.tabJson.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtJson
            // 
            this.txtJson.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtJson.Location = new System.Drawing.Point(3, 3);
            this.txtJson.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtJson.Multiline = true;
            this.txtJson.Name = "txtJson";
            this.txtJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtJson.Size = new System.Drawing.Size(1090, 553);
            this.txtJson.TabIndex = 13;
            this.txtJson.TextChanged += new System.EventHandler(this.txtJson_TextChanged);
            this.txtJson.Validating += new System.ComponentModel.CancelEventHandler(this.txtJson_Validating);
            // 
            // bttnAdd
            // 
            this.bttnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnAdd.Location = new System.Drawing.Point(948, 203);
            this.bttnAdd.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(103, 30);
            this.bttnAdd.TabIndex = 8;
            this.bttnAdd.Text = "Add/Update";
            this.bttnAdd.UseVisualStyleBackColor = true;
            this.bttnAdd.Click += new System.EventHandler(this.bttnAdd_Click);
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 82);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(148, 17);
            this.label6.TabIndex = 14;
            this.label6.Text = "Secret Key (Optional):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 54);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(152, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "Access Key (Optional):";
            // 
            // txtSecretKey
            // 
            this.txtSecretKey.Location = new System.Drawing.Point(175, 79);
            this.txtSecretKey.Name = "txtSecretKey";
            this.txtSecretKey.Size = new System.Drawing.Size(461, 22);
            this.txtSecretKey.TabIndex = 5;
            this.txtSecretKey.TextChanged += new System.EventHandler(this.txtSecretKey_TextChanged);
            this.txtSecretKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtSecretKey_Validating);
            // 
            // txtAccessKey
            // 
            this.txtAccessKey.Location = new System.Drawing.Point(175, 51);
            this.txtAccessKey.Name = "txtAccessKey";
            this.txtAccessKey.Size = new System.Drawing.Size(461, 22);
            this.txtAccessKey.TabIndex = 4;
            this.txtAccessKey.TextChanged += new System.EventHandler(this.txtAccessKey_TextChanged);
            this.txtAccessKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtAccessKey_Validating);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(154, 17);
            this.label4.TabIndex = 8;
            this.label4.Text = "AWS Profile (Optional):";
            // 
            // txtAWSProfile
            // 
            this.txtAWSProfile.Location = new System.Drawing.Point(175, 24);
            this.txtAWSProfile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtAWSProfile.Name = "txtAWSProfile";
            this.txtAWSProfile.Size = new System.Drawing.Size(461, 22);
            this.txtAWSProfile.TabIndex = 3;
            this.txtAWSProfile.TextChanged += new System.EventHandler(this.txtAWSProfile_TextChanged);
            this.txtAWSProfile.Validating += new System.ComponentModel.CancelEventHandler(this.txtAWSProfile_Validating);
            // 
            // bttnSave
            // 
            this.bttnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bttnSave.Location = new System.Drawing.Point(900, 620);
            this.bttnSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnSave.Name = "bttnSave";
            this.bttnSave.Size = new System.Drawing.Size(100, 28);
            this.bttnSave.TabIndex = 14;
            this.bttnSave.Text = "&Save";
            this.bttnSave.UseVisualStyleBackColor = true;
            this.bttnSave.Click += new System.EventHandler(this.bttnSave_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(37, 299);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Json:";
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Location = new System.Drawing.Point(6, 15);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(103, 17);
            this.lblServiceStatus.TabIndex = 14;
            this.lblServiceStatus.Text = "Service Status:";
            // 
            // bttnStart
            // 
            this.bttnStart.Location = new System.Drawing.Point(562, 12);
            this.bttnStart.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnStart.Name = "bttnStart";
            this.bttnStart.Size = new System.Drawing.Size(75, 23);
            this.bttnStart.TabIndex = 2;
            this.bttnStart.Text = "Start";
            this.bttnStart.UseVisualStyleBackColor = true;
            this.bttnStart.Click += new System.EventHandler(this.bttnStart_Click);
            // 
            // bttnStop
            // 
            this.bttnStop.Location = new System.Drawing.Point(562, 39);
            this.bttnStop.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnStop.Name = "bttnStop";
            this.bttnStop.Size = new System.Drawing.Size(75, 23);
            this.bttnStop.TabIndex = 3;
            this.bttnStop.Text = "Stop";
            this.bttnStop.UseVisualStyleBackColor = true;
            this.bttnStop.Click += new System.EventHandler(this.bttnStop_Click);
            // 
            // bttnRefresh
            // 
            this.bttnRefresh.Location = new System.Drawing.Point(481, 12);
            this.bttnRefresh.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnRefresh.Name = "bttnRefresh";
            this.bttnRefresh.Size = new System.Drawing.Size(75, 23);
            this.bttnRefresh.TabIndex = 1;
            this.bttnRefresh.Text = "Refresh";
            this.bttnRefresh.UseVisualStyleBackColor = true;
            this.bttnRefresh.Click += new System.EventHandler(this.bttnRefresh_Click);
            // 
            // bttnInstall
            // 
            this.bttnInstall.Location = new System.Drawing.Point(297, 38);
            this.bttnInstall.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnInstall.Name = "bttnInstall";
            this.bttnInstall.Size = new System.Drawing.Size(75, 23);
            this.bttnInstall.TabIndex = 5;
            this.bttnInstall.Text = "Install";
            this.bttnInstall.UseVisualStyleBackColor = true;
            this.bttnInstall.Click += new System.EventHandler(this.bttnInstall_Click);
            // 
            // bttnUninstall
            // 
            this.bttnUninstall.Location = new System.Drawing.Point(297, 65);
            this.bttnUninstall.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnUninstall.Name = "bttnUninstall";
            this.bttnUninstall.Size = new System.Drawing.Size(75, 23);
            this.bttnUninstall.TabIndex = 6;
            this.bttnUninstall.Text = "Uninstall";
            this.bttnUninstall.UseVisualStyleBackColor = true;
            this.bttnUninstall.Click += new System.EventHandler(this.bttnUninstall_Click);
            // 
            // cboServiceCredentials
            // 
            this.cboServiceCredentials.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboServiceCredentials.FormattingEnabled = true;
            this.cboServiceCredentials.Items.AddRange(new object[] {
            "LocalSystem",
            "LocalService",
            "NetworkService",
            "User (prompt)"});
            this.cboServiceCredentials.Location = new System.Drawing.Point(116, 38);
            this.cboServiceCredentials.Margin = new System.Windows.Forms.Padding(4);
            this.cboServiceCredentials.Name = "cboServiceCredentials";
            this.cboServiceCredentials.Size = new System.Drawing.Size(160, 24);
            this.cboServiceCredentials.TabIndex = 4;
            this.cboServiceCredentials.UseWaitCursor = true;
            // 
            // lblServiceCredentials
            // 
            this.lblServiceCredentials.AutoSize = true;
            this.lblServiceCredentials.Location = new System.Drawing.Point(7, 45);
            this.lblServiceCredentials.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServiceCredentials.Name = "lblServiceCredentials";
            this.lblServiceCredentials.Size = new System.Drawing.Size(58, 17);
            this.lblServiceCredentials.TabIndex = 22;
            this.lblServiceCredentials.Text = "Run As:";
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.Location = new System.Drawing.Point(1006, 620);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(100, 28);
            this.bttnCancel.TabIndex = 15;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            // 
            // tab1
            // 
            this.tab1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tab1.Controls.Add(this.tabDest);
            this.tab1.Controls.Add(this.tabSource);
            this.tab1.Controls.Add(this.tabService);
            this.tab1.Controls.Add(this.tabOther);
            this.tab1.Controls.Add(this.tabJson);
            this.tab1.Location = new System.Drawing.Point(12, 25);
            this.tab1.Name = "tab1";
            this.tab1.SelectedIndex = 0;
            this.tab1.Size = new System.Drawing.Size(1104, 588);
            this.tab1.TabIndex = 22;
            // 
            // tabDest
            // 
            this.tabDest.Controls.Add(this.lnkSourceConnections);
            this.tabDest.Controls.Add(this.lnkServiceStatus);
            this.tabDest.Controls.Add(this.bttnS3);
            this.tabDest.Controls.Add(this.bttnDestFolder);
            this.tabDest.Controls.Add(this.chkAutoUpgradeRepoDB);
            this.tabDest.Controls.Add(this.bttnConnect);
            this.tabDest.Controls.Add(this.lblVersionInfo);
            this.tabDest.Controls.Add(this.bttnDeployDatabase);
            this.tabDest.Controls.Add(this.label7);
            this.tabDest.Controls.Add(this.txtDestination);
            this.tabDest.Location = new System.Drawing.Point(4, 25);
            this.tabDest.Name = "tabDest";
            this.tabDest.Padding = new System.Windows.Forms.Padding(3);
            this.tabDest.Size = new System.Drawing.Size(1096, 559);
            this.tabDest.TabIndex = 2;
            this.tabDest.Text = "Destination:";
            this.tabDest.UseVisualStyleBackColor = true;
            // 
            // lnkSourceConnections
            // 
            this.lnkSourceConnections.AutoSize = true;
            this.lnkSourceConnections.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lnkSourceConnections.LinkColor = System.Drawing.Color.Black;
            this.lnkSourceConnections.Location = new System.Drawing.Point(6, 190);
            this.lnkSourceConnections.Name = "lnkSourceConnections";
            this.lnkSourceConnections.Size = new System.Drawing.Size(140, 16);
            this.lnkSourceConnections.TabIndex = 18;
            this.lnkSourceConnections.TabStop = true;
            this.lnkSourceConnections.Text = "Source Connections:";
            this.lnkSourceConnections.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSourceConnections_LinkClicked);
            // 
            // lnkServiceStatus
            // 
            this.lnkServiceStatus.AutoSize = true;
            this.lnkServiceStatus.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lnkServiceStatus.Location = new System.Drawing.Point(6, 161);
            this.lnkServiceStatus.Name = "lnkServiceStatus";
            this.lnkServiceStatus.Size = new System.Drawing.Size(103, 16);
            this.lnkServiceStatus.TabIndex = 16;
            this.lnkServiceStatus.TabStop = true;
            this.lnkServiceStatus.Text = "Service Status:";
            this.lnkServiceStatus.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkServiceStatus_LinkClicked);
            // 
            // bttnS3
            // 
            this.bttnS3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnS3.Image = global::DBADashServiceConfig.Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            this.bttnS3.Location = new System.Drawing.Point(1039, 19);
            this.bttnS3.Name = "bttnS3";
            this.bttnS3.Size = new System.Drawing.Size(28, 24);
            this.bttnS3.TabIndex = 10;
            this.toolTip1.SetToolTip(this.bttnS3, "Choose a S3 bucket destination");
            this.bttnS3.UseVisualStyleBackColor = true;
            this.bttnS3.Click += new System.EventHandler(this.bttnS3_Click);
            // 
            // bttnDestFolder
            // 
            this.bttnDestFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnDestFolder.Image = global::DBADashServiceConfig.Properties.Resources.FolderOpened_16x;
            this.bttnDestFolder.Location = new System.Drawing.Point(1005, 20);
            this.bttnDestFolder.Name = "bttnDestFolder";
            this.bttnDestFolder.Size = new System.Drawing.Size(28, 23);
            this.bttnDestFolder.TabIndex = 9;
            this.toolTip1.SetToolTip(this.bttnDestFolder, "Choose a folder destination path");
            this.bttnDestFolder.UseVisualStyleBackColor = true;
            this.bttnDestFolder.Click += new System.EventHandler(this.bttnDestFolder_Click);
            // 
            // chkAutoUpgradeRepoDB
            // 
            this.chkAutoUpgradeRepoDB.AutoSize = true;
            this.chkAutoUpgradeRepoDB.Checked = true;
            this.chkAutoUpgradeRepoDB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoUpgradeRepoDB.Location = new System.Drawing.Point(103, 48);
            this.chkAutoUpgradeRepoDB.Name = "chkAutoUpgradeRepoDB";
            this.chkAutoUpgradeRepoDB.Size = new System.Drawing.Size(307, 21);
            this.chkAutoUpgradeRepoDB.TabIndex = 8;
            this.chkAutoUpgradeRepoDB.Text = "Auto upgrade repository DB on service start";
            this.chkAutoUpgradeRepoDB.UseVisualStyleBackColor = true;
            this.chkAutoUpgradeRepoDB.CheckedChanged += new System.EventHandler(this.chkAutoUpgradeRepoDB_CheckedChanged);
            // 
            // bttnConnect
            // 
            this.bttnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnConnect.Image = global::DBADashServiceConfig.Properties.Resources.Connect_16x;
            this.bttnConnect.Location = new System.Drawing.Point(971, 20);
            this.bttnConnect.Name = "bttnConnect";
            this.bttnConnect.Size = new System.Drawing.Size(28, 23);
            this.bttnConnect.TabIndex = 7;
            this.toolTip1.SetToolTip(this.bttnConnect, "Connect to a SQL Instance that will store your DBA Dash repository database");
            this.bttnConnect.UseVisualStyleBackColor = true;
            this.bttnConnect.Click += new System.EventHandler(this.bttnConnect_Click);
            // 
            // lblVersionInfo
            // 
            this.lblVersionInfo.AutoSize = true;
            this.lblVersionInfo.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVersionInfo.Location = new System.Drawing.Point(6, 131);
            this.lblVersionInfo.Name = "lblVersionInfo";
            this.lblVersionInfo.Size = new System.Drawing.Size(93, 16);
            this.lblVersionInfo.TabIndex = 6;
            this.lblVersionInfo.Text = "version info...";
            // 
            // bttnDeployDatabase
            // 
            this.bttnDeployDatabase.Location = new System.Drawing.Point(103, 75);
            this.bttnDeployDatabase.Name = "bttnDeployDatabase";
            this.bttnDeployDatabase.Size = new System.Drawing.Size(214, 32);
            this.bttnDeployDatabase.TabIndex = 5;
            this.bttnDeployDatabase.Text = "Deploy/Update Database";
            this.toolTip1.SetToolTip(this.bttnDeployDatabase, "Click to create/upgrade your DBA Dash repository database");
            this.bttnDeployDatabase.UseVisualStyleBackColor = true;
            this.bttnDeployDatabase.Click += new System.EventHandler(this.bttnDeployDatabase_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(83, 17);
            this.label7.TabIndex = 3;
            this.label7.Text = "Destination:";
            // 
            // txtDestination
            // 
            this.txtDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestination.Location = new System.Drawing.Point(103, 20);
            this.txtDestination.Name = "txtDestination";
            this.txtDestination.Size = new System.Drawing.Size(862, 22);
            this.txtDestination.TabIndex = 1;
            this.txtDestination.Validated += new System.EventHandler(this.txtDestination_Validated);
            // 
            // tabSource
            // 
            this.tabSource.Controls.Add(this.groupBox2);
            this.tabSource.Controls.Add(this.groupBox1);
            this.tabSource.Location = new System.Drawing.Point(4, 25);
            this.tabSource.Name = "tabSource";
            this.tabSource.Padding = new System.Windows.Forms.Padding(3);
            this.tabSource.Size = new System.Drawing.Size(1096, 559);
            this.tabSource.TabIndex = 0;
            this.tabSource.Text = "Source";
            this.tabSource.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tabSrcOptions);
            this.groupBox2.Controls.Add(this.bttnAdd);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(10, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1069, 249);
            this.groupBox2.TabIndex = 27;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Add New Connection";
            // 
            // tabSrcOptions
            // 
            this.tabSrcOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabSrcOptions.Controls.Add(this.tabGeneral);
            this.tabSrcOptions.Controls.Add(this.tabExtendedEvents);
            this.tabSrcOptions.Controls.Add(this.tabAddConnectionOther);
            this.tabSrcOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabSrcOptions.Location = new System.Drawing.Point(16, 21);
            this.tabSrcOptions.Name = "tabSrcOptions";
            this.tabSrcOptions.SelectedIndex = 0;
            this.tabSrcOptions.Size = new System.Drawing.Size(1035, 177);
            this.tabSrcOptions.TabIndex = 21;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.pictureBox2);
            this.tabGeneral.Controls.Add(this.txtSource);
            this.tabGeneral.Controls.Add(this.bttnS3Src);
            this.tabGeneral.Controls.Add(this.bttnSrcFolder);
            this.tabGeneral.Controls.Add(this.bttnConnectSource);
            this.tabGeneral.Controls.Add(this.label1);
            this.tabGeneral.Location = new System.Drawing.Point(4, 25);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(1027, 148);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::DBADashServiceConfig.Properties.Resources.Information_blue_6227_16x16_cyan;
            this.pictureBox2.Location = new System.Drawing.Point(6, 6);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 14;
            this.pictureBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox2, resources.GetString("pictureBox2.ToolTip"));
            // 
            // txtSource
            // 
            this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSource.Location = new System.Drawing.Point(6, 28);
            this.txtSource.Multiline = true;
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(912, 103);
            this.txtSource.TabIndex = 13;
            // 
            // bttnS3Src
            // 
            this.bttnS3Src.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnS3Src.Image = global::DBADashServiceConfig.Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            this.bttnS3Src.Location = new System.Drawing.Point(991, 29);
            this.bttnS3Src.Name = "bttnS3Src";
            this.bttnS3Src.Size = new System.Drawing.Size(28, 23);
            this.bttnS3Src.TabIndex = 12;
            this.toolTip1.SetToolTip(this.bttnS3Src, "Choose a S3 bucket source");
            this.bttnS3Src.UseVisualStyleBackColor = true;
            this.bttnS3Src.Click += new System.EventHandler(this.bttnS3Src_Click);
            // 
            // bttnSrcFolder
            // 
            this.bttnSrcFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSrcFolder.Image = global::DBADashServiceConfig.Properties.Resources.FolderOpened_16x;
            this.bttnSrcFolder.Location = new System.Drawing.Point(958, 28);
            this.bttnSrcFolder.Name = "bttnSrcFolder";
            this.bttnSrcFolder.Size = new System.Drawing.Size(28, 23);
            this.bttnSrcFolder.TabIndex = 10;
            this.toolTip1.SetToolTip(this.bttnSrcFolder, "Choose a folder source path");
            this.bttnSrcFolder.UseVisualStyleBackColor = true;
            this.bttnSrcFolder.Click += new System.EventHandler(this.bttnSrcFolder_Click_1);
            // 
            // bttnConnectSource
            // 
            this.bttnConnectSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnConnectSource.Image = global::DBADashServiceConfig.Properties.Resources.Connect_16x;
            this.bttnConnectSource.Location = new System.Drawing.Point(924, 28);
            this.bttnConnectSource.Name = "bttnConnectSource";
            this.bttnConnectSource.Size = new System.Drawing.Size(28, 23);
            this.bttnConnectSource.TabIndex = 8;
            this.toolTip1.SetToolTip(this.bttnConnectSource, "Connect to a SQL Instance to monitor with DBA Dash");
            this.bttnConnectSource.UseVisualStyleBackColor = true;
            this.bttnConnectSource.Click += new System.EventHandler(this.bttnConnectSource_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source: ";
            // 
            // tabExtendedEvents
            // 
            this.tabExtendedEvents.Controls.Add(this.pnlExtendedEvents);
            this.tabExtendedEvents.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabExtendedEvents.Location = new System.Drawing.Point(4, 25);
            this.tabExtendedEvents.Name = "tabExtendedEvents";
            this.tabExtendedEvents.Padding = new System.Windows.Forms.Padding(3);
            this.tabExtendedEvents.Size = new System.Drawing.Size(1027, 148);
            this.tabExtendedEvents.TabIndex = 1;
            this.tabExtendedEvents.Text = "Extended Events";
            this.tabExtendedEvents.UseVisualStyleBackColor = true;
            // 
            // pnlExtendedEvents
            // 
            this.pnlExtendedEvents.Controls.Add(this.chkSlowQueryThreshold);
            this.pnlExtendedEvents.Controls.Add(this.chkDualSession);
            this.pnlExtendedEvents.Controls.Add(this.numSlowQueryThreshold);
            this.pnlExtendedEvents.Controls.Add(this.label9);
            this.pnlExtendedEvents.Controls.Add(this.lblSlow);
            this.pnlExtendedEvents.Controls.Add(this.chkPersistXESession);
            this.pnlExtendedEvents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlExtendedEvents.Location = new System.Drawing.Point(3, 3);
            this.pnlExtendedEvents.Name = "pnlExtendedEvents";
            this.pnlExtendedEvents.Size = new System.Drawing.Size(1021, 142);
            this.pnlExtendedEvents.TabIndex = 18;
            // 
            // chkSlowQueryThreshold
            // 
            this.chkSlowQueryThreshold.AutoSize = true;
            this.chkSlowQueryThreshold.Location = new System.Drawing.Point(6, 3);
            this.chkSlowQueryThreshold.Name = "chkSlowQueryThreshold";
            this.chkSlowQueryThreshold.Size = new System.Drawing.Size(280, 21);
            this.chkSlowQueryThreshold.TabIndex = 13;
            this.chkSlowQueryThreshold.Text = "Capture Slow Queries (Extended Event)";
            this.chkSlowQueryThreshold.UseVisualStyleBackColor = true;
            this.chkSlowQueryThreshold.CheckedChanged += new System.EventHandler(this.chkSlowQueryThreshold_CheckedChanged);
            // 
            // chkDualSession
            // 
            this.chkDualSession.AutoSize = true;
            this.chkDualSession.Enabled = false;
            this.chkDualSession.Location = new System.Drawing.Point(6, 57);
            this.chkDualSession.Name = "chkDualSession";
            this.chkDualSession.Size = new System.Drawing.Size(138, 21);
            this.chkDualSession.TabIndex = 17;
            this.chkDualSession.Text = "Use dual session";
            this.toolTip1.SetToolTip(this.chkDualSession, "Uses overlapping event sessions to try to capture events that occur during the br" +
        "eif period where the session is stopped to flush the ring buffer.");
            this.chkDualSession.UseVisualStyleBackColor = true;
            // 
            // numSlowQueryThreshold
            // 
            this.numSlowQueryThreshold.Enabled = false;
            this.numSlowQueryThreshold.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numSlowQueryThreshold.Location = new System.Drawing.Point(117, 86);
            this.numSlowQueryThreshold.Maximum = new decimal(new int[] {
            604800000,
            0,
            0,
            0});
            this.numSlowQueryThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numSlowQueryThreshold.Name = "numSlowQueryThreshold";
            this.numSlowQueryThreshold.Size = new System.Drawing.Size(173, 22);
            this.numSlowQueryThreshold.TabIndex = 12;
            this.numSlowQueryThreshold.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 88);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(108, 17);
            this.label9.TabIndex = 16;
            this.label9.Text = "Threshold (ms):";
            // 
            // lblSlow
            // 
            this.lblSlow.AutoSize = true;
            this.lblSlow.Location = new System.Drawing.Point(6, 116);
            this.lblSlow.Name = "lblSlow";
            this.lblSlow.Size = new System.Drawing.Size(557, 17);
            this.lblSlow.TabIndex = 14;
            this.lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabl" +
    "ed";
            // 
            // chkPersistXESession
            // 
            this.chkPersistXESession.AutoSize = true;
            this.chkPersistXESession.Enabled = false;
            this.chkPersistXESession.Location = new System.Drawing.Point(6, 30);
            this.chkPersistXESession.Name = "chkPersistXESession";
            this.chkPersistXESession.Size = new System.Drawing.Size(372, 21);
            this.chkPersistXESession.TabIndex = 15;
            this.chkPersistXESession.Text = "Persist XE sessions (to allow for manual configuration)";
            this.chkPersistXESession.UseVisualStyleBackColor = true;
            // 
            // tabAddConnectionOther
            // 
            this.tabAddConnectionOther.Controls.Add(this.pictureBox3);
            this.tabAddConnectionOther.Controls.Add(this.lnkExample);
            this.tabAddConnectionOther.Controls.Add(this.lnkNone);
            this.tabAddConnectionOther.Controls.Add(this.lnkALL);
            this.tabAddConnectionOther.Controls.Add(this.label8);
            this.tabAddConnectionOther.Controls.Add(this.txtSnapshotDBs);
            this.tabAddConnectionOther.Controls.Add(this.chkCollectPlans);
            this.tabAddConnectionOther.Controls.Add(this.grpRunningQueryThreshold);
            this.tabAddConnectionOther.Controls.Add(this.chkNoWMI);
            this.tabAddConnectionOther.Location = new System.Drawing.Point(4, 25);
            this.tabAddConnectionOther.Name = "tabAddConnectionOther";
            this.tabAddConnectionOther.Padding = new System.Windows.Forms.Padding(3);
            this.tabAddConnectionOther.Size = new System.Drawing.Size(1027, 148);
            this.tabAddConnectionOther.TabIndex = 3;
            this.tabAddConnectionOther.Text = "Other";
            this.tabAddConnectionOther.UseVisualStyleBackColor = true;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = global::DBADashServiceConfig.Properties.Resources.Warning_yellow_7231_16x16;
            this.pictureBox3.Location = new System.Drawing.Point(482, 94);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(16, 16);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox3.TabIndex = 26;
            this.pictureBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox3, "Warning: Avoid using this feature on databases with very large numbers of objects" +
        " or selecting all databases on servers with very large numbers of databases.");
            // 
            // lnkExample
            // 
            this.lnkExample.AutoSize = true;
            this.lnkExample.Location = new System.Drawing.Point(93, 119);
            this.lnkExample.Name = "lnkExample";
            this.lnkExample.Size = new System.Drawing.Size(61, 17);
            this.lnkExample.TabIndex = 25;
            this.lnkExample.TabStop = true;
            this.lnkExample.Text = "Example";
            this.lnkExample.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkExample_LinkClicked);
            // 
            // lnkNone
            // 
            this.lnkNone.AutoSize = true;
            this.lnkNone.Location = new System.Drawing.Point(44, 119);
            this.lnkNone.Name = "lnkNone";
            this.lnkNone.Size = new System.Drawing.Size(42, 17);
            this.lnkNone.TabIndex = 24;
            this.lnkNone.TabStop = true;
            this.lnkNone.Text = "None";
            this.lnkNone.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkNone_LinkClicked);
            // 
            // lnkALL
            // 
            this.lnkALL.AutoSize = true;
            this.lnkALL.Location = new System.Drawing.Point(15, 119);
            this.lnkALL.Name = "lnkALL";
            this.lnkALL.Size = new System.Drawing.Size(23, 17);
            this.lnkALL.TabIndex = 23;
            this.lnkALL.TabStop = true;
            this.lnkALL.Text = "All";
            this.lnkALL.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkALL_LinkClicked);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 74);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(199, 17);
            this.label8.TabIndex = 20;
            this.label8.Text = "Schema Snapshot Databases:";
            // 
            // txtSnapshotDBs
            // 
            this.txtSnapshotDBs.Location = new System.Drawing.Point(18, 94);
            this.txtSnapshotDBs.Name = "txtSnapshotDBs";
            this.txtSnapshotDBs.Size = new System.Drawing.Size(449, 22);
            this.txtSnapshotDBs.TabIndex = 17;
            // 
            // chkCollectPlans
            // 
            this.chkCollectPlans.AutoSize = true;
            this.chkCollectPlans.Location = new System.Drawing.Point(18, 45);
            this.chkCollectPlans.Name = "chkCollectPlans";
            this.chkCollectPlans.Size = new System.Drawing.Size(243, 21);
            this.chkCollectPlans.TabIndex = 13;
            this.chkCollectPlans.Text = "Collect Plans for Running Queries";
            this.chkCollectPlans.UseVisualStyleBackColor = true;
            this.chkCollectPlans.CheckedChanged += new System.EventHandler(this.chkCollectPlans_CheckedChanged);
            // 
            // grpRunningQueryThreshold
            // 
            this.grpRunningQueryThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpRunningQueryThreshold.Controls.Add(this.label15);
            this.grpRunningQueryThreshold.Controls.Add(this.txtDurationThreshold);
            this.grpRunningQueryThreshold.Controls.Add(this.txtCountThreshold);
            this.grpRunningQueryThreshold.Controls.Add(this.label17);
            this.grpRunningQueryThreshold.Controls.Add(this.label14);
            this.grpRunningQueryThreshold.Controls.Add(this.txtCPUThreshold);
            this.grpRunningQueryThreshold.Controls.Add(this.txtGrantThreshold);
            this.grpRunningQueryThreshold.Controls.Add(this.label16);
            this.grpRunningQueryThreshold.Enabled = false;
            this.grpRunningQueryThreshold.Location = new System.Drawing.Point(513, 6);
            this.grpRunningQueryThreshold.Name = "grpRunningQueryThreshold";
            this.grpRunningQueryThreshold.Size = new System.Drawing.Size(508, 127);
            this.grpRunningQueryThreshold.TabIndex = 22;
            this.grpRunningQueryThreshold.TabStop = false;
            this.grpRunningQueryThreshold.Text = "Running Queries Plan Collection Thresholds";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 32);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(134, 17);
            this.label15.TabIndex = 17;
            this.label15.Text = "Duration Threshold:";
            // 
            // txtDurationThreshold
            // 
            this.txtDurationThreshold.Location = new System.Drawing.Point(146, 30);
            this.txtDurationThreshold.Name = "txtDurationThreshold";
            this.txtDurationThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtDurationThreshold.TabIndex = 16;
            // 
            // txtCountThreshold
            // 
            this.txtCountThreshold.Location = new System.Drawing.Point(385, 63);
            this.txtCountThreshold.Name = "txtCountThreshold";
            this.txtCountThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtCountThreshold.TabIndex = 20;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(263, 63);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(117, 17);
            this.label17.TabIndex = 21;
            this.label17.Text = "Count Threshold:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 57);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(108, 17);
            this.label14.TabIndex = 15;
            this.label14.Text = "CPU Threshold:";
            // 
            // txtCPUThreshold
            // 
            this.txtCPUThreshold.Location = new System.Drawing.Point(146, 60);
            this.txtCPUThreshold.Name = "txtCPUThreshold";
            this.txtCPUThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtCPUThreshold.TabIndex = 14;
            // 
            // txtGrantThreshold
            // 
            this.txtGrantThreshold.Location = new System.Drawing.Point(385, 29);
            this.txtGrantThreshold.Name = "txtGrantThreshold";
            this.txtGrantThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtGrantThreshold.TabIndex = 18;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(263, 32);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(116, 17);
            this.label16.TabIndex = 19;
            this.label16.Text = "Grant Threshold:";
            // 
            // chkNoWMI
            // 
            this.chkNoWMI.Location = new System.Drawing.Point(18, 16);
            this.chkNoWMI.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkNoWMI.Name = "chkNoWMI";
            this.chkNoWMI.Size = new System.Drawing.Size(191, 21);
            this.chkNoWMI.TabIndex = 6;
            this.chkNoWMI.Text = "Don\'t use WMI";
            this.chkNoWMI.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.dgvConnections);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.lblSourceConnections);
            this.groupBox1.Controls.Add(this.txtSearch);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(10, 261);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1069, 292);
            this.groupBox1.TabIndex = 26;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Existing Connections";
            // 
            // dgvConnections
            // 
            this.dgvConnections.AllowUserToAddRows = false;
            this.dgvConnections.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConnections.Location = new System.Drawing.Point(16, 21);
            this.dgvConnections.Name = "dgvConnections";
            this.dgvConnections.RowHeadersWidth = 51;
            this.dgvConnections.RowTemplate.Height = 24;
            this.dgvConnections.Size = new System.Drawing.Size(1035, 237);
            this.dgvConnections.TabIndex = 23;
            this.dgvConnections.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvConnections_CellContentClick);
            this.dgvConnections.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            this.dgvConnections.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_RowValidated);
            this.dgvConnections.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgv_UserDeletedRow);
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(797, 267);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(57, 17);
            this.label13.TabIndex = 25;
            this.label13.Text = "Search:";
            // 
            // lblSourceConnections
            // 
            this.lblSourceConnections.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSourceConnections.AutoSize = true;
            this.lblSourceConnections.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSourceConnections.Location = new System.Drawing.Point(16, 267);
            this.lblSourceConnections.Name = "lblSourceConnections";
            this.lblSourceConnections.Size = new System.Drawing.Size(139, 17);
            this.lblSourceConnections.TabIndex = 22;
            this.lblSourceConnections.Text = "Source Connections:";
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(860, 264);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(191, 22);
            this.txtSearch.TabIndex = 24;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // tabService
            // 
            this.tabService.Controls.Add(this.pictureBox1);
            this.tabService.Controls.Add(this.lnkPermissions);
            this.tabService.Controls.Add(this.textBox1);
            this.tabService.Controls.Add(this.cboServiceCredentials);
            this.tabService.Controls.Add(this.lblServiceCredentials);
            this.tabService.Controls.Add(this.lblServiceStatus);
            this.tabService.Controls.Add(this.bttnRefresh);
            this.tabService.Controls.Add(this.bttnUninstall);
            this.tabService.Controls.Add(this.bttnStart);
            this.tabService.Controls.Add(this.bttnInstall);
            this.tabService.Controls.Add(this.bttnStop);
            this.tabService.Location = new System.Drawing.Point(4, 25);
            this.tabService.Name = "tabService";
            this.tabService.Padding = new System.Windows.Forms.Padding(3);
            this.tabService.Size = new System.Drawing.Size(1096, 559);
            this.tabService.TabIndex = 3;
            this.tabService.Text = "Service";
            this.tabService.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::DBADashServiceConfig.Properties.Resources.Information_blue_6227_16x16_cyan;
            this.pictureBox1.Location = new System.Drawing.Point(10, 230);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(16, 16);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 26;
            this.pictureBox1.TabStop = false;
            // 
            // lnkPermissions
            // 
            this.lnkPermissions.AutoSize = true;
            this.lnkPermissions.Location = new System.Drawing.Point(32, 230);
            this.lnkPermissions.Name = "lnkPermissions";
            this.lnkPermissions.Size = new System.Drawing.Size(243, 17);
            this.lnkPermissions.TabIndex = 25;
            this.lnkPermissions.TabStop = true;
            this.lnkPermissions.Text = "Permissions required for service user";
            this.lnkPermissions.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkPermissions_LinkClicked);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.Gainsboro;
            this.textBox1.Location = new System.Drawing.Point(10, 119);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(756, 104);
            this.textBox1.TabIndex = 24;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            // 
            // tabOther
            // 
            this.tabOther.Controls.Add(this.groupBox4);
            this.tabOther.Controls.Add(this.groupBox3);
            this.tabOther.Controls.Add(this.bttnSchedule);
            this.tabOther.Controls.Add(this.chkLogInternalPerfCounters);
            this.tabOther.Location = new System.Drawing.Point(4, 25);
            this.tabOther.Name = "tabOther";
            this.tabOther.Padding = new System.Windows.Forms.Padding(3);
            this.tabOther.Size = new System.Drawing.Size(1096, 559);
            this.tabOther.TabIndex = 5;
            this.tabOther.Text = "Options";
            this.tabOther.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkScanAzureDB);
            this.groupBox4.Controls.Add(this.lblHHmm);
            this.groupBox4.Controls.Add(this.bttnScanNow);
            this.groupBox4.Controls.Add(this.numAzureScanInterval);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.chkScanEvery);
            this.groupBox4.Location = new System.Drawing.Point(24, 334);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(1055, 208);
            this.groupBox4.TabIndex = 30;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Azure DB";
            // 
            // chkScanAzureDB
            // 
            this.chkScanAzureDB.AutoSize = true;
            this.chkScanAzureDB.Location = new System.Drawing.Point(18, 30);
            this.chkScanAzureDB.Name = "chkScanAzureDB";
            this.chkScanAzureDB.Size = new System.Drawing.Size(251, 21);
            this.chkScanAzureDB.TabIndex = 23;
            this.chkScanAzureDB.Text = "Scan for AzureDBs on service start";
            this.toolTip1.SetToolTip(this.chkScanAzureDB, "Add connection to Azure master DB.  Connections to other AzureDBs will be added o" +
        "n the fly at service start.");
            this.chkScanAzureDB.UseVisualStyleBackColor = true;
            this.chkScanAzureDB.CheckedChanged += new System.EventHandler(this.chkScanAzureDB_CheckedChanged);
            // 
            // lblHHmm
            // 
            this.lblHHmm.AutoSize = true;
            this.lblHHmm.Location = new System.Drawing.Point(586, 58);
            this.lblHHmm.Name = "lblHHmm";
            this.lblHHmm.Size = new System.Drawing.Size(0, 17);
            this.lblHHmm.TabIndex = 29;
            // 
            // bttnScanNow
            // 
            this.bttnScanNow.Location = new System.Drawing.Point(18, 68);
            this.bttnScanNow.Name = "bttnScanNow";
            this.bttnScanNow.Size = new System.Drawing.Size(104, 30);
            this.bttnScanNow.TabIndex = 22;
            this.bttnScanNow.Text = "Scan Now";
            this.toolTip1.SetToolTip(this.bttnScanNow, "Click this button to add connections for each Azure DB from the connection added " +
        "for the master database.");
            this.bttnScanNow.UseVisualStyleBackColor = true;
            this.bttnScanNow.Click += new System.EventHandler(this.bttnScanNow_Click);
            // 
            // numAzureScanInterval
            // 
            this.numAzureScanInterval.Increment = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numAzureScanInterval.Location = new System.Drawing.Point(586, 30);
            this.numAzureScanInterval.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numAzureScanInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numAzureScanInterval.Name = "numAzureScanInterval";
            this.numAzureScanInterval.Size = new System.Drawing.Size(95, 22);
            this.numAzureScanInterval.TabIndex = 28;
            this.numAzureScanInterval.ValueChanged += new System.EventHandler(this.numAzureScanInterval_ValueChanged);
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(19, 110);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(1030, 82);
            this.label10.TabIndex = 24;
            this.label10.Text = resources.GetString("label10.Text");
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(687, 31);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(61, 17);
            this.label11.TabIndex = 27;
            this.label11.Text = "seconds";
            // 
            // chkScanEvery
            // 
            this.chkScanEvery.AutoSize = true;
            this.chkScanEvery.Location = new System.Drawing.Point(362, 30);
            this.chkScanEvery.Name = "chkScanEvery";
            this.chkScanEvery.Size = new System.Drawing.Size(218, 21);
            this.chkScanEvery.TabIndex = 26;
            this.chkScanEvery.Text = "Scan for new AzureDBs every";
            this.toolTip1.SetToolTip(this.chkScanEvery, "Automatically detect when new azure DBs are created on this interval.");
            this.chkScanEvery.UseVisualStyleBackColor = true;
            this.chkScanEvery.CheckedChanged += new System.EventHandler(this.chkScanEvery_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.txtSecretKey);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.txtAccessKey);
            this.groupBox3.Controls.Add(this.txtAWSProfile);
            this.groupBox3.Location = new System.Drawing.Point(24, 82);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1055, 246);
            this.groupBox3.TabIndex = 16;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "AWS Credentials";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(19, 125);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(1030, 120);
            this.label12.TabIndex = 15;
            this.label12.Text = resources.GetString("label12.Text");
            // 
            // bttnSchedule
            // 
            this.bttnSchedule.Location = new System.Drawing.Point(24, 23);
            this.bttnSchedule.Name = "bttnSchedule";
            this.bttnSchedule.Size = new System.Drawing.Size(196, 44);
            this.bttnSchedule.TabIndex = 1;
            this.bttnSchedule.Text = "Configure Schedule";
            this.bttnSchedule.UseVisualStyleBackColor = true;
            this.bttnSchedule.Click += new System.EventHandler(this.bttnSchedule_Click);
            // 
            // chkLogInternalPerfCounters
            // 
            this.chkLogInternalPerfCounters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkLogInternalPerfCounters.AutoSize = true;
            this.chkLogInternalPerfCounters.Location = new System.Drawing.Point(828, 23);
            this.chkLogInternalPerfCounters.Name = "chkLogInternalPerfCounters";
            this.chkLogInternalPerfCounters.Size = new System.Drawing.Size(251, 21);
            this.chkLogInternalPerfCounters.TabIndex = 0;
            this.chkLogInternalPerfCounters.Text = "Log Internal Performance Counters";
            this.chkLogInternalPerfCounters.UseVisualStyleBackColor = true;
            this.chkLogInternalPerfCounters.CheckedChanged += new System.EventHandler(this.chkLogInternalPerfCounters_CheckedChanged);
            // 
            // tabJson
            // 
            this.tabJson.Controls.Add(this.txtJson);
            this.tabJson.Location = new System.Drawing.Point(4, 25);
            this.tabJson.Name = "tabJson";
            this.tabJson.Padding = new System.Windows.Forms.Padding(3);
            this.tabJson.Size = new System.Drawing.Size(1096, 559);
            this.tabJson.TabIndex = 6;
            this.tabJson.Text = "Json";
            this.tabJson.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(727, 19);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(28, 23);
            this.button1.TabIndex = 10;
            this.toolTip1.SetToolTip(this.button1, "Choose a folder destination path");
            this.button1.UseVisualStyleBackColor = true;
            // 
            // ServiceConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1137, 657);
            this.Controls.Add(this.tab1);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.bttnSave);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(842, 704);
            this.Name = "ServiceConfig";
            this.Text = "DBA Dash Service Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServiceConfig_FromClosing);
            this.Load += new System.EventHandler(this.ServiceConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.tab1.ResumeLayout(false);
            this.tabDest.ResumeLayout(false);
            this.tabDest.PerformLayout();
            this.tabSource.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tabSrcOptions.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.tabExtendedEvents.ResumeLayout(false);
            this.pnlExtendedEvents.ResumeLayout(false);
            this.pnlExtendedEvents.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).EndInit();
            this.tabAddConnectionOther.ResumeLayout(false);
            this.tabAddConnectionOther.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.grpRunningQueryThreshold.ResumeLayout(false);
            this.grpRunningQueryThreshold.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnections)).EndInit();
            this.tabService.ResumeLayout(false);
            this.tabService.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabOther.ResumeLayout(false);
            this.tabOther.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabJson.ResumeLayout(false);
            this.tabJson.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox txtJson;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAWSProfile;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Button bttnStop;
        private System.Windows.Forms.Button bttnStart;
        private System.Windows.Forms.Button bttnRefresh;
        private System.Windows.Forms.Button bttnUninstall;
        private System.Windows.Forms.Button bttnInstall;
        private System.Windows.Forms.ComboBox cboServiceCredentials;
        private System.Windows.Forms.Label lblServiceCredentials;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSecretKey;
        private System.Windows.Forms.TextBox txtAccessKey;
        private System.Windows.Forms.TabControl tab1;
        private System.Windows.Forms.TabPage tabDest;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.TabPage tabSource;
        private System.Windows.Forms.TabPage tabService;
        private System.Windows.Forms.Button bttnDeployDatabase;
        private System.Windows.Forms.Label lblVersionInfo;
        private System.Windows.Forms.Button bttnConnect;
        private System.Windows.Forms.Button bttnScanNow;
        private System.Windows.Forms.CheckBox chkScanAzureDB;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox chkAutoUpgradeRepoDB;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox chkScanEvery;
        private System.Windows.Forms.NumericUpDown numAzureScanInterval;
        private System.Windows.Forms.Label lblHHmm;
        private System.Windows.Forms.Button bttnDestFolder;
        private System.Windows.Forms.Button bttnS3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.LinkLabel lnkServiceStatus;
        private System.Windows.Forms.LinkLabel lnkSourceConnections;
        private System.Windows.Forms.Label lblSourceConnections;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.LinkLabel lnkPermissions;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TabControl tabSrcOptions;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.Button bttnS3Src;
        private System.Windows.Forms.Button bttnSrcFolder;
        private System.Windows.Forms.Button bttnConnectSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabExtendedEvents;
        private System.Windows.Forms.Panel pnlExtendedEvents;
        private System.Windows.Forms.CheckBox chkSlowQueryThreshold;
        private System.Windows.Forms.CheckBox chkDualSession;
        private System.Windows.Forms.NumericUpDown numSlowQueryThreshold;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblSlow;
        private System.Windows.Forms.CheckBox chkPersistXESession;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtSnapshotDBs;
        private System.Windows.Forms.TabPage tabOther;
        private System.Windows.Forms.CheckBox chkLogInternalPerfCounters;
        private System.Windows.Forms.Button bttnSchedule;
        private System.Windows.Forms.DataGridView dgvConnections;
        private System.Windows.Forms.TabPage tabJson;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TabPage tabAddConnectionOther;
        private System.Windows.Forms.CheckBox chkCollectPlans;
        private System.Windows.Forms.GroupBox grpRunningQueryThreshold;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtDurationThreshold;
        private System.Windows.Forms.TextBox txtCountThreshold;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtCPUThreshold;
        private System.Windows.Forms.TextBox txtGrantThreshold;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.CheckBox chkNoWMI;
        private System.Windows.Forms.TextBox txtSource;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.LinkLabel lnkALL;
        private System.Windows.Forms.LinkLabel lnkExample;
        private System.Windows.Forms.LinkLabel lnkNone;
        private System.Windows.Forms.PictureBox pictureBox3;
    }
}

