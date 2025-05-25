using DBADashGUI.CustomReports;

namespace DBADashGUI.AgentJobs
{
    partial class RunningJobs
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
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            minimumDurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            minToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            minToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            hrToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hrsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hrsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            hrsToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            hrsToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            dgvRunningJobs = new DBADashDataGridView();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRunningJobs).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsCols, tsFilter, tsClearFilter, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(951, 27);
            toolStrip1.TabIndex = 2;
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
            tsExcel.Text = "Excel";
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
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { minimumDurationToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // minimumDurationToolStripMenuItem
            // 
            minimumDurationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { noneToolStripMenuItem, minToolStripMenuItem, minToolStripMenuItem1, minToolStripMenuItem2, hrToolStripMenuItem, hrsToolStripMenuItem, hrsToolStripMenuItem1, hrsToolStripMenuItem2, hrsToolStripMenuItem3, dayToolStripMenuItem });
            minimumDurationToolStripMenuItem.Name = "minimumDurationToolStripMenuItem";
            minimumDurationToolStripMenuItem.Size = new System.Drawing.Size(217, 26);
            minimumDurationToolStripMenuItem.Text = "Minimum Duration";
            // 
            // noneToolStripMenuItem
            // 
            noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            noneToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            noneToolStripMenuItem.Tag = "-1";
            noneToolStripMenuItem.Text = "None";
            noneToolStripMenuItem.Click += SetMinimumDuration;
            // 
            // minToolStripMenuItem
            // 
            minToolStripMenuItem.Checked = true;
            minToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            minToolStripMenuItem.Name = "minToolStripMenuItem";
            minToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            minToolStripMenuItem.Tag = "60";
            minToolStripMenuItem.Text = "1min";
            minToolStripMenuItem.Click += SetMinimumDuration;
            // 
            // minToolStripMenuItem1
            // 
            minToolStripMenuItem1.Name = "minToolStripMenuItem1";
            minToolStripMenuItem1.Size = new System.Drawing.Size(133, 26);
            minToolStripMenuItem1.Tag = "600";
            minToolStripMenuItem1.Text = "10min";
            minToolStripMenuItem1.Click += SetMinimumDuration;
            // 
            // minToolStripMenuItem2
            // 
            minToolStripMenuItem2.Name = "minToolStripMenuItem2";
            minToolStripMenuItem2.Size = new System.Drawing.Size(133, 26);
            minToolStripMenuItem2.Tag = "1800";
            minToolStripMenuItem2.Text = "30min";
            minToolStripMenuItem2.Click += SetMinimumDuration;
            // 
            // hrToolStripMenuItem
            // 
            hrToolStripMenuItem.Name = "hrToolStripMenuItem";
            hrToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            hrToolStripMenuItem.Tag = "3600";
            hrToolStripMenuItem.Text = "1hr";
            hrToolStripMenuItem.Click += SetMinimumDuration;
            // 
            // hrsToolStripMenuItem
            // 
            hrsToolStripMenuItem.Name = "hrsToolStripMenuItem";
            hrsToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            hrsToolStripMenuItem.Tag = "7200";
            hrsToolStripMenuItem.Text = "2hrs";
            hrsToolStripMenuItem.Click += SetMinimumDuration;
            // 
            // hrsToolStripMenuItem1
            // 
            hrsToolStripMenuItem1.Name = "hrsToolStripMenuItem1";
            hrsToolStripMenuItem1.Size = new System.Drawing.Size(133, 26);
            hrsToolStripMenuItem1.Tag = "14400";
            hrsToolStripMenuItem1.Text = "4hrs";
            hrsToolStripMenuItem1.Click += SetMinimumDuration;
            // 
            // hrsToolStripMenuItem2
            // 
            hrsToolStripMenuItem2.Name = "hrsToolStripMenuItem2";
            hrsToolStripMenuItem2.Size = new System.Drawing.Size(133, 26);
            hrsToolStripMenuItem2.Tag = "21600";
            hrsToolStripMenuItem2.Text = "6hrs";
            hrsToolStripMenuItem2.Click += SetMinimumDuration;
            // 
            // hrsToolStripMenuItem3
            // 
            hrsToolStripMenuItem3.Name = "hrsToolStripMenuItem3";
            hrsToolStripMenuItem3.Size = new System.Drawing.Size(133, 26);
            hrsToolStripMenuItem3.Tag = "43200";
            hrsToolStripMenuItem3.Text = "12hrs";
            hrsToolStripMenuItem3.Click += SetMinimumDuration;
            // 
            // dayToolStripMenuItem
            // 
            dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            dayToolStripMenuItem.Size = new System.Drawing.Size(133, 26);
            dayToolStripMenuItem.Tag = "86400";
            dayToolStripMenuItem.Text = "1 day";
            dayToolStripMenuItem.Click += SetMinimumDuration;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // dgvRunningJobs
            // 
            dgvRunningJobs.AllowUserToAddRows = false;
            dgvRunningJobs.AllowUserToDeleteRows = false;
            dgvRunningJobs.AllowUserToOrderColumns = true;
            dgvRunningJobs.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvRunningJobs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvRunningJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvRunningJobs.DefaultCellStyle = dataGridViewCellStyle2;
            dgvRunningJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvRunningJobs.EnableHeadersVisualStyles = false;
            dgvRunningJobs.Location = new System.Drawing.Point(0, 27);
            dgvRunningJobs.Name = "dgvRunningJobs";
            dgvRunningJobs.ReadOnly = true;
            dgvRunningJobs.ResultSetID = 0;
            dgvRunningJobs.ResultSetName = null;
            dgvRunningJobs.RowHeadersVisible = false;
            dgvRunningJobs.RowHeadersWidth = 51;
            dgvRunningJobs.Size = new System.Drawing.Size(951, 380);
            dgvRunningJobs.TabIndex = 3;
            dgvRunningJobs.CellContentClick += DgvRunningJobs_CellContentClick;
            dgvRunningJobs.CellFormatting += DgvRunningJobs_CellFormatting;
            dgvRunningJobs.RowsAdded += DgvRunningJobs_RowsAdded;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 407);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(951, 22);
            statusStrip1.TabIndex = 4;
            statusStrip1.Text = "statusStrip1";
            // 
            // tsStatus
            // 
            tsStatus.Name = "tsStatus";
            tsStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 24);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Click += tsTrigger_Click;
            // 
            // RunningJobs
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvRunningJobs);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "RunningJobs";
            Size = new System.Drawing.Size(951, 429);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRunningJobs).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private DBADashDataGridView dgvRunningJobs;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem minimumDurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem minToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem hrToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hrsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hrsToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hrsToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem hrsToolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ToolStripButton tsTrigger;
    }
}
