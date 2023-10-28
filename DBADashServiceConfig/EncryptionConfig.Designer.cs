namespace DBADashServiceConfig
{
    partial class EncryptionConfig
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
            this.lnkGenerate = new System.Windows.Forms.LinkLabel();
            this.lnkShowHide = new System.Windows.Forms.LinkLabel();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.label19 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.grpEncryption = new System.Windows.Forms.GroupBox();
            this.chkEncrypt = new System.Windows.Forms.CheckBox();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.grpEncryption.SuspendLayout();
            this.SuspendLayout();
            // 
            // lnkGenerate
            // 
            this.lnkGenerate.AutoSize = true;
            this.lnkGenerate.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkGenerate.Location = new System.Drawing.Point(6, 98);
            this.lnkGenerate.Name = "lnkGenerate";
            this.lnkGenerate.Size = new System.Drawing.Size(69, 20);
            this.lnkGenerate.TabIndex = 12;
            this.lnkGenerate.TabStop = true;
            this.lnkGenerate.Text = "Generate";
            this.lnkGenerate.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkGenerate_LinkClicked);
            // 
            // lnkShowHide
            // 
            this.lnkShowHide.AutoSize = true;
            this.lnkShowHide.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkShowHide.Location = new System.Drawing.Point(377, 98);
            this.lnkShowHide.Name = "lnkShowHide";
            this.lnkShowHide.Size = new System.Drawing.Size(45, 20);
            this.lnkShowHide.TabIndex = 10;
            this.lnkShowHide.TabStop = true;
            this.lnkShowHide.Text = "Show";
            this.lnkShowHide.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkShowHide_LinkClicked);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(340, 230);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(94, 29);
            this.bttnUpdate.TabIndex = 9;
            this.bttnUpdate.Text = "&Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.BttnUpdate_Click);
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(6, 36);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(73, 20);
            this.label19.TabIndex = 8;
            this.label19.Text = "Password:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(6, 59);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(416, 27);
            this.txtPassword.TabIndex = 7;
            // 
            // grpEncryption
            // 
            this.grpEncryption.Controls.Add(this.label19);
            this.grpEncryption.Controls.Add(this.lnkGenerate);
            this.grpEncryption.Controls.Add(this.txtPassword);
            this.grpEncryption.Controls.Add(this.lnkShowHide);
            this.grpEncryption.Enabled = false;
            this.grpEncryption.Location = new System.Drawing.Point(12, 69);
            this.grpEncryption.Name = "grpEncryption";
            this.grpEncryption.Size = new System.Drawing.Size(445, 136);
            this.grpEncryption.TabIndex = 13;
            this.grpEncryption.TabStop = false;
            this.grpEncryption.Text = "Encryption Password";
            // 
            // chkEncrypt
            // 
            this.chkEncrypt.AutoSize = true;
            this.chkEncrypt.Location = new System.Drawing.Point(18, 25);
            this.chkEncrypt.Name = "chkEncrypt";
            this.chkEncrypt.Size = new System.Drawing.Size(80, 24);
            this.chkEncrypt.TabIndex = 14;
            this.chkEncrypt.Text = "Encrypt";
            this.chkEncrypt.UseVisualStyleBackColor = true;
            this.chkEncrypt.CheckedChanged += new System.EventHandler(this.ChkEncrypt_CheckedChanged);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(240, 230);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 15;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // EncryptionConfig
            // 
            this.AcceptButton = this.bttnUpdate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(473, 295);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.chkEncrypt);
            this.Controls.Add(this.grpEncryption);
            this.Controls.Add(this.bttnUpdate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "EncryptionConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Config File Encryption";
            this.grpEncryption.ResumeLayout(false);
            this.grpEncryption.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel lnkGenerate;
        private System.Windows.Forms.LinkLabel lnkShowHide;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.GroupBox grpEncryption;
        private System.Windows.Forms.CheckBox chkEncrypt;
        private System.Windows.Forms.Button bttnCancel;
    }
}