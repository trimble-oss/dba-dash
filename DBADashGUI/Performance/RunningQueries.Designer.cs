
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RunningQueries));
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsExcel = new System.Windows.Forms.ToolStripButton();
            this.tsBack = new System.Windows.Forms.ToolStripButton();
            this.tsGetLatest = new System.Windows.Forms.ToolStripButton();
            this.tsNext = new System.Windows.Forms.ToolStripButton();
            this.lblSnapshotDate = new System.Windows.Forms.ToolStripLabel();
            this.tsGroupBy = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hostNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loginNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.objectNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.planHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.programToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.queryHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.queryPlanHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sQLHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitResourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.waitTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsPrevious = new System.Windows.Forms.ToolStripButton();
            this.tsBlockingFilter = new System.Windows.Forms.ToolStripDropDownButton();
            this.showRootBlockersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearBlockingFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.dgvSessionWaits = new System.Windows.Forms.DataGridView();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.lblWaitsForSession = new System.Windows.Forms.ToolStripLabel();
            this.tsSessionWaitCopy = new System.Windows.Forms.ToolStripButton();
            this.tsSessionWaitExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.allSessionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.summaryViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsGroupByFilter = new System.Windows.Forms.ToolStripLabel();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSessionWaits)).BeginInit();
            this.toolStrip2.SuspendLayout();
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
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(1090, 380);
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
            this.tsNext,
            this.lblSnapshotDate,
            this.tsGroupBy,
            this.tsPrevious,
            this.tsBlockingFilter,
            this.tsGroupByFilter});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1090, 27);
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
            this.tsBack.Enabled = false;
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
            // tsNext
            // 
            this.tsNext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNext.Image = global::DBADashGUI.Properties.Resources.arrow_Forward_16xLG;
            this.tsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNext.Name = "tsNext";
            this.tsNext.Size = new System.Drawing.Size(29, 24);
            this.tsNext.Text = "Next Snapshot";
            this.tsNext.Click += new System.EventHandler(this.tsNext_Click);
            // 
            // lblSnapshotDate
            // 
            this.lblSnapshotDate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblSnapshotDate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblSnapshotDate.Name = "lblSnapshotDate";
            this.lblSnapshotDate.Size = new System.Drawing.Size(115, 24);
            this.lblSnapshotDate.Text = "Snapshot Date:";
            this.lblSnapshotDate.Visible = false;
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
            this.tsGroupBy.Size = new System.Drawing.Size(84, 24);
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
            // planHandleToolStripMenuItem
            // 
            this.planHandleToolStripMenuItem.Name = "planHandleToolStripMenuItem";
            this.planHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.planHandleToolStripMenuItem.Tag = "plan_handle";
            this.planHandleToolStripMenuItem.Text = "Plan Handle";
            this.planHandleToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
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
            // sQLHandleToolStripMenuItem
            // 
            this.sQLHandleToolStripMenuItem.Name = "sQLHandleToolStripMenuItem";
            this.sQLHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            this.sQLHandleToolStripMenuItem.Tag = "sql_handle";
            this.sQLHandleToolStripMenuItem.Text = "SQL Handle";
            this.sQLHandleToolStripMenuItem.Click += new System.EventHandler(this.tsGroupBy_Click);
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
            // tsPrevious
            // 
            this.tsPrevious.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPrevious.Image = global::DBADashGUI.Properties.Resources.arrow_back_16xLG;
            this.tsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPrevious.Name = "tsPrevious";
            this.tsPrevious.Size = new System.Drawing.Size(29, 24);
            this.tsPrevious.Text = "Previous Snapshot";
            this.tsPrevious.Click += new System.EventHandler(this.tsPrevious_Click);
            // 
            // tsBlockingFilter
            // 
            this.tsBlockingFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showRootBlockersToolStripMenuItem,
            this.clearBlockingFilterToolStripMenuItem});
            this.tsBlockingFilter.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsBlockingFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBlockingFilter.Name = "tsBlockingFilter";
            this.tsBlockingFilter.Size = new System.Drawing.Size(100, 24);
            this.tsBlockingFilter.Text = "Blocking";
            // 
            // showRootBlockersToolStripMenuItem
            // 
            this.showRootBlockersToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.showRootBlockersToolStripMenuItem.Name = "showRootBlockersToolStripMenuItem";
            this.showRootBlockersToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.showRootBlockersToolStripMenuItem.Text = "Show Root Blockers";
            this.showRootBlockersToolStripMenuItem.Click += new System.EventHandler(this.showRootBlockersToolStripMenuItem_Click);
            // 
            // clearBlockingFilterToolStripMenuItem
            // 
            this.clearBlockingFilterToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.clearBlockingFilterToolStripMenuItem.Name = "clearBlockingFilterToolStripMenuItem";
            this.clearBlockingFilterToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.clearBlockingFilterToolStripMenuItem.Text = "Clear Blocking Filter";
            this.clearBlockingFilterToolStripMenuItem.Click += new System.EventHandler(this.clearBlockingFilterToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dgv);
            this.splitContainer1.Panel1.Controls.Add(this.statusStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgvSessionWaits);
            this.splitContainer1.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer1.Size = new System.Drawing.Size(1090, 568);
            this.splitContainer1.SplitterDistance = 402;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 2;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 380);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1090, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsStatus
            // 
            this.tsStatus.Name = "tsStatus";
            this.tsStatus.Size = new System.Drawing.Size(0, 16);
            // 
            // dgvSessionWaits
            // 
            this.dgvSessionWaits.AllowUserToAddRows = false;
            this.dgvSessionWaits.AllowUserToDeleteRows = false;
            this.dgvSessionWaits.BackgroundColor = System.Drawing.Color.AliceBlue;
            this.dgvSessionWaits.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSessionWaits.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSessionWaits.Location = new System.Drawing.Point(0, 27);
            this.dgvSessionWaits.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvSessionWaits.Name = "dgvSessionWaits";
            this.dgvSessionWaits.ReadOnly = true;
            this.dgvSessionWaits.RowHeadersVisible = false;
            this.dgvSessionWaits.RowHeadersWidth = 51;
            this.dgvSessionWaits.RowTemplate.Height = 24;
            this.dgvSessionWaits.Size = new System.Drawing.Size(1090, 134);
            this.dgvSessionWaits.TabIndex = 0;
            this.dgvSessionWaits.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSessionWaits_CellContentClick);
            // 
            // toolStrip2
            // 
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblWaitsForSession,
            this.tsSessionWaitCopy,
            this.tsSessionWaitExcel,
            this.toolStripDropDownButton1});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(1090, 27);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // lblWaitsForSession
            // 
            this.lblWaitsForSession.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblWaitsForSession.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblWaitsForSession.Name = "lblWaitsForSession";
            this.lblWaitsForSession.Size = new System.Drawing.Size(159, 24);
            this.lblWaitsForSession.Text = "Waits For Session ID: ";
            // 
            // tsSessionWaitCopy
            // 
            this.tsSessionWaitCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSessionWaitCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsSessionWaitCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSessionWaitCopy.Name = "tsSessionWaitCopy";
            this.tsSessionWaitCopy.Size = new System.Drawing.Size(29, 24);
            this.tsSessionWaitCopy.Text = "Copy";
            this.tsSessionWaitCopy.Click += new System.EventHandler(this.tsSessionWaitCopy_Click);
            // 
            // tsSessionWaitExcel
            // 
            this.tsSessionWaitExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSessionWaitExcel.Image = global::DBADashGUI.Properties.Resources.excel16x16;
            this.tsSessionWaitExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSessionWaitExcel.Name = "tsSessionWaitExcel";
            this.tsSessionWaitExcel.Size = new System.Drawing.Size(29, 24);
            this.tsSessionWaitExcel.Text = "Excel";
            this.tsSessionWaitExcel.Click += new System.EventHandler(this.tsSessionWaitExcel_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allSessionsToolStripMenuItem,
            this.summaryViewToolStripMenuItem,
            this.sessionToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(34, 24);
            this.toolStripDropDownButton1.Text = "toolStripDropDownButton1";
            // 
            // allSessionsToolStripMenuItem
            // 
            this.allSessionsToolStripMenuItem.Name = "allSessionsToolStripMenuItem";
            this.allSessionsToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.allSessionsToolStripMenuItem.Text = "All Sessions";
            this.allSessionsToolStripMenuItem.Click += new System.EventHandler(this.allSessionsToolStripMenuItem_Click);
            // 
            // summaryViewToolStripMenuItem
            // 
            this.summaryViewToolStripMenuItem.Name = "summaryViewToolStripMenuItem";
            this.summaryViewToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.summaryViewToolStripMenuItem.Text = "Summary View";
            this.summaryViewToolStripMenuItem.Click += new System.EventHandler(this.summaryViewToolStripMenuItem_Click);
            // 
            // sessionToolStripMenuItem
            // 
            this.sessionToolStripMenuItem.Name = "sessionToolStripMenuItem";
            this.sessionToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.sessionToolStripMenuItem.Text = "Session ";
            this.sessionToolStripMenuItem.Click += new System.EventHandler(this.sessionToolStripMenuItem_Click);
            // 
            // tsGroupByFilter
            // 
            this.tsGroupByFilter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tsGroupByFilter.Name = "tsGroupByFilter";
            this.tsGroupByFilter.Size = new System.Drawing.Size(127, 24);
            this.tsGroupByFilter.Text = "{Group By Filter}";
            this.tsGroupByFilter.Visible = false;
            // 
            // RunningQueries
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "RunningQueries";
            this.Size = new System.Drawing.Size(1090, 595);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSessionWaits)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
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
        private System.Windows.Forms.ToolStripButton tsPrevious;
        private System.Windows.Forms.ToolStripButton tsNext;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.DataGridView dgvSessionWaits;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel lblWaitsForSession;
        private System.Windows.Forms.ToolStripButton tsSessionWaitCopy;
        private System.Windows.Forms.ToolStripButton tsSessionWaitExcel;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem allSessionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem summaryViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sessionToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsBlockingFilter;
        private System.Windows.Forms.ToolStripMenuItem showRootBlockersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearBlockingFilterToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ToolStripLabel tsGroupByFilter;
    }
}
