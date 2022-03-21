
namespace DBADashServiceConfig
{
    partial class InstallService
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallService));
            this.lnkPermissions = new System.Windows.Forms.LinkLabel();
            this.cboServiceCredentials = new System.Windows.Forms.ComboBox();
            this.lblServiceCredentials = new System.Windows.Forms.Label();
            this.bttnInstall = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lnkPermissions
            // 
            this.lnkPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkPermissions.AutoSize = true;
            this.lnkPermissions.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkPermissions.Location = new System.Drawing.Point(34, 442);
            this.lnkPermissions.Name = "lnkPermissions";
            this.lnkPermissions.Size = new System.Drawing.Size(248, 20);
            this.lnkPermissions.TabIndex = 32;
            this.lnkPermissions.TabStop = true;
            this.lnkPermissions.Text = "Permissions required for service user";
            this.lnkPermissions.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkPermissions_LinkClicked);
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
            this.cboServiceCredentials.Location = new System.Drawing.Point(118, 22);
            this.cboServiceCredentials.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cboServiceCredentials.Name = "cboServiceCredentials";
            this.cboServiceCredentials.Size = new System.Drawing.Size(160, 28);
            this.cboServiceCredentials.TabIndex = 27;
            // 
            // lblServiceCredentials
            // 
            this.lblServiceCredentials.AutoSize = true;
            this.lblServiceCredentials.Location = new System.Drawing.Point(9, 31);
            this.lblServiceCredentials.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServiceCredentials.Name = "lblServiceCredentials";
            this.lblServiceCredentials.Size = new System.Drawing.Size(57, 20);
            this.lblServiceCredentials.TabIndex = 30;
            this.lblServiceCredentials.Text = "Run As:";
            // 
            // bttnInstall
            // 
            this.bttnInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnInstall.Location = new System.Drawing.Point(800, 442);
            this.bttnInstall.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bttnInstall.Name = "bttnInstall";
            this.bttnInstall.Size = new System.Drawing.Size(104, 44);
            this.bttnInstall.TabIndex = 28;
            this.bttnInstall.Text = "Install";
            this.bttnInstall.UseVisualStyleBackColor = true;
            this.bttnInstall.Click += new System.EventHandler(this.bttnInstall_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttnCancel.Location = new System.Drawing.Point(677, 442);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(104, 42);
            this.bttnCancel.TabIndex = 34;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtOutput.Location = new System.Drawing.Point(12, 68);
            this.txtOutput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtOutput.Size = new System.Drawing.Size(892, 354);
            this.txtOutput.TabIndex = 35;
            this.txtOutput.Text = resources.GetString("txtOutput.Text");
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBox1.Image = global::DBADashServiceConfig.Properties.Resources.Information_blue_6227_16x16_cyan;
            this.pictureBox1.Location = new System.Drawing.Point(12, 442);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(16, 16);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 33;
            this.pictureBox1.TabStop = false;
            // 
            // InstallService
            // 
            this.AcceptButton = this.bttnInstall;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(916, 500);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lnkPermissions);
            this.Controls.Add(this.cboServiceCredentials);
            this.Controls.Add(this.lblServiceCredentials);
            this.Controls.Add(this.bttnInstall);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "InstallService";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Install Service";
            this.Load += new System.EventHandler(this.InstallService_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.LinkLabel lnkPermissions;
        private System.Windows.Forms.ComboBox cboServiceCredentials;
        private System.Windows.Forms.Label lblServiceCredentials;
        private System.Windows.Forms.Button bttnInstall;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.TextBox txtOutput;
    }
}