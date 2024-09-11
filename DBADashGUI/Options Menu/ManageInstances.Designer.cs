namespace DBADashGUI
{
    partial class ManageInstances
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManageInstances));
            dgv = new System.Windows.Forms.DataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colIsAzure = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            colDeleteRestore = new System.Windows.Forms.DataGridViewLinkColumn();
            colHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            txtSearch = new System.Windows.Forms.ToolStripTextBox();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            showActiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showDeletedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showAzureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            tsFilterError = new System.Windows.Forms.ToolStripStatusLabel();
            timer1 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colIsAzure, colDeleteRestore, colHidden });
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(621, 811);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // colInstance
            // 
            colInstance.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colInstance.DataPropertyName = "InstanceDisplayName";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            // 
            // colIsAzure
            // 
            colIsAzure.DataPropertyName = "IsAzure";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle1.NullValue = false;
            colIsAzure.DefaultCellStyle = dataGridViewCellStyle1;
            colIsAzure.HeaderText = "Is Azure?";
            colIsAzure.MinimumWidth = 6;
            colIsAzure.Name = "colIsAzure";
            colIsAzure.ReadOnly = true;
            colIsAzure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colIsAzure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colIsAzure.Width = 80;
            // 
            // colDeleteRestore
            // 
            colDeleteRestore.HeaderText = "Delete/Restore";
            colDeleteRestore.MinimumWidth = 6;
            colDeleteRestore.Name = "colDeleteRestore";
            colDeleteRestore.Width = 109;
            // 
            // colHidden
            // 
            colHidden.DataPropertyName = "IsHidden";
            colHidden.HeaderText = "Hidden";
            colHidden.MinimumWidth = 6;
            colHidden.Name = "colHidden";
            colHidden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colHidden.ToolTipText = "Check to hide instance.  Instance won't be shown by default at root level but will still be available in the tree.";
            colHidden.Width = 125;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "IsAzure";
            dataGridViewTextBoxColumn2.HeaderText = "Is Azure?";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 96;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { txtSearch, toolStripLabel1, toolStripDropDownButton1 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(621, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // txtSearch
            // 
            txtSearch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(150, 27);
            txtSearch.TextChanged += TxtSearch_TextChanged;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(56, 24);
            toolStripLabel1.Text = "Search:";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showActiveToolStripMenuItem, showDeletedToolStripMenuItem, showAzureToolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.FilterDropdown_16x;
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            toolStripDropDownButton1.Text = "Filter";
            // 
            // showActiveToolStripMenuItem
            // 
            showActiveToolStripMenuItem.Checked = true;
            showActiveToolStripMenuItem.CheckOnClick = true;
            showActiveToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            showActiveToolStripMenuItem.Name = "showActiveToolStripMenuItem";
            showActiveToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            showActiveToolStripMenuItem.Text = "Show Active";
            showActiveToolStripMenuItem.Click += Filter_Click;
            // 
            // showDeletedToolStripMenuItem
            // 
            showDeletedToolStripMenuItem.Checked = true;
            showDeletedToolStripMenuItem.CheckOnClick = true;
            showDeletedToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            showDeletedToolStripMenuItem.Name = "showDeletedToolStripMenuItem";
            showDeletedToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            showDeletedToolStripMenuItem.Text = "Show Deleted";
            showDeletedToolStripMenuItem.Click += Filter_Click;
            // 
            // showAzureToolStripMenuItem
            // 
            showAzureToolStripMenuItem.Checked = true;
            showAzureToolStripMenuItem.CheckOnClick = true;
            showAzureToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            showAzureToolStripMenuItem.Name = "showAzureToolStripMenuItem";
            showAzureToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            showAzureToolStripMenuItem.Text = "Show Azure";
            showAzureToolStripMenuItem.Click += Filter_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsFilterError });
            statusStrip1.Location = new System.Drawing.Point(0, 838);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(621, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // tsFilterError
            // 
            tsFilterError.Name = "tsFilterError";
            tsFilterError.Size = new System.Drawing.Size(51, 20);
            tsFilterError.Text = "{Error}";
            tsFilterError.Visible = false;
            // 
            // timer1
            // 
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;
            // 
            // ManageInstances
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(621, 860);
            Controls.Add(dgv);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ManageInstances";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Manage Instances";
            Load += ManageInstances_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsFilterError;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem showActiveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showDeletedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showAzureToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colIsAzure;
        private System.Windows.Forms.DataGridViewLinkColumn colDeleteRestore;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colHidden;
    }
}