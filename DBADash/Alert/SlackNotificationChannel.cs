using DBADashGUI.DBADashAlerts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    internal class SlackNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.Slack;

        [Description("OAuth Token. Create a new app. Copy Bot User OAuth Token from OAuth & Permissions section. Add chat:write bot token scope & re-install app.  \nhttps://api.slack.com/apps")]
        [PasswordPropertyText(true)]
        [Category("Slack Config")]
        public string Token { get; set; }

        [Description("Slack channel name or ID. e.g. #alerts")]
        [Category("Slack Config")]
        public string SlackChannel { get; set; }

        [DisplayName("Message Template")]
        [Description("Json message template (Leave blank to use default template).  Available parameters to replace: {title}, {text}, {instance}, {icon}, {emoji}")]
        [Category("Slack Config")]
        public JsonString MessageTemplate { get; set; }

        private string Template => string.IsNullOrEmpty(MessageTemplate)
            ? WebhookNotificationChannel.SlackTemplate
            : MessageTemplate;

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            using var client = new HttpClient();
            const string url = "https://slack.com/api/chat.postMessage";

            var payload =
                WebhookNotificationChannel.GeneratePayloadFromTemplate(alert, Template);

            // Add channel to the template
            var jObj = JObject.Parse(payload);
            jObj["channel"] = SlackChannel;

            // Add thread_ts to reply to the previous thread if available
            if (!string.IsNullOrEmpty(alert.CustomThreadKey))
            {
                jObj["thread_ts"] = alert.ThreadKey;
            }

            var requestJson = JsonConvert.SerializeObject(jObj);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            var response = await client.PostAsync(url, requestContent);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var messageResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (((JToken)messageResponse.ok).Value<bool>())
                {
                    string ts = messageResponse.ts;
                    if (string.IsNullOrEmpty(alert.CustomThreadKey) && ChannelID != null && !string.IsNullOrEmpty(connectionString))
                    {
                        // Persist the thread key so we can reply to the same thread.
                        await alert.SetCustomThreadKey(ChannelID.Value, ts, connectionString);
                    }
                }
                else
                {
                    var error = ((JToken)messageResponse.error).Value<string>();
                    throw new Exception($"Failed to send message: {error}");
                }
            }
            else
            {
                throw new Exception($"Failed to send message: {response.StatusCode}");
            }
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(SlackChannel))
            {
                yield return new ValidationResult("Slack channel is required");
            }

            if (string.IsNullOrEmpty(Token))
            {
                yield return new ValidationResult("Token is required");
            }

            foreach (var validationResult in ValidateBase(validationContext)) yield return validationResult;
        }
    }
}