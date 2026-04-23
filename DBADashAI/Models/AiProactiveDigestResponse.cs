namespace DBADashAI.Models
{
    public class AiProactiveDigestResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public DateTime GeneratedUtc { get; set; }

        public List<AiRiskForecastItem> Risks { get; set; } = [];

        public string? Summary { get; set; }

        public double ConfidenceScore { get; set; }

        public string ConfidenceLabel { get; set; } = "Low";
    }
}
