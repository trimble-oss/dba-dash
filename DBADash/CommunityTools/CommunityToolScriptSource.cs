using Octokit;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    /// <summary>A community tool script ready to deploy.</summary>
    public sealed class CommunityToolScript
    {
        public string Text { get; init; }

        public string Sha256 { get; init; }

        /// <summary>Whether the script is byte-for-byte the version the manifest pins.</summary>
        public bool MatchesPin { get; init; }

        /// <summary>URL or file path the script came from, for logging and results.</summary>
        public string Origin { get; init; }
    }

    /// <summary>
    /// Where deployment gets the scripts from.  Each script is fetched once per source and reused for every instance.
    /// </summary>
    public abstract class CommunityToolScriptSource
    {
        public enum SourceTypes
        {
            /// <summary>The upstream release the manifest pins, verified against the pinned hash.</summary>
            Pinned,

            /// <summary>The newest upstream release.  Not hash checked - it hasn't been tested with DBA Dash.</summary>
            Latest,

            /// <summary>Scripts the user has downloaded to a folder, for servers without internet access.</summary>
            Folder
        }

        private readonly ConcurrentDictionary<string, Lazy<Task<CommunityToolScript>>> cache = new(StringComparer.OrdinalIgnoreCase);

        public abstract SourceTypes SourceType { get; }

        public static CommunityToolScriptSource Create(SourceTypes sourceType, string folder = null) => sourceType switch
        {
            SourceTypes.Pinned => new PinnedGitHubScriptSource(),
            SourceTypes.Latest => new LatestGitHubScriptSource(),
            SourceTypes.Folder => new FolderScriptSource(folder),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType))
        };

        public Task<CommunityToolScript> GetScriptAsync(CommunityTool tool, CommunityToolFile file, CancellationToken cancellationToken = default) =>
            cache.GetOrAdd(tool.Family.Name + "/" + file.Path,
                    _ => new Lazy<Task<CommunityToolScript>>(() => FetchAsync(tool, file, cancellationToken)))
                .Value;

        protected abstract Task<CommunityToolScript> FetchAsync(CommunityTool tool, CommunityToolFile file, CancellationToken cancellationToken);

        protected static CommunityToolScript CreateScript(byte[] bytes, CommunityToolFile file, string origin)
        {
            var hash = ComputeSha256(bytes);
            return new CommunityToolScript
            {
                Text = Decode(bytes),
                Sha256 = hash,
                MatchesPin = string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase),
                Origin = origin
            };
        }

        public static string ComputeSha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

        private static string Decode(byte[] bytes)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        private static readonly Lazy<HttpClient> httpClient = new(() =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var version = typeof(CommunityToolScriptSource).Assembly.GetName().Version;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DBADash", $"{version?.Major}.{version?.Minor}"));
            return client;
        });

        protected internal static async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            Log.Information("Downloading {url}", url);
            using var response = await httpClient.Value.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        protected internal static string RawGitHubUrl(CommunityToolFamily family, string gitRef, string path) =>
            $"https://raw.githubusercontent.com/{family.Repository}/{Uri.EscapeDataString(gitRef)}/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}";
    }

    public sealed class PinnedGitHubScriptSource : CommunityToolScriptSource
    {
        public override SourceTypes SourceType => SourceTypes.Pinned;

        protected override async Task<CommunityToolScript> FetchAsync(CommunityTool tool, CommunityToolFile file, CancellationToken cancellationToken)
        {
            var url = RawGitHubUrl(tool.Family, tool.Family.Ref, file.Path);
            var script = CreateScript(await DownloadAsync(url, cancellationToken), file, url);
            if (!script.MatchesPin)
            {
                throw new InvalidOperationException($"Hash mismatch for {url}.  Expected {file.Sha256}, got {script.Sha256}.  The script was not deployed.");
            }
            return script;
        }
    }

    public sealed class LatestGitHubScriptSource : CommunityToolScriptSource
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<string>>> latestRefs = new(StringComparer.OrdinalIgnoreCase);

        public override SourceTypes SourceType => SourceTypes.Latest;

        protected override async Task<CommunityToolScript> FetchAsync(CommunityTool tool, CommunityToolFile file, CancellationToken cancellationToken)
        {
            var gitRef = await latestRefs.GetOrAdd(tool.Family.Name, _ => new Lazy<Task<string>>(() => ResolveLatestRefAsync(tool.Family))).Value;
            var url = RawGitHubUrl(tool.Family, gitRef, file.Path);
            return CreateScript(await DownloadAsync(url, cancellationToken), file, url);
        }

        private static async Task<string> ResolveLatestRefAsync(CommunityToolFamily family)
        {
            const string branchPrefix = "branch:";
            if (family.LatestRef?.StartsWith(branchPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                return family.LatestRef[branchPrefix.Length..];
            }
            var parts = family.Repository.Split('/');
            var client = new GitHubClient(new Octokit.ProductHeaderValue(Upgrade.GITHUB_APP));
            var release = await client.Repository.Release.GetLatest(parts[0], parts[1]);
            Log.Information("Latest release of {repo} is {tag}", family.Repository, release.TagName);
            return release.TagName;
        }
    }

    /// <summary>
    /// Scripts from a local folder.  A file is found at its repository path under the folder (a cloned or extracted
    /// repository), or by file name directly in the folder.  Tools with several builds share a file name, so those
    /// need the repository layout.  A script that differs from the pin is still deployed, with a warning.
    /// </summary>
    public sealed class FolderScriptSource : CommunityToolScriptSource
    {
        private readonly string folder;

        public FolderScriptSource(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException("A folder is required for the Folder source");
            }
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folder}");
            }
            this.folder = folder;
        }

        public override SourceTypes SourceType => SourceTypes.Folder;

        protected override async Task<CommunityToolScript> FetchAsync(CommunityTool tool, CommunityToolFile file, CancellationToken cancellationToken)
        {
            var path = FindFile(tool, file) ?? throw new FileNotFoundException($"{file.Path} not found in {folder}");
            var script = CreateScript(await File.ReadAllBytesAsync(path, cancellationToken), file, path);
            if (!script.MatchesPin)
            {
                Log.Warning("{path} is not the version DBA Dash has tested ({ref}).  Deploying it anyway.", path, tool.Family.Ref);
            }
            return script;
        }

        private string FindFile(CommunityTool tool, CommunityToolFile file)
        {
            var relative = file.Path.Replace('/', Path.DirectorySeparatorChar);
            var candidates = new[]
            {
                Path.Combine(folder, relative),
                Path.Combine(folder, tool.Family.Name, relative)
            }.ToList();
            // Flat folder only works when the file name is unique to this build.
            if (tool.Family.Tools.SelectMany(t => t.Files).Concat(tool.Family.SetupFiles).Count(f => string.Equals(f.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)) == 1)
            {
                candidates.Add(Path.Combine(folder, file.FileName));
            }
            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
