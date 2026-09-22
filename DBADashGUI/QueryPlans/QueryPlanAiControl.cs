using DBADash.QueryPlan.Analysis;
using DBADash.QueryPlan.Model;
using DBADash;
using DBADashGUI.AI;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// AI analysis of the statement on screen, on the same principle as the deadlock viewer's: the user
    /// sees exactly what would be sent, and nothing goes until they press the button.
    ///
    /// The preview is not a description of the payload - it is the payload, rendered by the same object
    /// the client serialises, so the two cannot drift.  It is worth reading before pressing send: a plan
    /// carries the statement as it was compiled or ran, which means literal values, and for an actual
    /// plan the parameter values it ran with.
    ///
    /// One statement, not the document.  The tab follows the statement selector, because that is the
    /// statement the reader is looking at and a batch of ten sent as one produces an answer about
    /// whichever the model found most interesting.
    ///
    /// The answer is rarely the end of it.  A plan raises questions an analysis cannot anticipate - would
    /// that index help the other queries on the table, why is the estimate wrong when the statistics are
    /// fresh, what would this look like with a different join order - so the answer comes with a box to
    /// ask the next one, and the whole exchange is stored against the query and shown again next time it
    /// turns up.
    /// </summary>
    internal sealed class QueryPlanAiControl : UserControl
    {
        private readonly TextBox _preview;
        private readonly AiConversationView _conversation = new() { Dock = DockStyle.Fill };
        private readonly SplitContainer _split;

        private readonly ToolStripButton _submit;
        private readonly ToolStripDropDownButton _options;
        private readonly ToolStripMenuItem _showRequest;
        private readonly ToolStripMenuItem _includePlanXml;
        private readonly ToolStripLabel _identity = new();

        /// <summary>Every stored conversation about this plan and this query, to pick from.  Hidden when there are none.</summary>
        private readonly ToolStripDropDownButton _history = new("Previous analyses")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Visible = false,
            ToolTipText = "Conversations about this plan, then about the same query with a different plan, newest first."
        };

        private readonly ToolStripStatusLabel _status = new();

        private PlanAnalysisPayload _payload;
        private AIServiceDiscovery.ServiceInfo _service;
        private CancellationTokenSource _inFlight;

        /// <summary>The exchange on screen, which a follow-up continues.  Null until something is asked or restored.</summary>
        private AiConversation _current;

        /// <summary>
        /// The payload <see cref="_current"/> was given, which is what its follow-ups carry.
        ///
        /// Pinned rather than rebuilt, because the toggle above can change what a payload contains at
        /// any moment.  A follow-up built from the current one would hand the model a plan it had never
        /// been shown - or take away the one it had - while the exchange on screen reads as though
        /// nothing moved.
        /// </summary>
        private PlanAnalysisPayload _conversationPayload;

        /// <summary>
        /// False when what is on screen is a conversation about a different plan for the same query.
        ///
        /// Those are worth showing - the other half of a regression usually is - but not worth adding
        /// to.  A follow-up sends the plan in front of the reader along with the transcript, so
        /// continuing one of these would hand the model this plan and an exchange about another, and
        /// the answer would be about neither.  "Analyse again" starts a conversation about this one.
        /// </summary>
        private bool _currentIsThisPlan;

        // Held so the payload can be rebuilt when the XML is toggled, and so the preview always matches
        // what the toggle currently says.
        private ExecutionPlan _plan;

        private PlanStatement _statement;
        private string _instance;
        private int? _instanceId;
        private string _fileName;

        // The status line is composed from these: an answer note and a service note, each set by its own
        // async lookup, plus the colour the more important of the two deserves.
        private string _analysisNote;

        private string _serviceNote;

        /// <summary>What the size of this request is worth saying, where it is worth saying anything.</summary>
        private string _payloadNote;

        private Color _statusColour = DashColors.Information;

        internal QueryPlanAiControl()
        {
            _preview = NewTextBox();

            // Image and text: the caption is what says whether this is the first run or another one, and
            // sending a plan out of the estate is not something to leave to an icon alone.
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

            _includePlanXml = new ToolStripMenuItem("Include plan XML", null, (_, _) => RebuildPayload())
            {
                CheckOnClick = true,
                Checked = true,
                ToolTipText = "Send the showplan XML as well as the summary.  It makes the analysis better and " +
                              "the request much larger, and it carries the statement's literal values.  It applies " +
                              "to the next analysis: a follow-up keeps the plan its conversation started with."
            };

            _options.DropDownItems.Add(_showRequest);
            _options.DropDownItems.Add(_includePlanXml);

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

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            toolbar.Items.Add(_submit);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_options);
            toolbar.Items.Add(_history);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_identity);

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
        /// Points the tab at a statement and shows what would be sent about it.  Deliberately does not
        /// contact anything: the tab can be opened, read and closed again without a byte leaving the
        /// machine.
        ///
        /// Called again whenever the reader picks a different statement, which makes it a different
        /// question - so anything on screen about the last one goes.
        /// </summary>
        internal void Show(ExecutionPlan plan, PlanStatement statement, string fileName, DBADashContext context)
        {
            _plan = plan;
            _statement = statement;
            _fileName = fileName;
            _instance = context?.InstanceName;
            _instanceId = context is { InstanceID: > 0 } ? context.InstanceID : null;

            // The XML goes by default - but not when it is large.  A plan of several megabytes is a real
            // thing to be looking at, and sending one is worth trying; it is just not what should happen
            // to somebody who pressed a button without reading.  So the expensive version is the one
            // they ask for, and the choice they make then holds until they pick another statement.
            _includePlanXml.Checked =
                (statement?.Xml?.Length ?? 0) <= PlanAnalysisPayload.LargePlanXmlLength;

            RebuildPayload();

            // A different statement means the previous conversation is about something else.
            _current = null;
            _conversationPayload = null;
            _currentIsThisPlan = true;
            _conversation.Clear();
            _split.Panel1Collapsed = false;
            _split.Panel2Collapsed = true;
            _showRequest.Visible = false;
            _showRequest.Checked = true;
            _analysisNote = null;
            _statusColour = DashColors.Information;
            _submit.Text = "Submit for analysis";
            _history.DropDownItems.Clear();
            _history.Visible = false;

            // Found once per control rather than per statement: the service does not change while a plan
            // is open, and probing it again on every click of the statement list would be rude.
            if (_service is null && _serviceNote is null) _ = FindServiceAsync();
            UpdateStatus();
            UpdateCanAsk();

            _ = LoadHistoryAsync(showLatest: true);
        }

        /// <summary>Rebuilds the payload and the preview from what is currently switched on.</summary>
        private void RebuildPayload()
        {
            if (_plan is null || _statement is null) return;

            _payload = PlanAnalysisPayload.Build(_plan, _statement, _instance, _fileName, _includePlanXml.Checked);

            _preview.Text = _payload.ToPreview().Replace("\n", Environment.NewLine);
            _identity.Text = $"Query {_payload.Signature}  Plan {_payload.PlanHash}";

            // The toggle carries the size, because the size is the whole of the decision it is asking
            // the reader to make - and in tokens as well as bytes, because tokens are what a model
            // refuses on.  A plan past anything says so; one that is merely large says how large and
            // stays theirs to send.
            var size = PlanAnalysisPayload.DescribeSize(_payload.PlanXmlLength);

            if (_payload.PlanXmlLength == 0)
            {
                _includePlanXml.Text = "Include plan XML";
            }
            else if (_includePlanXml.Checked && !_payload.PlanXmlIncluded)
            {
                _includePlanXml.Text = $"Include plan XML (too large to send: {size})";
            }
            else if (_payload.PlanXmlIsLarge)
            {
                _includePlanXml.Text = $"Include plan XML ({size})";
            }
            else
            {
                _includePlanXml.Text = "Include plan XML";
            }

            // The short version of it goes on the status line too.  The preview says the whole thing,
            // but it says it near the bottom of a long document, and this is the one part of it worth
            // having read before pressing Submit.
            _payloadNote = !_payload.PlanXmlIsLarge
                ? null
                : _payload.PlanXmlIncluded
                    ? $"Large plan: {size} of XML goes with this - the model may refuse it."
                    : $"The plan XML is {size} and is not being sent.";

            UpdateStatus();
        }

        /// <summary>
        /// Fills the Previous analyses drop-down, and when <paramref name="showLatest"/> shows the first
        /// entry - the newest conversation about this exact plan, or failing that the newest about the
        /// same query with a different plan.
        ///
        /// The fallback is worth having and worth labelling.  The same query with a different plan is
        /// often exactly what the reader needs to see - it is the other half of a regression - but an
        /// answer about a plan that is not the one on screen would be actively misleading if it were
        /// presented as one, so the status line says which it is.
        /// </summary>
        private async Task LoadHistoryAsync(bool showLatest)
        {
            var statement = _statement;
            var history = await PlanAnalysisHistory.FetchAsync(_payload?.Signature, _payload?.PlanHash);

            // A different statement was selected while this was in flight.
            if (IsDisposed || !ReferenceEquals(statement, _statement)) return;

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
        private async Task ShowEntryAsync(PlanAnalysisHistory.Entry entry)
        {
            if (_inFlight is not null) return;

            var statement = _statement;

            // A conversation stored before follow-ups existed has no id of its own.  Continuing it mints
            // one, so the answers added from here are stored as the exchange they are.
            var conversation = AiConversation.Restore(
                entry.ConversationId ?? Guid.NewGuid(),
                entry.Turns.Select(t => new AiConversation.Turn(t.Question, t.Analysis, t.Model, t.GeneratedUtc)));

            // The plan is not stored with its answer, so what a follow-up to this can carry is the
            // payload as it stands now.  Pinned at the moment it is restored for the same reason a
            // fresh one is: so the toggle cannot change it out from under the exchange.
            _conversationPayload = _payload;

            if (!await ShowConversationAsync(conversation, statement, entry.IsThisPlan)) return;

            foreach (var item in _history.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = ReferenceEquals(item.Tag, entry);
            }

            var age = entry.GeneratedUtc.ToLocalTime().ToString("g");
            var others = _history.DropDownItems.Count > 1
                ? $"  {_history.DropDownItems.Count} conversations are stored - see Previous analyses."
                : string.Empty;
            var thinner = entry.WithoutPlanXml
                ? "  It was produced from the summary alone, without the plan XML."
                : string.Empty;

            _analysisNote =
                $"Previous analysis of {entry.Scope}, by {entry.Model} on {age}.{thinner}{others}  " +
                "Generated advice - check it against the plan before acting on it.";
            _statusColour = entry.IsThisPlan ? DashColors.Information : DashColors.Warning;
            UpdateStatus();

            _submit.Text = "Analyse again";
            _submit.ToolTipText = "Start a new conversation about this plan.  Every answer is kept, and two runs over " +
                                  "the same plan rarely say quite the same thing.  To build on the analysis shown, ask " +
                                  "a follow-up instead.";
        }

        /// <summary>
        /// Composes the status line from the two things that arrive independently: what is on screen (a
        /// fresh answer, a stored one, or nothing yet) and whether a service is available to ask.  Both
        /// are found asynchronously, so whichever lands second must not erase the other.
        /// </summary>
        private void UpdateStatus()
        {
            var parts = new[] { _analysisNote, _serviceNote, _payloadNote }.Where(p => !string.IsNullOrEmpty(p));

            _status.Text = string.Join("  ", parts);

            // The size note only colours the line while nothing more important is using it: an answer
            // that came back, or one that did not, is the more useful thing to be looking at by then.
            _status.ForeColor = _payloadNote is not null && _statusColour == DashColors.Information
                ? DashColors.Warning
                : _statusColour;
        }

        private async Task FindServiceAsync()
        {
            // Started from the UI thread, so the continuation comes back to it: no Invoke, and no handle
            // check either - the tab has usually never been shown at this point, and waiting for a handle
            // would leave the button disabled until it was.
            _service = await PlanAnalysisClient.FindServiceAsync();
            if (IsDisposed) return;

            _submit.Enabled = _service is not null && _payload is not null;

            _serviceNote = _service is null
                ? "No AI service is configured, so nothing can be submitted."
                : $"Ready to submit to {_service.ServiceUrl}.  Nothing is sent until you do.";

            UpdateStatus();
            UpdateCanAsk();
        }

        /// <summary>
        /// Asks the model.  With no <paramref name="question"/> that starts a new conversation; with one it
        /// is the next turn of the conversation on screen, which goes up with the plan and everything said
        /// so far.
        /// </summary>
        private async Task SubmitAsync(string question = null)
        {
            if (_payload is null || _service is null || _inFlight is not null) return;

            // A follow-up with nothing to follow would be a first analysis with a question stuck on the
            // front, which is not what was asked for - and one that follows a conversation about another
            // plan would send this plan with that plan's transcript.  The ask box is disabled in both
            // cases; this is the belt to that braces.
            var continuing = question is null || !_currentIsThisPlan ? null : _current;
            if (question is not null && continuing is null) return;

            // What this conversation is about was settled when it started.  A first analysis sends what
            // is switched on now; a follow-up sends what the answer above it was given.
            var payload = continuing is null ? _payload : _conversationPayload ?? _payload;

            _inFlight = new CancellationTokenSource();
            _submit.Enabled = false;
            UpdateCanAsk();
            _analysisNote = question is null ? "Analysing..." : "Answering...";
            _statusColour = DashColors.Information;
            UpdateStatus();

            try
            {
                var statement = _statement;
                var result = await PlanAnalysisClient.AnalyseAsync(
                    payload, _service, _inFlight.Token, _instanceId, continuing, question);

                // The answer is stored either way; it just isn't shown against a statement selected while
                // it was coming.
                if (IsDisposed || !ReferenceEquals(statement, _statement)) return;

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

                // Held from here on, so that toggling the plan XML changes the next analysis rather
                // than the artifact this exchange is about.
                _conversationPayload = payload;

                // A follow-up is the reader's own and is kept on their machine rather than in the
                // repository - see AiLocalConversationStore.  The opening analysis is shared and the
                // service has already recorded it.
                var storeNote = question is null
                    ? null
                    : await AiLocalConversationStore.AppendTurnAsync(
                        conversation.Id,
                        AiLocalConversationStore.Artifact.QueryPlan,
                        payload.Signature,
                        payload.PlanHash,
                        _instance,
                        payload.StatementText,
                        payload.Version,
                        new AiLocalConversationStore.StoredTurn(
                            question,
                            result.Analysis ?? string.Empty,
                            result.Model,
                            result.GeneratedUtc ?? DateTime.UtcNow));

                if (!await ShowConversationAsync(conversation, statement)) return;

                _analysisNote = $"Analysed by {result.Model ?? "the configured model"}.  " +
                                "Generated advice - check it against the plan before acting on it." +
                                (storeNote is null ? string.Empty : "  " + storeNote);
                _statusColour = storeNote is null ? DashColors.Success : DashColors.Warning;
                UpdateStatus();

                // The conversation just stored joins the list, alongside the ones it didn't replace.
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

        /// <summary>Puts a conversation on screen and makes it the one a follow-up continues.</summary>
        /// <returns>False when a different statement was selected while it was rendering, so the caller
        /// stops rather than describing this one over the top of that one.</returns>
        private async Task<bool> ShowConversationAsync(AiConversation conversation, PlanStatement statement, bool isThisPlan = true)
        {
            // Set before rendering, so a follow-up asked while it renders continues the right exchange.
            _current = conversation;
            _currentIsThisPlan = isThisPlan;

            if (!await _conversation.ShowAsync(conversation, () => !IsDisposed && ReferenceEquals(statement, _statement)))
            {
                return false;
            }

            // The request has done its job: the reader has either checked it or chosen not to, and the
            // answer is what they are here for.  One button brings it back.
            _split.Panel2Collapsed = false;
            _showRequest.Visible = true;
            _showRequest.Checked = false;
            ShowRequest(false);
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
            else if (_current is { IsEmpty: false } && !_currentIsThisPlan)
            {
                _conversation.SetCanAsk(false,
                    "This conversation is about a different plan for the same query, so it cannot be added to.  " +
                    "Submit for analysis to start one about the plan on screen.");
            }
            else
            {
                _conversation.SetCanAsk(_current is { IsEmpty: false });
            }
        }

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
