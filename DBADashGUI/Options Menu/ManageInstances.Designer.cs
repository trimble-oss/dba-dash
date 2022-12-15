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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManageInstances));
            this.dgv = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.txtSearch = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.showActiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showDeletedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showAzureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsFilterError = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIsAzure = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colDeleteRestore = new System.Windows.Forms.DataGridViewLinkColumn();
            this.colShowInSummary = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colInstance,
            this.colIsAzure,
            this.colDeleteRestore,
            this.colShowInSummary});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(621, 811);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellContentClick);
            this.dgv.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellValueChanged);
            this.dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.Dgv_RowsAdded);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn1.DataPropertyName = "ConnectionID";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "IsAzure";
            this.dataGridViewTextBoxColumn2.HeaderText = "Is Azure?";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 96;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.txtSearch,
            this.toolStripLabel1,
            this.toolStripDropDownButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(621, 27);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // txtSearch
            // 
            this.txtSearch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(150, 27);
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(56, 24);
            this.toolStripLabel1.Text = "Search:";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showActiveToolStripMenuItem,
            this.showDeletedToolStripMenuItem,
            this.showAzureToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            this.toolStripDropDownButton1.Text = "Filter";
            // 
            // showActiveToolStripMenuItem
            // 
            this.showActiveToolStripMenuItem.Checked = true;
            this.showActiveToolStripMenuItem.CheckOnClick = true;
            this.showActiveToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showActiveToolStripMenuItem.Name = "showActiveToolStripMenuItem";
            this.showActiveToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            this.showActiveToolStripMenuItem.Text = "Show Active";
            this.showActiveToolStripMenuItem.Click += new System.EventHandler(this.Filter_Click);
            // 
            // showDeletedToolStripMenuItem
            // 
            this.showDeletedToolStripMenuItem.Checked = true;
            this.showDeletedToolStripMenuItem.CheckOnClick = true;
            this.showDeletedToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showDeletedToolStripMenuItem.Name = "showDeletedToolStripMenuItem";
            this.showDeletedToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            this.showDeletedToolStripMenuItem.Text = "Show Deleted";
            this.showDeletedToolStripMenuItem.Click += new System.EventHandler(this.Filter_Click);
            // 
            // showAzureToolStripMenuItem
            // 
            this.showAzureToolStripMenuItem.Checked = true;
            this.showAzureToolStripMenuItem.CheckOnClick = true;
            this.showAzureToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showAzureToolStripMenuItem.Name = "showAzureToolStripMenuItem";
            this.showAzureToolStripMenuItem.Size = new System.Drawing.Size(185, 26);
            this.showAzureToolStripMenuItem.Text = "Show Azure";
            this.showAzureToolStripMenuItem.Click += new System.EventHandler(this.Filter_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsFilterError});
            this.statusStrip1.Location = new System.Drawing.Point(0, 838);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(621, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsFilterError
            // 
            this.tsFilterError.Name = "tsFilterError";
            this.tsFilterError.Size = new System.Drawing.Size(51, 20);
            this.tsFilterError.Text = "{Error}";
            this.tsFilterError.Visible = false;
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // colInstance
            // 
            this.colInstance.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colInstance.DataPropertyName = "InstanceDisplayName";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            // 
            // colIsAzure
            // 
            this.colIsAzure.DataPropertyName = "IsAzure";
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle1.NullValue = false;
            this.colIsAzure.DefaultCellStyle = dataGridViewCellStyle1;
            this.colIsAzure.HeaderText = "Is Azure?";
            this.colIsAzure.MinimumWidth = 6;
            this.colIsAzure.Name = "colIsAzure";
            this.colIsAzure.ReadOnly = true;
            this.colIsAzure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.colIsAzure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colIsAzure.Width = 80;
            // 
            // colDeleteRestore
            // 
            this.colDeleteRestore.HeaderText = "Delete/Restore";
            this.colDeleteRestore.MinimumWidth = 6;
            this.colDeleteRestore.Name = "colDeleteRestore";
            this.colDeleteRestore.Width = 109;
            // 
            // colShowInSummary
            // 
            this.colShowInSummary.DataPropertyName = "ShowInSummary";
            this.colShowInSummary.HeaderText = "Show In Summary?";
            this.colShowInSummary.MinimumWidth = 6;
            this.colShowInSummary.Name = "colShowInSummary";
            this.colShowInSummary.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.colShowInSummary.ToolTipText = "Uncheck to hide instance from Summary tab and other tabs at root level";
            this.colShowInSummary.Width = 125;
            // 
            // ManageInstances
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(621, 860);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ManageInstances";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Instances";
            this.Load += new System.EventHandler(this.ManageInstances_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.DataGridViewCheckBoxColumn colShowInSummary;
    }
}