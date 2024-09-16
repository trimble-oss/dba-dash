namespace DBADashGUI.AgentJobs
{
    partial class AgentJobsControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentJobsControl));
            dgvJobs = new System.Windows.Forms.DataGridView();
            colHistory = new System.Windows.Forms.DataGridViewLinkColumn();
            Acknowledge = new System.Windows.Forms.DataGridViewLinkColumn();
            Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IsLastFail = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            TimeSinceLastFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StepLastFailed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            LastSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            TimeSinceLastSucceeded = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAckDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FailCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SucceedCount24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FailCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            SucceedCount7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            JobStepFails24Hrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            JobStepFails7Days = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MaxDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMaxDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            AvgDurationSec = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colAvgDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ConfiguredLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Configure = new System.Windows.Forms.DataGridViewLinkColumn();
            tsJobs = new System.Windows.Forms.ToolStrip();
            tsRefresh = new System.Windows.Forms.ToolStripButton();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsExcel = new System.Windows.Forms.ToolStripButton();
            tsConfigure = new System.Windows.Forms.ToolStripDropDownButton();
            configureInstanceThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            configureRootThresholdsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            acknowledgeErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            statusFilterToolStrip1 = new StatusFilterToolStrip();
            dgvJobHistory = new System.Windows.Forms.DataGridView();
            colRunDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colRunEndDateTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colStepID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colStepName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMessageID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colRunStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colRunDurationSec = new System.Windows.Forms.DataGridViewLinkColumn();
            colRunDuration = new System.Windows.Forms.DataGridViewLinkColumn();
            colRetries = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colMessage = new System.Windows.Forms.DataGridViewLinkColumn();
            colViewSteps = new System.Windows.Forms.DataGridViewLinkColumn();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            jobStep1 = new JobStep();
            toolStrip2 = new System.Windows.Forms.ToolStrip();
            tsRefreshHistory = new System.Windows.Forms.ToolStripButton();
            tsCopyHistory = new System.Windows.Forms.ToolStripButton();
            tsExcelHistory = new System.Windows.Forms.ToolStripButton();
            tsFilter = new System.Windows.Forms.ToolStripDropDownButton();
            showJobStepsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            failedOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tsBack = new System.Windows.Forms.ToolStripButton();
            tsJobName = new System.Windows.Forms.ToolStripLabel();
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
            dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn28 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvJobs).BeginInit();
            tsJobs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvJobHistory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // dgvJobs
            // 
            dgvJobs.AllowUserToAddRows = false;
            dgvJobs.AllowUserToDeleteRows = false;
            dgvJobs.BackgroundColor = System.Drawing.Color.White;
            dgvJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvJobs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colHistory, Acknowledge, Instance, name, colEnabled, Description, LastFail, IsLastFail, TimeSinceLastFail, StepLastFailed, LastSucceeded, TimeSinceLastSucceeded, colAckDate, FailCount24Hrs, SucceedCount24Hrs, FailCount7Days, SucceedCount7Days, JobStepFails24Hrs, JobStepFails7Days, MaxDurationSec, colMaxDuration, AvgDurationSec, colAvgDuration, ConfiguredLevel, Configure });
            dgvJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvJobs.Location = new System.Drawing.Point(0, 0);
            dgvJobs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvJobs.Name = "dgvJobs";
            dgvJobs.ReadOnly = true;
            dgvJobs.RowHeadersVisible = false;
            dgvJobs.RowHeadersWidth = 51;
            dgvJobs.RowTemplate.Height = 24;
            dgvJobs.Size = new System.Drawing.Size(2197, 216);
            dgvJobs.TabIndex = 0;
            dgvJobs.CellContentClick += DgvJobs_CellContentClick;
            dgvJobs.RowsAdded += DgvJobs_RowsAdded;
            // 
            // colHistory
            // 
            colHistory.HeaderText = "History";
            colHistory.MinimumWidth = 6;
            colHistory.Name = "colHistory";
            colHistory.ReadOnly = true;
            colHistory.Text = "View History";
            colHistory.UseColumnTextForLinkValue = true;
            colHistory.Width = 58;
            // 
            // Acknowledge
            // 
            Acknowledge.HeaderText = "Acknowledge";
            Acknowledge.MinimumWidth = 6;
            Acknowledge.Name = "Acknowledge";
            Acknowledge.ReadOnly = true;
            Acknowledge.Text = "Acknowledge";
            Acknowledge.Width = 125;
            // 
            // Instance
            // 
            Instance.DataPropertyName = "InstanceDisplayName";
            Instance.HeaderText = "Instance";
            Instance.MinimumWidth = 6;
            Instance.Name = "Instance";
            Instance.ReadOnly = true;
            Instance.Width = 90;
            // 
            // name
            // 
            name.DataPropertyName = "name";
            name.HeaderText = "Job Name";
            name.MinimumWidth = 6;
            name.Name = "name";
            name.ReadOnly = true;
            name.Width = 101;
            // 
            // colEnabled
            // 
            colEnabled.DataPropertyName = "enabled";
            colEnabled.HeaderText = "Enabled";
            colEnabled.MinimumWidth = 6;
            colEnabled.Name = "colEnabled";
            colEnabled.ReadOnly = true;
            colEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colEnabled.Width = 125;
            // 
            // Description
            // 
            Description.DataPropertyName = "description";
            Description.HeaderText = "Description";
            Description.MinimumWidth = 6;
            Description.Name = "Description";
            Description.ReadOnly = true;
            Description.Width = 108;
            // 
            // LastFail
            // 
            LastFail.DataPropertyName = "LastFailed";
            LastFail.HeaderText = "Last Failed";
            LastFail.MinimumWidth = 6;
            LastFail.Name = "LastFail";
            LastFail.ReadOnly = true;
            LastFail.Width = 106;
            // 
            // IsLastFail
            // 
            IsLastFail.DataPropertyName = "IsLastFail";
            IsLastFail.HeaderText = "Is Last Fail?";
            IsLastFail.MinimumWidth = 6;
            IsLastFail.Name = "IsLastFail";
            IsLastFail.ReadOnly = true;
            IsLastFail.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            IsLastFail.Width = 89;
            // 
            // TimeSinceLastFail
            // 
            TimeSinceLastFail.DataPropertyName = "TimeSinceLastFailed";
            TimeSinceLastFail.HeaderText = "Time Since Last Fail";
            TimeSinceLastFail.MinimumWidth = 6;
            TimeSinceLastFail.Name = "TimeSinceLastFail";
            TimeSinceLastFail.ReadOnly = true;
            TimeSinceLastFail.Width = 130;
            // 
            // StepLastFailed
            // 
            StepLastFailed.DataPropertyName = "StepLastFailed";
            StepLastFailed.HeaderText = "Step Last Failed";
            StepLastFailed.MinimumWidth = 6;
            StepLastFailed.Name = "StepLastFailed";
            StepLastFailed.ReadOnly = true;
            StepLastFailed.Width = 125;
            // 
            // LastSucceeded
            // 
            LastSucceeded.DataPropertyName = "LastSucceeded";
            LastSucceeded.HeaderText = "Last Succeeded";
            LastSucceeded.MinimumWidth = 6;
            LastSucceeded.Name = "LastSucceeded";
            LastSucceeded.ReadOnly = true;
            LastSucceeded.Width = 127;
            // 
            // TimeSinceLastSucceeded
            // 
            TimeSinceLastSucceeded.DataPropertyName = "TimeSinceLastSucceeded";
            TimeSinceLastSucceeded.HeaderText = "Time Since Last Succeded";
            TimeSinceLastSucceeded.MinimumWidth = 6;
            TimeSinceLastSucceeded.Name = "TimeSinceLastSucceeded";
            TimeSinceLastSucceeded.ReadOnly = true;
            TimeSinceLastSucceeded.Width = 193;
            // 
            // colAckDate
            // 
            colAckDate.DataPropertyName = "AckDate";
            colAckDate.HeaderText = "Acknowledged Date";
            colAckDate.MinimumWidth = 6;
            colAckDate.Name = "colAckDate";
            colAckDate.ReadOnly = true;
            colAckDate.Width = 125;
            // 
            // FailCount24Hrs
            // 
            FailCount24Hrs.DataPropertyName = "FailCount24Hrs";
            FailCount24Hrs.HeaderText = "Fail Count (24Hrs)";
            FailCount24Hrs.MinimumWidth = 6;
            FailCount24Hrs.Name = "FailCount24Hrs";
            FailCount24Hrs.ReadOnly = true;
            FailCount24Hrs.Width = 139;
            // 
            // SucceedCount24Hrs
            // 
            SucceedCount24Hrs.DataPropertyName = "SucceedCount24Hrs";
            SucceedCount24Hrs.HeaderText = "Succeed Count (24Hrs)";
            SucceedCount24Hrs.MinimumWidth = 6;
            SucceedCount24Hrs.Name = "SucceedCount24Hrs";
            SucceedCount24Hrs.ReadOnly = true;
            SucceedCount24Hrs.Width = 126;
            // 
            // FailCount7Days
            // 
            FailCount7Days.DataPropertyName = "FailCount7Days";
            FailCount7Days.HeaderText = "Fail Count (7 Days)";
            FailCount7Days.MinimumWidth = 6;
            FailCount7Days.Name = "FailCount7Days";
            FailCount7Days.ReadOnly = true;
            FailCount7Days.Width = 111;
            // 
            // SucceedCount7Days
            // 
            SucceedCount7Days.DataPropertyName = "SucceedCount7Days";
            SucceedCount7Days.HeaderText = "Succeed Count (7 Days)";
            SucceedCount7Days.MinimumWidth = 6;
            SucceedCount7Days.Name = "SucceedCount7Days";
            SucceedCount7Days.ReadOnly = true;
            SucceedCount7Days.Width = 141;
            // 
            // JobStepFails24Hrs
            // 
            JobStepFails24Hrs.DataPropertyName = "JobStepFails24Hrs";
            JobStepFails24Hrs.HeaderText = "Job Step Fails (24Hrs)";
            JobStepFails24Hrs.MinimumWidth = 6;
            JobStepFails24Hrs.Name = "JobStepFails24Hrs";
            JobStepFails24Hrs.ReadOnly = true;
            JobStepFails24Hrs.Width = 119;
            // 
            // JobStepFails7Days
            // 
            JobStepFails7Days.DataPropertyName = "JobStepFails7Days";
            JobStepFails7Days.HeaderText = "Job Step Fails (7 Days)";
            JobStepFails7Days.MinimumWidth = 6;
            JobStepFails7Days.Name = "JobStepFails7Days";
            JobStepFails7Days.ReadOnly = true;
            JobStepFails7Days.Width = 135;
            // 
            // MaxDurationSec
            // 
            MaxDurationSec.DataPropertyName = "MaxDurationSec";
            MaxDurationSec.HeaderText = "Max Duration (sec)";
            MaxDurationSec.MinimumWidth = 6;
            MaxDurationSec.Name = "MaxDurationSec";
            MaxDurationSec.ReadOnly = true;
            MaxDurationSec.Width = 143;
            // 
            // colMaxDuration
            // 
            colMaxDuration.DataPropertyName = "MaxDuration";
            colMaxDuration.HeaderText = "Max Duration";
            colMaxDuration.MinimumWidth = 6;
            colMaxDuration.Name = "colMaxDuration";
            colMaxDuration.ReadOnly = true;
            colMaxDuration.Width = 125;
            // 
            // AvgDurationSec
            // 
            AvgDurationSec.DataPropertyName = "AvgDurationSec";
            AvgDurationSec.HeaderText = "Avg Duration (sec)";
            AvgDurationSec.MinimumWidth = 6;
            AvgDurationSec.Name = "AvgDurationSec";
            AvgDurationSec.ReadOnly = true;
            AvgDurationSec.Width = 142;
            // 
            // colAvgDuration
            // 
            colAvgDuration.DataPropertyName = "AvgDuration";
            colAvgDuration.HeaderText = "Avg Duration";
            colAvgDuration.MinimumWidth = 6;
            colAvgDuration.Name = "colAvgDuration";
            colAvgDuration.ReadOnly = true;
            colAvgDuration.Width = 125;
            // 
            // ConfiguredLevel
            // 
            ConfiguredLevel.DataPropertyName = "ConfiguredLevel";
            ConfiguredLevel.HeaderText = "Configured Level";
            ConfiguredLevel.MinimumWidth = 6;
            ConfiguredLevel.Name = "ConfiguredLevel";
            ConfiguredLevel.ReadOnly = true;
            ConfiguredLevel.Width = 132;
            // 
            // Configure
            // 
            Configure.HeaderText = "Configure";
            Configure.MinimumWidth = 6;
            Configure.Name = "Configure";
            Configure.ReadOnly = true;
            Configure.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            Configure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            Configure.Text = "Configure";
            Configure.UseColumnTextForLinkValue = true;
            Configure.Width = 98;
            // 
            // tsJobs
            // 
            tsJobs.ImageScalingSize = new System.Drawing.Size(20, 20);
            tsJobs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefresh, tsCopy, tsExcel, tsConfigure, statusFilterToolStrip1 });
            tsJobs.Location = new System.Drawing.Point(0, 0);
            tsJobs.Name = "tsJobs";
            tsJobs.Size = new System.Drawing.Size(2197, 27);
            tsJobs.TabIndex = 3;
            tsJobs.Text = "toolStrip1";
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
            tsExcel.Text = "Export Excel";
            tsExcel.Click += TsExcel_Click;
            // 
            // tsConfigure
            // 
            tsConfigure.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsConfigure.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { configureInstanceThresholdsToolStripMenuItem, configureRootThresholdsToolStripMenuItem, acknowledgeErrorsToolStripMenuItem });
            tsConfigure.Image = Properties.Resources.SettingsOutline_16x;
            tsConfigure.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsConfigure.Name = "tsConfigure";
            tsConfigure.Size = new System.Drawing.Size(34, 24);
            tsConfigure.Text = "Configure";
            // 
            // configureInstanceThresholdsToolStripMenuItem
            // 
            configureInstanceThresholdsToolStripMenuItem.Name = "configureInstanceThresholdsToolStripMenuItem";
            configureInstanceThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureInstanceThresholdsToolStripMenuItem.Text = "Configure Instance Thresholds";
            configureInstanceThresholdsToolStripMenuItem.Click += ConfigureInstanceThresholdsToolStripMenuItem_Click;
            // 
            // configureRootThresholdsToolStripMenuItem
            // 
            configureRootThresholdsToolStripMenuItem.Name = "configureRootThresholdsToolStripMenuItem";
            configureRootThresholdsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            configureRootThresholdsToolStripMenuItem.Text = "Configure Root Thresholds";
            configureRootThresholdsToolStripMenuItem.Click += ConfigureRootThresholdsToolStripMenuItem_Click;
            // 
            // acknowledgeErrorsToolStripMenuItem
            // 
            acknowledgeErrorsToolStripMenuItem.Name = "acknowledgeErrorsToolStripMenuItem";
            acknowledgeErrorsToolStripMenuItem.Size = new System.Drawing.Size(290, 26);
            acknowledgeErrorsToolStripMenuItem.Text = "Acknowledge Errors";
            acknowledgeErrorsToolStripMenuItem.Click += AcknowledgeErrorsToolStripMenuItem_Click;
            // 
            // statusFilterToolStrip1
            // 
            statusFilterToolStrip1.Acknowledged = true;
            statusFilterToolStrip1.AcknowledgedVisible = true;
            statusFilterToolStrip1.Critical = true;
            statusFilterToolStrip1.CriticalVisible = true;
            statusFilterToolStrip1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            statusFilterToolStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            statusFilterToolStrip1.Image = (System.Drawing.Image)resources.GetObject("statusFilterToolStrip1.Image");
            statusFilterToolStrip1.ImageTransparentColor = System.Drawing.Color.Magenta;
            statusFilterToolStrip1.NA = true;
            statusFilterToolStrip1.Name = "statusFilterToolStrip1";
            statusFilterToolStrip1.NAVisible = true;
            statusFilterToolStrip1.OK = true;
            statusFilterToolStrip1.OKVisible = true;
            statusFilterToolStrip1.Size = new System.Drawing.Size(67, 24);
            statusFilterToolStrip1.Text = "ALL";
            statusFilterToolStrip1.Warning = true;
            statusFilterToolStrip1.WarningVisible = true;
            statusFilterToolStrip1.UserChangedStatusFilter += UserChangedStatusFilter;
            // 
            // dgvJobHistory
            // 
            dgvJobHistory.AllowUserToAddRows = false;
            dgvJobHistory.AllowUserToDeleteRows = false;
            dgvJobHistory.BackgroundColor = System.Drawing.Color.White;
            dgvJobHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvJobHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colRunDateTime, colRunEndDateTime, colStepID, colStepName, colMessageID, colSeverity, colRunStatus, colRunDurationSec, colRunDuration, colRetries, colMessage, colViewSteps });
            dgvJobHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvJobHistory.Location = new System.Drawing.Point(0, 27);
            dgvJobHistory.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgvJobHistory.Name = "dgvJobHistory";
            dgvJobHistory.ReadOnly = true;
            dgvJobHistory.RowHeadersVisible = false;
            dgvJobHistory.RowHeadersWidth = 51;
            dgvJobHistory.RowTemplate.Height = 24;
            dgvJobHistory.Size = new System.Drawing.Size(2197, 200);
            dgvJobHistory.TabIndex = 4;
            dgvJobHistory.CellContentClick += DgvJobHistory_CellContentClick;
            dgvJobHistory.RowsAdded += DgvJobHistory_RowsAdded;
            // 
            // colRunDateTime
            // 
            colRunDateTime.DataPropertyName = "RunDateTime";
            colRunDateTime.HeaderText = "Start";
            colRunDateTime.MinimumWidth = 6;
            colRunDateTime.Name = "colRunDateTime";
            colRunDateTime.ReadOnly = true;
            colRunDateTime.Width = 125;
            // 
            // colRunEndDateTime
            // 
            colRunEndDateTime.DataPropertyName = "RunEndDateTime";
            colRunEndDateTime.HeaderText = "Finish";
            colRunEndDateTime.MinimumWidth = 6;
            colRunEndDateTime.Name = "colRunEndDateTime";
            colRunEndDateTime.ReadOnly = true;
            colRunEndDateTime.Width = 125;
            // 
            // colStepID
            // 
            colStepID.DataPropertyName = "step_id";
            colStepID.HeaderText = "Step ID";
            colStepID.MinimumWidth = 6;
            colStepID.Name = "colStepID";
            colStepID.ReadOnly = true;
            colStepID.Width = 125;
            // 
            // colStepName
            // 
            colStepName.DataPropertyName = "step_name";
            colStepName.HeaderText = "Step Name";
            colStepName.MinimumWidth = 6;
            colStepName.Name = "colStepName";
            colStepName.ReadOnly = true;
            colStepName.Width = 125;
            // 
            // colMessageID
            // 
            colMessageID.DataPropertyName = "sql_message_id";
            colMessageID.HeaderText = "Message ID";
            colMessageID.MinimumWidth = 6;
            colMessageID.Name = "colMessageID";
            colMessageID.ReadOnly = true;
            colMessageID.Width = 125;
            // 
            // colSeverity
            // 
            colSeverity.DataPropertyName = "sql_severity";
            colSeverity.HeaderText = "Severity";
            colSeverity.MinimumWidth = 6;
            colSeverity.Name = "colSeverity";
            colSeverity.ReadOnly = true;
            colSeverity.Width = 125;
            // 
            // colRunStatus
            // 
            colRunStatus.DataPropertyName = "run_status_description";
            colRunStatus.HeaderText = "Status";
            colRunStatus.MinimumWidth = 6;
            colRunStatus.Name = "colRunStatus";
            colRunStatus.ReadOnly = true;
            colRunStatus.Width = 125;
            // 
            // colRunDurationSec
            // 
            colRunDurationSec.DataPropertyName = "RunDurationSec";
            colRunDurationSec.HeaderText = "Duration (sec)";
            colRunDurationSec.MinimumWidth = 6;
            colRunDurationSec.Name = "colRunDurationSec";
            colRunDurationSec.ReadOnly = true;
            colRunDurationSec.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colRunDurationSec.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colRunDurationSec.Width = 125;
            // 
            // colRunDuration
            // 
            colRunDuration.DataPropertyName = "RunDuration";
            colRunDuration.HeaderText = "Run Duration";
            colRunDuration.MinimumWidth = 6;
            colRunDuration.Name = "colRunDuration";
            colRunDuration.ReadOnly = true;
            colRunDuration.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colRunDuration.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            colRunDuration.Width = 125;
            // 
            // colRetries
            // 
            colRetries.DataPropertyName = "retries_attempted";
            colRetries.HeaderText = "Retries";
            colRetries.MinimumWidth = 6;
            colRetries.Name = "colRetries";
            colRetries.ReadOnly = true;
            colRetries.Width = 125;
            // 
            // colMessage
            // 
            colMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            colMessage.DataPropertyName = "message";
            colMessage.HeaderText = "Message";
            colMessage.MinimumWidth = 100;
            colMessage.Name = "colMessage";
            colMessage.ReadOnly = true;
            colMessage.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            colMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // colViewSteps
            // 
            colViewSteps.HeaderText = "Steps";
            colViewSteps.MinimumWidth = 25;
            colViewSteps.Name = "colViewSteps";
            colViewSteps.ReadOnly = true;
            colViewSteps.Text = "View Steps";
            colViewSteps.UseColumnTextForLinkValue = true;
            colViewSteps.Width = 125;
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
            splitContainer1.Panel1.Controls.Add(dgvJobs);
            splitContainer1.Panel1.Controls.Add(jobStep1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(dgvJobHistory);
            splitContainer1.Panel2.Controls.Add(toolStrip2);
            splitContainer1.Size = new System.Drawing.Size(2197, 448);
            splitContainer1.SplitterDistance = 216;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 5;
            // 
            // jobStep1
            // 
            jobStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            jobStep1.Location = new System.Drawing.Point(0, 0);
            jobStep1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            jobStep1.Name = "jobStep1";
            jobStep1.Size = new System.Drawing.Size(2197, 216);
            jobStep1.TabIndex = 1;
            jobStep1.Visible = false;
            // 
            // toolStrip2
            // 
            toolStrip2.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsRefreshHistory, tsCopyHistory, tsExcelHistory, tsFilter, tsBack, tsJobName });
            toolStrip2.Location = new System.Drawing.Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new System.Drawing.Size(2197, 27);
            toolStrip2.TabIndex = 5;
            toolStrip2.Text = "toolStrip2";
            // 
            // tsRefreshHistory
            // 
            tsRefreshHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsRefreshHistory.Image = Properties.Resources._112_RefreshArrow_Green_16x16_72;
            tsRefreshHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsRefreshHistory.Name = "tsRefreshHistory";
            tsRefreshHistory.Size = new System.Drawing.Size(29, 24);
            tsRefreshHistory.Text = "Refresh";
            tsRefreshHistory.Click += TsRefreshHistory_Click;
            // 
            // tsCopyHistory
            // 
            tsCopyHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopyHistory.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopyHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopyHistory.Name = "tsCopyHistory";
            tsCopyHistory.Size = new System.Drawing.Size(29, 24);
            tsCopyHistory.Text = "Copy";
            tsCopyHistory.Click += TsCopyHistory_Click;
            // 
            // tsExcelHistory
            // 
            tsExcelHistory.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsExcelHistory.Image = Properties.Resources.excel16x16;
            tsExcelHistory.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsExcelHistory.Name = "tsExcelHistory";
            tsExcelHistory.Size = new System.Drawing.Size(29, 24);
            tsExcelHistory.Text = "Export Excel";
            tsExcelHistory.Click += TsExcelHistory_Click;
            // 
            // tsFilter
            // 
            tsFilter.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showJobStepsToolStripMenuItem, failedOnlyToolStripMenuItem });
            tsFilter.Image = Properties.Resources.FilterDropdown_16x;
            tsFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsFilter.Name = "tsFilter";
            tsFilter.Size = new System.Drawing.Size(34, 24);
            tsFilter.Text = "Filter";
            // 
            // showJobStepsToolStripMenuItem
            // 
            showJobStepsToolStripMenuItem.CheckOnClick = true;
            showJobStepsToolStripMenuItem.Name = "showJobStepsToolStripMenuItem";
            showJobStepsToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            showJobStepsToolStripMenuItem.Text = "Show Job Steps";
            showJobStepsToolStripMenuItem.Click += ShowJobStepsToolStripMenuItem_Click;
            // 
            // failedOnlyToolStripMenuItem
            // 
            failedOnlyToolStripMenuItem.CheckOnClick = true;
            failedOnlyToolStripMenuItem.Name = "failedOnlyToolStripMenuItem";
            failedOnlyToolStripMenuItem.Size = new System.Drawing.Size(195, 26);
            failedOnlyToolStripMenuItem.Text = "Failed Only";
            failedOnlyToolStripMenuItem.Click += FailedOnlyToolStripMenuItem_Click;
            // 
            // tsBack
            // 
            tsBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsBack.Image = Properties.Resources.Previous_grey_16x;
            tsBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBack.Name = "tsBack";
            tsBack.Size = new System.Drawing.Size(29, 24);
            tsBack.Text = "Back";
            tsBack.Click += TsBack_Click;
            // 
            // tsJobName
            // 
            tsJobName.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            tsJobName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            tsJobName.ForeColor = System.Drawing.Color.Black;
            tsJobName.Name = "tsJobName";
            tsJobName.Size = new System.Drawing.Size(80, 24);
            tsJobName.Text = "Job Name";
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
            dataGridViewTextBoxColumn2.DataPropertyName = "name";
            dataGridViewTextBoxColumn2.HeaderText = "Job Name";
            dataGridViewTextBoxColumn2.MinimumWidth = 6;
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 101;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.DataPropertyName = "LastFailed";
            dataGridViewTextBoxColumn3.HeaderText = "Last Failed";
            dataGridViewTextBoxColumn3.MinimumWidth = 6;
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            dataGridViewTextBoxColumn3.Width = 106;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.DataPropertyName = "TimeSinceLastFailed";
            dataGridViewTextBoxColumn4.HeaderText = "Time Since Last Fail";
            dataGridViewTextBoxColumn4.MinimumWidth = 6;
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            dataGridViewTextBoxColumn4.Width = 130;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.DataPropertyName = "LastSucceeded";
            dataGridViewTextBoxColumn5.HeaderText = "Last Succeeded";
            dataGridViewTextBoxColumn5.MinimumWidth = 6;
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
            dataGridViewTextBoxColumn5.Width = 127;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.DataPropertyName = "TimeSinceLastSucceeded";
            dataGridViewTextBoxColumn6.HeaderText = "TimeSinceLastSucceded";
            dataGridViewTextBoxColumn6.MinimumWidth = 6;
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            dataGridViewTextBoxColumn6.Width = 193;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.DataPropertyName = "FailCount24Hrs";
            dataGridViewTextBoxColumn7.HeaderText = "Fail Count (24Hrs)";
            dataGridViewTextBoxColumn7.MinimumWidth = 6;
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            dataGridViewTextBoxColumn7.Width = 139;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.DataPropertyName = "SucceedCount24Hrs";
            dataGridViewTextBoxColumn8.HeaderText = "Succeed Count (24Hrs)";
            dataGridViewTextBoxColumn8.MinimumWidth = 6;
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            dataGridViewTextBoxColumn8.Width = 126;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.DataPropertyName = "FailCount7Days";
            dataGridViewTextBoxColumn9.HeaderText = "Fail Count (7 Days)";
            dataGridViewTextBoxColumn9.MinimumWidth = 6;
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            dataGridViewTextBoxColumn9.Width = 111;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.DataPropertyName = "SucceedCount7Days";
            dataGridViewTextBoxColumn10.HeaderText = "Succeed Count (7 Days)";
            dataGridViewTextBoxColumn10.MinimumWidth = 6;
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            dataGridViewTextBoxColumn10.Width = 141;
            // 
            // dataGridViewTextBoxColumn11
            // 
            dataGridViewTextBoxColumn11.DataPropertyName = "JobStepFails24Hrs";
            dataGridViewTextBoxColumn11.HeaderText = "Job Step Fails (24Hrs)";
            dataGridViewTextBoxColumn11.MinimumWidth = 6;
            dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            dataGridViewTextBoxColumn11.ReadOnly = true;
            dataGridViewTextBoxColumn11.Width = 119;
            // 
            // dataGridViewTextBoxColumn12
            // 
            dataGridViewTextBoxColumn12.DataPropertyName = "JobStepFails7Days";
            dataGridViewTextBoxColumn12.HeaderText = "Job Step Fails (7 Days)";
            dataGridViewTextBoxColumn12.MinimumWidth = 6;
            dataGridViewTextBoxColumn12.Name = "dataGridViewTextBoxColumn12";
            dataGridViewTextBoxColumn12.ReadOnly = true;
            dataGridViewTextBoxColumn12.Width = 135;
            // 
            // dataGridViewTextBoxColumn13
            // 
            dataGridViewTextBoxColumn13.DataPropertyName = "MaxDurationSec";
            dataGridViewTextBoxColumn13.HeaderText = "Max Duration (sec)";
            dataGridViewTextBoxColumn13.MinimumWidth = 6;
            dataGridViewTextBoxColumn13.Name = "dataGridViewTextBoxColumn13";
            dataGridViewTextBoxColumn13.ReadOnly = true;
            dataGridViewTextBoxColumn13.Width = 143;
            // 
            // dataGridViewTextBoxColumn14
            // 
            dataGridViewTextBoxColumn14.DataPropertyName = "AvgDurationSec";
            dataGridViewTextBoxColumn14.HeaderText = "Avg Duration (sec)";
            dataGridViewTextBoxColumn14.MinimumWidth = 6;
            dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            dataGridViewTextBoxColumn14.ReadOnly = true;
            dataGridViewTextBoxColumn14.Width = 142;
            // 
            // dataGridViewTextBoxColumn15
            // 
            dataGridViewTextBoxColumn15.DataPropertyName = "ConfiguredLevel";
            dataGridViewTextBoxColumn15.HeaderText = "Configured Level";
            dataGridViewTextBoxColumn15.MinimumWidth = 6;
            dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            dataGridViewTextBoxColumn15.ReadOnly = true;
            dataGridViewTextBoxColumn15.Width = 132;
            // 
            // dataGridViewTextBoxColumn16
            // 
            dataGridViewTextBoxColumn16.DataPropertyName = "RunDateTime";
            dataGridViewTextBoxColumn16.HeaderText = "Date/Time";
            dataGridViewTextBoxColumn16.MinimumWidth = 6;
            dataGridViewTextBoxColumn16.Name = "dataGridViewTextBoxColumn16";
            dataGridViewTextBoxColumn16.Width = 125;
            // 
            // dataGridViewTextBoxColumn17
            // 
            dataGridViewTextBoxColumn17.DataPropertyName = "step_id";
            dataGridViewTextBoxColumn17.HeaderText = "Step ID";
            dataGridViewTextBoxColumn17.MinimumWidth = 6;
            dataGridViewTextBoxColumn17.Name = "dataGridViewTextBoxColumn17";
            dataGridViewTextBoxColumn17.Width = 125;
            // 
            // dataGridViewTextBoxColumn18
            // 
            dataGridViewTextBoxColumn18.DataPropertyName = "step_name";
            dataGridViewTextBoxColumn18.HeaderText = "Step Name";
            dataGridViewTextBoxColumn18.MinimumWidth = 6;
            dataGridViewTextBoxColumn18.Name = "dataGridViewTextBoxColumn18";
            dataGridViewTextBoxColumn18.Width = 125;
            // 
            // dataGridViewTextBoxColumn19
            // 
            dataGridViewTextBoxColumn19.DataPropertyName = "sql_message_id";
            dataGridViewTextBoxColumn19.HeaderText = "Message ID";
            dataGridViewTextBoxColumn19.MinimumWidth = 6;
            dataGridViewTextBoxColumn19.Name = "dataGridViewTextBoxColumn19";
            dataGridViewTextBoxColumn19.Width = 125;
            // 
            // dataGridViewTextBoxColumn20
            // 
            dataGridViewTextBoxColumn20.DataPropertyName = "sql_severity";
            dataGridViewTextBoxColumn20.HeaderText = "Severity";
            dataGridViewTextBoxColumn20.MinimumWidth = 6;
            dataGridViewTextBoxColumn20.Name = "dataGridViewTextBoxColumn20";
            dataGridViewTextBoxColumn20.Width = 125;
            // 
            // dataGridViewTextBoxColumn21
            // 
            dataGridViewTextBoxColumn21.DataPropertyName = "message";
            dataGridViewTextBoxColumn21.HeaderText = "Message";
            dataGridViewTextBoxColumn21.MinimumWidth = 6;
            dataGridViewTextBoxColumn21.Name = "dataGridViewTextBoxColumn21";
            dataGridViewTextBoxColumn21.Width = 125;
            // 
            // dataGridViewTextBoxColumn22
            // 
            dataGridViewTextBoxColumn22.DataPropertyName = "run_status_description";
            dataGridViewTextBoxColumn22.HeaderText = "Status";
            dataGridViewTextBoxColumn22.MinimumWidth = 6;
            dataGridViewTextBoxColumn22.Name = "dataGridViewTextBoxColumn22";
            dataGridViewTextBoxColumn22.Width = 125;
            // 
            // dataGridViewTextBoxColumn23
            // 
            dataGridViewTextBoxColumn23.DataPropertyName = "RunDurationSec";
            dataGridViewTextBoxColumn23.HeaderText = "Duration (sec)";
            dataGridViewTextBoxColumn23.MinimumWidth = 6;
            dataGridViewTextBoxColumn23.Name = "dataGridViewTextBoxColumn23";
            dataGridViewTextBoxColumn23.Width = 125;
            // 
            // dataGridViewTextBoxColumn24
            // 
            dataGridViewTextBoxColumn24.DataPropertyName = "retries_attempted";
            dataGridViewTextBoxColumn24.HeaderText = "Retries";
            dataGridViewTextBoxColumn24.MinimumWidth = 6;
            dataGridViewTextBoxColumn24.Name = "dataGridViewTextBoxColumn24";
            dataGridViewTextBoxColumn24.Width = 125;
            // 
            // dataGridViewTextBoxColumn25
            // 
            dataGridViewTextBoxColumn25.DataPropertyName = "retries_attempted";
            dataGridViewTextBoxColumn25.HeaderText = "Retries";
            dataGridViewTextBoxColumn25.MinimumWidth = 6;
            dataGridViewTextBoxColumn25.Name = "dataGridViewTextBoxColumn25";
            dataGridViewTextBoxColumn25.Width = 125;
            // 
            // dataGridViewTextBoxColumn26
            // 
            dataGridViewTextBoxColumn26.DataPropertyName = "RunDurationSec";
            dataGridViewTextBoxColumn26.HeaderText = "Duration (sec)";
            dataGridViewTextBoxColumn26.MinimumWidth = 6;
            dataGridViewTextBoxColumn26.Name = "dataGridViewTextBoxColumn26";
            dataGridViewTextBoxColumn26.Width = 125;
            // 
            // dataGridViewTextBoxColumn27
            // 
            dataGridViewTextBoxColumn27.DataPropertyName = "RunDuration";
            dataGridViewTextBoxColumn27.HeaderText = "Run Duration";
            dataGridViewTextBoxColumn27.MinimumWidth = 6;
            dataGridViewTextBoxColumn27.Name = "dataGridViewTextBoxColumn27";
            dataGridViewTextBoxColumn27.Width = 125;
            // 
            // dataGridViewTextBoxColumn28
            // 
            dataGridViewTextBoxColumn28.DataPropertyName = "retries_attempted";
            dataGridViewTextBoxColumn28.HeaderText = "Retries";
            dataGridViewTextBoxColumn28.MinimumWidth = 6;
            dataGridViewTextBoxColumn28.Name = "dataGridViewTextBoxColumn28";
            dataGridViewTextBoxColumn28.Width = 125;
            // 
            // AgentJobsControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(tsJobs);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "AgentJobsControl";
            Size = new System.Drawing.Size(2197, 475);
            Load += AgentJobsControl_Load;
            Resize += ResizeForm;
            ((System.ComponentModel.ISupportInitialize)dgvJobs).EndInit();
            tsJobs.ResumeLayout(false);
            tsJobs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvJobHistory).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvJobs;
        private System.Windows.Forms.ToolStrip tsJobs;
        private System.Windows.Forms.ToolStripDropDownButton tsConfigure;
        private System.Windows.Forms.ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureRootThresholdsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private System.Windows.Forms.DataGridView dgvJobHistory;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton tsCopyHistory;
        private System.Windows.Forms.ToolStripDropDownButton tsFilter;
        private System.Windows.Forms.ToolStripMenuItem showJobStepsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsBack;
        private System.Windows.Forms.ToolStripButton tsRefreshHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn24;
        private System.Windows.Forms.ToolStripLabel tsJobName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn25;
        private System.Windows.Forms.ToolStripMenuItem failedOnlyToolStripMenuItem;
        private JobStep jobStep1;
        private System.Windows.Forms.ToolStripButton tsExcel;
        private System.Windows.Forms.ToolStripButton tsExcelHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn26;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn27;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn28;
        private StatusFilterToolStrip statusFilterToolStrip1;
        private System.Windows.Forms.ToolStripMenuItem acknowledgeErrorsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewLinkColumn colHistory;
        private System.Windows.Forms.DataGridViewLinkColumn Acknowledge;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastFail;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn StepLastFailed;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn TimeSinceLastSucceeded;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAckDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn FailCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn SucceedCount7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails24Hrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobStepFails7Days;
        private System.Windows.Forms.DataGridViewTextBoxColumn MaxDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaxDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn AvgDurationSec;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAvgDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn ConfiguredLevel;
        private System.Windows.Forms.DataGridViewLinkColumn Configure;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunDateTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunEndDateTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStepID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStepName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMessageID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRunStatus;
        private System.Windows.Forms.DataGridViewLinkColumn colRunDurationSec;
        private System.Windows.Forms.DataGridViewLinkColumn colRunDuration;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRetries;
        private System.Windows.Forms.DataGridViewLinkColumn colMessage;
        private System.Windows.Forms.DataGridViewLinkColumn colViewSteps;
    }
}
