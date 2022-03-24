namespace DBADashGUI.CollectionDates
{
    partial class CollectionErrors
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionErrors));
            this.dgvDBADashErrors = new System.Windows.Forms.DataGridView();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.tsRefreshErrors = new System.Windows.Forms.ToolStripButton();
            this.tsCopyErrors = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsErrorDays = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsErrors1Day = new System.Windows.Forms.ToolStripMenuItem();
            this.tsErrors2Days = new System.Windows.Forms.ToolStripMenuItem();
            this.tsErrors3Days = new System.Windows.Forms.ToolStripMenuItem();
            this.tsErrors7Days = new System.Windows.Forms.ToolStripMenuItem();
            this.tsErrors14Days = new System.Windows.Forms.ToolStripMenuItem();
            this.tsErrors30Days = new System.Windows.Forms.ToolStripMenuItem();
            this.tsAckErrors = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorContext = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDBADashErrors)).BeginInit();
            this.toolStrip3.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvDBADashErrors
            // 
            this.dgvDBADashErrors.AllowUserToAddRows = false;
            this.dgvDBADashErrors.AllowUserToDeleteRows = false;
            this.dgvDBADashErrors.BackgroundColor = System.Drawing.Color.White;
            this.dgvDBADashErrors.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvDBADashErrors.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvDBADashErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDBADashErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.ErrorDate,
            this.ErrorSource,
            this.ErrorContext,
            this.ErrorMessage});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvDBADashErrors.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvDBADashErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDBADashErrors.Location = new System.Drawing.Point(0, 27);
            this.dgvDBADashErrors.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvDBADashErrors.Name = "dgvDBADashErrors";
            this.dgvDBADashErrors.ReadOnly = true;
            this.dgvDBADashErrors.RowHeadersVisible = false;
            this.dgvDBADashErrors.RowHeadersWidth = 51;
            this.dgvDBADashErrors.RowTemplate.Height = 24;
            this.dgvDBADashErrors.Size = new System.Drawing.Size(829, 521);
            this.dgvDBADashErrors.TabIndex = 2;
            // 
            // toolStrip3
            // 
            this.toolStrip3.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefreshErrors,
            this.tsCopyErrors,
            this.tsExcel,
            this.tsErrorDays,
            this.tsAckErrors});
            this.toolStrip3.Location = new System.Drawing.Point(0, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(829, 27);
            this.toolStrip3.TabIndex = 3;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // tsRefreshErrors
            // 
            this.tsRefreshErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefreshErrors.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefreshErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefreshErrors.Name = "tsRefreshErrors";
            this.tsRefreshErrors.Size = new System.Drawing.Size(29, 24);
            this.tsRefreshErrors.Text = "Refresh";
            this.tsRefreshErrors.Click += new System.EventHandler(this.tsRefreshErrors_Click);
            // 
            // tsCopyErrors
            // 
            this.tsCopyErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopyErrors.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopyErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopyErrors.Name = "tsCopyErrors";
            this.tsCopyErrors.Size = new System.Drawing.Size(29, 24);
            this.tsCopyErrors.Text = "Copy";
            this.tsCopyErrors.Click += new System.EventHandler(this.tsCopyErrors_Click);
            // 
            // tsExcel
            // 
            this.tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsExcel.Name = "tsExcel";
            this.tsExcel.Size = new System.Drawing.Size(29, 24);
            this.tsExcel.Text = "Export Excel";
            this.tsExcel.Click += new System.EventHandler(this.tsExcel_Click);
            // 
            // tsErrorDays
            // 
            this.tsErrorDays.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsErrorDays.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsErrors1Day,
            this.tsErrors2Days,
            this.tsErrors3Days,
            this.tsErrors7Days,
            this.tsErrors14Days,
            this.tsErrors30Days});
            this.tsErrorDays.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsErrorDays.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsErrorDays.Name = "tsErrorDays";
            this.tsErrorDays.Size = new System.Drawing.Size(34, 24);
            this.tsErrorDays.Text = "toolStripDropDownButton1";
            // 
            // tsErrors1Day
            // 
            this.tsErrors1Day.Name = "tsErrors1Day";
            this.tsErrors1Day.Size = new System.Drawing.Size(144, 26);
            this.tsErrors1Day.Tag = "1";
            this.tsErrors1Day.Text = "1 Day";
            this.tsErrors1Day.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsErrors2Days
            // 
            this.tsErrors2Days.Name = "tsErrors2Days";
            this.tsErrors2Days.Size = new System.Drawing.Size(144, 26);
            this.tsErrors2Days.Tag = "2";
            this.tsErrors2Days.Text = "2 Days";
            this.tsErrors2Days.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsErrors3Days
            // 
            this.tsErrors3Days.Name = "tsErrors3Days";
            this.tsErrors3Days.Size = new System.Drawing.Size(144, 26);
            this.tsErrors3Days.Tag = "3";
            this.tsErrors3Days.Text = "3 Days";
            this.tsErrors3Days.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsErrors7Days
            // 
            this.tsErrors7Days.Name = "tsErrors7Days";
            this.tsErrors7Days.Size = new System.Drawing.Size(144, 26);
            this.tsErrors7Days.Tag = "7";
            this.tsErrors7Days.Text = "7 Days";
            this.tsErrors7Days.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsErrors14Days
            // 
            this.tsErrors14Days.Name = "tsErrors14Days";
            this.tsErrors14Days.Size = new System.Drawing.Size(144, 26);
            this.tsErrors14Days.Tag = "14";
            this.tsErrors14Days.Text = "14 Days";
            this.tsErrors14Days.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsErrors30Days
            // 
            this.tsErrors30Days.Name = "tsErrors30Days";
            this.tsErrors30Days.Size = new System.Drawing.Size(144, 26);
            this.tsErrors30Days.Tag = "30";
            this.tsErrors30Days.Text = "30 Days";
            this.tsErrors30Days.Click += new System.EventHandler(this.tsErrorDays_Click);
            // 
            // tsAckErrors
            // 
            this.tsAckErrors.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsAckErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsAckErrors.Image = ((System.Drawing.Image)(resources.GetObject("tsAckErrors.Image")));
            this.tsAckErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsAckErrors.Name = "tsAckErrors";
            this.tsAckErrors.Size = new System.Drawing.Size(145, 24);
            this.tsAckErrors.Text = "Acknowledge Errors";
            this.tsAckErrors.Click += new System.EventHandler(this.tsAckErrors_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "ErrorDate";
            this.dataGridViewTextBoxColumn2.HeaderText = "Date";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 67;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "ErrorSource";
            this.dataGridViewTextBoxColumn3.HeaderText = "Source";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 82;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "ErrorContext";
            this.dataGridViewTextBoxColumn4.HeaderText = "Error Context";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 120;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "ErrorMessage";
            this.dataGridViewTextBoxColumn5.HeaderText = "Message";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 94;
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "InstanceDisplayName";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // ErrorDate
            // 
            this.ErrorDate.DataPropertyName = "ErrorDate";
            this.ErrorDate.HeaderText = "Date";
            this.ErrorDate.MinimumWidth = 6;
            this.ErrorDate.Name = "ErrorDate";
            this.ErrorDate.ReadOnly = true;
            this.ErrorDate.Width = 67;
            // 
            // ErrorSource
            // 
            this.ErrorSource.DataPropertyName = "ErrorSource";
            this.ErrorSource.HeaderText = "Source";
            this.ErrorSource.MinimumWidth = 6;
            this.ErrorSource.Name = "ErrorSource";
            this.ErrorSource.ReadOnly = true;
            this.ErrorSource.Width = 82;
            // 
            // ErrorContext
            // 
            this.ErrorContext.DataPropertyName = "ErrorContext";
            this.ErrorContext.HeaderText = "Error Context";
            this.ErrorContext.MinimumWidth = 6;
            this.ErrorContext.Name = "ErrorContext";
            this.ErrorContext.ReadOnly = true;
            this.ErrorContext.Width = 120;
            // 
            // ErrorMessage
            // 
            this.ErrorMessage.DataPropertyName = "ErrorMessage";
            this.ErrorMessage.HeaderText = "Message";
            this.ErrorMessage.MinimumWidth = 6;
            this.ErrorMessage.Name = "ErrorMessage";
            this.ErrorMessage.ReadOnly = true;
            this.ErrorMessage.Width = 94;
            // 
            // CollectionErrors
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvDBADashErrors);
            this.Controls.Add(this.toolStrip3);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "CollectionErrors";
            this.Size = new System.Drawing.Size(829, 548);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDBADashErrors)).EndInit();
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvDBADashErrors;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripButton tsRefreshErrors;
        private System.Windows.Forms.ToolStripButton tsCopyErrors;
        private System.Windows.Forms.ToolStripDropDownButton tsErrorDays;
        private System.Windows.Forms.ToolStripMenuItem tsErrors1Day;
        private System.Windows.Forms.ToolStripMenuItem tsErrors2Days;
        private System.Windows.Forms.ToolStripMenuItem tsErrors3Days;
        private System.Windows.Forms.ToolStripMenuItem tsErrors7Days;
        private System.Windows.Forms.ToolStripMenuItem tsErrors14Days;
        private System.Windows.Forms.ToolStripMenuItem tsErrors30Days;
        private System.Windows.Forms.ToolStripButton tsAckErrors;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorContext;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorMessage;
    }
}
