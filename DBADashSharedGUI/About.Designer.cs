using System.Runtime.Versioning;

namespace DBADashGUI
{
    [SupportedOSPlatform("windows")]
    partial class About
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelCopyright = new System.Windows.Forms.Label();
            this.labelCompanyName = new System.Windows.Forms.Label();
            this.bttnOK = new System.Windows.Forms.Button();
            this.lnkDBADash = new System.Windows.Forms.LinkLabel();
            this.lblRepoVersion = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lnkLatestRelease = new System.Windows.Forms.LinkLabel();
            this.lblLatest = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblDeploymentType = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lnkAuthor = new System.Windows.Forms.LinkLabel();
            this.label7 = new System.Windows.Forms.Label();
            this.lnkLicense = new System.Windows.Forms.LinkLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.bttnUpgrade = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelVersion
            // 
            this.labelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelVersion.Location = new System.Drawing.Point(295, 0);
            this.labelVersion.MaximumSize = new System.Drawing.Size(0, 26);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(319, 26);
            this.labelVersion.TabIndex = 0;
            this.labelVersion.Text = "{Version}";
            this.labelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            this.labelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCopyright.Location = new System.Drawing.Point(295, 78);
            this.labelCopyright.MaximumSize = new System.Drawing.Size(0, 26);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(319, 26);
            this.labelCopyright.TabIndex = 21;
            this.labelCopyright.Text = "{Copyright}";
            this.labelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCompanyName
            // 
            this.labelCompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCompanyName.Location = new System.Drawing.Point(295, 104);
            this.labelCompanyName.MaximumSize = new System.Drawing.Size(0, 26);
            this.labelCompanyName.Name = "labelCompanyName";
            this.labelCompanyName.Size = new System.Drawing.Size(319, 26);
            this.labelCompanyName.TabIndex = 22;
            this.labelCompanyName.Text = "{Company}";
            this.labelCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bttnOK
            // 
            this.bttnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnOK.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.bttnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttnOK.ForeColor = System.Drawing.Color.Black;
            this.bttnOK.Location = new System.Drawing.Point(525, 306);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(115, 36);
            this.bttnOK.TabIndex = 24;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = false;
            // 
            // lnkDBADash
            // 
            this.lnkDBADash.AutoSize = true;
            this.lnkDBADash.Font = new System.Drawing.Font("Segoe UI", 20F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point);
            this.lnkDBADash.LinkColor = System.Drawing.Color.White;
            this.lnkDBADash.Location = new System.Drawing.Point(15, 14);
            this.lnkDBADash.Name = "lnkDBADash";
            this.lnkDBADash.Size = new System.Drawing.Size(182, 46);
            this.lnkDBADash.TabIndex = 25;
            this.lnkDBADash.TabStop = true;
            this.lnkDBADash.Text = "DBA Dash";
            this.lnkDBADash.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkDBADash_LinkClicked);
            // 
            // lblRepoVersion
            // 
            this.lblRepoVersion.AutoSize = true;
            this.lblRepoVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRepoVersion.Location = new System.Drawing.Point(295, 52);
            this.lblRepoVersion.Name = "lblRepoVersion";
            this.lblRepoVersion.Size = new System.Drawing.Size(319, 26);
            this.lblRepoVersion.TabIndex = 26;
            this.lblRepoVersion.Text = "{DB Version}";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(439, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(205, 28);
            this.label1.TabIndex = 27;
            this.label1.Text = "SQL Server Monitoring";
            // 
            // lnkLatestRelease
            // 
            this.lnkLatestRelease.AutoSize = true;
            this.lnkLatestRelease.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lnkLatestRelease.LinkColor = System.Drawing.Color.White;
            this.lnkLatestRelease.Location = new System.Drawing.Point(295, 26);
            this.lnkLatestRelease.Name = "lnkLatestRelease";
            this.lnkLatestRelease.Size = new System.Drawing.Size(319, 26);
            this.lnkLatestRelease.TabIndex = 28;
            this.lnkLatestRelease.TabStop = true;
            this.lnkLatestRelease.Text = "{Latest Release}";
            this.lnkLatestRelease.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkLatestRelease_LinkClicked);
            // 
            // lblLatest
            // 
            this.lblLatest.AutoSize = true;
            this.lblLatest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLatest.Location = new System.Drawing.Point(3, 26);
            this.lblLatest.Name = "lblLatest";
            this.lblLatest.Size = new System.Drawing.Size(286, 26);
            this.lblLatest.TabIndex = 29;
            this.lblLatest.Text = "Latest Version:";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47.3384F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52.6616F));
            this.tableLayoutPanel1.Controls.Add(this.lblDeploymentType, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.lnkLatestRelease, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblLatest, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelCompanyName, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelCopyright, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelVersion, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblRepoVersion, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.lnkAuthor, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.lnkLicense, 1, 7);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(23, 79);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(617, 204);
            this.tableLayoutPanel1.TabIndex = 30;
            // 
            // lblDeploymentType
            // 
            this.lblDeploymentType.AutoSize = true;
            this.lblDeploymentType.Location = new System.Drawing.Point(295, 156);
            this.lblDeploymentType.Name = "lblDeploymentType";
            this.lblDeploymentType.Size = new System.Drawing.Size(132, 20);
            this.lblDeploymentType.TabIndex = 35;
            this.lblDeploymentType.Text = "{DeploymentType}";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(3, 156);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(286, 26);
            this.label8.TabIndex = 35;
            this.label8.Text = "Deployment Type:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 20);
            this.label3.TabIndex = 30;
            this.label3.Text = "Version:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(84, 20);
            this.label4.TabIndex = 31;
            this.label4.Text = "DB Version:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 104);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(286, 26);
            this.label5.TabIndex = 32;
            this.label5.Text = "Company:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 78);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(77, 20);
            this.label6.TabIndex = 33;
            this.label6.Text = "Copyright:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(286, 26);
            this.label2.TabIndex = 34;
            this.label2.Text = "Created By:";
            // 
            // lnkAuthor
            // 
            this.lnkAuthor.AutoSize = true;
            this.lnkAuthor.LinkColor = System.Drawing.Color.White;
            this.lnkAuthor.Location = new System.Drawing.Point(295, 130);
            this.lnkAuthor.Name = "lnkAuthor";
            this.lnkAuthor.Size = new System.Drawing.Size(113, 20);
            this.lnkAuthor.TabIndex = 35;
            this.lnkAuthor.TabStop = true;
            this.lnkAuthor.Text = "David Wiseman";
            this.lnkAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkAuthor_LinkClicked);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 182);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 20);
            this.label7.TabIndex = 36;
            this.label7.Text = "License:";
            // 
            // lnkLicense
            // 
            this.lnkLicense.AutoSize = true;
            this.lnkLicense.LinkColor = System.Drawing.Color.White;
            this.lnkLicense.Location = new System.Drawing.Point(295, 182);
            this.lnkLicense.Name = "lnkLicense";
            this.lnkLicense.Size = new System.Drawing.Size(86, 20);
            this.lnkLicense.TabIndex = 37;
            this.lnkLicense.TabStop = true;
            this.lnkLicense.Text = "MIT License";
            this.lnkLicense.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkLicense_LinkClicked);
            // 
            // toolTip1
            // 
            this.toolTip1.ShowAlways = true;
            // 
            // bttnUpgrade
            // 
            this.bttnUpgrade.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttnUpgrade.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.bttnUpgrade.ForeColor = System.Drawing.Color.Black;
            this.bttnUpgrade.Location = new System.Drawing.Point(23, 306);
            this.bttnUpgrade.Name = "bttnUpgrade";
            this.bttnUpgrade.Size = new System.Drawing.Size(115, 36);
            this.bttnUpgrade.TabIndex = 31;
            this.bttnUpgrade.Text = "Upgrade";
            this.bttnUpgrade.UseVisualStyleBackColor = false;
            this.bttnUpgrade.Click += new System.EventHandler(this.BttnUpgrade_Click);
            // 
            // About
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(163)))));
            this.ClientSize = new System.Drawing.Size(659, 359);
            this.Controls.Add(this.bttnUpgrade);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lnkDBADash);
            this.Controls.Add(this.bttnOK);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "About";
            this.Padding = new System.Windows.Forms.Padding(12, 14, 12, 14);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Load += new System.EventHandler(this.About_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.Label labelCompanyName;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.LinkLabel lnkDBADash;
        private System.Windows.Forms.Label lblRepoVersion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel lnkLatestRelease;
        private System.Windows.Forms.Label lblLatest;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel lnkAuthor;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.LinkLabel lnkLicense;
        private System.Windows.Forms.Button bttnUpgrade;
        private System.Windows.Forms.Label lblDeploymentType;
        private System.Windows.Forms.Label label8;
    }
}
