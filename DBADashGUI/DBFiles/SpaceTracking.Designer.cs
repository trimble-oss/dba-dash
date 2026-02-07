using DBADashGUI.CustomReports;

namespace DBADashGUI
{
    partial class SpaceTracking
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend skDefaultLegend1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultLegend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpaceTracking));
            LiveChartsCore.Drawing.Padding padding1 = new LiveChartsCore.Drawing.Padding();
            LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip skDefaultTooltip1 = new LiveChartsCore.SkiaSharpView.SKCharts.SKDefaultTooltip();
            LiveChartsCore.Drawing.Padding padding2 = new LiveChartsCore.Drawing.Padding();
            elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            dgv = new DBADashDataGridView();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsHistory = new System.Windows.Forms.ToolStripButton();
            tsContext = new System.Windows.Forms.ToolStripLabel();
            tsUnits = new System.Windows.Forms.ToolStripDropDownButton();
            mBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            gBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDecimalPlaces = new System.Windows.Forms.ToolStripDropDownButton();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            pieChart1 = new LiveChartsCore.SkiaSharpView.WinForms.PieChart();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // elementHost1
            // 
            elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            elementHost1.Location = new System.Drawing.Point(0, 0);
            elementHost1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            elementHost1.Name = "elementHost1";
            elementHost1.Size = new System.Drawing.Size(482, 939);
            elementHost1.TabIndex = 0;
            elementHost1.Text = "elementHost1";
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle2;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(516, 939);
            dgv.TabIndex = 1;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellToolTipTextNeeded += Dgv_CellToolTipTextNeeded;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgv);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(pieChart1);
            splitContainer1.Panel2.Controls.Add(elementHost1);
            splitContainer1.Size = new System.Drawing.Size(1002, 939);
            splitContainer1.SplitterDistance = 516;
            splitContainer1.TabIndex = 2;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsBack, tsClearFilter, tsHistory, tsContext, tsUnits, tsDecimalPlaces });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1002, 27);
            toolStrip1.TabIndex = 2;
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
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Enabled = false;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // tsHistory
            // 
            tsHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHistory.Image = Properties.Resources.LineChart_16x;
            tsHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHistory.Name = "tsHistory";
            tsHistory.Size = new System.Drawing.Size(29, 24);
            tsHistory.Text = "History";
            tsHistory.Click += TsHistory_Click;
            // 
            // tsContext
            // 
            tsContext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsContext.Name = "tsContext";
            tsContext.Size = new System.Drawing.Size(18, 24);
            tsContext.Text = "...";
            // 
            // tsUnits
            // 
            tsUnits.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsUnits.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { mBToolStripMenuItem, gBToolStripMenuItem, tBToolStripMenuItem });
            tsUnits.Image = (System.Drawing.Image)resources.GetObject("tsUnits.Image");
            tsUnits.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsUnits.Name = "tsUnits";
            tsUnits.Size = new System.Drawing.Size(56, 24);
            tsUnits.Text = "Units";
            // 
            // mBToolStripMenuItem
            // 
            mBToolStripMenuItem.Name = "mBToolStripMenuItem";
            mBToolStripMenuItem.Size = new System.Drawing.Size(114, 26);
            mBToolStripMenuItem.Tag = "MB";
            mBToolStripMenuItem.Text = "MB";
            mBToolStripMenuItem.Click += SetUnit;
            // 
            // gBToolStripMenuItem
            // 
            gBToolStripMenuItem.Checked = true;
            gBToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            gBToolStripMenuItem.Name = "gBToolStripMenuItem";
            gBToolStripMenuItem.Size = new System.Drawing.Size(114, 26);
            gBToolStripMenuItem.Tag = "GB";
            gBToolStripMenuItem.Text = "GB";
            gBToolStripMenuItem.Click += SetUnit;
            // 
            // tBToolStripMenuItem
            // 
            tBToolStripMenuItem.Name = "tBToolStripMenuItem";
            tBToolStripMenuItem.Size = new System.Drawing.Size(114, 26);
            tBToolStripMenuItem.Tag = "TB";
            tBToolStripMenuItem.Text = "TB";
            tBToolStripMenuItem.Click += SetUnit;
            // 
            // tsDecimalPlaces
            // 
            tsDecimalPlaces.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsDecimalPlaces.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripMenuItem2, toolStripMenuItem3, toolStripMenuItem4, toolStripMenuItem5 });
            tsDecimalPlaces.Image = (System.Drawing.Image)resources.GetObject("tsDecimalPlaces.Image");
            tsDecimalPlaces.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDecimalPlaces.Name = "tsDecimalPlaces";
            tsDecimalPlaces.Size = new System.Drawing.Size(123, 24);
            tsDecimalPlaces.Text = "Decimal Places";
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(100, 26);
            toolStripMenuItem2.Tag = "N0";
            toolStripMenuItem2.Text = "0";
            toolStripMenuItem2.Click += SetDecimal;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Checked = true;
            toolStripMenuItem3.CheckState = System.Windows.Forms.CheckState.Checked;
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(100, 26);
            toolStripMenuItem3.Tag = "N1";
            toolStripMenuItem3.Text = "1";
            toolStripMenuItem3.Click += SetDecimal;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(100, 26);
            toolStripMenuItem4.Tag = "N2";
            toolStripMenuItem4.Text = "2";
            toolStripMenuItem4.Click += SetDecimal;
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size(100, 26);
            toolStripMenuItem5.Tag = "N3";
            toolStripMenuItem5.Text = "3";
            toolStripMenuItem5.Click += SetDecimal;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "AllocatedGB";
            dataGridViewCellStyle3.Format = "N1";
            dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewTextBoxColumn1.HeaderText = "Allocated GB";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 119;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "UsedGB";
            dataGridViewCellStyle4.Format = "N1";
            dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewTextBoxColumn2.HeaderText = "Used GB";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 94;
            // 
            // pieChart1
            // 
            pieChart1.AutoUpdateEnabled = true;
            pieChart1.ChartTheme = null;
            pieChart1.Dock = System.Windows.Forms.DockStyle.Fill;
            pieChart1.ForceGPU = false;
            skDefaultLegend1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultLegend1.Content = null;
            skDefaultLegend1.IsValid = true;
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
            pieChart1.Legend = skDefaultLegend1;
            pieChart1.Location = new System.Drawing.Point(0, 0);
            pieChart1.Name = "pieChart1";
            pieChart1.Size = new System.Drawing.Size(482, 939);
            pieChart1.TabIndex = 1;
            skDefaultTooltip1.AnimationsSpeed = System.TimeSpan.Parse("00:00:00.1500000");
            skDefaultTooltip1.Content = null;
            skDefaultTooltip1.IsValid = true;
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
            pieChart1.Tooltip = skDefaultTooltip1;
            pieChart1.UpdaterThrottler = System.TimeSpan.Parse("00:00:00.0500000");
            // 
            // SpaceTracking
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "SpaceTracking";
            Size = new System.Drawing.Size(1002, 966);
            Load += SpaceTracking_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private DBADashDataGridView dgv;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripButton tsHistory;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ToolStripLabel tsContext;
        private System.Windows.Forms.ToolStripDropDownButton tsUnits;
        private System.Windows.Forms.ToolStripMenuItem mBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tBToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDecimalPlaces;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private LiveChartsCore.SkiaSharpView.WinForms.PieChart pieChart1;
    }
}
