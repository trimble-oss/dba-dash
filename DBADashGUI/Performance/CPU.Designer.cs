namespace DBADashGUI.Performance
{
    partial class CPU
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
            components = new System.ComponentModel.Container();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CPU));
            LiveChartsCore.Drawing.Padding padding3 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip2 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding4 = new LiveChartsCore.Drawing.Padding();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            tsCopyData = new System.Windows.Forms.ToolStripMenuItem();
            exportDataToExcelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblCPU = new System.Windows.Forms.ToolStripLabel();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            AVGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            MAXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            chartCPU = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            tsLegend = new System.Windows.Forms.ToolStripDropDownButton();
            leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextMenuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyData, exportDataToExcelToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(218, 56);
            // 
            // tsCopyData
            // 
            tsCopyData.Image = Properties.Resources.ASX_Copy_grey_16x;
            tsCopyData.Name = "tsCopyData";
            tsCopyData.Size = new System.Drawing.Size(217, 26);
            tsCopyData.Text = "Copy Data";
            tsCopyData.Click += CopyData_Click;
            // 
            // exportDataToExcelToolStripMenuItem
            // 
            exportDataToExcelToolStripMenuItem.Image = Properties.Resources.excel16x16;
            exportDataToExcelToolStripMenuItem.Name = "exportDataToExcelToolStripMenuItem";
            exportDataToExcelToolStripMenuItem.Size = new System.Drawing.Size(217, 26);
            exportDataToExcelToolStripMenuItem.Text = "Export Data to Excel";
            exportDataToExcelToolStripMenuItem.Click += ExportDataToExcel_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblCPU, tsDateGrouping, tsAgg, tsLegend });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(878, 27);
            toolStrip1.TabIndex = 2;
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
            // lblCPU
            // 
            lblCPU.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblCPU.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblCPU.Name = "lblCPU";
            lblCPU.Size = new System.Drawing.Size(105, 24);
            lblCPU.Text = "CPU: Instance";
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.Image = Properties.Resources.Time_16x;
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(76, 24);
            tsDateGrouping.Text = "1min";
            // 
            // tsAgg
            // 
            tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { AVGToolStripMenuItem, MAXToolStripMenuItem });
            tsAgg.Image = Properties.Resources.AddComputedField_16x;
            tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsAgg.Name = "tsAgg";
            tsAgg.Size = new System.Drawing.Size(69, 24);
            tsAgg.Text = "Avg";
            // 
            // AVGToolStripMenuItem
            // 
            AVGToolStripMenuItem.Checked = true;
            AVGToolStripMenuItem.CheckOnClick = true;
            AVGToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            AVGToolStripMenuItem.Name = "AVGToolStripMenuItem";
            AVGToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            AVGToolStripMenuItem.Tag = "Avg";
            AVGToolStripMenuItem.Text = "Avg";
            AVGToolStripMenuItem.Click += AVGToolStripMenuItem_Click;
            // 
            // MAXToolStripMenuItem
            // 
            MAXToolStripMenuItem.CheckOnClick = true;
            MAXToolStripMenuItem.Name = "MAXToolStripMenuItem";
            MAXToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            MAXToolStripMenuItem.Tag = "Max";
            MAXToolStripMenuItem.Text = "Max";
            MAXToolStripMenuItem.Click += MAXToolStripMenuItem_Click;
            // 
            // chartCPU
            // 
            chartCPU.AutoUpdateEnabled = true;
            chartCPU.ChartTheme = null;
            chartCPU.Dock = System.Windows.Forms.DockStyle.Fill;
            chartCPU.ForceGPU = false;
            skDefaultLegend2.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend2.Content = null;
            skDefaultLegend2.IsValid = false;
            skDefaultLegend2.Opacity = 1F;
            padding3.Bottom = 0F;
            padding3.Left = 0F;
            padding3.Right = 0F;
            padding3.Top = 0F;
            skDefaultLegend2.Padding = padding3;
            skDefaultLegend2.RemoveOnCompleted = false;
            skDefaultLegend2.RotateTransform = 0F;
            skDefaultLegend2.X = 0F;
            skDefaultLegend2.Y = 0F;
            chartCPU.Legend = skDefaultLegend2;
            chartCPU.Location = new System.Drawing.Point(0, 27);
            chartCPU.MatchAxesScreenDataRatio = false;
            chartCPU.Name = "chartCPU";
            chartCPU.Size = new System.Drawing.Size(878, 303);
            chartCPU.TabIndex = 3;
            skDefaultTooltip2.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip2.Content = null;
            skDefaultTooltip2.IsValid = false;
            skDefaultTooltip2.Opacity = 1F;
            padding4.Bottom = 0F;
            padding4.Left = 0F;
            padding4.Right = 0F;
            padding4.Top = 0F;
            skDefaultTooltip2.Padding = padding4;
            skDefaultTooltip2.RemoveOnCompleted = false;
            skDefaultTooltip2.RotateTransform = 0F;
            skDefaultTooltip2.Wedge = 10;
            skDefaultTooltip2.X = 0F;
            skDefaultTooltip2.Y = 0F;
            chartCPU.Tooltip = skDefaultTooltip2;
            chartCPU.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
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
            // CPU
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(chartCPU);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CPU";
            Size = new System.Drawing.Size(878, 330);
            Load += CPU_Load;
            contextMenuStrip1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblCPU;
        private System.Windows.Forms.ToolStripDropDownButton tsAgg;
        private System.Windows.Forms.ToolStripMenuItem AVGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MAXToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsCopyData;
        private System.Windows.Forms.ToolStripMenuItem exportDataToExcelToolStripMenuItem;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chartCPU;
        private System.Windows.Forms.ToolStripDropDownButton tsLegend;
        private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hiddenToolStripMenuItem;
    }
}
