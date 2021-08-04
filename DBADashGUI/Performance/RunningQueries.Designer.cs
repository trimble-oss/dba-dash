
namespace DBADashGUI.Performance
{
    partial class RunningQueries
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RunningQueries));
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.tsGetLatest = new System.Windows.Forms.ToolStripButton();
            this.lblSnapshotDate = new System.Windows.Forms.ToolStripLabel();
            this.tsBlocking = new System.Windows.Forms.ToolStripButton();
            this.tsGroupBy = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hostNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loginNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.objectNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.programToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.queryHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.queryPlanHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitResourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.planHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sQLHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 31);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(717, 331);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellContentClick);
            this.dgv.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgv_CellFormatting);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsExcel,
            this.tsBack,
            this.tsGetLatest,
            this.lblSnapshotDate,
            this.tsBlocking,
            this.tsGroupBy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(717, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "Group By";
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
            this.tsExcel.Text = "Export to Excel";
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
            // tsGetLatest
            // 
            this.tsGetLatest.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsGetLatest.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsGetLatest.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGetLatest.Name = "tsGetLatest";
            this.tsGetLatest.Size = new System.Drawing.Size(99, 24);
            this.tsGetLatest.Text = "Get Latest";
            this.tsGetLatest.Click += new System.EventHandler(this.tsGetLatest_Click);
            // 
            // lblSnapshotDate
            // 
            this.lblSnapshotDate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblSnapshotDate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSnapshotDate.Name = "lblSnapshotDate";
            this.lblSnapshotDate.Size = new System.Drawing.Size(115, 24);
            this.lblSnapshotDate.Text = "Snapshot Date:";
            this.lblSnapshotDate.Visible = false;
            // 
            // tsBlocking
            // 
            this.tsBlocking.Image = global::DBADashGUI.Properties.Resources.Table_16x;
            this.tsBlocking.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBlocking.Name = "tsBlocking";
            this.tsBlocking.Size = new System.Drawing.Size(130, 24);
            this.tsBlocking.Text = "Show Blocking";
            this.tsBlocking.Click += new System.EventHandler(this.tsBlocking_Click);
            // 
            // tsGroupBy
            // 
            this.tsGroupBy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsGroupBy.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.databaseToolStripMenuItem,
            this.hostNameToolStripMenuItem,
            this.loginNameToolStripMenuItem,
            this.objectNameToolStripMenuItem,
            this.planHandleToolStripMenuItem,
            this.programToolStripMenuItem,
            this.queryHashToolStripMenuItem,
            this.queryPlanHashToolStripMenuItem,
            this.sQLHandleToolStripMenuItem,
            this.statusToolStripMenuItem,
            this.waitResourceToolStripMenuItem,
            this.waitTypeToolStripMenuItem,
            this.toolStripSeparator1,
            this.noneToolStripMenuItem});
            this.tsGroupBy.Enabled = false;
            this.tsGroupBy.Image = ((System.Drawing.Image)(resources.GetObject("tsGroupBy.Image")));
            this.tsGroupBy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGroupBy.Name = "tsGroupBy";
            this.tsGroupBy.Size = new System.Drawing.Size(84, 28);
            this.tsGroupBy.Text = "Group By";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(236, 26);
            this.toolStripMenuItem1.Tag = "client_interface_name";
            this.toolStripMenuItem1.Text = "Client Interface Name";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // databaseToolStripMenuItem
            // 
            this.databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            this.databaseToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.databaseToolStripMenuItem.Tag = "database_name";
            this.databaseToolStripMenuItem.Text = "Database";
            this.databaseToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // hostNameToolStripMenuItem
            // 
            this.hostNameToolStripMenuItem.Name = "hostNameToolStripMenuItem";
            this.hostNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.hostNameToolStripMenuItem.Tag = "host_name";
            this.hostNameToolStripMenuItem.Text = "Host Name";
            this.hostNameToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // loginNameToolStripMenuItem
            // 
            this.loginNameToolStripMenuItem.Name = "loginNameToolStripMenuItem";
            this.loginNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.loginNameToolStripMenuItem.Tag = "login_name";
            this.loginNameToolStripMenuItem.Text = "Login Name";
            this.loginNameToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // objectNameToolStripMenuItem
            // 
            this.objectNameToolStripMenuItem.Name = "objectNameToolStripMenuItem";
            this.objectNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.objectNameToolStripMenuItem.Tag = "object_name";
            this.objectNameToolStripMenuItem.Text = "Object Name";
            this.objectNameToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // programToolStripMenuItem
            // 
            this.programToolStripMenuItem.Name = "programToolStripMenuItem";
            this.programToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.programToolStripMenuItem.Tag = "program_name";
            this.programToolStripMenuItem.Text = "Program";
            this.programToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // queryHashToolStripMenuItem
            // 
            this.queryHashToolStripMenuItem.Name = "queryHashToolStripMenuItem";
            this.queryHashToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.queryHashToolStripMenuItem.Tag = "query_hash";
            this.queryHashToolStripMenuItem.Text = "Query Hash";
            this.queryHashToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // queryPlanHashToolStripMenuItem
            // 
            this.queryPlanHashToolStripMenuItem.Name = "queryPlanHashToolStripMenuItem";
            this.queryPlanHashToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.queryPlanHashToolStripMenuItem.Tag = "query_plan_hash";
            this.queryPlanHashToolStripMenuItem.Text = "Query Plan Hash";
            this.queryPlanHashToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // statusToolStripMenuItem
            // 
            this.statusToolStripMenuItem.Name = "statusToolStripMenuItem";
            this.statusToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.statusToolStripMenuItem.Tag = "status";
            this.statusToolStripMenuItem.Text = "Status";
            this.statusToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // waitResourceToolStripMenuItem
            // 
            this.waitResourceToolStripMenuItem.Name = "waitResourceToolStripMenuItem";
            this.waitResourceToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.waitResourceToolStripMenuItem.Tag = "wait_resource";
            this.waitResourceToolStripMenuItem.Text = "Wait Resource";
            this.waitResourceToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // waitTypeToolStripMenuItem
            // 
            this.waitTypeToolStripMenuItem.Name = "waitTypeToolStripMenuItem";
            this.waitTypeToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.waitTypeToolStripMenuItem.Tag = "wait_type";
            this.waitTypeToolStripMenuItem.Text = "Wait Type";
            this.waitTypeToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(233, 6);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.noneToolStripMenuItem.Text = "None";
            this.noneToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // planHandleToolStripMenuItem
            // 
            this.planHandleToolStripMenuItem.Name = "planHandleToolStripMenuItem";
            this.planHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.planHandleToolStripMenuItem.Tag = "plan_handle";
            this.planHandleToolStripMenuItem.Text = "Plan Handle";
            this.planHandleToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // sQLHandleToolStripMenuItem
            // 
            this.sQLHandleToolStripMenuItem.Name = "sQLHandleToolStripMenuItem";
            this.sQLHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.sQLHandleToolStripMenuItem.Tag = "sql_handle";
            this.sQLHandleToolStripMenuItem.Text = "SQL Handle";
            this.sQLHandleToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
            // 
            // RunningQueries
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "RunningQueries";
            this.Size = new System.Drawing.Size(717, 362);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripLabel lblSnapshotDate;
        private System.Windows.Forms.ToolStripButton tsGetLatest;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsBlocking;
        private System.Windows.Forms.ToolStripDropDownButton tsGroupBy;
        private System.Windows.Forms.ToolStripMenuItem databaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loginNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem programToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem statusToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem waitTypeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem queryHashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem queryPlanHashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem hostNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem waitResourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem planHandleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sQLHandleToolStripMenuItem;
    }
}
