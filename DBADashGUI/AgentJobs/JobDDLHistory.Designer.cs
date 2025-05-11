
namespace DBADashGUI.Changes
{
    partial class JobDDLHistory
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
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            diffControl1 = new DiffControl();
            dgv = new System.Windows.Forms.DataGridView();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDateModified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colJobInfoLink = new System.Windows.Forms.DataGridViewLinkColumn();
            colCompareInfo = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            SuspendLayout();
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
            splitContainer1.Panel1.Controls.Add(diffControl1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Size = new System.Drawing.Size(1017, 504);
            splitContainer1.SplitterDistance = 316;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
            // 
            // diffControl1
            // 
            diffControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            diffControl1.Location = new System.Drawing.Point(0, 0);
            diffControl1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            diffControl1.Mode = DiffControl.ViewMode.Inline;
            diffControl1.Name = "diffControl1";
            diffControl1.NewText = null;
            diffControl1.OldText = null;
            diffControl1.Size = new System.Drawing.Size(1017, 316);
            diffControl1.TabIndex = 0;
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colVersion, colSnapshotDate, colDateModified, colJobInfoLink, colCompareInfo });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersWidth = 51;
            dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.Size = new System.Drawing.Size(1017, 183);
            dgv.TabIndex = 0;
            dgv.CellContentClick += CellContentClick;
            dgv.SelectionChanged += Dgv_SelectionChanged;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "version_number";
            dataGridViewTextBoxColumn1.HeaderText = "Version Number";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "SnapshotDate";
            dataGridViewTextBoxColumn2.HeaderText = "Snapshot Date";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "date_modified";
            dataGridViewTextBoxColumn3.HeaderText = "Date Modified";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // colVersion
            // 
            colVersion.DataPropertyName = "version_number";
            colVersion.HeaderText = "Version Number";
            colVersion.MinimumWidth = 6;
            colVersion.Name = "colVersion";
            colVersion.ReadOnly = true;
            colVersion.Width = 125;
            // 
            // colSnapshotDate
            // 
            colSnapshotDate.DataPropertyName = "SnapshotDate";
            colSnapshotDate.HeaderText = "Snapshot Date";
            colSnapshotDate.MinimumWidth = 6;
            colSnapshotDate.Name = "colSnapshotDate";
            colSnapshotDate.ReadOnly = true;
            colSnapshotDate.Width = 125;
            // 
            // colDateModified
            // 
            colDateModified.DataPropertyName = "date_modified";
            colDateModified.HeaderText = "Date Modified";
            colDateModified.MinimumWidth = 6;
            colDateModified.Name = "colDateModified";
            colDateModified.ReadOnly = true;
            colDateModified.Width = 125;
            // 
            // colJobInfoLink
            // 
            colJobInfoLink.HeaderText = "Info";
            colJobInfoLink.MinimumWidth = 6;
            colJobInfoLink.Name = "colJobInfoLink";
            colJobInfoLink.ReadOnly = true;
            colJobInfoLink.Text = "Info";
            colJobInfoLink.UseColumnTextForLinkValue = true;
            colJobInfoLink.Width = 125;
            // 
            // colCompareInfo
            // 
            colCompareInfo.HeaderText = "Compare Info";
            colCompareInfo.MinimumWidth = 6;
            colCompareInfo.Name = "colCompareInfo";
            colCompareInfo.ReadOnly = true;
            colCompareInfo.Text = "Compare Info";
            colCompareInfo.UseColumnTextForLinkValue = true;
            colCompareInfo.Width = 125;
            // 
            // JobDDLHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "JobDDLHistory";
            Size = new System.Drawing.Size(1017, 504);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private DiffControl diffControl1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSnapshotDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDateModified;
        private System.Windows.Forms.DataGridViewLinkColumn colJobInfoLink;
        private System.Windows.Forms.DataGridViewLinkColumn colCompareInfo;
    }
}
