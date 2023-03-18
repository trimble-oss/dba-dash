namespace DBADashGUI.Changes
{
    partial class Alerts
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
            dgvAlertsConfig = new System.Windows.Forms.DataGridView();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            pivotByAlertNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dgvAlerts = new System.Windows.Forms.DataGridView();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshAlerts = new System.Windows.Forms.ToolStripButton();
            tsCopyAlerts = new System.Windows.Forms.ToolStripButton();
            tsExcelAlerts = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvAlertsConfig).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAlerts).BeginInit();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgvAlertsConfig
            // 
            dgvAlertsConfig.AllowUserToAddRows = false;
            dgvAlertsConfig.AllowUserToDeleteRows = false;
            dgvAlertsConfig.BackgroundColor = System.Drawing.Color.White;
            dgvAlertsConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAlertsConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvAlertsConfig.Location = new System.Drawing.Point(0, 27);
            dgvAlertsConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvAlertsConfig.Name = "dgvAlertsConfig";
            dgvAlertsConfig.ReadOnly = true;
            dgvAlertsConfig.RowHeadersVisible = false;
            dgvAlertsConfig.RowHeadersWidth = 51;
            dgvAlertsConfig.RowTemplate.Height = 24;
            dgvAlertsConfig.Size = new System.Drawing.Size(667, 339);
            dgvAlertsConfig.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgvAlertsConfig);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvAlerts);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(667, 734);
            splitContainer1.SplitterDistance = 366;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefresh, tsCopy, tsExcel, toolStripDropDownButton1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(667, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(152, 24);
            toolStripLabel1.Text = "Alerts Configuration";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { pivotByAlertNameToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.Column_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Pivot";
            // 
            // pivotByAlertNameToolStripMenuItem
            // 
            pivotByAlertNameToolStripMenuItem.CheckOnClick = true;
            pivotByAlertNameToolStripMenuItem.Name = "pivotByAlertNameToolStripMenuItem";
            pivotByAlertNameToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            pivotByAlertNameToolStripMenuItem.Text = "Pivot By Alert Name";
            pivotByAlertNameToolStripMenuItem.Click += PivotByAlertNameToolStripMenuItem_Click;
            // 
            // dgvAlerts
            // 
            dgvAlerts.AllowUserToAddRows = false;
            dgvAlerts.AllowUserToDeleteRows = false;
            dgvAlerts.BackgroundColor = System.Drawing.Color.White;
            dgvAlerts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvAlerts.Location = new System.Drawing.Point(0, 27);
            dgvAlerts.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvAlerts.Name = "dgvAlerts";
            dgvAlerts.ReadOnly = true;
            dgvAlerts.RowHeadersVisible = false;
            dgvAlerts.RowHeadersWidth = 51;
            dgvAlerts.RowTemplate.Height = 24;
            dgvAlerts.Size = new System.Drawing.Size(667, 336);
            dgvAlerts.TabIndex = 0;
            dgvAlerts.CellContentClick += DgvAlerts_CellContentClick;
            dgvAlerts.RowsAdded += DgvAlerts_RowsAdded;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel2, tsRefreshAlerts, tsCopyAlerts, tsExcelAlerts });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(667, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(51, 24);
            toolStripLabel2.Text = "Alerts";
            // 
            // tsRefreshAlerts
            // 
            tsRefreshAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshAlerts.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshAlerts.Name = "tsRefreshAlerts";
            tsRefreshAlerts.Size = new System.Drawing.Size(29, 24);
            tsRefreshAlerts.Text = "Refresh";
            tsRefreshAlerts.Click += TsRefreshAlerts_Click;
            // 
            // tsCopyAlerts
            // 
            tsCopyAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyAlerts.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyAlerts.Name = "tsCopyAlerts";
            tsCopyAlerts.Size = new System.Drawing.Size(29, 24);
            tsCopyAlerts.Text = "Copy";
            tsCopyAlerts.Click += TsCopyAlerts_Click;
            // 
            // tsExcelAlerts
            // 
            tsExcelAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelAlerts.Image = Properties.Resources.excel16x16;
            tsExcelAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelAlerts.Name = "tsExcelAlerts";
            tsExcelAlerts.Size = new System.Drawing.Size(29, 24);
            tsExcelAlerts.Text = "Export Excel";
            tsExcelAlerts.Click += TsExcelAlerts_Click;
            // 
            // Alerts
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Alerts";
            Size = new System.Drawing.Size(667, 734);
            ((System.ComponentModel.ISupportInitialize)dgvAlertsConfig).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAlerts).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvAlertsConfig;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.DataGridView dgvAlerts;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem pivotByAlertNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsRefreshAlerts;
        private System.Windows.Forms.ToolStripButton tsCopyAlerts;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelAlerts;
    }
}
