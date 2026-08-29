using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Multi-instance Extended Events view: lists the existing XE sessions across every instance in the selected node's
    /// context in a single filterable/sortable grid, with the instance name on each row.  Reuses the per-instance list
    /// message via <see cref="XESessionController.ListSessionsMultiAsync"/> (bounded fan-out), so nothing is persisted -
    /// it is an on-demand snapshot.  Running sessions only by default (the common question higher up the tree is "what's
    /// running where"); untick "Running only" to list every defined session.
    ///
    /// A refresh updates the grid <b>per instance as replies land</b> and keeps an instance's last-known rows if that
    /// refresh misses it (slow / offline / no reply within the window) rather than blanking it - so a partial round never
    /// makes instances silently vanish.  Progress and any non-responders are shown in the status bar.
    /// </summary>
    internal class MultiInstanceXEView : UserControl, ISetContext
    {
        // How many instances we message at once.  Each instance only ever receives a single lightweight session-list
        // query, so the per-instance load is negligible regardless of this - the cap is really about not flooding the
        // service / the repo connection pool with in-flight requests.  Offline instances are short-circuited (never
        // messaged), so this budget is spent on responsive instances.
        private const int MaxConcurrency = 25;

        private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripButton _tsRefresh = new("Refresh") { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, Image = Properties.Resources.ProjectSystemModelRefresh_16x, ToolTipText = "Queries each monitored instance for XE sessions" };

        private readonly ToolStripDropDownButton _tsFilter = new("Filter")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ToolTipText = "Filter which sessions are shown.",
            Image = Properties.Resources.FilterDropdown_16x,
        };

        private readonly ToolStripMenuItem _tsRunningOnly = new("Running Only")
        {
            CheckOnClick = true,
            Checked = true,
            ToolTipText = "Show only sessions that are currently running.  Untick to list every defined session."
        };

        private readonly ToolStripMenuItem _tsExcludeSystemHealth = new("Exclude System Health")
        {
            CheckOnClick = true,
            Checked = true,
            ToolTipText = "Exclude the system_health session."
        };

        private readonly ToolStripMenuItem _tsExcludeTelemetry = new("Exclude Telemetry")
        {
            CheckOnClick = true,
            Checked = true,
            ToolTipText = "Exclude telemetry sessions (telemetry_xevents)."
        };

        private readonly ToolStripMenuItem _tsExcludeAlwaysOnHealth = new("Exclude Always On Health")
        {
            CheckOnClick = true,
            Checked = true,
            ToolTipText = "Exclude the AlwaysOn_health session."
        };

        private readonly ToolStripMenuItem _tsExcludeDBADash = new("Exclude DBA Dash")
        {
            CheckOnClick = true,
            Checked = false,
            ToolTipText = "Exclude DBA Dash sessions (DBADash_1, DBADash_2, DBADash_AdHoc)."
        };

        private readonly ToolStripButton _tsClearFilter = new("Clear Filter")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Image = Properties.Resources.Eraser_16x,
            Enabled = false,
            ToolTipText = "No Filter Applied"
        };

        private readonly ToolStripButton _tsCopy = new("Copy") { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.ASX_Copy_blue_16x };
        private readonly ToolStripButton _tsExcel = new("Excel") { DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Properties.Resources.excel16x16 };

        private readonly DBADashDataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AllowUserToOrderColumns = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        private readonly StatusStrip _statusStrip = new();
        private readonly ToolStripStatusLabel _status = new() { TextAlign = System.Drawing.ContentAlignment.MiddleLeft };

        // _all is the master (every session across every in-scope instance); _sessions is the grid-bound projection of it
        // (all rows, or only running rows when "Running only" is ticked).  Keeping them separate lets the grid solely own
        // the DataView's RowFilter / Sort (the user's column filters + sort order), which then survive a refresh - the
        // "Running only" toggle is applied by row inclusion in the projection, never by touching RowFilter.
        private readonly DataTable _all = CreateEmptyTable();

        private readonly DataTable _sessions = CreateEmptyTable();
        private DBADashContext _context;
        private CancellationTokenSource _cts;

        // Live counters for the current / last refresh, so the status can re-render when the "Running only" toggle flips.
        private int _total, _collected, _excluded, _offline, _failed;

        private bool _refreshRunning;
        private DateTime? _lastRefreshTime;
        private readonly List<string> _problems = new();

        public MultiInstanceXEView()
        {
            _tsFilter.DropDownItems.AddRange(new ToolStripItem[]
            {
                _tsRunningOnly, _tsExcludeSystemHealth, _tsExcludeTelemetry, _tsExcludeAlwaysOnHealth, _tsExcludeDBADash
            });
            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _tsRefresh, _tsCopy, _tsExcel, _tsFilter, _tsClearFilter, new ToolStripSeparator(),
            });
            _statusStrip.Items.Add(_status);

            // Add fill first (lowest z-order) then bottom/top so docking lays out correctly.
            Controls.Add(_grid);
            Controls.Add(_statusStrip);
            Controls.Add(_toolStrip);

            _tsRefresh.Click += (_, _) => RefreshData();
            _tsRunningOnly.CheckedChanged += (_, _) => { RebuildGrid(); RenderStatus(); };
            _tsExcludeSystemHealth.CheckedChanged += (_, _) => { RebuildGrid(); RenderStatus(); };
            _tsExcludeTelemetry.CheckedChanged += (_, _) => { RebuildGrid(); RenderStatus(); };
            _tsExcludeAlwaysOnHealth.CheckedChanged += (_, _) => { RebuildGrid(); RenderStatus(); };
            _tsExcludeDBADash.CheckedChanged += (_, _) => { RebuildGrid(); RenderStatus(); };
            _tsCopy.Click += (_, _) => _grid.CopyGrid();
            _tsExcel.Click += (_, _) => _grid.ExportToExcel();
            _grid.DataBindingComplete += Grid_DataBindingComplete;
            _grid.CellContentClick += Grid_CellContentClick;
            _grid.RegisterClearFilter(_tsClearFilter);

            BuildColumns();
            _grid.DataSource = _sessions.DefaultView;
            _grid.ApplyTheme();
        }

        /// <summary>The fixed schema of the aggregated grid (matches the per-instance list message + the stamped columns).</summary>
        private static DataTable CreateEmptyTable()
        {
            var dt = new DataTable("Sessions");
            dt.Columns.Add("Instance", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("IsRunning", typeof(bool));
            dt.Columns.Add("StartTime", typeof(DateTime));
            dt.Columns.Add("EventCount", typeof(int));
            dt.Columns.Add("TargetTypes", typeof(string));
            dt.Columns.Add("InstanceID", typeof(int));
            dt.Columns.Add("CanManage", typeof(bool));
            dt.Columns.Add("CanWatch", typeof(bool));
            dt.Columns.Add("ActionStartStop", typeof(string));
            dt.Columns.Add("ActionWatch", typeof(string));
            dt.Columns.Add("ActionViewData", typeof(string));
            return dt;
        }

        /// <summary>
        /// Defines the grid's columns.  Bound data columns map by <c>DataPropertyName</c>; the Instance name and the
        /// action columns (start/stop, watch, view data, DDL) are <see cref="DataGridViewLinkColumn"/>s bound to
        /// precomputed text columns (empty text = a blank, non-clickable cell).  The <c>InstanceID</c> / <c>CanManage</c>
        /// / <c>CanWatch</c> data columns are carried in the bound table but not shown - they're read via the cell's
        /// <see cref="DataRowView"/> when an action link is clicked.
        /// </summary>
        private void BuildColumns()
        {
            _grid.Columns.Clear();
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "Instance",
                HeaderText = "Instance",
                DataPropertyName = "Instance",
                Frozen = true,
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Go to this instance's Extended Events node."
            });
            _grid.Columns.Add(new DataGridViewLinkColumn { Name = "Session", HeaderText = "Session", DataPropertyName = "Name", ToolTipText = "Click to view the CREATE EVENT SESSION script." });
            _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Running", HeaderText = "Running", DataPropertyName = "IsRunning", SortMode = DataGridViewColumnSortMode.Automatic });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Start Time", DataPropertyName = "StartTime" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Events", HeaderText = "Events", DataPropertyName = "EventCount" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Targets", HeaderText = "Targets", DataPropertyName = "TargetTypes" });
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "StartStop",
                HeaderText = "",
                DataPropertyName = "ActionStartStop",
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "Watch",
                HeaderText = "",
                DataPropertyName = "ActionWatch",
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
            _grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = "ViewData",
                HeaderText = "",
                DataPropertyName = "ActionViewData",
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        public void SetContext(DBADashContext context)
        {
            if (context == _context) return;
            _context = context;
            RefreshData();
        }

        private async void RefreshData()
        {
            if (_context == null) return;

            // Cancel any in-flight fan-out (e.g. a rapid context switch or a second Refresh click).
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();

            var ids = _context.InstanceIDs?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            // Drop rows for instances no longer in scope (e.g. a different group was selected), but keep in-scope
            // instances' existing rows - each instance's rows are replaced when its own reply lands, so a slow or missed
            // instance keeps its last-known sessions rather than disappearing.
            PruneToScope(new HashSet<int>(ids));
            RebuildGrid();

            _total = ids.Count;
            _collected = 0;
            _excluded = 0;
            _offline = 0;
            _failed = 0;
            _problems.Clear();

            if (ids.Count == 0)
            {
                _refreshRunning = false;
                _status.Text = "No instances in scope.";
                _status.ToolTipText = null;
                return;
            }

            _refreshRunning = true;
            _tsRefresh.Enabled = false;
            RenderStatus();

            string error = null;
            try
            {
                await XESessionController.ListSessionsMultiAsync(ids, MaxConcurrency, (result, rows) =>
                {
                    if (cts.IsCancellationRequested) return Task.CompletedTask;
                    if (result.Ok && rows != null)
                    {
                        ReplaceInstanceRows(result.InstanceID, rows);
                        _collected++;
                        RebuildGrid();
                    }
                    else if (result.Offline)
                    {
                        _offline++;
                        _problems.Add($"{result.Label}: {result.Message}");
                    }
                    else if (result.Skipped)
                    {
                        _excluded++;
                    }
                    else
                    {
                        _failed++;
                        _problems.Add($"{result.Label}: {result.Message}");
                    }
                    RenderStatus();
                    return Task.CompletedTask;
                }, cts.Token);

                if (cts.IsCancellationRequested) return;
                _lastRefreshTime = DateTime.Now;
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer refresh - the newer run owns the grid + status.
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested) error = ex.Message;
            }
            finally
            {
                // Only the owning (non-superseded) run finalises the UI, and it always renders the completed summary here
                // so the progress info persists after the run ends (rather than depending on the success path alone).
                if (_cts == cts)
                {
                    _refreshRunning = false;
                    _tsRefresh.Enabled = true;
                    if (error != null)
                    {
                        _status.Text = error;
                        _status.ToolTipText = null;
                    }
                    else
                    {
                        RenderStatus();
                    }
                    // This run still owns _cts - clear it so the next refresh's _cts?.Cancel() doesn't hit the CTS we're
                    // about to dispose (which would throw ObjectDisposedException).
                    _cts = null;
                }
                // Each run disposes its own CTS: the await has returned by here so the token is no longer observed.
                // Everything is UI-thread affine, so a superseded run reaching this point can't race the owning run.
                cts.Dispose();
            }
        }

        /// <summary>
        /// Projects the master table (<see cref="_all"/>) into the grid-bound table, including only running sessions when
        /// "Running only" is ticked.  Deliberately never touches the bound view's RowFilter / Sort, so the user's column
        /// filters and sort order (owned by the grid) survive every refresh and toggle.
        /// </summary>
        private void RebuildGrid()
        {
            var runningOnly = _tsRunningOnly.Checked;
            _sessions.BeginLoadData();
            try
            {
                _sessions.Rows.Clear();
                foreach (DataRow r in _all.Rows)
                {
                    if (runningOnly && !(r["IsRunning"] != DBNull.Value && Convert.ToBoolean(r["IsRunning"]))) continue;
                    if (IsExcludedByName(r["Name"] as string)) continue;
                    _sessions.ImportRow(r);
                }
            }
            finally
            {
                _sessions.EndLoadData();
            }
        }

        /// <summary>
        /// Determines whether a session should be excluded from the grid based on the "Exclude ..." filter toggles.
        /// The name comparison is case-insensitive.
        /// </summary>
        private bool IsExcludedByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (_tsExcludeSystemHealth.Checked &&
                string.Equals(name, "system_health", StringComparison.OrdinalIgnoreCase)) return true;

            if (_tsExcludeTelemetry.Checked &&
                string.Equals(name, "telemetry_xevents", StringComparison.OrdinalIgnoreCase)) return true;

            if (_tsExcludeAlwaysOnHealth.Checked &&
                string.Equals(name, "AlwaysOn_health", StringComparison.OrdinalIgnoreCase)) return true;

            if (_tsExcludeDBADash.Checked &&
                (string.Equals(name, "DBADash_1", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(name, "DBADash_2", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(name, "DBADash_AdHoc", StringComparison.OrdinalIgnoreCase))) return true;

            return false;
        }

        /// <summary>Removes master rows for instances that are no longer in the current context.</summary>
        private void PruneToScope(HashSet<int> inScope)
        {
            for (var i = _all.Rows.Count - 1; i >= 0; i--)
            {
                var row = _all.Rows[i];
                if (row["InstanceID"] == DBNull.Value || !inScope.Contains(Convert.ToInt32(row["InstanceID"])))
                {
                    row.Delete();
                }
            }
            _all.AcceptChanges();
        }

        /// <summary>Swaps in a fresh set of master rows for one instance (removes its old rows, adds the new).</summary>
        private void ReplaceInstanceRows(int instanceID, DataTable rows)
        {
            for (var i = _all.Rows.Count - 1; i >= 0; i--)
            {
                var row = _all.Rows[i];
                if (row["InstanceID"] != DBNull.Value && Convert.ToInt32(row["InstanceID"]) == instanceID)
                {
                    row.Delete();
                }
            }
            _all.AcceptChanges();
            _all.Merge(rows, false, MissingSchemaAction.Ignore);
        }

        private void RenderStatus()
        {
            var shown = _sessions.DefaultView.Count; // after the running-only projection + the user's column filters
            var totalAll = _all.Rows.Count;
            var sessionText = shown == totalAll
                ? $"{shown} session{Plural(shown)}"
                : $"{shown} of {totalAll} session{Plural(totalAll)}";

            var verb = _refreshRunning ? "Collecting" : "Collected";
            var sb = new StringBuilder($"{verb} {_collected} of {_total} instance{Plural(_total)}");
            var notes = new List<string>();
            if (_excluded > 0) notes.Add($"{_excluded} excluded");
            if (_offline > 0) notes.Add($"{_offline} offline");
            if (notes.Count > 0) sb.Append($" ({string.Join(", ", notes)})");
            if (_failed > 0) sb.Append($" · {_failed} not responding");
            sb.Append($" — {sessionText}");
            if (!_refreshRunning && _lastRefreshTime.HasValue) sb.Append($" · updated {_lastRefreshTime:HH:mm:ss}");
            _status.Text = sb.ToString();

            // Detail of the instances that didn't return anything, so the user can see what's missing from the grid.
            _status.ToolTipText = _problems.Count == 0 ? null : string.Join(Environment.NewLine, _problems);
        }

        private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            _grid.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        /// <summary>The target must carry an event stream to view/watch - only event_file and ring_buffer qualify.</summary>
        private static bool HasReadableTarget(string targetTypes) =>
            !string.IsNullOrEmpty(targetTypes) &&
            (targetTypes.IndexOf("event_file", StringComparison.OrdinalIgnoreCase) >= 0 ||
             targetTypes.IndexOf("ring_buffer", StringComparison.OrdinalIgnoreCase) >= 0);

        private async void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;

            var row = drv.Row;
            var instanceId = row["InstanceID"] != DBNull.Value ? Convert.ToInt32(row["InstanceID"]) : 0;
            var session = row["Name"] as string;
            var running = row["IsRunning"] != DBNull.Value && Convert.ToBoolean(row["IsRunning"]);
            var canManage = row["CanManage"] != DBNull.Value && Convert.ToBoolean(row["CanManage"]);
            var canWatch = row["CanWatch"] != DBNull.Value && Convert.ToBoolean(row["CanWatch"]);
            var readable = HasReadableTarget(row["TargetTypes"] as string);
            if (instanceId <= 0) return;

            switch (_grid.Columns[e.ColumnIndex].Name)
            {
                case "Instance":
                    NavigateToInstance(instanceId);
                    break;

                case "StartStop" when canManage && !string.IsNullOrEmpty(session):
                    await StartStopAsync(instanceId, session, running);
                    break;

                case "Watch" when running && canWatch && !string.IsNullOrEmpty(session):
                    XETraceLauncher.LaunchWatch(this, BuildContext(instanceId), session);
                    break;

                case "ViewData" when running && canWatch && readable && !string.IsNullOrEmpty(session):
                    XETraceLauncher.LaunchViewData(this, BuildContext(instanceId), session);
                    break;

                case "Session" when !string.IsNullOrEmpty(session):
                    await ScriptDdlAsync(instanceId, session);
                    break;
            }
        }

        /// <summary>Scripts the session's CREATE EVENT SESSION DDL and opens it in the shared code viewer.</summary>
        private async Task ScriptDdlAsync(int instanceId, string session)
        {
            _status.Text = $"Scripting {session}...";
            try
            {
                var ddl = await XESessionController.ScriptSessionAsync(BuildContext(instanceId), session, (_, _, _) => { });
                if (string.IsNullOrEmpty(ddl))
                {
                    _status.Text = $"Couldn't script {session}.";
                    return;
                }
                Common.ShowCodeViewer(ddl, $"Extended Events - {session}");
                _status.Text = $"Scripted {session}.";
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Selects the instance's Extended Events node in the tree (drills from the current context's instance).</summary>
        private void NavigateToInstance(int instanceId)
        {
            Main.MainFormInstance?.Instance_Selected(this, new Main.InstanceSelectedEventArgs
            {
                InstanceID = instanceId,
                Tab = Main.Tabs.ExtendedEvents,
                SearchFromRoot = true
            });
        }

        /// <summary>A minimal, fully messaging-capable context for one instance (agents/connection resolve from CommonData).</summary>
        private static DBADashContext BuildContext(int instanceId) =>
            new() { InstanceID = instanceId, InstanceName = XEInstanceLabels.Resolve(instanceId, instanceId.ToString()) };

        /// <summary>
        /// Starts / stops the session on the given instance, then refreshes so the grid reflects the real state.  Uses the
        /// same outcome checks as the per-instance viewer so a rejected or ineffective operation is surfaced, never a
        /// phantom success.
        /// </summary>
        private async Task StartStopAsync(int instanceId, string session, bool running)
        {
            var label = XEInstanceLabels.Resolve(instanceId, instanceId.ToString());

            // Confirm both directions: stopping interrupts an active capture, and starting begins a capture on the
            // instance - so ask before either.
            var action = running ? "Stop" : "Start";
            if (MessageBox.Show(this,
                    $"{action} the Extended Events session '{session}' on {label}?",
                    $"{action} Session", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var op = running ? XESessionOperation.Stop : XESessionOperation.Start;
            var verb = running ? "stopped" : "started";
            _status.Text = $"{op} {session} on {label}...";
            try
            {
                var outcome = await XESessionController.ControlSessionAsync(BuildContext(instanceId), session, op,
                    (_, _, _) => { });
                if (!outcome.Ok)
                {
                    var msg = string.IsNullOrWhiteSpace(outcome.Message) ? $"{session} could not be {verb}." : outcome.Message;
                    _status.Text = msg;
                    MessageBox.Show(this, msg, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (outcome.Running.HasValue && outcome.Running.Value == running)
                {
                    var msg = $"{session} was not {verb} - it is still {(outcome.Running.Value ? "running" : "stopped")}.";
                    _status.Text = msg;
                    MessageBox.Show(this, msg, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _status.Text = $"{session} {verb} on {label}.";
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "Extended Events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // Refresh just this instance's rows to reflect the real state - re-fanning out to every instance would
                // be excessive for a single start/stop.
                await RefreshInstanceAsync(instanceId);
            }
        }

        /// <summary>Re-lists a single instance and swaps in its rows, without disturbing the aggregate status / counters.</summary>
        private async Task RefreshInstanceAsync(int instanceId)
        {
            try
            {
                await XESessionController.ListSessionsMultiAsync(new[] { instanceId }, 1, (result, rows) =>
                {
                    if (result.Ok && rows != null)
                    {
                        ReplaceInstanceRows(result.InstanceID, rows);
                        RebuildGrid();
                    }
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                // Best-effort single-instance refresh - the session was already started/stopped; the grid just keeps its
                // last-known rows for this instance if the re-list fails.
                Serilog.Log.Debug(ex, "Multi-instance XE: single-instance refresh failed for instance {instanceID}.", instanceId);
            }
        }

        private static string Plural(int n) => n == 1 ? string.Empty : "s";
    }
}
