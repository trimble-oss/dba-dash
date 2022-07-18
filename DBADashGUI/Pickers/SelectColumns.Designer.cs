namespace DBADashGUI
{
    partial class SelectColumns
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
            this.components = new System.ComponentModel.Container();
            this.dgvCols = new System.Windows.Forms.DataGridView();
            this.colIsVisible = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bttnOK = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.lnkAll = new System.Windows.Forms.LinkLabel();
            this.lnkNone = new System.Windows.Forms.LinkLabel();
            this.lnkSelected = new System.Windows.Forms.LinkLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCols)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvCols
            // 
            this.dgvCols.AllowUserToAddRows = false;
            this.dgvCols.AllowUserToDeleteRows = false;
            this.dgvCols.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCols.BackgroundColor = System.Drawing.Color.White;
            this.dgvCols.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCols.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIsVisible,
            this.colColumn});
            this.dgvCols.Location = new System.Drawing.Point(12, 33);
            this.dgvCols.Name = "dgvCols";
            this.dgvCols.RowHeadersVisible = false;
            this.dgvCols.RowHeadersWidth = 51;
            this.dgvCols.RowTemplate.Height = 29;
            this.dgvCols.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCols.Size = new System.Drawing.Size(363, 441);
            this.dgvCols.TabIndex = 0;
            this.dgvCols.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.dgvCols_KeyPress);
            // 
            // colIsVisible
            // 
            this.colIsVisible.DataPropertyName = "IsVisible";
            this.colIsVisible.HeaderText = "";
            this.colIsVisible.MinimumWidth = 6;
            this.colIsVisible.Name = "colIsVisible";
            this.colIsVisible.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colIsVisible.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colIsVisible.Width = 50;
            // 
            // colColumn
            // 
            this.colColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colColumn.DataPropertyName = "ColumnName";
            this.colColumn.HeaderText = "Column";
            this.colColumn.MinimumWidth = 6;
            this.colColumn.Name = "colColumn";
            this.colColumn.ReadOnly = true;
            this.colColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // bttnOK
            // 
            this.bttnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnOK.Location = new System.Drawing.Point(281, 502);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(94, 29);
            this.bttnOK.TabIndex = 1;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.Location = new System.Drawing.Point(171, 502);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 2;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.bttnCancel_Click);
            // 
            // lnkAll
            // 
            this.lnkAll.AutoSize = true;
            this.lnkAll.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkAll.Location = new System.Drawing.Point(12, 9);
            this.lnkAll.Name = "lnkAll";
            this.lnkAll.Size = new System.Drawing.Size(27, 20);
            this.lnkAll.TabIndex = 3;
            this.lnkAll.TabStop = true;
            this.lnkAll.Text = "All";
            this.toolTip1.SetToolTip(this.lnkAll, "Check all items");
            this.lnkAll.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkAll_LinkClicked);
            // 
            // lnkNone
            // 
            this.lnkNone.AutoSize = true;
            this.lnkNone.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkNone.Location = new System.Drawing.Point(57, 10);
            this.lnkNone.Name = "lnkNone";
            this.lnkNone.Size = new System.Drawing.Size(45, 20);
            this.lnkNone.TabIndex = 4;
            this.lnkNone.TabStop = true;
            this.lnkNone.Text = "None";
            this.toolTip1.SetToolTip(this.lnkNone, "Remove all checks");
            this.lnkNone.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkNone_LinkClicked);
            // 
            // lnkSelected
            // 
            this.lnkSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkSelected.AutoSize = true;
            this.lnkSelected.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkSelected.Location = new System.Drawing.Point(259, 9);
            this.lnkSelected.Name = "lnkSelected";
            this.lnkSelected.Size = new System.Drawing.Size(116, 20);
            this.lnkSelected.TabIndex = 5;
            this.lnkSelected.TabStop = true;
            this.lnkSelected.Text = "Toggle Selected";
            this.toolTip1.SetToolTip(this.lnkSelected, "Check/Uncheck all the selected items.  Use spacebar to activate from the keyboard" +
        ".");
            this.lnkSelected.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSelected_LinkClicked);
            // 
            // SelectColumns
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(387, 552);
            this.Controls.Add(this.lnkSelected);
            this.Controls.Add(this.lnkNone);
            this.Controls.Add(this.lnkAll);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.dgvCols);
            this.Name = "SelectColumns";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Columns";
            this.Load += new System.EventHandler(this.SelectColumns_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCols)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvCols;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.LinkLabel lnkAll;
        private System.Windows.Forms.LinkLabel lnkNone;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colIsVisible;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColumn;
        private System.Windows.Forms.LinkLabel lnkSelected;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}