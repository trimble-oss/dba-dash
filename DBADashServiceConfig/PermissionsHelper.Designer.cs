namespace DBADashServiceConfig
{
    partial class PermissionsHelper
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
            txtServiceAccount = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            errorProvider1 = new System.Windows.Forms.ErrorProvider(components);
            bttnGrant = new System.Windows.Forms.Button();
            dgvPermissions = new System.Windows.Forms.DataGridView();
            dgvInstances = new System.Windows.Forms.DataGridView();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            bttnClose = new System.Windows.Forms.Button();
            bttnLocalAdmin = new System.Windows.Forms.Button();
            chkRevokeLocalAdmin = new System.Windows.Forms.CheckBox();
            timer1 = new System.Windows.Forms.Timer(components);
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblProgress = new System.Windows.Forms.ToolStripStatusLabel();
            bttnGrantRepositoryDB = new System.Windows.Forms.Button();
            lnkViewRepositoryDBScript = new System.Windows.Forms.LinkLabel();
            lnkViewMonitoredInstanceScript = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvPermissions).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvInstances).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // txtServiceAccount
            // 
            txtServiceAccount.Location = new System.Drawing.Point(189, 34);
            txtServiceAccount.Name = "txtServiceAccount";
            txtServiceAccount.Size = new System.Drawing.Size(251, 27);
            txtServiceAccount.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(36, 37);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(117, 20);
            label1.TabIndex = 2;
            label1.Text = "Service Account:";
            // 
            // errorProvider1
            // 
            errorProvider1.ContainerControl = this;
            // 
            // bttnGrant
            // 
            bttnGrant.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnGrant.Location = new System.Drawing.Point(498, 609);
            bttnGrant.Name = "bttnGrant";
            bttnGrant.Size = new System.Drawing.Size(270, 29);
            bttnGrant.TabIndex = 6;
            bttnGrant.Text = "Grant Access to monitored instances";
            bttnGrant.UseVisualStyleBackColor = true;
            bttnGrant.Click += Grant_Click;
            // 
            // dgvPermissions
            // 
            dgvPermissions.AllowUserToAddRows = false;
            dgvPermissions.AllowUserToDeleteRows = false;
            dgvPermissions.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvPermissions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPermissions.Location = new System.Drawing.Point(36, 389);
            dgvPermissions.Name = "dgvPermissions";
            dgvPermissions.RowHeadersVisible = false;
            dgvPermissions.RowHeadersWidth = 51;
            dgvPermissions.Size = new System.Drawing.Size(732, 212);
            dgvPermissions.TabIndex = 7;
            // 
            // dgvInstances
            // 
            dgvInstances.AllowUserToAddRows = false;
            dgvInstances.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvInstances.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInstances.Location = new System.Drawing.Point(36, 104);
            dgvInstances.Name = "dgvInstances";
            dgvInstances.ReadOnly = true;
            dgvInstances.RowHeadersWidth = 51;
            dgvInstances.Size = new System.Drawing.Size(732, 208);
            dgvInstances.TabIndex = 8;
            dgvInstances.UserDeletedRow += Instances_UserDeletedRow;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(36, 81);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(72, 20);
            label2.TabIndex = 9;
            label2.Text = "Instances:";
            // 
            // label3
            // 
            label3.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(36, 366);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(88, 20);
            label3.TabIndex = 10;
            label3.Text = "Permissions:";
            // 
            // bttnClose
            // 
            bttnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnClose.Location = new System.Drawing.Point(398, 609);
            bttnClose.Name = "bttnClose";
            bttnClose.Size = new System.Drawing.Size(94, 29);
            bttnClose.TabIndex = 11;
            bttnClose.Text = "&Close";
            bttnClose.UseVisualStyleBackColor = true;
            // 
            // bttnLocalAdmin
            // 
            bttnLocalAdmin.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnLocalAdmin.Location = new System.Drawing.Point(498, 318);
            bttnLocalAdmin.Name = "bttnLocalAdmin";
            bttnLocalAdmin.Size = new System.Drawing.Size(270, 29);
            bttnLocalAdmin.TabIndex = 13;
            bttnLocalAdmin.Text = "Grant Local Admin (WMI only)";
            bttnLocalAdmin.UseVisualStyleBackColor = true;
            bttnLocalAdmin.Click += LocalAdmin_Click;
            // 
            // chkRevokeLocalAdmin
            // 
            chkRevokeLocalAdmin.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            chkRevokeLocalAdmin.AutoSize = true;
            chkRevokeLocalAdmin.Location = new System.Drawing.Point(277, 321);
            chkRevokeLocalAdmin.Name = "chkRevokeLocalAdmin";
            chkRevokeLocalAdmin.Size = new System.Drawing.Size(202, 24);
            chkRevokeLocalAdmin.TabIndex = 14;
            chkRevokeLocalAdmin.Text = "Revoke if WMI is disabled";
            chkRevokeLocalAdmin.UseVisualStyleBackColor = true;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += Timer_Tick;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblProgress });
            statusStrip1.Location = new System.Drawing.Point(0, 677);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(800, 24);
            statusStrip1.TabIndex = 16;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblProgress
            // 
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new System.Drawing.Size(0, 18);
            // 
            // bttnGrantRepositoryDB
            // 
            bttnGrantRepositoryDB.Location = new System.Drawing.Point(498, 33);
            bttnGrantRepositoryDB.Name = "bttnGrantRepositoryDB";
            bttnGrantRepositoryDB.Size = new System.Drawing.Size(270, 29);
            bttnGrantRepositoryDB.TabIndex = 17;
            bttnGrantRepositoryDB.Text = "Grant Access to repository database";
            bttnGrantRepositoryDB.UseVisualStyleBackColor = true;
            bttnGrantRepositoryDB.Click += GrantRepositoryDB_Click;
            // 
            // lnkViewRepositoryDBScript
            // 
            lnkViewRepositoryDBScript.AutoSize = true;
            lnkViewRepositoryDBScript.Location = new System.Drawing.Point(685, 65);
            lnkViewRepositoryDBScript.Name = "lnkViewRepositoryDBScript";
            lnkViewRepositoryDBScript.Size = new System.Drawing.Size(83, 20);
            lnkViewRepositoryDBScript.TabIndex = 18;
            lnkViewRepositoryDBScript.TabStop = true;
            lnkViewRepositoryDBScript.Text = "View Script";
            lnkViewRepositoryDBScript.LinkClicked += ViewRepositoryDBScript_Click;
            // 
            // lnkViewMonitoredInstanceScript
            // 
            lnkViewMonitoredInstanceScript.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            lnkViewMonitoredInstanceScript.AutoSize = true;
            lnkViewMonitoredInstanceScript.Location = new System.Drawing.Point(685, 641);
            lnkViewMonitoredInstanceScript.Name = "lnkViewMonitoredInstanceScript";
            lnkViewMonitoredInstanceScript.Size = new System.Drawing.Size(83, 20);
            lnkViewMonitoredInstanceScript.TabIndex = 19;
            lnkViewMonitoredInstanceScript.TabStop = true;
            lnkViewMonitoredInstanceScript.Text = "View Script";
            lnkViewMonitoredInstanceScript.LinkClicked += ViewMonitoredInstanceScript_Click;
            // 
            // PermissionsHelper
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnClose;
            ClientSize = new System.Drawing.Size(800, 701);
            Controls.Add(lnkViewMonitoredInstanceScript);
            Controls.Add(lnkViewRepositoryDBScript);
            Controls.Add(bttnGrantRepositoryDB);
            Controls.Add(statusStrip1);
            Controls.Add(chkRevokeLocalAdmin);
            Controls.Add(bttnLocalAdmin);
            Controls.Add(bttnClose);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(dgvInstances);
            Controls.Add(dgvPermissions);
            Controls.Add(bttnGrant);
            Controls.Add(label1);
            Controls.Add(txtServiceAccount);
            DoubleBuffered = true;
            Name = "PermissionsHelper";
            Text = "Permissions Helper";
            Load += PermissionsHelper_Load;
            ((System.ComponentModel.ISupportInitialize)errorProvider1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvPermissions).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvInstances).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtServiceAccount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button bttnGrant;
        private System.Windows.Forms.DataGridView dgvPermissions;
        private System.Windows.Forms.DataGridView dgvInstances;
        private System.Windows.Forms.Button bttnClose;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnViewScript;
        private System.Windows.Forms.Button bttnLocalAdmin;
        private System.Windows.Forms.CheckBox chkRevokeLocalAdmin;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblProgress;
        private System.Windows.Forms.Button bttnGrantRepositoryDB;
        private System.Windows.Forms.LinkLabel lnkViewMonitoredInstanceScript;
        private System.Windows.Forms.LinkLabel lnkViewRepositoryDBScript;
    }
}