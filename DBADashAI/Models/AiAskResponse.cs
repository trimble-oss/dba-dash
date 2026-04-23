using System.Text.Json;

namespace DBADashAI.Models
{
    public class AiAskResponse
    {
        public string RequestId { get; set; } = string.Empty;

        public string Tool { get; set; } = string.Empty;

        /// <summary>
        /// The raw tool data as a JSON element. Single-tool responses contain the tool's
        /// result object directly; multi-tool responses wrap all results under a "tools" array.
        /// The schema matches the tool name returned in <see cref="Tool"/>.
        /// </summary>
        public JsonElement Data { get; set; }

        public string? Summary { get; set; }

        public List<AiEvidenceItem> Evidence { get; set; } = [];

        public long ToolExecutionMs { get; set; }

        public long TotalExecutionMs { get; set; }

        public double ConfidenceScore { get; set; }

        public string ConfidenceLabel { get; set; } = "Low";
    }
}
