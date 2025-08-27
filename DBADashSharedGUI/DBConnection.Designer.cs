namespace DBADash
{
    partial class DBConnection
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBConnection));
            txtServerName = new TextBox();
            lblServer = new Label();
            txtPassword = new TextBox();
            lblPassword = new Label();
            lblUserName = new Label();
            txtUserName = new TextBox();
            bttnConnect = new Button();
            bttnCancel = new Button();
            cboDatabase = new ComboBox();
            lblDatabase = new Label();
            chkTrustServerCert = new CheckBox();
            label3 = new Label();
            cboAuthType = new ComboBox();
            cboEncryption = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            txtHostNameInCertificate = new TextBox();
            label9 = new Label();
            lnkOptions = new LinkLabel();
            bttnAdvanced = new Button();
            tab = new TabControl();
            tabBasic = new TabPage();
            tabAdvanced = new TabPage();
            propertyGrid1 = new PropertyGrid();
            panel1 = new Panel();
            toolTip1 = new ToolTip(components);
            tab.SuspendLayout();
            tabBasic.SuspendLayout();
            tabAdvanced.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtServerName
            // 
            txtServerName.Location = new Point(262, 28);
            txtServerName.Margin = new Padding(3, 4, 3, 4);
            txtServerName.Name = "txtServerName";
            txtServerName.Size = new Size(279, 27);
            txtServerName.TabIndex = 0;
            txtServerName.Text = "localhost";
            // 
            // lblServer
            // 
            lblServer.AutoSize = true;
            lblServer.Location = new Point(71, 31);
            lblServer.Name = "lblServer";
            lblServer.Size = new Size(97, 20);
            lblServer.TabIndex = 1;
            lblServer.Text = "Server Name:";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(262, 132);
            txtPassword.Margin = new Padding(3, 4, 3, 4);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(279, 27);
            txtPassword.TabIndex = 3;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(101, 135);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(73, 20);
            lblPassword.TabIndex = 3;
            lblPassword.Text = "Password:";
            // 
            // lblUserName
            // 
            lblUserName.AutoSize = true;
            lblUserName.Location = new Point(101, 100);
            lblUserName.Name = "lblUserName";
            lblUserName.Size = new Size(82, 20);
            lblUserName.TabIndex = 2;
            lblUserName.Text = "User name:";
            // 
            // txtUserName
            // 
            txtUserName.Location = new Point(262, 97);
            txtUserName.Margin = new Padding(3, 4, 3, 4);
            txtUserName.Name = "txtUserName";
            txtUserName.Size = new Size(279, 27);
            txtUserName.TabIndex = 2;
            // 
            // bttnConnect
            // 
            bttnConnect.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            bttnConnect.Location = new Point(367, 13);
            bttnConnect.Margin = new Padding(3, 4, 3, 4);
            bttnConnect.Name = "bttnConnect";
            bttnConnect.Size = new Size(94, 29);
            bttnConnect.TabIndex = 8;
            bttnConnect.Text = "Connect";
            bttnConnect.UseVisualStyleBackColor = true;
            bttnConnect.Click += BttnConnect_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            bttnCancel.DialogResult = DialogResult.Cancel;
            bttnCancel.Location = new Point(467, 13);
            bttnCancel.Margin = new Padding(3, 4, 3, 4);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new Size(94, 29);
            bttnCancel.TabIndex = 9;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // cboDatabase
            // 
            cboDatabase.FormattingEnabled = true;
            cboDatabase.Location = new Point(258, 369);
            cboDatabase.Margin = new Padding(3, 4, 3, 4);
            cboDatabase.Name = "cboDatabase";
            cboDatabase.Size = new Size(279, 28);
            cboDatabase.TabIndex = 7;
            cboDatabase.DropDown += CboDatabase_Dropdown;
            // 
            // lblDatabase
            // 
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new Point(67, 377);
            lblDatabase.Name = "lblDatabase";
            lblDatabase.Size = new Size(75, 20);
            lblDatabase.TabIndex = 7;
            lblDatabase.Text = "Database:";
            // 
            // chkTrustServerCert
            // 
            chkTrustServerCert.AutoSize = true;
            chkTrustServerCert.Location = new Point(262, 250);
            chkTrustServerCert.Name = "chkTrustServerCert";
            chkTrustServerCert.Size = new Size(179, 24);
            chkTrustServerCert.TabIndex = 5;
            chkTrustServerCert.Text = "Trust Server Certificate";
            chkTrustServerCert.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(71, 65);
            label3.Name = "label3";
            label3.Size = new Size(109, 20);
            label3.TabIndex = 10;
            label3.Text = "Authentication:";
            // 
            // cboAuthType
            // 
            cboAuthType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAuthType.FormattingEnabled = true;
            cboAuthType.Items.AddRange(new object[] { "Windows Authentication", "SQL Server Authentication", "Microsoft Entra MFA" });
            cboAuthType.Location = new Point(262, 62);
            cboAuthType.Name = "cboAuthType";
            cboAuthType.Size = new Size(279, 28);
            cboAuthType.TabIndex = 1;
            cboAuthType.SelectedIndexChanged += cboAuthType_SelectedIndexChanged;
            // 
            // cboEncryption
            // 
            cboEncryption.FormattingEnabled = true;
            cboEncryption.Location = new Point(262, 215);
            cboEncryption.Margin = new Padding(3, 4, 3, 4);
            cboEncryption.Name = "cboEncryption";
            cboEncryption.Size = new Size(279, 28);
            cboEncryption.TabIndex = 4;
            cboEncryption.SelectedIndexChanged += cboEncryption_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(71, 218);
            label1.Name = "label1";
            label1.Size = new Size(82, 20);
            label1.TabIndex = 13;
            label1.Text = "Encryption:";
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.Fixed3D;
            label2.Location = new Point(156, 187);
            label2.Name = "label2";
            label2.Size = new Size(402, 2);
            label2.TabIndex = 2;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.Fixed3D;
            label4.Location = new Point(58, 346);
            label4.Name = "label4";
            label4.Size = new Size(500, 2);
            label4.TabIndex = 16;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 178);
            label5.Name = "label5";
            label5.Size = new Size(140, 20);
            label5.TabIndex = 17;
            label5.Text = "Connection Security";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(6, 3);
            label6.Name = "label6";
            label6.Size = new Size(50, 20);
            label6.TabIndex = 18;
            label6.Text = "Server";
            // 
            // label7
            // 
            label7.BorderStyle = BorderStyle.Fixed3D;
            label7.Location = new Point(62, 12);
            label7.Name = "label7";
            label7.Size = new Size(496, 2);
            label7.TabIndex = 19;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(6, 337);
            label8.Name = "label8";
            label8.Size = new Size(46, 20);
            label8.TabIndex = 20;
            label8.Text = "Other";
            // 
            // txtHostNameInCertificate
            // 
            txtHostNameInCertificate.Location = new Point(262, 280);
            txtHostNameInCertificate.Name = "txtHostNameInCertificate";
            txtHostNameInCertificate.Size = new Size(279, 27);
            txtHostNameInCertificate.TabIndex = 6;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(71, 283);
            label9.Name = "label9";
            label9.Size = new Size(170, 20);
            label9.TabIndex = 22;
            label9.Text = "Host name in certificate:";
            // 
            // lnkOptions
            // 
            lnkOptions.AutoSize = true;
            lnkOptions.Location = new Point(255, 408);
            lnkOptions.Name = "lnkOptions";
            lnkOptions.Size = new Size(112, 20);
            lnkOptions.TabIndex = 23;
            lnkOptions.TabStop = true;
            lnkOptions.Text = "{Other Options}";
            lnkOptions.LinkClicked += lnkOptions_LinkClicked;
            // 
            // bttnAdvanced
            // 
            bttnAdvanced.Location = new Point(12, 13);
            bttnAdvanced.Name = "bttnAdvanced";
            bttnAdvanced.Size = new Size(94, 29);
            bttnAdvanced.TabIndex = 24;
            bttnAdvanced.Text = "Advanced";
            bttnAdvanced.UseVisualStyleBackColor = true;
            bttnAdvanced.Click += bttnAdvanced_Click;
            // 
            // tab
            // 
            tab.Appearance = TabAppearance.FlatButtons;
            tab.Controls.Add(tabBasic);
            tab.Controls.Add(tabAdvanced);
            tab.Dock = DockStyle.Fill;
            tab.ItemSize = new Size(0, 1);
            tab.Location = new Point(0, 0);
            tab.Name = "tab";
            tab.SelectedIndex = 0;
            tab.Size = new Size(588, 453);
            tab.SizeMode = TabSizeMode.Fixed;
            tab.TabIndex = 25;
            // 
            // tabBasic
            // 
            tabBasic.Controls.Add(label6);
            tabBasic.Controls.Add(txtServerName);
            tabBasic.Controls.Add(lnkOptions);
            tabBasic.Controls.Add(lblServer);
            tabBasic.Controls.Add(label9);
            tabBasic.Controls.Add(cboDatabase);
            tabBasic.Controls.Add(txtHostNameInCertificate);
            tabBasic.Controls.Add(lblDatabase);
            tabBasic.Controls.Add(label8);
            tabBasic.Controls.Add(lblUserName);
            tabBasic.Controls.Add(label7);
            tabBasic.Controls.Add(label3);
            tabBasic.Controls.Add(lblPassword);
            tabBasic.Controls.Add(label5);
            tabBasic.Controls.Add(txtUserName);
            tabBasic.Controls.Add(label4);
            tabBasic.Controls.Add(cboAuthType);
            tabBasic.Controls.Add(label1);
            tabBasic.Controls.Add(txtPassword);
            tabBasic.Controls.Add(label2);
            tabBasic.Controls.Add(cboEncryption);
            tabBasic.Controls.Add(chkTrustServerCert);
            tabBasic.Location = new Point(4, 5);
            tabBasic.Name = "tabBasic";
            tabBasic.Padding = new Padding(3);
            tabBasic.Size = new Size(580, 444);
            tabBasic.TabIndex = 0;
            tabBasic.Text = "tabPage1";
            tabBasic.UseVisualStyleBackColor = true;
            // 
            // tabAdvanced
            // 
            tabAdvanced.Controls.Add(propertyGrid1);
            tabAdvanced.Location = new Point(4, 5);
            tabAdvanced.Name = "tabAdvanced";
            tabAdvanced.Padding = new Padding(3);
            tabAdvanced.Size = new Size(580, 444);
            tabAdvanced.TabIndex = 1;
            tabAdvanced.Text = "Advanced";
            tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // propertyGrid1
            // 
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.Location = new Point(3, 3);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(574, 438);
            propertyGrid1.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(bttnConnect);
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnAdvanced);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 453);
            panel1.Name = "panel1";
            panel1.Size = new Size(588, 55);
            panel1.TabIndex = 26;
            // 
            // DBConnection
            // 
            AcceptButton = bttnConnect;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new Size(588, 508);
            Controls.Add(tab);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DBConnection";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect";
            Load += DBConnection_Load;
            tab.ResumeLayout(false);
            tabBasic.ResumeLayout(false);
            tabBasic.PerformLayout();
            tabAdvanced.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Button bttnConnect;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.ComboBox cboDatabase;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.CheckBox chkTrustServerCert;
        private Label label3;
        private ComboBox cboAuthType;
        private ComboBox cboEncryption;
        private Label label1;
        private Label label2;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private TextBox txtHostNameInCertificate;
        private Label label9;
        private LinkLabel lnkOptions;
        private Button bttnAdvanced;
        private TabControl tab;
        private TabPage tabBasic;
        private TabPage tabAdvanced;
        private Panel panel1;
        private PropertyGrid propertyGrid1;
        private ToolTip toolTip1;
    }
}