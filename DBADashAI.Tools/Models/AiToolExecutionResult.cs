using System.Text.Json;

namespace DBADashAI.Models
{
    public class AiToolExecutionResult
    {
        public string Tool { get; set; } = string.Empty;

        public JsonElement Data { get; set; }

        public List<AiEvidenceItem> Evidence { get; set; } = [];

        public int RowCount { get; set; }

        public long ExecutionMs { get; set; }
    }
}
