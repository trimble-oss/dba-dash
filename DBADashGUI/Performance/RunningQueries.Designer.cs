
using DBADashGUI.CustomReports;

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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsCols = new System.Windows.Forms.ToolStripButton();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsGetLatest = new System.Windows.Forms.ToolStripButton();
            tsTriggerCollection = new System.Windows.Forms.ToolStripButton();
            tsNext = new System.Windows.Forms.ToolStripButton();
            lblSnapshotDate = new System.Windows.Forms.ToolStripLabel();
            tsGroupBy = new System.Windows.Forms.ToolStripDropDownButton();
            batchTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            commandToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dOPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hostNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            loginNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            objectNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            planHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            programToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            queryHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            queryPlanHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sQLHandleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            statusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            textToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            waitResourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            waitTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            workloadGroupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsPrevious = new System.Windows.Forms.ToolStripButton();
            tsBlockingFilter = new System.Windows.Forms.ToolStripDropDownButton();
            showRootBlockersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            blockedQueriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            blockingQueriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            blockedOrBlockingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            clearBlockingFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsGroupByFilter = new System.Windows.Forms.ToolStripLabel();
            tsClearFilter = new System.Windows.Forms.ToolStripButton();
            tsViewALL = new System.Windows.Forms.ToolStripButton();
            tsCursors = new System.Windows.Forms.ToolStripButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgv = new DBADashDataGridView();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            lblRowLimit = new System.Windows.Forms.ToolStripStatusLabel();
            tsEditLimit = new System.Windows.Forms.ToolStripStatusLabel();
            tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            dgvSessionWaits = new DBADashDataGridView();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            lblWaitsForSession = new System.Windows.Forms.ToolStripLabel();
            tsSessionWaitCopy = new System.Windows.Forms.ToolStripButton();
            tsSessionWaitExcel = new System.Windows.Forms.ToolStripButton();
            tsWaitsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            allSessionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            summaryViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSessionWaits).BeginInit();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsCols, tsBack, tsGetLatest, tsTriggerCollection, tsNext, lblSnapshotDate, tsGroupBy, tsPrevious, tsBlockingFilter, tsGroupByFilter, tsClearFilter, tsViewALL, tsCursors });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1271, 27);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "Group By";
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
            tsExcel.Text = "Export to Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsCols
            // 
            tsCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCols.Image = Properties.Resources.Column_16x;
            tsCols.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCols.Name = "tsCols";
            tsCols.Size = new System.Drawing.Size(29, 24);
            tsCols.Text = "Columns";
            tsCols.Click += TsCols_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Enabled = false;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // tsGetLatest
            // 
            tsGetLatest.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsGetLatest.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsGetLatest.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGetLatest.Name = "tsGetLatest";
            tsGetLatest.Size = new System.Drawing.Size(99, 24);
            tsGetLatest.Text = "Get Latest";
            tsGetLatest.Click += TsGetLatest_Click;
            // 
            // tsTriggerCollection
            // 
            tsTriggerCollection.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsTriggerCollection.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            tsTriggerCollection.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTriggerCollection.Name = "tsTriggerCollection";
            tsTriggerCollection.Size = new System.Drawing.Size(151, 24);
            tsTriggerCollection.Text = "Trigger Collection";
            tsTriggerCollection.ToolTipText = "Trigger Collection & Refresh\r\nThis avoids the need to wait for the next collection by sending a message to the service to trigger the collection to run immediately.";
            tsTriggerCollection.Click += TsTriggerCollection_Click;
            // 
            // tsNext
            // 
            tsNext.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsNext.Image = Properties.Resources.arrow_Forward_16xLG;
            tsNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsNext.Name = "tsNext";
            tsNext.Size = new System.Drawing.Size(29, 24);
            tsNext.Text = "Next Snapshot";
            tsNext.Click += TsNext_Click;
            // 
            // lblSnapshotDate
            // 
            lblSnapshotDate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblSnapshotDate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblSnapshotDate.Name = "lblSnapshotDate";
            lblSnapshotDate.Size = new System.Drawing.Size(115, 24);
            lblSnapshotDate.Text = "Snapshot Date:";
            lblSnapshotDate.Visible = false;
            // 
            // tsGroupBy
            // 
            tsGroupBy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsGroupBy.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { batchTextToolStripMenuItem, toolStripMenuItem1, commandToolStripMenuItem, contextInfoToolStripMenuItem, databaseToolStripMenuItem, dOPToolStripMenuItem, hostNameToolStripMenuItem, loginNameToolStripMenuItem, objectNameToolStripMenuItem, planHandleToolStripMenuItem, programToolStripMenuItem, queryHashToolStripMenuItem, queryPlanHashToolStripMenuItem, sQLHandleToolStripMenuItem, statusToolStripMenuItem, textToolStripMenuItem, waitResourceToolStripMenuItem, waitTypeToolStripMenuItem, workloadGroupToolStripMenuItem, toolStripSeparator1, noneToolStripMenuItem });
            tsGroupBy.Enabled = false;
            tsGroupBy.Image = (System.Drawing.Image)resources.GetObject("tsGroupBy.Image");
            tsGroupBy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGroupBy.Name = "tsGroupBy";
            tsGroupBy.Size = new System.Drawing.Size(84, 24);
            tsGroupBy.Text = "Group By";
            // 
            // batchTextToolStripMenuItem
            // 
            batchTextToolStripMenuItem.Name = "batchTextToolStripMenuItem";
            batchTextToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            batchTextToolStripMenuItem.Tag = "batch_text";
            batchTextToolStripMenuItem.Text = "Batch Text";
            batchTextToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(236, 26);
            toolStripMenuItem1.Tag = "client_interface_name";
            toolStripMenuItem1.Text = "Client Interface Name";
            toolStripMenuItem1.Click += TsGroupBy_Click;
            // 
            // commandToolStripMenuItem
            // 
            commandToolStripMenuItem.Name = "commandToolStripMenuItem";
            commandToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            commandToolStripMenuItem.Tag = "command";
            commandToolStripMenuItem.Text = "Command";
            commandToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // contextInfoToolStripMenuItem
            // 
            contextInfoToolStripMenuItem.Name = "contextInfoToolStripMenuItem";
            contextInfoToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            contextInfoToolStripMenuItem.Tag = "context_info";
            contextInfoToolStripMenuItem.Text = "Context Info";
            contextInfoToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // databaseToolStripMenuItem
            // 
            databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            databaseToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            databaseToolStripMenuItem.Tag = "database_name";
            databaseToolStripMenuItem.Text = "Database";
            databaseToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // dOPToolStripMenuItem
            // 
            dOPToolStripMenuItem.Name = "dOPToolStripMenuItem";
            dOPToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            dOPToolStripMenuItem.Tag = "dop";
            dOPToolStripMenuItem.Text = "DOP";
            dOPToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // hostNameToolStripMenuItem
            // 
            hostNameToolStripMenuItem.Name = "hostNameToolStripMenuItem";
            hostNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            hostNameToolStripMenuItem.Tag = "host_name";
            hostNameToolStripMenuItem.Text = "Host Name";
            hostNameToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // loginNameToolStripMenuItem
            // 
            loginNameToolStripMenuItem.Name = "loginNameToolStripMenuItem";
            loginNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            loginNameToolStripMenuItem.Tag = "login_name";
            loginNameToolStripMenuItem.Text = "Login Name";
            loginNameToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // objectNameToolStripMenuItem
            // 
            objectNameToolStripMenuItem.Name = "objectNameToolStripMenuItem";
            objectNameToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            objectNameToolStripMenuItem.Tag = "object_name";
            objectNameToolStripMenuItem.Text = "Object Name";
            objectNameToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // planHandleToolStripMenuItem
            // 
            planHandleToolStripMenuItem.Name = "planHandleToolStripMenuItem";
            planHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            planHandleToolStripMenuItem.Tag = "plan_handle";
            planHandleToolStripMenuItem.Text = "Plan Handle";
            planHandleToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // programToolStripMenuItem
            // 
            programToolStripMenuItem.Name = "programToolStripMenuItem";
            programToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            programToolStripMenuItem.Tag = "program_name";
            programToolStripMenuItem.Text = "Program";
            programToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // queryHashToolStripMenuItem
            // 
            queryHashToolStripMenuItem.Name = "queryHashToolStripMenuItem";
            queryHashToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            queryHashToolStripMenuItem.Tag = "query_hash";
            queryHashToolStripMenuItem.Text = "Query Hash";
            queryHashToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // queryPlanHashToolStripMenuItem
            // 
            queryPlanHashToolStripMenuItem.Name = "queryPlanHashToolStripMenuItem";
            queryPlanHashToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            queryPlanHashToolStripMenuItem.Tag = "query_plan_hash";
            queryPlanHashToolStripMenuItem.Text = "Query Plan Hash";
            queryPlanHashToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // sQLHandleToolStripMenuItem
            // 
            sQLHandleToolStripMenuItem.Name = "sQLHandleToolStripMenuItem";
            sQLHandleToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            sQLHandleToolStripMenuItem.Tag = "sql_handle";
            sQLHandleToolStripMenuItem.Text = "SQL Handle";
            sQLHandleToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // statusToolStripMenuItem
            // 
            statusToolStripMenuItem.Name = "statusToolStripMenuItem";
            statusToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            statusToolStripMenuItem.Tag = "status";
            statusToolStripMenuItem.Text = "Status";
            statusToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // textToolStripMenuItem
            // 
            textToolStripMenuItem.Name = "textToolStripMenuItem";
            textToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            textToolStripMenuItem.Tag = "text";
            textToolStripMenuItem.Text = "Text";
            textToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // waitResourceToolStripMenuItem
            // 
            waitResourceToolStripMenuItem.Name = "waitResourceToolStripMenuItem";
            waitResourceToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            waitResourceToolStripMenuItem.Tag = "wait_resource";
            waitResourceToolStripMenuItem.Text = "Wait Resource";
            waitResourceToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // waitTypeToolStripMenuItem
            // 
            waitTypeToolStripMenuItem.Name = "waitTypeToolStripMenuItem";
            waitTypeToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            waitTypeToolStripMenuItem.Tag = "wait_type";
            waitTypeToolStripMenuItem.Text = "Wait Type";
            waitTypeToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // workloadGroupToolStripMenuItem
            // 
            workloadGroupToolStripMenuItem.Name = "workloadGroupToolStripMenuItem";
            workloadGroupToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            workloadGroupToolStripMenuItem.Tag = "workload_group";
            workloadGroupToolStripMenuItem.Text = "Workload Group";
            workloadGroupToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(233, 6);
            // 
            // noneToolStripMenuItem
            // 
            noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            noneToolStripMenuItem.Size = new System.Drawing.Size(236, 26);
            noneToolStripMenuItem.Text = "None";
            noneToolStripMenuItem.Click += TsGroupBy_Click;
            // 
            // tsPrevious
            // 
            tsPrevious.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPrevious.Image = Properties.Resources.arrow_back_16xLG;
            tsPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPrevious.Name = "tsPrevious";
            tsPrevious.Size = new System.Drawing.Size(29, 24);
            tsPrevious.Text = "Previous Snapshot";
            tsPrevious.Click += TsPrevious_Click;
            // 
            // tsBlockingFilter
            // 
            tsBlockingFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showRootBlockersToolStripMenuItem, blockedQueriesToolStripMenuItem, blockingQueriesToolStripMenuItem, blockedOrBlockingToolStripMenuItem, toolStripMenuItem2, clearBlockingFilterToolStripMenuItem });
            tsBlockingFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsBlockingFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBlockingFilter.Name = "tsBlockingFilter";
            tsBlockingFilter.Size = new System.Drawing.Size(100, 24);
            tsBlockingFilter.Text = "Blocking";
            // 
            // showRootBlockersToolStripMenuItem
            // 
            showRootBlockersToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            showRootBlockersToolStripMenuItem.Name = "showRootBlockersToolStripMenuItem";
            showRootBlockersToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            showRootBlockersToolStripMenuItem.Text = "Root Blockers";
            showRootBlockersToolStripMenuItem.ToolTipText = "Queries holding locks that are required by other queries that are not themselves blocked waiting for locks held by another query.";
            showRootBlockersToolStripMenuItem.Click += ShowRootBlockersToolStripMenuItem_Click;
            // 
            // blockedQueriesToolStripMenuItem
            // 
            blockedQueriesToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            blockedQueriesToolStripMenuItem.Name = "blockedQueriesToolStripMenuItem";
            blockedQueriesToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            blockedQueriesToolStripMenuItem.Text = "Blocked Queries";
            blockedQueriesToolStripMenuItem.ToolTipText = "Queries waiting on locks held by other queries";
            blockedQueriesToolStripMenuItem.Click += BlockedQueriesToolStripMenuItem_Click;
            // 
            // blockingQueriesToolStripMenuItem
            // 
            blockingQueriesToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            blockingQueriesToolStripMenuItem.Name = "blockingQueriesToolStripMenuItem";
            blockingQueriesToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            blockingQueriesToolStripMenuItem.Text = "Blocking Queries";
            blockingQueriesToolStripMenuItem.ToolTipText = "Queries holding locks that are needed by other queries";
            blockingQueriesToolStripMenuItem.Click += BlockingQueriesToolStripMenuItem_Click;
            // 
            // blockedOrBlockingToolStripMenuItem
            // 
            blockedOrBlockingToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            blockedOrBlockingToolStripMenuItem.Name = "blockedOrBlockingToolStripMenuItem";
            blockedOrBlockingToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            blockedOrBlockingToolStripMenuItem.Text = "Blocked Or Blocking";
            blockedOrBlockingToolStripMenuItem.ToolTipText = "Queries either waiting for locks or holding locks required by other queries";
            blockedOrBlockingToolStripMenuItem.Click += BlockedOrBlockingToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(223, 6);
            // 
            // clearBlockingFilterToolStripMenuItem
            // 
            clearBlockingFilterToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            clearBlockingFilterToolStripMenuItem.Name = "clearBlockingFilterToolStripMenuItem";
            clearBlockingFilterToolStripMenuItem.Size = new System.Drawing.Size(226, 26);
            clearBlockingFilterToolStripMenuItem.Text = "Clear Blocking Filter";
            clearBlockingFilterToolStripMenuItem.Click += ClearBlockingFilterToolStripMenuItem_Click;
            // 
            // tsGroupByFilter
            // 
            tsGroupByFilter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsGroupByFilter.Name = "tsGroupByFilter";
            tsGroupByFilter.Size = new System.Drawing.Size(127, 24);
            tsGroupByFilter.Text = "{Group By Filter}";
            tsGroupByFilter.Visible = false;
            // 
            // tsClearFilter
            // 
            tsClearFilter.Enabled = false;
            tsClearFilter.Image = Properties.Resources.Eraser_16x;
            tsClearFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilter.Name = "tsClearFilter";
            tsClearFilter.Size = new System.Drawing.Size(104, 24);
            tsClearFilter.Text = "Clear Filter";
            // 
            // tsViewALL
            // 
            tsViewALL.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsViewALL.Image = (System.Drawing.Image)resources.GetObject("tsViewALL.Image");
            tsViewALL.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsViewALL.Name = "tsViewALL";
            tsViewALL.Size = new System.Drawing.Size(73, 24);
            tsViewALL.Text = "View ALL";
            tsViewALL.Click += tsViewALL_Click;
            // 
            // tsCursors
            // 
            tsCursors.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            tsCursors.Image = (System.Drawing.Image)resources.GetObject("tsCursors.Image");
            tsCursors.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCursors.Name = "tsCursors";
            tsCursors.Size = new System.Drawing.Size(61, 24);
            tsCursors.Text = "Cursors";
            tsCursors.Click += tsCursors_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(dgv);
            splitContainer1.Panel1.Controls.Add(statusStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvSessionWaits);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(1271, 660);
            splitContainer1.SplitterDistance = 466;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = true;
            dgv.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgv.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
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
            dgv.Location = new System.Drawing.Point(0, 0);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1271, 440);
            dgv.TabIndex = 2;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellFormatting += Dgv_CellFormatting;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblRowLimit, tsEditLimit, tsStatus });
            statusStrip1.Location = new System.Drawing.Point(0, 440);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1271, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblRowLimit
            // 
            lblRowLimit.ForeColor = System.Drawing.Color.Red;
            lblRowLimit.Name = "lblRowLimit";
            lblRowLimit.Size = new System.Drawing.Size(150, 20);
            lblRowLimit.Text = "Row Limit exceeded. ";
            lblRowLimit.Visible = false;
            // 
            // tsEditLimit
            // 
            tsEditLimit.IsLink = true;
            tsEditLimit.Name = "tsEditLimit";
            tsEditLimit.Size = new System.Drawing.Size(72, 20);
            tsEditLimit.Text = "Edit Limit";
            tsEditLimit.Click += TsEditLimit_Click;
            // 
            // tsStatus
            // 
            tsStatus.Name = "tsStatus";
            tsStatus.Size = new System.Drawing.Size(1184, 20);
            tsStatus.Spring = true;
            tsStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dgvSessionWaits
            // 
            dgvSessionWaits.AllowUserToAddRows = false;
            dgvSessionWaits.AllowUserToDeleteRows = false;
            dgvSessionWaits.AllowUserToOrderColumns = true;
            dgvSessionWaits.BackgroundColor = System.Drawing.Color.AliceBlue;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvSessionWaits.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvSessionWaits.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvSessionWaits.DefaultCellStyle = dataGridViewCellStyle4;
            dgvSessionWaits.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvSessionWaits.EnableHeadersVisualStyles = false;
            dgvSessionWaits.Location = new System.Drawing.Point(0, 27);
            dgvSessionWaits.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvSessionWaits.Name = "dgvSessionWaits";
            dgvSessionWaits.ReadOnly = true;
            dgvSessionWaits.RowHeadersVisible = false;
            dgvSessionWaits.RowHeadersWidth = 51;
            dgvSessionWaits.Size = new System.Drawing.Size(1271, 162);
            dgvSessionWaits.TabIndex = 0;
            dgvSessionWaits.CellContentClick += DgvSessionWaits_CellContentClick;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { lblWaitsForSession, tsSessionWaitCopy, tsSessionWaitExcel, tsWaitsFilter });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(1271, 27);
            toolStrip2.TabIndex = 1;
            toolStrip2.Text = "toolStrip2";
            // 
            // lblWaitsForSession
            // 
            lblWaitsForSession.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            lblWaitsForSession.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblWaitsForSession.Name = "lblWaitsForSession";
            lblWaitsForSession.Size = new System.Drawing.Size(159, 24);
            lblWaitsForSession.Text = "Waits For Session ID: ";
            // 
            // tsSessionWaitCopy
            // 
            tsSessionWaitCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSessionWaitCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsSessionWaitCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSessionWaitCopy.Name = "tsSessionWaitCopy";
            tsSessionWaitCopy.Size = new System.Drawing.Size(29, 24);
            tsSessionWaitCopy.Text = "Copy";
            tsSessionWaitCopy.Click += TsSessionWaitCopy_Click;
            // 
            // tsSessionWaitExcel
            // 
            tsSessionWaitExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSessionWaitExcel.Image = Properties.Resources.excel16x16;
            tsSessionWaitExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSessionWaitExcel.Name = "tsSessionWaitExcel";
            tsSessionWaitExcel.Size = new System.Drawing.Size(29, 24);
            tsSessionWaitExcel.Text = "Excel";
            tsSessionWaitExcel.Click += TsSessionWaitExcel_Click;
            // 
            // tsWaitsFilter
            // 
            tsWaitsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsWaitsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { allSessionsToolStripMenuItem, summaryViewToolStripMenuItem, sessionToolStripMenuItem });
            tsWaitsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsWaitsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsWaitsFilter.Name = "tsWaitsFilter";
            tsWaitsFilter.Size = new System.Drawing.Size(34, 24);
            tsWaitsFilter.Text = "Filter";
            // 
            // allSessionsToolStripMenuItem
            // 
            allSessionsToolStripMenuItem.Name = "allSessionsToolStripMenuItem";
            allSessionsToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            allSessionsToolStripMenuItem.Text = "All Sessions";
            allSessionsToolStripMenuItem.Click += AllSessionsToolStripMenuItem_Click;
            // 
            // summaryViewToolStripMenuItem
            // 
            summaryViewToolStripMenuItem.Name = "summaryViewToolStripMenuItem";
            summaryViewToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            summaryViewToolStripMenuItem.Text = "Summary View";
            summaryViewToolStripMenuItem.Click += SummaryViewToolStripMenuItem_Click;
            // 
            // sessionToolStripMenuItem
            // 
            sessionToolStripMenuItem.Name = "sessionToolStripMenuItem";
            sessionToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            sessionToolStripMenuItem.Text = "Session ";
            sessionToolStripMenuItem.Click += SessionToolStripMenuItem_Click;
            // 
            // RunningQueries
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "RunningQueries";
            Size = new System.Drawing.Size(1271, 687);
            Load += RunningQueries_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSessionWaits).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
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
        private DBADashDataGridView dgvSessionWaits;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripLabel lblWaitsForSession;
        private System.Windows.Forms.ToolStripButton tsSessionWaitCopy;
        private System.Windows.Forms.ToolStripButton tsSessionWaitExcel;
        private System.Windows.Forms.ToolStripDropDownButton tsWaitsFilter;
        private System.Windows.Forms.ToolStripMenuItem allSessionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem summaryViewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sessionToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsBlockingFilter;
        private System.Windows.Forms.ToolStripMenuItem showRootBlockersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearBlockingFilterToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ToolStripLabel tsGroupByFilter;
        private System.Windows.Forms.ToolStripButton tsCols;
        private System.Windows.Forms.ToolStripStatusLabel lblRowLimit;
        private System.Windows.Forms.ToolStripStatusLabel tsEditLimit;
        private System.Windows.Forms.ToolStripMenuItem blockedQueriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blockingQueriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blockedOrBlockingToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem contextInfoToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsTriggerCollection;
        private System.Windows.Forms.ToolStripButton tsClearFilter;
        private System.Windows.Forms.ToolStripButton tsViewALL;
        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStripButton tsCursors;
        private System.Windows.Forms.ToolStripMenuItem batchTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem textToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dOPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem commandToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem workloadGroupToolStripMenuItem;
    }
}
