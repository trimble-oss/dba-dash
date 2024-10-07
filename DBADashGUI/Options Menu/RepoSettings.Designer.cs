namespace DBADashGUI.Options_Menu
{
    partial class RepoSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepoSettings));
            dgv = new System.Windows.Forms.DataGridView();
            bttnOK = new System.Windows.Forms.Button();
            chkSettingName = new System.Windows.Forms.CheckBox();
            bttnResetDefaults = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Location = new System.Drawing.Point(12, 12);
            dgv.Name = "dgv";
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(720, 353);
            dgv.TabIndex = 0;
            dgv.CellValueChanged += CellValueChanged;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.Location = new System.Drawing.Point(638, 378);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 1;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // chkSettingName
            // 
            chkSettingName.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            chkSettingName.AutoSize = true;
            chkSettingName.Location = new System.Drawing.Point(12, 378);
            chkSettingName.Name = "chkSettingName";
            chkSettingName.Size = new System.Drawing.Size(122, 24);
            chkSettingName.TabIndex = 2;
            chkSettingName.Text = "Setting Name";
            chkSettingName.UseVisualStyleBackColor = true;
            chkSettingName.CheckedChanged += ChkSettingName_CheckedChanged;
            // 
            // bttnResetDefaults
            // 
            bttnResetDefaults.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnResetDefaults.Location = new System.Drawing.Point(453, 378);
            bttnResetDefaults.Name = "bttnResetDefaults";
            bttnResetDefaults.Size = new System.Drawing.Size(152, 29);
            bttnResetDefaults.TabIndex = 3;
            bttnResetDefaults.Text = "Reset Defaults";
            bttnResetDefaults.UseVisualStyleBackColor = true;
            bttnResetDefaults.Click += BttnResetDefaults_Click;
            // 
            // RepoSettings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(744, 419);
            Controls.Add(bttnResetDefaults);
            Controls.Add(chkSettingName);
            Controls.Add(bttnOK);
            Controls.Add(dgv);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(450, 200);
            Name = "RepoSettings";
            Text = "Repository Settings";
            Load += Options_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.CheckBox chkSettingName;
        private System.Windows.Forms.Button bttnResetDefaults;
    }
}