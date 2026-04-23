using System.Diagnostics;

namespace DBADashAI.Services
{
    public class AiRequestTelemetryService
    {
        private readonly ILogger<AiRequestTelemetryService> _logger;

        public AiRequestTelemetryService(ILogger<AiRequestTelemetryService> logger)
        {
            _logger = logger;
        }

        public Stopwatch Start(string requestId, string question, string? toolName)
        {
            _logger.LogInformation("AI request {RequestId} started. Tool={ToolName}, QuestionLength={QuestionLength}",
                requestId,
                toolName ?? "auto",
                question?.Length ?? 0);

            return Stopwatch.StartNew();
        }

        public void Complete(string requestId, string toolName, int rowCount, long toolMs, long totalMs, double confidenceScore, string confidenceLabel)
        {
            _logger.LogInformation("AI request {RequestId} completed. Tool={ToolName}, RowCount={RowCount}, ToolMs={ToolMs}, TotalMs={TotalMs}, ConfidenceScore={ConfidenceScore}, ConfidenceLabel={ConfidenceLabel}",
                requestId,
                toolName,
                rowCount,
                toolMs,
                totalMs,
                confidenceScore,
                confidenceLabel);
        }

        public void Fail(string requestId, Exception ex)
        {
            _logger.LogError(ex, "AI request {RequestId} failed.", requestId);
        }
    }
}
