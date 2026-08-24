using DBADash.Messaging;
using DBADash.XE;
using DBADashGUI.CustomReports;
using DBADashGUI.Messaging;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Ad-hoc XE trace UI.  Configures and runs a trace and shows events live.  Persistence + messaging are handled
    /// by <see cref="XETraceController"/> / <see cref="XETraceRepo"/>; this control is the UI only.
    /// </summary>
    public partial class QuickXETrace : UserControl, ISetContext
    {
        private const string AllEventsLabel = "(All events)";

        // Above this many instances in a single run, warn before starting - the trace overhead applies to each instance
        // independently, so tracing many at once multiplies the impact.
        private const int ManyInstancesWarningThreshold = 5;

        private DBADashContext _context;
        private XEObjectCatalog _catalog = new();

        private readonly List<XETraceEventDef> _extraEvents = new();
        private readonly List<XEFilter> _filters = new();
        private List<XEEventInfo> _allEvents = new();

        // Per-event customizable-column toggles (the collect_* switches), keyed by event name then column name.  Only
        // entries the user has changed are stored; anything absent falls back to the catalog default.  This is the only
        // per-event field selection XE actually supports - data columns are always captured and can't be deselected.
        private readonly Dictionary<string, Dictionary<string, bool>> _eventCustomizations = new(StringComparer.OrdinalIgnoreCase);

        // Global actions ("global fields") captured on every event.  Wired to lnkGlobalFields.
        private readonly List<XEActionDef> _globalActions = new(XETraceDefinition.DefaultGlobalActions);

        private readonly XEResultsControl _results = new() { Dock = DockStyle.Fill };
        private bool _cancelling;
        private bool _isRunning;

        // One entry per instance currently being traced (the current context instance plus any AG replicas / manually
        // added instances).  A single-instance run holds exactly one.  Each trace has its own conversation group and
        // repo session; a multi-instance run shares one RunGroupID so its sessions reload together in history.
        private sealed class RunningTrace
        {
            public DBADashContext Context;
            public Guid MessageGroup;
            public long? SessionID;
        }

        private readonly List<RunningTrace> _runningTraces = new();

        // How many of the run's instances the service has confirmed are actually running (each sends a confirmation
        // once its session is created + started).  Drives the "request sent" -> "running" status transition.
        private int _confirmedRunningCount;

        // An instance shown in the instances list.  The current context instance is always present as a mandatory,
        // checked item (IsCurrent - can't be unchecked or removed); the rest are AG replicas or manual additions.  The
        // "Instances to Trace" selection controls (grpInstances, chkIncludeAg, clbInstances, ...) live in the designer.
        private sealed class TraceInstance
        {
            public int InstanceID;
            public string Name;
            public bool IsAg;      // discovered via "Include AG replicas" (removed when that box is unticked)
            public bool IsCurrent; // the current context instance - mandatory, always traced

            public override string ToString() => IsCurrent ? $"{Name} (Current Instance - required)" : Name;
        }

        // Guards the AG checkbox handler while we set it programmatically (context switch / template load).
        private bool _loadingInstances;

        // Bumped on every trace start and whenever a running trace is stopped to switch instances.  Batch and summary
        // callbacks capture the generation they were started with and drop themselves if it no longer matches, so a
        // cancelled trace's in-flight events can't leak into a freshly cleared grid.
        private int _traceGeneration;

        // The history snapshot currently shown in the grid, or null for a live/empty grid.  We track this per instance
        // so switching back to an instance re-loads whatever snapshot it was showing (the events live in the DB, so
        // only the identifiers need preserving).  RunGroup is set for a multi-instance run so switching back reloads
        // the whole merged grid (all replicas), not just this instance's slice.
        private readonly record struct HistorySnapshot(long SessionId, Guid? RunGroup);

        private HistorySnapshot? _loadedSnapshot;
        private readonly Dictionary<int, HistorySnapshot> _historyByInstance = new();
        private byte[] _xelData;

        // While applying a saved template we set the checkboxes/filters directly, so suppress the CheckedChanged
        // handlers that would otherwise add/remove the default filters and rebuild grids mid-apply.
        private bool _loadingTemplate;

        private string _lastTemplateName;

        private readonly System.Windows.Forms.Timer _runTimer = new() { Interval = 1000 };
        private DateTime _traceStartTime;
        private int _traceDurationSeconds;

        // Keeps a trace alive: while one runs we send a heartbeat every XETraceHeartbeat.IntervalSeconds so the service
        // knows the client is still here.  If it stops (app crash / kill / network loss) the service stops the trace
        // itself.  A background timer (not the UI-thread _runTimer) so a busy UI thread can't stall the beats and
        // cause a false "client gone" stop.
        private System.Threading.Timer _heartbeatTimer;

        private int _heartbeatInFlight; // 0/1 guard (Interlocked) so a slow beat can't stack with the next tick

        private const int DefaultErrorSeverityThreshold = 11; // default for error_reported filter
        private const string RpcResetProc = "sp_reset_connection"; // connection-pool reset RPC, excluded by default

        public QuickXETrace()
        {
            InitializeComponent();
            InitControls();
            WireEvents();
            _runTimer.Tick += RunTimer_Tick;
            this.ApplyTheme();
            SetRunningState(false);

            // RPC Completed is on by default, so seed its default sp_reset_connection exclusion up front.
            if (chkRPC.Checked) AddDefaultRpcResetFilter();
            RefreshFilterGrid();
        }

        private void InitControls()
        {
            // Keep the config panel a fixed, snug width (it holds fixed-size groups) so the results grid gets the
            // rest of the space, and let the config scroll if the panel is shorter than its content.
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            panel1.AutoScroll = true;
            try { splitContainer1.SplitterDistance = 750; } catch { /* container not sized yet */ }

            cboTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTarget.DataSource = Enum.GetValues(typeof(XETraceTargetPreference));
            // Target drives the mode: Auto/LiveStream = target-less live streaming; EventFile/RingBuffer = durable.

            cboComparison.DropDownStyle = ComboBoxStyle.DropDownList;
            cboComparison.DataSource = Enum.GetValues(typeof(XEFilterOp));

            cboEvent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboField.DropDownStyle = ComboBoxStyle.DropDownList;
            cboOtherEvent.DropDownStyle = ComboBoxStyle.DropDownList;

            dgvFilters.AutoGenerateColumns = false;
            dgvFilters.Columns.Clear();
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Event", DataPropertyName = "Event", Width = 160 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field", DataPropertyName = "Field", Width = 150 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Comparison", DataPropertyName = "Comparison", Width = 120 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = "Value", Width = 160 });
            dgvFilters.Columns.Add(new DataGridViewLinkColumn
            { Name = ColFilterDelete, HeaderText = "", Text = "Delete", UseColumnTextForLinkValue = true, Width = 90 });

            // The events grid: event name + a "Fields" link (pick the event's data columns) + a "Delete" link.
            dgvEvents.AutoGenerateColumns = false;
            dgvEvents.ReadOnly = true;
            dgvEvents.MultiSelect = false;
            dgvEvents.Columns.Clear();
            dgvEvents.Columns.Add(new DataGridViewTextBoxColumn
            { Name = ColEventName, HeaderText = "Event", DataPropertyName = "Event", Width = 240, ReadOnly = true });
            dgvEvents.Columns.Add(new DataGridViewLinkColumn
            { Name = ColFields, HeaderText = "Fields", DataPropertyName = "Fields", Width = 150 });
            dgvEvents.Columns.Add(new DataGridViewLinkColumn
            { Name = ColDelete, HeaderText = "", Text = "Delete", UseColumnTextForLinkValue = true, Width = 90 });

            // Results view (shared with the Extended Events watch window) hosts the results + pivoted detail grids.
            splitContainer1.Panel2.Controls.Add(_results);

            UpdateGlobalFieldsLabel();
            UpdateXelCaptureState();
        }

        /// <summary>
        /// Enables "Capture .xel" for every target that can write one: the event_file target, and live streaming
        /// (Auto/LiveStream), where the service bolts an event_file target onto the live session.  The ring buffer
        /// (memory) can't produce a .xel, and none of these are available on Azure SQL Database (no event_file, no live
        /// streaming - Auto resolves to the ring buffer there).  Otherwise the option is disabled (and cleared) so the
        /// user isn't offered a capture that would silently produce nothing.
        /// </summary>
        private void UpdateXelCaptureState()
        {
            var supported = XelCaptureSupported();
            checkBox4.Enabled = supported;
            if (!supported) checkBox4.Checked = false;
            checkBox4.Text = supported ? "Capture .xel" : "Capture .xel (not on ring buffer / Azure SQL DB)";
        }

        private bool XelCaptureSupported()
        {
            // A .xel comes from the event_file target.  Live streaming (Auto/LiveStream) can also capture one because
            // the service gives the live session an event_file target alongside the stream.  The ring buffer (memory)
            // writes no file, and neither event_file nor live streaming is available on Azure SQL Database.
            if (_context?.EngineEdition == DatabaseEngineEdition.SqlDatabase) return false;
            var target = cboTarget.SelectedItem is XETraceTargetPreference p ? p : XETraceTargetPreference.Auto;
            return target is XETraceTargetPreference.EventFile
                or XETraceTargetPreference.Auto
                or XETraceTargetPreference.LiveStream;
        }

        private const string ColEventName = "colEventName";
        private const string ColFields = "colFields";
        private const string ColDelete = "colDelete";
        private const string ColFilterDelete = "colFilterDelete";

        private void WireEvents()
        {
            tsConfigure.Click += (_, _) => splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
            tsStartTrace.Click += async (_, _) => await StartAsync();
            tsStopTrace.Click += async (_, _) => await StopAsync();
            toolStripButton1.Click += (_, _) => ClearGrid();
            savexelToolStripMenuItem.Click += (_, _) => SaveXel();
            tsHistory.DropDownOpening += async (_, _) => await LoadHistoryMenuAsync();
            // Only offer "Save *.xel" when a .xel was actually captured (needs 'Capture .xel' + an event_file run).
            tsSave.DropDownOpening += (_, _) =>
            {
                var haveXel = _xelData is { Length: > 0 };
                savexelToolStripMenuItem.Enabled = haveXel;
                savexelToolStripMenuItem.ToolTipText = haveXel
                    ? string.Empty
                    : "No .xel captured. Enable 'Capture .xel' before running the trace.";
            };
            saveTemplateToolStripMenuItem.Click += (_, _) => SaveTemplate();
            tsTemplates.DropDownOpening += (_, _) => LoadTemplatesMenu();

            bttnAddEvent.Click += (_, _) => AddExtraEvent();
            dgvEvents.CellContentClick += DgvEvents_CellContentClick;
            lnkGlobalFields.LinkClicked += (_, _) => PickGlobalFields();
            txtEventFilter.TextChanged += (_, _) => FilterEventList();
            chkRPC.CheckedChanged += (_, _) => OnRpcCompletedChanged();
            chkBatchCompleted.CheckedChanged += (_, _) => { if (!_loadingTemplate) RefreshFilterEvents(); };
            chkErrorReported.CheckedChanged += (_, _) => { if (!_loadingTemplate) RefreshFilterEvents(); };

            cboTarget.SelectedIndexChanged += (_, _) => UpdateXelCaptureState();
            cboEvent.SelectedIndexChanged += (_, _) => RefreshFilterFields();
            bttnAddFilter.Click += (_, _) => AddFilter();
            dgvFilters.CellContentClick += DgvFilters_CellContentClick;

            // Instances-to-trace selection (grpInstances, designer).
            chkIncludeAg.CheckedChanged += (_, _) => { if (!_loadingInstances) _ = OnIncludeAgChangedAsync(); };
            btnAddInstance.Click += (_, _) => AddInstancesViaPicker();
            clbInstances.ItemCheck += ClbInstances_ItemCheck;
        }

        public void SetContext(DBADashContext context)
        {
            // As a context-following tab this is re-invoked whenever the tree selection changes.  A stopped/history
            // grid holds nothing that needs protecting (the events live in the DB), so only a *running* trace prompts.
            // We remember which history snapshot each instance was showing so switching back can re-load it.
            var switchingInstance = _context is { InstanceID: > 0 } && context?.InstanceID != _context.InstanceID;
            if (switchingInstance)
            {
                if (_isRunning)
                {
                    var extra = _runningTraces.Count > 1 ? $" (and {_runningTraces.Count - 1} other instance(s))" : string.Empty;
                    var answer = MessageBox.Show(this,
                        $"A trace is running for {_context.InstanceName}{extra}.\r\n\r\nStop it and switch to {context?.InstanceName}?",
                        "Ad-hoc Trace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes)
                    {
                        // Keep the running trace and bounce the tree selection back to its instance (deferred so the
                        // in-flight navigation finishes first).  We stay on _context and ignore this call.
                        var pinnedInstanceId = _context.InstanceID;
                        if (IsHandleCreated)
                        {
                            BeginInvoke(new Action(() => Main.MainFormInstance?.Instance_Selected(this,
                                new Main.InstanceSelectedEventArgs
                                { InstanceID = pinnedInstanceId, Tab = Main.Tabs.AdhocTrace, SearchFromRoot = true })));
                        }
                        return;
                    }
                    // Stop every outgoing trace using ITS own context/group before we re-point _context (avoid
                    // cross-wiring), and bump the generation so their in-flight batches can't leak into the next
                    // instance's grid.
                    _cancelling = true;
                    _traceGeneration++;
                    SetRunningState(false);
                    foreach (var rt in _runningTraces.ToList()) _ = StopTraceAsync(rt.Context, rt.MessageGroup);
                    _runningTraces.Clear();
                }

                if (_loadedSnapshot is { } outgoing)
                {
                    _historyByInstance[_context.InstanceID] = outgoing; // remember the outgoing instance's snapshot
                }
                else
                {
                    _historyByInstance.Remove(_context.InstanceID); // nothing loaded - don't re-show a stale snapshot
                }
            }

            _context = context;
            // Reset the instance list on a switch (the AG/added instances belonged to the previous instance); otherwise
            // just make sure the current instance is seeded (first load, or re-selecting the same instance).
            if (switchingInstance) ResetInstances();
            else EnsureCurrentInstanceSeeded();
            UpdateXelCaptureState(); // engine edition is now known (in-memory), so Auto+Azure DB can disable xel capture
            _ = LoadCatalogAsync();

            if (switchingInstance && context is { InstanceID: > 0 })
            {
                // Show the incoming instance's grid: re-load its remembered history snapshot from the DB, else clear.
                // A multi-instance run reloads its whole merged grid (RunGroup carried in the snapshot).
                ClearGrid();
                if (_historyByInstance.TryGetValue(context.InstanceID, out var snapshot))
                {
                    _ = LoadHistoryEventsAsync(snapshot.SessionId, snapshot.RunGroup);
                }
            }
        }

        /// <summary>Best-effort stop + cleanup of a trace we're abandoning, targeting its own captured context/group.</summary>
        private static async Task StopTraceAsync(DBADashContext context, Guid messageGroup)
        {
            try
            {
                await XETraceController.CancelAsync(context, messageGroup, (_, _, _) => { });
                await XETraceController.CleanupAsync(context, (_, _, _) => { });
            }
            catch
            {
                // Abandoned trace - nothing useful to surface; the server-side session will also time out on its own.
            }
        }

        // ---- Catalog -----------------------------------------------------------------------------

        private async Task LoadCatalogAsync()
        {
            if (_context is not { InstanceID: > 0 }) return;
            SetStatus("Loading extended events catalog...", string.Empty, DashColors.Information);
            try
            {
                _catalog = await XETraceController.GetCatalogAsync(_context, ControllerStatus);
                // An event name can exist in several packages (e.g. error_reported in sqlserver and xesvlpkg); show
                // one entry per name, preferring the sqlserver package (the one the trace built-ins/pickers mean).
                _allEvents = _catalog.Events
                    .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.FirstOrDefault(e => string.Equals(e.Package, "sqlserver", StringComparison.OrdinalIgnoreCase)) ?? g.First())
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                FilterEventList();
                RefreshFilterEvents();
                SetStatus($"Ready. {_allEvents.Count} events available.", string.Empty, DashColors.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
        }

        private void FilterEventList()
        {
            var search = txtEventFilter.Text.Trim();
            var filtered = string.IsNullOrEmpty(search)
                ? _allEvents
                : _allEvents.Where(e => e.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            cboOtherEvent.DataSource = filtered;
            cboOtherEvent.DisplayMember = nameof(XEEventInfo.Name);
        }

        // ---- Events ------------------------------------------------------------------------------

        private void AddExtraEvent()
        {
            if (cboOtherEvent.SelectedItem is not XEEventInfo evt) return;
            if (_extraEvents.Any(e => string.Equals(e.Name, evt.Name, StringComparison.OrdinalIgnoreCase))) return;
            _extraEvents.Add(new XETraceEventDef(evt.Package ?? "sqlserver", evt.Name, evt.Fields.Select(f => f.Name)));
            if (evt.Name == "error_reported")
            {
                AddDefaultErrorReportedFilter();
            }
            RefreshFilterEvents();
        }

        private void AddDefaultErrorReportedFilter()
        {
            if (_filters.Any(f => string.Equals(f.EventName, "error_reported", StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(f.Field, "severity", StringComparison.OrdinalIgnoreCase))) return;
            _filters.Add(new XEFilter
            {
                EventName = "error_reported",
                Field = "severity",
                FieldPackage = "sqlserver",
                IsAction = false, // severity is an error_reported data column, referenced as [severity] (not [sqlserver].[severity])
                IsNumeric = true,
                Op = XEFilterOp.GreaterThanOrEqual,
                Value = DefaultErrorSeverityThreshold.ToString()
            });
        }

        /// <summary>Keeps the default sp_reset_connection exclusion in step with the RPC Completed checkbox.</summary>
        private void OnRpcCompletedChanged()
        {
            if (_loadingTemplate) return;
            if (chkRPC.Checked) AddDefaultRpcResetFilter();
            else RemoveRpcResetFilter();
            RefreshFilterGrid();
            RefreshFilterEvents();
        }

        /// <summary>Excludes the connection-pool reset RPC (sp_reset_connection) - noise on almost every trace.</summary>
        private void AddDefaultRpcResetFilter()
        {
            if (_filters.Any(IsRpcResetFilter)) return;
            _filters.Add(new XEFilter
            {
                EventName = "rpc_completed",
                Field = "object_name",
                FieldPackage = "sqlserver",
                IsAction = false,
                IsNumeric = false,
                Op = XEFilterOp.NotEqual,
                Value = RpcResetProc
            });
        }

        private void RemoveRpcResetFilter() => _filters.RemoveAll(IsRpcResetFilter);

        private static bool IsRpcResetFilter(XEFilter f) =>
            string.Equals(f.EventName, "rpc_completed", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.Field, "object_name", StringComparison.OrdinalIgnoreCase) &&
            f.Op == XEFilterOp.NotEqual &&
            string.Equals(f.Value, RpcResetProc, StringComparison.OrdinalIgnoreCase);

        private void RemoveEventByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (string.Equals(name, "rpc_completed", StringComparison.OrdinalIgnoreCase))
            {
                chkRPC.Checked = false; // CheckedChanged -> RefreshFilterEvents
            }
            else if (string.Equals(name, "sql_batch_completed", StringComparison.OrdinalIgnoreCase))
            {
                chkBatchCompleted.Checked = false;
            }
            else if (string.Equals(name, "error_reported", StringComparison.OrdinalIgnoreCase))
            {
                chkErrorReported.Checked = false;
            }
            else
            {
                var idx = _extraEvents.FindIndex(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _extraEvents.RemoveAt(idx);
                RefreshFilterEvents();
            }
        }

        private IEnumerable<string> SelectedEventNames()
        {
            if (chkRPC.Checked) yield return "rpc_completed";
            if (chkBatchCompleted.Checked) yield return "sql_batch_completed";
            if (chkErrorReported.Checked) yield return "error_reported";
            foreach (var e in _extraEvents) yield return e.Name;
        }

        // ---- Filters -----------------------------------------------------------------------------

        private void RefreshFilterEvents()
        {
            var selected = cboEvent.SelectedItem as string;
            var items = new List<string> { AllEventsLabel };
            items.AddRange(SelectedEventNames());
            cboEvent.DataSource = items;
            cboEvent.SelectedItem = selected != null && items.Contains(selected) ? selected : AllEventsLabel;
            RefreshFilterFields();
            PruneStaleCustomizations();
            RefreshEventsGrid();
        }

        // ---- Events grid -------------------------------------------------------------------------

        /// <summary>Drops stored customization state for events that are no longer selected.</summary>
        private void PruneStaleCustomizations()
        {
            var selected = new HashSet<string>(SelectedEventNames(), StringComparer.OrdinalIgnoreCase);
            foreach (var stale in _eventCustomizations.Keys.Where(k => !selected.Contains(k)).ToList())
            {
                _eventCustomizations.Remove(stale);
            }
        }

        private void RefreshEventsGrid()
        {
            var dt = new DataTable();
            dt.Columns.Add("Event", typeof(string));
            dt.Columns.Add("Fields", typeof(string));
            foreach (var name in SelectedEventNames())
            {
                var dataCount = FieldsForEvent(name).Count;
                var custs = CustomizationsForEvent(name);
                var text = $"{dataCount} fields";
                if (custs.Count > 0)
                {
                    var on = custs.Count(c => IsCustomizationOn(name, c));
                    text += $" ({on}/{custs.Count} optional on)";
                }
                dt.Rows.Add(name, text);
            }
            dgvEvents.DataSource = dt;
        }

        private void DgvEvents_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var name = dgvEvents.Rows[e.RowIndex].Cells[ColEventName].Value as string;
            if (string.IsNullOrEmpty(name)) return;
            switch (dgvEvents.Columns[e.ColumnIndex].Name)
            {
                case ColDelete:
                    RemoveEventByName(name);
                    break;

                case ColFields:
                    EditEventFields(name);
                    break;
            }
        }

        private void EditEventFields(string name)
        {
            var dataColumns = FieldsForEvent(name);
            var customizations = CustomizationsForEvent(name);
            if (dataColumns.Count == 0 && customizations.Count == 0)
            {
                SetStatus($"No field information available for {name} (catalog not loaded?)", string.Empty, DashColors.Information);
                return;
            }
            var current = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in customizations) current[c.Name] = IsCustomizationOn(name, c);

            var result = XEEventFieldsForm.Show(this, name, dataColumns, customizations, current);
            if (result == null) return; // cancelled
            _eventCustomizations[name] = result;
            RefreshEventsGrid();
        }

        private List<string> FieldsForEvent(string name)
        {
            var evt = _catalog.FindEvent(name);
            return evt?.Fields.Select(f => f.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
        }

        /// <summary>The boolean customizable columns for an event (the only per-event toggles XE supports).</summary>
        private List<XECustomizableFieldInfo> CustomizationsForEvent(string name)
        {
            var evt = _catalog.FindEvent(name);
            return evt?.Customizations.Where(c => c.IsBoolean && !string.IsNullOrEmpty(c.Name)).ToList()
                   ?? new List<XECustomizableFieldInfo>();
        }

        /// <summary>Current on/off state of a customizable column: the user's stored choice, else the catalog default.</summary>
        private bool IsCustomizationOn(string eventName, XECustomizableFieldInfo c) =>
            _eventCustomizations.TryGetValue(eventName, out var state) && state.TryGetValue(c.Name, out var v)
                ? v
                : c.DefaultOn;

        // ---- Global fields (actions) -------------------------------------------------------------

        private void PickGlobalFields()
        {
            // Offer the instance's actions when known, always including whatever is currently selected so nothing is
            // silently lost if the catalog didn't return actions (e.g. an older service).
            var available = _catalog.Actions
                .Select(a => ActionRef(a.Package, a.Name))
                .Concat(_globalActions.Select(a => ActionRef(a.Package, a.Name)))
                .ToList();
            var current = _globalActions.Select(a => ActionRef(a.Package, a.Name));
            var picked = XEFieldPickerForm.Pick(this, "Global Fields (Actions)", available, current);
            if (picked == null) return; // cancelled

            _globalActions.Clear();
            foreach (var reference in picked)
            {
                var (package, actionName) = SplitActionRef(reference);
                _globalActions.Add(new XEActionDef(package, actionName));
            }
            UpdateGlobalFieldsLabel();
        }

        private void UpdateGlobalFieldsLabel()
        {
            lnkGlobalFields.Text = $"Global Fields ({_globalActions.Count})";
        }

        private static string ActionRef(string package, string name) =>
            $"{(string.IsNullOrEmpty(package) ? "sqlserver" : package)}.{name}";

        private static (string package, string name) SplitActionRef(string reference)
        {
            var dot = reference.IndexOf('.');
            return dot > 0
                ? (reference[..dot], reference[(dot + 1)..])
                : ("sqlserver", reference);
        }

        private void RefreshFilterFields()
        {
            cboField.DataSource = GetFieldsForScope(cboEvent.SelectedItem as string);
            cboField.DisplayMember = nameof(XEFieldInfo.Name);
        }

        private List<XEFieldInfo> GetFieldsForScope(string scope)
        {
            // Global predicate sources apply to any event and are referenced as [pkg].[name].  They win on a name
            // conflict, so e.g. session_id resolves to the [sqlserver].[session_id] predicate source rather than a
            // same-named data column (which would produce an invalid [session_id] reference).
            var byName = new Dictionary<string, XEFieldInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in _catalog.PredSources)
            {
                byName[a.Name] = a;
            }

            // For "(All events)" show global actions only - data columns vary per event.  For a specific event, also
            // offer its data columns (unless shadowed by an action of the same name).
            if (!string.IsNullOrEmpty(scope) && scope != AllEventsLabel)
            {
                var e = _catalog.FindEvent(scope);
                if (e != null)
                {
                    foreach (var f in e.Fields)
                    {
                        if (!byName.ContainsKey(f.Name)) byName[f.Name] = f;
                    }
                }
            }

            return byName.Values.OrderBy(f => f.Name).ToList();
        }

        private void AddFilter()
        {
            if (cboField.SelectedItem is not XEFieldInfo field)
            {
                SetStatus("Select a field to filter on", string.Empty, DashColors.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtValue.Text))
            {
                SetStatus("Enter a filter value", string.Empty, DashColors.Warning);
                return;
            }
            var scope = cboEvent.SelectedItem as string;
            _filters.Add(new XEFilter
            {
                EventName = scope == AllEventsLabel ? null : scope,
                Field = field.Name,
                FieldPackage = field.Package ?? "sqlserver",
                IsAction = field.IsAction,
                IsNumeric = field.IsNumeric,
                Op = (XEFilterOp)cboComparison.SelectedItem,
                Value = txtValue.Text.Trim()
            });
            RefreshFilterGrid();
            txtValue.Clear();
        }

        private void DgvFilters_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvFilters.Columns[e.ColumnIndex].Name == ColFilterDelete) RemoveFilterAt(e.RowIndex);
        }

        private void RemoveFilterAt(int index)
        {
            if (index < 0 || index >= _filters.Count) return;
            _filters.RemoveAt(index);
            RefreshFilterGrid();
        }

        private void RefreshFilterGrid()
        {
            var dt = new DataTable();
            dt.Columns.Add("Event");
            dt.Columns.Add("Field");
            dt.Columns.Add("Comparison");
            dt.Columns.Add("Value");
            foreach (var f in _filters)
            {
                dt.Rows.Add(f.EventName ?? AllEventsLabel, f.Field, f.Op.ToString(), f.Value);
            }
            dgvFilters.DataSource = dt;
        }

        // ---- Instances to trace ------------------------------------------------------------------

        /// <summary>Resolves (or removes) the current instance's AG replicas in the instances list.</summary>
        private async Task OnIncludeAgChangedAsync()
        {
            if (chkIncludeAg.Checked)
            {
                if (_context is not { InstanceID: > 0 }) { chkIncludeAg.Checked = false; return; }
                try
                {
                    var dt = await XETraceRepo.GetAgInstancesAsync(_context.InstanceID);
                    if (dt.Rows.Count == 0)
                    {
                        SetStatus("No other monitored AG replicas found for this instance.", string.Empty, DashColors.Warning);
                        _loadingInstances = true;
                        try { chkIncludeAg.Checked = false; } finally { _loadingInstances = false; }
                        return;
                    }
                    foreach (DataRow r in dt.Rows)
                    {
                        AddInstanceItem(Convert.ToInt32(r["InstanceID"]), r["InstanceName"] as string, isAg: true, check: true);
                    }
                    SetStatus($"Added {dt.Rows.Count} AG replica(s) to the trace.", string.Empty, DashColors.Information);
                }
                catch (Exception ex)
                {
                    SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                    _loadingInstances = true;
                    try { chkIncludeAg.Checked = false; } finally { _loadingInstances = false; }
                }
            }
            else
            {
                // Drop only the AG-discovered items; keep anything the user added manually.
                for (var i = clbInstances.Items.Count - 1; i >= 0; i--)
                {
                    if (clbInstances.Items[i] is TraceInstance { IsAg: true }) clbInstances.Items.RemoveAt(i);
                }
            }
            UpdateInstanceCount();
        }

        private void AddInstancesViaPicker()
        {
            if (_context is not { InstanceID: > 0 }) return;
            var existing = new HashSet<int>(clbInstances.Items.Cast<TraceInstance>().Select(t => t.InstanceID)) { _context.InstanceID };
            var candidates = CommonData.Instances.Rows.Cast<DataRow>()
                .Select(r => (ID: Convert.ToInt32(r["InstanceID"]), Name: r["InstanceGroupName"] as string))
                .Where(c => c.ID > 0 && !existing.Contains(c.ID) && !string.IsNullOrEmpty(c.Name))
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (candidates.Count == 0)
            {
                SetStatus("No other instances available to add.", string.Empty, DashColors.Warning);
                return;
            }
            var picked = XEFieldPickerForm.Pick(this, "Add Instances to Trace",
                candidates.Select(c => c.Name), Enumerable.Empty<string>());
            if (picked == null) return;
            foreach (var c in candidates.Where(c => picked.Contains(c.Name)))
            {
                AddInstanceItem(c.ID, c.Name, isAg: false, check: true);
            }
            UpdateInstanceCount();
        }

        /// <summary>Adds an instance to the instances list (deduped by InstanceID) with the given checked state.</summary>
        private void AddInstanceItem(int instanceId, string name, bool isAg, bool check)
        {
            if (instanceId <= 0 || instanceId == _context?.InstanceID) return;
            if (clbInstances.Items.Cast<TraceInstance>().Any(t => t.InstanceID == instanceId)) return;
            var idx = clbInstances.Items.Add(new TraceInstance { InstanceID = instanceId, Name = name ?? instanceId.ToString(), IsAg = isAg });
            clbInstances.SetItemChecked(idx, check);
        }

        private void UpdateInstanceCount()
        {
            var n = Math.Max(1, clbInstances.CheckedItems.Count); // the current instance is always checked
            lblInstanceCount.Text = $"Tracing {n} instance{(n == 1 ? string.Empty : "s")}";
        }

        /// <summary>The current instance is mandatory - block any attempt to uncheck its (IsCurrent) list item.</summary>
        private void ClbInstances_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (clbInstances.Items[e.Index] is TraceInstance { IsCurrent: true } && e.NewValue == CheckState.Unchecked)
            {
                e.NewValue = CheckState.Checked;
                return;
            }
            BeginInvoke(new Action(UpdateInstanceCount));
        }

        /// <summary>Resets the instance selection to just the (mandatory) current instance - used when it changes.</summary>
        private void ResetInstances()
        {
            _loadingInstances = true;
            try { chkIncludeAg.Checked = false; } finally { _loadingInstances = false; }
            clbInstances.Items.Clear();
            EnsureCurrentInstanceSeeded();
            UpdateInstanceCount();
        }

        /// <summary>Ensures the current instance is present as the mandatory, checked, first item in the list.</summary>
        private void EnsureCurrentInstanceSeeded()
        {
            if (_context is not { InstanceID: > 0 }) return;
            if (clbInstances.Items.Cast<TraceInstance>().Any(i => i.IsCurrent && i.InstanceID == _context.InstanceID)) return;

            // Drop any stale current-instance item (e.g. left over from a previous instance) then seed this one.
            for (var i = clbInstances.Items.Count - 1; i >= 0; i--)
            {
                if (clbInstances.Items[i] is TraceInstance { IsCurrent: true }) clbInstances.Items.RemoveAt(i);
            }
            clbInstances.Items.Insert(0, new TraceInstance
            { InstanceID = _context.InstanceID, Name = _context.InstanceName, IsCurrent = true });
            clbInstances.SetItemChecked(0, true);
            UpdateInstanceCount();
        }

        /// <summary>
        /// The set of contexts to trace: the current instance plus every other checked instance.  Non-current
        /// instances get a fresh context so their own ConnectionID / agents / edition resolve independently.
        /// </summary>
        private List<DBADashContext> EffectiveInstanceContexts()
        {
            var list = new List<DBADashContext>();
            foreach (var item in clbInstances.CheckedItems.Cast<TraceInstance>())
            {
                list.Add(item.IsCurrent && item.InstanceID == _context.InstanceID
                    ? _context
                    : BuildContextForInstance(item.InstanceID, item.Name));
            }
            if (list.Count == 0) list.Add(_context); // safety - the current instance is always seeded + checked
            return list.GroupBy(c => c.InstanceID).Select(g => g.First()).ToList();
        }

        private static DBADashContext BuildContextForInstance(int instanceId, string name) => new()
        {
            InstanceID = instanceId,
            InstanceName = name,
            RegularInstanceIDsWithHidden = new HashSet<int> { instanceId }
        };

        // ---- Run / stop --------------------------------------------------------------------------

        private XETraceConfig BuildConfig()
        {
            XETraceEventTypeFlags(out var events);
            var seconds = (int)(numMaxRunHrs.Value * 3600 + numMaxRunMin.Value * 60 + numMaxRunSec.Value);

            return new XETraceConfig
            {
                Events = events,
                ExtraEvents = _extraEvents.ToList(),
                Filters = _filters.ToList(),
                GlobalActions = _globalActions.ToList(),
                EventCustomizations = BuildEventCustomizations(),
                Target = (XETraceTargetPreference)cboTarget.SelectedItem,
                MaxDurationSeconds = seconds > 0 ? seconds : 300,
                CaptureXel = checkBox4.Checked
            };
        }

        /// <summary>
        /// Builds the per-event customizable-column map for the request.  Only toggles that differ from the catalog
        /// default are sent (keeping the generated DDL minimal); events with no changes are omitted entirely.
        /// </summary>
        private Dictionary<string, List<XECustomization>> BuildEventCustomizations()
        {
            var map = new Dictionary<string, List<XECustomization>>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in SelectedEventNames())
            {
                var changes = new List<XECustomization>();
                foreach (var c in CustomizationsForEvent(name))
                {
                    var on = IsCustomizationOn(name, c);
                    if (on != c.DefaultOn) changes.Add(new XECustomization(c.Name, on ? "1" : "0"));
                }
                if (changes.Count > 0) map[name] = changes;
            }
            return map;
        }

        private void XETraceEventTypeFlags(out XETraceEventType events)
        {
            events = 0;
            if (chkRPC.Checked) events |= XETraceEventType.RpcCompleted;
            if (chkBatchCompleted.Checked) events |= XETraceEventType.SqlBatchCompleted;
            if (chkErrorReported.Checked) events |= XETraceEventType.ErrorReported;
        }

        private async Task StartAsync()
        {
            var config = BuildConfig();
            if (config.Events == 0 && config.ExtraEvents.Count == 0)
            {
                SetStatus("Select at least one event", string.Empty, DashColors.Warning);
                return;
            }

            var instances = EffectiveInstanceContexts();

            // First-run cost warning.  XE traces add overhead to the monitored instance; make sure the user understands
            // that once, before they build the habit.  Suppressible ("Don't show this again") via a user setting.
            if (!Properties.Settings.Default.SuppressXETraceWarning)
            {
                var warn = XEWarningForm.Show(this, "Extended Events Trace",
                    "Extended Events traces can generate large volumes of data and add overhead to the monitored " +
                    "instance.  The impact depends on the events you select, the filters you apply and how busy the " +
                    "instance is.\r\n\r\n" +
                    "The most expensive traces capture high-frequency events (for example statement-level events on a " +
                    "busy server) or events that make the server do extra work (for example capturing query execution " +
                    "plans).\r\n\r\n" +
                    "Be selective about the events and filters you use, especially on busy production instances.",
                    showSuppress: true);
                if (!warn.Continue) return;
                if (warn.Suppress)
                {
                    Properties.Settings.Default.SuppressXETraceWarning = true;
                    Properties.Settings.Default.Save();
                }
            }

            // Many-instances warning.  The trace overhead applies to each instance independently, so tracing a large
            // number at once multiplies the impact.  Not suppressible - the risk scales with the count each run.
            if (instances.Count > ManyInstancesWarningThreshold)
            {
                var warn = XEWarningForm.Show(this, "Multi-Instance XE Trace",
                    $"You are about to start XE traces on {instances.Count} instances at the same time.\r\n\r\n" +
                    "The data streams back through the DBA Dash service and into this window at once.  Tracing many " +
                    "instances can put the service and this client under heavy load depending on the events and filter " +
                    "selection.\r\n\r\n" +
                    "Consider tracing fewer instances, or make sure your events and filters are selective.",
                    showSuppress: false);
                if (!warn.Continue) return;
            }

            // The service hard-caps the trace duration (AdhocXEMaxDurationSeconds).  With several instances the caps
            // may differ, so use the smallest so no instance's request is silently clamped server-side.  Warn and
            // clamp up-front; the per-instance server-side clamp remains as a backstop.
            var cap = instances.Select(t => t.AdhocXEMaxDurationSeconds).Where(c => c > 0).DefaultIfEmpty(0).Min();
            if (cap > 0 && config.MaxDurationSeconds > cap)
            {
                var result = MessageBox.Show(this,
                    $"The requested duration ({TimeSpan.FromSeconds(config.MaxDurationSeconds):g}) exceeds the maximum " +
                    $"of {TimeSpan.FromSeconds(cap):g} allowed by the service.\r\n\r\nThe trace will run for the maximum " +
                    "allowed duration instead.\r\n\r\nContinue?",
                    "Trace Duration Capped", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result != DialogResult.OK) return;
                config.MaxDurationSeconds = cap;
                SetDurationSeconds(cap); // reflect the clamped value back in the duration controls
            }

            ClearGrid();
            _cancelling = false;
            var generation = ++_traceGeneration;
            // Multi-instance run: tag each event with its source instance and share one RunGroupID so the per-instance
            // sessions reload together in history.  A single-instance run stays exactly as before (no tag, no group).
            var multi = instances.Count > 1;
            var runGroupID = multi ? Guid.NewGuid() : (Guid?)null;
            var instanceCount = instances.Count;

            _runningTraces.Clear();
            foreach (var t in instances)
            {
                _runningTraces.Add(new RunningTrace { Context = t, MessageGroup = Guid.NewGuid() });
            }

            _confirmedRunningCount = 0;
            _traceStartTime = DateTime.UtcNow;
            _traceDurationSeconds = config.MaxDurationSeconds;
            SetRunningState(true);
            splitContainer1.Panel1Collapsed = true; // auto-hide config once running
            // The message has been sent but the service hasn't confirmed yet - say so, and flip to "running" only when
            // the service reports each trace has actually started (see OnTraceConfirmedRunning).
            SetStatus(multi
                    ? $"Trace request sent to {instanceCount} instances.  Waiting for the service..."
                    : "Trace request sent.  Waiting for the service...",
                string.Empty, DashColors.Information);
            RunTimer_Tick(null, EventArgs.Empty);
            _runTimer.Start();

            var tasks = _runningTraces
                .Select(rt => RunOneTraceAsync(rt, config, generation, runGroupID, multi, instanceCount))
                .ToList();
            var outcomes = await Task.WhenAll(tasks);

            if (generation != _traceGeneration) return; // superseded (switched away / restarted) - the newer run owns the state

            SetRunningState(false);
            _runTimer.Stop();
            lblTime.Text = $"Ran for {FormatDuration(DateTime.UtcNow - _traceStartTime)}";

            // Remember the current instance's session so History can re-load this run's snapshot for it.  (Each run,
            // even a cancelled one, is persisted.)
            var primary = _runningTraces.FirstOrDefault(rt => rt.Context.InstanceID == _context.InstanceID);
            if (primary?.SessionID is { } sessionID)
            {
                // Carry the RunGroupID for a multi-instance run so switching away and back reloads the whole merged grid.
                var snapshot = new HistorySnapshot(sessionID, runGroupID);
                _historyByInstance[_context.InstanceID] = snapshot;
                _loadedSnapshot = snapshot;
            }

            // Snapshot the instances that ran before clearing, so a follow-up cleanup targets the actual instances
            // (including AG replicas / manually-added ones) rather than falling back to just the current instance.
            var completedTraces = _runningTraces.ToList();
            _runningTraces.Clear();

            if (_cancelling)
            {
                SetStatus("Trace stopped.", string.Empty, DashColors.Information);
                return;
            }

            var failures = outcomes.Where(o => o is { Ok: false }).ToList();
            var alreadyRunning = failures.FirstOrDefault(o =>
                o.Message?.Contains("already running", StringComparison.OrdinalIgnoreCase) == true);
            if (alreadyRunning != null &&
                MessageBox.Show(this, alreadyRunning.Message + "\r\n\r\nStop and clean it up now?", "Ad-hoc Trace",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                await CleanupAsync(completedTraces);
            }
            else if (failures.Count > 0)
            {
                var msg = failures.Count == 1
                    ? failures[0].Message
                    : $"{failures.Count} of {outcomes.Length} traces did not complete successfully:\r\n\r\n" +
                      string.Join("\r\n", failures.Select(f => f.Message));
                MessageBox.Show(this, msg, "Ad-hoc Trace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                SetStatus(multi
                        ? $"Trace complete.  Collected {_results.RowCount} events from {outcomes.Length} instances."
                        : $"Trace complete.  Collected {_results.RowCount} events.",
                    string.Empty, DashColors.Success);
            }
        }

        /// <summary>Runs one instance's trace, recording its session id.  Never throws - failures come back as an outcome.</summary>
        private async Task<XETraceController.XETraceOutcome> RunOneTraceAsync(RunningTrace rt, XETraceConfig config,
            int generation, Guid? runGroupID, bool tagInstance, int instanceCount)
        {
            try
            {
                var capturesXel = !tagInstance; // single-instance run only - a multi-instance .xel would be per-instance
                var outcome = await XETraceController.RunTraceAsync(rt.Context, config, rt.MessageGroup, ControllerStatus,
                    batch => AppendEventsAsync(generation, batch),
                    summary => OnSummary(generation, summary, capturesXel),
                    runGroupID, tagInstance,
                    onRunningConfirmed: () => OnTraceConfirmedRunning(generation, instanceCount));
                rt.SessionID = outcome?.SessionID;
                return outcome ?? new XETraceController.XETraceOutcome(false, false,
                    $"No result from the trace on {rt.Context.InstanceName}.", null);
            }
            catch (Exception ex)
            {
                if (generation == _traceGeneration) SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                return new XETraceController.XETraceOutcome(false, false,
                    $"{rt.Context.InstanceName}: {ex.Message}", rt.SessionID);
            }
        }

        /// <summary>
        /// The service has confirmed one instance's trace is actually running (session created + started).  Moves the
        /// status from "request sent" to "running"; for a multi-instance run it counts how many have started so far.
        /// Superseded once events arrive (AppendEventsAsync shows the collected count instead).
        /// </summary>
        private void OnTraceConfirmedRunning(int generation, int instanceCount)
        {
            if (generation != _traceGeneration) return; // stale (switched away / superseded)
            var confirmed = System.Threading.Interlocked.Increment(ref _confirmedRunningCount);
            SetStatus(instanceCount > 1
                    ? $"Trace running on {confirmed} of {instanceCount} instances.  Waiting for data..."
                    : "Trace running.  Waiting for data...",
                string.Empty, DashColors.Information);
        }

        private void RunTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing) { _runTimer.Stop(); return; }
            var elapsed = DateTime.UtcNow - _traceStartTime;
            var remaining = TimeSpan.FromSeconds(_traceDurationSeconds) - elapsed;
            lblTime.Text = $"Elapsed {FormatDuration(elapsed)}   |   Remaining {FormatDuration(remaining)}";
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }

        private async Task StopAsync()
        {
            if (_cancelling) return; // already stopping - ignore repeat clicks
            _cancelling = true;
            tsStopTrace.Enabled = false; // immediate feedback + prevent stacking clicks
            SetStatus("Stop requested...", string.Empty, DashColors.Warning);
            // Trip the token first - this is what actually ends the trace loop (for the event_file target the
            // reader reads the file, so dropping the session alone wouldn't stop it).  Then drop the session and
            // free the repo lock as a guarantee.  Fan out to every instance being traced - a failure stopping one
            // instance must not abort the stop/cleanup of the rest (they'd be left with orphaned sessions/locks).
            var errors = new List<string>();
            foreach (var rt in _runningTraces.ToList())
            {
                try
                {
                    await XETraceController.CancelAsync(rt.Context, rt.MessageGroup, ControllerStatus);
                    await XETraceController.CleanupAsync(rt.Context, ControllerStatus);
                }
                catch (Exception ex) { errors.Add($"{rt.Context.InstanceName}: {ex.Message}"); }
            }
            if (errors.Count > 0)
            {
                SetStatus("Error stopping one or more traces.", string.Join("\r\n", errors), DashColors.Fail);
            }
        }

        /// <summary>Force-cleans the given instances (or the current instance when none are supplied/running).  A
        /// failure cleaning one instance never aborts cleanup of the rest.</summary>
        private async Task CleanupAsync(IReadOnlyList<RunningTrace> traces = null)
        {
            traces ??= _runningTraces.ToList();
            if (traces.Count == 0)
            {
                await XETraceController.CleanupAsync(_context, ControllerStatus);
                return;
            }
            var errors = new List<string>();
            foreach (var rt in traces)
            {
                try { await XETraceController.CleanupAsync(rt.Context, ControllerStatus); }
                catch (Exception ex) { errors.Add($"{rt.Context.InstanceName}: {ex.Message}"); }
            }
            if (errors.Count > 0)
            {
                SetStatus("Error cleaning up one or more traces.", string.Join("\r\n", errors), DashColors.Fail);
            }
        }

        // ---- Live grid ---------------------------------------------------------------------------

        private Task AppendEventsAsync(int generation, DataTable batch)
        {
            if (generation != _traceGeneration) return Task.CompletedTask; // stale trace (switched/reset) - drop the batch
            if (InvokeRequired) return (Task)Invoke(new Func<Task>(() => AppendEventsAsync(generation, batch)));
            if (generation != _traceGeneration) return Task.CompletedTask; // re-check after marshalling to the UI thread
            _results.AppendEvents(batch);
            SetStatus($"Trace running.  Collected {_results.RowCount} events.", string.Empty, DashColors.Information);
            return Task.CompletedTask;
        }

        private void OnSummary(int generation, DataRow summary, bool capturesXel)
        {
            if (generation != _traceGeneration) return; // stale trace (switched/reset) - ignore its summary
            // Only a single-instance run captures a .xel (a multi-instance capture would be one file per instance).
            // The aggregate "Trace complete" status is set once the whole run finishes (see StartAsync).
            if (capturesXel)
            {
                _xelData = summary.Table.Columns.Contains("XelData") && summary["XelData"] != DBNull.Value
                    ? (byte[])summary["XelData"]
                    : null;
            }
        }

        private void ClearGrid()
        {
            _xelData = null;
            _loadedSnapshot = null; // the grid no longer shows a saved snapshot
            _results.Clear();
        }

        private void SaveXel()
        {
            if (_xelData is not { Length: > 0 })
            {
                SetStatus("No .xel captured (enable 'Capture .xel' before running)", string.Empty, DashColors.Warning);
                return;
            }
            using var dlg = new SaveFileDialog { Filter = "Extended Events (*.xel)|*.xel", FileName = "DBADashTrace.xel" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllBytes(dlg.FileName, _xelData);
            SetStatus($"Saved .xel to {dlg.FileName}", string.Empty, DashColors.Success);
        }

        // ---- History -----------------------------------------------------------------------------

        private async Task LoadHistoryMenuAsync()
        {
            tsHistory.DropDownItems.Clear();
            if (_context is not { InstanceID: > 0 }) return;
            try
            {
                var dt = await XETraceRepo.GetHistoryAsync(new[] { _context.InstanceID }, 7);
                if (dt.Rows.Count == 0)
                {
                    tsHistory.DropDownItems.Add(new ToolStripMenuItem("(no recent traces)") { Enabled = false });
                    return;
                }
                foreach (DataRow r in dt.Rows)
                {
                    var id = Convert.ToInt64(r["XETraceSessionID"]);
                    Guid? runGroup = r.Table.Columns.Contains("RunGroupID") && r["RunGroupID"] != DBNull.Value
                        ? (Guid)r["RunGroupID"]
                        : null;
                    var groupLabel = runGroup.HasValue ? "  [multi-instance]" : string.Empty;
                    var text = $"{r["StartTime"]:g}  -  {r["EventTypes"]}  ({r["TotalEvents"]} events){groupLabel}";
                    var item = new ToolStripMenuItem(text) { Tag = id };
                    item.Click += async (_, _) => await LoadHistoryEventsAsync(id, runGroup);
                    tsHistory.DropDownItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
        }

        private async Task LoadHistoryEventsAsync(long sessionID, Guid? runGroupID = null)
        {
            try
            {
                // A multi-instance run reloads every replica's events together (merged, in time order); a single run
                // loads just its one session.
                var stored = runGroupID.HasValue
                    ? await XETraceRepo.GetEventsByRunGroupAsync(runGroupID.Value)
                    : await XETraceRepo.GetEventsAsync(sessionID);
                _xelData = null;
                _results.LoadEvents(ExpandStoredEvents(stored));
                // Remember what's shown so switching back to this instance re-loads the same snapshot (the whole merged
                // grid when it's a multi-instance run).
                _loadedSnapshot = new HistorySnapshot(sessionID, runGroupID);
                SetStatus($"Loaded {_results.RowCount} events from history", string.Empty, DashColors.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
        }

        // Rebuilds a display table from the stored (event_type, timestamp, Fields JSON) rows - the union of JSON keys
        // becomes the columns.  The column type is inferred from the JSON token types (integer/float -> numeric) so
        // numeric fields like duration/cpu_time/reads come back typed - otherwise the grid's Group By disables
        // Sum/Sum %/Avg because a string column isn't numeric.
        private static DataTable ExpandStoredEvents(DataTable stored)
        {
            // Pass 1: parse each row's Fields JSON once and infer a column type per field across all rows.
            var parsed = new List<(DataRow Source, JObject Fields)>();
            var fieldTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            var order = new List<string>();

            foreach (DataRow r in stored.Rows)
            {
                JObject fields = null;
                if (r["Fields"] != DBNull.Value && r["Fields"] is string json && json.Length > 0)
                {
                    fields = JObject.Parse(json);
                    foreach (var p in fields.Properties())
                    {
                        if (!fieldTypes.ContainsKey(p.Name)) { fieldTypes[p.Name] = null; order.Add(p.Name); }
                        fieldTypes[p.Name] = MergeJsonType(fieldTypes[p.Name], p.Value);
                    }
                }
                parsed.Add((r, fields));
            }

            // Pass 2: build the typed table and fill it.
            var dt = new DataTable();
            dt.Columns.Add("event_type", typeof(string));
            dt.Columns.Add("timestamp", typeof(DateTime));
            foreach (var name in order)
            {
                dt.Columns.Add(name, fieldTypes[name] ?? typeof(string));
            }

            foreach (var (source, fields) in parsed)
            {
                var row = dt.NewRow();
                row["event_type"] = source["event_type"];
                if (source["timestamp"] != DBNull.Value) row["timestamp"] = source["timestamp"];
                if (fields != null)
                {
                    foreach (var p in fields.Properties())
                    {
                        row[p.Name] = ConvertJsonValue(p.Value, dt.Columns[p.Name].DataType);
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>Widens the running inferred type for a field as more JSON values are seen (null = not yet known).</summary>
        private static Type MergeJsonType(Type existing, JToken token)
        {
            var candidate = token?.Type switch
            {
                JTokenType.Integer => typeof(long),
                JTokenType.Float => typeof(double),
                JTokenType.Null or JTokenType.None => null, // null values don't constrain the type
                _ => typeof(string)
            };
            if (candidate == null) return existing;
            if (existing == null || existing == candidate) return candidate;
            // long + double both seen -> use double; anything else mixed -> fall back to string
            if ((existing == typeof(long) || existing == typeof(double)) &&
                (candidate == typeof(long) || candidate == typeof(double)))
            {
                return typeof(double);
            }
            return typeof(string);
        }

        private static object ConvertJsonValue(JToken token, Type type)
        {
            if (token == null || token.Type == JTokenType.Null) return DBNull.Value;
            try
            {
                if (type == typeof(long)) return token.Value<long>();
                if (type == typeof(double)) return token.Value<double>();
                return token.ToString();
            }
            catch
            {
                return DBNull.Value; // shouldn't happen (type was inferred to fit) - be defensive
            }
        }

        // ---- Templates ---------------------------------------------------------------------------

        /// <summary>Captures the current configuration as a named template (optionally global) with per-filter prompts.</summary>
        private void SaveTemplate()
        {
            var filters = _filters.ToList();
            var result = SaveXETemplateForm.Show(this, _lastTemplateName, filters, DBADashUser.HasManageGlobalViews);
            if (result == null) return;

            var cfg = BuildConfig();
            var template = new XETraceTemplate
            {
                Name = result.Name,
                Events = cfg.Events,
                ExtraEvents = cfg.ExtraEvents,
                GlobalActions = cfg.GlobalActions,
                EventCustomizations = cfg.EventCustomizations,
                Target = cfg.Target,
                MaxDurationSeconds = cfg.MaxDurationSeconds,
                CaptureXel = cfg.CaptureXel,
                IncludeAgReplicas = chkIncludeAg.Checked,
                Filters = filters.Select((f, i) => new XETraceFilterTemplate
                {
                    Filter = f,
                    Prompt = i < result.FilterPrompts.Count && result.FilterPrompts[i].Prompt,
                    PromptText = i < result.FilterPrompts.Count ? result.FilterPrompts[i].PromptText : null
                }).ToList()
            };

            var userID = result.IsGlobal ? DBADashUser.SystemUserID : DBADashUser.UserID;
            try
            {
                var exists = XETraceTemplateStore.Get(userID)
                    .Any(t => string.Equals(t.Name, result.Name, StringComparison.OrdinalIgnoreCase));
                if (exists && MessageBox.Show(this,
                        $"A {(result.IsGlobal ? "global " : string.Empty)}template named '{result.Name}' already exists.\r\n\r\nReplace it?",
                        "Save trace template", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                XETraceTemplateStore.Save(userID, template);
                _lastTemplateName = result.Name;
                SetStatus($"Saved template '{result.Name}'{(result.IsGlobal ? " (global)" : string.Empty)}.", string.Empty, DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Save trace template", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Populates the Templates dropdown: this user's templates, the global ones, then delete / save actions.</summary>
        private void LoadTemplatesMenu()
        {
            tsTemplates.DropDownItems.Clear();

            List<XETraceTemplate> userTemplates;
            List<XETraceTemplate> globalTemplates;
            try
            {
                userTemplates = XETraceTemplateStore.Get(DBADashUser.UserID);
                globalTemplates = XETraceTemplateStore.Get(DBADashUser.SystemUserID);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                tsTemplates.DropDownItems.Add(new ToolStripMenuItem("(error loading templates)") { Enabled = false });
                return;
            }

            if (userTemplates.Count == 0 && globalTemplates.Count == 0)
            {
                tsTemplates.DropDownItems.Add(new ToolStripMenuItem("(no saved templates)") { Enabled = false });
            }
            else
            {
                foreach (var t in userTemplates) tsTemplates.DropDownItems.Add(TemplateLoadItem(t, false));
                foreach (var t in globalTemplates) tsTemplates.DropDownItems.Add(TemplateLoadItem(t, true));
                tsTemplates.DropDownItems.Add(new ToolStripSeparator());
                tsTemplates.DropDownItems.Add(BuildDeleteMenu(userTemplates, globalTemplates));
            }

            tsTemplates.DropDownItems.Add(new ToolStripSeparator());
            var save = new ToolStripMenuItem("Save current as template...");
            save.Click += (_, _) => SaveTemplate();
            tsTemplates.DropDownItems.Add(save);
        }

        private ToolStripMenuItem TemplateLoadItem(XETraceTemplate t, bool global)
        {
            var item = new ToolStripMenuItem(global ? $"{t.Name} (global)" : t.Name);
            item.Click += (_, _) => ApplyTemplate(t);
            return item;
        }

        private ToolStripMenuItem BuildDeleteMenu(List<XETraceTemplate> userTemplates, List<XETraceTemplate> globalTemplates)
        {
            var del = new ToolStripMenuItem("Delete");
            foreach (var t in userTemplates)
            {
                var name = t.Name;
                var item = new ToolStripMenuItem(name);
                item.Click += (_, _) => DeleteTemplate(name, false);
                del.DropDownItems.Add(item);
            }
            foreach (var t in globalTemplates)
            {
                var name = t.Name;
                var item = new ToolStripMenuItem($"{name} (global)") { Enabled = DBADashUser.HasManageGlobalViews };
                item.Click += (_, _) => DeleteTemplate(name, true);
                del.DropDownItems.Add(item);
            }
            return del;
        }

        private void DeleteTemplate(string name, bool global)
        {
            if (MessageBox.Show(this, $"Delete template '{name}'{(global ? " (global)" : string.Empty)}?", "Delete trace template",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                XETraceTemplateStore.Delete(global ? DBADashUser.SystemUserID : DBADashUser.UserID, name);
                SetStatus($"Deleted template '{name}'.", string.Empty, DashColors.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Delete trace template", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Loads a template into the UI, prompting for any filter values flagged to prompt on load.</summary>
        private void ApplyTemplate(XETraceTemplate t)
        {
            if (t == null) return;

            // Clone the filters (never mutate the cached template) and resolve prompts BEFORE touching the UI, so
            // cancelling a prompt leaves the current configuration untouched.
            var cloned = (t.Filters ?? new List<XETraceFilterTemplate>())
                .Where(ft => ft?.Filter != null)
                .Select(ft => new XETraceFilterTemplate { Prompt = ft.Prompt, PromptText = ft.PromptText, Filter = CloneFilter(ft.Filter) })
                .ToList();

            var promptItems = cloned.Where(x => x.Prompt).ToList();
            if (promptItems.Count > 0)
            {
                var prompts = promptItems
                    .Select(p => (Label: PromptLabelFor(p), Default: p.Filter.Value ?? string.Empty))
                    .ToList();
                var entered = XETemplatePromptForm.Prompt(this, t.Name, prompts);
                if (entered == null) return; // cancelled - abandon the load
                for (var i = 0; i < promptItems.Count && i < entered.Count; i++) promptItems[i].Filter.Value = entered[i];
            }

            _loadingTemplate = true;
            try
            {
                chkRPC.Checked = t.Events.HasFlag(XETraceEventType.RpcCompleted);
                chkBatchCompleted.Checked = t.Events.HasFlag(XETraceEventType.SqlBatchCompleted);
                chkErrorReported.Checked = t.Events.HasFlag(XETraceEventType.ErrorReported);

                _extraEvents.Clear();
                _extraEvents.AddRange((t.ExtraEvents ?? new List<XETraceEventDef>())
                    .Select(e => new XETraceEventDef(e.Package, e.Name, e.DataColumns)));

                _globalActions.Clear();
                _globalActions.AddRange((t.GlobalActions ?? new List<XEActionDef>())
                    .Select(a => new XEActionDef(a.Package, a.Name)));

                _eventCustomizations.Clear();
                foreach (var kvp in t.EventCustomizations ?? new Dictionary<string, List<XECustomization>>())
                {
                    var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in kvp.Value ?? new List<XECustomization>()) map[c.Name] = c.Value != "0";
                    _eventCustomizations[kvp.Key] = map;
                }

                _filters.Clear();
                _filters.AddRange(cloned.Select(x => x.Filter));

                cboTarget.SelectedItem = t.Target;
                SetDurationSeconds(t.MaxDurationSeconds);
                checkBox4.Checked = t.CaptureXel;
            }
            finally
            {
                _loadingTemplate = false;
            }

            RefreshFilterEvents(); // rebuilds cboEvent/cboField + events grid, prunes stale customizations
            RefreshFilterGrid();
            UpdateGlobalFieldsLabel();
            UpdateXelCaptureState(); // clear xel capture if the loaded target can't produce one

            // Re-resolve AG replicas against the current instance (membership isn't stored - only the intent).  Setting
            // the checkbox fires OnIncludeAgChangedAsync, which adds the replicas (or clears the box if there are none).
            ResetInstances();
            if (t.IncludeAgReplicas) chkIncludeAg.Checked = true;

            _lastTemplateName = t.Name;
            SetStatus($"Loaded template '{t.Name}'.", string.Empty, DashColors.Information);
        }

        private static XEFilter CloneFilter(XEFilter f) => new()
        {
            EventName = f.EventName,
            Field = f.Field,
            FieldPackage = f.FieldPackage,
            IsAction = f.IsAction,
            IsNumeric = f.IsNumeric,
            Op = f.Op,
            Value = f.Value
        };

        private static string PromptLabelFor(XETraceFilterTemplate ft)
        {
            if (!string.IsNullOrWhiteSpace(ft.PromptText)) return ft.PromptText;
            var scope = string.IsNullOrEmpty(ft.Filter.EventName) ? "all events" : ft.Filter.EventName;
            return $"{ft.Filter.Field} ({scope} {ft.Filter.Op}):";
        }

        private void SetDurationSeconds(int seconds)
        {
            if (seconds < 0) seconds = 0;
            SetNum(numMaxRunHrs, seconds / 3600);
            SetNum(numMaxRunMin, seconds % 3600 / 60);
            SetNum(numMaxRunSec, seconds % 60);

            static void SetNum(NumericUpDown num, int value) =>
                num.Value = Math.Max(num.Minimum, Math.Min(num.Maximum, value));
        }

        // ---- State / status ----------------------------------------------------------------------

        private void SetRunningState(bool running)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetRunningState(running))); return; }
            _isRunning = running;
            tsStartTrace.Enabled = !running;
            tsStopTrace.Enabled = running;
            grpConfig.Enabled = groupBox1.Enabled = Filter.Enabled = grpInstances.Enabled = !running;

            if (running)
            {
                StartHeartbeat();
            }
            else
            {
                StopHeartbeat();
            }
        }

        /// <summary>
        /// Starts sending heartbeats for the current trace so the service knows the client is still here.  Every trace
        /// beats; a short one simply ends (and stops the timer) before the first beat is due, so nothing is wasted.
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            // Snapshot the (context, group) pairs at start so the timer never touches the mutable _runningTraces list
            // from a background thread.  A run's instances don't change once started.
            var beats = _runningTraces.Select(rt => (rt.Context, rt.MessageGroup)).ToArray();
            if (beats.Length == 0) return;
            var interval = TimeSpan.FromSeconds(XETraceHeartbeat.IntervalSeconds);
            _heartbeatTimer = new System.Threading.Timer(_ => SendHeartbeats(beats), null, interval, interval);
        }

        private void StopHeartbeat()
        {
            var timer = _heartbeatTimer;
            _heartbeatTimer = null;
            timer?.Dispose();
        }

        /// <summary>Beats every running trace (fire-and-forget).  Guarded so a slow tick can't stack with the next.</summary>
        private void SendHeartbeats((DBADashContext Context, Guid Group)[] beats)
        {
            if (System.Threading.Interlocked.CompareExchange(ref _heartbeatInFlight, 1, 0) != 0) return; // already in flight
            _ = SendHeartbeatsAsync(beats);
        }

        private async Task SendHeartbeatsAsync((DBADashContext Context, Guid Group)[] beats)
        {
            try
            {
                // Beat every instance concurrently - a slow/unavailable instance must not delay beats to the others
                // (a serial loop could let one instance consume the whole interval and starve the rest, causing
                // healthy traces to be declared abandoned).  Each beat swallows its own error.
                await Task.WhenAll(beats.Select(async b =>
                {
                    try { await MessagingHelper.SendHeartbeatAsync(b.Context, b.Group); }
                    catch (Exception ex) { Serilog.Log.Debug(ex, "Error sending XE trace heartbeat for {group}", b.Group); }
                }));
            }
            finally { System.Threading.Interlocked.Exchange(ref _heartbeatInFlight, 0); }
        }

        /// <summary>Sets the status bar text.  Used for our own concise progress messages.</summary>
        private void SetStatus(string message, string details, Color color)
        {
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(() => SetStatus(message, details, color)));
                return;
            }
            tsStatus.Text = message;
            tsStatus.ForeColor = color;
            tsStatus.ToolTipText = details;
        }

        /// <summary>
        /// Handed to the controller/messaging layer.  Only surfaces errors and warnings - routine progress replies
        /// ("Message Received", "Trace running on ...", per-batch counts) are suppressed so the status bar shows the
        /// concise messages set by our own handlers instead.
        /// </summary>
        private void ControllerStatus(string message, string details, Color color)
        {
            if (color == DashColors.Fail || color == DashColors.Warning)
            {
                SetStatus(message, details, color);
            }
        }

        private void SelectErrorReported(object sender, EventArgs e)
        {
            if (_loadingTemplate) return;
            if (chkErrorReported.Checked)
            {
                AddDefaultErrorReportedFilter();
            }
            else
            {
                _filters.Where(f => f.EventName == "error_reported").ToList().ForEach(f => _filters.Remove(f));
            }
            RefreshFilterGrid();
        }
    }
}