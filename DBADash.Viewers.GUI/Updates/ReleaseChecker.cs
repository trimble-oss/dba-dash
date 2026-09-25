using System.Net.Http;
using System.Text.Json;

namespace DBADashGUI.Viewers
{
    /// <summary>A release of the app that can be downloaded.</summary>
    /// <param name="Version">From the release's tag - 4.19.0.</param>
    /// <param name="ReleaseUrl">The release's page, with its notes.</param>
    /// <param name="DownloadUrl">The zip for this app.</param>
    public sealed record ReleaseInfo(Version Version, string ReleaseUrl, string DownloadUrl);

    /// <summary>
    /// Finds the latest release of the app on GitHub.  Only says what is there: nothing is downloaded, and nothing is
    /// replaced - the user chooses to download it, as an xcopy deployment is updated.
    /// </summary>
    public sealed class ReleaseChecker
    {
        private readonly HttpClient _http;
        private readonly string _releasesUrl;
        private readonly string _assetPrefix;

        /// <param name="http">The client to use.  Owned by the caller.</param>
        /// <param name="owner">The GitHub owner of the repository.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="assetPrefix">
        /// What the app's zip in a release starts with.  A release without one is not an update for this app - the release
        /// is of something else, or predates the app - and is ignored.
        /// </param>
        public ReleaseChecker(HttpClient http, string owner, string repository, string assetPrefix)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _releasesUrl = $"https://api.github.com/repos/{owner}/{repository}/releases/latest";
            _assetPrefix = assetPrefix ?? throw new ArgumentNullException(nameof(assetPrefix));
        }

        /// <summary>
        /// The latest published (not draft, not pre-release) release, or null when it isn't one for this app.  Throws
        /// when GitHub can't be reached or answers with something else.
        /// </summary>
        public async Task<ReleaseInfo> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _releasesUrl);
            // GitHub refuses a request with no user agent.
            request.Headers.UserAgent.ParseAdd("DBADashVisualizer");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            return Parse(document.RootElement);
        }

        /// <summary>The release described by GitHub's JSON, or null when it isn't a usable release for this app.</summary>
        internal ReleaseInfo Parse(JsonElement release)
        {
            if (release.ValueKind != JsonValueKind.Object) return null;
            if (IsTrue(release, "draft") || IsTrue(release, "prerelease")) return null;

            var tag = Text(release, "tag_name");
            if (!TryParseVersion(tag, out var version)) return null;

            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = Text(asset, "name");
                var url = Text(asset, "browser_download_url");

                // Signing replaces the -unsigned zip with a signed one of the same name less the suffix.  A release
                // that only has the unsigned one is a draft that has been published by mistake - don't offer it.
                if (name != null && url != null &&
                    name.StartsWith(_assetPrefix, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !name.EndsWith("-unsigned.zip", StringComparison.OrdinalIgnoreCase))
                {
                    return new ReleaseInfo(version, Text(release, "html_url"), url);
                }
            }

            return null;
        }

        /// <summary>A tag like "4.19.0" or "v4.19.0".  A version with a fourth part or a suffix is not one we release.</summary>
        internal static bool TryParseVersion(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag)) return false;

            var text = tag.Trim();
            if (text.StartsWith('v') || text.StartsWith('V')) text = text[1..];
            return Version.TryParse(text, out version);
        }

        private static string Text(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static bool IsTrue(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }
}
