﻿namespace DBADashGUI.DBADashAlerts
{
    partial class ActiveAlerts
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
            customReportView1 = new CustomReports.CustomReportView();
            SuspendLayout();
            // 
            // customReportView1
            // 
            customReportView1.AutoScroll = true;
            customReportView1.AutoSize = true;
            customReportView1.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            customReportView1.Dock = System.Windows.Forms.DockStyle.Fill;
            customReportView1.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            customReportView1.Location = new System.Drawing.Point(0, 0);
            customReportView1.Name = "customReportView1";
            customReportView1.Report = null;
            customReportView1.Size = new System.Drawing.Size(1337, 750);
            customReportView1.TabIndex = 0;
            // 
            // ActiveAlerts
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(customReportView1);
            Name = "ActiveAlerts";
            Size = new System.Drawing.Size(1337, 750);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CustomReports.CustomReportView customReportView1;
    }
}
