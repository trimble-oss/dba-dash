namespace DBADashGUI.AgentJobs
{
    partial class JobExecutionDialog
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
            label1 = new System.Windows.Forms.Label();
            cboStep = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            bttnStart = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            lblStartJobStatus = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            bttnStop = new System.Windows.Forms.Button();
            lblNextUpdateIn = new System.Windows.Forms.Label();
            txtStartStatus = new System.Windows.Forms.TextBox();
            txtExecuteStatus = new System.Windows.Forms.TextBox();
            timer1 = new System.Windows.Forms.Timer(components);
            lblTimer = new System.Windows.Forms.Label();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            lnkJobName = new System.Windows.Forms.LinkLabel();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 21);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(76, 20);
            label1.TabIndex = 0;
            label1.Text = "Job Name";
            // 
            // cboStep
            // 
            cboStep.FormattingEnabled = true;
            cboStep.Location = new System.Drawing.Point(99, 64);
            cboStep.Name = "cboStep";
            cboStep.Size = new System.Drawing.Size(499, 28);
            cboStep.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 67);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(81, 20);
            label2.TabIndex = 2;
            label2.Text = "Start Step: ";
            // 
            // bttnStart
            // 
            bttnStart.Location = new System.Drawing.Point(304, 106);
            bttnStart.Name = "bttnStart";
            bttnStart.Size = new System.Drawing.Size(94, 29);
            bttnStart.TabIndex = 4;
            bttnStart.Text = "&Start";
            bttnStart.UseVisualStyleBackColor = true;
            bttnStart.Click += Start_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(504, 106);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 5;
            bttnCancel.Text = "&Close";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += Cancel_Click;
            // 
            // lblStartJobStatus
            // 
            lblStartJobStatus.AutoSize = true;
            lblStartJobStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblStartJobStatus.Location = new System.Drawing.Point(12, 156);
            lblStartJobStatus.Name = "lblStartJobStatus";
            lblStartJobStatus.Size = new System.Drawing.Size(124, 20);
            lblStartJobStatus.TabIndex = 6;
            lblStartJobStatus.Text = "Start Job Status:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label3.Location = new System.Drawing.Point(12, 218);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(144, 20);
            label3.TabIndex = 7;
            label3.Text = "Execute Job Status:";
            // 
            // bttnStop
            // 
            bttnStop.Location = new System.Drawing.Point(404, 106);
            bttnStop.Name = "bttnStop";
            bttnStop.Size = new System.Drawing.Size(94, 29);
            bttnStop.TabIndex = 10;
            bttnStop.Text = "&Stop";
            bttnStop.UseVisualStyleBackColor = true;
            bttnStop.Click += Stop_Click;
            // 
            // lblNextUpdateIn
            // 
            lblNextUpdateIn.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            lblNextUpdateIn.AutoSize = true;
            lblNextUpdateIn.Location = new System.Drawing.Point(457, 319);
            lblNextUpdateIn.Name = "lblNextUpdateIn";
            lblNextUpdateIn.Size = new System.Drawing.Size(118, 20);
            lblNextUpdateIn.TabIndex = 11;
            lblNextUpdateIn.Text = "Next Update in...";
            lblNextUpdateIn.Visible = false;
            // 
            // txtStartStatus
            // 
            txtStartStatus.Location = new System.Drawing.Point(12, 179);
            txtStartStatus.Name = "txtStartStatus";
            txtStartStatus.Size = new System.Drawing.Size(586, 27);
            txtStartStatus.TabIndex = 12;
            // 
            // txtExecuteStatus
            // 
            txtExecuteStatus.Location = new System.Drawing.Point(12, 241);
            txtExecuteStatus.Multiline = true;
            txtExecuteStatus.Name = "txtExecuteStatus";
            txtExecuteStatus.Size = new System.Drawing.Size(586, 75);
            txtExecuteStatus.TabIndex = 13;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += Timer_Tick;
            // 
            // lblTimer
            // 
            lblTimer.AutoSize = true;
            lblTimer.Location = new System.Drawing.Point(10, 319);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new System.Drawing.Size(63, 20);
            lblTimer.TabIndex = 14;
            lblTimer.Text = "00:00:00";
            toolTip1.SetToolTip(lblTimer, "Time since job started until status monitor detects that the job has completed. This might vary slightly from duration in job history.");
            // 
            // lnkJobName
            // 
            lnkJobName.AutoSize = true;
            lnkJobName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            lnkJobName.Location = new System.Drawing.Point(99, 21);
            lnkJobName.Name = "lnkJobName";
            lnkJobName.Size = new System.Drawing.Size(92, 20);
            lnkJobName.TabIndex = 15;
            lnkJobName.TabStop = true;
            lnkJobName.Text = "{Job Name}";
            lnkJobName.LinkClicked += JobLink_Clicked;
            // 
            // JobExecutionDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(611, 357);
            Controls.Add(lnkJobName);
            Controls.Add(lblTimer);
            Controls.Add(txtExecuteStatus);
            Controls.Add(txtStartStatus);
            Controls.Add(lblNextUpdateIn);
            Controls.Add(bttnStop);
            Controls.Add(label3);
            Controls.Add(lblStartJobStatus);
            Controls.Add(bttnCancel);
            Controls.Add(bttnStart);
            Controls.Add(label2);
            Controls.Add(cboStep);
            Controls.Add(label1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "JobExecutionDialog";
            Text = "Execute Job";
            Load += JobExecutionDialog_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboStep;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnStart;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label lblStartJobStatus;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bttnStop;
        private System.Windows.Forms.Label lblNextUpdateIn;
        private System.Windows.Forms.TextBox txtStartStatus;
        private System.Windows.Forms.TextBox txtExecuteStatus;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblTimer;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.LinkLabel lnkJobName;
    }
}