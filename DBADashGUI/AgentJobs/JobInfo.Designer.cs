namespace DBADashGUI.AgentJobs
{
    partial class JobInfo
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            dgvSchedules = new DBADashGUI.CustomReports.DBADashDataGridView();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            lblJobName = new System.Windows.Forms.Label();
            pnlInfo = new System.Windows.Forms.Panel();
            dgvInfo = new DBADashGUI.CustomReports.DBADashDataGridView();
            label1 = new System.Windows.Forms.Label();
            pnlSteps = new System.Windows.Forms.Panel();
            dgvSteps = new DBADashGUI.CustomReports.DBADashDataGridView();
            label2 = new System.Windows.Forms.Label();
            pnlSchedule = new System.Windows.Forms.Panel();
            label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)dgvSchedules).BeginInit();
            tableLayoutPanel1.SuspendLayout();
            pnlInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInfo).BeginInit();
            pnlSteps.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSteps).BeginInit();
            pnlSchedule.SuspendLayout();
            SuspendLayout();
            // 
            // dgvSchedules
            // 
            dgvSchedules.AllowUserToAddRows = false;
            dgvSchedules.AllowUserToDeleteRows = false;
            dgvSchedules.AllowUserToOrderColumns = true;
            dgvSchedules.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvSchedules.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvSchedules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvSchedules.DefaultCellStyle = dataGridViewCellStyle2;
            dgvSchedules.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvSchedules.EnableHeadersVisualStyles = false;
            dgvSchedules.Location = new System.Drawing.Point(0, 20);
            dgvSchedules.Name = "dgvSchedules";
            dgvSchedules.ReadOnly = true;
            dgvSchedules.ResultSetID = 0;
            dgvSchedules.ResultSetName = null;
            dgvSchedules.RowHeadersVisible = false;
            dgvSchedules.RowHeadersWidth = 51;
            dgvSchedules.Size = new System.Drawing.Size(839, 86);
            dgvSchedules.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoScroll = true;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(lblJobName, 0, 0);
            tableLayoutPanel1.Controls.Add(pnlInfo, 0, 1);
            tableLayoutPanel1.Controls.Add(pnlSteps, 0, 2);
            tableLayoutPanel1.Controls.Add(pnlSchedule, 0, 3);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tableLayoutPanel1.Size = new System.Drawing.Size(845, 587);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // lblJobName
            // 
            lblJobName.AutoSize = true;
            lblJobName.Dock = System.Windows.Forms.DockStyle.Fill;
            lblJobName.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            lblJobName.Location = new System.Drawing.Point(3, 0);
            lblJobName.Name = "lblJobName";
            lblJobName.Size = new System.Drawing.Size(839, 31);
            lblJobName.TabIndex = 2;
            lblJobName.Text = "{Job Name}";
            lblJobName.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pnlInfo
            // 
            pnlInfo.Controls.Add(dgvInfo);
            pnlInfo.Controls.Add(label1);
            pnlInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlInfo.Location = new System.Drawing.Point(3, 34);
            pnlInfo.Name = "pnlInfo";
            pnlInfo.Size = new System.Drawing.Size(839, 216);
            pnlInfo.TabIndex = 3;
            // 
            // dgvInfo
            // 
            dgvInfo.AllowUserToAddRows = false;
            dgvInfo.AllowUserToDeleteRows = false;
            dgvInfo.AllowUserToOrderColumns = true;
            dgvInfo.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInfo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInfo.ColumnHeadersVisible = false;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvInfo.DefaultCellStyle = dataGridViewCellStyle4;
            dgvInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvInfo.EnableHeadersVisualStyles = false;
            dgvInfo.Location = new System.Drawing.Point(0, 20);
            dgvInfo.Name = "dgvInfo";
            dgvInfo.ReadOnly = true;
            dgvInfo.ResultSetID = 0;
            dgvInfo.ResultSetName = "Info";
            dgvInfo.RowHeadersVisible = false;
            dgvInfo.RowHeadersWidth = 51;
            dgvInfo.Size = new System.Drawing.Size(839, 196);
            dgvInfo.TabIndex = 3;
            // 
            // label1
            // 
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(839, 20);
            label1.TabIndex = 0;
            label1.Text = "Info";
            // 
            // pnlSteps
            // 
            pnlSteps.Controls.Add(dgvSteps);
            pnlSteps.Controls.Add(label2);
            pnlSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlSteps.Location = new System.Drawing.Point(3, 256);
            pnlSteps.Name = "pnlSteps";
            pnlSteps.Size = new System.Drawing.Size(839, 216);
            pnlSteps.TabIndex = 4;
            // 
            // dgvSteps
            // 
            dgvSteps.AllowUserToAddRows = false;
            dgvSteps.AllowUserToDeleteRows = false;
            dgvSteps.AllowUserToOrderColumns = true;
            dgvSteps.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvSteps.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dgvSteps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvSteps.DefaultCellStyle = dataGridViewCellStyle6;
            dgvSteps.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvSteps.EnableHeadersVisualStyles = false;
            dgvSteps.Location = new System.Drawing.Point(0, 20);
            dgvSteps.Name = "dgvSteps";
            dgvSteps.ReadOnly = true;
            dgvSteps.ResultSetID = 0;
            dgvSteps.ResultSetName = null;
            dgvSteps.RowHeadersVisible = false;
            dgvSteps.RowHeadersWidth = 51;
            dgvSteps.Size = new System.Drawing.Size(839, 196);
            dgvSteps.TabIndex = 1;
            dgvSteps.CellContentClick += Steps_CellContentClick;
            // 
            // label2
            // 
            label2.Dock = System.Windows.Forms.DockStyle.Top;
            label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label2.Location = new System.Drawing.Point(0, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(839, 20);
            label2.TabIndex = 1;
            label2.Text = "Steps";
            // 
            // pnlSchedule
            // 
            pnlSchedule.Controls.Add(dgvSchedules);
            pnlSchedule.Controls.Add(label3);
            pnlSchedule.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlSchedule.Location = new System.Drawing.Point(3, 478);
            pnlSchedule.Name = "pnlSchedule";
            pnlSchedule.Size = new System.Drawing.Size(839, 106);
            pnlSchedule.TabIndex = 5;
            // 
            // label3
            // 
            label3.Dock = System.Windows.Forms.DockStyle.Top;
            label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            label3.Location = new System.Drawing.Point(0, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(839, 20);
            label3.TabIndex = 2;
            label3.Text = "Schedule";
            // 
            // JobInfo
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "JobInfo";
            Size = new System.Drawing.Size(845, 587);
            ((System.ComponentModel.ISupportInitialize)dgvSchedules).EndInit();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            pnlInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvInfo).EndInit();
            pnlSteps.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSteps).EndInit();
            pnlSchedule.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private CustomReports.DBADashDataGridView dgvSchedules;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private CustomReports.DBADashDataGridView dgvSteps;
        private System.Windows.Forms.Label lblJobName;
        private CustomReports.DBADashDataGridView dgvInfo;
        private System.Windows.Forms.Panel pnlInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlSteps;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel pnlSchedule;
        private System.Windows.Forms.Label label3;
    }
}
