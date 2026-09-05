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
        /// <summary>
        /// Definitions run long - a wide table with its indexes, a procedure with a few hundred lines
        /// - and the whole payload has to stay a sensible thing to send.  Cut rather than drop: the
        /// first part of a definition carries the columns and the beginning of the logic, which is
        /// most of the value.
        /// </summary>
        private const int MaxDdlLength = 8000;

        internal static async Task<IReadOnlyList<DeadlockObjectDefinition>> FetchAsync(
            DBADashContext? context,
            DeadlockGraph graph,
            CancellationToken cancellationToken = default)
        {
            if (context is not { InstanceID: > 0 } || graph is null) return Array.Empty<DeadlockObjectDefinition>();

            var wanted = ObjectsByDatabase(graph);
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

        /// <summary>
        /// The objects worth asking about, grouped by the database they live in: the tables and
        /// indexes the deadlock was over, and the modules the statements were running in.
        /// </summary>
        private static Dictionary<string, List<string>> ObjectsByDatabase(DeadlockGraph graph)
        {
            var wanted = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            void Add(string? database, string? name)
            {
                if (string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(name)) return;

                if (!wanted.TryGetValue(database!, out var objects))
                {
                    wanted[database!] = objects = new List<string>();
                }

                if (!objects.Contains(name!, StringComparer.OrdinalIgnoreCase)) objects.Add(name!);
            }

            foreach (var resource in graph.Resources)
            {
                // Object names in a graph are three part - "Sales.dbo.Orders" - so the database comes
                // from the name itself rather than from the process that happened to be waiting.  The
                // resource splits its own name; Add ignores anything that came back null.
                Add(resource.DatabaseName, resource.SchemaQualifiedName);
            }

            foreach (var frame in graph.Processes.Select(p => p.PrimaryFrame).Where(f => f is { IsModule: true }))
            {
                Add(frame!.ModuleDatabaseName, $"{frame.ModuleSchemaName}.{frame.ModuleObjectName}");
            }

            return wanted;
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
                var ddl = SMOBaseClass.Unzip((byte[])reader["DDL"]) ?? string.Empty;
                var truncated = ddl.Length > MaxDdlLength;

                definitions.Add(new DeadlockObjectDefinition
                {
                    Database = database,
                    Name = $"{reader["SchemaName"]}.{reader["ObjectName"]}",
                    ObjectType = Convert.ToString(reader["TypeDescription"]) ?? string.Empty,
                    AsAt = reader["SnapshotDate"] as DateTime?,
                    Ddl = truncated ? ddl[..MaxDdlLength] + Environment.NewLine + "-- [truncated]" : ddl,
                    Truncated = truncated
                });
            }

            return definitions;
        }
    }
}
