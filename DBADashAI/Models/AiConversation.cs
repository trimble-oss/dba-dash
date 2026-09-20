namespace DBADashAI.Models
{
    /// <summary>
    /// The rules a conversation about an artifact has to obey, in one place because the deadlock and
    /// query plan requests are the same conversation over different artifacts.
    ///
    /// A conversation always starts with the artifact.  The opening user message is built by the
    /// service from the payload the caller sends - the graph, the plan, the findings - so the caller
    /// never holds it, and a follow-up carries only what was said after it: the model's answer, the
    /// reader's next question, the next answer, and so on.  <see cref="History"/> therefore starts
    /// with an assistant turn and alternates from there.
    ///
    /// The limits exist because the whole conversation is re-sent on every turn, and because the
    /// content goes to a provider that bills by the token.  They are generous enough that nobody
    /// working through a deadlock will meet them and tight enough that a loop cannot run up a bill.
    /// </summary>
    public static class AiConversation
    {
        /// <summary>Turns after the opening one.  20 answers and 20 questions is a long session with one deadlock.</summary>
        public const int MaxTurns = 40;

        /// <summary>A follow-up question is a question, not a second artifact pasted in.</summary>
        public const int MaxQuestionLength = 8 * 1024;

        /// <summary>Everything said so far, which is re-sent each turn on top of the artifact itself.</summary>
        public const int MaxHistoryLength = 256 * 1024;

        /// <summary>
        /// Checks the conversation is one the service can send on, returning the reason it isn't.
        /// The artifact itself is the caller's business to validate - this is only about the turns.
        /// </summary>
        public static string? Validate(List<AiConversationTurn> history, string? question)
        {
            if (history.Count > MaxTurns)
            {
                return $"The conversation exceeds {MaxTurns} turns.  Start a new analysis.";
            }

            if (history.Sum(t => t.Content?.Length ?? 0) > MaxHistoryLength)
            {
                return $"The conversation exceeds {MaxHistoryLength} characters.  Start a new analysis.";
            }

            if (question is { Length: > MaxQuestionLength })
            {
                return $"The question exceeds {MaxQuestionLength} characters.";
            }

            if (history.Count > 0 && string.IsNullOrWhiteSpace(question))
            {
                return "A follow-up question is required when a conversation is supplied.";
            }

            for (var i = 0; i < history.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(history[i].Content))
                {
                    return "A conversation turn is empty.";
                }

                // The opening turn is the artifact, built here rather than sent, so what follows
                // begins with the answer to it and alternates.  A provider rejects anything else,
                // and a caller that got this wrong is a caller whose transcript is not what it thinks.
                var expected = i % 2 == 0 ? AiConversationTurn.Assistant : AiConversationTurn.User;
                if (!string.Equals(history[i].Role, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Conversation turn {i + 1} should be from the {expected}.";
                }
            }

            return history.Count > 0 && history[^1].IsUser
                ? "The conversation ends with a question that was never answered."
                : null;
        }

        /// <summary>
        /// The messages to send: the artifact as the opening question, everything said since, and the
        /// new question last.  A first analysis is the degenerate case - one message, no follow-up.
        /// </summary>
        public static List<AiConversationTurn> Build(string openingPrompt, List<AiConversationTurn> history, string? question)
        {
            var messages = new List<AiConversationTurn>
            {
                new() { Role = AiConversationTurn.User, Content = openingPrompt }
            };

            messages.AddRange(history);

            if (!string.IsNullOrWhiteSpace(question))
            {
                messages.Add(new AiConversationTurn { Role = AiConversationTurn.User, Content = question! });
            }

            return messages;
        }
    }
}
