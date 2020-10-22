namespace DBAChecksGUI.Performance
{
    partial class AzureDBResourceStats
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
            this.chart1 = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            this.smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chart1
            // 
            this.chart1.BackColor = System.Drawing.Color.White;
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 27);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(882, 498);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsTime,
            this.tsOptions,
            this.tsMeasures});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(882, 27);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsTime
            // 
            this.tsTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem1,
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.days7ToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBAChecksGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 24);
            this.tsTime.Text = "Time";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem2.Tag = "5";
            this.toolStripMenuItem2.Text = "5 Mins";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem1.Tag = "15";
            this.toolStripMenuItem1.Text = "15 Mins";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(143, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Checked = true;
            this.ts1Hr.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(143, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(143, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(143, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(143, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(143, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(140, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(143, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.tsCustom_Click);
            // 
            // tsOptions
            // 
            this.tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smoothLinesToolStripMenuItem,
            this.pointsToolStripMenuItem});
            this.tsOptions.Image = global::DBAChecksGUI.Properties.Resources.LineChart_16x;
            this.tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsOptions.Name = "tsOptions";
            this.tsOptions.Size = new System.Drawing.Size(34, 24);
            this.tsOptions.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            this.smoothLinesToolStripMenuItem.CheckOnClick = true;
            this.smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            this.smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.smoothLinesToolStripMenuItem.Text = "Smooth lines";
            this.smoothLinesToolStripMenuItem.Click += new System.EventHandler(this.smoothLinesToolStripMenuItem_Click);
            // 
            // pointsToolStripMenuItem
            // 
            this.pointsToolStripMenuItem.CheckOnClick = true;
            this.pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            this.pointsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.pointsToolStripMenuItem.Text = "Points";
            this.pointsToolStripMenuItem.Click += new System.EventHandler(this.pointsToolStripMenuItem_Click);
            // 
            // tsMeasures
            // 
            this.tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMeasures.Image = global::DBAChecksGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(34, 24);
            this.tsMeasures.Text = "Columns";
            // 
            // AzureDBResourceStats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "AzureDBResourceStats";
            this.Size = new System.Drawing.Size(882, 525);
            this.Load += new System.EventHandler(this.AzureDBResourceStats_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chart1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pointsToolStripMenuItem;
    }
}
