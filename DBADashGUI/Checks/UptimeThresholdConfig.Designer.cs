
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
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.pnlThresholds = new System.Windows.Forms.Panel();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.chkInherit = new System.Windows.Forms.CheckBox();
            this.lnkConfigureRoot = new System.Windows.Forms.LinkLabel();
            this.bttnClearAlert = new System.Windows.Forms.Button();
            this.lblUptime = new System.Windows.Forms.Label();
            this.lblStartTime = new System.Windows.Forms.Label();
            this.pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(466, 197);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(87, 23);
            this.bttnUpdate.TabIndex = 46;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Controls.Add(this.label23);
            this.pnlThresholds.Controls.Add(this.label24);
            this.pnlThresholds.Controls.Add(this.chkEnabled);
            this.pnlThresholds.Controls.Add(this.label25);
            this.pnlThresholds.Controls.Add(this.label26);
            this.pnlThresholds.Controls.Add(this.numWarning);
            this.pnlThresholds.Controls.Add(this.numCritical);
            this.pnlThresholds.Location = new System.Drawing.Point(15, 40);
            this.pnlThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(538, 84);
            this.pnlThresholds.TabIndex = 45;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(288, 41);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(37, 17);
            this.label23.TabIndex = 45;
            this.label23.Text = "mins";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(463, 41);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(37, 17);
            this.label24.TabIndex = 44;
            this.label24.Text = "mins";
            // 
            // chkEnabled
            // 
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Checked = true;
            this.chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnabled.Location = new System.Drawing.Point(13, 41);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Size = new System.Drawing.Size(82, 21);
            this.chkEnabled.TabIndex = 41;
            this.chkEnabled.Text = "Enabled";
            this.chkEnabled.UseVisualStyleBackColor = true;
            this.chkEnabled.CheckedChanged += new System.EventHandler(this.chkEnabled_CheckedChanged);
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(354, 9);
            this.label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(54, 17);
            this.label25.TabIndex = 20;
            this.label25.Text = "Critical:";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(178, 10);
            this.label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(65, 17);
            this.label26.TabIndex = 19;
            this.label26.Text = "Warning:";
            // 
            // numWarning
            // 
            this.numWarning.Location = new System.Drawing.Point(181, 40);
            this.numWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numWarning.Name = "numWarning";
            this.numWarning.Size = new System.Drawing.Size(100, 22);
            this.numWarning.TabIndex = 22;
            this.numWarning.Value = new decimal(new int[] {
            2880,
            0,
            0,
            0});
            // 
            // numCritical
            // 
            this.numCritical.Location = new System.Drawing.Point(355, 40);
            this.numCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numCritical.Name = "numCritical";
            this.numCritical.Size = new System.Drawing.Size(100, 22);
            this.numCritical.TabIndex = 23;
            this.numCritical.Value = new decimal(new int[] {
            720,
            0,
            0,
            0});
            // 
            // chkInherit
            // 
            this.chkInherit.AutoSize = true;
            this.chkInherit.Location = new System.Drawing.Point(15, 12);
            this.chkInherit.Name = "chkInherit";
            this.chkInherit.Size = new System.Drawing.Size(69, 21);
            this.chkInherit.TabIndex = 44;
            this.chkInherit.Text = "Inherit";
            this.chkInherit.UseVisualStyleBackColor = true;
            this.chkInherit.CheckedChanged += new System.EventHandler(this.chkInherit_CheckedChanged);
            // 
            // lnkConfigureRoot
            // 
            this.lnkConfigureRoot.AutoSize = true;
            this.lnkConfigureRoot.Location = new System.Drawing.Point(403, 12);
            this.lnkConfigureRoot.Name = "lnkConfigureRoot";
            this.lnkConfigureRoot.Size = new System.Drawing.Size(150, 17);
            this.lnkConfigureRoot.TabIndex = 47;
            this.lnkConfigureRoot.TabStop = true;
            this.lnkConfigureRoot.Text = "Root Threshold Config";
            this.lnkConfigureRoot.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.chkConfigureRoot_LinkClicked);
            // 
            // bttnClearAlert
            // 
            this.bttnClearAlert.Location = new System.Drawing.Point(15, 197);
            this.bttnClearAlert.Name = "bttnClearAlert";
            this.bttnClearAlert.Size = new System.Drawing.Size(140, 23);
            this.bttnClearAlert.TabIndex = 48;
            this.bttnClearAlert.Text = "Clear Alert";
            this.bttnClearAlert.UseVisualStyleBackColor = true;
            this.bttnClearAlert.Click += new System.EventHandler(this.bttnClearAlert_Click);
            // 
            // lblUptime
            // 
            this.lblUptime.AutoSize = true;
            this.lblUptime.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUptime.Location = new System.Drawing.Point(12, 134);
            this.lblUptime.Name = "lblUptime";
            this.lblUptime.Size = new System.Drawing.Size(107, 17);
            this.lblUptime.TabIndex = 49;
            this.lblUptime.Text = "Current Uptime:";
            // 
            // lblStartTime
            // 
            this.lblStartTime.AutoSize = true;
            this.lblStartTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStartTime.Location = new System.Drawing.Point(12, 161);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(77, 17);
            this.lblStartTime.TabIndex = 50;
            this.lblStartTime.Text = "Start Time:";
            // 
            // UptimeThresholdConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(577, 248);
            this.Controls.Add(this.lblStartTime);
            this.Controls.Add(this.lblUptime);
            this.Controls.Add(this.bttnClearAlert);
            this.Controls.Add(this.lnkConfigureRoot);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.pnlThresholds);
            this.Controls.Add(this.chkInherit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UptimeThresholdConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Uptime Threshold Config";
            this.Load += new System.EventHandler(this.UptimeThresholdConfig_Load);
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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