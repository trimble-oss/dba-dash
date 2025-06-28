namespace DBADashServiceConfig
{
    partial class PerformanceCounters
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PerformanceCounters));
            dgv = new System.Windows.Forms.DataGridView();
            colObjectName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colCounterName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colInstanceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colIsDefault = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colRemove = new System.Windows.Forms.DataGridViewLinkColumn();
            panel1 = new System.Windows.Forms.Panel();
            bttnPreview = new System.Windows.Forms.Button();
            bttnLoadDefaults = new System.Windows.Forms.Button();
            bttnReset = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            bttnSave = new System.Windows.Forms.Button();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            splitContainer2 = new System.Windows.Forms.SplitContainer();
            dgvAvailable = new System.Windows.Forms.DataGridView();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsChangeConnection = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            lblSearch = new System.Windows.Forms.ToolStripLabel();
            txtSearch = new System.Windows.Forms.ToolStripTextBox();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            bttnAdd = new System.Windows.Forms.Button();
            lblObjectName = new System.Windows.Forms.Label();
            txtObjectName = new System.Windows.Forms.TextBox();
            lblCounterName = new System.Windows.Forms.Label();
            txtCounterName = new System.Windows.Forms.TextBox();
            lblInstance = new System.Windows.Forms.Label();
            cboInstance = new System.Windows.Forms.ComboBox();
            chkAllInstances = new System.Windows.Forms.CheckBox();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAvailable).BeginInit();
            toolStrip2.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colObjectName, colCounterName, colInstanceName, colIsDefault, colRemove });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 25);
            dgv.Name = "dgv";
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.Size = new System.Drawing.Size(1262, 380);
            dgv.TabIndex = 0;
            dgv.CellContentClick += SelectedGrid_CellContentClick;
            // 
            // colObjectName
            // 
            colObjectName.DataPropertyName = "ObjectName";
            colObjectName.HeaderText = "Object Name";
            colObjectName.MinimumWidth = 6;
            colObjectName.Name = "colObjectName";
            colObjectName.Width = 125;
            // 
            // colCounterName
            // 
            colCounterName.DataPropertyName = "CounterName";
            colCounterName.HeaderText = "Counter Name";
            colCounterName.MinimumWidth = 6;
            colCounterName.Name = "colCounterName";
            colCounterName.Width = 125;
            // 
            // colInstanceName
            // 
            colInstanceName.DataPropertyName = "InstanceName";
            colInstanceName.HeaderText = "Instance";
            colInstanceName.MinimumWidth = 6;
            colInstanceName.Name = "colInstanceName";
            colInstanceName.Width = 125;
            // 
            // colIsDefault
            // 
            colIsDefault.DataPropertyName = "IsDefault";
            colIsDefault.HeaderText = "Is Default";
            colIsDefault.MinimumWidth = 6;
            colIsDefault.Name = "colIsDefault";
            colIsDefault.ReadOnly = true;
            colIsDefault.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colIsDefault.Width = 125;
            // 
            // colRemove
            // 
            colRemove.HeaderText = "Remove";
            colRemove.MinimumWidth = 6;
            colRemove.Name = "colRemove";
            colRemove.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colRemove.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colRemove.Text = "Remove";
            colRemove.UseColumnTextForLinkValue = true;
            colRemove.Width = 125;
            // 
            // panel1
            // 
            panel1.Controls.Add(bttnPreview);
            panel1.Controls.Add(bttnLoadDefaults);
            panel1.Controls.Add(bttnReset);
            panel1.Controls.Add(bttnCancel);
            panel1.Controls.Add(bttnSave);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 826);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1262, 61);
            panel1.TabIndex = 1;
            // 
            // bttnPreview
            // 
            bttnPreview.Location = new System.Drawing.Point(348, 20);
            bttnPreview.Name = "bttnPreview";
            bttnPreview.Size = new System.Drawing.Size(162, 29);
            bttnPreview.TabIndex = 4;
            bttnPreview.Text = "Preview";
            toolTip1.SetToolTip(bttnPreview, "Preview the performance counters collection.  This will also include any custom performance counters from DBADash_CustomPerformanceCounters procedure if available on this instance.");
            bttnPreview.UseVisualStyleBackColor = true;
            bttnPreview.Click += Preview_Click;
            // 
            // bttnLoadDefaults
            // 
            bttnLoadDefaults.Location = new System.Drawing.Point(180, 20);
            bttnLoadDefaults.Name = "bttnLoadDefaults";
            bttnLoadDefaults.Size = new System.Drawing.Size(162, 29);
            bttnLoadDefaults.TabIndex = 3;
            bttnLoadDefaults.Text = "Load Defaults";
            toolTip1.SetToolTip(bttnLoadDefaults, "Add all default performance counters to the selection");
            bttnLoadDefaults.UseVisualStyleBackColor = true;
            bttnLoadDefaults.Click += LoadDefaults_Click;
            // 
            // bttnReset
            // 
            bttnReset.Location = new System.Drawing.Point(12, 20);
            bttnReset.Name = "bttnReset";
            bttnReset.Size = new System.Drawing.Size(162, 29);
            bttnReset.TabIndex = 2;
            bttnReset.Text = "Reset to defaults";
            toolTip1.SetToolTip(bttnReset, "Replace performance counter selection with defaults.");
            bttnReset.UseVisualStyleBackColor = true;
            bttnReset.Click += Reset_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(1056, 20);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 1;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += bttnCancel_Click;
            // 
            // bttnSave
            // 
            bttnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnSave.Location = new System.Drawing.Point(1156, 20);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 0;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += Save_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgv);
            splitContainer1.Panel2.Controls.Add(toolStrip1);
            splitContainer1.Size = new System.Drawing.Size(1262, 826);
            splitContainer1.SplitterDistance = 417;
            splitContainer1.TabIndex = 2;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer2.Location = new System.Drawing.Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(dgvAvailable);
            splitContainer2.Panel1.Controls.Add(toolStrip2);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(bttnAdd);
            splitContainer2.Panel2.Controls.Add(lblObjectName);
            splitContainer2.Panel2.Controls.Add(txtObjectName);
            splitContainer2.Panel2.Controls.Add(lblCounterName);
            splitContainer2.Panel2.Controls.Add(txtCounterName);
            splitContainer2.Panel2.Controls.Add(lblInstance);
            splitContainer2.Panel2.Controls.Add(cboInstance);
            splitContainer2.Panel2.Controls.Add(chkAllInstances);
            splitContainer2.Size = new System.Drawing.Size(1262, 417);
            splitContainer2.SplitterDistance = 810;
            splitContainer2.TabIndex = 1;
            // 
            // dgvAvailable
            // 
            dgvAvailable.AllowUserToAddRows = false;
            dgvAvailable.AllowUserToDeleteRows = false;
            dgvAvailable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAvailable.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvAvailable.Location = new System.Drawing.Point(0, 27);
            dgvAvailable.MultiSelect = false;
            dgvAvailable.Name = "dgvAvailable";
            dgvAvailable.ReadOnly = true;
            dgvAvailable.RowHeadersVisible = false;
            dgvAvailable.RowHeadersWidth = 51;
            dgvAvailable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvAvailable.Size = new System.Drawing.Size(810, 390);
            dgvAvailable.TabIndex = 1;
            dgvAvailable.SelectionChanged += AvailableCounters_SelectionChanged;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsChangeConnection, toolStripSeparator1, lblSearch, txtSearch, toolStripLabel2 });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(810, 27);
            toolStrip2.TabIndex = 0;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsChangeConnection
            // 
            tsChangeConnection.Image = Properties.Resources.Connect_16x;
            tsChangeConnection.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsChangeConnection.Name = "tsChangeConnection";
            tsChangeConnection.Size = new System.Drawing.Size(162, 24);
            tsChangeConnection.Text = "Change Connection";
            tsChangeConnection.Click += ChangeConnection_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // lblSearch
            // 
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(53, 24);
            lblSearch.Text = "Search";
            // 
            // txtSearch
            // 
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(150, 27);
            txtSearch.TextChanged += Search_TextChanged;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(140, 24);
            toolStripLabel2.Text = "Available Counters";
            // 
            // bttnAdd
            // 
            bttnAdd.Location = new System.Drawing.Point(188, 206);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(94, 29);
            bttnAdd.TabIndex = 7;
            bttnAdd.Text = "Add";
            toolTip1.SetToolTip(bttnAdd, "Select a performance counter in the grid to the left and click Add to add the performance counter.");
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += Add_Click;
            // 
            // lblObjectName
            // 
            lblObjectName.AutoSize = true;
            lblObjectName.Location = new System.Drawing.Point(20, 12);
            lblObjectName.Name = "lblObjectName";
            lblObjectName.Size = new System.Drawing.Size(100, 20);
            lblObjectName.TabIndex = 6;
            lblObjectName.Text = "Object Name:";
            // 
            // txtObjectName
            // 
            txtObjectName.Location = new System.Drawing.Point(20, 35);
            txtObjectName.Name = "txtObjectName";
            txtObjectName.Size = new System.Drawing.Size(262, 27);
            txtObjectName.TabIndex = 5;
            // 
            // lblCounterName
            // 
            lblCounterName.AutoSize = true;
            lblCounterName.Location = new System.Drawing.Point(20, 68);
            lblCounterName.Name = "lblCounterName";
            lblCounterName.Size = new System.Drawing.Size(108, 20);
            lblCounterName.TabIndex = 4;
            lblCounterName.Text = "Counter Name:";
            // 
            // txtCounterName
            // 
            txtCounterName.Location = new System.Drawing.Point(20, 91);
            txtCounterName.Name = "txtCounterName";
            txtCounterName.Size = new System.Drawing.Size(263, 27);
            txtCounterName.TabIndex = 3;
            // 
            // lblInstance
            // 
            lblInstance.AutoSize = true;
            lblInstance.Location = new System.Drawing.Point(20, 126);
            lblInstance.Name = "lblInstance";
            lblInstance.Size = new System.Drawing.Size(66, 20);
            lblInstance.TabIndex = 2;
            lblInstance.Text = "Instance:";
            // 
            // cboInstance
            // 
            cboInstance.FormattingEnabled = true;
            cboInstance.Location = new System.Drawing.Point(20, 149);
            cboInstance.Name = "cboInstance";
            cboInstance.Size = new System.Drawing.Size(263, 28);
            cboInstance.TabIndex = 1;
            // 
            // chkAllInstances
            // 
            chkAllInstances.AutoSize = true;
            chkAllInstances.Location = new System.Drawing.Point(300, 151);
            chkAllInstances.Name = "chkAllInstances";
            chkAllInstances.Size = new System.Drawing.Size(49, 24);
            chkAllInstances.TabIndex = 0;
            chkAllInstances.Text = "All";
            chkAllInstances.UseVisualStyleBackColor = true;
            chkAllInstances.CheckedChanged += AllInstances_CheckedChanged;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLabel1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1262, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(134, 22);
            toolStripLabel1.Text = "Selected Counters";
            // 
            // PerformanceCounters
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1262, 887);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "PerformanceCounters";
            Text = "Performance Counters";
            Load += PerformanceCounters_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAvailable).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TextBox txtCounterName;
        private System.Windows.Forms.Label lblInstance;
        private System.Windows.Forms.ComboBox cboInstance;
        private System.Windows.Forms.CheckBox chkAllInstances;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.Label lblObjectName;
        private System.Windows.Forms.TextBox txtObjectName;
        private System.Windows.Forms.Label lblCounterName;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.DataGridView dgvAvailable;
        private System.Windows.Forms.ToolStripLabel lblSearch;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.Button bttnReset;
        private System.Windows.Forms.Button bttnLoadDefaults;
        private System.Windows.Forms.Button bttnPreview;
        private System.Windows.Forms.ToolStripButton tsChangeConnection;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colObjectName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCounterName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstanceName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colIsDefault;
        private System.Windows.Forms.DataGridViewLinkColumn colRemove;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}