
namespace DBADashGUI
{
    partial class JobDiff
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobDiff));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.diffControl1 = new DBADashGUI.DiffControl();
            this.dgvJobs = new System.Windows.Forms.DataGridView();
            this.colDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescriptionA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescriptionB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCatA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCatB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDM_A = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDM_B = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSnapshotUpdatedA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSnapshotUpdatedB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.bttnCompare = new System.Windows.Forms.Button();
            this.cboB = new System.Windows.Forms.ComboBox();
            this.cboA = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 67);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.diffControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvJobs);
            this.splitContainer1.Size = new System.Drawing.Size(1338, 650);
            this.splitContainer1.SplitterDistance = 383;
            this.splitContainer1.TabIndex = 0;
            // 
            // diffControl1
            // 
            this.diffControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.diffControl1.Location = new System.Drawing.Point(0, 0);
            this.diffControl1.Mode = DBADashGUI.DiffControl.ViewMode.Inline;
            this.diffControl1.Name = "diffControl1";
            this.diffControl1.NewText = null;
            this.diffControl1.OldText = null;
            this.diffControl1.Size = new System.Drawing.Size(1338, 383);
            this.diffControl1.TabIndex = 0;
            // 
            // dgvJobs
            // 
            this.dgvJobs.AllowUserToAddRows = false;
            this.dgvJobs.AllowUserToDeleteRows = false;
            this.dgvJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvJobs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDiff,
            this.colName,
            this.colDescriptionA,
            this.colDescriptionB,
            this.colCatA,
            this.colCatB,
            this.colDM_A,
            this.colDM_B,
            this.colSnapshotUpdatedA,
            this.colSnapshotUpdatedB});
            this.dgvJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvJobs.Location = new System.Drawing.Point(0, 0);
            this.dgvJobs.Name = "dgvJobs";
            this.dgvJobs.ReadOnly = true;
            this.dgvJobs.RowHeadersVisible = false;
            this.dgvJobs.RowHeadersWidth = 51;
            this.dgvJobs.RowTemplate.Height = 24;
            this.dgvJobs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvJobs.Size = new System.Drawing.Size(1338, 263);
            this.dgvJobs.TabIndex = 0;
            this.dgvJobs.SelectionChanged += new System.EventHandler(this.dgvJobs_SelectionChanged);
            // 
            // colDiff
            // 
            this.colDiff.DataPropertyName = "Diff";
            this.colDiff.HeaderText = "Diff";
            this.colDiff.MinimumWidth = 6;
            this.colDiff.Name = "colDiff";
            this.colDiff.ReadOnly = true;
            this.colDiff.Width = 58;
            // 
            // colName
            // 
            this.colName.DataPropertyName = "name";
            this.colName.HeaderText = "Name";
            this.colName.MinimumWidth = 6;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.Width = 74;
            // 
            // colDescriptionA
            // 
            this.colDescriptionA.DataPropertyName = "description_A";
            this.colDescriptionA.HeaderText = "Description A";
            this.colDescriptionA.MinimumWidth = 6;
            this.colDescriptionA.Name = "colDescriptionA";
            this.colDescriptionA.ReadOnly = true;
            this.colDescriptionA.Width = 111;
            // 
            // colDescriptionB
            // 
            this.colDescriptionB.DataPropertyName = "description_B";
            this.colDescriptionB.HeaderText = "Description B";
            this.colDescriptionB.MinimumWidth = 6;
            this.colDescriptionB.Name = "colDescriptionB";
            this.colDescriptionB.ReadOnly = true;
            this.colDescriptionB.Width = 111;
            // 
            // colCatA
            // 
            this.colCatA.DataPropertyName = "category_A";
            this.colCatA.HeaderText = "Category A";
            this.colCatA.MinimumWidth = 6;
            this.colCatA.Name = "colCatA";
            this.colCatA.ReadOnly = true;
            this.colCatA.Width = 99;
            // 
            // colCatB
            // 
            this.colCatB.DataPropertyName = "category_B";
            this.colCatB.HeaderText = "Category B";
            this.colCatB.MinimumWidth = 6;
            this.colCatB.Name = "colCatB";
            this.colCatB.ReadOnly = true;
            this.colCatB.Width = 99;
            // 
            // colDM_A
            // 
            this.colDM_A.DataPropertyName = "date_modified_A";
            this.colDM_A.HeaderText = "Date Modified A";
            this.colDM_A.MinimumWidth = 6;
            this.colDM_A.Name = "colDM_A";
            this.colDM_A.ReadOnly = true;
            this.colDM_A.Width = 114;
            // 
            // colDM_B
            // 
            this.colDM_B.DataPropertyName = "date_modified_B";
            this.colDM_B.HeaderText = "Date Modified B";
            this.colDM_B.MinimumWidth = 6;
            this.colDM_B.Name = "colDM_B";
            this.colDM_B.ReadOnly = true;
            this.colDM_B.Width = 114;
            // 
            // colSnapshotUpdatedA
            // 
            this.colSnapshotUpdatedA.DataPropertyName = "SnapshotUpdated_A";
            this.colSnapshotUpdatedA.HeaderText = "Snapshot Updated A";
            this.colSnapshotUpdatedA.MinimumWidth = 6;
            this.colSnapshotUpdatedA.Name = "colSnapshotUpdatedA";
            this.colSnapshotUpdatedA.ReadOnly = true;
            this.colSnapshotUpdatedA.Width = 154;
            // 
            // colSnapshotUpdatedB
            // 
            this.colSnapshotUpdatedB.DataPropertyName = "SnapshotUpdated_B";
            this.colSnapshotUpdatedB.HeaderText = "Snapshot Updated B";
            this.colSnapshotUpdatedB.MinimumWidth = 6;
            this.colSnapshotUpdatedB.Name = "colSnapshotUpdatedB";
            this.colSnapshotUpdatedB.ReadOnly = true;
            this.colSnapshotUpdatedB.Width = 154;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.bttnCompare);
            this.panel1.Controls.Add(this.cboB);
            this.panel1.Controls.Add(this.cboA);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1338, 67);
            this.panel1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(320, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "Instance B:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Instance A:";
            // 
            // bttnCompare
            // 
            this.bttnCompare.Location = new System.Drawing.Point(620, 25);
            this.bttnCompare.Name = "bttnCompare";
            this.bttnCompare.Size = new System.Drawing.Size(133, 24);
            this.bttnCompare.TabIndex = 2;
            this.bttnCompare.Text = "Compare";
            this.bttnCompare.UseVisualStyleBackColor = true;
            this.bttnCompare.Click += new System.EventHandler(this.bttnCompare_Click);
            // 
            // cboB
            // 
            this.cboB.FormattingEnabled = true;
            this.cboB.Location = new System.Drawing.Point(404, 25);
            this.cboB.Name = "cboB";
            this.cboB.Size = new System.Drawing.Size(190, 24);
            this.cboB.TabIndex = 1;
            this.cboB.SelectedIndexChanged += new System.EventHandler(this.cboB_SelectedIndexChanged);
            // 
            // cboA
            // 
            this.cboA.FormattingEnabled = true;
            this.cboA.Location = new System.Drawing.Point(106, 25);
            this.cboA.Name = "cboA";
            this.cboA.Size = new System.Drawing.Size(190, 24);
            this.cboA.TabIndex = 0;
            this.cboA.SelectedIndexChanged += new System.EventHandler(this.cboA_SelectedIndexChanged);
            // 
            // JobDiff
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1338, 717);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "JobDiff";
            this.Text = "Job Diff";
            this.Load += new System.EventHandler(this.JobDiff_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvJobs)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private DiffControl diffControl1;
        private System.Windows.Forms.DataGridView dgvJobs;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cboB;
        private System.Windows.Forms.ComboBox cboA;
        private System.Windows.Forms.Button bttnCompare;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDiff;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDescriptionA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDescriptionB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCatA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCatB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDM_A;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDM_B;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSnapshotUpdatedA;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSnapshotUpdatedB;
    }
}