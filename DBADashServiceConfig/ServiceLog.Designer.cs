namespace DBADashServiceConfig
{
    partial class ServiceLog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceLog));
            txtLogFilter = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            bttnRefreshLog = new System.Windows.Forms.Button();
            cboLogs = new System.Windows.Forms.ComboBox();
            txtLog = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            chkWatch = new System.Windows.Forms.CheckBox();
            lnkSwitchFormat = new System.Windows.Forms.LinkLabel();
            bttnNotepad = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtLogFilter
            // 
            txtLogFilter.Location = new System.Drawing.Point(75, 23);
            txtLogFilter.Name = "txtLogFilter";
            txtLogFilter.Size = new System.Drawing.Size(144, 27);
            txtLogFilter.TabIndex = 9;
            txtLogFilter.KeyDown += TxtLogFilter_KeyDown;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(241, 23);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(64, 20);
            label2.TabIndex = 8;
            label2.Text = "Log File:";
            toolTip1.SetToolTip(label2, "Select a log file to read from");
            // 
            // bttnRefreshLog
            // 
            bttnRefreshLog.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            bttnRefreshLog.Location = new System.Drawing.Point(642, 23);
            bttnRefreshLog.Name = "bttnRefreshLog";
            bttnRefreshLog.Size = new System.Drawing.Size(41, 29);
            bttnRefreshLog.TabIndex = 7;
            toolTip1.SetToolTip(bttnRefreshLog, "Click to get latest entries from the log file");
            bttnRefreshLog.UseVisualStyleBackColor = true;
            bttnRefreshLog.Click += BttnRefreshLog_Click;
            // 
            // cboLogs
            // 
            cboLogs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboLogs.FormattingEnabled = true;
            cboLogs.Location = new System.Drawing.Point(311, 23);
            cboLogs.Name = "cboLogs";
            cboLogs.Size = new System.Drawing.Size(325, 28);
            cboLogs.TabIndex = 6;
            toolTip1.SetToolTip(cboLogs, "Select a log file to read from");
            cboLogs.SelectedIndexChanged += CboLogs_SelectedIndexChanged;
            // 
            // txtLog
            // 
            txtLog.BackColor = System.Drawing.Color.Black;
            txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            txtLog.Font = new System.Drawing.Font("Lucida Console", 10F);
            txtLog.ForeColor = System.Drawing.Color.White;
            txtLog.Location = new System.Drawing.Point(0, 77);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtLog.Size = new System.Drawing.Size(1124, 703);
            txtLog.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 23);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(45, 20);
            label1.TabIndex = 10;
            label1.Text = "Filter:";
            toolTip1.SetToolTip(label1, "Enter text to filter by or leave empty to return the full log");
            // 
            // panel1
            // 
            panel1.Controls.Add(chkWatch);
            panel1.Controls.Add(lnkSwitchFormat);
            panel1.Controls.Add(bttnNotepad);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(txtLogFilter);
            panel1.Controls.Add(cboLogs);
            panel1.Controls.Add(bttnRefreshLog);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1124, 77);
            panel1.TabIndex = 11;
            // 
            // chkWatch
            // 
            chkWatch.AutoSize = true;
            chkWatch.Location = new System.Drawing.Point(689, 26);
            chkWatch.Name = "chkWatch";
            chkWatch.Size = new System.Drawing.Size(72, 24);
            chkWatch.TabIndex = 13;
            chkWatch.Text = "Watch";
            chkWatch.UseVisualStyleBackColor = true;
            // 
            // lnkSwitchFormat
            // 
            lnkSwitchFormat.AutoSize = true;
            lnkSwitchFormat.Location = new System.Drawing.Point(887, 27);
            lnkSwitchFormat.Name = "lnkSwitchFormat";
            lnkSwitchFormat.Size = new System.Drawing.Size(225, 20);
            lnkSwitchFormat.TabIndex = 12;
            lnkSwitchFormat.TabStop = true;
            lnkSwitchFormat.Text = "Switch service log format to Text";
            lnkSwitchFormat.Visible = false;
            lnkSwitchFormat.LinkClicked += LnkSwitchFormat_LinkClicked;
            // 
            // bttnNotepad
            // 
            bttnNotepad.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            bttnNotepad.Location = new System.Drawing.Point(767, 23);
            bttnNotepad.Name = "bttnNotepad";
            bttnNotepad.Size = new System.Drawing.Size(105, 29);
            bttnNotepad.TabIndex = 11;
            bttnNotepad.Text = "Notepad";
            toolTip1.SetToolTip(bttnNotepad, "Open log in Notepad");
            bttnNotepad.UseVisualStyleBackColor = true;
            bttnNotepad.Click += BttnNotepad_Click;
            // 
            // ServiceLog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1124, 780);
            Controls.Add(txtLog);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "ServiceLog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Service Log";
            Load += ServiceLog_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLogFilter;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnRefreshLog;
        private System.Windows.Forms.ComboBox cboLogs;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button bttnNotepad;
        private System.Windows.Forms.LinkLabel lnkSwitchFormat;
        private System.Windows.Forms.CheckBox chkWatch;
    }
}