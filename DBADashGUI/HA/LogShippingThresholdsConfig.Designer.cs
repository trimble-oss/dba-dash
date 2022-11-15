namespace DBADashGUI.LogShipping
{
    partial class LogShippingThresholdsConfig
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
            this.label1 = new System.Windows.Forms.Label();
            this.numExcludePeriod = new System.Windows.Forms.NumericUpDown();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.numLRTimeSinceLastWarning = new System.Windows.Forms.NumericUpDown();
            this.numLRTimeSinceLastCritical = new System.Windows.Forms.NumericUpDown();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.chkLRTimeSinceLast = new System.Windows.Forms.CheckBox();
            this.chkLRLatency = new System.Windows.Forms.CheckBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.numLRLatencyWarning = new System.Windows.Forms.NumericUpDown();
            this.numLRLatencyCritical = new System.Windows.Forms.NumericUpDown();
            this.chkLRInherit = new System.Windows.Forms.CheckBox();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.chkExcludePeriod = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.pnlThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numExcludePeriod)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRTimeSinceLastWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRTimeSinceLastCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRLatencyWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRLatencyCritical)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlThresholds
            // 
            this.pnlThresholds.Controls.Add(this.panel1);
            this.pnlThresholds.Controls.Add(this.chkExcludePeriod);
            this.pnlThresholds.Controls.Add(this.label1);
            this.pnlThresholds.Controls.Add(this.numExcludePeriod);
            this.pnlThresholds.Location = new System.Drawing.Point(13, 39);
            this.pnlThresholds.Margin = new System.Windows.Forms.Padding(4);
            this.pnlThresholds.Name = "pnlThresholds";
            this.pnlThresholds.Size = new System.Drawing.Size(569, 197);
            this.pnlThresholds.TabIndex = 40;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(342, 153);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 17);
            this.label1.TabIndex = 51;
            this.label1.Text = "mins";
            // 
            // numExcludePeriod
            // 
            this.numExcludePeriod.Location = new System.Drawing.Point(235, 151);
            this.numExcludePeriod.Margin = new System.Windows.Forms.Padding(4);
            this.numExcludePeriod.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numExcludePeriod.Name = "numExcludePeriod";
            this.numExcludePeriod.Size = new System.Drawing.Size(100, 22);
            this.numExcludePeriod.TabIndex = 50;
            this.numExcludePeriod.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.numExcludePeriod.Validated += new System.EventHandler(this.NumExcludePeriod_Validated);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(341, 73);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(37, 17);
            this.label21.TabIndex = 49;
            this.label21.Text = "mins";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(516, 73);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(37, 17);
            this.label22.TabIndex = 48;
            this.label22.Text = "mins";
            // 
            // numLRTimeSinceLastWarning
            // 
            this.numLRTimeSinceLastWarning.Location = new System.Drawing.Point(234, 72);
            this.numLRTimeSinceLastWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numLRTimeSinceLastWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLRTimeSinceLastWarning.Name = "numLRTimeSinceLastWarning";
            this.numLRTimeSinceLastWarning.Size = new System.Drawing.Size(100, 22);
            this.numLRTimeSinceLastWarning.TabIndex = 46;
            this.numLRTimeSinceLastWarning.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // numLRTimeSinceLastCritical
            // 
            this.numLRTimeSinceLastCritical.Location = new System.Drawing.Point(408, 72);
            this.numLRTimeSinceLastCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numLRTimeSinceLastCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLRTimeSinceLastCritical.Name = "numLRTimeSinceLastCritical";
            this.numLRTimeSinceLastCritical.Size = new System.Drawing.Size(100, 22);
            this.numLRTimeSinceLastCritical.TabIndex = 47;
            this.numLRTimeSinceLastCritical.Value = new decimal(new int[] {
            2880,
            0,
            0,
            0});
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(341, 43);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(37, 17);
            this.label23.TabIndex = 45;
            this.label23.Text = "mins";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(516, 43);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(37, 17);
            this.label24.TabIndex = 44;
            this.label24.Text = "mins";
            // 
            // chkLRTimeSinceLast
            // 
            this.chkLRTimeSinceLast.AutoSize = true;
            this.chkLRTimeSinceLast.Checked = true;
            this.chkLRTimeSinceLast.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLRTimeSinceLast.Location = new System.Drawing.Point(4, 73);
            this.chkLRTimeSinceLast.Name = "chkLRTimeSinceLast";
            this.chkLRTimeSinceLast.Size = new System.Drawing.Size(131, 21);
            this.chkLRTimeSinceLast.TabIndex = 42;
            this.chkLRTimeSinceLast.Text = "Time Since Last";
            this.chkLRTimeSinceLast.UseVisualStyleBackColor = true;
            this.chkLRTimeSinceLast.CheckedChanged += new System.EventHandler(this.ChkLRTimeSinceLast_CheckedChanged);
            // 
            // chkLRLatency
            // 
            this.chkLRLatency.AutoSize = true;
            this.chkLRLatency.Checked = true;
            this.chkLRLatency.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLRLatency.Location = new System.Drawing.Point(4, 43);
            this.chkLRLatency.Name = "chkLRLatency";
            this.chkLRLatency.Size = new System.Drawing.Size(80, 21);
            this.chkLRLatency.TabIndex = 41;
            this.chkLRLatency.Text = "Latency";
            this.chkLRLatency.UseVisualStyleBackColor = true;
            this.chkLRLatency.CheckedChanged += new System.EventHandler(this.ChkLRLatency_CheckedChanged);
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(407, 11);
            this.label25.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(54, 17);
            this.label25.TabIndex = 20;
            this.label25.Text = "Critical:";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(231, 12);
            this.label26.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(65, 17);
            this.label26.TabIndex = 19;
            this.label26.Text = "Warning:";
            // 
            // numLRLatencyWarning
            // 
            this.numLRLatencyWarning.Location = new System.Drawing.Point(234, 42);
            this.numLRLatencyWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numLRLatencyWarning.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLRLatencyWarning.Name = "numLRLatencyWarning";
            this.numLRLatencyWarning.Size = new System.Drawing.Size(100, 22);
            this.numLRLatencyWarning.TabIndex = 22;
            this.numLRLatencyWarning.Value = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            // 
            // numLRLatencyCritical
            // 
            this.numLRLatencyCritical.Location = new System.Drawing.Point(408, 42);
            this.numLRLatencyCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numLRLatencyCritical.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numLRLatencyCritical.Name = "numLRLatencyCritical";
            this.numLRLatencyCritical.Size = new System.Drawing.Size(100, 22);
            this.numLRLatencyCritical.TabIndex = 23;
            this.numLRLatencyCritical.Value = new decimal(new int[] {
            2880,
            0,
            0,
            0});
            // 
            // chkLRInherit
            // 
            this.chkLRInherit.AutoSize = true;
            this.chkLRInherit.Location = new System.Drawing.Point(13, 11);
            this.chkLRInherit.Name = "chkLRInherit";
            this.chkLRInherit.Size = new System.Drawing.Size(69, 21);
            this.chkLRInherit.TabIndex = 39;
            this.chkLRInherit.Text = "Inherit";
            this.chkLRInherit.UseVisualStyleBackColor = true;
            this.chkLRInherit.CheckedChanged += new System.EventHandler(this.ChkLRInherit_CheckedChanged);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(507, 262);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 41;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.BttnUpdate_Click);
            // 
            // chkExcludePeriod
            // 
            this.chkExcludePeriod.AutoSize = true;
            this.chkExcludePeriod.Checked = true;
            this.chkExcludePeriod.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkExcludePeriod.Location = new System.Drawing.Point(5, 152);
            this.chkExcludePeriod.Name = "chkExcludePeriod";
            this.chkExcludePeriod.Size = new System.Drawing.Size(220, 21);
            this.chkExcludePeriod.TabIndex = 53;
            this.chkExcludePeriod.Text = "New Database Exclude Period";
            this.toolTip1.SetToolTip(this.chkExcludePeriod, "Databases in restoring state are detected as log shipping databases after this th" +
        "reshold");
            this.chkExcludePeriod.UseVisualStyleBackColor = true;
            this.chkExcludePeriod.CheckedChanged += new System.EventHandler(this.ChkExcludePeriod_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.chkLRLatency);
            this.panel1.Controls.Add(this.numLRLatencyCritical);
            this.panel1.Controls.Add(this.numLRLatencyWarning);
            this.panel1.Controls.Add(this.label26);
            this.panel1.Controls.Add(this.label21);
            this.panel1.Controls.Add(this.label25);
            this.panel1.Controls.Add(this.label22);
            this.panel1.Controls.Add(this.chkLRTimeSinceLast);
            this.panel1.Controls.Add(this.numLRTimeSinceLastWarning);
            this.panel1.Controls.Add(this.label24);
            this.panel1.Controls.Add(this.numLRTimeSinceLastCritical);
            this.panel1.Controls.Add(this.label23);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(569, 134);
            this.panel1.TabIndex = 54;
            // 
            // LogShippingThresholdsConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 301);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.pnlThresholds);
            this.Controls.Add(this.chkLRInherit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "LogShippingThresholdsConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Log Shipping Thresholds Config";
            this.Load += new System.EventHandler(this.LogShippingThresholdsConfig_Load);
            this.pnlThresholds.ResumeLayout(false);
            this.pnlThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numExcludePeriod)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRTimeSinceLastWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRTimeSinceLastCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRLatencyWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLRLatencyCritical)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlThresholds;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.NumericUpDown numLRTimeSinceLastWarning;
        private System.Windows.Forms.NumericUpDown numLRTimeSinceLastCritical;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox chkLRTimeSinceLast;
        private System.Windows.Forms.CheckBox chkLRLatency;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.NumericUpDown numLRLatencyWarning;
        private System.Windows.Forms.NumericUpDown numLRLatencyCritical;
        private System.Windows.Forms.CheckBox chkLRInherit;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numExcludePeriod;
        private System.Windows.Forms.CheckBox chkExcludePeriod;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}