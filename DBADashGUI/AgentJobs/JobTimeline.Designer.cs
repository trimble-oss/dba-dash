namespace DBADashGUI.AgentJobs
{
    partial class JobTimeline
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
            this.webCtrl = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsCategories = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsIncludeSteps = new System.Windows.Forms.ToolStripDropDownButton();
            this.outcomeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.webCtrl)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // webCtrl
            // 
            this.webCtrl.AllowExternalDrop = true;
            this.webCtrl.CreationProperties = null;
            this.webCtrl.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webCtrl.Location = new System.Drawing.Point(0, 27);
            this.webCtrl.Name = "webCtrl";
            this.webCtrl.Size = new System.Drawing.Size(1065, 697);
            this.webCtrl.TabIndex = 0;
            this.webCtrl.ZoomFactor = 1D;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsCategories,
            this.tsIncludeSteps});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1065, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.TsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy HTML";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
            // 
            // tsCategories
            // 
            this.tsCategories.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsCategories.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCategories.Name = "tsCategories";
            this.tsCategories.Size = new System.Drawing.Size(142, 24);
            this.tsCategories.Text = "ALL Categories";
            // 
            // tsIncludeSteps
            // 
            this.tsIncludeSteps.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.outcomeToolStripMenuItem,
            this.stepsToolStripMenuItem});
            this.tsIncludeSteps.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsIncludeSteps.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIncludeSteps.Name = "tsIncludeSteps";
            this.tsIncludeSteps.Size = new System.Drawing.Size(104, 24);
            this.tsIncludeSteps.Text = "Outcome";
            // 
            // outcomeToolStripMenuItem
            // 
            this.outcomeToolStripMenuItem.Checked = true;
            this.outcomeToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.outcomeToolStripMenuItem.Name = "outcomeToolStripMenuItem";
            this.outcomeToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.outcomeToolStripMenuItem.Text = "Outcome";
            this.outcomeToolStripMenuItem.Click += new System.EventHandler(this.Include_Steps);
            // 
            // stepsToolStripMenuItem
            // 
            this.stepsToolStripMenuItem.Name = "stepsToolStripMenuItem";
            this.stepsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.stepsToolStripMenuItem.Text = "Steps";
            this.stepsToolStripMenuItem.Click += new System.EventHandler(this.Include_Steps);
            // 
            // JobTimeline
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.webCtrl);
            this.Controls.Add(this.toolStrip1);
            this.Name = "JobTimeline";
            this.Size = new System.Drawing.Size(1065, 724);
            this.Resize += new System.EventHandler(this.JobTimeLine_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.webCtrl)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webCtrl;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsCategories;
        private System.Windows.Forms.ToolStripDropDownButton tsIncludeSteps;
        private System.Windows.Forms.ToolStripMenuItem outcomeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stepsToolStripMenuItem;
    }
}
