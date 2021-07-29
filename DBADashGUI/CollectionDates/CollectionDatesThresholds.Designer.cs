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
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.optInherit = new System.Windows.Forms.RadioButton();
            this.OptDisabled = new System.Windows.Forms.RadioButton();
            this.optEnabled = new System.Windows.Forms.RadioButton();
            this.pnlThresholds = new System.Windows.Forms.Panel();
            this.chkReferences = new System.Windows.Forms.CheckedListBox();
            this.chkCheckAll = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.pnlThresholds.SuspendLayout();
            this.SuspendLayout();
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(228, 16);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 17);
            this.label10.TabIndex = 52;
            this.label10.Text = "mins";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(228, 49);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 17);
            this.label8.TabIndex = 51;
            this.label8.Text = "mins";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(4, 46);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 17);
            this.label7.TabIndex = 47;
            this.label7.Text = "Critical:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 11);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 17);
            this.label9.TabIndex = 46;
            this.label9.Text = "Warning:";
            // 
            // numWarning
            // 
            this.numWarning.Location = new System.Drawing.Point(121, 11);
            this.numWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numWarning.Name = "numWarning";
            this.numWarning.Size = new System.Drawing.Size(100, 22);
            this.numWarning.TabIndex = 48;
            // 
            // numCritical
            // 
            this.numCritical.Location = new System.Drawing.Point(121, 44);
            this.numCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numCritical.Name = "numCritical";
            this.numCritical.Size = new System.Drawing.Size(100, 22);
            this.numCritical.TabIndex = 49;
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.Location = new System.Drawing.Point(290, 467);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 23);
            this.bttnCancel.TabIndex = 56;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnUpdate.Location = new System.Drawing.Point(371, 467);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 55;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // optInherit
            // 
            this.optInherit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.optInherit.AutoSize = true;
            this.optInherit.Location = new System.Drawing.Point(209, 329);
            this.optInherit.Margin = new System.Windows.Forms.Padding(4);
            this.optInherit.Name = "optInherit";
            this.optInherit.Size = new System.Drawing.Size(68, 21);
            this.optInherit.TabIndex = 59;
            this.optInherit.Text = "Inherit";
            this.optInherit.UseVisualStyleBackColor = true;
            this.optInherit.CheckedChanged += new System.EventHandler(this.optInherit_CheckedChanged);
            // 
            // OptDisabled
            // 
            this.OptDisabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OptDisabled.AutoSize = true;
            this.OptDisabled.Location = new System.Drawing.Point(102, 329);
            this.OptDisabled.Margin = new System.Windows.Forms.Padding(4);
            this.OptDisabled.Name = "OptDisabled";
            this.OptDisabled.Size = new System.Drawing.Size(84, 21);
            this.OptDisabled.TabIndex = 58;
            this.OptDisabled.Text = "Disabled";
            this.OptDisabled.UseVisualStyleBackColor = true;
            this.OptDisabled.CheckedChanged += new System.EventHandler(this.OptDisabled_CheckedChanged);
            // 
            // optEnabled
            // 
            this.optEnabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.optEnabled.AutoSize = true;
            this.optEnabled.Checked = true;
            this.optEnabled.Location = new System.Drawing.Point(13, 329);
            this.optEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.optEnabled.Name = "optEnabled";
            this.optEnabled.Size = new System.Drawing.Size(81, 21);
            this.optEnabled.TabIndex = 57;
            this.optEnabled.TabStop = true;
            this.optEnabled.Text = "Enabled";
            this.optEnabled.UseVisualStyleBackColor = true;
            this.optEnabled.CheckedChanged += new System.EventHandler(this.optEnabled_CheckedChanged);
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlThresholds.Controls.Add(this.label9);
            this.pnlThresholds.Controls.Add(this.numCritical);
            this.pnlThresholds.Controls.Add(this.numWarning);
            this.pnlThresholds.Controls.Add(this.label7);
            this.pnlThresholds.Controls.Add(this.label8);
            this.pnlThresholds.Controls.Add(this.label10);
            this.pnlThresholds.Location = new System.Drawing.Point(12, 357);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(386, 85);
            this.pnlThresholds.TabIndex = 60;
            // 
            // chkReferences
            // 
            this.chkReferences.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkReferences.CheckOnClick = true;
            this.chkReferences.FormattingEnabled = true;
            this.chkReferences.Location = new System.Drawing.Point(12, 46);
            this.chkReferences.Name = "chkReferences";
            this.chkReferences.Size = new System.Drawing.Size(434, 276);
            this.chkReferences.TabIndex = 61;
            // 
            // chkCheckAll
            // 
            this.chkCheckAll.AutoSize = true;
            this.chkCheckAll.Location = new System.Drawing.Point(13, 12);
            this.chkCheckAll.Name = "chkCheckAll";
            this.chkCheckAll.Size = new System.Drawing.Size(88, 21);
            this.chkCheckAll.TabIndex = 62;
            this.chkCheckAll.Text = "Check All";
            this.chkCheckAll.UseVisualStyleBackColor = true;
            this.chkCheckAll.CheckedChanged += new System.EventHandler(this.chkCheckAll_CheckedChanged);
            // 
            // CollectionDatesThresholds
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(458, 503);
            this.Controls.Add(this.chkCheckAll);
            this.Controls.Add(this.chkReferences);
            this.Controls.Add(this.pnlThresholds);
            this.Controls.Add(this.optInherit);
            this.Controls.Add(this.OptDisabled);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.optEnabled);
            this.Name = "CollectionDatesThresholds";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Collection Dates Thresholds";
            this.Load += new System.EventHandler(this.CollectionDatesThresholds_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.RadioButton optInherit;
        private System.Windows.Forms.RadioButton OptDisabled;
        private System.Windows.Forms.RadioButton optEnabled;
        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.CheckedListBox chkReferences;
        private System.Windows.Forms.CheckBox chkCheckAll;
    }
}