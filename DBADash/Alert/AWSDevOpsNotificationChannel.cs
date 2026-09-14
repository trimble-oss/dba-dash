using DBADashGUI.DBADashAlerts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    /// <summary>
    /// First-class channel for the AWS DevOps Agent incident webhook.
    /// A thin, easy-to-configure wrapper over the generic webhook transport
    /// (<see cref="WebhookSender"/>): the user supplies just the webhook URL and
    /// API key, and this channel maps the DBA Dash alert to the DevOps Agent
    /// incident schema and adds the required authentication headers.
    /// </summary>
    public class AWSDevOpsNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.AWSDevOps;

        public override bool IncludeNotificationCountInMessage => false;

        // Each DBA Dash alert maps to its own DevOps incident. A consolidated notification would
        // create one incident spanning multiple instances, leaving the agent to guess which one to investigate.
        public override bool SupportsConsolidation => false;

        [Browsable(false)]
        public override int? AlertConsolidationThreshold { get; set; }

        [Category("AWS DevOps Agent Config")]
        [DisplayName("Webhook Url")]
        [Description("The generic webhook URL provided by the DevOps Agent integration.")]
        [PasswordPropertyText(true)]
        public string WebhookUrl { get; set; }

        [Category("AWS DevOps Agent Config")]
        [DisplayName("API Key")]
        [Description("The API key / secret provided by the DevOps Agent integration. Sent as an 'Authorization: Bearer' header and stored encrypted with the channel configuration.")]
        [PasswordPropertyText(true)]
        public string ApiKey { get; set; }

        [Category("AWS DevOps Agent Config")]
        [DisplayName("Service (optional)")]
        [Description("Value for the incident 'service' field. Leave blank to use the monitored instance display name.")]
        public string Service { get; set; }

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
                throw new InvalidOperationException("Webhook Url is not configured for the AWS DevOps notification channel.");

            var now = DateTime.UtcNow;
            var eventTimestamp = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            var headers = new List<WebhookHeader>
            {
                new("Authorization", $"Bearer {ApiKey}"),
                new("x-amzn-event-timestamp", eventTimestamp)
            };

            var payload = GetPayload(alert, now);
            // The DevOps Agent doesn't expose the payload it received, so log exactly what was sent (including on failure)
            alert.NotificationLogMessage = payload;
            using var response = await WebhookSender.PostJsonAsync(WebhookUrl, payload, headers);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send notification to AWS DevOps Agent. Status: {response.StatusCode}. Response: {responseContent}");
            }
        }

        internal string GetPayload(Alert alert, DateTime utcNow)
        {
            // First notification for the alert -> created; any later notification (re-trigger,
            // escalation, acknowledgement) -> updated; resolution -> resolved.
            var action = alert.IsResolved ? "resolved" : alert.NotificationCount == 0 ? "created" : "updated";

            // The time of the event being reported, not always the trigger time. The DevOps Agent
            // de-duplicates on incidentId + timestamp, so a resolved/updated event that repeats the
            // created event's timestamp is dropped as a duplicate.
            var eventDate = action switch
            {
                "created" => alert.TriggerDate,
                "resolved" => alert.ResolvedDate ?? utcNow,
                _ => utcNow
            };

            var incident = new
            {
                eventType = "incident",
                // AlertID is the identity of the alert in DBA Dash: it is stable across the whole
                // trigger -> acknowledge -> resolve lifecycle of one occurrence, and a new occurrence
                // gets a new AlertID. That makes it the correct correlation key for the incident so
                // created/updated/resolved events land on the same DevOps incident and separate
                // occurrences of the same rule are kept as distinct incidents.
                // The test alert has no real AlertID, so fall back to DefaultThreadKey - which
                // includes the trigger time - to keep each one distinct.
                incidentId = alert.AlertID > 0 ? $"DBADash_{alert.AlertID}" : alert.DefaultThreadKey,
                action,
                priority = alert.PriorityBucket,
                // Include the instance so the same alert on different instances isn't correlated as one incident
                title = $"{alert.AlertName} on {alert.InstanceDisplayName}",
                description = GetDescription(alert),
                timestamp = eventDate.ToUtcDateTimeOffset().ToStandardString(),
                service = string.IsNullOrEmpty(Service) ? alert.InstanceDisplayName : Service,
                data = new
                {
                    alert.ConnectionID,
                    alert.InstanceDisplayName,
                    Priority = alert.Priority.ToString(),
                    alert.AlertType,
                    alert.Status,
                    AlertId = alert.AlertID,
                    metadata = string.IsNullOrEmpty(alert.CloudProvider)
                        ? null
                        : new
                        {
                            provider = alert.CloudProvider,
                            resourceId = alert.CloudResourceID,
                            region = alert.CloudRegion,
                            accountId = alert.CloudAccountID
                        }
                }
            };

            return JsonConvert.SerializeObject(incident, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary>
        /// Leads with the instance identity so the agent attributes the incident to the correct
        /// server/region rather than inferring it from the alert text.
        /// </summary>
        private static string GetDescription(Alert alert)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SQL Server instance: {alert.InstanceDisplayNameAndConnectionID}");
            if (!string.IsNullOrEmpty(alert.CloudProvider))
            {
                sb.AppendLine($"Cloud provider: {alert.CloudProvider}");
                AppendIfNotEmpty(sb, "Resource ID", alert.CloudResourceID);
                AppendIfNotEmpty(sb, "Region", alert.CloudRegion);
                AppendIfNotEmpty(sb, alert.CloudProvider == "Azure" ? "Subscription ID" : "Account ID", alert.CloudAccountID);
            }
            sb.AppendLine();
            sb.Append(alert.Message);
            return sb.ToString();
        }

        private static void AppendIfNotEmpty(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrEmpty(value)) sb.AppendLine($"{label}: {value}");
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
            {
                yield return new ValidationResult("Webhook Url is required", new[] { nameof(WebhookUrl) });
            }
            else if (Uri.TryCreate(WebhookUrl, UriKind.Absolute, out var uriResult))
            {
                if (uriResult.Scheme != Uri.UriSchemeHttps)
                {
                    yield return new ValidationResult("Webhook Url scheme must be https", new[] { nameof(WebhookUrl) });
                }
            }
            else
            {
                yield return new ValidationResult("Invalid Webhook Url", new[] { nameof(WebhookUrl) });
            }

            if (string.IsNullOrEmpty(ApiKey))
            {
                yield return new ValidationResult("API Key is required", new[] { nameof(ApiKey) });
            }

            foreach (var validationResult in ValidateBase(validationContext)) yield return validationResult;
        }
    }
}
