
using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class ResourceGovernor
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
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colLinkInstance = new System.Windows.Forms.DataGridViewLinkColumn();
            colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colClassifierFunction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colReconfigurationPending = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colReconfigurationError = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colMaxOutstandingIO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colScript = new System.Windows.Forms.DataGridViewLinkColumn();
            colDiff = new System.Windows.Forms.DataGridViewLinkColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsCompare = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colLinkInstance, colEnabled, colClassifierFunction, colReconfigurationPending, colReconfigurationError, colMaxOutstandingIO, colValidFrom, colValidTo, colSnapshotDate, colScript, colDiff });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
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
            dgv.Size = new System.Drawing.Size(895, 452);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.SelectionChanged += Dgv_SelectionChanged;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "InstanceDisplayName";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Width = 125;
            // 
            // colLinkInstance
            // 
            colLinkInstance.DataPropertyName = "InstanceDisplayName";
            colLinkInstance.HeaderText = "Instance";
            colLinkInstance.MinimumWidth = 6;
            colLinkInstance.Name = "colLinkInstance";
            colLinkInstance.ReadOnly = true;
            colLinkInstance.Width = 125;
            // 
            // colEnabled
            // 
            colEnabled.DataPropertyName = "is_enabled";
            colEnabled.HeaderText = "Enabled";
            colEnabled.MinimumWidth = 6;
            colEnabled.Name = "colEnabled";
            colEnabled.ReadOnly = true;
            colEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colEnabled.Width = 125;
            // 
            // colClassifierFunction
            // 
            colClassifierFunction.DataPropertyName = "classifier_function";
            colClassifierFunction.HeaderText = "Classifier Function";
            colClassifierFunction.MinimumWidth = 6;
            colClassifierFunction.Name = "colClassifierFunction";
            colClassifierFunction.ReadOnly = true;
            colClassifierFunction.Width = 125;
            // 
            // colReconfigurationPending
            // 
            colReconfigurationPending.DataPropertyName = "reconfiguration_pending";
            colReconfigurationPending.HeaderText = "Reconfiguration Pending";
            colReconfigurationPending.MinimumWidth = 6;
            colReconfigurationPending.Name = "colReconfigurationPending";
            colReconfigurationPending.ReadOnly = true;
            colReconfigurationPending.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colReconfigurationPending.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colReconfigurationPending.Width = 125;
            // 
            // colReconfigurationError
            // 
            colReconfigurationError.DataPropertyName = "reconfiguration_error";
            colReconfigurationError.HeaderText = "Reconfiguration Error";
            colReconfigurationError.MinimumWidth = 6;
            colReconfigurationError.Name = "colReconfigurationError";
            colReconfigurationError.ReadOnly = true;
            colReconfigurationError.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colReconfigurationError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colReconfigurationError.ToolTipText = "Indicates that there was an error generating the script due to pending reconfiguration";
            colReconfigurationError.Width = 125;
            // 
            // colMaxOutstandingIO
            // 
            colMaxOutstandingIO.DataPropertyName = "max_outstanding_io_per_volume";
            colMaxOutstandingIO.HeaderText = "Max Outstanding IO Per Volume";
            colMaxOutstandingIO.MinimumWidth = 6;
            colMaxOutstandingIO.Name = "colMaxOutstandingIO";
            colMaxOutstandingIO.ReadOnly = true;
            colMaxOutstandingIO.Width = 125;
            // 
            // colValidFrom
            // 
            colValidFrom.DataPropertyName = "ValidFrom";
            colValidFrom.HeaderText = "Valid From";
            colValidFrom.MinimumWidth = 6;
            colValidFrom.Name = "colValidFrom";
            colValidFrom.ReadOnly = true;
            colValidFrom.Width = 125;
            // 
            // colValidTo
            // 
            colValidTo.DataPropertyName = "ValidTo";
            colValidTo.HeaderText = "Valid To";
            colValidTo.MinimumWidth = 6;
            colValidTo.Name = "colValidTo";
            colValidTo.ReadOnly = true;
            colValidTo.Width = 125;
            // 
            // colSnapshotDate
            // 
            colSnapshotDate.DataPropertyName = "SnapshotDate";
            colSnapshotDate.HeaderText = "Validated Date";
            colSnapshotDate.MinimumWidth = 6;
            colSnapshotDate.Name = "colSnapshotDate";
            colSnapshotDate.ReadOnly = true;
            colSnapshotDate.Width = 125;
            // 
            // colScript
            // 
            colScript.HeaderText = "View Script";
            colScript.MinimumWidth = 6;
            colScript.Name = "colScript";
            colScript.ReadOnly = true;
            colScript.Text = "View Script";
            colScript.UseColumnTextForLinkValue = true;
            colScript.Width = 125;
            // 
            // colDiff
            // 
            colDiff.HeaderText = "Diff";
            colDiff.MinimumWidth = 6;
            colDiff.Name = "colDiff";
            colDiff.ReadOnly = true;
            colDiff.Text = "Diff Previous";
            colDiff.UseColumnTextForLinkValue = true;
            colDiff.Width = 125;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsClearFilter, tsBack, tsCompare });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(895, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
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
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // tsCompare
            // 
            tsCompare.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsCompare.Enabled = false;
            tsCompare.Image = Properties.Resources.Diff_16x;
            tsCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCompare.Name = "tsCompare";
            tsCompare.Size = new System.Drawing.Size(94, 24);
            tsCompare.Text = "Compare";
            tsCompare.ToolTipText = "Select two rows to enable compare";
            tsCompare.Click += TsCompare_Click;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "is_enabled";
            dataGridViewTextBoxColumn2.HeaderText = "Enabled";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "classifier_function";
            dataGridViewTextBoxColumn3.HeaderText = "Classifier Function";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "reconfiguration_pending";
            dataGridViewTextBoxColumn4.HeaderText = "Reconfiguration Pending";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "max_outstanding_io_per_volume";
            dataGridViewTextBoxColumn5.HeaderText = "Max Outstanding IO Per Volume";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.ToolTipText = "Indicates that there was an error generating the script doe to pending reconfiguration";
            dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "ValidFrom";
            dataGridViewTextBoxColumn6.HeaderText = "Valid From";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "ValidTo";
            dataGridViewTextBoxColumn7.HeaderText = "Valid To";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "ValidTo";
            dataGridViewTextBoxColumn8.HeaderText = "Valid To";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "SnapshotDate";
            dataGridViewTextBoxColumn9.HeaderText = "Validated Date";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.Width = 125;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Filter_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // ResourceGovernor
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ResourceGovernor";
            Size = new System.Drawing.Size(895, 479);
            Load += ResourceGovernor_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.ToolStripButton tsCompare;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewLinkColumn colLinkInstance;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colClassifierFunction;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colReconfigurationPending;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colReconfigurationError;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxOutstandingIO;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidTo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSnapshotDate;
        private System.Windows.Forms.DataGridViewLinkColumn colScript;
        private System.Windows.Forms.DataGridViewLinkColumn colDiff;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
    }
}
