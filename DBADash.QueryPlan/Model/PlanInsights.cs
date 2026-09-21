using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// One thing worth reading about a plan before its figures: a warning, a missing index, or
    /// something about how it was compiled or ran that explains what the figures show.
    /// </summary>
    public sealed class PlanInsight
    {
        internal PlanInsight(PlanWarningSeverity severity, string text, PlanOperator? op = null, PlanMissingIndex? missingIndex = null)
            : this(severity, text, op is null ? [] : new[] { op }, missingIndex)
        {
        }

        internal PlanInsight(
            PlanWarningSeverity severity,
            string text,
            IReadOnlyList<PlanOperator> operators,
            PlanMissingIndex? missingIndex = null,
            string? fullText = null,
            int warningCount = 0)
        {
            Severity = severity;
            Text = text;
            Operators = operators;
            MissingIndex = missingIndex;
            FullText = fullText;
            WarningCount = warningCount;
        }

        public PlanWarningSeverity Severity { get; }

        /// <summary>What was found, as plain text.</summary>
        public string Text { get; }

        /// <summary>
        /// The operators it is about, for the reader to go to.  Empty for something about the
        /// statement as a whole, and more than one where several operators reported the same thing
        /// and it is said once - see <see cref="PlanInsights.CombineWarningsFrom"/>.
        /// </summary>
        public IReadOnlyList<PlanOperator> Operators { get; }

        /// <summary>The operator it is about, or the first of them, or null for the statement.</summary>
        public PlanOperator? Operator => Operators.Count > 0 ? Operators[0] : null;

        /// <summary>The recommendation, for its CREATE INDEX statement, when this is a missing index.</summary>
        public PlanMissingIndex? MissingIndex { get; }

        /// <summary>
        /// The whole of a combined warning, one line per warning with the node it is on, for the
        /// reader to open when the card's summary is cut short - see
        /// <see cref="PlanInsights.CombineWarningsFrom"/>.  Null for anything that is not a combined
        /// warning, where <see cref="Text"/> already says everything.
        /// </summary>
        public string? FullText { get; }

        /// <summary>
        /// How many warnings a combined one stands for, or zero when this insight is not a combined
        /// warning.  <see cref="FullText"/> is set exactly when this is above one.
        /// </summary>
        public int WarningCount { get; }

        /// <summary>True when this is several warnings said once, with a <see cref="FullText"/> list.</summary>
        public bool IsCombinedWarning => FullText is not null;

        public override string ToString() => Text;
    }

    /// <summary>
    /// What is worth saying about a statement, or one operator in it, ahead of the property list.
    ///
    /// The plan's own warnings and missing indexes come first, then a few things the plan records
    /// but never calls out - an optimiser that gave up early, a wait for memory, parameters that ran
    /// with values the plan was not compiled for - which are exactly the ones a reader new to plans
    /// does not know to look for.  Here rather than in the viewer so it is tested without a window.
    /// </summary>
    public static class PlanInsights
    {
        /// <summary>
        /// Everything worth saying about the statement as a whole, worst first: every warning, the
        /// plan's and each operator's, every missing index, and what the statement's own properties
        /// say.  Empty when there is nothing to say.
        /// </summary>
        public static IReadOnlyList<PlanInsight> ForStatement(PlanStatement statement)
        {
            var insights = new List<PlanInsight>();

            insights.AddRange(Grouped(WarningsIn(statement), onOneOperator: false));

            foreach (var index in statement.MissingIndexes.OrderByDescending(i => i.Impact))
            {
                var reader = statement.Operators.FirstOrDefault(op => statement.MissingIndexesFor(op).Contains(index));
                insights.Add(new PlanInsight(PlanWarningSeverity.Warning, MissingIndexText(index), reader, index));
            }

            if (EarlyAbort(statement.OptimisationEarlyAbortReason) is { } abort) insights.Add(abort);

            if (statement.MemoryGrant?.GrantWaitTimeMs is long wait and > 0)
            {
                // A moment's wait is worth knowing; a second or more is the server short of memory.
                insights.Add(new PlanInsight(
                    wait >= 1000 ? PlanWarningSeverity.Warning : PlanWarningSeverity.Information,
                    "The query waited " + Milliseconds(wait) + " for its memory grant before it could start running."));
            }

            if (statement.QueryTimeStats is { UdfElapsedMs: > 0 } times)
            {
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Warning,
                    "Scalar user-defined functions took " + Milliseconds(times.UdfElapsedMs!.Value) +
                    (times.ElapsedMs is { } elapsed ? " of the statement's " + Milliseconds(elapsed) : string.Empty) +
                    ". The work they do is not shown in this plan."));
            }

            var changed = statement.Parameters.Where(p => p.CompiledValueDiffers).ToList();
            if (changed.Count > 0)
            {
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Information,
                    "Ran with different parameter values from those the plan was compiled for: " +
                    string.Join("; ", changed.Select(p => p.Name + " compiled for " + p.CompiledValue + ", ran with " + p.RuntimeValue)) +
                    ". A plan compiled for one value can suit another poorly."));
            }

            if (statement.WaitStats.Count > 0)
            {
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Information,
                    "Waited longest on " +
                    string.Join(", ", statement.WaitStats.OrderByDescending(w => w.WaitTimeMs).Take(3)
                        .Select(w => w.WaitType + " (" + Milliseconds(w.WaitTimeMs) + ")")) + "."));
            }

            if (!string.IsNullOrEmpty(statement.NonParallelPlanReason))
            {
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Information,
                    "The plan runs on a single thread: " + Words(statement.NonParallelPlanReason) + "."));
            }

            if (statement.HasPlan && !statement.IsActualPlan)
            {
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Information,
                    "This is an estimated plan: it shows what the optimizer expected, without the row counts and times of a real run."));
            }

            // Worst first, in the order found within each severity.
            return insights.OrderByDescending(i => i.Severity).ToList();
        }

        /// <summary>
        /// One operator's warnings, worst first, then the missing indexes on the table it reads.
        ///
        /// Its own warnings and the plan level ones that happen in it - see
        /// <see cref="PlanStatement.WarningsFor"/> - because a conversion showplan reported against
        /// the statement is still something this operator is doing, and the reader who selected it
        /// after seeing its warning marker came here to find out what that marker was about.
        /// </summary>
        public static IReadOnlyList<PlanInsight> ForOperator(PlanOperator op, PlanStatement statement)
        {
            IReadOnlyList<PlanOperator> here = [op];

            return Grouped(
                    op.Warnings.Concat(statement.WarningsFor(op)).Select(w => (Warning: w, Operators: here)).ToList(),
                    onOneOperator: true)
                .OrderByDescending(i => i.Severity)
                .Concat(statement.MissingIndexesFor(op)
                    .Select(index => new PlanInsight(PlanWarningSeverity.Warning, MissingIndexText(index), op, index)))
                .ToList();
        }

        /// <summary>
        /// The point at which several warnings of the same kind stop being worth a card each.
        ///
        /// One implicit conversion is something to go and look at; fourteen of them are one thing to
        /// know about the query, and fourteen cards of it push everything else off the panel - the
        /// spill, the missing index, the estimate that was out by a thousand - which is the opposite
        /// of what the cards are for.
        /// </summary>
        public const int CombineWarningsFrom = 4;

        /// <summary>The most of a combined warning's own text named before the rest is counted.</summary>
        private const int MaxCombinedDetails = 2;

        /// <summary>The most nodes a combined warning names.</summary>
        private const int MaxCombinedNodes = 3;

        /// <summary>
        /// What one of those named details is cut to.  A plan affecting convert carries the
        /// expression it is about, and a generated one runs to hundreds of characters; the whole of
        /// it is in the warnings list, and on the card for that operator on its own.
        /// </summary>
        private const int MaxCombinedDetailLength = 200;

        /// <summary>
        /// Every warning in the statement with the operators it is about: the plan's own first, as
        /// SSMS lists them, each pointed at the operators it was traced to, then each operator's own.
        /// </summary>
        private static IEnumerable<(PlanWarning Warning, IReadOnlyList<PlanOperator> Operators)> WarningsIn(PlanStatement statement)
        {
            foreach (var warning in statement.Warnings) yield return (warning, warning.Operators);

            foreach (var op in statement.Operators)
            {
                IReadOnlyList<PlanOperator> here = [op];
                foreach (var warning in op.Warnings) yield return (warning, here);
            }
        }

        /// <summary>
        /// One insight for each warning, except where the same one turns up
        /// <see cref="CombineWarningsFrom"/> times or more - then one for all of them, saying how
        /// many there are, which nodes they are on, and what they say.
        /// </summary>
        private static IEnumerable<PlanInsight> Grouped(
            IEnumerable<(PlanWarning Warning, IReadOnlyList<PlanOperator> Operators)> found,
            bool onOneOperator)
        {
            foreach (var group in found.GroupBy(f => f.Warning.Title, StringComparer.Ordinal))
            {
                var items = group.ToList();

                if (items.Count < CombineWarningsFrom)
                {
                    foreach (var (warning, operators) in items.OrderByDescending(i => i.Warning.Severity))
                    {
                        yield return new PlanInsight(warning.Severity, warning.ToString(), operators);
                    }

                    continue;
                }

                // In node order, which is roughly the order they appear in the plan, rather than the
                // order showplan happened to list the warnings in.
                var nodes = items.SelectMany(i => i.Operators).Distinct().OrderBy(op => op.NodeId).ToList();

                var text = new StringBuilder(group.Key)
                    .Append(": ")
                    .Append(items.Count.ToString(CultureInfo.CurrentCulture))
                    .Append(onOneOperator
                        ? " on this operator."

                        // Nothing in the plan matched, so there is nowhere to send the reader - which
                        // is worth saying by omission rather than by naming a node that is a guess.
                        : nodes.Count == 0 ? " in this statement." : " in this statement, on " + Nodes(nodes) + ".");

                // What each one said, on a line of its own: which expressions were converted, which
                // columns had no statistics.  Distinct, because the same conversion reported by four
                // operators is one thing to read.
                var details = items
                    .Select(i => i.Warning.Detail)
                    .Where(detail => !string.IsNullOrEmpty(detail))
                    .Distinct(StringComparer.Ordinal)
                    .Select(detail => PlanFormat.SingleLine(detail!, MaxCombinedDetailLength))
                    .ToList();

                // A bare newline: a card's label counts a "\r\n" as two characters and draws it as
                // one, which moves every link after it along by one.
                if (details.Count > 0) text.Append('\n').Append(PlanFormat.List(details, MaxCombinedDetails)).Append('.');

                yield return new PlanInsight(
                    items.Max(i => i.Warning.Severity),
                    text.ToString(),
                    nodes,
                    fullText: CombinedFullText(group.Key, items),
                    warningCount: items.Count);
            }
        }

        /// <summary>
        /// Every warning a combined card stands for, written out in full: a heading with the title
        /// and the count, then one line per warning with the node it is on and what it said.  What
        /// the card's "Show all" link and the properties grid open, so nothing is only reachable from
        /// the Warnings tab.
        /// </summary>
        private static string CombinedFullText(
            string title,
            IReadOnlyList<(PlanWarning Warning, IReadOnlyList<PlanOperator> Operators)> items)
        {
            var text = new StringBuilder(title)
                .Append(" (")
                .Append(items.Count.ToString(CultureInfo.CurrentCulture))
                .AppendLine(")")
                .AppendLine();

            foreach (var (warning, operators) in items.OrderBy(i => i.Operators.Count > 0 ? i.Operators[0].NodeId : int.MaxValue))
            {
                if (operators.Count > 0)
                {
                    text.Append(operators.Count == 1 ? "Node " : "Nodes ")
                        .Append(string.Join(", ", operators.Select(op => op.NodeId.ToString(CultureInfo.CurrentCulture))))
                        .Append(": ");
                }

                text.AppendLine(string.IsNullOrEmpty(warning.Detail) ? warning.Title : warning.Detail);
            }

            return text.ToString();
        }

        /// <summary>The nodes a combined warning is on, as a sentence says them.</summary>
        private static string Nodes(IReadOnlyList<PlanOperator> operators) =>
            (operators.Count == 1 ? "node " : "nodes ") +
            PlanFormat.List(
                operators.Select(op => op.NodeId.ToString(CultureInfo.CurrentCulture)).ToList(),
                MaxCombinedNodes);

        private static string MissingIndexText(PlanMissingIndex index)
        {
            var text = new StringBuilder("Missing index: the optimizer estimates an index on ")
                .Append(index.Table)
                .Append(" would reduce this statement's cost by ")
                .Append(index.Impact.ToString("0.#", CultureInfo.CurrentCulture))
                .Append("%.");

            var keys = index.EqualityColumns.Concat(index.InequalityColumns).ToList();
            if (keys.Count > 0) text.Append(" Keys: ").Append(string.Join(", ", keys)).Append('.');
            if (index.IncludedColumns.Count > 0) text.Append(" Include: ").Append(string.Join(", ", index.IncludedColumns)).Append('.');

            return text.ToString();
        }

        /// <summary>
        /// Only the reasons that mean the plan may not be the best available.  GoodEnoughPlanFound
        /// is the optimiser stopping because it had found a plan worth keeping, which is normal.
        /// </summary>
        private static PlanInsight? EarlyAbort(string? reason) => reason switch
        {
            "TimeOut" => new PlanInsight(
                PlanWarningSeverity.Warning,
                "The optimizer timed out: it stopped searching and used the best plan found so far, which may not be the best one available."),
            "MemoryLimitExceeded" => new PlanInsight(
                PlanWarningSeverity.Warning,
                "The optimizer ran out of memory: it stopped searching and used the best plan found so far, which may not be the best one available."),
            _ => null
        };

        private static string Milliseconds(long ms) => ms.ToString("N0", CultureInfo.CurrentCulture) + " ms";

        /// <summary>
        /// Showplan's reason codes as words - MaxDOPSetToOne as "max DOP set to one" - keeping runs
        /// of capitals such as DOP and TSQL together.
        /// </summary>
        internal static string Words(string code)
        {
            var words = new List<string>();
            var word = new StringBuilder();

            for (var i = 0; i < code.Length; i++)
            {
                var c = code[i];
                var startsWord = i > 0 && char.IsUpper(c) &&
                                 (char.IsLower(code[i - 1]) || (i + 1 < code.Length && char.IsLower(code[i + 1]) && char.IsUpper(code[i - 1])));

                if (startsWord && word.Length > 0)
                {
                    words.Add(word.ToString());
                    word.Clear();
                }

                word.Append(c);
            }

            if (word.Length > 0) words.Add(word.ToString());

            // Ordinary words lower case; acronyms, which are all capitals, as they are.
            return string.Join(" ", words.Select(w => w.Length > 1 && w.All(char.IsUpper) ? w : w.ToLowerInvariant()));
        }
    }
}
