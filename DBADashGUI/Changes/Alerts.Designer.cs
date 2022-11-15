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
            this.dgvAlertsConfig = new System.Windows.Forms.DataGridView();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.pivotByAlertNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvAlerts = new System.Windows.Forms.DataGridView();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefreshAlerts = new System.Windows.Forms.ToolStripButton();
            this.tsCopyAlerts = new System.Windows.Forms.ToolStripButton();
            this.tsExcelAlerts = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlertsConfig)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlerts)).BeginInit();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvAlertsConfig
            // 
            this.dgvAlertsConfig.AllowUserToAddRows = false;
            this.dgvAlertsConfig.AllowUserToDeleteRows = false;
            this.dgvAlertsConfig.BackgroundColor = System.Drawing.Color.White;
            this.dgvAlertsConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAlertsConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAlertsConfig.Location = new System.Drawing.Point(0, 27);
            this.dgvAlertsConfig.Name = "dgvAlertsConfig";
            this.dgvAlertsConfig.ReadOnly = true;
            this.dgvAlertsConfig.RowHeadersVisible = false;
            this.dgvAlertsConfig.RowHeadersWidth = 51;
            this.dgvAlertsConfig.RowTemplate.Height = 24;
            this.dgvAlertsConfig.Size = new System.Drawing.Size(667, 266);
            this.dgvAlertsConfig.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgvAlertsConfig);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvAlerts);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer1.Size = new System.Drawing.Size(667, 587);
            this.splitContainer1.SplitterDistance = 293;
            this.splitContainer1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.toolStripDropDownButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(667, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(152, 24);
            this.toolStripLabel1.Text = "Alerts Configuration";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.TsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.TsExcel_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pivotByAlertNameToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.Column_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            this.toolStripDropDownButton1.Text = "Pivot";
            // 
            // pivotByAlertNameToolStripMenuItem
            // 
            this.pivotByAlertNameToolStripMenuItem.CheckOnClick = true;
            this.pivotByAlertNameToolStripMenuItem.Name = "pivotByAlertNameToolStripMenuItem";
            this.pivotByAlertNameToolStripMenuItem.Size = new System.Drawing.Size(225, 26);
            this.pivotByAlertNameToolStripMenuItem.Text = "Pivot By Alert Name";
            this.pivotByAlertNameToolStripMenuItem.Click += new System.EventHandler(this.PivotByAlertNameToolStripMenuItem_Click);
            // 
            // dgvAlerts
            // 
            this.dgvAlerts.AllowUserToAddRows = false;
            this.dgvAlerts.AllowUserToDeleteRows = false;
            this.dgvAlerts.BackgroundColor = System.Drawing.Color.White;
            this.dgvAlerts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAlerts.Location = new System.Drawing.Point(0, 27);
            this.dgvAlerts.Name = "dgvAlerts";
            this.dgvAlerts.ReadOnly = true;
            this.dgvAlerts.RowHeadersVisible = false;
            this.dgvAlerts.RowHeadersWidth = 51;
            this.dgvAlerts.RowTemplate.Height = 24;
            this.dgvAlerts.Size = new System.Drawing.Size(667, 263);
            this.dgvAlerts.TabIndex = 0;
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.tsRefreshAlerts,
            this.tsCopyAlerts,
            this.tsExcelAlerts});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(667, 27);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(51, 24);
            this.toolStripLabel2.Text = "Alerts";
            // 
            // tsRefreshAlerts
            // 
            this.tsRefreshAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshAlerts.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshAlerts.Name = "tsRefreshAlerts";
            this.tsRefreshAlerts.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshAlerts.Text = "Refresh";
            this.tsRefreshAlerts.Click += new System.EventHandler(this.TsRefreshAlerts_Click);
            // 
            // tsCopyAlerts
            // 
            this.tsCopyAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyAlerts.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyAlerts.Name = "tsCopyAlerts";
            this.tsCopyAlerts.Size = new System.Drawing.Size(29, 24);
            this.tsCopyAlerts.Text = "Copy";
            this.tsCopyAlerts.Click += new System.EventHandler(this.TsCopyAlerts_Click);
            // 
            // tsExcelAlerts
            // 
            this.tsExcelAlerts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcelAlerts.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcelAlerts.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcelAlerts.Name = "tsExcelAlerts";
            this.tsExcelAlerts.Size = new System.Drawing.Size(29, 24);
            this.tsExcelAlerts.Text = "Export Excel";
            this.tsExcelAlerts.Click += new System.EventHandler(this.TsExcelAlerts_Click);
            // 
            // Alerts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "Alerts";
            this.Size = new System.Drawing.Size(667, 587);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlertsConfig)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlerts)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

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
