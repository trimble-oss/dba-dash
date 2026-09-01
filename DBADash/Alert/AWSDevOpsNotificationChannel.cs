using DBADashGUI.DBADashAlerts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

            var eventTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            var incident = new
            {
                eventType = "incident",
                // AlertID is the identity of the alert in DBA Dash: it is stable across the whole
                // trigger -> acknowledge -> resolve lifecycle of one occurrence, and a new occurrence
                // gets a new AlertID. That makes it the correct correlation key for the incident so
                // created/updated/resolved events land on the same DevOps incident and separate
                // occurrences of the same rule are kept as distinct incidents.
                // Consolidated notifications (and the test alert) have no real AlertID, so fall back
                // to DefaultThreadKey - which includes the trigger time - to keep each one distinct.
                incidentId = alert.AlertID > 0 ? $"DBADash_{alert.AlertID}" : alert.DefaultThreadKey,
                // First notification for the alert -> created; any later notification (re-trigger,
                // escalation, acknowledgement) -> updated; resolution -> resolved.
                action = alert.IsResolved ? "resolved" : alert.NotificationCount == 0 ? "created" : "updated",
                priority = alert.PriorityBucket,
                title = alert.AlertName,
                description = alert.Message,
                timestamp = alert.TriggerDate.ToUtcDateTimeOffset().ToStandardString(),
                service = string.IsNullOrEmpty(Service) ? alert.InstanceDisplayName : Service,
                data = new
                {
                    alert.ConnectionID,
                    alert.InstanceDisplayName,
                    Priority = alert.Priority.ToString(),
                    alert.AlertType,
                    alert.Status,
                    AlertId = alert.AlertID
                }
            };

            var headers = new List<WebhookHeader>
            {
                new("Authorization", $"Bearer {ApiKey}"),
                new("x-amzn-event-timestamp", eventTimestamp)
            };

            var payload = JsonConvert.SerializeObject(incident);
            using var response = await WebhookSender.PostJsonAsync(WebhookUrl, payload, headers);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send notification to AWS DevOps Agent. Status: {response.StatusCode}. Response: {responseContent}");
            }
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
