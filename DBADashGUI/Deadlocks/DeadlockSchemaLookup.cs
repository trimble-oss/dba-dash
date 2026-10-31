#nullable enable
using DBADash;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Fetches the definitions of the objects a deadlock touched from the repository's schema
    /// snapshots, as they were when the deadlock happened.
    ///
    /// The point-in-time part is what makes this worth doing rather than telling people to look the
    /// objects up themselves: the procedure may have been changed twice since the deadlock, and the
    /// definition that explains it is the one from before those changes.
    ///
    /// Everything here is best effort.  Schema snapshots are optional, the deadlock may predate the
    /// first one, and objects get dropped - so an empty result is an ordinary answer, not an error,
    /// and nothing here interrupts the user.
    /// </summary>
    internal static class DeadlockSchemaLookup
    {
        internal static async Task<IReadOnlyList<DeadlockObjectDefinition>> FetchAsync(
            DBADashContext? context,
            DeadlockGraph graph,
            CancellationToken cancellationToken = default)
        {
            if (context is not { InstanceID: > 0 } || graph is null) return Array.Empty<DeadlockObjectDefinition>();

            var wanted = DeadlockSchemaObjects.ByDatabase(graph);
            if (wanted.Count == 0) return Array.Empty<DeadlockObjectDefinition>();

            var definitions = new List<DeadlockObjectDefinition>();

            foreach (var (database, objects) in wanted)
            {
                try
                {
                    definitions.AddRange(
                        await FetchAsync(context.InstanceID, database, objects, graph.OccurredAt, cancellationToken));
                }
                catch (Exception ex)
                {
                    // A database with no snapshots, a dropped database, a permission the user does not
                    // have: the analysis is still worth running without this.
                    Log.Debug(ex, "Deadlock schema lookup failed for {database}", database);
                }
            }

            return definitions;
        }

        private static async Task<List<DeadlockObjectDefinition>> FetchAsync(
            int instanceId,
            string database,
            List<string> objects,
            DateTime? asAt,
            CancellationToken cancellationToken)
        {
            var definitions = new List<DeadlockObjectDefinition>();

            await using var connection = new SqlConnection(Common.ConnectionString);
            await using var command = new SqlCommand("dbo.DeadlockObjectDDL_Get", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            command.Parameters.AddWithValue("InstanceID", instanceId);
            command.Parameters.AddWithValue("DatabaseName", database);
            command.Parameters.AddWithValue("ObjectNames", string.Join(",", objects));
            command.Parameters.Add("SnapshotDate", SqlDbType.DateTime2).Value = (object?)asAt ?? DBNull.Value;

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                definitions.Add(DeadlockSchemaObjects.Definition(
                    database,
                    $"{reader["SchemaName"]}.{reader["ObjectName"]}",
                    Convert.ToString(reader["TypeDescription"]) ?? string.Empty,
                    reader["SnapshotDate"] as DateTime?,
                    SMOBaseClass.Unzip((byte[])reader["DDL"]) ?? string.Empty));
            }

            return definitions;
        }
    }
}
