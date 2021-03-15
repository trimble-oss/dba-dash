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
            this.objectExecChart = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.lblExecution = new System.Windows.Forms.ToolStripLabel();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // objectExecChart
            // 
            this.objectExecChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecChart.Location = new System.Drawing.Point(0, 31);
            this.objectExecChart.Name = "objectExecChart";
            this.objectExecChart.Size = new System.Drawing.Size(492, 251);
            this.objectExecChart.TabIndex = 0;
            this.objectExecChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblExecution,
            this.tsMeasures,
            this.tsDateGroup});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(492, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // lblExecution
            // 
            this.lblExecution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblExecution.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblExecution.Name = "lblExecution";
            this.lblExecution.Size = new System.Drawing.Size(116, 28);
            this.lblExecution.Text = "Execution Stats";
            // 
            // tsMeasures
            // 
            this.tsMeasures.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(105, 28);
            this.tsMeasures.Text = "Measures";
            // 
            // tsDateGroup
            // 
            this.tsDateGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsDateGroup.Image = ((System.Drawing.Image)(resources.GetObject("tsDateGroup.Image")));
            this.tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGroup.Name = "tsDateGroup";
            this.tsDateGroup.Size = new System.Drawing.Size(56, 28);
            this.tsDateGroup.Text = "1min";
            // 
            // ObjectExecution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.objectExecChart);
            this.Controls.Add(this.toolStrip1);
            this.Name = "ObjectExecution";
            this.Size = new System.Drawing.Size(492, 282);
            this.Load += new System.EventHandler(this.ObjectExecution_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LiveCharts.WinForms.CartesianChart objectExecChart;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel lblExecution;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripDropDownButton tsDateGroup;
    }
}
