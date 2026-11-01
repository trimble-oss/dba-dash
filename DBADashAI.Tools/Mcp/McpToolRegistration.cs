using DBADashAI.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// What both MCP hosts - the AI service's /mcp endpoint and the local DBADashMCP.exe - serve, so the
    /// two can't drift apart.  Each host adds its own transport.
    /// </summary>
    public static class McpToolRegistration
    {
        public const string ServerName = "dbadash";

        /// <summary>
        /// Lets describe-deadlock-graph and describe-query-plan read a file by its path.  The local server turns it on;
        /// the AI service leaves it off, where a path would be read on the service's machine with the service's account.
        /// </summary>
        public const string AllowFilePathsSetting = "Mcp:AllowFilePaths";

        public const string ServerInstructions =
            "DBA Dash monitors a fleet of SQL Server instances and keeps what it collects in a repository " +
            "database.  These tools read summaries from that repository - they never connect to the monitored " +
            "instances and change nothing.  Instance names are DBA Dash display names: use " +
            "instance-metadata-summary to see them, then pass one (or the start of several) as instance to " +
            "narrow another tool.  Start with active-alerts-summary or reliability-risk-summary for a general " +
            "health check, and top-queries for the heaviest queries.  For one deadlock, plan or instance's performance in " +
            "detail use list-deadlocks then get-deadlock, top-queries or running-queries-summary then get-query-plan, or " +
            "get-performance-summary - then get-root-blockers, get-waiting-queries, get-long-running-queries and " +
            "get-deadlock-signature for the queries behind what it shows.  The analyse-* prompts walk through each.  " +
            "describe-deadlock-graph and " +
            "describe-query-plan analyse XML the user already has.  Times are UTC; the tools that offer a to argument " +
            "can look at a past window, which is how to investigate an incident after the fact.  Results are capped; " +
            "when one says it hit maxRows or was trimmed, ask for a single instance or a shorter window.  A permission " +
            "error is for the user to fix - say what it says rather than retrying.  Results may be masked: ? for a literal, " +
            "(value A) for a parameter value (the same placeholder is the same value), [withheld] for text left out, and " +
            "login#..., host#... and app#... codes for logins, hosts and applications - these are masking, not part of " +
            "the query, so don't read them as its values or quote them back as if they were.  Nothing here can make a " +
            "change: when the user asks for one - an index, a setting, a code fix - write it out for them to review and run.";

        /// <summary>Names and describes the server to the client.</summary>
        public static void Configure(McpServerOptions options, string? version, AiDataMask? mask = null) =>
            Configure(options, version, AiDataMaskSource.Fixed(mask ?? AiDataMask.Default));

        /// <summary>Names and describes the server, masking each call as <paramref name="masks"/> says when the call starts.</summary>
        public static void Configure(McpServerOptions options, string? version, AiDataMaskSource masks)
        {
            options.ServerInfo = new Implementation
            {
                Name = ServerName,
                Title = "DBA Dash",
                Version = version ?? "unknown"
            };
            options.ServerInstructions = ServerInstructions;

            // The tools mask their data at the source (see RepositoryMcpTool.Mask).  This is the backstop: every tool's text
            // goes through the text filter too, so XML or JSON a tool passes on unmasked is still caught.  The mask is read
            // once, here, and the tools read the same one (AiDataMaskSource.ForCall).
            options.Filters.Request.CallToolFilters.Add(next => async (context, cancellationToken) =>
            {
                var dataMask = masks.Current;
                AiDataMaskSource.BeginCall(dataMask);
                var result = await next(context, cancellationToken);
                if (!dataMask.IsNone)
                {
                    foreach (var block in result.Content.OfType<TextContentBlock>())
                    {
                        block.Text = AiDataMasker.FilterText(block.Text, dataMask);
                    }
                }
                return result;
            });
        }

        /// <summary>
        /// Summary tools offered over MCP but not to /ask, whose router would otherwise start picking them.
        /// </summary>
        public static readonly IReadOnlyList<Type> McpOnlyToolTypes =
        [
            typeof(DeadlockListTool)
        ];

        /// <summary>
        /// Everything the MCP server offers: an adapter for each repository summary tool, the tools that fetch
        /// one deadlock, plan or instance's performance, and the analysis prompts.  The summary tools themselves
        /// come from <see cref="AiToolRegistration.AddAiTools"/>, which the AI service registers anyway for /ask.
        /// The MCP server picks up every <see cref="McpServerTool"/> and <see cref="McpServerPrompt"/> in the
        /// container, so this works with any transport.
        /// </summary>
        /// <param name="guiContext">True for the local server, which also offers the tools that read the GUI - see <see cref="AnalysisMcpPrompt.All"/>.</param>
        public static IServiceCollection AddMcpTools(this IServiceCollection services, bool guiContext = false)
        {
            foreach (var type in AiToolRegistration.ToolTypes.Concat(McpOnlyToolTypes))
            {
                services.AddSingleton<McpServerTool>(sp => AiToolMcpAdapter.Create(sp, type));
            }

            services.AddSingleton<McpServerTool, GetDeadlockTool>();
            services.AddSingleton<McpServerTool, GetQueryPlanTool>();
            services.AddSingleton<McpServerTool, GetPerformanceSummaryTool>();
            services.AddSingleton<McpServerTool, TopQueriesTool>();
            services.AddSingleton<McpServerTool, DescribeDeadlockGraphTool>();
            services.AddSingleton<McpServerTool, DescribeQueryPlanTool>();
            services.AddSingleton<McpServerTool, GetRootBlockersTool>();
            services.AddSingleton<McpServerTool, GetWaitingQueriesTool>();
            services.AddSingleton<McpServerTool, GetLongRunningQueriesTool>();
            services.AddSingleton<McpServerTool, GetDeadlockSignatureTool>();

            foreach (var prompt in AnalysisMcpPrompt.All(guiContext))
            {
                services.AddSingleton<McpServerPrompt>(prompt);
            }

            return services;
        }
    }
}
