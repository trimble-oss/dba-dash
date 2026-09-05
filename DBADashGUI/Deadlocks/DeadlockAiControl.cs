using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADashGUI.AgentJobs;
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
    /// </summary>
    internal sealed class DeadlockAiControl : UserControl
    {
        private readonly TextBox _preview;
        private readonly WebView2Wrapper _rendered = new() { Dock = DockStyle.Fill, Visible = false };
        private readonly TextBox _analysisText;
        private readonly SplitContainer _split;

        private readonly ToolStripButton _submit;
        private readonly ToolStripDropDownButton _options;
        private readonly ToolStripMenuItem _showRequest;
        private readonly ToolStripLabel _signature = new();
        private readonly ToolStripStatusLabel _status = new();

        private readonly ToolStripMenuItem _includeSchema;

        private DeadlockAnalysisPayload _payload;
        private AIServiceDiscovery.ServiceInfo _service;
        private CancellationTokenSource _inFlight;

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
            _analysisText = NewTextBox();

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
            UpdateOptionsVisibility();

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            toolbar.Items.Add(_submit);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_options);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_signature);

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2Collapsed = true // Nothing to show until an answer comes back
            };
            _split.Panel1.Controls.Add(_preview);
            _split.Panel2.Controls.Add(_rendered);
            _split.Panel2.Controls.Add(_analysisText);

            var statusBar = new StatusStrip();
            statusBar.Items.Add(_status);

            Controls.Add(_split);
            Controls.Add(toolbar);
            Controls.Add(statusBar);
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

            // A new deadlock means the previous answer is about something else.
            _analysisText.Clear();
            _split.Panel1Collapsed = false;
            _split.Panel2Collapsed = true;
            _showRequest.Visible = false;
            _showRequest.Checked = true;
            _analysisNote = null;
            _submit.Text = "Submit for analysis";
            UpdateOptionsVisibility();

            _ = FindServiceAsync();
            _ = FetchSchemaAsync(context);
            _ = ShowPreviousAnalysisAsync(_payload?.Signature);
        }

        /// <summary>
        /// Shows what was found the last time this deadlock pattern was analysed, if it ever was.
        ///
        /// The signature is the point of this: two hundred occurrences of one problem have one
        /// answer, and the reader should not have to know they are looking at a repeat, or pay for
        /// the answer again to find out.  Model and payload version are deliberately ignored here -
        /// an older answer is still worth reading, and asking again is one button away.
        /// </summary>
        private async Task ShowPreviousAnalysisAsync(string signature)
        {
            var graph = _graph;
            var history = await DeadlockAnalysisHistory.FetchAsync(signature);

            // A different deadlock was selected while this was in flight, or the reader has already
            // asked for a fresh answer - either way, do not overwrite what is on screen.
            if (IsDisposed || !ReferenceEquals(graph, _graph) || history.Count == 0) return;
            if (!_split.Panel2Collapsed || _inFlight is not null) return;

            var latest = history[0];
            await ShowAnalysisAsync(latest.Analysis);

            var age = latest.GeneratedUtc.ToLocalTime().ToString("g");
            var older = history.Count > 1 ? $"  {history.Count} analyses of this pattern are stored." : string.Empty;
            var thinner = latest.WithoutSchema && _schema.Count > 0
                ? "  It was produced without the object definitions now available - re-analyse to include them."
                : string.Empty;

            _analysisNote =
                $"Previous analysis of this deadlock pattern, by {latest.Model} on {age}.{thinner}{older}  " +
                "Generated advice - check it against the code before acting on it.";
            _statusColour = DashColors.Information;
            UpdateStatus();

            // With an answer already on screen, the button is asking for another opinion rather than
            // a first one, and should say so.
            _submit.Text = "Analyse again";
            _submit.ToolTipText = "Ask the model again.  Two runs over the same graph rarely say " +
                                  "quite the same thing, and the request may now carry more than it did.";
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
        }

        /// <summary>
        /// Asks the model.  Always asks: the reader has already been shown any previous analysis of
        /// this pattern, so pressing the button means they want another opinion - and two runs of one
        /// model over the same graph do not say the same thing anyway.
        /// </summary>
        private async Task SubmitAsync()
        {
            if (_payload is null || _service is null || _inFlight is not null) return;

            _inFlight = new CancellationTokenSource();
            _submit.Enabled = false;
            _analysisNote = "Analysing...";
            _statusColour = DashColors.Information;
            UpdateStatus();

            try
            {
                var result = await DeadlockAnalysisClient.AnalyseAsync(
                    _payload, _service, _inFlight.Token, _instanceId);
                if (IsDisposed) return;

                if (!result.Success)
                {
                    _analysisNote = result.Error;
                    _statusColour = DashColors.Fail;
                    UpdateStatus();
                    return;
                }

                await ShowAnalysisAsync(result.Analysis ?? string.Empty);
                _analysisNote = Describe(result);
                _statusColour = DashColors.Success;
                UpdateStatus();
            }
            finally
            {
                _inFlight?.Dispose();
                _inFlight = null;

                if (!IsDisposed)
                {
                    _submit.Enabled = _service is not null;
                }
            }
        }

        /// <summary>Says which model answered, and that the answer is still a reading of the graph.</summary>
        private static string Describe(DeadlockAnalysisClient.Result result) =>
            $"Analysed by {result.Model ?? "the configured model"}.  " +
            "Generated advice - check it against the code before acting on it.";

        /// <summary>
        /// Shows the answer, rendered from the Markdown the service asks the model for.  Falls back to
        /// the text as it came when the WebView2 runtime is missing - which is a supported state in
        /// this application, not an error.
        /// </summary>
        private async Task ShowAnalysisAsync(string markdown)
        {
            _analysisText.Text = markdown.Replace("\n", Environment.NewLine);

            var renderedOk = await _rendered.NavigateToLargeString(MarkdownRenderer.ToThemedHtml(markdown));
            if (IsDisposed) return;

            _rendered.Visible = renderedOk;
            _analysisText.Visible = !renderedOk;

            // The request has done its job: the reader has either checked it or chosen not to, and
            // the answer is what they are here for.  One button brings it back.
            _split.Panel2Collapsed = false;
            _showRequest.Visible = true;
            _showRequest.Checked = false;
            ShowRequest(false);
            UpdateOptionsVisibility();
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