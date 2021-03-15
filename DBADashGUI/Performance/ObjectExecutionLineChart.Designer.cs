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
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmoothPoint5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSmooth0 = new System.Windows.Forms.ToolStripMenuItem();
            this.pointSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsTitle = new System.Windows.Forms.ToolStripLabel();
            this.tsMeasures = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.chart1 = new DBADashGUI.Performance.CartesianChartWithDataTable();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mediumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.largeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.toolStrip1.Size = new System.Drawing.Size(683, 39);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsOptions
            // 
            this.tsOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lineSmoothnesToolStripMenuItem,
            this.pointSizeToolStripMenuItem});
            this.tsOptions.Image = global::DBADashGUI.Properties.Resources.SettingsOutline_16x;
            this.tsOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsOptions.Name = "tsOptions";
            this.tsOptions.Size = new System.Drawing.Size(34, 24);
            this.tsOptions.Text = "Options";
            // 
            // lineSmoothnesToolStripMenuItem
            // 
            this.lineSmoothnesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsSmooth1,
            this.toolStripMenuItem3,
            this.tsSmoothPoint5,
            this.toolStripMenuItem2,
            this.tsSmooth0});
            this.lineSmoothnesToolStripMenuItem.Name = "lineSmoothnesToolStripMenuItem";
            this.lineSmoothnesToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.lineSmoothnesToolStripMenuItem.Text = "Line Smoothnes";
            // 
            // tsSmooth1
            // 
            this.tsSmooth1.Name = "tsSmooth1";
            this.tsSmooth1.Size = new System.Drawing.Size(224, 26);
            this.tsSmooth1.Tag = "1.0";
            this.tsSmooth1.Text = "100%";
            this.tsSmooth1.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(224, 26);
            this.toolStripMenuItem3.Tag = "0.75";
            this.toolStripMenuItem3.Text = "75%";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // tsSmoothPoint5
            // 
            this.tsSmoothPoint5.Name = "tsSmoothPoint5";
            this.tsSmoothPoint5.Size = new System.Drawing.Size(224, 26);
            this.tsSmoothPoint5.Tag = "0.5";
            this.tsSmoothPoint5.Text = "50%";
            this.tsSmoothPoint5.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(224, 26);
            this.toolStripMenuItem2.Tag = "0.25";
            this.toolStripMenuItem2.Text = "25%";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // tsSmooth0
            // 
            this.tsSmooth0.Name = "tsSmooth0";
            this.tsSmooth0.Size = new System.Drawing.Size(224, 26);
            this.tsSmooth0.Tag = "0";
            this.tsSmooth0.Text = "0%";
            this.tsSmooth0.Click += new System.EventHandler(this.tsLineSmoothness_Click);
            // 
            // pointSizeToolStripMenuItem
            // 
            this.pointSizeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.smallToolStripMenuItem,
            this.mediumToolStripMenuItem,
            this.largeToolStripMenuItem});
            this.pointSizeToolStripMenuItem.Name = "pointSizeToolStripMenuItem";
            this.pointSizeToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.pointSizeToolStripMenuItem.Text = "Points";
            // 
            // tsTitle
            // 
            this.tsTitle.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.tsTitle.Name = "tsTitle";
            this.tsTitle.Size = new System.Drawing.Size(33, 24);
            this.tsTitle.Text = "abc";
            // 
            // tsMeasures
            // 
            this.tsMeasures.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsMeasures.Image = global::DBADashGUI.Properties.Resources.AddComputedField_16x;
            this.tsMeasures.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsMeasures.Name = "tsMeasures";
            this.tsMeasures.Size = new System.Drawing.Size(34, 24);
            this.tsMeasures.Text = "Measure";
            // 
            // tsGroup
            // 
            this.tsGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsGroup.Image = ((System.Drawing.Image)(resources.GetObject("tsGroup.Image")));
            this.tsGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGroup.Name = "tsGroup";
            this.tsGroup.Size = new System.Drawing.Size(56, 36);
            this.tsGroup.Text = "1min";
            // 
            // chart1
            // 
            this.chart1.DefaultLineSmoothness = 0.5D;
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 49);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(683, 448);
            this.chart1.TabIndex = 0;
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.noneToolStripMenuItem.Tag = "0";
            this.noneToolStripMenuItem.Text = "None";
            this.noneToolStripMenuItem.Click += new System.EventHandler(this.tsPointSize_Click);
            // 
            // smallToolStripMenuItem
            // 
            this.smallToolStripMenuItem.Name = "smallToolStripMenuItem";
            this.smallToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.smallToolStripMenuItem.Tag = "5";
            this.smallToolStripMenuItem.Text = "Small";
            this.smallToolStripMenuItem.Click += new System.EventHandler(this.tsPointSize_Click);
            // 
            // mediumToolStripMenuItem
            // 
            this.mediumToolStripMenuItem.Name = "mediumToolStripMenuItem";
            this.mediumToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.mediumToolStripMenuItem.Tag = "10";
            this.mediumToolStripMenuItem.Text = "Medium";
            this.mediumToolStripMenuItem.Click += new System.EventHandler(this.tsPointSize_Click);
            // 
            // largeToolStripMenuItem
            // 
            this.largeToolStripMenuItem.Name = "largeToolStripMenuItem";
            this.largeToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.largeToolStripMenuItem.Tag = "15";
            this.largeToolStripMenuItem.Text = "Large";
            this.largeToolStripMenuItem.Click += new System.EventHandler(this.tsPointSize_Click);
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
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem pointSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mediumToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem largeToolStripMenuItem;
    }
}
