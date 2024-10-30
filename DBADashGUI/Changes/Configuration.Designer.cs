using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class Configuration
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Configuration));
            dgvConfig = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            adviceConfiguredToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            adviceConfiguredALLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configuredOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configuredALLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            ((System.ComponentModel.ISupportInitialize)dgvConfig).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvConfig
            // 
            dgvConfig.AllowUserToAddRows = false;
            dgvConfig.AllowUserToDeleteRows = false;
            dgvConfig.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvConfig.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvConfig.DefaultCellStyle = dataGridViewCellStyle2;
            dgvConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvConfig.EnableHeadersVisualStyles = false;
            dgvConfig.Location = new System.Drawing.Point(0, 27);
            dgvConfig.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvConfig.Name = "dgvConfig";
            dgvConfig.ReadOnly = true;
            dgvConfig.ResultSetID = 0;
            dgvConfig.ResultSetName = null;
            dgvConfig.RowHeadersVisible = false;
            dgvConfig.RowHeadersWidth = 51;
            dgvConfig.Size = new System.Drawing.Size(568, 415);
            dgvConfig.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsCols, toolStripDropDownButton1, toolStripLabel1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(568, 27);
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
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { adviceConfiguredToolStripMenuItem, adviceConfiguredALLToolStripMenuItem, configuredOnlyToolStripMenuItem, configuredALLToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.FilterDropdown_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Filter";
            // 
            // adviceConfiguredToolStripMenuItem
            // 
            adviceConfiguredToolStripMenuItem.Name = "adviceConfiguredToolStripMenuItem";
            adviceConfiguredToolStripMenuItem.Size = new System.Drawing.Size(255, 26);
            adviceConfiguredToolStripMenuItem.Text = "Advice/Configured";
            adviceConfiguredToolStripMenuItem.ToolTipText = resources.GetString("adviceConfiguredToolStripMenuItem.ToolTipText");
            adviceConfiguredToolStripMenuItem.Click += ConfiguredOnlyToolStripMenuItem_Click;
            // 
            // adviceConfiguredALLToolStripMenuItem
            // 
            adviceConfiguredALLToolStripMenuItem.Checked = true;
            adviceConfiguredALLToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            adviceConfiguredALLToolStripMenuItem.Name = "adviceConfiguredALLToolStripMenuItem";
            adviceConfiguredALLToolStripMenuItem.Size = new System.Drawing.Size(255, 26);
            adviceConfiguredALLToolStripMenuItem.Text = "Advice/Configured - ALL";
            adviceConfiguredALLToolStripMenuItem.ToolTipText = "Show all configuration options with advice based highlighting\r\nBold = Default value has changed\r\nColor = Advice status.  See tooltip for info.\r\n";
            adviceConfiguredALLToolStripMenuItem.Click += ConfiguredOnlyToolStripMenuItem_Click;
            // 
            // configuredOnlyToolStripMenuItem
            // 
            configuredOnlyToolStripMenuItem.Name = "configuredOnlyToolStripMenuItem";
            configuredOnlyToolStripMenuItem.Size = new System.Drawing.Size(255, 26);
            configuredOnlyToolStripMenuItem.Text = "Configured";
            configuredOnlyToolStripMenuItem.ToolTipText = "Only show values that have been changed from the default.";
            configuredOnlyToolStripMenuItem.Click += ConfiguredOnlyToolStripMenuItem_Click;
            // 
            // configuredALLToolStripMenuItem
            // 
            configuredALLToolStripMenuItem.Name = "configuredALLToolStripMenuItem";
            configuredALLToolStripMenuItem.Size = new System.Drawing.Size(255, 26);
            configuredALLToolStripMenuItem.Text = "Configured - ALL";
            configuredALLToolStripMenuItem.ToolTipText = "Show all configuration options and highlight based on using default values only";
            configuredALLToolStripMenuItem.Click += ConfiguredOnlyToolStripMenuItem_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(106, 24);
            toolStripLabel1.Text = "Configuration";
            // 
            // Configuration
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            Controls.Add(dgvConfig);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Configuration";
            Size = new System.Drawing.Size(568, 442);
            ((System.ComponentModel.ISupportInitialize)dgvConfig).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvConfig;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem configuredOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripMenuItem configuredALLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem adviceConfiguredALLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem adviceConfiguredToolStripMenuItem;
    }
}
