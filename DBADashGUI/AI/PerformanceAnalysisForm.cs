using DBADash.PerformanceAnalysis;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// AI analysis of an instance's performance over a window, on the same principle as the plan and
    /// deadlock viewers: the reader sees exactly what would be sent, chooses what goes, and nothing leaves
    /// until they press the button.
    ///
    /// The window comes from the range the reader is looking at, capped to a week - see
    /// <see cref="PerformanceAnalysisWindow"/>.  Everything for it is read once; switching a section on or
    /// off, changing how many rows it sends, or dropping the baseline rebuilds the preview from what was
    /// read, so the size readout answers "what if I leave this out" at once.
    ///
    /// A form of its own rather than a panel on the Performance tab, so the charts stay in view while the
    /// reader reads the answer.
    /// </summary>
    internal sealed class PerformanceAnalysisForm : Form
    {
        private const int IncludeColumn = 0;
        private const int SectionColumn = 1;
        private const int TopNColumn = 2;
        private const int TimeSeriesColumn = 3;
        private const int SizeColumn = 4;

        private readonly int _instanceId;
        private readonly string _instance;

        private readonly DateTimePicker _from = NewPicker();
        private readonly DateTimePicker _to = NewPicker();
        private readonly Button _reload = new() { Text = "Reload", AutoSize = true };
        private readonly ComboBox _focus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox _baseline = new() { Text = "Compare with the same window a week earlier", AutoSize = true, Checked = true };
        private readonly Label _windowNote = new() { AutoSize = true, MaximumSize = new Size(380, 0) };

        /// <summary>
        /// Lets the model ask for more detail before it answers - blocking, waiting queries, long running
        /// queries, a deadlock signature - which goes to it on top of the request previewed here.  What it
        /// looked up is listed with the answer.
        /// </summary>
        private readonly CheckBox _investigate = new()
        {
            Text = "Let the AI look up more detail",
            AutoSize = true,
            Checked = true
        };

        /// <summary>Whether the conversation on screen was started with lookups allowed - its follow-ups keep the same offer.</summary>
        private bool _conversationInvestigate;

        /// <summary>
        /// The masking of what the lookups return - the figures sent are aggregates with nothing to mask.  The conversation
        /// on screen keeps the masking it started with, as it keeps its figures.
        /// </summary>
        private readonly AiMasking.MaskToggles _masking;
        private DBADashAI.Mcp.AiDataMask _conversationMask;

        private readonly DataGridView _sections = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        private readonly TextBox _preview = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9f)
        };

        private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        private readonly ToolStripButton _submit;
        private readonly AIModelMenu _model = new();

        /// <summary>
        /// The whole prompt - the service's instructions and the figures - to paste into an AI tool of the
        /// reader's own.  Always available: it spends nothing, and a reader without access to the service, or
        /// with none configured, still gets the analysis question in full.
        /// </summary>
        private readonly ToolStripButton _copyPrompt = new("Copy prompt", Properties.Resources.ASX_Copy_blue_16x)
        {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Enabled = false,
            ToolTipText = "Copy the full prompt - the instructions the AI service would use and the figures below - to paste into an AI tool of your own."
        };

        private readonly AiConversationView _conversation = new() { Dock = DockStyle.Fill };
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
        private readonly TabPage _analysisTab = new("Analysis") { ToolTipText = "The answer, and follow-up questions about it.  Kept until this window is closed." };

        private AIServiceDiscovery.ServiceInfo _service;

        /// <summary>Why there is no service to submit to - none configured, or no access to it.</summary>
        private string _noServiceReason = "No AI service is configured, so nothing can be submitted.";
        private CancellationTokenSource _inFlight;

        /// <summary>The exchange on screen, which a follow-up continues.  Null until something is asked.</summary>
        private AiConversation _current;

        /// <summary>
        /// The payload <see cref="_current"/> was given, which is what its follow-ups carry.  Pinned rather than
        /// rebuilt: the reader can change the sections or reload another window at any moment, and a follow-up
        /// built from the current payload would hand the model figures it had never been shown while the exchange
        /// on screen reads as though nothing moved.  Changed settings go with the next analysis instead.
        /// </summary>
        private PerformanceAnalysisPayload _conversationPayload;

        // The status line is composed from these, each set independently - see UpdateStatus.
        private string _loadNote;
        private string _analysisNote;
        private string _serviceNote;
        private string _sizeNote;
        private Color _statusColour = DashColors.Information;

        private PerformanceAnalysisOptions _options;
        private PerformanceAnalysisData _data;
        private PerformanceAnalysisPayload _payload;
        private CancellationTokenSource _loading;

        /// <summary>Set while the grid is being filled, so filling it is not taken for the reader changing it.</summary>
        private bool _populating;

        /// <summary>
        /// Whether the window came from the global date range, whose time of day and day of week filters the
        /// analysis does not apply.  A report with dates of its own has no such filters to leave out.
        /// </summary>
        private readonly bool _fromGlobalRange;

        private PerformanceAnalysisForm(int instanceId, string instance, PerformanceAnalysisFocus focus, (DateTime FromUtc, DateTime ToUtc)? windowUtc)
        {
            _instanceId = instanceId;
            _instance = instance;
            _options = PerformanceAnalysisOptions.Defaults(focus);

            // Image and text: the caption says whether this starts a conversation or another one, and sending
            // figures out of the estate is not something to leave to an icon alone.
            _submit = new ToolStripButton("Submit for analysis", Properties.Resources.DeadlockAnalyse_16x, async (_, _) => await SubmitAsync())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                ToolTipText = "Send the request shown to the AI service for analysis."
            };

            Text = $"AI Performance Analysis - {instance}";
            Icon = Icon.FromHandle(Properties.Resources.DeadlockAnalyse_16x.GetHicon());
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1200, 760);
            MinimumSize = new Size(800, 500);

            _masking = AiMasking.Toggles(() => { });

            _focus.Items.AddRange([PerformanceAnalysisPayload.DescribeFocus(PerformanceAnalysisFocus.General), PerformanceAnalysisPayload.DescribeFocus(PerformanceAnalysisFocus.LatchesAndSpinlocks)]);
            _focus.SelectedIndex = focus == PerformanceAnalysisFocus.LatchesAndSpinlocks ? 1 : 0;

            BuildLayout();
            _fromGlobalRange = windowUtc is null;
            var (fromUtc, toUtc) = windowUtc ?? (DateRange.FromUTC, DateRange.ToUTC);
            SetWindow(fromUtc, toUtc);
            PopulateSections();

            _reload.Click += async (_, _) => await LoadAsync();
            _focus.SelectedIndexChanged += (_, _) => ChangeFocus();
            _baseline.CheckedChanged += (_, _) =>
            {
                _options.IncludeBaseline = _baseline.Checked;
                RebuildPayload();
            };

            _sections.CurrentCellDirtyStateChanged += (_, _) =>
            {
                // A checkbox only reports its new value once the edit is committed, which would otherwise wait for the
                // reader to click somewhere else.
                if (_sections.IsCurrentCellDirty && _sections.CurrentCell is DataGridViewCheckBoxCell)
                {
                    _sections.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _sections.CellValidating += SectionsCellValidating;
            _sections.CellValueChanged += SectionsCellValueChanged;

            _conversation.QuestionAsked += async (_, question) => await SubmitAsync(question);
            _copyPrompt.Click += (_, _) => CopyPrompt();

            Shown += async (_, _) =>
            {
                _ = FindServiceAsync();
                await LoadAsync();
            };
            FormClosed += (_, _) =>
            {
                _loading?.Cancel();
                _inFlight?.Cancel();
            };


            this.ApplyTheme();
        }

        /// <summary>
        /// Opens the analysis for an instance, over the range the reader is looking at: <paramref name="windowUtc"/>
        /// where the report has dates of its own, the global date range otherwise.
        /// </summary>
        internal static void Open(IWin32Window owner, int instanceId, string instance, PerformanceAnalysisFocus focus = PerformanceAnalysisFocus.General,
            (DateTime FromUtc, DateTime ToUtc)? windowUtc = null)
        {
            // Not owned: an owned window always sits in front of the main window, which leaves the reader
            // working around it while an analysis runs.  A window of its own can go behind and has a taskbar entry.
            var form = new PerformanceAnalysisForm(instanceId, instance, focus, windowUtc) { ShowInTaskbar = true };
            if (owner is Control { TopLevelControl: Form parent })
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(parent.Left + Math.Max(0, (parent.Width - form.Width) / 2), parent.Top + Math.Max(0, (parent.Height - form.Height) / 2));
            }
            form.Show();
        }

        private static DateTimePicker NewPicker() => new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Width = 140
        };

        private void BuildLayout()
        {
            var window = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(6) };
            window.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            window.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            window.Controls.Add(new Label { Text = "Focus", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            window.Controls.Add(_focus, 1, 0);
            window.Controls.Add(new Label { Text = "From", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            window.Controls.Add(_from, 1, 1);
            window.Controls.Add(new Label { Text = "To", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            var toRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
            toRow.Controls.Add(_to);
            toRow.Controls.Add(_reload);
            window.Controls.Add(toRow, 1, 2);
            window.Controls.Add(_baseline, 0, 3);
            window.SetColumnSpan(_baseline, 2);
            window.Controls.Add(_investigate, 0, 4);
            window.SetColumnSpan(_investigate, 2);
            window.Controls.Add(_windowNote, 0, 5);
            window.SetColumnSpan(_windowNote, 2);
            new ToolTip().SetToolTip(_investigate,
                "Blocking, waiting queries, long running queries and deadlocks: the model can ask for up to a few lookups from the repository for this instance and window before it answers.  " +
                "Their results go to the model on top of the request shown here, and each one is listed with the answer.");

            _sections.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Send", FillWeight = 14 });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Section", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Top N", FillWeight = 16, ToolTipText = $"Rows named individually (1-{PerformanceAnalysisOptions.MaxTopN}); the rest are summarised." });
            _sections.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Over time", FillWeight = 20, ToolTipText = "Send a series over the window as well as the summary.  Most of a section's size." });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Size", ReadOnly = true, FillWeight = 22 });

            var left = new Panel { Dock = DockStyle.Fill };
            left.Controls.Add(_sections);
            left.Controls.Add(window);

            var request = new TabPage("Request") { ToolTipText = "Exactly what will be sent to the AI service." };
            request.Controls.Add(_preview);
            _tabs.TabPages.Add(request);
            _analysisTab.Controls.Add(_conversation);

            var options = new ToolStripDropDownButton("Options", Properties.Resources.SettingsOutline_16x, (EventHandler)null)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Options for the analysis request."
            };
            options.DropDownItems.Add(_model.Item);
            options.DropDownItems.AddRange(_masking.Items);

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            toolbar.Items.Add(_submit);
            toolbar.Items.Add(_copyPrompt);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(options);

            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(_tabs);
            right.Controls.Add(toolbar);

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(left);
            split.Panel2.Controls.Add(right);

            var statusBar = new StatusStrip { ShowItemToolTips = true, SizingGrip = false };
            statusBar.Items.Add(_status);

            Controls.Add(split);
            Controls.Add(statusBar);

            Load += (_, _) => split.SplitterDistance = 420;
        }

        /// <summary>Sets the pickers to a window, capped to a week, and says what the cap did where it did anything.</summary>
        private void SetWindow(DateTime fromUtc, DateTime toUtc)
        {
            var window = PerformanceAnalysisWindow.Create(fromUtc, toUtc);
            _from.Value = window.FromUtc.ToAppTimeZone();
            _to.Value = window.ToUtc.ToAppTimeZone();

            var note = window.WasClamped
                ? $"The range selected is longer than {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)}, so the analysis covers its last {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)}.  "
                : string.Empty;

            if (_fromGlobalRange && (DateRange.HasTimeOfDayFilter || DateRange.HasDayOfWeekFilter))
            {
                note += "Time of day and day of week filters are not applied to the analysis.";
            }

            _windowNote.Text = note;
            _windowNote.Visible = note.Length > 0;
        }

        private void ChangeFocus()
        {
            var focus = _focus.SelectedIndex == 1 ? PerformanceAnalysisFocus.LatchesAndSpinlocks : PerformanceAnalysisFocus.General;
            if (focus == _options.Focus) return;

            _options = PerformanceAnalysisOptions.Defaults(focus);
            _options.IncludeBaseline = _baseline.Checked;
            PopulateSections();
            RebuildPayload();
        }

        private void PopulateSections()
        {
            _populating = true;
            try
            {
                _sections.Rows.Clear();
                foreach (var section in _options.Sections)
                {
                    var index = _sections.Rows.Add(section.Include, PerformanceAnalysisOptions.Title(section.Section),
                        section.TopN?.ToString() ?? string.Empty, section.TimeSeries ?? false, string.Empty);
                    var row = _sections.Rows[index];
                    row.Tag = section.Section;

                    if (section.TopN is null)
                    {
                        row.Cells[TopNColumn].ReadOnly = true;
                    }

                    if (section.TimeSeries is null)
                    {
                        // Nothing to tick: hide the box rather than show one that does nothing.
                        row.Cells[TimeSeriesColumn] = new DataGridViewTextBoxCell { Value = string.Empty };
                        row.Cells[TimeSeriesColumn].ReadOnly = true;
                    }
                }
            }
            finally
            {
                _populating = false;
            }
        }

        private void SectionsCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != TopNColumn || _sections.Rows[e.RowIndex].Cells[TopNColumn].ReadOnly) return;

            if (!int.TryParse(Convert.ToString(e.FormattedValue), out var value) || value < 1 || value > PerformanceAnalysisOptions.MaxTopN)
            {
                _loadNote = $"Top N is a number from 1 to {PerformanceAnalysisOptions.MaxTopN}.";
                UpdateStatus();
                e.Cancel = true;
            }
        }

        private void SectionsCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_populating || e.RowIndex < 0) return;

            var row = _sections.Rows[e.RowIndex];
            var section = _options[(PerformanceSection)row.Tag];
            switch (e.ColumnIndex)
            {
                case IncludeColumn:
                    section.Include = row.Cells[IncludeColumn].Value is true;
                    break;
                case TopNColumn when int.TryParse(Convert.ToString(row.Cells[TopNColumn].Value), out var top):
                    section.TopN = top;
                    break;
                case TimeSeriesColumn when section.TimeSeries is not null:
                    section.TimeSeries = row.Cells[TimeSeriesColumn].Value is true;
                    break;
                default:
                    return;
            }

            RebuildPayload();
        }

        /// <summary>Reads everything for the window in the pickers, then shows the payload it makes.</summary>
        private async Task LoadAsync()
        {
            // A time in the hour the clocks skip in spring has no UTC equivalent.
            if (!_from.Value.TryAppTimeZoneToUtc(out var fromUtc) || !_to.Value.TryAppTimeZoneToUtc(out var toUtc))
            {
                _loadNote = "The window starts or ends in an hour the clocks skip for daylight saving.  Move it out of that hour.";
                UpdateStatus();
                return;
            }

            if (toUtc <= fromUtc)
            {
                _loadNote = "The window must end after it starts.";
                UpdateStatus();
                return;
            }

            _loading?.Cancel();
            _loading = new CancellationTokenSource();
            var token = _loading.Token;

            SetWindow(fromUtc, toUtc);
            var window = PerformanceAnalysisWindow.Create(fromUtc, toUtc);

            // The figures shown are for the window being replaced, so nothing can be built, copied or submitted
            // from them while the new one loads.  The conversation on screen keeps its own - _conversationPayload.
            _data = null;
            _payload = null;
            _sizeNote = null;
            _copyPrompt.Enabled = false;
            _submit.Enabled = false;
            _reload.Enabled = false;
            _loadNote = "Loading performance data...";
            UpdateStatus();
            _preview.Text = string.Empty;

            try
            {
                var data = await PerformanceAnalysisLoader.LoadAsync(Common.ConnectionString, Config.DefaultCommandTimeout, _instanceId, _instance, window, token);
                if (token.IsCancellationRequested || IsDisposed) return;

                _data = data;
                MarkEmptySections();
                RebuildPayload();
            }
            catch (OperationCanceledException)
            {
                // A newer load, or the form closing.
            }
            catch (Exception ex)
            {
                _loadNote = $"Could not load performance data: {ex.Message}";
                UpdateStatus();
            }
            finally
            {
                if (!IsDisposed) _reload.Enabled = true;
            }
        }

        /// <summary>
        /// Says which sections have nothing in this window.  They can still be sent - "no blocking" is worth
        /// knowing - but a latch section for an instance that does not collect them is only noise, so those
        /// start unticked.
        /// </summary>
        private void MarkEmptySections()
        {
            var empty = PerformanceAnalysisLoader.Empty(_data).ToHashSet();
            _populating = true;
            try
            {
                foreach (DataGridViewRow row in _sections.Rows)
                {
                    var section = (PerformanceSection)row.Tag;
                    var isEmpty = empty.Contains(section) || _data.Errors.ContainsKey(section);
                    row.DefaultCellStyle.ForeColor = isEmpty ? DashColors.NotApplicable : Color.Empty;

                    if (isEmpty && section is PerformanceSection.Latches or PerformanceSection.Spinlocks or PerformanceSection.RunningQueries or PerformanceSection.Deadlocks)
                    {
                        _options[section].Include = false;
                        row.Cells[IncludeColumn].Value = false;
                    }
                }
            }
            finally
            {
                _populating = false;
            }
        }

        private void RebuildPayload()
        {
            if (_data is null) return;

            _payload = PerformanceAnalysisPayload.Build(_data, _options, utc => utc.ToAppTimeZone(), DescribeTimeZone());
            var preview = _payload.ToPreview();
            _preview.Text = preview.Replace("\n", Environment.NewLine).Replace("\r\r", "\r");

            var empty = PerformanceAnalysisLoader.Empty(_data).ToHashSet();
            foreach (DataGridViewRow row in _sections.Rows)
            {
                var section = (PerformanceSection)row.Tag;
                var sent = _payload.Sections.FirstOrDefault(s => s.Section == section);
                row.Cells[SizeColumn].Value = _data.Errors.ContainsKey(section) ? "failed"
                    : sent is not null ? $"{(sent.Content.Length + sent.Title.Length) / 1024d:0.0} KB"
                    : empty.Contains(section) ? "no data"
                    : string.Empty;
                row.Cells[SizeColumn].ToolTipText = _data.Errors.TryGetValue(section, out var error) ? error : string.Empty;
            }

            _loadNote = null;
            _copyPrompt.Enabled = _payload.Sections.Count > 0;
            _sizeNote = $"Request size: {PerformanceAnalysisPayload.DescribeSize(preview.Length)}." +
                        (_payload.IsLarge ? "  This is large - consider leaving out a section or a time series." : string.Empty);
            _submit.Enabled = CanSubmit;
            UpdateStatus();
        }

        private void CopyPrompt()
        {
            if (_payload is null) return;

            Clipboard.SetText(_payload.ToPrompt());
            _analysisNote = "Prompt copied.  It carries the instance name and the figures shown - paste it only where you are happy for those to go.";
            _statusColour = DashColors.Information;
            UpdateStatus();
        }

        private bool CanSubmit => _service is not null && _payload is { Sections.Count: > 0 } && _inFlight is null;

        /// <summary>
        /// Composes the status line from the things that arrive independently: the data loading, the
        /// service being found, an answer coming back, and the size of the request.  Whichever lands last
        /// must not erase the others.
        /// </summary>
        private void UpdateStatus()
        {
            // "Ready to submit" is only news before anything has been.  The absence of a service is worth
            // saying throughout - it is why the button is disabled.
            var serviceNote = _service is not null && _analysisNote is not null ? null : _serviceNote;
            var parts = new[] { _loadNote, _analysisNote, serviceNote, _sizeNote }.Where(p => !string.IsNullOrEmpty(p)).ToList();

            _status.Text = string.Join("  ", parts);
            _status.ToolTipText = AIStatusText.ToolTip(parts);
            _status.ForeColor = _payload is { IsLarge: true } && _statusColour == DashColors.Information
                ? DashColors.Warning
                : _statusColour;
        }

        private async Task FindServiceAsync()
        {
            var service = await QueryPlans.PlanAnalysisClient.FindServiceAsync();
            if (IsDisposed) return;

            // What spends the tokens is submitting, and that is what access controls: the AIUser role is what
            // unmasks the service's API key, and without it a request is turned away.  Everything else here -
            // reading the figures, previewing, copying the prompt to use elsewhere - is open to anyone who can
            // see the charts.
            // Whatever the source: a local service can carry the repository's key, and with it its access flag.
            if (service is { HasApiKeyAccess: false })
            {
                _service = null;
                _noServiceReason = "You don't have access to the AI service (the AIUser role), so nothing can be submitted.  " +
                                   "Copy prompt gives you the request to use with an AI tool of your own.";
            }
            else
            {
                _service = service;
                _noServiceReason = "No AI service is configured, so nothing can be submitted.  " +
                                   "Copy prompt gives you the request to use with an AI tool of your own.";
            }

            _serviceNote = _service is null
                ? _noServiceReason
                : $"Ready to submit to {_service.ServiceUrl}.  Nothing is sent until you do.";
            _submit.Enabled = CanSubmit;
            UpdateStatus();
            UpdateCanAsk();

            // After the button: the model list is a nicety, and a slow answer to it should not keep the
            // reader from submitting with the configured model.
            await _model.LoadAsync(_service);
        }

        /// <summary>
        /// Asks the model.  With no <paramref name="question"/> that starts a new conversation about the
        /// request as it stands; with one it is the next turn of the conversation on screen, which goes up
        /// with the request that conversation started from and everything said since.
        ///
        /// Nothing is stored.  The conversation lasts as long as this window.
        /// </summary>
        private async Task SubmitAsync(string question = null)
        {
            if (_service is null || _inFlight is not null) return;

            // A follow-up goes with the figures its conversation started from, so it can be asked while a reload is under way.
            var continuing = question is null ? null : _current;
            if (question is not null && continuing is null) return;
            if (question is null && _payload is null) return;

            var payload = continuing is null ? _payload : _conversationPayload;
            var investigate = continuing is null ? _investigate.Checked : _conversationInvestigate;
            var mask = continuing is null ? _masking.Mask : _conversationMask;

            // Only the lookups carry statement text; without them there is nothing masking would change.
            if (continuing is null && investigate && !AiMasking.ConfirmUnmasked(mask, this)) return;

            _inFlight = new CancellationTokenSource();
            _submit.Enabled = false;
            UpdateCanAsk();
            _analysisNote = question is null ? "Analysing..." : "Answering...";
            _statusColour = DashColors.Information;
            UpdateStatus();

            try
            {
                var result = await PerformanceAnalysisClient.AnalyseAsync(payload, _service, _inFlight.Token, continuing, question, _model.SelectedModel, investigate, mask.Word);
                if (IsDisposed) return;

                if (!result.Success)
                {
                    _analysisNote = result.Error;
                    _statusColour = DashColors.Fail;
                    UpdateStatus();

                    // The question went nowhere, so it comes back rather than being lost with the attempt.
                    if (question is not null) _conversation.RestoreQuestion(question);
                    return;
                }

                var conversation = continuing ?? AiConversation.Restore(result.ConversationId ?? Guid.NewGuid(), Array.Empty<AiConversation.Turn>());
                conversation.Add(new AiConversation.Turn(question, result.Analysis ?? string.Empty, result.Model, result.GeneratedUtc ?? DateTime.UtcNow)
                {
                    Investigations = result.Investigations
                });

                _current = conversation;
                _conversationPayload = payload;
                _conversationInvestigate = investigate;
                _conversationMask = mask;

                if (!_tabs.TabPages.Contains(_analysisTab)) _tabs.TabPages.Add(_analysisTab);
                _tabs.SelectedTab = _analysisTab;
                if (!await _conversation.ShowAsync(conversation, () => !IsDisposed && ReferenceEquals(conversation, _current))) return;

                _analysisNote = $"Analysed by {result.Model ?? "the configured model"}.  Generated advice - check it against the charts before acting on it.";
                _statusColour = DashColors.Success;
                UpdateStatus();

                _submit.Text = "Analyse again";
                _submit.ToolTipText = "Start a new conversation from the request as it stands now.  To build on the analysis shown, ask a follow-up instead - " +
                                      "it keeps the figures the analysis was given, whatever has changed since.";
            }
            finally
            {
                _inFlight?.Dispose();
                _inFlight = null;

                if (!IsDisposed)
                {
                    _submit.Enabled = CanSubmit;
                    UpdateCanAsk();
                    if (question is not null) _conversation.FocusQuestion();
                }
            }
        }

        /// <summary>Whether a follow-up can be asked: there has to be something to follow, a service, and nothing in flight.</summary>
        private void UpdateCanAsk()
        {
            if (_service is null)
            {
                _conversation.SetCanAsk(false, _noServiceReason);
            }
            else if (_inFlight is not null)
            {
                _conversation.SetCanAsk(false, "Waiting for the model...");
            }
            else
            {
                _conversation.SetCanAsk(_current is { IsEmpty: false });
            }
        }

        private static string DescribeTimeZone()
        {
            var zone = DateHelper.AppTimeZone;
            var offset = zone.GetUtcOffset(DateTime.UtcNow);
            return $"{zone.Id}, UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}";
        }
    }
}
