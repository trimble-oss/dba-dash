namespace DBADashGUI.Checks
{
    partial class IdentityColumns
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IdentityColumns));
            dgv = new System.Windows.Forms.DataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            tsColumns = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsConfig = new System.Windows.Forms.ToolStripDropDownButton();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureDatabaseThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(826, 458);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, statusFilterToolStrip1, tsColumns, tsCopy, tsExcel, tsConfig });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(826, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
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
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.Acknowledged = false;
            statusFilterToolStrip1.AcknowledgedVisible = false;
            statusFilterToolStrip1.Critical = true;
            statusFilterToolStrip1.CriticalVisible = true;
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.NA = true;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.NAVisible = true;
            statusFilterToolStrip1.OK = true;
            statusFilterToolStrip1.OKVisible = true;
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.Warning = true;
            statusFilterToolStrip1.WarningVisible = true;
            statusFilterToolStrip1.UserChangedStatusFilter += Filter_Click;
            // 
            // tsColumns
            // 
            tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsColumns.Image = Properties.Resources.Column_16x;
            tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsColumns.Name = "tsColumns";
            tsColumns.Size = new System.Drawing.Size(29, 24);
            tsColumns.Text = "Columns";
            tsColumns.Click += TsColumns_Click;
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
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsConfig
            // 
            tsConfig.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfig.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureRootThresholdsToolStripMenuItem, configureInstanceThresholdsToolStripMenuItem, configureDatabaseThresholdsToolStripMenuItem });
            tsConfig.Image = Properties.Resources.SettingsOutline_16x;
            tsConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfig.Name = "tsConfig";
            tsConfig.Size = new System.Drawing.Size(34, 24);
            tsConfig.Text = "Configure";
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            configureRootThresholdsToolStripMenuItem.Click += ConfigureRootThresholdsToolStripMenuItem_Click;
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            configureInstanceThresholdsToolStripMenuItem.Enabled = false;
            configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            configureInstanceThresholdsToolStripMenuItem.Click += ConfigureInstanceThresholdsToolStripMenuItem_Click;
            // 
            // configureDatabaseThresholdsToolStripMenuItem
            // 
            configureDatabaseThresholdsToolStripMenuItem.Enabled = false;
            configureDatabaseThresholdsToolStripMenuItem.Name = "configureDatabaseThresholdsToolStripMenuItem";
            configureDatabaseThresholdsToolStripMenuItem.Size = new System.Drawing.Size(299, 26);
            configureDatabaseThresholdsToolStripMenuItem.Text = "Configure Database Thresholds";
            configureDatabaseThresholdsToolStripMenuItem.Visible = false;
            configureDatabaseThresholdsToolStripMenuItem.Click += ConfigureDatabaseThresholdsToolStripMenuItem_Click;
            // 
            // IdentityColumns
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
            Name = "IdentityColumns";
            Size = new System.Drawing.Size(826, 485);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsColumns;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsConfig;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;
    }
}
