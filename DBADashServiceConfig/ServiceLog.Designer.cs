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
            this.components = new System.ComponentModel.Container();
            this.txtLogFilter = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnRefreshLog = new System.Windows.Forms.Button();
            this.cboLogs = new System.Windows.Forms.ComboBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.bttnNotepad = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLogFilter
            // 
            this.txtLogFilter.Location = new System.Drawing.Point(75, 23);
            this.txtLogFilter.Name = "txtLogFilter";
            this.txtLogFilter.Size = new System.Drawing.Size(144, 27);
            this.txtLogFilter.TabIndex = 9;
            this.txtLogFilter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtLogFilter_KeyDown);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(241, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 20);
            this.label2.TabIndex = 8;
            this.label2.Text = "Log File:";
            this.toolTip1.SetToolTip(this.label2, "Select a log file to read from");
            // 
            // bttnRefreshLog
            // 
            this.bttnRefreshLog.Image = global::DBADashServiceConfig.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.bttnRefreshLog.Location = new System.Drawing.Point(642, 23);
            this.bttnRefreshLog.Name = "bttnRefreshLog";
            this.bttnRefreshLog.Size = new System.Drawing.Size(41, 29);
            this.bttnRefreshLog.TabIndex = 7;
            this.toolTip1.SetToolTip(this.bttnRefreshLog, "Click to get latest entries from the log file");
            this.bttnRefreshLog.UseVisualStyleBackColor = true;
            this.bttnRefreshLog.Click += new System.EventHandler(this.BttnRefreshLog_Click);
            // 
            // cboLogs
            // 
            this.cboLogs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLogs.FormattingEnabled = true;
            this.cboLogs.Location = new System.Drawing.Point(311, 23);
            this.cboLogs.Name = "cboLogs";
            this.cboLogs.Size = new System.Drawing.Size(325, 28);
            this.cboLogs.TabIndex = 6;
            this.toolTip1.SetToolTip(this.cboLogs, "Select a log file to read from");
            this.cboLogs.SelectedIndexChanged += new System.EventHandler(this.CboLogs_SelectedIndexChanged);
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Lucida Console", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtLog.ForeColor = System.Drawing.Color.White;
            this.txtLog.Location = new System.Drawing.Point(0, 77);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(1083, 703);
            this.txtLog.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 20);
            this.label1.TabIndex = 10;
            this.label1.Text = "Filter:";
            this.toolTip1.SetToolTip(this.label1, "Enter text to filter by or leave empty to return the full log");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.bttnNotepad);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.txtLogFilter);
            this.panel1.Controls.Add(this.cboLogs);
            this.panel1.Controls.Add(this.bttnRefreshLog);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1083, 77);
            this.panel1.TabIndex = 11;
            // 
            // bttnNotepad
            // 
            this.bttnNotepad.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.bttnNotepad.Location = new System.Drawing.Point(689, 23);
            this.bttnNotepad.Name = "bttnNotepad";
            this.bttnNotepad.Size = new System.Drawing.Size(105, 29);
            this.bttnNotepad.TabIndex = 11;
            this.bttnNotepad.Text = "Notepad";
            this.toolTip1.SetToolTip(this.bttnNotepad, "Open log in Notepad");
            this.bttnNotepad.UseVisualStyleBackColor = true;
            this.bttnNotepad.Click += new System.EventHandler(this.BttnNotepad_Click);
            // 
            // ServiceLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1083, 780);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.panel1);
            this.Name = "ServiceLog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Service Log";
            this.Load += new System.EventHandler(this.ServiceLog_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}