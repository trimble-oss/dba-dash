using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Live viewer for an <b>existing</b> XE session.  Non-destructively tails the session's target (event_file or
    /// ring_buffer) via <see cref="XESessionController.WatchAsync"/> and streams events into an
    /// <see cref="XEResultsControl"/>.  The session is never started/stopped by watching - disposing the control (or
    /// Stop) just ends the tail.
    /// </summary>
    /// <remarks>
    /// A <see cref="UserControl"/> rather than a form so it can be hosted in a window (see
    /// <see cref="XETraceLauncher.LaunchWatch"/>), a tab or any other container.  Call <see cref="Watch"/> after the
    /// control has a handle so async UI updates can marshal correctly.
    /// </remarks>
    public sealed partial class XEWatchControl : UserControl
    {
        private DBADashContext _context;
        private string _sessionName;
        private int _durationSeconds;

        private Guid _messageGroup;
        private bool _watching;
        private bool _cancelling;

        // Keeps the watch alive: while it runs we beat every XETraceHeartbeat.IntervalSeconds so the service knows the
        // client is still here; if the beats stop the service ends the watch rather than polling to its deadline.  A
        // background timer (not the UI thread) so a busy UI can't stall the beats and cause a false "client gone" stop.
        private System.Threading.Timer _heartbeatTimer;
        private int _heartbeatInFlight; // 0/1 guard (Interlocked) so a slow beat can't stack with the next tick

        public XEWatchControl()
        {
            InitializeComponent();

            _stopButton.Click += async (_, _) => await StopAsync();
            _clearButton.Click += (_, _) => _results.Clear();

            this.ApplyTheme();
        }

        /// <summary>Begins tailing the given session.  Call once the control is hosted and has a handle.</summary>
        public void Watch(DBADashContext context, string sessionName, int durationSeconds = 3600)
        {
            _context = context;
            _sessionName = sessionName;
            _durationSeconds = durationSeconds;
            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            _cancelling = false;
            _watching = true;
            _messageGroup = Guid.NewGuid();
            _stopButton.Enabled = true;
            SetStatus($"Watching {_sessionName}...", DashColors.Information);
            StartHeartbeat();

            try
            {
                var outcome = await XESessionController.WatchAsync(_context, _sessionName, _durationSeconds,
                    _messageGroup, ControllerStatus, AppendBatchAsync, OnSummary);

                if (_cancelling)
                {
                    SetStatus("Watch stopped.", DashColors.Information);
                }
                else if (outcome is { Ok: false })
                {
                    SetStatus(outcome.Message, DashColors.Fail);
                    if (!IsDisposed)
                    {
                        MessageBox.Show(this, outcome.Message, "Watch XE Session", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
            }
            finally
            {
                _watching = false;
                StopHeartbeat();
                if (!IsDisposed) _stopButton.Enabled = false;
            }
        }

        /// <summary>Starts beating for the current watch.  Every watch beats; a short one ends before the first beat.</summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            var group = _messageGroup;
            var context = _context;
            var interval = TimeSpan.FromSeconds(XETraceHeartbeat.IntervalSeconds);
            _heartbeatTimer = new System.Threading.Timer(_ => SendHeartbeat(context, group), null, interval, interval);
        }

        private void StopHeartbeat()
        {
            var timer = _heartbeatTimer;
            _heartbeatTimer = null;
            timer?.Dispose();
        }

        private void SendHeartbeat(DBADashContext context, Guid group)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _heartbeatInFlight, 1, 0) != 0) return; // already in flight
            _ = SendHeartbeatAsync(context, group);
        }

        private async Task SendHeartbeatAsync(DBADashContext context, Guid group)
        {
            try { await MessagingHelper.SendHeartbeatAsync(context, group); }
            catch (Exception ex) { Serilog.Log.Debug(ex, "Error sending XE watch heartbeat for {group}", group); }
            finally { System.Threading.Interlocked.Exchange(ref _heartbeatInFlight, 0); }
        }

        private Task AppendBatchAsync(DataTable batch)
        {
            if (IsDisposed) return Task.CompletedTask;
            // Marshal to the UI thread before touching the grid (mirrors QuickXETrace.AppendEventsAsync); harmless
            // no-op when already on it, but guards against a reply continuation resuming off the UI thread.
            if (InvokeRequired) return (Task)Invoke(new Func<Task>(() => AppendBatchAsync(batch)));
            if (IsDisposed) return Task.CompletedTask; // re-check after marshalling
            _results.AppendEvents(batch);
            SetStatus($"Watching {_sessionName}.  Collected {_results.RowCount} events.", DashColors.Information);
            return Task.CompletedTask;
        }

        private void OnSummary(DataRow summary)
        {
            var total = summary.Table.Columns.Contains("TotalEvents") ? summary["TotalEvents"] : 0;
            SetStatus($"Watch complete.  Collected {total} events.", DashColors.Success);
        }

        private async Task StopAsync()
        {
            if (_cancelling || !_watching) return;
            _cancelling = true;
            _stopButton.Enabled = false;
            SetStatus("Stop requested...", DashColors.Warning);
            try { await XESessionController.CancelWatchAsync(_context, _messageGroup, ControllerStatus); }
            catch (Exception ex) { SetStatus(ex.Message, DashColors.Fail); }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_watching && !_cancelling)
            {
                // Stop the tail so the service watch loop ends promptly rather than running to its deadline.
                _cancelling = true;
                StopHeartbeat();
                try { _ = XESessionController.CancelWatchAsync(_context, _messageGroup, ControllerStatus); }
                catch { /* best-effort on close */ }
            }
            base.OnHandleDestroyed(e);
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

        // Only surface errors/warnings from the messaging layer; routine progress is shown by our own handlers.
        private void ControllerStatus(string message, string details, Color color)
        {
            if (color == DashColors.Fail || color == DashColors.Warning)
            {
                SetStatus(message, color);
            }
        }
    }
}
