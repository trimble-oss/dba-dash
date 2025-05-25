using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Control = System.Windows.Forms.Control;

namespace DBADashGUI.AgentJobs
{
    public partial class JobExecutionDialog : Form
    {
        #region Constants

        private const int STATUS_MONITOR_INTERVAL_SECONDS = 10;
        private const int MessageLifetime = 60;

        #endregion Constants

        #region Properties

        private static int ReceiveMessageTimeoutMs => (MessageLifetime * 1000) + 1000;

        public Guid JobId { get; set; }
        public int InstanceId { get; set; }

        public string JobName { get => lnkJobName.Text; set => lnkJobName.Text = value; }

        private string StartStep
        {
            get
            {
                var row = ((DataRowView)cboStep.SelectedItem)?.Row;
                return cboStep.SelectedIndex == 0 || row == null ? string.Empty : row.Field<string>("step_name");
            }
        }

        #endregion Properties

        #region Fields

        private DBADashContext _currentContext;

        private CancellationTokenSource _statusMonitorCts;

        private int LastRunTime;
        private int LastRunDate;

        private DateTime JobStart;

        private JobInfoForm _jobInfoForm;

        #endregion Fields

        #region Event Handlers & Initialization

        public JobExecutionDialog()
        {
            InitializeComponent();
            this.ApplyTheme();
            this.FormClosed += JobExecutionDialog_FormClosed;
        }

        private void JobExecutionDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopStatusMonitoring();
        }

        private void JobExecutionDialog_Load(object sender, EventArgs e)
        {
            _currentContext = CommonData.GetDBADashContext(InstanceId);
            _currentContext.JobID = JobId;
            Text = "Execute Job - " + _currentContext.InstanceName + " \\ " + JobName;
            var steps = CommonData.GetJobSteps(InstanceId, JobId);
            cboStep.DataSource = steps;
            cboStep.ValueMember = "step_id";
            cboStep.Format += CboStep_Format;
            txtStartStatus.BackColor = DashColors.GrayLight;
            txtExecuteStatus.BackColor = DashColors.GrayLight;
        }

        private static void CboStep_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is not DataRowView rowView) return;
            var row = rowView.Row;
            var stepId = row.Field<int>("step_id");
            var stepName = row.Field<string>("step_name");

            e.Value = $"{stepId} - {stepName}";
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            try
            {
                await StartJobAsync();
            }
            catch (Exception ex)
            {
                InvokeSetStatus(txtStartStatus, ex.Message, DashColors.Fail);
            }
        }

        private async void Stop_Click(object sender, EventArgs e)
        {
            try
            {
                await StopJobAsync();
            }
            catch (Exception ex)
            {
                InvokeSetStatus(txtStartStatus, ex.Message, DashColors.Fail);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblTimer.Text = TimeSpan.FromSeconds(Convert.ToInt32(DateTime.Now.Subtract(JobStart).TotalSeconds)).ToString("c");
        }

        private void JobLink_Clicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_jobInfoForm == null)
            {
                _jobInfoForm = new JobInfoForm()
                {
                    DBADashContext = _currentContext
                };
                _jobInfoForm.FormClosed += (_, _) => { _jobInfoForm = null; };
            }
            _jobInfoForm.Show();
            _jobInfoForm.Focus();
        }

        #endregion Event Handlers & Initialization

        #region Start Job

        private async Task StartJobAsync()
        {
            if (!ValidateMessagingAccess())
            {
                return;
            }

            var messageGroup = Guid.NewGuid();
            InvokeSetStatus(txtStartStatus, "Send job start message", DashColors.Information);
            await SendJobMessage(AgentJobExecutionMessage.JobActions.StartJob, messageGroup);

            var completed = false;
            while (!completed)
            {
                var reply = await CollectionMessaging.ReceiveReply(messageGroup, ReceiveMessageTimeoutMs);
                completed = ProcessJobStartMessageResponse(reply);
            }
        }

        private bool ProcessJobStartMessageResponse(ResponseMessage reply)
        {
            switch (reply.Type)
            {
                case ResponseMessage.ResponseTypes.Progress:
                    InvokeSetStatus(txtStartStatus, reply.Message, DashColors.Information);
                    return false;

                case ResponseMessage.ResponseTypes.Failure:
                    InvokeSetStatus(txtStartStatus, reply.Message, DashColors.Fail);
                    return true;

                case ResponseMessage.ResponseTypes.Success:
                    bttnStop.Enabled = true;
                    bttnStart.Enabled = false;
                    var message = GetJobRunMessage(reply);
                    InvokeSetStatus(txtStartStatus, message, DashColors.Success);

                    var dtJob = reply.Data.Tables["JOB"];
                    if (dtJob == null) return false;
                    var helpJob = new HelpJobInfoExtended(dtJob);
                    LastRunTime = helpJob.LastRunTime;
                    LastRunDate = helpJob.LastRunDate;
                    InvokeSetStatus(txtExecuteStatus, "Waiting for new job status...", DashColors.Information);
                    StartStatusMonitoring();
                    // It's done but wait for end dialog
                    return false;

                case ResponseMessage.ResponseTypes.EndConversation:
                    return true;

                case ResponseMessage.ResponseTypes.Cancellation:
                    InvokeSetStatus(txtExecuteStatus, "Cancelled", DashColors.Warning);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Start Job

        #region Status Monitoring

        private void StartStatusMonitoring()
        {
            StopStatusMonitoring();
            _statusMonitorCts = new CancellationTokenSource();
            _ = Task.Run(() => JobStatusMonitorAsync(_statusMonitorCts.Token));
        }

        private void StopStatusMonitoring()
        {
            _statusMonitorCts?.Cancel();
            _statusMonitorCts?.Dispose();
            _statusMonitorCts = null;
        }

        private async Task JobStatusMonitorAsync(CancellationToken cancellationToken)
        {
            InvokeStartTimer();
            while (!cancellationToken.IsCancellationRequested)
            {
                for (var i = STATUS_MONITOR_INTERVAL_SECONDS; i > 0 && !cancellationToken.IsCancellationRequested; i--)
                {
                    InvokeSetStatus(lblNextUpdateIn, $"Next update in...{i}", DashColors.Information);
                    await Task.Delay(1000, cancellationToken);
                }
                if (cancellationToken.IsCancellationRequested)
                    break;
                try
                {
                    await GetExecutionStatus();
                }
                catch (Exception ex)
                {
                    InvokeSetStatus(txtExecuteStatus, ex.Message, DashColors.Fail);
                    break;
                }
            }
            if (!Disposing)
            {
                InvokeStopTimer();
                bttnStart.Invoke(() => { bttnStart.Enabled = true; });
                InvokeSetStatus(lblNextUpdateIn, "", Color.Transparent);
            }
        }

        private async Task GetExecutionStatus()
        {
            if (_currentContext.ImportAgentID == null) return;
            var messageGroup = Guid.NewGuid();

            InvokeSetStatus(lblNextUpdateIn, "Send message", DashColors.Information);
            await SendJobMessage(AgentJobExecutionMessage.JobActions.StatusOnly, messageGroup);

            var completed = false;
            while (!completed)
            {
                var reply = await CollectionMessaging.ReceiveReply(messageGroup, ReceiveMessageTimeoutMs);

                completed = ProcessExecutionStatusResponse(reply);
            }
        }

        private bool ProcessExecutionStatusResponse(ResponseMessage reply)
        {
            switch (reply.Type)
            {
                case ResponseMessage.ResponseTypes.Progress:
                    InvokeSetStatus(lblNextUpdateIn, reply.Message, DashColors.Information);
                    return false;

                case ResponseMessage.ResponseTypes.Failure:
                    throw new Exception("Error checking status: " + reply.Message);

                case ResponseMessage.ResponseTypes.Success:
                    var dtJob = reply.Data.Tables["JOB"];
                    if (dtJob == null) return false;
                    var helpJob = new HelpJobInfoExtended(dtJob);

                    if (helpJob.IsIdle && (helpJob.LastRunDate > LastRunDate || helpJob.LastRunTime > LastRunTime))
                    {
                        InvokeSetStatus(txtExecuteStatus, helpJob.LastRunOutcomeDescription, helpJob.LastRunOutcomeColor);
                        StopStatusMonitoring();
                    }
                    else if (helpJob.IsIdle)
                    {
                        InvokeSetStatus(txtExecuteStatus, "Waiting for new job status...", DashColors.Information);
                    }
                    else
                    {
                        InvokeSetStatus(txtExecuteStatus, helpJob.StatusInfo, DashColors.Information);
                    }

                    return false;// It's done but wait for end dialog

                case ResponseMessage.ResponseTypes.EndConversation:
                    return true;

                case ResponseMessage.ResponseTypes.Cancellation:
                    InvokeSetStatus(txtExecuteStatus, reply.Message, DashColors.Warning);
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Status Monitoring

        #region Stop Job

        private async Task StopJobAsync()
        {
            if (!ValidateMessagingAccess())
            {
                return;
            }

            var messageGroup = Guid.NewGuid();

            InvokeSetStatus(txtStartStatus, "Send Job stop message", DashColors.Information);
            await SendJobMessage(AgentJobExecutionMessage.JobActions.StopJob, messageGroup);

            var completed = false;
            while (!completed)
            {
                ResponseMessage reply;
                try
                {
                    reply = await CollectionMessaging.ReceiveReply(messageGroup, ReceiveMessageTimeoutMs);
                }
                catch (Exception ex)
                {
                    InvokeSetStatus(txtStartStatus, ex.Message, DashColors.Fail);
                    return;
                }

                completed = ProcessJobStopResponse(reply);
            }
        }

        private bool ProcessJobStopResponse(ResponseMessage reply)
        {
            switch (reply.Type)
            {
                case ResponseMessage.ResponseTypes.Progress:
                    InvokeSetStatus(txtStartStatus, reply.Message, DashColors.Information);
                    return false;

                case ResponseMessage.ResponseTypes.Failure:
                    InvokeSetStatus(txtStartStatus, reply.Message, DashColors.Fail);
                    return true;

                case ResponseMessage.ResponseTypes.Success:
                    bttnStop.Enabled = true;
                    bttnStart.Enabled = false;
                    var message = GetJobRunMessage(reply);
                    InvokeSetStatus(txtStartStatus, message, DashColors.Success);

                    return false;// It's done but wait for end dialog

                case ResponseMessage.ResponseTypes.EndConversation:
                    return true;

                case ResponseMessage.ResponseTypes.Cancellation:
                    InvokeSetStatus(txtStartStatus, "Cancelled", DashColors.Warning);
                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Stop Job

        #region Helper

        private AgentJobExecutionMessage CreateJobExecutionMessage(AgentJobExecutionMessage.JobActions action)
        {
            return new AgentJobExecutionMessage()
            {
                JobId = JobId,
                ConnectionID = _currentContext.ConnectionID,
                ImportAgent = _currentContext.ImportAgent,
                CollectAgent = _currentContext.CollectAgent,
                StepName = action == AgentJobExecutionMessage.JobActions.StartJob ? StartStep : string.Empty,
                JobAction = action,
                Lifetime = MessageLifetime
            };
        }

        private async Task SendJobMessage(AgentJobExecutionMessage.JobActions action, Guid messageGroup)
        {
            var message = CreateJobExecutionMessage(action);
            await MessageProcessing.SendMessageToService(
                message.Serialize(),
                (int)_currentContext.ImportAgentID!,
                messageGroup,
                Common.ConnectionString,
                MessageLifetime
                );
        }

        private static void InvokeSetStatus(Control control, string message, Color color)
        {
            if (control.IsDisposed) return;
            control.Invoke(() =>
            {
                if (control.IsDisposed) return;
                control.Text = message;
                control.ForeColor = color;
                control.Visible = true;
            });
        }

        private bool ValidateMessagingAccess() => ValidateMessagingAccess(txtStartStatus);

        private bool ValidateMessagingAccess(Control statusControl)
        {
            if (!DBADashUser.AllowMessaging)
            {
                InvokeSetStatus(statusControl, "Sorry, you do not have access to use the messaging feature", DashColors.Fail);
                return false;
            }
            if (!DBADashUser.AllowJobExecution)
            {
                InvokeSetStatus(statusControl, "Sorry, you do not have access to execute jobs.", DashColors.Fail);
                return false;
            }
            if (!_currentContext.CanMessage)
            {
                InvokeSetStatus(statusControl, "Messaging is not enabled", DashColors.Fail);
                return false;
            }

            if (_currentContext.ImportAgentID == null)
            {
                InvokeSetStatus(statusControl, "No Import Agent", DashColors.Fail);
                return false;
            }

            return true;
        }

        private static string GetJobRunMessage(ResponseMessage reply)
        {
            var dtMessages = reply.Data.Tables["RunJobMessages"];
            return dtMessages is { Rows.Count: > 0 } ? dtMessages.Rows[0].Field<string>("Message") : reply.Message;
        }

        public void InvokeStartTimer()
        {
            if (InvokeRequired)
            {
                Invoke(InvokeStartTimer);
                return;
            }
            JobStart = DateTime.Now;
            timer1.Start();
        }

        public void InvokeStopTimer()
        {
            if (InvokeRequired)
            {
                Invoke(InvokeStopTimer);
                return;
            }
            timer1.Stop();
        }

        #endregion Helper
    }
}