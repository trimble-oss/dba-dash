using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    public sealed class CommunityToolFileUpdate
    {
        public CommunityTool Tool { get; init; }
        public CommunityToolFile File { get; init; }
        public string NewVersion { get; init; }
        public string NewVersionDate { get; init; }
        public string NewSha256 { get; init; }
    }

    public sealed class CommunityToolFamilyUpdate
    {
        public CommunityToolFamily Family { get; init; }
        public string NewRef { get; init; }
        public List<CommunityToolFileUpdate> Files { get; init; } = new();

        /// <summary>Why the family couldn't be updated.  The manifest is left at the old pin for this family.</summary>
        public string Error { get; init; }
    }

    /// <summary>
    /// Moves the manifest's pins to each family's newest upstream release.  Used by the scheduled GitHub workflow that
    /// raises a PR when a tool is updated - never at runtime.
    ///
    /// <para>The JSON is edited in place rather than re-serialized so the PR diff shows only the values that changed.</para>
    /// </summary>
    public static class CommunityToolsManifestUpdater
    {
        public static async Task<List<CommunityToolFamilyUpdate>> GetUpdatesAsync(CommunityToolsManifest manifest, CancellationToken cancellationToken = default)
        {
            var client = CreateGitHubClient();
            var updates = new List<CommunityToolFamilyUpdate>();
            foreach (var family in manifest.Families)
            {
                try
                {
                    var update = await GetUpdateAsync(client, family, cancellationToken);
                    if (update != null)
                    {
                        updates.Add(update);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unable to check {family} for updates", family.Name);
                    updates.Add(new CommunityToolFamilyUpdate { Family = family, Error = ex.Message });
                }
            }
            return updates;
        }

        /// <summary>The family's update, or null when the newest release has the same scripts as the pin.</summary>
        private static async Task<CommunityToolFamilyUpdate> GetUpdateAsync(GitHubClient client, CommunityToolFamily family, CancellationToken cancellationToken)
        {
            var newRef = await ResolveLatestRefAsync(client, family);
            if (string.Equals(newRef, family.Ref, StringComparison.Ordinal))
            {
                Log.Information("{family} is up to date at {ref}", family.Name, family.Ref);
                return null;
            }
            Log.Information("{family}: checking {newRef} (pinned {ref})", family.Name, newRef, family.Ref);

            var update = new CommunityToolFamilyUpdate { Family = family, NewRef = newRef };
            foreach (var tool in family.Tools)
            {
                foreach (var file in tool.Files)
                {
                    var url = CommunityToolScriptSource.RawGitHubUrl(family, newRef, file.Path);
                    var bytes = await CommunityToolScriptSource.DownloadAsync(url, cancellationToken);
                    var hash = CommunityToolScriptSource.ComputeSha256(bytes);
                    if (string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var (version, date) = CommunityToolVersionChecker.ParseVersion(family, Encoding.UTF8.GetString(bytes));
                    if (version == null)
                    {
                        throw new InvalidOperationException($"VersionPattern no longer matches {file.Path} at {newRef}");
                    }
                    update.Files.Add(new CommunityToolFileUpdate { Tool = tool, File = file, NewVersion = version, NewVersionDate = date, NewSha256 = hash });
                }
            }
            if (update.Files.Count == 0)
            {
                // Release made no change to the scripts we use.  The old pin still works, so leave it alone.
                Log.Information("{family}: {newRef} has no script changes", family.Name, newRef);
                return null;
            }
            return update;
        }

        /// <summary>The newest release tag, or the head commit of the branch for families that don't publish releases.</summary>
        private static async Task<string> ResolveLatestRefAsync(GitHubClient client, CommunityToolFamily family)
        {
            const string branchPrefix = "branch:";
            var parts = family.Repository.Split('/');
            if (family.LatestRef?.StartsWith(branchPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                // Pin the commit, not the branch, so the hashes stay valid after the branch moves on.
                var branch = await client.Repository.Branch.Get(parts[0], parts[1], family.LatestRef[branchPrefix.Length..]);
                return branch.Commit.Sha;
            }
            var release = await client.Repository.Release.GetLatest(parts[0], parts[1]);
            return release.TagName;
        }

        private static GitHubClient CreateGitHubClient()
        {
            var client = new GitHubClient(new ProductHeaderValue(Upgrade.GITHUB_APP));
            // Authenticated requests get a much higher rate limit.  The workflow supplies its token.
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                client.Credentials = new Credentials(token);
            }
            return client;
        }

        /// <summary>Applies the updates (ignoring failed ones) to the manifest JSON.</summary>
        public static string Apply(string json, IEnumerable<CommunityToolFamilyUpdate> updates)
        {
            foreach (var update in updates.Where(u => u.Error == null))
            {
                json = ReplaceFamilyRef(json, update);
                foreach (var file in update.Files)
                {
                    json = UpdateFileLine(json, file);
                }
            }
            return json;
        }

        /// <summary>
        /// The family's Ref is the first one after its Name.  Families can share a Ref (several use yyyyMMdd release
        /// tags), so it can't be found by value alone.
        /// </summary>
        private static string ReplaceFamilyRef(string json, CommunityToolFamilyUpdate update)
        {
            var nameIndex = json.IndexOf($"\"Name\": \"{update.Family.Name}\"", StringComparison.Ordinal);
            var oldRef = $"\"Ref\": \"{update.Family.Ref}\"";
            var refIndex = nameIndex < 0 ? -1 : json.IndexOf("\"Ref\": ", nameIndex, StringComparison.Ordinal);
            if (refIndex < 0 || string.CompareOrdinal(json, refIndex, oldRef, 0, oldRef.Length) != 0)
            {
                throw new InvalidOperationException($"Couldn't find {oldRef} for {update.Family.Name} in the manifest");
            }
            return string.Concat(json.AsSpan(0, refIndex), $"\"Ref\": \"{update.NewRef}\"", json.AsSpan(refIndex + oldRef.Length));
        }

        /// <summary>Each file is on one line, identified by its pinned hash.</summary>
        private static string UpdateFileLine(string json, CommunityToolFileUpdate update)
        {
            var lines = json.Split('\n');
            var matches = lines.Select((line, index) => (line, index))
                .Where(l => l.line.Contains($"\"Sha256\": \"{update.File.Sha256}\"", StringComparison.OrdinalIgnoreCase)
                            && l.line.Contains($"\"Path\": \"{update.File.Path}\"", StringComparison.Ordinal))
                .ToList();
            if (matches.Count != 1)
            {
                throw new InvalidOperationException($"Expected one manifest line for {update.File.Path}, found {matches.Count}");
            }
            var (fileLine, i) = matches[0];
            fileLine = SetValue(fileLine, "Version", update.NewVersion);
            fileLine = SetValue(fileLine, "VersionDate", update.NewVersionDate);
            fileLine = SetValue(fileLine, "Sha256", update.NewSha256);
            lines[i] = fileLine;
            return string.Join('\n', lines);
        }

        /// <summary>Sets a property on a single-line file object, adding it before Sha256 or removing it as needed.</summary>
        private static string SetValue(string line, string name, string value)
        {
            var existing = new Regex($"\"{name}\": \"[^\"]*\"(, )?");
            if (existing.IsMatch(line))
            {
                return value == null
                    ? existing.Replace(line, "", 1)
                    : existing.Replace(line, m => $"\"{name}\": \"{value}\"{m.Groups[1].Value}", 1);
            }
            return value == null ? line : ReplaceOnce(line, "\"Sha256\":", $"\"{name}\": \"{value}\", \"Sha256\":");
        }

        private static string ReplaceOnce(string text, string oldValue, string newValue)
        {
            var index = text.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0 || text.IndexOf(oldValue, index + oldValue.Length, StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException($"Expected exactly one {oldValue} in the manifest");
            }
            return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
        }

        /// <summary>Markdown summary for the PR description.</summary>
        public static string ToMarkdown(IEnumerable<CommunityToolFamilyUpdate> updates)
        {
            var sb = new StringBuilder();
            foreach (var update in updates)
            {
                var family = update.Family;
                if (update.Error != null)
                {
                    sb.AppendLine($"### {family.DisplayName} - not updated");
                    sb.AppendLine($"Error: {update.Error}");
                    sb.AppendLine();
                    continue;
                }
                sb.AppendLine($"### {family.DisplayName}");
                var oldRefUrl = Uri.EscapeDataString(family.Ref);
                var newRefUrl = Uri.EscapeDataString(update.NewRef);
                sb.AppendLine($"[{Escape(family.Ref)}]({family.RepositoryUrl}/tree/{oldRefUrl}) → [{Escape(update.NewRef)}]({family.RepositoryUrl}/tree/{newRefUrl}) · [compare]({family.RepositoryUrl}/compare/{oldRefUrl}...{newRefUrl})");
                sb.AppendLine();
                sb.AppendLine("| Tool | File | Version |");
                sb.AppendLine("|---|---|---|");
                foreach (var file in update.Files)
                {
                    sb.AppendLine($"| {file.Tool.Name} | {file.File.Path} | {Describe(file.File.Version, file.File.VersionDate)} → {Describe(file.NewVersion, file.NewVersionDate)} |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string Describe(string version, string date) => Escape(date == null ? version : $"{version} ({date})");

        /// <summary>
        /// Release names and versions come from upstream, so markdown in them is shown as text rather than formatting
        /// the PR description (or breaking the table).
        /// </summary>
        private static readonly Regex MarkdownSpecial = new(@"[\\`*_{}\[\]()<>#+!|~-]");

        internal static string Escape(string text) =>
            text == null ? null : MarkdownSpecial.Replace(text.ReplaceLineEndings(" "), @"\$0");
    }
}
