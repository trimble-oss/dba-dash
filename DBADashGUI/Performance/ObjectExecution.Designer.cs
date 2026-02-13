namespace DBADashGUI.Performance
{
    partial class ObjectExecution
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
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectExecution));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            objectExecChart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblExecution = new System.Windows.Forms.ToolStripLabel();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTop = new System.Windows.Forms.ToolStripDropDownButton();
            tsTop5 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop10 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop20 = new System.Windows.Forms.ToolStripMenuItem();
            tsTop40 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            includeOtherToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // objectExecChart
            // 
            objectExecChart.AutoUpdateEnabled = true;
            objectExecChart.ChartTheme = null;
            objectExecChart.Dock = System.Windows.Forms.DockStyle.Fill;
            objectExecChart.ForceGPU = false;
            skDefaultLegend1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend1.Content = null;
            skDefaultLegend1.IsValid = false;
            skDefaultLegend1.Opacity = 1F;
            padding1.Bottom = 0F;
            padding1.Left = 0F;
            padding1.Right = 0F;
            padding1.Top = 0F;
            skDefaultLegend1.Padding = padding1;
            skDefaultLegend1.RemoveOnCompleted = false;
            skDefaultLegend1.RotateTransform = 0F;
            skDefaultLegend1.X = 0F;
            skDefaultLegend1.Y = 0F;
            objectExecChart.Legend = skDefaultLegend1;
            objectExecChart.Location = new System.Drawing.Point(0, 27);
            objectExecChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            objectExecChart.MatchAxesScreenDataRatio = false;
            objectExecChart.Name = "objectExecChart";
            objectExecChart.Size = new System.Drawing.Size(492, 325);
            objectExecChart.TabIndex = 0;
            skDefaultTooltip1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip1.Content = null;
            skDefaultTooltip1.IsValid = false;
            skDefaultTooltip1.Opacity = 1F;
            padding2.Bottom = 0F;
            padding2.Left = 0F;
            padding2.Right = 0F;
            padding2.Top = 0F;
            skDefaultTooltip1.Padding = padding2;
            skDefaultTooltip1.RemoveOnCompleted = false;
            skDefaultTooltip1.RotateTransform = 0F;
            skDefaultTooltip1.Wedge = 10;
            skDefaultTooltip1.X = 0F;
            skDefaultTooltip1.Y = 0F;
            objectExecChart.Tooltip = skDefaultTooltip1;
            objectExecChart.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblExecution, tsDateGroup, tsMeasures, tsLegend, tsTop });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(492, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsClose
            // 
            tsClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClose.Image = Properties.Resources.Close_red_16x;
            tsClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClose.Name = "tsClose";
            tsClose.Size = new System.Drawing.Size(29, 24);
            tsClose.Text = "Close";
            tsClose.Visible = false;
            tsClose.Click += TsClose_Click;
            // 
            // tsUp
            // 
            tsUp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsUp.Image = Properties.Resources.arrow_Up_16xLG;
            tsUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsUp.Name = "tsUp";
            tsUp.Size = new System.Drawing.Size(29, 24);
            tsUp.Text = "Move Up";
            tsUp.Click += TsUp_Click;
            // 
            // lblExecution
            // 
            lblExecution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblExecution.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblExecution.Name = "lblExecution";
            lblExecution.Size = new System.Drawing.Size(116, 24);
            lblExecution.Text = "Execution Stats";
            // 
            // tsDateGroup
            // 
            tsDateGroup.Image = Properties.Resources.Time_16x;
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(76, 24);
            tsDateGroup.Text = "1min";
            // 
            // tsMeasures
            // 
            tsMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasures.Name = "tsMeasures";
            tsMeasures.Size = new System.Drawing.Size(105, 24);
            tsMeasures.Text = "Measures";
            // 
            // tsLegend
            // 
            tsLegend.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsLegend.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { leftToolStripMenuItem, rightToolStripMenuItem, topToolStripMenuItem, bottomToolStripMenuItem, hiddenToolStripMenuItem });
            tsLegend.Image = Properties.Resources.LegendHS;
            tsLegend.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsLegend.Name = "tsLegend";
            tsLegend.Size = new System.Drawing.Size(34, 24);
            tsLegend.Text = "Legend Position";
            // 
            // leftToolStripMenuItem
            // 
            leftToolStripMenuItem.Name = "leftToolStripMenuItem";
            leftToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            leftToolStripMenuItem.Tag = "Left";
            leftToolStripMenuItem.Text = "Left";
            leftToolStripMenuItem.Click += SetLegendPosition;
            // 
            // rightToolStripMenuItem
            // 
            rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            rightToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            rightToolStripMenuItem.Tag = "Right";
            rightToolStripMenuItem.Text = "Right";
            rightToolStripMenuItem.Click += SetLegendPosition;
            // 
            // topToolStripMenuItem
            // 
            topToolStripMenuItem.Name = "topToolStripMenuItem";
            topToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            topToolStripMenuItem.Tag = "Top";
            topToolStripMenuItem.Text = "Top";
            topToolStripMenuItem.Click += SetLegendPosition;
            // 
            // bottomToolStripMenuItem
            // 
            bottomToolStripMenuItem.Name = "bottomToolStripMenuItem";
            bottomToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            bottomToolStripMenuItem.Tag = "Bottom";
            bottomToolStripMenuItem.Text = "Bottom";
            bottomToolStripMenuItem.Click += SetLegendPosition;
            // 
            // hiddenToolStripMenuItem
            // 
            hiddenToolStripMenuItem.Checked = true;
            hiddenToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            hiddenToolStripMenuItem.Name = "hiddenToolStripMenuItem";
            hiddenToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            hiddenToolStripMenuItem.Tag = "Hidden";
            hiddenToolStripMenuItem.Text = "Hidden";
            hiddenToolStripMenuItem.Click += SetLegendPosition;
            // 
            // tsTop
            // 
            tsTop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsTop.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsTop5, tsTop10, tsTop20, tsTop40, toolStripMenuItem1, includeOtherToolStripMenuItem });
            tsTop.Image = (System.Drawing.Image)resources.GetObject("tsTop.Image");
            tsTop.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTop.Name = "tsTop";
            tsTop.Size = new System.Drawing.Size(60, 24);
            tsTop.Tag = "5";
            tsTop.Text = "Top 5";
            // 
            // tsTop5
            // 
            tsTop5.Checked = true;
            tsTop5.CheckState = System.Windows.Forms.CheckState.Checked;
            tsTop5.Name = "tsTop5";
            tsTop5.Size = new System.Drawing.Size(246, 26);
            tsTop5.Tag = "5";
            tsTop5.Text = "Top 5 (Overall/Period)";
            tsTop5.Click += Top_Select;
            // 
            // tsTop10
            // 
            tsTop10.Name = "tsTop10";
            tsTop10.Size = new System.Drawing.Size(246, 26);
            tsTop10.Tag = "10";
            tsTop10.Text = "Top 10 (Overall/Period)";
            tsTop10.Click += Top_Select;
            // 
            // tsTop20
            // 
            tsTop20.Name = "tsTop20";
            tsTop20.Size = new System.Drawing.Size(246, 26);
            tsTop20.Tag = "20";
            tsTop20.Text = "Top 20 (Overall/Period)";
            tsTop20.Click += Top_Select;
            // 
            // tsTop40
            // 
            tsTop40.Name = "tsTop40";
            tsTop40.Size = new System.Drawing.Size(246, 26);
            tsTop40.Tag = "40";
            tsTop40.Text = "Top 40 (Overall/Period)";
            tsTop40.Click += Top_Select;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(243, 6);
            // 
            // includeOtherToolStripMenuItem
            // 
            includeOtherToolStripMenuItem.Checked = true;
            includeOtherToolStripMenuItem.CheckOnClick = true;
            includeOtherToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            includeOtherToolStripMenuItem.Name = "includeOtherToolStripMenuItem";
            includeOtherToolStripMenuItem.Size = new System.Drawing.Size(246, 26);
            includeOtherToolStripMenuItem.Text = "Include Other";
            includeOtherToolStripMenuItem.Click += includeOtherToolStripMenuItem_Click;
            // 
            // ObjectExecution
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(objectExecChart);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ObjectExecution";
            Size = new System.Drawing.Size(492, 352);
            Load += ObjectExecution_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart objectExecChart;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblExecution;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private System.Windows.Forms.ToolStripDropDownButton tsTop;
        private System.Windows.Forms.ToolStripMenuItem tsTop10;
        private System.Windows.Forms.ToolStripMenuItem tsTop20;
        private System.Windows.Forms.ToolStripMenuItem tsTop5;
        private System.Windows.Forms.ToolStripMenuItem tsTop40;
        private System.Windows.Forms.ToolStripMenuItem includeOtherToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripDropDownButton tsLegend;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hiddenToolStripMenuItem;
    }
}
