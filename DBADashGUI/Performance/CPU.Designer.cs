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
            components = new System.ComponentModel.Container();
            chartCPU = new LiveCharts.WinForms.CartesianChart();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            tsCopyData = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsClose = new System.Windows.Forms.ToolStripButton();
            tsUp = new System.Windows.Forms.ToolStripButton();
            lblCPU = new System.Windows.Forms.ToolStripLabel();
            tsDateGrouping = new System.Windows.Forms.ToolStripDropDownButton();
            tsAgg = new System.Windows.Forms.ToolStripDropDownButton();
            AVGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            MAXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportDataToExcelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextMenuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // chartCPU
            // 
            chartCPU.ContextMenuStrip = contextMenuStrip1;
            chartCPU.Dock = System.Windows.Forms.DockStyle.Fill;
            chartCPU.Location = new System.Drawing.Point(0, 27);
            chartCPU.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chartCPU.Name = "chartCPU";
            chartCPU.Size = new System.Drawing.Size(878, 303);
            chartCPU.TabIndex = 1;
            chartCPU.Text = "CPU";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopyData, exportDataToExcelToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(218, 84);
            // 
            // tsCopyData
            // 
            tsCopyData.Image = Properties.Resources.ASX_Copy_grey_16x;
            tsCopyData.Name = "tsCopyData";
            tsCopyData.Size = new System.Drawing.Size(217, 26);
            tsCopyData.Text = "Copy Data";
            tsCopyData.Click += CopyData_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsClose, tsUp, lblCPU, tsDateGrouping, tsAgg });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(878, 27);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsClose
            // 
            tsClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClose.Image = Properties.Resources.Close_red_16x;
            tsClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClose.Name = "tsClose";
            tsClose.Size = new System.Drawing.Size(29, 24);
            tsClose.Text = "Close";
            tsClose.Visible = false;
            tsClose.Click += TsClose_Click;
            // 
            // tsUp
            // 
            tsUp.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsUp.Image = Properties.Resources.arrow_Up_16xLG;
            tsUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsUp.Name = "tsUp";
            tsUp.Size = new System.Drawing.Size(29, 24);
            tsUp.Text = "Move Up";
            tsUp.Click += TsUp_Click;
            // 
            // lblCPU
            // 
            lblCPU.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblCPU.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblCPU.Name = "lblCPU";
            lblCPU.Size = new System.Drawing.Size(105, 24);
            lblCPU.Text = "CPU: Instance";
            // 
            // tsDateGrouping
            // 
            tsDateGrouping.Image = Properties.Resources.Time_16x;
            tsDateGrouping.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDateGrouping.Name = "tsDateGrouping";
            tsDateGrouping.Size = new System.Drawing.Size(76, 24);
            tsDateGrouping.Text = "1min";
            // 
            // tsAgg
            // 
            tsAgg.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { AVGToolStripMenuItem, MAXToolStripMenuItem });
            tsAgg.Image = Properties.Resources.AddComputedField_16x;
            tsAgg.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsAgg.Name = "tsAgg";
            tsAgg.Size = new System.Drawing.Size(69, 24);
            tsAgg.Text = "Avg";
            // 
            // AVGToolStripMenuItem
            // 
            AVGToolStripMenuItem.Checked = true;
            AVGToolStripMenuItem.CheckOnClick = true;
            AVGToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            AVGToolStripMenuItem.Name = "AVGToolStripMenuItem";
            AVGToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            AVGToolStripMenuItem.Tag = "Avg";
            AVGToolStripMenuItem.Text = "Avg";
            AVGToolStripMenuItem.Click += AVGToolStripMenuItem_Click;
            // 
            // MAXToolStripMenuItem
            // 
            MAXToolStripMenuItem.CheckOnClick = true;
            MAXToolStripMenuItem.Name = "MAXToolStripMenuItem";
            MAXToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            MAXToolStripMenuItem.Tag = "Max";
            MAXToolStripMenuItem.Text = "Max";
            MAXToolStripMenuItem.Click += MAXToolStripMenuItem_Click;
            // 
            // exportDataToExcelToolStripMenuItem
            // 
            exportDataToExcelToolStripMenuItem.Image = Properties.Resources.excel16x16;
            exportDataToExcelToolStripMenuItem.Name = "exportDataToExcelToolStripMenuItem";
            exportDataToExcelToolStripMenuItem.Size = new System.Drawing.Size(217, 26);
            exportDataToExcelToolStripMenuItem.Text = "Export Data to Excel";
            exportDataToExcelToolStripMenuItem.Click += ExportDataToExcel_Click;
            // 
            // CPU
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(chartCPU);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CPU";
            Size = new System.Drawing.Size(878, 330);
            Load += CPU_Load;
            contextMenuStrip1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsCopyData;
        private System.Windows.Forms.ToolStripMenuItem exportDataToExcelToolStripMenuItem;
    }
}
