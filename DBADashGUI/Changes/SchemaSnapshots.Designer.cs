namespace DBADashGUI.Changes
{
    partial class SchemaSnapshots
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SchemaSnapshots));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.splitSnapshotSummary = new System.Windows.Forms.SplitContainer();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsSummaryBack = new System.Windows.Forms.ToolStripButton();
            this.tsSummaryPageNum = new System.Windows.Forms.ToolStripLabel();
            this.tsSummaryNext = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.tsSummaryPageSize = new System.Windows.Forms.ToolStripComboBox();
            this.gvSnapshots = new System.Windows.Forms.DataGridView();
            this.DB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValidatedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValidForDays = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DaysSinceValidation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Modified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Dropped = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gvSnapshotsDetail = new System.Windows.Forms.DataGridView();
            this.colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colView = new System.Windows.Forms.DataGridViewLinkColumn();
            this.Diff = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.splitSnapshotSummary)).BeginInit();
            this.splitSnapshotSummary.Panel1.SuspendLayout();
            this.splitSnapshotSummary.Panel2.SuspendLayout();
            this.splitSnapshotSummary.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gvSnapshots)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvSnapshotsDetail)).BeginInit();
            this.SuspendLayout();
            // 
            // splitSnapshotSummary
            // 
            this.splitSnapshotSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitSnapshotSummary.Location = new System.Drawing.Point(0, 0);
            this.splitSnapshotSummary.Name = "splitSnapshotSummary";
            this.splitSnapshotSummary.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitSnapshotSummary.Panel1
            // 
            this.splitSnapshotSummary.Panel1.Controls.Add(this.toolStrip2);
            this.splitSnapshotSummary.Panel1.Controls.Add(this.gvSnapshots);
            // 
            // splitSnapshotSummary.Panel2
            // 
            this.splitSnapshotSummary.Panel2.Controls.Add(this.gvSnapshotsDetail);
            this.splitSnapshotSummary.Size = new System.Drawing.Size(2019, 1221);
            this.splitSnapshotSummary.SplitterDistance = 746;
            this.splitSnapshotSummary.TabIndex = 1;
            // 
            // toolStrip2
            // 
            this.toolStrip2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsSummaryBack,
            this.tsSummaryPageNum,
            this.tsSummaryNext,
            this.toolStripLabel3,
            this.tsSummaryPageSize});
            this.toolStrip2.Location = new System.Drawing.Point(0, 718);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(2019, 28);
            this.toolStrip2.TabIndex = 2;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsSummaryBack
            // 
            this.tsSummaryBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSummaryBack.Image = ((System.Drawing.Image)(resources.GetObject("tsSummaryBack.Image")));
            this.tsSummaryBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSummaryBack.Name = "tsSummaryBack";
            this.tsSummaryBack.Size = new System.Drawing.Size(29, 25);
            this.tsSummaryBack.Text = "Previous";
            this.tsSummaryBack.Click += new System.EventHandler(this.tsSummaryBack_Click);
            // 
            // tsSummaryPageNum
            // 
            this.tsSummaryPageNum.Name = "tsSummaryPageNum";
            this.tsSummaryPageNum.Size = new System.Drawing.Size(53, 25);
            this.tsSummaryPageNum.Text = "Page 1";
            // 
            // tsSummaryNext
            // 
            this.tsSummaryNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSummaryNext.Image = ((System.Drawing.Image)(resources.GetObject("tsSummaryNext.Image")));
            this.tsSummaryNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSummaryNext.Name = "tsSummaryNext";
            this.tsSummaryNext.Size = new System.Drawing.Size(29, 25);
            this.tsSummaryNext.Text = "Next";
            this.tsSummaryNext.Click += new System.EventHandler(this.tsSummaryNext_Click);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(75, 25);
            this.toolStripLabel3.Text = "Page Size:";
            // 
            // tsSummaryPageSize
            // 
            this.tsSummaryPageSize.Items.AddRange(new object[] {
            "100",
            "200",
            "500",
            "1000",
            "5000"});
            this.tsSummaryPageSize.Name = "tsSummaryPageSize";
            this.tsSummaryPageSize.Size = new System.Drawing.Size(121, 28);
            this.tsSummaryPageSize.Text = "100";
            this.tsSummaryPageSize.Validated += new System.EventHandler(this.tsSummaryPageSize_Validated);
            // 
            // gvSnapshots
            // 
            this.gvSnapshots.AllowUserToAddRows = false;
            this.gvSnapshots.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gvSnapshots.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gvSnapshots.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvSnapshots.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DB,
            this.SnapshotDate,
            this.ValidatedDate,
            this.ValidForDays,
            this.DaysSinceValidation,
            this.colCreated,
            this.Modified,
            this.Dropped});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gvSnapshots.DefaultCellStyle = dataGridViewCellStyle2;
            this.gvSnapshots.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvSnapshots.Location = new System.Drawing.Point(0, 0);
            this.gvSnapshots.Name = "gvSnapshots";
            this.gvSnapshots.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gvSnapshots.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.gvSnapshots.RowHeadersVisible = false;
            this.gvSnapshots.RowHeadersWidth = 51;
            this.gvSnapshots.RowTemplate.Height = 24;
            this.gvSnapshots.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gvSnapshots.Size = new System.Drawing.Size(2019, 746);
            this.gvSnapshots.TabIndex = 0;
            this.gvSnapshots.SelectionChanged += new System.EventHandler(this.gvSnapshots_SelectionChanged);
            // 
            // DB
            // 
            this.DB.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.DB.DataPropertyName = "DB";
            this.DB.HeaderText = "DB";
            this.DB.MinimumWidth = 6;
            this.DB.Name = "DB";
            this.DB.ReadOnly = true;
            this.DB.Width = 56;
            // 
            // SnapshotDate
            // 
            this.SnapshotDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.SnapshotDate.DataPropertyName = "SnapshotDate";
            this.SnapshotDate.HeaderText = "Snapshot Date";
            this.SnapshotDate.MinimumWidth = 6;
            this.SnapshotDate.Name = "SnapshotDate";
            this.SnapshotDate.ReadOnly = true;
            this.SnapshotDate.Width = 120;
            // 
            // ValidatedDate
            // 
            this.ValidatedDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.ValidatedDate.DataPropertyName = "ValidatedDate";
            this.ValidatedDate.HeaderText = "Validated Date";
            this.ValidatedDate.MinimumWidth = 6;
            this.ValidatedDate.Name = "ValidatedDate";
            this.ValidatedDate.ReadOnly = true;
            this.ValidatedDate.Width = 119;
            // 
            // ValidForDays
            // 
            this.ValidForDays.DataPropertyName = "ValidForDays";
            this.ValidForDays.HeaderText = "Valid For (Days)";
            this.ValidForDays.MinimumWidth = 6;
            this.ValidForDays.Name = "ValidForDays";
            this.ValidForDays.ReadOnly = true;
            this.ValidForDays.Width = 125;
            // 
            // DaysSinceValidation
            // 
            this.DaysSinceValidation.DataPropertyName = "DaysSinceValidation";
            this.DaysSinceValidation.HeaderText = "Days Since Validation";
            this.DaysSinceValidation.MinimumWidth = 6;
            this.DaysSinceValidation.Name = "DaysSinceValidation";
            this.DaysSinceValidation.ReadOnly = true;
            this.DaysSinceValidation.Width = 125;
            // 
            // colCreated
            // 
            this.colCreated.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colCreated.DataPropertyName = "Created";
            this.colCreated.HeaderText = "Created";
            this.colCreated.MinimumWidth = 6;
            this.colCreated.Name = "colCreated";
            this.colCreated.ReadOnly = true;
            this.colCreated.Width = 87;
            // 
            // Modified
            // 
            this.Modified.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Modified.DataPropertyName = "Modified";
            this.Modified.HeaderText = "Modified";
            this.Modified.MinimumWidth = 6;
            this.Modified.Name = "Modified";
            this.Modified.ReadOnly = true;
            this.Modified.Width = 90;
            // 
            // Dropped
            // 
            this.Dropped.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.Dropped.DataPropertyName = "Dropped";
            this.Dropped.HeaderText = "Dropped";
            this.Dropped.MinimumWidth = 6;
            this.Dropped.Name = "Dropped";
            this.Dropped.ReadOnly = true;
            this.Dropped.Width = 92;
            // 
            // gvSnapshotsDetail
            // 
            this.gvSnapshotsDetail.AllowUserToAddRows = false;
            this.gvSnapshotsDetail.AllowUserToDeleteRows = false;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gvSnapshotsDetail.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.gvSnapshotsDetail.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvSnapshotsDetail.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colObjectName,
            this.colSchemaName,
            this.colAction,
            this.colView,
            this.Diff});
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gvSnapshotsDetail.DefaultCellStyle = dataGridViewCellStyle5;
            this.gvSnapshotsDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvSnapshotsDetail.Location = new System.Drawing.Point(0, 0);
            this.gvSnapshotsDetail.Name = "gvSnapshotsDetail";
            this.gvSnapshotsDetail.ReadOnly = true;
            this.gvSnapshotsDetail.RowHeadersVisible = false;
            this.gvSnapshotsDetail.RowHeadersWidth = 51;
            this.gvSnapshotsDetail.RowTemplate.Height = 24;
            this.gvSnapshotsDetail.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.gvSnapshotsDetail.Size = new System.Drawing.Size(2019, 471);
            this.gvSnapshotsDetail.TabIndex = 0;
            this.gvSnapshotsDetail.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gvSnapshotsDetail_CellContentClick);
            // 
            // colObjectName
            // 
            this.colObjectName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.colObjectName.DataPropertyName = "ObjectName";
            this.colObjectName.HeaderText = "Object Name";
            this.colObjectName.MinimumWidth = 6;
            this.colObjectName.Name = "colObjectName";
            this.colObjectName.ReadOnly = true;
            this.colObjectName.Width = 119;
            // 
            // colSchemaName
            // 
            this.colSchemaName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.colSchemaName.DataPropertyName = "SchemaName";
            this.colSchemaName.HeaderText = "Schema Name";
            this.colSchemaName.MinimumWidth = 6;
            this.colSchemaName.Name = "colSchemaName";
            this.colSchemaName.ReadOnly = true;
            this.colSchemaName.Width = 129;
            // 
            // colAction
            // 
            this.colAction.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.colAction.DataPropertyName = "Action";
            this.colAction.HeaderText = "Action";
            this.colAction.MinimumWidth = 6;
            this.colAction.Name = "colAction";
            this.colAction.ReadOnly = true;
            this.colAction.Width = 76;
            // 
            // colView
            // 
            this.colView.DataPropertyName = "newDDLID";
            this.colView.HeaderText = "View";
            this.colView.MinimumWidth = 6;
            this.colView.Name = "colView";
            this.colView.ReadOnly = true;
            this.colView.Text = "View";
            this.colView.UseColumnTextForLinkValue = true;
            this.colView.Width = 125;
            // 
            // Diff
            // 
            this.Diff.DataPropertyName = "OldDDLID";
            this.Diff.HeaderText = "Diff";
            this.Diff.MinimumWidth = 6;
            this.Diff.Name = "Diff";
            this.Diff.ReadOnly = true;
            this.Diff.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Diff.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Diff.Text = "Diff";
            this.Diff.UseColumnTextForLinkValue = true;
            this.Diff.Width = 125;
            // 
            // SchemaSnapshots
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitSnapshotSummary);
            this.Name = "SchemaSnapshots";
            this.Size = new System.Drawing.Size(2019, 1221);
            this.splitSnapshotSummary.Panel1.ResumeLayout(false);
            this.splitSnapshotSummary.Panel1.PerformLayout();
            this.splitSnapshotSummary.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitSnapshotSummary)).EndInit();
            this.splitSnapshotSummary.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gvSnapshots)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvSnapshotsDetail)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitSnapshotSummary;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsSummaryBack;
        private System.Windows.Forms.ToolStripLabel tsSummaryPageNum;
        private System.Windows.Forms.ToolStripButton tsSummaryNext;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripComboBox tsSummaryPageSize;
        private System.Windows.Forms.DataGridView gvSnapshots;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidatedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidForDays;
        private System.Windows.Forms.DataGridViewTextBoxColumn DaysSinceValidation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn Modified;
        private System.Windows.Forms.DataGridViewTextBoxColumn Dropped;
        private System.Windows.Forms.DataGridView gvSnapshotsDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchemaName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAction;
        private System.Windows.Forms.DataGridViewLinkColumn colView;
        private System.Windows.Forms.DataGridViewLinkColumn Diff;
    }
}
