namespace DBADashServiceConfig
{
    partial class CronExpressionBuilder
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            cboFrequency = new System.Windows.Forms.ComboBox();
            numInterval = new System.Windows.Forms.NumericUpDown();
            numSecondBase = new System.Windows.Forms.NumericUpDown();
            numMinuteBase = new System.Windows.Forms.NumericUpDown();
            numHourBase = new System.Windows.Forms.NumericUpDown();
            dtpTime = new System.Windows.Forms.DateTimePicker();
            txtCustom = new System.Windows.Forms.TextBox();
            lblPreview = new System.Windows.Forms.Label();
            lblFrequency = new System.Windows.Forms.Label();
            lblInterval = new System.Windows.Forms.Label();
            lblTime = new System.Windows.Forms.Label();
            lblDay = new System.Windows.Forms.Label();
            lblCustom = new System.Windows.Forms.Label();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            chkDaysOfWeek = new System.Windows.Forms.CheckedListBox();
            txtCron = new System.Windows.Forms.TextBox();
            lblCron = new System.Windows.Forms.Label();
            lblOffset = new System.Windows.Forms.Label();
            numIntegerSeconds = new System.Windows.Forms.NumericUpDown();
            lblSeconds = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSecondBase).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinuteBase).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHourBase).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numIntegerSeconds).BeginInit();
            SuspendLayout();
            // 
            // cboFrequency
            // 
            cboFrequency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboFrequency.Location = new System.Drawing.Point(174, 10);
            cboFrequency.Name = "cboFrequency";
            cboFrequency.Size = new System.Drawing.Size(280, 28);
            cboFrequency.TabIndex = 1;
            cboFrequency.SelectedIndexChanged += CboFrequency_SelectedIndexChanged;
            // 
            // numInterval
            // 
            numInterval.Location = new System.Drawing.Point(174, 86);
            numInterval.Name = "numInterval";
            numInterval.Size = new System.Drawing.Size(100, 27);
            numInterval.TabIndex = 3;
            numInterval.ValueChanged += NumInterval_ValueChanged;
            // 
            // numSecondBase
            // 
            numSecondBase.Location = new System.Drawing.Point(314, 50);
            numSecondBase.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numSecondBase.Name = "numSecondBase";
            numSecondBase.Size = new System.Drawing.Size(50, 27);
            numSecondBase.TabIndex = 6;
            numSecondBase.ValueChanged += NumInterval_ValueChanged;
            // 
            // numMinuteBase
            // 
            numMinuteBase.Location = new System.Drawing.Point(244, 50);
            numMinuteBase.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numMinuteBase.Name = "numMinuteBase";
            numMinuteBase.Size = new System.Drawing.Size(50, 27);
            numMinuteBase.TabIndex = 5;
            numMinuteBase.ValueChanged += NumInterval_ValueChanged;
            // 
            // numHourBase
            // 
            numHourBase.Location = new System.Drawing.Point(174, 50);
            numHourBase.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
            numHourBase.Name = "numHourBase";
            numHourBase.Size = new System.Drawing.Size(50, 27);
            numHourBase.TabIndex = 4;
            numHourBase.ValueChanged += NumInterval_ValueChanged;
            // 
            // dtpTime
            // 
            dtpTime.Location = new System.Drawing.Point(174, 50);
            dtpTime.Name = "dtpTime";
            dtpTime.Size = new System.Drawing.Size(280, 27);
            dtpTime.TabIndex = 7;
            dtpTime.ValueChanged += DtpTime_ValueChanged;
            // 
            // txtCustom
            // 
            txtCustom.Location = new System.Drawing.Point(174, 50);
            txtCustom.Name = "txtCustom";
            txtCustom.Size = new System.Drawing.Size(280, 27);
            txtCustom.TabIndex = 9;
            txtCustom.Visible = false;
            txtCustom.TextChanged += TxtCustom_TextChanged;
            // 
            // lblPreview
            // 
            lblPreview.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblPreview.Location = new System.Drawing.Point(174, 277);
            lblPreview.MaximumSize = new System.Drawing.Size(360, 0);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new System.Drawing.Size(280, 144);
            lblPreview.TabIndex = 10;
            lblPreview.Text = "No expression";
            // 
            // lblFrequency
            // 
            lblFrequency.AutoSize = true;
            lblFrequency.Location = new System.Drawing.Point(12, 15);
            lblFrequency.Name = "lblFrequency";
            lblFrequency.Size = new System.Drawing.Size(79, 20);
            lblFrequency.TabIndex = 0;
            lblFrequency.Text = "Frequency:";
            // 
            // lblInterval
            // 
            lblInterval.AutoSize = true;
            lblInterval.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblInterval.Location = new System.Drawing.Point(12, 93);
            lblInterval.Name = "lblInterval";
            lblInterval.Size = new System.Drawing.Size(68, 20);
            lblInterval.TabIndex = 2;
            lblInterval.Text = "Interval:";
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Location = new System.Drawing.Point(12, 57);
            lblTime.Name = "lblTime";
            lblTime.Size = new System.Drawing.Size(45, 20);
            lblTime.TabIndex = 4;
            lblTime.Text = "Time:";
            // 
            // lblDay
            // 
            lblDay.AutoSize = true;
            lblDay.Location = new System.Drawing.Point(12, 130);
            lblDay.Name = "lblDay";
            lblDay.Size = new System.Drawing.Size(38, 20);
            lblDay.TabIndex = 6;
            lblDay.Text = "Day:";
            // 
            // lblCustom
            // 
            lblCustom.AutoSize = true;
            lblCustom.Location = new System.Drawing.Point(12, 57);
            lblCustom.Name = "lblCustom";
            lblCustom.Size = new System.Drawing.Size(82, 20);
            lblCustom.TabIndex = 8;
            lblCustom.Text = "Expression:";
            // 
            // bttnOK
            // 
            bttnOK.Location = new System.Drawing.Point(288, 430);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(80, 30);
            bttnOK.TabIndex = 11;
            bttnOK.Text = "OK";
            bttnOK.Click += BttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(374, 430);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(80, 30);
            bttnCancel.TabIndex = 13;
            bttnCancel.Text = "Cancel";
            bttnCancel.Click += BttnCancel_Click;
            // 
            // chkDaysOfWeek
            // 
            chkDaysOfWeek.FormattingEnabled = true;
            chkDaysOfWeek.Location = new System.Drawing.Point(174, 119);
            chkDaysOfWeek.Name = "chkDaysOfWeek";
            chkDaysOfWeek.Size = new System.Drawing.Size(280, 114);
            chkDaysOfWeek.TabIndex = 14;
            // 
            // txtCron
            // 
            txtCron.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtCron.Location = new System.Drawing.Point(174, 241);
            txtCron.Name = "txtCron";
            txtCron.ReadOnly = true;
            txtCron.Size = new System.Drawing.Size(280, 20);
            txtCron.TabIndex = 15;
            // 
            // lblCron
            // 
            lblCron.AutoSize = true;
            lblCron.Location = new System.Drawing.Point(12, 246);
            lblCron.Name = "lblCron";
            lblCron.Size = new System.Drawing.Size(43, 20);
            lblCron.TabIndex = 16;
            lblCron.Text = "Cron:";
            // 
            // lblOffset
            // 
            lblOffset.AutoSize = true;
            lblOffset.Location = new System.Drawing.Point(12, 57);
            lblOffset.Name = "lblOffset";
            lblOffset.Size = new System.Drawing.Size(136, 20);
            lblOffset.TabIndex = 17;
            lblOffset.Text = "Offset (HH:MM:SS):";
            lblOffset.Visible = false;
            // 
            // numIntegerSeconds
            // 
            numIntegerSeconds.Location = new System.Drawing.Point(174, 50);
            numIntegerSeconds.Maximum = new decimal(new int[] { 86400, 0, 0, 0 });
            numIntegerSeconds.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numIntegerSeconds.Name = "numIntegerSeconds";
            numIntegerSeconds.Size = new System.Drawing.Size(140, 27);
            numIntegerSeconds.TabIndex = 18;
            numIntegerSeconds.Value = new decimal(new int[] { 60, 0, 0, 0 });
            numIntegerSeconds.Visible = false;
            numIntegerSeconds.ValueChanged += NumInterval_ValueChanged;
            // 
            // lblSeconds
            // 
            lblSeconds.AutoSize = true;
            lblSeconds.Location = new System.Drawing.Point(12, 57);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new System.Drawing.Size(67, 20);
            lblSeconds.TabIndex = 19;
            lblSeconds.Text = "Seconds:";
            lblSeconds.Visible = false;
            // 
            // CronExpressionBuilder
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(484, 483);
            Controls.Add(lblCron);
            Controls.Add(txtCron);
            Controls.Add(chkDaysOfWeek);
            Controls.Add(lblFrequency);
            Controls.Add(cboFrequency);
            Controls.Add(lblInterval);
            Controls.Add(numInterval);
            Controls.Add(lblOffset);
            Controls.Add(lblTime);
            Controls.Add(lblDay);
            Controls.Add(lblCustom);
            Controls.Add(lblPreview);
            Controls.Add(bttnOK);
            Controls.Add(bttnCancel);
            Controls.Add(txtCustom);
            Controls.Add(numHourBase);
            Controls.Add(dtpTime);
            Controls.Add(numMinuteBase);
            Controls.Add(numSecondBase);
            Controls.Add(numIntegerSeconds);
            Controls.Add(lblSeconds);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CronExpressionBuilder";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Cron Expression Builder";
            Load += CronExpressionBuilder_Load;
            ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSecondBase).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinuteBase).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHourBase).EndInit();
            ((System.ComponentModel.ISupportInitialize)numIntegerSeconds).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.ComboBox cboFrequency;
        private System.Windows.Forms.NumericUpDown numInterval;
        private System.Windows.Forms.NumericUpDown numSecondBase;
        private System.Windows.Forms.NumericUpDown numMinuteBase;
        private System.Windows.Forms.NumericUpDown numHourBase;
        private System.Windows.Forms.DateTimePicker dtpTime;
        private System.Windows.Forms.TextBox txtCustom;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Label lblOffset;
        private System.Windows.Forms.Label lblFrequency;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Label lblDay;
        private System.Windows.Forms.Label lblCustom;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.CheckedListBox chkDaysOfWeek;
        private System.Windows.Forms.TextBox txtCron;
        private System.Windows.Forms.Label lblCron;
        private System.Windows.Forms.NumericUpDown numIntegerSeconds;
        private System.Windows.Forms.Label lblSeconds;
    }
}
