using System.Text;
using DBADashAI.Models;

namespace DBADashAI.Services
{
    /// <summary>
    /// Turns a query plan into the prompt the model answers.  The query plan counterpart of
    /// <see cref="AiDeadlockPromptBuilder"/>, built on the same principle.
    ///
    /// The insights the caller already established go in ahead of the plan deliberately.  They cost
    /// nothing, they come from rules over the parsed plan, and putting them first means the model
    /// spends its reasoning on what they imply for this query rather than rediscovering them.  It also
    /// stops it contradicting the Warnings tab the user is looking at while they read the answer,
    /// which would leave them trusting neither.
    ///
    /// The operator summary goes in for a reason the deadlock prompt has no equivalent of: showplan
    /// reports subtree costs, which put 100% on the root of every plan, so a model reading the XML
    /// alone has to do the subtraction itself to find the expensive operator.  The caller has already
    /// done it.
    /// </summary>
    public class AiPlanPromptBuilder
    {
        public string Build(AiPlanAnalysisRequest request)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine(
                "You are analysing a SQL Server execution plan for one statement, for a DBA who is looking at it right now.");
            prompt.AppendLine();
            prompt.AppendLine(
                "Answer in Markdown, using these four sections as level 2 headings, in this order.  " +
                "Keep to short paragraphs and lists; the answer is rendered and read on screen, not " +
                "parsed.  Use code formatting for object names, statements and settings.");
            prompt.AppendLine("1. What this plan does - two or three sentences, in plain terms.");
            prompt.AppendLine("2. Where the time goes - the operators that account for the cost, and why they cost it.");
            prompt.AppendLine("3. What to do - ranked, cheapest and safest first, each with its trade-off.");
            prompt.AppendLine("4. What to check next - what you cannot tell from the plan alone.");
            prompt.AppendLine();
            prompt.AppendLine(
                request.IsActualPlan
                    ? "This is an actual plan, so the row counts and times are what happened, not what was expected.  " +
                      "Where an estimate and an actual disagree, that gap is usually the story."
                    : "This is an estimated plan.  There are no measurements in it, so anything about what actually " +
                      "happened would be inference - say so where it matters rather than asserting it.");
            prompt.AppendLine(
                "Do not restate the findings below - they are already on screen; build on them, and say so if you " +
                "disagree with one.  Do not invent object, index or column names that are not present.  A missing " +
                "index the optimizer asked for is a suggestion from a cost model that saw one statement, not a " +
                "recommendation - treat it as one, and say what it would cost elsewhere.");
            prompt.AppendLine();
            prompt.AppendLine(
                "The reader can ask follow-up questions after this answer.  Answer those directly and as briefly " +
                "as the question allows, without the four headings and without repeating what you have already " +
                "said - the whole conversation stays on their screen.  Everything above is still all you know " +
                "about this plan, so where a follow-up asks for something it does not contain, say what would " +
                "answer it rather than guessing.");

            Section(prompt, "Query identity (groups this statement across the plans it has had)", request.Signature);
            Section(prompt, "Plan identity (this shape of plan for it)", request.PlanHash);
            Section(prompt, "Instance", request.Instance);

            prompt.AppendLine();
            prompt.AppendLine("Statement:");
            prompt.AppendLine(request.StatementText);
            if (request.StatementTruncated) prompt.AppendLine("... (the statement was cut short)");

            Section(prompt, "Plan context", request.Context);
            Section(prompt, "Statement statistics", request.Statistics);
            Section(prompt, "Findings already established locally", request.Insights);
            Section(prompt, "Missing indexes the optimizer asked for", request.MissingIndexes);
            Section(prompt, "Operators by their own cost (cost of the operator itself, not its subtree)", request.Operators);
            Section(prompt, "Waits", request.Waits);
            Section(prompt, "Parameters", request.Parameters);
            Section(prompt, "Objects touched", request.Objects);

            AppendPlanXml(prompt, request);

            return prompt.ToString();
        }

        /// <summary>
        /// The plan XML, where the caller sent it.
        ///
        /// It is optional in a way a deadlock graph is not: a plan can be hundreds of kilobytes, the
        /// reader can switch it off, and the summary above carries the figures an answer is mostly
        /// built from.  When it is absent the model is told so plainly, because the difference between
        /// "the plan does not say" and "I was not shown the plan" matters to whoever reads the answer.
        /// </summary>
        private static void AppendPlanXml(StringBuilder prompt, AiPlanAnalysisRequest request)
        {
            prompt.AppendLine();

            if (string.IsNullOrWhiteSpace(request.PlanXml))
            {
                prompt.AppendLine(
                    "The plan XML itself was not supplied - it was too large to send, or the reader chose not to " +
                    "send it.  Everything above is what you have.  Answer from it, and where something would need " +
                    "the XML - an individual operator's properties, the exact predicate on a scan - say that rather " +
                    "than inferring it.");
                return;
            }

            prompt.AppendLine("Showplan XML for this statement:");
            prompt.AppendLine(request.PlanXml);
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
