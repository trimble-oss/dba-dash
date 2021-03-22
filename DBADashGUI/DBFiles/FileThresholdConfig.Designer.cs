namespace DBADashGUI.DBFiles
{
    partial class FileThresholdConfig
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
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.lblDriveCritical = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDriveWarning = new System.Windows.Forms.Label();
            this.numWarning = new System.Windows.Forms.NumericUpDown();
            this.numCritical = new System.Windows.Forms.NumericUpDown();
            this.OptDisabled = new System.Windows.Forms.RadioButton();
            this.optMB = new System.Windows.Forms.RadioButton();
            this.optPercent = new System.Windows.Forms.RadioButton();
            this.grpFreespace = new System.Windows.Forms.GroupBox();
            this.chkInherit = new System.Windows.Forms.CheckBox();
            this.grpMaxSize = new System.Windows.Forms.GroupBox();
            this.chkMaxSizeDisable = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numMaxSizeCritical = new System.Windows.Forms.NumericUpDown();
            this.numMaxSizeWarning = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).BeginInit();
            this.grpFreespace.SuspendLayout();
            this.grpMaxSize.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSizeCritical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSizeWarning)).BeginInit();
            this.SuspendLayout();
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(305, 305);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 23);
            this.bttnCancel.TabIndex = 41;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Location = new System.Drawing.Point(386, 305);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(75, 23);
            this.bttnUpdate.TabIndex = 40;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.bttnUpdate_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 69);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 17);
            this.label6.TabIndex = 20;
            this.label6.Text = "Critical:";
            // 
            // lblDriveCritical
            // 
            this.lblDriveCritical.AutoSize = true;
            this.lblDriveCritical.Location = new System.Drawing.Point(247, 67);
            this.lblDriveCritical.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveCritical.Name = "lblDriveCritical";
            this.lblDriveCritical.Size = new System.Drawing.Size(20, 17);
            this.lblDriveCritical.TabIndex = 25;
            this.lblDriveCritical.Text = "%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 33);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 17);
            this.label5.TabIndex = 19;
            this.label5.Text = "Warning:";
            // 
            // lblDriveWarning
            // 
            this.lblDriveWarning.AutoSize = true;
            this.lblDriveWarning.Location = new System.Drawing.Point(247, 35);
            this.lblDriveWarning.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDriveWarning.Name = "lblDriveWarning";
            this.lblDriveWarning.Size = new System.Drawing.Size(20, 17);
            this.lblDriveWarning.TabIndex = 24;
            this.lblDriveWarning.Text = "%";
            // 
            // numWarning
            // 
            this.numWarning.DecimalPlaces = 1;
            this.numWarning.Location = new System.Drawing.Point(79, 33);
            this.numWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numWarning.Name = "numWarning";
            this.numWarning.Size = new System.Drawing.Size(160, 22);
            this.numWarning.TabIndex = 22;
            this.numWarning.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // numCritical
            // 
            this.numCritical.DecimalPlaces = 1;
            this.numCritical.Location = new System.Drawing.Point(79, 65);
            this.numCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numCritical.Name = "numCritical";
            this.numCritical.Size = new System.Drawing.Size(160, 22);
            this.numCritical.TabIndex = 23;
            this.numCritical.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // OptDisabled
            // 
            this.OptDisabled.AutoSize = true;
            this.OptDisabled.Location = new System.Drawing.Point(337, 80);
            this.OptDisabled.Margin = new System.Windows.Forms.Padding(4);
            this.OptDisabled.Name = "OptDisabled";
            this.OptDisabled.Size = new System.Drawing.Size(84, 21);
            this.OptDisabled.TabIndex = 37;
            this.OptDisabled.Text = "Disabled";
            this.OptDisabled.UseVisualStyleBackColor = true;
            this.OptDisabled.CheckedChanged += new System.EventHandler(this.OptDisabled_CheckedChanged);
            // 
            // optMB
            // 
            this.optMB.AutoSize = true;
            this.optMB.Location = new System.Drawing.Point(337, 51);
            this.optMB.Margin = new System.Windows.Forms.Padding(4);
            this.optMB.Name = "optMB";
            this.optMB.Size = new System.Drawing.Size(49, 21);
            this.optMB.TabIndex = 36;
            this.optMB.Text = "MB";
            this.optMB.UseVisualStyleBackColor = true;
            this.optMB.CheckedChanged += new System.EventHandler(this.optMB_CheckedChanged);
            // 
            // optPercent
            // 
            this.optPercent.AutoSize = true;
            this.optPercent.Checked = true;
            this.optPercent.Location = new System.Drawing.Point(337, 22);
            this.optPercent.Margin = new System.Windows.Forms.Padding(4);
            this.optPercent.Name = "optPercent";
            this.optPercent.Size = new System.Drawing.Size(94, 21);
            this.optPercent.TabIndex = 35;
            this.optPercent.TabStop = true;
            this.optPercent.Text = "Percent %";
            this.optPercent.UseVisualStyleBackColor = true;
            this.optPercent.CheckedChanged += new System.EventHandler(this.optPercent_CheckedChanged);
            // 
            // grpFreespace
            // 
            this.grpFreespace.Controls.Add(this.label6);
            this.grpFreespace.Controls.Add(this.label5);
            this.grpFreespace.Controls.Add(this.lblDriveCritical);
            this.grpFreespace.Controls.Add(this.numCritical);
            this.grpFreespace.Controls.Add(this.OptDisabled);
            this.grpFreespace.Controls.Add(this.numWarning);
            this.grpFreespace.Controls.Add(this.optMB);
            this.grpFreespace.Controls.Add(this.lblDriveWarning);
            this.grpFreespace.Controls.Add(this.optPercent);
            this.grpFreespace.Location = new System.Drawing.Point(13, 39);
            this.grpFreespace.Name = "grpFreespace";
            this.grpFreespace.Size = new System.Drawing.Size(448, 121);
            this.grpFreespace.TabIndex = 42;
            this.grpFreespace.TabStop = false;
            this.grpFreespace.Text = "File Freespace";
            this.toolTip1.SetToolTip(this.grpFreespace, "Use this to set thresholds for the amount of free space in your database filegrou" +
        "ps (for manually growing files).  ");
            // 
            // chkInherit
            // 
            this.chkInherit.AutoSize = true;
            this.chkInherit.Location = new System.Drawing.Point(23, 12);
            this.chkInherit.Name = "chkInherit";
            this.chkInherit.Size = new System.Drawing.Size(69, 21);
            this.chkInherit.TabIndex = 43;
            this.chkInherit.Text = "Inherit";
            this.chkInherit.UseVisualStyleBackColor = true;
            this.chkInherit.CheckedChanged += new System.EventHandler(this.chkInherit_CheckedChanged);
            // 
            // grpMaxSize
            // 
            this.grpMaxSize.Controls.Add(this.chkMaxSizeDisable);
            this.grpMaxSize.Controls.Add(this.label1);
            this.grpMaxSize.Controls.Add(this.label2);
            this.grpMaxSize.Controls.Add(this.label3);
            this.grpMaxSize.Controls.Add(this.numMaxSizeCritical);
            this.grpMaxSize.Controls.Add(this.numMaxSizeWarning);
            this.grpMaxSize.Controls.Add(this.label4);
            this.grpMaxSize.Location = new System.Drawing.Point(13, 166);
            this.grpMaxSize.Name = "grpMaxSize";
            this.grpMaxSize.Size = new System.Drawing.Size(448, 121);
            this.grpMaxSize.TabIndex = 44;
            this.grpMaxSize.TabStop = false;
            this.grpMaxSize.Text = "% Max Size";
            this.toolTip1.SetToolTip(this.grpMaxSize, "Use this to set a threshold that will alert you when filegroups are approaching t" +
        "heir max size. ");
            // 
            // chkMaxSizeDisable
            // 
            this.chkMaxSizeDisable.AutoSize = true;
            this.chkMaxSizeDisable.Location = new System.Drawing.Point(337, 32);
            this.chkMaxSizeDisable.Name = "chkMaxSizeDisable";
            this.chkMaxSizeDisable.Size = new System.Drawing.Size(85, 21);
            this.chkMaxSizeDisable.TabIndex = 26;
            this.chkMaxSizeDisable.Text = "Disabled";
            this.chkMaxSizeDisable.UseVisualStyleBackColor = true;
            this.chkMaxSizeDisable.CheckedChanged += new System.EventHandler(this.chkMaxSizeDisable_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 69);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 17);
            this.label1.TabIndex = 20;
            this.label1.Text = "Critical:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 33);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 17);
            this.label2.TabIndex = 19;
            this.label2.Text = "Warning:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(247, 67);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 17);
            this.label3.TabIndex = 25;
            this.label3.Text = "%";
            // 
            // numMaxSizeCritical
            // 
            this.numMaxSizeCritical.DecimalPlaces = 1;
            this.numMaxSizeCritical.Location = new System.Drawing.Point(79, 65);
            this.numMaxSizeCritical.Margin = new System.Windows.Forms.Padding(4);
            this.numMaxSizeCritical.Name = "numMaxSizeCritical";
            this.numMaxSizeCritical.Size = new System.Drawing.Size(160, 22);
            this.numMaxSizeCritical.TabIndex = 23;
            this.numMaxSizeCritical.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            // 
            // numMaxSizeWarning
            // 
            this.numMaxSizeWarning.DecimalPlaces = 1;
            this.numMaxSizeWarning.Location = new System.Drawing.Point(79, 33);
            this.numMaxSizeWarning.Margin = new System.Windows.Forms.Padding(4);
            this.numMaxSizeWarning.Name = "numMaxSizeWarning";
            this.numMaxSizeWarning.Size = new System.Drawing.Size(160, 22);
            this.numMaxSizeWarning.TabIndex = 22;
            this.numMaxSizeWarning.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(247, 35);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 17);
            this.label4.TabIndex = 24;
            this.label4.Text = "%";
            // 
            // FileThresholdConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 346);
            this.Controls.Add(this.grpMaxSize);
            this.Controls.Add(this.chkInherit);
            this.Controls.Add(this.grpFreespace);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FileThresholdConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File Threshold Config";
            ((System.ComponentModel.ISupportInitialize)(this.numWarning)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCritical)).EndInit();
            this.grpFreespace.ResumeLayout(false);
            this.grpFreespace.PerformLayout();
            this.grpMaxSize.ResumeLayout(false);
            this.grpMaxSize.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSizeCritical)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSizeWarning)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblDriveCritical;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblDriveWarning;
        private System.Windows.Forms.NumericUpDown numWarning;
        private System.Windows.Forms.NumericUpDown numCritical;
        private System.Windows.Forms.RadioButton OptDisabled;
        private System.Windows.Forms.RadioButton optMB;
        private System.Windows.Forms.RadioButton optPercent;
        private System.Windows.Forms.GroupBox grpFreespace;
        private System.Windows.Forms.CheckBox chkInherit;
        private System.Windows.Forms.GroupBox grpMaxSize;
        private System.Windows.Forms.CheckBox chkMaxSizeDisable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numMaxSizeCritical;
        private System.Windows.Forms.NumericUpDown numMaxSizeWarning;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}