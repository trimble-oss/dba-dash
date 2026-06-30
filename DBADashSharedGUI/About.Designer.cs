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
            components = new System.ComponentModel.Container();
            labelVersion = new Label();
            labelCopyright = new Label();
            labelCompanyName = new Label();
            bttnOK = new Button();
            lnkDBADash = new LinkLabel();
            lblRepoVersion = new Label();
            label1 = new Label();
            lnkLatestRelease = new LinkLabel();
            lblLatest = new Label();
            tableLayoutPanel1 = new TableLayoutPanel();
            lblDeploymentType = new Label();
            label8 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label2 = new Label();
            lnkAuthor = new LinkLabel();
            label7 = new Label();
            lnkLicense = new LinkLabel();
            toolTip1 = new ToolTip(components);
            bttnUpgrade = new Button();
            chkPreRelease = new CheckBox();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // labelVersion
            // 
            labelVersion.Dock = DockStyle.Fill;
            labelVersion.Location = new Point(295, 0);
            labelVersion.MaximumSize = new Size(0, 26);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new Size(319, 26);
            labelVersion.TabIndex = 0;
            labelVersion.Text = "{Version}";
            labelVersion.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            labelCopyright.Dock = DockStyle.Fill;
            labelCopyright.Location = new Point(295, 78);
            labelCopyright.MaximumSize = new Size(0, 26);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(319, 26);
            labelCopyright.TabIndex = 21;
            labelCopyright.Text = "{Copyright}";
            labelCopyright.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelCompanyName
            // 
            labelCompanyName.Dock = DockStyle.Fill;
            labelCompanyName.Location = new Point(295, 104);
            labelCompanyName.MaximumSize = new Size(0, 26);
            labelCompanyName.Name = "labelCompanyName";
            labelCompanyName.Size = new Size(319, 26);
            labelCompanyName.TabIndex = 22;
            labelCompanyName.Text = "{Company}";
            labelCompanyName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            bttnOK.BackColor = SystemColors.ButtonFace;
            bttnOK.DialogResult = DialogResult.OK;
            bttnOK.ForeColor = Color.Black;
            bttnOK.Location = new Point(525, 306);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new Size(115, 36);
            bttnOK.TabIndex = 24;
            bttnOK.Text = "OK";
            bttnOK.UseVisualStyleBackColor = false;
            // 
            // lnkDBADash
            // 
            lnkDBADash.AutoSize = true;
            lnkDBADash.Font = new Font("Segoe UI", 20F, FontStyle.Bold | FontStyle.Italic);
            lnkDBADash.LinkColor = Color.White;
            lnkDBADash.Location = new Point(15, 14);
            lnkDBADash.Name = "lnkDBADash";
            lnkDBADash.Size = new Size(182, 46);
            lnkDBADash.TabIndex = 25;
            lnkDBADash.TabStop = true;
            lnkDBADash.Text = "DBA Dash";
            lnkDBADash.LinkClicked += LnkDBADash_LinkClicked;
            // 
            // lblRepoVersion
            // 
            lblRepoVersion.AutoSize = true;
            lblRepoVersion.Dock = DockStyle.Fill;
            lblRepoVersion.Location = new Point(295, 52);
            lblRepoVersion.Name = "lblRepoVersion";
            lblRepoVersion.Size = new Size(319, 26);
            lblRepoVersion.TabIndex = 26;
            lblRepoVersion.Text = "{DB Version}";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Italic);
            label1.Location = new Point(439, 14);
            label1.Name = "label1";
            label1.Size = new Size(205, 28);
            label1.TabIndex = 27;
            label1.Text = "SQL Server Monitoring";
            // 
            // lnkLatestRelease
            // 
            lnkLatestRelease.AutoSize = true;
            lnkLatestRelease.Dock = DockStyle.Fill;
            lnkLatestRelease.LinkColor = Color.White;
            lnkLatestRelease.Location = new Point(295, 26);
            lnkLatestRelease.Name = "lnkLatestRelease";
            lnkLatestRelease.Size = new Size(319, 26);
            lnkLatestRelease.TabIndex = 28;
            lnkLatestRelease.TabStop = true;
            lnkLatestRelease.Text = "{Latest Release}";
            lnkLatestRelease.LinkClicked += LnkLatestRelease_LinkClicked;
            // 
            // lblLatest
            // 
            lblLatest.AutoSize = true;
            lblLatest.Dock = DockStyle.Fill;
            lblLatest.Location = new Point(3, 26);
            lblLatest.Name = "lblLatest";
            lblLatest.Size = new Size(286, 26);
            lblLatest.TabIndex = 29;
            lblLatest.Text = "Latest Version:";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47.3384F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52.6616F));
            tableLayoutPanel1.Controls.Add(lblDeploymentType, 1, 6);
            tableLayoutPanel1.Controls.Add(label8, 0, 6);
            tableLayoutPanel1.Controls.Add(lnkLatestRelease, 1, 1);
            tableLayoutPanel1.Controls.Add(lblLatest, 0, 1);
            tableLayoutPanel1.Controls.Add(labelCompanyName, 1, 4);
            tableLayoutPanel1.Controls.Add(labelCopyright, 1, 3);
            tableLayoutPanel1.Controls.Add(labelVersion, 1, 0);
            tableLayoutPanel1.Controls.Add(label3, 0, 0);
            tableLayoutPanel1.Controls.Add(lblRepoVersion, 1, 2);
            tableLayoutPanel1.Controls.Add(label4, 0, 2);
            tableLayoutPanel1.Controls.Add(label5, 0, 4);
            tableLayoutPanel1.Controls.Add(label6, 0, 3);
            tableLayoutPanel1.Controls.Add(label2, 0, 5);
            tableLayoutPanel1.Controls.Add(lnkAuthor, 1, 5);
            tableLayoutPanel1.Controls.Add(label7, 0, 7);
            tableLayoutPanel1.Controls.Add(lnkLicense, 1, 7);
            tableLayoutPanel1.Location = new Point(23, 79);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 8;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(617, 204);
            tableLayoutPanel1.TabIndex = 30;
            // 
            // lblDeploymentType
            // 
            lblDeploymentType.AutoSize = true;
            lblDeploymentType.Location = new Point(295, 156);
            lblDeploymentType.Name = "lblDeploymentType";
            lblDeploymentType.Size = new Size(132, 20);
            lblDeploymentType.TabIndex = 35;
            lblDeploymentType.Text = "{DeploymentType}";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Dock = DockStyle.Fill;
            label8.Location = new Point(3, 156);
            label8.Name = "label8";
            label8.Size = new Size(286, 26);
            label8.TabIndex = 35;
            label8.Text = "Deployment Type:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(3, 0);
            label3.Name = "label3";
            label3.Size = new Size(60, 20);
            label3.TabIndex = 30;
            label3.Text = "Version:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(3, 52);
            label4.Name = "label4";
            label4.Size = new Size(84, 20);
            label4.TabIndex = 31;
            label4.Text = "DB Version:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Dock = DockStyle.Fill;
            label5.Location = new Point(3, 104);
            label5.Name = "label5";
            label5.Size = new Size(286, 26);
            label5.TabIndex = 32;
            label5.Text = "Company:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(3, 78);
            label6.Name = "label6";
            label6.Size = new Size(77, 20);
            label6.TabIndex = 33;
            label6.Text = "Copyright:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(3, 130);
            label2.Name = "label2";
            label2.Size = new Size(286, 26);
            label2.TabIndex = 34;
            label2.Text = "Created By:";
            // 
            // lnkAuthor
            // 
            lnkAuthor.AutoSize = true;
            lnkAuthor.LinkColor = Color.White;
            lnkAuthor.Location = new Point(295, 130);
            lnkAuthor.Name = "lnkAuthor";
            lnkAuthor.Size = new Size(113, 20);
            lnkAuthor.TabIndex = 35;
            lnkAuthor.TabStop = true;
            lnkAuthor.Text = "David Wiseman";
            lnkAuthor.LinkClicked += LnkAuthor_LinkClicked;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(3, 182);
            label7.Name = "label7";
            label7.Size = new Size(60, 20);
            label7.TabIndex = 36;
            label7.Text = "License:";
            // 
            // lnkLicense
            // 
            lnkLicense.AutoSize = true;
            lnkLicense.LinkColor = Color.White;
            lnkLicense.Location = new Point(295, 182);
            lnkLicense.Name = "lnkLicense";
            lnkLicense.Size = new Size(86, 20);
            lnkLicense.TabIndex = 37;
            lnkLicense.TabStop = true;
            lnkLicense.Text = "MIT License";
            lnkLicense.LinkClicked += LnkLicense_LinkClicked;
            // 
            // toolTip1
            // 
            toolTip1.ShowAlways = true;
            // 
            // bttnUpgrade
            // 
            bttnUpgrade.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            bttnUpgrade.BackColor = SystemColors.ButtonFace;
            bttnUpgrade.ForeColor = Color.Black;
            bttnUpgrade.Location = new Point(23, 306);
            bttnUpgrade.Name = "bttnUpgrade";
            bttnUpgrade.Size = new Size(115, 36);
            bttnUpgrade.TabIndex = 31;
            bttnUpgrade.Text = "Upgrade";
            bttnUpgrade.UseVisualStyleBackColor = false;
            bttnUpgrade.Click += BttnUpgrade_Click;
            // 
            // chkPreRelease
            // 
            chkPreRelease.AutoSize = true;
            chkPreRelease.Location = new Point(155, 313);
            chkPreRelease.Name = "chkPreRelease";
            chkPreRelease.Size = new Size(229, 24);
            chkPreRelease.TabIndex = 32;
            chkPreRelease.Text = "Check for pre-release updates";
            chkPreRelease.UseVisualStyleBackColor = true;
            // 
            // About
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0, 99, 163);
            ClientSize = new Size(659, 359);
            Controls.Add(chkPreRelease);
            Controls.Add(bttnUpgrade);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(label1);
            Controls.Add(lnkDBADash);
            Controls.Add(bttnOK);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "About";
            Padding = new Padding(12, 14, 12, 14);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About";
            Load += About_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private CheckBox chkPreRelease;
    }
}
