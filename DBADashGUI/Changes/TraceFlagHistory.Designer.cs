using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class TraceFlagHistory
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            dgvHistory = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            TraceFlag = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Change = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvFlags = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            tsClearFilterHistory = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFlags).BeginInit();
            toolStrip1.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgvHistory
            // 
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.AllowUserToDeleteRows = false;
            dgvHistory.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvHistory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, TraceFlag, ChangeDate, Change });
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvHistory.DefaultCellStyle = dataGridViewCellStyle6;
            dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvHistory.EnableHeadersVisualStyles = false;
            dgvHistory.Location = new System.Drawing.Point(0, 27);
            dgvHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvHistory.Name = "dgvHistory";
            dgvHistory.ReadOnly = true;
            dgvHistory.ResultSetID = 0;
            dgvHistory.ResultSetName = null;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.RowHeadersWidth = 51;
            dgvHistory.Size = new System.Drawing.Size(676, 218);
            dgvHistory.TabIndex = 0;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 90;
            // 
            // TraceFlag
            // 
            TraceFlag.DataPropertyName = "TraceFlag";
            TraceFlag.HeaderText = "Trace Flag";
            TraceFlag.MinimumWidth = 6;
            TraceFlag.Name = "TraceFlag";
            TraceFlag.ReadOnly = true;
            TraceFlag.Width = 105;
            // 
            // ChangeDate
            // 
            ChangeDate.DataPropertyName = "ChangeDate";
            ChangeDate.HeaderText = "Change Date";
            ChangeDate.MinimumWidth = 6;
            ChangeDate.Name = "ChangeDate";
            ChangeDate.ReadOnly = true;
            ChangeDate.Width = 120;
            // 
            // Change
            // 
            Change.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            Change.DataPropertyName = "Change";
            Change.HeaderText = "Change";
            Change.MinimumWidth = 6;
            Change.Name = "Change";
            Change.ReadOnly = true;
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
            splitContainer1.Panel1.Controls.Add(dgvFlags);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvHistory);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(676, 494);
            splitContainer1.SplitterDistance = 244;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // dgvFlags
            // 
            dgvFlags.AllowUserToAddRows = false;
            dgvFlags.AllowUserToDeleteRows = false;
            dgvFlags.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvFlags.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            dgvFlags.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvFlags.DefaultCellStyle = dataGridViewCellStyle8;
            dgvFlags.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvFlags.EnableHeadersVisualStyles = false;
            dgvFlags.Location = new System.Drawing.Point(0, 27);
            dgvFlags.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvFlags.Name = "dgvFlags";
            dgvFlags.ReadOnly = true;
            dgvFlags.ResultSetID = 0;
            dgvFlags.ResultSetName = null;
            dgvFlags.RowHeadersVisible = false;
            dgvFlags.RowHeadersWidth = 51;
            dgvFlags.Size = new System.Drawing.Size(676, 217);
            dgvFlags.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefresh, tsCopy, tsExcel, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(676, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(86, 24);
            toolStripLabel1.Text = "Trace Flags";
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
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Filter_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel2, tsRefreshHistory, tsCopyHistory, tsExcelHistory, tsClearFilterHistory });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(676, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(61, 24);
            toolStripLabel2.Text = "History";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHistory.Name = "tsRefreshHistory";
            tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            tsRefreshHistory.Text = "Refresh";
            tsRefreshHistory.Click += TsRefreshHistory_Click;
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // tsClearFilterHistory
            // 
            tsClearFilterHistory.Enabled = false;
            tsClearFilterHistory.Image = Properties.Resources.Filter_16x;
            tsClearFilterHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterHistory.Name = "tsClearFilterHistory";
            tsClearFilterHistory.Size = new System.Drawing.Size(104, 24);
            tsClearFilterHistory.Text = "Clear Filter";
            // 
            // TraceFlagHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "TraceFlagHistory";
            Size = new System.Drawing.Size(676, 494);
            ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvFlags).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgvHistory;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvFlags;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn TraceFlag;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChangeDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn Change;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripButton tsClearFilterHistory;
    }
}
