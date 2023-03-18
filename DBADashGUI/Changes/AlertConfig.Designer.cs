namespace DBADashGUI.Changes
{
    partial class AlertConfig
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
            groupBox1 = new System.Windows.Forms.GroupBox();
            optNA = new System.Windows.Forms.RadioButton();
            optDefault = new System.Windows.Forms.RadioButton();
            optCritical = new System.Windows.Forms.RadioButton();
            optWarning = new System.Windows.Forms.RadioButton();
            numNotificationPeriodHrs = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            bttnUpdate = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            chkDefault = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numNotificationPeriodHrs).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(optNA);
            groupBox1.Controls.Add(optDefault);
            groupBox1.Controls.Add(optCritical);
            groupBox1.Controls.Add(optWarning);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(514, 105);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Alert Level";
            // 
            // optNA
            // 
            optNA.AutoSize = true;
            optNA.Location = new System.Drawing.Point(183, 44);
            optNA.Name = "optNA";
            optNA.Size = new System.Drawing.Size(130, 24);
            optNA.TabIndex = 3;
            optNA.TabStop = true;
            optNA.Text = "N/A (Disabled)";
            optNA.UseVisualStyleBackColor = true;
            // 
            // optDefault
            // 
            optDefault.AutoSize = true;
            optDefault.Location = new System.Drawing.Point(321, 44);
            optDefault.Name = "optDefault";
            optDefault.Size = new System.Drawing.Size(79, 24);
            optDefault.TabIndex = 2;
            optDefault.TabStop = true;
            optDefault.Text = "Default";
            optDefault.UseVisualStyleBackColor = true;
            // 
            // optCritical
            // 
            optCritical.AutoSize = true;
            optCritical.Location = new System.Drawing.Point(99, 44);
            optCritical.Name = "optCritical";
            optCritical.Size = new System.Drawing.Size(76, 24);
            optCritical.TabIndex = 1;
            optCritical.TabStop = true;
            optCritical.Text = "Critical";
            optCritical.UseVisualStyleBackColor = true;
            // 
            // optWarning
            // 
            optWarning.AutoSize = true;
            optWarning.Location = new System.Drawing.Point(6, 44);
            optWarning.Name = "optWarning";
            optWarning.Size = new System.Drawing.Size(85, 24);
            optWarning.TabIndex = 0;
            optWarning.TabStop = true;
            optWarning.Text = "Warning";
            optWarning.UseVisualStyleBackColor = true;
            // 
            // numNotificationPeriodHrs
            // 
            numNotificationPeriodHrs.Increment = new decimal(new int[] { 24, 0, 0, 0 });
            numNotificationPeriodHrs.Location = new System.Drawing.Point(290, 147);
            numNotificationPeriodHrs.Maximum = new decimal(new int[] { 32767, 0, 0, 0 });
            numNotificationPeriodHrs.Name = "numNotificationPeriodHrs";
            numNotificationPeriodHrs.Size = new System.Drawing.Size(150, 27);
            numNotificationPeriodHrs.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 150);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(206, 20);
            label1.TabIndex = 2;
            label1.Text = "Alert Notification Period (hrs):";
            // 
            // bttnUpdate
            // 
            bttnUpdate.Location = new System.Drawing.Point(432, 213);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(94, 29);
            bttnUpdate.TabIndex = 3;
            bttnUpdate.Text = "&Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(333, 213);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 4;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // chkDefault
            // 
            chkDefault.AutoSize = true;
            chkDefault.Location = new System.Drawing.Point(446, 148);
            chkDefault.Name = "chkDefault";
            chkDefault.Size = new System.Drawing.Size(80, 24);
            chkDefault.TabIndex = 5;
            chkDefault.Text = "Default";
            chkDefault.UseVisualStyleBackColor = true;
            chkDefault.CheckedChanged += ChkDefault_CheckedChanged;
            // 
            // AlertConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(544, 263);
            Controls.Add(chkDefault);
            Controls.Add(bttnCancel);
            Controls.Add(bttnUpdate);
            Controls.Add(label1);
            Controls.Add(numNotificationPeriodHrs);
            Controls.Add(groupBox1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "AlertConfig";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Alert Config";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numNotificationPeriodHrs).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton optDefault;
        private System.Windows.Forms.RadioButton optCritical;
        private System.Windows.Forms.RadioButton optWarning;
        private System.Windows.Forms.NumericUpDown numNotificationPeriodHrs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.CheckBox chkDefault;
        private System.Windows.Forms.RadioButton optNA;
    }
}