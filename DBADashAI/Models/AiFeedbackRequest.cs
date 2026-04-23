namespace DBADashAI.Models
{
    public class AiFeedbackRequest
    {
        public string RequestId { get; set; } = string.Empty;

        public bool IsHelpful { get; set; }

        public string? Category { get; set; }

        public string? Comment { get; set; }

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
