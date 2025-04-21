namespace DBADashGUI.HA
{
    partial class RepositoryMetricsConfig
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepositoryMetricsConfig));
            chkInherit = new System.Windows.Forms.CheckBox();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            dgvConfig = new DBADashGUI.CustomReports.DBADashDataGridView();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgvConfig).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // chkInherit
            // 
            chkInherit.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            chkInherit.AutoSize = true;
            chkInherit.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            chkInherit.Location = new System.Drawing.Point(557, 12);
            chkInherit.Name = "chkInherit";
            chkInherit.Size = new System.Drawing.Size(73, 24);
            chkInherit.TabIndex = 1;
            chkInherit.Text = "Inherit";
            chkInherit.UseVisualStyleBackColor = true;
            chkInherit.Click += Inherit_Click;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.Location = new System.Drawing.Point(536, 405);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 2;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += OK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(436, 405);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 3;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            // 
            // dgvConfig
            // 
            dgvConfig.AllowUserToAddRows = false;
            dgvConfig.AllowUserToDeleteRows = false;
            dgvConfig.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvConfig.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvConfig.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvConfig.DefaultCellStyle = dataGridViewCellStyle2;
            dgvConfig.EnableHeadersVisualStyles = false;
            dgvConfig.Location = new System.Drawing.Point(19, 42);
            dgvConfig.Name = "dgvConfig";
            dgvConfig.ResultSetID = 0;
            dgvConfig.ResultSetName = null;
            dgvConfig.RowHeadersVisible = false;
            dgvConfig.RowHeadersWidth = 51;
            dgvConfig.Size = new System.Drawing.Size(611, 357);
            dgvConfig.TabIndex = 4;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.Information_blue_6227_16x16;
            pictureBox1.Location = new System.Drawing.Point(19, 20);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(16, 16);
            pictureBox1.TabIndex = 6;
            pictureBox1.TabStop = false;
            toolTip1.SetToolTip(pictureBox1, resources.GetString("pictureBox1.ToolTip"));
            // 
            // AGMetricsConfig
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(642, 451);
            Controls.Add(pictureBox1);
            Controls.Add(dgvConfig);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(chkInherit);
            Name = "AGMetricsConfig";
            Text = "AG Metrics Config";
            Load += AGMetricsConfig_Load;
            ((System.ComponentModel.ISupportInitialize)dgvConfig).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.CheckBox chkInherit;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private CustomReports.DBADashDataGridView dgvConfig;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}