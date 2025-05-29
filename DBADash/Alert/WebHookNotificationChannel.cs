using DBADashGUI.DBADashAlerts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    public class WebhookNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.Webhook;

        [DisplayName("Message Template")]
        [Description("Json message template (Leave blank to use default template).  Available parameters to replace: {title}, {text}, {instance}, {threadkey}, {icon}, {emoji}")]
        [Category("Webhook Config")]
        public JsonString MessageTemplate { get; set; }

        private const string GoogleChatCardTemplate = @"{
    ""thread"":  {
                   ""threadKey"":  ""{threadkey}""
               },
    ""cardsV2"":  [
                    {
                        ""card"":  {
                                     ""header"":  {
                                                    ""imageType"":  ""CIRCLE"",
                                                    ""title"":  ""{title}"",
                                                    ""imageAltText"":  ""DBA Dash"",
                                                    ""subtitle"":  ""{instance}"",
                                                    ""imageUrl"":  ""https://dbadash.com/{icon}""
                                                },
                                     ""sections"":  [
                                                      {
                                                          ""widgets"":  [
                                                                          {
                                                                              ""textParagraph"":  {
                                                                                                    ""text"":  ""{text}""
                                                                                                }
                                                                          }
                                                                      ]
                                                      }
                                                  ]
                                 }
                    }
                ]
}";

        public const string SlackTemplate = @"{
    ""blocks"": [
    	{
    		""type"": ""header"",
    		""text"": {
    			""type"": ""plain_text"",
    			""text"": ""{emoji} {title}""
    		}
    	},
    	{
    		""type"": ""section"",
    		""text"": {
    			""type"": ""mrkdwn"",
    			""text"": ""_{instance}_""
    		}
    	},
        {
          ""type"": ""divider""
        },
    	{
    		""type"": ""section"",
    		""text"": {
    			""type"": ""mrkdwn"",
    			""text"": ""{text}""
    		}
    	}
    ]
}";

        private const string GenericTemplate = @"{
    ""text"": ""{title}
{instance}
{text}
""
}";

        private string Template
        {
            get
            {
                if (!string.IsNullOrEmpty(MessageTemplate))
                {
                    return MessageTemplate;
                }
                return WebhookType switch
                {
                    WebhookTypes.Google => GoogleChatCardTemplate,
                    WebhookTypes.Slack => SlackTemplate,
                    _ => GenericTemplate
                };
            }
        }

        private string _webhookUrl;

        [PasswordPropertyText(true)]
        [Category("Webhook Config")]
        [DisplayName("Webhook Url")]
        public string WebhookUrl
        {
            get => _webhookUrl;
            set
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult))
                {
                    if (uriResult.Scheme != Uri.UriSchemeHttps)
                    {
                        throw new Exception("URL Scheme must be https");
                    }
                }
                else
                {
                    throw new Exception("Invalid WebhookUrl");
                }

                _webhookUrl = value;
            }
        }

        public enum WebhookTypes
        {
            Google,
            Slack,
            Other,
            None
        }

        [Category("Webhook Config")]
        [DisplayName("Webhook Type (automatic)")]
        [Description("DBA Dash provides specific support for Google and Slack. \nNote: Using Slack API is preferred over webhook as it supports threads.")]
        public WebhookTypes WebhookType
        {
            get
            {
                if (string.IsNullOrEmpty(WebhookUrl))
                {
                    return WebhookTypes.None;
                }
                else if (WebhookUrl.StartsWith("https://chat.googleapis.com"))
                {
                    return WebhookTypes.Google;
                }
                else if (WebhookUrl.StartsWith("https://hooks.slack.com/"))
                {
                    return WebhookTypes.Slack;
                }
                else
                {
                    return WebhookTypes.Other;
                }
            }
        }

        private const string GoogleWebhookReplyOption = "&messageReplyOption=REPLY_MESSAGE_FALLBACK_TO_NEW_THREAD";

        public override string EscapeText(string text) => EscapeTextJson(text);

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            var url = WebhookUrl;
            if (WebhookType == WebhookTypes.Google && !WebhookUrl.EndsWith(GoogleWebhookReplyOption))
            {
                url += GoogleWebhookReplyOption;
            }

            using var client = new HttpClient();
            var payload = ReplacePlaceholders(alert, Template);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(WebhookUrl))
            {
                yield return new ValidationResult("Webhook Url is required");
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