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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsEnableTimer = new System.Windows.Forms.ToolStripButton();
            this.tsDisableTimer = new System.Windows.Forms.ToolStripButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
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
            this.tsDisableTimer});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(883, 27);
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
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBAChecksGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 24);
            this.tsTime.Text = "Time";
            this.tsTime.Click += new System.EventHandler(this.tsTime_Click_1);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(224, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(224, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(224, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(224, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(224, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(224, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(221, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(224, 26);
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.customToolStripMenuItem_Click);
            // 
            // tsEnableTimer
            // 
            this.tsEnableTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsEnableTimer.Image = global::DBAChecksGUI.Properties.Resources.StartTime_16x;
            this.tsEnableTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsEnableTimer.Name = "tsEnableTimer";
            this.tsEnableTimer.Size = new System.Drawing.Size(29, 24);
            this.tsEnableTimer.Text = "Enable auto refresh timer";
            this.tsEnableTimer.Click += new System.EventHandler(this.tsEnableTimer_Click);
            // 
            // tsDisableTimer
            // 
            this.tsDisableTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDisableTimer.Image = global::DBAChecksGUI.Properties.Resources.StopTime_16x;
            this.tsDisableTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDisableTimer.Name = "tsDisableTimer";
            this.tsDisableTimer.Size = new System.Drawing.Size(29, 24);
            this.tsDisableTimer.Text = "Disable auto refresh timer";
            this.tsDisableTimer.Click += new System.EventHandler(this.tsDisableTimer_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ioPerformance1
            // 
            this.ioPerformance1.Dock = System.Windows.Forms.DockStyle.Top;
            this.ioPerformance1.Location = new System.Drawing.Point(0, 291);
            this.ioPerformance1.Name = "ioPerformance1";
            this.ioPerformance1.Size = new System.Drawing.Size(883, 294);
            this.ioPerformance1.TabIndex = 3;
            // 
            // cpu1
            // 
            this.cpu1.Dock = System.Windows.Forms.DockStyle.Top;
            this.cpu1.Location = new System.Drawing.Point(0, 27);
            this.cpu1.Name = "cpu1";
            this.cpu1.Size = new System.Drawing.Size(883, 264);
            this.cpu1.TabIndex = 4;
            // 
            // Performance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ioPerformance1);
            this.Controls.Add(this.cpu1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Performance";
            this.Size = new System.Drawing.Size(883, 623);
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
    }
}
