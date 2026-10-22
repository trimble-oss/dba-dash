using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// One execution of an agent job: a timeline of its steps, a row per step history record (retries included), and the
    /// full message of the selected step.  Opened from a bar on the job timeline.  Previous/Next move through the job's
    /// other executions.
    /// </summary>
    [DesignerCategory("")]
    internal sealed class JobExecutionForm : Form
    {
        private readonly int _instanceId;
        private readonly Guid _jobId;
        private readonly string _jobName;

        /// <summary>msdb instance_id of a history row of the execution shown.  Null while it's still running.</summary>
        private int? _historyId;

        /// <summary>
        /// For a running execution, a time inside it (UTC).  Lets a refresh find it once it has finished, rather than
        /// moving on to whatever runs next.  Null with no <see cref="_historyId"/> means the execution running now.
        /// </summary>
        private DateTime? _runningAtUtc;

        /// <summary>History row to select on the next load - the step clicked on the timeline.  Used once.</summary>
        private int? _selectHistoryId;

        /// <summary>Start afresh on the next load, after moving to another execution.</summary>
        private bool _newExecution = true;

        private int? _previousId;
        private int? _nextId;
        private bool _nextIsRunning;

        private DataTable _dt;
        private DateTime _startUtc;
        private DateTime _endUtc;
        private bool _syncing;

        private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly Label _summary = new() { Dock = DockStyle.Top, AutoSize = false, Padding = new Padding(8, 6, 8, 4), Font = new Font("Segoe UI", 9.75f) };
        private readonly SplitContainer _outer = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        private readonly SplitContainer _inner = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        private readonly JobTimelineChart _chart = new() { Dock = DockStyle.Fill, BarActivatedText = "Show step", ActivateHint = false, Title = "Steps" };
        private readonly DataGridView _grid;
        private readonly TextBox _message = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Font = new Font("Consolas", 10f) };
        private readonly ToolStripButton _previous;
        private readonly ToolStripButton _next;
        private readonly ToolStripButton _runningQueries;
        private readonly ToolStripButton _formatMessage;

        /// <param name="historyId">msdb instance_id of any history row of the execution</param>
        /// <param name="runningAtUtc">With no <paramref name="historyId"/>: a time inside an execution that was running (the bar's start)</param>
        public JobExecutionForm(int instanceId, Guid jobId, string jobName, int? historyId, DateTime? runningAtUtc)
        {
            _instanceId = instanceId;
            _jobId = jobId;
            _jobName = jobName;
            _historyId = historyId;
            _runningAtUtc = historyId.HasValue ? null : runningAtUtc;
            _selectHistoryId = historyId;

            Text = $"Job Execution: {jobName}";
            Size = LogicalToDeviceUnits(new Size(1100, 760));
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape && !_chart.ContainsFocus) Close();
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ShowCellToolTips = true
            };
            _grid.Columns.AddRange(
                Column("step_id", "Step", 50),
                Column("step_name", "Step Name", 180),
                Column("Status", "Status", 90),
                Column("RunDateTime", "Start", 155, "G"),
                Column("FinishDateTime", "Finish", 155, "G"),
                Column("Duration", "Duration", 85),
                Column("Typical", "Typical", 85),
                Column("retries_attempted", "Retries", 60),
                Column("sql_message_id", "Message ID", 95),
                Column("sql_severity", "Severity", 65),
                Column("message", "Message", 300));
            _grid.Columns["Typical"].ToolTipText = "Average duration of the last 30 successful runs before this execution";
            _grid.Columns["message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
            _grid.SelectionChanged += Grid_SelectionChanged;

            _chart.BarActivated += (_, bar) => SelectGridRow(bar);
            _chart.SelectedBarChanged += (_, bar) => SelectGridRow(bar);
            _chart.EmptyText = "No history found for this execution";

            var refresh = new ToolStripButton("Refresh", Properties.Resources._112_RefreshArrow_Green_16x16_72, (_, _) => LoadData()) { DisplayStyle = ToolStripItemDisplayStyle.Image };
            _previous = new ToolStripButton("Previous", Properties.Resources.Previous_grey_16x, (_, _) => ShowExecution(_previousId, false)) { ToolTipText = "Previous execution of this job" };
            _next = new ToolStripButton("Next", Properties.Resources.Next_grey_16x, (_, _) => ShowExecution(_nextId, _nextIsRunning))
            {
                ToolTipText = "Next execution of this job",
                TextImageRelation = TextImageRelation.TextBeforeImage
            };
            var openJob = new ToolStripButton("Open Job", Properties.Resources.NewWindow_16x, (_, _) => OpenJob());
            _runningQueries = new ToolStripButton("Running Queries", Properties.Resources.Watch_16x, (_, _) => ShowRunningQueries())
            {
                ToolTipText = "Show running queries captured for this job during the execution"
            };
            var copy = new ToolStripButton("Copy", Properties.Resources.ASX_Copy_blue_16x, (_, _) => CopyDetails()) { ToolTipText = "Copy the execution details as text" };
            _formatMessage = new ToolStripButton("Format Message", Properties.Resources.WordWrap_16x)
            {
                CheckOnClick = true,
                Checked = true,
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Put each message from the step output on its own line"
            };
            _formatMessage.CheckedChanged += (_, _) => ShowMessage();
            _toolStrip.Items.AddRange(new ToolStripItem[] { refresh, new ToolStripSeparator(), _previous, _next, new ToolStripSeparator(), openJob, _runningQueries, copy, _formatMessage });

            _inner.Panel1.Controls.Add(_grid);
            _inner.Panel2.Controls.Add(_message);
            _outer.Panel1.Controls.Add(_chart);
            _outer.Panel2.Controls.Add(_inner);
            Controls.Add(_outer);
            Controls.Add(_summary);
            Controls.Add(_toolStrip);

            Load += (_, _) =>
            {
                _summary.Height = _summary.Font.Height * 2 + _summary.Padding.Vertical + LogicalToDeviceUnits(4);
                _outer.SplitterDistance = Math.Max(LogicalToDeviceUnits(120), _outer.Height * 2 / 5);
                _inner.SplitterDistance = Math.Max(LogicalToDeviceUnits(80), _inner.Height * 3 / 5);
                LoadData();
            };
            this.ApplyTheme();
        }

        private DataGridViewTextBoxColumn Column(string name, string header, int width, string format = null) => new()
        {
            Name = name,
            DataPropertyName = name,
            HeaderText = header,
            Width = LogicalToDeviceUnits(width),
            DefaultCellStyle = format == null ? new DataGridViewCellStyle() : new DataGridViewCellStyle { Format = format },
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        private DataSet GetExecution()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobExecution_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", _instanceId);
            cmd.Parameters.AddWithValue("@job_id", _jobId);
            cmd.Parameters.AddWithValue("@instance_id", _historyId.HasValue ? _historyId.Value : DBNull.Value);
            cmd.Parameters.Add("@RunDateTime", SqlDbType.DateTime2).Value = _runningAtUtc.HasValue ? _runningAtUtc.Value : DBNull.Value;
            var ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        /// <summary>Move to another execution of the job: a history row, or (<paramref name="running"/>) the one running now.</summary>
        private void ShowExecution(int? historyId, bool running)
        {
            if (!historyId.HasValue && !running) return;
            _historyId = historyId;
            _runningAtUtc = null;
            _selectHistoryId = null;
            _newExecution = true;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var selected = _newExecution ? null : _chart.SelectedBar;
                var ds = GetExecution();
                var dt = ds.Tables[0];
                var neighbours = ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0 ? ds.Tables[1].Rows[0] : null;
                _previousId = neighbours?["PreviousExecutionID"] as int?;
                _nextId = neighbours?["NextExecutionID"] as int?;
                _nextIsRunning = neighbours?["NextIsRunning"] is true;
                _previous.Enabled = _previousId.HasValue;
                _next.Enabled = _nextId.HasValue || _nextIsRunning;

                if (dt.Rows.Count > 0)
                {
                    _startUtc = dt.AsEnumerable().Min(r => (DateTime)r["RunDateTime"]);
                    _endUtc = dt.AsEnumerable().Max(r => (DateTime)r["FinishDateTime"]);
                }
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns.Add("Status", typeof(string));
                dt.Columns.Add("Duration", typeof(string));
                dt.Columns.Add("Typical", typeof(string));
                dt.Columns.Add("IsSlow", typeof(bool));
                foreach (DataRow r in dt.Rows)
                {
                    var status = (RunStatus)(int)r["run_status"];
                    var duration = (int)r["RunDurationSec"];
                    r["Status"] = status == RunStatus.InProgress ? "In Progress" : status.ToString();
                    r["Duration"] = JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(duration), true);
                    if (r["TypicalDurationSec"] is long typical)
                    {
                        r["Typical"] = JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(typical), true);
                        r["IsSlow"] = IsSlow(duration, typical);
                    }
                    else
                    {
                        r["IsSlow"] = false;
                    }
                }

                // Pin the form to the execution: once a running one has finished, a refresh stays on it rather than
                // following the next run.  While it's running, remember a time inside it for the same reason.
                var outcome = dt.AsEnumerable().FirstOrDefault(r => (int)r["step_id"] == 0);
                if (!_historyId.HasValue && outcome != null)
                {
                    if (outcome["instance_id"] is int outcomeId)
                    {
                        _historyId = outcomeId;
                        _runningAtUtc = null;
                    }
                    else
                    {
                        _runningAtUtc = _startUtc;
                    }
                }

                _dt = dt;
                _summary.Text = Summary(dt);
                _runningQueries.Enabled = dt.Rows.Count > 0;

                var model = JobTimelineModel.Build(dt, singleJob: true, includeSteps: true);
                var (from, to) = Period(dt);
                _chart.SetData(model, from, to, _newExecution);
                _newExecution = false;

                _syncing = true;
                try
                {
                    _grid.DataSource = dt;
                }
                finally
                {
                    _syncing = false;
                }

                if (selected != null)
                {
                    SelectRow(r => (int)r["step_id"] == selected.StepId && (DateTime)r["RunDateTime"] == selected.Start);
                }
                else if (_selectHistoryId.HasValue && dt.AsEnumerable().Any(r => r["instance_id"] is int id && id == _selectHistoryId && (int)r["step_id"] != 0))
                {
                    SelectRow(r => r["instance_id"] is int id && id == _selectHistoryId);
                }
                else
                {
                    // Start on the step that failed - that's usually why someone is looking - then any other step that didn't
                    // succeed, then a step that ran long, else the outcome.
                    SelectRow(r => (int)r["step_id"] != 0 && (int)r["run_status"] == (int)RunStatus.Failed);
                    if (_grid.SelectedRows.Count == 0) SelectRow(r => (int)r["step_id"] != 0 && (int)r["run_status"] != (int)RunStatus.Succeeded);
                    if (_grid.SelectedRows.Count == 0) SelectRow(r => (int)r["step_id"] != 0 && (bool)r["IsSlow"]);
                    if (_grid.SelectedRows.Count == 0) SelectRow(r => (int)r["step_id"] == 0);
                }
                _selectHistoryId = null;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading job execution");
            }
        }

        /// <summary>At least twice the typical duration, and a minute longer - so a 2 second step taking 5 isn't flagged.</summary>
        private static bool IsSlow(long durationSec, long typicalSec) => durationSec >= typicalSec * 2 && durationSec - typicalSec >= 60;

        /// <summary>The span of the execution, with a little room either side so the bars aren't against the edges.</summary>
        private static (DateTime From, DateTime To) Period(DataTable dt)
        {
            if (dt.Rows.Count == 0) return (DateTime.Now.AddMinutes(-1), DateTime.Now);
            var start = dt.AsEnumerable().Min(r => (DateTime)r["RunDateTime"]);
            var end = dt.AsEnumerable().Max(r => (DateTime)r["FinishDateTime"]);
            var margin = TimeSpan.FromTicks(Math.Max((end - start).Ticks / 40, TimeSpan.TicksPerSecond));
            return (start - margin, end + margin);
        }

        private string Summary(DataTable dt)
        {
            if (dt.Rows.Count == 0) return $"{_jobName}{Environment.NewLine}No history found for this execution.  It may have been purged from the repository.";
            var outcome = dt.AsEnumerable().FirstOrDefault(r => (int)r["step_id"] == 0);
            var steps = dt.AsEnumerable().Where(r => (int)r["step_id"] != 0).ToList();
            var sb = new StringBuilder(_jobName).AppendLine();
            if (outcome != null)
            {
                sb.Append((string)outcome["Status"]);
                sb.Append($"  ·  Started {(DateTime)outcome["RunDateTime"]:G}");
                sb.Append((int)outcome["run_status"] == (int)RunStatus.InProgress ? $"  ·  Elapsed {outcome["Duration"]}" : $"  ·  Finished {(DateTime)outcome["FinishDateTime"]:G}  ·  Duration {outcome["Duration"]}");
                if (outcome["Typical"] is string typical) sb.Append($" (typical {typical})");
            }
            else
            {
                sb.Append("No job outcome recorded");
            }
            sb.Append($"  ·  {steps.Select(r => (int)r["step_id"]).Distinct().Count()} steps run");
            var retries = steps.Count(r => (int)r["run_status"] == (int)RunStatus.Retry);
            if (retries > 0) sb.Append($"  ·  {retries} {(retries == 1 ? "retry" : "retries")}");
            var failed = steps.Count(r => (int)r["run_status"] == (int)RunStatus.Failed);
            if (failed > 0) sb.Append($"  ·  {failed} failed");
            var slow = steps.Count(r => (bool)r["IsSlow"]);
            if (slow > 0) sb.Append($"  ·  {slow} ran long");
            return sb.ToString();
        }

        private void SelectRow(Func<DataRow, bool> match)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.DataBoundItem is DataRowView drv && match(drv.Row))
                {
                    row.Selected = true;
                    _grid.CurrentCell = row.Cells[0];
                    return;
                }
            }
            _grid.ClearSelection();
        }

        private void SelectGridRow(TimelineBar bar)
        {
            if (_syncing || bar == null || _dt == null) return;
            _syncing = true;
            try
            {
                SelectRow(r => _dt.Rows.IndexOf(r) == bar.SourceIndex);
                ShowMessage();
            }
            finally
            {
                _syncing = false;
            }
        }

        private DataRow SelectedRow => _grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].DataBoundItem is DataRowView drv ? drv.Row : null;

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            ShowMessage();
            if (_syncing) return;
            var row = SelectedRow;
            if (row == null) return;
            var index = _dt.Rows.IndexOf(row);
            _syncing = true;
            try
            {
                _chart.SelectBar(b => b.SourceIndex == index);
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ShowMessage()
        {
            var row = SelectedRow;
            _message.Text = row == null ? string.Empty : StepDetails(row, _formatMessage.Checked);
        }

        private static string StepDetails(DataRow r, bool format)
        {
            var sb = new StringBuilder();
            var stepId = (int)r["step_id"];
            sb.AppendLine(stepId == 0 ? "Job outcome" : $"Step {stepId}: {r["step_name"]}");
            sb.Append($"Status: {r["Status"]}    Start: {(DateTime)r["RunDateTime"]:G}    Duration: {r["Duration"]}");
            if (r["Typical"] is string typical) sb.Append($" (typical {typical})");
            if (r["retries_attempted"] is int retries && retries > 0) sb.Append($"    Retries: {retries}");
            if (r["sql_message_id"] is int msgId && msgId != 0) sb.Append($"    Message ID: {msgId}");
            if (r["sql_severity"] is int severity && severity != 0) sb.Append($"    Severity: {severity}");
            sb.AppendLine().AppendLine();
            var message = Convert.ToString(r["message"]);
            sb.Append(format ? FormatAgentMessage(message) : message);
            return sb.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Agent stores a step's output as one line.  For a T-SQL step each message printed ends with its SQLSTATE and
        /// number, e.g. "Done [SQLSTATE 01000] (Message 50000)", and Agent's own sentences are separated by two spaces
        /// ("Executed as user: X.  The step failed.").  Break the text at those points.
        /// </summary>
        internal static string FormatAgentMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            // Keep a full stop that follows the last message on its line: "(Message 50000).  The step succeeded."
            var text = Regex.Replace(message, @"(\[SQLSTATE \w{5}\]\s*\((?:Message|Error) -?\d+\)\.?)\s*", "$1\n");
            text = Regex.Replace(text, @"\.\s{2,}(?=\S)", ".\n");
            return text.Trim();
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;
            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case "Status":
                    {
                        var color = TimelineBar.StatusColor((RunStatus)(int)drv["run_status"]);
                        e.CellStyle.BackColor = color;
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.SelectionBackColor = ControlPaint.Dark(color, 0.05f);
                        e.CellStyle.SelectionForeColor = Color.White;
                        break;
                    }
                case "Duration" when drv["IsSlow"] is true:
                    e.CellStyle.BackColor = DashColors.Warning;
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.SelectionBackColor = ControlPaint.Dark(DashColors.Warning, 0.05f);
                    e.CellStyle.SelectionForeColor = Color.White;
                    break;
            }
        }

        private void Grid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Duration") return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is DataRowView drv && drv["IsSlow"] is true)
            {
                e.ToolTipText = $"Ran long: at least twice the typical duration of {drv["Typical"]}";
            }
        }

        private void OpenJob()
        {
            try
            {
                var jobContext = CommonData.GetDBADashContext(_instanceId);
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = _jobId;
                jobContext.ObjectName = _jobName;
                new JobInfoForm { DBADashContext = jobContext }.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void ShowRunningQueries()
        {
            var viewer = new RunningQueriesViewer { SnapshotDateFrom = _startUtc, SnapshotDateTo = _endUtc, InstanceID = _instanceId, JobId = _jobId };
            viewer.ShowSingleInstance();
        }

        private void CopyDetails()
        {
            if (_dt == null) return;
            var sb = new StringBuilder(_summary.Text).AppendLine().AppendLine();
            foreach (DataRow r in _dt.Rows)
            {
                sb.AppendLine(StepDetails(r, _formatMessage.Checked)).AppendLine();
            }
            Clipboard.SetText(sb.ToString());
        }
    }
}
