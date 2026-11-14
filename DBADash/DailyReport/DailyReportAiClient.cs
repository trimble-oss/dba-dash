using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.DailyReport
{
    /// <summary>The AI summary of a report, or why there isn't one</summary>
    public class DailyReportAiSummary
    {
        public string Summary { get; init; }
        public string Model { get; init; }
        public string Error { get; init; }
        public bool Success => !string.IsNullOrWhiteSpace(Summary);
    }

    /// <summary>
    /// Asks the DBA Dash AI service for a short summary of a daily report (POST /api/ai/summarise-daily-report).  Finds the service as the
    /// GUI's deadlock and plan analysis do: the one registered in the repository (AI.ServiceConfig_Get), else a local one on the default port.  The report sent is
    /// metadata only - names and figures, no query text - so it's sent as metadataOnly, which meets any data setting the service has.
    /// A failure is returned, never thrown: the report goes out without the summary.
    /// </summary>
    public static class DailyReportAiClient
    {
        public const string DefaultLocalServiceUrl = "http://localhost:5055";
        public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

        private static readonly AiCertificatePins Pins = new();

        private static readonly HttpClient Client = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, certificate, _, errors) =>
                Pins.IsTrustedOrPinned(request.RequestUri, errors, certificate?.Thumbprint)
        })
        { Timeout = Timeout };

        public static async Task<DailyReportAiSummary> SummariseAsync(string repositoryConnectionString, string reportName, string reportText, CancellationToken cancellationToken = default)
        {
            try
            {
                var (url, apiKey, notFound) = await FindServiceAsync(repositoryConnectionString, cancellationToken);
                if (url == null)
                {
                    return new DailyReportAiSummary { Error = notFound };
                }

                var text = reportText.Length > 60_000 ? reportText[..60_000] : reportText;
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/api/ai/summarise-daily-report")
                {
                    Content = JsonContent.Create(new { reportName, report = text, dataDetail = "metadataOnly" })
                };
                if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Add("X-API-Key", apiKey.Trim());

                using var response = await Client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return new DailyReportAiSummary { Error = $"The AI service returned {(int)response.StatusCode}: {ErrorText(body)}" };
                }

                using var doc = JsonDocument.Parse(body);
                var summary = doc.RootElement.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var model = doc.RootElement.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                return string.IsNullOrWhiteSpace(summary)
                    ? new DailyReportAiSummary { Error = "The AI service returned an empty summary." }
                    : new DailyReportAiSummary { Summary = summary.Trim(), Model = model };
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            // Anything - a malformed registered URL, a lost connection, an unexpected reply - leaves the report without a summary rather than failing it
            {
                return new DailyReportAiSummary { Error = ex is TaskCanceledException ? "The AI service didn't answer in time." : ex.Message };
            }
        }

        /// <summary>
        /// The service registered in the repository when it's enabled, else a local service if one answers on the default port.  Like the
        /// GUI's deadlock and plan analysis, an enabled registration is used even when its heartbeat is old (IsActive): a service that is
        /// really down fails the request with its own error, which says more than "not available".  When nothing is found, NotFound says why.
        /// </summary>
        private static async Task<(string Url, string ApiKey, string NotFound)> FindServiceAsync(string connectionString, CancellationToken cancellationToken)
        {
            string registration;
            await using (var cn = new SqlConnection(connectionString))
            {
                await using var cmd = new SqlCommand("AI.ServiceConfig_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 10 };
                try
                {
                    await cn.OpenAsync(cancellationToken);
                    await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (!await r.ReadAsync(cancellationToken))
                    {
                        registration = "No DBA Dash AI service is registered in the repository";
                    }
                    else
                    {
                        var url = r["ServiceUrl"] as string;
                        var enabled = r["IsEnabled"] is bool e && e;
                        if (string.IsNullOrWhiteSpace(url))
                        {
                            registration = "The DBA Dash AI service registered in the repository has no service URL";
                        }
                        else if (!enabled)
                        {
                            registration = "The DBA Dash AI service registered in the repository is disabled";
                        }
                        else
                        {
                            Pins.Set(url, Optional(r, "CertificateThumbprint"));
                            return (url, r["ApiKey"] as string, null);
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 2812)
                {
                    registration = "This repository has no AI service registration (it's older than the AI schema)";
                }
                catch (SqlException ex) when (ex.Number == 229)
                {
                    registration = "The repository login can't read the AI service registration (it needs the AIUser role)";
                }
            }

            try
            {
                using var probe = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, probe.Token);
                using var health = await Client.GetAsync($"{DefaultLocalServiceUrl}/api/ai/health", linked.Token);
                if (health.IsSuccessStatusCode) return (DefaultLocalServiceUrl, null, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // No local service
            }
            return (null, null, $"{registration}, and none is running locally on {DefaultLocalServiceUrl}.");
        }

        private static string Optional(SqlDataReader r, string column)
        {
            for (var i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), column, StringComparison.OrdinalIgnoreCase)) return r.IsDBNull(i) ? null : r.GetValue(i).ToString();
            }
            return null;
        }

        /// <summary>The problem detail or error message from a failed response, kept short</summary>
        private static string ErrorText(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var name in new[] { "detail", "error", "title" })
                {
                    if (doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String) return Short(v.GetString());
                }
            }
            catch (JsonException)
            {
                // Not JSON
            }
            return Short(body);
        }

        private static string Short(string s) => string.IsNullOrWhiteSpace(s) ? "(no detail)" : s.Length > 300 ? s[..300] + "..." : s;
    }
}
