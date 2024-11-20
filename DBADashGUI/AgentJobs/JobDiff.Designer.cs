
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
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            diffControl1 = new DiffControl();
            dgvJobs = new System.Windows.Forms.DataGridView();
            colDiff = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDescriptionA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDescriptionB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCatA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCatB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDM_A = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDM_B = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSnapshotUpdatedA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSnapshotUpdatedB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            panel1 = new System.Windows.Forms.Panel();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            bttnCompare = new System.Windows.Forms.Button();
            cboB = new System.Windows.Forms.ComboBox();
            cboA = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvJobs).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 84);
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
            splitContainer1.Panel2.Controls.Add(dgvJobs);
            splitContainer1.Size = new System.Drawing.Size(1338, 812);
            splitContainer1.SplitterDistance = 478;
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
            diffControl1.Size = new System.Drawing.Size(1338, 478);
            diffControl1.TabIndex = 0;
            // 
            // dgvJobs
            // 
            dgvJobs.AllowUserToAddRows = false;
            dgvJobs.AllowUserToDeleteRows = false;
            dgvJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvJobs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colDiff, colName, colDescriptionA, colDescriptionB, colCatA, colCatB, colDM_A, colDM_B, colSnapshotUpdatedA, colSnapshotUpdatedB });
            dgvJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvJobs.Location = new System.Drawing.Point(0, 0);
            dgvJobs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvJobs.Name = "dgvJobs";
            dgvJobs.ReadOnly = true;
            dgvJobs.RowHeadersVisible = false;
            dgvJobs.RowHeadersWidth = 51;
            dgvJobs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvJobs.Size = new System.Drawing.Size(1338, 329);
            dgvJobs.TabIndex = 0;
            dgvJobs.SelectionChanged += DgvJobs_SelectionChanged;
            // 
            // colDiff
            // 
            colDiff.DataPropertyName = "Diff";
            colDiff.HeaderText = "Diff";
            colDiff.MinimumWidth = 6;
            colDiff.Name = "colDiff";
            colDiff.ReadOnly = true;
            colDiff.Width = 58;
            // 
            // colName
            // 
            colName.DataPropertyName = "name";
            colName.HeaderText = "Name";
            colName.MinimumWidth = 6;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 74;
            // 
            // colDescriptionA
            // 
            colDescriptionA.DataPropertyName = "description_A";
            colDescriptionA.HeaderText = "Description A";
            colDescriptionA.MinimumWidth = 6;
            colDescriptionA.Name = "colDescriptionA";
            colDescriptionA.ReadOnly = true;
            colDescriptionA.Width = 111;
            // 
            // colDescriptionB
            // 
            colDescriptionB.DataPropertyName = "description_B";
            colDescriptionB.HeaderText = "Description B";
            colDescriptionB.MinimumWidth = 6;
            colDescriptionB.Name = "colDescriptionB";
            colDescriptionB.ReadOnly = true;
            colDescriptionB.Width = 111;
            // 
            // colCatA
            // 
            colCatA.DataPropertyName = "category_A";
            colCatA.HeaderText = "Category A";
            colCatA.MinimumWidth = 6;
            colCatA.Name = "colCatA";
            colCatA.ReadOnly = true;
            colCatA.Width = 99;
            // 
            // colCatB
            // 
            colCatB.DataPropertyName = "category_B";
            colCatB.HeaderText = "Category B";
            colCatB.MinimumWidth = 6;
            colCatB.Name = "colCatB";
            colCatB.ReadOnly = true;
            colCatB.Width = 99;
            // 
            // colDM_A
            // 
            colDM_A.DataPropertyName = "date_modified_A";
            colDM_A.HeaderText = "Date Modified A";
            colDM_A.MinimumWidth = 6;
            colDM_A.Name = "colDM_A";
            colDM_A.ReadOnly = true;
            colDM_A.Width = 114;
            // 
            // colDM_B
            // 
            colDM_B.DataPropertyName = "date_modified_B";
            colDM_B.HeaderText = "Date Modified B";
            colDM_B.MinimumWidth = 6;
            colDM_B.Name = "colDM_B";
            colDM_B.ReadOnly = true;
            colDM_B.Width = 114;
            // 
            // colSnapshotUpdatedA
            // 
            colSnapshotUpdatedA.DataPropertyName = "SnapshotUpdated_A";
            colSnapshotUpdatedA.HeaderText = "Snapshot Updated A";
            colSnapshotUpdatedA.MinimumWidth = 6;
            colSnapshotUpdatedA.Name = "colSnapshotUpdatedA";
            colSnapshotUpdatedA.ReadOnly = true;
            colSnapshotUpdatedA.Width = 154;
            // 
            // colSnapshotUpdatedB
            // 
            colSnapshotUpdatedB.DataPropertyName = "SnapshotUpdated_B";
            colSnapshotUpdatedB.HeaderText = "Snapshot Updated B";
            colSnapshotUpdatedB.MinimumWidth = 6;
            colSnapshotUpdatedB.Name = "colSnapshotUpdatedB";
            colSnapshotUpdatedB.ReadOnly = true;
            colSnapshotUpdatedB.Width = 154;
            // 
            // panel1
            // 
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(bttnCompare);
            panel1.Controls.Add(cboB);
            panel1.Controls.Add(cboA);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1338, 84);
            panel1.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(320, 35);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(79, 20);
            label2.TabIndex = 4;
            label2.Text = "Instance B:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(22, 34);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(80, 20);
            label1.TabIndex = 3;
            label1.Text = "Instance A:";
            // 
            // bttnCompare
            // 
            bttnCompare.ForeColor = System.Drawing.Color.Black;
            bttnCompare.Location = new System.Drawing.Point(620, 31);
            bttnCompare.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCompare.Name = "bttnCompare";
            bttnCompare.Size = new System.Drawing.Size(133, 30);
            bttnCompare.TabIndex = 2;
            bttnCompare.Text = "Compare";
            bttnCompare.UseVisualStyleBackColor = true;
            bttnCompare.Click += BttnCompare_Click;
            // 
            // cboB
            // 
            cboB.FormattingEnabled = true;
            cboB.Location = new System.Drawing.Point(404, 31);
            cboB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboB.Name = "cboB";
            cboB.Size = new System.Drawing.Size(190, 28);
            cboB.TabIndex = 1;
            cboB.SelectedIndexChanged += CboB_SelectedIndexChanged;
            // 
            // cboA
            // 
            cboA.FormattingEnabled = true;
            cboA.Location = new System.Drawing.Point(106, 31);
            cboA.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboA.Name = "cboA";
            cboA.Size = new System.Drawing.Size(190, 28);
            cboA.TabIndex = 0;
            cboA.SelectedIndexChanged += CboA_SelectedIndexChanged;
            // 
            // JobDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1338, 896);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "JobDiff";
            Text = "Job Diff";
            Load += JobDiff_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvJobs).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
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