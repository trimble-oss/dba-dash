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
            this.chart1 = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days30ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days90ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days180ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsFileGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsFile = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsGrid = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // chart1
            // 
            this.chart1.BackColor = System.Drawing.Color.White;
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(1134, 521);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTime,
            this.toolStripDropDownButton1,
            this.tsRefresh,
            this.tsFileGroup,
            this.tsFile,
            this.tsGrid,
            this.tsCopy,
            this.tsExcel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1134, 27);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsTime
            // 
            this.tsTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.days7ToolStripMenuItem,
            this.days30ToolStripMenuItem,
            this.days90ToolStripMenuItem,
            this.days180ToolStripMenuItem,
            this.yearToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 24);
            this.tsTime.Text = "Time";
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(152, 26);
            this.days7ToolStripMenuItem.Tag = "7";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.Days_Click);
            // 
            // days30ToolStripMenuItem
            // 
            this.days30ToolStripMenuItem.Name = "days30ToolStripMenuItem";
            this.days30ToolStripMenuItem.Size = new System.Drawing.Size(152, 26);
            this.days30ToolStripMenuItem.Tag = "30";
            this.days30ToolStripMenuItem.Text = "30 Days";
            this.days30ToolStripMenuItem.Click += new System.EventHandler(this.Days_Click);
            // 
            // days90ToolStripMenuItem
            // 
            this.days90ToolStripMenuItem.Checked = true;
            this.days90ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.days90ToolStripMenuItem.Name = "days90ToolStripMenuItem";
            this.days90ToolStripMenuItem.Size = new System.Drawing.Size(152, 26);
            this.days90ToolStripMenuItem.Tag = "90";
            this.days90ToolStripMenuItem.Text = "90 Days";
            this.days90ToolStripMenuItem.Click += new System.EventHandler(this.Days_Click);
            // 
            // days180ToolStripMenuItem
            // 
            this.days180ToolStripMenuItem.Name = "days180ToolStripMenuItem";
            this.days180ToolStripMenuItem.Size = new System.Drawing.Size(152, 26);
            this.days180ToolStripMenuItem.Tag = "180";
            this.days180ToolStripMenuItem.Text = "180 Days";
            this.days180ToolStripMenuItem.Click += new System.EventHandler(this.Days_Click);
            // 
            // yearToolStripMenuItem
            // 
            this.yearToolStripMenuItem.Name = "yearToolStripMenuItem";
            this.yearToolStripMenuItem.Size = new System.Drawing.Size(152, 26);
            this.yearToolStripMenuItem.Tag = "365";
            this.yearToolStripMenuItem.Text = "1 Year";
            this.yearToolStripMenuItem.Click += new System.EventHandler(this.Days_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(152, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.Custom_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smoothLinesToolStripMenuItem,
            this.pointsToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.LineChart_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
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
            // pointsToolStripMenuItem
            // 
            this.pointsToolStripMenuItem.Checked = true;
            this.pointsToolStripMenuItem.CheckOnClick = true;
            this.pointsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            this.pointsToolStripMenuItem.Size = new System.Drawing.Size(181, 26);
            this.pointsToolStripMenuItem.Text = "Points";
            this.pointsToolStripMenuItem.Click += new System.EventHandler(this.pointsToolStripMenuItem_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsFileGroup
            // 
            this.tsFileGroup.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFileGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFileGroup.Name = "tsFileGroup";
            this.tsFileGroup.Size = new System.Drawing.Size(107, 24);
            this.tsFileGroup.Text = "FileGroup";
            this.tsFileGroup.Visible = false;
            // 
            // tsFile
            // 
            this.tsFile.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFile.Name = "tsFile";
            this.tsFile.Size = new System.Drawing.Size(66, 24);
            this.tsFile.Text = "File";
            this.tsFile.Visible = false;
            // 
            // tsGrid
            // 
            this.tsGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsGrid.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsGrid.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGrid.Name = "tsGrid";
            this.tsGrid.Size = new System.Drawing.Size(29, 24);
            this.tsGrid.Text = "Toggle Grid";
            this.tsGrid.Click += new System.EventHandler(this.tsGrid_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export to Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy Grid";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.chart1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Size = new System.Drawing.Size(1134, 521);
            this.splitContainer1.SplitterDistance = 649;
            this.splitContainer1.TabIndex = 4;
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 29;
            this.dgv.Size = new System.Drawing.Size(96, 100);
            this.dgv.TabIndex = 0;
            // 
            // DBSpaceHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "DBSpaceHistory";
            this.Size = new System.Drawing.Size(1134, 548);
            this.Load += new System.EventHandler(this.DBSpaceHistory_Load);
            this.Resize += new System.EventHandler(this.DBSpaceHistory_Resize);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chart1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days30ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days90ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days180ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yearToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
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
    }
}
