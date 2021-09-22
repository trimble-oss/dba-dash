namespace DBADashGUI.LastGoodCheckDB
{
    partial class LastGoodCheckDBConfig
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
            this.pnlThresholds = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.numMinimumAge = new System.Windows.Forms.NumericUpDown();
            this.label24 = new System.Windows.Forms.Label();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.chkInherit = new System.Windows.Forms.CheckBox();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.txtExcluded = new System.Windows.Forms.TextBox();
            this.pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinimumAge)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Controls.Add(this.label1);
            this.pnlThresholds.Controls.Add(this.txtExcluded);
            this.pnlThresholds.Controls.Add(this.label3);
            this.pnlThresholds.Controls.Add(this.label2);
            this.pnlThresholds.Controls.Add(this.label23);
            this.pnlThresholds.Controls.Add(this.numMinimumAge);
            this.pnlThresholds.Controls.Add(this.label24);
            this.pnlThresholds.Controls.Add(this.chkEnabled);
            this.pnlThresholds.Controls.Add(this.label25);
            this.pnlThresholds.Controls.Add(this.label26);
            this.pnlThresholds.Controls.Add(this.numWarning);
            this.pnlThresholds.Controls.Add(this.numCritical);
            this.pnlThresholds.Location = new System.Drawing.Point(13, 43);
            this.pnlThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(562, 161);
            this.pnlThresholds.TabIndex = 42;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(208, 17);
            this.label3.TabIndex = 61;
            this.label3.Text = "New Database Exclusion Period";
            this.toolTip1.SetToolTip(this.label3, "This option allows you to exclude recently created databases from the DBCC thresh" +
        "old");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(364, 102);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 17);
            this.label2.TabIndex = 60;
            this.label2.Text = "mins";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(364, 41);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(28, 17);
            this.label23.TabIndex = 45;
            this.label23.Text = "hrs";
            // 
            // numMinimumAge
            // 
            this.numMinimumAge.Location = new System.Drawing.Point(257, 100);
            this.numMinimumAge.Margin = new System.Windows.Forms.Padding(4);
            this.numMinimumAge.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numMinimumAge.Name = "numMinimumAge";
            this.numMinimumAge.Size = new System.Drawing.Size(100, 22);
            this.numMinimumAge.TabIndex = 59;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(364, 72);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(28, 17);
            this.label24.TabIndex = 44;
            this.label24.Text = "hrs";
            // 
            // chkEnabled
            // 
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Checked = true;
            this.chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnabled.Location = new System.Drawing.Point(13, 10);
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
            this.label25.Location = new System.Drawing.Point(10, 72);
            this.label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(54, 17);
            this.label25.TabIndex = 20;
            this.label25.Text = "Critical:";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(10, 41);
            this.label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(65, 17);
            this.label26.TabIndex = 19;
            this.label26.Text = "Warning:";
            // 
            // numWarning
            // 
            this.numWarning.Location = new System.Drawing.Point(257, 40);
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
            168,
            0,
            0,
            0});
            // 
            // numCritical
            // 
            this.numCritical.Location = new System.Drawing.Point(257, 70);
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
            336,
            0,
            0,
            0});
            // 
            // chkInherit
            // 
            this.chkInherit.AutoSize = true;
            this.chkInherit.Location = new System.Drawing.Point(26, 15);
            this.chkInherit.Name = "chkInherit";
            this.chkInherit.Size = new System.Drawing.Size(69, 21);
            this.chkInherit.TabIndex = 41;
            this.chkInherit.Text = "Inherit";
            this.chkInherit.UseVisualStyleBackColor = true;
            this.chkInherit.CheckedChanged += new System.EventHandler(this.chkInherit_CheckedChanged);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(485, 226);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 43;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 134);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 17);
            this.label1.TabIndex = 63;
            this.label1.Text = "Excluded DBs:";
            this.toolTip1.SetToolTip(this.label1, "Comma-separated list of database names to exclude from DBCC threshold.  LIKE oper" +
        "ator wildcards supported");
            // 
            // txtExcluded
            // 
            this.txtExcluded.Location = new System.Drawing.Point(257, 131);
            this.txtExcluded.Name = "txtExcluded";
            this.txtExcluded.Size = new System.Drawing.Size(290, 22);
            this.txtExcluded.TabIndex = 62;
            // 
            // LastGoodCheckDBConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 261);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.pnlThresholds);
            this.Controls.Add(this.chkInherit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "LastGoodCheckDBConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Last Good CheckDB Config";
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinimumAge)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.CheckBox chkInherit;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numMinimumAge;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtExcluded;
    }
}