namespace DBADashServiceConfig
{
    partial class TimeoutConfig
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeoutConfig));
            dgv = new System.Windows.Forms.DataGridView();
            colCollectionType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colTimeout = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDelete = new System.Windows.Forms.DataGridViewLinkColumn();
            txtTimeout = new System.Windows.Forms.TextBox();
            cboCollection = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            bttnAdd = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            bttnCancel = new System.Windows.Forms.Button();
            bttnSave = new System.Windows.Forms.Button();
            lnkPreview = new System.Windows.Forms.LinkLabel();
            chkDefaultTimeout = new System.Windows.Forms.CheckBox();
            txtDefaultTimeout = new System.Windows.Forms.TextBox();
            panel2 = new System.Windows.Forms.Panel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            chkAddPartitionsTimeout = new System.Windows.Forms.CheckBox();
            chkPurgeTimeout = new System.Windows.Forms.CheckBox();
            chkImportTimeout = new System.Windows.Forms.CheckBox();
            themedTabControl1 = new DBADashGUI.Theme.ThemedTabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            panel3 = new System.Windows.Forms.Panel();
            tabPage2 = new System.Windows.Forms.TabPage();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            label3 = new System.Windows.Forms.Label();
            txtAddPartitionsCommandTimeout = new System.Windows.Forms.TextBox();
            txtPurgeDataCommandTimeout = new System.Windows.Forms.TextBox();
            txtImportCommandTimeout = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            themedTabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            panel3.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colCollectionType, colTimeout, colDelete });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(3, 68);
            dgv.Name = "dgv";
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(788, 301);
            dgv.TabIndex = 0;
            dgv.CellContentClick += CustomTimeouts_CellContentClick;
            // 
            // colCollectionType
            // 
            colCollectionType.DataPropertyName = "CollectionType";
            colCollectionType.HeaderText = "Collection Type";
            colCollectionType.MinimumWidth = 6;
            colCollectionType.Name = "colCollectionType";
            colCollectionType.ReadOnly = true;
            colCollectionType.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colCollectionType.Width = 125;
            // 
            // colTimeout
            // 
            colTimeout.DataPropertyName = "Timeout";
            colTimeout.HeaderText = "Timeout";
            colTimeout.MinimumWidth = 6;
            colTimeout.Name = "colTimeout";
            colTimeout.Width = 125;
            // 
            // colDelete
            // 
            colDelete.HeaderText = "";
            colDelete.MinimumWidth = 6;
            colDelete.Name = "colDelete";
            colDelete.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colDelete.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colDelete.Text = "Delete";
            colDelete.UseColumnTextForLinkValue = true;
            colDelete.Width = 125;
            // 
            // txtTimeout
            // 
            txtTimeout.Location = new System.Drawing.Point(458, 16);
            txtTimeout.Name = "txtTimeout";
            txtTimeout.Size = new System.Drawing.Size(125, 27);
            txtTimeout.TabIndex = 1;
            txtTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // cboCollection
            // 
            cboCollection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboCollection.FormattingEnabled = true;
            cboCollection.Location = new System.Drawing.Point(97, 15);
            cboCollection.Name = "cboCollection";
            cboCollection.Size = new System.Drawing.Size(215, 28);
            cboCollection.TabIndex = 2;
            cboCollection.SelectedIndexChanged += Collection_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 20);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(79, 20);
            label1.TabIndex = 3;
            label1.Text = "Collection:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(318, 20);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(134, 20);
            label2.TabIndex = 4;
            label2.Text = "Timeout (seconds):";
            // 
            // bttnAdd
            // 
            bttnAdd.Location = new System.Drawing.Point(589, 16);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(94, 29);
            bttnAdd.TabIndex = 5;
            bttnAdd.Text = "&Add";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += Add_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnSave);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 470);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(802, 53);
            panel1.TabIndex = 6;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(596, 14);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 1;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += Cancel_Click;
            // 
            // bttnSave
            // 
            bttnSave.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnSave.Location = new System.Drawing.Point(696, 14);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 0;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += Save_Click;
            // 
            // lnkPreview
            // 
            lnkPreview.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkPreview.AutoSize = true;
            lnkPreview.Location = new System.Drawing.Point(597, 17);
            lnkPreview.Name = "lnkPreview";
            lnkPreview.Size = new System.Drawing.Size(186, 20);
            lnkPreview.TabIndex = 5;
            lnkPreview.TabStop = true;
            lnkPreview.Text = "Preview Effective Timeouts";
            lnkPreview.LinkClicked += Preview_Click;
            // 
            // chkDefaultTimeout
            // 
            chkDefaultTimeout.AutoSize = true;
            chkDefaultTimeout.Location = new System.Drawing.Point(12, 16);
            chkDefaultTimeout.Name = "chkDefaultTimeout";
            chkDefaultTimeout.Size = new System.Drawing.Size(196, 24);
            chkDefaultTimeout.TabIndex = 4;
            chkDefaultTimeout.Text = "Custom Default Timeout:";
            toolTip1.SetToolTip(chkDefaultTimeout, "Only applied to collections without an explicit timeout");
            chkDefaultTimeout.UseVisualStyleBackColor = true;
            chkDefaultTimeout.CheckedChanged += DefaultTimeout_CheckChanged;
            // 
            // txtDefaultTimeout
            // 
            txtDefaultTimeout.Location = new System.Drawing.Point(214, 13);
            txtDefaultTimeout.Name = "txtDefaultTimeout";
            txtDefaultTimeout.Size = new System.Drawing.Size(125, 27);
            txtDefaultTimeout.TabIndex = 2;
            txtDefaultTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // panel2
            // 
            panel2.Controls.Add(label1);
            panel2.Controls.Add(txtTimeout);
            panel2.Controls.Add(bttnAdd);
            panel2.Controls.Add(cboCollection);
            panel2.Controls.Add(label2);
            panel2.Dock = System.Windows.Forms.DockStyle.Top;
            panel2.Location = new System.Drawing.Point(3, 3);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(788, 65);
            panel2.TabIndex = 7;
            // 
            // chkAddPartitionsTimeout
            // 
            chkAddPartitionsTimeout.AutoSize = true;
            chkAddPartitionsTimeout.Location = new System.Drawing.Point(31, 86);
            chkAddPartitionsTimeout.Name = "chkAddPartitionsTimeout";
            chkAddPartitionsTimeout.Size = new System.Drawing.Size(185, 24);
            chkAddPartitionsTimeout.TabIndex = 8;
            chkAddPartitionsTimeout.Text = "Add partitions timeout:";
            toolTip1.SetToolTip(chkAddPartitionsTimeout, "Timeout for adding new partitions.  Most data in DBA Dash is partitioned which makes it efficient to remove data older than the configured retention");
            chkAddPartitionsTimeout.UseVisualStyleBackColor = true;
            chkAddPartitionsTimeout.CheckedChanged += AddPartitionsTimeout_CheckChanged;
            // 
            // chkPurgeTimeout
            // 
            chkPurgeTimeout.AutoSize = true;
            chkPurgeTimeout.Location = new System.Drawing.Point(31, 54);
            chkPurgeTimeout.Name = "chkPurgeTimeout";
            chkPurgeTimeout.Size = new System.Drawing.Size(128, 24);
            chkPurgeTimeout.TabIndex = 7;
            chkPurgeTimeout.Text = "Purge timeout:";
            toolTip1.SetToolTip(chkPurgeTimeout, "Timeout for running 'dbo.PurgeData' to remove old data based on data retention settings.");
            chkPurgeTimeout.UseVisualStyleBackColor = true;
            chkPurgeTimeout.CheckedChanged += PurgeTimeout_CheckChanged;
            // 
            // chkImportTimeout
            // 
            chkImportTimeout.AutoSize = true;
            chkImportTimeout.Location = new System.Drawing.Point(31, 20);
            chkImportTimeout.Name = "chkImportTimeout";
            chkImportTimeout.Size = new System.Drawing.Size(135, 24);
            chkImportTimeout.TabIndex = 6;
            chkImportTimeout.Text = "Import timeout:";
            toolTip1.SetToolTip(chkImportTimeout, "Timeout for writing data to the repository database");
            chkImportTimeout.UseVisualStyleBackColor = true;
            chkImportTimeout.CheckedChanged += ImportTimeout_CheckChanged;
            // 
            // themedTabControl1
            // 
            themedTabControl1.Controls.Add(tabPage1);
            themedTabControl1.Controls.Add(tabPage2);
            themedTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            themedTabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            themedTabControl1.Location = new System.Drawing.Point(0, 0);
            themedTabControl1.Name = "themedTabControl1";
            themedTabControl1.Padding = new System.Drawing.Point(20, 8);
            themedTabControl1.SelectedIndex = 0;
            themedTabControl1.Size = new System.Drawing.Size(802, 470);
            themedTabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(dgv);
            tabPage1.Controls.Add(panel3);
            tabPage1.Controls.Add(panel2);
            tabPage1.Location = new System.Drawing.Point(4, 39);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(3);
            tabPage1.Size = new System.Drawing.Size(794, 427);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Collection Timeouts";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.Controls.Add(lnkPreview);
            panel3.Controls.Add(chkDefaultTimeout);
            panel3.Controls.Add(txtDefaultTimeout);
            panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel3.Location = new System.Drawing.Point(3, 369);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(788, 55);
            panel3.TabIndex = 8;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(pictureBox1);
            tabPage2.Controls.Add(label3);
            tabPage2.Controls.Add(chkAddPartitionsTimeout);
            tabPage2.Controls.Add(chkPurgeTimeout);
            tabPage2.Controls.Add(chkImportTimeout);
            tabPage2.Controls.Add(txtAddPartitionsCommandTimeout);
            tabPage2.Controls.Add(txtPurgeDataCommandTimeout);
            tabPage2.Controls.Add(txtImportCommandTimeout);
            tabPage2.Location = new System.Drawing.Point(4, 39);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(3);
            tabPage2.Size = new System.Drawing.Size(794, 427);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Other Timeouts";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            pictureBox1.Image = Properties.Resources.Information_blue_6227_16x16_cyan;
            pictureBox1.Location = new System.Drawing.Point(31, 381);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(20, 20);
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            // 
            // label3
            // 
            label3.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            label3.Location = new System.Drawing.Point(57, 381);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(672, 20);
            label3.TabIndex = 9;
            label3.Text = "Additional timeouts relating to the GUI are available in the Options\\Repository Settings menu of the GUI";
            // 
            // txtAddPartitionsCommandTimeout
            // 
            txtAddPartitionsCommandTimeout.Enabled = false;
            txtAddPartitionsCommandTimeout.Location = new System.Drawing.Point(251, 84);
            txtAddPartitionsCommandTimeout.Name = "txtAddPartitionsCommandTimeout";
            txtAddPartitionsCommandTimeout.Size = new System.Drawing.Size(125, 27);
            txtAddPartitionsCommandTimeout.TabIndex = 2;
            txtAddPartitionsCommandTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // txtPurgeDataCommandTimeout
            // 
            txtPurgeDataCommandTimeout.Enabled = false;
            txtPurgeDataCommandTimeout.Location = new System.Drawing.Point(251, 51);
            txtPurgeDataCommandTimeout.Name = "txtPurgeDataCommandTimeout";
            txtPurgeDataCommandTimeout.Size = new System.Drawing.Size(125, 27);
            txtPurgeDataCommandTimeout.TabIndex = 1;
            txtPurgeDataCommandTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // txtImportCommandTimeout
            // 
            txtImportCommandTimeout.Enabled = false;
            txtImportCommandTimeout.Location = new System.Drawing.Point(251, 18);
            txtImportCommandTimeout.Name = "txtImportCommandTimeout";
            txtImportCommandTimeout.Size = new System.Drawing.Size(125, 27);
            txtImportCommandTimeout.TabIndex = 0;
            txtImportCommandTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // TimeoutConfig
            // 
            AcceptButton = bttnSave;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(802, 523);
            Controls.Add(themedTabControl1);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(820, 570);
            Name = "TimeoutConfig";
            Text = "Custom Timeouts";
            Load += TimeoutConfig_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            themedTabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.TextBox txtTimeout;
        private System.Windows.Forms.ComboBox cboCollection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox chkDefaultTimeout;
        private System.Windows.Forms.TextBox txtDefaultTimeout;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.LinkLabel lnkPreview;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCollectionType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTimeout;
        private System.Windows.Forms.DataGridViewLinkColumn colDelete;
        private DBADashGUI.Theme.ThemedTabControl themedTabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.CheckBox chkImportTimeout;
        private System.Windows.Forms.TextBox txtAddPartitionsCommandTimeout;
        private System.Windows.Forms.TextBox txtPurgeDataCommandTimeout;
        private System.Windows.Forms.TextBox txtImportCommandTimeout;
        private System.Windows.Forms.CheckBox chkAddPartitionsTimeout;
        private System.Windows.Forms.CheckBox chkPurgeTimeout;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label3;
    }
}