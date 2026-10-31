using System.Data;
using System.Text;
using DBADash;
using DBADash.Deadlock;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADashAI.Models;
using DBADashAI.Services;
using Microsoft.Data.SqlClient;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// The deadlocks the Deadlocks collection stored, one row each, newest first.  An <see cref="IAiTool"/>
    /// so it takes the same instance/hoursBack/maxRows arguments as the summary tools, but offered over MCP
    /// only: /ask's deadlocks-summary already answers "how many", and a list is what a client needs to pick
    /// one for get-deadlock.
    /// </summary>
    public class DeadlockListTool(SqlToolExecutor sql) : IAiTool
    {
        public const string ToolName = "list-deadlocks";

        public string Name => ToolName;

        public bool SupportsEndTime => true;

        public string Description =>
            "List individual deadlocks the DBA Dash Deadlocks collection captured, newest first, with the victim's " +
            "database, application, login, host, procedure and statement, and the deadlock's signature (deadlocks " +
            "with the same signature are the same pattern recurring).";

        public string InputHint =>
            "Use to find a deadlock to look at in detail: pass a row's InstanceID, EventTime and DeadlockHash to " +
            "get-deadlock.  GraphAvailable says whether its graph is still kept.";

        public string[] Keywords => ["deadlock", "deadlocks", "victim"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await sql.QueryAsync("AI.DeadlockList_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, request.ToUtc, cancellationToken);
            return new AiToolResult
            {
                RowCount = rows.Count,
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    rows
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.Deadlocks + dbo.DeadlockProcesses + dbo.DeadlockXml",
                        Detail = "Deadlocks captured by the Deadlocks collection (EventTime is UTC).  Empty when the collection is not enabled - deadlocks-summary counts them from a performance counter either way."
                    }
                ]
            };
        }
    }

    /// <summary>
    /// One deadlock, as the AI service's deadlock analysis would be shown it: parsed, run through the same
    /// rules the deadlock viewer's Findings tab uses, with the definitions of the objects involved as the schema
    /// snapshots had them at the time, and the graph.  The text is <see cref="AiDeadlockPromptBuilder.Data"/>.
    /// </summary>
    public sealed class GetDeadlockTool(IConfiguration? config) : RepositoryMcpTool(
        "get-deadlock",
        "One deadlock in detail: its participants, the objects and modules involved, the findings DBA Dash's own " +
        "deadlock rules made of it, the definitions of those objects as they were when it happened (from schema " +
        "snapshots, where collected), and the deadlock graph XML.  Pass instanceId, eventTime and deadlockHash " +
        "exactly as list-deadlocks returned them.",
        """
        {
          "type": "object",
          "properties": {
            "instanceId": { "type": "integer", "description": "InstanceID from list-deadlocks." },
            "eventTime": { "type": "string", "description": "EventTime from list-deadlocks (UTC)." },
            "deadlockHash": { "type": "string", "description": "DeadlockHash from list-deadlocks, 0x followed by 32 hex digits." },
            "includeSchema": { "type": "boolean", "description": "Include the definitions of the objects involved.  Defaults to true." }
          },
          "required": ["instanceId", "eventTime", "deadlockHash"],
          "additionalProperties": false
        }
        """,
        config)
    {
        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var instanceId = args.Int("instanceId", 1, int.MaxValue, required: true)!.Value;
            var eventTime = args.UtcDateTime("eventTime", required: true)!.Value;
            var deadlockHash = args.Hex("deadlockHash", 16, required: true)!;
            var includeSchema = args.Bool("includeSchema") ?? true;

            var rows = await sql.QueryAsync("AI.DeadlockGraph_Get",
            [
                new SqlParameter("@InstanceID", SqlDbType.Int) { Value = instanceId },
                new SqlParameter("@EventTime", SqlDbType.DateTime2) { Scale = 3, Value = eventTime },
                new SqlParameter("@DeadlockHash", SqlDbType.VarChar, 34) { Value = deadlockHash }
            ], cancellationToken);

            if (rows.Count == 0)
            {
                throw new McpToolException(
                    "No graph is stored for that deadlock.  Check the three values are exactly as list-deadlocks gave " +
                    "them; if they are, its graph has passed its retention (GraphAvailable = 0).");
            }

            var instance = Convert.ToString(rows[0]["InstanceDisplayName"]);
            IReadOnlyList<DeadlockGraph> graphs;
            try
            {
                graphs = DeadlockParser.Parse(Convert.ToString(rows[0]["DeadlockGraph"]));
            }
            catch (DeadlockParseException ex)
            {
                throw new McpToolException($"The stored graph could not be read: {ex.Message}");
            }

            if (graphs.Count == 0)
            {
                throw new McpToolException("The stored graph has no deadlock in it.");
            }

            var heading = $"Deadlock on {instance} at {eventTime:yyyy-MM-dd HH:mm:ss.fff} UTC.";
            if (graphs.Count > 1)
            {
                heading += $"  The stored graph holds {graphs.Count} deadlocks; this is the first.";
            }

            return await DescribeAsync(graphs[0], instance, instanceId, heading, includeSchema, sql, cancellationToken);
        }

        /// <summary>
        /// A deadlock as get-deadlock describes it, wherever the graph came from - the repository here, the GUI's viewer
        /// for the local server's get-open-deadlock.  Object definitions are looked up only with an
        /// <paramref name="instanceId"/>, since the schema snapshots are per instance.
        /// </summary>
        public static async Task<string> DescribeAsync(
            DeadlockGraph graph,
            string? instance,
            int? instanceId,
            string heading,
            bool includeSchema,
            SqlToolExecutor sql,
            CancellationToken cancellationToken)
        {
            var schema = includeSchema && instanceId is { } id
                ? await FetchSchemaAsync(sql, id, graph, cancellationToken)
                : [];
            var payload = DeadlockAnalysisPayload.Build(graph, DeadlockAnalyser.Analyse(graph), instance, schema);

            var text = new StringBuilder();
            text.AppendLine(heading);

            if (includeSchema && schema.Count == 0)
            {
                text.AppendLine(instanceId is null
                    ? "No object definitions: it isn't known which instance the deadlock came from."
                    : "No object definitions were found - schema snapshots may not be collected for these databases.");
            }

            text.Append(AiDeadlockPromptBuilder.Data(ToRequest(payload, instanceId)));
            return text.ToString();
        }

        /// <summary>The payload as the AI service receives it from the deadlock viewer - see DeadlockAnalysisClient.ToJson.</summary>
        internal static AiDeadlockAnalysisRequest ToRequest(DeadlockAnalysisPayload payload, int? instanceId) => new()
        {
            GraphXml = payload.GraphXml,
            Signature = payload.Signature,
            SignatureVersion = (byte)payload.SignatureVersion,
            PayloadVersion = payload.Version,
            InstanceId = instanceId,
            Instance = payload.Instance,
            Participants = payload.Participants.ToList(),
            Objects = payload.Objects.ToList(),
            Modules = payload.Modules.ToList(),
            Findings = payload.Findings.ToList(),
            Schema = payload.Schema.Select(s => new AiDeadlockObjectDefinition
            {
                Name = s.Name,
                Database = s.Database,
                ObjectType = s.ObjectType,
                AsAt = s.AsAt,
                Ddl = s.Ddl,
                Truncated = s.Truncated
            }).ToList()
        };

        /// <summary>
        /// The definitions as they were when the deadlock happened - the deadlock viewer's lookup, through the
        /// tool's connection.  Best effort: a database without snapshots just contributes nothing.
        /// </summary>
        private static async Task<IReadOnlyList<DeadlockObjectDefinition>> FetchSchemaAsync(
            SqlToolExecutor sql,
            int instanceId,
            DeadlockGraph graph,
            CancellationToken cancellationToken)
        {
            var definitions = new List<DeadlockObjectDefinition>();

            foreach (var (database, objects) in DeadlockSchemaObjects.ByDatabase(graph))
            {
                List<Dictionary<string, object?>> rows;
                try
                {
                    rows = await sql.QueryAsync("dbo.DeadlockObjectDDL_Get",
                    [
                        new SqlParameter("@InstanceID", SqlDbType.Int) { Value = instanceId },
                        new SqlParameter("@DatabaseName", SqlDbType.NVarChar, 128) { Value = database },
                        new SqlParameter("@ObjectNames", SqlDbType.NVarChar, -1) { Value = string.Join(",", objects) },
                        new SqlParameter("@SnapshotDate", SqlDbType.DateTime2) { Scale = 3, Value = (object?)graph.OccurredAt ?? DBNull.Value }
                    ], cancellationToken);
                }
                catch (SqlException)
                {
                    continue;
                }

                definitions.AddRange(rows.Select(row => DeadlockSchemaObjects.Definition(
                    database,
                    $"{row["SchemaName"]}.{row["ObjectName"]}",
                    Convert.ToString(row["TypeDescription"]) ?? string.Empty,
                    row["SnapshotDate"] as DateTime?,
                    row["DDL"] is byte[] ddl ? SMOBaseClass.Unzip(ddl) ?? string.Empty : string.Empty)));
            }

            return definitions;
        }
    }
}
