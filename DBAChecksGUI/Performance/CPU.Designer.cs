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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            this.AVGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MAXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartCPU
            // 
            this.chartCPU.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartCPU.Location = new System.Drawing.Point(0, 31);
            this.chartCPU.Name = "chartCPU";
            this.chartCPU.Size = new System.Drawing.Size(878, 233);
            this.chartCPU.TabIndex = 1;
            this.chartCPU.Text = "CPU";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsAgg});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(878, 31);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(38, 28);
            this.toolStripLabel1.Text = "CPU";
            // 
            // tsAgg
            // 
            this.tsAgg.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AVGToolStripMenuItem,
            this.MAXToolStripMenuItem});
            this.tsAgg.Image = global::DBAChecksGUI.Properties.Resources.AutoSum_16x;
            this.tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsAgg.Name = "tsAgg";
            this.tsAgg.Size = new System.Drawing.Size(34, 28);
            this.tsAgg.Text = "Aggregation";
            // 
            // AVGToolStripMenuItem
            // 
            this.AVGToolStripMenuItem.Checked = true;
            this.AVGToolStripMenuItem.CheckOnClick = true;
            this.AVGToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AVGToolStripMenuItem.Name = "AVGToolStripMenuItem";
            this.AVGToolStripMenuItem.Size = new System.Drawing.Size(124, 26);
            this.AVGToolStripMenuItem.Text = "AVG";
            this.AVGToolStripMenuItem.Click += new System.EventHandler(this.AVGToolStripMenuItem_Click);
            // 
            // MAXToolStripMenuItem
            // 
            this.MAXToolStripMenuItem.CheckOnClick = true;
            this.MAXToolStripMenuItem.Name = "MAXToolStripMenuItem";
            this.MAXToolStripMenuItem.Size = new System.Drawing.Size(124, 26);
            this.MAXToolStripMenuItem.Text = "MAX";
            this.MAXToolStripMenuItem.Click += new System.EventHandler(this.MAXToolStripMenuItem_Click);
            // 
            // CPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chartCPU);
            this.Controls.Add(this.toolStrip1);
            this.Name = "CPU";
            this.Size = new System.Drawing.Size(878, 264);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartCPU;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripDropDownButton tsAgg;
        private System.Windows.Forms.ToolStripMenuItem AVGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MAXToolStripMenuItem;
    }
}
