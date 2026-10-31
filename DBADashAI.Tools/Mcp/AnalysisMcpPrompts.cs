using System.Text;
using System.Text.Json;
using DBADash.PerformanceAnalysis;
using DBADashAI.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// The built-in analyses, as MCP prompts: a user picks one in their client (in Claude Code, typing
    /// /mcp__dbadash__analyse-deadlock), and the client's model fetches the data with the matching tool and answers
    /// under the same headings the AI service's analysis does.  The headings and guidance come from the same
    /// builders, so the two don't drift apart; only the framing differs, because here the model has to fetch the
    /// data itself and nothing is already on the reader's screen.
    /// </summary>
    public sealed class AnalysisMcpPrompt(
        string name,
        string title,
        string description,
        IReadOnlyList<PromptArgument> arguments,
        Func<IReadOnlyDictionary<string, string>, string> build) : McpServerPrompt
    {
        public override Prompt ProtocolPrompt { get; } = new()
        {
            Name = name,
            Title = title,
            Description = description,
            Arguments = arguments.ToList()
        };

        public override IReadOnlyList<object> Metadata => [];

        public override ValueTask<GetPromptResult> GetAsync(
            RequestContext<GetPromptRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new GetPromptResult
            {
                Description = ProtocolPrompt.Description,
                Messages =
                [
                    new PromptMessage
                    {
                        Role = Role.User,
                        Content = new TextContentBlock { Text = Render(request.Params?.Arguments) }
                    }
                ]
            });
        }

        /// <summary>The prompt's text for these arguments.  A blank argument counts as left out.</summary>
        public string Render(IDictionary<string, JsonElement>? arguments)
        {
            var values = (arguments ?? new Dictionary<string, JsonElement>())
                .Where(a => a.Value.ValueKind != JsonValueKind.Null)
                .ToDictionary(a => a.Key, a => a.Value.ValueKind == JsonValueKind.String ? a.Value.GetString() ?? "" : a.Value.GetRawText())
                .Where(a => !string.IsNullOrWhiteSpace(a.Value))
                .ToDictionary(a => a.Key, a => a.Value.Trim());

            return build(values);
        }

        /// <param name="guiContext">
        /// True for the local server, which can see the GUI (get-gui-context, get-open-deadlock, get-open-query-plan).
        /// Without arguments its prompts then start from what the user has open, rather than from the most recent.
        /// </param>
        public static IEnumerable<AnalysisMcpPrompt> All(bool guiContext = false) =>
        [
            new(
                "analyse-deadlock",
                "Analyse a deadlock",
                "Analyse a deadlock DBA Dash captured: what happened, why, what to do, and what to check next.  " +
                (guiContext
                    ? "Without arguments it analyses the deadlock open in the DBA Dash GUI, or else the most recent on " +
                      "the instance and in the time window the GUI shows, or else the most recent anywhere."
                    : "Without arguments it analyses the most recent one."),
                [
                    Argument("instance", "Only deadlocks on this instance."),
                    Argument("instanceId", "InstanceID of a specific deadlock, from list-deadlocks."),
                    Argument("eventTime", "EventTime of a specific deadlock, from list-deadlocks."),
                    Argument("deadlockHash", "DeadlockHash of a specific deadlock, from list-deadlocks."),
                    .. FileArguments(guiContext, "A deadlock graph file (.xdl) to analyse instead - with instance, the instance it happened on, for the object definitions.")
                ],
                args => Deadlock(args, guiContext)),
            new(
                "analyse-query-plan",
                "Analyse a query plan",
                "Analyse a captured execution plan: what it does, where the time goes, what to do, and what to check next.  " +
                (guiContext
                    ? "Without arguments it analyses the plan open in the DBA Dash GUI, or else the heaviest query's plan " +
                      "on the instance and in the time window the GUI shows, or else across every instance."
                    : "Without arguments it looks for the heaviest query's plan."),
                [
                    Argument("queryPlanHash", "The plan's query_plan_hash, from top-queries or running-queries-summary."),
                    Argument("instance", "The instance it ran on."),
                    .. FileArguments(guiContext, "A plan file (.sqlplan) to analyse instead."),
                    .. (guiContext ? [Argument("statement", "With path: which statement with a plan, numbered from 1.")] : Array.Empty<PromptArgument>())
                ],
                args => Plan(args, guiContext)),
            new(
                "analyse-performance",
                "Analyse an instance's performance",
                "Analyse an instance's performance over a window against the same window a week earlier." +
                (guiContext ? "  Without arguments it uses the instance and time window the DBA Dash GUI shows." : ""),
                [
                    Argument("instance", "The instance's DBA Dash display name.", required: !guiContext),
                    Argument("hoursBack", "How many hours to look at, up to 168.  Defaults to 24."),
                    Argument("focus", "general (the default) or latches-and-spinlocks."),
                    Argument("to", "When the window ends, in UTC.  Defaults to now.")
                ],
                args => Performance(args, guiContext))
        ];

        /// <summary>
        /// A file to analyse, on the local server only: describe-deadlock-graph and describe-query-plan read a path there,
        /// and the GUI's "Ask Claude Code" hands its open deadlock or plan over as one.
        /// </summary>
        private static PromptArgument[] FileArguments(bool guiContext, string description) =>
            guiContext ? [Argument("path", description)] : [];

        private static PromptArgument Argument(string name, string description, bool required = false) => new()
        {
            Name = name,
            Description = description,
            Required = required
        };

        private const string NoDeadlocks =
            "If list-deadlocks is empty, say so: the Deadlocks collection may not be enabled, and deadlocks-summary will " +
            "show whether any happened.";

        private static string Deadlock(IReadOnlyDictionary<string, string> args, bool guiContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Analyse a SQL Server deadlock that DBA Dash captured.");
            prompt.AppendLine();

            if (guiContext && args.TryGetValue("path", out var path))
            {
                var on = args.TryGetValue("instance", out var from) ? $", instance {from} (for the object definitions)" : string.Empty;
                prompt.AppendLine($"Fetch it with the describe-deadlock-graph tool: path {path}{on}.");
            }
            else if (args.TryGetValue("instanceId", out var id) && args.TryGetValue("eventTime", out var time) &&
                args.TryGetValue("deadlockHash", out var hash))
            {
                prompt.AppendLine($"Fetch it with the get-deadlock tool: instanceId {id}, eventTime {time}, deadlockHash {hash}.");
            }
            else if (args.TryGetValue("instance", out var instance))
            {
                prompt.AppendLine(
                    $"Find the most recent deadlock on {instance} with the list-deadlocks tool (widen hoursBack if there are " +
                    "none in the last day), then fetch it with get-deadlock.");
                prompt.AppendLine(NoDeadlocks);
            }
            else if (guiContext)
            {
                prompt.AppendLine("Work out which deadlock I mean, in this order, stopping at the first that gives one:");
                prompt.AppendLine("1. The deadlock open in the DBA Dash GUI: call get-open-deadlock.");
                prompt.AppendLine(
                    "2. The most recent deadlock on the instance and in the time window the GUI shows: call get-gui-context, " +
                    "then list-deadlocks with its instance and hoursBack, and fetch the newest with get-deadlock.  If the GUI " +
                    "shows a folder of several instances rather than one, call list-deadlocks without instance.");
                prompt.AppendLine(
                    "3. The most recent deadlock anywhere: list-deadlocks without instance, widening hoursBack if there are " +
                    "none in the last day, then get-deadlock.");
                prompt.AppendLine("Start the answer by saying which deadlock this is and which of those steps found it.");
                prompt.AppendLine(NoDeadlocks);
            }
            else
            {
                prompt.AppendLine(
                    "Find the most recent deadlock with the list-deadlocks tool (widen hoursBack if there are none in the " +
                    "last day), then fetch it with get-deadlock.");
                prompt.AppendLine(NoDeadlocks);
            }

            AppendAnswerShape(prompt, AiDeadlockPromptBuilder.AnswerSections);
            prompt.AppendLine(
                "The graph is what SQL Server observed, not what the application intended.  Where you are inferring " +
                "intent, say so.  The tool result includes findings from DBA Dash's own deadlock rules: build on them " +
                "rather than repeating them, and say so if you disagree with one.  The object definitions are from " +
                "schema snapshots - the last known state before the deadlock.  Do not invent object or index names " +
                "that are not present.");
            return prompt.ToString();
        }

        private const string FromRunningQueries =
            "top-queries, picking the heaviest query whose plan is stored (PlanStored true), then fetch it with get-query-plan " +
            "by its PlanHash - or, where Query Stats isn't collected, running-queries-summary's longest running query that " +
            "has a query_plan_hash";

        private static string Plan(IReadOnlyDictionary<string, string> args, bool guiContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Analyse a SQL Server execution plan that DBA Dash captured.");
            prompt.AppendLine();

            args.TryGetValue("instance", out var instance);

            if (guiContext && args.TryGetValue("path", out var path))
            {
                var which = args.TryGetValue("statement", out var statement) ? $", statement {statement}" : string.Empty;
                prompt.AppendLine($"Fetch it with the describe-query-plan tool: path {path}{which}.");
            }
            else if (args.TryGetValue("queryPlanHash", out var hash))
            {
                var on = instance is null ? string.Empty : $", instance {instance}";
                prompt.AppendLine($"Fetch it with the get-query-plan tool: queryPlanHash {hash}{on}.");
            }
            else if (instance is not null)
            {
                prompt.AppendLine($"Find a plan worth looking at on {instance} with {FromRunningQueries}.");
            }
            else if (guiContext)
            {
                prompt.AppendLine("Work out which plan I mean, in this order, stopping at the first that gives one:");
                prompt.AppendLine("1. The plan open in the DBA Dash GUI: call get-open-query-plan.");
                prompt.AppendLine(
                    $"2. On the instance and in the time window the GUI shows: call get-gui-context, then {FromRunningQueries}.");
                prompt.AppendLine($"3. Across every instance, over the last day: {FromRunningQueries}.");
                prompt.AppendLine("Start the answer by saying which plan this is and which of those steps found it.");
            }
            else
            {
                prompt.AppendLine($"Find a plan worth looking at with {FromRunningQueries}.");
            }

            prompt.AppendLine("Ask for the plan XML only if the summary isn't enough.");
            AppendAnswerShape(prompt, AiPlanPromptBuilder.AnswerSections);
            prompt.AppendLine(
                "The tool result says whether this is an actual or an estimated plan - an estimated plan has no " +
                "measurements, so anything about what actually happened is inference.  Build on the warnings DBA " +
                "Dash's plan rules raised rather than repeating them.  A missing index the optimizer asked for is a " +
                "suggestion from a cost model that saw one statement, not a recommendation - say what it would cost " +
                "elsewhere.  Do not invent object, index or column names that are not present.");
            return prompt.ToString();
        }

        private static string Performance(IReadOnlyDictionary<string, string> args, bool guiContext)
        {
            var latches = args.TryGetValue("focus", out var focus) && focus == "latches-and-spinlocks";
            var prompt = new StringBuilder();

            if (!args.ContainsKey("instance") && guiContext)
            {
                prompt.AppendLine(
                    "Call get-gui-context to find the instance and time window the DBA Dash GUI is showing - if it shows a " +
                    "folder rather than one instance, or the GUI isn't open, ask me which instance.  Then fetch the figures " +
                    $"for them with the get-performance-summary tool{(latches ? ", focus latches-and-spinlocks" : "")}.  Times are UTC.");
            }
            else
            {
                var call = new List<string> { $"instance {args.GetValueOrDefault("instance", "(ask me which)")}" };
                if (args.TryGetValue("hoursBack", out var hours)) call.Add($"hoursBack {hours}");
                if (args.TryGetValue("to", out var to)) call.Add($"to {to}");
                if (latches) call.Add("focus latches-and-spinlocks");

                prompt.AppendLine($"Fetch the figures with the get-performance-summary tool: {string.Join(", ", call)}.  Times are UTC.");
            }

            prompt.AppendLine();
            prompt.Append(PerformanceAnalysisPrompt.Instructions(latches));
            prompt.AppendLine();
            prompt.AppendLine(
                "You can look further before you answer, for the same instance and window - narrow hoursBack and to onto a " +
                "peak for its detail.  Where the figures point at blocking, tempdb or lock waits, a long query or a recurring " +
                "deadlock, and knowing which queries are involved would change what you recommend, look them up and name them " +
                "rather than sending me to find out: get-root-blockers before naming the cause of lock waits, get-waiting-queries " +
                "with waitType PAGELATCH% and tempdbOnly before naming the cause of tempdb contention, get-long-running-queries " +
                "for a long query or old open transaction, and get-deadlock-signature for a signature in the Deadlocks section.  " +
                "Say in the answer what the lookups showed.");
            return prompt.ToString();
        }

        private static void AppendAnswerShape(StringBuilder prompt, IEnumerable<string> sections)
        {
            prompt.AppendLine();
            prompt.AppendLine("Answer in Markdown, using these sections as level 2 headings, in this order.  Keep to short " +
                              "paragraphs and lists.  Use code formatting for object names, statements and settings.");
            foreach (var section in sections)
            {
                prompt.AppendLine(section);
            }

            prompt.AppendLine();
        }
    }
}
