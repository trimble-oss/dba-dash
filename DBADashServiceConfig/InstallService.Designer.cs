
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
            lnkPermissions = new System.Windows.Forms.LinkLabel();
            cboServiceCredentials = new System.Windows.Forms.ComboBox();
            lblServiceCredentials = new System.Windows.Forms.Label();
            bttnInstall = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            txtOutput = new System.Windows.Forms.TextBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lnkPermissions
            // 
            lnkPermissions.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lnkPermissions.AutoSize = true;
            lnkPermissions.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkPermissions.Location = new System.Drawing.Point(34, 442);
            lnkPermissions.Name = "lnkPermissions";
            lnkPermissions.Size = new System.Drawing.Size(248, 20);
            lnkPermissions.TabIndex = 32;
            lnkPermissions.TabStop = true;
            lnkPermissions.Text = "Permissions required for service user";
            lnkPermissions.LinkClicked += LnkPermissions_LinkClicked;
            // 
            // cboServiceCredentials
            // 
            cboServiceCredentials.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboServiceCredentials.FormattingEnabled = true;
            cboServiceCredentials.Items.AddRange(new object[] { "LocalSystem", "LocalService", "NetworkService", "User (prompt)" });
            cboServiceCredentials.Location = new System.Drawing.Point(118, 22);
            cboServiceCredentials.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            cboServiceCredentials.Name = "cboServiceCredentials";
            cboServiceCredentials.Size = new System.Drawing.Size(160, 28);
            cboServiceCredentials.TabIndex = 27;
            // 
            // lblServiceCredentials
            // 
            lblServiceCredentials.AutoSize = true;
            lblServiceCredentials.Location = new System.Drawing.Point(9, 31);
            lblServiceCredentials.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblServiceCredentials.Name = "lblServiceCredentials";
            lblServiceCredentials.Size = new System.Drawing.Size(57, 20);
            lblServiceCredentials.TabIndex = 30;
            lblServiceCredentials.Text = "Run As:";
            // 
            // bttnInstall
            // 
            bttnInstall.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnInstall.Location = new System.Drawing.Point(800, 442);
            bttnInstall.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            bttnInstall.Name = "bttnInstall";
            bttnInstall.Size = new System.Drawing.Size(104, 44);
            bttnInstall.TabIndex = 28;
            bttnInstall.Text = "Install";
            bttnInstall.UseVisualStyleBackColor = true;
            bttnInstall.Click += BttnInstall_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(677, 442);
            bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(104, 42);
            bttnCancel.TabIndex = 34;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            // 
            // txtOutput
            // 
            txtOutput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtOutput.Font = new System.Drawing.Font("Courier New", 10.2F);
            txtOutput.Location = new System.Drawing.Point(12, 68);
            txtOutput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtOutput.Size = new System.Drawing.Size(892, 354);
            txtOutput.TabIndex = 35;
            txtOutput.Text = resources.GetString("txtOutput.Text");
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            pictureBox1.Image = Properties.Resources.Information_blue_6227_16x16_cyan;
            pictureBox1.Location = new System.Drawing.Point(12, 442);
            pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(16, 16);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 33;
            pictureBox1.TabStop = false;
            // 
            // InstallService
            // 
            AcceptButton = bttnInstall;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(916, 500);
            Controls.Add(txtOutput);
            Controls.Add(bttnCancel);
            Controls.Add(pictureBox1);
            Controls.Add(lnkPermissions);
            Controls.Add(cboServiceCredentials);
            Controls.Add(lblServiceCredentials);
            Controls.Add(bttnInstall);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "InstallService";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Install Service";
            Load += InstallService_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
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