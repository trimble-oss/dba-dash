using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    public enum CommunityToolStatus
    {
        NotInstalled,
        UpToDate,
        Outdated,

        /// <summary>Newer than the version DBA Dash pins.  Fine to use - it just hasn't been tested with DBA Dash.</summary>
        Newer,

        /// <summary>Installed, but the version couldn't be read or compared.</summary>
        Unknown,

        /// <summary>The tool doesn't support this instance (version or platform).</summary>
        NotSupported
    }

    public sealed class CommunityToolVersionInfo
    {
        public CommunityTool Tool { get; init; }
        public CommunityToolStatus Status { get; init; }
        public string InstalledVersion { get; init; }
        public string InstalledVersionDate { get; init; }

        /// <summary>The pinned version for this instance's build of the tool.</summary>
        public string ExpectedVersion { get; init; }

        /// <summary>Where the checked copy is (or would be deployed) - by default the copy DBA Dash runs.</summary>
        public string Database { get; init; }

        /// <summary>About other copies, e.g. one in master that's ignored because the connection's database has one.</summary>
        public string Note { get; init; }

        public string Message { get; init; }
    }

    /// <summary>
    /// Reads which community tools are installed and at what version.
    ///
    /// <para>The version comes from the procedure's definition (each family's VersionPattern) rather than from running
    /// the tool's own version check: that needs only VIEW DEFINITION, never executes third-party code, and works for
    /// tools that have no version parameter at all.</para>
    /// </summary>
    public static class CommunityToolVersionChecker
    {
        /// <summary>Version info is near the top of each script, so there's no need to read megabytes of definition.</summary>
        private const int DefinitionPrefixLength = 100000;

        /// <param name="database">Database to check, or null for the copy DBA Dash runs - the connection's database, then master.</param>
        public static async Task<List<CommunityToolVersionInfo>> CheckAsync(string connectionString, IReadOnlyCollection<CommunityTool> tools, string database = null, CancellationToken cancellationToken = default)
        {
            var info = await ConnectionInfo.GetConnectionInfoAsync(connectionString);
            CommunityToolDeployer.GetTargetDatabase(info, database); // Validates the database for Azure SQL DB

            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);
            var lookupOrder = CommunityToolLocator.LookupOrder(info, cn.Database);
            var databases = string.IsNullOrWhiteSpace(database) ? lookupOrder : lookupOrder.Append(database).ToList();
            var installed = await CommunityToolLocator.GetInstalledAsync(cn, tools, databases, cancellationToken);

            var locations = tools.ToDictionary(t => t, t => CommunityToolLocator.Locate(database, lookupOrder, installed[t.Name]));
            var definitions = new Dictionary<CommunityTool, string>();
            foreach (var group in locations.GroupBy(l => l.Value.Database, StringComparer.OrdinalIgnoreCase))
            {
                cn.ChangeDatabase(group.Key);
                var inDatabase = await GetDefinitionsAsync(cn, group.Select(l => l.Key).ToList(), cancellationToken);
                foreach (var tool in group.Select(l => l.Key))
                {
                    definitions[tool] = inDatabase.GetValueOrDefault(tool.Name);
                }
            }

            return tools.Select(tool => GetVersionInfo(tool, info, locations[tool].Database, definitions[tool], locations[tool].Note)).ToList();
        }

        internal static CommunityToolVersionInfo GetVersionInfo(CommunityTool tool, ConnectionInfo info, string database, string definition, string note)
        {
            var file = tool.GetFile(info, out var notSupportedReason);
            if (definition == null)
            {
                return new CommunityToolVersionInfo
                {
                    Tool = tool,
                    Status = file == null ? CommunityToolStatus.NotSupported : CommunityToolStatus.NotInstalled,
                    ExpectedVersion = file?.Version,
                    Database = database,
                    Note = note,
                    Message = notSupportedReason
                };
            }

            var (version, date) = ParseVersion(tool.Family, definition);
            var comparison = file == null ? null : CompareToPin(version, date, file);
            return new CommunityToolVersionInfo
            {
                Tool = tool,
                Status = comparison switch
                {
                    null => CommunityToolStatus.Unknown,
                    < 0 => CommunityToolStatus.Outdated,
                    0 => CommunityToolStatus.UpToDate,
                    > 0 => CommunityToolStatus.Newer
                },
                InstalledVersion = version,
                InstalledVersionDate = date,
                ExpectedVersion = file?.Version,
                Database = database,
                Note = note,
                Message = notSupportedReason
            };
        }

        public static (string version, string date) ParseVersion(CommunityToolFamily family, string definition)
        {
            var match = family.VersionRegex?.Match(definition);
            if (match is not { Success: true })
            {
                return (null, null);
            }
            var date = match.Groups["date"];
            return (match.Groups["version"].Value, date.Success ? date.Value : null);
        }

        /// <summary>
        /// Negative when the installed version is older than the pin.  Dates (yyyyMMdd) are compared when both sides
        /// have one, otherwise dotted versions.  Null when they can't be compared.
        /// </summary>
        public static int? CompareToPin(string version, string date, CommunityToolFile pinned)
        {
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(pinned.VersionDate))
            {
                return Math.Sign(string.CompareOrdinal(date, pinned.VersionDate));
            }
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(pinned.Version))
            {
                return null;
            }
            if (Version.TryParse(version, out var installed) && Version.TryParse(pinned.Version, out var expected))
            {
                return installed.CompareTo(expected);
            }
            return string.Equals(version, pinned.Version, StringComparison.OrdinalIgnoreCase) ? 0 : null;
        }

        /// <summary>Definitions of the tools present as dbo procedures in the connection's current database, by name.</summary>
        private static async Task<Dictionary<string, string>> GetDefinitionsAsync(SqlConnection cn, IReadOnlyCollection<CommunityTool> tools, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (tools.Count == 0)
            {
                return result;
            }
            await using var cmd = new SqlCommand { Connection = cn };
            var parameterNames = tools.Select((tool, i) =>
            {
                cmd.Parameters.AddWithValue("@p" + i, tool.Name);
                return "@p" + i;
            }).ToList();
            cmd.CommandText = $@"SELECT O.name, LEFT(M.definition, {DefinitionPrefixLength})
FROM sys.objects O
JOIN sys.sql_modules M ON M.object_id = O.object_id
WHERE O.schema_id = SCHEMA_ID(N'{CommunityTool.Schema}')
AND O.type = 'P'
AND O.name IN ({string.Join(',', parameterNames)})";
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                // Definition is NULL for an encrypted procedure, or one we can't see - treat it as installed, version unknown.
                result[rdr.GetString(0)] = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
            }
            return result;
        }
    }
}
