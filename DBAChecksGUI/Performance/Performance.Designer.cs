namespace DBAChecksGUI.Performance
{
    partial class Performance
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Performance));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days14toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days28ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsEnableTimer = new System.Windows.Forms.ToolStripButton();
            this.tsDisableTimer = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            this.minToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.hrToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hrsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.objectExecution1 = new DBAChecksGUI.Performance.ObjectExecution();
            this.blocking1 = new DBAChecksGUI.Performance.Blocking();
            this.waits1 = new DBAChecksGUI.Performance.Waits();
            this.ioPerformance1 = new DBAChecksGUI.Performance.IOPerformance();
            this.cpu1 = new DBAChecksGUI.Performance.CPU();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTime,
            this.tsEnableTimer,
            this.tsDisableTimer,
            this.toolStripDropDownButton1,
            this.tsRefresh,
            this.tsGrouping});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(883, 31);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsTime
            // 
            this.tsTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.days7ToolStripMenuItem,
            this.days14toolStripMenuItem,
            this.days28ToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBAChecksGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 28);
            this.tsTime.Text = "Time";
            this.tsTime.Click += new System.EventHandler(this.tsTime_Click_1);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(144, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(144, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(144, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(144, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(144, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(144, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days14toolStripMenuItem
            // 
            this.days14toolStripMenuItem.Name = "days14toolStripMenuItem";
            this.days14toolStripMenuItem.Size = new System.Drawing.Size(144, 26);
            this.days14toolStripMenuItem.Tag = "20160";
            this.days14toolStripMenuItem.Text = "14 Days";
            this.days14toolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days28ToolStripMenuItem
            // 
            this.days28ToolStripMenuItem.Name = "days28ToolStripMenuItem";
            this.days28ToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
            this.days28ToolStripMenuItem.Tag = "40320";
            this.days28ToolStripMenuItem.Text = "28 Days";
            this.days28ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(141, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(144, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.customToolStripMenuItem_Click);
            // 
            // tsEnableTimer
            // 
            this.tsEnableTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsEnableTimer.Image = global::DBAChecksGUI.Properties.Resources.StartTime_16x;
            this.tsEnableTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsEnableTimer.Name = "tsEnableTimer";
            this.tsEnableTimer.Size = new System.Drawing.Size(29, 28);
            this.tsEnableTimer.Text = "Enable auto refresh timer";
            this.tsEnableTimer.Click += new System.EventHandler(this.tsEnableTimer_Click);
            // 
            // tsDisableTimer
            // 
            this.tsDisableTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDisableTimer.Image = global::DBAChecksGUI.Properties.Resources.StopTime_16x;
            this.tsDisableTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDisableTimer.Name = "tsDisableTimer";
            this.tsDisableTimer.Size = new System.Drawing.Size(29, 28);
            this.tsDisableTimer.Text = "Disable auto refresh timer";
            this.tsDisableTimer.Click += new System.EventHandler(this.tsDisableTimer_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smoothLinesToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBAChecksGUI.Properties.Resources.LineChart_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 28);
            this.toolStripDropDownButton1.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            this.smoothLinesToolStripMenuItem.Checked = true;
            this.smoothLinesToolStripMenuItem.CheckOnClick = true;
            this.smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            this.smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.smoothLinesToolStripMenuItem.Text = "Smooth Lines";
            this.smoothLinesToolStripMenuItem.Click += new System.EventHandler(this.smoothLinesToolStripMenuItem_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 28);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsGrouping
            // 
            this.tsGrouping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsGrouping.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minToolStripMenuItem,
            this.minToolStripMenuItem1,
            this.hrToolStripMenuItem,
            this.hrsToolStripMenuItem,
            this.dayToolStripMenuItem1});
            this.tsGrouping.Image = ((System.Drawing.Image)(resources.GetObject("tsGrouping.Image")));
            this.tsGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGrouping.Name = "tsGrouping";
            this.tsGrouping.Size = new System.Drawing.Size(100, 28);
            this.tsGrouping.Text = "Date Group";
            // 
            // minToolStripMenuItem
            // 
            this.minToolStripMenuItem.Name = "minToolStripMenuItem";
            this.minToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.minToolStripMenuItem.Tag = "_1MIN";
            this.minToolStripMenuItem.Text = "1min";
            this.minToolStripMenuItem.Click += new System.EventHandler(this.tsDateGroup_Click);
            // 
            // minToolStripMenuItem1
            // 
            this.minToolStripMenuItem1.Name = "minToolStripMenuItem1";
            this.minToolStripMenuItem1.Size = new System.Drawing.Size(224, 26);
            this.minToolStripMenuItem1.Tag = "_10MIN";
            this.minToolStripMenuItem1.Text = "10min";
            this.minToolStripMenuItem1.Click += new System.EventHandler(this.tsDateGroup_Click);
            // 
            // hrToolStripMenuItem
            // 
            this.hrToolStripMenuItem.Name = "hrToolStripMenuItem";
            this.hrToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.hrToolStripMenuItem.Tag = "_60MIN";
            this.hrToolStripMenuItem.Text = "1Hr";
            this.hrToolStripMenuItem.Click += new System.EventHandler(this.tsDateGroup_Click);
            // 
            // hrsToolStripMenuItem
            // 
            this.hrsToolStripMenuItem.Name = "hrsToolStripMenuItem";
            this.hrsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.hrsToolStripMenuItem.Tag = "_120MIN";
            this.hrsToolStripMenuItem.Text = "2Hrs";
            this.hrsToolStripMenuItem.Click += new System.EventHandler(this.tsDateGroup_Click);
            // 
            // dayToolStripMenuItem1
            // 
            this.dayToolStripMenuItem1.Name = "dayToolStripMenuItem1";
            this.dayToolStripMenuItem1.Size = new System.Drawing.Size(224, 26);
            this.dayToolStripMenuItem1.Tag = "DAY";
            this.dayToolStripMenuItem1.Text = "Day";
            this.dayToolStripMenuItem1.Click += new System.EventHandler(this.tsDateGroup_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // objectExecution1
            // 
            this.objectExecution1.Dock = System.Windows.Forms.DockStyle.Top;
            this.objectExecution1.Location = new System.Drawing.Point(0, 1279);
            this.objectExecution1.Name = "objectExecution1";
            this.objectExecution1.Size = new System.Drawing.Size(883, 282);
            this.objectExecution1.TabIndex = 7;
            // 
            // blocking1
            // 
            this.blocking1.Dock = System.Windows.Forms.DockStyle.Top;
            this.blocking1.Location = new System.Drawing.Point(0, 871);
            this.blocking1.Name = "blocking1";
            this.blocking1.Size = new System.Drawing.Size(883, 408);
            this.blocking1.TabIndex = 6;
            // 
            // waits1
            // 
            this.waits1.Dock = System.Windows.Forms.DockStyle.Top;
            this.waits1.Location = new System.Drawing.Point(0, 589);
            this.waits1.Name = "waits1";
            this.waits1.Size = new System.Drawing.Size(883, 282);
            this.waits1.TabIndex = 5;
            // 
            // ioPerformance1
            // 
            this.ioPerformance1.Dock = System.Windows.Forms.DockStyle.Top;
            this.ioPerformance1.Location = new System.Drawing.Point(0, 295);
            this.ioPerformance1.Name = "ioPerformance1";
            this.ioPerformance1.Size = new System.Drawing.Size(883, 294);
            this.ioPerformance1.SmoothLines = true;
            this.ioPerformance1.TabIndex = 3;
            // 
            // cpu1
            // 
            this.cpu1.Dock = System.Windows.Forms.DockStyle.Top;
            this.cpu1.Location = new System.Drawing.Point(0, 31);
            this.cpu1.Name = "cpu1";
            this.cpu1.Size = new System.Drawing.Size(883, 264);
            this.cpu1.SmoothLines = true;
            this.cpu1.TabIndex = 4;
            // 
            // Performance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.objectExecution1);
            this.Controls.Add(this.blocking1);
            this.Controls.Add(this.waits1);
            this.Controls.Add(this.ioPerformance1);
            this.Controls.Add(this.cpu1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Performance";
            this.Size = new System.Drawing.Size(883, 1620);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripButton tsEnableTimer;
        private System.Windows.Forms.ToolStripButton tsDisableTimer;
        private DBAChecksGUI.Performance.IOPerformance ioPerformance1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private CPU cpu1;
        private Waits waits1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days14toolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days28ToolStripMenuItem;
        private Blocking blocking1;
        private ObjectExecution objectExecution1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsGrouping;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem hrToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hrsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem1;
    }
}
