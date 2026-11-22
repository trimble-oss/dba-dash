using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JobActions = DBADash.Messaging.ElasticJobExecutionMessage.JobActions;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// Start or stop (cancel) an elastic job, and follow its execution - the elastic job counterpart of <see cref="AgentJobs.JobExecutionDialog"/>.
    /// Sends <see cref="ElasticJobExecutionMessage"/> to the service, which runs jobs.sp_start_job/jobs.sp_stop_job in the job database.
    ///
    /// <para>Opens on the job's latest execution.  While an execution is active, its status - the job's and each step's against each
    /// target - is refreshed every <see cref="StatusIntervalSeconds"/> seconds until it completes.</para>
    /// </summary>
    [DesignerCategory("")]
    internal sealed class ElasticJobExecutionDialog : Form
    {
        private const int StatusIntervalSeconds = 10;
        private const int MessageLifetime = 60;

        private readonly int _instanceId;
        private readonly Guid _jobId;
        private readonly string _jobName;
        private DBADashContext _context;

        /// <summary>The execution shown - the one started here, or the job's latest.  Null until there is one.</summary>
        private Guid? _executionId;

        private bool _executionActive;

        /// <summary>
        /// Incremented when Start or Stop is sent and again when its reply arrives.  A status reply to a request sent before then is ignored -
        /// it's about the execution shown before, and would replace the one started or stopped.
        /// </summary>
        private int _requestVersion;

        /// <summary>
        /// Start or Stop is waiting for its reply.  No status is requested meanwhile (Refresh is disabled, and a poll is skipped) - its reply could
        /// arrive after the Start/Stop reply and replace the execution with the one shown before.
        /// </summary>
        private bool _operationInFlight;
        private DateTime? _executionStartUtc;
        private CancellationTokenSource _monitorCts;

        private readonly LinkLabel _lnkJob = new() { AutoSize = true, Font = new Font("Segoe UI", 9.75f, FontStyle.Bold), Margin = new Padding(3, 6, 3, 6) };
        private readonly Button _btnStart = new() { Text = "Start", AutoSize = true, Image = Properties.Resources.ProjectSystemModelRefresh_16x, TextImageRelation = TextImageRelation.ImageBeforeText };
        private readonly Button _btnStop = new() { Text = "Stop", AutoSize = true, Enabled = false };
        private readonly Button _btnRefresh = new() { Text = "Refresh", AutoSize = true, Image = Properties.Resources._112_RefreshArrow_Green_16x16_72, TextImageRelation = TextImageRelation.ImageBeforeText };
        private readonly TextBox _txtRequest = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 40, ScrollBars = ScrollBars.Vertical };
        private readonly TextBox _txtExecution = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 60, ScrollBars = ScrollBars.Vertical };
        private readonly Label _lblElapsed = new() { AutoSize = true, Margin = new Padding(3, 6, 12, 6) };
        private readonly Label _lblNextUpdate = new() { AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
        private readonly System.Windows.Forms.Timer _elapsedTimer = new() { Interval = 1000 };
        private readonly DataGridView _grid;

        public ElasticJobExecutionDialog(int instanceId, Guid jobId, string jobName)
        {
            _instanceId = instanceId;
            _jobId = jobId;
            _jobName = jobName;

            Text = "Elastic Job - " + jobName;
            Size = LogicalToDeviceUnits(new Size(1000, 620));
            MinimumSize = LogicalToDeviceUnits(new Size(600, 400));
            StartPosition = FormStartPosition.CenterParent;

            _lnkJob.Text = jobName;
            _lnkJob.LinkClicked += (_, _) => ElasticJobHistoryReport.ShowHistory(_context, _instanceId, _jobId);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ShowCellToolTips = true
            };
            _grid.CellFormatting += Grid_CellFormatting;

            _btnStart.Click += async (_, _) => await RunSafeAsync(StartJobAsync, _txtRequest);
            _btnStop.Click += async (_, _) => await RunSafeAsync(StopJobAsync, _txtRequest);
            _btnRefresh.Click += async (_, _) => await RunSafeAsync(RefreshStatusAsync, _txtExecution);
            _elapsedTimer.Tick += (_, _) => ShowElapsed();

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            buttons.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnRefresh, _lblElapsed, _lblNextUpdate });

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(6) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(FieldLabel("Job:"), 0, 0);
            layout.Controls.Add(_lnkJob, 1, 0);
            layout.Controls.Add(buttons, 0, 1);
            layout.SetColumnSpan(buttons, 2);
            layout.Controls.Add(FieldLabel("Request:"), 0, 2);
            layout.Controls.Add(_txtRequest, 1, 2);
            layout.Controls.Add(FieldLabel("Execution:"), 0, 3);
            layout.Controls.Add(_txtExecution, 1, 3);
            layout.Controls.Add(_grid, 0, 4);
            layout.SetColumnSpan(_grid, 2);
            Controls.Add(layout);

            this.ApplyTheme();
            _txtRequest.BackColor = DashColors.GrayLight;
            _txtExecution.BackColor = DashColors.GrayLight;
            Load += async (_, _) => await RunSafeAsync(OnLoadAsync, _txtExecution);
            FormClosed += (_, _) =>
            {
                StopMonitoring();
                _elapsedTimer.Dispose();
            };
        }

        /// <summary>For the event handlers (async void) - an error is shown rather than taking down the app</summary>
        private async Task RunSafeAsync(Func<Task> action, Control statusControl)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                if (!IsDisposed) SetStatus(statusControl, ex.Message, DashColors.Fail);
            }
        }

        private static Label FieldLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 6) };

        private async Task OnLoadAsync()
        {
            _context = CommonData.GetDBADashContext(_instanceId);
            Text = $"Elastic Job - {_context.InstanceName} \\ {_jobName}";
            if (!ValidateMessagingAccess()) return;
            // The job's latest execution - following it if it's still running
            await RefreshStatusAsync();
        }

        #region Start/Stop

        private async Task StartJobAsync()
        {
            if (!ValidateMessagingAccess()) return;
            _btnStart.Enabled = false;
            SetStatus(_txtRequest, "Sending job start message", DashColors.Information);
            var outcome = await SendOperationAsync(JobActions.StartJob, null);
            if (outcome == null) return;
            if (!outcome.Succeeded)
            {
                SetStatus(_txtRequest, outcome.Error, DashColors.Fail);
                _btnStart.Enabled = !_executionActive;
                ResumeMonitoringIfActive();
                return;
            }
            SetStatus(_txtRequest, RunMessage(outcome.Data) ?? "Job started", DashColors.Success);
            var executions = outcome.Data?.Tables[ElasticJobExecutionMessage.ExecutionsTableName];
            if (!ShowExecution(executions))
            {
                // Started, but its row isn't there yet - follow it by the ID sp_start_job returned
                _executionId = RunExecutionId(outcome.Data);
                SetExecutionActive(true);
                SetStatus(_txtExecution, "Waiting for the execution to start...", DashColors.Information);
            }
            StartMonitoring();
        }

        private async Task StopJobAsync()
        {
            if (!ValidateMessagingAccess()) return;
            _btnStop.Enabled = false;
            SetStatus(_txtRequest, "Sending job stop message", DashColors.Information);
            // The execution shown if it's running - otherwise every active execution of the job
            var outcome = await SendOperationAsync(JobActions.StopJob, _executionActive ? _executionId : null);
            if (outcome == null) return;
            if (!outcome.Succeeded)
            {
                SetStatus(_txtRequest, outcome.Error, DashColors.Fail);
                _btnStop.Enabled = _executionActive;
                // A poll that was waiting when Stop was clicked was dropped, which may have ended monitoring
                ResumeMonitoringIfActive();
                return;
            }
            // The agent cancels the execution asynchronously - keep following it until it's no longer active
            SetStatus(_txtRequest, RunMessage(outcome.Data) ?? "Cancellation requested", DashColors.Success);
            ShowExecution(outcome.Data?.Tables[ElasticJobExecutionMessage.ExecutionsTableName]);
            if (_executionActive) StartMonitoring();
        }

        /// <summary>
        /// Sends Start or Stop.  While it's waiting for the reply no status is requested, and status replies to requests sent before the reply
        /// arrived are ignored - see <see cref="_operationInFlight"/> and <see cref="_requestVersion"/>.
        /// </summary>
        private async Task<MessagingHelper.MessageOutcome> SendOperationAsync(JobActions action, Guid? executionId)
        {
            _operationInFlight = true;
            _requestVersion++;
            _btnRefresh.Enabled = false;
            try
            {
                return await SendAsync(action, executionId, _txtRequest);
            }
            finally
            {
                _operationInFlight = false;
                _requestVersion++;
                if (!IsDisposed) _btnRefresh.Enabled = true;
            }
        }

        private void ResumeMonitoringIfActive()
        {
            if (_executionActive && _monitorCts == null) StartMonitoring();
        }

        /// <summary>The execution started or stopped (null if more than one was stopped)</summary>
        private static Guid? RunExecutionId(DataSet ds)
        {
            var dt = ds?.Tables[ElasticJobExecutionMessage.MessagesTableName];
            return dt is { Rows.Count: > 0 } && dt.Columns.Contains("JobExecutionId") && dt.Rows[0]["JobExecutionId"] is Guid id ? id : null;
        }

        private static string RunMessage(DataSet ds)
        {
            var dt = ds?.Tables[ElasticJobExecutionMessage.MessagesTableName];
            return dt is { Rows.Count: > 0 } && dt.Rows[0]["Message"] is string { Length: > 0 } message ? message : null;
        }

        #endregion Start/Stop

        #region Status

        private Task RefreshStatusAsync() => GetStatusAsync();

        /// <summary>
        /// Gets the status of the execution shown while it's active.  Otherwise the job's latest active execution, or its latest if none is - so
        /// a run started since (by its schedule, say) is picked up.  True if it's still active.  Null on failure.
        /// <para>Not requested while Start or Stop is waiting for its reply, and the reply is ignored if Start or Stop was sent meanwhile: the
        /// execution is taken to be as active as it was, so monitoring carries on.</para>
        /// </summary>
        private async Task<bool?> GetStatusAsync()
        {
            if (_operationInFlight) return _executionActive;
            if (!ValidateMessagingAccess()) return null;
            _btnRefresh.Enabled = false;
            var version = _requestVersion;
            try
            {
                var outcome = await SendAsync(JobActions.StatusOnly, _executionActive ? _executionId : null, _txtExecution);
                if (outcome == null) return null;
                if (version != _requestVersion) return _executionActive;
                if (!outcome.Succeeded)
                {
                    SetStatus(_txtExecution, "Error checking status: " + outcome.Error, DashColors.Fail);
                    return null;
                }
                if (!ShowExecution(outcome.Data?.Tables[ElasticJobExecutionMessage.ExecutionsTableName]))
                {
                    if (_executionActive)
                    {
                        SetStatus(_txtExecution, "Waiting for the execution to start...", DashColors.Information);
                        return true;
                    }
                    SetStatus(_txtExecution, "The job hasn't run", DashColors.Information);
                    return false;
                }
                if (_executionActive && _monitorCts == null)
                {
                    StartMonitoring();
                }
                return _executionActive;
            }
            finally
            {
                if (!IsDisposed) _btnRefresh.Enabled = !_operationInFlight;
            }
        }

        /// <summary>
        /// Shows an execution: the job's row (no step) as its status, and every row in the grid.  False if there's no job row - the job
        /// hasn't run, or the execution started hasn't been recorded yet.
        /// </summary>
        private bool ShowExecution(DataTable executions)
        {
            if (executions == null || !executions.Columns.Contains("step_id") || !executions.Columns.Contains("job_execution_id")) return false;
            var jobRow = executions.Rows.Cast<DataRow>().FirstOrDefault(r => r["step_id"] == DBNull.Value);
            if (jobRow == null) return false;

            _executionId = (Guid)jobRow["job_execution_id"];
            var lifecycle = Field<string>(jobRow, "lifecycle");
            var isActive = Field<bool?>(jobRow, "is_active") == true;
            _executionStartUtc = Field<DateTime?>(jobRow, "start_time") ?? Field<DateTime?>(jobRow, "create_time");
            var endUtc = Field<DateTime?>(jobRow, "end_time");

            var status = ElasticJobTimelineModel.LifecycleText(lifecycle);
            if (_executionStartUtc.HasValue) status += $".  Started {_executionStartUtc.Value.ToAppTimeZone():G}";
            if (endUtc.HasValue) status += $", ended {endUtc.Value.ToAppTimeZone():G}";
            var message = Field<string>(jobRow, "last_message");
            if (!string.IsNullOrEmpty(message)) status += Environment.NewLine + message;
            SetStatus(_txtExecution, status, LifecycleColor(lifecycle, isActive));

            BindGrid(executions);
            SetExecutionActive(isActive);
            if (!isActive && endUtc.HasValue && _executionStartUtc.HasValue)
            {
                _lblElapsed.Text = "Duration: " + FormatDuration(endUtc.Value - _executionStartUtc.Value);
            }
            return true;
        }

        private void SetExecutionActive(bool isActive)
        {
            _executionActive = isActive;
            _btnStop.Enabled = isActive;
            _btnStart.Enabled = !isActive;
            if (isActive)
            {
                ShowElapsed();
                _elapsedTimer.Start();
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        private static T Field<T>(DataRow row, string column) =>
            row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? (T)row[column] : default;

        private void ShowElapsed()
        {
            _lblElapsed.Text = _executionStartUtc.HasValue ? "Elapsed: " + FormatDuration(DateTime.UtcNow - _executionStartUtc.Value) : string.Empty;
        }

        private static string FormatDuration(TimeSpan duration) =>
            TimeSpan.FromSeconds(Math.Max(0, Math.Round(duration.TotalSeconds))).ToString("c");

        /// <summary>Colour for a lifecycle - as dbo.ElasticJobOutcome classes them</summary>
        internal static Color LifecycleColor(string lifecycle, bool isActive) => lifecycle switch
        {
            "Succeeded" => DashColors.Success,
            "SucceededWithSkipped" or "Canceled" or "Skipped" or "WaitingForRetry" => DashColors.Warning,
            "Failed" or "TimedOut" => DashColors.Fail,
            _ => isActive ? DashColors.Information : DashColors.Fail
        };

        #endregion Status

        #region Monitoring

        /// <summary>Refreshes the status every <see cref="StatusIntervalSeconds"/> seconds until the execution is no longer active</summary>
        private async void StartMonitoring()
        {
            StopMonitoring();
            var cts = new CancellationTokenSource();
            _monitorCts = cts;
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    for (var i = StatusIntervalSeconds; i > 0; i--)
                    {
                        _lblNextUpdate.Text = $"Next update in...{i}";
                        await Task.Delay(1000, cts.Token);
                    }
                    _lblNextUpdate.Text = "Checking status...";
                    var active = await GetStatusAsync();
                    if (active != true) break;
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped - closed, or monitoring restarted
            }
            catch (Exception ex)
            {
                SetStatus(_txtExecution, ex.Message, DashColors.Fail);
            }
            finally
            {
                if (_monitorCts == cts)
                {
                    _monitorCts = null;
                    if (!IsDisposed) _lblNextUpdate.Text = string.Empty;
                }
                cts.Dispose();
            }
        }

        private void StopMonitoring()
        {
            var cts = _monitorCts;
            _monitorCts = null;
            cts?.Cancel();
        }

        #endregion Monitoring

        #region Grid

        /// <summary>Columns of jobs.job_executions shown, where the view has them - they vary with the version of the agent</summary>
        private static readonly (string Name, string Header, int Width)[] GridColumns =
        {
            ("step_id", "Step", 45),
            ("step_name", "Step Name", 140),
            ("target_type", "Target Type", 100),
            ("target_server_name", "Server", 160),
            ("target_elastic_pool_name", "Elastic Pool", 100),
            ("target_database_name", "Database", 120),
            ("lifecycle", "Lifecycle", 110),
            ("start_time", "Start", 140),
            ("end_time", "End", 140),
            ("current_attempts", "Attempts", 65),
            ("last_message", "Message", 250),
        };

        private void BindGrid(DataTable executions)
        {
            if (_grid.Columns.Count == 0)
            {
                foreach (var (name, header, width) in GridColumns.Where(c => executions.Columns.Contains(c.Name)))
                {
                    _grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = name,
                        DataPropertyName = name,
                        HeaderText = header,
                        Width = LogicalToDeviceUnits(width),
                        SortMode = DataGridViewColumnSortMode.Automatic
                    });
                }
                if (_grid.Columns.Contains("last_message"))
                {
                    _grid.Columns["last_message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    _grid.Columns["last_message"].MinimumWidth = LogicalToDeviceUnits(150);
                }
            }
            var firstRow = _grid.FirstDisplayedScrollingRowIndex;
            _grid.DataSource = executions;
            if (firstRow > 0 && firstRow < _grid.RowCount) _grid.FirstDisplayedScrollingRowIndex = firstRow;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var column = _grid.Columns[e.ColumnIndex].Name;
            // The job's own row has no step - step 0, named (Job), as in the history report (dbo.ElasticJobHistory_Get)
            if (column is "step_id" or "step_name" && e.Value is null or DBNull
                && _grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var jobRow } && jobRow["step_id"] == DBNull.Value)
            {
                e.Value = column == "step_id" ? "0" : "(Job)";
                e.FormattingApplied = true;
                return;
            }
            if (e.Value == null || e.Value == DBNull.Value) return;
            switch (e.Value)
            {
                case DateTime utc:
                    e.Value = utc.ToAppTimeZone().ToString("G");
                    e.FormattingApplied = true;
                    break;

                case string lifecycle when column == "lifecycle":
                    var isActive = _grid.Rows[e.RowIndex].DataBoundItem is DataRowView { Row: var row } && Field<bool?>(row, "is_active") == true;
                    e.Value = ElasticJobTimelineModel.LifecycleText(lifecycle);
                    e.CellStyle.ForeColor = LifecycleColor(lifecycle, isActive);
                    e.FormattingApplied = true;
                    break;

                case string message when column == "last_message":
                    _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = message.Length > 2000 ? message[..2000] + "..." : message;
                    break;
            }
        }

        #endregion Grid

        #region Messaging

        /// <summary>
        /// Sends the message and waits for the reply.  Null if messaging isn't available (the reason is shown in <paramref name="statusControl"/>)
        /// or the dialog was closed while waiting.
        /// </summary>
        private async Task<MessagingHelper.MessageOutcome> SendAsync(JobActions action, Guid? executionId, Control statusControl)
        {
            if (_context.ImportAgentID == null)
            {
                SetStatus(statusControl, "No Import Agent", DashColors.Fail);
                return null;
            }
            var message = new ElasticJobExecutionMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                JobId = _jobId,
                JobExecutionId = executionId,
                JobAction = action,
                Lifetime = MessageLifetime
            };
            MessagingHelper.MessageOutcome outcome;
            try
            {
                outcome = await MessagingHelper.SendAndWaitAsync(message, (int)_context.ImportAgentID,
                    (status, _, color) => SetStatus(statusControl, status, color));
            }
            catch (Exception ex)
            {
                outcome = new MessagingHelper.MessageOutcome(false, ex.Message, null);
            }
            // Closed while waiting for the reply - nothing to show it in
            return IsDisposed ? null : outcome;
        }

        private bool ValidateMessagingAccess()
        {
            if (_context == null) return false;
            string error = null;
            if (!DBADashUser.AllowMessaging)
                error = "Sorry, you do not have access to use the messaging feature";
            else if (!DBADashUser.AllowJobExecution)
                error = "Sorry, you do not have access to execute jobs.";
            else if (!_context.CanMessage)
                error = "Messaging is not enabled";
            else if (_context.ImportAgentID == null)
                error = "No Import Agent";
            if (error == null) return true;
            SetStatus(_txtRequest, error, DashColors.Fail);
            _btnStart.Enabled = false;
            _btnStop.Enabled = false;
            _btnRefresh.Enabled = false;
            return false;
        }

        private static void SetStatus(Control control, string message, Color color)
        {
            if (control.IsDisposed) return;
            if (control.InvokeRequired)
            {
                control.Invoke(() => SetStatus(control, message, color));
                return;
            }
            control.Text = message;
            control.ForeColor = color;
        }

        #endregion Messaging
    }
}
