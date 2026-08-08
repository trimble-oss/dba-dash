namespace DBADashGUI.Performance
{
    partial class SessionDetailViewer
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
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsPlan = new System.Windows.Forms.ToolStripButton();
            tsJobInfo = new System.Windows.Forms.ToolStripButton();
            tsKill = new System.Windows.Forms.ToolStripButton();
            tsLatest = new System.Windows.Forms.ToolStripButton();
            tsCollectNow = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            tabs = new DBADashGUI.Theme.ThemedTabControl();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsBack, tsLatest, tsCollectNow, tsPlan, tsJobInfo, tsKill });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1000, 27);
            toolStrip1.TabIndex = 0;
            // 
            // tsPlan
            // 
            tsPlan.Image = Properties.Resources.query_plan;
            tsPlan.Name = "tsPlan";
            tsPlan.Size = new System.Drawing.Size(61, 24);
            tsPlan.Text = "Plan";
            tsPlan.Click += TsPlan_Click;
            // 
            // tsJobInfo
            // 
            tsJobInfo.Image = Properties.Resources.Information_blue_6227_16x16;
            tsJobInfo.Name = "tsJobInfo";
            tsJobInfo.Size = new System.Drawing.Size(80, 24);
            tsJobInfo.Text = "Job Info";
            tsJobInfo.Visible = false;
            tsJobInfo.Click += TsJobInfo_Click;
            //
            // tsKill
            //
            tsKill.Image = Properties.Resources.Close_red_16x;
            tsKill.Name = "tsKill";
            tsKill.Size = new System.Drawing.Size(80, 24);
            tsKill.Text = "Kill";
            tsKill.ToolTipText = "Kill this session (SPID) on the source instance.";
            tsKill.Visible = false;
            tsKill.Click += TsKill_Click;
            //
            // tsLatest
            //
            tsLatest.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsLatest.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLatest.Name = "tsLatest";
            tsLatest.Size = new System.Drawing.Size(80, 24);
            tsLatest.Text = "Get Latest";
            tsLatest.ToolTipText = "Load the most recent snapshot that contains this session.";
            tsLatest.Click += TsLatest_Click;
            //
            // tsCollectNow
            //
            tsCollectNow.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsCollectNow.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCollectNow.Name = "tsCollectNow";
            tsCollectNow.Size = new System.Drawing.Size(80, 24);
            tsCollectNow.Text = "Trigger Collection";
            tsCollectNow.ToolTipText = "Trigger Collection & Refresh\r\nThis avoids the need to wait for the next collection by sending a message to the service to trigger the collection to run immediately.";
            tsCollectNow.Click += TsCollectNow_Click;
            //
            // tsBack
            //
            tsBack.Image = Properties.Resources.arrow_back_16xLG;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(80, 24);
            tsBack.Text = "Back";
            tsBack.ToolTipText = "Return to the previously viewed snapshot.";
            tsBack.Enabled = false;
            tsBack.Click += TsBack_Click;
            //
            // tsStatus
            // 
            tsStatus.Name = "tsStatus";
            tsStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 942);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1000, 22);
            statusStrip1.TabIndex = 2;
            // 
            // tabs
            // 
            tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tabs.Location = new System.Drawing.Point(0, 27);
            tabs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tabs.Name = "tabs";
            tabs.Padding = new System.Drawing.Point(20, 8);
            tabs.SelectedIndex = 0;
            tabs.Size = new System.Drawing.Size(1000, 915);
            tabs.TabIndex = 1;
            // 
            // SessionDetailViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1000, 964);
            Controls.Add(tabs);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "SessionDetailViewer";
            Text = "Session Detail";
            Load += SessionDetailViewer_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsPlan;
        private System.Windows.Forms.ToolStripButton tsJobInfo;
        private System.Windows.Forms.ToolStripButton tsKill;
        private System.Windows.Forms.ToolStripButton tsLatest;
        private System.Windows.Forms.ToolStripButton tsCollectNow;
        private System.Windows.Forms.ToolStripButton tsBack;
        private DBADashGUI.Theme.ThemedTabControl tabs;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
    }
}
