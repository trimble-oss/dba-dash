using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// The objects a deadlock touched whose definitions are worth fetching from the schema snapshots, and
    /// how a definition is cut to size.  Shared by the GUI's deadlock viewer and the MCP get-deadlock tool,
    /// which each fetch the definitions with their own repository connection.
    /// </summary>
    public static class DeadlockSchemaObjects
    {
        /// <summary>
        /// Definitions run long - a wide table with its indexes, a procedure with a few hundred lines
        /// - and the whole payload has to stay a sensible thing to send.  Cut rather than drop: the
        /// first part of a definition carries the columns and the beginning of the logic, which is
        /// most of the value.
        /// </summary>
        public const int MaxDdlLength = 8000;

        /// <summary>
        /// The objects worth asking about, grouped by the database they live in: the tables and
        /// indexes the deadlock was over, and the modules the statements were running in.
        /// </summary>
        public static Dictionary<string, List<string>> ByDatabase(DeadlockGraph graph)
        {
            var wanted = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            void Add(string? database, string? name)
            {
                if (string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(name)) return;

                if (!wanted.TryGetValue(database, out var objects))
                {
                    wanted[database] = objects = new List<string>();
                }

                if (!objects.Contains(name, StringComparer.OrdinalIgnoreCase)) objects.Add(name);
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

        /// <summary>A definition as read from the snapshots, cut to <see cref="MaxDdlLength"/>.</summary>
        public static DeadlockObjectDefinition Definition(
            string database,
            string name,
            string objectType,
            DateTime? asAt,
            string ddl)
        {
            var truncated = ddl.Length > MaxDdlLength;

            return new DeadlockObjectDefinition
            {
                Database = database,
                Name = name,
                ObjectType = objectType,
                AsAt = asAt,
                Ddl = truncated ? ddl[..MaxDdlLength] + Environment.NewLine + "-- [truncated]" : ddl,
                Truncated = truncated
            };
        }
    }
}
