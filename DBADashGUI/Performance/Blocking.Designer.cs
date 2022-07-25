namespace DBADashGUI.Performance
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
            this.tsClose = new System.Windows.Forms.ToolStripButton();
            this.tsUp = new System.Windows.Forms.ToolStripButton();
            this.lblBlocking = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chartBlocking
            // 
            this.chartBlocking.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartBlocking.Location = new System.Drawing.Point(0, 0);
            this.chartBlocking.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chartBlocking.Name = "chartBlocking";
            this.chartBlocking.Size = new System.Drawing.Size(712, 510);
            this.chartBlocking.TabIndex = 0;
            this.chartBlocking.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsClose,
            this.tsUp,
            this.lblBlocking});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(712, 27);
            this.toolStrip1.TabIndex = 1;
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
            this.tsUp.Visible = false;
            this.tsUp.Click += new System.EventHandler(this.tsUp_Click);
            // 
            // lblBlocking
            // 
            this.lblBlocking.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblBlocking.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblBlocking.Name = "lblBlocking";
            this.lblBlocking.Size = new System.Drawing.Size(69, 24);
            this.lblBlocking.Text = "Blocking";
            // 
            // Blocking
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.chartBlocking);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Blocking";
            this.Size = new System.Drawing.Size(712, 510);
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
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
    }
}
