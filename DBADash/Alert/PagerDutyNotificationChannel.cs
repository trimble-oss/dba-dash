using DBADashGUI.DBADashAlerts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static DBADashGUI.DBADashAlerts.NotificationChannelBase;

namespace DBADash.Alert
{
    public class PagerDutyNotificationChannel : NotificationChannelBase
    {
        [Category("PagerDuty Configuration")]
        [DisplayName("Integration Key (Routing Key)")]
        [Description("Add an Events API V2 integration to your PagerDuty service. The integration key (also known as a routing key) for the PagerDuty service. ")]
        public string IntegrationKey { get; set; }

        [Category("PagerDuty Configuration")]
        [DisplayName("Severity (Override)")]
        [Description("Severity can be critical, error, warning or info. Leave blank to set automatically based on alert priority.  https://developer.pagerduty.com/docs/send-alert-event")]
        public string Severity { get; set; }

        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.PagerDuty;

        private string PagerDutySeverity(Alert.Priorities priority)
        {
            if (!string.IsNullOrEmpty(Severity)) return Severity;
            switch (priority)
            {
                case Alert.Priorities.Critical:
                    return "critical";
                    break;

                case Alert.Priorities.High1:
                case Alert.Priorities.High2:
                case Alert.Priorities.High3:
                case Alert.Priorities.High4:
                case Alert.Priorities.High5:
                case Alert.Priorities.High6:
                case Alert.Priorities.High7:
                case Alert.Priorities.High8:
                case Alert.Priorities.High9:
                case Alert.Priorities.High10:
                    return "error";
                    break;

                case Alert.Priorities.Medium1:
                case Alert.Priorities.Medium2:
                case Alert.Priorities.Medium3:
                case Alert.Priorities.Medium4:
                case Alert.Priorities.Medium5:
                case Alert.Priorities.Medium6:
                case Alert.Priorities.Medium7:
                case Alert.Priorities.Medium8:
                case Alert.Priorities.Medium9:
                case Alert.Priorities.Medium10:
                case Alert.Priorities.Low1:
                case Alert.Priorities.Low2:
                case Alert.Priorities.Low3:
                case Alert.Priorities.Low4:
                case Alert.Priorities.Low5:
                case Alert.Priorities.Low6:
                case Alert.Priorities.Low7:
                case Alert.Priorities.Low8:
                case Alert.Priorities.Low9:
                case Alert.Priorities.Low10:
                    return "warning";
                    break;

                case Alert.Priorities.Information1:
                case Alert.Priorities.Information2:
                case Alert.Priorities.Information3:
                case Alert.Priorities.Information4:
                case Alert.Priorities.Information5:
                case Alert.Priorities.Information6:
                case Alert.Priorities.Information7:
                case Alert.Priorities.Information8:
                case Alert.Priorities.Information9:
                case Alert.Priorities.Information10:
                case Alert.Priorities.Success:
                    return "info";
                    break;

                default:
                    return "info";
            }
        }

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            if (string.IsNullOrEmpty(IntegrationKey))
                throw new InvalidOperationException("Integration Key (Routing Key) is not configured for PagerDuty notification channel.");

            var pagerDutyEvent = new
            {
                routing_key = IntegrationKey,
                event_action = alert.IsResolved ? "resolve" : alert.IsAcknowledged ? "acknowledge" : "trigger",
                dedup_key = alert.DefaultThreadKey,
                payload = new
                {
                    component = "SQL Server",
                    @class = alert.AlertType,
                    summary = alert.AlertName + " on " + alert.ConnectionID,
                    source = alert.ConnectionID,
                    severity = PagerDutySeverity(alert.Priority),
                    timestamp = alert.TriggerDate,
                    custom_details = new
                    {
                        AlertId = alert.AlertID,
                        AdditionalInfo = alert.Message,
                        Priority = alert.Priority.ToString(),
                        alert.AlertType
                    }
                },
                links = new[]
                {
                    new {
                        href = "http://dbadash.com",
                        text = "DBA Dash Homepage"
                    }
                },
                client = "DBA Dash",
            };

            var jsonContent = JsonConvert.SerializeObject(pagerDutyEvent);
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("https://events.pagerduty.com/v2/enqueue", new StringContent(jsonContent, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send notification to PagerDuty. Response: {responseContent}");
            }
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var validationResult in base.ValidateBase(validationContext))
            {
                yield return validationResult;
            }

            if (string.IsNullOrEmpty(IntegrationKey))
            {
                yield return new ValidationResult("Integration Key (Routing Key) is required for PagerDuty notification channel.", new[] { nameof(IntegrationKey) });
            }
        }
    }
}