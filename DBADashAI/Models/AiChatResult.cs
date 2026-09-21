namespace DBADashAI.Models
{
    /// <summary>
    /// What came back from the provider, and whether it is an answer.
    ///
    /// The distinction earns its keep because an analysis is stored.  A provider that refuses - a key
    /// that has expired, a model that is overloaded, a prompt over the model's context limit - is
    /// reported as a sentence saying so, and a caller that cannot tell that sentence from an answer
    /// writes it into the repository as the analysis of the plan and shows it again, as an analysis,
    /// the next time that plan turns up.
    ///
    /// <see cref="Text"/> is the answer when there is one and the reason when there is not, so a
    /// caller that only displays the result can still use it without looking at anything else.
    /// </summary>
    public sealed class AiChatResult
    {
        public bool Success => Failure == AiChatFailure.None;

        /// <summary>The answer, or - when there is not one - the reason there is not.</summary>
        public string Text { get; init; } = string.Empty;

        public AiChatFailure Failure { get; init; }

        /// <summary>The provider's status code, where the provider is what refused.</summary>
        public int? ProviderStatusCode { get; init; }

        /// <summary>Ties the caller's message to the logged exception or response body.</summary>
        public string? ErrorId { get; init; }

        public static AiChatResult Answer(string text) => new() { Text = text };

        public static AiChatResult Failed(AiChatFailure failure, string text, string? errorId = null, int? status = null) =>
            new() { Failure = failure, Text = text, ErrorId = errorId, ProviderStatusCode = status };
    }

    /// <summary>
    /// Why there is no answer.  Separated from the message because the callers act on these
    /// differently: what a reader should do about a request that was too large is not what they
    /// should do about a provider that is down, and neither is worth storing as an analysis.
    /// </summary>
    public enum AiChatFailure
    {
        None = 0,

        /// <summary>No provider is configured, or the settings for the one named are incomplete.</summary>
        NotConfigured,

        /// <summary>The prompt is over what the model will accept - the one failure a caller can fix.</summary>
        TooLarge,

        /// <summary>The provider refused or failed for any other reason.</summary>
        Provider
    }
}
