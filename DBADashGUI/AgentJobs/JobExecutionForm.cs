using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// One execution of an agent job: a timeline of its steps, a row per step history record (retries included), and the
    /// full message of the selected step.  Opened from a bar on the job timeline.
    /// </summary>
    [DesignerCategory("")]
    internal sealed class JobExecutionForm : Form
    {
        private readonly int _instanceId;
        private readonly Guid _jobId;
        private readonly string _jobName;
        private int? _historyId;

        /// <summary>The history row to select when the data first loads - the step that was clicked on the timeline.</summary>
        private readonly int? _initialSelection;

        private DataTable _dt;
        private DateTime _startUtc;
        private DateTime _endUtc;
        private bool _syncing;

        private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly Label _summary = new() { Dock = DockStyle.Top, AutoSize = false, Height = 52, Padding = new Padding(8, 6, 8, 4), Font = new Font("Segoe UI", 9.75f) };
        private readonly SplitContainer _outer = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        private readonly SplitContainer _inner = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        private readonly JobTimelineChart _chart = new() { Dock = DockStyle.Fill, BarActivatedText = "Show step", ActivateHint = false };
        private readonly DataGridView _grid;
        private readonly TextBox _message = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Font = new Font("Consolas", 10f) };
        private readonly ToolStripButton _runningQueries;

        /// <param name="historyId">msdb instance_id of any history row of the execution.  Null for the execution that is running now.</param>
        public JobExecutionForm(int instanceId, Guid jobId, string jobName, int? historyId)
        {
            _instanceId = instanceId;
            _jobId = jobId;
            _jobName = jobName;
            _historyId = historyId;
            _initialSelection = historyId;

            Text = $"Job Execution: {jobName}";
            Size = new Size(1100, 760);
            StartPosition = FormStartPosition.CenterParent;

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
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            };
            _grid.Columns.AddRange(
                Column("step_id", "Step", 50),
                Column("step_name", "Step Name", 180),
                Column("Status", "Status", 90),
                Column("RunDateTime", "Start", 155, "G"),
                Column("FinishDateTime", "Finish", 155, "G"),
                Column("Duration", "Duration", 90),
                Column("retries_attempted", "Retries", 60),
                Column("sql_message_id", "Message ID", 95),
                Column("sql_severity", "Severity", 65),
                Column("message", "Message", 300));
            _grid.Columns["message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += Grid_SelectionChanged;

            _chart.BarActivated += (_, bar) => SelectGridRow(bar);
            _chart.SelectedBarChanged += (_, bar) => SelectGridRow(bar);
            _chart.EmptyText = "No history found for this execution";

            var refresh = new ToolStripButton("Refresh", Properties.Resources._112_RefreshArrow_Green_16x16_72, (_, _) => LoadData()) { DisplayStyle = ToolStripItemDisplayStyle.Image };
            var openJob = new ToolStripButton("Open Job", Properties.Resources.NewWindow_16x, (_, _) => OpenJob());
            _runningQueries = new ToolStripButton("Running Queries", Properties.Resources.Watch_16x, (_, _) => ShowRunningQueries())
            {
                ToolTipText = "Show running queries captured for this job during the execution"
            };
            var copy = new ToolStripButton("Copy", Properties.Resources.ASX_Copy_blue_16x, (_, _) => CopyDetails()) { ToolTipText = "Copy the execution details as text" };
            _toolStrip.Items.AddRange(new ToolStripItem[] { refresh, openJob, _runningQueries, copy });

            _inner.Panel1.Controls.Add(_grid);
            _inner.Panel2.Controls.Add(_message);
            _outer.Panel1.Controls.Add(_chart);
            _outer.Panel2.Controls.Add(_inner);
            Controls.Add(_outer);
            Controls.Add(_summary);
            Controls.Add(_toolStrip);

            Load += (_, _) =>
            {
                _outer.SplitterDistance = Math.Max(120, _outer.Height * 2 / 5);
                _inner.SplitterDistance = Math.Max(80, _inner.Height * 3 / 5);
                LoadData();
            };
            this.ApplyTheme();
        }

        private static DataGridViewTextBoxColumn Column(string name, string header, int width, string format = null) => new()
        {
            Name = name,
            DataPropertyName = name,
            HeaderText = header,
            Width = width,
            DefaultCellStyle = format == null ? new DataGridViewCellStyle() : new DataGridViewCellStyle { Format = format },
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        private DataTable GetExecution()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobExecution_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("@InstanceID", _instanceId);
            cmd.Parameters.AddWithValue("@job_id", _jobId);
            cmd.Parameters.AddWithValue("@instance_id", _historyId.HasValue ? _historyId.Value : DBNull.Value);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private void LoadData()
        {
            try
            {
                var selected = _chart.SelectedBar;
                var dt = GetExecution();
                if (dt.Rows.Count > 0)
                {
                    _startUtc = dt.AsEnumerable().Min(r => (DateTime)r["RunDateTime"]);
                    _endUtc = dt.AsEnumerable().Max(r => (DateTime)r["FinishDateTime"]);
                }
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dt.Columns.Add("Status", typeof(string));
                dt.Columns.Add("Duration", typeof(string));
                foreach (DataRow r in dt.Rows)
                {
                    var status = (RunStatus)(int)r["run_status"];
                    r["Status"] = status == RunStatus.InProgress ? "In Progress" : status.ToString();
                    r["Duration"] = JobTimelineChart.FormatDuration(TimeSpan.FromSeconds((int)r["RunDurationSec"]), true);
                }

                // Once a running execution finishes, pin the form to it rather than following the next one
                var outcome = dt.AsEnumerable().FirstOrDefault(r => (int)r["step_id"] == 0);
                if (!_historyId.HasValue && outcome != null && outcome["instance_id"] != DBNull.Value) _historyId = (int)outcome["instance_id"];

                _dt = dt;
                _summary.Text = Summary(dt);
                _runningQueries.Enabled = dt.Rows.Count > 0;

                var model = JobTimelineModel.Build(dt, singleJob: true, includeSteps: true);
                var (from, to) = Period(dt);
                _chart.Title = "Steps";
                _chart.SetData(model, from, to);

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
                else if (_initialSelection.HasValue && dt.AsEnumerable().Any(r => r["instance_id"] is int id && id == _initialSelection && (int)r["step_id"] != 0))
                {
                    SelectRow(r => r["instance_id"] is int id && id == _initialSelection);
                }
                else
                {
                    // Start on the step that failed - that's usually why someone is looking - then any other step that didn't succeed, else the outcome
                    SelectRow(r => (int)r["step_id"] != 0 && (int)r["run_status"] == (int)RunStatus.Failed);
                    if (_grid.SelectedRows.Count == 0) SelectRow(r => (int)r["step_id"] != 0 && (int)r["run_status"] != (int)RunStatus.Succeeded);
                    if (_grid.SelectedRows.Count == 0) SelectRow(r => (int)r["step_id"] == 0);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading job execution");
            }
        }

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
            _message.Text = row == null ? string.Empty : StepDetails(row);
        }

        private static string StepDetails(DataRow r)
        {
            var sb = new StringBuilder();
            var stepId = (int)r["step_id"];
            sb.AppendLine(stepId == 0 ? "Job outcome" : $"Step {stepId}: {r["step_name"]}");
            sb.Append($"Status: {r["Status"]}    Start: {(DateTime)r["RunDateTime"]:G}    Duration: {r["Duration"]}");
            if (r["retries_attempted"] is int retries && retries > 0) sb.Append($"    Retries: {retries}");
            if (r["sql_message_id"] is int msgId && msgId != 0) sb.Append($"    Message ID: {msgId}");
            if (r["sql_severity"] is int severity && severity != 0) sb.Append($"    Severity: {severity}");
            sb.AppendLine().AppendLine();
            sb.Append(Convert.ToString(r["message"]));
            return sb.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Status") return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;
            var color = TimelineBar.StatusColor((RunStatus)(int)drv["run_status"]);
            e.CellStyle.BackColor = color;
            e.CellStyle.ForeColor = Color.White;
            e.CellStyle.SelectionBackColor = ControlPaint.Dark(color, 0.05f);
            e.CellStyle.SelectionForeColor = Color.White;
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
                sb.AppendLine(StepDetails(r)).AppendLine();
            }
            Clipboard.SetText(sb.ToString());
        }
    }
}
