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
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days14toolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days28ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataPointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cpu1 = new DBAChecksGUI.Performance.CPU();
            this.objectExecution1 = new DBAChecksGUI.Performance.ObjectExecution();
            this.ioPerformance1 = new DBAChecksGUI.Performance.IOPerformance();
            this.blocking1 = new DBAChecksGUI.Performance.Blocking();
            this.waits1 = new DBAChecksGUI.Performance.Waits();
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTime,
            this.toolStripDropDownButton1,
            this.tsRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1757, 31);
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
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smoothLinesToolStripMenuItem,
            this.dataPointsToolStripMenuItem});
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
            // dataPointsToolStripMenuItem
            // 
            this.dataPointsToolStripMenuItem.Checked = true;
            this.dataPointsToolStripMenuItem.CheckOnClick = true;
            this.dataPointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dataPointsToolStripMenuItem.Name = "dataPointsToolStripMenuItem";
            this.dataPointsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.dataPointsToolStripMenuItem.Text = "Data Points";
            this.dataPointsToolStripMenuItem.Click += new System.EventHandler(this.dataPointsToolStripMenuItem_Click);
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
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.cpu1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.objectExecution1, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.ioPerformance1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.blocking1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.waits1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 31);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1757, 1861);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // cpu1
            // 
            this.cpu1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cpu1.Location = new System.Drawing.Point(3, 3);
            this.cpu1.Name = "cpu1";
            this.cpu1.Size = new System.Drawing.Size(1751, 366);
            this.cpu1.SmoothLines = true;
            this.cpu1.TabIndex = 4;
            // 
            // objectExecution1
            // 
            this.objectExecution1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecution1.Location = new System.Drawing.Point(3, 1491);
            this.objectExecution1.Name = "objectExecution1";
            this.objectExecution1.Size = new System.Drawing.Size(1751, 367);
            this.objectExecution1.TabIndex = 7;
            // 
            // ioPerformance1
            // 
            this.ioPerformance1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioPerformance1.Drive = "";
            this.ioPerformance1.FileGroup = "";
            this.ioPerformance1.Location = new System.Drawing.Point(3, 375);
            this.ioPerformance1.Name = "ioPerformance1";
            this.ioPerformance1.Size = new System.Drawing.Size(1751, 366);
            this.ioPerformance1.SmoothLines = true;
            this.ioPerformance1.TabIndex = 3;
            // 
            // blocking1
            // 
            this.blocking1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.blocking1.Location = new System.Drawing.Point(3, 1119);
            this.blocking1.Name = "blocking1";
            this.blocking1.Size = new System.Drawing.Size(1751, 366);
            this.blocking1.TabIndex = 6;
            // 
            // waits1
            // 
            this.waits1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waits1.Location = new System.Drawing.Point(3, 747);
            this.waits1.Name = "waits1";
            this.waits1.Size = new System.Drawing.Size(1751, 366);
            this.waits1.TabIndex = 5;
            this.waits1.WaitType = null;
            // 
            // Performance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Performance";
            this.Size = new System.Drawing.Size(1757, 1892);
            this.Load += new System.EventHandler(this.Performance_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
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
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem dataPointsToolStripMenuItem;
    }
}
