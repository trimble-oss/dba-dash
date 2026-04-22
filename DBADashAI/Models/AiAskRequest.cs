namespace DBADashAI.Models
{
    public class AiAskRequest
    {
        public const int MaxQuestionLength = 2000;
        public const int MaxAllowedRows = 200;

        public string Question { get; set; } = string.Empty;

        public string? ToolName { get; set; }

        public bool IncludeAiSummary { get; set; } = true;

        public int MaxRows { get; set; } = 50;

        /// <summary>Extracted or explicit instance name filter. Null = all instances.</summary>
        public string? InstanceFilter { get; set; }

        /// <summary>How many hours of data to look back. Null = SP default (typically 24).</summary>
        public int? HoursBack { get; set; }

        /// <summary>Override the configured AI model for this request. Null = use configured default.</summary>
        public string? ModelOverride { get; set; }

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                return "Question is required.";
            }

            if (Question.Length > MaxQuestionLength)
            {
                return $"Question exceeds maximum length of {MaxQuestionLength} characters.";
            }

            if (MaxRows < 1 || MaxRows > MaxAllowedRows)
            {
                return $"MaxRows must be between 1 and {MaxAllowedRows}.";
            }

            return null;
        }
    }
}
