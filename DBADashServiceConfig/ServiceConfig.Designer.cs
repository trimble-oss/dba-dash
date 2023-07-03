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
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.bttnSave = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.lnkSourceConnections = new System.Windows.Forms.LinkLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.chkScanEvery = new System.Windows.Forms.CheckBox();
            this.bttnScanNow = new System.Windows.Forms.Button();
            this.chkScanAzureDB = new System.Windows.Forms.CheckBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.chkScriptJobs = new System.Windows.Forms.CheckBox();
            this.chkCollectSessionWaits = new System.Windows.Forms.CheckBox();
            this.chkDualSession = new System.Windows.Forms.CheckBox();
            this.bttnConnectSource = new System.Windows.Forms.Button();
            this.bttnSrcFolder = new System.Windows.Forms.Button();
            this.bttnS3Src = new System.Windows.Forms.Button();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.bttnDeployDatabase = new System.Windows.Forms.Button();
            this.bttnConnect = new System.Windows.Forms.Button();
            this.bttnDestFolder = new System.Windows.Forms.Button();
            this.bttnS3 = new System.Windows.Forms.Button();
            this.lnkStop = new System.Windows.Forms.LinkLabel();
            this.lnkStart = new System.Windows.Forms.LinkLabel();
            this.lnkRefresh = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.lblConfigFileRetention = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnAWS = new System.Windows.Forms.Button();
            this.chkLogInternalPerfCounters = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabJson = new System.Windows.Forms.TabPage();
            this.txtJson = new System.Windows.Forms.TextBox();
            this.tabOther = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblSummaryRefreshCron = new System.Windows.Forms.Label();
            this.chkSummaryRefresh = new System.Windows.Forms.CheckBox();
            this.txtSummaryRefreshCron = new System.Windows.Forms.TextBox();
            this.lnkDeleteConfigBackups = new System.Windows.Forms.LinkLabel();
            this.numBackupRetention = new System.Windows.Forms.NumericUpDown();
            this.lblEncryptionStatus = new System.Windows.Forms.Label();
            this.bttnEncryption = new System.Windows.Forms.Button();
            this.numIdentityCollectionThreshold = new System.Windows.Forms.NumericUpDown();
            this.bttnSchedule = new System.Windows.Forms.Button();
            this.chkDefaultIdentityCollection = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lblHHmm = new System.Windows.Forms.Label();
            this.numAzureScanInterval = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.tabSource = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tabSrcOptions = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabExtendedEvents = new System.Windows.Forms.TabPage();
            this.pnlExtendedEvents = new System.Windows.Forms.Panel();
            this.chkSlowQueryThreshold = new System.Windows.Forms.CheckBox();
            this.numSlowQueryThreshold = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.lblSlow = new System.Windows.Forms.Label();
            this.chkPersistXESession = new System.Windows.Forms.CheckBox();
            this.tabRunningQueries = new System.Windows.Forms.TabPage();
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
            this.tabAddConnectionOther = new System.Windows.Forms.TabPage();
            this.chkWriteToSecondaryDestinations = new System.Windows.Forms.CheckBox();
            this.lblIOCollectionLevel = new System.Windows.Forms.Label();
            this.cboIOLevel = new System.Windows.Forms.ComboBox();
            this.lnkExample = new System.Windows.Forms.LinkLabel();
            this.lnkNone = new System.Windows.Forms.LinkLabel();
            this.lnkALL = new System.Windows.Forms.LinkLabel();
            this.lblSchemaSnapshotDBs = new System.Windows.Forms.Label();
            this.txtSnapshotDBs = new System.Windows.Forms.TextBox();
            this.chkNoWMI = new System.Windows.Forms.CheckBox();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvConnections = new System.Windows.Forms.DataGridView();
            this.label13 = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.tabDest = new System.Windows.Forms.TabPage();
            this.lblServiceWarning = new System.Windows.Forms.Label();
            this.bttnAbout = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.bttnViewServiceLog = new System.Windows.Forms.Button();
            this.lnkInstall = new System.Windows.Forms.LinkLabel();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.chkAutoUpgradeRepoDB = new System.Windows.Forms.CheckBox();
            this.lblVersionInfo = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtDestination = new System.Windows.Forms.TextBox();
            this.tab1 = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel1.SuspendLayout();
            this.tabJson.SuspendLayout();
            this.tabOther.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBackupRetention)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIdentityCollectionThreshold)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).BeginInit();
            this.tabSource.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabSrcOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabExtendedEvents.SuspendLayout();
            this.pnlExtendedEvents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).BeginInit();
            this.tabRunningQueries.SuspendLayout();
            this.grpRunningQueryThreshold.SuspendLayout();
            this.tabAddConnectionOther.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnections)).BeginInit();
            this.tabDest.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tab1.SuspendLayout();
            this.SuspendLayout();
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // bttnSave
            // 
            this.bttnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bttnSave.Location = new System.Drawing.Point(909, 16);
            this.bttnSave.Name = "bttnSave";
            this.bttnSave.Size = new System.Drawing.Size(101, 35);
            this.bttnSave.TabIndex = 14;
            this.bttnSave.Text = "&Save";
            this.bttnSave.UseVisualStyleBackColor = true;
            this.bttnSave.Click += new System.EventHandler(this.BttnSave_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(37, 373);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 20);
            this.label5.TabIndex = 12;
            this.label5.Text = "Json:";
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.Location = new System.Drawing.Point(1016, 15);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(5);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(101, 35);
            this.bttnCancel.TabIndex = 15;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // lnkSourceConnections
            // 
            this.lnkSourceConnections.AutoSize = true;
            this.lnkSourceConnections.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lnkSourceConnections.LinkColor = System.Drawing.Color.Black;
            this.lnkSourceConnections.Location = new System.Drawing.Point(27, 25);
            this.lnkSourceConnections.Name = "lnkSourceConnections";
            this.lnkSourceConnections.Size = new System.Drawing.Size(128, 16);
            this.lnkSourceConnections.TabIndex = 18;
            this.lnkSourceConnections.TabStop = true;
            this.lnkSourceConnections.Text = "Source Connections:";
            this.lnkSourceConnections.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkSourceConnections_LinkClicked);
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
            // chkScanEvery
            // 
            this.chkScanEvery.AutoSize = true;
            this.chkScanEvery.Location = new System.Drawing.Point(18, 72);
            this.chkScanEvery.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkScanEvery.Name = "chkScanEvery";
            this.chkScanEvery.Size = new System.Drawing.Size(223, 24);
            this.chkScanEvery.TabIndex = 26;
            this.chkScanEvery.Text = "Scan for new AzureDBs every";
            this.toolTip1.SetToolTip(this.chkScanEvery, "Automatically detect when new azure DBs are created on this interval.");
            this.chkScanEvery.UseVisualStyleBackColor = true;
            this.chkScanEvery.CheckedChanged += new System.EventHandler(this.ChkScanEvery_CheckedChanged);
            // 
            // bttnScanNow
            // 
            this.bttnScanNow.Location = new System.Drawing.Point(441, 65);
            this.bttnScanNow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnScanNow.Name = "bttnScanNow";
            this.bttnScanNow.Size = new System.Drawing.Size(104, 37);
            this.bttnScanNow.TabIndex = 22;
            this.bttnScanNow.Text = "Scan Now";
            this.toolTip1.SetToolTip(this.bttnScanNow, "Click this button to add connections for each Azure DB from the connection added " +
        "for the master database.");
            this.bttnScanNow.UseVisualStyleBackColor = true;
            this.bttnScanNow.Click += new System.EventHandler(this.BttnScanNow_Click);
            // 
            // chkScanAzureDB
            // 
            this.chkScanAzureDB.AutoSize = true;
            this.chkScanAzureDB.Location = new System.Drawing.Point(18, 37);
            this.chkScanAzureDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkScanAzureDB.Name = "chkScanAzureDB";
            this.chkScanAzureDB.Size = new System.Drawing.Size(256, 24);
            this.chkScanAzureDB.TabIndex = 23;
            this.chkScanAzureDB.Text = "Scan for AzureDBs on service start";
            this.toolTip1.SetToolTip(this.chkScanAzureDB, "Add connection to Azure master DB.  Connections to other AzureDBs will be added o" +
        "n the fly at service start.");
            this.chkScanAzureDB.UseVisualStyleBackColor = true;
            this.chkScanAzureDB.CheckedChanged += new System.EventHandler(this.ChkScanAzureDB_CheckedChanged);
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = global::DBADashServiceConfig.Properties.Resources.Warning_yellow_7231_16x16;
            this.pictureBox3.Location = new System.Drawing.Point(679, 91);
            this.pictureBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(16, 16);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox3.TabIndex = 26;
            this.pictureBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox3, "Warning: Avoid using this feature on databases with very large numbers of objects" +
        " or selecting all databases on servers with very large numbers of databases.");
            // 
            // chkScriptJobs
            // 
            this.chkScriptJobs.AutoSize = true;
            this.chkScriptJobs.Checked = true;
            this.chkScriptJobs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkScriptJobs.Location = new System.Drawing.Point(146, 8);
            this.chkScriptJobs.Name = "chkScriptJobs";
            this.chkScriptJobs.Size = new System.Drawing.Size(96, 20);
            this.chkScriptJobs.TabIndex = 28;
            this.chkScriptJobs.Text = "Script Jobs";
            this.toolTip1.SetToolTip(this.chkScriptJobs, "If Jobs collection is enabled, the job definition will be scripted via SMO");
            this.chkScriptJobs.UseVisualStyleBackColor = true;
            // 
            // chkCollectSessionWaits
            // 
            this.chkCollectSessionWaits.AutoSize = true;
            this.chkCollectSessionWaits.Checked = true;
            this.chkCollectSessionWaits.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCollectSessionWaits.Location = new System.Drawing.Point(9, 8);
            this.chkCollectSessionWaits.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkCollectSessionWaits.Name = "chkCollectSessionWaits";
            this.chkCollectSessionWaits.Size = new System.Drawing.Size(159, 20);
            this.chkCollectSessionWaits.TabIndex = 27;
            this.chkCollectSessionWaits.Text = "Collect Session Waits";
            this.toolTip1.SetToolTip(this.chkCollectSessionWaits, "Collect Session Waits for Running Queries");
            this.chkCollectSessionWaits.UseVisualStyleBackColor = true;
            // 
            // chkDualSession
            // 
            this.chkDualSession.AutoSize = true;
            this.chkDualSession.Enabled = false;
            this.chkDualSession.Location = new System.Drawing.Point(6, 71);
            this.chkDualSession.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkDualSession.Name = "chkDualSession";
            this.chkDualSession.Size = new System.Drawing.Size(133, 20);
            this.chkDualSession.TabIndex = 17;
            this.chkDualSession.Text = "Use dual session";
            this.toolTip1.SetToolTip(this.chkDualSession, "Uses overlapping event sessions to try to capture events that occur during the br" +
        "eif period where the session is stopped to flush the ring buffer.");
            this.chkDualSession.UseVisualStyleBackColor = true;
            // 
            // bttnConnectSource
            // 
            this.bttnConnectSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnConnectSource.Image = global::DBADashServiceConfig.Properties.Resources.Connect_16x;
            this.bttnConnectSource.Location = new System.Drawing.Point(957, 35);
            this.bttnConnectSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnConnectSource.Name = "bttnConnectSource";
            this.bttnConnectSource.Size = new System.Drawing.Size(27, 29);
            this.bttnConnectSource.TabIndex = 8;
            this.toolTip1.SetToolTip(this.bttnConnectSource, "Connect to a SQL Instance to monitor with DBA Dash");
            this.bttnConnectSource.UseVisualStyleBackColor = true;
            this.bttnConnectSource.Click += new System.EventHandler(this.BttnConnectSource_Click);
            // 
            // bttnSrcFolder
            // 
            this.bttnSrcFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSrcFolder.Image = global::DBADashServiceConfig.Properties.Resources.FolderOpened_16x;
            this.bttnSrcFolder.Location = new System.Drawing.Point(991, 35);
            this.bttnSrcFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnSrcFolder.Name = "bttnSrcFolder";
            this.bttnSrcFolder.Size = new System.Drawing.Size(27, 29);
            this.bttnSrcFolder.TabIndex = 10;
            this.toolTip1.SetToolTip(this.bttnSrcFolder, "Choose a folder source path");
            this.bttnSrcFolder.UseVisualStyleBackColor = true;
            this.bttnSrcFolder.Click += new System.EventHandler(this.BttnSrcFolder_Click_1);
            // 
            // bttnS3Src
            // 
            this.bttnS3Src.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnS3Src.Image = global::DBADashServiceConfig.Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            this.bttnS3Src.Location = new System.Drawing.Point(1024, 36);
            this.bttnS3Src.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnS3Src.Name = "bttnS3Src";
            this.bttnS3Src.Size = new System.Drawing.Size(27, 29);
            this.bttnS3Src.TabIndex = 12;
            this.toolTip1.SetToolTip(this.bttnS3Src, "Choose a S3 bucket source");
            this.bttnS3Src.UseVisualStyleBackColor = true;
            this.bttnS3Src.Click += new System.EventHandler(this.BttnS3Src_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::DBADashServiceConfig.Properties.Resources.Information_blue_6227_16x16_cyan;
            this.pictureBox2.Location = new System.Drawing.Point(6, 8);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 14;
            this.pictureBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox2, resources.GetString("pictureBox2.ToolTip"));
            // 
            // bttnDeployDatabase
            // 
            this.bttnDeployDatabase.Location = new System.Drawing.Point(103, 93);
            this.bttnDeployDatabase.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnDeployDatabase.Name = "bttnDeployDatabase";
            this.bttnDeployDatabase.Size = new System.Drawing.Size(214, 29);
            this.bttnDeployDatabase.TabIndex = 5;
            this.bttnDeployDatabase.Text = "Deploy/Update Database";
            this.toolTip1.SetToolTip(this.bttnDeployDatabase, "Click to create/upgrade your DBA Dash repository database");
            this.bttnDeployDatabase.UseVisualStyleBackColor = true;
            this.bttnDeployDatabase.Click += new System.EventHandler(this.BttnDeployDatabase_Click);
            // 
            // bttnConnect
            // 
            this.bttnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnConnect.Image = global::DBADashServiceConfig.Properties.Resources.Connect_16x;
            this.bttnConnect.Location = new System.Drawing.Point(1013, 25);
            this.bttnConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnConnect.Name = "bttnConnect";
            this.bttnConnect.Size = new System.Drawing.Size(27, 29);
            this.bttnConnect.TabIndex = 7;
            this.toolTip1.SetToolTip(this.bttnConnect, "Connect to a SQL Instance that will store your DBA Dash repository database");
            this.bttnConnect.UseVisualStyleBackColor = true;
            this.bttnConnect.Click += new System.EventHandler(this.BttnConnect_Click);
            // 
            // bttnDestFolder
            // 
            this.bttnDestFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnDestFolder.Image = global::DBADashServiceConfig.Properties.Resources.FolderOpened_16x;
            this.bttnDestFolder.Location = new System.Drawing.Point(1047, 25);
            this.bttnDestFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnDestFolder.Name = "bttnDestFolder";
            this.bttnDestFolder.Size = new System.Drawing.Size(27, 29);
            this.bttnDestFolder.TabIndex = 9;
            this.toolTip1.SetToolTip(this.bttnDestFolder, "Choose a folder destination path");
            this.bttnDestFolder.UseVisualStyleBackColor = true;
            this.bttnDestFolder.Click += new System.EventHandler(this.BttnDestFolder_Click);
            // 
            // bttnS3
            // 
            this.bttnS3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnS3.Image = global::DBADashServiceConfig.Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            this.bttnS3.Location = new System.Drawing.Point(1082, 25);
            this.bttnS3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnS3.Name = "bttnS3";
            this.bttnS3.Size = new System.Drawing.Size(27, 29);
            this.bttnS3.TabIndex = 10;
            this.toolTip1.SetToolTip(this.bttnS3, "Choose a S3 bucket destination");
            this.bttnS3.UseVisualStyleBackColor = true;
            this.bttnS3.Click += new System.EventHandler(this.BttnS3_Click);
            // 
            // lnkStop
            // 
            this.lnkStop.Image = global::DBADashServiceConfig.Properties.Resources.Stop_16x;
            this.lnkStop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkStop.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkStop.Location = new System.Drawing.Point(367, 43);
            this.lnkStop.Name = "lnkStop";
            this.lnkStop.Size = new System.Drawing.Size(62, 21);
            this.lnkStop.TabIndex = 20;
            this.lnkStop.TabStop = true;
            this.lnkStop.Text = "Stop";
            this.lnkStop.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.toolTip1.SetToolTip(this.lnkStop, "Stop the DBA Dash service");
            this.lnkStop.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkStop_LinkClicked);
            // 
            // lnkStart
            // 
            this.lnkStart.Image = global::DBADashServiceConfig.Properties.Resources.StartWithoutDebug_16x;
            this.lnkStart.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkStart.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkStart.Location = new System.Drawing.Point(270, 43);
            this.lnkStart.Name = "lnkStart";
            this.lnkStart.Size = new System.Drawing.Size(69, 21);
            this.lnkStart.TabIndex = 19;
            this.lnkStart.TabStop = true;
            this.lnkStart.Text = "Start";
            this.lnkStart.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.toolTip1.SetToolTip(this.lnkStart, "Start the DBA Dash service");
            this.lnkStart.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkStart_LinkClicked);
            // 
            // lnkRefresh
            // 
            this.lnkRefresh.Image = global::DBADashServiceConfig.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.lnkRefresh.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkRefresh.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkRefresh.Location = new System.Drawing.Point(457, 43);
            this.lnkRefresh.Name = "lnkRefresh";
            this.lnkRefresh.Size = new System.Drawing.Size(80, 21);
            this.lnkRefresh.TabIndex = 21;
            this.lnkRefresh.TabStop = true;
            this.lnkRefresh.Text = "Refresh";
            this.lnkRefresh.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.toolTip1.SetToolTip(this.lnkRefresh, "Refresh the status of the DBA Dash service");
            this.lnkRefresh.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkRefresh_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 108);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(156, 20);
            this.label3.TabIndex = 43;
            this.label3.Text = "Summary refresh cron:";
            this.toolTip1.SetToolTip(this.label3, "Run Summary_Upd on this schedule.  Improves the performance of loading the summar" +
        "y page in the GUI.  Cron expression or value in seconds");
            // 
            // lblConfigFileRetention
            // 
            this.lblConfigFileRetention.AutoSize = true;
            this.lblConfigFileRetention.Location = new System.Drawing.Point(16, 72);
            this.lblConfigFileRetention.Name = "lblConfigFileRetention";
            this.lblConfigFileRetention.Size = new System.Drawing.Size(197, 20);
            this.lblConfigFileRetention.TabIndex = 40;
            this.lblConfigFileRetention.Text = "Config File Retention (Days):";
            this.toolTip1.SetToolTip(this.lblConfigFileRetention, "Backups of config are created automatically to provide a rollback option. Remove " +
        "old configs on this schedule. ");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(265, 20);
            this.label2.TabIndex = 32;
            this.label2.Text = "Identity Collection Threshold (Used %):";
            this.toolTip1.SetToolTip(this.label2, "Collection threshold at which to collect identity column usage.");
            // 
            // bttnAWS
            // 
            this.bttnAWS.Location = new System.Drawing.Point(16, 149);
            this.bttnAWS.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnAWS.Name = "bttnAWS";
            this.bttnAWS.Size = new System.Drawing.Size(197, 55);
            this.bttnAWS.TabIndex = 35;
            this.bttnAWS.Text = "AWS Credentials";
            this.toolTip1.SetToolTip(this.bttnAWS, "Used when writing to a S3 bucket destination or reading from a S3 bucket.  Altern" +
        "atively, use an instance profile to avoid storing credentials in the config.  Or" +
        " consider using an encrypted config.");
            this.bttnAWS.UseVisualStyleBackColor = true;
            this.bttnAWS.Click += new System.EventHandler(this.bttnAWS_Click);
            // 
            // chkLogInternalPerfCounters
            // 
            this.chkLogInternalPerfCounters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkLogInternalPerfCounters.AutoSize = true;
            this.chkLogInternalPerfCounters.Location = new System.Drawing.Point(845, 27);
            this.chkLogInternalPerfCounters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkLogInternalPerfCounters.Name = "chkLogInternalPerfCounters";
            this.chkLogInternalPerfCounters.Size = new System.Drawing.Size(259, 24);
            this.chkLogInternalPerfCounters.TabIndex = 0;
            this.chkLogInternalPerfCounters.Text = "Log Internal Performance Counters";
            this.toolTip1.SetToolTip(this.chkLogInternalPerfCounters, "Internal performance counters are available on the Metrics tab in the GUI.  They " +
        "track things like how long each collection took to run.");
            this.chkLogInternalPerfCounters.UseVisualStyleBackColor = true;
            this.chkLogInternalPerfCounters.CheckedChanged += new System.EventHandler(this.ChkLogInternalPerfCounters_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.bttnSave);
            this.panel1.Controls.Add(this.bttnCancel);
            this.panel1.Controls.Add(this.lnkSourceConnections);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 765);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1137, 64);
            this.panel1.TabIndex = 23;
            // 
            // tabJson
            // 
            this.tabJson.Controls.Add(this.txtJson);
            this.tabJson.Location = new System.Drawing.Point(4, 29);
            this.tabJson.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabJson.Name = "tabJson";
            this.tabJson.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabJson.Size = new System.Drawing.Size(1129, 732);
            this.tabJson.TabIndex = 6;
            this.tabJson.Text = "Json";
            this.tabJson.UseVisualStyleBackColor = true;
            // 
            // txtJson
            // 
            this.txtJson.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtJson.Location = new System.Drawing.Point(3, 4);
            this.txtJson.Multiline = true;
            this.txtJson.Name = "txtJson";
            this.txtJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtJson.Size = new System.Drawing.Size(1123, 724);
            this.txtJson.TabIndex = 13;
            this.txtJson.TextChanged += new System.EventHandler(this.TxtJson_TextChanged);
            this.txtJson.Validating += new System.ComponentModel.CancelEventHandler(this.TxtJson_Validating);
            // 
            // tabOther
            // 
            this.tabOther.Controls.Add(this.groupBox3);
            this.tabOther.Controls.Add(this.groupBox4);
            this.tabOther.Location = new System.Drawing.Point(4, 29);
            this.tabOther.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabOther.Name = "tabOther";
            this.tabOther.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabOther.Size = new System.Drawing.Size(1129, 732);
            this.tabOther.TabIndex = 5;
            this.tabOther.Text = "Options";
            this.tabOther.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lblSummaryRefreshCron);
            this.groupBox3.Controls.Add(this.chkSummaryRefresh);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.txtSummaryRefreshCron);
            this.groupBox3.Controls.Add(this.lnkDeleteConfigBackups);
            this.groupBox3.Controls.Add(this.lblConfigFileRetention);
            this.groupBox3.Controls.Add(this.numBackupRetention);
            this.groupBox3.Controls.Add(this.lblEncryptionStatus);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.bttnEncryption);
            this.groupBox3.Controls.Add(this.numIdentityCollectionThreshold);
            this.groupBox3.Controls.Add(this.bttnAWS);
            this.groupBox3.Controls.Add(this.bttnSchedule);
            this.groupBox3.Controls.Add(this.chkDefaultIdentityCollection);
            this.groupBox3.Controls.Add(this.chkLogInternalPerfCounters);
            this.groupBox3.Location = new System.Drawing.Point(8, 19);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1113, 260);
            this.groupBox3.TabIndex = 38;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Miscellaneous";
            // 
            // lblSummaryRefreshCron
            // 
            this.lblSummaryRefreshCron.AutoSize = true;
            this.lblSummaryRefreshCron.Location = new System.Drawing.Point(441, 111);
            this.lblSummaryRefreshCron.Name = "lblSummaryRefreshCron";
            this.lblSummaryRefreshCron.Size = new System.Drawing.Size(0, 20);
            this.lblSummaryRefreshCron.TabIndex = 45;
            // 
            // chkSummaryRefresh
            // 
            this.chkSummaryRefresh.AutoSize = true;
            this.chkSummaryRefresh.Location = new System.Drawing.Point(407, 109);
            this.chkSummaryRefresh.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkSummaryRefresh.Name = "chkSummaryRefresh";
            this.chkSummaryRefresh.Size = new System.Drawing.Size(18, 17);
            this.chkSummaryRefresh.TabIndex = 44;
            this.chkSummaryRefresh.UseVisualStyleBackColor = true;
            this.chkSummaryRefresh.CheckedChanged += new System.EventHandler(this.chkSummaryRefresh_CheckedChanged);
            // 
            // txtSummaryRefreshCron
            // 
            this.txtSummaryRefreshCron.Enabled = false;
            this.txtSummaryRefreshCron.Location = new System.Drawing.Point(286, 103);
            this.txtSummaryRefreshCron.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtSummaryRefreshCron.Name = "txtSummaryRefreshCron";
            this.txtSummaryRefreshCron.Size = new System.Drawing.Size(114, 27);
            this.txtSummaryRefreshCron.TabIndex = 42;
            this.txtSummaryRefreshCron.Validated += new System.EventHandler(this.txtSummaryRefreshCron_Validated);
            // 
            // lnkDeleteConfigBackups
            // 
            this.lnkDeleteConfigBackups.AutoSize = true;
            this.lnkDeleteConfigBackups.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkDeleteConfigBackups.Location = new System.Drawing.Point(407, 75);
            this.lnkDeleteConfigBackups.Name = "lnkDeleteConfigBackups";
            this.lnkDeleteConfigBackups.Size = new System.Drawing.Size(159, 20);
            this.lnkDeleteConfigBackups.TabIndex = 41;
            this.lnkDeleteConfigBackups.TabStop = true;
            this.lnkDeleteConfigBackups.Text = "Delete Config Backups";
            this.lnkDeleteConfigBackups.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkDeleteConfigBackups_LinkClicked);
            // 
            // numBackupRetention
            // 
            this.numBackupRetention.Location = new System.Drawing.Point(286, 69);
            this.numBackupRetention.Name = "numBackupRetention";
            this.numBackupRetention.Size = new System.Drawing.Size(114, 27);
            this.numBackupRetention.TabIndex = 39;
            this.numBackupRetention.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            this.numBackupRetention.ValueChanged += new System.EventHandler(this.NumBackupRetention_ValueChanged);
            // 
            // lblEncryptionStatus
            // 
            this.lblEncryptionStatus.Location = new System.Drawing.Point(421, 209);
            this.lblEncryptionStatus.Name = "lblEncryptionStatus";
            this.lblEncryptionStatus.Size = new System.Drawing.Size(197, 27);
            this.lblEncryptionStatus.TabIndex = 38;
            this.lblEncryptionStatus.Text = "{Encryption Status}";
            this.lblEncryptionStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // bttnEncryption
            // 
            this.bttnEncryption.Location = new System.Drawing.Point(421, 149);
            this.bttnEncryption.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnEncryption.Name = "bttnEncryption";
            this.bttnEncryption.Size = new System.Drawing.Size(197, 55);
            this.bttnEncryption.TabIndex = 37;
            this.bttnEncryption.Text = "Configure Encryption";
            this.bttnEncryption.UseVisualStyleBackColor = true;
            this.bttnEncryption.Click += new System.EventHandler(this.bttnEncryption_Click);
            // 
            // numIdentityCollectionThreshold
            // 
            this.numIdentityCollectionThreshold.Enabled = false;
            this.numIdentityCollectionThreshold.Location = new System.Drawing.Point(286, 35);
            this.numIdentityCollectionThreshold.Name = "numIdentityCollectionThreshold";
            this.numIdentityCollectionThreshold.Size = new System.Drawing.Size(114, 27);
            this.numIdentityCollectionThreshold.TabIndex = 34;
            this.numIdentityCollectionThreshold.ValueChanged += new System.EventHandler(this.NumIdentityCollectionThreshold_ValueChanged);
            // 
            // bttnSchedule
            // 
            this.bttnSchedule.Location = new System.Drawing.Point(218, 149);
            this.bttnSchedule.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnSchedule.Name = "bttnSchedule";
            this.bttnSchedule.Size = new System.Drawing.Size(197, 55);
            this.bttnSchedule.TabIndex = 1;
            this.bttnSchedule.Text = "Configure Schedule";
            this.bttnSchedule.UseVisualStyleBackColor = true;
            this.bttnSchedule.Click += new System.EventHandler(this.BttnSchedule_Click);
            // 
            // chkDefaultIdentityCollection
            // 
            this.chkDefaultIdentityCollection.AutoSize = true;
            this.chkDefaultIdentityCollection.Checked = true;
            this.chkDefaultIdentityCollection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDefaultIdentityCollection.Location = new System.Drawing.Point(407, 37);
            this.chkDefaultIdentityCollection.Name = "chkDefaultIdentityCollection";
            this.chkDefaultIdentityCollection.Size = new System.Drawing.Size(80, 24);
            this.chkDefaultIdentityCollection.TabIndex = 33;
            this.chkDefaultIdentityCollection.Text = "Default";
            this.chkDefaultIdentityCollection.UseVisualStyleBackColor = true;
            this.chkDefaultIdentityCollection.CheckedChanged += new System.EventHandler(this.ChkDefaultIdentityCollection_CheckedChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.chkScanAzureDB);
            this.groupBox4.Controls.Add(this.lblHHmm);
            this.groupBox4.Controls.Add(this.bttnScanNow);
            this.groupBox4.Controls.Add(this.numAzureScanInterval);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.chkScanEvery);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Location = new System.Drawing.Point(9, 285);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Size = new System.Drawing.Size(1113, 144);
            this.groupBox4.TabIndex = 30;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Azure DB";
            // 
            // lblHHmm
            // 
            this.lblHHmm.AutoSize = true;
            this.lblHHmm.Location = new System.Drawing.Point(247, 103);
            this.lblHHmm.Name = "lblHHmm";
            this.lblHHmm.Size = new System.Drawing.Size(63, 20);
            this.lblHHmm.TabIndex = 29;
            this.lblHHmm.Text = "00:00:00";
            // 
            // numAzureScanInterval
            // 
            this.numAzureScanInterval.Increment = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numAzureScanInterval.Location = new System.Drawing.Point(247, 72);
            this.numAzureScanInterval.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
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
            this.numAzureScanInterval.Size = new System.Drawing.Size(95, 27);
            this.numAzureScanInterval.TabIndex = 28;
            this.numAzureScanInterval.ValueChanged += new System.EventHandler(this.NumAzureScanInterval_ValueChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(347, 75);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(62, 20);
            this.label11.TabIndex = 27;
            this.label11.Text = "seconds";
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.label10.Location = new System.Drawing.Point(585, 37);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(504, 85);
            this.label10.TabIndex = 24;
            this.label10.Text = resources.GetString("label10.Text");
            // 
            // tabSource
            // 
            this.tabSource.Controls.Add(this.groupBox2);
            this.tabSource.Controls.Add(this.groupBox1);
            this.tabSource.Location = new System.Drawing.Point(4, 29);
            this.tabSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabSource.Name = "tabSource";
            this.tabSource.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabSource.Size = new System.Drawing.Size(1129, 732);
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
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.groupBox2.Location = new System.Drawing.Point(10, 8);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(1102, 311);
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
            this.tabSrcOptions.Controls.Add(this.tabRunningQueries);
            this.tabSrcOptions.Controls.Add(this.tabAddConnectionOther);
            this.tabSrcOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tabSrcOptions.Location = new System.Drawing.Point(16, 27);
            this.tabSrcOptions.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabSrcOptions.Name = "tabSrcOptions";
            this.tabSrcOptions.SelectedIndex = 0;
            this.tabSrcOptions.Size = new System.Drawing.Size(1067, 221);
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
            this.tabGeneral.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabGeneral.Size = new System.Drawing.Size(1059, 192);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // txtSource
            // 
            this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSource.Location = new System.Drawing.Point(6, 35);
            this.txtSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtSource.Multiline = true;
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(945, 115);
            this.txtSource.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source: ";
            // 
            // tabExtendedEvents
            // 
            this.tabExtendedEvents.Controls.Add(this.pnlExtendedEvents);
            this.tabExtendedEvents.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tabExtendedEvents.Location = new System.Drawing.Point(4, 25);
            this.tabExtendedEvents.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabExtendedEvents.Name = "tabExtendedEvents";
            this.tabExtendedEvents.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabExtendedEvents.Size = new System.Drawing.Size(1059, 192);
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
            this.pnlExtendedEvents.Location = new System.Drawing.Point(3, 4);
            this.pnlExtendedEvents.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlExtendedEvents.Name = "pnlExtendedEvents";
            this.pnlExtendedEvents.Size = new System.Drawing.Size(1053, 184);
            this.pnlExtendedEvents.TabIndex = 18;
            // 
            // chkSlowQueryThreshold
            // 
            this.chkSlowQueryThreshold.AutoSize = true;
            this.chkSlowQueryThreshold.Location = new System.Drawing.Point(6, 4);
            this.chkSlowQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkSlowQueryThreshold.Name = "chkSlowQueryThreshold";
            this.chkSlowQueryThreshold.Size = new System.Drawing.Size(263, 20);
            this.chkSlowQueryThreshold.TabIndex = 13;
            this.chkSlowQueryThreshold.Text = "Capture Slow Queries (Extended Event)";
            this.chkSlowQueryThreshold.UseVisualStyleBackColor = true;
            this.chkSlowQueryThreshold.CheckedChanged += new System.EventHandler(this.ChkSlowQueryThreshold_CheckedChanged);
            // 
            // numSlowQueryThreshold
            // 
            this.numSlowQueryThreshold.Enabled = false;
            this.numSlowQueryThreshold.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numSlowQueryThreshold.Location = new System.Drawing.Point(117, 108);
            this.numSlowQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
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
            this.label9.Location = new System.Drawing.Point(6, 109);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(100, 16);
            this.label9.TabIndex = 16;
            this.label9.Text = "Threshold (ms):";
            // 
            // lblSlow
            // 
            this.lblSlow.AutoSize = true;
            this.lblSlow.Location = new System.Drawing.Point(6, 145);
            this.lblSlow.Name = "lblSlow";
            this.lblSlow.Size = new System.Drawing.Size(524, 16);
            this.lblSlow.TabIndex = 14;
            this.lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabl" +
    "ed";
            // 
            // chkPersistXESession
            // 
            this.chkPersistXESession.AutoSize = true;
            this.chkPersistXESession.Enabled = false;
            this.chkPersistXESession.Location = new System.Drawing.Point(6, 37);
            this.chkPersistXESession.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkPersistXESession.Name = "chkPersistXESession";
            this.chkPersistXESession.Size = new System.Drawing.Size(347, 20);
            this.chkPersistXESession.TabIndex = 15;
            this.chkPersistXESession.Text = "Persist XE sessions (to allow for manual configuration)";
            this.chkPersistXESession.UseVisualStyleBackColor = true;
            // 
            // tabRunningQueries
            // 
            this.tabRunningQueries.Controls.Add(this.chkCollectPlans);
            this.tabRunningQueries.Controls.Add(this.grpRunningQueryThreshold);
            this.tabRunningQueries.Controls.Add(this.chkCollectSessionWaits);
            this.tabRunningQueries.Location = new System.Drawing.Point(4, 25);
            this.tabRunningQueries.Name = "tabRunningQueries";
            this.tabRunningQueries.Padding = new System.Windows.Forms.Padding(3);
            this.tabRunningQueries.Size = new System.Drawing.Size(1059, 192);
            this.tabRunningQueries.TabIndex = 4;
            this.tabRunningQueries.Text = "Running Queries";
            this.tabRunningQueries.UseVisualStyleBackColor = true;
            // 
            // chkCollectPlans
            // 
            this.chkCollectPlans.AutoSize = true;
            this.chkCollectPlans.Location = new System.Drawing.Point(9, 43);
            this.chkCollectPlans.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkCollectPlans.Name = "chkCollectPlans";
            this.chkCollectPlans.Size = new System.Drawing.Size(227, 20);
            this.chkCollectPlans.TabIndex = 13;
            this.chkCollectPlans.Text = "Collect Plans for Running Queries";
            this.chkCollectPlans.UseVisualStyleBackColor = true;
            this.chkCollectPlans.CheckedChanged += new System.EventHandler(this.ChkCollectPlans_CheckedChanged);
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
            this.grpRunningQueryThreshold.Location = new System.Drawing.Point(545, 16);
            this.grpRunningQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpRunningQueryThreshold.Name = "grpRunningQueryThreshold";
            this.grpRunningQueryThreshold.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpRunningQueryThreshold.Size = new System.Drawing.Size(509, 132);
            this.grpRunningQueryThreshold.TabIndex = 22;
            this.grpRunningQueryThreshold.TabStop = false;
            this.grpRunningQueryThreshold.Text = "Running Queries Plan Collection Thresholds";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 40);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(124, 16);
            this.label15.TabIndex = 17;
            this.label15.Text = "Duration Threshold:";
            // 
            // txtDurationThreshold
            // 
            this.txtDurationThreshold.Location = new System.Drawing.Point(146, 37);
            this.txtDurationThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDurationThreshold.Name = "txtDurationThreshold";
            this.txtDurationThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtDurationThreshold.TabIndex = 16;
            // 
            // txtCountThreshold
            // 
            this.txtCountThreshold.Location = new System.Drawing.Point(385, 79);
            this.txtCountThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtCountThreshold.Name = "txtCountThreshold";
            this.txtCountThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtCountThreshold.TabIndex = 20;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(263, 83);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(108, 16);
            this.label17.TabIndex = 21;
            this.label17.Text = "Count Threshold:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 83);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(102, 16);
            this.label14.TabIndex = 15;
            this.label14.Text = "CPU Threshold:";
            // 
            // txtCPUThreshold
            // 
            this.txtCPUThreshold.Location = new System.Drawing.Point(146, 79);
            this.txtCPUThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtCPUThreshold.Name = "txtCPUThreshold";
            this.txtCPUThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtCPUThreshold.TabIndex = 14;
            // 
            // txtGrantThreshold
            // 
            this.txtGrantThreshold.Location = new System.Drawing.Point(385, 37);
            this.txtGrantThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtGrantThreshold.Name = "txtGrantThreshold";
            this.txtGrantThreshold.Size = new System.Drawing.Size(100, 22);
            this.txtGrantThreshold.TabIndex = 18;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(263, 40);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(106, 16);
            this.label16.TabIndex = 19;
            this.label16.Text = "Grant Threshold:";
            // 
            // tabAddConnectionOther
            // 
            this.tabAddConnectionOther.Controls.Add(this.chkWriteToSecondaryDestinations);
            this.tabAddConnectionOther.Controls.Add(this.lblIOCollectionLevel);
            this.tabAddConnectionOther.Controls.Add(this.cboIOLevel);
            this.tabAddConnectionOther.Controls.Add(this.chkScriptJobs);
            this.tabAddConnectionOther.Controls.Add(this.pictureBox3);
            this.tabAddConnectionOther.Controls.Add(this.lnkExample);
            this.tabAddConnectionOther.Controls.Add(this.lnkNone);
            this.tabAddConnectionOther.Controls.Add(this.lnkALL);
            this.tabAddConnectionOther.Controls.Add(this.lblSchemaSnapshotDBs);
            this.tabAddConnectionOther.Controls.Add(this.txtSnapshotDBs);
            this.tabAddConnectionOther.Controls.Add(this.chkNoWMI);
            this.tabAddConnectionOther.Location = new System.Drawing.Point(4, 25);
            this.tabAddConnectionOther.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabAddConnectionOther.Name = "tabAddConnectionOther";
            this.tabAddConnectionOther.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabAddConnectionOther.Size = new System.Drawing.Size(1059, 192);
            this.tabAddConnectionOther.TabIndex = 3;
            this.tabAddConnectionOther.Text = "Other";
            this.tabAddConnectionOther.UseVisualStyleBackColor = true;
            // 
            // chkWriteToSecondaryDestinations
            // 
            this.chkWriteToSecondaryDestinations.AutoSize = true;
            this.chkWriteToSecondaryDestinations.Checked = true;
            this.chkWriteToSecondaryDestinations.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWriteToSecondaryDestinations.Location = new System.Drawing.Point(264, 8);
            this.chkWriteToSecondaryDestinations.Name = "chkWriteToSecondaryDestinations";
            this.chkWriteToSecondaryDestinations.Size = new System.Drawing.Size(216, 20);
            this.chkWriteToSecondaryDestinations.TabIndex = 31;
            this.chkWriteToSecondaryDestinations.Text = "Write to secondary destinations";
            this.chkWriteToSecondaryDestinations.UseVisualStyleBackColor = true;
            // 
            // lblIOCollectionLevel
            // 
            this.lblIOCollectionLevel.AutoSize = true;
            this.lblIOCollectionLevel.Location = new System.Drawing.Point(9, 49);
            this.lblIOCollectionLevel.Name = "lblIOCollectionLevel";
            this.lblIOCollectionLevel.Size = new System.Drawing.Size(121, 16);
            this.lblIOCollectionLevel.TabIndex = 30;
            this.lblIOCollectionLevel.Text = "IO Collection Level:";
            // 
            // cboIOLevel
            // 
            this.cboIOLevel.FormattingEnabled = true;
            this.cboIOLevel.Location = new System.Drawing.Point(224, 45);
            this.cboIOLevel.Name = "cboIOLevel";
            this.cboIOLevel.Size = new System.Drawing.Size(135, 24);
            this.cboIOLevel.TabIndex = 29;
            // 
            // lnkExample
            // 
            this.lnkExample.AutoSize = true;
            this.lnkExample.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkExample.Location = new System.Drawing.Point(613, 115);
            this.lnkExample.Name = "lnkExample";
            this.lnkExample.Size = new System.Drawing.Size(60, 16);
            this.lnkExample.TabIndex = 25;
            this.lnkExample.TabStop = true;
            this.lnkExample.Text = "Example";
            this.lnkExample.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkExample_LinkClicked);
            // 
            // lnkNone
            // 
            this.lnkNone.AutoSize = true;
            this.lnkNone.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkNone.Location = new System.Drawing.Point(565, 115);
            this.lnkNone.Name = "lnkNone";
            this.lnkNone.Size = new System.Drawing.Size(40, 16);
            this.lnkNone.TabIndex = 24;
            this.lnkNone.TabStop = true;
            this.lnkNone.Text = "None";
            this.lnkNone.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkNone_LinkClicked);
            // 
            // lnkALL
            // 
            this.lnkALL.AutoSize = true;
            this.lnkALL.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkALL.Location = new System.Drawing.Point(535, 115);
            this.lnkALL.Name = "lnkALL";
            this.lnkALL.Size = new System.Drawing.Size(22, 16);
            this.lnkALL.TabIndex = 23;
            this.lnkALL.TabStop = true;
            this.lnkALL.Text = "All";
            this.lnkALL.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkALL_LinkClicked);
            // 
            // lblSchemaSnapshotDBs
            // 
            this.lblSchemaSnapshotDBs.AutoSize = true;
            this.lblSchemaSnapshotDBs.Location = new System.Drawing.Point(9, 91);
            this.lblSchemaSnapshotDBs.Name = "lblSchemaSnapshotDBs";
            this.lblSchemaSnapshotDBs.Size = new System.Drawing.Size(190, 16);
            this.lblSchemaSnapshotDBs.TabIndex = 20;
            this.lblSchemaSnapshotDBs.Text = "Schema Snapshot Databases:";
            // 
            // txtSnapshotDBs
            // 
            this.txtSnapshotDBs.Location = new System.Drawing.Point(224, 88);
            this.txtSnapshotDBs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtSnapshotDBs.Name = "txtSnapshotDBs";
            this.txtSnapshotDBs.Size = new System.Drawing.Size(449, 22);
            this.txtSnapshotDBs.TabIndex = 17;
            // 
            // chkNoWMI
            // 
            this.chkNoWMI.AutoSize = true;
            this.chkNoWMI.Location = new System.Drawing.Point(9, 8);
            this.chkNoWMI.Name = "chkNoWMI";
            this.chkNoWMI.Size = new System.Drawing.Size(115, 20);
            this.chkNoWMI.TabIndex = 6;
            this.chkNoWMI.Text = "Don\'t use WMI";
            this.chkNoWMI.UseVisualStyleBackColor = true;
            // 
            // bttnAdd
            // 
            this.bttnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnAdd.Location = new System.Drawing.Point(981, 253);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(103, 37);
            this.bttnAdd.TabIndex = 8;
            this.bttnAdd.Text = "Add/Update";
            this.bttnAdd.UseVisualStyleBackColor = true;
            this.bttnAdd.Click += new System.EventHandler(this.BttnAdd_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.dgvConnections);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.txtSearch);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.groupBox1.Location = new System.Drawing.Point(10, 325);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(1102, 384);
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
            this.dgvConnections.BackgroundColor = System.Drawing.Color.White;
            this.dgvConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvConnections.Location = new System.Drawing.Point(16, 27);
            this.dgvConnections.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvConnections.Name = "dgvConnections";
            this.dgvConnections.RowHeadersWidth = 51;
            this.dgvConnections.RowTemplate.Height = 24;
            this.dgvConnections.Size = new System.Drawing.Size(1067, 315);
            this.dgvConnections.TabIndex = 23;
            this.dgvConnections.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvConnections_CellContentClick);
            this.dgvConnections.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.Dgv_RowsAdded);
            this.dgvConnections.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_RowValidated);
            this.dgvConnections.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.Dgv_UserDeletedRow);
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label13.Location = new System.Drawing.Point(830, 353);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(53, 16);
            this.label13.TabIndex = 25;
            this.label13.Text = "Search:";
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(893, 349);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(191, 22);
            this.txtSearch.TabIndex = 24;
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);
            // 
            // tabDest
            // 
            this.tabDest.Controls.Add(this.lblServiceWarning);
            this.tabDest.Controls.Add(this.bttnAbout);
            this.tabDest.Controls.Add(this.groupBox5);
            this.tabDest.Controls.Add(this.bttnS3);
            this.tabDest.Controls.Add(this.bttnDestFolder);
            this.tabDest.Controls.Add(this.chkAutoUpgradeRepoDB);
            this.tabDest.Controls.Add(this.bttnConnect);
            this.tabDest.Controls.Add(this.lblVersionInfo);
            this.tabDest.Controls.Add(this.bttnDeployDatabase);
            this.tabDest.Controls.Add(this.label7);
            this.tabDest.Controls.Add(this.txtDestination);
            this.tabDest.Location = new System.Drawing.Point(4, 29);
            this.tabDest.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabDest.Name = "tabDest";
            this.tabDest.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabDest.Size = new System.Drawing.Size(1129, 732);
            this.tabDest.TabIndex = 2;
            this.tabDest.Text = "Destination:";
            this.tabDest.UseVisualStyleBackColor = true;
            // 
            // lblServiceWarning
            // 
            this.lblServiceWarning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblServiceWarning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(147)))), ((int)(((byte)(37)))));
            this.lblServiceWarning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblServiceWarning.ForeColor = System.Drawing.Color.White;
            this.lblServiceWarning.Location = new System.Drawing.Point(9, 499);
            this.lblServiceWarning.Name = "lblServiceWarning";
            this.lblServiceWarning.Size = new System.Drawing.Size(1103, 47);
            this.lblServiceWarning.TabIndex = 24;
            this.lblServiceWarning.Text = "Warning: ";
            this.lblServiceWarning.Visible = false;
            // 
            // bttnAbout
            // 
            this.bttnAbout.Location = new System.Drawing.Point(103, 168);
            this.bttnAbout.Name = "bttnAbout";
            this.bttnAbout.Size = new System.Drawing.Size(214, 29);
            this.bttnAbout.TabIndex = 20;
            this.bttnAbout.Text = "Check for Updates";
            this.bttnAbout.UseVisualStyleBackColor = true;
            this.bttnAbout.Click += new System.EventHandler(this.BttnAbout_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.bttnViewServiceLog);
            this.groupBox5.Controls.Add(this.lnkInstall);
            this.groupBox5.Controls.Add(this.lblServiceStatus);
            this.groupBox5.Controls.Add(this.lnkRefresh);
            this.groupBox5.Controls.Add(this.lnkStart);
            this.groupBox5.Controls.Add(this.lnkStop);
            this.groupBox5.Location = new System.Drawing.Point(9, 549);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox5.Size = new System.Drawing.Size(1103, 140);
            this.groupBox5.TabIndex = 19;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Service";
            // 
            // bttnViewServiceLog
            // 
            this.bttnViewServiceLog.Location = new System.Drawing.Point(15, 88);
            this.bttnViewServiceLog.Name = "bttnViewServiceLog";
            this.bttnViewServiceLog.Size = new System.Drawing.Size(187, 29);
            this.bttnViewServiceLog.TabIndex = 20;
            this.bttnViewServiceLog.Text = "View Service Log";
            this.bttnViewServiceLog.UseVisualStyleBackColor = true;
            this.bttnViewServiceLog.Click += new System.EventHandler(this.BttnViewServiceLog_Click);
            // 
            // lnkInstall
            // 
            this.lnkInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkInstall.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lnkInstall.Image = global::DBADashServiceConfig.Properties.Resources.install;
            this.lnkInstall.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkInstall.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkInstall.Location = new System.Drawing.Point(937, 43);
            this.lnkInstall.Name = "lnkInstall";
            this.lnkInstall.Size = new System.Drawing.Size(160, 21);
            this.lnkInstall.TabIndex = 22;
            this.lnkInstall.TabStop = true;
            this.lnkInstall.Text = "Install as service";
            this.lnkInstall.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.lnkInstall.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkInstall_LinkClicked);
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Location = new System.Drawing.Point(15, 43);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(100, 20);
            this.lblServiceStatus.TabIndex = 23;
            this.lblServiceStatus.Text = "Service Status";
            // 
            // chkAutoUpgradeRepoDB
            // 
            this.chkAutoUpgradeRepoDB.AutoSize = true;
            this.chkAutoUpgradeRepoDB.Checked = true;
            this.chkAutoUpgradeRepoDB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoUpgradeRepoDB.Location = new System.Drawing.Point(103, 60);
            this.chkAutoUpgradeRepoDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkAutoUpgradeRepoDB.Name = "chkAutoUpgradeRepoDB";
            this.chkAutoUpgradeRepoDB.Size = new System.Drawing.Size(321, 24);
            this.chkAutoUpgradeRepoDB.TabIndex = 8;
            this.chkAutoUpgradeRepoDB.Text = "Auto upgrade repository DB on service start";
            this.chkAutoUpgradeRepoDB.UseVisualStyleBackColor = true;
            this.chkAutoUpgradeRepoDB.CheckedChanged += new System.EventHandler(this.ChkAutoUpgradeRepoDB_CheckedChanged);
            // 
            // lblVersionInfo
            // 
            this.lblVersionInfo.AutoSize = true;
            this.lblVersionInfo.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblVersionInfo.Location = new System.Drawing.Point(103, 127);
            this.lblVersionInfo.Name = "lblVersionInfo";
            this.lblVersionInfo.Size = new System.Drawing.Size(83, 16);
            this.lblVersionInfo.TabIndex = 6;
            this.lblVersionInfo.Text = "version info...";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 25);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 20);
            this.label7.TabIndex = 3;
            this.label7.Text = "Destination:";
            // 
            // txtDestination
            // 
            this.txtDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestination.Location = new System.Drawing.Point(103, 25);
            this.txtDestination.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtDestination.Name = "txtDestination";
            this.txtDestination.Size = new System.Drawing.Size(903, 27);
            this.txtDestination.TabIndex = 1;
            this.txtDestination.Validated += new System.EventHandler(this.TxtDestination_Validated);
            // 
            // tab1
            // 
            this.tab1.Controls.Add(this.tabDest);
            this.tab1.Controls.Add(this.tabSource);
            this.tab1.Controls.Add(this.tabOther);
            this.tab1.Controls.Add(this.tabJson);
            this.tab1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab1.Location = new System.Drawing.Point(0, 0);
            this.tab1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tab1.Name = "tab1";
            this.tab1.SelectedIndex = 0;
            this.tab1.Size = new System.Drawing.Size(1137, 765);
            this.tab1.TabIndex = 22;
            // 
            // ServiceConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1137, 829);
            this.Controls.Add(this.tab1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(842, 866);
            this.Name = "ServiceConfig";
            this.Text = "DBA Dash Service Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServiceConfig_FromClosing);
            this.Load += new System.EventHandler(this.ServiceConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabJson.ResumeLayout(false);
            this.tabJson.PerformLayout();
            this.tabOther.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBackupRetention)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIdentityCollectionThreshold)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).EndInit();
            this.tabSource.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tabSrcOptions.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            this.tabExtendedEvents.ResumeLayout(false);
            this.pnlExtendedEvents.ResumeLayout(false);
            this.pnlExtendedEvents.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).EndInit();
            this.tabRunningQueries.ResumeLayout(false);
            this.tabRunningQueries.PerformLayout();
            this.grpRunningQueryThreshold.ResumeLayout(false);
            this.grpRunningQueryThreshold.PerformLayout();
            this.tabAddConnectionOther.ResumeLayout(false);
            this.tabAddConnectionOther.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvConnections)).EndInit();
            this.tabDest.ResumeLayout(false);
            this.tabDest.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.tab1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.LinkLabel lnkSourceConnections;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabControl tab1;
        private System.Windows.Forms.TabPage tabDest;
        private System.Windows.Forms.Label lblServiceWarning;
        private System.Windows.Forms.Button bttnAbout;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button bttnViewServiceLog;
        private System.Windows.Forms.LinkLabel lnkInstall;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.LinkLabel lnkRefresh;
        private System.Windows.Forms.LinkLabel lnkStart;
        private System.Windows.Forms.LinkLabel lnkStop;
        private System.Windows.Forms.Button bttnS3;
        private System.Windows.Forms.Button bttnDestFolder;
        private System.Windows.Forms.CheckBox chkAutoUpgradeRepoDB;
        private System.Windows.Forms.Button bttnConnect;
        private System.Windows.Forms.Label lblVersionInfo;
        private System.Windows.Forms.Button bttnDeployDatabase;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.TabPage tabSource;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TabControl tabSrcOptions;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.TextBox txtSource;
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
        private System.Windows.Forms.TabPage tabRunningQueries;
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
        private System.Windows.Forms.CheckBox chkCollectSessionWaits;
        private System.Windows.Forms.TabPage tabAddConnectionOther;
        private System.Windows.Forms.Label lblIOCollectionLevel;
        private System.Windows.Forms.ComboBox cboIOLevel;
        private System.Windows.Forms.CheckBox chkScriptJobs;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.LinkLabel lnkExample;
        private System.Windows.Forms.LinkLabel lnkNone;
        private System.Windows.Forms.LinkLabel lnkALL;
        private System.Windows.Forms.Label lblSchemaSnapshotDBs;
        private System.Windows.Forms.TextBox txtSnapshotDBs;
        private System.Windows.Forms.CheckBox chkNoWMI;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dgvConnections;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.TabPage tabOther;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnEncryption;
        private System.Windows.Forms.NumericUpDown numIdentityCollectionThreshold;
        private System.Windows.Forms.Button bttnAWS;
        private System.Windows.Forms.Button bttnSchedule;
        private System.Windows.Forms.CheckBox chkDefaultIdentityCollection;
        private System.Windows.Forms.CheckBox chkLogInternalPerfCounters;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox chkScanAzureDB;
        private System.Windows.Forms.Label lblHHmm;
        private System.Windows.Forms.Button bttnScanNow;
        private System.Windows.Forms.NumericUpDown numAzureScanInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox chkScanEvery;
        private System.Windows.Forms.TabPage tabJson;
        private System.Windows.Forms.TextBox txtJson;
        private System.Windows.Forms.Label lblEncryptionStatus;
        private System.Windows.Forms.NumericUpDown numBackupRetention;
        private System.Windows.Forms.LinkLabel lnkDeleteConfigBackups;
        private System.Windows.Forms.CheckBox chkWriteToSecondaryDestinations;
        private System.Windows.Forms.Label lblSummaryRefreshCron;
        private System.Windows.Forms.CheckBox chkSummaryRefresh;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSummaryRefreshCron;
        private System.Windows.Forms.Label lblConfigFileRetention;
    }
}

