namespace DBADashGUI.Performance
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureDBResourceStats));
            chartDB = new LiveCharts.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            smoothLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            chartPool = new LiveCharts.WinForms.CartesianChart();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsPool = new System.Windows.Forms.ToolStripLabel();
            tsPoolMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            toolStrip3 = new System.Windows.Forms.ToolStrip();
            tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            tsDB = new System.Windows.Forms.ToolStripLabel();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip2.SuspendLayout();
            toolStrip3.SuspendLayout();
            SuspendLayout();
            // 
            // chartDB
            // 
            chartDB.BackColor = System.Drawing.Color.White;
            chartDB.Dock = System.Windows.Forms.DockStyle.Fill;
            chartDB.Location = new System.Drawing.Point(0, 27);
            chartDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chartDB.Name = "chartDB";
            chartDB.Size = new System.Drawing.Size(882, 284);
            chartDB.TabIndex = 0;
            chartDB.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsOptions, tsDateGrouping });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(882, 27);
            toolStrip1.TabIndex = 4;
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
            // tsOptions
            // 
            tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { smoothLinesToolStripMenuItem, pointsToolStripMenuItem });
            tsOptions.Image = Properties.Resources.LineChart_16x;
            tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsOptions.Name = "tsOptions";
            tsOptions.Size = new System.Drawing.Size(34, 24);
            tsOptions.Text = "Chart Options";
            // 
            // smoothLinesToolStripMenuItem
            // 
            smoothLinesToolStripMenuItem.CheckOnClick = true;
            smoothLinesToolStripMenuItem.Name = "smoothLinesToolStripMenuItem";
            smoothLinesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            smoothLinesToolStripMenuItem.Text = "Smooth lines";
            smoothLinesToolStripMenuItem.Click += SmoothLinesToolStripMenuItem_Click;
            // 
            // pointsToolStripMenuItem
            // 
            pointsToolStripMenuItem.CheckOnClick = true;
            pointsToolStripMenuItem.Name = "pointsToolStripMenuItem";
            pointsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            pointsToolStripMenuItem.Text = "Points";
            pointsToolStripMenuItem.Click += PointsToolStripMenuItem_Click;
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsDateGrouping.Image = (System.Drawing.Image)resources.GetObject("tsDateGrouping.Image");
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(59, 24);
            tsDateGrouping.Text = "None";
            tsDateGrouping.ToolTipText = "Date Grouping";
            // 
            // chartPool
            // 
            chartPool.BackColor = System.Drawing.Color.White;
            chartPool.Dock = System.Windows.Forms.DockStyle.Fill;
            chartPool.Location = new System.Drawing.Point(0, 27);
            chartPool.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chartPool.Name = "chartPool";
            chartPool.Size = new System.Drawing.Size(882, 287);
            chartPool.TabIndex = 5;
            chartPool.Text = "cartesianChart1";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(chartDB);
            splitContainer1.Panel1.Controls.Add(toolStrip3);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(chartPool);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(882, 629);
            splitContainer1.SplitterDistance = 311;
            splitContainer1.TabIndex = 6;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsPool, tsPoolMeasures });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(882, 27);
            toolStrip2.TabIndex = 6;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsPool
            // 
            tsPool.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsPool.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsPool.Name = "tsPool";
            tsPool.Size = new System.Drawing.Size(88, 24);
            tsPool.Text = "Elastic Pool";
            // 
            // tsPoolMeasures
            // 
            tsPoolMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPoolMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsPoolMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPoolMeasures.Name = "tsPoolMeasures";
            tsPoolMeasures.Size = new System.Drawing.Size(34, 24);
            tsPoolMeasures.Text = "Columns";
            // 
            // toolStrip3
            // 
            toolStrip3.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsMeasures, tsDB });
            toolStrip3.Location = new System.Drawing.Point(0, 0);
            toolStrip3.Name = "toolStrip3";
            toolStrip3.Size = new System.Drawing.Size(882, 27);
            toolStrip3.TabIndex = 1;
            toolStrip3.Text = "toolStrip3";
            // 
            // tsMeasures
            // 
            tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsMeasures.Image = Properties.Resources.AddComputedField_16x;
            tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsMeasures.Name = "tsMeasures";
            tsMeasures.Size = new System.Drawing.Size(34, 24);
            tsMeasures.Text = "Columns";
            // 
            // tsDB
            // 
            tsDB.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsDB.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsDB.Name = "tsDB";
            tsDB.Size = new System.Drawing.Size(38, 24);
            tsDB.Text = "DB: ";
            // 
            // AzureDBResourceStats
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AzureDBResourceStats";
            Size = new System.Drawing.Size(882, 656);
            Load += AzureDBResourceStats_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            toolStrip3.ResumeLayout(false);
            toolStrip3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartDB;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem smoothLinesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pointsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private LiveCharts.WinForms.CartesianChart chartPool;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel tsPool;
        private System.Windows.Forms.ToolStripDropDownButton tsPoolMeasures;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripLabel tsDB;
    }
}
