namespace DBADashAI.Models
{
    public class AiRiskForecastItem
    {
        public string Category { get; set; } = string.Empty;

        public string Severity { get; set; } = "Info";

        public string Title { get; set; } = string.Empty;

        public string Evidence { get; set; } = string.Empty;

        public string RecommendedAction { get; set; } = string.Empty;

        public double ConfidenceScore { get; set; }

        public string? RunbookUrl { get; set; }
    }
}
