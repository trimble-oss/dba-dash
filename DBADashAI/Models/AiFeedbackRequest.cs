namespace DBADashAI.Models
{
    public class AiFeedbackRequest
    {
        public string RequestId { get; set; } = string.Empty;

        public bool IsHelpful { get; set; }

        public string? Category { get; set; }

        public string? Comment { get; set; }

        /// <summary>
        /// The tool name from <see cref="AiAskResponse.Tool"/>. Stored so feedback can
        /// be correlated back to the routing decision that produced the response.
        /// </summary>
        public string? ToolName { get; set; }

        /// <summary>
        /// First 120 characters of the original question. Stored to help diagnose
        /// which question patterns lead to unhelpful routing decisions.
        /// </summary>
        public string? QuestionExcerpt { get; set; }

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(RequestId))
            {
                return "RequestId is required.";
            }

            if (!string.IsNullOrWhiteSpace(Comment) && Comment.Length > 1000)
            {
                return "Comment exceeds maximum length of 1000 characters.";
            }

            return null;
        }
    }
}
