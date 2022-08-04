namespace DBADashGUI.Performance
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
            this.tsClose = new System.Windows.Forms.ToolStripButton();
            this.tsUp = new System.Windows.Forms.ToolStripButton();
            this.lblCPU = new System.Windows.Forms.ToolStripLabel();
            this.tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            this.AVGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MAXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartCPU
            // 
            this.chartCPU.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartCPU.Location = new System.Drawing.Point(0, 27);
            this.chartCPU.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chartCPU.Name = "chartCPU";
            this.chartCPU.Size = new System.Drawing.Size(878, 303);
            this.chartCPU.TabIndex = 1;
            this.chartCPU.Text = "CPU";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsClose,
            this.tsUp,
            this.lblCPU,
            this.tsDateGrouping,
            this.tsAgg});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(878, 27);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsClose
            // 
            this.tsClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsClose.Image = global::DBADashGUI.Properties.Resources.Close_red_16x;
            this.tsClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsClose.Name = "tsClose";
            this.tsClose.Size = new System.Drawing.Size(29, 24);
            this.tsClose.Text = "Close";
            this.tsClose.Visible = false;
            this.tsClose.Click += new System.EventHandler(this.TsClose_Click);
            // 
            // tsUp
            // 
            this.tsUp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsUp.Image = global::DBADashGUI.Properties.Resources.arrow_Up_16xLG;
            this.tsUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsUp.Name = "tsUp";
            this.tsUp.Size = new System.Drawing.Size(29, 24);
            this.tsUp.Text = "Move Up";
            this.tsUp.Click += new System.EventHandler(this.TsUp_Click);
            // 
            // lblCPU
            // 
            this.lblCPU.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblCPU.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblCPU.Name = "lblCPU";
            this.lblCPU.Size = new System.Drawing.Size(105, 24);
            this.lblCPU.Text = "CPU: Instance";
            this.lblCPU.Visible = false;
            // 
            // tsAgg
            // 
            this.tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AVGToolStripMenuItem,
            this.MAXToolStripMenuItem});
            this.tsAgg.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsAgg.Name = "tsAgg";
            this.tsAgg.Size = new System.Drawing.Size(69, 24);
            this.tsAgg.Text = "Avg";
            // 
            // AVGToolStripMenuItem
            // 
            this.AVGToolStripMenuItem.Checked = true;
            this.AVGToolStripMenuItem.CheckOnClick = true;
            this.AVGToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AVGToolStripMenuItem.Name = "AVGToolStripMenuItem";
            this.AVGToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.AVGToolStripMenuItem.Tag = "Avg";
            this.AVGToolStripMenuItem.Text = "Avg";
            this.AVGToolStripMenuItem.Click += new System.EventHandler(this.AVGToolStripMenuItem_Click);
            // 
            // MAXToolStripMenuItem
            // 
            this.MAXToolStripMenuItem.CheckOnClick = true;
            this.MAXToolStripMenuItem.Name = "MAXToolStripMenuItem";
            this.MAXToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.MAXToolStripMenuItem.Tag = "Max";
            this.MAXToolStripMenuItem.Text = "Max";
            this.MAXToolStripMenuItem.Click += new System.EventHandler(this.MAXToolStripMenuItem_Click);
            // 
            // tsDateGrouping
            // 
            this.tsDateGrouping.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGrouping.Name = "tsDateGrouping";
            this.tsDateGrouping.Size = new System.Drawing.Size(76, 24);
            this.tsDateGrouping.Text = "1min";
            // 
            // CPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chartCPU);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "CPU";
            this.Size = new System.Drawing.Size(878, 330);
            this.Load += new System.EventHandler(this.CPU_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart chartCPU;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblCPU;
        private System.Windows.Forms.ToolStripDropDownButton tsAgg;
        private System.Windows.Forms.ToolStripMenuItem AVGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MAXToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGrouping;
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
    }
}
