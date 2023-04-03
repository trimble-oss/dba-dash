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
            this.components = new System.ComponentModel.Container();
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.chkInherit = new System.Windows.Forms.CheckBox();
            this.chkWarning = new System.Windows.Forms.CheckBox();
            this.chkCritical = new System.Windows.Forms.CheckBox();
            this.lblObject = new System.Windows.Forms.Label();
            this.pnlConfig = new System.Windows.Forms.Panel();
            this.bttnUp = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.pnlConfig.SuspendLayout();
            this.SuspendLayout();
            // 
            // numWarning
            // 
            this.numWarning.Location = new System.Drawing.Point(174, 7);
            this.numWarning.Name = "numWarning";
            this.numWarning.Size = new System.Drawing.Size(150, 27);
            this.numWarning.TabIndex = 0;
            // 
            // numCritical
            // 
            this.numCritical.Location = new System.Drawing.Point(174, 51);
            this.numCritical.Name = "numCritical";
            this.numCritical.Size = new System.Drawing.Size(150, 27);
            this.numCritical.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(149, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Warning Threshold %";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Critical Threshold %";
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(311, 153);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(94, 29);
            this.bttnUpdate.TabIndex = 4;
            this.bttnUpdate.Text = "&Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.BttnUpdate_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(211, 153);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 5;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // chkInherit
            // 
            this.chkInherit.AutoSize = true;
            this.chkInherit.Enabled = false;
            this.chkInherit.Location = new System.Drawing.Point(342, 12);
            this.chkInherit.Name = "chkInherit";
            this.chkInherit.Size = new System.Drawing.Size(73, 24);
            this.chkInherit.TabIndex = 6;
            this.chkInherit.Text = "Inherit";
            this.chkInherit.UseVisualStyleBackColor = true;
            this.chkInherit.CheckedChanged += new System.EventHandler(this.ChkInherit_CheckedChanged);
            // 
            // chkWarning
            // 
            this.chkWarning.AutoSize = true;
            this.chkWarning.Checked = true;
            this.chkWarning.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWarning.Location = new System.Drawing.Point(330, 12);
            this.chkWarning.Name = "chkWarning";
            this.chkWarning.Size = new System.Drawing.Size(18, 17);
            this.chkWarning.TabIndex = 7;
            this.toolTip1.SetToolTip(this.chkWarning, "Enable/Disable check");
            this.chkWarning.UseVisualStyleBackColor = true;
            this.chkWarning.CheckedChanged += new System.EventHandler(this.ChkWarning_CheckedChanged);
            // 
            // chkCritical
            // 
            this.chkCritical.AutoSize = true;
            this.chkCritical.Checked = true;
            this.chkCritical.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCritical.Location = new System.Drawing.Point(330, 56);
            this.chkCritical.Name = "chkCritical";
            this.chkCritical.Size = new System.Drawing.Size(18, 17);
            this.chkCritical.TabIndex = 8;
            this.toolTip1.SetToolTip(this.chkCritical, "Enable/Disable check");
            this.chkCritical.UseVisualStyleBackColor = true;
            this.chkCritical.CheckedChanged += new System.EventHandler(this.ChkCritical_CheckedChanged);
            // 
            // lblObject
            // 
            this.lblObject.AutoSize = true;
            this.lblObject.Location = new System.Drawing.Point(49, 13);
            this.lblObject.Name = "lblObject";
            this.lblObject.Size = new System.Drawing.Size(51, 20);
            this.lblObject.TabIndex = 9;
            this.lblObject.Text = "{Root}";
            // 
            // pnlConfig
            // 
            this.pnlConfig.Controls.Add(this.label1);
            this.pnlConfig.Controls.Add(this.numWarning);
            this.pnlConfig.Controls.Add(this.chkCritical);
            this.pnlConfig.Controls.Add(this.numCritical);
            this.pnlConfig.Controls.Add(this.chkWarning);
            this.pnlConfig.Controls.Add(this.label2);
            this.pnlConfig.Location = new System.Drawing.Point(12, 36);
            this.pnlConfig.Name = "pnlConfig";
            this.pnlConfig.Size = new System.Drawing.Size(360, 98);
            this.pnlConfig.TabIndex = 10;
            // 
            // bttnUp
            // 
            this.bttnUp.Image = global::DBADashGUI.Properties.Resources.arrow_Up_16xLG;
            this.bttnUp.Location = new System.Drawing.Point(12, 7);
            this.bttnUp.Name = "bttnUp";
            this.bttnUp.Size = new System.Drawing.Size(31, 29);
            this.bttnUp.TabIndex = 11;
            this.toolTip1.SetToolTip(this.bttnUp, "Up Level");
            this.bttnUp.UseVisualStyleBackColor = true;
            this.bttnUp.Click += new System.EventHandler(this.BttnUp_Click);
            // 
            // IdentityColumnsThresholdConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 202);
            this.Controls.Add(this.bttnUp);
            this.Controls.Add(this.pnlConfig);
            this.Controls.Add(this.lblObject);
            this.Controls.Add(this.chkInherit);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "IdentityColumnsThresholdConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Identity Columns Threshold Configuration";
            this.Load += new System.EventHandler(this.IdentityColumnsThresholdConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.pnlConfig.ResumeLayout(false);
            this.pnlConfig.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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