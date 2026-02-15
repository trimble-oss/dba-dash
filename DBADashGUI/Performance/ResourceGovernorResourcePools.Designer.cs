namespace DBADashGUI.Performance
{
    partial class ResourceGovernorResourcePools
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
            if (disposing)
            {
                colorExtractionCts?.Cancel();
                colorExtractionCts?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
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
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsMetrics = new System.Windows.Forms.ToolStripDropDownButton();
            tsChartType = new System.Windows.Forms.ToolStripDropDownButton();
            stackedAreaToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stackedColumnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            tsHideTable = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsViewConfig = new System.Windows.Forms.ToolStripButton();
            pnlCharts = new System.Windows.Forms.Panel();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsMetrics, tsChartType, tsClose, tsUp, tsHideTable, toolStripLabel1, tsViewConfig });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1009, 27);
            toolStrip1.TabIndex = 0;
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
            tsRefresh.Click += Refresh_Click;
            // 
            // tsMetrics
            // 
            tsMetrics.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMetrics.Image = Properties.Resources.AddComputedField_16x;
            tsMetrics.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMetrics.Name = "tsMetrics";
            tsMetrics.Size = new System.Drawing.Size(34, 24);
            tsMetrics.Text = "Metrics";
            // 
            // tsChartType
            // 
            tsChartType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { stackedAreaToolstripMenuItem, stackedColumnToolStripMenuItem, lineToolStripMenuItem });
            tsChartType.Image = Properties.Resources.StackedAreaChart;
            tsChartType.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsChartType.Name = "tsChartType";
            tsChartType.Size = new System.Drawing.Size(34, 24);
            tsChartType.Text = "Chart Type";
            // 
            // stackedAreaToolstripMenuItem
            // 
            stackedAreaToolstripMenuItem.Checked = true;
            stackedAreaToolstripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            stackedAreaToolstripMenuItem.Name = "stackedAreaToolstripMenuItem";
            stackedAreaToolstripMenuItem.Size = new System.Drawing.Size(195, 26);
            stackedAreaToolstripMenuItem.Tag = "StackedArea";
            stackedAreaToolstripMenuItem.Text = "StackedArea";
            stackedAreaToolstripMenuItem.Click += SelectChartType_Click;
            // 
            // stackedColumnToolStripMenuItem
            // 
            stackedColumnToolStripMenuItem.Name = "stackedColumnToolStripMenuItem";
            stackedColumnToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            stackedColumnToolStripMenuItem.Tag = "StackedColumn";
            stackedColumnToolStripMenuItem.Text = "StackedColumn";
            stackedColumnToolStripMenuItem.Click += SelectChartType_Click;
            // 
            // lineToolStripMenuItem
            // 
            lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            lineToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            lineToolStripMenuItem.Tag = "Line";
            lineToolStripMenuItem.Text = "Line";
            lineToolStripMenuItem.Click += SelectChartType_Click;
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
            tsClose.Click += Close_Click;
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
            tsUp.Click += MoveUp_Click;
            // 
            // tsHideTable
            // 
            tsHideTable.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHideTable.Image = Properties.Resources.Table_16x;
            tsHideTable.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHideTable.Name = "tsHideTable";
            tsHideTable.Size = new System.Drawing.Size(29, 24);
            tsHideTable.Text = "Show/Hide Table";
            tsHideTable.Click += ShowHideTable_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(115, 24);
            toolStripLabel1.Text = "Resource Pools";
            // 
            // tsViewConfig
            // 
            tsViewConfig.Image = Properties.Resources.SettingsOutline_16x;
            tsViewConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsViewConfig.Name = "tsViewConfig";
            tsViewConfig.Size = new System.Drawing.Size(113, 24);
            tsViewConfig.Text = "View Conifg";
            tsViewConfig.Click += ViewConfig_Click;
            // 
            // pnlCharts
            // 
            pnlCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlCharts.Location = new System.Drawing.Point(0, 27);
            pnlCharts.Name = "pnlCharts";
            pnlCharts.Size = new System.Drawing.Size(1009, 488);
            pnlCharts.TabIndex = 1;
            // 
            // ResourceGovernorResourcePools
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlCharts);
            Controls.Add(toolStrip1);
            Name = "ResourceGovernorResourcePools";
            Size = new System.Drawing.Size(1009, 515);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.Panel pnlCharts;
        private System.Windows.Forms.ToolStripDropDownButton tsMetrics;
        private System.Windows.Forms.ToolStripDropDownButton tsChartType;
        private System.Windows.Forms.ToolStripMenuItem stackedAreaToolstripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stackedColumnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lineToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
        private System.Windows.Forms.ToolStripButton tsHideTable;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsViewConfig;
    }
}
