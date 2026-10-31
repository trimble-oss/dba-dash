namespace DBADashAI.Models
{
    public class AiEvidenceItem
    {
        public string Source { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public int Rank { get; set; }

        public double Score { get; set; }
    }
}
