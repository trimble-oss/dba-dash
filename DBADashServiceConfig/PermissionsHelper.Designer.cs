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
            bttnViewScript = new System.Windows.Forms.Button();
            bttnLocalAdmin = new System.Windows.Forms.Button();
            chkRevokeLocalAdmin = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvPermissions).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvInstances).BeginInit();
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
            bttnGrant.Location = new System.Drawing.Point(674, 615);
            bttnGrant.Name = "bttnGrant";
            bttnGrant.Size = new System.Drawing.Size(94, 29);
            bttnGrant.TabIndex = 6;
            bttnGrant.Text = "Grant";
            bttnGrant.UseVisualStyleBackColor = true;
            bttnGrant.Click += bttnGrant_Click;
            // 
            // dgvPermissions
            // 
            dgvPermissions.AllowUserToAddRows = false;
            dgvPermissions.AllowUserToDeleteRows = false;
            dgvPermissions.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvPermissions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPermissions.Location = new System.Drawing.Point(36, 364);
            dgvPermissions.Name = "dgvPermissions";
            dgvPermissions.RowHeadersVisible = false;
            dgvPermissions.RowHeadersWidth = 51;
            dgvPermissions.Size = new System.Drawing.Size(732, 235);
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
            dgvInstances.Size = new System.Drawing.Size(732, 183);
            dgvInstances.TabIndex = 8;
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
            label3.Location = new System.Drawing.Point(36, 341);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(88, 20);
            label3.TabIndex = 10;
            label3.Text = "Permissions:";
            // 
            // bttnClose
            // 
            bttnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnClose.Location = new System.Drawing.Point(574, 615);
            bttnClose.Name = "bttnClose";
            bttnClose.Size = new System.Drawing.Size(94, 29);
            bttnClose.TabIndex = 11;
            bttnClose.Text = "&Close";
            bttnClose.UseVisualStyleBackColor = true;
            // 
            // bttnViewScript
            // 
            bttnViewScript.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            bttnViewScript.Location = new System.Drawing.Point(36, 615);
            bttnViewScript.Name = "bttnViewScript";
            bttnViewScript.Size = new System.Drawing.Size(94, 29);
            bttnViewScript.TabIndex = 12;
            bttnViewScript.Text = "View Script";
            bttnViewScript.UseVisualStyleBackColor = true;
            bttnViewScript.Click += bttnViewScript_Click;
            // 
            // bttnLocalAdmin
            // 
            bttnLocalAdmin.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnLocalAdmin.Location = new System.Drawing.Point(530, 293);
            bttnLocalAdmin.Name = "bttnLocalAdmin";
            bttnLocalAdmin.Size = new System.Drawing.Size(238, 29);
            bttnLocalAdmin.TabIndex = 13;
            bttnLocalAdmin.Text = "Grant Local Admin (WMI only)";
            bttnLocalAdmin.UseVisualStyleBackColor = true;
            bttnLocalAdmin.Click += LocalAdmin_Click;
            // 
            // chkRevokeLocalAdmin
            // 
            chkRevokeLocalAdmin.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            chkRevokeLocalAdmin.AutoSize = true;
            chkRevokeLocalAdmin.Location = new System.Drawing.Point(322, 296);
            chkRevokeLocalAdmin.Name = "chkRevokeLocalAdmin";
            chkRevokeLocalAdmin.Size = new System.Drawing.Size(202, 24);
            chkRevokeLocalAdmin.TabIndex = 14;
            chkRevokeLocalAdmin.Text = "Revoke if WMI is disabled";
            chkRevokeLocalAdmin.UseVisualStyleBackColor = true;
            // 
            // PermissionsHelper
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnClose;
            ClientSize = new System.Drawing.Size(800, 676);
            Controls.Add(chkRevokeLocalAdmin);
            Controls.Add(bttnLocalAdmin);
            Controls.Add(bttnViewScript);
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
    }
}