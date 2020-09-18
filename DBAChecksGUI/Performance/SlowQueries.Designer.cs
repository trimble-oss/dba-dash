namespace DBAChecksGUI
{
    partial class SlowQueries
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SlowQueries));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgvSummary = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.minsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.instanceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clientToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usernameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.objectNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.databaseNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Grp = new System.Windows.Forms.DataGridViewLinkColumn();
            this._1to5 = new System.Windows.Forms.DataGridViewLinkColumn();
            this._5to10 = new System.Windows.Forms.DataGridViewLinkColumn();
            this._10to20 = new System.Windows.Forms.DataGridViewLinkColumn();
            this._20to30 = new System.Windows.Forms.DataGridViewLinkColumn();
            this._30to60 = new System.Windows.Forms.DataGridViewLinkColumn();
            this._1to5min = new System.Windows.Forms.DataGridViewLinkColumn();
            this._5to10min = new System.Windows.Forms.DataGridViewLinkColumn();
            this._10to30min = new System.Windows.Forms.DataGridViewLinkColumn();
            this._30to60min = new System.Windows.Forms.DataGridViewLinkColumn();
            this._1hrPlus = new System.Windows.Forms.DataGridViewLinkColumn();
            this.Total = new System.Windows.Forms.DataGridViewLinkColumn();
            this.TotalDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalCPU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalIO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TotalPhysicalIO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtInstance = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.txtClient = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.txtApp = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
            this.txtDatabase = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel5 = new System.Windows.Forms.ToolStripLabel();
            this.txtObject = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel6 = new System.Windows.Forms.ToolStripLabel();
            this.txtUser = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel7 = new System.Windows.Forms.ToolStripLabel();
            this.txtText = new System.Windows.Forms.ToolStripTextBox();
            this.appToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSummary
            // 
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgvSummary.BackgroundColor = System.Drawing.Color.White;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Grp,
            this._1to5,
            this._5to10,
            this._10to20,
            this._20to30,
            this._30to60,
            this._1to5min,
            this._5to10min,
            this._10to30min,
            this._30to60min,
            this._1hrPlus,
            this.Total,
            this.TotalDuration,
            this.TotalCPU,
            this.TotalIO,
            this.TotalPhysicalIO});
            this.dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSummary.Location = new System.Drawing.Point(0, 27);
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.RowHeadersVisible = false;
            this.dgvSummary.RowHeadersWidth = 51;
            this.dgvSummary.RowTemplate.Height = 24;
            this.dgvSummary.Size = new System.Drawing.Size(1829, 1071);
            this.dgvSummary.TabIndex = 0;
            this.dgvSummary.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSummary_CellContentClick);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTime,
            this.tsRefresh,
            this.tsGroup,
            this.toolStripLabel1,
            this.txtInstance,
            this.toolStripLabel2,
            this.txtClient,
            this.toolStripLabel3,
            this.txtApp,
            this.toolStripLabel4,
            this.txtDatabase,
            this.toolStripLabel5,
            this.txtObject,
            this.toolStripLabel6,
            this.txtUser,
            this.toolStripLabel7,
            this.txtText});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1829, 27);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsTime
            // 
            this.tsTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minsToolStripMenuItem,
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBAChecksGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(34, 24);
            this.tsTime.Text = "Time";
            // 
            // minsToolStripMenuItem
            // 
            this.minsToolStripMenuItem.Checked = true;
            this.minsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.minsToolStripMenuItem.Name = "minsToolStripMenuItem";
            this.minsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.minsToolStripMenuItem.Tag = "15";
            this.minsToolStripMenuItem.Text = "15 Mins";
            this.minsToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(224, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(224, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(224, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(224, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(224, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(224, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(221, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(224, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.tsCustom_Click);
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            // 
            // tsGroup
            // 
            this.tsGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsGroup.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.appToolStripMenuItem,
            this.clientToolStripMenuItem,
            this.databaseNameToolStripMenuItem,
            this.instanceToolStripMenuItem,
            this.objectNameToolStripMenuItem,
            this.usernameToolStripMenuItem});
            this.tsGroup.Image = ((System.Drawing.Image)(resources.GetObject("tsGroup.Image")));
            this.tsGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsGroup.Name = "tsGroup";
            this.tsGroup.Size = new System.Drawing.Size(84, 24);
            this.tsGroup.Text = "Group By";
            // 
            // instanceToolStripMenuItem
            // 
            this.instanceToolStripMenuItem.Checked = true;
            this.instanceToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.instanceToolStripMenuItem.Name = "instanceToolStripMenuItem";
            this.instanceToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.instanceToolStripMenuItem.Tag = "ConnectionID";
            this.instanceToolStripMenuItem.Text = "Instance";
            this.instanceToolStripMenuItem.Click += new System.EventHandler(this.GroupBy_Click);
            // 
            // clientToolStripMenuItem
            // 
            this.clientToolStripMenuItem.Name = "clientToolStripMenuItem";
            this.clientToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.clientToolStripMenuItem.Tag = "client_hostname";
            this.clientToolStripMenuItem.Text = "Client";
            this.clientToolStripMenuItem.Click += new System.EventHandler(this.GroupBy_Click);
            // 
            // usernameToolStripMenuItem
            // 
            this.usernameToolStripMenuItem.Name = "usernameToolStripMenuItem";
            this.usernameToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.usernameToolStripMenuItem.Tag = "username";
            this.usernameToolStripMenuItem.Text = "Username";
            this.usernameToolStripMenuItem.Click += new System.EventHandler(this.GroupBy_Click);
            // 
            // objectNameToolStripMenuItem
            // 
            this.objectNameToolStripMenuItem.Name = "objectNameToolStripMenuItem";
            this.objectNameToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.objectNameToolStripMenuItem.Tag = "object_name";
            this.objectNameToolStripMenuItem.Text = "Object Name";
            this.objectNameToolStripMenuItem.Click += new System.EventHandler(this.GroupBy_Click);
            // 
            // databaseNameToolStripMenuItem
            // 
            this.databaseNameToolStripMenuItem.Name = "databaseNameToolStripMenuItem";
            this.databaseNameToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.databaseNameToolStripMenuItem.Tag = "DatabaseName";
            this.databaseNameToolStripMenuItem.Text = "Database Name";
            this.databaseNameToolStripMenuItem.Click += new System.EventHandler(this.GroupBy_Click);
            // 
            // Grp
            // 
            this.Grp.DataPropertyName = "Grp";
            this.Grp.HeaderText = "Group";
            this.Grp.MinimumWidth = 6;
            this.Grp.Name = "Grp";
            this.Grp.ReadOnly = true;
            this.Grp.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Grp.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Grp.Width = 77;
            // 
            // _1to5
            // 
            this._1to5.DataPropertyName = "1-5 seconds";
            this._1to5.HeaderText = "1-5 Seconds";
            this._1to5.MinimumWidth = 6;
            this._1to5.Name = "_1to5";
            this._1to5.ReadOnly = true;
            this._1to5.Width = 85;
            // 
            // _5to10
            // 
            this._5to10.DataPropertyName = "5-10 seconds";
            this._5to10.HeaderText = "5-10 seconds";
            this._5to10.MinimumWidth = 6;
            this._5to10.Name = "_5to10";
            this._5to10.ReadOnly = true;
            this._5to10.Width = 90;
            // 
            // _10to20
            // 
            this._10to20.DataPropertyName = "10-20 seconds";
            this._10to20.HeaderText = "10-20 Seconds";
            this._10to20.MinimumWidth = 6;
            this._10to20.Name = "_10to20";
            this._10to20.ReadOnly = true;
            this._10to20.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this._10to20.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this._10to20.Width = 122;
            // 
            // _20to30
            // 
            this._20to30.DataPropertyName = "20-30 seconds";
            this._20to30.HeaderText = "20-30 Seconds";
            this._20to30.MinimumWidth = 6;
            this._20to30.Name = "_20to30";
            this._20to30.ReadOnly = true;
            this._20to30.Width = 99;
            // 
            // _30to60
            // 
            this._30to60.DataPropertyName = "30-60 seconds";
            this._30to60.HeaderText = "30-60 Seconds";
            this._30to60.MinimumWidth = 6;
            this._30to60.Name = "_30to60";
            this._30to60.ReadOnly = true;
            this._30to60.Width = 99;
            // 
            // _1to5min
            // 
            this._1to5min.DataPropertyName = "1-5 minutes";
            this._1to5min.HeaderText = "1-5 min";
            this._1to5min.MinimumWidth = 6;
            this._1to5min.Name = "_1to5min";
            this._1to5min.ReadOnly = true;
            this._1to5min.Width = 55;
            // 
            // _5to10min
            // 
            this._5to10min.DataPropertyName = "5-10 minutes";
            this._5to10min.HeaderText = "5-10 min";
            this._5to10min.MinimumWidth = 6;
            this._5to10min.Name = "_5to10min";
            this._5to10min.ReadOnly = true;
            this._5to10min.Width = 62;
            // 
            // _10to30min
            // 
            this._10to30min.DataPropertyName = "10-30 minutes";
            this._10to30min.HeaderText = "10-30 min";
            this._10to30min.MinimumWidth = 6;
            this._10to30min.Name = "_10to30min";
            this._10to30min.ReadOnly = true;
            this._10to30min.Width = 69;
            // 
            // _30to60min
            // 
            this._30to60min.DataPropertyName = "30-60 minutes";
            this._30to60min.HeaderText = "30-60 min";
            this._30to60min.MinimumWidth = 6;
            this._30to60min.Name = "_30to60min";
            this._30to60min.ReadOnly = true;
            this._30to60min.Width = 69;
            // 
            // _1hrPlus
            // 
            this._1hrPlus.DataPropertyName = "1hr+";
            this._1hrPlus.HeaderText = "1Hr+";
            this._1hrPlus.MinimumWidth = 6;
            this._1hrPlus.Name = "_1hrPlus";
            this._1hrPlus.ReadOnly = true;
            this._1hrPlus.Width = 45;
            // 
            // Total
            // 
            this.Total.DataPropertyName = "Total";
            this.Total.HeaderText = "Total";
            this.Total.MinimumWidth = 6;
            this.Total.Name = "Total";
            this.Total.ReadOnly = true;
            this.Total.Width = 46;
            // 
            // TotalDuration
            // 
            this.TotalDuration.DataPropertyName = "TotalDuration";
            dataGridViewCellStyle5.Format = "#,#,,.000";
            this.TotalDuration.DefaultCellStyle = dataGridViewCellStyle5;
            this.TotalDuration.HeaderText = "Total Duration (sec)";
            this.TotalDuration.MinimumWidth = 6;
            this.TotalDuration.Name = "TotalDuration";
            this.TotalDuration.ReadOnly = true;
            this.TotalDuration.Width = 149;
            // 
            // TotalCPU
            // 
            this.TotalCPU.DataPropertyName = "TotalCPU";
            dataGridViewCellStyle6.Format = "#,#,,.000";
            this.TotalCPU.DefaultCellStyle = dataGridViewCellStyle6;
            this.TotalCPU.HeaderText = "Total CPU (sec)";
            this.TotalCPU.MinimumWidth = 6;
            this.TotalCPU.Name = "TotalCPU";
            this.TotalCPU.ReadOnly = true;
            this.TotalCPU.Width = 126;
            // 
            // TotalIO
            // 
            this.TotalIO.DataPropertyName = "TotalIO";
            dataGridViewCellStyle7.Format = "N0";
            this.TotalIO.DefaultCellStyle = dataGridViewCellStyle7;
            this.TotalIO.HeaderText = "Total IO";
            this.TotalIO.MinimumWidth = 6;
            this.TotalIO.Name = "TotalIO";
            this.TotalIO.ReadOnly = true;
            this.TotalIO.Width = 81;
            // 
            // TotalPhysicalIO
            // 
            this.TotalPhysicalIO.DataPropertyName = "TotalPhysicalIO";
            dataGridViewCellStyle8.Format = "N0";
            this.TotalPhysicalIO.DefaultCellStyle = dataGridViewCellStyle8;
            this.TotalPhysicalIO.HeaderText = "Total Physical IO";
            this.TotalPhysicalIO.MinimumWidth = 6;
            this.TotalPhysicalIO.Name = "TotalPhysicalIO";
            this.TotalPhysicalIO.ReadOnly = true;
            this.TotalPhysicalIO.Width = 118;
            // 
            // txtInstance
            // 
            this.txtInstance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtInstance.Name = "txtInstance";
            this.txtInstance.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(66, 20);
            this.toolStripLabel1.Text = "Instance:";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(50, 20);
            this.toolStripLabel2.Text = "Client:";
            // 
            // txtClient
            // 
            this.txtClient.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtClient.Name = "txtClient";
            this.txtClient.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(40, 20);
            this.toolStripLabel3.Text = "App:";
            // 
            // txtApp
            // 
            this.txtApp.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtApp.Name = "txtApp";
            this.txtApp.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel4
            // 
            this.toolStripLabel4.Name = "toolStripLabel4";
            this.toolStripLabel4.Size = new System.Drawing.Size(75, 20);
            this.toolStripLabel4.Text = "Database:";
            // 
            // txtDatabase
            // 
            this.txtDatabase.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel5
            // 
            this.toolStripLabel5.Name = "toolStripLabel5";
            this.toolStripLabel5.Size = new System.Drawing.Size(56, 20);
            this.toolStripLabel5.Text = "Object:";
            // 
            // txtObject
            // 
            this.txtObject.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtObject.Name = "txtObject";
            this.txtObject.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel6
            // 
            this.toolStripLabel6.Name = "toolStripLabel6";
            this.toolStripLabel6.Size = new System.Drawing.Size(38, 20);
            this.toolStripLabel6.Text = "User";
            // 
            // txtUser
            // 
            this.txtUser.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(100, 27);
            // 
            // toolStripLabel7
            // 
            this.toolStripLabel7.Name = "toolStripLabel7";
            this.toolStripLabel7.Size = new System.Drawing.Size(36, 20);
            this.toolStripLabel7.Text = "Text";
            // 
            // txtText
            // 
            this.txtText.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtText.Name = "txtText";
            this.txtText.Size = new System.Drawing.Size(100, 27);
            // 
            // appToolStripMenuItem
            // 
            this.appToolStripMenuItem.Name = "appToolStripMenuItem";
            this.appToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.appToolStripMenuItem.Tag = "client_app_name";
            this.appToolStripMenuItem.Text = "App";
            // 
            // SlowQueries
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvSummary);
            this.Controls.Add(this.toolStrip1);
            this.Name = "SlowQueries";
            this.Size = new System.Drawing.Size(1829, 1098);
            this.Load += new System.EventHandler(this.SlowQueries_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSummary;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripMenuItem minsToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsGroup;
        private System.Windows.Forms.ToolStripMenuItem instanceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clientToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem usernameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem databaseNameToolStripMenuItem;
        private System.Windows.Forms.DataGridViewLinkColumn Grp;
        private System.Windows.Forms.DataGridViewLinkColumn _1to5;
        private System.Windows.Forms.DataGridViewLinkColumn _5to10;
        private System.Windows.Forms.DataGridViewLinkColumn _10to20;
        private System.Windows.Forms.DataGridViewLinkColumn _20to30;
        private System.Windows.Forms.DataGridViewLinkColumn _30to60;
        private System.Windows.Forms.DataGridViewLinkColumn _1to5min;
        private System.Windows.Forms.DataGridViewLinkColumn _5to10min;
        private System.Windows.Forms.DataGridViewLinkColumn _10to30min;
        private System.Windows.Forms.DataGridViewLinkColumn _30to60min;
        private System.Windows.Forms.DataGridViewLinkColumn _1hrPlus;
        private System.Windows.Forms.DataGridViewLinkColumn Total;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalCPU;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalIO;
        private System.Windows.Forms.DataGridViewTextBoxColumn TotalPhysicalIO;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox txtInstance;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripTextBox txtClient;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripTextBox txtApp;
        private System.Windows.Forms.ToolStripLabel toolStripLabel4;
        private System.Windows.Forms.ToolStripTextBox txtDatabase;
        private System.Windows.Forms.ToolStripLabel toolStripLabel5;
        private System.Windows.Forms.ToolStripTextBox txtObject;
        private System.Windows.Forms.ToolStripLabel toolStripLabel6;
        private System.Windows.Forms.ToolStripTextBox txtUser;
        private System.Windows.Forms.ToolStripLabel toolStripLabel7;
        private System.Windows.Forms.ToolStripTextBox txtText;
        private System.Windows.Forms.ToolStripMenuItem appToolStripMenuItem;
    }
}
