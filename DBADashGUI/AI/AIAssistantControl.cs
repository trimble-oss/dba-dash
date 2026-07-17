#nullable enable
using DBADashGUI.AgentJobs;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using Markdig;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    public class AIAssistantControl : UserControl
    {
    /// <summary>
    /// Indicates whether the AI service is available for the current repository.
    /// Used by Main form to determine if the AI Assistant tab should be shown.
    /// </summary>
    public bool IsServiceAvailable { get; private set; }

    private readonly ComboBox cboCategory = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        IntegralHeight = false,
        MaxDropDownItems = 16,
        DropDownHeight = 320,
        Margin = new Padding(0, 6, 0, 4)
    };
    private readonly ComboBox cboExamples = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        IntegralHeight = false,
        MaxDropDownItems = 16,
        DropDownHeight = 320,
        Margin = new Padding(0, 6, 0, 4)
    };
    private readonly List<(string Category, string Question)> _allExamples = new();
    private const string ExamplesPlaceholder = "Please select an example question";
    private readonly Label lblExamplesHint = new()
    {
        Dock = DockStyle.Fill,
        Text = "Pick a category and a sample question above or enter your own text in the box below.",
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        AutoSize = false,
        Padding = new Padding(0, 2, 0, 6)
    };
    private readonly TextBox txtQuestion = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox cboModel = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        IntegralHeight = false,
        MaxDropDownItems = 10,
        Margin = new Padding(0, 6, 0, 4)
    };
    private readonly Label lblModel = new()
    {
        Text = "Model",
        AutoSize = true,
        Dock = DockStyle.Left,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 0, 4, 0)
    };
    private readonly Button btnAsk = new() { Text = "Ask", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(0, 36) };
    private readonly TextBox txtSummary = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
    private readonly TextBox txtJson = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, WordWrap = false };
    private readonly WebView2Wrapper webViewSummary = new() { Dock = DockStyle.Fill };
    private readonly TabControl tabSummary = new() { Dock = DockStyle.Fill };
    private readonly TabPage tabRenderedSummary = new("Rendered");
    private readonly TabPage tabRawSummary = new("Raw");

    // Feedback footer
    private readonly Button btnThumbsUp = new() { Text = "\uD83D\uDC4D", Width = 36, Height = 28, Enabled = false };
    private readonly Button btnThumbsDown = new() { Text = "\uD83D\uDC4E", Width = 36, Height = 28, Enabled = false };
    private readonly ToolTip _toolTip = new();
    private readonly Label lblConfidence = new() { AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, Visible = false };

    // Tracks the most recent response so feedback can reference it
    private string? _lastRequestId;
    private string? _lastToolName;
    private string? _lastQuestionExcerpt;

    private readonly GroupBox jsonGroup = new() { Text = "Tool Output (JSON)", Dock = DockStyle.Fill };
    private readonly TableLayoutPanel root = new() { Dock = DockStyle.Fill };
    private readonly Label lblStatus = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        Visible = false,
        AutoSize = true,
        Padding = new Padding(4, 2, 4, 2)
    };
    private readonly ProgressBar progressRunning = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 40, Visible = false };

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(120) };

    private bool _seedingExamples = false;
    private string? _serviceUrl = null; // Resolved by AIServiceDiscovery, not hardcoded
    private AIServiceDiscovery.ServiceSource _serviceSource = AIServiceDiscovery.ServiceSource.None;
    private string? _provider = null;
    // One-shot timer: retries InitializeAsync when the API key was unavailable at startup
    // (e.g. GUI connected before the service finished its registration delay).
    private readonly System.Windows.Forms.Timer _initRetryTimer = new() { Interval = 10_000 };
    private readonly string? _configuredToolName = Normalize(ConfigurationManager.AppSettings["AIToolName"]);
    private readonly int _configuredMaxRows = ParseInt(ConfigurationManager.AppSettings["AIMaxRows"], 50, 1, 500);
    private readonly bool _configuredIncludeSummary = ParseBool(ConfigurationManager.AppSettings["AIIncludeSummary"], true);
    private readonly bool _showJson = ParseBool(ConfigurationManager.AppSettings["AIShowJson"], false);
    private readonly SynchronizationContext? _uiSynchronizationContext;

    public AIAssistantControl()
    {
        _uiSynchronizationContext = SynchronizationContext.Current;
        BuildLayout();
        this.ApplyTheme();
        WireEvents();
        ApplyConfigVisibility();
        // Don't initialize yet - wait for repository connection
    }

    /// <summary>
    /// Call this when the repository connection changes to discover the AI service for that repository.
    /// </summary>
    public async Task RefreshConnectionAsync()
    {
        var serviceInfo = await AIServiceDiscovery.GetServiceInfoAsync().ConfigureAwait(false);
        if (IsDisposed) return;

        await RunOnUiThreadAsync(() => ApplyServiceInfoAsync(serviceInfo)).ConfigureAwait(false);
    }

    private Task RunOnUiThreadAsync(Func<Task> action)
    {
        if (IsDisposed)
        {
            return Task.CompletedTask;
        }

        if (SynchronizationContext.Current == _uiSynchronizationContext && _uiSynchronizationContext != null)
        {
            return action();
        }

        if (_uiSynchronizationContext != null)
        {
            var tcs = new TaskCompletionSource<object?>();
            _uiSynchronizationContext.Post(async _ =>
            {
                if (IsDisposed)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                try
                {
                    await action().ConfigureAwait(true);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        if (IsHandleCreated)
        {
            return InvokeAsync(action);
        }

        return action();
    }

    private async Task ApplyServiceInfoAsync(AIServiceDiscovery.ServiceInfo? serviceInfo)
    {
        try
        {
            // Clear previous state
            _allExamples.Clear();
            cboCategory.Items.Clear();
            cboModel.Items.Clear();
            _provider = null;
            lblStatus.Visible = false;
            btnAsk.Enabled = true;
            IsServiceAvailable = false;
            _serviceUrl = null;
            _serviceSource = AIServiceDiscovery.ServiceSource.None;
            UpdateModelSelectorVisibility();

            // Invalidate cached key so the new source's key is used
            AIApiKeyProvider.InvalidateCache();

            if (serviceInfo == null)
            {
                ShowNoServiceMessage();
                IsServiceAvailable = false;
                return;
            }

            if (!serviceInfo.IsEnabled)
            {
                ShowServiceDisabledMessage();
                IsServiceAvailable = false;
                return;
            }

            _serviceUrl = serviceInfo.ServiceUrl;
            _serviceSource = serviceInfo.Source;

            // For repository-sourced services
            // permission on AI.ServiceConfig — meaning they received the real API key.
            // Without it, the masked placeholder 'xxxx' would be sent and all requests
            // would be rejected with 401. Show the tab but disable Ask and explain why.
            if (serviceInfo.Source == AIServiceDiscovery.ServiceSource.Repository && !serviceInfo.HasApiKeyAccess)
            {
                ShowNoApiKeyAccessMessage();
                IsServiceAvailable = true; // Tab is visible so the user knows to request access
                return;
            }

            // Seed API key from discovery result so AIApiKeyProvider doesn't need a separate DB call.
            // Only seed if we actually have a key - seeding null would cause the cache to serve null
            // and suppress the fallback DB lookup in GetApiKeyAsync.
            if (serviceInfo.ApiKey != null)
            {
                AIApiKeyProvider.SeedFromServiceInfo(serviceInfo);
            }
            else if (serviceInfo.Source != AIServiceDiscovery.ServiceSource.Local)
            {
                // Repository mode expects a key. A null key here means the service hasn't
                // finished writing its registration to the DB yet - show the tab so it's
                // visible, but schedule a retry to pick up the key once it's registered.
                IsServiceAvailable = true;
                ShowServiceStartingMessage();
                _initRetryTimer.Start();
                return;
            }
            // Local mode is unauthenticated by design (the loopback binding is the security
            // boundary, so the service never registers a key in the DB). A null key is the
            // expected, permanent state - fall through and use the service directly.

            if (!serviceInfo.IsActive)
            {
                IsServiceAvailable = false;
                return;
            }

            // Service is available - show tab
            IsServiceAvailable = true;

            // Load examples and models - failures here are non-fatal; the tab
            // is still usable without them.
            try
            {
                await LoadProviderAsync();
                await LoadExampleQuestionsAsync();
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeAsync: examples/models load failed: {ex}");
            }

            // The repository fingerprint is derived from SQL Server's own canonical
            // identity (SERVERPROPERTY('ServerName') + DB_NAME()), so server-name aliases
            // (localhost vs machine name, FQDN, IP, named port) do NOT cause false
            // mismatches - a mismatch means the service really is pointed at a different
            // server/database. It is surfaced as a non-blocking warning rather than hiding
            // the tab, so the user is informed but can still use the service (issue #2000).
            if (serviceInfo.RepositoryMismatch)
            {
                ShowStatusWarning("⚠ The local AI service may be connected to a different repository than the one you are viewing. " +
                                  "If AI answers don't match this repository, check the AI service's repository connection.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeAsync failed: {ex}");
            IsServiceAvailable = false;
            btnAsk.Enabled = true;
        }
    }

    private void UpdateModelSelectorVisibility()
    {
        var showModelSelector = string.Equals(_provider, "Anthropic", StringComparison.OrdinalIgnoreCase);
        cboModel.Visible = showModelSelector;
        lblModel.Visible = showModelSelector;
    }

    private async Task LoadProviderAsync()
    {
        try
        {
            var apiKey = await AIApiKeyProvider.GetApiKeyAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl("/api/ai/diagnostics"));
            AddApiKeyHeader(request, apiKey);

            using var response = await HttpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                AIApiKeyProvider.InvalidateCache();
                apiKey = await AIApiKeyProvider.GetApiKeyAsync();

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, BuildUrl("/api/ai/diagnostics"));
                    AddApiKeyHeader(retryRequest, apiKey);
                    using var retryResponse = await HttpClient.SendAsync(retryRequest);

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        await ParseProviderFromResponse(retryResponse);
                    }
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                await ParseProviderFromResponse(response);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load AI provider: {ex.Message}");
        }

        UpdateModelSelectorVisibility();
    }

    private async Task ParseProviderFromResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        _provider = doc.RootElement.TryGetProperty("provider", out var providerElement)
            ? providerElement.GetString()
            : null;
    }

    private Task InvokeAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        BeginInvoke(new Action(async () =>
        {
            try
            {
                await action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }));
        return tcs.Task;
    }
        private void ShowNoServiceMessage()
    {
        cboCategory.Items.Clear();
        cboCategory.Items.Add("(AI service not configured - set up a shared service in the repository or configure a local service in Options)");
        cboCategory.SelectedIndex = 0;
    }

    private void ShowServiceDisabledMessage()
    {
        cboCategory.Items.Clear();
        cboCategory.Items.Add("(AI service disabled for this repository)");
        cboCategory.SelectedIndex = 0;
        btnAsk.Enabled = false;
    }

    private void ShowServiceStartingMessage()
    {
        cboCategory.Items.Clear();
        cboCategory.Items.Add("(AI service is starting up...)");
        cboCategory.SelectedIndex = 0;
        lblStatus.Text = "⏳ Waiting for AI service to complete registration. Retrying shortly...";
        lblStatus.Visible = true;
        lblStatus.ForeColor = System.Drawing.Color.SteelBlue;
        btnAsk.Enabled = false;
    }

    private void ShowStatusWarning(string message)
    {
        lblStatus.Text = message;
        lblStatus.Visible = true;
        lblStatus.ForeColor = System.Drawing.Color.OrangeRed;
    }

    private void ShowNoApiKeyAccessMessage()
    {
        cboCategory.Items.Clear();
        cboCategory.Items.Add("(AI service access restricted)");
        cboCategory.SelectedIndex = 0;
        lblStatus.Text = "⚠ You do not have permission to use the AI service. Ask your DBA to grant you the AIUser role in the repository database.";
        lblStatus.Visible = true;
        lblStatus.ForeColor = System.Drawing.Color.OrangeRed;
        btnAsk.Enabled = false;
    }

    private void ShowServiceInactiveMessage(AIServiceDiscovery.ServiceInfo serviceInfo)
    {
        var lastSeen = serviceInfo.LastHeartbeat.HasValue
            ? $"Last seen: {serviceInfo.LastHeartbeat.Value:g}"
            : "Never active";

        var sourceLabel = serviceInfo.Source == AIServiceDiscovery.ServiceSource.Local ? "Local" : "Shared";
        lblStatus.Text = $"⚠ AI service ({sourceLabel}) is not responding. {lastSeen}";
        lblStatus.Visible = true;
        lblStatus.ForeColor = System.Drawing.Color.Orange;
    }

    private void BuildLayout()
    {
        // ── Quick Start section ────────────────────────────────────────────
        var examplesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8, 18, 8, 4)
        };
        examplesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        examplesLayout.Controls.Add(new Label { Text = "Category", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 0, 0) }, 0, 0);
        examplesLayout.Controls.Add(cboCategory, 0, 1);
        examplesLayout.Controls.Add(new Label { Text = "Example Question", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(0, 4, 0, 0) }, 0, 2);
        examplesLayout.Controls.Add(cboExamples, 0, 3);
        examplesLayout.Controls.Add(lblExamplesHint, 0, 4);

        var examplesGroup = new GroupBox { Text = "Quick Start", Dock = DockStyle.Fill };
        examplesGroup.Controls.Add(examplesLayout);

        // ── Question section ───────────────────────────────────────────────
        var questionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 4, 0, 0)
        };
        questionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        questionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        questionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        questionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        questionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var questionLabel = new Label
        {
            Text = "Question",
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 4, 0, 4)
        };

        // Model label + combo in a small panel, placed top-right alongside the Question label
        var modelHeaderPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0)
        };
        modelHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        modelHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        modelHeaderPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        modelHeaderPanel.Controls.Add(lblModel, 0, 0);
        modelHeaderPanel.Controls.Add(cboModel, 1, 0);

        questionPanel.Controls.Add(questionLabel, 0, 0);
        questionPanel.Controls.Add(modelHeaderPanel, 1, 0);
        questionPanel.Controls.Add(txtQuestion, 0, 1);
        questionPanel.Controls.Add(btnAsk, 1, 1);
        questionPanel.Controls.Add(lblStatus, 0, 2);
        questionPanel.Controls.Add(progressRunning, 1, 2);

        // ── AI Summary section ─────────────────────────────────────────────
        tabRenderedSummary.Controls.Add(webViewSummary);
        tabRawSummary.Controls.Add(txtSummary);
        tabSummary.TabPages.Add(tabRenderedSummary);
        tabSummary.TabPages.Add(tabRawSummary);

        // Feedback footer: confidence label on the left, thumbs buttons on the right
        var feedbackPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            ColumnCount = 3,
            RowCount = 1,
            AutoSize = true,
            Padding = new Padding(4, 2, 4, 2)
        };
        feedbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        feedbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        feedbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        feedbackPanel.Controls.Add(lblConfidence, 0, 0);
        feedbackPanel.Controls.Add(btnThumbsUp, 1, 0);
        feedbackPanel.Controls.Add(btnThumbsDown, 2, 0);

        var summaryGroup = new GroupBox { Text = "AI Summary", Dock = DockStyle.Fill };
        summaryGroup.Controls.Add(tabSummary);
        summaryGroup.Controls.Add(feedbackPanel);

        jsonGroup.Controls.Add(txtJson);

        // ── Nest two SplitContainers for 3 resizable sections ──────────────
        // Inner split: Question (top) | AI Summary (bottom)
        var innerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            FixedPanel = FixedPanel.None
        };
        innerSplit.Panel1.Controls.Add(questionPanel);
        innerSplit.Panel2.Controls.Add(summaryGroup);
        if (_showJson) innerSplit.Panel2.Controls.Add(jsonGroup);

        // Outer split: Quick Start (top) | Inner split (bottom)
        var outerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            FixedPanel = FixedPanel.None
        };
        outerSplit.Panel1.Controls.Add(examplesGroup);
        outerSplit.Panel2.Controls.Add(innerSplit);

        void AdjustSplitLayout()
        {
            try
            {
                var quickStartPreferredHeight = examplesGroup.GetPreferredSize(new Size(Math.Max(outerSplit.ClientSize.Width - 16, 0), 0)).Height + 12;
                var quickStartHeight = Math.Min(220, Math.Max(120, quickStartPreferredHeight));
                SetSplitterLayout(outerSplit, quickStartHeight, minPanel1: 96, minPanel2: 120);

                var questionHeight = Math.Min(160, Math.Max(96, innerSplit.ClientSize.Height / 3));
                SetSplitterLayout(innerSplit, questionHeight, minPanel1: 96, minPanel2: 120);
            }
            catch { }
        }

        outerSplit.HandleCreated += (_, _) => AdjustSplitLayout();
        innerSplit.HandleCreated += (_, _) => AdjustSplitLayout();
        outerSplit.SizeChanged += (_, _) => AdjustSplitLayout();
        innerSplit.SizeChanged += (_, _) => AdjustSplitLayout();

        Controls.Add(outerSplit);
    }

    private static void SetSplitterLayout(SplitContainer splitContainer, int desiredPanel1Size, int minPanel1, int minPanel2)
    {
        var available = splitContainer.Orientation == Orientation.Horizontal
            ? splitContainer.ClientSize.Height - splitContainer.SplitterWidth
            : splitContainer.ClientSize.Width - splitContainer.SplitterWidth;

        if (available <= 0)
        {
            return;
        }

        splitContainer.Panel1MinSize = Math.Min(minPanel1, available);
        splitContainer.Panel2MinSize = Math.Min(minPanel2, Math.Max(0, available - splitContainer.Panel1MinSize));

        var maxPanel1Size = Math.Max(0, available - splitContainer.Panel2MinSize);
        var minPanel1Size = Math.Min(splitContainer.Panel1MinSize, maxPanel1Size);
        splitContainer.SplitterDistance = Math.Clamp(desiredPanel1Size, minPanel1Size, maxPanel1Size);
    }

    private async Task LoadExampleQuestionsAsync()
    {
        _seedingExamples = true;

        try
        {
            // In local unauthenticated mode there is no API key (by design) - send the
            // request anyway. AddApiKeyHeader omits the header when the key is empty, and
            // the 401 path below handles the authenticated case by fetching/retrying a key.
            var apiKey = await AIApiKeyProvider.GetApiKeyAsync();

            var url = BuildUrl("/api/ai/examples");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddApiKeyHeader(request, apiKey);

            using var response = await HttpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                AIApiKeyProvider.InvalidateCache();
                apiKey = await AIApiKeyProvider.GetApiKeyAsync();

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    AddApiKeyHeader(retryRequest, apiKey);
                    using var retryResponse = await HttpClient.SendAsync(retryRequest);

                    if (retryResponse.IsSuccessStatusCode)
                        await ParseExamplesFromResponse(retryResponse);
                    else
                        ShowStatusWarning($"⚠ Could not load examples: {retryResponse.StatusCode}");
                }
                else
                {
                    ShowStatusWarning("⚠ Authentication failed and no API key available for retry.");
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                await ParseExamplesFromResponse(response);
            }
            else
            {
                ShowStatusWarning($"⚠ Could not load examples: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            ShowStatusWarning($"⚠ Could not load examples: {ex.Message}");
        }

        if (_allExamples.Count == 0)
        {
            cboCategory.Items.Add("(No examples available)");
            cboCategory.SelectedIndex = 0;
            _seedingExamples = false;
            System.Diagnostics.Debug.WriteLine($"LoadExampleQuestionsAsync: No examples loaded. Total count: {_allExamples.Count}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"LoadExampleQuestionsAsync: Loaded {_allExamples.Count} examples");

        _allExamples.Sort((a, b) =>
        {
            var c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Question, b.Question, StringComparison.OrdinalIgnoreCase);
        });

        var categories = _allExamples.Select(e => e.Category).Distinct().OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        cboCategory.Items.Add("All Categories");
        foreach (var cat in categories)
            cboCategory.Items.Add(cat);
        cboCategory.SelectedIndex = 0;

        FilterExamplesByCategory();
        _seedingExamples = false;
    }

    private async Task ParseExamplesFromResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            var category = group.GetProperty("category").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(category)) continue;

            foreach (var q in group.GetProperty("questions").EnumerateArray())
            {
                var question = q.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(question))
                {
                    _allExamples.Add((category, question));
                }
            }
        }
    }

    private async Task LoadModelsAsync()
    {
        if (!string.Equals(_provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            cboModel.Items.Clear();
            return;
        }

        try
        {
            var apiKey = await AIApiKeyProvider.GetApiKeyAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl("/api/ai/models"));
            AddApiKeyHeader(request, apiKey);

            using var response = await HttpClient.SendAsync(request);

            // If 401, try refreshing the key once
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                AIApiKeyProvider.InvalidateCache();
                apiKey = await AIApiKeyProvider.GetApiKeyAsync();

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, BuildUrl("/api/ai/models"));
                    AddApiKeyHeader(retryRequest, apiKey);
                    using var retryResponse = await HttpClient.SendAsync(retryRequest);

                    if (retryResponse.IsSuccessStatusCode)
                    {
                        await ParseModelsFromResponse(retryResponse);
                    }
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                await ParseModelsFromResponse(response);
            }
        }
        catch (Exception ex)
        {
            // API unavailable
            System.Diagnostics.Debug.WriteLine($"Failed to load models: {ex.Message}");
        }

        cboModel.Items.Insert(0, new ModelItem("", "(Default - configured model)"));

        cboModel.SelectedIndex = 0;
    }

    private async Task ParseModelsFromResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var modelName = item.GetProperty("modelName").GetString() ?? string.Empty;
            var displayName = item.GetProperty("displayName").GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                cboModel.Items.Add(new ModelItem(modelName, displayName));
            }
        }
    }

    private sealed class ModelItem(string modelName, string displayName)
    {
        public string ModelName { get; } = modelName;

        public override string ToString() => displayName;
    }

    private void FilterExamplesByCategory()
    {
        _seedingExamples = true;
        cboExamples.Items.Clear();
        cboExamples.Items.Add(ExamplesPlaceholder);

        var selectedCategory = cboCategory.SelectedItem?.ToString();
        var isAll = selectedCategory == "All Categories" || string.IsNullOrEmpty(selectedCategory);

        var filtered = isAll
            ? _allExamples
            : _allExamples.Where(e => e.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var (_, question) in filtered)
            cboExamples.Items.Add(question);

        cboExamples.SelectedIndex = 0;
        _seedingExamples = false;
    }

    private void WireEvents()
    {
        btnAsk.Click += async (_, _) => await AskAsync();
        btnThumbsUp.Click += async (_, _) => await SendFeedbackAsync(helpful: true);
        btnThumbsDown.Click += async (_, _) => await SendFeedbackAsync(helpful: false);
        _initRetryTimer.Tick += async (_, _) =>
        {
            _initRetryTimer.Stop();
            await RefreshConnectionAsync();
        };
        _toolTip.SetToolTip(btnThumbsUp, "This response was helpful");
        _toolTip.SetToolTip(btnThumbsDown, "This response was not helpful");
        cboCategory.SelectedIndexChanged += (_, _) =>
        {
            if (_seedingExamples) return;
            FilterExamplesByCategory();
        };
        cboExamples.SelectedIndexChanged += (_, _) =>
        {
            if (_seedingExamples || cboExamples.SelectedIndex < 0) return;
            var selected = cboExamples.SelectedItem?.ToString();
            if (selected == ExamplesPlaceholder) return;
            txtQuestion.Text = selected ?? string.Empty;
            txtQuestion.SelectionStart = txtQuestion.TextLength;
            txtQuestion.Focus();
        };
    }


    private void ApplyConfigVisibility()
    {
        jsonGroup.Visible = _showJson;
    }

    private async Task AskAsync()
    {
        if (string.IsNullOrWhiteSpace(txtQuestion.Text))
        {
            MessageBox.Show("Enter a question.", "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            btnAsk.Enabled = false;
            SetBusy(true, "Running AI analysis...");
            txtSummary.Text = string.Empty;
            txtJson.Text = string.Empty;
            lblConfidence.Visible = false;
            btnThumbsUp.Enabled = false;
            btnThumbsDown.Enabled = false;
            _lastRequestId = null;
            _lastToolName = null;
            _lastQuestionExcerpt = null;

            // Get API key from repository database
            var apiKey = await AIApiKeyProvider.GetApiKeyAsync();

            var selectedModel = (cboModel.SelectedItem as ModelItem)?.ModelName;
            var payload = new
            {
                question = txtQuestion.Text,
                toolName = _configuredToolName,
                includeAiSummary = _configuredIncludeSummary,
                maxRows = _configuredMaxRows,
                modelOverride = string.IsNullOrWhiteSpace(selectedModel) ? (string?)null : selectedModel
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/ai/ask"));
            request.Content = JsonContent.Create(payload);

            // Add API key header if available
            AddApiKeyHeader(request, apiKey);

            using var response = await HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // If 401 Unauthorized, invalidate cache and retry once
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                AIApiKeyProvider.InvalidateCache();
                apiKey = await AIApiKeyProvider.GetApiKeyAsync();

                using var retryRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/ai/ask"));
                retryRequest.Content = JsonContent.Create(payload);

                AddApiKeyHeader(retryRequest, apiKey);

                using var retryResponse = await HttpClient.SendAsync(retryRequest);
                body = await retryResponse.Content.ReadAsStringAsync();

                if (!retryResponse.IsSuccessStatusCode)
                {
                    txtJson.Text = body;
                    MessageBox.Show($"AI request failed: {(int)retryResponse.StatusCode} {retryResponse.ReasonPhrase}\n\nPlease ensure the AI service is running and the API key is configured in the database.", 
                        "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                txtJson.Text = PrettyJson(body);
                using var retryDoc = JsonDocument.Parse(body);
                if (retryDoc.RootElement.TryGetProperty("summary", out var retrySummaryElement))
                {
                    txtSummary.Text = retrySummaryElement.GetString() ?? string.Empty;
                    await RenderSummaryMarkdownAsync(txtSummary.Text);
                }
                else
                {
                    tabSummary.SelectedTab = tabRawSummary;
                }
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                txtJson.Text = body;
                MessageBox.Show($"AI request failed: {(int)response.StatusCode} {response.ReasonPhrase}", "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtJson.Text = PrettyJson(body);

            using var doc = JsonDocument.Parse(body);

            // Capture response metadata for feedback submission
            _lastRequestId = doc.RootElement.TryGetProperty("requestId", out var ridEl) ? ridEl.GetString() : null;
            _lastToolName = doc.RootElement.TryGetProperty("tool", out var toolEl) ? toolEl.GetString() : null;
            _lastQuestionExcerpt = txtQuestion.Text.Length <= 120 ? txtQuestion.Text : txtQuestion.Text[..120];

            // Show confidence label
            if (doc.RootElement.TryGetProperty("confidenceLabel", out var confEl))
            {
                lblConfidence.Text = $"Confidence: {confEl.GetString()}";
                lblConfidence.Visible = true;
            }
            else
            {
                lblConfidence.Visible = false;
            }

            // Enable feedback buttons only when we have a valid request to reference
            var hasFeedbackTarget = !string.IsNullOrEmpty(_lastRequestId);
            btnThumbsUp.Enabled = hasFeedbackTarget;
            btnThumbsDown.Enabled = hasFeedbackTarget;

            if (doc.RootElement.TryGetProperty("summary", out var summaryElement))
            {
                txtSummary.Text = summaryElement.GetString() ?? string.Empty;
                await RenderSummaryMarkdownAsync(txtSummary.Text);
            }
            else
            {
                tabSummary.SelectedTab = tabRawSummary;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnAsk.Enabled = true;
            SetBusy(false);
        }
    }

    private async Task SendFeedbackAsync(bool helpful)
    {
        if (string.IsNullOrEmpty(_lastRequestId)) return;

        btnThumbsUp.Enabled = false;
        btnThumbsDown.Enabled = false;

        try
        {
            var apiKey = await AIApiKeyProvider.GetApiKeyAsync();
            var payload = new
            {
                requestId = _lastRequestId,
                isHelpful = helpful,
                toolName = _lastToolName,
                questionExcerpt = _lastQuestionExcerpt
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/ai/feedback"));
            request.Content = JsonContent.Create(payload);
            AddApiKeyHeader(request, apiKey);

            using var response = await HttpClient.SendAsync(request);
            // Best-effort — don't surface errors to the user for a non-critical action
            System.Diagnostics.Debug.WriteLine($"Feedback submitted: helpful={helpful}, status={response.StatusCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Feedback submission failed: {ex.Message}");
        }
    }

    private async Task RenderSummaryMarkdownAsync(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            tabSummary.SelectedTab = tabRawSummary;
            return;
        }

        try
        {
            var theme = ThemeExtensions.CurrentTheme;
            var fgColor = ColorTranslator.ToHtml(theme.ForegroundColor);
            var bgColor = ColorTranslator.ToHtml(theme.BackgroundColor);
            var codeBgColor = ColorTranslator.ToHtml(theme.InputBackColor);
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var htmlBody = Markdown.ToHtml(markdown, pipeline);
            var html = $$"""
                         <html>
                         <head>
                           <meta charset='utf-8'/>
                           <style>
                             body { font-family: Segoe UI, Arial, sans-serif; margin: 16px; color: {{fgColor}}; background: {{bgColor}}; }
                             h1,h2,h3 { margin: 8px 0; }
                             ul { padding-left: 20px; }
                             code { background: {{codeBgColor}}; padding: 2px 4px; border-radius: 3px; }
                             pre { background: {{codeBgColor}}; padding: 10px; border-radius: 4px; overflow-x: auto; }
                           </style>
                         </head>
                         <body>{{htmlBody}}</body>
                         </html>
                         """;

            var success = await webViewSummary.NavigateToLargeString(html);
            tabSummary.SelectedTab = success ? tabRenderedSummary : tabRawSummary;
        }
        catch
        {
            tabSummary.SelectedTab = tabRawSummary;
        }
    }

    private void SetBusy(bool isBusy, string statusMessage = "")
    {
        UseWaitCursor = isBusy;
        progressRunning.Visible = isBusy;
        lblStatus.Visible = isBusy;
        lblStatus.Text = isBusy ? statusMessage : string.Empty;
        btnAsk.Text = isBusy ? "Working..." : "Ask";
    }

    private string BuildUrl(string path)
    {
        // Use discovered service URL (local or repository), fall back to app.config for backwards compatibility
        var baseUrl = _serviceUrl ?? (ConfigurationManager.AppSettings["AIApiBaseUrl"] ?? "http://localhost:5055");
        return $"{baseUrl.TrimEnd('/')}{path}";
    }

    private static string PrettyJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int ParseInt(string? value, int defaultValue, int min, int max)
    {
        if (!int.TryParse(value, out var parsed)) return defaultValue;
        return Math.Clamp(parsed, min, max);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (string.Equals(value.Trim(), "(Auto)", StringComparison.OrdinalIgnoreCase)) return null;
        return value.Trim();
    }

    /// <summary>
    /// Sanitizes an API key so it is safe to use as an HTTP header value.
    /// HTTP headers must contain only ASCII characters (0x20–0x7E). Any key
    /// that contains non-ASCII bytes (e.g. due to DB encoding issues) will
    /// cause an HttpRequestException. Returns null when the key is unusable.
    /// </summary>
    private static string? SanitizeApiKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();
        // Reject the key entirely if it contains non-ASCII characters so the
        // caller gets a clear null rather than an exception mid-flight.
        foreach (var ch in key)
        {
            if (ch < 0x20 || ch > 0x7E)
                return null;
        }
        return key;
    }

    private static void AddApiKeyHeader(HttpRequestMessage request, string? apiKey)
    {
        var safe = SanitizeApiKey(apiKey);
        if (safe != null)
            request.Headers.Add("X-API-Key", safe);
    }
}

}
