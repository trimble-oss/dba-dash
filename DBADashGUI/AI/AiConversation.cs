#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBADashGUI.AI
{
    /// <summary>
    /// A conversation with the model about one artifact - a deadlock graph, a query plan.
    ///
    /// The artifact itself is not in here.  It is the opening question, and the service rebuilds it
    /// from the payload on every turn, so what the viewer has to carry is only what was said
    /// afterwards: the first answer, the reader's next question, the next answer.  That is also what
    /// the repository stores, and what a conversation read back from it becomes.
    ///
    /// Shared between the deadlock and query plan viewers because a conversation about a deadlock and
    /// a conversation about a plan differ only in what started them.
    /// </summary>
    internal sealed class AiConversation
    {
        /// <summary>One answer, and the question that produced it where there was one.</summary>
        internal sealed record Turn(string? Question, string Answer, string? Model, DateTime GeneratedUtc)
        {
            /// <summary>The opening turn, whose question was the artifact rather than anything typed.</summary>
            public bool IsOpening => string.IsNullOrWhiteSpace(Question);
        }

        private readonly List<Turn> _turns = new();

        internal AiConversation(Guid id) => Id = id;

        /// <summary>A conversation about to be started: nothing said yet, and an id to store it under.</summary>
        internal static AiConversation Start() => new(Guid.NewGuid());

        /// <summary>A conversation read back from the repository, in the order it was had.</summary>
        internal static AiConversation Restore(Guid id, IEnumerable<Turn> turns)
        {
            var conversation = new AiConversation(id);
            conversation._turns.AddRange(turns);
            return conversation;
        }

        internal Guid Id { get; }

        internal IReadOnlyList<Turn> Turns => _turns;

        internal int Count => _turns.Count;

        internal bool IsEmpty => _turns.Count == 0;

        /// <summary>The model that answered last, which is the one a reader is being told about.</summary>
        internal string? LatestModel => _turns.Count == 0 ? null : _turns[^1].Model;

        internal DateTime? LatestGeneratedUtc => _turns.Count == 0 ? null : _turns[^1].GeneratedUtc;

        internal void Add(Turn turn) => _turns.Add(turn);

        /// <summary>
        /// What was said after the opening question, as the service wants it: the answers and the
        /// questions in the order they happened, starting with an answer.
        ///
        /// The opening question is deliberately absent - the service builds it from the payload that
        /// goes with every request, so replaying it from here would let a follow-up quietly send a
        /// different artifact from the one the first answer was about.
        /// </summary>
        internal List<WireTurn> ToHistory()
        {
            var history = new List<WireTurn>();

            foreach (var turn in _turns)
            {
                // The question comes before the answer it produced, except on the opening turn where
                // the question was the artifact itself.
                if (!turn.IsOpening) history.Add(new WireTurn("user", turn.Question!));
                history.Add(new WireTurn("assistant", turn.Answer));
            }

            return history;
        }

        /// <summary>One turn as it goes on the wire.</summary>
        internal sealed record WireTurn(string Role, string Content);

        /// <summary>
        /// The whole conversation as one Markdown document, which is how it is shown: the answers as
        /// the model wrote them, with the reader's questions quoted between them and a rule to
        /// separate one exchange from the next.
        ///
        /// The questions are quoted line by line rather than dropped in whole.  A question is typed by
        /// a person, and a person who types a line starting with a hash would otherwise get a heading
        /// in the middle of someone else's answer.
        /// </summary>
        internal string ToMarkdown()
        {
            var markdown = new StringBuilder();

            foreach (var turn in _turns)
            {
                if (markdown.Length > 0) markdown.AppendLine().AppendLine("---").AppendLine();

                if (!turn.IsOpening)
                {
                    foreach (var line in turn.Question!.Replace("\r\n", "\n").Split('\n'))
                    {
                        markdown.AppendLine("> " + line);
                    }
                    markdown.AppendLine();
                }

                markdown.AppendLine(turn.Answer);
            }

            return markdown.ToString();
        }

        /// <summary>
        /// The same thing as plain text, for the fallback when the rendered view is unavailable -
        /// which is a supported state in this application, not an error.  The Markdown is shown as
        /// written rather than stripped: it is readable, and a half-hearted strip is not.
        /// </summary>
        internal string ToPlainText()
        {
            var separator = Environment.NewLine + Environment.NewLine + new string('-', 60) + Environment.NewLine + Environment.NewLine;

            return string.Join(separator, _turns.Select(Text));

            static string Text(Turn turn)
            {
                var question = turn.IsOpening
                    ? string.Empty
                    : "Q: " + Lines(turn.Question!) + Environment.NewLine + Environment.NewLine;

                return question + Lines(turn.Answer);
            }

            // Models answer with bare newlines, and a WinForms text box wants the pair.
            static string Lines(string text) => text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }
    }
}
