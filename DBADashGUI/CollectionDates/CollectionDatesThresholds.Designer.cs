namespace DBADashGUI.CollectionDates
{
    partial class CollectionDatesThresholds
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
            label10 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            numWarning = new System.Windows.Forms.NumericUpDown();
            numCritical = new System.Windows.Forms.NumericUpDown();
            labelWarningMultiplier = new System.Windows.Forms.Label();
            numWarningMultiplier = new System.Windows.Forms.NumericUpDown();
            labelWarningMultiplierSuffix = new System.Windows.Forms.Label();
            labelCriticalMultiplier = new System.Windows.Forms.Label();
            numCriticalMultiplier = new System.Windows.Forms.NumericUpDown();
            labelCriticalMultiplierSuffix = new System.Windows.Forms.Label();
            numWarningBuffer = new System.Windows.Forms.NumericUpDown();
            labelWarningBufferSuffix = new System.Windows.Forms.Label();
            numCriticalBuffer = new System.Windows.Forms.NumericUpDown();
            labelCriticalBufferSuffix = new System.Windows.Forms.Label();
            bttnCancel = new System.Windows.Forms.Button();
            bttnUpdate = new System.Windows.Forms.Button();
            optInherit = new System.Windows.Forms.RadioButton();
            OptDisabled = new System.Windows.Forms.RadioButton();
            optExplicit = new System.Windows.Forms.RadioButton();
            optSchedule = new System.Windows.Forms.RadioButton();
            pnlThresholds = new System.Windows.Forms.Panel();
            pnlSchedule = new System.Windows.Forms.Panel();
            chkReferences = new System.Windows.Forms.CheckedListBox();
            chkCheckAll = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)numWarning).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWarningMultiplier).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCriticalMultiplier).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWarningBuffer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCriticalBuffer).BeginInit();
            pnlThresholds.SuspendLayout();
            pnlSchedule.SuspendLayout();
            SuspendLayout();
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(227, 10);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(40, 20);
            label10.TabIndex = 52;
            label10.Text = "mins";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(227, 48);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(40, 20);
            label8.TabIndex = 51;
            label8.Text = "mins";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(4, 48);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(58, 20);
            label7.TabIndex = 47;
            label7.Text = "Critical:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(4, 8);
            label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(67, 20);
            label9.TabIndex = 46;
            label9.Text = "Warning:";
            // 
            // numWarning
            // 
            numWarning.Location = new System.Drawing.Point(140, 6);
            numWarning.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numWarning.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numWarning.Name = "numWarning";
            numWarning.Size = new System.Drawing.Size(80, 27);
            numWarning.TabIndex = 48;
            // 
            // numCritical
            // 
            numCritical.Location = new System.Drawing.Point(140, 46);
            numCritical.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numCritical.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numCritical.Name = "numCritical";
            numCritical.Size = new System.Drawing.Size(80, 27);
            numCritical.TabIndex = 49;
            // 
            // labelWarningMultiplier
            // 
            labelWarningMultiplier.AutoSize = true;
            labelWarningMultiplier.Location = new System.Drawing.Point(4, 8);
            labelWarningMultiplier.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelWarningMultiplier.Name = "labelWarningMultiplier";
            labelWarningMultiplier.Size = new System.Drawing.Size(135, 20);
            labelWarningMultiplier.TabIndex = 63;
            labelWarningMultiplier.Text = "Warning multiplier:";
            // 
            // numWarningMultiplier
            // 
            numWarningMultiplier.DecimalPlaces = 1;
            numWarningMultiplier.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numWarningMultiplier.Location = new System.Drawing.Point(140, 5);
            numWarningMultiplier.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numWarningMultiplier.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numWarningMultiplier.Name = "numWarningMultiplier";
            numWarningMultiplier.Size = new System.Drawing.Size(80, 27);
            numWarningMultiplier.TabIndex = 64;
            numWarningMultiplier.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // labelWarningMultiplierSuffix
            // 
            labelWarningMultiplierSuffix.AutoSize = true;
            labelWarningMultiplierSuffix.Location = new System.Drawing.Point(227, 8);
            labelWarningMultiplierSuffix.Name = "labelWarningMultiplierSuffix";
            labelWarningMultiplierSuffix.Size = new System.Drawing.Size(151, 20);
            labelWarningMultiplierSuffix.TabIndex = 65;
            labelWarningMultiplierSuffix.Text = "* collection interval +";
            // 
            // labelCriticalMultiplier
            // 
            labelCriticalMultiplier.AutoSize = true;
            labelCriticalMultiplier.Location = new System.Drawing.Point(4, 48);
            labelCriticalMultiplier.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelCriticalMultiplier.Name = "labelCriticalMultiplier";
            labelCriticalMultiplier.Size = new System.Drawing.Size(126, 20);
            labelCriticalMultiplier.TabIndex = 70;
            labelCriticalMultiplier.Text = "Critical multiplier:";
            // 
            // numCriticalMultiplier
            // 
            numCriticalMultiplier.DecimalPlaces = 1;
            numCriticalMultiplier.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numCriticalMultiplier.Location = new System.Drawing.Point(140, 45);
            numCriticalMultiplier.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numCriticalMultiplier.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            numCriticalMultiplier.Name = "numCriticalMultiplier";
            numCriticalMultiplier.Size = new System.Drawing.Size(80, 27);
            numCriticalMultiplier.TabIndex = 71;
            numCriticalMultiplier.Value = new decimal(new int[] { 6, 0, 0, 0 });
            // 
            // labelCriticalMultiplierSuffix
            // 
            labelCriticalMultiplierSuffix.AutoSize = true;
            labelCriticalMultiplierSuffix.Location = new System.Drawing.Point(227, 48);
            labelCriticalMultiplierSuffix.Name = "labelCriticalMultiplierSuffix";
            labelCriticalMultiplierSuffix.Size = new System.Drawing.Size(151, 20);
            labelCriticalMultiplierSuffix.TabIndex = 72;
            labelCriticalMultiplierSuffix.Text = "* collection interval +";
            // 
            // numWarningBuffer
            // 
            numWarningBuffer.DecimalPlaces = 1;
            numWarningBuffer.Location = new System.Drawing.Point(386, 8);
            numWarningBuffer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numWarningBuffer.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numWarningBuffer.Name = "numWarningBuffer";
            numWarningBuffer.Size = new System.Drawing.Size(80, 27);
            numWarningBuffer.TabIndex = 67;
            numWarningBuffer.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // labelWarningBufferSuffix
            // 
            labelWarningBufferSuffix.AutoSize = true;
            labelWarningBufferSuffix.Location = new System.Drawing.Point(473, 11);
            labelWarningBufferSuffix.Name = "labelWarningBufferSuffix";
            labelWarningBufferSuffix.Size = new System.Drawing.Size(40, 20);
            labelWarningBufferSuffix.TabIndex = 68;
            labelWarningBufferSuffix.Text = "mins";
            // 
            // numCriticalBuffer
            // 
            numCriticalBuffer.DecimalPlaces = 1;
            numCriticalBuffer.Location = new System.Drawing.Point(386, 45);
            numCriticalBuffer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numCriticalBuffer.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numCriticalBuffer.Name = "numCriticalBuffer";
            numCriticalBuffer.Size = new System.Drawing.Size(80, 27);
            numCriticalBuffer.TabIndex = 74;
            numCriticalBuffer.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // labelCriticalBufferSuffix
            // 
            labelCriticalBufferSuffix.AutoSize = true;
            labelCriticalBufferSuffix.Location = new System.Drawing.Point(473, 48);
            labelCriticalBufferSuffix.Name = "labelCriticalBufferSuffix";
            labelCriticalBufferSuffix.Size = new System.Drawing.Size(40, 20);
            labelCriticalBufferSuffix.TabIndex = 75;
            labelCriticalBufferSuffix.Text = "mins";
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(435, 605);
            bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(75, 29);
            bttnCancel.TabIndex = 56;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // bttnUpdate
            // 
            bttnUpdate.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnUpdate.Location = new System.Drawing.Point(516, 605);
            bttnUpdate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(75, 29);
            bttnUpdate.TabIndex = 55;
            bttnUpdate.Text = "Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // optInherit
            // 
            optInherit.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            optInherit.AutoSize = true;
            optInherit.Location = new System.Drawing.Point(341, 459);
            optInherit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            optInherit.Name = "optInherit";
            optInherit.Size = new System.Drawing.Size(72, 24);
            optInherit.TabIndex = 59;
            optInherit.Text = "Inherit";
            optInherit.UseVisualStyleBackColor = true;
            optInherit.CheckedChanged += OptInherit_CheckedChanged;
            // 
            // OptDisabled
            // 
            OptDisabled.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            OptDisabled.AutoSize = true;
            OptDisabled.Location = new System.Drawing.Point(246, 459);
            OptDisabled.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            OptDisabled.Name = "OptDisabled";
            OptDisabled.Size = new System.Drawing.Size(89, 24);
            OptDisabled.TabIndex = 58;
            OptDisabled.Text = "Disabled";
            OptDisabled.UseVisualStyleBackColor = true;
            OptDisabled.CheckedChanged += OptDisabled_CheckedChanged;
            // 
            // optExplicit
            // 
            optExplicit.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            optExplicit.AutoSize = true;
            optExplicit.Location = new System.Drawing.Point(156, 459);
            optExplicit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            optExplicit.Name = "optExplicit";
            optExplicit.Size = new System.Drawing.Size(78, 24);
            optExplicit.TabIndex = 57;
            optExplicit.Text = "Explicit";
            optExplicit.UseVisualStyleBackColor = true;
            optExplicit.CheckedChanged += OptExplicit_CheckedChanged;
            // 
            // optSchedule
            // 
            optSchedule.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            optSchedule.AutoSize = true;
            optSchedule.Checked = true;
            optSchedule.Location = new System.Drawing.Point(14, 459);
            optSchedule.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            optSchedule.Name = "optSchedule";
            optSchedule.Size = new System.Drawing.Size(134, 24);
            optSchedule.TabIndex = 69;
            optSchedule.TabStop = true;
            optSchedule.Text = "Schedule Based";
            optSchedule.UseVisualStyleBackColor = true;
            optSchedule.CheckedChanged += OptSchedule_CheckedChanged;
            // 
            // pnlThresholds
            // 
            pnlThresholds.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            pnlThresholds.Controls.Add(label9);
            pnlThresholds.Controls.Add(numCritical);
            pnlThresholds.Controls.Add(numWarning);
            pnlThresholds.Controls.Add(label7);
            pnlThresholds.Controls.Add(label8);
            pnlThresholds.Controls.Add(label10);
            pnlThresholds.Location = new System.Drawing.Point(14, 492);
            pnlThresholds.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlThresholds.Name = "pnlThresholds";
            pnlThresholds.Size = new System.Drawing.Size(386, 89);
            pnlThresholds.TabIndex = 60;
            // 
            // pnlSchedule
            // 
            pnlSchedule.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            pnlSchedule.Controls.Add(labelWarningMultiplier);
            pnlSchedule.Controls.Add(numWarningMultiplier);
            pnlSchedule.Controls.Add(labelWarningMultiplierSuffix);
            pnlSchedule.Controls.Add(labelCriticalMultiplier);
            pnlSchedule.Controls.Add(numCriticalMultiplier);
            pnlSchedule.Controls.Add(labelCriticalMultiplierSuffix);
            pnlSchedule.Controls.Add(numWarningBuffer);
            pnlSchedule.Controls.Add(labelWarningBufferSuffix);
            pnlSchedule.Controls.Add(numCriticalBuffer);
            pnlSchedule.Controls.Add(labelCriticalBufferSuffix);
            pnlSchedule.Location = new System.Drawing.Point(13, 492);
            pnlSchedule.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlSchedule.Name = "pnlSchedule";
            pnlSchedule.Size = new System.Drawing.Size(578, 89);
            pnlSchedule.TabIndex = 76;
            // 
            // chkReferences
            // 
            chkReferences.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkReferences.CheckOnClick = true;
            chkReferences.FormattingEnabled = true;
            chkReferences.Location = new System.Drawing.Point(12, 58);
            chkReferences.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkReferences.Name = "chkReferences";
            chkReferences.Size = new System.Drawing.Size(590, 378);
            chkReferences.TabIndex = 61;
            // 
            // chkCheckAll
            // 
            chkCheckAll.AutoSize = true;
            chkCheckAll.Location = new System.Drawing.Point(13, 15);
            chkCheckAll.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkCheckAll.Name = "chkCheckAll";
            chkCheckAll.Size = new System.Drawing.Size(92, 24);
            chkCheckAll.TabIndex = 62;
            chkCheckAll.Text = "Check All";
            chkCheckAll.UseVisualStyleBackColor = true;
            chkCheckAll.CheckedChanged += ChkCheckAll_CheckedChanged;
            // 
            // CollectionDatesThresholds
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(614, 647);
            Controls.Add(chkCheckAll);
            Controls.Add(chkReferences);
            Controls.Add(pnlSchedule);
            Controls.Add(pnlThresholds);
            Controls.Add(optInherit);
            Controls.Add(OptDisabled);
            Controls.Add(bttnCancel);
            Controls.Add(bttnUpdate);
            Controls.Add(optExplicit);
            Controls.Add(optSchedule);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CollectionDatesThresholds";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Collection Dates Thresholds";
            Load += CollectionDatesThresholds_Load;
            ((System.ComponentModel.ISupportInitialize)numWarning).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCritical).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWarningMultiplier).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCriticalMultiplier).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWarningBuffer).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCriticalBuffer).EndInit();
            pnlThresholds.ResumeLayout(false);
            pnlThresholds.PerformLayout();
            pnlSchedule.ResumeLayout(false);
            pnlSchedule.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.Label labelWarningMultiplier;
        private System.Windows.Forms.NumericUpDown numWarningMultiplier;
        private System.Windows.Forms.Label labelWarningMultiplierSuffix;
        private System.Windows.Forms.Label labelCriticalMultiplier;
        private System.Windows.Forms.NumericUpDown numCriticalMultiplier;
        private System.Windows.Forms.Label labelCriticalMultiplierSuffix;
        private System.Windows.Forms.NumericUpDown numWarningBuffer;
        private System.Windows.Forms.Label labelWarningBufferSuffix;
        private System.Windows.Forms.NumericUpDown numCriticalBuffer;
        private System.Windows.Forms.Label labelCriticalBufferSuffix;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.RadioButton optInherit;
        private System.Windows.Forms.RadioButton OptDisabled;
        private System.Windows.Forms.RadioButton optExplicit;
        private System.Windows.Forms.RadioButton optSchedule;
        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.Panel pnlSchedule;
        private System.Windows.Forms.CheckedListBox chkReferences;
        private System.Windows.Forms.CheckBox chkCheckAll;
    }
}
