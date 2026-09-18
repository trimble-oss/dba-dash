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
        {
            Severity = severity;
            Text = text;
            Operator = op;
            MissingIndex = missingIndex;
        }

        public PlanWarningSeverity Severity { get; }

        /// <summary>What was found, as plain text.</summary>
        public string Text { get; }

        /// <summary>The operator it is about, for the reader to go to, or null for the statement.</summary>
        public PlanOperator? Operator { get; }

        /// <summary>The recommendation, for its CREATE INDEX statement, when this is a missing index.</summary>
        public PlanMissingIndex? MissingIndex { get; }

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

            insights.AddRange(statement.Warnings.Select(w => new PlanInsight(w.Severity, w.ToString())));

            foreach (var op in statement.Operators)
            {
                insights.AddRange(op.Warnings.Select(w => new PlanInsight(w.Severity, w.ToString(), op)));
            }

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

        /// <summary>One operator's warnings, worst first, then the missing indexes on the table it reads.</summary>
        public static IReadOnlyList<PlanInsight> ForOperator(PlanOperator op, PlanStatement statement) =>
            op.Warnings
                .OrderByDescending(w => w.Severity)
                .Select(w => new PlanInsight(w.Severity, w.ToString(), op))
                .Concat(statement.MissingIndexesFor(op)
                    .Select(index => new PlanInsight(PlanWarningSeverity.Warning, MissingIndexText(index), op, index)))
                .ToList();

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
