namespace DBADashGUI.Performance
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
            this.tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.criticalWaitsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stringFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // waitChart
            // 
            this.waitChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitChart.Location = new System.Drawing.Point(0, 31);
            this.waitChart.Name = "waitChart";
            this.waitChart.Size = new System.Drawing.Size(492, 251);
            this.waitChart.TabIndex = 0;
            this.waitChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblWaits,
            this.tsDateGrouping,
            this.tsFilter});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(492, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // lblWaits
            // 
            this.lblWaits.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblWaits.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaits.Name = "lblWaits";
            this.lblWaits.Size = new System.Drawing.Size(119, 28);
            this.lblWaits.Text = "Waits : Instance";
            // 
            // tsDateGrouping
            // 
            this.tsDateGrouping.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsDateGrouping.Image = ((System.Drawing.Image)(resources.GetObject("tsDateGrouping.Image")));
            this.tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGrouping.Name = "tsDateGrouping";
            this.tsDateGrouping.Size = new System.Drawing.Size(56, 28);
            this.tsDateGrouping.Text = "1min";
            // 
            // tsFilter
            // 
            this.tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.criticalWaitsOnlyToolStripMenuItem,
            this.stringFilterToolStripMenuItem});
            this.tsFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsFilter.Name = "tsFilter";
            this.tsFilter.Size = new System.Drawing.Size(34, 28);
            // 
            // criticalWaitsOnlyToolStripMenuItem
            // 
            this.criticalWaitsOnlyToolStripMenuItem.CheckOnClick = true;
            this.criticalWaitsOnlyToolStripMenuItem.Name = "criticalWaitsOnlyToolStripMenuItem";
            this.criticalWaitsOnlyToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.criticalWaitsOnlyToolStripMenuItem.Text = "Critical Waits Only";
            this.criticalWaitsOnlyToolStripMenuItem.ToolTipText = "Wait types that can indicate serious performance problems. e.g. RESOURCE_SEMAPHOR" +
    "E, THREADPOOL";
            this.criticalWaitsOnlyToolStripMenuItem.Click += new System.EventHandler(this.criticalWaitsOnlyToolStripMenuItem_Click);
            // 
            // stringFilterToolStripMenuItem
            // 
            this.stringFilterToolStripMenuItem.Name = "stringFilterToolStripMenuItem";
            this.stringFilterToolStripMenuItem.Size = new System.Drawing.Size(212, 26);
            this.stringFilterToolStripMenuItem.Text = "String Filter";
            this.stringFilterToolStripMenuItem.Click += new System.EventHandler(this.stringFilterToolStripMenuItem_Click);
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
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem criticalWaitsOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stringFilterToolStripMenuItem;
    }
}
