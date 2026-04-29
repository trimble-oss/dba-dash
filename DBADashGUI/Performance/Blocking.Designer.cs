namespace DBADashGUI.Performance
{
    partial class Blocking
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Blocking));
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblBlocking = new System.Windows.Forms.ToolStripLabel();
            tsView = new System.Windows.Forms.ToolStripDropDownButton();
            blockingSnapshotsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deadlocksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDeadlocks = new System.Windows.Forms.ToolStripButton();
            chartBlocking = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblBlocking, tsView, tsDeadlocks });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(712, 27);
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
            tsUp.Visible = false;
            tsUp.Click += TsUp_Click;
            // 
            // lblBlocking
            // 
            lblBlocking.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblBlocking.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblBlocking.Name = "lblBlocking";
            lblBlocking.Size = new System.Drawing.Size(161, 24);
            lblBlocking.Text = "Blocking && Deadlocks";
            // 
            // tsView
            // 
            tsView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { blockingSnapshotsToolStripMenuItem, deadlocksToolStripMenuItem });
            tsView.Image = (System.Drawing.Image)resources.GetObject("tsView.Image");
            tsView.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsView.Name = "tsView";
            tsView.Size = new System.Drawing.Size(55, 24);
            tsView.Text = "View";
            // 
            // blockingSnapshotsToolStripMenuItem
            // 
            blockingSnapshotsToolStripMenuItem.Checked = true;
            blockingSnapshotsToolStripMenuItem.CheckOnClick = true;
            blockingSnapshotsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            blockingSnapshotsToolStripMenuItem.Name = "blockingSnapshotsToolStripMenuItem";
            blockingSnapshotsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            blockingSnapshotsToolStripMenuItem.Text = "Blocking Snapshots";
            blockingSnapshotsToolStripMenuItem.Click += BlockingSnapshots_Click;
            // 
            // deadlocksToolStripMenuItem
            // 
            deadlocksToolStripMenuItem.Checked = true;
            deadlocksToolStripMenuItem.CheckOnClick = true;
            deadlocksToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            deadlocksToolStripMenuItem.Name = "deadlocksToolStripMenuItem";
            deadlocksToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            deadlocksToolStripMenuItem.Text = "Deadlocks";
            deadlocksToolStripMenuItem.Click += DeadlocksSelection_Click;
            // 
            // tsDeadlocks
            // 
            tsDeadlocks.Image = Properties.Resources.VBReport_16x;
            tsDeadlocks.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDeadlocks.Name = "tsDeadlocks";
            tsDeadlocks.Size = new System.Drawing.Size(142, 24);
            tsDeadlocks.Text = "Show Deadlocks";
            tsDeadlocks.ToolTipText = resources.GetString("tsDeadlocks.ToolTipText");
            tsDeadlocks.Click += ShowDeadlocks_Click;
            // 
            // chartBlocking
            // 
            chartBlocking.AutoUpdateEnabled = true;
            chartBlocking.ChartTheme = null;
            chartBlocking.Dock = System.Windows.Forms.DockStyle.Fill;
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
            chartBlocking.Legend = skDefaultLegend1;
            chartBlocking.Location = new System.Drawing.Point(0, 27);
            chartBlocking.MatchAxesScreenDataRatio = false;
            chartBlocking.Name = "chartBlocking";
            chartBlocking.Size = new System.Drawing.Size(712, 483);
            chartBlocking.TabIndex = 2;
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
            chartBlocking.Tooltip = skDefaultTooltip1;
            chartBlocking.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // Blocking
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(chartBlocking);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Blocking";
            Size = new System.Drawing.Size(712, 510);
            Load += Blocking_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblBlocking;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chartBlocking;
        private System.Windows.Forms.ToolStripDropDownButton tsView;
        private System.Windows.Forms.ToolStripMenuItem blockingSnapshotsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deadlocksToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsDeadlocks;
    }
}
