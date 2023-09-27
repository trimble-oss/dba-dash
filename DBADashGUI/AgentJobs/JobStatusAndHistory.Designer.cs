namespace DBADashGUI.AgentJobs
{
    partial class JobStatusAndHistory
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
            agentJobsControl1 = new AgentJobsControl();
            SuspendLayout();
            // 
            // agentJobsControl1
            // 
            agentJobsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            agentJobsControl1.IncludeAcknowledged = true;
            agentJobsControl1.IncludeCritical = true;
            agentJobsControl1.IncludeNA = true;
            agentJobsControl1.IncludeOK = true;
            agentJobsControl1.IncludeWarning = true;
            agentJobsControl1.Location = new System.Drawing.Point(0, 0);
            agentJobsControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            agentJobsControl1.Name = "agentJobsControl1";
            agentJobsControl1.Size = new System.Drawing.Size(2170, 870);
            agentJobsControl1.TabIndex = 0;
            // 
            // JobStatusAndHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(2170, 870);
            Controls.Add(agentJobsControl1);
            Name = "JobStatusAndHistory";
            Text = "Job Status & History";
            ResumeLayout(false);
        }

        #endregion

        private AgentJobsControl agentJobsControl1;
    }
}