using DBADashGUI.CustomReports;

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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SchemaSnapshots));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            splitSnapshotSummary = new System.Windows.Forms.SplitContainer();
            gvSnapshots = new DBADashDataGridView();
            dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDB = new System.Windows.Forms.DataGridViewLinkColumn();
            SnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ValidatedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ValidForDays = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DaysSinceValidation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Modified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Dropped = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colExport = new System.Windows.Forms.DataGridViewLinkColumn();
            colTriggerSnapshot = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsSummaryBack = new System.Windows.Forms.ToolStripButton();
            tsSummaryPageNum = new System.Windows.Forms.ToolStripLabel();
            tsSummaryNext = new System.Windows.Forms.ToolStripButton();
            toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            tsSummaryPageSize = new System.Windows.Forms.ToolStripComboBox();
            lblStatus = new System.Windows.Forms.ToolStripLabel();
            gvSnapshotsDetail = new DBADashDataGridView();
            colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colView = new System.Windows.Forms.DataGridViewLinkColumn();
            colDiff = new System.Windows.Forms.DataGridViewLinkColumn();
            colExternalDiff = new System.Windows.Forms.DataGridViewLinkColumn();
            dgvInstanceSummary = new DBADashDataGridView();
            colInstance = new System.Windows.Forms.DataGridViewLinkColumn();
            colLastUpdated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colLastValidated = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsTrigger = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)splitSnapshotSummary).BeginInit();
            splitSnapshotSummary.Panel1.SuspendLayout();
            splitSnapshotSummary.Panel2.SuspendLayout();
            splitSnapshotSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gvSnapshots).BeginInit();
            toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gvSnapshotsDetail).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvInstanceSummary).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // splitSnapshotSummary
            // 
            splitSnapshotSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
            splitSnapshotSummary.Location = new System.Drawing.Point(0, 255);
            splitSnapshotSummary.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitSnapshotSummary.Name = "splitSnapshotSummary";
            splitSnapshotSummary.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitSnapshotSummary.Panel1
            // 
            splitSnapshotSummary.Panel1.Controls.Add(gvSnapshots);
            splitSnapshotSummary.Panel1.Controls.Add(toolStrip2);
            // 
            // splitSnapshotSummary.Panel2
            // 
            splitSnapshotSummary.Panel2.Controls.Add(gvSnapshotsDetail);
            splitSnapshotSummary.Size = new System.Drawing.Size(2019, 1271);
            splitSnapshotSummary.SplitterDistance = 776;
            splitSnapshotSummary.SplitterWidth = 5;
            splitSnapshotSummary.TabIndex = 1;
            // 
            // gvSnapshots
            // 
            gvSnapshots.AllowUserToAddRows = false;
            gvSnapshots.AllowUserToDeleteRows = false;
            gvSnapshots.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            gvSnapshots.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            gvSnapshots.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvSnapshots.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { dataGridViewTextBoxColumn14, colDB, SnapshotDate, ValidatedDate, ValidForDays, DaysSinceValidation, colCreated, Modified, Dropped, colExport, colTriggerSnapshot });
            gvSnapshots.Dock = System.Windows.Forms.DockStyle.Fill;
            gvSnapshots.EnableHeadersVisualStyles = false;
            gvSnapshots.Location = new System.Drawing.Point(0, 0);
            gvSnapshots.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            gvSnapshots.Name = "gvSnapshots";
            gvSnapshots.ReadOnly = true;
            gvSnapshots.ResultSetID = 0;
            gvSnapshots.ResultSetName = null;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            gvSnapshots.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            gvSnapshots.RowHeadersVisible = false;
            gvSnapshots.RowHeadersWidth = 51;
            gvSnapshots.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            gvSnapshots.Size = new System.Drawing.Size(2019, 745);
            gvSnapshots.TabIndex = 0;
            gvSnapshots.CellContentClick += GvSnapshots_CellContentClick;
            gvSnapshots.SelectionChanged += GvSnapshots_SelectionChanged;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "InstanceGroupName";
            dataGridViewTextBoxColumn14.HeaderText = "Instance";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 125;
            // 
            // colDB
            // 
            colDB.DataPropertyName = "DB";
            colDB.HeaderText = "DB";
            colDB.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colDB.MinimumWidth = 6;
            colDB.Name = "colDB";
            colDB.ReadOnly = true;
            colDB.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colDB.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colDB.Width = 56;
            // 
            // SnapshotDate
            // 
            SnapshotDate.DataPropertyName = "SnapshotDate";
            SnapshotDate.HeaderText = "Snapshot Date";
            SnapshotDate.MinimumWidth = 6;
            SnapshotDate.Name = "SnapshotDate";
            SnapshotDate.ReadOnly = true;
            SnapshotDate.Width = 131;
            // 
            // ValidatedDate
            // 
            ValidatedDate.DataPropertyName = "ValidatedDate";
            ValidatedDate.HeaderText = "Validated Date";
            ValidatedDate.MinimumWidth = 6;
            ValidatedDate.Name = "ValidatedDate";
            ValidatedDate.ReadOnly = true;
            ValidatedDate.Width = 130;
            // 
            // ValidForDays
            // 
            ValidForDays.DataPropertyName = "ValidForDays";
            ValidForDays.HeaderText = "Valid For (Days)";
            ValidForDays.MinimumWidth = 6;
            ValidForDays.Name = "ValidForDays";
            ValidForDays.ReadOnly = true;
            ValidForDays.Width = 125;
            // 
            // DaysSinceValidation
            // 
            DaysSinceValidation.DataPropertyName = "DaysSinceValidation";
            DaysSinceValidation.HeaderText = "Days Since Validation";
            DaysSinceValidation.MinimumWidth = 6;
            DaysSinceValidation.Name = "DaysSinceValidation";
            DaysSinceValidation.ReadOnly = true;
            DaysSinceValidation.Width = 125;
            // 
            // colCreated
            // 
            colCreated.DataPropertyName = "Created";
            colCreated.HeaderText = "Created";
            colCreated.MinimumWidth = 6;
            colCreated.Name = "colCreated";
            colCreated.ReadOnly = true;
            colCreated.Width = 87;
            // 
            // Modified
            // 
            Modified.DataPropertyName = "Modified";
            Modified.HeaderText = "Modified";
            Modified.MinimumWidth = 6;
            Modified.Name = "Modified";
            Modified.ReadOnly = true;
            Modified.Width = 90;
            // 
            // Dropped
            // 
            Dropped.DataPropertyName = "Dropped";
            Dropped.HeaderText = "Dropped";
            Dropped.MinimumWidth = 6;
            Dropped.Name = "Dropped";
            Dropped.ReadOnly = true;
            Dropped.Width = 92;
            // 
            // colExport
            // 
            colExport.HeaderText = "Export";
            colExport.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colExport.MinimumWidth = 6;
            colExport.Name = "colExport";
            colExport.ReadOnly = true;
            colExport.Text = "Export";
            colExport.ToolTipText = "Export DB Schema to disk";
            colExport.UseColumnTextForLinkValue = true;
            colExport.Width = 125;
            // 
            // colTriggerSnapshot
            // 
            colTriggerSnapshot.HeaderText = "Trigger Snapshot";
            colTriggerSnapshot.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colTriggerSnapshot.MinimumWidth = 6;
            colTriggerSnapshot.Name = "colTriggerSnapshot";
            colTriggerSnapshot.ReadOnly = true;
            colTriggerSnapshot.Text = "Trigger Snapshot";
            colTriggerSnapshot.UseColumnTextForLinkValue = true;
            colTriggerSnapshot.Width = 125;
            // 
            // toolStrip2
            // 
            toolStrip2.Dock = System.Windows.Forms.DockStyle.Bottom;
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsSummaryBack, tsSummaryPageNum, tsSummaryNext, toolStripLabel3, tsSummaryPageSize, lblStatus });
            toolStrip2.Location = new System.Drawing.Point(0, 745);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip2.Size = new System.Drawing.Size(2019, 31);
            toolStrip2.TabIndex = 2;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsSummaryBack
            // 
            tsSummaryBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSummaryBack.Image = (System.Drawing.Image)resources.GetObject("tsSummaryBack.Image");
            tsSummaryBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSummaryBack.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsSummaryBack.Name = "tsSummaryBack";
            tsSummaryBack.Size = new System.Drawing.Size(29, 24);
            tsSummaryBack.Text = "Previous";
            tsSummaryBack.Click += TsSummaryBack_Click;
            // 
            // tsSummaryPageNum
            // 
            tsSummaryPageNum.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsSummaryPageNum.Name = "tsSummaryPageNum";
            tsSummaryPageNum.Size = new System.Drawing.Size(53, 24);
            tsSummaryPageNum.Text = "Page 1";
            // 
            // tsSummaryNext
            // 
            tsSummaryNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSummaryNext.Image = (System.Drawing.Image)resources.GetObject("tsSummaryNext.Image");
            tsSummaryNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSummaryNext.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsSummaryNext.Name = "tsSummaryNext";
            tsSummaryNext.Size = new System.Drawing.Size(29, 24);
            tsSummaryNext.Text = "Next";
            tsSummaryNext.Click += TsSummaryNext_Click;
            // 
            // toolStripLabel3
            // 
            toolStripLabel3.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            toolStripLabel3.Name = "toolStripLabel3";
            toolStripLabel3.Size = new System.Drawing.Size(75, 24);
            toolStripLabel3.Text = "Page Size:";
            // 
            // tsSummaryPageSize
            // 
            tsSummaryPageSize.Items.AddRange(new object[] { "100", "200", "500", "1000", "5000" });
            tsSummaryPageSize.Name = "tsSummaryPageSize";
            tsSummaryPageSize.Size = new System.Drawing.Size(121, 31);
            tsSummaryPageSize.Text = "100";
            tsSummaryPageSize.Validated += TsSummaryPageSize_Validated;
            // 
            // lblStatus
            // 
            lblStatus.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblStatus.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 24);
            // 
            // gvSnapshotsDetail
            // 
            gvSnapshotsDetail.AllowUserToAddRows = false;
            gvSnapshotsDetail.AllowUserToDeleteRows = false;
            gvSnapshotsDetail.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            gvSnapshotsDetail.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            gvSnapshotsDetail.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvSnapshotsDetail.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colObjectName, colSchemaName, colAction, colView, colDiff, colExternalDiff });
            gvSnapshotsDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            gvSnapshotsDetail.EnableHeadersVisualStyles = false;
            gvSnapshotsDetail.Location = new System.Drawing.Point(0, 0);
            gvSnapshotsDetail.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            gvSnapshotsDetail.Name = "gvSnapshotsDetail";
            gvSnapshotsDetail.ReadOnly = true;
            gvSnapshotsDetail.ResultSetID = 0;
            gvSnapshotsDetail.ResultSetName = null;
            gvSnapshotsDetail.RowHeadersVisible = false;
            gvSnapshotsDetail.RowHeadersWidth = 51;
            gvSnapshotsDetail.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            gvSnapshotsDetail.Size = new System.Drawing.Size(2019, 490);
            gvSnapshotsDetail.TabIndex = 0;
            gvSnapshotsDetail.CellContentClick += GvSnapshotsDetail_CellContentClick;
            // 
            // colObjectName
            // 
            colObjectName.DataPropertyName = "ObjectName";
            colObjectName.HeaderText = "Object Name";
            colObjectName.MinimumWidth = 6;
            colObjectName.Name = "colObjectName";
            colObjectName.ReadOnly = true;
            colObjectName.Width = 119;
            // 
            // colSchemaName
            // 
            colSchemaName.DataPropertyName = "SchemaName";
            colSchemaName.HeaderText = "Schema Name";
            colSchemaName.MinimumWidth = 6;
            colSchemaName.Name = "colSchemaName";
            colSchemaName.ReadOnly = true;
            colSchemaName.Width = 129;
            // 
            // colAction
            // 
            colAction.DataPropertyName = "Action";
            colAction.HeaderText = "Action";
            colAction.MinimumWidth = 6;
            colAction.Name = "colAction";
            colAction.ReadOnly = true;
            colAction.Width = 76;
            // 
            // colView
            // 
            colView.DataPropertyName = "newDDLID";
            colView.HeaderText = "View";
            colView.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colView.MinimumWidth = 6;
            colView.Name = "colView";
            colView.ReadOnly = true;
            colView.Text = "View";
            colView.UseColumnTextForLinkValue = true;
            colView.Width = 125;
            // 
            // colDiff
            // 
            colDiff.DataPropertyName = "OldDDLID";
            colDiff.HeaderText = "Diff";
            colDiff.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colDiff.MinimumWidth = 6;
            colDiff.Name = "colDiff";
            colDiff.ReadOnly = true;
            colDiff.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colDiff.Text = "Diff";
            colDiff.UseColumnTextForLinkValue = true;
            colDiff.Width = 125;
            // 
            // colExternalDiff
            // 
            colExternalDiff.HeaderText = "External Diff";
            colExternalDiff.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colExternalDiff.MinimumWidth = 6;
            colExternalDiff.Name = "colExternalDiff";
            colExternalDiff.ReadOnly = true;
            colExternalDiff.Text = "External Diff";
            colExternalDiff.UseColumnTextForLinkValue = true;
            colExternalDiff.Width = 125;
            // 
            // dgvInstanceSummary
            // 
            dgvInstanceSummary.AllowUserToAddRows = false;
            dgvInstanceSummary.AllowUserToDeleteRows = false;
            dgvInstanceSummary.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInstanceSummary.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dgvInstanceSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInstanceSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colLastUpdated, colLastValidated });
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvInstanceSummary.DefaultCellStyle = dataGridViewCellStyle5;
            dgvInstanceSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvInstanceSummary.EnableHeadersVisualStyles = false;
            dgvInstanceSummary.Location = new System.Drawing.Point(0, 31);
            dgvInstanceSummary.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvInstanceSummary.Name = "dgvInstanceSummary";
            dgvInstanceSummary.ReadOnly = true;
            dgvInstanceSummary.ResultSetID = 0;
            dgvInstanceSummary.ResultSetName = null;
            dgvInstanceSummary.RowHeadersVisible = false;
            dgvInstanceSummary.RowHeadersWidth = 51;
            dgvInstanceSummary.Size = new System.Drawing.Size(2019, 224);
            dgvInstanceSummary.TabIndex = 3;
            dgvInstanceSummary.CellContentClick += DgvInstanceSummary_CellContentClick;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "InstanceGroupName";
            colInstance.HeaderText = "Instance";
            colInstance.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colInstance.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colInstance.Width = 90;
            // 
            // colLastUpdated
            // 
            colLastUpdated.DataPropertyName = "LastUpdated";
            colLastUpdated.HeaderText = "Last Updated";
            colLastUpdated.MinimumWidth = 6;
            colLastUpdated.Name = "colLastUpdated";
            colLastUpdated.ReadOnly = true;
            colLastUpdated.Width = 122;
            // 
            // colLastValidated
            // 
            colLastValidated.DataPropertyName = "LastValidated";
            colLastValidated.HeaderText = "Last Validated";
            colLastValidated.MinimumWidth = 6;
            colLastValidated.Name = "colLastValidated";
            colLastValidated.ReadOnly = true;
            colLastValidated.Width = 127;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsBack, tsTrigger });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip1.Size = new System.Drawing.Size(2019, 31);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Up level";
            tsBack.Click += TsBack_Click;
            // 
            // tsTrigger
            // 
            tsTrigger.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTrigger.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTrigger.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTrigger.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsTrigger.Name = "tsTrigger";
            tsTrigger.Size = new System.Drawing.Size(151, 24);
            tsTrigger.Text = "Trigger Collection";
            tsTrigger.Visible = false;
            tsTrigger.Click += tsTrigger_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn1.DataPropertyName = "DB";
            dataGridViewTextBoxColumn1.HeaderText = "DB";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn2.DataPropertyName = "SnapshotDate";
            dataGridViewTextBoxColumn2.HeaderText = "Snapshot Date";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn3.DataPropertyName = "ValidatedDate";
            dataGridViewTextBoxColumn3.HeaderText = "Validated Date";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn4.DataPropertyName = "ValidForDays";
            dataGridViewTextBoxColumn4.HeaderText = "Valid For (Days)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn5.DataPropertyName = "DaysSinceValidation";
            dataGridViewTextBoxColumn5.HeaderText = "Days Since Validation";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn6.DataPropertyName = "Created";
            dataGridViewTextBoxColumn6.HeaderText = "Created";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn7.DataPropertyName = "Modified";
            dataGridViewTextBoxColumn7.HeaderText = "Modified";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn8.DataPropertyName = "Dropped";
            dataGridViewTextBoxColumn8.HeaderText = "Dropped";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn9.DataPropertyName = "ObjectName";
            dataGridViewTextBoxColumn9.HeaderText = "Object Name";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 125;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn10.DataPropertyName = "SchemaName";
            dataGridViewTextBoxColumn10.HeaderText = "Schema Name";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 125;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn11.DataPropertyName = "Action";
            dataGridViewTextBoxColumn11.HeaderText = "Action";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 125;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn12.DataPropertyName = "SchemaName";
            dataGridViewTextBoxColumn12.HeaderText = "Schema Name";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 125;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewTextBoxColumn13.DataPropertyName = "Action";
            dataGridViewTextBoxColumn13.HeaderText = "Action";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 125;
            // 
            // SchemaSnapshots
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvInstanceSummary);
            Controls.Add(splitSnapshotSummary);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "SchemaSnapshots";
            Size = new System.Drawing.Size(2019, 1526);
            Load += SchemaSnapshots_Load;
            splitSnapshotSummary.Panel1.ResumeLayout(false);
            splitSnapshotSummary.Panel1.PerformLayout();
            splitSnapshotSummary.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitSnapshotSummary).EndInit();
            splitSnapshotSummary.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gvSnapshots).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gvSnapshotsDetail).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvInstanceSummary).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitSnapshotSummary;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsSummaryBack;
        private System.Windows.Forms.ToolStripLabel tsSummaryPageNum;
        private System.Windows.Forms.ToolStripButton tsSummaryNext;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripComboBox tsSummaryPageSize;
        private DBADashDataGridView gvSnapshots;
        private DBADashDataGridView gvSnapshotsDetail;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private DBADashDataGridView dgvInstanceSummary;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.DataGridViewLinkColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLastUpdated;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLastValidated;
        private System.Windows.Forms.ToolStripButton tsTrigger;
        private System.Windows.Forms.ToolStripLabel lblStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewLinkColumn colDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidatedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidForDays;
        private System.Windows.Forms.DataGridViewTextBoxColumn DaysSinceValidation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCreated;
        private System.Windows.Forms.DataGridViewTextBoxColumn Modified;
        private System.Windows.Forms.DataGridViewTextBoxColumn Dropped;
        private System.Windows.Forms.DataGridViewLinkColumn colExport;
        private System.Windows.Forms.DataGridViewLinkColumn colTriggerSnapshot;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchemaName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAction;
        private System.Windows.Forms.DataGridViewLinkColumn colView;
        private System.Windows.Forms.DataGridViewLinkColumn colDiff;
        private System.Windows.Forms.DataGridViewLinkColumn colExternalDiff;
    }
}
