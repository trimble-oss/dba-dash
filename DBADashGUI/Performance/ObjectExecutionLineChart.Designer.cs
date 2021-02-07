namespace DBADashGUI.Performance
{
    partial class ObjectExecutionLineChart
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectExecutionLineChart));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsOptions = new System.Windows.Forms.ToolStripDropDownButton();
            this.lineSmoothnesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth1 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmoothPoint5 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth0 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsTitle = new System.Windows.Forms.ToolStripLabel();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.chart1 = new DBADashGUI.Performance.CartesianChartWithDataTable();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsOptions,
            this.tsTitle,
            this.tsMeasures,
            this.tsGroup});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(546, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsOptions
            // 
            this.tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lineSmoothnesToolStripMenuItem});
            this.tsOptions.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsOptions.Name = "tsOptions";
            this.tsOptions.Size = new System.Drawing.Size(34, 28);
            this.tsOptions.Text = "Options";
            // 
            // lineSmoothnesToolStripMenuItem
            // 
            this.lineSmoothnesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsSmooth1,
            this.tsSmoothPoint5,
            this.tsSmooth0});
            this.lineSmoothnesToolStripMenuItem.Name = "lineSmoothnesToolStripMenuItem";
            this.lineSmoothnesToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.lineSmoothnesToolStripMenuItem.Text = "Line Smoothnes";
            // 
            // tsSmooth1
            // 
            this.tsSmooth1.Name = "tsSmooth1";
            this.tsSmooth1.Size = new System.Drawing.Size(111, 26);
            this.tsSmooth1.Tag = "1";
            this.tsSmooth1.Text = "1.0";
            this.tsSmooth1.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // tsSmoothPoint5
            // 
            this.tsSmoothPoint5.Name = "tsSmoothPoint5";
            this.tsSmoothPoint5.Size = new System.Drawing.Size(111, 26);
            this.tsSmoothPoint5.Tag = "0.5";
            this.tsSmoothPoint5.Text = "0.5";
            this.tsSmoothPoint5.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // tsSmooth0
            // 
            this.tsSmooth0.Name = "tsSmooth0";
            this.tsSmooth0.Size = new System.Drawing.Size(111, 26);
            this.tsSmooth0.Tag = "0";
            this.tsSmooth0.Text = "0";
            this.tsSmooth0.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // tsTitle
            // 
            this.tsTitle.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.tsTitle.Name = "tsTitle";
            this.tsTitle.Size = new System.Drawing.Size(33, 28);
            this.tsTitle.Text = "abc";
            // 
            // tsMeasures
            // 
            this.tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMeasures.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(34, 28);
            this.tsMeasures.Text = "Measure";
            // 
            // tsGroup
            // 
            this.tsGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsGroup.Image = ((System.Drawing.Image)(resources.GetObject("tsGroup.Image")));
            this.tsGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGroup.Name = "tsGroup";
            this.tsGroup.Size = new System.Drawing.Size(56, 28);
            this.tsGroup.Text = "1min";
            // 
            // chart1
            // 
            this.chart1.DefaultLineSmoothness = 0.5D;
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 31);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(546, 366);
            this.chart1.TabIndex = 0;
            // 
            // ObjectExecutionLineChart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "ObjectExecutionLineChart";
            this.Size = new System.Drawing.Size(546, 397);
            this.Load += new System.EventHandler(this.ObjectExecutionLineChart_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CartesianChartWithDataTable chart1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsOptions;
        private System.Windows.Forms.ToolStripMenuItem lineSmoothnesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsSmooth1;
        private System.Windows.Forms.ToolStripMenuItem tsSmoothPoint5;
        private System.Windows.Forms.ToolStripMenuItem tsSmooth0;
        private System.Windows.Forms.ToolStripLabel tsTitle;
        private System.Windows.Forms.ToolStripDropDownButton tsMeasures;
        private System.Windows.Forms.ToolStripDropDownButton tsGroup;
    }
}
