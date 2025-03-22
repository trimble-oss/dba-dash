namespace DBADashGUI.Backups
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
            components = new System.ComponentModel.Container();
            chkUseFG = new System.Windows.Forms.CheckBox();
            chkUsePartial = new System.Windows.Forms.CheckBox();
            chkBackupInherit = new System.Windows.Forms.CheckBox();
            pnlBackupThresholds = new System.Windows.Forms.Panel();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            numMinimumAge = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            txtExcluded = new System.Windows.Forms.TextBox();
            label15 = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            numLogWarning = new System.Windows.Forms.NumericUpDown();
            numLogCritical = new System.Windows.Forms.NumericUpDown();
            label13 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            numDiffWarning = new System.Windows.Forms.NumericUpDown();
            numDiffCritical = new System.Windows.Forms.NumericUpDown();
            label10 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            chkLog = new System.Windows.Forms.CheckBox();
            chkDiff = new System.Windows.Forms.CheckBox();
            chkFull = new System.Windows.Forms.CheckBox();
            label7 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            numFullWarning = new System.Windows.Forms.NumericUpDown();
            numFullCritical = new System.Windows.Forms.NumericUpDown();
            bttnUpdate = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            chkSnapshotBackups = new System.Windows.Forms.CheckBox();
            chkCopyOnlyBackups = new System.Windows.Forms.CheckBox();
            pnlBackupThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMinimumAge).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLogWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLogCritical).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDiffWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDiffCritical).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFullWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFullCritical).BeginInit();
            SuspendLayout();
            // 
            // chkUseFG
            // 
            chkUseFG.AutoSize = true;
            chkUseFG.Location = new System.Drawing.Point(278, 298);
            chkUseFG.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkUseFG.Name = "chkUseFG";
            chkUseFG.Size = new System.Drawing.Size(180, 24);
            chkUseFG.TabIndex = 44;
            chkUseFG.Text = "Use Filegroup Backups";
            chkUseFG.UseVisualStyleBackColor = true;
            // 
            // chkUsePartial
            // 
            chkUsePartial.AutoSize = true;
            chkUsePartial.Location = new System.Drawing.Point(278, 266);
            chkUsePartial.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkUsePartial.Name = "chkUsePartial";
            chkUsePartial.Size = new System.Drawing.Size(158, 24);
            chkUsePartial.TabIndex = 43;
            chkUsePartial.Text = "Use Partial Backups";
            chkUsePartial.UseVisualStyleBackColor = true;
            // 
            // chkBackupInherit
            // 
            chkBackupInherit.AutoSize = true;
            chkBackupInherit.Location = new System.Drawing.Point(30, 18);
            chkBackupInherit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkBackupInherit.Name = "chkBackupInherit";
            chkBackupInherit.Size = new System.Drawing.Size(73, 24);
            chkBackupInherit.TabIndex = 42;
            chkBackupInherit.Text = "Inherit";
            chkBackupInherit.UseVisualStyleBackColor = true;
            chkBackupInherit.CheckedChanged += ChkBackupInherit_CheckedChanged;
            // 
            // pnlBackupThresholds
            // 
            pnlBackupThresholds.Controls.Add(chkCopyOnlyBackups);
            pnlBackupThresholds.Controls.Add(chkSnapshotBackups);
            pnlBackupThresholds.Controls.Add(label3);
            pnlBackupThresholds.Controls.Add(label2);
            pnlBackupThresholds.Controls.Add(numMinimumAge);
            pnlBackupThresholds.Controls.Add(label1);
            pnlBackupThresholds.Controls.Add(txtExcluded);
            pnlBackupThresholds.Controls.Add(label15);
            pnlBackupThresholds.Controls.Add(chkUseFG);
            pnlBackupThresholds.Controls.Add(label16);
            pnlBackupThresholds.Controls.Add(chkUsePartial);
            pnlBackupThresholds.Controls.Add(numLogWarning);
            pnlBackupThresholds.Controls.Add(numLogCritical);
            pnlBackupThresholds.Controls.Add(label13);
            pnlBackupThresholds.Controls.Add(label14);
            pnlBackupThresholds.Controls.Add(numDiffWarning);
            pnlBackupThresholds.Controls.Add(numDiffCritical);
            pnlBackupThresholds.Controls.Add(label10);
            pnlBackupThresholds.Controls.Add(label8);
            pnlBackupThresholds.Controls.Add(chkLog);
            pnlBackupThresholds.Controls.Add(chkDiff);
            pnlBackupThresholds.Controls.Add(chkFull);
            pnlBackupThresholds.Controls.Add(label7);
            pnlBackupThresholds.Controls.Add(label9);
            pnlBackupThresholds.Controls.Add(numFullWarning);
            pnlBackupThresholds.Controls.Add(numFullCritical);
            pnlBackupThresholds.Location = new System.Drawing.Point(13, 52);
            pnlBackupThresholds.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pnlBackupThresholds.Name = "pnlBackupThresholds";
            pnlBackupThresholds.Size = new System.Drawing.Size(633, 402);
            pnlBackupThresholds.TabIndex = 41;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 232);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(217, 20);
            label3.TabIndex = 58;
            label3.Text = "New Database Exclusion Period";
            toolTip1.SetToolTip(label3, "This option allows you to exclude recently created databases from the backup check.");
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(386, 230);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(40, 20);
            label2.TabIndex = 57;
            label2.Text = "mins";
            // 
            // numMinimumAge
            // 
            numMinimumAge.Location = new System.Drawing.Point(278, 230);
            numMinimumAge.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numMinimumAge.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numMinimumAge.Name = "numMinimumAge";
            numMinimumAge.Size = new System.Drawing.Size(100, 27);
            numMinimumAge.TabIndex = 56;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 184);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(102, 20);
            label1.TabIndex = 55;
            label1.Text = "Excluded DBs:";
            toolTip1.SetToolTip(label1, "Comma-separated list of database names to exclude from backup check.  LIKE operator wildcards supported");
            // 
            // txtExcluded
            // 
            txtExcluded.Location = new System.Drawing.Point(278, 180);
            txtExcluded.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtExcluded.Name = "txtExcluded";
            txtExcluded.Size = new System.Drawing.Size(339, 27);
            txtExcluded.TabIndex = 54;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(385, 130);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(40, 20);
            label15.TabIndex = 53;
            label15.Text = "mins";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(560, 130);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(40, 20);
            label16.TabIndex = 52;
            label16.Text = "mins";
            // 
            // numLogWarning
            // 
            numLogWarning.Location = new System.Drawing.Point(278, 129);
            numLogWarning.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numLogWarning.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numLogWarning.Name = "numLogWarning";
            numLogWarning.Size = new System.Drawing.Size(100, 27);
            numLogWarning.TabIndex = 50;
            numLogWarning.Value = new decimal(new int[] { 720, 0, 0, 0 });
            // 
            // numLogCritical
            // 
            numLogCritical.Location = new System.Drawing.Point(452, 129);
            numLogCritical.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numLogCritical.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numLogCritical.Name = "numLogCritical";
            numLogCritical.Size = new System.Drawing.Size(100, 27);
            numLogCritical.TabIndex = 51;
            numLogCritical.Value = new decimal(new int[] { 1440, 0, 0, 0 });
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(385, 92);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(40, 20);
            label13.TabIndex = 49;
            label13.Text = "mins";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(560, 92);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(40, 20);
            label14.TabIndex = 48;
            label14.Text = "mins";
            // 
            // numDiffWarning
            // 
            numDiffWarning.Enabled = false;
            numDiffWarning.Location = new System.Drawing.Point(278, 91);
            numDiffWarning.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numDiffWarning.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numDiffWarning.Name = "numDiffWarning";
            numDiffWarning.Size = new System.Drawing.Size(100, 27);
            numDiffWarning.TabIndex = 46;
            numDiffWarning.Value = new decimal(new int[] { 1440, 0, 0, 0 });
            // 
            // numDiffCritical
            // 
            numDiffCritical.Enabled = false;
            numDiffCritical.Location = new System.Drawing.Point(452, 91);
            numDiffCritical.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numDiffCritical.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numDiffCritical.Name = "numDiffCritical";
            numDiffCritical.Size = new System.Drawing.Size(100, 27);
            numDiffCritical.TabIndex = 47;
            numDiffCritical.Value = new decimal(new int[] { 2880, 0, 0, 0 });
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(385, 55);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(40, 20);
            label10.TabIndex = 45;
            label10.Text = "mins";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(560, 55);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(40, 20);
            label8.TabIndex = 44;
            label8.Text = "mins";
            // 
            // chkLog
            // 
            chkLog.AutoSize = true;
            chkLog.Checked = true;
            chkLog.CheckState = System.Windows.Forms.CheckState.Checked;
            chkLog.Location = new System.Drawing.Point(17, 129);
            chkLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkLog.Name = "chkLog";
            chkLog.Size = new System.Drawing.Size(56, 24);
            chkLog.TabIndex = 43;
            chkLog.Text = "Log";
            chkLog.UseVisualStyleBackColor = true;
            chkLog.CheckedChanged += ChkLog_CheckedChanged;
            // 
            // chkDiff
            // 
            chkDiff.AutoSize = true;
            chkDiff.Location = new System.Drawing.Point(17, 92);
            chkDiff.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkDiff.Name = "chkDiff";
            chkDiff.Size = new System.Drawing.Size(56, 24);
            chkDiff.TabIndex = 42;
            chkDiff.Text = "Diff";
            chkDiff.UseVisualStyleBackColor = true;
            chkDiff.CheckedChanged += ChkDiff_CheckedChanged;
            // 
            // chkFull
            // 
            chkFull.AutoSize = true;
            chkFull.Checked = true;
            chkFull.CheckState = System.Windows.Forms.CheckState.Checked;
            chkFull.Location = new System.Drawing.Point(17, 55);
            chkFull.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkFull.Name = "chkFull";
            chkFull.Size = new System.Drawing.Size(54, 24);
            chkFull.TabIndex = 41;
            chkFull.Text = "Full";
            chkFull.UseVisualStyleBackColor = true;
            chkFull.CheckedChanged += ChkFull_CheckedChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(449, 16);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(58, 20);
            label7.TabIndex = 20;
            label7.Text = "Critical:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(275, 16);
            label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(67, 20);
            label9.TabIndex = 19;
            label9.Text = "Warning:";
            // 
            // numFullWarning
            // 
            numFullWarning.Location = new System.Drawing.Point(278, 54);
            numFullWarning.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numFullWarning.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numFullWarning.Name = "numFullWarning";
            numFullWarning.Size = new System.Drawing.Size(100, 27);
            numFullWarning.TabIndex = 22;
            numFullWarning.Value = new decimal(new int[] { 10080, 0, 0, 0 });
            // 
            // numFullCritical
            // 
            numFullCritical.Location = new System.Drawing.Point(452, 54);
            numFullCritical.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numFullCritical.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numFullCritical.Name = "numFullCritical";
            numFullCritical.Size = new System.Drawing.Size(100, 27);
            numFullCritical.TabIndex = 23;
            numFullCritical.Value = new decimal(new int[] { 14400, 0, 0, 0 });
            // 
            // bttnUpdate
            // 
            bttnUpdate.Location = new System.Drawing.Point(555, 464);
            bttnUpdate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(75, 29);
            bttnUpdate.TabIndex = 45;
            bttnUpdate.Text = "Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // chkSnapshotBackups
            // 
            chkSnapshotBackups.AutoSize = true;
            chkSnapshotBackups.Checked = true;
            chkSnapshotBackups.CheckState = System.Windows.Forms.CheckState.Checked;
            chkSnapshotBackups.Location = new System.Drawing.Point(278, 329);
            chkSnapshotBackups.Name = "chkSnapshotBackups";
            chkSnapshotBackups.Size = new System.Drawing.Size(178, 24);
            chkSnapshotBackups.TabIndex = 59;
            chkSnapshotBackups.Text = "Use Snapshot Backups";
            chkSnapshotBackups.UseVisualStyleBackColor = true;
            // 
            // chkCopyOnlyBackups
            // 
            chkCopyOnlyBackups.AutoSize = true;
            chkCopyOnlyBackups.Checked = true;
            chkCopyOnlyBackups.CheckState = System.Windows.Forms.CheckState.Checked;
            chkCopyOnlyBackups.Location = new System.Drawing.Point(278, 359);
            chkCopyOnlyBackups.Name = "chkCopyOnlyBackups";
            chkCopyOnlyBackups.Size = new System.Drawing.Size(185, 24);
            chkCopyOnlyBackups.TabIndex = 60;
            chkCopyOnlyBackups.Text = "Use Copy Only Backups";
            chkCopyOnlyBackups.UseVisualStyleBackColor = true;
            // 
            // BackupThresholdsConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(661, 508);
            Controls.Add(bttnUpdate);
            Controls.Add(chkBackupInherit);
            Controls.Add(pnlBackupThresholds);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "BackupThresholdsConfig";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Backup Thresholds";
            Load += BackupThresholds_Load;
            pnlBackupThresholds.ResumeLayout(false);
            pnlBackupThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numMinimumAge).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLogWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLogCritical).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDiffWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDiffCritical).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFullWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFullCritical).EndInit();
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtExcluded;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numMinimumAge;
        private System.Windows.Forms.CheckBox chkCopyOnlyBackups;
        private System.Windows.Forms.CheckBox chkSnapshotBackups;
    }
}