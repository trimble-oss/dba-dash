using DBADash.Messaging;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Lists the <b>existing</b> extended-events sessions on a monitored instance as a scrolling list of
    /// <see cref="XESessionRow"/> controls, each offering start / stop / watch and a link to the session DDL, plus a
    /// shortcut to launch a new ad-hoc trace.  Session listing, control and scripting go through
    /// <see cref="XESessionController"/>; watching opens an <see cref="XEWatchControl"/>.  Shown from the instance's
    /// "Extended Events" tree node.
    /// </summary>
    public sealed partial class ExtendedEventsViewer : UserControl, ISetContext
    {
        private DBADashContext _context;

        public ExtendedEventsViewer()
        {
            InitializeComponent();

            _refreshButton.Click += async (_, _) => await LoadSessionsAsync();
            _adhocButton.Click += (_, _) => XETraceLauncher.LaunchAdhocTrace(this, _context);

            this.ApplyTheme();
        }

        public void SetContext(DBADashContext context)
        {
            _context = context;
            var canManage = context is { InstanceID: > 0 } && context.CanMessage && DBADashUser.AllowManageXE;
            _refreshButton.Enabled = canManage;
            _adhocButton.Visible = context is { InstanceID: > 0 } && context.CanMessage && DBADashUser.AllowXETrace;
            if (!canManage)
            {
                ClearRows();
                SetStatus("Managing extended events isn't enabled for you on this instance.", DashColors.Warning);
                return;
            }
            _ = LoadSessionsAsync();
        }

        private async Task LoadSessionsAsync()
        {
            if (_context is not { InstanceID: > 0 }) return;
            SetStatus("Loading sessions...", DashColors.Information);
            try
            {
                var dt = await XESessionController.ListSessionsAsync(_context, ControllerStatus);
                if (dt == null)
                {
                    SetStatus("No response from the service.", DashColors.Fail);
                    return;
                }
                BuildRows(dt);
                SetStatus($"{dt.Rows.Count} session(s).", DashColors.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
            }
        }

        private void BuildRows(DataTable dt)
        {
            _list.SuspendLayout();
            ClearRows();
            foreach (DataRow dataRow in dt.Rows)
            {
                var row = new XESessionRow();
                row.Bind(dataRow);
                row.SetPolicy(_context.CanManageXESession(row.SessionName), _context.CanWatchXESession(row.SessionName));
                row.StartStopClicked += async (s, _) => await OnRowStartStop((XESessionRow)s);
                row.WatchClicked += (s, _) => OnRowWatch((XESessionRow)s);
                row.ViewDataClicked += (s, _) => OnRowViewData((XESessionRow)s);
                row.NameClicked += async (s, _) => await OnRowScript((XESessionRow)s);
                _list.Controls.Add(row);
            }
            _list.ResumeLayout();
            _list.ApplyTheme(DBADashUser.SelectedTheme);
        }

        private void ClearRows()
        {
            var rows = _list.Controls;
            for (var i = rows.Count - 1; i >= 0; i--)
            {
                var c = rows[i];
                rows.RemoveAt(i);
                c.Dispose();
            }
        }

        private async Task OnRowStartStop(XESessionRow row)
        {
            if (string.IsNullOrEmpty(row.SessionName)) return;
            await ControlSessionAsync(row.SessionName, row.IsRunning ? XESessionOperation.Stop : XESessionOperation.Start);
        }

        private void OnRowWatch(XESessionRow row)
        {
            if (row.IsRunning && !string.IsNullOrEmpty(row.SessionName)) WatchSession(row.SessionName);
        }

        private void OnRowViewData(XESessionRow row)
        {
            if (row.IsRunning && !string.IsNullOrEmpty(row.SessionName))
            {
                XETraceLauncher.LaunchViewData(this, _context, row.SessionName);
            }
        }

        private async Task OnRowScript(XESessionRow row)
        {
            if (string.IsNullOrEmpty(row.SessionName)) return;
            SetStatus($"Scripting {row.SessionName}...", DashColors.Information);
            try
            {
                var ddl = await XESessionController.ScriptSessionAsync(_context, row.SessionName, ControllerStatus);
                if (string.IsNullOrEmpty(ddl))
                {
                    SetStatus($"Couldn't script {row.SessionName}.", DashColors.Warning);
                    return;
                }
                Common.ShowCodeViewer(ddl, $"Extended Events - {row.SessionName}");
                SetStatus($"Scripted {row.SessionName}.", DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task ControlSessionAsync(string name, XESessionOperation operation)
        {
            var starting = operation == XESessionOperation.Start;
            var verb = starting ? "started" : "stopped";
            SetStatus($"{operation} {name}...", DashColors.Information);
            try
            {
                var outcome = await XESessionController.ControlSessionAsync(_context, name, operation, ControllerStatus);
                await LoadSessionsAsync(); // refresh the list to reflect the real state either way

                if (!outcome.Ok)
                {
                    // The service rejected the request (e.g. not permitted by the manageable-sessions list, or a SQL
                    // permission error).  Surface the real reason - never report a phantom success.
                    var msg = string.IsNullOrWhiteSpace(outcome.Message)
                        ? $"{name} could not be {verb}."
                        : outcome.Message;
                    SetStatus(msg, DashColors.Fail);
                    MessageBox.Show(this, msg, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Accepted - but confirm it actually took effect (the reported running state must match the request).
                if (outcome.Running.HasValue && outcome.Running.Value != starting)
                {
                    var msg = $"{name} was not {verb} - it is still {(outcome.Running.Value ? "running" : "stopped")}.";
                    SetStatus(msg, DashColors.Warning);
                    MessageBox.Show(this, msg, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SetStatus($"{name} {verb}.", DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void WatchSession(string name)
        {
            XETraceLauncher.LaunchWatch(this, _context, name);
        }

        private void SetStatus(string message, Color color)
        {
            if (_statusStrip.InvokeRequired)
            {
                _statusStrip.Invoke(new Action(() => SetStatus(message, color)));
                return;
            }
            _status.Text = message;
            _status.ForeColor = color;
        }

        private void ControllerStatus(string message, string details, Color color)
        {
            if (color == DashColors.Fail || color == DashColors.Warning)
            {
                SetStatus(message, color);
            }
        }
    }
}
