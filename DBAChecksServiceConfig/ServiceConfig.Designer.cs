namespace DBAChecksServiceConfig
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
            this.cboSource = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtJson = new System.Windows.Forms.TextBox();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.chkNoWMI = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSecretKey = new System.Windows.Forms.TextBox();
            this.txtAccessKey = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAWSProfile = new System.Windows.Forms.TextBox();
            this.bttnSave = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.chkCustomizeSchedule = new System.Windows.Forms.CheckBox();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.bttnStart = new System.Windows.Forms.Button();
            this.bttnStop = new System.Windows.Forms.Button();
            this.bttnRefresh = new System.Windows.Forms.Button();
            this.bttnInstall = new System.Windows.Forms.Button();
            this.bttnUninstall = new System.Windows.Forms.Button();
            this.cboServiceCredentials = new System.Windows.Forms.ComboBox();
            this.lblServiceCredentials = new System.Windows.Forms.Label();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.chkCustomizeMaintenanceChron = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtDestination = new System.Windows.Forms.TextBox();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.bttnRemove = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.numSlowQueryThreshold = new System.Windows.Forms.NumericUpDown();
            this.chkSlowQueryThreshold = new System.Windows.Forms.CheckBox();
            this.lblSlow = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // cboSource
            // 
            this.cboSource.FormattingEnabled = true;
            this.cboSource.ItemHeight = 16;
            this.cboSource.Location = new System.Drawing.Point(9, 34);
            this.cboSource.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboSource.Name = "cboSource";
            this.cboSource.Size = new System.Drawing.Size(718, 24);
            this.cboSource.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source: ";
            // 
            // txtJson
            // 
            this.txtJson.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtJson.Location = new System.Drawing.Point(37, 283);
            this.txtJson.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtJson.Multiline = true;
            this.txtJson.Name = "txtJson";
            this.txtJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtJson.Size = new System.Drawing.Size(763, 330);
            this.txtJson.TabIndex = 13;
            this.txtJson.Validating += new System.ComponentModel.CancelEventHandler(this.txtJson_Validating);
            // 
            // bttnAdd
            // 
            this.bttnAdd.Location = new System.Drawing.Point(624, 86);
            this.bttnAdd.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(103, 23);
            this.bttnAdd.TabIndex = 8;
            this.bttnAdd.Text = "Add/Update";
            this.bttnAdd.UseVisualStyleBackColor = true;
            this.bttnAdd.Click += new System.EventHandler(this.bttnAdd_Click);
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // chkNoWMI
            // 
            this.chkNoWMI.AutoSize = true;
            this.chkNoWMI.Location = new System.Drawing.Point(442, 9);
            this.chkNoWMI.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkNoWMI.Name = "chkNoWMI";
            this.chkNoWMI.Size = new System.Drawing.Size(121, 21);
            this.chkNoWMI.TabIndex = 6;
            this.chkNoWMI.Text = "Don\'t use WMI";
            this.chkNoWMI.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(17, 85);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(148, 17);
            this.label6.TabIndex = 14;
            this.label6.Text = "Secret Key (Optional):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(152, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "Access Key (Optional):";
            // 
            // txtSecretKey
            // 
            this.txtSecretKey.Location = new System.Drawing.Point(176, 82);
            this.txtSecretKey.Name = "txtSecretKey";
            this.txtSecretKey.Size = new System.Drawing.Size(461, 22);
            this.txtSecretKey.TabIndex = 5;
            this.txtSecretKey.TextChanged += new System.EventHandler(this.txtSecretKey_TextChanged);
            this.txtSecretKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtSecretKey_Validating);
            // 
            // txtAccessKey
            // 
            this.txtAccessKey.Location = new System.Drawing.Point(176, 54);
            this.txtAccessKey.Name = "txtAccessKey";
            this.txtAccessKey.Size = new System.Drawing.Size(461, 22);
            this.txtAccessKey.TabIndex = 4;
            this.txtAccessKey.TextChanged += new System.EventHandler(this.txtAccessKey_TextChanged);
            this.txtAccessKey.Validating += new System.ComponentModel.CancelEventHandler(this.txtAccessKey_Validating);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 30);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(154, 17);
            this.label4.TabIndex = 8;
            this.label4.Text = "AWS Profile (Optional):";
            // 
            // txtAWSProfile
            // 
            this.txtAWSProfile.Location = new System.Drawing.Point(176, 27);
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
            this.bttnSave.Location = new System.Drawing.Point(587, 620);
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
            this.label5.Location = new System.Drawing.Point(37, 264);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Json:";
            // 
            // chkCustomizeSchedule
            // 
            this.chkCustomizeSchedule.AutoSize = true;
            this.chkCustomizeSchedule.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkCustomizeSchedule.Location = new System.Drawing.Point(569, 9);
            this.chkCustomizeSchedule.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkCustomizeSchedule.Name = "chkCustomizeSchedule";
            this.chkCustomizeSchedule.Size = new System.Drawing.Size(158, 21);
            this.chkCustomizeSchedule.TabIndex = 7;
            this.chkCustomizeSchedule.Text = "Customize Schedule";
            this.chkCustomizeSchedule.UseVisualStyleBackColor = true;
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
            this.bttnCancel.Location = new System.Drawing.Point(693, 620);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(100, 28);
            this.bttnCancel.TabIndex = 15;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(40, 25);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(763, 214);
            this.tabControl1.TabIndex = 22;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.chkCustomizeMaintenanceChron);
            this.tabPage3.Controls.Add(this.label7);
            this.tabPage3.Controls.Add(this.txtDestination);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(755, 185);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Destination:";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // chkCustomizeMaintenanceChron
            // 
            this.chkCustomizeMaintenanceChron.AutoSize = true;
            this.chkCustomizeMaintenanceChron.Location = new System.Drawing.Point(103, 49);
            this.chkCustomizeMaintenanceChron.Name = "chkCustomizeMaintenanceChron";
            this.chkCustomizeMaintenanceChron.Size = new System.Drawing.Size(222, 21);
            this.chkCustomizeMaintenanceChron.TabIndex = 4;
            this.chkCustomizeMaintenanceChron.Text = "Customize Maintenance Chron";
            this.chkCustomizeMaintenanceChron.UseVisualStyleBackColor = true;
            this.chkCustomizeMaintenanceChron.CheckedChanged += new System.EventHandler(this.chkCustomizeMaintenanceChron_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(82, 17);
            this.label7.TabIndex = 3;
            this.label7.Text = "Destinction:";
            // 
            // txtDestination
            // 
            this.txtDestination.Location = new System.Drawing.Point(103, 20);
            this.txtDestination.Name = "txtDestination";
            this.txtDestination.Size = new System.Drawing.Size(550, 22);
            this.txtDestination.TabIndex = 1;
            this.txtDestination.TextChanged += new System.EventHandler(this.txtDestination_TextChanged);
            this.txtDestination.Validated += new System.EventHandler(this.txtDestination_Validated);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.lblSlow);
            this.tabPage1.Controls.Add(this.chkSlowQueryThreshold);
            this.tabPage1.Controls.Add(this.numSlowQueryThreshold);
            this.tabPage1.Controls.Add(this.bttnRemove);
            this.tabPage1.Controls.Add(this.chkCustomizeSchedule);
            this.tabPage1.Controls.Add(this.bttnAdd);
            this.tabPage1.Controls.Add(this.cboSource);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.chkNoWMI);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(755, 185);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Source";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // bttnRemove
            // 
            this.bttnRemove.Location = new System.Drawing.Point(624, 113);
            this.bttnRemove.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnRemove.Name = "bttnRemove";
            this.bttnRemove.Size = new System.Drawing.Size(103, 23);
            this.bttnRemove.TabIndex = 9;
            this.bttnRemove.Text = "Remove";
            this.bttnRemove.UseVisualStyleBackColor = true;
            this.bttnRemove.Click += new System.EventHandler(this.bttnRemove_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.txtAWSProfile);
            this.tabPage2.Controls.Add(this.txtAccessKey);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.txtSecretKey);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(755, 185);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "AWS Credentials";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.cboServiceCredentials);
            this.tabPage4.Controls.Add(this.lblServiceCredentials);
            this.tabPage4.Controls.Add(this.lblServiceStatus);
            this.tabPage4.Controls.Add(this.bttnRefresh);
            this.tabPage4.Controls.Add(this.bttnUninstall);
            this.tabPage4.Controls.Add(this.bttnStart);
            this.tabPage4.Controls.Add(this.bttnInstall);
            this.tabPage4.Controls.Add(this.bttnStop);
            this.tabPage4.Location = new System.Drawing.Point(4, 25);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(755, 185);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Service";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // numSlowQueryThreshold
            // 
            this.numSlowQueryThreshold.Enabled = false;
            this.numSlowQueryThreshold.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numSlowQueryThreshold.Location = new System.Drawing.Point(9, 87);
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
            // chkSlowQueryThreshold
            // 
            this.chkSlowQueryThreshold.AutoSize = true;
            this.chkSlowQueryThreshold.Location = new System.Drawing.Point(9, 63);
            this.chkSlowQueryThreshold.Name = "chkSlowQueryThreshold";
            this.chkSlowQueryThreshold.Size = new System.Drawing.Size(206, 21);
            this.chkSlowQueryThreshold.TabIndex = 13;
            this.chkSlowQueryThreshold.Text = "Slow Query Threshold (ms):";
            this.chkSlowQueryThreshold.UseVisualStyleBackColor = true;
            this.chkSlowQueryThreshold.CheckedChanged += new System.EventHandler(this.chkSlowQueryThreshold_CheckedChanged);
            // 
            // lblSlow
            // 
            this.lblSlow.AutoSize = true;
            this.lblSlow.Location = new System.Drawing.Point(9, 116);
            this.lblSlow.Name = "lblSlow";
            this.lblSlow.Size = new System.Drawing.Size(557, 17);
            this.lblSlow.TabIndex = 14;
            this.lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabl" +
    "ed";
            // 
            // ServiceConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 657);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.bttnSave);
            this.Controls.Add(this.txtJson);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ServiceConfig";
            this.Text = "DBAChecks Service Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServiceConfig_FromClosing);
            this.Load += new System.EventHandler(this.ServiceConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSlowQueryThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cboSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtJson;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.CheckBox chkNoWMI;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAWSProfile;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chkCustomizeSchedule;
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
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Button bttnRemove;
        private System.Windows.Forms.CheckBox chkCustomizeMaintenanceChron;
        private System.Windows.Forms.NumericUpDown numSlowQueryThreshold;
        private System.Windows.Forms.CheckBox chkSlowQueryThreshold;
        private System.Windows.Forms.Label lblSlow;
    }
}

