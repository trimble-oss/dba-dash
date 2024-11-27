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
            components = new System.ComponentModel.Container();
            dgvCols = new System.Windows.Forms.DataGridView();
            colIsVisible = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            lnkAll = new System.Windows.Forms.LinkLabel();
            lnkNone = new System.Windows.Forms.LinkLabel();
            lnkSelected = new System.Windows.Forms.LinkLabel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgvCols).BeginInit();
            SuspendLayout();
            // 
            // dgvCols
            // 
            dgvCols.AllowUserToAddRows = false;
            dgvCols.AllowUserToDeleteRows = false;
            dgvCols.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvCols.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            dgvCols.BackgroundColor = System.Drawing.Color.White;
            dgvCols.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCols.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colIsVisible, colColumn });
            dgvCols.Location = new System.Drawing.Point(26, 68);
            dgvCols.Margin = new System.Windows.Forms.Padding(6);
            dgvCols.Name = "dgvCols";
            dgvCols.RowHeadersVisible = false;
            dgvCols.RowHeadersWidth = 51;
            dgvCols.RowTemplate.Height = 29;
            dgvCols.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvCols.Size = new System.Drawing.Size(771, 904);
            dgvCols.TabIndex = 0;
            dgvCols.KeyPress += DgvCols_KeyPress;
            // 
            // colIsVisible
            // 
            colIsVisible.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            colIsVisible.DataPropertyName = "IsVisible";
            colIsVisible.HeaderText = "";
            colIsVisible.MinimumWidth = 6;
            colIsVisible.Name = "colIsVisible";
            colIsVisible.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colIsVisible.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colIsVisible.Width = 45;
            // 
            // colColumn
            // 
            colColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colColumn.DataPropertyName = "Name";
            colColumn.HeaderText = "Item";
            colColumn.MinimumWidth = 6;
            colColumn.Name = "colColumn";
            colColumn.ReadOnly = true;
            colColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.ForeColor = System.Drawing.Color.Black;
            bttnOK.Location = new System.Drawing.Point(597, 1029);
            bttnOK.Margin = new System.Windows.Forms.Padding(6);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(200, 59);
            bttnOK.TabIndex = 1;
            bttnOK.Text = "OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.ForeColor = System.Drawing.Color.Black;
            bttnCancel.Location = new System.Drawing.Point(363, 1029);
            bttnCancel.Margin = new System.Windows.Forms.Padding(6);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(200, 59);
            bttnCancel.TabIndex = 2;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // lnkAll
            // 
            lnkAll.AutoSize = true;
            lnkAll.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkAll.Location = new System.Drawing.Point(26, 18);
            lnkAll.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            lnkAll.Name = "lnkAll";
            lnkAll.Size = new System.Drawing.Size(51, 41);
            lnkAll.TabIndex = 3;
            lnkAll.TabStop = true;
            lnkAll.Text = "All";
            toolTip1.SetToolTip(lnkAll, "Check all items");
            lnkAll.LinkClicked += LnkAll_LinkClicked;
            // 
            // lnkNone
            // 
            lnkNone.AutoSize = true;
            lnkNone.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkNone.Location = new System.Drawing.Point(121, 20);
            lnkNone.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            lnkNone.Name = "lnkNone";
            lnkNone.Size = new System.Drawing.Size(91, 41);
            lnkNone.TabIndex = 4;
            lnkNone.TabStop = true;
            lnkNone.Text = "None";
            toolTip1.SetToolTip(lnkNone, "Remove all checks");
            lnkNone.LinkClicked += LnkNone_LinkClicked;
            // 
            // lnkSelected
            // 
            lnkSelected.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkSelected.AutoSize = true;
            lnkSelected.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkSelected.Location = new System.Drawing.Point(550, 18);
            lnkSelected.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            lnkSelected.Name = "lnkSelected";
            lnkSelected.Size = new System.Drawing.Size(229, 41);
            lnkSelected.TabIndex = 5;
            lnkSelected.TabStop = true;
            lnkSelected.Text = "Toggle Selected";
            toolTip1.SetToolTip(lnkSelected, "Check/Uncheck all the selected items.  Use spacebar to activate from the keyboard.");
            lnkSelected.LinkClicked += LnkSelected_LinkClicked;
            // 
            // SelectColumns
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(17F, 41F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(822, 1132);
            Controls.Add(lnkSelected);
            Controls.Add(lnkNone);
            Controls.Add(lnkAll);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(dgvCols);
            Margin = new System.Windows.Forms.Padding(6);
            Name = "SelectColumns";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Columns";
            Load += SelectColumns_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCols).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvCols;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.LinkLabel lnkAll;
        private System.Windows.Forms.LinkLabel lnkNone;
        private System.Windows.Forms.LinkLabel lnkSelected;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colIsVisible;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColumn;
    }
}