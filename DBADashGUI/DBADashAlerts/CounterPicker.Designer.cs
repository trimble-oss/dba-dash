namespace DBADashGUI.DBADashAlerts
{
    partial class CounterPicker
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            dgv = new DBADashGUI.CustomReports.DBADashDataGridView();
            colCounterID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInstanceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            panel1 = new System.Windows.Forms.Panel();
            lblSearch = new System.Windows.Forms.Label();
            txtSearch = new System.Windows.Forms.TextBox();
            bttnCancel = new System.Windows.Forms.Button();
            bttnOK = new System.Windows.Forms.Button();
            lblSelectedCounter = new System.Windows.Forms.Label();
            timer1 = new System.Windows.Forms.Timer(components);
            chkAllInstances = new System.Windows.Forms.CheckBox();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgv.ColumnHeadersHeight = 29;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colCounterID, colObjectName, colCounterName, colInstanceName });
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle4;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersWidth = 51;
            dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.Size = new System.Drawing.Size(696, 340);
            dgv.TabIndex = 0;
            dgv.SelectionChanged += Dgv_SelectionChanged;
            // 
            // colCounterID
            // 
            colCounterID.DataPropertyName = "CounterID";
            colCounterID.HeaderText = "Counter ID";
            colCounterID.MinimumWidth = 6;
            colCounterID.Name = "colCounterID";
            colCounterID.ReadOnly = true;
            colCounterID.Visible = false;
            colCounterID.Width = 125;
            // 
            // colObjectName
            // 
            colObjectName.DataPropertyName = "object_name";
            colObjectName.HeaderText = "Object Name";
            colObjectName.MinimumWidth = 6;
            colObjectName.Name = "colObjectName";
            colObjectName.ReadOnly = true;
            colObjectName.Width = 126;
            // 
            // colCounterName
            // 
            colCounterName.DataPropertyName = "counter_name";
            colCounterName.HeaderText = "Counter Name";
            colCounterName.MinimumWidth = 6;
            colCounterName.Name = "colCounterName";
            colCounterName.ReadOnly = true;
            colCounterName.Width = 134;
            // 
            // colInstanceName
            // 
            colInstanceName.DataPropertyName = "instance_name";
            colInstanceName.HeaderText = "Instance Name";
            colInstanceName.MinimumWidth = 6;
            colInstanceName.Name = "colInstanceName";
            colInstanceName.ReadOnly = true;
            colInstanceName.Width = 136;
            // 
            // panel1
            // 
            panel1.Controls.Add(chkAllInstances);
            panel1.Controls.Add(lblSearch);
            panel1.Controls.Add(txtSearch);
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnOK);
            panel1.Controls.Add(lblSelectedCounter);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 340);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(696, 109);
            panel1.TabIndex = 1;
            // 
            // lblSearch
            // 
            lblSearch.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblSearch.AutoSize = true;
            lblSearch.Location = new System.Drawing.Point(410, 16);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(56, 20);
            lblSearch.TabIndex = 4;
            lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            txtSearch.Location = new System.Drawing.Point(472, 13);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(194, 27);
            txtSearch.TabIndex = 3;
            txtSearch.TextChanged += Search_TextChanged;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(472, 58);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 2;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += bttnCancel_Click;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.Location = new System.Drawing.Point(572, 58);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 1;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += bttnOK_Click;
            // 
            // lblSelectedCounter
            // 
            lblSelectedCounter.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblSelectedCounter.AutoSize = true;
            lblSelectedCounter.Location = new System.Drawing.Point(22, 62);
            lblSelectedCounter.Name = "lblSelectedCounter";
            lblSelectedCounter.Size = new System.Drawing.Size(125, 20);
            lblSelectedCounter.TabIndex = 0;
            lblSelectedCounter.Text = "Selected Counter:";
            // 
            // timer1
            // 
            timer1.Interval = 500;
            timer1.Tick += ApplySearch;
            // 
            // chkAllInstances
            // 
            chkAllInstances.AutoSize = true;
            chkAllInstances.Location = new System.Drawing.Point(22, 12);
            chkAllInstances.Name = "chkAllInstances";
            chkAllInstances.Size = new System.Drawing.Size(113, 24);
            chkAllInstances.TabIndex = 5;
            chkAllInstances.Text = "All Instances";
            toolTip1.SetToolTip(chkAllInstances, "Apply to all instances of the selected counter");
            chkAllInstances.UseVisualStyleBackColor = true;
            chkAllInstances.CheckedChanged += ChkAllInstances_CheckedChanged;
            // 
            // CounterPicker
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(696, 449);
            Controls.Add(dgv);
            Controls.Add(panel1);
            Name = "CounterPicker";
            Text = "Counter Picker";
            Load += CounterPicker_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private CustomReports.DBADashDataGridView dgv;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Label lblSelectedCounter;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCounterID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstanceName;
        private System.Windows.Forms.CheckBox chkAllInstances;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}