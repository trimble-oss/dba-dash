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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
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
    private readonly ComboBox cboModel = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        IntegralHeight = false,
        MaxDropDownItems = 10,
        Margin = new Padding(0, 6, 0, 4)
    };
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

    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(120) };
    private bool _seedingExamples = false;
    private readonly string _apiBaseUrl = (ConfigurationManager.AppSettings["AIApiBaseUrl"] ?? "http://localhost:5055").TrimEnd('/');
    private readonly string? _configuredToolName = Normalize(ConfigurationManager.AppSettings["AIToolName"]);
    private readonly int _configuredMaxRows = ParseInt(ConfigurationManager.AppSettings["AIMaxRows"], 50, 1, 500);
    private readonly bool _configuredIncludeSummary = ParseBool(ConfigurationManager.AppSettings["AIIncludeSummary"], true);
    private readonly bool _showJson = ParseBool(ConfigurationManager.AppSettings["AIShowJson"], false);

    public AIAssistantControl()
    {
        BuildLayout();
        this.ApplyTheme();
        WireEvents();
        ApplyConfigVisibility();
        _ = LoadExampleQuestionsAsync();
        _ = LoadModelsAsync();
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
        examplesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
            RowCount = 4,
            Padding = new Padding(0, 4, 0, 0)
        };
        questionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        questionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        questionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        var modelLabel = new Label
        {
            Text = "Model",
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 4, 0, 4)
        };
        questionPanel.Controls.Add(questionLabel, 0, 0);
        questionPanel.SetColumnSpan(questionLabel, 2);
        questionPanel.Controls.Add(modelLabel, 0, 1);
        questionPanel.Controls.Add(cboModel, 1, 1);
        questionPanel.Controls.Add(txtQuestion, 0, 2);
        questionPanel.Controls.Add(btnAsk, 1, 2);
        questionPanel.Controls.Add(lblStatus, 0, 3);
        questionPanel.Controls.Add(progressRunning, 1, 3);

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
                outerSplit.Panel1MinSize = 50;
                outerSplit.Panel2MinSize = 200;
                outerSplit.SplitterDistance = 280;
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

    private async Task LoadExampleQuestionsAsync()
    {
        _seedingExamples = true;

        try
        {
            using var response = await SharedHttpClient.GetAsync(BuildUrl("/api/ai/examples"));
            if (response.IsSuccessStatusCode)
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
        }
        catch
        {
            // API unavailable — no examples shown
        }

        if (_allExamples.Count == 0)
        {
            cboCategory.Items.Add("(No examples available)");
            cboCategory.SelectedIndex = 0;
            _seedingExamples = false;
            return;
        }

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

    private async Task LoadModelsAsync()
    {
        try
        {
            using var response = await SharedHttpClient.GetAsync(BuildUrl("/api/ai/models"));
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var modelName = item.GetProperty("modelName").GetString() ?? string.Empty;
                    var displayName = item.GetProperty("displayName").GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(modelName))
                    {
                        cboModel.Items.Add(new ModelItem(modelName, displayName));
                    }
                }
            }
        }
        catch
        {
            // API unavailable
        }

        if (cboModel.Items.Count == 0)
        {
            cboModel.Items.Add(new ModelItem("", "(Default - configured model)"));
        }

        cboModel.SelectedIndex = 0;
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

            var selectedModel = (cboModel.SelectedItem as ModelItem)?.ModelName;
            var payload = new
            {
                question = txtQuestion.Text,
                toolName = _configuredToolName,
                includeAiSummary = _configuredIncludeSummary,
                maxRows = _configuredMaxRows,
                modelOverride = string.IsNullOrWhiteSpace(selectedModel) ? (string?)null : selectedModel
            };

            using var response = await SharedHttpClient.PostAsJsonAsync(BuildUrl("/api/ai/ask"), payload);
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
}
