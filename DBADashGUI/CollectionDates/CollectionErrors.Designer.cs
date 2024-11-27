using DBADashGUI.CustomReports;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CollectionErrors));
            dgvDBADashErrors = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewLinkColumn();
            ErrorDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ErrorSource = new System.Windows.Forms.DataGridViewLinkColumn();
            ErrorContext = new System.Windows.Forms.DataGridViewLinkColumn();
            ErrorMessage = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip3 = new System.Windows.Forms.ToolStrip();
            tsRefreshErrors = new System.Windows.Forms.ToolStripButton();
            tsCopyErrors = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsErrorDays = new System.Windows.Forms.ToolStripDropDownButton();
            tsErrors1Day = new System.Windows.Forms.ToolStripMenuItem();
            tsErrors2Days = new System.Windows.Forms.ToolStripMenuItem();
            tsErrors3Days = new System.Windows.Forms.ToolStripMenuItem();
            tsErrors7Days = new System.Windows.Forms.ToolStripMenuItem();
            tsErrors14Days = new System.Windows.Forms.ToolStripMenuItem();
            tsErrors30Days = new System.Windows.Forms.ToolStripMenuItem();
            tsAckErrors = new System.Windows.Forms.ToolStripButton();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            contextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            txtContext = new System.Windows.Forms.ToolStripTextBox();
            instanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            txtInstance = new System.Windows.Forms.ToolStripTextBox();
            messageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            txtMessage = new System.Windows.Forms.ToolStripTextBox();
            sourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            txtSource = new System.Windows.Forms.ToolStripTextBox();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            tsClearFilters = new System.Windows.Forms.ToolStripMenuItem();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvDBADashErrors).BeginInit();
            toolStrip3.SuspendLayout();
            SuspendLayout();
            // 
            // dgvDBADashErrors
            // 
            dgvDBADashErrors.AllowUserToAddRows = false;
            dgvDBADashErrors.AllowUserToDeleteRows = false;
            dgvDBADashErrors.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvDBADashErrors.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDBADashErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDBADashErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, ErrorDate, ErrorSource, ErrorContext, ErrorMessage });
            dgvDBADashErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvDBADashErrors.EnableHeadersVisualStyles = false;
            dgvDBADashErrors.Location = new System.Drawing.Point(0, 31);
            dgvDBADashErrors.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvDBADashErrors.Name = "dgvDBADashErrors";
            dgvDBADashErrors.ReadOnly = true;
            dgvDBADashErrors.ResultSetID = 0;
            dgvDBADashErrors.ResultSetName = null;
            dgvDBADashErrors.RowHeadersVisible = false;
            dgvDBADashErrors.RowHeadersWidth = 51;
            dgvDBADashErrors.Size = new System.Drawing.Size(829, 517);
            dgvDBADashErrors.TabIndex = 2;
            dgvDBADashErrors.CellContentClick += DgvDBADashErrors_CellContentClick;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Instance.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            Instance.Width = 90;
            // 
            // ErrorDate
            // 
            ErrorDate.DataPropertyName = "ErrorDate";
            ErrorDate.HeaderText = "Date";
            ErrorDate.MinimumWidth = 6;
            ErrorDate.Name = "ErrorDate";
            ErrorDate.ReadOnly = true;
            ErrorDate.Width = 67;
            // 
            // ErrorSource
            // 
            ErrorSource.DataPropertyName = "ErrorSource";
            ErrorSource.HeaderText = "Source";
            ErrorSource.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            ErrorSource.MinimumWidth = 6;
            ErrorSource.Name = "ErrorSource";
            ErrorSource.ReadOnly = true;
            ErrorSource.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ErrorSource.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            ErrorSource.Width = 82;
            // 
            // ErrorContext
            // 
            ErrorContext.DataPropertyName = "ErrorContext";
            ErrorContext.HeaderText = "Error Context";
            ErrorContext.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            ErrorContext.MinimumWidth = 6;
            ErrorContext.Name = "ErrorContext";
            ErrorContext.ReadOnly = true;
            ErrorContext.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ErrorContext.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            ErrorContext.Width = 120;
            // 
            // ErrorMessage
            // 
            ErrorMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            ErrorMessage.DataPropertyName = "ErrorMessage";
            ErrorMessage.HeaderText = "Message";
            ErrorMessage.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            ErrorMessage.MinimumWidth = 50;
            ErrorMessage.Name = "ErrorMessage";
            ErrorMessage.ReadOnly = true;
            ErrorMessage.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            ErrorMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // toolStrip3
            // 
            toolStrip3.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefreshErrors, tsCopyErrors, tsExcel, tsErrorDays, tsAckErrors, tsFilter });
            toolStrip3.Location = new System.Drawing.Point(0, 0);
            toolStrip3.Name = "toolStrip3";
            toolStrip3.Size = new System.Drawing.Size(829, 31);
            toolStrip3.TabIndex = 3;
            toolStrip3.Text = "toolStrip3";
            // 
            // tsRefreshErrors
            // 
            tsRefreshErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshErrors.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshErrors.Name = "tsRefreshErrors";
            tsRefreshErrors.Size = new System.Drawing.Size(29, 28);
            tsRefreshErrors.Text = "Refresh";
            tsRefreshErrors.Click += TsRefreshErrors_Click;
            // 
            // tsCopyErrors
            // 
            tsCopyErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyErrors.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyErrors.Name = "tsCopyErrors";
            tsCopyErrors.Size = new System.Drawing.Size(29, 28);
            tsCopyErrors.Text = "Copy";
            tsCopyErrors.Click += TsCopyErrors_Click;
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 28);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsErrorDays
            // 
            tsErrorDays.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsErrorDays.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsErrors1Day, tsErrors2Days, tsErrors3Days, tsErrors7Days, tsErrors14Days, tsErrors30Days });
            tsErrorDays.Image = Properties.Resources.Time_16x;
            tsErrorDays.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsErrorDays.Name = "tsErrorDays";
            tsErrorDays.Size = new System.Drawing.Size(34, 28);
            tsErrorDays.Text = "toolStripDropDownButton1";
            // 
            // tsErrors1Day
            // 
            tsErrors1Day.Name = "tsErrors1Day";
            tsErrors1Day.Size = new System.Drawing.Size(144, 26);
            tsErrors1Day.Tag = "1";
            tsErrors1Day.Text = "1 Day";
            tsErrors1Day.Click += TsErrorDays_Click;
            // 
            // tsErrors2Days
            // 
            tsErrors2Days.Name = "tsErrors2Days";
            tsErrors2Days.Size = new System.Drawing.Size(144, 26);
            tsErrors2Days.Tag = "2";
            tsErrors2Days.Text = "2 Days";
            tsErrors2Days.Click += TsErrorDays_Click;
            // 
            // tsErrors3Days
            // 
            tsErrors3Days.Name = "tsErrors3Days";
            tsErrors3Days.Size = new System.Drawing.Size(144, 26);
            tsErrors3Days.Tag = "3";
            tsErrors3Days.Text = "3 Days";
            tsErrors3Days.Click += TsErrorDays_Click;
            // 
            // tsErrors7Days
            // 
            tsErrors7Days.Name = "tsErrors7Days";
            tsErrors7Days.Size = new System.Drawing.Size(144, 26);
            tsErrors7Days.Tag = "7";
            tsErrors7Days.Text = "7 Days";
            tsErrors7Days.Click += TsErrorDays_Click;
            // 
            // tsErrors14Days
            // 
            tsErrors14Days.Name = "tsErrors14Days";
            tsErrors14Days.Size = new System.Drawing.Size(144, 26);
            tsErrors14Days.Tag = "14";
            tsErrors14Days.Text = "14 Days";
            tsErrors14Days.Click += TsErrorDays_Click;
            // 
            // tsErrors30Days
            // 
            tsErrors30Days.Name = "tsErrors30Days";
            tsErrors30Days.Size = new System.Drawing.Size(144, 26);
            tsErrors30Days.Tag = "30";
            tsErrors30Days.Text = "30 Days";
            tsErrors30Days.Click += TsErrorDays_Click;
            // 
            // tsAckErrors
            // 
            tsAckErrors.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsAckErrors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsAckErrors.Image = (System.Drawing.Image)resources.GetObject("tsAckErrors.Image");
            tsAckErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsAckErrors.Name = "tsAckErrors";
            tsAckErrors.Size = new System.Drawing.Size(145, 28);
            tsAckErrors.Text = "Acknowledge Errors";
            tsAckErrors.Click += TsAckErrors_Click;
            // 
            // tsFilter
            // 
            tsFilter.BackColor = System.Drawing.SystemColors.Control;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { contextToolStripMenuItem, instanceToolStripMenuItem, messageToolStripMenuItem, sourceToolStripMenuItem, toolStripSeparator1, tsClearFilters });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(76, 28);
            tsFilter.Text = "Filter";
            // 
            // contextToolStripMenuItem
            // 
            contextToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { txtContext });
            contextToolStripMenuItem.Name = "contextToolStripMenuItem";
            contextToolStripMenuItem.Size = new System.Drawing.Size(169, 26);
            contextToolStripMenuItem.Text = "Context";
            // 
            // txtContext
            // 
            txtContext.Name = "txtContext";
            txtContext.Size = new System.Drawing.Size(150, 27);
            txtContext.KeyPress += Filter_KeyPress;
            txtContext.TextChanged += TxtContext_TextChanged;
            // 
            // instanceToolStripMenuItem
            // 
            instanceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { txtInstance });
            instanceToolStripMenuItem.Name = "instanceToolStripMenuItem";
            instanceToolStripMenuItem.Size = new System.Drawing.Size(169, 26);
            instanceToolStripMenuItem.Text = "Instance";
            // 
            // txtInstance
            // 
            txtInstance.Name = "txtInstance";
            txtInstance.Size = new System.Drawing.Size(150, 27);
            txtInstance.KeyPress += Filter_KeyPress;
            txtInstance.TextChanged += TxtInstance_TextChanged;
            // 
            // messageToolStripMenuItem
            // 
            messageToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { txtMessage });
            messageToolStripMenuItem.Name = "messageToolStripMenuItem";
            messageToolStripMenuItem.Size = new System.Drawing.Size(169, 26);
            messageToolStripMenuItem.Text = "Message";
            // 
            // txtMessage
            // 
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new System.Drawing.Size(150, 27);
            txtMessage.KeyPress += Filter_KeyPress;
            txtMessage.TextChanged += TxtMessage_TextChanged;
            // 
            // sourceToolStripMenuItem
            // 
            sourceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { txtSource });
            sourceToolStripMenuItem.Name = "sourceToolStripMenuItem";
            sourceToolStripMenuItem.Size = new System.Drawing.Size(169, 26);
            sourceToolStripMenuItem.Text = "Source";
            // 
            // txtSource
            // 
            txtSource.Name = "txtSource";
            txtSource.Size = new System.Drawing.Size(150, 27);
            txtSource.KeyPress += Filter_KeyPress;
            txtSource.TextChanged += TxtSource_TextChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(166, 6);
            // 
            // tsClearFilters
            // 
            tsClearFilters.Name = "tsClearFilters";
            tsClearFilters.Size = new System.Drawing.Size(169, 26);
            tsClearFilters.Text = "Clear Filters";
            tsClearFilters.Click += TsClearFilters_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "ErrorDate";
            dataGridViewTextBoxColumn2.HeaderText = "Date";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 67;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "ErrorSource";
            dataGridViewTextBoxColumn3.HeaderText = "Source";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 82;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "ErrorContext";
            dataGridViewTextBoxColumn4.HeaderText = "Error Context";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 120;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "ErrorMessage";
            dataGridViewTextBoxColumn5.HeaderText = "Message";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 94;
            // 
            // CollectionErrors
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgvDBADashErrors);
            Controls.Add(toolStrip3);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "CollectionErrors";
            Size = new System.Drawing.Size(829, 548);
            ((System.ComponentModel.ISupportInitialize)dgvDBADashErrors).EndInit();
            toolStrip3.ResumeLayout(false);
            toolStrip3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgvDBADashErrors;
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
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem sourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox txtSource;
        private System.Windows.Forms.ToolStripMenuItem contextToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox txtContext;
        private System.Windows.Forms.ToolStripMenuItem messageToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox txtMessage;
        private System.Windows.Forms.ToolStripMenuItem tsClearFilters;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.DataGridViewLinkColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn ErrorDate;
        private System.Windows.Forms.DataGridViewLinkColumn ErrorSource;
        private System.Windows.Forms.DataGridViewLinkColumn ErrorContext;
        private System.Windows.Forms.DataGridViewLinkColumn ErrorMessage;
        private System.Windows.Forms.ToolStripMenuItem instanceToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox txtInstance;
    }
}
