using System.Text;
using DBADash.PerformanceAnalysis;
using DBADashAI.Services;

namespace DBADashAI.Mcp
{
    /// <summary>
    /// One instance's performance over a window, compared with the same window a week earlier - what the
    /// GUI's AI performance analysis sends, read with the same loader from the procedures behind the charts.
    /// The text is <see cref="PerformanceAnalysisPrompt.Data"/>.  Times are UTC, as everything else over MCP.
    /// </summary>
    public sealed class GetPerformanceSummaryTool(IConfiguration? config) : RepositoryMcpTool(
        "get-performance-summary",
        "One instance's performance over a window of up to 7 days, compared with the same window a week earlier: " +
        "CPU, waits, IO by drive and database, performance counters, memory, blocking, object execution, and latch " +
        "and spinlock statistics, with time series where they help tie peaks together.  Focus on latches-and-" +
        "spinlocks for contention detail.  Times are UTC.",
        $$"""
        {
          "type": "object",
          "properties": {
            "instance": { "type": "string", "description": "The instance's DBA Dash display name.  instance-metadata-summary lists them." },
            "hoursBack": { "type": "integer", "minimum": 1, "maximum": {{MaxHours}}, "description": "How many hours the window covers, ending at to.  Defaults to 24." },
            "to": { "type": "string", "description": "When the window ends, in UTC.  Defaults to now." },
            "focus": { "type": "string", "enum": ["general", "latches-and-spinlocks"], "description": "general (the default) or latches-and-spinlocks." },
            "includeBaseline": { "type": "boolean", "description": "Compare with the same window a week earlier.  Defaults to true." }
          },
          "required": ["instance"],
          "additionalProperties": false
        }
        """,
        config)
    {
        private const int MaxHours = 7 * 24;

        protected override async Task<string> RunAsync(Arguments args, SqlToolExecutor sql, CancellationToken cancellationToken)
        {
            var name = args.String("instance", required: true)!;
            var hours = args.Int("hoursBack", 1, MaxHours) ?? 24;
            var toUtc = args.UtcDateTime("to") ?? DateTime.UtcNow;
            var focus = args.String("focus") switch
            {
                null or "general" => PerformanceAnalysisFocus.General,
                "latches-and-spinlocks" => PerformanceAnalysisFocus.LatchesAndSpinlocks,
                var other => throw new McpToolException($"focus must be general or latches-and-spinlocks, not '{other}'.")
            };

            var (instanceId, instance) = await ResolveInstanceAsync(sql, name, cancellationToken);
            var window = PerformanceAnalysisWindow.Create(toUtc.AddHours(-hours), toUtc);
            var data = await PerformanceAnalysisLoader.LoadAsync(sql.ConnectionString, sql.CommandTimeoutSeconds,
                instanceId, instance, window, cancellationToken);

            var options = PerformanceAnalysisOptions.Defaults(focus);
            options.IncludeBaseline = args.Bool("includeBaseline") ?? true;

            var payload = PerformanceAnalysisPayload.Build(data, options, utc => utc, "UTC");

            var text = new StringBuilder();
            text.AppendLine($"Focus: {PerformanceAnalysisPayload.DescribeFocus(focus)}");
            text.Append(PerformanceAnalysisPrompt.Data(payload.Instance, payload.Window, payload.Baseline, payload.Notes,
                payload.Sections.Select(s => (s.Title, s.Content))));
            return text.ToString();
        }
    }
}
