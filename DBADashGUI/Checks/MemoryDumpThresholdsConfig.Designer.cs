namespace DBADashGUI.Checks
{
    partial class MemoryDumpThresholdsConfig
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
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.chkWarning = new System.Windows.Forms.CheckBox();
            this.chkCritical = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.lblAckDate = new System.Windows.Forms.Label();
            this.lnkClear = new System.Windows.Forms.LinkLabel();
            this.lnkAcknowledge = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // numWarning
            // 
            this.numWarning.Location = new System.Drawing.Point(200, 24);
            this.numWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numWarning.Name = "numWarning";
            this.numWarning.Size = new System.Drawing.Size(150, 27);
            this.numWarning.TabIndex = 0;
            // 
            // numCritical
            // 
            this.numCritical.Location = new System.Drawing.Point(200, 59);
            this.numCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numCritical.Name = "numCritical";
            this.numCritical.Size = new System.Drawing.Size(150, 27);
            this.numCritical.TabIndex = 2;
            // 
            // chkWarning
            // 
            this.chkWarning.AutoSize = true;
            this.chkWarning.Location = new System.Drawing.Point(356, 29);
            this.chkWarning.Name = "chkWarning";
            this.chkWarning.Size = new System.Drawing.Size(18, 17);
            this.chkWarning.TabIndex = 1;
            this.chkWarning.UseVisualStyleBackColor = true;
            this.chkWarning.CheckedChanged += new System.EventHandler(this.NumWarning_CheckChanged);
            // 
            // chkCritical
            // 
            this.chkCritical.AutoSize = true;
            this.chkCritical.Location = new System.Drawing.Point(356, 64);
            this.chkCritical.Name = "chkCritical";
            this.chkCritical.Size = new System.Drawing.Size(18, 17);
            this.chkCritical.TabIndex = 3;
            this.chkCritical.UseVisualStyleBackColor = true;
            this.chkCritical.CheckedChanged += new System.EventHandler(this.ChkCritical_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 59);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Critical Threshold (Hrs):";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(172, 20);
            this.label2.TabIndex = 5;
            this.label2.Text = "Warning Threshold (Hrs):";
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(180, 158);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 6;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(280, 158);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(94, 29);
            this.bttnUpdate.TabIndex = 7;
            this.bttnUpdate.Text = "&Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.BttnUpdate_Click);
            // 
            // lblAckDate
            // 
            this.lblAckDate.AutoSize = true;
            this.lblAckDate.Location = new System.Drawing.Point(200, 97);
            this.lblAckDate.Name = "lblAckDate";
            this.lblAckDate.Size = new System.Drawing.Size(75, 20);
            this.lblAckDate.TabIndex = 9;
            this.lblAckDate.Text = "{AckDate}";
            // 
            // lnkClear
            // 
            this.lnkClear.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkClear.Location = new System.Drawing.Point(356, 97);
            this.lnkClear.Name = "lnkClear";
            this.lnkClear.Size = new System.Drawing.Size(53, 20);
            this.lnkClear.TabIndex = 5;
            this.lnkClear.TabStop = true;
            this.lnkClear.Text = "Clear";
            this.lnkClear.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkClear_LinkClicked);
            // 
            // lnkAcknowledge
            // 
            this.lnkAcknowledge.AutoSize = true;
            this.lnkAcknowledge.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkAcknowledge.Location = new System.Drawing.Point(12, 97);
            this.lnkAcknowledge.Name = "lnkAcknowledge";
            this.lnkAcknowledge.Size = new System.Drawing.Size(99, 20);
            this.lnkAcknowledge.TabIndex = 4;
            this.lnkAcknowledge.TabStop = true;
            this.lnkAcknowledge.Text = "Acknowledge";
            this.lnkAcknowledge.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkAcknowledge_Click);
            // 
            // MemoryDumpThresholdsConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 199);
            this.Controls.Add(this.lnkAcknowledge);
            this.Controls.Add(this.lnkClear);
            this.Controls.Add(this.lblAckDate);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkCritical);
            this.Controls.Add(this.chkWarning);
            this.Controls.Add(this.numCritical);
            this.Controls.Add(this.numWarning);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MemoryDumpThresholdsConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Memory Dump Thresholds";
            this.Load += new System.EventHandler(this.MemoryDumpThresholdsConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.CheckBox chkWarning;
        private System.Windows.Forms.CheckBox chkCritical;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Label lblAckDate;
        private System.Windows.Forms.LinkLabel lnkClear;
        private System.Windows.Forms.LinkLabel lnkAcknowledge;
    }
}