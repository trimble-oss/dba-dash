using System.Text.Json;
using DBADash;
using Microsoft.Data.SqlClient;

namespace DBADashMCP
{
    /// <summary>
    /// Finds the repository to serve, the same way the GUI does: the connections the user saved in the GUI
    /// (%AppData%\DBADashGUI\ConnectionStrings.json), or the service's ServiceConfig.json alongside the exe
    /// when they have saved none.  The saved connection strings are DPAPI-encrypted for the current user,
    /// so this only works as the user who saved them - which is the point: the MCP server reads the
    /// repository as that user, with their permissions.
    /// </summary>
    internal static class RepositoryConnectionResolver
    {
        internal sealed record SavedConnection(string Name, string ConnectionString, bool IsDefault);

        private sealed class StoredConnection
        {
            public string? Name { get; set; }
            public string? EncryptedConnectionString { get; set; }
            public bool IsDefault { get; set; }
        }

        internal static string GuiConnectionsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBADashGUI", "ConnectionStrings.json");

        /// <summary>The user's saved connections, in the GUI's order (by name).</summary>
        internal static List<SavedConnection> GetSavedConnections()
        {
            if (File.Exists(GuiConnectionsPath))
            {
                var stored = JsonSerializer.Deserialize<List<StoredConnection>>(File.ReadAllText(GuiConnectionsPath)) ?? [];
                return stored
                    .Where(c => !string.IsNullOrWhiteSpace(c.EncryptedConnectionString))
                    .Select(c => new SavedConnection(c.Name ?? string.Empty, c.EncryptedConnectionString!.UserDecryptString(), c.IsDefault))
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (File.Exists(BasicConfig.JsonConfigPath))
            {
                var cfg = BasicConfig.Deserialize(File.ReadAllText(BasicConfig.JsonConfigPath));
                if (cfg.DestinationConnection.Type == DBADashConnection.ConnectionType.SQL)
                {
                    return [new SavedConnection("DBA Dash", cfg.DestinationConnection.ConnectionString, true)];
                }
            }

            return [];
        }

        /// <summary>
        /// The connection string to use: the one given, else the saved connection with the given name, else
        /// the user's default.  Throws with a message for the user when there is none.
        /// </summary>
        internal static string Resolve(string? connectionString, string? connectionName)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return Tag(connectionString);
            }

            var saved = GetSavedConnections();

            if (!string.IsNullOrWhiteSpace(connectionName))
            {
                var named = saved.FirstOrDefault(c => string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException(
                                $"No saved repository connection is called '{connectionName}'.  Run DBADashMCP --list-connections to see them.");
                return Tag(named.ConnectionString);
            }

            var chosen = saved.FirstOrDefault(c => c.IsDefault) ?? saved.FirstOrDefault()
                         ?? throw new InvalidOperationException(
                             "No repository connection found.  Connect to the repository in the DBA Dash GUI first, " +
                             "or pass --connection-string.");
            return Tag(chosen.ConnectionString);
        }

        /// <summary>Names the connections so the DBA can tell them apart from the GUI's in the repository's sessions.</summary>
        private static string Tag(string connectionString) =>
            new SqlConnectionStringBuilder(connectionString) { ApplicationName = "DBADashMCP" }.ConnectionString;
    }
}
