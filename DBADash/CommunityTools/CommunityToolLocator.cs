using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.CommunityTools
{
    /// <summary>Which copy of a tool to check or update, and anything worth telling the user about the other copies.</summary>
    public sealed record CommunityToolLocation(string Database, string Note);

    /// <summary>
    /// Works out which copy of a tool DBA Dash runs.  Every community tool is an sp_ procedure, so SQL Server looks in
    /// the connection's database first and then in master.  Checking and updating the copy that's actually used avoids
    /// reporting a tool in master as missing, and avoids a deploy leaving a second copy in the user database that
    /// silently takes over from the one in master.
    /// </summary>
    public static class CommunityToolLocator
    {
        private const string Master = "master";

        /// <summary>The databases searched for an sp_ procedure, in order.  Azure SQL Database has no lookup in master.</summary>
        public static List<string> LookupOrder(ConnectionInfo info, string connectionDatabase) =>
            info.IsAzureDB || string.Equals(connectionDatabase, Master, StringComparison.OrdinalIgnoreCase)
                ? new List<string> { connectionDatabase }
                : new List<string> { connectionDatabase, Master };

        /// <param name="requested">Database the user asked for, or null for the copy DBA Dash uses.</param>
        /// <param name="lookupOrder">From <see cref="LookupOrder"/>.</param>
        /// <param name="installedIn">Databases that have a copy of the tool.</param>
        public static CommunityToolLocation Locate(string requested, IReadOnlyList<string> lookupOrder, ICollection<string> installedIn)
        {
            bool Installed(string db) => installedIn.Contains(db, StringComparer.OrdinalIgnoreCase);
            var used = lookupOrder.FirstOrDefault(Installed);
            if (string.IsNullOrWhiteSpace(requested))
            {
                var ignored = lookupOrder.SkipWhile(db => !string.Equals(db, used, StringComparison.OrdinalIgnoreCase)).Skip(1).Where(Installed).ToList();
                return new CommunityToolLocation(used ?? lookupOrder[0],
                    ignored.Count == 0 ? null : $"Also in {string.Join(", ", ignored)}, which isn't used");
            }
            if (!Installed(requested) || string.Equals(requested, used, StringComparison.OrdinalIgnoreCase))
            {
                return new CommunityToolLocation(requested, used == null || Installed(requested) ? null : $"DBA Dash uses the copy in {used}");
            }
            return new CommunityToolLocation(requested, used != null
                ? $"The copy in {used} is used instead"
                : $"DBA Dash won't find this copy - it connects to {lookupOrder[0]}");
        }

        /// <summary>For each tool, the databases (from <paramref name="databases"/>) that have it as a dbo procedure.</summary>
        public static async Task<Dictionary<string, List<string>>> GetInstalledAsync(SqlConnection cn, IReadOnlyCollection<CommunityTool> tools, IEnumerable<string> databases, CancellationToken cancellationToken)
        {
            var result = tools.ToDictionary(t => t.Name, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
            var original = cn.Database;
            try
            {
                foreach (var db in databases.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    cn.ChangeDatabase(db);
                    await using var cmd = new SqlCommand("SELECT name FROM sys.procedures WHERE schema_id = SCHEMA_ID(N'" + CommunityTool.Schema + "')", cn);
                    await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                    while (await rdr.ReadAsync(cancellationToken))
                    {
                        if (result.TryGetValue(rdr.GetString(0), out var installedIn))
                        {
                            installedIn.Add(db);
                        }
                    }
                }
            }
            finally
            {
                cn.ChangeDatabase(original);
            }
            return result;
        }
    }
}
