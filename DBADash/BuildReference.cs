using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DBADash
{
    public class BuildReference
    {
        private const int MAX_URL_REDIRECT = 10;
        private static readonly TimeSpan DEFAULT_HTTP_TIMEOUT = TimeSpan.FromSeconds(30);
        private const string BUILD_REFERENCE_URL = "https://dataplat.dbatools.io/assets/dbatools-buildref-index.json";

        public static async Task UpdateAsync(string connectionString)
        {
            var jsonBuildRef = await GetBuildReferenceJsonAsync();
            await UpdateBuildReferenceAsync(connectionString, jsonBuildRef);
        }

        public static async Task UpdateBuildReferenceAsync(string connectionString, string jsonBuildRef)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.BuildReference_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("BuildReferenceJson", jsonBuildRef);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> GetBuildReferenceJsonAsync() => await GetStringFromUrlAsync(BUILD_REFERENCE_URL, System.Net.Mime.MediaTypeNames.Application.Json);

        public static async Task<string> GetStringFromUrlAsync(string url, string expectedMediaType = null, int maxRedirects = MAX_URL_REDIRECT, TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(url);
            ArgumentOutOfRangeException.ThrowIfNegative(maxRedirects);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false // Handle manually
            };

            using var client = new HttpClient(handler, disposeHandler: true);
            client.Timeout = timeout ?? DEFAULT_HTTP_TIMEOUT;
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DBADash", $"{version.Major}.{version.Minor}"));
            client.DefaultRequestHeaders.Accept.Clear();
            if (!string.IsNullOrWhiteSpace(expectedMediaType))
            {
                client.DefaultRequestHeaders.Accept.ParseAdd(expectedMediaType);
            }

            // Track a valid absolute base Uri to safely resolve relative redirects
            if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException("The initial URL must be an absolute URI.");
            }

            int redirectCount = 0;
            // Handle redirects manually. Automatic redirect didn't work
            // Handle standard HTTP/1.1 redirect status codes: 301, 302, 303, 307, 308
            while (true)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(expectedMediaType))
                {
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.ParseAdd(expectedMediaType);
                }
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                var isRedirect = response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                                  response.StatusCode == System.Net.HttpStatusCode.Found ||
                                  response.StatusCode == System.Net.HttpStatusCode.SeeOther ||
                                  response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
                                  response.StatusCode == System.Net.HttpStatusCode.PermanentRedirect;

                if (isRedirect && redirectCount < maxRedirects)
                {
                    var location = response.Headers.Location;
                    if (location == null)
                        throw new HttpRequestException($"Redirect response missing Location header at URL '{url}'. Redirects followed: {redirectCount}.");

                    Uri nextUri;
                    if (location.IsAbsoluteUri)
                    {
                        nextUri = location;
                    }
                    else
                    {
                        // Resolve relative Location header against the last known absolute baseUri
                        if (!Uri.TryCreate(baseUri, location.ToString(), out nextUri))
                            throw new HttpRequestException($"Invalid relative Location header '{location}' encountered at '{url}'. Redirects followed: {redirectCount}.");
                    }

                    // Update tracking state
                    baseUri = nextUri;
                    url = nextUri.AbsoluteUri;
                    redirectCount++;
                    // Continue loop to follow redirect
                    continue;
                }

                // If still a redirect but we've hit the limit, throw a specific error
                if (isRedirect && redirectCount >= maxRedirects)
                    throw new HttpRequestException($"Exceeded maximum redirects ({maxRedirects}) while requesting '{url}'.");

                // Not a redirect, ensure success and return content
                response.EnsureSuccessStatusCode();
                // Ensure expected content type if provided and header is present
                if (!string.IsNullOrWhiteSpace(expectedMediaType) &&
                    response.Content.Headers.ContentType != null &&
                    !response.Content.Headers.ContentType.MediaType.Equals(expectedMediaType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException($"Unexpected content type '{response.Content.Headers.ContentType}' for URL '{url}'. Expected '{expectedMediaType}'.");
                }
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            // Defensive: shouldn't reach here; loop returns or throws above
            throw new HttpRequestException($"Unexpected termination while fetching '{url}'. Redirects followed: {redirectCount}.");
        }
    }
}