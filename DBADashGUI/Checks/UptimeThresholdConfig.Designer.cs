
namespace DBADashGUI.Checks
{
    partial class UptimeThresholdConfig
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
            bttnUpdate = new System.Windows.Forms.Button();
            pnlThresholds = new System.Windows.Forms.Panel();
            label23 = new System.Windows.Forms.Label();
            label24 = new System.Windows.Forms.Label();
            chkEnabled = new System.Windows.Forms.CheckBox();
            label25 = new System.Windows.Forms.Label();
            label26 = new System.Windows.Forms.Label();
            numWarning = new System.Windows.Forms.NumericUpDown();
            numCritical = new System.Windows.Forms.NumericUpDown();
            chkInherit = new System.Windows.Forms.CheckBox();
            lnkConfigureRoot = new System.Windows.Forms.LinkLabel();
            bttnClearAlert = new System.Windows.Forms.Button();
            lblUptime = new System.Windows.Forms.Label();
            lblStartTime = new System.Windows.Forms.Label();
            pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).BeginInit();
            SuspendLayout();
            // 
            // bttnUpdate
            // 
            bttnUpdate.Location = new System.Drawing.Point(466, 246);
            bttnUpdate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(87, 29);
            bttnUpdate.TabIndex = 46;
            bttnUpdate.Text = "Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // pnlThresholds
            // 
            pnlThresholds.Controls.Add(label23);
            pnlThresholds.Controls.Add(label24);
            pnlThresholds.Controls.Add(chkEnabled);
            pnlThresholds.Controls.Add(label25);
            pnlThresholds.Controls.Add(label26);
            pnlThresholds.Controls.Add(numWarning);
            pnlThresholds.Controls.Add(numCritical);
            pnlThresholds.Location = new System.Drawing.Point(15, 50);
            pnlThresholds.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pnlThresholds.Name = "pnlThresholds";
            pnlThresholds.Size = new System.Drawing.Size(538, 105);
            pnlThresholds.TabIndex = 45;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new System.Drawing.Point(288, 51);
            label23.Name = "label23";
            label23.Size = new System.Drawing.Size(40, 20);
            label23.TabIndex = 45;
            label23.Text = "mins";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new System.Drawing.Point(463, 51);
            label24.Name = "label24";
            label24.Size = new System.Drawing.Size(40, 20);
            label24.TabIndex = 44;
            label24.Text = "mins";
            // 
            // chkEnabled
            // 
            chkEnabled.AutoSize = true;
            chkEnabled.Checked = true;
            chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            chkEnabled.Location = new System.Drawing.Point(13, 51);
            chkEnabled.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkEnabled.Name = "chkEnabled";
            chkEnabled.Size = new System.Drawing.Size(85, 24);
            chkEnabled.TabIndex = 41;
            chkEnabled.Text = "Enabled";
            chkEnabled.UseVisualStyleBackColor = true;
            chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Location = new System.Drawing.Point(354, 11);
            label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label25.Name = "label25";
            label25.Size = new System.Drawing.Size(58, 20);
            label25.TabIndex = 20;
            label25.Text = "Critical:";
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new System.Drawing.Point(178, 12);
            label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label26.Name = "label26";
            label26.Size = new System.Drawing.Size(67, 20);
            label26.TabIndex = 19;
            label26.Text = "Warning:";
            // 
            // numWarning
            // 
            numWarning.Location = new System.Drawing.Point(181, 50);
            numWarning.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numWarning.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numWarning.Name = "numWarning";
            numWarning.Size = new System.Drawing.Size(100, 27);
            numWarning.TabIndex = 22;
            numWarning.Value = new decimal(new int[] { 2880, 0, 0, 0 });
            // 
            // numCritical
            // 
            numCritical.Location = new System.Drawing.Point(355, 50);
            numCritical.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numCritical.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numCritical.Name = "numCritical";
            numCritical.Size = new System.Drawing.Size(100, 27);
            numCritical.TabIndex = 23;
            numCritical.Value = new decimal(new int[] { 720, 0, 0, 0 });
            // 
            // chkInherit
            // 
            chkInherit.AutoSize = true;
            chkInherit.Location = new System.Drawing.Point(15, 15);
            chkInherit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkInherit.Name = "chkInherit";
            chkInherit.Size = new System.Drawing.Size(73, 24);
            chkInherit.TabIndex = 44;
            chkInherit.Text = "Inherit";
            chkInherit.UseVisualStyleBackColor = true;
            chkInherit.CheckedChanged += ChkInherit_CheckedChanged;
            // 
            // lnkConfigureRoot
            // 
            lnkConfigureRoot.AutoSize = true;
            lnkConfigureRoot.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkConfigureRoot.Location = new System.Drawing.Point(403, 15);
            lnkConfigureRoot.Name = "lnkConfigureRoot";
            lnkConfigureRoot.Size = new System.Drawing.Size(158, 20);
            lnkConfigureRoot.TabIndex = 47;
            lnkConfigureRoot.TabStop = true;
            lnkConfigureRoot.Text = "Root Threshold Config";
            lnkConfigureRoot.LinkClicked += ChkConfigureRoot_LinkClicked;
            // 
            // bttnClearAlert
            // 
            bttnClearAlert.Location = new System.Drawing.Point(15, 246);
            bttnClearAlert.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnClearAlert.Name = "bttnClearAlert";
            bttnClearAlert.Size = new System.Drawing.Size(140, 29);
            bttnClearAlert.TabIndex = 48;
            bttnClearAlert.Text = "Clear Alert";
            bttnClearAlert.UseVisualStyleBackColor = true;
            bttnClearAlert.Click += BttnClearAlert_Click;
            // 
            // lblUptime
            // 
            lblUptime.AutoSize = true;
            lblUptime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblUptime.Location = new System.Drawing.Point(12, 168);
            lblUptime.Name = "lblUptime";
            lblUptime.Size = new System.Drawing.Size(111, 20);
            lblUptime.TabIndex = 49;
            lblUptime.Text = "Current Uptime:";
            // 
            // lblStartTime
            // 
            lblStartTime.AutoSize = true;
            lblStartTime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblStartTime.Location = new System.Drawing.Point(12, 201);
            lblStartTime.Name = "lblStartTime";
            lblStartTime.Size = new System.Drawing.Size(78, 20);
            lblStartTime.TabIndex = 50;
            lblStartTime.Text = "Start Time:";
            // 
            // UptimeThresholdConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(577, 310);
            Controls.Add(lblStartTime);
            Controls.Add(lblUptime);
            Controls.Add(bttnClearAlert);
            Controls.Add(lnkConfigureRoot);
            Controls.Add(bttnUpdate);
            Controls.Add(pnlThresholds);
            Controls.Add(chkInherit);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UptimeThresholdConfig";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Uptime Threshold Config";
            Load += UptimeThresholdConfig_Load;
            pnlThresholds.ResumeLayout(false);
            pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.CheckBox chkInherit;
        private System.Windows.Forms.LinkLabel lnkConfigureRoot;
        private System.Windows.Forms.Button bttnClearAlert;
        private System.Windows.Forms.Label lblUptime;
        private System.Windows.Forms.Label lblStartTime;
    }
}