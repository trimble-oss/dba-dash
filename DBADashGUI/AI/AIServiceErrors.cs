#nullable enable
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DBADashGUI.AI
{
    /// <summary>
    /// A refusal from the AI service, worded for the reader.
    /// </summary>
    internal static class AIServiceErrors
    {
        private static readonly Regex LeadingRequestId = new(@"^RequestId=(?<id>[0-9a-fA-F]+)\.\s*", RegexOptions.Compiled);

        /// <summary>
        /// What the service said first, and the envelope it said it in last.  These end up on a status
        /// line that is clipped to the width of the viewer, so the part that is cut off should be the
        /// part that is only any use for finding the request in the service log.
        /// </summary>
        internal static string Describe(HttpResponseMessage response, string body)
        {
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            var detail = Detail(body);
            if (detail is null) return status;

            var match = LeadingRequestId.Match(detail);
            return match.Success
                ? $"{detail[match.Length..]} ({status}, RequestId={match.Groups["id"].Value})"
                : $"{detail} ({status})";
        }

        /// <summary>
        /// What the service said, rather than the envelope it said it in.  A refusal arrives as
        /// ProblemDetails, and what belongs in front of the reader is its detail - which for the one
        /// refusal they can do something about, a request larger than the model will take, is the part
        /// saying what to do about it.  Something other than the service answering - a proxy, or a
        /// host that fell over before the service saw the request - is passed on as it came: whatever
        /// it said is better than nothing.
        /// </summary>
        private static string? Detail(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            try
            {
                using var document = JsonDocument.Parse(body);

                foreach (var property in new[] { "detail", "error" })
                {
                    if (document.RootElement.ValueKind == JsonValueKind.Object
                        && document.RootElement.TryGetProperty(property, out var value)
                        && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }
            }
            catch (JsonException)
            {
            }

            return body;
        }
    }
}
