using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADashGUI.AgentJobs;
using DBADash;
using DBADashGUI.AI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// AI analysis of the deadlock, on the same principle as everything else that leaves the estate:
    /// the user sees exactly what would be sent, and nothing goes until they press the button.
    ///
    /// The preview is not a description of the payload - it is the payload, rendered by the same
    /// object the client serialises, so the two cannot drift.  It is worth reading before pressing
    /// send: a deadlock graph carries the statements as they ran, and those routinely include
    /// parameter values.
    ///
    /// Once an answer arrives the request gives up the screen to it - by then it has served its
    /// purpose, and the analysis is what the reader came for - but it stays one button away.
    ///
    /// The answer is rarely the end of it.  An analysis raises questions - is that index the one that
    /// would fix it, what would happen if the transaction were shorter, why is the isolation level
    /// what it is - so the answer comes with a box to ask the next one, and the whole exchange is
    /// stored and shown again next time the deadlock is opened.  Each follow-up carries the deadlock
    /// and everything said so far back to the service, which holds no conversation state of its own.
    /// </summary>
    internal sealed class DeadlockAiControl : UserControl
    {
        private readonly TextBox _preview;
        private readonly AiConversationView _conversation = new() { Dock = DockStyle.Fill };
        private readonly SplitContainer _split;

        private readonly ToolStripButton _submit;
        private readonly ToolStripDropDownButton _options;
        private readonly ToolStripMenuItem _showRequest;
        private readonly ToolStripLabel _signature = new();

        /// <summary>Every stored conversation about this deadlock and its pattern, to pick from.  Hidden when there are none.</summary>
        private readonly ToolStripDropDownButton _history = new("Previous analyses")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Visible = false,
            ToolTipText = "Conversations about this deadlock, then about other occurrences of its pattern, newest first."
        };

        private readonly ToolStripStatusLabel _status = new();

        private readonly ToolStripMenuItem _includeSchema;

        private DeadlockAnalysisPayload _payload;
        private AIServiceDiscovery.ServiceInfo _service;
        private CancellationTokenSource _inFlight;

        /// <summary>The exchange on screen, which a follow-up continues.  Null until something is asked or restored.</summary>
        private AiConversation _current;

        // Held so the payload can be rebuilt when the schema is toggled without going back to the
        // repository, and so the preview always matches what the toggle currently says.
        private DeadlockGraph _graph;

        private string _instance;
        private int? _instanceId;

        // The status line is composed from these: an answer note and a service note, each set by its
        // own async lookup, plus the colour the more important of the two deserves.
        private string _analysisNote;

        private string _serviceNote;
        private Color _statusColour = DashColors.Information;
        private IReadOnlyList<DeadlockObjectDefinition> _schema = Array.Empty<DeadlockObjectDefinition>();

        internal DeadlockAiControl()
        {
            _preview = NewTextBox();

            // Image and text: the caption is what says whether this is the first run or another one,
            // and sending the deadlock out of the estate is not something to leave to an icon alone.
            _submit = new ToolStripButton("Submit for analysis", Properties.Resources.DeadlockAnalyse_16x, async (_, _) => await SubmitAsync())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                ToolTipText = "Send everything shown below to the AI service for analysis."
            };

            _options = new ToolStripDropDownButton("Options", Properties.Resources.SettingsOutline_16x, (EventHandler)null)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Options for the analysis request."
            };

            _showRequest = new ToolStripMenuItem("Show request", null, (_, _) => ShowRequest(_showRequest.Checked))
            {
                CheckOnClick = true,
                Checked = true,
                Visible = false, // Nothing to hide the request behind until an analysis exists
                ToolTipText = "Show what was sent to the AI service alongside the analysis."
            };

            _includeSchema = new ToolStripMenuItem("Include schema", null, (_, _) => RebuildPayload())
            {
                CheckOnClick = true,
                Checked = true,
                Visible = false, // Shown once we know whether the repository has any
                ToolTipText = "Include the definitions of the objects involved, as they were at the " +
                              "time of the deadlock.  They make the analysis better and the request larger."
            };

            _options.DropDownItems.Add(_showRequest);
            _options.DropDownItems.Add(_includeSchema);

            // Follow-up questions stay on this machine, which is what makes them private and also what
            // means they need moving by hand.  The dialog lives with the feature it belongs to.
            _options.DropDownItems.Add(new ToolStripSeparator());
            _options.DropDownItems.Add(new ToolStripMenuItem(
                "My AI conversations...", null,
                async (_, _) => await AiConversationsForm.OpenAsync(FindForm()))
            {
                ToolTipText = "Your follow-up questions and answers are saved on this computer only.  " +
                              "Export them before it is replaced."
            });
            UpdateOptionsVisibility();

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            toolbar.Items.Add(_submit);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_options);
            toolbar.Items.Add(_history);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_signature);

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2Collapsed = true // Nothing to show until an answer comes back
            };
            _split.Panel1.Controls.Add(_preview);
            _split.Panel2.Controls.Add(_conversation);

            var statusBar = new StatusStrip();
            statusBar.Items.Add(_status);

            Controls.Add(_split);
            Controls.Add(toolbar);
            Controls.Add(statusBar);

            _conversation.QuestionAsked += async (_, question) => await SubmitAsync(question);
        }

        private static TextBox NewTextBox() => new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f)
        };

        /// <summary>
        /// Builds the payload for a graph and shows it.  Deliberately does not contact anything: the
        /// tab can be opened, read and closed again without a byte leaving the machine.
        /// </summary>
        internal void Show(DeadlockGraph graph, string instance, DBADashContext context)
        {
            _graph = graph;
            _instance = instance;
            _instanceId = context is { InstanceID: > 0 } ? context.InstanceID : null;
            _schema = Array.Empty<DeadlockObjectDefinition>();
            _includeSchema.Visible = false;

            RebuildPayload();

            // A new deadlock means the previous conversation is about something else.
            _current = null;
            _conversation.Clear();
            _split.Panel1Collapsed = false;
            _split.Panel2Collapsed = true;
            _showRequest.Visible = false;
            _showRequest.Checked = true;
            _analysisNote = null;
            _submit.Text = "Submit for analysis";
            _history.DropDownItems.Clear();
            _history.Visible = false;
            UpdateOptionsVisibility();

            _ = FindServiceAsync();
            _ = FetchSchemaAsync(context);
            _ = LoadHistoryAsync(showLatest: true);
        }

        /// <summary>
        /// Fills the Previous analyses drop-down, and when <paramref name="showLatest"/> shows the first entry -
        /// the newest conversation about this exact deadlock, or failing that the newest about its pattern.
        ///
        /// The pattern is the point of the fallback: two hundred occurrences of one problem have one
        /// answer, and the reader should not have to know they are looking at a repeat, or pay for
        /// the answer again to find out.  A conversation about this exact graph is still the better one
        /// to lead with where there is one - a signature is a grouping heuristic, and can be too broad.
        /// Model and payload version are deliberately ignored - an older answer is still worth reading,
        /// and asking again is one button away.
        /// </summary>
        private async Task LoadHistoryAsync(bool showLatest)
        {
            var graph = _graph;
            var history = await DeadlockAnalysisHistory.FetchAsync(_payload?.Signature, _payload?.DeadlockHash);

            // A different deadlock was selected while this was in flight.
            if (IsDisposed || !ReferenceEquals(graph, _graph)) return;

            _history.DropDownItems.Clear();
            foreach (var entry in history)
            {
                var instance = string.IsNullOrEmpty(entry.Instance) ? string.Empty : $" on {entry.Instance}";
                var item = new ToolStripMenuItem(
                    $"{entry.GeneratedUtc.ToLocalTime():g}  {entry.Model}{instance}  ({entry.Scope}, {entry.Length})")
                {
                    Tag = entry
                };
                item.Click += async (_, _) => await ShowEntryAsync(entry);
                _history.DropDownItems.Add(item);
            }

            _history.Visible = history.Count > 0;
            if (history.Count == 0) return;

            // Once a conversation is on screen it stays: the reader is reading it, or has just asked for it.
            if (showLatest && _split.Panel2Collapsed && _inFlight is null)
            {
                await ShowEntryAsync(history[0]);
            }
        }

        /// <summary>
        /// Shows a stored conversation, saying what it is about and where it came from.  The reader can
        /// carry on adding to it: a conversation read back is a conversation, not a transcript.
        /// </summary>
        private async Task ShowEntryAsync(DeadlockAnalysisHistory.Entry entry)
        {
            if (_inFlight is not null) return;

            var graph = _graph;

            // A conversation stored before follow-ups existed has no id of its own.  Continuing it mints
            // one, so the answers added from here are stored as the exchange they are; the turns already
            // stored keep theirs, and stay a conversation of their own in the drop-down.
            var conversation = AiConversation.Restore(
                entry.ConversationId ?? Guid.NewGuid(),
                entry.Turns.Select(t => new AiConversation.Turn(t.Question, t.Analysis, t.Model, t.GeneratedUtc)));

            if (!await ShowConversationAsync(conversation, graph)) return;

            foreach (var item in _history.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = ReferenceEquals(item.Tag, entry);
            }

            var age = entry.GeneratedUtc.ToLocalTime().ToString("g");
            var others = _history.DropDownItems.Count > 1
                ? $"  {_history.DropDownItems.Count} conversations are stored - see Previous analyses."
                : string.Empty;
            var thinner = entry.WithoutSchema && _schema.Count > 0
                ? "  It was produced without the object definitions now available - re-analyse to include them."
                : string.Empty;

            _analysisNote =
                $"Previous analysis of {entry.Scope}, by {entry.Model} on {age}.{thinner}{others}  " +
                "Generated advice - check it against the code before acting on it.";
            _statusColour = DashColors.Information;
            UpdateStatus();

            // With an answer already on screen, the button is starting a fresh conversation rather than
            // the first one, and should say so.  Building on what is shown is what the question box is for.
            _submit.Text = "Analyse again";
            _submit.ToolTipText = "Start a new conversation about this graph.  Every answer is kept, and two runs " +
                                  "over the same graph rarely say quite the same thing - the request may also now " +
                                  "carry more than it did.  To build on the analysis shown, ask a follow-up instead.";
        }

        /// <summary>
        /// Composes the status line from the two things that arrive independently: what is on screen
        /// (a fresh answer, a stored one, or nothing yet) and whether a service is available to ask.
        /// Both are found asynchronously, so whichever lands second must not erase the other.
        /// </summary>
        private void UpdateStatus()
        {
            var parts = new[] { _analysisNote, _serviceNote }.Where(p => !string.IsNullOrEmpty(p));

            _status.Text = string.Join("  ", parts);
            _status.ForeColor = _statusColour;
        }

        /// <summary>Rebuilds the payload and the preview from what is currently switched on.</summary>
        private void RebuildPayload()
        {
            if (_graph is null) return;

            var schema = _includeSchema.Checked ? _schema : Array.Empty<DeadlockObjectDefinition>();
            _payload = DeadlockAnalysisPayload.Build(_graph, DeadlockAnalyser.Analyse(_graph), _instance, schema);

            _preview.Text = _payload.ToPreview().Replace("\n", Environment.NewLine);
            _signature.Text = $"Signature {_payload.Signature}";
        }

        /// <summary>
        /// Looks for the definitions of the objects involved.  Runs after the preview is already on
        /// screen, because the repository may be slow and the graph is worth reading meanwhile - the
        /// preview simply gains a section when they arrive.
        /// </summary>
        private async Task FetchSchemaAsync(DBADashContext context)
        {
            var graph = _graph;
            _schema = await DeadlockSchemaLookup.FetchAsync(context, graph);

            // A different deadlock was selected while this was in flight.
            if (IsDisposed || !ReferenceEquals(graph, _graph)) return;

            _includeSchema.Visible = _schema.Count > 0;
            _includeSchema.Text = $"Include schema ({_schema.Count})";
            UpdateOptionsVisibility();
            if (_schema.Count > 0) RebuildPayload();
        }

        private async Task FindServiceAsync()
        {
            // Started from the UI thread, so the continuation comes back to it: no Invoke, and no
            // handle check either - the tab has usually never been shown at this point, and waiting
            // for a handle would leave the button disabled until it was.
            _service = await DeadlockAnalysisClient.FindServiceAsync();
            if (IsDisposed) return;

            _submit.Enabled = _service is not null;

            _serviceNote = _service is null
                ? "No AI service is configured, so nothing can be submitted."
                : $"Ready to submit to {_service.ServiceUrl}.  Nothing is sent until you do.";

            UpdateStatus();
            UpdateCanAsk();
        }

        /// <summary>
        /// Asks the model.  With no <paramref name="question"/> that starts a new conversation: the reader
        /// has already been shown any previous one about this pattern, so pressing the button means they
        /// want another opinion - and two runs of one model over the same graph do not say the same thing
        /// anyway.  With a question it is the next turn of the conversation on screen, which goes up with
        /// the graph and everything said so far.
        /// </summary>
        private async Task SubmitAsync(string question = null)
        {
            if (_payload is null || _service is null || _inFlight is not null) return;

            // A follow-up with nothing to follow would be a first analysis with a question stuck on the
            // front, which is not what was asked for.
            var continuing = question is null ? null : _current;
            if (question is not null && continuing is null) return;

            _inFlight = new CancellationTokenSource();
            _submit.Enabled = false;
            UpdateCanAsk();
            _analysisNote = question is null ? "Analysing..." : "Answering...";
            _statusColour = DashColors.Information;
            UpdateStatus();

            try
            {
                var graph = _graph;
                var result = await DeadlockAnalysisClient.AnalyseAsync(
                    _payload, _service, _inFlight.Token, _instanceId, continuing, question);

                // The answer is stored either way; it just isn't shown against a deadlock selected while it was coming.
                if (IsDisposed || !ReferenceEquals(graph, _graph)) return;

                if (!result.Success)
                {
                    _analysisNote = result.Error;
                    _statusColour = DashColors.Fail;
                    UpdateStatus();

                    // The question went nowhere, so it comes back rather than being lost with the attempt.
                    if (question is not null) _conversation.RestoreQuestion(question);
                    return;
                }

                // The service mints the conversation id when the caller had none, which is every first
                // analysis; a follow-up gets back the one it sent.
                var conversation = continuing ?? AiConversation.Restore(
                    result.ConversationId ?? Guid.NewGuid(),
                    Array.Empty<AiConversation.Turn>());

                conversation.Add(new AiConversation.Turn(
                    question,
                    result.Analysis ?? string.Empty,
                    result.Model,
                    result.GeneratedUtc ?? DateTime.UtcNow));

                // A follow-up is the reader's own and is kept on their machine rather than in the
                // repository - see AiLocalConversationStore.  The opening analysis is shared and the
                // service has already recorded it.
                //
                // Keyed on the occurrence hash as well as the signature: a signature version change
                // moves the repository's rows and cannot move these, so the hash is what still finds
                // this conversation afterwards.
                var storeNote = question is null
                    ? null
                    : await AiLocalConversationStore.AppendTurnAsync(
                        conversation.Id,
                        AiLocalConversationStore.Artifact.Deadlock,
                        _payload.Signature,
                        _payload.DeadlockHash,
                        _instance,
                        null,
                        _payload.Version,
                        new AiLocalConversationStore.StoredTurn(
                            question,
                            result.Analysis ?? string.Empty,
                            result.Model,
                            result.GeneratedUtc ?? DateTime.UtcNow));

                if (!await ShowConversationAsync(conversation, graph)) return;

                _analysisNote = Describe(result) + (storeNote is null ? string.Empty : "  " + storeNote);
                _statusColour = storeNote is null ? DashColors.Success : DashColors.Warning;
                UpdateStatus();

                // The conversation just stored joins the list, alongside the ones it didn't replace.  Nothing
                // on screen is checked: what is showing is the live conversation, not a stored one.
                _submit.Text = "Analyse again";
                _ = LoadHistoryAsync(showLatest: false);
            }
            finally
            {
                _inFlight?.Dispose();
                _inFlight = null;

                if (!IsDisposed)
                {
                    _submit.Enabled = _service is not null;
                    UpdateCanAsk();

                    // Straight back to the box after a follow-up: the answer usually raises the next one.
                    if (question is not null) _conversation.FocusQuestion();
                }
            }
        }

        /// <summary>Says which model answered, and that the answer is still a reading of the graph.</summary>
        private static string Describe(DeadlockAnalysisClient.Result result) =>
            $"Analysed by {result.Model ?? "the configured model"}.  " +
            "Generated advice - check it against the code before acting on it.";

        /// <summary>Puts a conversation on screen and makes it the one a follow-up continues.</summary>
        /// <returns>False when a different deadlock was selected while it was rendering, so the caller
        /// stops rather than describing this one over the top of that one.</returns>
        private async Task<bool> ShowConversationAsync(AiConversation conversation, DeadlockGraph graph)
        {
            // Set before rendering, so a follow-up asked while it renders continues the right exchange.
            _current = conversation;

            if (!await _conversation.ShowAsync(conversation, () => !IsDisposed && ReferenceEquals(graph, _graph)))
            {
                return false;
            }

            // The request has done its job: the reader has either checked it or chosen not to, and
            // the answer is what they are here for.  One button brings it back.
            _split.Panel2Collapsed = false;
            _showRequest.Visible = true;
            _showRequest.Checked = false;
            ShowRequest(false);
            UpdateOptionsVisibility();
            UpdateCanAsk();

            return true;
        }

        /// <summary>
        /// Whether a follow-up can be asked: there has to be something to follow, a service to ask it of,
        /// and nothing already in flight.
        /// </summary>
        private void UpdateCanAsk()
        {
            if (_service is null)
            {
                _conversation.SetCanAsk(false, "No AI service is configured, so follow-up questions cannot be asked.");
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

        /// <summary>
        /// The Options button is only worth showing when it has something to offer: hide it whenever
        /// none of its items are visible so an empty dropdown isn't presented.
        /// </summary>
        private void UpdateOptionsVisibility() =>
            _options.Visible = _options.DropDownItems.OfType<ToolStripItem>().Any(i => i.Available);

        private void ShowRequest(bool show)
        {
            _split.Panel1Collapsed = !show;
            if (!show) return;

            // A third to the request, the rest to the answer.
            var distance = _split.Height / 3;
            var maximum = _split.Height - _split.Panel2MinSize - _split.SplitterWidth;

            if (distance >= _split.Panel1MinSize && distance <= maximum) _split.SplitterDistance = distance;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inFlight?.Cancel();
            base.Dispose(disposing);
        }
    }
}
