
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.tsCompare = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLinkInstance = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colClassifierFunction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReconfigurationPending = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colReconfigurationError = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colMaxOutstandingIO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSnapshotDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScript = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colDiff = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colLinkInstance,
            this.colEnabled,
            this.colClassifierFunction,
            this.colReconfigurationPending,
            this.colReconfigurationError,
            this.colMaxOutstandingIO,
            this.colValidFrom,
            this.colValidTo,
            this.colSnapshotDate,
            this.colScript,
            this.colDiff});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(895, 452);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.SelectionChanged += new System.EventHandler(this.dgv_SelectionChanged);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.tsBack,
            this.tsCompare});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(895, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
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
            // tsBack
            // 
            this.tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBack.Image = global::DBADashGUI.Properties.Resources.Previous_grey_16x;
            this.tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBack.Name = "tsBack";
            this.tsBack.Size = new System.Drawing.Size(29, 24);
            this.tsBack.Text = "Back";
            this.tsBack.Click += new System.EventHandler(this.tsBack_Click);
            // 
            // tsCompare
            // 
            this.tsCompare.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsCompare.Enabled = false;
            this.tsCompare.Image = global::DBADashGUI.Properties.Resources.Diff_16x;
            this.tsCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCompare.Name = "tsCompare";
            this.tsCompare.Size = new System.Drawing.Size(94, 24);
            this.tsCompare.Text = "Compare";
            this.tsCompare.ToolTipText = "Select two rows to enable compare";
            this.tsCompare.Click += new System.EventHandler(this.tsCompare_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 125;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "is_enabled";
            this.dataGridViewTextBoxColumn2.HeaderText = "Enabled";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 125;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "classifier_function";
            this.dataGridViewTextBoxColumn3.HeaderText = "Classifier Function";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 125;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "reconfiguration_pending";
            this.dataGridViewTextBoxColumn4.HeaderText = "Reconfiguration Pending";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 125;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "max_outstanding_io_per_volume";
            this.dataGridViewTextBoxColumn5.HeaderText = "Max Outstanding IO Per Volume";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.ToolTipText = "Indicates that there was an error generating the script doe to pending reconfigur" +
    "ation";
            this.dataGridViewTextBoxColumn5.Width = 125;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "ValidFrom";
            this.dataGridViewTextBoxColumn6.HeaderText = "Valid From";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 125;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "ValidTo";
            this.dataGridViewTextBoxColumn7.HeaderText = "Valid To";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 125;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "ValidTo";
            this.dataGridViewTextBoxColumn8.HeaderText = "Valid To";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 125;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.DataPropertyName = "SnapshotDate";
            this.dataGridViewTextBoxColumn9.HeaderText = "Validated Date";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.Width = 125;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "InstanceDisplayName";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Width = 125;
            // 
            // colLinkInstance
            // 
            this.colLinkInstance.DataPropertyName = "InstanceDisplayName";
            this.colLinkInstance.HeaderText = "Instance";
            this.colLinkInstance.MinimumWidth = 6;
            this.colLinkInstance.Name = "colLinkInstance";
            this.colLinkInstance.ReadOnly = true;
            this.colLinkInstance.Width = 125;
            // 
            // colEnabled
            // 
            this.colEnabled.DataPropertyName = "is_enabled";
            this.colEnabled.HeaderText = "Enabled";
            this.colEnabled.MinimumWidth = 6;
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.ReadOnly = true;
            this.colEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colEnabled.Width = 125;
            // 
            // colClassifierFunction
            // 
            this.colClassifierFunction.DataPropertyName = "classifier_function";
            this.colClassifierFunction.HeaderText = "Classifier Function";
            this.colClassifierFunction.MinimumWidth = 6;
            this.colClassifierFunction.Name = "colClassifierFunction";
            this.colClassifierFunction.ReadOnly = true;
            this.colClassifierFunction.Width = 125;
            // 
            // colReconfigurationPending
            // 
            this.colReconfigurationPending.DataPropertyName = "reconfiguration_pending";
            this.colReconfigurationPending.HeaderText = "Reconfiguration Pending";
            this.colReconfigurationPending.MinimumWidth = 6;
            this.colReconfigurationPending.Name = "colReconfigurationPending";
            this.colReconfigurationPending.ReadOnly = true;
            this.colReconfigurationPending.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colReconfigurationPending.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colReconfigurationPending.Width = 125;
            // 
            // colReconfigurationError
            // 
            this.colReconfigurationError.DataPropertyName = "reconfiguration_error";
            this.colReconfigurationError.HeaderText = "Reconfiguration Error";
            this.colReconfigurationError.MinimumWidth = 6;
            this.colReconfigurationError.Name = "colReconfigurationError";
            this.colReconfigurationError.ReadOnly = true;
            this.colReconfigurationError.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colReconfigurationError.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colReconfigurationError.ToolTipText = "Indicates that there was an error generating the script due to pending reconfigur" +
    "ation";
            this.colReconfigurationError.Width = 125;
            // 
            // colMaxOutstandingIO
            // 
            this.colMaxOutstandingIO.DataPropertyName = "max_outstanding_io_per_volume";
            this.colMaxOutstandingIO.HeaderText = "Max Outstanding IO Per Volume";
            this.colMaxOutstandingIO.MinimumWidth = 6;
            this.colMaxOutstandingIO.Name = "colMaxOutstandingIO";
            this.colMaxOutstandingIO.ReadOnly = true;
            this.colMaxOutstandingIO.Width = 125;
            // 
            // colValidFrom
            // 
            this.colValidFrom.DataPropertyName = "ValidFrom";
            this.colValidFrom.HeaderText = "Valid From";
            this.colValidFrom.MinimumWidth = 6;
            this.colValidFrom.Name = "colValidFrom";
            this.colValidFrom.ReadOnly = true;
            this.colValidFrom.Width = 125;
            // 
            // colValidTo
            // 
            this.colValidTo.DataPropertyName = "ValidTo";
            this.colValidTo.HeaderText = "Valid To";
            this.colValidTo.MinimumWidth = 6;
            this.colValidTo.Name = "colValidTo";
            this.colValidTo.ReadOnly = true;
            this.colValidTo.Width = 125;
            // 
            // colSnapshotDate
            // 
            this.colSnapshotDate.DataPropertyName = "SnapshotDate";
            this.colSnapshotDate.HeaderText = "Validated Date";
            this.colSnapshotDate.MinimumWidth = 6;
            this.colSnapshotDate.Name = "colSnapshotDate";
            this.colSnapshotDate.ReadOnly = true;
            this.colSnapshotDate.Width = 125;
            // 
            // colScript
            // 
            this.colScript.HeaderText = "View Script";
            this.colScript.MinimumWidth = 6;
            this.colScript.Name = "colScript";
            this.colScript.ReadOnly = true;
            this.colScript.Text = "View Script";
            this.colScript.UseColumnTextForLinkValue = true;
            this.colScript.Width = 125;
            // 
            // colDiff
            // 
            this.colDiff.HeaderText = "Diff";
            this.colDiff.MinimumWidth = 6;
            this.colDiff.Name = "colDiff";
            this.colDiff.ReadOnly = true;
            this.colDiff.Text = "Diff Previous";
            this.colDiff.UseColumnTextForLinkValue = true;
            this.colDiff.Width = 125;
            // 
            // ResourceGovernor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ResourceGovernor";
            this.Size = new System.Drawing.Size(895, 479);
            this.Load += new System.EventHandler(this.ResourceGovernor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
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
    }
}
