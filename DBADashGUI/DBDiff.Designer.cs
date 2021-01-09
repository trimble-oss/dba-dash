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
            this.cboInstanceA = new System.Windows.Forms.ComboBox();
            this.cboDatabaseA = new System.Windows.Forms.ComboBox();
            this.cboInstanceB = new System.Windows.Forms.ComboBox();
            this.cboDatabaseB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.bttnCompare = new System.Windows.Forms.Button();
            this.gvDiff = new System.Windows.Forms.DataGridView();
            this.ObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SchemaName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ObjectType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiffType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.bttnCopyA = new System.Windows.Forms.Button();
            this.bttnCopyB = new System.Windows.Forms.Button();
            this.bttnSwitch = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cboDate_B = new System.Windows.Forms.ComboBox();
            this.cboDate_A = new System.Windows.Forms.ComboBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.pnlDiffType = new System.Windows.Forms.Panel();
            this.chkDiffType = new System.Windows.Forms.CheckedListBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkObjectType = new System.Windows.Forms.CheckedListBox();
            this.label7 = new System.Windows.Forms.Label();
            this.chkIgnoreWhiteSpace = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.gvDiff)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.pnlDiffType.SuspendLayout();
            this.SuspendLayout();
            // 
            // cboInstanceA
            // 
            this.cboInstanceA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstanceA.FormattingEnabled = true;
            this.cboInstanceA.Location = new System.Drawing.Point(146, 12);
            this.cboInstanceA.Name = "cboInstanceA";
            this.cboInstanceA.Size = new System.Drawing.Size(181, 24);
            this.cboInstanceA.TabIndex = 0;
            this.cboInstanceA.SelectedIndexChanged += new System.EventHandler(this.cboInstanceA_SelectedIndexChanged);
            // 
            // cboDatabaseA
            // 
            this.cboDatabaseA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDatabaseA.FormattingEnabled = true;
            this.cboDatabaseA.Location = new System.Drawing.Point(146, 42);
            this.cboDatabaseA.Name = "cboDatabaseA";
            this.cboDatabaseA.Size = new System.Drawing.Size(181, 24);
            this.cboDatabaseA.TabIndex = 1;
            this.cboDatabaseA.SelectedIndexChanged += new System.EventHandler(this.cboDatabaseA_SelectedIndexChanged);
            // 
            // cboInstanceB
            // 
            this.cboInstanceB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstanceB.FormattingEnabled = true;
            this.cboInstanceB.Location = new System.Drawing.Point(612, 13);
            this.cboInstanceB.Name = "cboInstanceB";
            this.cboInstanceB.Size = new System.Drawing.Size(181, 24);
            this.cboInstanceB.TabIndex = 2;
            this.cboInstanceB.SelectedIndexChanged += new System.EventHandler(this.cboInstanceB_SelectedIndexChanged);
            // 
            // cboDatabaseB
            // 
            this.cboDatabaseB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDatabaseB.FormattingEnabled = true;
            this.cboDatabaseB.Location = new System.Drawing.Point(612, 43);
            this.cboDatabaseB.Name = "cboDatabaseB";
            this.cboDatabaseB.Size = new System.Drawing.Size(181, 24);
            this.cboDatabaseB.TabIndex = 3;
            this.cboDatabaseB.SelectedIndexChanged += new System.EventHandler(this.cboDatabaseB_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Instance A:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(477, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Instance B:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Database A:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(477, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 17);
            this.label4.TabIndex = 7;
            this.label4.Text = "Database B:";
            // 
            // bttnCompare
            // 
            this.bttnCompare.Location = new System.Drawing.Point(870, 74);
            this.bttnCompare.Name = "bttnCompare";
            this.bttnCompare.Size = new System.Drawing.Size(75, 23);
            this.bttnCompare.TabIndex = 8;
            this.bttnCompare.Text = "Compare";
            this.bttnCompare.UseVisualStyleBackColor = true;
            this.bttnCompare.Click += new System.EventHandler(this.bttnCompare_Click);
            // 
            // gvDiff
            // 
            this.gvDiff.AllowUserToAddRows = false;
            this.gvDiff.AllowUserToDeleteRows = false;
            this.gvDiff.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gvDiff.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ObjectName,
            this.SchemaName,
            this.ObjectType,
            this.DiffType});
            this.gvDiff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gvDiff.Location = new System.Drawing.Point(207, 0);
            this.gvDiff.Name = "gvDiff";
            this.gvDiff.ReadOnly = true;
            this.gvDiff.RowHeadersWidth = 51;
            this.gvDiff.RowTemplate.Height = 24;
            this.gvDiff.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gvDiff.Size = new System.Drawing.Size(937, 372);
            this.gvDiff.TabIndex = 9;
            this.gvDiff.SelectionChanged += new System.EventHandler(this.gvDiff_SelectionChanged);
            // 
            // ObjectName
            // 
            this.ObjectName.DataPropertyName = "ObjectName";
            this.ObjectName.HeaderText = "Object Name";
            this.ObjectName.MinimumWidth = 6;
            this.ObjectName.Name = "ObjectName";
            this.ObjectName.ReadOnly = true;
            this.ObjectName.Width = 125;
            // 
            // SchemaName
            // 
            this.SchemaName.DataPropertyName = "SchemaName";
            this.SchemaName.HeaderText = "Schema";
            this.SchemaName.MinimumWidth = 6;
            this.SchemaName.Name = "SchemaName";
            this.SchemaName.ReadOnly = true;
            this.SchemaName.Width = 125;
            // 
            // ObjectType
            // 
            this.ObjectType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.ObjectType.DataPropertyName = "TypeDescription";
            this.ObjectType.HeaderText = "Object Type";
            this.ObjectType.MinimumWidth = 6;
            this.ObjectType.Name = "ObjectType";
            this.ObjectType.ReadOnly = true;
            this.ObjectType.Width = 114;
            // 
            // DiffType
            // 
            this.DiffType.DataPropertyName = "DiffType";
            this.DiffType.HeaderText = "Diff Type";
            this.DiffType.MinimumWidth = 6;
            this.DiffType.Name = "DiffType";
            this.DiffType.ReadOnly = true;
            this.DiffType.Width = 125;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.chkIgnoreWhiteSpace);
            this.panel1.Controls.Add(this.bttnCopyA);
            this.panel1.Controls.Add(this.bttnCopyB);
            this.panel1.Controls.Add(this.bttnSwitch);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.cboDate_B);
            this.panel1.Controls.Add(this.cboDate_A);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cboInstanceA);
            this.panel1.Controls.Add(this.bttnCompare);
            this.panel1.Controls.Add(this.cboDatabaseA);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cboInstanceB);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cboDatabaseB);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1144, 149);
            this.panel1.TabIndex = 10;
            // 
            // bttnCopyA
            // 
            this.bttnCopyA.Image = global::DBADashGUI.Properties.Resources.Next_grey_16x;
            this.bttnCopyA.Location = new System.Drawing.Point(252, 103);
            this.bttnCopyA.Name = "bttnCopyA";
            this.bttnCopyA.Size = new System.Drawing.Size(75, 23);
            this.bttnCopyA.TabIndex = 16;
            this.bttnCopyA.UseVisualStyleBackColor = true;
            this.bttnCopyA.Click += new System.EventHandler(this.bttnCopyA_Click);
            // 
            // bttnCopyB
            // 
            this.bttnCopyB.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.bttnCopyB.Location = new System.Drawing.Point(612, 103);
            this.bttnCopyB.Name = "bttnCopyB";
            this.bttnCopyB.Size = new System.Drawing.Size(75, 23);
            this.bttnCopyB.TabIndex = 15;
            this.bttnCopyB.UseVisualStyleBackColor = true;
            this.bttnCopyB.Click += new System.EventHandler(this.bttnCopyB_Click);
            // 
            // bttnSwitch
            // 
            this.bttnSwitch.Image = global::DBADashGUI.Properties.Resources.SwitchSourceOrTarget_16x;
            this.bttnSwitch.Location = new System.Drawing.Point(361, 37);
            this.bttnSwitch.Name = "bttnSwitch";
            this.bttnSwitch.Size = new System.Drawing.Size(75, 23);
            this.bttnSwitch.TabIndex = 14;
            this.bttnSwitch.UseVisualStyleBackColor = true;
            this.bttnSwitch.Click += new System.EventHandler(this.bttnSwitch_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(477, 78);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(124, 17);
            this.label6.TabIndex = 13;
            this.label6.Text = "Snapshot Version:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 73);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Snapshot Version:";
            // 
            // cboDate_B
            // 
            this.cboDate_B.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDate_B.FormattingEnabled = true;
            this.cboDate_B.Location = new System.Drawing.Point(612, 73);
            this.cboDate_B.Name = "cboDate_B";
            this.cboDate_B.Size = new System.Drawing.Size(181, 24);
            this.cboDate_B.TabIndex = 11;
            // 
            // cboDate_A
            // 
            this.cboDate_A.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDate_A.FormattingEnabled = true;
            this.cboDate_A.Location = new System.Drawing.Point(146, 72);
            this.cboDate_A.Name = "cboDate_A";
            this.cboDate_A.Size = new System.Drawing.Size(181, 24);
            this.cboDate_A.TabIndex = 10;
            this.cboDate_A.SelectedIndexChanged += new System.EventHandler(this.cboDate_A_SelectedIndexChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 149);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.pnlDiffType);
            this.splitContainer1.Panel1.Controls.Add(this.gvDiff);
            this.splitContainer1.Panel1.Controls.Add(this.chkObjectType);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label7);
            this.splitContainer1.Size = new System.Drawing.Size(1144, 732);
            this.splitContainer1.SplitterDistance = 372;
            this.splitContainer1.TabIndex = 11;
            // 
            // pnlDiffType
            // 
            this.pnlDiffType.Controls.Add(this.chkDiffType);
            this.pnlDiffType.Controls.Add(this.label8);
            this.pnlDiffType.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDiffType.Location = new System.Drawing.Point(207, 323);
            this.pnlDiffType.Name = "pnlDiffType";
            this.pnlDiffType.Size = new System.Drawing.Size(937, 49);
            this.pnlDiffType.TabIndex = 16;
            // 
            // chkDiffType
            // 
            this.chkDiffType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkDiffType.FormattingEnabled = true;
            this.chkDiffType.Location = new System.Drawing.Point(0, 17);
            this.chkDiffType.MultiColumn = true;
            this.chkDiffType.Name = "chkDiffType";
            this.chkDiffType.Size = new System.Drawing.Size(937, 32);
            this.chkDiffType.TabIndex = 17;
            this.chkDiffType.SelectedIndexChanged += new System.EventHandler(this.chkDiffType_SelectedIndexChanged_1);
            this.chkDiffType.SelectedValueChanged += new System.EventHandler(this.chkDiffType_SelectedValueChanged);
            // 
            // label8
            // 
            this.label8.Dock = System.Windows.Forms.DockStyle.Top;
            this.label8.Location = new System.Drawing.Point(0, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(937, 17);
            this.label8.TabIndex = 16;
            this.label8.Text = "Diff Type:";
            // 
            // chkObjectType
            // 
            this.chkObjectType.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkObjectType.FormattingEnabled = true;
            this.chkObjectType.Location = new System.Drawing.Point(0, 0);
            this.chkObjectType.Name = "chkObjectType";
            this.chkObjectType.Size = new System.Drawing.Size(207, 372);
            this.chkObjectType.TabIndex = 9;
            this.chkObjectType.SelectedValueChanged += new System.EventHandler(this.chkObjectType_SelectedValueChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(401, 167);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(331, 17);
            this.label7.TabIndex = 0;
            this.label7.Text = "Diff (Loaded programatically due to designer issue)";
            this.label7.Visible = false;
            // 
            // chkIgnoreWhiteSpace
            // 
            this.chkIgnoreWhiteSpace.AutoSize = true;
            this.chkIgnoreWhiteSpace.Location = new System.Drawing.Point(870, 13);
            this.chkIgnoreWhiteSpace.Name = "chkIgnoreWhiteSpace";
            this.chkIgnoreWhiteSpace.Size = new System.Drawing.Size(148, 21);
            this.chkIgnoreWhiteSpace.TabIndex = 17;
            this.chkIgnoreWhiteSpace.Text = "Ignore Whitespace";
            this.chkIgnoreWhiteSpace.UseVisualStyleBackColor = true;
            this.chkIgnoreWhiteSpace.CheckedChanged += new System.EventHandler(this.chkIgnoreWhiteSpace_CheckedChanged);
            // 
            // DBDiff
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1144, 881);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DBDiff";
            this.Text = "DBDiff";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.DBDiff_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gvDiff)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.pnlDiffType.ResumeLayout(false);
            this.ResumeLayout(false);

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
    }
}