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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectExecution));
            objectExecChart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblExecution = new System.Windows.Forms.ToolStripLabel();
            tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
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
            objectExecChart.Dock = System.Windows.Forms.DockStyle.Fill;
            objectExecChart.Location = new System.Drawing.Point(0, 27);
            objectExecChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            objectExecChart.Name = "objectExecChart";
            objectExecChart.Size = new System.Drawing.Size(492, 325);
            objectExecChart.TabIndex = 0;
            objectExecChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblExecution, tsDateGroup, tsMeasures, tsTop });
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
    }
}
