using DBADash.Deadlock;
using DBADash.Deadlock.Model;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashAI.Services;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// Where the XML for <see cref="DescribeDeadlockGraphTool"/> and <see cref="DescribeQueryPlanTool"/> comes from:
    /// passed in, or - on the local server only - read from a file the user names, such as a .xdl or .sqlplan they
    /// saved from SSMS.  Reading by path is off unless Mcp:AllowFilePaths is set, because on the AI service the path
    /// would be on the service's machine, read with the service's account.
    /// </summary>
    internal static class XmlSource
    {
        public const string AllowFilePathsSetting = McpToolRegistration.AllowFilePathsSetting;

        /// <summary>Plans run to megabytes; much past this is not a plan.</summary>
        private const long MaxFileBytes = 32L * 1024 * 1024;

        public static string PathDescription(bool allowFilePaths, string kind) => allowFilePaths
            ? $"The path of a {kind} file on this machine.  Pass xml or path, not both."
            : "Not available on this server: reading files is only offered by the local DBA Dash MCP server.  Pass xml instead.";

        public static string Read(RepositoryMcpTool.Arguments args, bool allowFilePaths)
        {
            var xml = args.String("xml");
            var path = args.String("path");

            if (xml is not null && path is not null)
            {
                throw new McpToolException("Pass xml or path, not both.");
            }

            if (xml is not null)
            {
                return xml;
            }

            if (path is null)
            {
                throw new McpToolException("Pass the XML as xml, or a file's path as path.");
            }

            if (!allowFilePaths)
            {
                throw new McpToolException("This server doesn't read files.  Read the file yourself and pass its contents as xml.");
            }

            var file = new FileInfo(path);
            if (!file.Exists)
            {
                throw new McpToolException($"There is no file at {path}.");
            }

            if (file.Length > MaxFileBytes)
            {
                throw new McpToolException($"{path} is {file.Length / 1024 / 1024} MB, too large to be a deadlock graph or plan.");
            }

            // .xdl and .sqlplan files are often UTF-16 with a byte order mark, which this detects.
            return File.ReadAllText(path);
        }
    }

    /// <summary>
    /// A deadlock graph the client already has - pasted, or saved by SSMS or an XE session - described as get-deadlock
    /// describes a stored one.  With an instance the object definitions are looked up from its schema snapshots too.
    /// </summary>
    public sealed class DescribeDeadlockGraphTool(IConfiguration? config) : RepositoryMcpTool(
        "describe-deadlock-graph",
        "Analyse a deadlock graph that isn't in the repository - pasted XML, or a .xdl file - in the same detail as " +
        "get-deadlock: participants, objects and modules, the findings of DBA Dash's deadlock rules, and the graph.  " +
        "Give instance to also get the definitions of the objects involved from that instance's schema snapshots.  For a " +
        "deadlock DBA Dash collected, use list-deadlocks and get-deadlock instead.",
        Schema(config?.GetValue<bool?>(XmlSource.AllowFilePathsSetting) ?? false),
        config)
    {
        private readonly bool _allowFilePaths = config?.GetValue<bool?>(XmlSource.AllowFilePathsSetting) ?? false;

        private static string Schema(bool allowFilePaths) => $$"""
            {
              "type": "object",
              "properties": {
                "xml": { "type": "string", "description": "The deadlock graph XML: a <deadlock> element, a deadlock list, or the XE event around it." },
                "path": { "type": "string", "description": "{{XmlSource.PathDescription(allowFilePaths, ".xdl or .xml")}}" },
                "graph": { "type": "integer", "minimum": 1, "description": "Which deadlock, when the XML holds several.  Defaults to 1." },
                "instance": { "type": "string", "description": "The instance it happened on, to look up the object definitions." }
              },
              "additionalProperties": false
            }
            """;

        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var xml = XmlSource.Read(args, _allowFilePaths);
            var which = args.Int("graph", 1, 1000) ?? 1;
            var instanceName = args.String("instance");

            IReadOnlyList<DeadlockGraph> graphs;
            try
            {
                graphs = DeadlockParser.Parse(xml);
            }
            catch (DeadlockParseException ex)
            {
                throw new McpToolException($"That isn't a deadlock graph DBA Dash can read: {ex.Message}");
            }

            if (graphs.Count == 0)
            {
                throw new McpToolException("The XML has no deadlock in it.");
            }

            if (which > graphs.Count)
            {
                throw new McpToolException($"The XML holds {graphs.Count} deadlock{(graphs.Count == 1 ? "" : "s")}, so graph must be 1 to {graphs.Count}.");
            }

            (int InstanceId, string Instance)? instance = instanceName is null
                ? null
                : await ResolveInstanceAsync(sql, instanceName, cancellationToken);

            var heading = graphs.Count > 1
                ? $"Deadlock {which} of the {graphs.Count} in the XML given."
                : "The deadlock in the XML given.";

            return await GetDeadlockTool.DescribeAsync(graphs[which - 1], instance?.Instance, instance?.InstanceId, heading,
                includeSchema: instance is not null, sql, cancellationToken);
        }
    }

    /// <summary>
    /// An execution plan the client already has - pasted, or a .sqlplan file - described as get-query-plan describes a
    /// stored one, one statement at a time.
    /// </summary>
    public sealed class DescribeQueryPlanTool(IConfiguration? config) : RepositoryMcpTool(
        "describe-query-plan",
        "Analyse an execution plan that isn't in the repository - pasted showplan XML, or a .sqlplan file - in the same " +
        "detail as get-query-plan: the statement, context and statistics, the warnings DBA Dash's plan rules raise, " +
        "missing indexes and the operators ranked by their own cost.  A plan with several statements is described one " +
        "statement at a time; it lists them.  For a plan DBA Dash captured, use get-query-plan instead.",
        Schema(config?.GetValue<bool?>(XmlSource.AllowFilePathsSetting) ?? false),
        config)
    {
        private readonly bool _allowFilePaths = config?.GetValue<bool?>(XmlSource.AllowFilePathsSetting) ?? false;

        private static string Schema(bool allowFilePaths) => $$"""
            {
              "type": "object",
              "properties": {
                "xml": { "type": "string", "description": "The showplan XML." },
                "path": { "type": "string", "description": "{{XmlSource.PathDescription(allowFilePaths, ".sqlplan or .xml")}}" },
                "statement": { "type": "integer", "minimum": 1, "description": "Which statement with a plan, numbered from 1.  Defaults to the most expensive." },
                "includePlanXml": { "type": "boolean", "description": "Include the statement's showplan XML in the result.  Defaults to false." }
              },
              "additionalProperties": false
            }
            """;

        protected override Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var xml = XmlSource.Read(args, _allowFilePaths);
            var which = args.Int("statement", 1, 10000);
            var includePlanXml = args.Bool("includePlanXml") ?? false;

            ExecutionPlan plan;
            try
            {
                plan = PlanParser.Parse(xml);
            }
            catch (PlanParseException ex)
            {
                throw new McpToolException($"That isn't a plan DBA Dash can read: {ex.Message}");
            }

            var withPlans = plan.StatementsWithPlans.ToList();
            if (withPlans.Count == 0)
            {
                throw new McpToolException("The plan has no statement with a plan in it.");
            }

            if (which > withPlans.Count)
            {
                throw new McpToolException($"The plan has {withPlans.Count} statement{(withPlans.Count == 1 ? "" : "s")} with plans, so statement must be 1 to {withPlans.Count}.");
            }

            var statement = which is { } n
                ? withPlans[n - 1]
                : plan.PrimaryStatement is { } primary && withPlans.Contains(primary) ? primary : withPlans[0];

            // Describe says which statement this is; the list is so the model can ask for another by its number.
            var heading = withPlans.Count == 1
                ? "The plan in the XML given."
                : "The plan in the XML given.  Its statements with plans: " +
                  string.Join("; ", withPlans.Select((s, i) => $"{i + 1}: {Shorten(s.StatementText)}"));

            return Task.FromResult(GetQueryPlanTool.Describe(plan, statement, null, null, heading, includePlanXml, MaxOutputChars));
        }

        private static string Shorten(string? text)
        {
            var single = string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return single.Length <= 80 ? single : single[..80] + "...";
        }
    }
}
