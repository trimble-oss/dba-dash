namespace DBADashGUI.DBFiles
{
    partial class FileThresholdConfig
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
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.pnlThresholds = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.lblDriveCritical = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDriveWarning = new System.Windows.Forms.Label();
            this.numDriveWarning = new System.Windows.Forms.NumericUpDown();
            this.numDriveCritical = new System.Windows.Forms.NumericUpDown();
            this.optInherit = new System.Windows.Forms.RadioButton();
            this.OptDisabled = new System.Windows.Forms.RadioButton();
            this.optMB = new System.Windows.Forms.RadioButton();
            this.optPercent = new System.Windows.Forms.RadioButton();
            this.pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(224, 158);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 23);
            this.bttnCancel.TabIndex = 41;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(305, 158);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 40;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Controls.Add(this.label6);
            this.pnlThresholds.Controls.Add(this.lblDriveCritical);
            this.pnlThresholds.Controls.Add(this.label5);
            this.pnlThresholds.Controls.Add(this.lblDriveWarning);
            this.pnlThresholds.Controls.Add(this.numDriveWarning);
            this.pnlThresholds.Controls.Add(this.numDriveCritical);
            this.pnlThresholds.Location = new System.Drawing.Point(13, 52);
            this.pnlThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(364, 86);
            this.pnlThresholds.TabIndex = 39;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 52);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 17);
            this.label6.TabIndex = 20;
            this.label6.Text = "Critical:";
            // 
            // lblDriveCritical
            // 
            this.lblDriveCritical.AutoSize = true;
            this.lblDriveCritical.Location = new System.Drawing.Point(244, 50);
            this.lblDriveCritical.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveCritical.Name = "lblDriveCritical";
            this.lblDriveCritical.Size = new System.Drawing.Size(20, 17);
            this.lblDriveCritical.TabIndex = 25;
            this.lblDriveCritical.Text = "%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(4, 16);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 17);
            this.label5.TabIndex = 19;
            this.label5.Text = "Warning:";
            // 
            // lblDriveWarning
            // 
            this.lblDriveWarning.AutoSize = true;
            this.lblDriveWarning.Location = new System.Drawing.Point(244, 18);
            this.lblDriveWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveWarning.Name = "lblDriveWarning";
            this.lblDriveWarning.Size = new System.Drawing.Size(20, 17);
            this.lblDriveWarning.TabIndex = 24;
            this.lblDriveWarning.Text = "%";
            // 
            // numDriveWarning
            // 
            this.numDriveWarning.DecimalPlaces = 1;
            this.numDriveWarning.Location = new System.Drawing.Point(76, 16);
            this.numDriveWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numDriveWarning.Name = "numDriveWarning";
            this.numDriveWarning.Size = new System.Drawing.Size(160, 22);
            this.numDriveWarning.TabIndex = 22;
            this.numDriveWarning.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // numDriveCritical
            // 
            this.numDriveCritical.DecimalPlaces = 1;
            this.numDriveCritical.Location = new System.Drawing.Point(76, 48);
            this.numDriveCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numDriveCritical.Name = "numDriveCritical";
            this.numDriveCritical.Size = new System.Drawing.Size(160, 22);
            this.numDriveCritical.TabIndex = 23;
            this.numDriveCritical.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // optInherit
            // 
            this.optInherit.AutoSize = true;
            this.optInherit.Location = new System.Drawing.Point(305, 13);
            this.optInherit.Margin = new System.Windows.Forms.Padding(4);
            this.optInherit.Name = "optInherit";
            this.optInherit.Size = new System.Drawing.Size(68, 21);
            this.optInherit.TabIndex = 38;
            this.optInherit.Text = "Inherit";
            this.optInherit.UseVisualStyleBackColor = true;
            this.optInherit.CheckedChanged += new System.EventHandler(this.optInherit_CheckedChanged);
            // 
            // OptDisabled
            // 
            this.OptDisabled.AutoSize = true;
            this.OptDisabled.Location = new System.Drawing.Point(189, 13);
            this.OptDisabled.Margin = new System.Windows.Forms.Padding(4);
            this.OptDisabled.Name = "OptDisabled";
            this.OptDisabled.Size = new System.Drawing.Size(84, 21);
            this.OptDisabled.TabIndex = 37;
            this.OptDisabled.Text = "Disabled";
            this.OptDisabled.UseVisualStyleBackColor = true;
            this.OptDisabled.CheckedChanged += new System.EventHandler(this.OptDisabled_CheckedChanged);
            // 
            // optMB
            // 
            this.optMB.AutoSize = true;
            this.optMB.Location = new System.Drawing.Point(118, 13);
            this.optMB.Margin = new System.Windows.Forms.Padding(4);
            this.optMB.Name = "optMB";
            this.optMB.Size = new System.Drawing.Size(49, 21);
            this.optMB.TabIndex = 36;
            this.optMB.Text = "MB";
            this.optMB.UseVisualStyleBackColor = true;
            this.optMB.CheckedChanged += new System.EventHandler(this.optMB_CheckedChanged);
            // 
            // optPercent
            // 
            this.optPercent.AutoSize = true;
            this.optPercent.Checked = true;
            this.optPercent.Location = new System.Drawing.Point(13, 13);
            this.optPercent.Margin = new System.Windows.Forms.Padding(4);
            this.optPercent.Name = "optPercent";
            this.optPercent.Size = new System.Drawing.Size(94, 21);
            this.optPercent.TabIndex = 35;
            this.optPercent.TabStop = true;
            this.optPercent.Text = "Percent %";
            this.optPercent.UseVisualStyleBackColor = true;
            this.optPercent.CheckedChanged += new System.EventHandler(this.optPercent_CheckedChanged);
            // 
            // FileThresholdConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(408, 214);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.pnlThresholds);
            this.Controls.Add(this.optInherit);
            this.Controls.Add(this.OptDisabled);
            this.Controls.Add(this.optMB);
            this.Controls.Add(this.optPercent);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FileThresholdConfig";
            this.Text = "File Threshold Config";
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDriveCritical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblDriveCritical;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblDriveWarning;
        private System.Windows.Forms.NumericUpDown numDriveWarning;
        private System.Windows.Forms.NumericUpDown numDriveCritical;
        private System.Windows.Forms.RadioButton optInherit;
        private System.Windows.Forms.RadioButton OptDisabled;
        private System.Windows.Forms.RadioButton optMB;
        private System.Windows.Forms.RadioButton optPercent;
    }
}