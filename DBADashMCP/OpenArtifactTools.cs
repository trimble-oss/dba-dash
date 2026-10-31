using DBADash;
using DBADash.Deadlock;
using DBADash.Deadlock.Model;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashAI.Mcp;
using DBADashAI.Services;
using Microsoft.Extensions.Configuration;

namespace DBADashMCP
{
    /// <summary>
    /// Reads the GUI's snapshot, and says whether the GUI is on the same repository as this server - an InstanceID
    /// from another repository would look up another instance's schema.
    /// </summary>
    internal sealed class GuiContextReader(IConfiguration config)
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private readonly string? _connectionString = config.GetConnectionString("Repository");
        private string? _fingerprint;

        public async Task<(GuiContextSnapshot? Snapshot, bool? SameRepository)> ReadAsync(CancellationToken cancellationToken)
        {
            var snapshot = await GuiContextPipe.TryReadAsync(Timeout, cancellationToken);
            if (snapshot is null)
            {
                return (null, null);
            }

            _fingerprint ??= RepositoryFingerprint.GetFingerprint(_connectionString);
            bool? same = snapshot.RepositoryFingerprint is null || _fingerprint is null
                ? null
                : string.Equals(snapshot.RepositoryFingerprint, _fingerprint, StringComparison.Ordinal);
            return (snapshot, same);
        }

        public const string NotOpen =
            "The DBA Dash GUI isn't open for this Windows user, or didn't answer.";

        /// <summary>Where an artifact came from, for the result's first line.</summary>
        public static string Source(GuiContextArtifact artifact) =>
            artifact.Instance is { } instance ? $"from {instance}"
            : artifact.FileName is { } file ? $"opened from {Path.GetFileName(file)}"
            : "from a source the GUI didn't record";

        /// <summary>
        /// The artifact's InstanceID, only when the GUI is known to be on this server's repository.  When that can't be told
        /// - a fingerprint couldn't be read - the ID may belong to another instance here, so it isn't used.
        /// </summary>
        public static int? InstanceId(GuiContextArtifact artifact, bool? sameRepository) =>
            sameRepository == true ? artifact.InstanceId : null;
    }

    /// <summary>
    /// The deadlock open in the GUI's deadlock viewer, described as get-deadlock describes a stored one.  Works for one
    /// opened from a file too: the GUI sends the graph, not just its key.
    /// </summary>
    internal sealed class GetOpenDeadlockTool(IConfiguration config, GuiContextReader gui) : RepositoryMcpTool(
        "get-open-deadlock",
        "The deadlock the user has open in the DBA Dash GUI's deadlock viewer, in the same detail as get-deadlock: " +
        "participants, objects and modules, DBA Dash's findings, object definitions where the instance is known, and " +
        "the graph.  Use it when the user means the deadlock they are looking at.",
        """
        {
          "type": "object",
          "properties": {
            "includeSchema": { "type": "boolean", "description": "Include the definitions of the objects involved.  Defaults to true." }
          },
          "additionalProperties": false
        }
        """,
        config)
    {
        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var includeSchema = args.Bool("includeSchema") ?? true;
            var (snapshot, same) = await gui.ReadAsync(cancellationToken);
            if (snapshot is null)
            {
                throw new McpToolException(GuiContextReader.NotOpen);
            }

            var open = snapshot.Deadlock ?? throw new McpToolException(
                "No deadlock is open in the DBA Dash GUI.  get-gui-context says which instance and time window it is showing.");

            IReadOnlyList<DeadlockGraph> graphs;
            try
            {
                graphs = DeadlockParser.Parse(open.Xml);
            }
            catch (DeadlockParseException ex)
            {
                throw new McpToolException($"The open deadlock graph could not be read: {ex.Message}");
            }

            if (graphs.Count == 0)
            {
                throw new McpToolException("The open deadlock graph has no deadlock in it.");
            }

            var heading = $"The deadlock open in the DBA Dash GUI (\"{open.Title}\"), {GuiContextReader.Source(open)}.  " +
                          $"DeadlockHash {open.DeadlockHash}.";
            if (same == false)
            {
                heading += "  The GUI is connected to a different repository from this server, so no object definitions are looked up.";
            }

            return await GetDeadlockTool.DescribeAsync(graphs[0], open.Instance, GuiContextReader.InstanceId(open, same),
                heading, includeSchema, sql, cancellationToken);
        }
    }

    /// <summary>
    /// The plan open in the GUI's plan viewer - the statement selected in it - described as get-query-plan describes a
    /// stored one.  Works for Query Store, live and file plans too: the GUI sends the plan, not just its hash.
    /// </summary>
    internal sealed class GetOpenQueryPlanTool(IConfiguration config, GuiContextReader gui) : RepositoryMcpTool(
        "get-open-query-plan",
        "The execution plan the user has open in the DBA Dash GUI's plan viewer - the statement selected in it - in the " +
        "same detail as get-query-plan.  Use it when the user means the plan they are looking at.",
        """
        {
          "type": "object",
          "properties": {
            "includePlanXml": { "type": "boolean", "description": "Include the showplan XML, which can be large.  Defaults to false." }
          },
          "additionalProperties": false
        }
        """,
        config)
    {
        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var includePlanXml = args.Bool("includePlanXml") ?? false;
            var (snapshot, same) = await gui.ReadAsync(cancellationToken);
            if (snapshot is null)
            {
                throw new McpToolException(GuiContextReader.NotOpen);
            }

            var open = snapshot.Plan ?? throw new McpToolException(
                "No query plan is open in the DBA Dash GUI.  get-gui-context says which instance and time window it is showing.");

            ExecutionPlan plan;
            try
            {
                plan = PlanParser.Parse(open.Xml);
            }
            catch (PlanParseException ex)
            {
                throw new McpToolException($"The open plan could not be read: {ex.Message}");
            }

            var statement = open.StatementIndex >= 0 && open.StatementIndex < plan.Statements.Count
                ? plan.Statements[open.StatementIndex]
                : plan.StatementsWithPlans.FirstOrDefault() ?? throw new McpToolException("The open plan has no statement with a plan in it.");

            var heading = $"The plan open in the DBA Dash GUI (\"{open.Title}\"), {GuiContextReader.Source(open)}." +
                          (open.QueryPlanHash is { } hash ? $"  query_plan_hash {hash}." : "");

            return GetQueryPlanTool.Describe(plan, statement, open.Instance, GuiContextReader.InstanceId(open, same),
                heading, includePlanXml, MaxOutputChars);
        }
    }
}
