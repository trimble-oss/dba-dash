using DBADash.Messaging;
using DBADashGUI.Theme;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// One-shot viewer for the <b>existing</b> captured data in an XE session's target (event_file preferred,
    /// ring_buffer fallback).  Loads the current target contents once via
    /// <see cref="XESessionController.ViewTargetDataAsync"/> into a shared <see cref="XEResultsControl"/>.  Read-only
    /// and non-destructive - unlike <see cref="XEWatchControl"/> it doesn't tail; Refresh re-reads the target's
    /// current contents.  Hosted in a window by <see cref="XETraceLauncher.LaunchViewData"/>.
    /// </summary>
    public sealed partial class XEViewDataControl : UserControl
    {
        // Range preset -> lower bound.  "All" reads the whole target (slower on a large history).  A recent range
        // usually lives in the current rollover file, so the service reads far less than the whole target.
        private const string RangeLastHour = "Last hour";

        private const string RangeLastDay = "Last 24 hours";
        private const string RangeLast7Days = "Last 7 days";
        private const string RangeAll = "All";

        private DBADashContext _context;
        private string _sessionName;
        private bool _loading;

        public XEViewDataControl()
        {
            InitializeComponent();

            // Combo contents reference runtime constants, so they're populated here rather than in the designer.
            _rangeCombo.Items.AddRange(new object[] { RangeLastHour, RangeLastDay, RangeLast7Days, RangeAll });
            _rangeCombo.SelectedItem = RangeLastDay;
            _maxRowsCombo.Items.AddRange(new object[] { "1000", "10000", "50000", "100000" });
            _maxRowsCombo.Text = XEViewTargetDataMessage.DefaultMaxEvents.ToString();

            _refreshButton.Click += async (_, _) => await LoadAsync();
            _rangeCombo.SelectedIndexChanged += async (_, _) => await LoadAsync();
            _clearButton.Click += (_, _) => _results.Clear();

            this.ApplyTheme();
        }

        /// <summary>Loads the session's target data for the selected range.  Call once the control is hosted and has a handle.</summary>
        public void ViewData(DBADashContext context, string sessionName, int maxEvents)
        {
            _context = context;
            _sessionName = sessionName;
            if (maxEvents > 0) _maxRowsCombo.Text = maxEvents.ToString();
            _ = LoadAsync();
        }

        /// <summary>The selected range as a UTC lower bound (null = unbounded / whole target).  The view is always
        /// newest-anchored (up to "now"), so a range is a lower bound only.</summary>
        private DateTime? SelectedStartUtc()
        {
            var now = DateTime.UtcNow;
            return (_rangeCombo.SelectedItem as string) switch
            {
                RangeLastHour => now.AddHours(-1),
                RangeLastDay => now.AddDays(-1),
                RangeLast7Days => now.AddDays(-7),
                _ => (DateTime?)null // All
            };
        }

        private int SelectedMaxRows() =>
            int.TryParse(_maxRowsCombo.Text, out var n) && n > 0 ? n : XEViewTargetDataMessage.DefaultMaxEvents;

        private static string FormatElapsed(TimeSpan elapsed) =>
            elapsed.TotalSeconds >= 1 ? $"{elapsed.TotalSeconds:N1}s" : $"{elapsed.TotalMilliseconds:N0}ms";

        private async Task LoadAsync()
        {
            if (_loading || _context is not { InstanceID: > 0 } || string.IsNullOrEmpty(_sessionName)) return;
            _loading = true;
            _refreshButton.Enabled = false;
            var startUtc = SelectedStartUtc();
            var maxRows = SelectedMaxRows();
            SetStatus($"Loading captured data for {_sessionName}...", DashColors.Information);
            var totalSw = Stopwatch.StartNew();
            try
            {
                var roundTripSw = Stopwatch.StartNew();
                var outcome = await XESessionController.ViewTargetDataAsync(_context, _sessionName, maxRows,
                    startUtc, ControllerStatus);
                roundTripSw.Stop();

                if (!outcome.Ok)
                {
                    SetStatus(outcome.Message, DashColors.Fail);
                    if (!IsDisposed)
                    {
                        MessageBox.Show(this, outcome.Message, "View XE Data", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    return;
                }

                if (outcome.Events == null || outcome.Events.Rows.Count == 0)
                {
                    _results.Clear();
                    totalSw.Stop();
                    SetStatus($"No events in the {outcome.TargetType} target for the selected range " +
                              $"({FormatElapsed(totalSw.Elapsed)}).", DashColors.Information);
                    return;
                }

                var bindSw = Stopwatch.StartNew();
                _results.LoadEvents(outcome.Events);
                bindSw.Stop();
                totalSw.Stop();
                Serilog.Log.Information(
                    "View XE data {session}: {rows} rows - round-trip {roundTripMs}ms, grid bind {bindMs}ms",
                    _sessionName, outcome.TotalEvents, roundTripSw.ElapsedMilliseconds, bindSw.ElapsedMilliseconds);

                var more = outcome.Capped ? $" (showing the newest {outcome.MaxEvents:N0} - more are available)" : string.Empty;
                SetStatus($"{outcome.TotalEvents:N0} event(s) from the {outcome.TargetType} target{more} - " +
                          $"loaded in {FormatElapsed(totalSw.Elapsed)}.", DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
            }
            finally
            {
                _loading = false;
                if (!IsDisposed) _refreshButton.Enabled = true;
            }
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
            if (color == DashColors.Fail || color == DashColors.Warning) SetStatus(message, color);
        }
    }
}