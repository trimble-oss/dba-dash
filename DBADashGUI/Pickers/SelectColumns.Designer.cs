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
            pnlOKCancel = new System.Windows.Forms.Panel();
            pnlTop = new System.Windows.Forms.Panel();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)dgvCols).BeginInit();
            pnlOKCancel.SuspendLayout();
            pnlTop.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvCols
            // 
            dgvCols.AllowUserToAddRows = false;
            dgvCols.AllowUserToDeleteRows = false;
            dgvCols.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            dgvCols.BackgroundColor = System.Drawing.Color.White;
            dgvCols.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCols.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colIsVisible, colColumn });
            dgvCols.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvCols.Location = new System.Drawing.Point(23, 3);
            dgvCols.Name = "dgvCols";
            dgvCols.RowHeadersVisible = false;
            dgvCols.RowHeadersWidth = 51;
            dgvCols.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvCols.Size = new System.Drawing.Size(374, 452);
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
            colIsVisible.Width = 23;
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
            bttnOK.Location = new System.Drawing.Point(303, 13);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 1;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.ForeColor = System.Drawing.Color.Black;
            bttnCancel.Location = new System.Drawing.Point(203, 13);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 2;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // lnkAll
            // 
            lnkAll.AutoSize = true;
            lnkAll.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkAll.Location = new System.Drawing.Point(23, 18);
            lnkAll.Name = "lnkAll";
            lnkAll.Size = new System.Drawing.Size(27, 20);
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
            lnkNone.Location = new System.Drawing.Point(56, 18);
            lnkNone.Name = "lnkNone";
            lnkNone.Size = new System.Drawing.Size(45, 20);
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
            lnkSelected.Location = new System.Drawing.Point(281, 18);
            lnkSelected.Name = "lnkSelected";
            lnkSelected.Size = new System.Drawing.Size(116, 20);
            lnkSelected.TabIndex = 5;
            lnkSelected.TabStop = true;
            lnkSelected.Text = "Toggle Selected";
            toolTip1.SetToolTip(lnkSelected, "Check/Uncheck all the selected items.  Use spacebar to activate from the keyboard.");
            lnkSelected.LinkClicked += LnkSelected_LinkClicked;
            // 
            // pnlOKCancel
            // 
            pnlOKCancel.Controls.Add(bttnCancel);
            pnlOKCancel.Controls.Add(bttnOK);
            pnlOKCancel.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlOKCancel.Location = new System.Drawing.Point(0, 499);
            pnlOKCancel.Name = "pnlOKCancel";
            pnlOKCancel.Size = new System.Drawing.Size(420, 66);
            pnlOKCancel.TabIndex = 6;
            // 
            // pnlTop
            // 
            pnlTop.Controls.Add(lnkAll);
            pnlTop.Controls.Add(lnkNone);
            pnlTop.Controls.Add(lnkSelected);
            pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            pnlTop.Location = new System.Drawing.Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.Size = new System.Drawing.Size(420, 41);
            pnlTop.TabIndex = 7;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(dgvCols, 1, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 41);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new System.Drawing.Size(420, 458);
            tableLayoutPanel1.TabIndex = 8;
            // 
            // SelectColumns
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(420, 565);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(pnlTop);
            Controls.Add(pnlOKCancel);
            Name = "SelectColumns";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Columns";
            Load += SelectColumns_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCols).EndInit();
            pnlOKCancel.ResumeLayout(false);
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
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
        private System.Windows.Forms.Panel pnlOKCancel;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}