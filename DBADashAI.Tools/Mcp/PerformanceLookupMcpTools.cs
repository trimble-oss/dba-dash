using System.Globalization;
using DBADashAI.Services;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// One of the AI service's performance lookups (see <see cref="PerformanceLookups"/>), offered over MCP: the detail
    /// behind what get-performance-summary shows - which session was blocking, which queries were waiting, what ran long,
    /// what one deadlock signature involved.  The built-in analysis caps its model at three lookups, because each is
    /// another model call; an MCP client makes its own calls and decides that for itself, so only the output is capped.
    /// </summary>
    public abstract class PerformanceLookupMcpTool(string toolName, string lookup, string description, string extraProperties, string? requiredArgument, IConfiguration? config)
        : RepositoryMcpTool(toolName, description, Schema(extraProperties, requiredArgument), config)
    {
        private const int MaxHours = 7 * 24;

        private static string Schema(string extraProperties, string? requiredArgument)
        {
            var required = requiredArgument is null ? "\"instance\"" : $"\"instance\", \"{requiredArgument}\"";
            return $$"""
            {
              "type": "object",
              "properties": {
                "instance": { "type": "string", "description": "The instance's DBA Dash display name.  instance-metadata-summary lists them." },
                "hoursBack": { "type": "integer", "minimum": 1, "maximum": {{MaxHours}}, "description": "How many hours the window covers, ending at to.  Defaults to 24.  Narrow it to a peak for the detail of that peak." },
                "to": { "type": "string", "description": "When the window ends, in UTC.  Defaults to now." }{{extraProperties}}
              },
              "required": [{{required}}],
              "additionalProperties": false
            }
            """;
        }

        /// <summary>The lookup's own arguments, read and checked, and how the result's first line describes them.</summary>
        protected virtual (string? WaitType, bool TempDBOnly, string? Signature, string Describes) ReadArguments(Arguments args) =>
            (null, false, null, string.Empty);

        protected abstract string Title { get; }

        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var instanceName = args.String("instance", required: true)!;
            var hours = args.Int("hoursBack", 1, MaxHours) ?? 24;
            var toUtc = args.UtcDateTime("to") ?? DateTime.UtcNow;
            var fromUtc = toUtc.AddHours(-hours);
            var (waitType, tempDBOnly, signature, describes) = ReadArguments(args);

            var (instanceId, instance) = await ResolveInstanceAsync(sql, instanceName, cancellationToken);
            var sets = await sql.QueryMultiAsync(PerformanceLookups.Procedure(lookup),
                PerformanceLookups.Parameters(lookup, instanceId, fromUtc, toUtc, waitType, tempDBOnly, signature), cancellationToken);

            var summary = $"{Title}{describes} on {instance}, {Utc(fromUtc)} to {Utc(toUtc)} UTC";
            return PerformanceLookups.Format(summary, sets, lookup, Utc, "Times are UTC.", timesAreLocal: false, MaxOutputChars);
        }

        private static string Utc(DateTime time) => time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public sealed class GetRootBlockersTool(IConfiguration? config) : PerformanceLookupMcpTool(
        "get-root-blockers",
        PerformanceLookups.Blocking,
        "The root blockers in an instance's worst blocking snapshots over a window - what each was running, which " +
        "application and login, and whether it was sleeping with a transaction open - and what the sessions it blocked " +
        "waited on.  Use it to name the cause of blocking or LCK_M waits rather than guessing from the totals.  From the " +
        "running queries snapshots, so a blocker is seen as it was at that moment.",
        "",
        null,
        config)
    {
        protected override string Title => "Root blockers";
    }

    public sealed class GetWaitingQueriesTool(IConfiguration? config) : PerformanceLookupMcpTool(
        "get-waiting-queries",
        PerformanceLookups.WaitingQueries,
        "The queries seen waiting on a wait type on an instance over a window, most sessions first - the statement, " +
        "database, application and how long each had waited.  Use it to tie a wait in get-performance-summary or " +
        "waits-summary to the queries behind it: PAGELATCH% with tempdbOnly for tempdb contention, LCK_M% for lock waits.  " +
        "From the running queries snapshots.",
        """
        ,
                "waitType": { "type": "string", "description": "A wait type, or a LIKE pattern such as PAGELATCH% or LCK_M%." },
                "tempdbOnly": { "type": "boolean", "description": "Only waits on tempdb pages - for telling tempdb contention from contention elsewhere.  Defaults to false." }
        """,
        "waitType",
        config)
    {
        protected override string Title => "Queries waiting";

        protected override (string? WaitType, bool TempDBOnly, string? Signature, string Describes) ReadArguments(Arguments args)
        {
            var waitType = args.String("waitType", required: true)!.ToUpperInvariant();
            if (!PerformanceLookups.IsWaitType(waitType))
            {
                throw new McpToolException("waitType must be a wait type, or a pattern of one such as PAGELATCH%.");
            }

            var tempdb = args.Bool("tempdbOnly") ?? false;
            return (waitType, tempdb, null, $" on {waitType}{(tempdb ? " in tempdb" : "")}");
        }
    }

    public sealed class GetLongRunningQueriesTool(IConfiguration? config) : PerformanceLookupMcpTool(
        "get-long-running-queries",
        PerformanceLookups.LongRunning,
        "The longest running queries and the longest open transactions seen on an instance over a window - the statement, " +
        "session, login, application and how long each had been running when seen.  Use it to name a query or an old " +
        "open transaction that could be holding locks or log.  From the running queries snapshots, so a duration is how " +
        "long it had run when the snapshot was taken.",
        "",
        null,
        config)
    {
        protected override string Title => "Long running queries and transactions";
    }

    public sealed class GetDeadlockSignatureTool(IConfiguration? config) : PerformanceLookupMcpTool(
        "get-deadlock-signature",
        PerformanceLookups.Deadlock,
        "One deadlock pattern on an instance over a window: how often its signature occurred, and the processes and " +
        "resources of its latest occurrences - the statements, procedures, objects and locks involved.  A signature that " +
        "recurs is one problem to fix.  Signatures are in list-deadlocks and get-performance-summary's Deadlocks section; " +
        "for one occurrence's whole graph and findings use get-deadlock.",
        """
        ,
                "signature": { "type": "string", "description": "The deadlock signature, 0x followed by 16 hex digits." }
        """,
        "signature",
        config)
    {
        protected override string Title => "Deadlock signature";

        protected override (string? WaitType, bool TempDBOnly, string? Signature, string Describes) ReadArguments(Arguments args)
        {
            var signature = args.Hex("signature", 8, required: true)!;
            return (null, false, signature, $" {signature}");
        }
    }
}
