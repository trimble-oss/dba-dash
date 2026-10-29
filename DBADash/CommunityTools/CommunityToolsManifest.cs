using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash.CommunityTools
{
    /// <summary>
    /// The community tools DBA Dash knows how to deploy, and the upstream version each one is pinned to.
    ///
    /// <para>Shipped as an embedded resource rather than the scripts themselves: DBA Dash never redistributes the
    /// tools, it fetches them from the author's repository at deploy time and checks the hash against the pin.
    /// Moving to a new upstream release is a change to this file only.</para>
    /// </summary>
    public sealed class CommunityToolsManifest
    {
        private const string ResourceName = "DBADash.CommunityTools.CommunityToolsManifest.json";

        public const string Disclaimer =
            "Community tools are third-party scripts written and maintained by their authors, not by DBA Dash.  " +
            "They are provided under their own licenses and without warranty.  DBA Dash only fetches and deploys them - " +
            "review each tool's license and documentation before using it in your environment.";

        public List<CommunityToolFamily> Families { get; set; } = new();

        [JsonIgnore]
        public IEnumerable<CommunityTool> Tools => Families.SelectMany(f => f.Tools);

        private static readonly Lazy<CommunityToolsManifest> _default = new(() => Parse(Utility.GetResourceString(ResourceName)));

        /// <summary>The manifest embedded in this build.</summary>
        public static CommunityToolsManifest Default => _default.Value;

        public static CommunityToolsManifest Parse(string json)
        {
            var manifest = JsonConvert.DeserializeObject<CommunityToolsManifest>(json) ?? throw new InvalidOperationException("Community tools manifest is empty");
            foreach (var family in manifest.Families)
            {
                foreach (var tool in family.Tools)
                {
                    tool.Family = family;
                }
            }
            return manifest;
        }

        public CommunityTool GetTool(string name) =>
            Tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Resolves a comma-separated list of tool names and/or family names (e.g. "FirstResponderKit,sp_WhoIsActive").
        /// "*" selects everything.  Unknown names throw so a typo on the command line doesn't silently deploy nothing.
        /// </summary>
        public List<CommunityTool> ResolveTools(string selection)
        {
            if (string.IsNullOrWhiteSpace(selection))
            {
                throw new ArgumentException("No community tools specified");
            }
            var names = selection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (names.Contains("*"))
            {
                return Tools.ToList();
            }
            var result = new List<CommunityTool>();
            foreach (var name in names)
            {
                var family = Families.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                var matched = family != null ? family.Tools : new List<CommunityTool> { GetTool(name) ?? throw new ArgumentException($"Unknown community tool: {name}") };
                result.AddRange(matched.Where(t => !result.Contains(t)));
            }
            return result;
        }
    }

    /// <summary>A set of tools published together from one repository, such as the First Responder Kit.</summary>
    public sealed class CommunityToolFamily
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Author { get; set; }

        /// <summary>GitHub owner/repo.</summary>
        public string Repository { get; set; }

        /// <summary>The release tag or commit the pinned hashes were taken from.</summary>
        public string Ref { get; set; }

        /// <summary>How to find the newest version: "release" for the latest GitHub release, or "branch:name".</summary>
        public string LatestRef { get; set; }

        public string License { get; set; }
        public string LicenseUrl { get; set; }

        /// <summary>
        /// Finds the version in a deployed procedure's definition.  Named groups: "version", and optionally "date"
        /// (yyyyMMdd) which is preferred for ordering when present.
        /// </summary>
        public string VersionPattern { get; set; }

        public List<CommunityTool> Tools { get; set; } = new();

        [JsonIgnore]
        public string RepositoryUrl => $"https://github.com/{Repository}";

        private Regex _versionRegex;

        [JsonIgnore]
        public Regex VersionRegex => string.IsNullOrEmpty(VersionPattern)
            ? null
            : _versionRegex ??= new Regex(VersionPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
    }

    public sealed class CommunityTool
    {
        public string Name { get; set; }

        /// <summary>Oldest SQL Server major version the tool supports (13 = 2016).  Null for no minimum.</summary>
        public int? MinMajorVersion { get; set; }

        /// <summary>Whether the tool can be deployed to Azure SQL Database.</summary>
        public bool AzureSqlDb { get; set; }

        /// <summary>One file normally.  Tools with builds for different SQL versions list one per version range.</summary>
        public List<CommunityToolFile> Files { get; set; } = new();

        [JsonIgnore]
        public CommunityToolFamily Family { get; internal set; }

        public const string Schema = "dbo";

        /// <summary>
        /// The file to deploy to an instance, or null with a reason when the tool doesn't support it.
        /// </summary>
        public CommunityToolFile GetFile(ConnectionInfo info, out string notSupportedReason)
        {
            notSupportedReason = null;
            if (info.IsAzureDB && !AzureSqlDb)
            {
                notSupportedReason = "Not supported on Azure SQL Database";
                return null;
            }
            var major = EffectiveMajorVersion(info);
            if (MinMajorVersion.HasValue && major < MinMajorVersion)
            {
                notSupportedReason = $"Requires SQL Server major version {MinMajorVersion} or later";
                return null;
            }
            var file = Files.FirstOrDefault(f => f.Supports(major));
            if (file == null)
            {
                notSupportedReason = $"No build for SQL Server major version {major}";
            }
            return file;
        }

        /// <summary>
        /// Azure SQL Database and Managed Instance report 12 as their major version but run the latest engine, so they
        /// get the newest build of each tool.
        /// </summary>
        public static int EffectiveMajorVersion(ConnectionInfo info) =>
            info.EngineEdition is Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlDatabase
                or Microsoft.SqlServer.Management.Common.DatabaseEngineEdition.SqlManagedInstance
                ? int.MaxValue
                : info.MajorVersion;

        public override string ToString() => Name;
    }

    public sealed class CommunityToolFile
    {
        /// <summary>Path of the script within the repository.</summary>
        public string Path { get; set; }

        public int? MinMajorVersion { get; set; }
        public int? MaxMajorVersion { get; set; }

        /// <summary>The version the pinned script reports, as captured by the family's VersionPattern.</summary>
        public string Version { get; set; }

        /// <summary>The pinned script's version date (yyyyMMdd), when the family's VersionPattern captures one.</summary>
        public string VersionDate { get; set; }

        /// <summary>SHA-256 of the pinned script, lower-case hex.</summary>
        public string Sha256 { get; set; }

        [JsonIgnore]
        public string FileName => System.IO.Path.GetFileName(Path);

        public bool Supports(int majorVersion) =>
            (!MinMajorVersion.HasValue || majorVersion >= MinMajorVersion) &&
            (!MaxMajorVersion.HasValue || majorVersion <= MaxMajorVersion);
    }
}
