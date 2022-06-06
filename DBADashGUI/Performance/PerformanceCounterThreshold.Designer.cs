namespace DBADashGUI.Performance
{
    partial class PerformanceCounterThreshold
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
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lnkGoodToMax = new System.Windows.Forms.LinkLabel();
            this.lnkWarningToMax = new System.Windows.Forms.LinkLabel();
            this.lnkCriticalToMax = new System.Windows.Forms.LinkLabel();
            this.chkCritical = new System.Windows.Forms.CheckBox();
            this.chkWarning = new System.Windows.Forms.CheckBox();
            this.chkGood = new System.Windows.Forms.CheckBox();
            this.grpThresholds = new System.Windows.Forms.GroupBox();
            this.numGoodTo = new System.Windows.Forms.NumericUpDown();
            this.numWarningTo = new System.Windows.Forms.NumericUpDown();
            this.numGoodFrom = new System.Windows.Forms.NumericUpDown();
            this.numWarningFrom = new System.Windows.Forms.NumericUpDown();
            this.numCriticalTo = new System.Windows.Forms.NumericUpDown();
            this.numCriticalFrom = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkUpdateAllInstances = new System.Windows.Forms.CheckBox();
            this.cboCounterInstance = new System.Windows.Forms.ComboBox();
            this.cboCounter = new System.Windows.Forms.ComboBox();
            this.cboObject = new System.Windows.Forms.ComboBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkAllInstances = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cboInstances = new System.Windows.Forms.ComboBox();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.grpThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGoodTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarningTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGoodFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarningFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCriticalTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCriticalFrom)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Critical From:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(336, 34);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 20);
            this.label4.TabIndex = 7;
            this.label4.Text = "Critical To:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(336, 94);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 20);
            this.label1.TabIndex = 11;
            this.label1.Text = "Warning To:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 20);
            this.label3.TabIndex = 9;
            this.label3.Text = "Warning From:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(336, 152);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 20);
            this.label5.TabIndex = 15;
            this.label5.Text = "Good To:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 152);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 20);
            this.label6.TabIndex = 13;
            this.label6.Text = "Good From:";
            // 
            // lnkGoodToMax
            // 
            this.lnkGoodToMax.AutoSize = true;
            this.lnkGoodToMax.Location = new System.Drawing.Point(565, 179);
            this.lnkGoodToMax.Name = "lnkGoodToMax";
            this.lnkGoodToMax.Size = new System.Drawing.Size(37, 20);
            this.lnkGoodToMax.TabIndex = 20;
            this.lnkGoodToMax.TabStop = true;
            this.lnkGoodToMax.Text = "Max";
            this.lnkGoodToMax.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkGoodToMax_LinkClicked);
            // 
            // lnkWarningToMax
            // 
            this.lnkWarningToMax.AutoSize = true;
            this.lnkWarningToMax.Location = new System.Drawing.Point(565, 121);
            this.lnkWarningToMax.Name = "lnkWarningToMax";
            this.lnkWarningToMax.Size = new System.Drawing.Size(37, 20);
            this.lnkWarningToMax.TabIndex = 21;
            this.lnkWarningToMax.TabStop = true;
            this.lnkWarningToMax.Text = "Max";
            this.lnkWarningToMax.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkWarningToMax_LinkClicked);
            // 
            // lnkCriticalToMax
            // 
            this.lnkCriticalToMax.AutoSize = true;
            this.lnkCriticalToMax.Location = new System.Drawing.Point(565, 61);
            this.lnkCriticalToMax.Name = "lnkCriticalToMax";
            this.lnkCriticalToMax.Size = new System.Drawing.Size(37, 20);
            this.lnkCriticalToMax.TabIndex = 22;
            this.lnkCriticalToMax.TabStop = true;
            this.lnkCriticalToMax.Text = "Max";
            this.lnkCriticalToMax.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCriticalToMax_LinkClicked);
            // 
            // chkCritical
            // 
            this.chkCritical.AutoSize = true;
            this.chkCritical.Checked = true;
            this.chkCritical.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCritical.Location = new System.Drawing.Point(625, 34);
            this.chkCritical.Name = "chkCritical";
            this.chkCritical.Size = new System.Drawing.Size(73, 24);
            this.chkCritical.TabIndex = 23;
            this.chkCritical.Text = "Inherit";
            this.toolTip1.SetToolTip(this.chkCritical, "Check to Inherit/Disable");
            this.chkCritical.UseVisualStyleBackColor = true;
            this.chkCritical.CheckedChanged += new System.EventHandler(this.chkCriticalEnabled_CheckedChanged);
            // 
            // chkWarning
            // 
            this.chkWarning.AutoSize = true;
            this.chkWarning.Checked = true;
            this.chkWarning.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWarning.Location = new System.Drawing.Point(625, 94);
            this.chkWarning.Name = "chkWarning";
            this.chkWarning.Size = new System.Drawing.Size(73, 24);
            this.chkWarning.TabIndex = 24;
            this.chkWarning.Text = "Inherit";
            this.toolTip1.SetToolTip(this.chkWarning, "Check to Inherit/Disable");
            this.chkWarning.UseVisualStyleBackColor = true;
            this.chkWarning.CheckedChanged += new System.EventHandler(this.chkWarningEnabled_CheckedChanged);
            // 
            // chkGood
            // 
            this.chkGood.AutoSize = true;
            this.chkGood.Checked = true;
            this.chkGood.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGood.Location = new System.Drawing.Point(625, 152);
            this.chkGood.Name = "chkGood";
            this.chkGood.Size = new System.Drawing.Size(73, 24);
            this.chkGood.TabIndex = 25;
            this.chkGood.Text = "Inherit";
            this.toolTip1.SetToolTip(this.chkGood, "Check to Inherit/Disable");
            this.chkGood.UseVisualStyleBackColor = true;
            this.chkGood.CheckedChanged += new System.EventHandler(this.chkGoodEnabled_CheckedChanged);
            // 
            // grpThresholds
            // 
            this.grpThresholds.Controls.Add(this.numGoodTo);
            this.grpThresholds.Controls.Add(this.numWarningTo);
            this.grpThresholds.Controls.Add(this.numGoodFrom);
            this.grpThresholds.Controls.Add(this.numWarningFrom);
            this.grpThresholds.Controls.Add(this.numCriticalTo);
            this.grpThresholds.Controls.Add(this.numCriticalFrom);
            this.grpThresholds.Controls.Add(this.label2);
            this.grpThresholds.Controls.Add(this.chkGood);
            this.grpThresholds.Controls.Add(this.chkWarning);
            this.grpThresholds.Controls.Add(this.chkCritical);
            this.grpThresholds.Controls.Add(this.label4);
            this.grpThresholds.Controls.Add(this.lnkCriticalToMax);
            this.grpThresholds.Controls.Add(this.lnkWarningToMax);
            this.grpThresholds.Controls.Add(this.label3);
            this.grpThresholds.Controls.Add(this.lnkGoodToMax);
            this.grpThresholds.Controls.Add(this.label5);
            this.grpThresholds.Controls.Add(this.label1);
            this.grpThresholds.Controls.Add(this.label6);
            this.grpThresholds.Location = new System.Drawing.Point(37, 284);
            this.grpThresholds.Name = "grpThresholds";
            this.grpThresholds.Size = new System.Drawing.Size(749, 242);
            this.grpThresholds.TabIndex = 26;
            this.grpThresholds.TabStop = false;
            this.grpThresholds.Text = "Thresholds";
            // 
            // numGoodTo
            // 
            this.numGoodTo.DecimalPlaces = 9;
            this.numGoodTo.Enabled = false;
            this.numGoodTo.Location = new System.Drawing.Point(469, 149);
            this.numGoodTo.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numGoodTo.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numGoodTo.Name = "numGoodTo";
            this.numGoodTo.Size = new System.Drawing.Size(150, 27);
            this.numGoodTo.TabIndex = 40;
            this.numGoodTo.ThousandsSeparator = true;
            // 
            // numWarningTo
            // 
            this.numWarningTo.DecimalPlaces = 9;
            this.numWarningTo.Enabled = false;
            this.numWarningTo.Location = new System.Drawing.Point(469, 92);
            this.numWarningTo.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numWarningTo.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numWarningTo.Name = "numWarningTo";
            this.numWarningTo.Size = new System.Drawing.Size(150, 27);
            this.numWarningTo.TabIndex = 39;
            this.numWarningTo.ThousandsSeparator = true;
            // 
            // numGoodFrom
            // 
            this.numGoodFrom.DecimalPlaces = 9;
            this.numGoodFrom.Enabled = false;
            this.numGoodFrom.Location = new System.Drawing.Point(160, 149);
            this.numGoodFrom.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numGoodFrom.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numGoodFrom.Name = "numGoodFrom";
            this.numGoodFrom.Size = new System.Drawing.Size(150, 27);
            this.numGoodFrom.TabIndex = 38;
            this.numGoodFrom.ThousandsSeparator = true;
            // 
            // numWarningFrom
            // 
            this.numWarningFrom.DecimalPlaces = 9;
            this.numWarningFrom.Enabled = false;
            this.numWarningFrom.Location = new System.Drawing.Point(160, 92);
            this.numWarningFrom.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numWarningFrom.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numWarningFrom.Name = "numWarningFrom";
            this.numWarningFrom.Size = new System.Drawing.Size(150, 27);
            this.numWarningFrom.TabIndex = 37;
            this.numWarningFrom.ThousandsSeparator = true;
            // 
            // numCriticalTo
            // 
            this.numCriticalTo.DecimalPlaces = 9;
            this.numCriticalTo.Enabled = false;
            this.numCriticalTo.Location = new System.Drawing.Point(469, 31);
            this.numCriticalTo.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numCriticalTo.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numCriticalTo.Name = "numCriticalTo";
            this.numCriticalTo.Size = new System.Drawing.Size(150, 27);
            this.numCriticalTo.TabIndex = 36;
            this.numCriticalTo.ThousandsSeparator = true;
            // 
            // numCriticalFrom
            // 
            this.numCriticalFrom.DecimalPlaces = 9;
            this.numCriticalFrom.Enabled = false;
            this.numCriticalFrom.Location = new System.Drawing.Point(160, 31);
            this.numCriticalFrom.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            589824});
            this.numCriticalFrom.Minimum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            -2146893824});
            this.numCriticalFrom.Name = "numCriticalFrom";
            this.numCriticalFrom.Size = new System.Drawing.Size(150, 27);
            this.numCriticalFrom.TabIndex = 35;
            this.numCriticalFrom.ThousandsSeparator = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(18, 39);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 20);
            this.label7.TabIndex = 28;
            this.label7.Text = "Object:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(18, 75);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 20);
            this.label8.TabIndex = 29;
            this.label8.Text = "Counter:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(18, 108);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(122, 20);
            this.label9.TabIndex = 30;
            this.label9.Text = "Counter Instance:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkUpdateAllInstances);
            this.groupBox2.Controls.Add(this.cboCounterInstance);
            this.groupBox2.Controls.Add(this.cboCounter);
            this.groupBox2.Controls.Add(this.cboObject);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Location = new System.Drawing.Point(37, 112);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(749, 152);
            this.groupBox2.TabIndex = 33;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Counter";
            // 
            // chkUpdateAllInstances
            // 
            this.chkUpdateAllInstances.AutoSize = true;
            this.chkUpdateAllInstances.Location = new System.Drawing.Point(625, 109);
            this.chkUpdateAllInstances.Name = "chkUpdateAllInstances";
            this.chkUpdateAllInstances.Size = new System.Drawing.Size(102, 24);
            this.chkUpdateAllInstances.TabIndex = 36;
            this.chkUpdateAllInstances.Text = "Update All";
            this.toolTip1.SetToolTip(this.chkUpdateAllInstances, "Check to update all instances of this counter");
            this.chkUpdateAllInstances.UseVisualStyleBackColor = true;
            // 
            // cboCounterInstance
            // 
            this.cboCounterInstance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCounterInstance.FormattingEnabled = true;
            this.cboCounterInstance.Location = new System.Drawing.Point(163, 105);
            this.cboCounterInstance.Name = "cboCounterInstance";
            this.cboCounterInstance.Size = new System.Drawing.Size(439, 28);
            this.cboCounterInstance.TabIndex = 35;
            this.cboCounterInstance.SelectedValueChanged += new System.EventHandler(this.cboCounterInstance_SelectedValueChanged);
            // 
            // cboCounter
            // 
            this.cboCounter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCounter.FormattingEnabled = true;
            this.cboCounter.Location = new System.Drawing.Point(163, 71);
            this.cboCounter.Name = "cboCounter";
            this.cboCounter.Size = new System.Drawing.Size(439, 28);
            this.cboCounter.TabIndex = 34;
            this.cboCounter.SelectedValueChanged += new System.EventHandler(this.cboCounter_SelectedValueChanged);
            // 
            // cboObject
            // 
            this.cboObject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObject.FormattingEnabled = true;
            this.cboObject.Location = new System.Drawing.Point(163, 38);
            this.cboObject.Name = "cboObject";
            this.cboObject.Size = new System.Drawing.Size(439, 28);
            this.cboObject.TabIndex = 33;
            this.cboObject.SelectedValueChanged += new System.EventHandler(this.cboObject_SelectedValueChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chkAllInstances);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.cboInstances);
            this.groupBox3.Location = new System.Drawing.Point(37, 23);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(749, 71);
            this.groupBox3.TabIndex = 34;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Instance";
            // 
            // chkAllInstances
            // 
            this.chkAllInstances.AutoSize = true;
            this.chkAllInstances.Checked = true;
            this.chkAllInstances.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAllInstances.Location = new System.Drawing.Point(625, 28);
            this.chkAllInstances.Name = "chkAllInstances";
            this.chkAllInstances.Size = new System.Drawing.Size(113, 24);
            this.chkAllInstances.TabIndex = 36;
            this.chkAllInstances.Text = "All Instances";
            this.toolTip1.SetToolTip(this.chkAllInstances, "Check to set root level threshold configuration for all instances");
            this.chkAllInstances.UseVisualStyleBackColor = true;
            this.chkAllInstances.CheckedChanged += new System.EventHandler(this.chkAllInstances_CheckedChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(18, 29);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(66, 20);
            this.label10.TabIndex = 35;
            this.label10.Text = "Instance:";
            // 
            // cboInstances
            // 
            this.cboInstances.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstances.Enabled = false;
            this.cboInstances.FormattingEnabled = true;
            this.cboInstances.Location = new System.Drawing.Point(163, 26);
            this.cboInstances.Name = "cboInstances";
            this.cboInstances.Size = new System.Drawing.Size(439, 28);
            this.cboInstances.TabIndex = 34;
            this.cboInstances.SelectedValueChanged += new System.EventHandler(this.cboInstances_SelectedValueChanged);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(562, 562);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(94, 29);
            this.bttnUpdate.TabIndex = 35;
            this.bttnUpdate.Text = "&Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(670, 562);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 36;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // PerformanceCounterThreshold
            // 
            this.AcceptButton = this.bttnUpdate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(816, 617);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.grpThresholds);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.Name = "PerformanceCounterThreshold";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Counter Threshold";
            this.Load += new System.EventHandler(this.PerformanceCounterThreshold_Load);
            this.grpThresholds.ResumeLayout(false);
            this.grpThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGoodTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarningTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGoodFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWarningFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCriticalTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCriticalFrom)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.LinkLabel lnkGoodToMax;
        private System.Windows.Forms.LinkLabel lnkWarningToMax;
        private System.Windows.Forms.LinkLabel lnkCriticalToMax;
        private System.Windows.Forms.CheckBox chkCritical;
        private System.Windows.Forms.CheckBox chkWarning;
        private System.Windows.Forms.CheckBox chkGood;
        private System.Windows.Forms.GroupBox grpThresholds;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkUpdateAllInstances;
        private System.Windows.Forms.ComboBox cboCounterInstance;
        private System.Windows.Forms.ComboBox cboCounter;
        private System.Windows.Forms.ComboBox cboObject;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox chkAllInstances;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cboInstances;
        private System.Windows.Forms.NumericUpDown numGoodTo;
        private System.Windows.Forms.NumericUpDown numWarningTo;
        private System.Windows.Forms.NumericUpDown numGoodFrom;
        private System.Windows.Forms.NumericUpDown numWarningFrom;
        private System.Windows.Forms.NumericUpDown numCriticalTo;
        private System.Windows.Forms.NumericUpDown numCriticalFrom;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}