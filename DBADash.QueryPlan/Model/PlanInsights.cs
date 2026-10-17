using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBADash.QueryPlan.Layout;

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

        /// <summary>One of DBA Dash's own findings - see <see cref="IsAnalysis"/>.</summary>
        internal static PlanInsight Analysis(
            PlanWarningSeverity severity,
            string title,
            string text,
            IReadOnlyList<PlanOperator>? operators = null,
            bool dependsOnPlanKind = false) =>
            new(severity, text, operators ?? [], title: title) { DependsOnPlanKind = dependsOnPlanKind };

        internal PlanInsight(
            PlanWarningSeverity severity,
            string text,
            IReadOnlyList<PlanOperator> operators,
            PlanMissingIndex? missingIndex = null,
            string? fullText = null,
            int warningCount = 0,
            string? title = null)
        {
            Title = title;
            Severity = severity;
            Text = text;
            Operators = operators;
            MissingIndex = missingIndex;
            FullText = fullText;
            WarningCount = warningCount;
        }

        public PlanWarningSeverity Severity { get; }

        /// <summary>
        /// A short name for a finding of DBA Dash's own, for a list to show beside the plan's
        /// warning titles.  Null for the plan's warnings and missing indexes.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        /// True when this is DBA Dash's own reading of the plan rather than a warning or missing
        /// index SQL Server put in it - see <see cref="PlanInsights.AnalysisFor"/>.  A reader
        /// comparing with SSMS needs to know which is which: SSMS will not show these.
        /// </summary>
        public bool IsAnalysis => Title is not null;

        /// <summary>
        /// A finding only one kind of plan can give - one read from a run's figures, or the large
        /// grant only an estimated plan is judged on - so an estimated plan and an actual one having
        /// it or not says nothing about which is better.  The insight's
        /// <see cref="PlanWarning.IsRuntime"/>, for comparing two plans.
        /// </summary>
        public bool DependsOnPlanKind { get; private init; }

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

            insights.AddRange(AnalysisFor(statement));

            // Worst first, in the order found within each severity.
            return insights.OrderByDescending(i => i.Severity).ToList();
        }

        /// <summary>
        /// DBA Dash's own findings about the statement - what the plan records but SQL Server never
        /// raises as a warning - in the order found.  <see cref="ForStatement"/> without the plan's
        /// warnings and missing indexes, for a list that already shows those.
        /// </summary>
        public static IReadOnlyList<PlanInsight> AnalysisFor(PlanStatement statement)
        {
            var insights = new List<PlanInsight>();

            if (EarlyAbort(statement.OptimisationEarlyAbortReason) is { } abort) insights.Add(abort);

            if (statement.MemoryGrant?.GrantWaitTimeMs is long wait and > 0)
            {
                // Any wait means the memory was already given to other queries - the server (or, under
                // Resource Governor, the query's pool) was short of workspace memory, and this query
                // queued (RESOURCE_SEMAPHORE) for its turn.
                insights.Add(PlanInsight.Analysis(
                    PlanWarningSeverity.Warning,
                    "Memory grant wait",
                    "The query waited " + Milliseconds(wait) + " for its memory grant before it could start running. " +
                    "Other queries were holding the available query memory (or, under Resource Governor, their pool's share of it), " +
                    "so this one had to queue (RESOURCE_SEMAPHORE) until enough was released. " +
                    "Look for large or excessive grants in the queries running alongside it.",
                    dependsOnPlanKind: true));
            }

            if (ExcessiveMemoryGrant(statement) is { } excessiveGrant) insights.Add(excessiveGrant);

            if (ScalarUdfInsight(statement) is { } udfInsight)
            {
                insights.Add(udfInsight);
            }

            if (OptionalParametersInsight(statement) is { } optional) insights.Add(optional);

            var tableVariables = TableVariableInsight(statement, PlanTableVariables.OperatorsIn(statement));
            if (tableVariables is not null) insights.Add(tableVariables);

            insights.AddRange(PlanTableVariables.FunctionsIn(statement).Select(call => FunctionInsight(statement, call)));

            if (BuiltInFunctionsInsight(PlanTableVariables.BuiltInFunctionsIn(statement)) is { } builtIn) insights.Add(builtIn);

            insights.AddRange(statement.Operators
                .OrderBy(op => op.NodeId)
                .Select(op => PartitionEliminationInsight(op, statement))
                .OfType<PlanInsight>());

            var changed = statement.Parameters.Where(p => p.CompiledValueDiffers).ToList();
            if (changed.Count > 0)
            {
                insights.Add(PlanInsight.Analysis(
                    PlanWarningSeverity.Information,
                    "Parameter values differ",
                    "Ran with different parameter values from those the plan was compiled for: " +
                    string.Join("; ", changed.Select(p => p.Name + " compiled for " + p.CompiledValue + ", ran with " + p.RuntimeValue)) +
                    ". A plan compiled for one value can suit another poorly.",
                    dependsOnPlanKind: true));
            }

            if (statement.WaitStats.Count > 0)
            {
                insights.Add(PlanInsight.Analysis(
                    PlanWarningSeverity.Information,
                    "Waits",
                    "Waited longest on " +
                    string.Join(", ", statement.WaitStats.OrderByDescending(w => w.WaitTimeMs).Take(3)
                        .Select(w => w.WaitType + " (" + Milliseconds(w.WaitTimeMs) + ")")) + ".",
                    dependsOnPlanKind: true));
            }

            // Said above, as part of the scalar UDF or table variable card, rather than repeated here
            // as a plain reason code.
            var explainedAbove = IsUdfNonParallelReason(statement.NonParallelPlanReason) ||
                                 (tableVariables is not null && IsTableVariableNonParallelReason(statement.NonParallelPlanReason));

            if (!explainedAbove && !string.IsNullOrEmpty(statement.NonParallelPlanReason))
            {
                insights.Add(PlanInsight.Analysis(
                    PlanWarningSeverity.Information,
                    "Serial plan",
                    "The plan runs on a single thread: " + Words(statement.NonParallelPlanReason) + "."));
            }

            if (statement.HasPlan && !statement.IsActualPlan)
            {
                insights.Add(PlanInsight.Analysis(
                    PlanWarningSeverity.Information,
                    EstimatedPlanTitle,
                    "This is an estimated plan: it shows what the optimizer expected, without the row counts and times of a real run.",
                    dependsOnPlanKind: true));
            }

            return insights;
        }

        /// <summary>
        /// The title of the note that a plan is estimated - about the plan rather than the query, so
        /// left out when two plans are compared, which already say which kind each is.
        /// </summary>
        public const string EstimatedPlanTitle = "Estimated plan";

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
                .Concat(PlanOptionalParameters.On(op, statement) is { } use
                        && NothingLeftToOptimizeInsight(statement, [use]) is null
                    ? new[] { OptionalParametersInsight(statement, [use], onOneOperator: true) }
                    : Enumerable.Empty<PlanInsight>())
                .Concat(PartitionEliminationInsight(op, statement) is { } partitions ? new[] { partitions } : Enumerable.Empty<PlanInsight>())
                .Concat(PlanTableVariables.Touches(op) ? new[] { TableVariableInsight(statement, [op])! } : Enumerable.Empty<PlanInsight>())
                .Concat(PlanTableVariables.FunctionCallFor(op, statement) is { } call
                    ? new[] { FunctionInsight(statement, call) }
                    : Enumerable.Empty<PlanInsight>())
                .Concat(PlanTableVariables.IsBuiltInFunction(op) ? new[] { BuiltInFunctionsInsight([op])! } : Enumerable.Empty<PlanInsight>())
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
            "TimeOut" => PlanInsight.Analysis(
                PlanWarningSeverity.Warning,
                "Optimizer timed out",
                "The optimizer timed out: it stopped searching and used the best plan found so far, which may not be the best one available."),
            "MemoryLimitExceeded" => PlanInsight.Analysis(
                PlanWarningSeverity.Warning,
                "Optimizer out of memory",
                "The optimizer ran out of memory: it stopped searching and used the best plan found so far, which may not be the best one available."),
            _ => null
        };

        private static string Milliseconds(long ms) => ms.ToString("N0", CultureInfo.CurrentCulture) + " ms";

        /// <summary>
        /// A grant far larger than the query used - query memory other queries could not be granted
        /// while it ran, and a common cause of RESOURCE_SEMAPHORE waits for them.  On an
        /// actual plan it is judged by what was used; an estimated plan has nothing to measure it
        /// against, so there a large desired grant is called out on its own.  Null when the grant
        /// was small or, on an actual plan, well used.
        /// </summary>
        private static PlanInsight? ExcessiveMemoryGrant(PlanStatement statement)
        {
            var grant = statement.MemoryGrant;
            if (grant is null) return null;

            if (!statement.IsActualPlan)
            {
                if (grant.EffectiveDesiredMemoryKb is not { } desired || desired < PlanMemoryGrantInfo.ExcessiveGrantMinKb) return null;

                return PlanInsight.Analysis(
                    PlanWarningSeverity.Warning,
                    "Large memory grant",
                    "The optimizer wants a memory grant of " + PlanFormat.Kilobytes(desired) +
                    " (the server may cap it, and a parallel plan asks for more). While the query runs, a large " +
                    "grant is query memory other queries cannot be granted, and is a common cause of " +
                    "RESOURCE_SEMAPHORE waits for them. " + GrantSizingHint +
                    " An actual plan shows how much is really used.",
                    dependsOnPlanKind: true);
            }

            if (!grant.IsExcessive ||
                grant is not { GrantedMemoryKb: { } granted, MaxUsedMemoryKb: { } used, GrantUsedFraction: { } usedFraction })
            {
                return null;
            }

            return PlanInsight.Analysis(
                PlanWarningSeverity.Warning,
                "Excessive memory grant",
                "The query was granted " + PlanFormat.Kilobytes(granted) + " of memory but used only " +
                PlanFormat.Kilobytes(used) + " (" + PlanFormat.Percent(usedFraction) +
                "). The rest was query memory other queries could not be granted while this one ran, and an " +
                "over-sized grant is a common cause of RESOURCE_SEMAPHORE waits for other queries. " + GrantSizingHint,
                dependsOnPlanKind: true);
        }

        /// <summary>Where a grant's size usually comes from, for the reader to go and look.</summary>
        private const string GrantSizingHint =
            "Grants are mostly sized for Sort operators, and for the hash tables built by Hash Match " +
            "joins and aggregates; each operator's memory fraction or grant shows where it goes. Check the " +
            "estimated rows and row sizes going into them. An ORDER BY that an index could supply, or " +
            "fewer or narrower columns (variable-length columns are estimated at half their declared size, " +
            "and (max) columns at 4,000 bytes), can shrink the grant.";

        /// <summary>
        /// One card for everything a scalar UDF is worth saying, rather than one about its time and
        /// another about it blocking parallelism - both are the same UDF, and a reader should not
        /// have to work that out from two separate cards.  Null when neither is true.
        /// </summary>
        private static PlanInsight? ScalarUdfInsight(PlanStatement statement)
        {
            var times = statement.QueryTimeStats;
            var udfTimes = new List<string>();
            if (times?.UdfElapsedMs is { } udfElapsed and > 0) udfTimes.Add(Milliseconds(udfElapsed) + " elapsed");
            if (times?.UdfCpuMs is { } udfCpu and > 0) udfTimes.Add(Milliseconds(udfCpu) + " CPU");

            // Reported by the optimiser itself at compile time, so this is true even when there is no
            // actual run to measure the times above against.
            var blocksParallelism = IsUdfNonParallelReason(statement.NonParallelPlanReason);

            if (udfTimes.Count == 0 && !blocksParallelism) return null;

            var text = new StringBuilder("Scalar user-defined functions");

            if (udfTimes.Count > 0)
            {
                text.Append(" took ").Append(string.Join(", ", udfTimes));
                if (times?.ElapsedMs is { } elapsed) text.Append(" of the statement's ").Append(Milliseconds(elapsed));
                if (blocksParallelism) text.Append(", and force this plan to run on a single thread");
            }
            else
            {
                text.Append(" force this plan to run on a single thread");
            }

            text.Append(". They run once per row")
                .Append(blocksParallelism ? " and block parallelism" : string.Empty)
                .Append("; the work they do is not shown in this plan")
                .Append(blocksParallelism ? " - consider an inline table-valued function or rewriting the logic without a UDF" : string.Empty)
                .Append('.');

            // Raised only for its times when it does not block parallelism, and only a run has times.
            return PlanInsight.Analysis(PlanWarningSeverity.Critical, "Scalar UDF", text.ToString(), dependsOnPlanKind: !blocksParallelism);
        }

        /// <summary>
        /// The catch-all query - WHERE (col = @p OR @p IS NULL) - compiled without OPTION (RECOMPILE),
        /// which <see cref="PlanOptionalParameters"/> finds.  Null when there is none.
        /// </summary>
        private static PlanInsight? OptionalParametersInsight(PlanStatement statement)
        {
            var uses = PlanOptionalParameters.In(statement);
            if (NothingLeftToOptimizeInsight(statement, uses) is { } optimized) return optimized;
            return uses.Count == 0 ? null : OptionalParametersInsight(statement, uses, onOneOperator: false);
        }

        /// <summary>
        /// A plan SQL Server 2025's optional parameter plan optimization compiled, with no optional
        /// parameter conditions left in it that these values could use - none at all, or only ones
        /// whose parameters are NULL, which match every row.  Neither OPTION (RECOMPILE) nor another
        /// variant could do better, so it is only worth saying that the optimization was applied.
        /// Null when the plan is not a variant, or when a condition it left is on a supplied value.
        /// </summary>
        private static PlanInsight? NothingLeftToOptimizeInsight(PlanStatement statement, IReadOnlyList<PlanOptionalParameterUse> uses)
        {
            var optimizedBy = PlanOptionalParameters.OptimizedBy(statement.StatementText);
            if (optimizedBy.Count == 0) return null;

            var remaining = uses.SelectMany(u => u.Parameters).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!remaining.All(p => IsNull(statement, p))) return null;

            var text = AppendOptimizedBy(new StringBuilder(), optimizedBy);
            if (remaining.Count > 0)
            {
                var one = remaining.Count == 1;
                text.Append(one ? " The condition" : " The conditions")
                    .Append(" left on ")
                    .Append(PlanFormat.List(remaining, MaxNamedParameters))
                    .Append(one ? " matches" : " match")
                    .Append(" every row, as ")
                    .Append(one ? "it is" : "they are")
                    .Append(" NULL, so neither OPTION (RECOMPILE) nor another variant could do better for these values.");
            }

            return PlanInsight.Analysis(PlanWarningSeverity.Information, OptimizedOptionalParametersTitle, text.ToString());
        }

        /// <summary>
        /// Whether <paramref name="parameter"/> is NULL in the values this plan ran with, or was
        /// compiled for when it has no runtime values.  False when the plan doesn't list it.
        /// </summary>
        private static bool IsNull(PlanStatement statement, string parameter) =>
            statement.Parameters.FirstOrDefault(p => string.Equals(p.Name, parameter, StringComparison.OrdinalIgnoreCase)) is { } found
            && string.Equals(found.RuntimeValue ?? found.CompiledValue, "NULL", StringComparison.OrdinalIgnoreCase);

        private static StringBuilder AppendOptimizedBy(StringBuilder text, IReadOnlyList<string> optimizedBy) =>
            text.Append("Optional parameters, optimized by SQL Server: optional parameter plan optimization compiled this plan for whether ")
                .Append(PlanFormat.List(optimizedBy, MaxNamedParameters))
                .Append(optimizedBy.Count == 1 ? " is" : " are")
                .Append(" NULL, removing ")
                .Append(optimizedBy.Count == 1 ? "its" : "their")
                .Append(" IS NULL test from the plan.");

        private static PlanInsight OptionalParametersInsight(
            PlanStatement statement,
            IReadOnlyList<PlanOptionalParameterUse> uses,
            bool onOneOperator)
        {
            // A plan SQL Server 2025 compiled for which parameters are NULL: those parameters' IS NULL
            // tests are gone from it, and what is left is the conditions it did not choose by.
            // Here at least one of those is supplied (NothingLeftToOptimizeInsight takes the rest), so
            // still worth a card - how much it costs depends on selectivity the plan cannot show - but
            // the query is already being optimized for its optional parameters, so not a warning.
            // The conditions left on NULL parameters match every row, so there is nothing in them to
            // name.
            var optimizedBy = PlanOptionalParameters.OptimizedBy(statement.StatementText);
            var isVariant = optimizedBy.Count > 0;
            bool Named(string parameter) => !isVariant || !IsNull(statement, parameter);

            uses = uses.Where(u => u.Parameters.Any(Named)).ToList();
            var parameters = uses.SelectMany(u => u.Parameters).Where(Named).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var operators = uses.Select(u => u.Operator).ToList();
            var one = parameters.Count == 1;

            var where = onOneOperator ? "this operator's predicate matches" :
                operators.Count == 1 ? "the predicate on " + Nodes(operators) + " matches" : "the predicates on " + Nodes(operators) + " match";

            var text = new StringBuilder();

            if (isVariant)
            {
                AppendOptimizedBy(text, optimizedBy)
                    .Append(' ')
                    .Append(char.ToUpperInvariant(where[0])).Append(where, 1, where.Length - 1)
                    .Append(" whether or not ")
                    .Append(PlanFormat.List(parameters, MaxNamedParameters))
                    .Append(one ? " is" : " are")
                    .Append(" supplied, and can't seek on ")
                    .Append(one ? "that condition" : "those conditions")
                    .Append(", which matters if ")
                    .Append(one ? "it is" : "they are")
                    .Append(" selective.");
            }
            else
            {
                text.Append("Optional parameters without OPTION (RECOMPILE): ")
                    .Append(where)
                    .Append(" whether or not ")
                    .Append(PlanFormat.List(parameters, MaxNamedParameters))
                    .Append(one ? " is" : " are")
                    .Append(" supplied (@p IS NULL OR col = @p, col = ISNULL(@p, col) or col = COALESCE(@p, col)). One plan has to serve every combination of values, so it can't seek on ")
                    .Append(one ? "that condition" : "those conditions")
                    .Append(" and may read far more rows than the values given need.");
            }

            // What each run would pay to compile, from what this plan took - a guide rather than a
            // measure, since a plan compiled for known values is often simpler.
            var compileCost = statement.CompileCpuMs is { } compileCpu
                ? " This plan took " + Milliseconds(compileCpu) + " of CPU to compile" +
                  (statement.QueryTimeStats?.CpuMs is { } runCpu ? ", against " + Milliseconds(runCpu) + " to run." : ".")
                : string.Empty;

            // Bare newlines: a card's label counts "\r\n" as two characters and draws it as one.
            text.Append("\nOptions:")
                .Append("\n- OPTION (RECOMPILE) on the statement compiles each run for its own values, removing the conditions not used. ")
                .Append("It adds a compile to every execution, which may cost too much CPU for a frequently run query.")
                .Append(compileCost)
                .Append("\n- Dynamic SQL, parameterized with sp_executesql, that includes only the conditions supplied gets a cached plan for each combination, ")
                .Append("at the cost of more complex code.");

            // Already in use when this is a variant, so only offered when it is not.
            if (!isVariant)
            {
                // The configuration is on by default in the 2025 builds checked, but can be switched off
                // per database, so it is named for the reader to check.
                text.Append("\n- SQL Server 2025 handles optional parameters itself with optional parameter plan optimization, ")
                    .Append("which needs compatibility level 170 and the OPTIONAL_PARAMETER_OPTIMIZATION database scoped configuration on. ")
                    .Append("It compiles plans by which parameters are NULL, typically seeking on one supplied parameter per plan and ")
                    .Append("leaving the other conditions in as residual predicates.");

                // Checked on 17.0.1135.8 at level 170: ISNULL and COALESCE spellings compile as a single
                // plan, with no optional_predicate variants.
                var defaulted = uses.SelectMany(u => u.Defaulted).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (defaulted.Count > 0)
                {
                    text.Append(" It only recognizes (col = @p OR @p IS NULL), not ISNULL(@p, col) or COALESCE(@p, col), so ")
                        .Append(defaulted.Count == 1 ? "the condition on " : "the conditions on ")
                        .Append(PlanFormat.List(defaulted, MaxNamedParameters))
                        .Append(" would need rewriting in that form first. On a nullable column that also changes the results: ")
                        .Append("the OR form keeps rows where the column is NULL, which col = ISNULL(@p, col) leaves out.");
                }
            }

            text.Append("\nNone of these helps unless an index supports the conditions that remain. WITH RECOMPILE on a procedure does not help.");

            // Warning whatever operator it is on: whether a residual after a seek is cheap depends on
            // how selective the seek is, which the plan cannot tell us for other values.
            return PlanInsight.Analysis(
                isVariant ? PlanWarningSeverity.Information : PlanWarningSeverity.Warning,
                isVariant ? OptimizedOptionalParametersTitle : "Optional parameters",
                text.ToString(),
                operators);
        }

        /// <summary>
        /// A partitioned scan or seek reading every partition - see <see cref="PlanPartitionAccess"/>
        /// for how that is told from the plan.  Null when partitions were eliminated, when there is
        /// only one to read, or when the operator did not run.
        ///
        /// Showplan doesn't say which column the table is partitioned on, so the card can't say which
        /// predicate should have eliminated partitions, or that one was meant to.  It gives the
        /// general advice, and names the DATETIME values and mismatched type ranges it finds on the
        /// operator as facts for the reader to judge: a DATETIME value against a DATETIME2 partition
        /// function is the easy one to miss, as it seeks and filters correctly and shows no
        /// conversion warning.
        /// </summary>
        private static PlanInsight? PartitionEliminationInsight(PlanOperator op, PlanStatement statement)
        {
            if (op.Partitions is not { IsEliminated: false } partitions || !ReadsPartitions(op.Kind)) return null;
            if (op.Runtime is { ActualExecutions: 0 }) return null;

            // A constant range that doesn't start at the first partition is IsEliminated; one from
            // the first partition is very likely all of them, and without a partition seek it is
            // certainly all of them.  A partition seek with neither is one this doesn't understand.
            if (!partitions.ReadsEveryPartition && partitions.ConstantRange is null) return null;

            var count = partitions.PartitionsAccessed ?? partitions.ConstantRange?.Count;
            if (count is <= 1) return null;

            var seeks = op.Kind is PlanOperatorKind.ClusteredIndexSeek or PlanOperatorKind.NonClusteredIndexSeek;
            var actual = op.HasRuntime;
            var table = op.PrimaryObject is { Table: { Length: > 0 } name } target
                ? string.IsNullOrEmpty(target.Schema) ? name : target.Schema + "." + name
                : "the table";

            // "Accessed", as SQL Server's own Partitions Accessed says: it doesn't suggest a seek read
            // everything in each partition, as "read" would.
            var text = new StringBuilder("This query's ")
                .Append(seeks ? "seek " : "scan ")
                .Append(actual ? "accessed " : "accesses ");

            if (partitions.ReadsEveryPartition)
            {
                text.Append(count is { } all ? "all " + all.ToString("N0", CultureInfo.CurrentCulture) + " partitions" : "every partition")
                    .Append(" of ").Append(table)
                    .Append(". Partition elimination is not in effect.");
            }
            else
            {
                var range = partitions.ConstantRange!.Value;
                text.Append(count!.Value.ToString("N0", CultureInfo.CurrentCulture))
                    .Append(" partitions of ").Append(table)
                    .Append(" (").Append(range.Start.ToString(CultureInfo.CurrentCulture))
                    .Append(" to ").Append(range.End.ToString(CultureInfo.CurrentCulture))
                    .Append("). Partition elimination doesn't appear to be in effect: the range is fixed and starts at the first partition.");
            }

            // Two examples of different types, so neither reads as a guess at this table's.  Saying
            // the rows still come back right is what stops a reader dismissing it as working.
            text.Append(" Consider adding a predicate on the partitioning column. If there is one, the value's type must match the partition function's exactly, including precision. " +
                        "For example, a DATETIME value against a DATETIME2 partition function, or DATETIME2(7) against DATETIME2(3), " +
                        "prevents elimination even though the query still returns the right rows.");

            var datetimeValues = DatetimeValues(op, statement);
            var mismatched = MismatchedTypeRanges(op, statement);

            if (datetimeValues.Count > 0)
            {
                text.Append(" This operator compares against ")
                    .Append(datetimeValues.Count == 1 ? "a DATETIME value: " : "DATETIME values: ")
                    .Append(PlanFormat.List(datetimeValues, MaxNamedParameters));
            }

            if (mismatched.Count > 0)
            {
                text.Append(datetimeValues.Count > 0 ? ", and its" : " Its")
                    .Append(" seek range is worked out with ")
                    .Append(string.Join(" and ", mismatched))
                    .Append(", which SQL Server uses when a value's type differs from the column's");
            }

            if (datetimeValues.Count > 0 || mismatched.Count > 0)
            {
                text.Append(". If that comparison is with the partitioning column, it is the likely cause.");
            }

            return PlanInsight.Analysis(
                datetimeValues.Count > 0 || mismatched.Count > 0 ? PlanWarningSeverity.Warning : PlanWarningSeverity.Information,
                "No partition elimination",
                text.ToString(),
                [op]);
        }

        /// <summary>The operators that read a table's partitions, as opposed to writing to them.</summary>
        private static bool ReadsPartitions(PlanOperatorKind kind) => kind is
            PlanOperatorKind.TableScan or PlanOperatorKind.ClusteredIndexScan or PlanOperatorKind.NonClusteredIndexScan or
            PlanOperatorKind.ColumnstoreIndexScan or PlanOperatorKind.ClusteredIndexSeek or PlanOperatorKind.NonClusteredIndexSeek;

        /// <summary>The functions SQL Server builds a seek range with when the value's type isn't the column's.</summary>
        private static readonly string[] MismatchedTypeRangeFunctions = ["GetRangeWithMismatchedTypes", "GetRangeThroughConvert"];

        private static readonly Regex BracketedName = new(@"\[(?<n>[^\]]+)\]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex DatetimeConvert =
            new(@"\bCONVERT(?:_IMPLICIT)?\(datetime,", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// The operator's predicates, and what each of the plan's own names in them stands for, a
        /// few levels down: a seek's range is often an Expr1004 that a Compute Scalar below it works
        /// out, and the part that matters is in there.
        /// </summary>
        private static IReadOnlyList<string> PredicateTexts(PlanOperator op, PlanStatement statement)
        {
            var texts = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new[] { op.SeekPredicate, op.Predicate }.OfType<string>().ToList();

            for (var level = 0; level < 4 && pending.Count > 0; level++)
            {
                texts.AddRange(pending);
                pending = pending
                    .SelectMany(t => BracketedName.Matches(t).Cast<Match>().Select(m => m.Groups["n"].Value))
                    .Where(n => PlanExpressions.IsGeneratedName(n) && seen.Add(n))
                    .Select(n => statement.ExpressionNamed(n)?.Definition)
                    .OfType<string>()
                    .ToList();
            }

            return texts;
        }

        /// <summary>
        /// The DATETIME values the operator compares with: CONVERT(datetime, ...) expressions, and
        /// parameters or variables the plan says are DATETIME.  In the order found.
        /// </summary>
        private static IReadOnlyList<string> DatetimeValues(PlanOperator op, PlanStatement statement)
        {
            var datetimeParameters = new HashSet<string>(
                statement.Parameters
                    .Where(p => string.Equals(p.DataType, "datetime", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Name),
                StringComparer.OrdinalIgnoreCase);

            var found = new List<string>();
            foreach (var text in PredicateTexts(op, statement))
            {
                foreach (Match convert in DatetimeConvert.Matches(text))
                {
                    found.Add(Balanced(text, convert.Index, text.IndexOf('(', convert.Index)));
                }

                foreach (Match name in BracketedName.Matches(text))
                {
                    if (datetimeParameters.Contains(name.Groups["n"].Value)) found.Add(name.Groups["n"].Value);
                }
            }

            // A parameter already inside a CONVERT(datetime, ...) found above is said by that.
            return found
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(value => !found.Any(other => other.Length > value.Length && other.Contains("[" + value + "]")))
                .ToList();
        }

        /// <summary>The mismatched type range functions the operator's seek is worked out with.</summary>
        private static IReadOnlyList<string> MismatchedTypeRanges(PlanOperator op, PlanStatement statement)
        {
            var texts = PredicateTexts(op, statement);
            return MismatchedTypeRangeFunctions
                .Where(function => texts.Any(t => t.IndexOf(function + "(", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// The call that starts at <paramref name="start"/>, up to the bracket closing the one at
        /// <paramref name="open"/>, or to the end of the text when it isn't closed.
        /// </summary>
        private static string Balanced(string text, int start, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')' && --depth == 0) return text.Substring(start, i - start + 1);
            }

            return text.Substring(start);
        }

        /// <summary>
        /// The table variables in <paramref name="operators"/> - see <see cref="PlanTableVariables"/>:
        /// that they have no statistics, how many rows the optimizer believed each held when it
        /// compiled the plan, and what they mean for a parallel plan.  Information, as using one is
        /// a choice with trade-offs rather than a fault; the misestimate it causes shows on the
        /// arrows.  Null when there are no operators.
        /// </summary>
        private static PlanInsight? TableVariableInsight(PlanStatement statement, IReadOnlyList<PlanOperator> operators)
        {
            if (operators.Count == 0) return null;

            var names = operators.SelectMany(PlanTableVariables.NamesOn).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var one = names.Count == 1;

            var text = new StringBuilder(one ? "Table variable " : "Table variables ")
                .Append(PlanFormat.List(names, MaxNamedParameters))
                .Append(one ? " has" : " have")
                .Append(" no statistics, so the optimizer has to guess how many rows match a predicate or join on ")
                .Append(one ? "it" : "them")
                .Append('.');

            // What the optimizer believed each held, from the first read of it: a write doesn't say.
            var reads = operators
                .Where(op => op.TableCardinality is not null && !PlanTableVariables.Modifies(op))
                .GroupBy(op => PlanTableVariables.NamesOn(op).First(), StringComparer.OrdinalIgnoreCase)
                .Select(g => (Name: g.Key, Read: g.First()))
                .ToList();

            var compiledEmpty = false;
            var compiledFilled = false;

            foreach (var (name, read) in reads)
            {
                if (read.TableCardinality == 0)
                {
                    compiledEmpty = true;
                    text.Append(" The plan was compiled while ").Append(name).Append(" was empty, so it is costed for one row");

                    if (read.ActualRows is { } actual and > 1)
                    {
                        text.Append(", but ").Append(Nodes([read])).Append(" returned ").Append(RowCount(actual));
                    }

                    text.Append('.');
                    continue;
                }

                compiledFilled = true;
                var held = read.TableCardinality!.Value;
                text.Append(" The optimizer knew ").Append(name).Append(" held ").Append(RowCount(held)).Append(" when it compiled the plan");

                // Without a predicate the estimate is the table's size, and there was nothing to guess.
                // Compared per execution, as the table's size is: a scan of 100 rows on the inner side
                // of a loop that runs ten times is estimated at 1,000 in all, and guessed nothing.
                // Shown in all, though, alongside the node's actual rows, which are in all too.
                if (Math.Abs(read.EstimateRows - held) >= 1)
                {
                    text.Append(", but not how many of them match: it estimated ")
                        .Append(RowCount(read.EstimateRowsAllExecutions ?? read.EstimatedTotalRows))
                        .Append(" for ")
                        .Append(Nodes([read]));

                    if (read.ActualRows is { } actual) text.Append(", which returned ").Append(PlanFormat.Rows(actual));
                }

                text.Append('.');
            }

            if (compiledEmpty)
            {
                // Checked at levels 140 and 170: a SHOWPLAN plan reads zero at both, as nothing runs
                // to fill the table.  A plan from the cache or Query Store has no run figures either,
                // but a run compiled it, and the plan doesn't reliably say which of the two it is.
                text.Append(" Table variable deferred compilation (compatibility level 150, SQL Server 2019, and above) compiles a statement with the " +
                            "table's row count the first time it runs, and later runs reuse that plan; OPTION (RECOMPILE) compiles it with the current count every time.")
                    .Append(statement.IsActualPlan
                        ? " This plan was compiled with neither, or while the table was empty."
                        : " An estimated plan (SET SHOWPLAN_XML, or Display Estimated Execution Plan) always shows one row, as nothing runs to fill the table; " +
                          "a plan from the plan cache or Query Store was compiled with neither, or while the table was empty.");

                text.Append(" A plan costed for one row looks cheap, which favors nested loops joins and a serial plan.");
            }
            else if (compiledFilled && !PlanOptionalParameters.HasRecompileHint(statement.StatementText))
            {
                text.Append(" Unless the statement recompiles, later runs reuse this plan however many rows the table holds then.");
            }

            var modified = operators.Where(PlanTableVariables.Modifies)
                .SelectMany(PlanTableVariables.NamesOn)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            text.Append(modified.Count > 0 || IsTableVariableNonParallelReason(statement.NonParallelPlanReason)
                ? " This statement changes " + (modified.Count > 0 ? PlanFormat.List(modified, MaxNamedParameters) : "a table variable") +
                  ", so it runs on a single thread: SQL Server never uses a parallel plan for a statement that modifies a table variable."
                : " Reading a table variable can use a parallel plan, but the statement that fills it always runs on a single thread.");

            // A bare newline: a card's label counts "\r\n" as two characters and draws it as one.
            // INSERT ... SELECT runs in parallel only into a heap or a clustered columnstore index, and
            // only with TABLOCK, from compatibility level 130.
            text.Append("\nFor more than a few rows, consider a temp table (#table) instead: it has statistics, and filling one can use a parallel plan - " +
                        "SELECT ... INTO can, and so can INSERT ... SELECT into a heap with a TABLOCK hint (compatibility level 130, SQL Server 2016, and above).");

            return PlanInsight.Analysis(PlanWarningSeverity.Information, TableVariableTitle, text.ToString(), operators);
        }

        /// <summary>
        /// A call to a user-defined table-valued function - see <see cref="PlanFunctionCall"/>: the
        /// optimizer can't see inside it, so its row count is a guess unless interleaved execution
        /// ran it first.  Information, for the same reason as the table variable card.
        /// </summary>
        private static PlanInsight FunctionInsight(PlanStatement statement, PlanFunctionCall call)
        {
            var function = call.Function;
            var calls = function.Runtime?.LogicalExecutions ?? (long)Math.Round(function.EstimatedExecutions);
            var text = new StringBuilder();

            if (call.IsMultiStatement)
            {
                text.Append("Multi-statement table-valued function ").Append(call.Name)
                    .Append(": SQL Server runs it to fill a table variable with its rows, then reads them from there. ")
                    .Append("The work inside it isn't shown in this plan, its rows have no statistics, and filling the table variable runs on a single thread.");
            }
            else
            {
                // Called with arguments from the outer query, a multi-statement function streams its
                // rows like a CLR one does, and the plan doesn't say which it is.
                text.Append("Table-valued function ").Append(call.Name)
                    .Append(" (a multi-statement or CLR function): the work inside it isn't shown in this plan, and the optimizer can't see how many rows it returns.");
            }

            // What this run returned.  A function that fills a table returns its rows through a scan
            // of that table, and its own operator counts none.  The scan's rows read are the table's
            // size only if it ran once and read to the end - a TOP, an EXISTS or a merge join can stop
            // it early - and its rows out only if it had no predicate to filter them, too.
            var returned = call.Readers.Count > 0
                ? call.Readers.FirstOrDefault(r => IsScan(r.Kind) && r.Runtime is { LogicalExecutions: 1 } run &&
                                                   run.ActualEndOfScans is { } ends && ends >= run.ActualExecutions) is { Runtime: { } scan } reader
                    ? scan.ActualRowsRead ?? (string.IsNullOrEmpty(reader.Predicate) ? scan.ActualRows : null)
                    : null
                : function.Runtime?.ActualRows;

            // Interleaved execution compiles the plan for the count the function returned, which the
            // read of its table records.  Anything but the estimator's fixed guess on a plan with no run
            // figures means the same, as only a run is flagged: checked on a cached plan at level 170,
            // which carries the count and nothing else to say how it got it.  The guess is 1 with the
            // legacy estimator (model version 70) and 100 from SQL Server 2014's on; either, where the
            // plan doesn't say which estimator.
            var guess = statement.CardinalityEstimationModelVersion switch
            {
                null or "" => (double?)null,
                "70" => 1,
                _ => 100
            };
            var compiledFor = call.Readers.Select(r => r.TableCardinality).FirstOrDefault(c => c is not null) ?? function.EstimateRows;
            var fixedGuess = guess is { } g ? function.EstimateRows == g : function.EstimateRows is 100 or 1;

            if (call.IsInterleaved || (call.IsMultiStatement && !function.HasRuntime && !fixedGuess))
            {
                text.Append(call.IsInterleaved
                        ? " Interleaved execution ran it before the rest of the plan was compiled, so the plan was built for the " + RowCount(compiledFor) + " it returned then."
                        : " The plan was compiled for " + RowCount(compiledFor) + ", not a fixed guess: interleaved execution ran the function before compiling " +
                          "the rest of the plan, and that is how many it returned.");

                if (call.IsInterleaved && returned is { } now && now != (long)compiledFor)
                {
                    text.Append(" This run it returned ").Append(RowCount(now)).Append('.');
                }

                if (!PlanOptionalParameters.HasRecompileHint(statement.StatementText))
                {
                    text.Append(" Later runs reuse the plan, however many rows the function returns.");
                }
            }
            else if (call.IsMultiStatement)
            {
                // The fixed guesses, checked at compatibility levels 110 (legacy estimator), 130 and 170.
                text.Append(function.EstimateRows switch
                {
                    100 => " The optimizer used its fixed guess for a multi-statement function, 100 rows (the legacy cardinality estimator, from before SQL Server 2014, guesses 1)",
                    1 => " The optimizer used the legacy cardinality estimator's fixed guess for a multi-statement function, 1 row (SQL Server 2014's estimator and later guess 100)",
                    _ => " The optimizer estimated " + RowCount(function.EstimateRows)
                });

                if (returned is { } actual) text.Append(", and it returned ").Append(RowCount(actual));

                // Checked: not interleaved below level 140, nor in INSERT ... SELECT.
                text.Append(". Interleaved execution (compatibility level 140, SQL Server 2017, and above) runs a multi-statement function " +
                            "before compiling the rest of the plan, and plans for the rows it returns. ")
                    .Append(statement.IsActualPlan
                        ? "It didn't here: it needs level 140 or above, doesn't apply in a statement that modifies data, and can be switched off " +
                          "with the INTERLEAVED_EXECUTION_TVF database scoped configuration or the DISABLE_INTERLEAVED_EXECUTION_TVF hint."
                        : "It only happens when the query runs, and not in a statement that modifies data, so an estimated plan (SET SHOWPLAN_XML, or " +
                          "Display Estimated Execution Plan) always shows the fixed guess. A plan from the plan cache or Query Store that shows it was " +
                          "compiled without interleaved execution - unless the function happened to return exactly that many rows when interleaved " +
                          "execution ran it, which such a plan can't tell apart, as only a run's figures flag it.");
            }
            else
            {
                text.Append(" The optimizer estimated ").Append(RowCount(function.EstimateRows));
                if (calls > 1) text.Append(" per call");

                if (returned is { } actual)
                {
                    text.Append(", and it returned ").Append(RowCount(actual));
                    if (calls > 1) text.Append(" over ").Append(calls.ToString("N0", CultureInfo.CurrentCulture)).Append(" calls");
                }

                text.Append('.');

                // Checked: CROSS APPLY dbo.MSTVF(v.m) runs once per outer row, and isn't interleaved.
                if (calls > 1)
                {
                    text.Append(" If it is a multi-statement function, interleaved execution can't improve the estimate: " +
                                "it doesn't apply when the function's arguments come from the outer query, as they do when it runs once per outer row.");
                }
            }

            text.Append("\nIf it can be written as an inline table-valued function (RETURNS TABLE AS RETURN SELECT ...), SQL Server expands it " +
                        "into the calling query, where the optimizer estimates it like the rest of the query.");

            return PlanInsight.Analysis(PlanWarningSeverity.Information, FunctionTitle, text.ToString(), call.Operators);
        }

        private static bool IsScan(PlanOperatorKind kind) =>
            kind is PlanOperatorKind.TableScan or PlanOperatorKind.ClusteredIndexScan or PlanOperatorKind.NonClusteredIndexScan;

        /// <summary>
        /// The built-in table-valued functions in <paramref name="functions"/> - DMVs, STRING_SPLIT,
        /// OPENJSON - said once for all of them: a query over the DMVs reads half a dozen, and a card
        /// each would bury everything else.  Names the one whose estimate was furthest out, where a
        /// run says so.  Null when there are none.
        /// </summary>
        private static PlanInsight? BuiltInFunctionsInsight(IReadOnlyList<PlanOperator> functions)
        {
            if (functions.Count == 0) return null;

            // As the plan names them - SYSSESSIONS rather than sys.dm_exec_sessions - which is what
            // the operators show.
            var names = functions.Select(f => f.PrimaryObject?.Table is { Length: > 0 } table ? table : f.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var one = names.Count == 1;

            var text = new StringBuilder(one ? "Built-in table-valued function " : "Built-in table-valued functions ")
                .Append(PlanFormat.List(names, MaxNamedFunctions))
                .Append(": the optimizer has no statistics on the rows ")
                .Append(one ? "it returns, so its estimate is" : "they return, so their estimates are")
                .Append(" often a fixed guess, and the work ")
                .Append(one ? "it does" : "they do")
                .Append(" isn't shown in this plan.");

            var worst = functions
                .Where(f => f.RowEstimateError >= PlanLayoutEngine.EstimateMismatchThreshold)
                .OrderByDescending(f => f.RowEstimateError)
                .FirstOrDefault();

            if (worst is not null)
            {
                text.Append(functions.Count == 1 ? " It was estimated at " : " The furthest out was " + Nodes([worst]) + ": estimated at ")
                    .Append(RowCount(worst.EstimateRowsAllExecutions!.Value))
                    .Append(worst.Runtime!.LogicalExecutions > 1
                        ? " over " + worst.Runtime.LogicalExecutions.ToString("N0", CultureInfo.CurrentCulture) + " calls"
                        : string.Empty)
                    .Append(functions.Count == 1 ? " and returned " : ", it returned ")
                    .Append(RowCount(worst.Runtime!.ActualRows))
                    .Append('.');
            }

            text.Append("\nWhere a wrong estimate leads to a poor join or memory grant, putting the function's rows in a temp table first gives the optimizer statistics on them.");

            return PlanInsight.Analysis(PlanWarningSeverity.Information, BuiltInFunctionsTitle, text.ToString(), functions);
        }

        /// <summary>The most built-in functions the card names before the rest are counted.</summary>
        private const int MaxNamedFunctions = 4;

        private const string BuiltInFunctionsTitle = "Built-in table-valued functions";

        /// <summary>A row count with its noun: "1 row", "486 rows".</summary>
        private static string RowCount(double rows) => PlanFormat.Rows(rows) + (rows == 1 ? " row" : " rows");

        private const string TableVariableTitle = "Table variable";

        private const string FunctionTitle = "Table-valued function";

        private static bool IsTableVariableNonParallelReason(string? reason) =>
            string.Equals(reason, PlanTableVariables.NonParallelPlanReason, StringComparison.OrdinalIgnoreCase);

        /// <summary>The most parameters an optional parameters card names before the rest are counted.</summary>
        private const int MaxNamedParameters = 4;

        /// <summary>
        /// The title of the optional parameters note on a plan SQL Server 2025 already optimized -
        /// apart from the catch-all warning's, so comparing a catch-all plan with an optimized one
        /// shows the warning gone rather than the same title on both.
        /// </summary>
        private const string OptimizedOptionalParametersTitle = "Optional parameters (optimized)";

        /// <summary>The reason code showplan uses when a T-SQL UDF stops a plan going parallel.</summary>
        private const string TSqlUdfNonParallelReason = "TSQLUserDefinedFunctionsNotParallelizable";

        private static bool IsUdfNonParallelReason(string? reason) =>
            string.Equals(reason, TSqlUdfNonParallelReason, StringComparison.OrdinalIgnoreCase);

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
