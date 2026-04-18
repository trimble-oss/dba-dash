namespace DBADashAI.Models;

public sealed class AiAskRequest
{
    public const int MaxQuestionLength = 2000;
    public const int MaxAllowedRows = 200;

    public string Question { get; set; } = string.Empty;

    public string? ToolName { get; set; }

    public bool IncludeAiSummary { get; set; } = true;

    public int MaxRows { get; set; } = 50;

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
