namespace DBAChecksGUI.Performance
{
    partial class CPU
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
            this.chartCPU = new LiveCharts.WinForms.CartesianChart();
            this.SuspendLayout();
            // 
            // chartCPU
            // 
            this.chartCPU.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartCPU.Location = new System.Drawing.Point(0, 0);
            this.chartCPU.Name = "chartCPU";
            this.chartCPU.Size = new System.Drawing.Size(878, 264);
            this.chartCPU.TabIndex = 1;
            this.chartCPU.Text = "CPU";
            // 
            // CPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chartCPU);
            this.Name = "CPU";
            this.Size = new System.Drawing.Size(878, 264);
            this.ResumeLayout(false);

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartCPU;
    }
}
