using DBADashGUI.CustomReports;

namespace DBADashGUI
{
    partial class ConfigurationHistory
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
            dgv = new DBADashDataGridView();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            value_in_use = new System.Windows.Forms.DataGridViewTextBoxColumn();
            new_value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            new_value_in_use = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            is_dynamic = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            is_advanced = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            default_value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Minimum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Maximum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            configuration1 = new Changes.Configuration();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsHistoryExcel = new System.Windows.Forms.ToolStripButton();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Instance, colName, Description, value, value_in_use, new_value, new_value_in_use, ValidFrom, ValidTo, is_dynamic, is_advanced, default_value, Minimum, Maximum });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgv.DefaultCellStyle = dataGridViewCellStyle2;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1678, 488);
            dgv.TabIndex = 0;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 90;
            // 
            // colName
            // 
            colName.DataPropertyName = "name";
            colName.HeaderText = "Name";
            colName.MinimumWidth = 6;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 74;
            // 
            // Description
            // 
            Description.DataPropertyName = "description";
            Description.HeaderText = "Description";
            Description.MinimumWidth = 6;
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 108;
            // 
            // value
            // 
            value.DataPropertyName = "value";
            value.HeaderText = "Value";
            value.MinimumWidth = 6;
            value.Name = "value";
            value.ReadOnly = true;
            value.Width = 73;
            // 
            // value_in_use
            // 
            value_in_use.DataPropertyName = "value_in_use";
            value_in_use.HeaderText = "Value in Use";
            value_in_use.MinimumWidth = 6;
            value_in_use.Name = "value_in_use";
            value_in_use.ReadOnly = true;
            value_in_use.Width = 117;
            // 
            // new_value
            // 
            new_value.DataPropertyName = "new_value";
            new_value.HeaderText = "New Value";
            new_value.MinimumWidth = 6;
            new_value.Name = "new_value";
            new_value.ReadOnly = true;
            new_value.Width = 104;
            // 
            // new_value_in_use
            // 
            new_value_in_use.DataPropertyName = "new_value_in_use";
            new_value_in_use.HeaderText = "New Value In Use";
            new_value_in_use.MinimumWidth = 6;
            new_value_in_use.Name = "new_value_in_use";
            new_value_in_use.ReadOnly = true;
            new_value_in_use.Width = 113;
            // 
            // ValidFrom
            // 
            ValidFrom.DataPropertyName = "ValidFrom";
            ValidFrom.HeaderText = "Valid From";
            ValidFrom.MinimumWidth = 6;
            ValidFrom.Name = "ValidFrom";
            ValidFrom.ReadOnly = true;
            ValidFrom.Width = 96;
            // 
            // ValidTo
            // 
            ValidTo.DataPropertyName = "ValidTo";
            ValidTo.HeaderText = "Valid To";
            ValidTo.MinimumWidth = 6;
            ValidTo.Name = "ValidTo";
            ValidTo.ReadOnly = true;
            ValidTo.Width = 82;
            // 
            // is_dynamic
            // 
            is_dynamic.DataPropertyName = "is_dynamic";
            is_dynamic.HeaderText = "Is Dynamic";
            is_dynamic.MinimumWidth = 6;
            is_dynamic.Name = "is_dynamic";
            is_dynamic.ReadOnly = true;
            is_dynamic.Width = 74;
            // 
            // is_advanced
            // 
            is_advanced.DataPropertyName = "is_advanced";
            is_advanced.HeaderText = "Is Advanced";
            is_advanced.MinimumWidth = 6;
            is_advanced.Name = "is_advanced";
            is_advanced.ReadOnly = true;
            is_advanced.Width = 82;
            // 
            // default_value
            // 
            default_value.DataPropertyName = "default_value";
            default_value.HeaderText = "Default";
            default_value.MinimumWidth = 6;
            default_value.Name = "default_value";
            default_value.ReadOnly = true;
            default_value.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            default_value.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            default_value.Width = 59;
            // 
            // Minimum
            // 
            Minimum.DataPropertyName = "Minimum";
            Minimum.HeaderText = "Minimum";
            Minimum.MinimumWidth = 6;
            Minimum.Name = "Minimum";
            Minimum.ReadOnly = true;
            Minimum.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Minimum.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            Minimum.Width = 69;
            // 
            // Maximum
            // 
            Maximum.DataPropertyName = "Maximum";
            Maximum.HeaderText = "Maximum";
            Maximum.MinimumWidth = 6;
            Maximum.Name = "Maximum";
            Maximum.ReadOnly = true;
            Maximum.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Maximum.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            Maximum.Width = 72;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(configuration1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Panel2.Controls.Add(toolStrip1);
            splitContainer1.Size = new System.Drawing.Size(1678, 1040);
            splitContainer1.SplitterDistance = 520;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // configuration1
            // 
            configuration1.BackColor = System.Drawing.Color.White;
            configuration1.Dock = System.Windows.Forms.DockStyle.Fill;
            configuration1.Location = new System.Drawing.Point(0, 0);
            configuration1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            configuration1.Name = "configuration1";
            configuration1.Size = new System.Drawing.Size(1678, 520);
            configuration1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1, tsRefresh, tsCopy, tsHistoryExcel, tsClearFilter });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1678, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(192, 24);
            toolStripLabel1.Text = "Configuration Change Log";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Name = "tsRefresh";
            tsRefresh.Size = new System.Drawing.Size(29, 24);
            tsRefresh.Text = "Refresh";
            tsRefresh.Click += TsRefresh_Click;
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // tsHistoryExcel
            // 
            tsHistoryExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsHistoryExcel.Image = Properties.Resources.excel16x16;
            tsHistoryExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHistoryExcel.Name = "tsHistoryExcel";
            tsHistoryExcel.Size = new System.Drawing.Size(29, 24);
            tsHistoryExcel.Text = "Excel";
            tsHistoryExcel.Click += TsHistoryExcel_Click;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // ConfigurationHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ConfigurationHistory";
            Size = new System.Drawing.Size(1678, 1040);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgv;
        private Changes.Configuration configuration1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsHistoryExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn value;
        private System.Windows.Forms.DataGridViewTextBoxColumn value_in_use;
        private System.Windows.Forms.DataGridViewTextBoxColumn new_value;
        private System.Windows.Forms.DataGridViewTextBoxColumn new_value_in_use;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValidTo;
        private System.Windows.Forms.DataGridViewCheckBoxColumn is_dynamic;
        private System.Windows.Forms.DataGridViewCheckBoxColumn is_advanced;
        private System.Windows.Forms.DataGridViewTextBoxColumn default_value;
        private System.Windows.Forms.DataGridViewTextBoxColumn Minimum;
        private System.Windows.Forms.DataGridViewTextBoxColumn Maximum;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
