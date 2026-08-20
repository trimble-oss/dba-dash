namespace DBADashGUI.Pickers
{
    partial class DurationDropDown
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chkNotSet = new CheckBox();
            numDays = new NumericUpDown();
            lblDays = new Label();
            numHours = new NumericUpDown();
            lblHours = new Label();
            numMinutes = new NumericUpDown();
            numSeconds = new NumericUpDown();
            lblSeconds = new Label();
            lblMinutes = new Label();
            ((System.ComponentModel.ISupportInitialize)numDays).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHours).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSeconds).BeginInit();
            SuspendLayout();
            // 
            // chkNotSet
            // 
            chkNotSet.AutoSize = true;
            chkNotSet.Location = new Point(0, 0);
            chkNotSet.Name = "chkNotSet";
            chkNotSet.Size = new Size(79, 24);
            chkNotSet.TabIndex = 0;
            chkNotSet.Text = "Not set";
            chkNotSet.UseVisualStyleBackColor = true;
            chkNotSet.CheckedChanged += ChkNotSet_CheckedChanged;
            // 
            // numDays
            // 
            numDays.Location = new Point(0, 28);
            numDays.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numDays.Name = "numDays";
            numDays.Size = new Size(60, 27);
            numDays.TabIndex = 1;
            numDays.KeyDown += Num_KeyDown;
            // 
            // lblDays
            // 
            lblDays.AutoSize = true;
            lblDays.Location = new Point(62, 32);
            lblDays.Name = "lblDays";
            lblDays.Size = new Size(39, 20);
            lblDays.TabIndex = 2;
            lblDays.Text = "days";
            // 
            // numHours
            // 
            numHours.Location = new Point(102, 28);
            numHours.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
            numHours.Name = "numHours";
            numHours.Size = new Size(48, 27);
            numHours.TabIndex = 3;
            numHours.KeyDown += Num_KeyDown;
            // 
            // lblHours
            // 
            lblHours.AutoSize = true;
            lblHours.Location = new Point(152, 32);
            lblHours.Name = "lblHours";
            lblHours.Size = new Size(28, 20);
            lblHours.TabIndex = 4;
            lblHours.Text = "hrs";
            // 
            // numMinutes
            // 
            numMinutes.Location = new Point(188, 28);
            numMinutes.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numMinutes.Name = "numMinutes";
            numMinutes.Size = new Size(48, 27);
            numMinutes.TabIndex = 5;
            numMinutes.KeyDown += Num_KeyDown;
            // 
            // numSeconds
            // 
            numSeconds.Location = new Point(284, 28);
            numSeconds.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numSeconds.Name = "numSeconds";
            numSeconds.Size = new Size(48, 27);
            numSeconds.TabIndex = 6;
            numSeconds.KeyDown += Num_KeyDown;
            // 
            // lblSeconds
            // 
            lblSeconds.AutoSize = true;
            lblSeconds.Location = new Point(338, 32);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new Size(36, 20);
            lblSeconds.TabIndex = 7;
            lblSeconds.Text = "secs";
            // 
            // lblMinutes
            // 
            lblMinutes.AutoSize = true;
            lblMinutes.Location = new Point(238, 32);
            lblMinutes.Name = "lblMinutes";
            lblMinutes.Size = new Size(40, 20);
            lblMinutes.TabIndex = 6;
            lblMinutes.Text = "mins";
            // 
            // DurationDropDown
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chkNotSet);
            Controls.Add(numDays);
            Controls.Add(lblDays);
            Controls.Add(numHours);
            Controls.Add(lblHours);
            Controls.Add(numMinutes);
            Controls.Add(lblMinutes);
            Controls.Add(numSeconds);
            Controls.Add(lblSeconds);
            Name = "DurationDropDown";
            Size = new Size(380, 55);
            ((System.ComponentModel.ISupportInitialize)numDays).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHours).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSeconds).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkNotSet;
        private System.Windows.Forms.NumericUpDown numDays;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.NumericUpDown numHours;
        private System.Windows.Forms.Label lblHours;
        private System.Windows.Forms.NumericUpDown numMinutes;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.NumericUpDown numSeconds;
        private System.Windows.Forms.Label lblSeconds;
    }
}
