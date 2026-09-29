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
                // Any wait means the memory was already given to other queries - the server (or, under
                // Resource Governor, the query's pool) was short of workspace memory, and this query
                // queued (RESOURCE_SEMAPHORE) for its turn.
                insights.Add(new PlanInsight(
                    PlanWarningSeverity.Warning,
                    "The query waited " + Milliseconds(wait) + " for its memory grant before it could start running. " +
                    "Other queries were holding the available query memory (or, under Resource Governor, their pool's share of it), " +
                    "so this one had to queue (RESOURCE_SEMAPHORE) until enough was released. " +
                    "Look for large or excessive grants in the queries running alongside it."));
            }

            if (ExcessiveMemoryGrant(statement) is { } excessiveGrant) insights.Add(excessiveGrant);

            if (ScalarUdfInsight(statement) is { } udfInsight)
            {
                insights.Add(udfInsight);
            }

            if (OptionalParametersInsight(statement) is { } optional) insights.Add(optional);

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

            // Said above, as part of the scalar UDF card, rather than repeated here as a plain reason code.
            if (!IsUdfNonParallelReason(statement.NonParallelPlanReason) && !string.IsNullOrEmpty(statement.NonParallelPlanReason))
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
                .Concat(PlanOptionalParameters.On(op, statement) is { } use
                        && NothingLeftToOptimizeInsight(statement, [use]) is null
                    ? new[] { OptionalParametersInsight(statement, [use], onOneOperator: true) }
                    : Enumerable.Empty<PlanInsight>())
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

                return new PlanInsight(
                    PlanWarningSeverity.Warning,
                    "The optimizer wants a memory grant of " + PlanFormat.Kilobytes(desired) +
                    " (the server may cap it, and a parallel plan asks for more). While the query runs, a large " +
                    "grant is query memory other queries cannot be granted, and is a common cause of " +
                    "RESOURCE_SEMAPHORE waits for them. " + GrantSizingHint +
                    " An actual plan shows how much is really used.");
            }

            if (!grant.IsExcessive ||
                grant is not { GrantedMemoryKb: { } granted, MaxUsedMemoryKb: { } used, GrantUsedFraction: { } usedFraction })
            {
                return null;
            }

            return new PlanInsight(
                PlanWarningSeverity.Warning,
                "The query was granted " + PlanFormat.Kilobytes(granted) + " of memory but used only " +
                PlanFormat.Kilobytes(used) + " (" + PlanFormat.Percent(usedFraction) +
                "). The rest was query memory other queries could not be granted while this one ran, and an " +
                "over-sized grant is a common cause of RESOURCE_SEMAPHORE waits for other queries. " + GrantSizingHint);
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

            return new PlanInsight(PlanWarningSeverity.Critical, text.ToString());
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

            return new PlanInsight(PlanWarningSeverity.Information, text.ToString());
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
            return new PlanInsight(
                isVariant ? PlanWarningSeverity.Information : PlanWarningSeverity.Warning,
                text.ToString(),
                operators);
        }

        /// <summary>The most parameters an optional parameters card names before the rest are counted.</summary>
        private const int MaxNamedParameters = 4;

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
