using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class AgentJobAlertsTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public AgentJobAlertsTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "agent-job-alerts";

        public string Name => ToolName;

        public string Description => "List agent job alerts and job status details including fail counts, last success/failure, duration stats, and step failures.";

        public string InputHint => "Use for questions about failed jobs, job alerts, job status, job history, job duration, SQL Agent incidents, or which jobs are failing.";

        public string[] Keywords => ["job", "jobs", "agent", "agent job", "failed", "failure", "failing", "job fail", "schedule", "scheduled", "running long", "stuck", "hung", "executing", "step", "job history", "sql agent", "sqlagent", "maintenance plan", "job status", "job duration", "step fail"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("DBADash.AI_AgentJobAlerts_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    jobAlerts = results.ElementAtOrDefault(0) ?? [],
                    jobStatus = results.ElementAtOrDefault(1) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "Alert.ActiveAlerts + dbo.AgentJobStatus",
                        Detail = "Agent job alerts and job status details with fail counts, durations, and step failure info for jobs in warning/critical status."
                    }
                ]
            };
        }
    }
}
