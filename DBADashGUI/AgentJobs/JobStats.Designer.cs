
using DBADashGUI.CustomReports;

namespace DBADashGUI.AgentJobs
{
    partial class JobStats
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobStats));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsJob = new System.Windows.Forms.ToolStripLabel();
            tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            dateRangeToolStripMenuItem1 = new DateRangeToolStripMenuItem();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            chart1 = new DBADashGUI.Performance.CartesianChartWithDataTable();
            dgv = new DBADashDataGridView();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsDateGroup, tsJob, tsMeasures, tsClearFilter, tsBack, dateRangeToolStripMenuItem1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(904, 27);
            toolStrip1.TabIndex = 1;
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
            tsExcel.Text = "Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsDateGroup
            // 
            tsDateGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsDateGroup.Image = (System.Drawing.Image)resources.GetObject("tsDateGroup.Image");
            tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGroup.Name = "tsDateGroup";
            tsDateGroup.Size = new System.Drawing.Size(106, 24);
            tsDateGroup.Text = "{DateGroup}";
            // 
            // tsJob
            // 
            tsJob.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsJob.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsJob.Name = "tsJob";
            tsJob.Size = new System.Drawing.Size(46, 24);
            tsJob.Text = "{Job}";
            tsJob.Visible = false;
            // 
            // tsMeasures
            // 
            tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasures.Name = "tsMeasures";
            tsMeasures.Size = new System.Drawing.Size(34, 24);
            tsMeasures.Text = "Measures";
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
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // dateRangeToolStripMenuItem1
            // 
            dateRangeToolStripMenuItem1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            dateRangeToolStripMenuItem1.DefaultTimeSpan = System.TimeSpan.Parse("01:00:00");
            dateRangeToolStripMenuItem1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            dateRangeToolStripMenuItem1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dateRangeToolStripMenuItem1.Image = (System.Drawing.Image)resources.GetObject("dateRangeToolStripMenuItem1.Image");
            dateRangeToolStripMenuItem1.ImageTransparentColor = System.Drawing.Color.Magenta;
            dateRangeToolStripMenuItem1.MaximumTimeSpan = System.TimeSpan.Parse("10675199.02:48:05.4775807");
            dateRangeToolStripMenuItem1.MinimumTimeSpan = System.TimeSpan.Parse("-10675199.02:48:05.4775808");
            dateRangeToolStripMenuItem1.Name = "dateRangeToolStripMenuItem1";
            dateRangeToolStripMenuItem1.SelectedTimeSpan = System.TimeSpan.Parse("01:00:00");
            dateRangeToolStripMenuItem1.Size = new System.Drawing.Size(71, 24);
            dateRangeToolStripMenuItem1.Text = "1 Hr";
            dateRangeToolStripMenuItem1.Visible = false;
            dateRangeToolStripMenuItem1.DateRangeChanged += DateRangeChanged;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(chart1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Size = new System.Drawing.Size(904, 664);
            splitContainer1.SplitterDistance = 332;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // chart1
            // 
            chart1.DefaultLineSmoothness = 0.5D;
            chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            chart1.Location = new System.Drawing.Point(0, 0);
            chart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chart1.Name = "chart1";
            chart1.Size = new System.Drawing.Size(904, 332);
            chart1.TabIndex = 0;
            chart1.Text = "cartesianChartWithDataTable1";
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
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(904, 327);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            // 
            // JobStats
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "JobStats";
            Size = new System.Drawing.Size(904, 691);
            Load += JobStats_Load;
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

        private Performance.CartesianChartWithDataTable chart1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStripLabel tsJob;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private DateRangeToolStripMenuItem dateRangeToolStripMenuItem1;
    }
}
