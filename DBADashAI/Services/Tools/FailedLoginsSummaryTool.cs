using DBADashAI.Models;

namespace DBADashAI.Services.Tools
{
    public class FailedLoginsSummaryTool : IAiTool
    {
        private readonly SqlToolExecutor _sql;

        public FailedLoginsSummaryTool(SqlToolExecutor sql)
        {
            _sql = sql;
        }

        public const string ToolName = "failed-logins-summary";

        public string Name => ToolName;

        public string Description => "Summarize failed login activity across instances, including counts per instance and the top offending logins and client hosts.";

        public string InputHint => "Use for questions about failed logins, login failures, authentication errors, error 18456, brute-force attempts, or which accounts/clients are failing to connect.";

        public string[] Keywords => ["failed login", "failed logins", "login failed", "login failure", "authentication", "auth failure", "18456", "bad password", "brute force", "cannot connect", "connect failed", "credential", "account locked", "unauthorized", "access denied", "security", "intrusion", "login error"];

        public async Task<AiToolResult> RunAsync(AiAskRequest request, CancellationToken cancellationToken)
        {
            var results = await _sql.QueryMultiAsync("AI.FailedLoginsRisk_Get", request.MaxRows, request.InstanceFilter, request.HoursBack, cancellationToken);
            return new AiToolResult
            {
                RowCount = results.Sum(r => r.Count),
                Data = new
                {
                    generatedUtc = DateTime.UtcNow,
                    instanceCounts = results.ElementAtOrDefault(0) ?? [],
                    topLogins = results.ElementAtOrDefault(1) ?? []
                },
                Evidence =
                [
                    new AiEvidenceItem
                    {
                        Source = "dbo.FailedLogins",
                        Detail = "Failed login counts per instance plus the top offending logins and client hosts within the time window."
                    }
                ]
            };
        }
    }
}
