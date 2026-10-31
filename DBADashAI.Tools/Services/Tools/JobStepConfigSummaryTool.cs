using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class JobStepConfigSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public JobStepConfigSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "job-step-config-summary";

        public string Name => ToolName;

        public string Description => "Summarize SQL Agent job step configuration (on-failure action, retry attempts/interval, subsystem, database, output file) for jobs in failure status, to explain why steps may be silently failing or not retrying.";

        public string InputHint => "Use for questions about job step configuration, why a failing step does not fail the job, retry settings, continue-on-failure behavior, or silently failing job steps.";

        public string[] Keywords => ["job step", "job steps", "step config", "step configuration", "on failure", "on fail", "continue on failure", "retry", "retries", "retry attempts", "silently failing", "silent failure", "quit with failure", "go to next step", "step setup", "job setup", "step action", "ignore failure"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var rows = await _sql.QueryAsync("AI.JobStepConfig_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
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
                        Source = "dbo.JobSteps + dbo.AgentJobStatus",
                        Detail = "Step-level configuration (on-fail action, retry settings) for jobs in warning/critical status or with recent step failures."
                    }
                ]
            };
        }
    }
}
