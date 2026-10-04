#nullable enable
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// A "Model" menu for the AI analysis tabs: the configured model, or one of the others the service
    /// will accept.
    ///
    /// Only shown where there is a choice.  Anthropic and Ollama take a model per request - for Ollama
    /// the list is whatever is installed on the server - while Azure OpenAI is addressed by deployment,
    /// so there is nothing to pick from.
    /// </summary>
    internal sealed class AIModelMenu
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// The reader's choice, shared by every tab for the rest of the session: someone who switched to a
        /// bigger model for one plan most likely wants it for the next one too.  Null is the configured
        /// model.
        /// </summary>
        private static string? _sessionChoice;

        /// <summary>
        /// Raised when the reader picks a model in any tab.  The choice is shared, so every open menu has
        /// to show it: one still showing the old model would send the new one while saying otherwise.
        /// </summary>
        private static event Action? SessionChoiceChanged;

        private readonly List<string> _models = new();

        /// <summary>
        /// Which load is the latest.  A tab asks again each time it is pointed at something new, so loads
        /// can overlap, and only the last one asked for may fill the menu.
        /// </summary>
        private int _generation;

        internal ToolStripMenuItem Item { get; } = new("Model")
        {
            Visible = false, // Until we know there is a choice
            ToolTipText = "The model that answers.  The default is the one configured for the AI service."
        };

        internal AIModelMenu()
        {
            SessionChoiceChanged += OnSessionChoiceChanged;
            // Static event, so without this every viewer ever opened would be kept alive by it.
            Item.Disposed += (_, _) => SessionChoiceChanged -= OnSessionChoiceChanged;
        }

        private void OnSessionChoiceChanged()
        {
            // The menus all live on the UI thread today, but one opened on another would otherwise be
            // touched from the wrong thread.
            if (Item.Owner is { IsHandleCreated: true, InvokeRequired: true } owner)
            {
                owner.BeginInvoke(ShowSelection);
                return;
            }
            ShowSelection();
        }

        /// <summary>
        /// The model to ask for, or null for the configured one.  A choice made elsewhere that this
        /// service does not offer is not sent: it would only be refused.
        /// </summary>
        internal string? SelectedModel =>
            _sessionChoice is not null && _models.Contains(_sessionChoice) ? _sessionChoice : null;

        /// <summary>
        /// Fills the menu from the service.  Leaves it hidden when the provider takes no choice, or the
        /// service cannot say - analysis still works, with the configured model.
        ///
        /// Everything is fetched before anything is touched, and only by the latest call: an earlier one
        /// still waiting when the next starts finds it has been overtaken and drops what it got.  Until
        /// then the menu keeps what it showed, which for a tab moving between deadlocks on one service
        /// is the same list.
        /// </summary>
        internal async Task LoadAsync(AIServiceDiscovery.ServiceInfo? service)
        {
            var generation = ++_generation;

            List<(string Name, string Display)>? models = null;
            string? configured = null;

            if (service is not null)
            {
                try
                {
                    (models, configured) = await FetchAsync(service);
                }
                catch (Exception ex)
                {
                    // Whatever went wrong, without the list there is nothing to choose from - and the
                    // configured model still answers, so it is not worth more than a hidden menu.
                    System.Diagnostics.Debug.WriteLine($"Failed to load AI models: {ex.Message}");
                }
            }

            if (generation != _generation || Item.IsDisposed) return;

            _models.Clear();
            Item.DropDownItems.Clear();

            if (models is not { Count: > 0 })
            {
                Item.Visible = false;
                ShowSelection();
                return;
            }

            _models.AddRange(models.Select(m => m.Name));

            Item.DropDownItems.Add(NewItem(null, configured is null ? "Default" : $"Default ({configured})"));
            Item.DropDownItems.Add(new ToolStripSeparator());
            foreach (var (name, display) in models)
            {
                Item.DropDownItems.Add(NewItem(name, display));
            }

            ShowSelection();
            Item.Visible = true;
        }

        /// <summary>
        /// The models on offer and the configured one, or no models when the provider takes no choice.
        /// </summary>
        private static async Task<(List<(string Name, string Display)>? Models, string? Configured)> FetchAsync(
            AIServiceDiscovery.ServiceInfo service)
        {
            using var diagnostics = await GetJsonAsync(service, "/api/ai/diagnostics");
            var provider = String(diagnostics.RootElement, "provider");
            if (!SupportsModelChoice(provider)) return (null, null);

            var configured = ConfiguredModel(diagnostics.RootElement, provider!);

            using var models = await GetJsonAsync(service, "/api/ai/models");
            if (models.RootElement.ValueKind != JsonValueKind.Array) return (null, configured);

            var list = models.RootElement.EnumerateArray()
                .Select(m => (Name: String(m, "modelName"), Display: String(m, "displayName")))
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .Select(m => (m.Name!, string.IsNullOrWhiteSpace(m.Display) ? m.Name! : m.Display!))
                .ToList();

            return (list, configured);
        }

        internal static bool SupportsModelChoice(string? provider) =>
            string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase);

        private ToolStripMenuItem NewItem(string? model, string text)
        {
            var item = new ToolStripMenuItem(text) { Tag = model };
            item.Click += (_, _) =>
            {
                _sessionChoice = model;
                SessionChoiceChanged?.Invoke(); // This menu included
            };
            return item;
        }

        private void ShowSelection()
        {
            var selected = SelectedModel;
            foreach (var item in Item.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = Equals(item.Tag as string, selected);
            }
            Item.Text = selected is null ? "Model" : $"Model: {selected}";
        }

        /// <summary>The configured model's name as diagnostics reports it, where it is set.</summary>
        private static string? ConfiguredModel(JsonElement diagnostics, string provider)
        {
            var section = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? "ollama" : "anthropic";
            if (!diagnostics.TryGetProperty(section, out var settings)) return null;

            var model = String(settings, "model");
            return string.IsNullOrWhiteSpace(model) || model.StartsWith('(') ? null : model;
        }

        private static string? String(JsonElement element, string property) =>
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static async Task<JsonDocument> GetJsonAsync(AIServiceDiscovery.ServiceInfo service, string path)
        {
            var baseUrl = service.ServiceUrl
                          ?? ConfigurationManager.AppSettings["AIApiBaseUrl"]
                          ?? "http://localhost:5055";

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}{path}");
            var apiKey = service.ApiKey ?? await AIApiKeyProvider.GetApiKeyAsync();
            if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-API-Key", apiKey);

            using var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
