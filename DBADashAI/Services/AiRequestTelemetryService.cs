using System.Diagnostics;

namespace DBADashAI.Services
{
    public class AiRequestTelemetryService
    {
        private const int QuestionExcerptLength = 80;
        private readonly ILogger<AiRequestTelemetryService> _logger;

        public AiRequestTelemetryService(ILogger<AiRequestTelemetryService> logger)
        {
            _logger = logger;
        }

        public Stopwatch Start(string requestId, string question, string? toolName)
        {
            _logger.LogInformation(
                "AI request {RequestId} started. Tool={ToolName}, QuestionLength={QuestionLength}",
                requestId,
                toolName ?? "auto",
                question?.Length ?? 0);

            // Question content logged at Debug only — it may contain server names, database
            // names, or other infrastructure details. Enable Debug logging for DBADashAI in
            // appsettings.json to include it during diagnostics.
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "AI request {RequestId} question excerpt: {QuestionExcerpt}",
                    requestId,
                    GetExcerpt(question));
            }

            return Stopwatch.StartNew();
        }

        public void Complete(string requestId, string toolName, int rowCount, long toolMs, long totalMs, double confidenceScore, string confidenceLabel)
        {
            _logger.LogInformation(
                "AI request {RequestId} completed. Tool={ToolName}, RowCount={RowCount}, ToolMs={ToolMs}, TotalMs={TotalMs}, ConfidenceScore={ConfidenceScore}, ConfidenceLabel={ConfidenceLabel}",
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

        /// <summary>
        /// Returns the first <see cref="QuestionExcerptLength"/> characters of the question
        /// with a trailing ellipsis when truncated. Whitespace is normalised so multi-line
        /// questions produce a readable single-line log entry.
        /// </summary>
        private static string GetExcerpt(string? question)
        {
            if (string.IsNullOrWhiteSpace(question)) return string.Empty;
            var normalised = System.Text.RegularExpressions.Regex.Replace(question.Trim(), @"\s+", " ");
            return normalised.Length <= QuestionExcerptLength
                ? normalised
                : normalised[..QuestionExcerptLength] + "…";
        }
    }
}
