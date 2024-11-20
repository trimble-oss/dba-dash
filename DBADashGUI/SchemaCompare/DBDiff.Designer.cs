namespace DBADashGUI
{
    partial class DBDiff
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBDiff));
            cboInstanceA = new System.Windows.Forms.ComboBox();
            cboDatabaseA = new System.Windows.Forms.ComboBox();
            cboInstanceB = new System.Windows.Forms.ComboBox();
            cboDatabaseB = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            bttnCompare = new System.Windows.Forms.Button();
            gvDiff = new System.Windows.Forms.DataGridView();
            ObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ObjectType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DiffType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            panel1 = new System.Windows.Forms.Panel();
            chkIgnoreWhiteSpace = new System.Windows.Forms.CheckBox();
            bttnCopyA = new System.Windows.Forms.Button();
            bttnCopyB = new System.Windows.Forms.Button();
            bttnSwitch = new System.Windows.Forms.Button();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            cboDate_B = new System.Windows.Forms.ComboBox();
            cboDate_A = new System.Windows.Forms.ComboBox();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            pnlDiffType = new System.Windows.Forms.Panel();
            chkDiffType = new System.Windows.Forms.CheckedListBox();
            label8 = new System.Windows.Forms.Label();
            chkObjectType = new System.Windows.Forms.CheckedListBox();
            label7 = new System.Windows.Forms.Label();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)gvDiff).BeginInit();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            pnlDiffType.SuspendLayout();
            SuspendLayout();
            // 
            // cboInstanceA
            // 
            cboInstanceA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboInstanceA.FormattingEnabled = true;
            cboInstanceA.Location = new System.Drawing.Point(146, 15);
            cboInstanceA.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboInstanceA.Name = "cboInstanceA";
            cboInstanceA.Size = new System.Drawing.Size(181, 28);
            cboInstanceA.TabIndex = 0;
            cboInstanceA.SelectedIndexChanged += CboInstanceA_SelectedIndexChanged;
            // 
            // cboDatabaseA
            // 
            cboDatabaseA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDatabaseA.FormattingEnabled = true;
            cboDatabaseA.Location = new System.Drawing.Point(146, 52);
            cboDatabaseA.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboDatabaseA.Name = "cboDatabaseA";
            cboDatabaseA.Size = new System.Drawing.Size(181, 28);
            cboDatabaseA.TabIndex = 1;
            cboDatabaseA.SelectedIndexChanged += CboDatabaseA_SelectedIndexChanged;
            // 
            // cboInstanceB
            // 
            cboInstanceB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboInstanceB.FormattingEnabled = true;
            cboInstanceB.Location = new System.Drawing.Point(612, 16);
            cboInstanceB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboInstanceB.Name = "cboInstanceB";
            cboInstanceB.Size = new System.Drawing.Size(181, 28);
            cboInstanceB.TabIndex = 2;
            cboInstanceB.SelectedIndexChanged += CboInstanceB_SelectedIndexChanged;
            // 
            // cboDatabaseB
            // 
            cboDatabaseB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDatabaseB.FormattingEnabled = true;
            cboDatabaseB.Location = new System.Drawing.Point(612, 54);
            cboDatabaseB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboDatabaseB.Name = "cboDatabaseB";
            cboDatabaseB.Size = new System.Drawing.Size(181, 28);
            cboDatabaseB.TabIndex = 3;
            cboDatabaseB.SelectedIndexChanged += CboDatabaseB_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 16);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(80, 20);
            label1.TabIndex = 4;
            label1.Text = "Instance A:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(477, 19);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(79, 20);
            label2.TabIndex = 5;
            label2.Text = "Instance B:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 54);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(89, 20);
            label3.TabIndex = 6;
            label3.Text = "Database A:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(477, 60);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(88, 20);
            label4.TabIndex = 7;
            label4.Text = "Database B:";
            // 
            // bttnCompare
            // 
            bttnCompare.ForeColor = System.Drawing.Color.Black;
            bttnCompare.Location = new System.Drawing.Point(870, 86);
            bttnCompare.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCompare.Name = "bttnCompare";
            bttnCompare.Size = new System.Drawing.Size(148, 34);
            bttnCompare.TabIndex = 8;
            bttnCompare.Text = "Compare";
            bttnCompare.UseVisualStyleBackColor = true;
            bttnCompare.Click += BttnCompare_Click;
            // 
            // gvDiff
            // 
            gvDiff.AllowUserToAddRows = false;
            gvDiff.AllowUserToDeleteRows = false;
            gvDiff.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gvDiff.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { ObjectName, SchemaName, ObjectType, DiffType });
            gvDiff.Dock = System.Windows.Forms.DockStyle.Fill;
            gvDiff.Location = new System.Drawing.Point(207, 0);
            gvDiff.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            gvDiff.Name = "gvDiff";
            gvDiff.ReadOnly = true;
            gvDiff.RowHeadersWidth = 51;
            gvDiff.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            gvDiff.Size = new System.Drawing.Size(937, 465);
            gvDiff.TabIndex = 9;
            gvDiff.SelectionChanged += GvDiff_SelectionChanged;
            // 
            // ObjectName
            // 
            ObjectName.DataPropertyName = "ObjectName";
            ObjectName.HeaderText = "Object Name";
            ObjectName.MinimumWidth = 6;
            ObjectName.Name = "ObjectName";
            ObjectName.ReadOnly = true;
            ObjectName.Width = 125;
            // 
            // SchemaName
            // 
            SchemaName.DataPropertyName = "SchemaName";
            SchemaName.HeaderText = "Schema";
            SchemaName.MinimumWidth = 6;
            SchemaName.Name = "SchemaName";
            SchemaName.ReadOnly = true;
            SchemaName.Width = 125;
            // 
            // ObjectType
            // 
            ObjectType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            ObjectType.DataPropertyName = "TypeDescription";
            ObjectType.HeaderText = "Object Type";
            ObjectType.MinimumWidth = 6;
            ObjectType.Name = "ObjectType";
            ObjectType.ReadOnly = true;
            ObjectType.Width = 117;
            // 
            // DiffType
            // 
            DiffType.DataPropertyName = "DiffType";
            DiffType.HeaderText = "Diff Type";
            DiffType.MinimumWidth = 6;
            DiffType.Name = "DiffType";
            DiffType.ReadOnly = true;
            DiffType.Width = 125;
            // 
            // panel1
            // 
            panel1.Controls.Add(chkIgnoreWhiteSpace);
            panel1.Controls.Add(bttnCopyA);
            panel1.Controls.Add(bttnCopyB);
            panel1.Controls.Add(bttnSwitch);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(cboDate_B);
            panel1.Controls.Add(cboDate_A);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(cboInstanceA);
            panel1.Controls.Add(bttnCompare);
            panel1.Controls.Add(cboDatabaseA);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(cboInstanceB);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(cboDatabaseB);
            panel1.Controls.Add(label2);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1144, 186);
            panel1.TabIndex = 10;
            // 
            // chkIgnoreWhiteSpace
            // 
            chkIgnoreWhiteSpace.AutoSize = true;
            chkIgnoreWhiteSpace.Location = new System.Drawing.Point(870, 16);
            chkIgnoreWhiteSpace.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkIgnoreWhiteSpace.Name = "chkIgnoreWhiteSpace";
            chkIgnoreWhiteSpace.Size = new System.Drawing.Size(155, 24);
            chkIgnoreWhiteSpace.TabIndex = 17;
            chkIgnoreWhiteSpace.Text = "Ignore Whitespace";
            chkIgnoreWhiteSpace.UseVisualStyleBackColor = true;
            chkIgnoreWhiteSpace.CheckedChanged += ChkIgnoreWhiteSpace_CheckedChanged;
            // 
            // bttnCopyA
            // 
            bttnCopyA.Image = Properties.Resources.Next_grey_16x;
            bttnCopyA.Location = new System.Drawing.Point(252, 129);
            bttnCopyA.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCopyA.Name = "bttnCopyA";
            bttnCopyA.Size = new System.Drawing.Size(75, 29);
            bttnCopyA.TabIndex = 16;
            bttnCopyA.UseVisualStyleBackColor = true;
            bttnCopyA.Click += BttnCopyA_Click;
            // 
            // bttnCopyB
            // 
            bttnCopyB.Image = Properties.Resources.Previous_grey_16x;
            bttnCopyB.Location = new System.Drawing.Point(612, 129);
            bttnCopyB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCopyB.Name = "bttnCopyB";
            bttnCopyB.Size = new System.Drawing.Size(75, 29);
            bttnCopyB.TabIndex = 15;
            bttnCopyB.UseVisualStyleBackColor = true;
            bttnCopyB.Click += BttnCopyB_Click;
            // 
            // bttnSwitch
            // 
            bttnSwitch.Image = Properties.Resources.SwitchSourceOrTarget_16x;
            bttnSwitch.Location = new System.Drawing.Point(361, 46);
            bttnSwitch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnSwitch.Name = "bttnSwitch";
            bttnSwitch.Size = new System.Drawing.Size(75, 29);
            bttnSwitch.TabIndex = 14;
            bttnSwitch.UseVisualStyleBackColor = true;
            bttnSwitch.Click += BttnSwitch_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(477, 98);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(125, 20);
            label6.TabIndex = 13;
            label6.Text = "Snapshot Version:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(12, 91);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(125, 20);
            label5.TabIndex = 12;
            label5.Text = "Snapshot Version:";
            // 
            // cboDate_B
            // 
            cboDate_B.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDate_B.FormattingEnabled = true;
            cboDate_B.Location = new System.Drawing.Point(612, 91);
            cboDate_B.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboDate_B.Name = "cboDate_B";
            cboDate_B.Size = new System.Drawing.Size(181, 28);
            cboDate_B.TabIndex = 11;
            // 
            // cboDate_A
            // 
            cboDate_A.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDate_A.FormattingEnabled = true;
            cboDate_A.Location = new System.Drawing.Point(146, 90);
            cboDate_A.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cboDate_A.Name = "cboDate_A";
            cboDate_A.Size = new System.Drawing.Size(181, 28);
            cboDate_A.TabIndex = 10;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 186);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(pnlDiffType);
            splitContainer1.Panel1.Controls.Add(gvDiff);
            splitContainer1.Panel1.Controls.Add(chkObjectType);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(label7);
            splitContainer1.Size = new System.Drawing.Size(1144, 915);
            splitContainer1.SplitterDistance = 465;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 11;
            // 
            // pnlDiffType
            // 
            pnlDiffType.Controls.Add(chkDiffType);
            pnlDiffType.Controls.Add(label8);
            pnlDiffType.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlDiffType.Location = new System.Drawing.Point(207, 404);
            pnlDiffType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pnlDiffType.Name = "pnlDiffType";
            pnlDiffType.Size = new System.Drawing.Size(937, 61);
            pnlDiffType.TabIndex = 16;
            // 
            // chkDiffType
            // 
            chkDiffType.Dock = System.Windows.Forms.DockStyle.Fill;
            chkDiffType.FormattingEnabled = true;
            chkDiffType.Location = new System.Drawing.Point(0, 21);
            chkDiffType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkDiffType.MultiColumn = true;
            chkDiffType.Name = "chkDiffType";
            chkDiffType.Size = new System.Drawing.Size(937, 40);
            chkDiffType.TabIndex = 17;
            chkDiffType.SelectedValueChanged += ChkDiffType_SelectedValueChanged;
            // 
            // label8
            // 
            label8.Dock = System.Windows.Forms.DockStyle.Top;
            label8.Location = new System.Drawing.Point(0, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(937, 21);
            label8.TabIndex = 16;
            label8.Text = "Diff Type:";
            // 
            // chkObjectType
            // 
            chkObjectType.CheckOnClick = true;
            chkObjectType.Dock = System.Windows.Forms.DockStyle.Left;
            chkObjectType.FormattingEnabled = true;
            chkObjectType.Location = new System.Drawing.Point(0, 0);
            chkObjectType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkObjectType.Name = "chkObjectType";
            chkObjectType.Size = new System.Drawing.Size(207, 465);
            chkObjectType.TabIndex = 9;
            chkObjectType.SelectedValueChanged += ChkObjectType_SelectedValueChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(401, 209);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(351, 20);
            label7.TabIndex = 0;
            label7.Text = "Diff (Loaded programatically due to designer issue)";
            label7.Visible = false;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "ObjectName";
            dataGridViewTextBoxColumn1.HeaderText = "Object Name";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "SchemaName";
            dataGridViewTextBoxColumn2.HeaderText = "Schema";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewTextBoxColumn3.DataPropertyName = "TypeDescription";
            dataGridViewTextBoxColumn3.HeaderText = "Object Type";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "DiffType";
            dataGridViewTextBoxColumn4.HeaderText = "Diff Type";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // DBDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1144, 1101);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "DBDiff";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "DBDiff";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            Load += DBDiff_Load;
            ((System.ComponentModel.ISupportInitialize)gvDiff).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            pnlDiffType.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ComboBox cboInstanceA;
        private System.Windows.Forms.ComboBox cboDatabaseA;
        private System.Windows.Forms.ComboBox cboInstanceB;
        private System.Windows.Forms.ComboBox cboDatabaseB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button bttnCompare;
        private System.Windows.Forms.DataGridView gvDiff;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchemaName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ObjectType;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiffType;
        private System.Windows.Forms.CheckedListBox chkObjectType;
        private System.Windows.Forms.ComboBox cboDate_A;
        private System.Windows.Forms.ComboBox cboDate_B;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel pnlDiffType;
        private System.Windows.Forms.CheckedListBox chkDiffType;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button bttnSwitch;
        private System.Windows.Forms.Button bttnCopyB;
        private System.Windows.Forms.Button bttnCopyA;
        private System.Windows.Forms.CheckBox chkIgnoreWhiteSpace;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
    }
}