using DBADashGUI.Theme;

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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceConfig));
            errorProvider1 = new System.Windows.Forms.ErrorProvider(components);
            bttnSave = new System.Windows.Forms.Button();
            label5 = new System.Windows.Forms.Label();
            bttnCancel = new System.Windows.Forms.Button();
            lnkSourceConnections = new System.Windows.Forms.LinkLabel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            button1 = new System.Windows.Forms.Button();
            chkScanEvery = new System.Windows.Forms.CheckBox();
            bttnScanNow = new System.Windows.Forms.Button();
            chkScanAzureDB = new System.Windows.Forms.CheckBox();
            pictureBox3 = new System.Windows.Forms.PictureBox();
            chkScriptJobs = new System.Windows.Forms.CheckBox();
            chkCollectSessionWaits = new System.Windows.Forms.CheckBox();
            chkDualSession = new System.Windows.Forms.CheckBox();
            bttnConnectSource = new System.Windows.Forms.Button();
            bttnSrcFolder = new System.Windows.Forms.Button();
            bttnS3Src = new System.Windows.Forms.Button();
            pictureBox2 = new System.Windows.Forms.PictureBox();
            bttnDeployDatabase = new System.Windows.Forms.Button();
            bttnConnect = new System.Windows.Forms.Button();
            bttnDestFolder = new System.Windows.Forms.Button();
            bttnS3 = new System.Windows.Forms.Button();
            lnkStop = new System.Windows.Forms.LinkLabel();
            lnkStart = new System.Windows.Forms.LinkLabel();
            lnkRefresh = new System.Windows.Forms.LinkLabel();
            label3 = new System.Windows.Forms.Label();
            lblConfigFileRetention = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            bttnAWS = new System.Windows.Forms.Button();
            chkLogInternalPerfCounters = new System.Windows.Forms.CheckBox();
            panel1 = new System.Windows.Forms.Panel();
            tabJson = new System.Windows.Forms.TabPage();
            txtJson = new System.Windows.Forms.TextBox();
            tabOther = new System.Windows.Forms.TabPage();
            groupBox3 = new System.Windows.Forms.GroupBox();
            lblSummaryRefreshCron = new System.Windows.Forms.Label();
            chkSummaryRefresh = new System.Windows.Forms.CheckBox();
            txtSummaryRefreshCron = new System.Windows.Forms.TextBox();
            lnkDeleteConfigBackups = new System.Windows.Forms.LinkLabel();
            numBackupRetention = new System.Windows.Forms.NumericUpDown();
            lblEncryptionStatus = new System.Windows.Forms.Label();
            bttnEncryption = new System.Windows.Forms.Button();
            numIdentityCollectionThreshold = new System.Windows.Forms.NumericUpDown();
            bttnSchedule = new System.Windows.Forms.Button();
            chkDefaultIdentityCollection = new System.Windows.Forms.CheckBox();
            groupBox4 = new System.Windows.Forms.GroupBox();
            lblHHmm = new System.Windows.Forms.Label();
            numAzureScanInterval = new System.Windows.Forms.NumericUpDown();
            label11 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            tabSource = new System.Windows.Forms.TabPage();
            groupBox2 = new System.Windows.Forms.GroupBox();
            tabSrcOptions = new ThemedTabControl();
            tabGeneral = new System.Windows.Forms.TabPage();
            txtSource = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            tabExtendedEvents = new System.Windows.Forms.TabPage();
            pnlExtendedEvents = new System.Windows.Forms.Panel();
            chkSlowQueryThreshold = new System.Windows.Forms.CheckBox();
            numSlowQueryThreshold = new System.Windows.Forms.NumericUpDown();
            label9 = new System.Windows.Forms.Label();
            lblSlow = new System.Windows.Forms.Label();
            chkPersistXESession = new System.Windows.Forms.CheckBox();
            tabRunningQueries = new System.Windows.Forms.TabPage();
            chkCollectPlans = new System.Windows.Forms.CheckBox();
            grpRunningQueryThreshold = new System.Windows.Forms.GroupBox();
            label15 = new System.Windows.Forms.Label();
            txtDurationThreshold = new System.Windows.Forms.TextBox();
            txtCountThreshold = new System.Windows.Forms.TextBox();
            label17 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            txtCPUThreshold = new System.Windows.Forms.TextBox();
            txtGrantThreshold = new System.Windows.Forms.TextBox();
            label16 = new System.Windows.Forms.Label();
            tabAddConnectionOther = new System.Windows.Forms.TabPage();
            chkWriteToSecondaryDestinations = new System.Windows.Forms.CheckBox();
            lblIOCollectionLevel = new System.Windows.Forms.Label();
            cboIOLevel = new System.Windows.Forms.ComboBox();
            lnkExample = new System.Windows.Forms.LinkLabel();
            lnkNone = new System.Windows.Forms.LinkLabel();
            lnkALL = new System.Windows.Forms.LinkLabel();
            lblSchemaSnapshotDBs = new System.Windows.Forms.Label();
            txtSnapshotDBs = new System.Windows.Forms.TextBox();
            chkNoWMI = new System.Windows.Forms.CheckBox();
            bttnAdd = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            dgvConnections = new System.Windows.Forms.DataGridView();
            label13 = new System.Windows.Forms.Label();
            txtSearch = new System.Windows.Forms.TextBox();
            tabDest = new System.Windows.Forms.TabPage();
            lblServiceWarning = new System.Windows.Forms.Label();
            bttnAbout = new System.Windows.Forms.Button();
            groupBox5 = new System.Windows.Forms.GroupBox();
            bttnViewServiceLog = new System.Windows.Forms.Button();
            lnkInstall = new System.Windows.Forms.LinkLabel();
            lblServiceStatus = new System.Windows.Forms.Label();
            chkAutoUpgradeRepoDB = new System.Windows.Forms.CheckBox();
            lblVersionInfo = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            txtDestination = new System.Windows.Forms.TextBox();
            tab1 = new ThemedTabControl();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            panel1.SuspendLayout();
            tabJson.SuspendLayout();
            tabOther.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBackupRetention).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numIdentityCollectionThreshold).BeginInit();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numAzureScanInterval).BeginInit();
            tabSource.SuspendLayout();
            groupBox2.SuspendLayout();
            tabSrcOptions.SuspendLayout();
            tabGeneral.SuspendLayout();
            tabExtendedEvents.SuspendLayout();
            pnlExtendedEvents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSlowQueryThreshold).BeginInit();
            tabRunningQueries.SuspendLayout();
            grpRunningQueryThreshold.SuspendLayout();
            tabAddConnectionOther.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConnections).BeginInit();
            tabDest.SuspendLayout();
            groupBox5.SuspendLayout();
            tab1.SuspendLayout();
            SuspendLayout();
            // 
            // errorProvider1
            // 
            errorProvider1.ContainerControl = this;
            // 
            // bttnSave
            // 
            bttnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            bttnSave.Location = new System.Drawing.Point(909, 16);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(101, 35);
            bttnSave.TabIndex = 14;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += BttnSave_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(37, 373);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(40, 20);
            label5.TabIndex = 12;
            label5.Text = "Json:";
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(1016, 15);
            bttnCancel.Margin = new System.Windows.Forms.Padding(5);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(101, 35);
            bttnCancel.TabIndex = 15;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // lnkSourceConnections
            // 
            lnkSourceConnections.AutoSize = true;
            lnkSourceConnections.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lnkSourceConnections.LinkColor = System.Drawing.Color.Black;
            lnkSourceConnections.Location = new System.Drawing.Point(27, 25);
            lnkSourceConnections.Name = "lnkSourceConnections";
            lnkSourceConnections.Size = new System.Drawing.Size(128, 16);
            lnkSourceConnections.TabIndex = 18;
            lnkSourceConnections.TabStop = true;
            lnkSourceConnections.Text = "Source Connections:";
            lnkSourceConnections.LinkClicked += LnkSourceConnections_LinkClicked;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(727, 19);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(28, 23);
            button1.TabIndex = 10;
            toolTip1.SetToolTip(button1, "Choose a folder destination path");
            button1.UseVisualStyleBackColor = true;
            // 
            // chkScanEvery
            // 
            chkScanEvery.AutoSize = true;
            chkScanEvery.Location = new System.Drawing.Point(18, 72);
            chkScanEvery.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkScanEvery.Name = "chkScanEvery";
            chkScanEvery.Size = new System.Drawing.Size(223, 24);
            chkScanEvery.TabIndex = 26;
            chkScanEvery.Text = "Scan for new AzureDBs every";
            toolTip1.SetToolTip(chkScanEvery, "Automatically detect when new azure DBs are created on this interval.");
            chkScanEvery.UseVisualStyleBackColor = true;
            chkScanEvery.CheckedChanged += ChkScanEvery_CheckedChanged;
            // 
            // bttnScanNow
            // 
            bttnScanNow.Location = new System.Drawing.Point(441, 65);
            bttnScanNow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnScanNow.Name = "bttnScanNow";
            bttnScanNow.Size = new System.Drawing.Size(104, 37);
            bttnScanNow.TabIndex = 22;
            bttnScanNow.Text = "Scan Now";
            toolTip1.SetToolTip(bttnScanNow, "Click this button to add connections for each Azure DB from the connection added for the master database.");
            bttnScanNow.UseVisualStyleBackColor = true;
            bttnScanNow.Click += BttnScanNow_Click;
            // 
            // chkScanAzureDB
            // 
            chkScanAzureDB.AutoSize = true;
            chkScanAzureDB.Location = new System.Drawing.Point(18, 37);
            chkScanAzureDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkScanAzureDB.Name = "chkScanAzureDB";
            chkScanAzureDB.Size = new System.Drawing.Size(256, 24);
            chkScanAzureDB.TabIndex = 23;
            chkScanAzureDB.Text = "Scan for AzureDBs on service start";
            toolTip1.SetToolTip(chkScanAzureDB, "Add connection to Azure master DB.  Connections to other AzureDBs will be added on the fly at service start.");
            chkScanAzureDB.UseVisualStyleBackColor = true;
            chkScanAzureDB.CheckedChanged += ChkScanAzureDB_CheckedChanged;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = Properties.Resources.Warning_yellow_7231_16x16;
            pictureBox3.Location = new System.Drawing.Point(679, 91);
            pictureBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new System.Drawing.Size(16, 16);
            pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            pictureBox3.TabIndex = 26;
            pictureBox3.TabStop = false;
            toolTip1.SetToolTip(pictureBox3, "Warning: Avoid using this feature on databases with very large numbers of objects or selecting all databases on servers with very large numbers of databases.");
            // 
            // chkScriptJobs
            // 
            chkScriptJobs.AutoSize = true;
            chkScriptJobs.Checked = true;
            chkScriptJobs.CheckState = System.Windows.Forms.CheckState.Checked;
            chkScriptJobs.Location = new System.Drawing.Point(146, 8);
            chkScriptJobs.Name = "chkScriptJobs";
            chkScriptJobs.Size = new System.Drawing.Size(96, 20);
            chkScriptJobs.TabIndex = 28;
            chkScriptJobs.Text = "Script Jobs";
            toolTip1.SetToolTip(chkScriptJobs, "If Jobs collection is enabled, the job definition will be scripted via SMO");
            chkScriptJobs.UseVisualStyleBackColor = true;
            // 
            // chkCollectSessionWaits
            // 
            chkCollectSessionWaits.AutoSize = true;
            chkCollectSessionWaits.Checked = true;
            chkCollectSessionWaits.CheckState = System.Windows.Forms.CheckState.Checked;
            chkCollectSessionWaits.Location = new System.Drawing.Point(9, 8);
            chkCollectSessionWaits.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkCollectSessionWaits.Name = "chkCollectSessionWaits";
            chkCollectSessionWaits.Size = new System.Drawing.Size(159, 20);
            chkCollectSessionWaits.TabIndex = 27;
            chkCollectSessionWaits.Text = "Collect Session Waits";
            toolTip1.SetToolTip(chkCollectSessionWaits, "Collect Session Waits for Running Queries");
            chkCollectSessionWaits.UseVisualStyleBackColor = true;
            // 
            // chkDualSession
            // 
            chkDualSession.AutoSize = true;
            chkDualSession.Enabled = false;
            chkDualSession.Location = new System.Drawing.Point(6, 71);
            chkDualSession.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkDualSession.Name = "chkDualSession";
            chkDualSession.Size = new System.Drawing.Size(133, 20);
            chkDualSession.TabIndex = 17;
            chkDualSession.Text = "Use dual session";
            toolTip1.SetToolTip(chkDualSession, "Uses overlapping event sessions to try to capture events that occur during the breif period where the session is stopped to flush the ring buffer.");
            chkDualSession.UseVisualStyleBackColor = true;
            // 
            // bttnConnectSource
            // 
            bttnConnectSource.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnConnectSource.Image = Properties.Resources.Connect_16x;
            bttnConnectSource.Location = new System.Drawing.Point(957, 35);
            bttnConnectSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnConnectSource.Name = "bttnConnectSource";
            bttnConnectSource.Size = new System.Drawing.Size(27, 29);
            bttnConnectSource.TabIndex = 8;
            toolTip1.SetToolTip(bttnConnectSource, "Connect to a SQL Instance to monitor with DBA Dash");
            bttnConnectSource.UseVisualStyleBackColor = true;
            bttnConnectSource.Click += BttnConnectSource_Click;
            // 
            // bttnSrcFolder
            // 
            bttnSrcFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnSrcFolder.Image = Properties.Resources.FolderOpened_16x;
            bttnSrcFolder.Location = new System.Drawing.Point(991, 35);
            bttnSrcFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnSrcFolder.Name = "bttnSrcFolder";
            bttnSrcFolder.Size = new System.Drawing.Size(27, 29);
            bttnSrcFolder.TabIndex = 10;
            toolTip1.SetToolTip(bttnSrcFolder, "Choose a folder source path");
            bttnSrcFolder.UseVisualStyleBackColor = true;
            bttnSrcFolder.Click += BttnSrcFolder_Click_1;
            // 
            // bttnS3Src
            // 
            bttnS3Src.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnS3Src.Image = Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            bttnS3Src.Location = new System.Drawing.Point(1024, 36);
            bttnS3Src.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnS3Src.Name = "bttnS3Src";
            bttnS3Src.Size = new System.Drawing.Size(27, 29);
            bttnS3Src.TabIndex = 12;
            toolTip1.SetToolTip(bttnS3Src, "Choose a S3 bucket source");
            bttnS3Src.UseVisualStyleBackColor = true;
            bttnS3Src.Click += BttnS3Src_Click;
            // 
            // pictureBox2
            // 
            pictureBox2.Image = Properties.Resources.Information_blue_6227_16x16_cyan;
            pictureBox2.Location = new System.Drawing.Point(6, 8);
            pictureBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new System.Drawing.Size(16, 16);
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            pictureBox2.TabIndex = 14;
            pictureBox2.TabStop = false;
            toolTip1.SetToolTip(pictureBox2, resources.GetString("pictureBox2.ToolTip"));
            // 
            // bttnDeployDatabase
            // 
            bttnDeployDatabase.Location = new System.Drawing.Point(103, 93);
            bttnDeployDatabase.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnDeployDatabase.Name = "bttnDeployDatabase";
            bttnDeployDatabase.Size = new System.Drawing.Size(214, 29);
            bttnDeployDatabase.TabIndex = 5;
            bttnDeployDatabase.Text = "Deploy/Update Database";
            toolTip1.SetToolTip(bttnDeployDatabase, "Click to create/upgrade your DBA Dash repository database");
            bttnDeployDatabase.UseVisualStyleBackColor = true;
            bttnDeployDatabase.Click += BttnDeployDatabase_Click;
            // 
            // bttnConnect
            // 
            bttnConnect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnConnect.Image = Properties.Resources.Connect_16x;
            bttnConnect.Location = new System.Drawing.Point(1013, 25);
            bttnConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnConnect.Name = "bttnConnect";
            bttnConnect.Size = new System.Drawing.Size(27, 29);
            bttnConnect.TabIndex = 7;
            toolTip1.SetToolTip(bttnConnect, "Connect to a SQL Instance that will store your DBA Dash repository database");
            bttnConnect.UseVisualStyleBackColor = true;
            bttnConnect.Click += BttnConnect_Click;
            // 
            // bttnDestFolder
            // 
            bttnDestFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnDestFolder.Image = Properties.Resources.FolderOpened_16x;
            bttnDestFolder.Location = new System.Drawing.Point(1047, 25);
            bttnDestFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnDestFolder.Name = "bttnDestFolder";
            bttnDestFolder.Size = new System.Drawing.Size(27, 29);
            bttnDestFolder.TabIndex = 9;
            toolTip1.SetToolTip(bttnDestFolder, "Choose a folder destination path");
            bttnDestFolder.UseVisualStyleBackColor = true;
            bttnDestFolder.Click += BttnDestFolder_Click;
            // 
            // bttnS3
            // 
            bttnS3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnS3.Image = Properties.Resources.Arch_Amazon_Simple_Storage_Service_16;
            bttnS3.Location = new System.Drawing.Point(1082, 25);
            bttnS3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnS3.Name = "bttnS3";
            bttnS3.Size = new System.Drawing.Size(27, 29);
            bttnS3.TabIndex = 10;
            toolTip1.SetToolTip(bttnS3, "Choose a S3 bucket destination");
            bttnS3.UseVisualStyleBackColor = true;
            bttnS3.Click += BttnS3_Click;
            // 
            // lnkStop
            // 
            lnkStop.Image = Properties.Resources.Stop_16x;
            lnkStop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lnkStop.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkStop.Location = new System.Drawing.Point(367, 43);
            lnkStop.Name = "lnkStop";
            lnkStop.Size = new System.Drawing.Size(62, 21);
            lnkStop.TabIndex = 20;
            lnkStop.TabStop = true;
            lnkStop.Text = "Stop";
            lnkStop.TextAlign = System.Drawing.ContentAlignment.TopRight;
            toolTip1.SetToolTip(lnkStop, "Stop the DBA Dash service");
            lnkStop.LinkClicked += LnkStop_LinkClicked;
            // 
            // lnkStart
            // 
            lnkStart.Image = Properties.Resources.StartWithoutDebug_16x;
            lnkStart.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lnkStart.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkStart.Location = new System.Drawing.Point(270, 43);
            lnkStart.Name = "lnkStart";
            lnkStart.Size = new System.Drawing.Size(69, 21);
            lnkStart.TabIndex = 19;
            lnkStart.TabStop = true;
            lnkStart.Text = "Start";
            lnkStart.TextAlign = System.Drawing.ContentAlignment.TopRight;
            toolTip1.SetToolTip(lnkStart, "Start the DBA Dash service");
            lnkStart.LinkClicked += LnkStart_LinkClicked;
            // 
            // lnkRefresh
            // 
            lnkRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            lnkRefresh.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lnkRefresh.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkRefresh.Location = new System.Drawing.Point(457, 43);
            lnkRefresh.Name = "lnkRefresh";
            lnkRefresh.Size = new System.Drawing.Size(80, 21);
            lnkRefresh.TabIndex = 21;
            lnkRefresh.TabStop = true;
            lnkRefresh.Text = "Refresh";
            lnkRefresh.TextAlign = System.Drawing.ContentAlignment.TopRight;
            toolTip1.SetToolTip(lnkRefresh, "Refresh the status of the DBA Dash service");
            lnkRefresh.LinkClicked += LnkRefresh_LinkClicked;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(16, 108);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(156, 20);
            label3.TabIndex = 43;
            label3.Text = "Summary refresh cron:";
            toolTip1.SetToolTip(label3, "Run Summary_Upd on this schedule.  Improves the performance of loading the summary page in the GUI.  Cron expression or value in seconds");
            // 
            // lblConfigFileRetention
            // 
            lblConfigFileRetention.AutoSize = true;
            lblConfigFileRetention.Location = new System.Drawing.Point(16, 72);
            lblConfigFileRetention.Name = "lblConfigFileRetention";
            lblConfigFileRetention.Size = new System.Drawing.Size(197, 20);
            lblConfigFileRetention.TabIndex = 40;
            lblConfigFileRetention.Text = "Config File Retention (Days):";
            toolTip1.SetToolTip(lblConfigFileRetention, "Backups of config are created automatically to provide a rollback option. Remove old configs on this schedule. ");
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(16, 36);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(265, 20);
            label2.TabIndex = 32;
            label2.Text = "Identity Collection Threshold (Used %):";
            toolTip1.SetToolTip(label2, "Collection threshold at which to collect identity column usage.");
            // 
            // bttnAWS
            // 
            bttnAWS.Location = new System.Drawing.Point(16, 149);
            bttnAWS.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnAWS.Name = "bttnAWS";
            bttnAWS.Size = new System.Drawing.Size(197, 55);
            bttnAWS.TabIndex = 35;
            bttnAWS.Text = "AWS Credentials";
            toolTip1.SetToolTip(bttnAWS, "Used when writing to a S3 bucket destination or reading from a S3 bucket.  Alternatively, use an instance profile to avoid storing credentials in the config.  Or consider using an encrypted config.");
            bttnAWS.UseVisualStyleBackColor = true;
            bttnAWS.Click += BttnAWS_Click;
            // 
            // chkLogInternalPerfCounters
            // 
            chkLogInternalPerfCounters.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            chkLogInternalPerfCounters.AutoSize = true;
            chkLogInternalPerfCounters.Location = new System.Drawing.Point(845, 27);
            chkLogInternalPerfCounters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkLogInternalPerfCounters.Name = "chkLogInternalPerfCounters";
            chkLogInternalPerfCounters.Size = new System.Drawing.Size(259, 24);
            chkLogInternalPerfCounters.TabIndex = 0;
            chkLogInternalPerfCounters.Text = "Log Internal Performance Counters";
            toolTip1.SetToolTip(chkLogInternalPerfCounters, "Internal performance counters are available on the Metrics tab in the GUI.  They track things like how long each collection took to run.");
            chkLogInternalPerfCounters.UseVisualStyleBackColor = true;
            chkLogInternalPerfCounters.CheckedChanged += ChkLogInternalPerfCounters_CheckedChanged;
            // 
            // panel1
            // 
            panel1.Controls.Add(bttnSave);
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(lnkSourceConnections);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 765);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1137, 64);
            panel1.TabIndex = 23;
            // 
            // tabJson
            // 
            tabJson.Controls.Add(txtJson);
            tabJson.Location = new System.Drawing.Point(4, 29);
            tabJson.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabJson.Name = "tabJson";
            tabJson.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabJson.Size = new System.Drawing.Size(1129, 732);
            tabJson.TabIndex = 6;
            tabJson.Text = "Json";
            tabJson.UseVisualStyleBackColor = true;
            // 
            // txtJson
            // 
            txtJson.Dock = System.Windows.Forms.DockStyle.Fill;
            txtJson.Location = new System.Drawing.Point(3, 4);
            txtJson.Multiline = true;
            txtJson.Name = "txtJson";
            txtJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtJson.Size = new System.Drawing.Size(1123, 724);
            txtJson.TabIndex = 13;
            txtJson.TextChanged += TxtJson_TextChanged;
            txtJson.Validating += TxtJson_Validating;
            // 
            // tabOther
            // 
            tabOther.Controls.Add(groupBox3);
            tabOther.Controls.Add(groupBox4);
            tabOther.Location = new System.Drawing.Point(4, 29);
            tabOther.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabOther.Name = "tabOther";
            tabOther.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabOther.Size = new System.Drawing.Size(1129, 732);
            tabOther.TabIndex = 5;
            tabOther.Text = "Options";
            tabOther.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(lblSummaryRefreshCron);
            groupBox3.Controls.Add(chkSummaryRefresh);
            groupBox3.Controls.Add(label3);
            groupBox3.Controls.Add(txtSummaryRefreshCron);
            groupBox3.Controls.Add(lnkDeleteConfigBackups);
            groupBox3.Controls.Add(lblConfigFileRetention);
            groupBox3.Controls.Add(numBackupRetention);
            groupBox3.Controls.Add(lblEncryptionStatus);
            groupBox3.Controls.Add(label2);
            groupBox3.Controls.Add(bttnEncryption);
            groupBox3.Controls.Add(numIdentityCollectionThreshold);
            groupBox3.Controls.Add(bttnAWS);
            groupBox3.Controls.Add(bttnSchedule);
            groupBox3.Controls.Add(chkDefaultIdentityCollection);
            groupBox3.Controls.Add(chkLogInternalPerfCounters);
            groupBox3.Location = new System.Drawing.Point(8, 19);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(1113, 260);
            groupBox3.TabIndex = 38;
            groupBox3.TabStop = false;
            groupBox3.Text = "Miscellaneous";
            // 
            // lblSummaryRefreshCron
            // 
            lblSummaryRefreshCron.AutoSize = true;
            lblSummaryRefreshCron.Location = new System.Drawing.Point(441, 111);
            lblSummaryRefreshCron.Name = "lblSummaryRefreshCron";
            lblSummaryRefreshCron.Size = new System.Drawing.Size(0, 20);
            lblSummaryRefreshCron.TabIndex = 45;
            // 
            // chkSummaryRefresh
            // 
            chkSummaryRefresh.AutoSize = true;
            chkSummaryRefresh.Location = new System.Drawing.Point(407, 109);
            chkSummaryRefresh.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSummaryRefresh.Name = "chkSummaryRefresh";
            chkSummaryRefresh.Size = new System.Drawing.Size(18, 17);
            chkSummaryRefresh.TabIndex = 44;
            chkSummaryRefresh.UseVisualStyleBackColor = true;
            chkSummaryRefresh.CheckedChanged += ChkSummaryRefresh_CheckedChanged;
            // 
            // txtSummaryRefreshCron
            // 
            txtSummaryRefreshCron.Enabled = false;
            txtSummaryRefreshCron.Location = new System.Drawing.Point(286, 103);
            txtSummaryRefreshCron.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSummaryRefreshCron.Name = "txtSummaryRefreshCron";
            txtSummaryRefreshCron.Size = new System.Drawing.Size(114, 27);
            txtSummaryRefreshCron.TabIndex = 42;
            txtSummaryRefreshCron.Validated += TxtSummaryRefreshCron_Validated;
            // 
            // lnkDeleteConfigBackups
            // 
            lnkDeleteConfigBackups.AutoSize = true;
            lnkDeleteConfigBackups.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkDeleteConfigBackups.Location = new System.Drawing.Point(407, 75);
            lnkDeleteConfigBackups.Name = "lnkDeleteConfigBackups";
            lnkDeleteConfigBackups.Size = new System.Drawing.Size(159, 20);
            lnkDeleteConfigBackups.TabIndex = 41;
            lnkDeleteConfigBackups.TabStop = true;
            lnkDeleteConfigBackups.Text = "Delete Config Backups";
            lnkDeleteConfigBackups.LinkClicked += LnkDeleteConfigBackups_LinkClicked;
            // 
            // numBackupRetention
            // 
            numBackupRetention.Location = new System.Drawing.Point(286, 69);
            numBackupRetention.Name = "numBackupRetention";
            numBackupRetention.Size = new System.Drawing.Size(114, 27);
            numBackupRetention.TabIndex = 39;
            numBackupRetention.Value = new decimal(new int[] { 7, 0, 0, 0 });
            numBackupRetention.ValueChanged += NumBackupRetention_ValueChanged;
            // 
            // lblEncryptionStatus
            // 
            lblEncryptionStatus.Location = new System.Drawing.Point(421, 209);
            lblEncryptionStatus.Name = "lblEncryptionStatus";
            lblEncryptionStatus.Size = new System.Drawing.Size(197, 27);
            lblEncryptionStatus.TabIndex = 38;
            lblEncryptionStatus.Text = "{Encryption Status}";
            lblEncryptionStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // bttnEncryption
            // 
            bttnEncryption.Location = new System.Drawing.Point(421, 149);
            bttnEncryption.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnEncryption.Name = "bttnEncryption";
            bttnEncryption.Size = new System.Drawing.Size(197, 55);
            bttnEncryption.TabIndex = 37;
            bttnEncryption.Text = "Configure Encryption";
            bttnEncryption.UseVisualStyleBackColor = true;
            bttnEncryption.Click += BttnEncryption_Click;
            // 
            // numIdentityCollectionThreshold
            // 
            numIdentityCollectionThreshold.Enabled = false;
            numIdentityCollectionThreshold.Location = new System.Drawing.Point(286, 35);
            numIdentityCollectionThreshold.Name = "numIdentityCollectionThreshold";
            numIdentityCollectionThreshold.Size = new System.Drawing.Size(114, 27);
            numIdentityCollectionThreshold.TabIndex = 34;
            numIdentityCollectionThreshold.ValueChanged += NumIdentityCollectionThreshold_ValueChanged;
            // 
            // bttnSchedule
            // 
            bttnSchedule.Location = new System.Drawing.Point(218, 149);
            bttnSchedule.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnSchedule.Name = "bttnSchedule";
            bttnSchedule.Size = new System.Drawing.Size(197, 55);
            bttnSchedule.TabIndex = 1;
            bttnSchedule.Text = "Configure Schedule";
            bttnSchedule.UseVisualStyleBackColor = true;
            bttnSchedule.Click += BttnSchedule_Click;
            // 
            // chkDefaultIdentityCollection
            // 
            chkDefaultIdentityCollection.AutoSize = true;
            chkDefaultIdentityCollection.Checked = true;
            chkDefaultIdentityCollection.CheckState = System.Windows.Forms.CheckState.Checked;
            chkDefaultIdentityCollection.Location = new System.Drawing.Point(407, 37);
            chkDefaultIdentityCollection.Name = "chkDefaultIdentityCollection";
            chkDefaultIdentityCollection.Size = new System.Drawing.Size(80, 24);
            chkDefaultIdentityCollection.TabIndex = 33;
            chkDefaultIdentityCollection.Text = "Default";
            chkDefaultIdentityCollection.UseVisualStyleBackColor = true;
            chkDefaultIdentityCollection.CheckedChanged += ChkDefaultIdentityCollection_CheckedChanged;
            // 
            // groupBox4
            // 
            groupBox4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox4.Controls.Add(chkScanAzureDB);
            groupBox4.Controls.Add(lblHHmm);
            groupBox4.Controls.Add(bttnScanNow);
            groupBox4.Controls.Add(numAzureScanInterval);
            groupBox4.Controls.Add(label11);
            groupBox4.Controls.Add(chkScanEvery);
            groupBox4.Controls.Add(label10);
            groupBox4.Location = new System.Drawing.Point(9, 285);
            groupBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox4.Size = new System.Drawing.Size(1113, 144);
            groupBox4.TabIndex = 30;
            groupBox4.TabStop = false;
            groupBox4.Text = "Azure DB";
            // 
            // lblHHmm
            // 
            lblHHmm.AutoSize = true;
            lblHHmm.Location = new System.Drawing.Point(247, 103);
            lblHHmm.Name = "lblHHmm";
            lblHHmm.Size = new System.Drawing.Size(63, 20);
            lblHHmm.TabIndex = 29;
            lblHHmm.Text = "00:00:00";
            // 
            // numAzureScanInterval
            // 
            numAzureScanInterval.Increment = new decimal(new int[] { 3600, 0, 0, 0 });
            numAzureScanInterval.Location = new System.Drawing.Point(247, 72);
            numAzureScanInterval.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            numAzureScanInterval.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numAzureScanInterval.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            numAzureScanInterval.Name = "numAzureScanInterval";
            numAzureScanInterval.Size = new System.Drawing.Size(95, 27);
            numAzureScanInterval.TabIndex = 28;
            numAzureScanInterval.ValueChanged += NumAzureScanInterval_ValueChanged;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(347, 75);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(62, 20);
            label11.TabIndex = 27;
            label11.Text = "seconds";
            // 
            // label10
            // 
            label10.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            label10.Location = new System.Drawing.Point(585, 37);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(504, 85);
            label10.TabIndex = 24;
            label10.Text = resources.GetString("label10.Text");
            // 
            // tabSource
            // 
            tabSource.Controls.Add(groupBox2);
            tabSource.Controls.Add(groupBox1);
            tabSource.Location = new System.Drawing.Point(4, 29);
            tabSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabSource.Name = "tabSource";
            tabSource.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabSource.Size = new System.Drawing.Size(1129, 732);
            tabSource.TabIndex = 0;
            tabSource.Text = "Source";
            tabSource.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox2.Controls.Add(tabSrcOptions);
            groupBox2.Controls.Add(bttnAdd);
            groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            groupBox2.Location = new System.Drawing.Point(10, 8);
            groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox2.Size = new System.Drawing.Size(1102, 311);
            groupBox2.TabIndex = 27;
            groupBox2.TabStop = false;
            groupBox2.Text = "Add New Connection";
            // 
            // tabSrcOptions
            // 
            tabSrcOptions.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tabSrcOptions.Controls.Add(tabGeneral);
            tabSrcOptions.Controls.Add(tabExtendedEvents);
            tabSrcOptions.Controls.Add(tabRunningQueries);
            tabSrcOptions.Controls.Add(tabAddConnectionOther);
            tabSrcOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            tabSrcOptions.Location = new System.Drawing.Point(16, 27);
            tabSrcOptions.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabSrcOptions.Name = "tabSrcOptions";
            tabSrcOptions.SelectedIndex = 0;
            tabSrcOptions.Size = new System.Drawing.Size(1067, 221);
            tabSrcOptions.TabIndex = 21;
            // 
            // tabGeneral
            // 
            tabGeneral.Controls.Add(pictureBox2);
            tabGeneral.Controls.Add(txtSource);
            tabGeneral.Controls.Add(bttnS3Src);
            tabGeneral.Controls.Add(bttnSrcFolder);
            tabGeneral.Controls.Add(bttnConnectSource);
            tabGeneral.Controls.Add(label1);
            tabGeneral.Location = new System.Drawing.Point(4, 29);
            tabGeneral.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabGeneral.Size = new System.Drawing.Size(1059, 188);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "General";
            tabGeneral.UseVisualStyleBackColor = true;
            // 
            // txtSource
            // 
            txtSource.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtSource.Location = new System.Drawing.Point(6, 35);
            txtSource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSource.Multiline = true;
            txtSource.Name = "txtSource";
            txtSource.Size = new System.Drawing.Size(945, 111);
            txtSource.TabIndex = 13;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(27, 8);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(56, 16);
            label1.TabIndex = 2;
            label1.Text = "Source: ";
            // 
            // tabExtendedEvents
            // 
            tabExtendedEvents.Controls.Add(pnlExtendedEvents);
            tabExtendedEvents.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            tabExtendedEvents.Location = new System.Drawing.Point(4, 29);
            tabExtendedEvents.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabExtendedEvents.Name = "tabExtendedEvents";
            tabExtendedEvents.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabExtendedEvents.Size = new System.Drawing.Size(1059, 188);
            tabExtendedEvents.TabIndex = 1;
            tabExtendedEvents.Text = "Extended Events";
            tabExtendedEvents.UseVisualStyleBackColor = true;
            // 
            // pnlExtendedEvents
            // 
            pnlExtendedEvents.Controls.Add(chkSlowQueryThreshold);
            pnlExtendedEvents.Controls.Add(chkDualSession);
            pnlExtendedEvents.Controls.Add(numSlowQueryThreshold);
            pnlExtendedEvents.Controls.Add(label9);
            pnlExtendedEvents.Controls.Add(lblSlow);
            pnlExtendedEvents.Controls.Add(chkPersistXESession);
            pnlExtendedEvents.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlExtendedEvents.Location = new System.Drawing.Point(3, 4);
            pnlExtendedEvents.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlExtendedEvents.Name = "pnlExtendedEvents";
            pnlExtendedEvents.Size = new System.Drawing.Size(1053, 180);
            pnlExtendedEvents.TabIndex = 18;
            // 
            // chkSlowQueryThreshold
            // 
            chkSlowQueryThreshold.AutoSize = true;
            chkSlowQueryThreshold.Location = new System.Drawing.Point(6, 4);
            chkSlowQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSlowQueryThreshold.Name = "chkSlowQueryThreshold";
            chkSlowQueryThreshold.Size = new System.Drawing.Size(263, 20);
            chkSlowQueryThreshold.TabIndex = 13;
            chkSlowQueryThreshold.Text = "Capture Slow Queries (Extended Event)";
            chkSlowQueryThreshold.UseVisualStyleBackColor = true;
            chkSlowQueryThreshold.CheckedChanged += ChkSlowQueryThreshold_CheckedChanged;
            // 
            // numSlowQueryThreshold
            // 
            numSlowQueryThreshold.Enabled = false;
            numSlowQueryThreshold.Increment = new decimal(new int[] { 1000, 0, 0, 0 });
            numSlowQueryThreshold.Location = new System.Drawing.Point(117, 108);
            numSlowQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            numSlowQueryThreshold.Maximum = new decimal(new int[] { 604800000, 0, 0, 0 });
            numSlowQueryThreshold.Minimum = new decimal(new int[] { 1, 0, 0, int.MinValue });
            numSlowQueryThreshold.Name = "numSlowQueryThreshold";
            numSlowQueryThreshold.Size = new System.Drawing.Size(173, 22);
            numSlowQueryThreshold.TabIndex = 12;
            numSlowQueryThreshold.Value = new decimal(new int[] { 1, 0, 0, int.MinValue });
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(6, 109);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(100, 16);
            label9.TabIndex = 16;
            label9.Text = "Threshold (ms):";
            // 
            // lblSlow
            // 
            lblSlow.AutoSize = true;
            lblSlow.Location = new System.Drawing.Point(6, 145);
            lblSlow.Name = "lblSlow";
            lblSlow.Size = new System.Drawing.Size(524, 16);
            lblSlow.TabIndex = 14;
            lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabled";
            // 
            // chkPersistXESession
            // 
            chkPersistXESession.AutoSize = true;
            chkPersistXESession.Enabled = false;
            chkPersistXESession.Location = new System.Drawing.Point(6, 37);
            chkPersistXESession.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkPersistXESession.Name = "chkPersistXESession";
            chkPersistXESession.Size = new System.Drawing.Size(347, 20);
            chkPersistXESession.TabIndex = 15;
            chkPersistXESession.Text = "Persist XE sessions (to allow for manual configuration)";
            chkPersistXESession.UseVisualStyleBackColor = true;
            // 
            // tabRunningQueries
            // 
            tabRunningQueries.Controls.Add(chkCollectPlans);
            tabRunningQueries.Controls.Add(grpRunningQueryThreshold);
            tabRunningQueries.Controls.Add(chkCollectSessionWaits);
            tabRunningQueries.Location = new System.Drawing.Point(4, 29);
            tabRunningQueries.Name = "tabRunningQueries";
            tabRunningQueries.Padding = new System.Windows.Forms.Padding(3);
            tabRunningQueries.Size = new System.Drawing.Size(1059, 188);
            tabRunningQueries.TabIndex = 4;
            tabRunningQueries.Text = "Running Queries";
            tabRunningQueries.UseVisualStyleBackColor = true;
            // 
            // chkCollectPlans
            // 
            chkCollectPlans.AutoSize = true;
            chkCollectPlans.Location = new System.Drawing.Point(9, 43);
            chkCollectPlans.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkCollectPlans.Name = "chkCollectPlans";
            chkCollectPlans.Size = new System.Drawing.Size(227, 20);
            chkCollectPlans.TabIndex = 13;
            chkCollectPlans.Text = "Collect Plans for Running Queries";
            chkCollectPlans.UseVisualStyleBackColor = true;
            chkCollectPlans.CheckedChanged += ChkCollectPlans_CheckedChanged;
            // 
            // grpRunningQueryThreshold
            // 
            grpRunningQueryThreshold.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpRunningQueryThreshold.Controls.Add(label15);
            grpRunningQueryThreshold.Controls.Add(txtDurationThreshold);
            grpRunningQueryThreshold.Controls.Add(txtCountThreshold);
            grpRunningQueryThreshold.Controls.Add(label17);
            grpRunningQueryThreshold.Controls.Add(label14);
            grpRunningQueryThreshold.Controls.Add(txtCPUThreshold);
            grpRunningQueryThreshold.Controls.Add(txtGrantThreshold);
            grpRunningQueryThreshold.Controls.Add(label16);
            grpRunningQueryThreshold.Enabled = false;
            grpRunningQueryThreshold.Location = new System.Drawing.Point(545, 16);
            grpRunningQueryThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            grpRunningQueryThreshold.Name = "grpRunningQueryThreshold";
            grpRunningQueryThreshold.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            grpRunningQueryThreshold.Size = new System.Drawing.Size(509, 132);
            grpRunningQueryThreshold.TabIndex = 22;
            grpRunningQueryThreshold.TabStop = false;
            grpRunningQueryThreshold.Text = "Running Queries Plan Collection Thresholds";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(6, 40);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(124, 16);
            label15.TabIndex = 17;
            label15.Text = "Duration Threshold:";
            // 
            // txtDurationThreshold
            // 
            txtDurationThreshold.Location = new System.Drawing.Point(146, 37);
            txtDurationThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtDurationThreshold.Name = "txtDurationThreshold";
            txtDurationThreshold.Size = new System.Drawing.Size(100, 22);
            txtDurationThreshold.TabIndex = 16;
            // 
            // txtCountThreshold
            // 
            txtCountThreshold.Location = new System.Drawing.Point(385, 79);
            txtCountThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtCountThreshold.Name = "txtCountThreshold";
            txtCountThreshold.Size = new System.Drawing.Size(100, 22);
            txtCountThreshold.TabIndex = 20;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new System.Drawing.Point(263, 83);
            label17.Name = "label17";
            label17.Size = new System.Drawing.Size(108, 16);
            label17.TabIndex = 21;
            label17.Text = "Count Threshold:";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(6, 83);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(102, 16);
            label14.TabIndex = 15;
            label14.Text = "CPU Threshold:";
            // 
            // txtCPUThreshold
            // 
            txtCPUThreshold.Location = new System.Drawing.Point(146, 79);
            txtCPUThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtCPUThreshold.Name = "txtCPUThreshold";
            txtCPUThreshold.Size = new System.Drawing.Size(100, 22);
            txtCPUThreshold.TabIndex = 14;
            // 
            // txtGrantThreshold
            // 
            txtGrantThreshold.Location = new System.Drawing.Point(385, 37);
            txtGrantThreshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtGrantThreshold.Name = "txtGrantThreshold";
            txtGrantThreshold.Size = new System.Drawing.Size(100, 22);
            txtGrantThreshold.TabIndex = 18;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(263, 40);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(106, 16);
            label16.TabIndex = 19;
            label16.Text = "Grant Threshold:";
            // 
            // tabAddConnectionOther
            // 
            tabAddConnectionOther.Controls.Add(chkWriteToSecondaryDestinations);
            tabAddConnectionOther.Controls.Add(lblIOCollectionLevel);
            tabAddConnectionOther.Controls.Add(cboIOLevel);
            tabAddConnectionOther.Controls.Add(chkScriptJobs);
            tabAddConnectionOther.Controls.Add(pictureBox3);
            tabAddConnectionOther.Controls.Add(lnkExample);
            tabAddConnectionOther.Controls.Add(lnkNone);
            tabAddConnectionOther.Controls.Add(lnkALL);
            tabAddConnectionOther.Controls.Add(lblSchemaSnapshotDBs);
            tabAddConnectionOther.Controls.Add(txtSnapshotDBs);
            tabAddConnectionOther.Controls.Add(chkNoWMI);
            tabAddConnectionOther.Location = new System.Drawing.Point(4, 29);
            tabAddConnectionOther.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabAddConnectionOther.Name = "tabAddConnectionOther";
            tabAddConnectionOther.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabAddConnectionOther.Size = new System.Drawing.Size(1059, 188);
            tabAddConnectionOther.TabIndex = 3;
            tabAddConnectionOther.Text = "Other";
            tabAddConnectionOther.UseVisualStyleBackColor = true;
            // 
            // chkWriteToSecondaryDestinations
            // 
            chkWriteToSecondaryDestinations.AutoSize = true;
            chkWriteToSecondaryDestinations.Checked = true;
            chkWriteToSecondaryDestinations.CheckState = System.Windows.Forms.CheckState.Checked;
            chkWriteToSecondaryDestinations.Location = new System.Drawing.Point(264, 8);
            chkWriteToSecondaryDestinations.Name = "chkWriteToSecondaryDestinations";
            chkWriteToSecondaryDestinations.Size = new System.Drawing.Size(216, 20);
            chkWriteToSecondaryDestinations.TabIndex = 31;
            chkWriteToSecondaryDestinations.Text = "Write to secondary destinations";
            chkWriteToSecondaryDestinations.UseVisualStyleBackColor = true;
            // 
            // lblIOCollectionLevel
            // 
            lblIOCollectionLevel.AutoSize = true;
            lblIOCollectionLevel.Location = new System.Drawing.Point(9, 49);
            lblIOCollectionLevel.Name = "lblIOCollectionLevel";
            lblIOCollectionLevel.Size = new System.Drawing.Size(121, 16);
            lblIOCollectionLevel.TabIndex = 30;
            lblIOCollectionLevel.Text = "IO Collection Level:";
            // 
            // cboIOLevel
            // 
            cboIOLevel.FormattingEnabled = true;
            cboIOLevel.Location = new System.Drawing.Point(224, 45);
            cboIOLevel.Name = "cboIOLevel";
            cboIOLevel.Size = new System.Drawing.Size(135, 24);
            cboIOLevel.TabIndex = 29;
            // 
            // lnkExample
            // 
            lnkExample.AutoSize = true;
            lnkExample.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkExample.Location = new System.Drawing.Point(613, 115);
            lnkExample.Name = "lnkExample";
            lnkExample.Size = new System.Drawing.Size(60, 16);
            lnkExample.TabIndex = 25;
            lnkExample.TabStop = true;
            lnkExample.Text = "Example";
            lnkExample.LinkClicked += LnkExample_LinkClicked;
            // 
            // lnkNone
            // 
            lnkNone.AutoSize = true;
            lnkNone.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkNone.Location = new System.Drawing.Point(565, 115);
            lnkNone.Name = "lnkNone";
            lnkNone.Size = new System.Drawing.Size(40, 16);
            lnkNone.TabIndex = 24;
            lnkNone.TabStop = true;
            lnkNone.Text = "None";
            lnkNone.LinkClicked += LnkNone_LinkClicked;
            // 
            // lnkALL
            // 
            lnkALL.AutoSize = true;
            lnkALL.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkALL.Location = new System.Drawing.Point(535, 115);
            lnkALL.Name = "lnkALL";
            lnkALL.Size = new System.Drawing.Size(22, 16);
            lnkALL.TabIndex = 23;
            lnkALL.TabStop = true;
            lnkALL.Text = "All";
            lnkALL.LinkClicked += LnkALL_LinkClicked;
            // 
            // lblSchemaSnapshotDBs
            // 
            lblSchemaSnapshotDBs.AutoSize = true;
            lblSchemaSnapshotDBs.Location = new System.Drawing.Point(9, 91);
            lblSchemaSnapshotDBs.Name = "lblSchemaSnapshotDBs";
            lblSchemaSnapshotDBs.Size = new System.Drawing.Size(190, 16);
            lblSchemaSnapshotDBs.TabIndex = 20;
            lblSchemaSnapshotDBs.Text = "Schema Snapshot Databases:";
            // 
            // txtSnapshotDBs
            // 
            txtSnapshotDBs.Location = new System.Drawing.Point(224, 88);
            txtSnapshotDBs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSnapshotDBs.Name = "txtSnapshotDBs";
            txtSnapshotDBs.Size = new System.Drawing.Size(449, 22);
            txtSnapshotDBs.TabIndex = 17;
            // 
            // chkNoWMI
            // 
            chkNoWMI.AutoSize = true;
            chkNoWMI.Location = new System.Drawing.Point(9, 8);
            chkNoWMI.Name = "chkNoWMI";
            chkNoWMI.Size = new System.Drawing.Size(115, 20);
            chkNoWMI.TabIndex = 6;
            chkNoWMI.Text = "Don't use WMI";
            chkNoWMI.UseVisualStyleBackColor = true;
            // 
            // bttnAdd
            // 
            bttnAdd.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnAdd.Location = new System.Drawing.Point(981, 253);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(103, 37);
            bttnAdd.TabIndex = 8;
            bttnAdd.Text = "Add/Update";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += BttnAdd_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(dgvConnections);
            groupBox1.Controls.Add(label13);
            groupBox1.Controls.Add(txtSearch);
            groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            groupBox1.Location = new System.Drawing.Point(10, 325);
            groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox1.Size = new System.Drawing.Size(1102, 384);
            groupBox1.TabIndex = 26;
            groupBox1.TabStop = false;
            groupBox1.Text = "Existing Connections";
            // 
            // dgvConnections
            // 
            dgvConnections.AllowUserToAddRows = false;
            dgvConnections.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvConnections.BackgroundColor = System.Drawing.Color.White;
            dgvConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConnections.Location = new System.Drawing.Point(16, 27);
            dgvConnections.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvConnections.Name = "dgvConnections";
            dgvConnections.RowHeadersWidth = 51;
            dgvConnections.RowTemplate.Height = 24;
            dgvConnections.Size = new System.Drawing.Size(1067, 315);
            dgvConnections.TabIndex = 23;
            dgvConnections.CellContentClick += DgvConnections_CellContentClick;
            dgvConnections.RowsAdded += Dgv_RowsAdded;
            dgvConnections.RowValidated += Dgv_RowValidated;
            dgvConnections.UserDeletedRow += Dgv_UserDeletedRow;
            // 
            // label13
            // 
            label13.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            label13.AutoSize = true;
            label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            label13.Location = new System.Drawing.Point(830, 353);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(53, 16);
            label13.TabIndex = 25;
            label13.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            txtSearch.Location = new System.Drawing.Point(893, 349);
            txtSearch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(191, 22);
            txtSearch.TabIndex = 24;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            // 
            // tabDest
            // 
            tabDest.Controls.Add(lblServiceWarning);
            tabDest.Controls.Add(bttnAbout);
            tabDest.Controls.Add(groupBox5);
            tabDest.Controls.Add(bttnS3);
            tabDest.Controls.Add(bttnDestFolder);
            tabDest.Controls.Add(chkAutoUpgradeRepoDB);
            tabDest.Controls.Add(bttnConnect);
            tabDest.Controls.Add(lblVersionInfo);
            tabDest.Controls.Add(bttnDeployDatabase);
            tabDest.Controls.Add(label7);
            tabDest.Controls.Add(txtDestination);
            tabDest.Location = new System.Drawing.Point(4, 29);
            tabDest.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabDest.Name = "tabDest";
            tabDest.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabDest.Size = new System.Drawing.Size(1129, 732);
            tabDest.TabIndex = 2;
            tabDest.Text = "Destination:";
            tabDest.UseVisualStyleBackColor = true;
            // 
            // lblServiceWarning
            // 
            lblServiceWarning.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblServiceWarning.BackColor = System.Drawing.Color.FromArgb(228, 147, 37);
            lblServiceWarning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            lblServiceWarning.ForeColor = System.Drawing.Color.White;
            lblServiceWarning.Location = new System.Drawing.Point(9, 499);
            lblServiceWarning.Name = "lblServiceWarning";
            lblServiceWarning.Size = new System.Drawing.Size(1103, 47);
            lblServiceWarning.TabIndex = 24;
            lblServiceWarning.Text = "Warning: ";
            lblServiceWarning.Visible = false;
            // 
            // bttnAbout
            // 
            bttnAbout.Location = new System.Drawing.Point(103, 168);
            bttnAbout.Name = "bttnAbout";
            bttnAbout.Size = new System.Drawing.Size(214, 29);
            bttnAbout.TabIndex = 20;
            bttnAbout.Text = "Check for Updates";
            bttnAbout.UseVisualStyleBackColor = true;
            bttnAbout.Click += BttnAbout_Click;
            // 
            // groupBox5
            // 
            groupBox5.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox5.Controls.Add(bttnViewServiceLog);
            groupBox5.Controls.Add(lnkInstall);
            groupBox5.Controls.Add(lblServiceStatus);
            groupBox5.Controls.Add(lnkRefresh);
            groupBox5.Controls.Add(lnkStart);
            groupBox5.Controls.Add(lnkStop);
            groupBox5.Location = new System.Drawing.Point(9, 549);
            groupBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox5.Size = new System.Drawing.Size(1103, 140);
            groupBox5.TabIndex = 19;
            groupBox5.TabStop = false;
            groupBox5.Text = "Service";
            // 
            // bttnViewServiceLog
            // 
            bttnViewServiceLog.Location = new System.Drawing.Point(15, 88);
            bttnViewServiceLog.Name = "bttnViewServiceLog";
            bttnViewServiceLog.Size = new System.Drawing.Size(187, 29);
            bttnViewServiceLog.TabIndex = 20;
            bttnViewServiceLog.Text = "View Service Log";
            bttnViewServiceLog.UseVisualStyleBackColor = true;
            bttnViewServiceLog.Click += BttnViewServiceLog_Click;
            // 
            // lnkInstall
            // 
            lnkInstall.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkInstall.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lnkInstall.Image = Properties.Resources.install;
            lnkInstall.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lnkInstall.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkInstall.Location = new System.Drawing.Point(937, 43);
            lnkInstall.Name = "lnkInstall";
            lnkInstall.Size = new System.Drawing.Size(160, 21);
            lnkInstall.TabIndex = 22;
            lnkInstall.TabStop = true;
            lnkInstall.Text = "Install as service";
            lnkInstall.TextAlign = System.Drawing.ContentAlignment.TopRight;
            lnkInstall.LinkClicked += LnkInstall_LinkClicked;
            // 
            // lblServiceStatus
            // 
            lblServiceStatus.AutoSize = true;
            lblServiceStatus.Location = new System.Drawing.Point(15, 43);
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.Size = new System.Drawing.Size(100, 20);
            lblServiceStatus.TabIndex = 23;
            lblServiceStatus.Text = "Service Status";
            // 
            // chkAutoUpgradeRepoDB
            // 
            chkAutoUpgradeRepoDB.AutoSize = true;
            chkAutoUpgradeRepoDB.Checked = true;
            chkAutoUpgradeRepoDB.CheckState = System.Windows.Forms.CheckState.Checked;
            chkAutoUpgradeRepoDB.Location = new System.Drawing.Point(103, 60);
            chkAutoUpgradeRepoDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkAutoUpgradeRepoDB.Name = "chkAutoUpgradeRepoDB";
            chkAutoUpgradeRepoDB.Size = new System.Drawing.Size(321, 24);
            chkAutoUpgradeRepoDB.TabIndex = 8;
            chkAutoUpgradeRepoDB.Text = "Auto upgrade repository DB on service start";
            chkAutoUpgradeRepoDB.UseVisualStyleBackColor = true;
            chkAutoUpgradeRepoDB.CheckedChanged += ChkAutoUpgradeRepoDB_CheckedChanged;
            // 
            // lblVersionInfo
            // 
            lblVersionInfo.AutoSize = true;
            lblVersionInfo.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblVersionInfo.Location = new System.Drawing.Point(103, 127);
            lblVersionInfo.Name = "lblVersionInfo";
            lblVersionInfo.Size = new System.Drawing.Size(83, 16);
            lblVersionInfo.TabIndex = 6;
            lblVersionInfo.Text = "version info...";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(6, 25);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(88, 20);
            label7.TabIndex = 3;
            label7.Text = "Destination:";
            // 
            // txtDestination
            // 
            txtDestination.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtDestination.Location = new System.Drawing.Point(103, 25);
            txtDestination.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtDestination.Name = "txtDestination";
            txtDestination.Size = new System.Drawing.Size(903, 27);
            txtDestination.TabIndex = 1;
            txtDestination.Validated += TxtDestination_Validated;
            // 
            // tab1
            // 
            tab1.Controls.Add(tabDest);
            tab1.Controls.Add(tabSource);
            tab1.Controls.Add(tabOther);
            tab1.Controls.Add(tabJson);
            tab1.Dock = System.Windows.Forms.DockStyle.Fill;
            tab1.Location = new System.Drawing.Point(0, 0);
            tab1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tab1.Name = "tab1";
            tab1.SelectedIndex = 0;
            tab1.Size = new System.Drawing.Size(1137, 765);
            tab1.TabIndex = 22;
            // 
            // ServiceConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1137, 829);
            Controls.Add(tab1);
            Controls.Add(label5);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(842, 866);
            Name = "ServiceConfig";
            Text = "DBA Dash Service Config";
            FormClosing += ServiceConfig_FromClosing;
            Load += ServiceConfig_Load;
            ((System.ComponentModel.ISupportInitialize)errorProvider1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            tabJson.ResumeLayout(false);
            tabJson.PerformLayout();
            tabOther.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBackupRetention).EndInit();
            ((System.ComponentModel.ISupportInitialize)numIdentityCollectionThreshold).EndInit();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numAzureScanInterval).EndInit();
            tabSource.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            tabSrcOptions.ResumeLayout(false);
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            tabExtendedEvents.ResumeLayout(false);
            pnlExtendedEvents.ResumeLayout(false);
            pnlExtendedEvents.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSlowQueryThreshold).EndInit();
            tabRunningQueries.ResumeLayout(false);
            tabRunningQueries.PerformLayout();
            grpRunningQueryThreshold.ResumeLayout(false);
            grpRunningQueryThreshold.PerformLayout();
            tabAddConnectionOther.ResumeLayout(false);
            tabAddConnectionOther.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConnections).EndInit();
            tabDest.ResumeLayout(false);
            tabDest.PerformLayout();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            tab1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private ThemedTabControl tab1;
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
        private ThemedTabControl tabSrcOptions;
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

