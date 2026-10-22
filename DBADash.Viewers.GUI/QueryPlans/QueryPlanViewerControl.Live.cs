using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashGUI.Viewers;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// Following a query that is still running: a bar above the toolbar to refresh the plan, or have it refresh itself,
    /// and the refresh itself, which swaps in the newer capture without losing the reader's place.
    ///
    /// The plan is fetched whole once.  After that the source sends only the operators' counters, which it writes into
    /// the plan - see PlanLivePatcher - so each refresh is a few small rows through messaging rather than the whole plan.
    /// </summary>
    public sealed partial class QueryPlanViewerControl
    {
        /// <summary>Where a live plan's refreshes come from.  Null for any other plan.</summary>
        private readonly ILivePlanSource _live;

        /// <summary>The request this tab is following, when it is a live plan. Null for a static plan.</summary>
        public string LiveRequestKey => _live?.RequestKey;

        // The choices for refreshing by itself, in seconds - 0 is off.  Each refresh is a message through the repository
        // and the service, so nothing faster than every couple of seconds is offered.
        private static readonly int[] AutoRefreshIntervals = [0, 2, 5, 10, 30];
        private const int DefaultAutoRefresh = 2;

        // Refreshing by itself stops after this long, so a viewer left open overnight doesn't poll the instance until
        // morning.  Turning it back on starts the clock again.
        private static readonly TimeSpan AutoRefreshLimit = TimeSpan.FromMinutes(30);

        // Refreshing by itself stops after this many failures in a row - messaging down, the service stopped.
        private const int MaxLiveFailures = 3;

        private readonly System.Windows.Forms.Timer _liveTimer = new();
        private readonly CancellationTokenSource _liveCancel = new();
        private ToolStripButton _liveRefreshButton;
        private ToolStripDropDownButton _liveAutoMenu;
        private ToolStripLabel _liveStatus;

        // What the status says in full, and the detail behind it - the label shows as much of the text as fits.
        private string _liveStatusText = string.Empty;
        private string _liveStatusDetails;
        private int _autoRefreshSeconds;
        private DateTime _autoRefreshStartedUtc;
        private bool _liveBusy;
        private bool _liveEnded;
        private int _liveFailures;

        // Set while a refresh swaps in the newer capture, so the properties panel keeps its place - see ShowSelection.
        private bool _refreshingLive;

        // The XML editor is only refilled when its tab is looked at: a big plan's XML set on every refresh is work for
        // a tab that is usually not showing.
        private bool _xmlStale;

        private ToolStrip BuildLiveBar()
        {
            var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            bar.Items.Add(new ToolStripLabel("Live")
            {
                Font = new Font(bar.Font, FontStyle.Bold),
                ToolTipText = "A query that is still running.  Refresh to see how far it has got - operators moving rows are highlighted, " +
                              "with their rows moving along the arrows, and operators that haven't started are dimmed.  " +
                              "Waits stay as they were when the plan was first captured."
            });
            bar.Items.Add(new ToolStripLabel(_live.Description));
            bar.Items.Add(new ToolStripSeparator());

            _liveRefreshButton = new ToolStripButton("Refresh", null, async (_, _) => await RefreshLiveAsync())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Get the latest counters for the running query (F5)"
            };
            bar.Items.Add(_liveRefreshButton);

            _liveAutoMenu = new ToolStripDropDownButton { DisplayStyle = ToolStripItemDisplayStyle.Text };
            foreach (var seconds in AutoRefreshIntervals)
            {
                var item = new ToolStripMenuItem(seconds == 0 ? "Off" : $"Every {seconds} seconds") { Tag = seconds };
                item.Click += (_, _) => SetAutoRefresh(seconds);
                _liveAutoMenu.DropDownItems.Add(item);
            }
            bar.Items.Add(_liveAutoMenu);
            bar.Items.Add(new ToolStripSeparator());

            // Clipped with an ellipsis to the room left on the bar, rather than pushed into the overflow menu - a failure
            // is the longest thing it says and the one that most needs seeing.  The whole of it, and any detail, is the
            // tooltip.
            _liveStatus = new ToolStripLabel();
            bar.Items.Add(_liveStatus);
            _liveStatus.EnableAutoTruncate(text => string.IsNullOrEmpty(_liveStatusDetails)
                ? text
                : text + Environment.NewLine + Environment.NewLine + _liveStatusDetails);

            _liveTimer.Tick += async (_, _) => await LiveTimerTick();
            _tabs.SelectedIndexChanged += (_, _) => ShowXmlIfStale();

            Disposed += (_, _) =>
            {
                _liveTimer.Dispose();
                _liveCancel.Cancel();
                _liveCancel.Dispose();
            };

            // After the plan has been shown on Load - the first refresh gives the counters later ones are compared with,
            // so operators show as active from the second.
            Load += (_, _) => BeginInvoke(() => SetAutoRefresh(DefaultAutoRefresh));

            return bar;
        }

        private void SetAutoRefresh(int seconds)
        {
            if (_liveEnded) seconds = 0;

            _autoRefreshSeconds = seconds;
            _autoRefreshStartedUtc = DateTime.UtcNow;
            _liveFailures = 0;
            _liveTimer.Stop();

            _liveAutoMenu.Text = seconds == 0 ? "Auto Refresh: Off" : $"Auto Refresh: {seconds}s";
            _liveAutoMenu.ToolTipText = seconds == 0
                ? "Refresh the plan by itself while the query runs."
                : $"Refreshing every {seconds} seconds, for up to {AutoRefreshLimit.TotalMinutes:0} minutes.";
            foreach (var item in _liveAutoMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = (int)item.Tag == seconds;
            }

            if (seconds == 0) return;

            _liveTimer.Interval = seconds * 1000;
            _liveTimer.Start();
            _ = RefreshLiveAsync();
        }

        private async Task LiveTimerTick()
        {
            if (DateTime.UtcNow - _autoRefreshStartedUtc > AutoRefreshLimit)
            {
                SetAutoRefresh(0);
                SetLiveStatus(_liveStatusText + $"  -  Auto refresh stopped after {AutoRefreshLimit.TotalMinutes:0} minutes", _liveStatusDetails);
                return;
            }

            // A tab that isn't showing doesn't need to be kept up to date: it is refreshed when it is looked at again.
            if (!Visible) return;

            await RefreshLiveAsync();
        }

        private async Task RefreshLiveAsync()
        {
            // One at a time: a reply slower than the interval would otherwise have refreshes piling up behind it.
            if (_live == null || _liveBusy || _liveEnded || IsDisposed) return;

            _liveBusy = true;
            _liveRefreshButton.Enabled = false;
            try
            {
                var capture = await _live.CaptureAsync(_liveCancel.Token);
                if (IsDisposed) return;

                _liveFailures = 0;
                if (capture.Completed)
                {
                    EndLive(capture.Message ?? "The query has completed.  Showing the last capture.");
                    return;
                }

                ApplyLiveCapture(capture);
            }
            catch (OperationCanceledException)
            {
                // Closed while waiting for the reply.
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;

                SetLiveStatus("Refresh failed: " + ex.Message, ex.ToString());
                if (++_liveFailures >= MaxLiveFailures && _autoRefreshSeconds > 0)
                {
                    SetAutoRefresh(0);
                    SetLiveStatus(_liveStatusText + "  -  Auto refresh stopped", _liveStatusDetails);
                }
            }
            finally
            {
                _liveBusy = false;
                if (!IsDisposed) _liveRefreshButton.Enabled = !_liveEnded;
            }
        }

        /// <summary>The query has finished: the plan stays as last captured, and there is nothing more to refresh.</summary>
        private void EndLive(string message)
        {
            _liveEnded = true;
            SetAutoRefresh(0);
            _liveRefreshButton.Enabled = false;
            _liveAutoMenu.Enabled = false;
            SetLiveStatus(message);

            // Nothing is running any more, so nothing should look as though it is.
            _graphControl.SetLive(null);
        }

        private void ApplyLiveCapture(LivePlanCapture capture)
        {
            if (capture.PlanXml is { } xml && !string.Equals(xml, _sourceXml, StringComparison.Ordinal))
            {
                ExecutionPlan parsed;
                try
                {
                    parsed = PlanParser.Parse(xml);
                }
                catch (PlanParseException ex)
                {
                    SetLiveStatus("The refreshed plan could not be read: " + ex.Message, ex.ToString());
                    return;
                }

                if (parsed.Statements.Count > 0) ReplacePlan(parsed, xml, keepPlace: !capture.PlanChanged);
            }

            _graphControl.SetLive(capture.Live);

            var status = "Captured " + capture.CapturedAt.ToString("T", CultureInfo.CurrentCulture);
            if (capture.Elapsed is { } elapsed)
            {
                status += "  -  running for " + (elapsed.TotalDays >= 1 ? elapsed.ToString(@"d\.hh\:mm\:ss") : elapsed.ToString(@"hh\:mm\:ss"));
            }
            if (capture.PlanChanged) status += "  -  moved on to another statement";
            if (!string.IsNullOrEmpty(capture.Message)) status += "  -  " + capture.Message;

            SetLiveStatus(status);
        }

        /// <summary>
        /// Show a newer capture of the plan.  With <paramref name="keepPlace"/> it is the same plan with newer counters, so
        /// the reader stays where they were - the statement, zoom, selection and collapsed branches - and only the figures
        /// change.  Without it the query has moved on to a different statement, which is shown as though just opened.
        ///
        /// The AI tab keeps the capture it was shown with: showing it another one starts its conversation again, which a
        /// refresh every few seconds would make impossible to have.
        /// </summary>
        private void ReplacePlan(ExecutionPlan plan, string xml, bool keepPlace)
        {
            var index = _current is null ? -1 : _plan.Statements.ToList().IndexOf(_current);

            _plan = plan;
            _sourceXml = xml;
            _selectable.Clear();
            _selectable.AddRange(plan.Statements);
            _statements.Show(PlanStatementSummary.For(_plan));

            var sameShape = keepPlace && index >= 0 && index < plan.Statements.Count;
            if (!sameShape)
            {
                _current = null;
                // The new statement is shown as though just opened, except the AI tab, which keeps the capture the
                // reader opened it with. Picking a statement themselves still starts a new conversation.
                Show(plan.PrimaryStatement ?? plan.Statements[0], retargetAi: false);
                return;
            }

            var statement = plan.Statements[index];
            _current = statement;
            _statements.Select(statement);

            // Carrying the selection over refreshes the properties for it - see ShowSelection.  With nothing selected the
            // overview needs telling about the new figures itself.
            _refreshingLive = true;
            try
            {
                _graphControl.ReloadStatement(statement);
                if (_graphControl.SelectedNode is null) _properties.ShowKeepingPlace(null, statement);
            }
            finally
            {
                _refreshingLive = false;
            }

            PopulateMetricSelector();
            ShowCurrentLineWidth();
            ShowCollapsedState();
            UpdateTimeMenu(statement);
            ShowOperators(statement);
            ShowInsights(statement);
            ShowMissingIndexes(statement);
            ShowExpressions(statement);
            ShowParameters(statement);
            ShowWaits(statement);

            if (!string.IsNullOrWhiteSpace(statement.StatementText)) _queryText.Text = statement.StatementText;

            _xmlStale = true;
            ShowXmlIfStale();

            SetSummary(Summarise(statement, _graphControl.PlanLayout));

            // The matches again, on the new operators - without going to the first one as typing does, which would move
            // the view on every refresh.
            if (!string.IsNullOrWhiteSpace(_findBox.Text)) _graphControl.Find(_findBox.Text);
        }

        private void SetLiveStatus(string text, string details = null)
        {
            _liveStatusText = text ?? string.Empty;
            _liveStatusDetails = details;
            _liveStatus.SetTruncatedText(_liveStatusText);
        }

        private void ShowXmlIfStale()
        {
            if (!_xmlStale || !_xmlHost.Visible) return;

            _xmlText.Text = _sourceXml ?? string.Empty;
            _xmlStale = false;
        }
    }
}
