using System.Text;
using DBADashAI.Models;

namespace DBADashAI.Services
{
    /// <summary>
    /// Turns a deadlock into the prompt the model answers.
    ///
    /// The findings the caller already established go in ahead of the graph deliberately.  They cost
    /// nothing, they are always right - they come from rules over the parsed graph - and putting them
    /// first means the model spends its reasoning on what they imply for this schema and this code
    /// rather than rediscovering them.  It also stops it contradicting the panel the user is looking
    /// at while they read the answer, which would leave them trusting neither.
    /// </summary>
    public class AiDeadlockPromptBuilder
    {
        public string Build(AiDeadlockAnalysisRequest request)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine(
                "You are analysing a SQL Server deadlock graph for a DBA who is looking at it right now.");
            prompt.AppendLine();
            prompt.AppendLine(
                "Answer in Markdown, using these four sections as level 2 headings, in this order.  " +
                "Keep to short paragraphs and lists; the answer is rendered and read on screen, not " +
                "parsed.  Use code formatting for object names, statements and settings.");
            prompt.AppendLine("1. What happened - two or three sentences, in plain terms.");
            prompt.AppendLine("2. Why - the root cause, tied to what the graph actually shows.");
            prompt.AppendLine("3. What to do - ranked, cheapest and safest first, each with its trade-off.");
            prompt.AppendLine("4. What to check next - what you cannot tell from the graph alone.");
            prompt.AppendLine();
            prompt.AppendLine(
                "The graph is what SQL Server observed, not what the application intended.  Where you are " +
                "inferring intent, say so.  Do not restate the findings below - they are already on screen; " +
                "build on them, and say so if you disagree with one.  Do not invent object or index names " +
                "that are not present.");

            Section(prompt, "Signature (identifies this deadlock pattern)", request.Signature);
            Section(prompt, "Instance", request.Instance);
            Section(prompt, "Participants", request.Participants);
            Section(prompt, "Objects contended", request.Objects);
            Section(prompt, "Modules involved", request.Modules);
            Section(prompt, "Findings already established locally", request.Findings);

            AppendSchema(prompt, request);

            prompt.AppendLine();
            prompt.AppendLine("Deadlock graph:");
            prompt.AppendLine(request.GraphXml);

            return prompt.ToString();
        }

        /// <summary>
        /// The definitions of the objects involved, where the caller had them.
        ///
        /// Their value is in what the graph cannot say: which index the statement could have used,
        /// what the procedure does either side of the statement that deadlocked, whether the table is
        /// a heap.  They are dated because they come from scheduled snapshots - the model is told to
        /// treat them as the last known state before the deadlock, not a guarantee of the state at
        /// the moment of it.
        /// </summary>
        private static void AppendSchema(StringBuilder prompt, AiDeadlockAnalysisRequest request)
        {
            if (request.Schema.Count == 0)
            {
                prompt.AppendLine();
                prompt.AppendLine(
                    "No object definitions were supplied, so anything about indexes, column types or " +
                    "what a procedure does elsewhere would be inference from the statements alone.  Say so " +
                    "where it matters rather than assuming.");
                return;
            }

            prompt.AppendLine();
            prompt.AppendLine(
                "Object definitions follow, taken from schema snapshots.  Each is the last known state " +
                "before the deadlock rather than a certainty about the moment of it, and some are cut " +
                "short where noted.");

            foreach (var definition in request.Schema)
            {
                var asAt = definition.AsAt is { } date ? $", snapshot {date:u}" : string.Empty;
                var truncated = definition.Truncated ? ", truncated" : string.Empty;

                prompt.AppendLine();
                prompt.AppendLine($"-- {definition.Database}.{definition.Name} ({definition.ObjectType}{asAt}{truncated})");
                prompt.AppendLine(definition.Ddl);
            }
        }

        private static void Section(StringBuilder prompt, string heading, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            prompt.AppendLine();
            prompt.AppendLine($"{heading}: {value}");
        }

        private static void Section(StringBuilder prompt, string heading, IReadOnlyList<string> values)
        {
            if (values.Count == 0) return;

            prompt.AppendLine();
            prompt.AppendLine($"{heading}:");
            foreach (var value in values)
            {
                prompt.AppendLine($"- {value}");
            }
        }
    }
}
