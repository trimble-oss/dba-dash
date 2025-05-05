namespace DBADashGUI.DBFiles
{
    partial class DBSpaceHistory
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBSpaceHistory));
            chart1 = new LiveCharts.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsDateRange = new DateRangeToolStripMenuItem();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsFileGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsFile = new System.Windows.Forms.ToolStripDropDownButton();
            tsUnits = new System.Windows.Forms.ToolStripDropDownButton();
            mBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            gBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsGrid = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgv = new System.Windows.Forms.DataGridView();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            SuspendLayout();
            // 
            // chart1
            // 
            chart1.BackColor = System.Drawing.Color.White;
            chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            chart1.Location = new System.Drawing.Point(0, 0);
            chart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chart1.Name = "chart1";
            chart1.Size = new System.Drawing.Size(1134, 521);
            chart1.TabIndex = 0;
            chart1.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsDateRange, toolStripDropDownButton1, tsRefresh, tsFileGroup, tsFile, tsUnits, tsGrid, tsCopy, tsExcel });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1134, 27);
            toolStrip1.TabIndex = 3;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsDateRange
            // 
            tsDateRange.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDateRange.DefaultTimeSpan = System.TimeSpan.Parse("90.00:00:00");
            tsDateRange.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            tsDateRange.Font = new System.Drawing.Font("Segoe UI", 9F);
            tsDateRange.Image = (System.Drawing.Image)resources.GetObject("tsDateRange.Image");
            tsDateRange.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateRange.MaximumTimeSpan = System.TimeSpan.Parse("10675199.02:48:05.4775807");
            tsDateRange.MinimumTimeSpan = System.TimeSpan.Parse("1.00:00:00");
            tsDateRange.Name = "tsDateRange";
            tsDateRange.SelectedTimeSpan = System.TimeSpan.Parse("90.00:00:00");
            tsDateRange.Size = new System.Drawing.Size(95, 24);
            tsDateRange.Text = "90 Days";
            tsDateRange.DateRangeChanged += DateRangeChanged;
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { smoothLinesToolStripMenuItem, pointsToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.LineChart_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            smoothLinesToolStripMenuItem.Checked = true;
            smoothLinesToolStripMenuItem.CheckOnClick = true;
            smoothLinesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            smoothLinesToolStripMenuItem.Text = "Smooth Lines";
            smoothLinesToolStripMenuItem.Click += SmoothLinesToolStripMenuItem_Click;
            // 
            // pointsToolStripMenuItem
            // 
            pointsToolStripMenuItem.Checked = true;
            pointsToolStripMenuItem.CheckOnClick = true;
            pointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            pointsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            pointsToolStripMenuItem.Text = "Points";
            pointsToolStripMenuItem.Click += PointsToolStripMenuItem_Click;
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
            // tsFileGroup
            // 
            tsFileGroup.Image = Properties.Resources.FilterDropdown_16x;
            tsFileGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFileGroup.Name = "tsFileGroup";
            tsFileGroup.Size = new System.Drawing.Size(107, 24);
            tsFileGroup.Text = "FileGroup";
            tsFileGroup.Visible = false;
            // 
            // tsFile
            // 
            tsFile.Image = Properties.Resources.FilterDropdown_16x;
            tsFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFile.Name = "tsFile";
            tsFile.Size = new System.Drawing.Size(66, 24);
            tsFile.Text = "File";
            tsFile.Visible = false;
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
            mBToolStripMenuItem.Checked = true;
            mBToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            mBToolStripMenuItem.Name = "mBToolStripMenuItem";
            mBToolStripMenuItem.Size = new System.Drawing.Size(114, 26);
            mBToolStripMenuItem.Tag = "MB";
            mBToolStripMenuItem.Text = "MB";
            mBToolStripMenuItem.Click += SetUnit;
            // 
            // gBToolStripMenuItem
            // 
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
            // tsGrid
            // 
            tsGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsGrid.Image = Properties.Resources.Table_16x;
            tsGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGrid.Name = "tsGrid";
            tsGrid.Size = new System.Drawing.Size(29, 24);
            tsGrid.Text = "Toggle Grid";
            tsGrid.Click += TsGrid_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy Grid";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(chart1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Panel2Collapsed = true;
            splitContainer1.Size = new System.Drawing.Size(1134, 521);
            splitContainer1.SplitterDistance = 649;
            splitContainer1.TabIndex = 4;
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(96, 100);
            dgv.TabIndex = 0;
            // 
            // DBSpaceHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DBSpaceHistory";
            Size = new System.Drawing.Size(1134, 548);
            Load += DBSpaceHistory_Load;
            Resize += DBSpaceHistory_Resize;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chart1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripMenuItem pointsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsFileGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsFile;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsGrid;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsUnits;
        private System.Windows.Forms.ToolStripMenuItem mBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gBToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tBToolStripMenuItem;
        private DateRangeToolStripMenuItem tsDateRange;
    }
}
