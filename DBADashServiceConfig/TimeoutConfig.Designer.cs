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
            lnkPreview = new System.Windows.Forms.LinkLabel();
            chkDefaultTimeout = new System.Windows.Forms.CheckBox();
            txtDefaultTimeout = new System.Windows.Forms.TextBox();
            bttnCancel = new System.Windows.Forms.Button();
            bttnSave = new System.Windows.Forms.Button();
            panel2 = new System.Windows.Forms.Panel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colCollectionType, colTimeout, colDelete });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 65);
            dgv.Name = "dgv";
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(802, 382);
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
            panel1.Controls.Add(lnkPreview);
            panel1.Controls.Add(chkDefaultTimeout);
            panel1.Controls.Add(txtDefaultTimeout);
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnSave);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 447);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(802, 76);
            panel1.TabIndex = 6;
            // 
            // lnkPreview
            // 
            lnkPreview.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkPreview.AutoSize = true;
            lnkPreview.Location = new System.Drawing.Point(391, 25);
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
            chkDefaultTimeout.Location = new System.Drawing.Point(12, 25);
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
            txtDefaultTimeout.Location = new System.Drawing.Point(214, 22);
            txtDefaultTimeout.Name = "txtDefaultTimeout";
            txtDefaultTimeout.Size = new System.Drawing.Size(125, 27);
            txtDefaultTimeout.TabIndex = 2;
            txtDefaultTimeout.KeyPress += AllowOnlyDigits_KeyPress;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(596, 22);
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
            bttnSave.Location = new System.Drawing.Point(696, 22);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 0;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += Save_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(label1);
            panel2.Controls.Add(txtTimeout);
            panel2.Controls.Add(bttnAdd);
            panel2.Controls.Add(cboCollection);
            panel2.Controls.Add(label2);
            panel2.Dock = System.Windows.Forms.DockStyle.Top;
            panel2.Location = new System.Drawing.Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(802, 65);
            panel2.TabIndex = 7;
            // 
            // TimeoutConfig
            // 
            AcceptButton = bttnSave;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(802, 523);
            Controls.Add(dgv);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(820, 570);
            Name = "TimeoutConfig";
            Text = "Custom Timeouts";
            Load += TimeoutConfig_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
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
    }
}