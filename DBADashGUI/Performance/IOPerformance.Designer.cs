namespace DBADashGUI.Performance
{
    partial class IOPerformance
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IOPerformance));
            this.chartIO = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.lblIOPerformance = new System.Windows.Forms.ToolStripLabel();
            this.tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsDrives = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsFileGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsIOSummary = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartIO
            // 
            this.chartIO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartIO.Location = new System.Drawing.Point(0, 27);
            this.chartIO.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chartIO.Name = "chartIO";
            this.chartIO.Size = new System.Drawing.Size(773, 341);
            this.chartIO.TabIndex = 4;
            this.chartIO.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMeasures,
            this.lblIOPerformance,
            this.tsDateGroup,
            this.tsDrives,
            this.tsFileGroup,
            this.tsIOSummary});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(773, 27);
            this.toolStrip1.TabIndex = 5;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsMeasures
            // 
            this.tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMeasures.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(34, 24);
            this.tsMeasures.Text = "Measures";
            // 
            // lblIOPerformance
            // 
            this.lblIOPerformance.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblIOPerformance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblIOPerformance.Name = "lblIOPerformance";
            this.lblIOPerformance.Size = new System.Drawing.Size(119, 24);
            this.lblIOPerformance.Text = "IO Performance";
            // 
            // tsDateGroup
            // 
            this.tsDateGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsDateGroup.Image = ((System.Drawing.Image)(resources.GetObject("tsDateGroup.Image")));
            this.tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGroup.Name = "tsDateGroup";
            this.tsDateGroup.Size = new System.Drawing.Size(56, 24);
            this.tsDateGroup.Text = "1min";
            // 
            // tsDrives
            // 
            this.tsDrives.Image = global::DBADashGUI.Properties.Resources.Hard_Drive;
            this.tsDrives.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDrives.Name = "tsDrives";
            this.tsDrives.Size = new System.Drawing.Size(34, 24);
            // 
            // tsFileGroup
            // 
            this.tsFileGroup.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFileGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFileGroup.Name = "tsFileGroup";
            this.tsFileGroup.Size = new System.Drawing.Size(106, 24);
            this.tsFileGroup.Text = "Filegroup";
            this.tsFileGroup.Visible = false;
            // 
            // tsIOSummary
            // 
            this.tsIOSummary.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsIOSummary.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsIOSummary.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsIOSummary.Name = "tsIOSummary";
            this.tsIOSummary.Size = new System.Drawing.Size(29, 24);
            this.tsIOSummary.Text = "View Table Summary";
            this.tsIOSummary.Click += new System.EventHandler(this.TsIOSummary_Click);
            // 
            // IOPerformance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chartIO);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "IOPerformance";
            this.Size = new System.Drawing.Size(773, 368);
            this.Load += new System.EventHandler(this.IOPerformance_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartIO;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripLabel lblIOPerformance;
        private System.Windows.Forms.ToolStripDropDownButton tsDrives;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
        private System.Windows.Forms.ToolStripDropDownButton tsFileGroup;
        private System.Windows.Forms.ToolStripButton tsIOSummary;
    }
}
