using DBADashGUI.DBADashAlerts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    /// <summary>
    /// Microsoft Teams channel using a Teams Workflow ("Send webhook alerts to a channel").
    /// A thin wrapper over the generic webhook transport (<see cref="WebhookSender"/>) that
    /// sends the alert as an Adaptive Card in the message envelope the Workflow expects.
    /// The legacy Office 365 connector (incoming webhook) is retired by Microsoft, so a plain
    /// {"text": ...} payload no longer works for most users.
    /// </summary>
    public class TeamsNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.Teams;

        // Teams rejects cards over ~28KB. Leave headroom for the rest of the card.
        internal const int MaxTextLength = 20000;

        [Category("Teams Config")]
        [DisplayName("Workflow Url")]
        [Description("The URL from the Teams Workflow. In Teams, open the channel, click ... > Workflows and choose \"Send webhook alerts to a channel\". Copy the URL it gives you once the workflow is created.  The URL contains a secret and is stored encrypted with the channel configuration.")]
        [PasswordPropertyText(true)]
        public string WebhookUrl { get; set; }

        [Category("Teams Config")]
        [DisplayName("Message Template (optional)")]
        [Description("Optional Json message template to override the default Adaptive Card.  The Workflow expects {\"type\":\"message\",\"attachments\":[{\"contentType\":\"application/vnd.microsoft.card.adaptive\",\"content\":{...card...}}]}.  Leave blank to use the default.  Available parameters to replace: {title}, {text}, {instance}, {connectionid}, {instanceandconnectionid}, {icon}, {iconurl}, {emoji}, {priority}, {prioritybucket}, {triggerdate}, {now}, {cloudprovider}, {cloudresourceid}, {cloudregion}, {cloudaccountid}")]
        public JsonString MessageTemplate { get; set; }

        public override string EscapeText(string text) => EscapeTextJson(text);

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
                throw new InvalidOperationException("Workflow Url is not configured for the Teams notification channel.");

            var payload = GetPayload(alert);
            using var response = await WebhookSender.PostJsonAsync(WebhookUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send notification to Teams. Status: {response.StatusCode}. Response: {responseContent}");
            }
            // Note: Workflows return 202 Accepted before the flow runs.  If the card is rejected by Teams,
            // the error is only visible in the workflow run history in Teams/Power Automate.
        }

        internal string GetPayload(Alert alert)
        {
            if (!string.IsNullOrEmpty(MessageTemplate))
            {
                return ReplacePlaceholders(alert, MessageTemplate);
            }

            var card = new JObject
            {
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["type"] = "AdaptiveCard",
                ["version"] = "1.4",
                ["msteams"] = new JObject { ["width"] = "Full" },
                ["body"] = new JArray(
                    new JObject
                    {
                        ["type"] = "Container",
                        ["style"] = GetContainerStyle(alert),
                        ["bleed"] = true,
                        ["items"] = new JArray(
                            new JObject
                            {
                                ["type"] = "ColumnSet",
                                ["columns"] = new JArray(
                                    new JObject
                                    {
                                        ["type"] = "Column",
                                        ["width"] = "auto",
                                        ["verticalContentAlignment"] = "Center",
                                        ["items"] = new JArray(new JObject
                                        {
                                            ["type"] = "Image",
                                            ["url"] = alert.GetIconUrl(),
                                            ["altText"] = alert.Status,
                                            ["size"] = "Small"
                                        })
                                    },
                                    new JObject
                                    {
                                        ["type"] = "Column",
                                        ["width"] = "stretch",
                                        ["verticalContentAlignment"] = "Center",
                                        ["items"] = new JArray(
                                            new JObject
                                            {
                                                ["type"] = "TextBlock",
                                                ["text"] = $"{alert.AlertName} [{alert.Status}]",
                                                ["weight"] = "Bolder",
                                                ["size"] = "Medium",
                                                ["wrap"] = true
                                            },
                                            new JObject
                                            {
                                                ["type"] = "TextBlock",
                                                ["text"] = alert.InstanceDisplayName,
                                                ["isSubtle"] = true,
                                                ["spacing"] = "None",
                                                ["wrap"] = true
                                            })
                                    })
                            })
                    },
                    new JObject
                    {
                        ["type"] = "FactSet",
                        ["facts"] = new JArray(GetFacts(alert).Select(f => new JObject { ["title"] = f.Key, ["value"] = f.Value }))
                    })
            };

            foreach (var block in GetTextBlocks(alert.Message))
            {
                ((JArray)card["body"]!).Add(block);
            }

            var message = new JObject
            {
                ["type"] = "message",
                ["attachments"] = new JArray(new JObject
                {
                    ["contentType"] = "application/vnd.microsoft.card.adaptive",
                    ["contentUrl"] = null,
                    ["content"] = card
                })
            };
            return message.ToString(Formatting.None);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetFacts(Alert alert)
        {
            yield return new("Status", alert.Status);
            var priority = alert.Priority.ToString();
            yield return new("Priority", string.Equals(priority, alert.PriorityBucket, StringComparison.OrdinalIgnoreCase) ? priority : $"{priority} ({alert.PriorityBucket})");
            yield return new("Triggered", alert.TriggerDate.ToUtcDateTimeOffset().ToStandardString());
            if (alert.IsResolved && alert.ResolvedDate.HasValue)
            {
                yield return new("Resolved", alert.ResolvedDate.Value.ToUtcDateTimeOffset().ToStandardString());
            }
            if (!string.IsNullOrEmpty(alert.CloudProvider))
            {
                yield return new("Cloud", string.Join(" / ", new[] { alert.CloudProvider, alert.CloudRegion }.Where(s => !string.IsNullOrEmpty(s))));
            }
        }

        /// <summary>
        /// Teams doesn't reliably render single line breaks inside a TextBlock, so each line
        /// of the message is sent as its own TextBlock.  Blank lines add spacing between blocks.
        /// </summary>
        internal static IEnumerable<JObject> GetTextBlocks(string message)
        {
            if (string.IsNullOrEmpty(message)) yield break;
            if (message.Length > MaxTextLength)
            {
                message = message[..MaxTextLength] + "\n…(truncated)";
            }

            var isFirst = true;
            var addSpace = false;
            foreach (var line in message.Replace("\r\n", "\n").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    addSpace = !isFirst;
                    continue;
                }
                yield return new JObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = line,
                    ["wrap"] = true,
                    ["spacing"] = isFirst || addSpace ? "Medium" : "None"
                };
                isFirst = false;
                addSpace = false;
            }
        }

        private static string GetContainerStyle(Alert alert)
        {
            if (alert.IsResolved) return "good";
            if (alert.IsAcknowledged) return "emphasis";
            return (short)alert.Priority switch
            {
                41 => "good",
                < 11 => "attention",
                < 31 => "warning",
                _ => "emphasis"
            };
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
            {
                yield return new ValidationResult("Workflow Url is required", new[] { nameof(WebhookUrl) });
            }
            else if (Uri.TryCreate(WebhookUrl, UriKind.Absolute, out var uriResult))
            {
                if (uriResult.Scheme != Uri.UriSchemeHttps)
                {
                    yield return new ValidationResult("Workflow Url scheme must be https", new[] { nameof(WebhookUrl) });
                }
            }
            else
            {
                yield return new ValidationResult("Invalid Workflow Url", new[] { nameof(WebhookUrl) });
            }

            if (!string.IsNullOrEmpty(MessageTemplate))
            {
                if (!Placeholders.Any(p => MessageTemplate.ToString().Contains(p, StringComparison.InvariantCultureIgnoreCase)))
                {
                    yield return new ValidationResult($"Message template must contain at least one of the following placeholders: {string.Join(", ", Placeholders)}.  Or leave blank to use the default template.");
                }
            }

            foreach (var validationResult in ValidateBase(validationContext)) yield return validationResult;
        }
    }
}
