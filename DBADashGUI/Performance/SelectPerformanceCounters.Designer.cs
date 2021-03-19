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
            this.dgvCounters = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.bttnOK = new System.Windows.Forms.Button();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAvg = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colMin = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colMax = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colTotal = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colCurrent = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colSampleCount = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCounters)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvCounters
            // 
            this.dgvCounters.AllowUserToAddRows = false;
            this.dgvCounters.AllowUserToDeleteRows = false;
            this.dgvCounters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvCounters.BackgroundColor = System.Drawing.Color.White;
            this.dgvCounters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCounters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colObjectName,
            this.colCounterName,
            this.colInstance,
            this.colAvg,
            this.colMin,
            this.colMax,
            this.colTotal,
            this.colCurrent,
            this.colSampleCount});
            this.dgvCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCounters.Location = new System.Drawing.Point(0, 0);
            this.dgvCounters.Name = "dgvCounters";
            this.dgvCounters.RowHeadersVisible = false;
            this.dgvCounters.RowHeadersWidth = 51;
            this.dgvCounters.RowTemplate.Height = 24;
            this.dgvCounters.Size = new System.Drawing.Size(1020, 605);
            this.dgvCounters.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.bttnOK);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 605);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1020, 59);
            this.panel1.TabIndex = 1;
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(919, 16);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(89, 31);
            this.bttnOK.TabIndex = 0;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "object_name";
            this.dataGridViewTextBoxColumn1.HeaderText = "Object";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Width = 78;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "counter_name";
            this.dataGridViewTextBoxColumn2.HeaderText = "Counter";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.Width = 87;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "instance_name";
            this.dataGridViewTextBoxColumn3.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.Width = 90;
            // 
            // colObjectName
            // 
            this.colObjectName.DataPropertyName = "object_name";
            this.colObjectName.HeaderText = "Object";
            this.colObjectName.MinimumWidth = 6;
            this.colObjectName.Name = "colObjectName";
            this.colObjectName.Width = 78;
            // 
            // colCounterName
            // 
            this.colCounterName.DataPropertyName = "counter_name";
            this.colCounterName.HeaderText = "Counter";
            this.colCounterName.MinimumWidth = 6;
            this.colCounterName.Name = "colCounterName";
            this.colCounterName.Width = 87;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "instance_name";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.Width = 90;
            // 
            // colAvg
            // 
            this.colAvg.DataPropertyName = "Avg";
            this.colAvg.HeaderText = "Avg";
            this.colAvg.MinimumWidth = 6;
            this.colAvg.Name = "colAvg";
            this.colAvg.Width = 38;
            // 
            // colMin
            // 
            this.colMin.DataPropertyName = "Min";
            this.colMin.HeaderText = "Min";
            this.colMin.MinimumWidth = 6;
            this.colMin.Name = "colMin";
            this.colMin.Width = 36;
            // 
            // colMax
            // 
            this.colMax.DataPropertyName = "Max";
            this.colMax.HeaderText = "Max";
            this.colMax.MinimumWidth = 6;
            this.colMax.Name = "colMax";
            this.colMax.Width = 39;
            // 
            // colTotal
            // 
            this.colTotal.DataPropertyName = "Total";
            this.colTotal.HeaderText = "Total";
            this.colTotal.MinimumWidth = 6;
            this.colTotal.Name = "colTotal";
            this.colTotal.Width = 46;
            // 
            // colCurrent
            // 
            this.colCurrent.DataPropertyName = "Current";
            this.colCurrent.HeaderText = "Current";
            this.colCurrent.MinimumWidth = 6;
            this.colCurrent.Name = "colCurrent";
            this.colCurrent.Width = 61;
            // 
            // colSampleCount
            // 
            this.colSampleCount.DataPropertyName = "SampleCount";
            this.colSampleCount.HeaderText = "Sample Count";
            this.colSampleCount.MinimumWidth = 6;
            this.colSampleCount.Name = "colSampleCount";
            this.colSampleCount.Width = 102;
            // 
            // SelectPerformanceCounters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1020, 664);
            this.Controls.Add(this.dgvCounters);
            this.Controls.Add(this.panel1);
            this.Name = "SelectPerformanceCounters";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Performance Counters";
            this.Load += new System.EventHandler(this.SelectPerformanceCounters_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCounters)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

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
    }
}