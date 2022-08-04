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
            this.objectExecChart = new LiveCharts.WinForms.CartesianChart();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsClose = new System.Windows.Forms.ToolStripButton();
            this.tsUp = new System.Windows.Forms.ToolStripButton();
            this.lblExecution = new System.Windows.Forms.ToolStripLabel();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsDateGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // objectExecChart
            // 
            this.objectExecChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectExecChart.Location = new System.Drawing.Point(0, 27);
            this.objectExecChart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.objectExecChart.Name = "objectExecChart";
            this.objectExecChart.Size = new System.Drawing.Size(492, 325);
            this.objectExecChart.TabIndex = 0;
            this.objectExecChart.Text = "cartesianChart1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsClose,
            this.tsUp,
            this.lblExecution,
            this.tsDateGroup,
            this.tsMeasures});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(492, 27);
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
            this.tsUp.Click += new System.EventHandler(this.TsUp_Click);
            // 
            // lblExecution
            // 
            this.lblExecution.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblExecution.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblExecution.Name = "lblExecution";
            this.lblExecution.Size = new System.Drawing.Size(116, 24);
            this.lblExecution.Text = "Execution Stats";
            // 
            // tsMeasures
            // 
            this.tsMeasures.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(105, 24);
            this.tsMeasures.Text = "Measures";
            // 
            // tsDateGroup
            // 
            this.tsDateGroup.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsDateGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDateGroup.Name = "tsDateGroup";
            this.tsDateGroup.Size = new System.Drawing.Size(76, 24);
            this.tsDateGroup.Text = "1min";
            // 
            // ObjectExecution
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.objectExecChart);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ObjectExecution";
            this.Size = new System.Drawing.Size(492, 352);
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
        private System.Windows.Forms.ToolStripButton tsClose;
        private System.Windows.Forms.ToolStripButton tsUp;
    }
}
