namespace DBADashGUI.Performance
{
    partial class SelectPerformanceCounters
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
            dgvCounters = new System.Windows.Forms.DataGridView();
            colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAvg = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colMin = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colMax = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colTotal = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colCurrent = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colSampleCount = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            panel1 = new System.Windows.Forms.Panel();
            bttnCancel = new System.Windows.Forms.Button();
            bttnClear = new System.Windows.Forms.Button();
            lblSearch = new System.Windows.Forms.Label();
            txtSearch = new System.Windows.Forms.TextBox();
            bttnOK = new System.Windows.Forms.Button();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvCounters).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvCounters
            // 
            dgvCounters.AllowUserToAddRows = false;
            dgvCounters.AllowUserToDeleteRows = false;
            dgvCounters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvCounters.BackgroundColor = System.Drawing.Color.White;
            dgvCounters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCounters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colObjectName, colCounterName, colInstance, colAvg, colMin, colMax, colTotal, colCurrent, colSampleCount });
            dgvCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvCounters.Location = new System.Drawing.Point(0, 0);
            dgvCounters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvCounters.Name = "dgvCounters";
            dgvCounters.RowHeadersVisible = false;
            dgvCounters.RowHeadersWidth = 51;
            dgvCounters.Size = new System.Drawing.Size(1020, 756);
            dgvCounters.TabIndex = 0;
            // 
            // colObjectName
            // 
            colObjectName.DataPropertyName = "object_name";
            colObjectName.HeaderText = "Object";
            colObjectName.MinimumWidth = 6;
            colObjectName.Name = "colObjectName";
            colObjectName.Width = 82;
            // 
            // colCounterName
            // 
            colCounterName.DataPropertyName = "counter_name";
            colCounterName.HeaderText = "Counter";
            colCounterName.MinimumWidth = 6;
            colCounterName.Name = "colCounterName";
            colCounterName.Width = 90;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "instance_name";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.Width = 92;
            // 
            // colAvg
            // 
            colAvg.DataPropertyName = "Avg";
            colAvg.HeaderText = "Avg";
            colAvg.MinimumWidth = 6;
            colAvg.Name = "colAvg";
            colAvg.Width = 41;
            // 
            // colMin
            // 
            colMin.DataPropertyName = "Min";
            colMin.HeaderText = "Min";
            colMin.MinimumWidth = 6;
            colMin.Name = "colMin";
            colMin.Width = 40;
            // 
            // colMax
            // 
            colMax.DataPropertyName = "Max";
            colMax.HeaderText = "Max";
            colMax.MinimumWidth = 6;
            colMax.Name = "colMax";
            colMax.Width = 43;
            // 
            // colTotal
            // 
            colTotal.DataPropertyName = "Total";
            colTotal.HeaderText = "Total";
            colTotal.MinimumWidth = 6;
            colTotal.Name = "colTotal";
            colTotal.Width = 48;
            // 
            // colCurrent
            // 
            colCurrent.DataPropertyName = "Current";
            colCurrent.HeaderText = "Current";
            colCurrent.MinimumWidth = 6;
            colCurrent.Name = "colCurrent";
            colCurrent.Width = 63;
            // 
            // colSampleCount
            // 
            colSampleCount.DataPropertyName = "SampleCount";
            colSampleCount.HeaderText = "Sample Count";
            colSampleCount.MinimumWidth = 6;
            colSampleCount.Name = "colSampleCount";
            colSampleCount.Width = 108;
            // 
            // panel1
            // 
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnClear);
            panel1.Controls.Add(lblSearch);
            panel1.Controls.Add(txtSearch);
            panel1.Controls.Add(bttnOK);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 756);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1020, 74);
            panel1.TabIndex = 1;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.ForeColor = System.Drawing.Color.Black;
            bttnCancel.Location = new System.Drawing.Point(813, 20);
            bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(89, 39);
            bttnCancel.TabIndex = 4;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // bttnClear
            // 
            bttnClear.ForeColor = System.Drawing.Color.Black;
            bttnClear.Location = new System.Drawing.Point(611, 20);
            bttnClear.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnClear.Name = "bttnClear";
            bttnClear.Size = new System.Drawing.Size(134, 39);
            bttnClear.TabIndex = 3;
            bttnClear.Text = "Clear Selected";
            bttnClear.UseVisualStyleBackColor = true;
            bttnClear.Click += BttnClear_Click;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Location = new System.Drawing.Point(12, 29);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(56, 20);
            lblSearch.TabIndex = 2;
            lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(75, 25);
            txtSearch.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(244, 27);
            txtSearch.TabIndex = 1;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.ForeColor = System.Drawing.Color.Black;
            bttnOK.Location = new System.Drawing.Point(919, 20);
            bttnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(89, 39);
            bttnOK.TabIndex = 0;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "object_name";
            dataGridViewTextBoxColumn1.HeaderText = "Object";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.Width = 78;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "counter_name";
            dataGridViewTextBoxColumn2.HeaderText = "Counter";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.Width = 87;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "instance_name";
            dataGridViewTextBoxColumn3.HeaderText = "Instance";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.Width = 90;
            // 
            // SelectPerformanceCounters
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1020, 830);
            Controls.Add(dgvCounters);
            Controls.Add(panel1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "SelectPerformanceCounters";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Performance Counters";
            Load += SelectPerformanceCounters_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCounters).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvCounters;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colAvg;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colMin;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colMax;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colTotal;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colSampleCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnClear;
    }
}