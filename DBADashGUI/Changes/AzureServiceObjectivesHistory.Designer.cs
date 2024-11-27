using DBADashGUI.CustomReports;

namespace DBADashGUI.Changes
{
    partial class AzureServiceObjectivesHistory
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            dgv = new DBADashDataGridView();
            colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEditionNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colServiceObjective = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colServiceObjectiveNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolNameNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsClearFilterDB = new System.Windows.Forms.ToolStripButton();
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            dgvPool = new DBADashDataGridView();
            colPoolInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolPoolName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolDTUOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolDTUNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolCPUOld = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolCPULimitNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colPoolValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colChangeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsRefreshPool = new System.Windows.Forms.ToolStripButton();
            tsCopyPool = new System.Windows.Forms.ToolStripButton();
            toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            tsPoolExcel = new System.Windows.Forms.ToolStripButton();
            tsClearFilterPool = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPool).BeginInit();
            toolStrip2.SuspendLayout();
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
            dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colInstance, colDB, colEdition, colEditionNew, colServiceObjective, colServiceObjectiveNew, colPoolName, colPoolNameNew, colValidFrom, colValidTo });
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
            dgv.Location = new System.Drawing.Point(0, 31);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.ReadOnly = true;
            dgv.ResultSetID = 0;
            dgv.ResultSetName = null;
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(1344, 440);
            dgv.TabIndex = 0;
            // 
            // colInstance
            // 
            colInstance.DataPropertyName = "Instance";
            colInstance.HeaderText = "Instance";
            colInstance.MinimumWidth = 6;
            colInstance.Name = "colInstance";
            colInstance.ReadOnly = true;
            colInstance.Width = 90;
            // 
            // colDB
            // 
            colDB.DataPropertyName = "db";
            colDB.HeaderText = "DB";
            colDB.MinimumWidth = 6;
            colDB.Name = "colDB";
            colDB.ReadOnly = true;
            colDB.Width = 56;
            // 
            // colEdition
            // 
            colEdition.DataPropertyName = "edition";
            colEdition.HeaderText = "Edition (Old)";
            colEdition.MinimumWidth = 6;
            colEdition.Name = "colEdition";
            colEdition.ReadOnly = true;
            colEdition.Width = 107;
            // 
            // colEditionNew
            // 
            colEditionNew.DataPropertyName = "new_edition";
            colEditionNew.HeaderText = "Edition (New)";
            colEditionNew.MinimumWidth = 6;
            colEditionNew.Name = "colEditionNew";
            colEditionNew.ReadOnly = true;
            colEditionNew.Width = 111;
            // 
            // colServiceObjective
            // 
            colServiceObjective.DataPropertyName = "service_objective";
            colServiceObjective.HeaderText = "Service Objective (Old)";
            colServiceObjective.MinimumWidth = 6;
            colServiceObjective.Name = "colServiceObjective";
            colServiceObjective.ReadOnly = true;
            colServiceObjective.Width = 138;
            // 
            // colServiceObjectiveNew
            // 
            colServiceObjectiveNew.DataPropertyName = "new_service_objective";
            colServiceObjectiveNew.HeaderText = "Service Objective (New)";
            colServiceObjectiveNew.MinimumWidth = 6;
            colServiceObjectiveNew.Name = "colServiceObjectiveNew";
            colServiceObjectiveNew.ReadOnly = true;
            colServiceObjectiveNew.Width = 138;
            // 
            // colPoolName
            // 
            colPoolName.DataPropertyName = "elastic_pool_name";
            colPoolName.HeaderText = "Pool Name";
            colPoolName.MinimumWidth = 6;
            colPoolName.Name = "colPoolName";
            colPoolName.ReadOnly = true;
            colPoolName.Width = 98;
            // 
            // colPoolNameNew
            // 
            colPoolNameNew.DataPropertyName = "new_elastic_pool_name";
            colPoolNameNew.HeaderText = "Pool Name (New)";
            colPoolNameNew.MinimumWidth = 6;
            colPoolNameNew.Name = "colPoolNameNew";
            colPoolNameNew.ReadOnly = true;
            colPoolNameNew.Width = 135;
            // 
            // colValidFrom
            // 
            colValidFrom.DataPropertyName = "ValidFrom";
            colValidFrom.HeaderText = "Old Config Valid From";
            colValidFrom.MinimumWidth = 6;
            colValidFrom.Name = "colValidFrom";
            colValidFrom.ReadOnly = true;
            colValidFrom.Width = 130;
            // 
            // colValidTo
            // 
            colValidTo.DataPropertyName = "ValidTo";
            colValidTo.HeaderText = "Change Date";
            colValidTo.MinimumWidth = 6;
            colValidTo.Name = "colValidTo";
            colValidTo.ReadOnly = true;
            colValidTo.Width = 110;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, toolStripLabel1, tsExcel, tsClearFilterDB });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip1.Size = new System.Drawing.Size(1344, 31);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefresh.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefresh.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
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
            tsCopy.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy";
            tsCopy.Click += TsCopy_Click;
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel1.Font = new System.Drawing.Font("Segoe UI", 0.5625F, System.Drawing.FontStyle.Bold);
            toolStripLabel1.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new System.Drawing.Size(33, 24);
            toolStripLabel1.Text = "DB Service Objectives History";
            // 
            // tsExcel
            // 
            tsExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcel.Image = Properties.Resources.excel16x16;
            tsExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsExcel.Name = "tsExcel";
            tsExcel.Size = new System.Drawing.Size(29, 24);
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsClearFilterDB
            // 
            tsClearFilterDB.Enabled = false;
            tsClearFilterDB.Image = Properties.Resources.Filter_16x;
            tsClearFilterDB.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterDB.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsClearFilterDB.Name = "tsClearFilterDB";
            tsClearFilterDB.Size = new System.Drawing.Size(104, 24);
            tsClearFilterDB.Text = "Clear Filter";
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            dataGridViewTextBoxColumn1.HeaderText = "Instance";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.DataPropertyName = "db";
            dataGridViewTextBoxColumn2.HeaderText = "DB";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 56;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "edition";
            dataGridViewTextBoxColumn3.HeaderText = "Edition (Old)";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 116;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "new_edition";
            dataGridViewTextBoxColumn4.HeaderText = "Edition (New)";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 121;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "service_objective";
            dataGridViewTextBoxColumn5.HeaderText = "Service Objective (Old)";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 138;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "service_objective_new";
            dataGridViewTextBoxColumn6.HeaderText = "Service Objective (New)";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 138;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "elastic_pool_name";
            dataGridViewTextBoxColumn7.HeaderText = "Pool Name";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 98;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "new_elastic_pool_name";
            dataGridViewTextBoxColumn8.HeaderText = "Pool Name (New)";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 135;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "ValidFrom";
            dataGridViewTextBoxColumn9.HeaderText = "Old Config Valid From";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 130;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "ValidTo";
            dataGridViewTextBoxColumn10.HeaderText = "Change Date";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 110;
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
            splitContainer1.Panel1.Controls.Add(dgv);
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvPool);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(1344, 942);
            splitContainer1.SplitterDistance = 471;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 2;
            // 
            // dgvPool
            // 
            dgvPool.AllowUserToAddRows = false;
            dgvPool.AllowUserToDeleteRows = false;
            dgvPool.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvPool.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvPool.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPool.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colPoolInstance, colPoolPoolName, colPoolDTUOld, colPoolDTUNew, colPoolCPUOld, colPoolCPULimitNew, colPoolValidFrom, colChangeDate });
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvPool.DefaultCellStyle = dataGridViewCellStyle4;
            dgvPool.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvPool.EnableHeadersVisualStyles = false;
            dgvPool.Location = new System.Drawing.Point(0, 31);
            dgvPool.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvPool.Name = "dgvPool";
            dgvPool.ReadOnly = true;
            dgvPool.ResultSetID = 0;
            dgvPool.ResultSetName = null;
            dgvPool.RowHeadersVisible = false;
            dgvPool.RowHeadersWidth = 51;
            dgvPool.Size = new System.Drawing.Size(1344, 435);
            dgvPool.TabIndex = 1;
            // 
            // colPoolInstance
            // 
            colPoolInstance.DataPropertyName = "Instance";
            colPoolInstance.HeaderText = "Instance";
            colPoolInstance.MinimumWidth = 6;
            colPoolInstance.Name = "colPoolInstance";
            colPoolInstance.ReadOnly = true;
            colPoolInstance.Width = 90;
            // 
            // colPoolPoolName
            // 
            colPoolPoolName.DataPropertyName = "elastic_pool_name";
            colPoolPoolName.HeaderText = "Pool Name";
            colPoolPoolName.MinimumWidth = 6;
            colPoolPoolName.Name = "colPoolPoolName";
            colPoolPoolName.ReadOnly = true;
            colPoolPoolName.Width = 98;
            // 
            // colPoolDTUOld
            // 
            colPoolDTUOld.DataPropertyName = "elastic_pool_dtu_limit_old";
            colPoolDTUOld.HeaderText = "DTU Limit (Old)";
            colPoolDTUOld.MinimumWidth = 6;
            colPoolDTUOld.Name = "colPoolDTUOld";
            colPoolDTUOld.ReadOnly = true;
            colPoolDTUOld.Width = 124;
            // 
            // colPoolDTUNew
            // 
            colPoolDTUNew.DataPropertyName = "elastic_pool_dtu_limit_new";
            colPoolDTUNew.HeaderText = "DTU Limit (New)";
            colPoolDTUNew.MinimumWidth = 6;
            colPoolDTUNew.Name = "colPoolDTUNew";
            colPoolDTUNew.ReadOnly = true;
            colPoolDTUNew.Width = 128;
            // 
            // colPoolCPUOld
            // 
            colPoolCPUOld.DataPropertyName = "elastic_pool_cpu_limit_old";
            colPoolCPUOld.HeaderText = "CPU Limit (Old)";
            colPoolCPUOld.MinimumWidth = 6;
            colPoolCPUOld.Name = "colPoolCPUOld";
            colPoolCPUOld.ReadOnly = true;
            colPoolCPUOld.Width = 123;
            // 
            // colPoolCPULimitNew
            // 
            colPoolCPULimitNew.DataPropertyName = "elastic_pool_cpu_limit_new";
            colPoolCPULimitNew.HeaderText = "CPU Limit (New)";
            colPoolCPULimitNew.MinimumWidth = 6;
            colPoolCPULimitNew.Name = "colPoolCPULimitNew";
            colPoolCPULimitNew.ReadOnly = true;
            colPoolCPULimitNew.Width = 127;
            // 
            // colPoolValidFrom
            // 
            colPoolValidFrom.DataPropertyName = "ValidFrom";
            colPoolValidFrom.HeaderText = "Old Config Valid From";
            colPoolValidFrom.MinimumWidth = 6;
            colPoolValidFrom.Name = "colPoolValidFrom";
            colPoolValidFrom.ReadOnly = true;
            colPoolValidFrom.Width = 130;
            // 
            // colChangeDate
            // 
            colChangeDate.DataPropertyName = "ValidTo";
            colChangeDate.HeaderText = "Change Date";
            colChangeDate.MinimumWidth = 6;
            colChangeDate.Name = "colChangeDate";
            colChangeDate.ReadOnly = true;
            colChangeDate.Width = 110;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefreshPool, tsCopyPool, toolStripLabel2, tsPoolExcel, tsClearFilterPool });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            toolStrip2.Size = new System.Drawing.Size(1344, 31);
            toolStrip2.TabIndex = 0;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsRefreshPool
            // 
            tsRefreshPool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshPool.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshPool.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsRefreshPool.Name = "tsRefreshPool";
            tsRefreshPool.Size = new System.Drawing.Size(29, 24);
            tsRefreshPool.Text = "toolStripButton1";
            tsRefreshPool.Click += TsRefreshPool_Click;
            // 
            // tsCopyPool
            // 
            tsCopyPool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyPool.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyPool.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsCopyPool.Name = "tsCopyPool";
            tsCopyPool.Size = new System.Drawing.Size(29, 24);
            tsCopyPool.Text = "toolStripButton1";
            tsCopyPool.Click += TsCopyPool_Click;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            toolStripLabel2.Font = new System.Drawing.Font("Segoe UI", 0.5625F, System.Drawing.FontStyle.Bold);
            toolStripLabel2.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new System.Drawing.Size(15, 24);
            toolStripLabel2.Text = "Pool History";
            // 
            // tsPoolExcel
            // 
            tsPoolExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPoolExcel.Image = Properties.Resources.excel16x16;
            tsPoolExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPoolExcel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsPoolExcel.Name = "tsPoolExcel";
            tsPoolExcel.Size = new System.Drawing.Size(29, 24);
            tsPoolExcel.Text = "Export Excel";
            tsPoolExcel.Click += TsPoolExcel_Click;
            // 
            // tsClearFilterPool
            // 
            tsClearFilterPool.Enabled = false;
            tsClearFilterPool.Image = Properties.Resources.Filter_16x;
            tsClearFilterPool.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClearFilterPool.Margin = new System.Windows.Forms.Padding(0, 2, 0, 5);
            tsClearFilterPool.Name = "tsClearFilterPool";
            tsClearFilterPool.Size = new System.Drawing.Size(104, 24);
            tsClearFilterPool.Text = "Clear Filter";
            // 
            // AzureServiceObjectivesHistory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AzureServiceObjectivesHistory";
            Size = new System.Drawing.Size(1344, 942);
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPool).EndInit();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DBADashDataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEditionNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServiceObjective;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServiceObjectiveNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolNameNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidTo;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DBADashDataGridView dgvPool;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolPoolName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolDTUOld;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolDTUNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolCPUOld;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolCPULimitNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colChangeDate;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsRefreshPool;
        private System.Windows.Forms.ToolStripButton tsCopyPool;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsPoolExcel;
        private System.Windows.Forms.ToolStripButton tsClearFilterDB;
        private System.Windows.Forms.ToolStripButton tsClearFilterPool;
    }
}
