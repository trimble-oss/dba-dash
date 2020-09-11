namespace DBAChecksGUI.Performance
{
    partial class Blocking
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
            this.chartBlocking = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.lblBlocking = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartBlocking
            // 
            this.chartBlocking.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartBlocking.Location = new System.Drawing.Point(0, 0);
            this.chartBlocking.Name = "chartBlocking";
            this.chartBlocking.Size = new System.Drawing.Size(712, 408);
            this.chartBlocking.TabIndex = 0;
            this.chartBlocking.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblBlocking});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(712, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // lblBlocking
            // 
            this.lblBlocking.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblBlocking.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBlocking.Name = "lblBlocking";
            this.lblBlocking.Size = new System.Drawing.Size(69, 22);
            this.lblBlocking.Text = "Blocking";
            // 
            // Blocking
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.chartBlocking);
            this.Name = "Blocking";
            this.Size = new System.Drawing.Size(712, 408);
            this.Load += new System.EventHandler(this.Blocking_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartBlocking;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblBlocking;
    }
}
