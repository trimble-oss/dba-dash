using DBADash.Messaging;
using DBADash.XE;
using DBADashGUI.CustomReports;
using DBADashGUI.Messaging;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
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

        // Save-events-to-file menu items, added to the tsSave dropdown at runtime (see WireEvents).
        private ToolStripMenuItem _saveEventsJsonMenuItem;
        private ToolStripMenuItem _saveEventsJsonGzMenuItem;
        private ToolStripMenuItem _saveEventsXmlMenuItem;
        private ToolStripMenuItem _saveEventsXmlGzMenuItem;

        private bool _cancelling;
        private bool _isRunning;

        // False when the current instance's DBA Dash service has ad-hoc XE tracing disabled: the tab stays visible (the
        // user holds the AdhocXE role) but the config + start controls are disabled with an explanation, rather than
        // letting a trace be built that the service would only reject on start.  Defaults true so construction-time state
        // matches the old behaviour until SetContext resolves the real capability.
        private bool _adhocServiceAvailable = true;

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

        // At a root / instance-group node the tab has no single "current instance": the instance selector is populated
        // from the group's instances (all unchecked - the user picks which to trace).  Group nodes all report InstanceID
        // 0, so we track the group's instance set here to know when to re-sync the selector (switching between two groups).
        private HashSet<int> _lastGroupScope;

        private bool IsGroupMode => _context is { InstanceID: <= 0 };

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

        // Explains the " *" event-field marker drawn in the filter field dropdown (see CboField_DrawItem).
        private readonly ToolTip _fieldTip = new();

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

        // XE stores duration/cpu_time in microseconds, which trips up users (Profiler shows milliseconds) and is
        // tedious to type (10 seconds = 10000000).  For these fields the filter value is entered as a number + a unit
        // (cboUnit) and converted to microseconds; the grid shows the value back in the friendliest matching unit.
        // The unit logic is shared with the template load-time prompt - see XEDurationUnits.
        // cboUnit itself is a designer control (see QuickXETrace.Designer.cs).
        private int _valueBoxFullWidth; // txtValue width when no unit selector is shown (restored from the designer)
        private const int NarrowValueWidth = 70; // txtValue width when the unit selector is shown beside it

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
            cboComparison.DataSource = StringOps; // initial neutral set; reconfigured per field in UpdateFilterInputsForField
            UpdateComparisonHint();

            cboEvent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboField.DropDownStyle = ComboBoxStyle.DropDownList;
            _fieldTip.SetToolTip(cboField,
                "* marks an event-specific field - it applies only to events that expose it.\r\n" +
                "Fields without * are global and apply to every event.");
            cboOtherEvent.DropDownStyle = ComboBoxStyle.DropDownList;

            // Unit selector (designer control cboUnit) for microsecond duration fields (duration, cpu_time): shown beside
            // the value box - narrowed to make room - so the user enters e.g. "10 sec" instead of the zeros in 10000000.
            _valueBoxFullWidth = txtValue.Width;
            cboUnit.DataSource = XEDurationUnits.BindingList();
            cboUnit.DisplayMember = nameof(XEDurationUnits.Unit.Label);
            cboUnit.SelectedIndex = XEDurationUnits.IndexOf(XEDurationUnits.DefaultUnit); // default to ms (Profiler unit)

            dgvFilters.AutoGenerateColumns = false;
            // Read-only: the grid is rebuilt from _filters on every RefreshFilterGrid, so in-cell edits would be
            // silently discarded (and never reach the running trace).  To change a filter the user deletes and re-adds
            // it via the input controls above.  The Delete link column still works (CellContentClick fires regardless).
            dgvFilters.ReadOnly = true;
            dgvFilters.Columns.Clear();
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Applies To", DataPropertyName = "Event", Width = 210 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field", DataPropertyName = "Field", Width = 140 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Comparison", DataPropertyName = "Comparison", Width = 110 });
            dgvFilters.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = "Value", Width = 150 });
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

            _fieldTip.SetToolTip(txtSamplePercent,
                "Capture only a sample of the matching events, to cut volume and overhead on high-frequency events.\r\n" +
                "Enter a percentage (decimals allowed, e.g. 10 or 0.1).  Leave blank to capture every event.\r\n" +
                "XE samples in whole 1-in-N steps, so the effective rate shown may differ slightly from what you type.\r\n" +
                "Sampling is applied after your filters, so it samples the matching events - not everything the server does.");

            UpdateGlobalFieldsLabel();
            UpdateXelCaptureState();
            UpdateSampleAvailability(); // hidden until the catalog confirms the sampling objects exist
        }

        /// <summary>
        /// Enables "Capture .xel" for every target that can write one: the event_file target, and live streaming
        /// (Auto/LiveStream), where the service bolts an event_file target onto the live session.  The ring buffer
        /// (memory) can't produce a .xel, and neither can Azure SQL Database - it has no local disk for the event_file
        /// target (even though it can now stream live).  Otherwise the option is disabled (and cleared) so the user
        /// isn't offered a capture that would silently produce nothing.
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
            // writes no file, and Azure SQL Database has no local disk for an event_file - so no .xel there, even though
            // live streaming itself works on Azure SQL DB.
            if (_context?.EngineEdition == DatabaseEngineEdition.SqlDatabase) return false;
            var target = cboTarget.SelectedItem is XETraceTargetPreference p ? p : XETraceTargetPreference.Auto;
            return target is XETraceTargetPreference.EventFile
                or XETraceTargetPreference.Auto
                or XETraceTargetPreference.LiveStream;
        }

        // ---- Sampling ------------------------------------------------------------------------------

        /// <summary>Whether the instance exposes the objects the sampling predicate needs (divides_by_uint64 + counter).</summary>
        private bool SampleSupported() =>
            _catalog.SupportsComparator("divides_by_uint64") &&
            _catalog.PredSources.Any(p => string.Equals(p.Name, "counter", StringComparison.OrdinalIgnoreCase));

        /// <summary>Shows the sampling controls only when the instance supports the sampling predicate.</summary>
        private void UpdateSampleAvailability()
        {
            if (txtSamplePercent == null) return; // not yet created (early designer/init paths)
            var supported = SampleSupported();
            lblSample.Visible = supported;
            txtSamplePercent.Visible = supported;
            lblSampleEffective.Visible = supported;
            if (!supported) txtSamplePercent.Clear();
        }

        /// <summary>
        /// Blocks casual junk from the sample % box - allows digits, a single (culture) decimal separator, and control
        /// keys.  Deliberately lightweight: paste and culture edge cases still get through, but the effective-rate label
        /// and the Start-time check are the real validation, so this only needs to stop obvious mistyping.
        /// </summary>
        private void SamplePercent_KeyPress(object sender, KeyPressEventArgs e)
        {
            var c = e.KeyChar;
            if (char.IsControl(c) || char.IsDigit(c)) return;
            var decimalSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            // Allow one decimal separator only.
            if (decimalSep.Length == 1 && c == decimalSep[0] && !txtSamplePercent.Text.Contains(decimalSep)) return;
            e.Handled = true;
        }

        private bool TryGetSamplePercent(out double percent) =>
            double.TryParse(txtSamplePercent.Text.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.CurrentCulture, out percent);

        /// <summary>Reflects the entered percentage back as the effective 1-in-N rate XE will actually apply.</summary>
        private void UpdateSampleEffectiveLabel()
        {
            if (string.IsNullOrEmpty(txtSamplePercent.Text.Trim())) { lblSampleEffective.Text = string.Empty; return; }
            if (!TryGetSamplePercent(out var pct) || pct <= 0 || pct > 100)
            {
                lblSampleEffective.Text = "Enter 1-100%";
                return;
            }
            var n = XETraceDefinition.SampleNFromPercent(pct);
            if (n < 2) { lblSampleEffective.Text = "All events (no sampling)"; return; }
            var effective = XETraceDefinition.PercentFromSampleN(n);
            var approx = Math.Abs(effective - pct) > 0.05 ? "≈ " : string.Empty; // "≈" when the % had to be rounded
            lblSampleEffective.Text = $"{approx}1 in {n} events ({effective:0.###}%)";
        }

        /// <summary>The sampling divisor N for the current input (0 = no sampling / unsupported).</summary>
        private int ComputeSampleN() =>
            SampleSupported() && TryGetSamplePercent(out var pct) ? XETraceDefinition.SampleNFromPercent(pct) : 0;

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

            // Save the events currently in the grid to a DBA Dash-native file (works for any target, including ring
            // buffer / Azure SQL DB where no .xel exists).  Inserted just after "Save *.xel".
            _saveEventsJsonMenuItem = new ToolStripMenuItem("Save Events as JSON...", null,
                (_, _) => SaveEventsToFile(GridSerializer.JsonExtension));
            _saveEventsJsonGzMenuItem = new ToolStripMenuItem("Save Events as Compressed JSON...", null,
                (_, _) => SaveEventsToFile(GridSerializer.CompressedJsonExtension));
            _saveEventsXmlMenuItem = new ToolStripMenuItem("Save Events as XML...", null,
                (_, _) => SaveEventsToFile(GridSerializer.XmlExtension));
            _saveEventsXmlGzMenuItem = new ToolStripMenuItem("Save Events as Compressed XML...", null,
                (_, _) => SaveEventsToFile(GridSerializer.CompressedXmlExtension));
            var xelIndex = tsSave.DropDownItems.IndexOf(savexelToolStripMenuItem);
            tsSave.DropDownItems.Insert(xelIndex + 1, _saveEventsXmlGzMenuItem);
            tsSave.DropDownItems.Insert(xelIndex + 1, _saveEventsXmlMenuItem);
            tsSave.DropDownItems.Insert(xelIndex + 1, _saveEventsJsonGzMenuItem);
            tsSave.DropDownItems.Insert(xelIndex + 1, _saveEventsJsonMenuItem);

            // Only offer "Save *.xel" when a .xel was actually captured (needs 'Capture .xel' + an event_file run).
            tsSave.DropDownOpening += (_, _) =>
            {
                var haveXel = _xelData is { Length: > 0 };
                savexelToolStripMenuItem.Enabled = haveXel;
                savexelToolStripMenuItem.ToolTipText = haveXel
                    ? string.Empty
                    : "No .xel captured. Enable 'Capture .xel' before running the trace.";
                var haveEvents = _results.RowCount > 0;
                var noEventsTip = haveEvents ? string.Empty : "No events in the grid to save.";
                foreach (var item in new[] { _saveEventsJsonMenuItem, _saveEventsJsonGzMenuItem, _saveEventsXmlMenuItem, _saveEventsXmlGzMenuItem })
                {
                    item.Enabled = haveEvents;
                    item.ToolTipText = noEventsTip;
                }
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
            txtSamplePercent.TextChanged += (_, _) => UpdateSampleEffectiveLabel();
            txtSamplePercent.KeyPress += SamplePercent_KeyPress;
            cboEvent.SelectedIndexChanged += (_, _) => RefreshFilterFields();
            cboField.SelectedIndexChanged += (_, _) => UpdateFilterInputsForField();
            cboComparison.SelectedIndexChanged += (_, _) => { UpdateComparisonHint(); UpdateCaseSensitiveOption(); };
            cboComparison.Format += CboComparison_Format;
            cboField.Format += CboField_Format;
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
            // A switch is any change of node identity - or leaving a running (possibly group) trace.  Group nodes all
            // report InstanceID 0, so also treat a change of the group's instance set as a switch (handled below).
            // Dropping from a group (InstanceID 0) down to an instance is also a switch, so the group-level picks are
            // cleared and the instance seeds only its own current instance (otherwise the root selection leaks down).
            var leavingGroupForInstance = _context is { InstanceID: <= 0 } && context is { InstanceID: > 0 };
            var switchingInstance = _context != null && context?.InstanceID != _context.InstanceID &&
                                    (_context.InstanceID > 0 || _isRunning || leavingGroupForInstance);
            if (switchingInstance)
            {
                if (_isRunning)
                {
                    var extra = _runningTraces.Count > 1 ? $" (and {_runningTraces.Count - 1} other instance(s))" : string.Empty;
                    var answer = MessageBox.Show(this,
                        $"A XE trace is running for {_context.InstanceName}{extra}.\r\n\r\nStop it and switch to {context?.InstanceName}?",
                        "Ad-hoc XE Trace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

            // The service may have ad-hoc XE tracing disabled for this instance.  Keep the tab (the user holds the
            // AdhocXE role) but disable the config + start controls and explain, instead of loading the catalog and
            // letting a trace be built that would only be rejected on start.
            // At group level availability is per-instance (checked when the trace fans out / rejected per instance), so
            // the tab is available; at instance level it follows the collect agent's advertised capability.
            _adhocServiceAvailable = IsGroupMode || (context is { InstanceID: > 0 } && context.CanRunAdhocXE);
            if (!_adhocServiceAvailable)
            {
                if (switchingInstance) ResetInstances();
                SetRunningState(false); // applies the disabled state (config + start off)
                tsStartTrace.ToolTipText =
                    "Ad-hoc XE tracing is disabled on the DBA Dash service for this instance.";
                SetStatus("Ad-hoc XE tracing is disabled on the DBA Dash service for this instance.", string.Empty,
                    DashColors.Fail);
                return;
            }
            tsStartTrace.ToolTipText = null;

            // Instance selector: at instance level seed the mandatory current instance; at group level sync the list to
            // the group's instances (all unchecked - the user picks which to trace).  A switch resets first.
            if (switchingInstance) ResetInstances();
            if (IsGroupMode) ResetGroupInstancesOnScopeChange();
            else EnsureCurrentInstanceSeeded();

            // AG-replica resolution and the inline history dropdown both need a single current instance, so they're only
            // meaningful at instance level; at group level use the "XE Trace History" tab for history.
            chkIncludeAg.Enabled = !IsGroupMode;
            tsHistory.Enabled = !IsGroupMode;

            SetRunningState(_isRunning); // re-enable config controls when returning from a disabled instance
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
            // At instance level use the current instance; at group level use a representative instance from the group so
            // the event/field pickers are populated (the trace definition is validated per instance on the service, so an
            // event a specific instance's version doesn't support is rejected there).
            var catalogContext = _context is { InstanceID: > 0 } ? _context : RepresentativeInstanceContext();
            if (catalogContext == null) return;
            SetStatus("Loading extended events catalog...", string.Empty, DashColors.Information);
            try
            {
                _catalog = await XETraceController.GetCatalogAsync(catalogContext, ControllerStatus);
                // An event name can exist in several packages (e.g. error_reported in sqlserver and xesvlpkg); show
                // one entry per name, preferring the sqlserver package (the one the trace built-ins/pickers mean).
                _allEvents = _catalog.Events
                    .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.FirstOrDefault(e => string.Equals(e.Package, "sqlserver", StringComparison.OrdinalIgnoreCase)) ?? g.First())
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                FilterEventList();
                RefreshFilterEvents();
                UpdateSampleAvailability(); // catalog now known - show sampling only if its XE objects exist
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
            RefreshFilterGrid(); // an all-events data-column filter's applicable-events list depends on the event set
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
            UpdateFilterInputsForField();
        }

        // Operators offered per field type.  Numeric supports the ordering operators but not LIKE; string supports
        // equality/inequality and LIKE (the ordering operators aren't valid for strings - see BuildFilterTerm).  This
        // is what stops LIKE being offered on numeric fields (and the ordering ops on strings).
        private static readonly XEFilterOp[] NumericOps =
        {
            XEFilterOp.Equal, XEFilterOp.NotEqual, XEFilterOp.GreaterThan, XEFilterOp.LessThan,
            XEFilterOp.GreaterThanOrEqual, XEFilterOp.LessThanOrEqual
        };

        private static readonly XEFilterOp[] StringOps =
        {
            XEFilterOp.Equal, XEFilterOp.NotEqual, XEFilterOp.Like
        };

        /// <summary>
        /// Reconfigures the filter input controls for the currently selected field: the operator list (numeric vs
        /// string ops), the microsecond unit selector (duration fields), the case-insensitive option (unicode string
        /// fields), and a predictable default operator.  Runs on every field (and event-scope) change.
        /// </summary>
        private void UpdateFilterInputsForField()
        {
            if (cboUnit == null) return; // controls not yet created (early designer/init paths)

            var field = cboField.SelectedItem as XEFieldInfo;
            var isNumeric = field?.IsNumeric == true;
            var isDuration = field != null && XEDurationUnits.IsDurationField(field);

            // Offer only the operators valid for the field's type.  Rebinding drops any op that isn't valid for the
            // newly selected field (e.g. a LIKE left over from a string field when switching to a numeric one).
            cboComparison.DataSource = isNumeric ? NumericOps : StringOps;

            // Duration (microsecond) fields enter their value as a number + unit; other fields use the full-width box.
            cboUnit.Visible = isDuration;
            txtValue.Width = isDuration ? NarrowValueWidth : _valueBoxFullWidth;

            // Case-sensitive matching is opt-in (the bare operators are case-insensitive by default), so default the
            // checkbox off.  Its per-operator visibility is set by UpdateCaseSensitiveOption below.
            chkCaseSensitive.Checked = false;

            // Predictable default operator: duration/cpu_time -> >= (find slow/expensive queries); everything else ->
            // equals.  Set after rebinding so the selection lands in the new list (this also fires the operator-changed
            // handler, which refreshes the case-sensitive option and the LIKE hint for the new operator).
            cboComparison.SelectedItem = isDuration ? XEFilterOp.GreaterThanOrEqual : XEFilterOp.Equal;
            UpdateCaseSensitiveOption(); // in case the operator selection didn't actually change
        }

        /// <summary>
        /// Shows the case-sensitive checkbox only when it applies: a unicode string field with an equality/inequality
        /// operator (there is no case-sensitive LIKE comparator) whose case-sensitive comparator exists on the instance
        /// (verified against the catalog's <c>pred_compare</c> list, so we never emit DDL for a comparator it lacks).
        /// The bare operators are case-insensitive regardless of collation, so this is offered on every instance.
        /// </summary>
        private void UpdateCaseSensitiveOption()
        {
            if (chkCaseSensitive == null) return; // not yet created (early designer/init paths)
            chkCaseSensitive.Visible = OfferCaseSensitive(cboField.SelectedItem as XEFieldInfo,
                cboComparison.SelectedItem is XEFilterOp o ? o : XEFilterOp.Equal);
        }

        /// <summary>Whether the case-sensitive option applies for the given field + operator on the current instance.</summary>
        private bool OfferCaseSensitive(XEFieldInfo field, XEFilterOp op) =>
            field?.IsUnicodeString == true &&
            _catalog.SupportsComparator(XETraceDefinition.CaseSensitiveUnicodeComparator(op));

        /// <summary>
        /// Shows a wildcard hint on the value box while LIKE is selected.  The filter emits a SQL LIKE predicate, so it
        /// uses SQL wildcards (% and _) - not the * that Profiler / the SSMS Extended Events grid use - which users
        /// routinely misremember.  The hint is cleared for every other operator (an exact value, no wildcards).
        /// </summary>
        private void UpdateComparisonHint()
        {
            var isLike = cboComparison.SelectedItem is XEFilterOp.Like;
            _fieldTip.SetToolTip(txtValue, isLike
                ? "LIKE wildcards:\r\n" +
                  "  %  matches any sequence of characters (e.g. %report%)\r\n" +
                  "  _  matches a single character (e.g. DBADas_)\r\n" +
                  "[...] character ranges are not supported."
                : string.Empty);
        }

        /// <summary>
        /// Annotates the LIKE item in the comparison combo with its wildcard cue (Like -> "Like (% = wildcard)"), so the
        /// hint is visible in the drop-down itself rather than only in the value box tooltip.  Purely cosmetic - the
        /// bound item is still the <see cref="XEFilterOp"/>, so filter creation is unaffected.
        /// </summary>
        private static void CboComparison_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is XEFilterOp.Like)
            {
                e.Value = "Like (% = wildcard)";
            }
        }

        /// <summary>
        /// Suffixes an event-specific data column with " *" in the field combo (list and closed box) - a compact cue
        /// that, unlike a global predicate source, it only applies to events that expose it (see the "* = event
        /// field..." tooltip on the combo).  Global predicate sources are shown plain.  Purely cosmetic: the bound item
        /// is still the <see cref="XEFieldInfo"/>, so filter creation is unaffected.
        /// </summary>
        private void CboField_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is XEFieldInfo { IsAction: false } field)
            {
                e.Value = field.Name + " *";
            }
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

            // For a specific event, add its data columns (unless shadowed by a predicate source of the same name).
            // For "(All events)", add the data columns exposed by the *selected* events too, so a common event field
            // (e.g. duration on rpc_completed and sql_batch_completed) can be filtered once under "(All events)" and
            // applied to every selected event that exposes it.  The builder skips it for events that don't have the
            // column, so the union across the selected events - not the intersection - is the useful set to offer.
            var eventScopes = scope == AllEventsLabel ? SelectedEventNames() : new[] { scope };
            foreach (var eventName in eventScopes)
            {
                if (string.IsNullOrEmpty(eventName) || eventName == AllEventsLabel) continue;
                var e = _catalog.FindEvent(eventName);
                if (e == null) continue;
                foreach (var f in e.Fields)
                {
                    if (!byName.ContainsKey(f.Name)) byName[f.Name] = f;
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
            if (!TryGetFilterValue(field, out var value, out var error))
            {
                SetStatus(error, string.Empty, DashColors.Warning);
                return;
            }
            var scope = cboEvent.SelectedItem as string;
            var op = (XEFilterOp)cboComparison.SelectedItem;

            // Case-sensitive only when the checkbox is ticked AND it genuinely applies: a unicode string =/<> whose
            // case-sensitive comparator exists on the instance.  Recomputed here (rather than trusting the checkbox's
            // Visible state) so a mismatch can never emit DDL for a missing comparator.  When it applies, capture the
            // comparator's owning package from the catalog so the DDL references it correctly.
            var comparator = XETraceDefinition.CaseSensitiveUnicodeComparator(op);
            var caseSensitive = chkCaseSensitive.Checked && OfferCaseSensitive(field, op);

            _filters.Add(new XEFilter
            {
                EventName = scope == AllEventsLabel ? null : scope,
                Field = field.Name,
                FieldPackage = field.Package ?? "sqlserver",
                IsAction = field.IsAction,
                IsNumeric = field.IsNumeric,
                CaseSensitive = caseSensitive,
                ComparatorPackage = caseSensitive ? _catalog.ComparatorPackage(comparator) : null,
                Op = op,
                Value = value
            });
            RefreshFilterGrid();
            txtValue.Clear();
        }

        /// <summary>
        /// The value string to store on the filter.  For a microsecond duration field the value box holds a number and
        /// <see cref="cboUnit"/> its unit, converted here to whole microseconds (the unit XE expects in the DDL); every
        /// other field stores the trimmed text as typed.
        /// </summary>
        private bool TryGetFilterValue(XEFieldInfo field, out string value, out string error)
        {
            value = txtValue.Text.Trim();
            error = string.Empty;

            if (!XEDurationUnits.IsDurationField(field)) return true;

            var unit = cboUnit.SelectedItem as XEDurationUnits.Unit;
            return XEDurationUnits.TryToMicroseconds(value, unit, out value, out error);
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
                dt.Rows.Add(FilterAppliesToText(f), f.Field, FilterComparisonDisplay(f), FilterValueDisplay(f));
            }
            dgvFilters.DataSource = dt;
        }

        /// <summary>Comparison column text - the operator, flagged "(case sensitive)" for a case-sensitive string match.</summary>
        private static string FilterComparisonDisplay(XEFilter f) =>
            f.CaseSensitive ? $"{f.Op} (case sensitive)" : f.Op.ToString();

        /// <summary>
        /// Display text for the Value column.  A microsecond duration value is shown in the friendliest whole unit
        /// (e.g. 10000000 -> "10 sec"), with the raw microseconds appended for precision; other values show as stored.
        /// </summary>
        private static string FilterValueDisplay(XEFilter f) =>
            XEDurationUnits.IsDurationField(f) ? XEDurationUnits.Humanize(f.Value) : f.Value;

        /// <summary>
        /// The "Applies To" text for a filter row: a specific-event filter names its event (flagged "(not traced)" if
        /// that event isn't currently selected); a global predicate source shows "(All events)"; and an all-events data
        /// column shows "(All applicable): &lt;events&gt;" - the currently-matching events, prefixed to make clear the
        /// filter is not pinned to that fixed list but will also cover any future event that exposes the column.  This
        /// is recomputed whenever the event set changes; a data column matching no selected event is called out as a no-op.
        /// </summary>
        private string FilterAppliesToText(XEFilter f)
        {
            var selected = SelectedEventNames().ToList();

            if (!string.IsNullOrEmpty(f.EventName))
            {
                var traced = selected.Any(n => string.Equals(n, f.EventName, StringComparison.OrdinalIgnoreCase));
                return traced ? f.EventName : $"{f.EventName} (not traced)";
            }

            // All-events scope.  A global predicate source is valid on every event; a data column only on the selected
            // events that expose it (the service skips it elsewhere, and applies it to any future event that has it).
            if (f.IsAction) return AllEventsLabel;

            var applicable = selected
                .Where(name => _catalog.FindEvent(name)?.Fields
                    .Any(x => string.Equals(x.Name, f.Field, StringComparison.OrdinalIgnoreCase)) == true)
                .ToList();
            return applicable.Count == 0
                ? "No applicable events"
                : $"(All applicable): {string.Join(", ", applicable)}";
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
            if (_context == null) return;
            var existing = new HashSet<int>(clbInstances.Items.Cast<TraceInstance>().Select(t => t.InstanceID));
            if (_context is { InstanceID: > 0 }) existing.Add(_context.InstanceID);
            // At group level, offer only the instances in this node's scope (the tag group / all at root); at instance
            // level offer every monitored instance (so AG replicas / any cross-instance can be added).  Only offer
            // instances that actually support Extended Events - an older-version instance can't be traced and would also
            // fail the catalog load if it happened to be the first (representative) one added.
            var scope = IsGroupMode ? _context.InstanceIDs : null;
            var candidates = CommonData.Instances.Rows.Cast<DataRow>()
                .Select(XEInstanceLabels.ToCandidate)
                .Where(c => c != null && !existing.Contains(c.InstanceID) && (scope == null || scope.Contains(c.InstanceID))
                            && BuildContextForInstance(c.InstanceID, c.ListLabel).IsXESupported)
                // When grouping by tag, an instance in more than one tag group appears once per group in
                // CommonData.Instances, so collapse to one candidate per instance to avoid duplicate picker entries.
                .DistinctBy(c => c.InstanceID)
                .ToList();
            if (candidates.Count == 0)
            {
                SetStatus("No other instances available to add.", string.Empty, DashColors.Warning);
                return;
            }
            var picked = XEInstancePickerForm.Pick(this, "Add Instances to Trace", candidates);
            if (picked == null || picked.Count == 0) return;
            var byId = candidates.ToDictionary(c => c.InstanceID);
            foreach (var id in picked)
            {
                if (byId.TryGetValue(id, out var c)) AddInstanceItem(id, c.ListLabel, isAg: false, check: true);
            }
            UpdateInstanceCount();

            // Group level has no "current instance" to load the events catalog from, so load it from the first added
            // instance once we have one (the config pickers need it, and Start refuses until it's available).
            if (IsGroupMode && _catalog.Events.Count == 0) _ = LoadCatalogAsync();
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
            // Instance level always has the (mandatory) current instance checked; group level can have none selected.
            var n = clbInstances.CheckedItems.Count;
            lblInstanceCount.Text = n == 0
                ? "No instances selected"
                : $"Tracing {n} instance{(n == 1 ? string.Empty : "s")}";
        }

        /// <summary>
        /// Group level only: the instance selector starts empty and the user adds instances through the "Add instance"
        /// picker (better than scrolling a listbox pre-filled with a whole group / the entire estate).  We only clear it
        /// when the group's instance set actually changes (switching between two groups); re-selecting the same group
        /// node keeps the user's picks, so the frequent context-following SetContext calls don't wipe the selection.
        /// </summary>
        private void ResetGroupInstancesOnScopeChange()
        {
            var scope = _context?.InstanceIDs ?? new HashSet<int>();
            if (_lastGroupScope != null && _lastGroupScope.SetEquals(scope)) return; // same group - keep current selection
            _lastGroupScope = new HashSet<int>(scope);
            _loadingInstances = true;
            try { clbInstances.Items.Clear(); } finally { _loadingInstances = false; }
            UpdateInstanceCount();
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

        /// <summary>Resets the instance selection when the node changes - to the current instance, or (group level) empty
        /// so the group's instances re-sync from scratch on the incoming node.</summary>
        private void ResetInstances()
        {
            _loadingInstances = true;
            try { chkIncludeAg.Checked = false; } finally { _loadingInstances = false; }
            clbInstances.Items.Clear();
            _lastGroupScope = null; // force a fresh group sync (or none) for the incoming node
            if (!IsGroupMode) EnsureCurrentInstanceSeeded();
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
            { InstanceID = _context.InstanceID, Name = XEInstanceLabels.Resolve(_context.InstanceID, _context.InstanceName), IsCurrent = true });
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
            // Instance level always has the current instance checked; group level can legitimately have none selected
            // (Start is gated on there being at least one - see StartAsync).
            if (list.Count == 0 && _context is { InstanceID: > 0 }) list.Add(_context);
            return list.GroupBy(c => c.InstanceID).Select(g => g.First()).ToList();
        }

        private static DBADashContext BuildContextForInstance(int instanceId, string name) => new()
        {
            InstanceID = instanceId,
            InstanceName = name,
            RegularInstanceIDsWithHidden = new HashSet<int> { instanceId }
        };

        /// <summary>Group level: an instance to load the catalog from - the first checked, else the first listed.</summary>
        private DBADashContext RepresentativeInstanceContext()
        {
            var item = clbInstances.CheckedItems.Cast<TraceInstance>().FirstOrDefault()
                       ?? clbInstances.Items.Cast<TraceInstance>().FirstOrDefault();
            return item == null ? null : BuildContextForInstance(item.InstanceID, item.Name);
        }

        // ---- Run / stop --------------------------------------------------------------------------

        private XETraceConfig BuildConfig()
        {
            XETraceEventTypeFlags(out var events);
            var seconds = maxDuration.TotalSeconds;

            return new XETraceConfig
            {
                Events = events,
                ExtraEvents = _extraEvents.ToList(),
                EventDefs = BuildEventDefs(),
                Filters = _filters.ToList(),
                GlobalActions = _globalActions.ToList(),
                EventCustomizations = BuildEventCustomizations(),
                Target = (XETraceTargetPreference)cboTarget.SelectedItem,
                MaxDurationSeconds = seconds > 0 ? (int)seconds.Value : 300,
                SampleN = ComputeSampleN(),
                CaptureXel = checkBox4.Checked,
                Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
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

        /// <summary>
        /// Resolves every selected event (the built-in shortcuts and the extra events) to a typed definition carrying
        /// its data columns from the catalog.  The service applies each data-column filter (and the severity floor)
        /// only to events that expose the relevant column, so the built-ins resolve their columns from the catalog
        /// exactly like the extra events do - there is no hard-coded per-event column list.
        /// </summary>
        private List<XETraceEventDef> BuildEventDefs()
        {
            var defs = new List<XETraceEventDef>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in SelectedEventNames())
            {
                if (!seen.Add(name)) continue;
                var evt = _catalog.FindEvent(name);
                var package = evt?.Package ?? "sqlserver";
                var columns = evt?.Fields.Select(f => f.Name).Where(n => !string.IsNullOrEmpty(n))
                              ?? Enumerable.Empty<string>();
                defs.Add(new XETraceEventDef(package, name, columns));
            }
            return defs;
        }

        private async Task StartAsync()
        {
            var config = BuildConfig();
            if (config.EventDefs.Count == 0)
            {
                SetStatus("Select at least one event", string.Empty, DashColors.Warning);
                return;
            }

            // A non-empty but invalid sampling percentage would otherwise be silently ignored (no sampling) - surface it.
            if (txtSamplePercent.Visible && !string.IsNullOrWhiteSpace(txtSamplePercent.Text) &&
                (!TryGetSamplePercent(out var samplePct) || samplePct <= 0 || samplePct > 100))
            {
                SetStatus("Enter a sampling percentage between 0 and 100, or clear it to capture every event.",
                    string.Empty, DashColors.Warning);
                return;
            }

            var instances = EffectiveInstanceContexts();
            if (instances.Count == 0)
            {
                // Group level with nothing ticked - there's no instance to trace.
                SetStatus("Select at least one instance to trace.", string.Empty, DashColors.Warning);
                return;
            }

            // Every event carries its data columns from the catalog (the service applies data-column filters and the
            // severity floor per the columns each event exposes).  If the catalog hasn't loaded the events would be
            // sent with no columns, so refuse to start until it's available.  At group level there's no current instance
            // to load it from until instances are added, so load it now (from a selected instance) rather than getting
            // stuck telling the user to wait for a load that was never started.
            if (_catalog.Events.Count == 0)
            {
                await LoadCatalogAsync();
                if (_catalog.Events.Count == 0)
                {
                    SetStatus("Couldn't load the Extended Events catalog.  Check the selected instance(s) support " +
                              "Extended Events, then try again.", string.Empty, DashColors.Warning);
                    return;
                }
            }

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
                MessageBox.Show(this, alreadyRunning.Message + "\r\n\r\nStop and clean it up now?", "Ad-hoc XE Trace",
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
                MessageBox.Show(this, msg, "Ad-hoc XE Trace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                // In a multi-instance run, tag each live batch with its source instance (resolved from the trace's own
                // InstanceID) so the merged grid can tell replicas apart.  History reload derives this from the session
                // row instead (see XEStoredEvents.Expand), so it isn't persisted into the event JSON.
                var instanceLabel = tagInstance ? XEInstanceLabels.Resolve(rt.Context.InstanceID, rt.Context.InstanceName) : null;
                var outcome = await XETraceController.RunTraceAsync(rt.Context, config, rt.MessageGroup, ControllerStatus,
                    batch => AppendEventsAsync(generation, batch, instanceLabel),
                    summary => OnSummary(generation, summary, capturesXel),
                    runGroupID,
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
            if (_cancelling) return; // stopping - leave the frozen "Stopping..." label in place
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
            // Give immediate, honest feedback the moment Stop is clicked.  The trace doesn't flip to "stopped" until
            // its own reply loop receives the terminal reply, which can lag - especially over an SQS relay, where a
            // backlog of in-flight event batches must drain first.  Freeze the clock and show a "Stopping..." state
            // (which AppendEventsAsync / RunTimer_Tick honour via _cancelling) so the UI doesn't keep painting
            // "Trace running..." over this while that drain completes.
            _runTimer.Stop();
            SetStatus("Stopping...", string.Empty, DashColors.Warning);
            lblTime.Text = "Stopping...";
            // Trip the token first - this is what actually ends the trace loop (for the event_file target the
            // reader reads the file, so dropping the session alone wouldn't stop it).  Then drop the session and
            // free the repo lock as a guarantee.  Fan out to every instance being traced concurrently - a slow
            // (SQS) round-trip to one instance must not delay stopping the rest, and a failure stopping one must
            // not abort the stop/cleanup of the others (they'd be left with orphaned sessions/locks).
            var results = await Task.WhenAll(_runningTraces.ToList().Select(async rt =>
            {
                try
                {
                    await XETraceController.CancelAsync(rt.Context, rt.MessageGroup, ControllerStatus);
                    await XETraceController.CleanupAsync(rt.Context, ControllerStatus);
                    return null;
                }
                catch (Exception ex) { return $"{rt.Context.InstanceName}: {ex.Message}"; }
            }));
            var errors = results.Where(e => e != null).ToList();
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

        private Task AppendEventsAsync(int generation, DataTable batch, string instanceLabel = null)
        {
            if (generation != _traceGeneration) return Task.CompletedTask; // stale trace (switched/reset) - drop the batch
            if (InvokeRequired) return (Task)Invoke(new Func<Task>(() => AppendEventsAsync(generation, batch, instanceLabel)));
            if (generation != _traceGeneration) return Task.CompletedTask; // re-check after marshalling to the UI thread
            if (instanceLabel != null) StampInstance(batch, instanceLabel);
            _results.AppendEvents(batch);
            // While stopping, in-flight batches (e.g. an SQS backlog still arriving) are real captured events, so keep
            // them and their count - but say we're stopping rather than repainting "Trace running..." over the
            // "Stopping..." feedback.
            SetStatus(_cancelling
                    ? $"Stopping...  Collected {_results.RowCount} events."
                    : $"Trace running.  Collected {_results.RowCount} events.",
                string.Empty, _cancelling ? DashColors.Warning : DashColors.Information);
            return Task.CompletedTask;
        }

        /// <summary>Stamps the source-instance column on every row of a live batch (multi-instance run only).</summary>
        private static void StampInstance(DataTable batch, string instanceLabel)
        {
            if (batch == null) return;
            if (!batch.Columns.Contains(XETraceController.InstanceColumn))
            {
                batch.Columns.Add(XETraceController.InstanceColumn, typeof(string));
            }
            foreach (DataRow row in batch.Rows)
            {
                row[XETraceController.InstanceColumn] = instanceLabel ?? string.Empty;
            }
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

        /// <summary>
        /// Saves the events currently in the grid to a DBA Dash-native file (JSON or XML).  Unlike "Save *.xel" this
        /// works for every target - ring buffer / Azure SQL DB included - since it serializes the shredded grid rather
        /// than a captured event_file.  Re-open with the XE file viewer.
        /// </summary>
        private void SaveEventsToFile(string extension)
        {
            var events = _results.CurrentEvents;
            if (events is not { Rows.Count: > 0 })
            {
                SetStatus("No events in the grid to save", string.Empty, DashColors.Warning);
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Filter = GridSerializer.SaveFilter,
                FilterIndex = GridSerializer.SaveFilterIndex(extension),
                FileName = "DBADashTrace" + extension
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                GridSerializer.SaveDataTable(events, dlg.FileName);
                SetStatus($"Saved {events.Rows.Count:N0} event(s) to {dlg.FileName}", string.Empty, DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Save events", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Loads a .xel or DBA Dash-native (JSON/XML) trace file from disk into the ad-hoc grid.</summary>
        private async Task OpenTraceFileAsync()
        {
            string path;
            using (var dlg = new OpenFileDialog { Filter = XEFileLoader.OpenFilter })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }
            SetStatus($"Loading {Path.GetFileName(path)}...", string.Empty, DashColors.Information);
            try
            {
                var result = await XEFileLoader.LoadAsync(path);
                // The grid no longer reflects a live capture or a DB history snapshot.
                _xelData = null;
                _loadedSnapshot = null;
                _results.LoadEvents(result.Table, convertTimestampToLocal: result.TimestampsAreUtc, takeOwnership: true);
                SetStatus($"Loaded {_results.RowCount:N0} event(s) from {Path.GetFileName(path)}", string.Empty,
                    _results.RowCount > 0 ? DashColors.Success : DashColors.Warning);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Open trace file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ---- History -----------------------------------------------------------------------------

        private async Task LoadHistoryMenuAsync()
        {
            tsHistory.DropDownItems.Clear();

            // Open a trace file from disk (.xel or DBA Dash JSON/XML) - available regardless of instance context.
            var openFile = new ToolStripMenuItem("Open trace file...", Properties.Resources.FolderOpened_16x);
            openFile.Click += async (_, _) => await OpenTraceFileAsync();
            tsHistory.DropDownItems.Add(openFile);
            tsHistory.DropDownItems.Add(new ToolStripSeparator());

            if (_context is not { InstanceID: > 0 }) return;

            // Shortcut to the full "Trace History" report (all traces, with view/DDL/delete actions).
            var viewAll = new ToolStripMenuItem("View all trace history...");
            viewAll.Click += (_, _) => Main.MainFormInstance?.Instance_Selected(this,
                new Main.InstanceSelectedEventArgs
                { InstanceID = _context.InstanceID, Tab = Main.Tabs.XETraceSessions, SearchFromRoot = true });
            tsHistory.DropDownItems.Add(viewAll);
            tsHistory.DropDownItems.Add(new ToolStripSeparator());

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
                    var note = r.Table.Columns.Contains("Notes") ? r["Notes"] as string : null;
                    // Show a short note inline (truncated) with the full note as the item tooltip.
                    var noteLabel = string.IsNullOrWhiteSpace(note)
                        ? string.Empty
                        : $"  -  {(note.Length > 40 ? note[..40] + "..." : note)}";
                    // StartTime is stored UTC.  Convert to the app time zone so the dropdown matches the Trace History
                    // report (which converts datetime columns to local time), rather than showing raw UTC.
                    var startTime = Convert.ToDateTime(r["StartTime"]).ToAppTimeZone();
                    var text = $"{startTime:g}  -  {r["EventTypes"]}  ({r["TotalEvents"]} events){groupLabel}{noteLabel}";
                    var item = new ToolStripMenuItem(text) { Tag = id, ToolTipText = note };
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
                // loads just its one session.  Stored timestamps are UTC and converted to app time zone during the
                // build, so LoadEvents must not convert again.
                var expanded = runGroupID.HasValue
                    ? await XETraceRepo.GetExpandedEventsByRunGroupAsync(runGroupID.Value, convertTimestampToLocal: true)
                    : await XETraceRepo.GetExpandedEventsAsync(sessionID, convertTimestampToLocal: true);
                _xelData = null;
                _results.LoadEvents(expanded, convertTimestampToLocal: false, takeOwnership: true);
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
                    .Select(p => (Label: PromptLabelFor(p), Default: p.Filter.Value ?? string.Empty,
                        IsDuration: XEDurationUnits.IsDurationField(p.Filter)))
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

        private void SetDurationSeconds(int seconds) =>
            maxDuration.TotalSeconds = Math.Max(0, seconds);

        // ---- State / status ----------------------------------------------------------------------

        private void SetRunningState(bool running)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetRunningState(running))); return; }
            _isRunning = running;
            // The config + start controls are usable only when not running AND the service has ad-hoc tracing enabled.
            var configurable = !running && _adhocServiceAvailable;
            tsStartTrace.Enabled = configurable;
            tsStopTrace.Enabled = running;
            grpConfig.Enabled = groupBox1.Enabled = Filter.Enabled = grpInstances.Enabled = configurable;

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