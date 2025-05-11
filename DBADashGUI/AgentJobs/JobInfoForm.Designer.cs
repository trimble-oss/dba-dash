namespace DBADashGUI.AgentJobs
{
    partial class JobInfoForm
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
            tabs = new DBADashGUI.Theme.ThemedTabControl();
            tabInfo = new System.Windows.Forms.TabPage();
            jobInfo1 = new JobInfo();
            tabStatus = new System.Windows.Forms.TabPage();
            agentJobsControl1 = new AgentJobsControl();
            tabDDL = new System.Windows.Forms.TabPage();
            jobddlHistory1 = new DBADashGUI.Changes.JobDDLHistory();
            tabStats = new System.Windows.Forms.TabPage();
            jobStats1 = new JobStats();
            tabTimeline = new System.Windows.Forms.TabPage();
            jobTimeline1 = new JobTimeline();
            tabs.SuspendLayout();
            tabInfo.SuspendLayout();
            tabStatus.SuspendLayout();
            tabDDL.SuspendLayout();
            tabStats.SuspendLayout();
            tabTimeline.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(tabInfo);
            tabs.Controls.Add(tabStatus);
            tabs.Controls.Add(tabDDL);
            tabs.Controls.Add(tabStats);
            tabs.Controls.Add(tabTimeline);
            tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tabs.Location = new System.Drawing.Point(0, 0);
            tabs.Name = "tabs";
            tabs.Padding = new System.Drawing.Point(20, 8);
            tabs.SelectedIndex = 0;
            tabs.Size = new System.Drawing.Size(1380, 947);
            tabs.TabIndex = 0;
            tabs.SelectedIndexChanged += Tab_TabIndexChanged;
            // 
            // tabInfo
            // 
            tabInfo.Controls.Add(jobInfo1);
            tabInfo.Location = new System.Drawing.Point(4, 39);
            tabInfo.Name = "tabInfo";
            tabInfo.Padding = new System.Windows.Forms.Padding(3);
            tabInfo.Size = new System.Drawing.Size(1372, 904);
            tabInfo.TabIndex = 0;
            tabInfo.Text = "Job Info";
            tabInfo.UseVisualStyleBackColor = true;
            // 
            // jobInfo1
            // 
            jobInfo1.CustomTitle = null;
            jobInfo1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobInfo1.Location = new System.Drawing.Point(3, 3);
            jobInfo1.Name = "jobInfo1";
            jobInfo1.Size = new System.Drawing.Size(1366, 898);
            jobInfo1.TabIndex = 0;
            // 
            // tabStatus
            // 
            tabStatus.Controls.Add(agentJobsControl1);
            tabStatus.Location = new System.Drawing.Point(4, 39);
            tabStatus.Name = "tabStatus";
            tabStatus.Padding = new System.Windows.Forms.Padding(3);
            tabStatus.Size = new System.Drawing.Size(1372, 904);
            tabStatus.TabIndex = 1;
            tabStatus.Text = "Job Status";
            tabStatus.UseVisualStyleBackColor = true;
            // 
            // agentJobsControl1
            // 
            agentJobsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            agentJobsControl1.IncludeAcknowledged = true;
            agentJobsControl1.IncludeCritical = true;
            agentJobsControl1.IncludeNA = true;
            agentJobsControl1.IncludeOK = true;
            agentJobsControl1.IncludeWarning = true;
            agentJobsControl1.Location = new System.Drawing.Point(3, 3);
            agentJobsControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            agentJobsControl1.Name = "agentJobsControl1";
            agentJobsControl1.ShowSteps = false;
            agentJobsControl1.Size = new System.Drawing.Size(1366, 898);
            agentJobsControl1.TabIndex = 0;
            // 
            // tabDDL
            // 
            tabDDL.Controls.Add(jobddlHistory1);
            tabDDL.Location = new System.Drawing.Point(4, 39);
            tabDDL.Name = "tabDDL";
            tabDDL.Size = new System.Drawing.Size(1372, 904);
            tabDDL.TabIndex = 2;
            tabDDL.Text = "Job DDL";
            tabDDL.UseVisualStyleBackColor = true;
            // 
            // jobddlHistory1
            // 
            jobddlHistory1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobddlHistory1.Location = new System.Drawing.Point(0, 0);
            jobddlHistory1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            jobddlHistory1.Name = "jobddlHistory1";
            jobddlHistory1.Size = new System.Drawing.Size(1372, 904);
            jobddlHistory1.TabIndex = 0;
            // 
            // tabStats
            // 
            tabStats.Controls.Add(jobStats1);
            tabStats.Location = new System.Drawing.Point(4, 39);
            tabStats.Name = "tabStats";
            tabStats.Size = new System.Drawing.Size(1372, 904);
            tabStats.TabIndex = 3;
            tabStats.Text = "Job Stats";
            tabStats.UseVisualStyleBackColor = true;
            // 
            // jobStats1
            // 
            jobStats1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobStats1.Location = new System.Drawing.Point(0, 0);
            jobStats1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            jobStats1.Name = "jobStats1";
            jobStats1.Size = new System.Drawing.Size(1372, 904);
            jobStats1.TabIndex = 0;
            jobStats1.UseGlobalTime = true;
            // 
            // tabTimeline
            // 
            tabTimeline.Controls.Add(jobTimeline1);
            tabTimeline.Location = new System.Drawing.Point(4, 39);
            tabTimeline.Name = "tabTimeline";
            tabTimeline.Size = new System.Drawing.Size(1372, 904);
            tabTimeline.TabIndex = 4;
            tabTimeline.Text = "Timeline";
            tabTimeline.UseVisualStyleBackColor = true;
            // 
            // jobTimeline1
            // 
            jobTimeline1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobTimeline1.IsActive = false;
            jobTimeline1.Location = new System.Drawing.Point(0, 0);
            jobTimeline1.Name = "jobTimeline1";
            jobTimeline1.Size = new System.Drawing.Size(1372, 904);
            jobTimeline1.TabIndex = 0;
            jobTimeline1.UseGlobalTime = true;
            // 
            // JobInfoForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1380, 947);
            Controls.Add(tabs);
            Name = "JobInfoForm";
            Text = "{Job}";
            Load += JobInfoForm_Load;
            tabs.ResumeLayout(false);
            tabInfo.ResumeLayout(false);
            tabStatus.ResumeLayout(false);
            tabDDL.ResumeLayout(false);
            tabStats.ResumeLayout(false);
            tabTimeline.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Theme.ThemedTabControl tabs;
        private System.Windows.Forms.TabPage tabInfo;
        private System.Windows.Forms.TabPage tabStatus;
        private System.Windows.Forms.TabPage tabDDL;
        private System.Windows.Forms.TabPage tabStats;
        private System.Windows.Forms.TabPage tabTimeline;
        private JobInfo jobInfo1;
        private AgentJobsControl agentJobsControl1;
        private Changes.JobDDLHistory jobddlHistory1;
        private JobStats jobStats1;
        private JobTimeline jobTimeline1;
    }
}