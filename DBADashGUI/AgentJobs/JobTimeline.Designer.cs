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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobTimeline));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopyDropDown = new System.Windows.Forms.ToolStripDropDownButton();
            hTMLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsCategories = new System.Windows.Forms.ToolStripDropDownButton();
            tsIncludeSteps = new System.Windows.Forms.ToolStripDropDownButton();
            outcomeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stepsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bothToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            dateRangeToolStripMenuItem1 = new DateRangeToolStripMenuItem();
            WebView2Wrapper1 = new WebView2Wrapper();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopyDropDown, tsCategories, tsIncludeSteps, tsDateGroup, dateRangeToolStripMenuItem1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1065, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCopyDropDown
            // 
            tsCopyDropDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyDropDown.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { hTMLToolStripMenuItem, imageToolStripMenuItem });
            tsCopyDropDown.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyDropDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyDropDown.Name = "tsCopyDropDown";
            tsCopyDropDown.Size = new System.Drawing.Size(34, 24);
            tsCopyDropDown.Text = "Copy";
            // 
            // hTMLToolStripMenuItem
            // 
            hTMLToolStripMenuItem.Name = "hTMLToolStripMenuItem";
            hTMLToolStripMenuItem.Size = new System.Drawing.Size(134, 26);
            hTMLToolStripMenuItem.Text = "HTML";
            hTMLToolStripMenuItem.Click += Copy_HTML;
            // 
            // imageToolStripMenuItem
            // 
            imageToolStripMenuItem.Name = "imageToolStripMenuItem";
            imageToolStripMenuItem.Size = new System.Drawing.Size(134, 26);
            imageToolStripMenuItem.Text = "Image";
            imageToolStripMenuItem.Click += Copy_Image;
            // 
            // tsCategories
            // 
            tsCategories.Image = Properties.Resources.FilterDropdown_16x;
            tsCategories.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCategories.Name = "tsCategories";
            tsCategories.Size = new System.Drawing.Size(142, 24);
            tsCategories.Text = "ALL Categories";
            // 
            // tsIncludeSteps
            // 
            tsIncludeSteps.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { outcomeToolStripMenuItem, stepsToolStripMenuItem, bothToolStripMenuItem });
            tsIncludeSteps.Image = Properties.Resources.SettingsOutline_16x;
            tsIncludeSteps.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsIncludeSteps.Name = "tsIncludeSteps";
            tsIncludeSteps.Size = new System.Drawing.Size(104, 24);
            tsIncludeSteps.Text = "Outcome";
            // 
            // outcomeToolStripMenuItem
            // 
            outcomeToolStripMenuItem.Checked = true;
            outcomeToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            outcomeToolStripMenuItem.Name = "outcomeToolStripMenuItem";
            outcomeToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
            outcomeToolStripMenuItem.Text = "Outcome";
            outcomeToolStripMenuItem.Click += Include_Steps;
            // 
            // stepsToolStripMenuItem
            // 
            stepsToolStripMenuItem.Name = "stepsToolStripMenuItem";
            stepsToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
            stepsToolStripMenuItem.Text = "Steps";
            stepsToolStripMenuItem.Click += Include_Steps;
            // 
            // bothToolStripMenuItem
            // 
            bothToolStripMenuItem.Name = "bothToolStripMenuItem";
            bothToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
            bothToolStripMenuItem.Text = "Both";
            bothToolStripMenuItem.Click += Include_Steps;
            // 
            // tsDateGroup
            // 
            tsDateGroup.Image = Properties.Resources.Time_16x;
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(79, 24);
            tsDateGroup.Tag = "0";
            tsDateGroup.Text = "None";
            // 
            // dateRangeToolStripMenuItem1
            // 
            dateRangeToolStripMenuItem1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            dateRangeToolStripMenuItem1.DefaultTimeSpan = System.TimeSpan.Parse("01:00:00");
            dateRangeToolStripMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            dateRangeToolStripMenuItem1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dateRangeToolStripMenuItem1.Image = (System.Drawing.Image)resources.GetObject("dateRangeToolStripMenuItem1.Image");
            dateRangeToolStripMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta;
            dateRangeToolStripMenuItem1.MaximumTimeSpan = System.TimeSpan.Parse("10675199.02:48:05.4775807");
            dateRangeToolStripMenuItem1.MinimumTimeSpan = System.TimeSpan.Parse("-10675199.02:48:05.4775808");
            dateRangeToolStripMenuItem1.Name = "dateRangeToolStripMenuItem1";
            dateRangeToolStripMenuItem1.SelectedTimeSpan = System.TimeSpan.Parse("01:00:00");
            dateRangeToolStripMenuItem1.Size = new System.Drawing.Size(71, 24);
            dateRangeToolStripMenuItem1.Text = "1 Hr";
            dateRangeToolStripMenuItem1.Visible = false;
            dateRangeToolStripMenuItem1.DateRangeChanged += DateRangeChanged;
            // 
            // WebView2Wrapper1
            // 
            WebView2Wrapper1.Dock = System.Windows.Forms.DockStyle.Fill;
            WebView2Wrapper1.Location = new System.Drawing.Point(0, 27);
            WebView2Wrapper1.Name = "WebView2Wrapper1";
            WebView2Wrapper1.Size = new System.Drawing.Size(1065, 697);
            WebView2Wrapper1.TabIndex = 2;
            WebView2Wrapper1.SetupCompleted += WebView2_SetupCompleted;
            // 
            // JobTimeline
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(WebView2Wrapper1);
            Controls.Add(toolStrip1);
            Name = "JobTimeline";
            Size = new System.Drawing.Size(1065, 724);
            Load += JobTimeline_Load;
            Resize += JobTimeLine_Resize;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsCategories;
        private System.Windows.Forms.ToolStripDropDownButton tsIncludeSteps;
        private System.Windows.Forms.ToolStripMenuItem outcomeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stepsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsCopyDropDown;
        private System.Windows.Forms.ToolStripMenuItem hTMLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bothToolStripMenuItem;
        private WebView2Wrapper WebView2Wrapper1;
        private DateRangeToolStripMenuItem dateRangeToolStripMenuItem1;
    }
}
