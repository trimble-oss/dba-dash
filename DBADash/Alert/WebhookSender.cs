using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Alert
{
    /// <summary>
    /// A single custom HTTP header to send with a webhook request.
    /// Editable via the PropertyGrid's built-in collection editor.
    /// </summary>
    public class WebhookHeader
    {
        public WebhookHeader()
        {
        }

        public WebhookHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [Description("Header name (e.g. Authorization).")]
        public string Name { get; set; }

        // Header values can contain secrets (e.g. Authorization tokens). PasswordPropertyText
        // makes the collection editor's property grid render the value with password characters
        // instead of plain text.
        [PasswordPropertyText(true)]
        [Description("Header value (e.g. Bearer {token}). Stored encrypted with the channel configuration.")]
        public string Value { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(Name) ? "(new header)" : $"{Name}: {MaskedValue}";

        // Mask the value in the item label shown in the collection editor's list.
        private string MaskedValue => string.IsNullOrEmpty(Value) ? string.Empty : "********";
    }

    /// <summary>
    /// Shared transport used by the generic webhook channel and the channels that
    /// wrap it (e.g. AWS DevOps). Posts a JSON payload and applies any custom
    /// headers, handling the request/content header split transparently.
    /// </summary>
    public static class WebhookSender
    {
        // A single shared HttpClient avoids socket exhaustion / connection churn when alerts
        // are sent frequently across multiple channels.
        private static readonly HttpClient Client = new();

        public static async Task<HttpResponseMessage> PostJsonAsync(string url, string jsonPayload, IEnumerable<WebhookHeader> headers = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header == null || string.IsNullOrWhiteSpace(header.Name)) continue;

                    // Reject CR/LF in header names/values. Values are placeholder-expanded (e.g. from
                    // {Text}) and added via TryAddWithoutValidation, so unescaped newlines could break
                    // the request or enable header-injection style issues.
                    if (ContainsLineBreak(header.Name) || ContainsLineBreak(header.Value)) continue;

                    // Request headers and content headers are separate collections in HttpClient.
                    // Try the request headers first; if the header belongs to the content (e.g.
                    // Content-Type), TryAddWithoutValidation returns false and we set it there instead.
                    if (request.Headers.TryAddWithoutValidation(header.Name, header.Value)) continue;

                    request.Content.Headers.Remove(header.Name);
                    request.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }

            return await Client.SendAsync(request);
        }

        private static bool ContainsLineBreak(string value) =>
            value != null && (value.Contains('\r') || value.Contains('\n'));
    }
}
