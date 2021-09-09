
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnAdd = new System.Windows.Forms.Button();
            this.cboTagName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboTagValue = new System.Windows.Forms.ComboBox();
            this.chkTags = new System.Windows.Forms.CheckedListBox();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.splitAddSystem = new System.Windows.Forms.SplitContainer();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlTagEdit = new System.Windows.Forms.Panel();
            this.pnlTagReport = new System.Windows.Forms.Panel();
            this.dgvReport = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.splitEditReport = new System.Windows.Forms.SplitContainer();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTagName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTagValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewLinkColumn();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitAddSystem)).BeginInit();
            this.splitAddSystem.Panel1.SuspendLayout();
            this.splitAddSystem.Panel2.SuspendLayout();
            this.splitAddSystem.SuspendLayout();
            this.pnlTagEdit.SuspendLayout();
            this.pnlTagReport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitEditReport)).BeginInit();
            this.splitEditReport.Panel1.SuspendLayout();
            this.splitEditReport.Panel2.SuspendLayout();
            this.splitEditReport.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.toolStrip2);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.bttnAdd);
            this.panel1.Controls.Add(this.cboTagName);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cboTagValue);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(917, 80);
            this.panel1.TabIndex = 7;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Tag Name:";
            // 
            // bttnAdd
            // 
            this.bttnAdd.Location = new System.Drawing.Point(640, 34);
            this.bttnAdd.Name = "bttnAdd";
            this.bttnAdd.Size = new System.Drawing.Size(75, 23);
            this.bttnAdd.TabIndex = 5;
            this.bttnAdd.Text = "Add";
            this.bttnAdd.UseVisualStyleBackColor = true;
            this.bttnAdd.Click += new System.EventHandler(this.bttnAdd_Click);
            // 
            // cboTagName
            // 
            this.cboTagName.FormattingEnabled = true;
            this.cboTagName.Location = new System.Drawing.Point(102, 34);
            this.cboTagName.Name = "cboTagName";
            this.cboTagName.Size = new System.Drawing.Size(177, 24);
            this.cboTagName.TabIndex = 1;
            this.cboTagName.SelectedValueChanged += new System.EventHandler(this.cboTagName_SelectedValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(311, 37);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Tag Value:";
            // 
            // cboTagValue
            // 
            this.cboTagValue.FormattingEnabled = true;
            this.cboTagValue.Location = new System.Drawing.Point(408, 34);
            this.cboTagValue.Name = "cboTagValue";
            this.cboTagValue.Size = new System.Drawing.Size(178, 24);
            this.cboTagValue.TabIndex = 3;
            // 
            // chkTags
            // 
            this.chkTags.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkTags.FormattingEnabled = true;
            this.chkTags.Location = new System.Drawing.Point(0, 23);
            this.chkTags.Name = "chkTags";
            this.chkTags.Size = new System.Drawing.Size(917, 127);
            this.chkTags.TabIndex = 8;
            this.chkTags.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chkTags_ItemCheck);
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.SeaShell;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTagName,
            this.colTagValue});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 23);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(917, 124);
            this.dgv.TabIndex = 9;
            // 
            // splitAddSystem
            // 
            this.splitAddSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitAddSystem.Location = new System.Drawing.Point(0, 80);
            this.splitAddSystem.Name = "splitAddSystem";
            this.splitAddSystem.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitAddSystem.Panel1
            // 
            this.splitAddSystem.Panel1.Controls.Add(this.chkTags);
            this.splitAddSystem.Panel1.Controls.Add(this.label4);
            // 
            // splitAddSystem.Panel2
            // 
            this.splitAddSystem.Panel2.Controls.Add(this.dgv);
            this.splitAddSystem.Panel2.Controls.Add(this.label1);
            this.splitAddSystem.Size = new System.Drawing.Size(917, 301);
            this.splitAddSystem.SplitterDistance = 150;
            this.splitAddSystem.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.SystemColors.Control;
            this.label4.Dock = System.Windows.Forms.DockStyle.Top;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(917, 23);
            this.label4.TabIndex = 11;
            this.label4.Text = "User Tags";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(917, 23);
            this.label1.TabIndex = 10;
            this.label1.Text = "System Tags";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pnlTagEdit
            // 
            this.pnlTagEdit.Controls.Add(this.splitAddSystem);
            this.pnlTagEdit.Controls.Add(this.panel1);
            this.pnlTagEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTagEdit.Location = new System.Drawing.Point(0, 0);
            this.pnlTagEdit.Name = "pnlTagEdit";
            this.pnlTagEdit.Size = new System.Drawing.Size(917, 381);
            this.pnlTagEdit.TabIndex = 11;
            // 
            // pnlTagReport
            // 
            this.pnlTagReport.Controls.Add(this.dgvReport);
            this.pnlTagReport.Controls.Add(this.toolStrip1);
            this.pnlTagReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTagReport.Location = new System.Drawing.Point(0, 0);
            this.pnlTagReport.Name = "pnlTagReport";
            this.pnlTagReport.Size = new System.Drawing.Size(917, 394);
            this.pnlTagReport.TabIndex = 12;
            // 
            // dgvReport
            // 
            this.dgvReport.AllowUserToAddRows = false;
            this.dgvReport.AllowUserToDeleteRows = false;
            this.dgvReport.BackgroundColor = System.Drawing.Color.White;
            this.dgvReport.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReport.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance});
            this.dgvReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvReport.Location = new System.Drawing.Point(0, 31);
            this.dgvReport.Name = "dgvReport";
            this.dgvReport.ReadOnly = true;
            this.dgvReport.RowHeadersVisible = false;
            this.dgvReport.RowHeadersWidth = 51;
            this.dgvReport.RowTemplate.Height = 24;
            this.dgvReport.Size = new System.Drawing.Size(917, 363);
            this.dgvReport.TabIndex = 0;
            this.dgvReport.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvReport_CellContentClick);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(917, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // splitEditReport
            // 
            this.splitEditReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitEditReport.Location = new System.Drawing.Point(0, 0);
            this.splitEditReport.Name = "splitEditReport";
            this.splitEditReport.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitEditReport.Panel1
            // 
            this.splitEditReport.Panel1.Controls.Add(this.pnlTagEdit);
            // 
            // splitEditReport.Panel2
            // 
            this.splitEditReport.Panel2.Controls.Add(this.pnlTagReport);
            this.splitEditReport.Size = new System.Drawing.Size(917, 779);
            this.splitEditReport.SplitterDistance = 381;
            this.splitEditReport.TabIndex = 12;
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 28);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 28);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 28);
            this.tsExcel.Text = "Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsBack});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(917, 27);
            this.toolStrip2.TabIndex = 6;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // tsBack
            // 
            this.tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBack.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBack.Name = "tsBack";
            this.tsBack.Size = new System.Drawing.Size(29, 24);
            this.tsBack.Text = "Back";
            this.tsBack.Click += new System.EventHandler(this.tsBack_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Tag Name";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Tag Value";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 125;
            // 
            // colTagName
            // 
            this.colTagName.HeaderText = "Tag Name";
            this.colTagName.MinimumWidth = 6;
            this.colTagName.Name = "colTagName";
            this.colTagName.ReadOnly = true;
            this.colTagName.Width = 125;
            // 
            // colTagValue
            // 
            this.colTagValue.HeaderText = "Tag Value";
            this.colTagValue.MinimumWidth = 6;
            this.colTagValue.Name = "colTagValue";
            this.colTagValue.ReadOnly = true;
            this.colTagValue.Width = 125;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "Instance";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colInstance.Width = 125;
            // 
            // Tags
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitEditReport);
            this.Name = "Tags";
            this.Size = new System.Drawing.Size(917, 779);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.splitAddSystem.Panel1.ResumeLayout(false);
            this.splitAddSystem.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitAddSystem)).EndInit();
            this.splitAddSystem.ResumeLayout(false);
            this.pnlTagEdit.ResumeLayout(false);
            this.pnlTagReport.ResumeLayout(false);
            this.pnlTagReport.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitEditReport.Panel1.ResumeLayout(false);
            this.splitEditReport.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitEditReport)).EndInit();
            this.splitEditReport.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.ComboBox cboTagName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboTagValue;
        private System.Windows.Forms.CheckedListBox chkTags;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.SplitContainer splitAddSystem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTagValue;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlTagEdit;
        private System.Windows.Forms.Panel pnlTagReport;
        private System.Windows.Forms.DataGridView dgvReport;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitEditReport;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.DataGridViewLinkColumn colInstance;
    }
}
