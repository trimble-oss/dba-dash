
using DBADashGUI.CustomReports;

namespace DBADashGUI.Tagging
{
    partial class Tags
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            panel1 = new System.Windows.Forms.Panel();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsBack = new System.Windows.Forms.ToolStripButton();
            lblInstance = new System.Windows.Forms.ToolStripLabel();
            label2 = new System.Windows.Forms.Label();
            bttnAdd = new System.Windows.Forms.Button();
            cboTagName = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            cboTagValue = new System.Windows.Forms.ComboBox();
            dgv = new DBADashDataGridView();
            colTagName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTagValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitAddSystem = new System.Windows.Forms.SplitContainer();
            dgvTags = new DBADashDataGridView();
            colTagID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colTagName1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ColTagValue1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            label4 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            pnlTagEdit = new System.Windows.Forms.Panel();
            pnlTagReport = new System.Windows.Forms.Panel();
            dgvReport = new DBADashDataGridView();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            splitEditReport = new System.Windows.Forms.SplitContainer();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tsClearFilterReport = new System.Windows.Forms.ToolStripButton();
            panel1.SuspendLayout();
            toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitAddSystem).BeginInit();
            splitAddSystem.Panel1.SuspendLayout();
            splitAddSystem.Panel2.SuspendLayout();
            splitAddSystem.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTags).BeginInit();
            pnlTagEdit.SuspendLayout();
            pnlTagReport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReport).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitEditReport).BeginInit();
            splitEditReport.Panel1.SuspendLayout();
            splitEditReport.Panel2.SuspendLayout();
            splitEditReport.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            panel1.Controls.Add(toolStrip2);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(bttnAdd);
            panel1.Controls.Add(cboTagName);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(cboTagValue);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(917, 100);
            panel1.TabIndex = 7;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsBack, lblInstance });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(917, 27);
            toolStrip2.TabIndex = 6;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // lblInstance
            // 
            lblInstance.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblInstance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblInstance.Name = "lblInstance";
            lblInstance.Size = new System.Drawing.Size(76, 24);
            lblInstance.Text = "Instance: ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(3, 46);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(79, 20);
            label2.TabIndex = 2;
            label2.Text = "Tag Name:";
            // 
            // bttnAdd
            // 
            bttnAdd.ForeColor = System.Drawing.Color.Black;
            bttnAdd.Location = new System.Drawing.Point(640, 42);
            bttnAdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(75, 29);
            bttnAdd.TabIndex = 5;
            bttnAdd.Text = "Add";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += BttnAdd_Click;
            // 
            // cboTagName
            // 
            cboTagName.FormattingEnabled = true;
            cboTagName.Location = new System.Drawing.Point(102, 42);
            cboTagName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboTagName.Name = "cboTagName";
            cboTagName.Size = new System.Drawing.Size(177, 28);
            cboTagName.TabIndex = 1;
            cboTagName.SelectedValueChanged += CboTagName_SelectedValueChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(311, 46);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(75, 20);
            label3.TabIndex = 4;
            label3.Text = "Tag Value:";
            // 
            // cboTagValue
            // 
            cboTagValue.FormattingEnabled = true;
            cboTagValue.Location = new System.Drawing.Point(408, 42);
            cboTagValue.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboTagValue.Name = "cboTagValue";
            cboTagValue.Size = new System.Drawing.Size(178, 28);
            cboTagValue.TabIndex = 3;
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.FromArgb(255, 245, 228);
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colTagName, colTagValue });
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle8;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 29);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(917, 155);
            dgv.TabIndex = 9;
            // 
            // colTagName
            // 
            colTagName.HeaderText = "Tag Name";
            colTagName.MinimumWidth = 6;
            colTagName.Name = "colTagName";
            colTagName.ReadOnly = true;
            colTagName.Width = 125;
            // 
            // colTagValue
            // 
            colTagValue.HeaderText = "Tag Value";
            colTagValue.MinimumWidth = 6;
            colTagValue.Name = "colTagValue";
            colTagValue.ReadOnly = true;
            colTagValue.Width = 125;
            // 
            // splitAddSystem
            // 
            splitAddSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            splitAddSystem.Location = new System.Drawing.Point(0, 100);
            splitAddSystem.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitAddSystem.Name = "splitAddSystem";
            splitAddSystem.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitAddSystem.Panel1
            // 
            splitAddSystem.Panel1.Controls.Add(dgvTags);
            splitAddSystem.Panel1.Controls.Add(label4);
            // 
            // splitAddSystem.Panel2
            // 
            splitAddSystem.Panel2.Controls.Add(dgv);
            splitAddSystem.Panel2.Controls.Add(label1);
            splitAddSystem.Size = new System.Drawing.Size(917, 376);
            splitAddSystem.SplitterDistance = 187;
            splitAddSystem.SplitterWidth = 5;
            splitAddSystem.TabIndex = 10;
            // 
            // dgvTags
            // 
            dgvTags.AllowUserToAddRows = false;
            dgvTags.AllowUserToDeleteRows = false;
            dgvTags.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle9.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvTags.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle9;
            dgvTags.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTags.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colTagID, colCheck, colTagName1, ColTagValue1 });
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle10.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvTags.DefaultCellStyle = dataGridViewCellStyle10;
            dgvTags.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvTags.EnableHeadersVisualStyles = false;
            dgvTags.Location = new System.Drawing.Point(0, 29);
            dgvTags.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvTags.Name = "dgvTags";
            dgvTags.ResultSetID = 0;
            dgvTags.ResultSetName = null;
            dgvTags.RowHeadersVisible = false;
            dgvTags.RowHeadersWidth = 51;
            dgvTags.Size = new System.Drawing.Size(917, 158);
            dgvTags.TabIndex = 12;
            dgvTags.CellMouseDoubleClick += DgvTags_CellMouseDoubleClick;
            dgvTags.CellMouseUp += DgvTags_CellMouseUp;
            dgvTags.CellValueChanged += DgvTags_CellValueChanged;
            // 
            // colTagID
            // 
            colTagID.HeaderText = "TagID";
            colTagID.MinimumWidth = 6;
            colTagID.Name = "colTagID";
            colTagID.ReadOnly = true;
            colTagID.Visible = false;
            colTagID.Width = 125;
            // 
            // colCheck
            // 
            colCheck.HeaderText = "";
            colCheck.MinimumWidth = 6;
            colCheck.Name = "colCheck";
            colCheck.Width = 125;
            // 
            // colTagName1
            // 
            colTagName1.HeaderText = "Tag Name";
            colTagName1.MinimumWidth = 6;
            colTagName1.Name = "colTagName1";
            colTagName1.ReadOnly = true;
            colTagName1.Width = 125;
            // 
            // ColTagValue1
            // 
            ColTagValue1.HeaderText = "Tag Value";
            ColTagValue1.MinimumWidth = 6;
            ColTagValue1.Name = "ColTagValue1";
            ColTagValue1.ReadOnly = true;
            ColTagValue1.Width = 125;
            // 
            // label4
            // 
            label4.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            label4.Dock = System.Windows.Forms.DockStyle.Top;
            label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold);
            label4.ForeColor = System.Drawing.Color.White;
            label4.Location = new System.Drawing.Point(0, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(917, 29);
            label4.TabIndex = 11;
            label4.Text = "User Tags";
            label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            label1.BackColor = System.Drawing.Color.FromArgb(0, 99, 163);
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold);
            label1.ForeColor = System.Drawing.Color.White;
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(917, 29);
            label1.TabIndex = 10;
            label1.Text = "System Tags";
            label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pnlTagEdit
            // 
            pnlTagEdit.Controls.Add(splitAddSystem);
            pnlTagEdit.Controls.Add(panel1);
            pnlTagEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlTagEdit.Location = new System.Drawing.Point(0, 0);
            pnlTagEdit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlTagEdit.Name = "pnlTagEdit";
            pnlTagEdit.Size = new System.Drawing.Size(917, 476);
            pnlTagEdit.TabIndex = 11;
            // 
            // pnlTagReport
            // 
            pnlTagReport.Controls.Add(dgvReport);
            pnlTagReport.Controls.Add(toolStrip1);
            pnlTagReport.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlTagReport.Location = new System.Drawing.Point(0, 0);
            pnlTagReport.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlTagReport.Name = "pnlTagReport";
            pnlTagReport.Size = new System.Drawing.Size(917, 493);
            pnlTagReport.TabIndex = 12;
            // 
            // dgvReport
            // 
            dgvReport.AllowUserToAddRows = false;
            dgvReport.AllowUserToDeleteRows = false;
            dgvReport.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle11.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle11.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvReport.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle11;
            dgvReport.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle12.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvReport.DefaultCellStyle = dataGridViewCellStyle12;
            dgvReport.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvReport.EnableHeadersVisualStyles = false;
            dgvReport.Location = new System.Drawing.Point(0, 27);
            dgvReport.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvReport.Name = "dgvReport";
            dgvReport.ReadOnly = true;
            dgvReport.ResultSetID = 0;
            dgvReport.ResultSetName = null;
            dgvReport.RowHeadersVisible = false;
            dgvReport.RowHeadersWidth = 51;
            dgvReport.Size = new System.Drawing.Size(917, 466);
            dgvReport.TabIndex = 0;
            dgvReport.CellContentClick += DgvReport_CellContentClick;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsClearFilterReport });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(917, 27);
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
            tsExcel.Text = "Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // splitEditReport
            // 
            splitEditReport.Dock = System.Windows.Forms.DockStyle.Fill;
            splitEditReport.Location = new System.Drawing.Point(0, 0);
            splitEditReport.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitEditReport.Name = "splitEditReport";
            splitEditReport.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitEditReport.Panel1
            // 
            splitEditReport.Panel1.Controls.Add(pnlTagEdit);
            // 
            // splitEditReport.Panel2
            // 
            splitEditReport.Panel2.Controls.Add(pnlTagReport);
            splitEditReport.Size = new System.Drawing.Size(917, 974);
            splitEditReport.SplitterDistance = 476;
            splitEditReport.SplitterWidth = 5;
            splitEditReport.TabIndex = 12;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Tag Name";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Visible = false;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Tag Value";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "Tag Name";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "Tag Value";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.HeaderText = "Tag Value";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 125;
            // 
            // tsClearFilterReport
            // 
            tsClearFilterReport.Enabled = false;
            tsClearFilterReport.Image = Properties.Resources.Filter_16x;
            tsClearFilterReport.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterReport.Name = "tsClearFilterReport";
            tsClearFilterReport.Size = new System.Drawing.Size(104, 24);
            tsClearFilterReport.Text = "Clear Filter";
            // 
            // Tags
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitEditReport);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "Tags";
            Size = new System.Drawing.Size(917, 974);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            splitAddSystem.Panel1.ResumeLayout(false);
            splitAddSystem.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitAddSystem).EndInit();
            splitAddSystem.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTags).EndInit();
            pnlTagEdit.ResumeLayout(false);
            pnlTagReport.ResumeLayout(false);
            pnlTagReport.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReport).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitEditReport.Panel1.ResumeLayout(false);
            splitEditReport.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitEditReport).EndInit();
            splitEditReport.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.ComboBox cboTagName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboTagValue;
        private DBADashDataGridView dgv;
        private System.Windows.Forms.SplitContainer splitAddSystem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagValue;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlTagEdit;
        private System.Windows.Forms.Panel pnlTagReport;
        private DBADashDataGridView dgvReport;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitEditReport;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsBack;
        private DBADashDataGridView dgvTags;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagID;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCheck;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagName1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColTagValue1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.ToolStripLabel lblInstance;
        private System.Windows.Forms.ToolStripButton tsClearFilterReport;
    }
}
