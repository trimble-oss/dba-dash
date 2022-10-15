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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBConnection));
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.chkIntegratedSecurity = new System.Windows.Forms.CheckBox();
            this.pnlAuth = new System.Windows.Forms.Panel();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.bttnConnect = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.cboDatabase = new System.Windows.Forms.ComboBox();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.chkEncrypt = new System.Windows.Forms.CheckBox();
            this.chkTrustServerCert = new System.Windows.Forms.CheckBox();
            this.pnlAuth.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(113, 9);
            this.txtServerName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(358, 27);
            this.txtServerName.TabIndex = 0;
            this.txtServerName.Text = "localhost";
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(12, 9);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(97, 20);
            this.lblServer.TabIndex = 1;
            this.lblServer.Text = "Server Name:";
            // 
            // chkIntegratedSecurity
            // 
            this.chkIntegratedSecurity.AutoSize = true;
            this.chkIntegratedSecurity.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkIntegratedSecurity.Checked = true;
            this.chkIntegratedSecurity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIntegratedSecurity.Location = new System.Drawing.Point(315, 44);
            this.chkIntegratedSecurity.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkIntegratedSecurity.Name = "chkIntegratedSecurity";
            this.chkIntegratedSecurity.Size = new System.Drawing.Size(156, 24);
            this.chkIntegratedSecurity.TabIndex = 2;
            this.chkIntegratedSecurity.Text = "Integrated Security";
            this.chkIntegratedSecurity.UseVisualStyleBackColor = true;
            this.chkIntegratedSecurity.CheckedChanged += new System.EventHandler(this.ChkIntegratedSecurity_CheckedChanged);
            // 
            // pnlAuth
            // 
            this.pnlAuth.Controls.Add(this.txtPassword);
            this.pnlAuth.Controls.Add(this.label2);
            this.pnlAuth.Controls.Add(this.label1);
            this.pnlAuth.Controls.Add(this.txtUserName);
            this.pnlAuth.Enabled = false;
            this.pnlAuth.Location = new System.Drawing.Point(113, 107);
            this.pnlAuth.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlAuth.Name = "pnlAuth";
            this.pnlAuth.Size = new System.Drawing.Size(364, 110);
            this.pnlAuth.TabIndex = 3;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(122, 49);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(236, 27);
            this.txtPassword.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "User name";
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(122, 14);
            this.txtUserName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(236, 27);
            this.txtUserName.TabIndex = 0;
            // 
            // bttnConnect
            // 
            this.bttnConnect.Location = new System.Drawing.Point(296, 356);
            this.bttnConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnConnect.Name = "bttnConnect";
            this.bttnConnect.Size = new System.Drawing.Size(75, 29);
            this.bttnConnect.TabIndex = 4;
            this.bttnConnect.Text = "Connect";
            this.bttnConnect.UseVisualStyleBackColor = true;
            this.bttnConnect.Click += new System.EventHandler(this.BttnConnect_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttnCancel.Location = new System.Drawing.Point(405, 356);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 29);
            this.bttnCancel.TabIndex = 5;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // cboDatabase
            // 
            this.cboDatabase.FormattingEnabled = true;
            this.cboDatabase.Location = new System.Drawing.Point(113, 245);
            this.cboDatabase.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cboDatabase.Name = "cboDatabase";
            this.cboDatabase.Size = new System.Drawing.Size(358, 28);
            this.cboDatabase.TabIndex = 6;
            this.cboDatabase.DropDown += new System.EventHandler(this.CboDatabase_Dropdown);
            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(12, 249);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(75, 20);
            this.lblDatabase.TabIndex = 7;
            this.lblDatabase.Text = "Database:";
            // 
            // chkEncrypt
            // 
            this.chkEncrypt.AutoSize = true;
            this.chkEncrypt.Checked = true;
            this.chkEncrypt.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEncrypt.Location = new System.Drawing.Point(113, 280);
            this.chkEncrypt.Name = "chkEncrypt";
            this.chkEncrypt.Size = new System.Drawing.Size(159, 24);
            this.chkEncrypt.TabIndex = 8;
            this.chkEncrypt.Text = "Encrypt Connection";
            this.chkEncrypt.UseVisualStyleBackColor = true;
            // 
            // chkTrustServerCert
            // 
            this.chkTrustServerCert.AutoSize = true;
            this.chkTrustServerCert.Checked = true;
            this.chkTrustServerCert.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTrustServerCert.Location = new System.Drawing.Point(292, 280);
            this.chkTrustServerCert.Name = "chkTrustServerCert";
            this.chkTrustServerCert.Size = new System.Drawing.Size(179, 24);
            this.chkTrustServerCert.TabIndex = 9;
            this.chkTrustServerCert.Text = "Trust Server Certificate";
            this.chkTrustServerCert.UseVisualStyleBackColor = true;
            // 
            // DBConnection
            // 
            this.AcceptButton = this.bttnConnect;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(512, 400);
            this.Controls.Add(this.chkTrustServerCert);
            this.Controls.Add(this.chkEncrypt);
            this.Controls.Add(this.lblDatabase);
            this.Controls.Add(this.cboDatabase);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnConnect);
            this.Controls.Add(this.pnlAuth);
            this.Controls.Add(this.chkIntegratedSecurity);
            this.Controls.Add(this.lblServer);
            this.Controls.Add(this.txtServerName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "DBConnection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connect";
            this.Load += new System.EventHandler(this.DBConnection_Load);
            this.pnlAuth.ResumeLayout(false);
            this.pnlAuth.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.CheckBox chkIntegratedSecurity;
        private System.Windows.Forms.Panel pnlAuth;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Button bttnConnect;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.ComboBox cboDatabase;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.CheckBox chkEncrypt;
        private System.Windows.Forms.CheckBox chkTrustServerCert;
    }
}