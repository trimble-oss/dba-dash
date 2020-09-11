namespace DBAChecksGUI.Performance
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
            this.chartIO = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.lblIOPerformance = new System.Windows.Forms.ToolStripLabel();
            this.tsDrives = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartIO
            // 
            this.chartIO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartIO.Location = new System.Drawing.Point(0, 27);
            this.chartIO.Name = "chartIO";
            this.chartIO.Size = new System.Drawing.Size(773, 267);
            this.chartIO.TabIndex = 4;
            this.chartIO.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsMeasures,
            this.lblIOPerformance,
            this.tsDrives});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(773, 27);
            this.toolStrip1.TabIndex = 5;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsMeasures
            // 
            this.tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMeasures.Image = global::DBAChecksGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(34, 24);
            this.tsMeasures.Text = "Measures";
            // 
            // lblIOPerformance
            // 
            this.lblIOPerformance.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblIOPerformance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblIOPerformance.Name = "lblIOPerformance";
            this.lblIOPerformance.Size = new System.Drawing.Size(119, 24);
            this.lblIOPerformance.Text = "IO Performance";
            // 
            // tsDrives
            // 
            this.tsDrives.Image = global::DBAChecksGUI.Properties.Resources.Hard_Drive;
            this.tsDrives.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDrives.Name = "tsDrives";
            this.tsDrives.Size = new System.Drawing.Size(34, 24);
            // 
            // IOPerformance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chartIO);
            this.Controls.Add(this.toolStrip1);
            this.Name = "IOPerformance";
            this.Size = new System.Drawing.Size(773, 294);
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
    }
}
