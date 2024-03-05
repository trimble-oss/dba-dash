namespace DBADashGUI.Checks
{
    partial class IdentityColumnsThresholdConfig
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
            numWarning = new System.Windows.Forms.NumericUpDown();
            numCritical = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            bttnUpdate = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            chkInherit = new System.Windows.Forms.CheckBox();
            chkWarning = new System.Windows.Forms.CheckBox();
            chkCritical = new System.Windows.Forms.CheckBox();
            lblObject = new System.Windows.Forms.Label();
            pnlConfig = new System.Windows.Forms.Panel();
            bttnUp = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)numWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).BeginInit();
            pnlConfig.SuspendLayout();
            SuspendLayout();
            // 
            // numWarning
            // 
            numWarning.Location = new System.Drawing.Point(245, 8);
            numWarning.Name = "numWarning";
            numWarning.Size = new System.Drawing.Size(152, 27);
            numWarning.TabIndex = 0;
            // 
            // numCritical
            // 
            numCritical.Location = new System.Drawing.Point(245, 52);
            numCritical.Name = "numCritical";
            numCritical.Size = new System.Drawing.Size(152, 27);
            numCritical.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(10, 11);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(149, 20);
            label1.TabIndex = 2;
            label1.Text = "Warning Threshold %";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(10, 55);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(140, 20);
            label2.TabIndex = 3;
            label2.Text = "Critical Threshold %";
            // 
            // bttnUpdate
            // 
            bttnUpdate.Location = new System.Drawing.Point(329, 150);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(94, 29);
            bttnUpdate.TabIndex = 4;
            bttnUpdate.Text = "&Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(187, 150);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 5;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // chkInherit
            // 
            chkInherit.AutoSize = true;
            chkInherit.Enabled = false;
            chkInherit.Location = new System.Drawing.Point(15, 153);
            chkInherit.Name = "chkInherit";
            chkInherit.Size = new System.Drawing.Size(73, 24);
            chkInherit.TabIndex = 6;
            chkInherit.Text = "Inherit";
            chkInherit.UseVisualStyleBackColor = true;
            chkInherit.CheckedChanged += ChkInherit_CheckedChanged;
            // 
            // chkWarning
            // 
            chkWarning.AutoSize = true;
            chkWarning.Checked = true;
            chkWarning.CheckState = System.Windows.Forms.CheckState.Checked;
            chkWarning.Location = new System.Drawing.Point(403, 13);
            chkWarning.Name = "chkWarning";
            chkWarning.Size = new System.Drawing.Size(18, 17);
            chkWarning.TabIndex = 7;
            toolTip1.SetToolTip(chkWarning, "Enable/Disable check");
            chkWarning.UseVisualStyleBackColor = true;
            chkWarning.CheckedChanged += ChkWarning_CheckedChanged;
            // 
            // chkCritical
            // 
            chkCritical.AutoSize = true;
            chkCritical.Checked = true;
            chkCritical.CheckState = System.Windows.Forms.CheckState.Checked;
            chkCritical.Location = new System.Drawing.Point(403, 57);
            chkCritical.Name = "chkCritical";
            chkCritical.Size = new System.Drawing.Size(18, 17);
            chkCritical.TabIndex = 8;
            toolTip1.SetToolTip(chkCritical, "Enable/Disable check");
            chkCritical.UseVisualStyleBackColor = true;
            chkCritical.CheckedChanged += ChkCritical_CheckedChanged;
            // 
            // lblObject
            // 
            lblObject.Location = new System.Drawing.Point(49, 7);
            lblObject.Name = "lblObject";
            lblObject.Size = new System.Drawing.Size(383, 42);
            lblObject.TabIndex = 9;
            lblObject.Text = "{Root}";
            // 
            // pnlConfig
            // 
            pnlConfig.Controls.Add(label1);
            pnlConfig.Controls.Add(numWarning);
            pnlConfig.Controls.Add(chkCritical);
            pnlConfig.Controls.Add(numCritical);
            pnlConfig.Controls.Add(chkWarning);
            pnlConfig.Controls.Add(label2);
            pnlConfig.Location = new System.Drawing.Point(2, 52);
            pnlConfig.Name = "pnlConfig";
            pnlConfig.Size = new System.Drawing.Size(439, 88);
            pnlConfig.TabIndex = 10;
            // 
            // bttnUp
            // 
            bttnUp.Image = Properties.Resources.arrow_Up_16xLG;
            bttnUp.Location = new System.Drawing.Point(12, 7);
            bttnUp.Name = "bttnUp";
            bttnUp.Size = new System.Drawing.Size(31, 29);
            bttnUp.TabIndex = 11;
            toolTip1.SetToolTip(bttnUp, "Up Level");
            bttnUp.UseVisualStyleBackColor = true;
            bttnUp.Click += BttnUp_Click;
            // 
            // IdentityColumnsThresholdConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(444, 195);
            Controls.Add(bttnUp);
            Controls.Add(pnlConfig);
            Controls.Add(lblObject);
            Controls.Add(chkInherit);
            Controls.Add(bttnCancel);
            Controls.Add(bttnUpdate);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "IdentityColumnsThresholdConfig";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Identity Columns Threshold Configuration";
            Load += IdentityColumnsThresholdConfig_Load;
            ((System.ComponentModel.ISupportInitialize)numWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).EndInit();
            pnlConfig.ResumeLayout(false);
            pnlConfig.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.CheckBox chkInherit;
        private System.Windows.Forms.CheckBox chkWarning;
        private System.Windows.Forms.CheckBox chkCritical;
        private System.Windows.Forms.Label lblObject;
        private System.Windows.Forms.Panel pnlConfig;
        private System.Windows.Forms.Button bttnUp;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}