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
            this.cboDestination = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtJson = new System.Windows.Forms.TextBox();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.chkNoWMI = new System.Windows.Forms.CheckBox();
            this.pnlAWS = new System.Windows.Forms.Panel();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblServiceCredentials = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bttnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.pnlAWS.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cboSource
            // 
            this.cboSource.FormattingEnabled = true;
            this.cboSource.ItemHeight = 13;
            this.cboSource.Location = new System.Drawing.Point(125, 13);
            this.cboSource.Margin = new System.Windows.Forms.Padding(2);
            this.cboSource.Name = "cboSource";
            this.cboSource.Size = new System.Drawing.Size(281, 21);
            this.cboSource.TabIndex = 0;
            this.cboSource.Validating += new System.ComponentModel.CancelEventHandler(this.cboSource_Validating);
            // 
            // cboDestination
            // 
            this.cboDestination.FormattingEnabled = true;
            this.cboDestination.Location = new System.Drawing.Point(125, 64);
            this.cboDestination.Margin = new System.Windows.Forms.Padding(2);
            this.cboDestination.Name = "cboDestination";
            this.cboDestination.Size = new System.Drawing.Size(281, 21);
            this.cboDestination.TabIndex = 9;
            this.cboDestination.Validating += new System.ComponentModel.CancelEventHandler(this.cboDestination_Validating);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 66);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Destination: ";
            // 
            // txtJson
            // 
            this.txtJson.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtJson.Location = new System.Drawing.Point(28, 296);
            this.txtJson.Margin = new System.Windows.Forms.Padding(2);
            this.txtJson.Multiline = true;
            this.txtJson.Name = "txtJson";
            this.txtJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtJson.Size = new System.Drawing.Size(486, 203);
            this.txtJson.TabIndex = 13;
            this.txtJson.Validating += new System.ComponentModel.CancelEventHandler(this.txtJson_Validating);
            // 
            // bttnAdd
            // 
            this.bttnAdd.Enabled = false;
            this.bttnAdd.Location = new System.Drawing.Point(430, 125);
            this.bttnAdd.Margin = new System.Windows.Forms.Padding(2);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(56, 19);
            this.bttnAdd.TabIndex = 12;
            this.bttnAdd.Text = "Add";
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
            this.chkNoWMI.Location = new System.Drawing.Point(314, 42);
            this.chkNoWMI.Margin = new System.Windows.Forms.Padding(2);
            this.chkNoWMI.Name = "chkNoWMI";
            this.chkNoWMI.Size = new System.Drawing.Size(97, 17);
            this.chkNoWMI.TabIndex = 8;
            this.chkNoWMI.Text = "Don\'t use WMI";
            this.chkNoWMI.UseVisualStyleBackColor = true;
            // 
            // pnlAWS
            // 
            this.pnlAWS.Controls.Add(this.label4);
            this.pnlAWS.Controls.Add(this.txtAWSProfile);
            this.pnlAWS.Location = new System.Drawing.Point(2, 88);
            this.pnlAWS.Margin = new System.Windows.Forms.Padding(2);
            this.pnlAWS.Name = "pnlAWS";
            this.pnlAWS.Size = new System.Drawing.Size(450, 24);
            this.pnlAWS.TabIndex = 10;
            this.pnlAWS.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 2);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(115, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "AWS Profile (Optional):";
            // 
            // txtAWSProfile
            // 
            this.txtAWSProfile.Location = new System.Drawing.Point(123, 2);
            this.txtAWSProfile.Margin = new System.Windows.Forms.Padding(2);
            this.txtAWSProfile.Name = "txtAWSProfile";
            this.txtAWSProfile.Size = new System.Drawing.Size(210, 20);
            this.txtAWSProfile.TabIndex = 10;
            // 
            // bttnSave
            // 
            this.bttnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnSave.Location = new System.Drawing.Point(353, 504);
            this.bttnSave.Margin = new System.Windows.Forms.Padding(2);
            this.bttnSave.Name = "bttnSave";
            this.bttnSave.Size = new System.Drawing.Size(75, 23);
            this.bttnSave.TabIndex = 14;
            this.bttnSave.Text = "&Save";
            this.bttnSave.UseVisualStyleBackColor = true;
            this.bttnSave.Click += new System.EventHandler(this.bttnSave_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(28, 281);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(32, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Json:";
            // 
            // chkCustomizeSchedule
            // 
            this.chkCustomizeSchedule.AutoSize = true;
            this.chkCustomizeSchedule.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkCustomizeSchedule.Location = new System.Drawing.Point(284, 125);
            this.chkCustomizeSchedule.Margin = new System.Windows.Forms.Padding(2);
            this.chkCustomizeSchedule.Name = "chkCustomizeSchedule";
            this.chkCustomizeSchedule.Size = new System.Drawing.Size(122, 17);
            this.chkCustomizeSchedule.TabIndex = 11;
            this.chkCustomizeSchedule.Text = "Customize Schedule";
            this.chkCustomizeSchedule.UseVisualStyleBackColor = true;
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Location = new System.Drawing.Point(4, 21);
            this.lblServiceStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(79, 13);
            this.lblServiceStatus.TabIndex = 14;
            this.lblServiceStatus.Text = "Service Status:";
            // 
            // bttnStart
            // 
            this.bttnStart.Location = new System.Drawing.Point(370, 18);
            this.bttnStart.Margin = new System.Windows.Forms.Padding(2);
            this.bttnStart.Name = "bttnStart";
            this.bttnStart.Size = new System.Drawing.Size(56, 19);
            this.bttnStart.TabIndex = 2;
            this.bttnStart.Text = "Start";
            this.bttnStart.UseVisualStyleBackColor = true;
            this.bttnStart.Click += new System.EventHandler(this.bttnStart_Click);
            // 
            // bttnStop
            // 
            this.bttnStop.Location = new System.Drawing.Point(430, 18);
            this.bttnStop.Margin = new System.Windows.Forms.Padding(2);
            this.bttnStop.Name = "bttnStop";
            this.bttnStop.Size = new System.Drawing.Size(56, 19);
            this.bttnStop.TabIndex = 3;
            this.bttnStop.Text = "Stop";
            this.bttnStop.UseVisualStyleBackColor = true;
            this.bttnStop.Click += new System.EventHandler(this.bttnStop_Click);
            // 
            // bttnRefresh
            // 
            this.bttnRefresh.Location = new System.Drawing.Point(310, 19);
            this.bttnRefresh.Margin = new System.Windows.Forms.Padding(2);
            this.bttnRefresh.Name = "bttnRefresh";
            this.bttnRefresh.Size = new System.Drawing.Size(56, 19);
            this.bttnRefresh.TabIndex = 1;
            this.bttnRefresh.Text = "Refresh";
            this.bttnRefresh.UseVisualStyleBackColor = true;
            this.bttnRefresh.Click += new System.EventHandler(this.bttnRefresh_Click);
            // 
            // bttnInstall
            // 
            this.bttnInstall.Location = new System.Drawing.Point(310, 47);
            this.bttnInstall.Margin = new System.Windows.Forms.Padding(2);
            this.bttnInstall.Name = "bttnInstall";
            this.bttnInstall.Size = new System.Drawing.Size(56, 19);
            this.bttnInstall.TabIndex = 5;
            this.bttnInstall.Text = "Install";
            this.bttnInstall.UseVisualStyleBackColor = true;
            this.bttnInstall.Click += new System.EventHandler(this.bttnInstall_Click);
            // 
            // bttnUninstall
            // 
            this.bttnUninstall.Location = new System.Drawing.Point(370, 47);
            this.bttnUninstall.Margin = new System.Windows.Forms.Padding(2);
            this.bttnUninstall.Name = "bttnUninstall";
            this.bttnUninstall.Size = new System.Drawing.Size(56, 19);
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
            this.cboServiceCredentials.Location = new System.Drawing.Point(125, 47);
            this.cboServiceCredentials.Name = "cboServiceCredentials";
            this.cboServiceCredentials.Size = new System.Drawing.Size(121, 21);
            this.cboServiceCredentials.TabIndex = 4;
            this.cboServiceCredentials.UseWaitCursor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblServiceCredentials);
            this.groupBox1.Controls.Add(this.lblServiceStatus);
            this.groupBox1.Controls.Add(this.cboServiceCredentials);
            this.groupBox1.Controls.Add(this.bttnStart);
            this.groupBox1.Controls.Add(this.bttnUninstall);
            this.groupBox1.Controls.Add(this.bttnStop);
            this.groupBox1.Controls.Add(this.bttnInstall);
            this.groupBox1.Controls.Add(this.bttnRefresh);
            this.groupBox1.Location = new System.Drawing.Point(22, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(491, 82);
            this.groupBox1.TabIndex = 21;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Service";
            // 
            // lblServiceCredentials
            // 
            this.lblServiceCredentials.AutoSize = true;
            this.lblServiceCredentials.Location = new System.Drawing.Point(6, 50);
            this.lblServiceCredentials.Name = "lblServiceCredentials";
            this.lblServiceCredentials.Size = new System.Drawing.Size(45, 13);
            this.lblServiceCredentials.TabIndex = 22;
            this.lblServiceCredentials.Text = "Run As:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.cboSource);
            this.groupBox2.Controls.Add(this.chkCustomizeSchedule);
            this.groupBox2.Controls.Add(this.cboDestination);
            this.groupBox2.Controls.Add(this.bttnAdd);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.chkNoWMI);
            this.groupBox2.Controls.Add(this.pnlAWS);
            this.groupBox2.Location = new System.Drawing.Point(22, 102);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(491, 162);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Add Connection";
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(433, 504);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 23);
            this.bttnCancel.TabIndex = 15;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            // 
            // ServiceConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(531, 534);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.bttnSave);
            this.Controls.Add(this.txtJson);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ServiceConfig";
            this.Text = "DBAChecks Service Config";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServiceConfig_FromClosing);
            this.Load += new System.EventHandler(this.ServiceConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.pnlAWS.ResumeLayout(false);
            this.pnlAWS.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cboSource;
        private System.Windows.Forms.ComboBox cboDestination;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtJson;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.CheckBox chkNoWMI;
        private System.Windows.Forms.Panel pnlAWS;
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
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblServiceCredentials;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

