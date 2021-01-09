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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.value_in_use = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.new_value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.new_value_in_use = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.is_dynamic = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.is_advanced = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.default_value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Minimum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Maximum = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.configuration1 = new DBADashGUI.Changes.Configuration();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.Name,
            this.Description,
            this.value,
            this.value_in_use,
            this.new_value,
            this.new_value_in_use,
            this.ValidFrom,
            this.ValidTo,
            this.is_dynamic,
            this.is_advanced,
            this.default_value,
            this.Minimum,
            this.Maximum});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(1678, 385);
            this.dgv.TabIndex = 0;
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "Instance";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // Name
            // 
            this.Name.DataPropertyName = "name";
            this.Name.HeaderText = "Name";
            this.Name.MinimumWidth = 6;
            this.Name.Name = "Name";
            this.Name.ReadOnly = true;
            this.Name.Width = 74;
            // 
            // Description
            // 
            this.Description.DataPropertyName = "description";
            this.Description.HeaderText = "Description";
            this.Description.MinimumWidth = 6;
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 108;
            // 
            // value
            // 
            this.value.DataPropertyName = "value";
            this.value.HeaderText = "Value";
            this.value.MinimumWidth = 6;
            this.value.Name = "value";
            this.value.ReadOnly = true;
            this.value.Width = 73;
            // 
            // value_in_use
            // 
            this.value_in_use.DataPropertyName = "value_in_use";
            this.value_in_use.HeaderText = "Value in Use";
            this.value_in_use.MinimumWidth = 6;
            this.value_in_use.Name = "value_in_use";
            this.value_in_use.ReadOnly = true;
            this.value_in_use.Width = 85;
            // 
            // new_value
            // 
            this.new_value.DataPropertyName = "new_value";
            this.new_value.HeaderText = "New Value";
            this.new_value.MinimumWidth = 6;
            this.new_value.Name = "new_value";
            this.new_value.ReadOnly = true;
            this.new_value.Width = 96;
            // 
            // new_value_in_use
            // 
            this.new_value_in_use.DataPropertyName = "new_value_in_use";
            this.new_value_in_use.HeaderText = "New Value In Use";
            this.new_value_in_use.MinimumWidth = 6;
            this.new_value_in_use.Name = "new_value_in_use";
            this.new_value_in_use.ReadOnly = true;
            this.new_value_in_use.Width = 113;
            // 
            // ValidFrom
            // 
            this.ValidFrom.DataPropertyName = "ValidFrom";
            this.ValidFrom.HeaderText = "Valid From";
            this.ValidFrom.MinimumWidth = 6;
            this.ValidFrom.Name = "ValidFrom";
            this.ValidFrom.ReadOnly = true;
            this.ValidFrom.Width = 96;
            // 
            // ValidTo
            // 
            this.ValidTo.DataPropertyName = "ValidTo";
            this.ValidTo.HeaderText = "Valid To";
            this.ValidTo.MinimumWidth = 6;
            this.ValidTo.Name = "ValidTo";
            this.ValidTo.ReadOnly = true;
            this.ValidTo.Width = 82;
            // 
            // is_dynamic
            // 
            this.is_dynamic.DataPropertyName = "is_dynamic";
            this.is_dynamic.HeaderText = "Is Dynamic";
            this.is_dynamic.MinimumWidth = 6;
            this.is_dynamic.Name = "is_dynamic";
            this.is_dynamic.ReadOnly = true;
            this.is_dynamic.Width = 74;
            // 
            // is_advanced
            // 
            this.is_advanced.DataPropertyName = "is_advanced";
            this.is_advanced.HeaderText = "Is Advanced";
            this.is_advanced.MinimumWidth = 6;
            this.is_advanced.Name = "is_advanced";
            this.is_advanced.ReadOnly = true;
            this.is_advanced.Width = 82;
            // 
            // default_value
            // 
            this.default_value.DataPropertyName = "default_value";
            this.default_value.HeaderText = "Default";
            this.default_value.MinimumWidth = 6;
            this.default_value.Name = "default_value";
            this.default_value.ReadOnly = true;
            this.default_value.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.default_value.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.default_value.Width = 59;
            // 
            // Minimum
            // 
            this.Minimum.DataPropertyName = "Minimum";
            this.Minimum.HeaderText = "Minimum";
            this.Minimum.MinimumWidth = 6;
            this.Minimum.Name = "Minimum";
            this.Minimum.ReadOnly = true;
            this.Minimum.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Minimum.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Minimum.Width = 69;
            // 
            // Maximum
            // 
            this.Maximum.DataPropertyName = "Maximum";
            this.Maximum.HeaderText = "Maximum";
            this.Maximum.MinimumWidth = 6;
            this.Maximum.Name = "Maximum";
            this.Maximum.ReadOnly = true;
            this.Maximum.Width = 72;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.configuration1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgv);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(1678, 832);
            this.splitContainer1.SplitterDistance = 416;
            this.splitContainer1.TabIndex = 2;
            // 
            // configuration1
            // 
            this.configuration1.BackColor = System.Drawing.Color.White;
            this.configuration1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configuration1.Location = new System.Drawing.Point(0, 0);
            this.configuration1.Name = "configuration1";
            this.configuration1.Size = new System.Drawing.Size(1678, 416);
            this.configuration1.TabIndex = 1;
            this.configuration1.Load += new System.EventHandler(this.configuration1_Load);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tsRefresh,
            this.tsCopy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1678, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(192, 24);
            this.toolStripLabel1.Text = "Configuration Change Log";
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
            // ConfigurationHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Size = new System.Drawing.Size(1678, 832);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name;
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
        private System.Windows.Forms.DataGridViewCheckBoxColumn Maximum;
        private Changes.Configuration configuration1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
    }
}
