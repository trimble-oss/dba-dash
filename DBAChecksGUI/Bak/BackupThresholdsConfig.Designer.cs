namespace DBAChecksGUI.Backups
{
    partial class BackupThresholdsConfig
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
            this.chkUseFG = new System.Windows.Forms.CheckBox();
            this.chkUsePartial = new System.Windows.Forms.CheckBox();
            this.chkBackupInherit = new System.Windows.Forms.CheckBox();
            this.pnlBackupThresholds = new System.Windows.Forms.Panel();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.numLogWarning = new System.Windows.Forms.NumericUpDown();
            this.numLogCritical = new System.Windows.Forms.NumericUpDown();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.numDiffWarning = new System.Windows.Forms.NumericUpDown();
            this.numDiffCritical = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.chkDiff = new System.Windows.Forms.CheckBox();
            this.chkFull = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.numFullWarning = new System.Windows.Forms.NumericUpDown();
            this.numFullCritical = new System.Windows.Forms.NumericUpDown();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.pnlBackupThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // chkUseFG
            // 
            this.chkUseFG.AutoSize = true;
            this.chkUseFG.Location = new System.Drawing.Point(296, 147);
            this.chkUseFG.Name = "chkUseFG";
            this.chkUseFG.Size = new System.Drawing.Size(176, 21);
            this.chkUseFG.TabIndex = 44;
            this.chkUseFG.Text = "Use Filegroup Backups";
            this.chkUseFG.UseVisualStyleBackColor = true;
            // 
            // chkUsePartial
            // 
            this.chkUsePartial.AutoSize = true;
            this.chkUsePartial.Location = new System.Drawing.Point(122, 147);
            this.chkUsePartial.Name = "chkUsePartial";
            this.chkUsePartial.Size = new System.Drawing.Size(157, 21);
            this.chkUsePartial.TabIndex = 43;
            this.chkUsePartial.Text = "Use Partial Backups";
            this.chkUsePartial.UseVisualStyleBackColor = true;
            // 
            // chkBackupInherit
            // 
            this.chkBackupInherit.AutoSize = true;
            this.chkBackupInherit.Location = new System.Drawing.Point(24, 14);
            this.chkBackupInherit.Name = "chkBackupInherit";
            this.chkBackupInherit.Size = new System.Drawing.Size(69, 21);
            this.chkBackupInherit.TabIndex = 42;
            this.chkBackupInherit.Text = "Inherit";
            this.chkBackupInherit.UseVisualStyleBackColor = true;
            this.chkBackupInherit.CheckedChanged += new System.EventHandler(this.chkBackupInherit_CheckedChanged);
            // 
            // pnlBackupThresholds
            // 
            this.pnlBackupThresholds.Controls.Add(this.label15);
            this.pnlBackupThresholds.Controls.Add(this.chkUseFG);
            this.pnlBackupThresholds.Controls.Add(this.label16);
            this.pnlBackupThresholds.Controls.Add(this.chkUsePartial);
            this.pnlBackupThresholds.Controls.Add(this.numLogWarning);
            this.pnlBackupThresholds.Controls.Add(this.numLogCritical);
            this.pnlBackupThresholds.Controls.Add(this.label13);
            this.pnlBackupThresholds.Controls.Add(this.label14);
            this.pnlBackupThresholds.Controls.Add(this.numDiffWarning);
            this.pnlBackupThresholds.Controls.Add(this.numDiffCritical);
            this.pnlBackupThresholds.Controls.Add(this.label10);
            this.pnlBackupThresholds.Controls.Add(this.label8);
            this.pnlBackupThresholds.Controls.Add(this.chkLog);
            this.pnlBackupThresholds.Controls.Add(this.chkDiff);
            this.pnlBackupThresholds.Controls.Add(this.chkFull);
            this.pnlBackupThresholds.Controls.Add(this.label7);
            this.pnlBackupThresholds.Controls.Add(this.label9);
            this.pnlBackupThresholds.Controls.Add(this.numFullWarning);
            this.pnlBackupThresholds.Controls.Add(this.numFullCritical);
            this.pnlBackupThresholds.Location = new System.Drawing.Point(13, 42);
            this.pnlBackupThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlBackupThresholds.Name = "pnlBackupThresholds";
            this.pnlBackupThresholds.Size = new System.Drawing.Size(509, 193);
            this.pnlBackupThresholds.TabIndex = 41;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(229, 101);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(37, 17);
            this.label15.TabIndex = 53;
            this.label15.Text = "mins";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(404, 101);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(37, 17);
            this.label16.TabIndex = 52;
            this.label16.Text = "mins";
            // 
            // numLogWarning
            // 
            this.numLogWarning.Location = new System.Drawing.Point(122, 100);
            this.numLogWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numLogWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLogWarning.Name = "numLogWarning";
            this.numLogWarning.Size = new System.Drawing.Size(100, 22);
            this.numLogWarning.TabIndex = 50;
            this.numLogWarning.Value = new decimal(new int[] {
            720,
            0,
            0,
            0});
            // 
            // numLogCritical
            // 
            this.numLogCritical.Location = new System.Drawing.Point(296, 100);
            this.numLogCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numLogCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLogCritical.Name = "numLogCritical";
            this.numLogCritical.Size = new System.Drawing.Size(100, 22);
            this.numLogCritical.TabIndex = 51;
            this.numLogCritical.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(229, 71);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(37, 17);
            this.label13.TabIndex = 49;
            this.label13.Text = "mins";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(404, 71);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(37, 17);
            this.label14.TabIndex = 48;
            this.label14.Text = "mins";
            // 
            // numDiffWarning
            // 
            this.numDiffWarning.Enabled = false;
            this.numDiffWarning.Location = new System.Drawing.Point(122, 70);
            this.numDiffWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numDiffWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numDiffWarning.Name = "numDiffWarning";
            this.numDiffWarning.Size = new System.Drawing.Size(100, 22);
            this.numDiffWarning.TabIndex = 46;
            this.numDiffWarning.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // numDiffCritical
            // 
            this.numDiffCritical.Enabled = false;
            this.numDiffCritical.Location = new System.Drawing.Point(296, 70);
            this.numDiffCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numDiffCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numDiffCritical.Name = "numDiffCritical";
            this.numDiffCritical.Size = new System.Drawing.Size(100, 22);
            this.numDiffCritical.TabIndex = 47;
            this.numDiffCritical.Value = new decimal(new int[] {
            2880,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(229, 41);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 17);
            this.label10.TabIndex = 45;
            this.label10.Text = "mins";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(404, 41);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 17);
            this.label8.TabIndex = 44;
            this.label8.Text = "mins";
            // 
            // chkLog
            // 
            this.chkLog.AutoSize = true;
            this.chkLog.Checked = true;
            this.chkLog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLog.Location = new System.Drawing.Point(14, 101);
            this.chkLog.Name = "chkLog";
            this.chkLog.Size = new System.Drawing.Size(54, 21);
            this.chkLog.TabIndex = 43;
            this.chkLog.Text = "Log";
            this.chkLog.UseVisualStyleBackColor = true;
            this.chkLog.CheckedChanged += new System.EventHandler(this.chkLog_CheckedChanged);
            // 
            // chkDiff
            // 
            this.chkDiff.AutoSize = true;
            this.chkDiff.Location = new System.Drawing.Point(13, 71);
            this.chkDiff.Name = "chkDiff";
            this.chkDiff.Size = new System.Drawing.Size(51, 21);
            this.chkDiff.TabIndex = 42;
            this.chkDiff.Text = "Diff";
            this.chkDiff.UseVisualStyleBackColor = true;
            this.chkDiff.CheckedChanged += new System.EventHandler(this.chkDiff_CheckedChanged);
            // 
            // chkFull
            // 
            this.chkFull.AutoSize = true;
            this.chkFull.Checked = true;
            this.chkFull.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFull.Location = new System.Drawing.Point(13, 41);
            this.chkFull.Name = "chkFull";
            this.chkFull.Size = new System.Drawing.Size(52, 21);
            this.chkFull.TabIndex = 41;
            this.chkFull.Text = "Full";
            this.chkFull.UseVisualStyleBackColor = true;
            this.chkFull.CheckedChanged += new System.EventHandler(this.chkFull_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(293, 10);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 17);
            this.label7.TabIndex = 20;
            this.label7.Text = "Critical:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(119, 10);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 17);
            this.label9.TabIndex = 19;
            this.label9.Text = "Warning:";
            // 
            // numFullWarning
            // 
            this.numFullWarning.Location = new System.Drawing.Point(122, 40);
            this.numFullWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numFullWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numFullWarning.Name = "numFullWarning";
            this.numFullWarning.Size = new System.Drawing.Size(100, 22);
            this.numFullWarning.TabIndex = 22;
            this.numFullWarning.Value = new decimal(new int[] {
            10080,
            0,
            0,
            0});
            // 
            // numFullCritical
            // 
            this.numFullCritical.Location = new System.Drawing.Point(296, 40);
            this.numFullCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numFullCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numFullCritical.Name = "numFullCritical";
            this.numFullCritical.Size = new System.Drawing.Size(100, 22);
            this.numFullCritical.TabIndex = 23;
            this.numFullCritical.Value = new decimal(new int[] {
            14400,
            0,
            0,
            0});
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(447, 242);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 45;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // BackupThresholdsConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 275);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.chkBackupInherit);
            this.Controls.Add(this.pnlBackupThresholds);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "BackupThresholdsConfig";
            this.Text = "Backup Thresholds";
            this.Load += new System.EventHandler(this.BackupThresholds_Load);
            this.pnlBackupThresholds.ResumeLayout(false);
            this.pnlBackupThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDiffCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFullCritical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkUseFG;
        private System.Windows.Forms.CheckBox chkUsePartial;
        private System.Windows.Forms.CheckBox chkBackupInherit;
        private System.Windows.Forms.Panel pnlBackupThresholds;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.NumericUpDown numLogWarning;
        private System.Windows.Forms.NumericUpDown numLogCritical;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.NumericUpDown numDiffWarning;
        private System.Windows.Forms.NumericUpDown numDiffCritical;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkLog;
        private System.Windows.Forms.CheckBox chkDiff;
        private System.Windows.Forms.CheckBox chkFull;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numFullWarning;
        private System.Windows.Forms.NumericUpDown numFullCritical;
        private System.Windows.Forms.Button bttnUpdate;
    }
}