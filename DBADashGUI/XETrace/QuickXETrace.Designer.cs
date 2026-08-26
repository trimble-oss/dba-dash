namespace DBADashGUI.XETrace
{
    partial class QuickXETrace
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
            if (disposing)
            {
                components?.Dispose();
                _runTimer?.Dispose();
                _heartbeatTimer?.Dispose();
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
            components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QuickXETrace));
            cboEvent = new System.Windows.Forms.ComboBox();
            Filter = new DBADashGUI.Controls.IconGroupBox();
            dgvFilters = new DBADashGUI.CustomReports.DBADashDataGridView();
            bttnAddFilter = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            lblComparison = new System.Windows.Forms.Label();
            lblField = new System.Windows.Forms.Label();
            lblEvent = new System.Windows.Forms.Label();
            txtValue = new System.Windows.Forms.TextBox();
            cboComparison = new System.Windows.Forms.ComboBox();
            cboField = new System.Windows.Forms.ComboBox();
            cboUnit = new System.Windows.Forms.ComboBox();
            groupBox1 = new DBADashGUI.Controls.IconGroupBox();
            lnkGlobalFields = new System.Windows.Forms.LinkLabel();
            dgvEvents = new DBADashGUI.CustomReports.DBADashDataGridView();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtEventFilter = new System.Windows.Forms.TextBox();
            bttnAddEvent = new System.Windows.Forms.Button();
            cboOtherEvent = new System.Windows.Forms.ComboBox();
            chkErrorReported = new System.Windows.Forms.CheckBox();
            chkBatchCompleted = new System.Windows.Forms.CheckBox();
            chkRPC = new System.Windows.Forms.CheckBox();
            grpConfig = new DBADashGUI.Controls.IconGroupBox();
            maxDuration = new DBADashGUI.Pickers.DurationDropDown();
            checkBox4 = new System.Windows.Forms.CheckBox();
            label4 = new System.Windows.Forms.Label();
            lblTarget = new System.Windows.Forms.Label();
            cboTarget = new System.Windows.Forms.ComboBox();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsConfigure = new System.Windows.Forms.ToolStripButton();
            tsStartTrace = new System.Windows.Forms.ToolStripButton();
            tsStopTrace = new System.Windows.Forms.ToolStripButton();
            toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            tsSave = new System.Windows.Forms.ToolStripDropDownButton();
            savexelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveTemplateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsTemplates = new System.Windows.Forms.ToolStripDropDownButton();
            tsHistory = new System.Windows.Forms.ToolStripDropDownButton();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            panel1 = new System.Windows.Forms.Panel();
            grpInstances = new DBADashGUI.Controls.IconGroupBox();
            chkIncludeAg = new System.Windows.Forms.CheckBox();
            btnAddInstance = new System.Windows.Forms.Button();
            lblInstanceCount = new System.Windows.Forms.Label();
            clbInstances = new System.Windows.Forms.CheckedListBox();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            tsStatus = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            lblTime = new System.Windows.Forms.ToolStripStatusLabel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            Filter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFilters).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEvents).BeginInit();
            grpConfig.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel1.SuspendLayout();
            grpInstances.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // cboEvent
            // 
            cboEvent.FormattingEnabled = true;
            cboEvent.Location = new System.Drawing.Point(6, 54);
            cboEvent.Name = "cboEvent";
            cboEvent.Size = new System.Drawing.Size(151, 28);
            cboEvent.TabIndex = 0;
            // 
            // Filter
            // 
            Filter.Controls.Add(dgvFilters);
            Filter.Controls.Add(bttnAddFilter);
            Filter.Controls.Add(label1);
            Filter.Controls.Add(lblComparison);
            Filter.Controls.Add(lblField);
            Filter.Controls.Add(lblEvent);
            Filter.Controls.Add(cboUnit);
            Filter.Controls.Add(txtValue);
            Filter.Controls.Add(cboComparison);
            Filter.Controls.Add(cboField);
            Filter.Controls.Add(cboEvent);
            Filter.Location = new System.Drawing.Point(12, 582);
            Filter.Name = "Filter";
            Filter.Size = new System.Drawing.Size(733, 243);
            Filter.TabIndex = 1;
            Filter.TabStop = false;
            Filter.Text = "Filter";
            // 
            // dgvFilters
            // 
            dgvFilters.AllowUserToAddRows = false;
            dgvFilters.AllowUserToOrderColumns = true;
            dgvFilters.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvFilters.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvFilters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvFilters.DefaultCellStyle = dataGridViewCellStyle2;
            dgvFilters.EnableHeadersVisualStyles = false;
            dgvFilters.Location = new System.Drawing.Point(6, 88);
            dgvFilters.Name = "dgvFilters";
            dgvFilters.RowHeadersVisible = false;
            dgvFilters.RowHeadersWidth = 51;
            dgvFilters.Size = new System.Drawing.Size(715, 140);
            dgvFilters.TabIndex = 9;
            // 
            // bttnAddFilter
            // 
            bttnAddFilter.Location = new System.Drawing.Point(621, 52);
            bttnAddFilter.Name = "bttnAddFilter";
            bttnAddFilter.Size = new System.Drawing.Size(94, 29);
            bttnAddFilter.TabIndex = 8;
            bttnAddFilter.Text = "Add Filter";
            bttnAddFilter.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(477, 31);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(45, 20);
            label1.TabIndex = 7;
            label1.Text = "Value";
            // 
            // lblComparison
            // 
            lblComparison.AutoSize = true;
            lblComparison.Location = new System.Drawing.Point(320, 31);
            lblComparison.Name = "lblComparison";
            lblComparison.Size = new System.Drawing.Size(89, 20);
            lblComparison.TabIndex = 6;
            lblComparison.Text = "Comparison";
            // 
            // lblField
            // 
            lblField.AutoSize = true;
            lblField.Location = new System.Drawing.Point(163, 31);
            lblField.Name = "lblField";
            lblField.Size = new System.Drawing.Size(41, 20);
            lblField.TabIndex = 5;
            lblField.Text = "Field";
            // 
            // lblEvent
            // 
            lblEvent.AutoSize = true;
            lblEvent.Location = new System.Drawing.Point(6, 31);
            lblEvent.Name = "lblEvent";
            lblEvent.Size = new System.Drawing.Size(45, 20);
            lblEvent.TabIndex = 4;
            lblEvent.Text = "Event";
            // 
            // txtValue
            // 
            txtValue.Location = new System.Drawing.Point(477, 54);
            txtValue.Name = "txtValue";
            txtValue.Size = new System.Drawing.Size(125, 27);
            txtValue.TabIndex = 3;
            // 
            // cboComparison
            // 
            cboComparison.FormattingEnabled = true;
            cboComparison.Location = new System.Drawing.Point(320, 54);
            cboComparison.Name = "cboComparison";
            cboComparison.Size = new System.Drawing.Size(151, 28);
            cboComparison.TabIndex = 2;
            // 
            // cboField
            // 
            cboField.DropDownWidth = 300;
            cboField.FormattingEnabled = true;
            cboField.Location = new System.Drawing.Point(163, 54);
            cboField.Name = "cboField";
            cboField.Size = new System.Drawing.Size(151, 28);
            cboField.TabIndex = 1;
            //
            // cboUnit
            //
            cboUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboUnit.FormattingEnabled = true;
            cboUnit.Location = new System.Drawing.Point(551, 54);
            cboUnit.Name = "cboUnit";
            cboUnit.Size = new System.Drawing.Size(66, 28);
            cboUnit.TabIndex = 4;
            cboUnit.Visible = false;
            //
            // groupBox1
            //
            groupBox1.Controls.Add(lnkGlobalFields);
            groupBox1.Controls.Add(dgvEvents);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(txtEventFilter);
            groupBox1.Controls.Add(bttnAddEvent);
            groupBox1.Controls.Add(cboOtherEvent);
            groupBox1.Controls.Add(chkErrorReported);
            groupBox1.Controls.Add(chkBatchCompleted);
            groupBox1.Controls.Add(chkRPC);
            groupBox1.Location = new System.Drawing.Point(12, 300);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(733, 276);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "Events";
            // 
            // lnkGlobalFields
            // 
            lnkGlobalFields.AutoSize = true;
            lnkGlobalFields.Location = new System.Drawing.Point(561, 29);
            lnkGlobalFields.Name = "lnkGlobalFields";
            lnkGlobalFields.Size = new System.Drawing.Size(160, 20);
            lnkGlobalFields.TabIndex = 10;
            lnkGlobalFields.TabStop = true;
            lnkGlobalFields.Text = "Global Fields {number}";
            // 
            // dgvEvents
            // 
            dgvEvents.AllowUserToAddRows = false;
            dgvEvents.AllowUserToDeleteRows = false;
            dgvEvents.AllowUserToOrderColumns = true;
            dgvEvents.BackgroundColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(255, 255, 255);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvEvents.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvEvents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(211, 211, 216);
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvEvents.DefaultCellStyle = dataGridViewCellStyle4;
            dgvEvents.EnableHeadersVisualStyles = false;
            dgvEvents.Location = new System.Drawing.Point(12, 125);
            dgvEvents.Name = "dgvEvents";
            dgvEvents.RowHeadersVisible = false;
            dgvEvents.RowHeadersWidth = 51;
            dgvEvents.Size = new System.Drawing.Size(709, 140);
            dgvEvents.TabIndex = 9;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(157, 70);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(89, 20);
            label3.TabIndex = 8;
            label3.Text = "Other Event:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(13, 70);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(56, 20);
            label2.TabIndex = 7;
            label2.Text = "Search:";
            // 
            // txtEventFilter
            // 
            txtEventFilter.Location = new System.Drawing.Point(13, 92);
            txtEventFilter.Name = "txtEventFilter";
            txtEventFilter.Size = new System.Drawing.Size(125, 27);
            txtEventFilter.TabIndex = 6;
            // 
            // bttnAddEvent
            // 
            bttnAddEvent.Location = new System.Drawing.Point(316, 90);
            bttnAddEvent.Name = "bttnAddEvent";
            bttnAddEvent.Size = new System.Drawing.Size(94, 29);
            bttnAddEvent.TabIndex = 4;
            bttnAddEvent.Text = "Add Event";
            bttnAddEvent.UseVisualStyleBackColor = true;
            // 
            // cboOtherEvent
            // 
            cboOtherEvent.DropDownWidth = 300;
            cboOtherEvent.FormattingEnabled = true;
            cboOtherEvent.Location = new System.Drawing.Point(159, 91);
            cboOtherEvent.Name = "cboOtherEvent";
            cboOtherEvent.Size = new System.Drawing.Size(151, 28);
            cboOtherEvent.TabIndex = 3;
            // 
            // chkErrorReported
            // 
            chkErrorReported.AutoSize = true;
            chkErrorReported.Location = new System.Drawing.Point(326, 29);
            chkErrorReported.Name = "chkErrorReported";
            chkErrorReported.Size = new System.Drawing.Size(129, 24);
            chkErrorReported.TabIndex = 2;
            chkErrorReported.Text = "Error Reported";
            toolTip1.SetToolTip(chkErrorReported, resources.GetString("chkErrorReported.ToolTip"));
            chkErrorReported.UseVisualStyleBackColor = true;
            chkErrorReported.CheckedChanged += SelectErrorReported;
            // 
            // chkBatchCompleted
            // 
            chkBatchCompleted.AutoSize = true;
            chkBatchCompleted.Location = new System.Drawing.Point(162, 29);
            chkBatchCompleted.Name = "chkBatchCompleted";
            chkBatchCompleted.Size = new System.Drawing.Size(146, 24);
            chkBatchCompleted.TabIndex = 1;
            chkBatchCompleted.Text = "Batch Completed";
            toolTip1.SetToolTip(chkBatchCompleted, resources.GetString("chkBatchCompleted.ToolTip"));
            chkBatchCompleted.UseVisualStyleBackColor = true;
            // 
            // chkRPC
            // 
            chkRPC.Location = new System.Drawing.Point(13, 29);
            chkRPC.Name = "chkRPC";
            chkRPC.Size = new System.Drawing.Size(135, 24);
            chkRPC.TabIndex = 0;
            chkRPC.Text = "RPC Completed";
            toolTip1.SetToolTip(chkRPC, resources.GetString("chkRPC.ToolTip"));
            chkRPC.UseVisualStyleBackColor = true;
            // 
            // grpConfig
            // 
            grpConfig.Controls.Add(maxDuration);
            grpConfig.Controls.Add(checkBox4);
            grpConfig.Controls.Add(label4);
            grpConfig.Controls.Add(lblTarget);
            grpConfig.Controls.Add(cboTarget);
            grpConfig.Location = new System.Drawing.Point(12, 167);
            grpConfig.Name = "grpConfig";
            grpConfig.Size = new System.Drawing.Size(733, 127);
            grpConfig.TabIndex = 3;
            grpConfig.TabStop = false;
            grpConfig.Text = "XE Trace Configuration:";
            // 
            // maxDuration
            // 
            maxDuration.AllowDays = false;
            maxDuration.BackColor = System.Drawing.Color.FromArgb(241, 241, 246);
            maxDuration.ForeColor = System.Drawing.Color.FromArgb(0, 79, 131);
            maxDuration.IncludeSeconds = true;
            maxDuration.Location = new System.Drawing.Point(341, 39);
            maxDuration.MinimumSize = new System.Drawing.Size(380, 0);
            maxDuration.Name = "maxDuration";
            maxDuration.Size = new System.Drawing.Size(380, 34);
            maxDuration.TabIndex = 10;
            maxDuration.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBox4
            // 
            checkBox4.AutoSize = true;
            checkBox4.Location = new System.Drawing.Point(65, 73);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new System.Drawing.Size(109, 24);
            checkBox4.TabIndex = 9;
            checkBox4.Text = "Capture .xel";
            checkBox4.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(234, 42);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(106, 20);
            label4.TabIndex = 3;
            label4.Text = "Max Run Time:";
            // 
            // lblTarget
            // 
            lblTarget.AutoSize = true;
            lblTarget.Location = new System.Drawing.Point(6, 42);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new System.Drawing.Size(53, 20);
            lblTarget.TabIndex = 1;
            lblTarget.Text = "Target:";
            // 
            // cboTarget
            // 
            cboTarget.FormattingEnabled = true;
            cboTarget.Location = new System.Drawing.Point(65, 39);
            cboTarget.Name = "cboTarget";
            cboTarget.Size = new System.Drawing.Size(151, 28);
            cboTarget.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsConfigure, tsStartTrace, tsStopTrace, toolStripButton1, tsSave, tsTemplates, tsHistory });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1710, 27);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsConfigure
            // 
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(98, 24);
            tsConfigure.Text = "Configure";
            // 
            // tsStartTrace
            // 
            tsStartTrace.Image = Properties.Resources.StatusAnnotations_Play_32xLG_color;
            tsStartTrace.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsStartTrace.Name = "tsStartTrace";
            tsStartTrace.Size = new System.Drawing.Size(103, 24);
            tsStartTrace.Text = "Start Trace";
            // 
            // tsStopTrace
            // 
            tsStopTrace.Image = Properties.Resources.StatusAnnotations_Stop_32xLG_color;
            tsStopTrace.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsStopTrace.Name = "tsStopTrace";
            tsStopTrace.Size = new System.Drawing.Size(103, 24);
            tsStopTrace.Text = "Stop Trace";
            // 
            // toolStripButton1
            // 
            toolStripButton1.Image = Properties.Resources.Eraser_16x;
            toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new System.Drawing.Size(67, 24);
            toolStripButton1.Text = "Clear";
            // 
            // tsSave
            // 
            tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsSave.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { savexelToolStripMenuItem, saveTemplateToolStripMenuItem });
            tsSave.Image = Properties.Resources.Save_16x;
            tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSave.Name = "tsSave";
            tsSave.Size = new System.Drawing.Size(34, 24);
            tsSave.Text = "Save";
            // 
            // savexelToolStripMenuItem
            // 
            savexelToolStripMenuItem.Name = "savexelToolStripMenuItem";
            savexelToolStripMenuItem.Size = new System.Drawing.Size(216, 26);
            savexelToolStripMenuItem.Text = "Save *.xel";
            // 
            // saveTemplateToolStripMenuItem
            // 
            saveTemplateToolStripMenuItem.Name = "saveTemplateToolStripMenuItem";
            saveTemplateToolStripMenuItem.Size = new System.Drawing.Size(216, 26);
            saveTemplateToolStripMenuItem.Text = "Save as Template...";
            // 
            // tsTemplates
            // 
            tsTemplates.Image = Properties.Resources.SettingsOutline_16x;
            tsTemplates.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsTemplates.Name = "tsTemplates";
            tsTemplates.Size = new System.Drawing.Size(111, 24);
            tsTemplates.Text = "Templates";
            // 
            // tsHistory
            // 
            tsHistory.Image = Properties.Resources.history;
            tsHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsHistory.Name = "tsHistory";
            tsHistory.Size = new System.Drawing.Size(90, 24);
            tsHistory.Text = "History";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel1);
            splitContainer1.Size = new System.Drawing.Size(1710, 847);
            splitContainer1.SplitterDistance = 764;
            splitContainer1.TabIndex = 5;
            // 
            // panel1
            // 
            panel1.Controls.Add(grpInstances);
            panel1.Controls.Add(grpConfig);
            panel1.Controls.Add(Filter);
            panel1.Controls.Add(groupBox1);
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(764, 847);
            panel1.TabIndex = 6;
            // 
            // grpInstances
            // 
            grpInstances.Controls.Add(chkIncludeAg);
            grpInstances.Controls.Add(btnAddInstance);
            grpInstances.Controls.Add(lblInstanceCount);
            grpInstances.Controls.Add(clbInstances);
            grpInstances.Location = new System.Drawing.Point(12, 16);
            grpInstances.Name = "grpInstances";
            grpInstances.Size = new System.Drawing.Size(733, 145);
            grpInstances.TabIndex = 7;
            grpInstances.TabStop = false;
            grpInstances.Text = "Instances to Trace:";
            // 
            // chkIncludeAg
            // 
            chkIncludeAg.AutoSize = true;
            chkIncludeAg.Location = new System.Drawing.Point(6, 26);
            chkIncludeAg.Name = "chkIncludeAg";
            chkIncludeAg.Size = new System.Drawing.Size(158, 24);
            chkIncludeAg.TabIndex = 1;
            chkIncludeAg.Text = "Include AG replicas";
            chkIncludeAg.UseVisualStyleBackColor = true;
            // 
            // btnAddInstance
            // 
            btnAddInstance.Location = new System.Drawing.Point(200, 22);
            btnAddInstance.Name = "btnAddInstance";
            btnAddInstance.Size = new System.Drawing.Size(130, 29);
            btnAddInstance.TabIndex = 2;
            btnAddInstance.Text = "Add Instance...";
            btnAddInstance.UseVisualStyleBackColor = true;
            // 
            // lblInstanceCount
            // 
            lblInstanceCount.AutoSize = true;
            lblInstanceCount.Location = new System.Drawing.Point(345, 28);
            lblInstanceCount.Name = "lblInstanceCount";
            lblInstanceCount.Size = new System.Drawing.Size(127, 20);
            lblInstanceCount.TabIndex = 3;
            lblInstanceCount.Text = "Tracing 1 instance";
            // 
            // clbInstances
            // 
            clbInstances.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            clbInstances.CheckOnClick = true;
            clbInstances.IntegralHeight = false;
            clbInstances.Location = new System.Drawing.Point(6, 57);
            clbInstances.Name = "clbInstances";
            clbInstances.Size = new System.Drawing.Size(715, 82);
            clbInstances.TabIndex = 4;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsStatus, toolStripStatusLabel1, lblTime });
            statusStrip1.Location = new System.Drawing.Point(0, 874);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new System.Drawing.Size(1710, 26);
            statusStrip1.TabIndex = 6;
            statusStrip1.Text = "statusStrip1";
            // 
            // tsStatus
            // 
            tsStatus.Name = "tsStatus";
            tsStatus.Size = new System.Drawing.Size(47, 20);
            tsStatus.Text = "status";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(13, 20);
            toolStripStatusLabel1.Text = "|";
            // 
            // lblTime
            // 
            lblTime.Name = "lblTime";
            lblTime.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            lblTime.Size = new System.Drawing.Size(63, 20);
            lblTime.Text = "00:00:00";
            lblTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // QuickXETrace
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "QuickXETrace";
            Size = new System.Drawing.Size(1710, 900);
            Filter.ResumeLayout(false);
            Filter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFilters).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEvents).EndInit();
            grpConfig.ResumeLayout(false);
            grpConfig.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            grpInstances.ResumeLayout(false);
            grpInstances.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cboEvent;
        private DBADashGUI.Controls.IconGroupBox Filter;
        private System.Windows.Forms.Label lblEvent;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.ComboBox cboComparison;
        private System.Windows.Forms.ComboBox cboField;
        private System.Windows.Forms.Button bttnAddFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblComparison;
        private System.Windows.Forms.Label lblField;
        private DBADashGUI.Controls.IconGroupBox groupBox1;
        private System.Windows.Forms.Button bttnAddEvent;
        private System.Windows.Forms.ComboBox cboOtherEvent;
        private System.Windows.Forms.CheckBox chkErrorReported;
        private System.Windows.Forms.CheckBox chkBatchCompleted;
        private System.Windows.Forms.CheckBox chkRPC;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtEventFilter;
        private DBADashGUI.Controls.IconGroupBox grpConfig;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.ComboBox cboTarget;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsStartTrace;
        private System.Windows.Forms.ToolStripButton tsStopTrace;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripDropDownButton tsSave;
        private System.Windows.Forms.ToolStripMenuItem savexelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveTemplateToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton tsTemplates;
        private CustomReports.DBADashDataGridView dgvFilters;
        private System.Windows.Forms.ToolStripButton tsConfigure;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripDropDownButton tsHistory;
        private CustomReports.DBADashDataGridView dgvEvents;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblTime;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.LinkLabel lnkGlobalFields;
        private System.Windows.Forms.ToolTip toolTip1;
        private DBADashGUI.Controls.IconGroupBox grpInstances;
        private System.Windows.Forms.CheckBox chkIncludeAg;
        private System.Windows.Forms.Button btnAddInstance;
        private System.Windows.Forms.Label lblInstanceCount;
        private System.Windows.Forms.CheckedListBox clbInstances;
        private Pickers.DurationDropDown maxDuration;
        private System.Windows.Forms.ComboBox cboUnit;
    }
}
