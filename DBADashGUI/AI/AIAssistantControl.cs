using DBADashGUI.AgentJobs;
using Markdig;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI;

public class AIAssistantControl : UserControl
{
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
        AutoSize = true,
        Padding = new Padding(0, 4, 0, 2)
    };
    private readonly TextBox txtQuestion = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly Button btnAsk = new() { Text = "Ask", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(0, 36) };
    private readonly TextBox txtSummary = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
    private readonly TextBox txtJson = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, WordWrap = false };
    private readonly WebView2Wrapper webViewSummary = new() { Dock = DockStyle.Fill };
    private readonly TabControl tabSummary = new() { Dock = DockStyle.Fill };
    private readonly TabPage tabRenderedSummary = new("Rendered");
    private readonly TabPage tabRawSummary = new("Raw");

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

    private bool _seedingExamples = false;
    private readonly string _apiBaseUrl = (ConfigurationManager.AppSettings["AIApiBaseUrl"] ?? "http://localhost:5055").TrimEnd('/');
    private readonly string? _configuredToolName = Normalize(ConfigurationManager.AppSettings["AIToolName"]);
    private readonly int _configuredMaxRows = ParseInt(ConfigurationManager.AppSettings["AIMaxRows"], 50, 1, 500);
    private readonly bool _configuredIncludeSummary = ParseBool(ConfigurationManager.AppSettings["AIIncludeSummary"], true);
    private readonly bool _showJson = ParseBool(ConfigurationManager.AppSettings["AIShowJson"], false);

    public AIAssistantControl()
    {
        BuildLayout();
        WireEvents();
        ApplyConfigVisibility();
        SeedExampleQuestions();
    }

    private void BuildLayout()
    {
        // ── Quick Start section ────────────────────────────────────────────
        var examplesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8, 18, 8, 4)
        };
        examplesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        examplesLayout.Controls.Add(new Label { Text = "Category", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 4) }, 0, 0);
        examplesLayout.Controls.Add(cboCategory, 0, 1);
        examplesLayout.Controls.Add(new Label { Text = "Example Question", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.BottomLeft, Padding = new Padding(0, 4, 0, 4) }, 0, 2);
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
        questionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
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
        questionPanel.Controls.Add(questionLabel, 0, 0);
        questionPanel.SetColumnSpan(questionLabel, 2);
        questionPanel.Controls.Add(txtQuestion, 0, 1);
        questionPanel.Controls.Add(btnAsk, 1, 1);
        questionPanel.Controls.Add(lblStatus, 0, 2);
        questionPanel.Controls.Add(progressRunning, 1, 2);

        // ── AI Summary section ─────────────────────────────────────────────
        tabRenderedSummary.Controls.Add(webViewSummary);
        tabRawSummary.Controls.Add(txtSummary);
        tabSummary.TabPages.Add(tabRenderedSummary);
        tabSummary.TabPages.Add(tabRawSummary);

        var summaryGroup = new GroupBox { Text = "AI Summary", Dock = DockStyle.Fill };
        summaryGroup.Controls.Add(tabSummary);

        jsonGroup.Controls.Add(txtJson);

        // ── Nest two SplitContainers for 3 resizable sections ──────────────
        // Inner split: Question (top) | AI Summary (bottom)
        var innerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            FixedPanel = FixedPanel.Panel1
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
            FixedPanel = FixedPanel.Panel1
        };
        outerSplit.Panel1.Controls.Add(examplesGroup);
        outerSplit.Panel2.Controls.Add(innerSplit);

        // Defer setting min sizes and SplitterDistance until the controls are sized
        outerSplit.HandleCreated += (_, _) =>
        {
            try
            {
                outerSplit.Panel1MinSize = 310;
                outerSplit.Panel2MinSize = 200;
                outerSplit.SplitterDistance = 320;
            }
            catch { }
        };
        innerSplit.HandleCreated += (_, _) =>
        {
            try
            {
                innerSplit.Panel1MinSize = 120;
                innerSplit.Panel2MinSize = 150;
                innerSplit.SplitterDistance = 150;
            }
            catch { }
        };

        Controls.Add(outerSplit);
    }

    private void SeedExampleQuestions()
    {
        _seedingExamples = true;

        _allExamples.AddRange(new (string, string)[]
        {
            ("Alerts", "What are the top issues requiring immediate DBA action right now?"),
            ("Alerts", "What unresolved alerts are currently highest priority?"),
            ("Alerts", "Which alert types are recurring most often this week?"),
            ("Alerts", "Which critical alerts are still active and unacknowledged?"),
            ("Alerts", "Which instances have the most active alerts right now?"),

            ("Availability Groups & DR", "What are our top DR and RPO risks across active instances?"),
            ("Availability Groups & DR", "Which Availability Groups are showing recent health instability?"),
            ("Availability Groups & DR", "Which instances have AG alerts plus backup risk right now?"),
            ("Availability Groups & DR", "Which replicas/AG-related alerts should be prioritized immediately?"),

            ("Backups & Storage", "Which databases are at backup risk right now?"),
            ("Backups & Storage", "Which databases are growing fastest this week?"),
            ("Backups & Storage", "Which databases have not had a recent successful full backup?"),
            ("Backups & Storage", "Which drives are at risk of filling up?"),
            ("Backups & Storage", "Which instances have the lowest free drive space?"),

            ("Capacity Forecasting", "Summarize storage and memory capacity risks for the next 7 days."),
            ("Capacity Forecasting", "Which drives are most likely to fill up soon?"),
            ("Capacity Forecasting", "Which instances show high memory pressure right now?"),
            ("Capacity Forecasting", "Which servers have less than 15% free space?"),

            ("Configuration Drift", "What configuration changes happened in the last week?"),
            ("Configuration Drift", "What high-risk SQL configuration changes happened in the last 14 days?"),
            ("Configuration Drift", "What SQL patching changes were detected recently?"),
            ("Configuration Drift", "Which instances have the highest configuration drift risk this week?"),
            ("Configuration Drift", "Which servers had frequent config changes that need review?"),
            ("Configuration Drift", "Which servers had hardware or driver changes recently?"),
            ("Configuration Drift", "Which trace flag or DB option changes need review?"),

            ("Cross-Signal Analysis", "Correlate alerts, waits, blocking, deadlocks, and storage risk by instance."),
            ("Cross-Signal Analysis", "Show likely root-cause clusters across the estate."),
            ("Cross-Signal Analysis", "What are the top systemic risks where multiple signals align?"),
            ("Cross-Signal Analysis", "Which instances have the highest multi-signal risk score?"),

            ("Instance Inventory", "How many instances are Enterprise Edition vs Standard Edition?"),
            ("Instance Inventory", "How many servers have more than 16GB of RAM?"),
            ("Instance Inventory", "How many servers have more than 64GB of RAM?"),
            ("Instance Inventory", "How many SQL Servers are on SQL 2016?"),
            ("Instance Inventory", "How many SQL Servers are on SQL 2019 or newer?"),
            ("Instance Inventory", "List SQL Server counts by major version."),
            ("Instance Inventory", "Which instances have the highest memory and CPU footprint?"),

            ("Jobs", "Are there any currently running jobs that look stuck?"),
            ("Jobs", "Which jobs are failing repeatedly over the last 7 days?"),
            ("Jobs", "Which jobs are running longer than normal?"),
            ("Jobs", "Which jobs failed today?"),

            ("Operational Hygiene", "Show unresolved vs resolved-unacknowledged alert counts by instance."),
            ("Operational Hygiene", "What stale alert workflow issues should be cleaned up today?"),
            ("Operational Hygiene", "Where is alert hygiene debt accumulating right now?"),
            ("Operational Hygiene", "Which instances have the highest resolved-but-unacknowledged alert backlog?"),

            ("Performance & Waits", "Are deadlocks increasing today?"),
            ("Performance & Waits", "Are we seeing blocking in the last 24 hours?"),
            ("Performance & Waits", "What are the top waits right now?"),
            ("Performance & Waits", "Which instances have the highest query wait times right now?"),
            ("Performance & Waits", "Which sessions are currently causing the most blocking?"),

            ("Query Performance", "Which queries are driving the most CPU right now?"),
            ("Query Performance", "Which queries have the biggest plan regressions this week?"),
            ("Query Performance", "Which queries have the highest average duration today?"),
            ("Query Performance", "Which queries regressed in the last day?"),

            ("Reliability", "Show the top servers by reliability risk score."),
            ("Reliability", "Which instances are most unstable based on restarts, offline events, and job failures?"),
            ("Reliability", "Which reliability incidents are recurring this week?"),
            ("Reliability", "Which unresolved reliability alerts need immediate attention?"),

            ("Triage & Summary", "Summarize current DBA risks for the next 24 hours."),
            ("Triage & Summary", "What should I check first to triage current performance issues?"),
            ("Triage & Summary", "Why are we slow right now?"),

            ("Workload Pressure", "Correlate waits, blocking, deadlocks, and slow queries for the last 24 hours."),
            ("Workload Pressure", "Where do we have sustained lock contention and query regressions?"),
            ("Workload Pressure", "Which instances show the highest workload pressure right now?"),
            ("Workload Pressure", "Which servers have the highest combined pressure score?"),
        });

        // Sort all examples alphabetically by category then question
        _allExamples.Sort((a, b) =>
        {
            var c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Question, b.Question, StringComparison.OrdinalIgnoreCase);
        });

        // Populate categories sorted alphabetically
        var categories = _allExamples.Select(e => e.Category).Distinct().OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        cboCategory.Items.Add("All Categories");
        foreach (var cat in categories)
            cboCategory.Items.Add(cat);
        cboCategory.SelectedIndex = 0;

        FilterExamplesByCategory();
        _seedingExamples = false;
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

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var payload = new
            {
                question = txtQuestion.Text,
                toolName = _configuredToolName,
                includeAiSummary = _configuredIncludeSummary,
                maxRows = _configuredMaxRows
            };

            using var response = await client.PostAsJsonAsync(BuildUrl("/api/ai/ask"), payload);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                txtJson.Text = body;
                MessageBox.Show($"AI request failed: {(int)response.StatusCode} {response.ReasonPhrase}", "AI Assistant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtJson.Text = PrettyJson(body);

            using var doc = JsonDocument.Parse(body);
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

    private async Task RenderSummaryMarkdownAsync(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            tabSummary.SelectedTab = tabRawSummary;
            return;
        }

        try
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var htmlBody = Markdown.ToHtml(markdown, pipeline);
            var html = $$"""
                         <html>
                         <head>
                           <meta charset='utf-8'/>
                           <style>
                             body { font-family: Segoe UI, Arial, sans-serif; margin: 16px; color: #222; }
                             h1,h2,h3 { margin: 8px 0; }
                             ul { padding-left: 20px; }
                             code { background: #f3f3f3; padding: 2px 4px; border-radius: 3px; }
                             pre { background: #f3f3f3; padding: 10px; border-radius: 4px; overflow-x: auto; }
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
        lblStatus.Text = isBusy ? $"⏳ {statusMessage}" : string.Empty;
        btnAsk.Text = isBusy ? "Running..." : "Ask";
    }

    private string BuildUrl(string path)
    {
        return $"{_apiBaseUrl}{path}";
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
}
