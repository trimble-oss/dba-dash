using DBADashGUI.DBADashAlerts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    public class WebhookNotificationChannel : NotificationChannelBase
    {
        public override NotificationChannelTypes NotificationChannelType => NotificationChannelTypes.Webhook;

        [DisplayName("Message Template")]
        [Description("Json message template (Leave blank to use default template).  Available parameters to replace: {title}, {text}, {instance}, {connectionid}, {instanceandconnectionid}, {threadkey}, {icon}, {emoji}")]
        [Category("Webhook Config")]
        public JsonString MessageTemplate { get; set; }

        [DisplayName("Headers")]
        [Description("Optional custom HTTP headers to send with the webhook request (e.g. Authorization). Header values support the same placeholders as the message template (e.g. {Now} for a send-time timestamp, {PriorityBucket}). Values are stored encrypted with the channel configuration.")]
        [Category("Webhook Config")]
        // Must be a non-null instance: the PropertyGrid's collection editor mutates the list in
        // place, and when the property is null it has no IList to write to, so edits are discarded.
        public List<WebhookHeader> Headers { get; set; } = new();

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

        /// <summary>
        /// Applies placeholder replacement to header names and values. Header values are
        /// not JSON, so replacement is done raw (identity escaping) rather than JSON-escaped.
        /// </summary>
        private IEnumerable<WebhookHeader> ResolveHeaders(Alert alert)
        {
            if (Headers == null) return null;
            return Headers.Where(h => h != null).Select(h => new WebhookHeader(
                ReplacePlaceholders(alert, h.Name ?? string.Empty, s => s),
                ReplacePlaceholders(alert, h.Value ?? string.Empty, s => s)));
        }

        protected override async Task InternalSendNotificationAsync(Alert alert, string connectionString)
        {
            var url = WebhookUrl;
            if (WebhookType == WebhookTypes.Google && !WebhookUrl.EndsWith(GoogleWebhookReplyOption))
            {
                url += GoogleWebhookReplyOption;
            }

            var payload = ReplacePlaceholders(alert, Template);
            using var response = await WebhookSender.PostJsonAsync(url, payload, ResolveHeaders(alert));
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