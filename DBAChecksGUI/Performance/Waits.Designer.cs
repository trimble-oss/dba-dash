namespace DBAChecksGUI.Performance
{
    partial class Waits
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Waits));
            this.waitChart = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.lblWaits = new System.Windows.Forms.ToolStripLabel();
            this.tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // waitChart
            // 
            this.waitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitChart.Location = new System.Drawing.Point(0, 27);
            this.waitChart.Name = "waitChart";
            this.waitChart.Size = new System.Drawing.Size(492, 255);
            this.waitChart.TabIndex = 0;
            this.waitChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblWaits,
            this.tsDateGrouping});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(492, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // lblWaits
            // 
            this.lblWaits.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaits.Name = "lblWaits";
            this.lblWaits.Size = new System.Drawing.Size(119, 24);
            this.lblWaits.Text = "Waits : Instance";
            // 
            // tsDateGrouping
            // 
            this.tsDateGrouping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsDateGrouping.Image = ((System.Drawing.Image)(resources.GetObject("tsDateGrouping.Image")));
            this.tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGrouping.Name = "tsDateGrouping";
            this.tsDateGrouping.Size = new System.Drawing.Size(56, 24);
            this.tsDateGrouping.Text = "1min";
            // 
            // Waits
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.waitChart);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Waits";
            this.Size = new System.Drawing.Size(492, 282);
            this.Load += new System.EventHandler(this.Waits_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart waitChart;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblWaits;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
    }
}
