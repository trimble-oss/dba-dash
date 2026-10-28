using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Compare
{
    /// <summary>
    /// Two statements' plans set against each other: the figures that say which ran better, and the
    /// operators, objects, waits, parameters and warnings that say why.
    ///
    /// The usual question is "what changed" - the plan from before an index or a code change and the
    /// plan after it, a fast execution and a slow one, the plan on one server and on another.  The
    /// answer is spread across the statement properties, every operator, the waits and the
    /// parameters, and reading two plans side by side for it means holding one in your head while
    /// reading the other.  So the figures are lined up here, once, with the direction each one is
    /// better in, and the viewer only has to show them.
    ///
    /// "Before" and "after" are only names for the two sides: every difference is the after side
    /// relative to the before side, and swapping them swaps every sign.
    ///
    /// A side can also be several statements - every statement of a batch or a procedure - for the
    /// change that rewrote it, splitting one statement into a few with temp tables.  The statements
    /// no longer match one for one, so they are not paired: each side's figures are added up (or the
    /// largest taken, for the ones that don't add, like a memory grant), and the totals compared.
    /// </summary>
    public sealed class PlanComparison
    {
        private PlanComparison(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after)
        {
            BeforeStatements = before;
            AfterStatements = after;
        }

        /// <summary>The before side's first statement - its only one unless <see cref="IsCombined"/>.</summary>
        public PlanStatement Before => BeforeStatements[0];

        public PlanStatement After => AfterStatements[0];

        public IReadOnlyList<PlanStatement> BeforeStatements { get; }

        public IReadOnlyList<PlanStatement> AfterStatements { get; }

        /// <summary>Either side is more than one statement, so the figures are totals across them.</summary>
        public bool IsCombined => BeforeStatements.Count > 1 || AfterStatements.Count > 1;

        /// <summary>Statement level figures, in the groups the summary shows them in.</summary>
        public IReadOnlyList<PlanComparisonMetric> Metrics { get; private set; } = [];

        /// <summary>The handful of differences worth reading first, most important first.</summary>
        public IReadOnlyList<PlanComparisonHighlight> Highlights { get; private set; } = [];

        /// <summary>Operators grouped by what they are, so a hash join that became a loop join shows as one row going and another arriving.</summary>
        public IReadOnlyList<PlanOperatorGroupComparison> Operators { get; private set; } = [];

        /// <summary>Operators grouped by the object and index they touch and how, so a scan that became a seek is visible at a glance.</summary>
        public IReadOnlyList<PlanOperatorGroupComparison> Objects { get; private set; } = [];

        public IReadOnlyList<PlanWaitComparison> Waits { get; private set; } = [];

        public IReadOnlyList<PlanParameterComparison> Parameters { get; private set; } = [];

        public IReadOnlyList<PlanWarningComparison> Warnings { get; private set; } = [];

        public IReadOnlyList<PlanMissingIndexComparison> MissingIndexes { get; private set; } = [];

        /// <summary>True when both statements carry measurements, so run time figures can be compared at all.</summary>
        public bool BothActual => IsActual(BeforeStatements) && IsActual(AfterStatements);

        /// <summary>Every statement of the side carries measurements.</summary>
        private static bool IsActual(IReadOnlyList<PlanStatement> statements) => statements.All(s => s.IsActualPlan);

        /// <summary>
        /// Showplan's query hash matches: the two are the same query, give or take literals.  When it
        /// does not, they may still be - a comment or a changed column list changes the hash - but it
        /// is worth saying, since comparing two different queries is rarely what was meant.  For
        /// several statements, every statement matches the one in the same place on the other side.
        /// </summary>
        public bool? SameQuery => HashesMatch(BeforeStatements, AfterStatements, s => s.QueryHash);

        /// <summary>Showplan's query plan hash matches: the optimiser chose the same shape both times.</summary>
        public bool? SamePlanShape => HashesMatch(BeforeStatements, AfterStatements, s => s.QueryPlanHash);

        /// <summary>
        /// Compare <paramref name="before"/> with <paramref name="after"/>.  The builds are the SQL
        /// Server versions the plans came from, where the caller has them - a plan that changed
        /// across an upgrade is one of the reasons to compare.
        /// </summary>
        public static PlanComparison Compare(PlanStatement before, PlanStatement after, string? beforeBuild = null, string? afterBuild = null)
        {
            ArgumentNullException.ThrowIfNull(before);
            ArgumentNullException.ThrowIfNull(after);

            return Compare([before], [after], beforeBuild, afterBuild);
        }

        /// <summary>
        /// Compare one set of statements with another as a whole - a procedure before and after it
        /// was rewritten - by the totals of each side's figures.
        /// </summary>
        public static PlanComparison Compare(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after, string? beforeBuild = null, string? afterBuild = null)
        {
            ArgumentNullException.ThrowIfNull(before);
            ArgumentNullException.ThrowIfNull(after);
            if (before.Count == 0) throw new ArgumentException("No statements to compare.", nameof(before));
            if (after.Count == 0) throw new ArgumentException("No statements to compare.", nameof(after));

            var comparison = new PlanComparison(before, after);
            comparison.Metrics = BuildMetrics(before, after, beforeBuild, afterBuild);
            comparison.Operators = GroupOperators(before, after, byObject: false);
            comparison.Objects = GroupOperators(before, after, byObject: true);
            comparison.Waits = CompareWaits(before, after);
            comparison.Parameters = CompareParameters(before, after);
            comparison.Warnings = CompareWarnings(before, after);
            comparison.MissingIndexes = CompareMissingIndexes(before, after);
            comparison.Highlights = BuildHighlights(comparison);
            return comparison;
        }

        /// <summary>The metric with <paramref name="name"/>, or null when neither side had it.</summary>
        public PlanComparisonMetric? Metric(string name) =>
            Metrics.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.Ordinal));

        /// <summary>
        /// The summary as plain text, for pasting into a ticket: a line per figure, with the change.
        /// </summary>
        public string ToText()
        {
            var text = new System.Text.StringBuilder();

            foreach (var highlight in Highlights) text.Append("* ").AppendLine(highlight.Text);
            if (Highlights.Count > 0) text.AppendLine();

            foreach (var group in Metrics.GroupBy(m => m.Group))
            {
                text.AppendLine(group.Key);
                foreach (var metric in group)
                {
                    text.Append("  ").Append(metric.Name).Append(": ")
                        .Append(metric.BeforeText ?? "-").Append(" -> ").Append(metric.AfterText ?? "-");
                    if (!string.IsNullOrEmpty(metric.DifferenceText)) text.Append("  (").Append(metric.DifferenceText).Append(')');
                    text.AppendLine();
                }

                text.AppendLine();
            }

            return text.ToString().TrimEnd() + Environment.NewLine;
        }

        private static bool? HashesMatch(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after, Func<PlanStatement, string?> hash)
        {
            if (before.Concat(after).Any(s => string.IsNullOrEmpty(hash(s)))) return null;
            if (before.Count != after.Count) return false;

            return before.Zip(after).All(pair => string.Equals(hash(pair.First), hash(pair.Second), StringComparison.OrdinalIgnoreCase));
        }

        // ---------------------------------------------------------------- statement figures

        // Groups, in the order the summary shows them.
        internal const string RuntimeGroup = "Run Time";
        internal const string IoGroup = "I/O";
        internal const string MemoryGroup = "Memory Grant";
        internal const string EstimatesGroup = "Estimates";
        internal const string ParallelismGroup = "Parallelism";
        internal const string ShapeGroup = "Plan Shape";
        internal const string CompilationGroup = "Compilation";
        internal const string IdentityGroup = "Statement";

        // Names the highlights and the tests look metrics up by.
        public const string ElapsedName = "Elapsed Time";
        public const string CpuName = "CPU Time";
        public const string WaitName = "Wait Time";
        public const string LogicalReadsName = "Logical Reads";
        public const string PhysicalReadsName = "Physical Reads";
        public const string GrantedName = "Granted";
        public const string GrantWaitName = "Grant Wait";
        public const string CostName = "Estimated Cost";
        public const string SpillsName = "Spills";
        public const string GrantUsedName = "Grant Used";
        public const string ExcessiveGrantName = "Excessive Grant";
        public const string WarningsName = "Warnings";
        public const string DopName = "Degree of Parallelism";
        public const string OperatorsName = "Operators";
        public const string StatementsName = "Statements";

        // Differences smaller than these are noise between two runs of the same plan, not a change.
        public const double MillisecondsNoise = 10;
        public const double KilobytesNoise = 1024;

        private static IReadOnlyList<PlanComparisonMetric> BuildMetrics(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after, string? beforeBuild, string? afterBuild)
        {
            var metrics = new List<PlanComparisonMetric>();
            // An estimated plan can't have the warnings only a run raises, so against an actual plan
            // they would count as the actual plan's fault.  Left out of both sides when the types differ.
            var countRuntimeWarnings = IsActual(before) == IsActual(after);
            var b = StatementFigures.For(before, countRuntimeWarnings);
            var a = StatementFigures.For(after, countRuntimeWarnings);
            var combined = before.Count > 1 || after.Count > 1;

            // Said of the figures that don't add up across statements - a grant is held by one
            // statement at a time, so the total of them is memory no query ever had.
            var largest = combined ? "  Across several statements, the largest of them." : string.Empty;

            void Number(string group, string name, PlanComparisonUnit unit, PlanComparisonDirection direction,
                Func<StatementFigures, double?> figure, string description, double minimumDifference = 0, bool estimate = false)
            {
                var metric = PlanComparisonMetric.Number(group, name, unit, direction, figure(b), figure(a), description, minimumDifference, estimate);
                if (metric is not null) metrics.Add(metric);
            }

            void Text(string group, string name, Func<StatementFigures, string?> figure, string description)
            {
                var metric = PlanComparisonMetric.Text(group, name, figure(b), figure(a), description);
                if (metric is not null) metrics.Add(metric);
            }


            Number(RuntimeGroup, ElapsedName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Measured(f.SumOfAll(s => s.QueryTimeStats?.ElapsedMs)), "How long the statement took, start to finish.", MillisecondsNoise);
            Number(RuntimeGroup, CpuName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Measured(f.SumOfAll(s => s.QueryTimeStats?.CpuMs)), "CPU used across every thread.", MillisecondsNoise);
            Number(RuntimeGroup, "CPU per Elapsed", PlanComparisonUnit.Multiple, PlanComparisonDirection.Neutral,
                f => f.CpuPerElapsed, "CPU time divided by elapsed time: roughly how many cores were busy on average.  Well under 1 is a query that spent its time waiting.");
            Number(RuntimeGroup, WaitName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.WaitMs, "The total of the waits the plan recorded - see the Waits tab for which.", MillisecondsNoise);
            Number(RuntimeGroup, "UDF Elapsed Time", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Measured(f.Sum(s => s.QueryTimeStats?.UdfElapsedMs)), "Time spent inside scalar user defined functions.", MillisecondsNoise);
            Number(RuntimeGroup, "UDF CPU Time", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Measured(f.Sum(s => s.QueryTimeStats?.UdfCpuMs)), "CPU spent inside scalar user defined functions.", MillisecondsNoise);
            Number(RuntimeGroup, "Rows Returned", PlanComparisonUnit.Rows, PlanComparisonDirection.Neutral,
                f => f.Measured(f.Sum(s => s.RootOperator?.ActualRows)), "Rows the statement produced.  A different count usually means different parameters or data, which makes the other figures harder to compare." +
                                                             (combined ? "  Across several statements, rows inserted into a temp table count as well as rows returned." : string.Empty));

            Number(IoGroup, LogicalReadsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.LogicalReads, "Pages read from the buffer pool, summed over every operator.");
            Number(IoGroup, PhysicalReadsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.PhysicalReads, "Pages read from disk.  Depends on what was already cached as much as on the plan.");
            Number(IoGroup, "Read-Ahead Reads", PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.ReadAheads, "Pages read from disk ahead of being needed.");
            Number(IoGroup, "LOB Logical Reads", PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.LobLogicalReads, "Large object pages read from the buffer pool.");
            Number(IoGroup, "Rows Read", PlanComparisonUnit.Rows, PlanComparisonDirection.LowerIsBetter,
                f => f.RowsRead, "Rows the scans and seeks examined before their predicates, where SQL Server reported it.");

            Number(MemoryGroup, "Requested", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Max(s => s.MemoryGrant?.RequestedMemoryKb), "Query memory the statement asked for, sized from the optimiser's estimates.  More is not worse in itself: a grant sized from better estimates is often what stops a spill." + largest, KilobytesNoise, estimate: true);
            Number(MemoryGroup, GrantedName, PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Max(s => s.MemoryGrant?.GrantedMemoryKb), "Query memory it was given - memory no other query could use meanwhile.  Whether it was the right size is Grant Used and Spills." + largest, KilobytesNoise, estimate: true);
            Number(MemoryGroup, "Max Used", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.Neutral,
                f => f.Measured(f.Max(s => s.MemoryGrant?.MaxUsedMemoryKb)), "The most of the grant that was used." + largest, KilobytesNoise);
            // Neither way is better: low is memory reserved for nothing, high is little headroom before a
            // spill, and anywhere between is fine.  The two ends that are a problem are judged on their
            // own - an excessive grant below, and a spill by Spills.
            Number(MemoryGroup, GrantUsedName, PlanComparisonUnit.Percent, PlanComparisonDirection.Neutral,
                f => f.Measured(f.LargestGrant?.GrantUsedFraction),
                "The share of the grant that was used.  Neither higher nor lower is better in itself: very low on a large grant is memory reserved for nothing (see Excessive Grant), and very high leaves little headroom before a spill (see Spills)." +
                (combined ? "  Across several statements, of the largest grant." : string.Empty), 0.05);

            var excessive = PlanComparisonMetric.Flag(MemoryGroup, ExcessiveGrantName,
                b.ExcessiveGrant, a.ExcessiveGrant,
                "A large grant that went mostly unused: at least " + PlanFormat.Kilobytes(PlanMemoryGrantInfo.ExcessiveGrantMinKb) +
                " granted and under " + PlanFormat.Percent(PlanMemoryGrantInfo.ExcessiveGrantMaxUsedFraction) +
                " of it used - the same rule as the viewer's excessive grant warning.  Memory reserved that other queries could not have." +
                (combined ? "  Across several statements, any of them." : string.Empty));
            if (excessive is not null) metrics.Add(excessive);
            Number(MemoryGroup, "Desired", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.Neutral,
                f => f.Max(s => s.MemoryGrant?.EffectiveDesiredMemoryKb), "What the optimiser wanted, from its estimates." + largest, KilobytesNoise, estimate: true);
            Number(MemoryGroup, GrantWaitName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Measured(f.Sum(s => s.MemoryGrant?.GrantWaitTimeMs)), "Time spent waiting for the grant (RESOURCE_SEMAPHORE).", MillisecondsNoise);
            Number(MemoryGroup, SpillsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.Spills, "Operators that ran out of memory and spilled to tempdb.");

            Number(EstimatesGroup, CostName, PlanComparisonUnit.Cost, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.StatementSubTreeCost), "The optimiser's cost for the statement.  A model figure, not a time: it says what the optimiser expected, which is not always what happened.  Lower is better, but a plan built from more accurate estimates can cost more and still run faster.", estimate: true);
            Number(EstimatesGroup, "Estimated Rows", PlanComparisonUnit.Rows, PlanComparisonDirection.Neutral,
                f => f.Sum(s => s.StatementEstRows), "Rows the optimiser expected the statement to return.");
            Number(EstimatesGroup, "Worst Row Estimate", PlanComparisonUnit.Multiple, PlanComparisonDirection.LowerIsBetter,
                f => f.WorstEstimateError, "The furthest any operator's row estimate was out, either way.  Bad estimates are the usual reason the optimiser chose a poor plan.");
            Text(EstimatesGroup, "Cardinality Estimator", f => f.Distinct(s => s.CardinalityEstimationModelVersion),
                "The cardinality estimator version - a change here, from a compatibility level or a hint, changes estimates across the plan.");

            Number(ParallelismGroup, DopName, PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Max(s => s.DegreeOfParallelism), "The degree of parallelism the plan was built for." + largest);
            Number(ParallelismGroup, "Threads Reserved", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Max(s => s.ReservedThreads), "Worker threads reserved for the parallel branches." + largest);
            Number(ParallelismGroup, "Threads Used", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Max(s => s.UsedThreads), "Worker threads that were used." + largest);
            Text(ParallelismGroup, "Non-Parallel Reason", f => f.Distinct(s => s.NonParallelPlanReason),
                "Why the optimiser could not use a parallel plan, where it said.");

            if (combined)
            {
                Number(ShapeGroup, StatementsName, PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                    f => f.Statements.Count, "Statements compared on this side, every figure being their total.");
            }

            Number(ShapeGroup, OperatorsName, PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.OperatorCount, "Operators in the plan.");
            Number(ShapeGroup, WarningsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.WarningCount, "Warnings SQL Server put on the statement and its operators - listed, with DBA Dash's own findings, on the Insights tab." +
                                     (countRuntimeWarnings ? string.Empty : "  Not counting the warnings only a run can raise - spills, grant warnings, waits - since one plan is estimated."));
            Number(ShapeGroup, "Missing Indexes", PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.MissingIndexes.Count), "Indexes the optimiser said it wanted.");
            Number(ShapeGroup, "Scans", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Scans, "Table and index scans.");
            Number(ShapeGroup, "Seeks", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Seeks, "Index seeks.");
            Number(ShapeGroup, "Lookups", PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.Lookups, "Key and RID lookups - a seek fetching columns its index did not cover.");
            Number(ShapeGroup, "Sorts", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Sorts, "Sort operators.");
            Number(ShapeGroup, "Batch Mode Operators", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.BatchModeOperators, "Operators the optimiser planned to run in batch mode.");

            Number(CompilationGroup, "Compile Time", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.CompileTimeMs), "Time taken to compile the plan.", MillisecondsNoise);
            Number(CompilationGroup, "Compile CPU", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.CompileCpuMs), "CPU taken to compile the plan.", MillisecondsNoise);
            Number(CompilationGroup, "Compile Memory", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.CompileMemoryKb), "Memory used to compile the plan.", KilobytesNoise);
            Number(CompilationGroup, "Cached Plan Size", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Sum(s => s.CachedPlanSizeKb), "The plan's size in the plan cache.");
            Text(CompilationGroup, "Optimisation Level", f => f.Distinct(s => s.OptimisationLevel),
                "TRIVIAL or FULL.  A trivial plan was never costed against alternatives.");
            Text(CompilationGroup, "Early Abort Reason", f => f.Distinct(s => s.OptimisationEarlyAbortReason),
                "Why the optimiser stopped searching early, where it did - a time out means it may have missed a better plan.");

            Text(IdentityGroup, "Plan Type", f => f.PlanType,
                "Actual plans carry run time figures; estimated plans only the optimiser's expectations.");
            Text(IdentityGroup, "Statement Type", f => f.Distinct(s => s.StatementType), "SELECT, INSERT, UPDATE and the rest.");
            // A list of hashes is unreadable, and statements that don't pair up have none to compare.
            if (!combined)
            {
                Text(IdentityGroup, "Query Hash", f => f.Statements[0].QueryHash,
                    "Identifies the query, give or take literals.  Different hashes usually mean different queries.");
                Text(IdentityGroup, "Query Plan Hash", f => f.Statements[0].QueryPlanHash,
                    "Identifies the plan's shape.  The same hash means the optimiser chose the same plan.");
            }

            var build = PlanComparisonMetric.Text(IdentityGroup, "SQL Server Build", beforeBuild, afterBuild, "The SQL Server version the plan came from.");
            if (build is not null) metrics.Add(build);

            return metrics;
        }

        /// <summary>
        /// A side's figures that have to be worked out - from the operators, or across its statements -
        /// worked out once.  With one statement, every total is that statement's own figure.
        /// </summary>
        private sealed class StatementFigures
        {
            public required IReadOnlyList<PlanStatement> Statements { get; init; }
            public double? CpuPerElapsed { get; init; }
            public double? WaitMs { get; init; }
            public double? LogicalReads { get; init; }
            public double? PhysicalReads { get; init; }
            public double? ReadAheads { get; init; }
            public double? LobLogicalReads { get; init; }
            public double? RowsRead { get; init; }
            public double? Spills { get; init; }

            /// <summary>The grant was excessive, or null where there is no used figure to judge it by - an estimated plan.</summary>
            public bool? ExcessiveGrant { get; init; }

            /// <summary>The biggest grant of the side's statements, which Granted and Grant Used are of.</summary>
            public PlanMemoryGrantInfo? LargestGrant { get; init; }

            /// <summary>The grant the excessive grant figure is about: the biggest excessive one, or the biggest.</summary>
            public PlanMemoryGrantInfo? ExcessiveOrLargestGrant { get; init; }

            /// <summary>
            /// Every statement of the side was measured, or none was.  A side of actual and estimated
            /// statements has run time figures for only some of them, and a total of those would be
            /// compared as though it were the whole - so it has none.
            /// </summary>
            public bool Complete { get; init; }

            /// <summary>A run time figure, kept only where the side is <see cref="Complete"/>.</summary>
            public double? Measured(double? value) => Complete ? value : null;

            /// <summary>The statements' figure added up, or null unless every one of them has it.</summary>
            public double? SumOfAll(Func<PlanStatement, double?> figure) =>
                Statements.All(s => figure(s) is not null) ? Sum(figure) : null;
            public double? WorstEstimateError { get; init; }
            public int OperatorCount { get; init; }
            public int WarningCount { get; init; }
            public int Scans { get; init; }
            public int Seeks { get; init; }
            public int Lookups { get; init; }
            public int Sorts { get; init; }
            public int BatchModeOperators { get; init; }
            public required string PlanType { get; init; }

            /// <summary>The statements' figure added up, or null when none of them has it.</summary>
            public double? Sum(Func<PlanStatement, double?> figure)
            {
                double? total = null;
                foreach (var statement in Statements)
                {
                    if (figure(statement) is { } value) total = (total ?? 0) + value;
                }

                return total;
            }

            public double? Max(Func<PlanStatement, double?> figure) =>
                Statements.Select(figure).Max();

            /// <summary>The statements' differing values, listed once each in order.</summary>
            public string? Distinct(Func<PlanStatement, string?> figure)
            {
                var values = Statements.Select(figure).Where(v => !string.IsNullOrEmpty(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return values.Count == 0 ? null : string.Join(", ", values);
            }

            public static StatementFigures For(IReadOnlyList<PlanStatement> statements, bool countRuntimeWarnings)
            {
                var operators = statements.SelectMany(s => s.Operators).ToList();
                var runtime = operators.Where(o => o.Runtime is not null).Select(o => o.Runtime!).ToList();
                var actual = statements.Where(s => s.IsActualPlan).ToList();
                var warnings = statements.SelectMany(s => s.AllWarnings).ToList();
                var complete = actual.Count == 0 || actual.Count == statements.Count;
                var timed = statements.All(s => s.QueryTimeStats is { CpuMs: not null, ElapsedMs: not null });
                var cpu = statements.Sum(s => s.QueryTimeStats?.CpuMs ?? 0);
                var elapsed = statements.Sum(s => s.QueryTimeStats?.ElapsedMs ?? 0);
                var grants = statements.Select(s => s.MemoryGrant).Where(g => g is not null).Select(g => g!).ToList();
                var judged = grants.Where(g => g.GrantUsedFraction is not null).ToList();

                return new StatementFigures
                {
                    Statements = statements,
                    Complete = complete,
                    CpuPerElapsed = complete && timed && elapsed > 0 ? cpu / (double)elapsed : null,
                    // Wait stats came to showplan with the time stats (SQL Server 2016 SP1), so an actual
                    // plan without time stats is one from before that - no figure, rather than no waits.
                    WaitMs = HasWaitStats(statements) ? statements.Sum(s => s.WaitStats.Sum(w => w.WaitTimeMs)) : null,
                    LogicalReads = complete ? SumOf(runtime, r => r.ActualLogicalReads) : null,
                    PhysicalReads = complete ? SumOf(runtime, r => r.ActualPhysicalReads) : null,
                    ReadAheads = complete ? SumOf(runtime, r => r.ActualReadAheads) : null,
                    LobLogicalReads = complete ? SumOf(runtime, r => r.ActualLobLogicalReads) : null,
                    RowsRead = complete ? SumOf(runtime, r => r.ActualRowsRead) : null,
                    // Only a run can spill, so an estimated plan has no figure rather than a zero.
                    Spills = actual.Count > 0 && complete ? actual.Sum(s => s.AllWarnings.Count(w => w.IsSpill)) : null,
                    ExcessiveGrant = judged.Count > 0 && complete ? judged.Any(g => g.IsExcessive) : null,
                    // By size alone for Granted and Grant Used, so both describe the same grant.
                    LargestGrant = grants.MaxBy(g => g.GrantedMemoryKb ?? 0),
                    ExcessiveOrLargestGrant = grants.OrderByDescending(g => g.IsExcessive).ThenByDescending(g => g.GrantedMemoryKb ?? 0).FirstOrDefault(),
                    WorstEstimateError = operators.Select(o => o.RowEstimateError).Max(),
                    OperatorCount = operators.Count,
                    WarningCount = warnings.Count(w => countRuntimeWarnings || !w.IsRuntime),
                    Scans = operators.Count(o => IsScan(o.Kind)),
                    Seeks = operators.Count(o => IsSeek(o.Kind) && !o.IsLookup),
                    Lookups = operators.Count(o => o.IsLookup || o.Kind is PlanOperatorKind.KeyLookup or PlanOperatorKind.RidLookup),
                    Sorts = operators.Count(o => o.Kind is PlanOperatorKind.Sort or PlanOperatorKind.TopSort),
                    BatchModeOperators = operators.Count(o => string.Equals(o.EstimatedExecutionMode, "Batch", StringComparison.OrdinalIgnoreCase)),
                    PlanType = actual.Count == statements.Count ? "Actual" : actual.Count == 0 ? "Estimated" : "Mixed"
                };
            }

            private static double? SumOf(List<PlanRuntimeCounters> runtime, Func<PlanRuntimeCounters, long?> selector)
            {
                double? total = null;
                foreach (var counters in runtime)
                {
                    if (selector(counters) is { } value) total = (total ?? 0) + value;
                }

                return total;
            }
        }

        private static bool IsScan(PlanOperatorKind kind) =>
            kind is PlanOperatorKind.TableScan
                or PlanOperatorKind.ClusteredIndexScan
                or PlanOperatorKind.NonClusteredIndexScan
                or PlanOperatorKind.ColumnstoreIndexScan;

        private static bool IsSeek(PlanOperatorKind kind) =>
            kind is PlanOperatorKind.ClusteredIndexSeek
                or PlanOperatorKind.NonClusteredIndexSeek;

        // ---------------------------------------------------------------- operators and objects

        private static IReadOnlyList<PlanOperatorGroupComparison> GroupOperators(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after, bool byObject)
        {
            var groups = new Dictionary<string, PlanOperatorGroupComparison>(StringComparer.OrdinalIgnoreCase);

            void Add(IReadOnlyList<PlanStatement> statements, bool isBefore)
            {
                // An operator's cost share is of its own statement, so across several it is weighted by
                // that statement's share of the side's cost.
                var sideCost = statements.Sum(EffectiveCost);
                foreach (var (statement, op) in statements.SelectMany(s => s.Operators.Select(o => (s, o))))
                {
                    if (op.NodeId < 0) continue;

                    string key;
                    string? objectName = null, index = null;
                    var name = OperatorName(op);

                    if (byObject)
                    {
                        if (op.PrimaryObject is not { } target || string.IsNullOrEmpty(target.QualifiedTableName)) continue;
                        objectName = target.QualifiedTableName;
                        index = target.Index;
                        key = objectName + "|" + index + "|" + name;
                    }
                    else
                    {
                        key = name;
                    }

                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new PlanOperatorGroupComparison(name, objectName, index);
                        groups.Add(key, group);
                    }

                    var weight = statements.Count > 1 && sideCost > 0 ? EffectiveCost(statement) / sideCost : 1;
                    (isBefore ? group.BeforeFigures : group.AfterFigures).Add(op, weight);
                }
            }

            Add(before, isBefore: true);
            Add(after, isBefore: false);

            // The rows that changed the most come first: something new or gone, then by the larger
            // cost share either side - the operator the plan is about, on at least one of the sides.
            return groups.Values
                .OrderBy(g => g.Status == PlanPresence.Both ? 1 : 0)
                .ThenByDescending(g => Math.Max(g.Before?.CostShare ?? 0, g.After?.CostShare ?? 0))
                .ThenBy(g => g.Object, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Operator, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The cost an operator's <see cref="PlanOperator.CostPercent"/> is a share of - the parser's
        /// statement total, which falls back to the root's subtree cost where showplan gives the
        /// statement none (a cursor operation).
        /// </summary>
        private static double EffectiveCost(PlanStatement statement) =>
            Math.Max(statement.StatementSubTreeCost, statement.RootOperator?.EstimatedTotalSubtreeCost ?? 0);

        /// <summary>
        /// The operator's caption, with its logical operation where that says which job it is doing -
        /// a Hash Match that aggregates and one that joins are different operators for this purpose.
        /// </summary>
        public static string OperatorName(PlanOperator op)
        {
            if (op.IsLookup && op.Kind is not PlanOperatorKind.KeyLookup) return "Key Lookup";

            // The physical name under the logical one, rather than the caption: the caption of a
            // hash join already says "(Join)", and "Hash Match (Join) (Left Outer Join)" says it twice.
            return !string.IsNullOrEmpty(op.LogicalOp) &&
                   !string.IsNullOrEmpty(op.PhysicalOp) &&
                   !string.Equals(op.LogicalOp, op.PhysicalOp, StringComparison.OrdinalIgnoreCase) &&
                   op.Kind is PlanOperatorKind.HashMatchJoin or PlanOperatorKind.HashMatchAggregate or PlanOperatorKind.MergeJoin
                       or PlanOperatorKind.NestedLoops or PlanOperatorKind.AdaptiveJoin or PlanOperatorKind.Sort
                ? op.PhysicalOp + " (" + op.LogicalOp + ")"
                : op.DisplayName;
        }

        // ---------------------------------------------------------------- waits, parameters, warnings

        private static IReadOnlyList<PlanWaitComparison> CompareWaits(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after)
        {
            var beforeWaits = before.SelectMany(s => s.WaitStats).GroupBy(w => w.WaitType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (Ms: g.Sum(w => w.WaitTimeMs), Count: g.Sum(w => w.WaitCount)), StringComparer.OrdinalIgnoreCase);
            var afterWaits = after.SelectMany(s => s.WaitStats).GroupBy(w => w.WaitType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (Ms: g.Sum(w => w.WaitTimeMs), Count: g.Sum(w => w.WaitCount)), StringComparer.OrdinalIgnoreCase);

            // A wait missing from one side only means none was recorded where that side could record
            // waits at all - an actual plan with time stats (see StatementFigures.WaitMs).
            var comparable = HasWaitStats(before) && HasWaitStats(after);

            return beforeWaits.Keys.Union(afterWaits.Keys, StringComparer.OrdinalIgnoreCase)
                .Select(type =>
                {
                    var hasBefore = beforeWaits.TryGetValue(type, out var b);
                    var hasAfter = afterWaits.TryGetValue(type, out var a);
                    return new PlanWaitComparison(type,
                        hasBefore ? b.Ms : null, hasAfter ? a.Ms : null,
                        hasBefore ? b.Count : null, hasAfter ? a.Count : null, comparable);
                })
                .OrderByDescending(w => Math.Max(w.BeforeMs ?? 0, w.AfterMs ?? 0))
                .ThenBy(w => w.WaitType, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool HasWaitStats(PlanStatement statement) => statement.IsActualPlan && statement.QueryTimeStats is not null;

        /// <summary>Every statement could record waits - with only some, a missing wait may just be one not measured.</summary>
        private static bool HasWaitStats(IReadOnlyList<PlanStatement> statements) => statements.All(HasWaitStats);

        private static IReadOnlyList<PlanParameterComparison> CompareParameters(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after)
        {
            // A procedure's statements share its parameters, so the first statement to have one speaks for it.
            var beforeParameters = before.SelectMany(s => s.Parameters).ToList();
            var afterParameters = after.SelectMany(s => s.Parameters).ToList();
            var names = beforeParameters.Select(p => p.Name)
                .Concat(afterParameters.Select(p => p.Name))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return names
                .Select(name => new PlanParameterComparison(
                    name,
                    beforeParameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)),
                    afterParameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        private static IReadOnlyList<PlanWarningComparison> CompareWarnings(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after)
        {
            // Keyed on the source as well as the title, so one of DBA Dash's findings is never counted
            // in with a plan warning that happens to share its name.
            var groups = new Dictionary<(bool IsAnalysis, string Title), PlanWarningComparison>();

            PlanWarningComparison Group(bool isAnalysis, string title)
            {
                var key = (isAnalysis, title.ToUpperInvariant());
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new PlanWarningComparison(title, isAnalysis);
                    groups.Add(key, group);
                }

                return group;
            }

            void Add(IReadOnlyList<PlanStatement> statements, bool isBefore)
            {
                foreach (var warning in statements.SelectMany(s => s.AllWarnings))
                {
                    Group(false, warning.Title).Add(warning.Severity, warning.IsRuntime, isBefore);
                }

                // DBA Dash's own findings too, as the viewer lists them beside the warnings - a scalar
                // UDF or an optimizer time out gone from the new plan is as much the answer as a spill
                // gone.  Not the note that a plan is estimated, which is about the plan, not the query.
                foreach (var insight in statements.SelectMany(PlanInsights.AnalysisFor).Where(i => i.Title != PlanInsights.EstimatedPlanTitle))
                {
                    Group(true, insight.Title!).Add(insight.Severity, insight.DependsOnPlanKind, isBefore);
                }
            }

            Add(before, isBefore: true);
            Add(after, isBefore: false);

            // A warning only a run can raise, against a plan that never ran, says nothing either way.
            var sameType = IsActual(before) == IsActual(after);
            foreach (var group in groups.Values)
            {
                group.Change = !sameType && group.DependsOnPlanKind ? PlanComparisonChange.NotComparable
                    : group.AfterCount == group.BeforeCount ? PlanComparisonChange.Same
                    // A note of DBA Dash's - a serial plan, parameters that ran with other values - is
                    // worth seeing come or go, but isn't better or worse for it.
                    : group.IsAnalysis && group.Severity == PlanWarningSeverity.Information ? PlanComparisonChange.Changed
                    : group.AfterCount < group.BeforeCount ? PlanComparisonChange.Better
                    : PlanComparisonChange.Worse;
            }

            return groups.Values
                .OrderByDescending(w => w.Severity)
                .ThenByDescending(w => Math.Abs(w.AfterCount - w.BeforeCount))
                .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<PlanMissingIndexComparison> CompareMissingIndexes(IReadOnlyList<PlanStatement> before, IReadOnlyList<PlanStatement> after)
        {
            // Matched on the index they describe, which is the recommendation - the impact is the
            // optimiser's guess for that plan and differs even for the same index.
            var beforeIndexes = before.SelectMany(s => s.MissingIndexes).GroupBy(i => i.CreateStatementOneLine, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var afterIndexes = after.SelectMany(s => s.MissingIndexes).GroupBy(i => i.CreateStatementOneLine, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return beforeIndexes.Keys.Union(afterIndexes.Keys, StringComparer.OrdinalIgnoreCase)
                .Select(key => new PlanMissingIndexComparison(
                    beforeIndexes.GetValueOrDefault(key), afterIndexes.GetValueOrDefault(key)))
                .OrderByDescending(i => Math.Max(i.Before?.Impact ?? 0, i.After?.Impact ?? 0))
                .ToList();
        }

        // ---------------------------------------------------------------- highlights

        /// <summary>The metrics a highlight is written for when they changed, in the order they are worth reading.</summary>
        private static readonly string[] HighlightedMetrics =
        [
            ElapsedName, CpuName, LogicalReadsName, PhysicalReadsName, WaitName, GrantWaitName, SpillsName, ExcessiveGrantName, WarningsName, CostName
        ];

        /// <summary>
        /// An excessive grant said with its figures, which are the point: "Excessive Grant: No → Yes"
        /// doesn't say how much memory, or how little of it was used.  Only called when both sides could
        /// be judged, so both have a grant with a used figure.
        /// </summary>
        private static string ExcessiveGrantText(PlanComparisonChange change, PlanMemoryGrantInfo before, PlanMemoryGrantInfo after)
        {
            static string Grant(PlanMemoryGrantInfo grant) =>
                PlanFormat.Kilobytes(grant.GrantedMemoryKb ?? 0) + " granted, " + PlanFormat.Percent(grant.GrantUsedFraction ?? 0) + " used";

            return change == PlanComparisonChange.Worse
                ? "Excessive memory grant: " + Grant(after) + " (was " + Grant(before) + ")."
                : "No longer an excessive memory grant: " + Grant(after) + " (was " + Grant(before) + ").";
        }

        private static string Statements(int count) => count.ToString(CultureInfo.InvariantCulture) + (count == 1 ? " statement" : " statements");

        /// <summary>The grant a side's excessive grant figure is about - see <see cref="StatementFigures.ExcessiveOrLargestGrant"/>.</summary>
        private static PlanMemoryGrantInfo? ExcessiveOrLargestGrant(IReadOnlyList<PlanStatement> statements) =>
            StatementFigures.For(statements, countRuntimeWarnings: true).ExcessiveOrLargestGrant;

        private static IReadOnlyList<PlanComparisonHighlight> BuildHighlights(PlanComparison comparison)
        {
            var highlights = new List<PlanComparisonHighlight>();
            var before = comparison.BeforeStatements;
            var after = comparison.AfterStatements;

            if (comparison.IsCombined)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Comparing " + Statements(before.Count) + " with " + Statements(after.Count) +
                    " as a whole: times, reads and costs are totals across them; memory grants and parallelism the largest."));
            }

            // Statements that don't pair up are expected when comparing a rewrite as a whole.
            if (comparison.SameQuery == false && !comparison.IsCombined)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "The query hashes differ - these may be different queries, so compare with care."));
            }

            if (!IsActual(before) || !IsActual(after))
            {
                var estimated = !IsActual(before) && !IsActual(after)
                    ? "Both plans are estimated"
                    : (IsActual(before) ? "The after plan" : "The before plan") + " is estimated";
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.NotComparable,
                    estimated + ", so run time figures can't be compared - only the optimiser's estimates."));
            }

            highlights.Add(comparison.SamePlanShape switch
            {
                true when comparison.IsCombined => new PlanComparisonHighlight(PlanComparisonChange.Same,
                    "Same plan shapes: every statement's query plan hash matches the statement in the same place."),
                false when comparison.IsCombined => new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Different plan shapes: the statements' query plan hashes don't all match one for one."),
                true => new PlanComparisonHighlight(PlanComparisonChange.Same,
                    "Same plan shape (the query plan hashes match): the same operators in the same arrangement, though estimates, memory grant and DOP can still differ."),
                false => new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Different plan shape (the query plan hashes differ)."),
                _ => new PlanComparisonHighlight(PlanComparisonChange.Changed, "The plans can't be matched by hash - see the operator and object differences.")
            });

            foreach (var name in HighlightedMetrics)
            {
                if (comparison.Metric(name) is not { Change: PlanComparisonChange.Better or PlanComparisonChange.Worse } metric) continue;

                var text = name == ExcessiveGrantName
                    ? ExcessiveGrantText(metric.Change, ExcessiveOrLargestGrant(before)!, ExcessiveOrLargestGrant(after)!)
                    : metric.Name + ": " + metric.BeforeText + " → " + metric.AfterText + " (" + metric.DifferenceText + ")";
                highlights.Add(new PlanComparisonHighlight(metric.Change, text, metric));
            }

            if (comparison.Metric(DopName) is { Change: PlanComparisonChange.Changed } dop)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Degree of parallelism changed: " + dop.BeforeText + " → " + dop.AfterText + ".", dop));
            }

            if (comparison.Metric("Cardinality Estimator") is { Change: PlanComparisonChange.Changed } ce)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Different cardinality estimator: " + ce.BeforeText + " → " + ce.AfterText + "."));
            }

            // The access methods that came and went - a scan that became a seek is the change most
            // plan comparisons are looking for.
            const int maxAccessChanges = 4;
            var accessChanges = comparison.Objects.Where(o => o.Status != PlanPresence.Both).ToList();
            foreach (var access in accessChanges.Take(maxAccessChanges))
            {
                var what = access.Operator + " on " + access.ObjectDisplayName;
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    access.Status == PlanPresence.AfterOnly ? "Added: " + what : "Removed: " + what));
            }

            if (accessChanges.Count > maxAccessChanges)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    (accessChanges.Count - maxAccessChanges).ToString(CultureInfo.InvariantCulture) + " more object access changes - see the Objects tab."));
            }

            var sniffed = comparison.Parameters.Where(p => p.CompiledValueDiffers).Select(p => p.Name).ToList();
            if (sniffed.Count > 0)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "Compiled for different parameter values: " + PlanFormat.List(sniffed, 3) + "."));
            }

            return highlights;
        }
    }

    /// <summary>Which way a figure is better in, for colouring its change.</summary>
    public enum PlanComparisonDirection
    {
        /// <summary>A change is worth seeing but is neither good nor bad in itself - a degree of parallelism, a row count.</summary>
        Neutral,
        LowerIsBetter,
        HigherIsBetter
    }

    /// <summary>What happened to a figure between the two plans.</summary>
    public enum PlanComparisonChange
    {
        Same,

        /// <summary>Different, but by less than the noise between two runs of one plan.</summary>
        Similar,

        Better,
        Worse,

        /// <summary>Different, in a figure that has no better direction.</summary>
        Changed,

        /// <summary>Only one side has the figure - an estimated plan against an actual one.</summary>
        NotComparable
    }

    /// <summary>Which way a number moved, by more than noise.</summary>
    public enum PlanComparisonTrend
    {
        /// <summary>The same, near enough; or a text figure, or one only a side has.</summary>
        None,
        Higher,
        Lower
    }

    public enum PlanComparisonUnit
    {
        Count,
        Rows,
        Cost,
        Milliseconds,
        Kilobytes,

        /// <summary>A fraction from 0 to 1, shown as a percentage.</summary>
        Percent,

        /// <summary>A ratio shown as "3.2x".</summary>
        Multiple,
        Text
    }

    /// <summary>One statement level figure, both sides of it, and what changed.</summary>
    public sealed class PlanComparisonMetric
    {
        private PlanComparisonMetric(string group, string name, PlanComparisonUnit unit, PlanComparisonDirection direction, string description)
        {
            Group = group;
            Name = name;
            Unit = unit;
            Direction = direction;
            Description = description;
        }

        public string Group { get; }

        public string Name { get; }

        public string Description { get; }

        public PlanComparisonUnit Unit { get; }

        public PlanComparisonDirection Direction { get; }

        /// <summary>The before figure, or null for a text figure and for a figure the side does not have.</summary>
        public double? Before { get; private init; }

        public double? After { get; private init; }

        public string? BeforeText { get; private init; }

        public string? AfterText { get; private init; }

        public PlanComparisonChange Change { get; private init; }

        /// <summary>
        /// Which way the figure moved, when it moved by more than noise - what the change is labelled
        /// with.  Kept apart from <see cref="Change"/>, which is only whether that is good news: a
        /// figure that went up is "higher" whether or not higher is worse.
        /// </summary>
        public PlanComparisonTrend Trend { get; private init; }

        /// <summary>
        /// The figure is the optimiser's expectation - a cost, or memory sized from its estimates -
        /// rather than something that happened.  Better or worse is still worth showing, but as a
        /// weaker signal than a measured one: a plan built from more accurate estimates can cost more,
        /// and be granted more, and still be the plan that runs faster.
        /// </summary>
        public bool IsEstimate { get; private init; }

        /// <summary>
        /// After less before, with the relative change: "+1.2 s (3.2x higher)", "-900 ms (75% lower)".
        /// Empty when there is nothing to say - the same, or only one side.
        /// </summary>
        public string DifferenceText { get; private init; } = string.Empty;

        /// <summary>After divided by before, or null where either is missing or before is zero.</summary>
        public double? Ratio => Before is > 0 && After is { } after ? after / Before.Value : null;

        /// <summary>A number figure, or null when neither side has it - there is no row to show.</summary>
        internal static PlanComparisonMetric? Number(string group, string name, PlanComparisonUnit unit, PlanComparisonDirection direction,
            double? before, double? after, string description, double minimumDifference = 0, bool estimate = false)
        {
            if (before is null && after is null) return null;

            var change = ChangeOf(before, after, direction, unit, minimumDifference);
            return new PlanComparisonMetric(group, name, unit, direction, description)
            {
                Before = before,
                After = after,
                BeforeText = before is { } b ? Format(unit, b) : null,
                AfterText = after is { } a ? Format(unit, a) : null,
                Change = change,
                Trend = change is PlanComparisonChange.Better or PlanComparisonChange.Worse or PlanComparisonChange.Changed
                    ? after > before ? PlanComparisonTrend.Higher : PlanComparisonTrend.Lower
                    : PlanComparisonTrend.None,
                IsEstimate = estimate,
                DifferenceText = Difference(unit, before, after)
            };
        }

        /// <summary>A text figure, or null when neither side has it.</summary>
        internal static PlanComparisonMetric? Text(string group, string name, string? before, string? after, string description)
        {
            if (string.IsNullOrEmpty(before) && string.IsNullOrEmpty(after)) return null;

            return new PlanComparisonMetric(group, name, PlanComparisonUnit.Text, PlanComparisonDirection.Neutral, description)
            {
                BeforeText = string.IsNullOrEmpty(before) ? null : before,
                AfterText = string.IsNullOrEmpty(after) ? null : after,
                Change = string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    ? PlanComparisonChange.Same
                    : PlanComparisonChange.Changed
            };
        }

        /// <summary>
        /// A yes or no figure where yes is a problem - an excessive grant - or null when neither side
        /// can be judged.  Going to yes is worse and going to no better; a side that can't be judged
        /// makes it not comparable.
        /// </summary>
        internal static PlanComparisonMetric? Flag(string group, string name, bool? before, bool? after, string description)
        {
            if (before is null && after is null) return null;

            return new PlanComparisonMetric(group, name, PlanComparisonUnit.Text, PlanComparisonDirection.LowerIsBetter, description)
            {
                BeforeText = YesNo(before),
                AfterText = YesNo(after),
                Change = before is not { } b || after is not { } a
                    ? PlanComparisonChange.NotComparable
                    : b == a ? PlanComparisonChange.Same
                    : a ? PlanComparisonChange.Worse : PlanComparisonChange.Better
            };

            static string? YesNo(bool? value) => value switch
            {
                true => "Yes",
                false => "No",
                _ => null
            };
        }

        /// <summary>
        /// A change smaller than this share of the larger figure is called similar rather than
        /// better or worse: two runs of one plan never take quite the same time.
        /// </summary>
        public const double SimilarFraction = 0.05;

        private static PlanComparisonChange ChangeOf(double? before, double? after, PlanComparisonDirection direction,
            PlanComparisonUnit unit, double minimumDifference)
        {
            if (before is not { } b || after is not { } a) return PlanComparisonChange.NotComparable;

            // Compared as shown, so two figures that print the same are never called different.
            if (Format(unit, b) == Format(unit, a)) return PlanComparisonChange.Same;

            var difference = Math.Abs(a - b);
            var larger = Math.Max(Math.Abs(a), Math.Abs(b));
            // A percentage is compared in points, not relative to itself.
            var small = unit == PlanComparisonUnit.Percent
                ? difference < Math.Max(minimumDifference, 0.0001)
                : difference < minimumDifference || (larger > 0 && difference / larger < SimilarFraction);

            if (small) return PlanComparisonChange.Similar;

            return direction switch
            {
                PlanComparisonDirection.LowerIsBetter => a < b ? PlanComparisonChange.Better : PlanComparisonChange.Worse,
                PlanComparisonDirection.HigherIsBetter => a > b ? PlanComparisonChange.Better : PlanComparisonChange.Worse,
                _ => PlanComparisonChange.Changed
            };
        }

        internal static string Format(PlanComparisonUnit unit, double value) => unit switch
        {
            PlanComparisonUnit.Count => value.ToString("N0", CultureInfo.InvariantCulture),
            PlanComparisonUnit.Rows => PlanFormat.Rows(value),
            PlanComparisonUnit.Cost => PlanFormat.Cost(value),
            PlanComparisonUnit.Milliseconds => PlanFormat.Duration((long)Math.Round(value)),
            PlanComparisonUnit.Kilobytes => PlanFormat.Kilobytes((long)Math.Round(value)),
            PlanComparisonUnit.Percent => (value * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%",
            PlanComparisonUnit.Multiple => value.ToString(value >= 10 ? "N0" : "0.##", CultureInfo.InvariantCulture) + "x",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };

        private static string Difference(PlanComparisonUnit unit, double? before, double? after)
        {
            if (before is not { } b || after is not { } a) return string.Empty;
            if (Format(unit, b) == Format(unit, a)) return string.Empty;

            var delta = a - b;
            var sign = delta < 0 ? "-" : "+";

            // Points, since a relative change in a percentage is a percentage of a percentage.
            if (unit == PlanComparisonUnit.Percent)
            {
                return sign + Math.Abs(delta * 100).ToString("0.#", CultureInfo.InvariantCulture) + " pts";
            }

            var text = sign + Format(unit, Math.Abs(delta));
            return Relative(b, a) is { } relative ? text + " (" + relative + ")" : text;
        }

        /// <summary>
        /// The change as a reader would say it: "35% higher" for a modest change, "3.2x higher" or
        /// "10x lower" once it is a multiple, which is where a percentage stops being easy to read.
        /// </summary>
        internal static string? Relative(double before, double after)
        {
            if (before <= 0 || after < 0) return null;
            if (after == 0) return "100% lower";

            var ratio = after / before;
            var culture = CultureInfo.InvariantCulture;

            if (ratio >= 2) return ratio.ToString(ratio >= 10 ? "N0" : "0.#", culture) + "x higher";
            if (ratio <= 0.5)
            {
                var inverse = 1 / ratio;
                return inverse.ToString(inverse >= 10 ? "N0" : "0.#", culture) + "x lower";
            }

            var percent = (ratio - 1) * 100;
            return Math.Abs(percent).ToString(Math.Abs(percent) < 10 ? "0.#" : "0", culture) + "% " + (percent >= 0 ? "higher" : "lower");
        }

        public override string ToString() => Name + ": " + (BeforeText ?? "-") + " -> " + (AfterText ?? "-");
    }

    /// <summary>A difference worth reading first, and whether it is good news.</summary>
    public sealed class PlanComparisonHighlight
    {
        internal PlanComparisonHighlight(PlanComparisonChange change, string text, PlanComparisonMetric? metric = null)
        {
            Change = change;
            Text = text;
            Metric = metric;
        }

        public PlanComparisonChange Change { get; }

        /// <summary>The figure the highlight is about, where it is about one - for its trend and whether it is an estimate.</summary>
        public PlanComparisonMetric? Metric { get; }

        public string Text { get; }

        public override string ToString() => Text;
    }

    /// <summary>Which of the plans something appears in.</summary>
    public enum PlanPresence
    {
        Both,
        BeforeOnly,
        AfterOnly
    }

    /// <summary>What a group of operators did on one side.</summary>
    public sealed class PlanOperatorGroupFigures
    {
        private readonly List<PlanOperator> _operators = new();

        public IReadOnlyList<PlanOperator> Operators => _operators;

        public int Count => _operators.Count;

        /// <summary>The share of the statement's estimated cost these operators carry, 0 to 1.</summary>
        public double CostShare { get; private set; }

        /// <summary>Rows produced: actual on an actual plan, estimated for all executions otherwise.</summary>
        public double Rows { get; private set; }

        public double? ActualExecutions { get; private set; }

        public long? LogicalReads { get; private set; }

        public long? PhysicalReads { get; private set; }

        /// <summary>Rows examined before the predicate, where reported.</summary>
        public long? RowsRead { get; private set; }

        /// <summary>Elapsed time in these operators themselves, their inputs' taken out.</summary>
        public long? OwnElapsedMs { get; private set; }

        public long? OwnCpuMs { get; private set; }

        public int WarningCount { get; private set; }

        internal void Add(PlanOperator op, double costWeight = 1)
        {
            _operators.Add(op);
            CostShare += op.CostPercent * costWeight;
            Rows += op.RowsForDisplay;
            WarningCount += op.Warnings.Count;
            ActualExecutions = Plus(ActualExecutions, op.Runtime?.ActualExecutions);
            LogicalReads = Plus(LogicalReads, op.Runtime?.ActualLogicalReads);
            PhysicalReads = Plus(PhysicalReads, op.Runtime?.ActualPhysicalReads);
            RowsRead = Plus(RowsRead, op.Runtime?.ActualRowsRead);
            OwnElapsedMs = Plus(OwnElapsedMs, op.OwnElapsedMs);
            OwnCpuMs = Plus(OwnCpuMs, op.OwnCpuMs);
        }

        private static long? Plus(long? total, long? value) => value is { } v ? (total ?? 0) + v : total;

        private static double? Plus(double? total, long? value) => value is { } v ? (total ?? 0) + v : total;
    }

    /// <summary>
    /// A group of operators - by kind, or by the object they touch and how - on each side.
    ///
    /// Plans are not matched operator for operator.  Once the shape changes there is no reliable
    /// pairing - a join reordered moves every operator under it - and a pairing that is sometimes
    /// wrong is worse than none.  Grouping is always right, and it still answers the questions that
    /// matter: which access methods appeared and went, and where the cost and the time moved.
    /// </summary>
    public sealed class PlanOperatorGroupComparison
    {
        internal PlanOperatorGroupComparison(string op, string? objectName, string? index)
        {
            Operator = op;
            Object = objectName;
            Index = index;
        }

        public string Operator { get; }

        /// <summary>The fully qualified table, when grouped by object.</summary>
        public string? Object { get; }

        public string? Index { get; }

        /// <summary>The object with its index, for a sentence.</summary>
        public string ObjectDisplayName =>
            string.IsNullOrEmpty(Index) ? Object ?? string.Empty : Object + "." + Index;

        internal PlanOperatorGroupFigures BeforeFigures { get; } = new();

        internal PlanOperatorGroupFigures AfterFigures { get; } = new();

        /// <summary>The before side's figures, or null when the before plan has none of these.</summary>
        public PlanOperatorGroupFigures? Before => BeforeFigures.Count > 0 ? BeforeFigures : null;

        public PlanOperatorGroupFigures? After => AfterFigures.Count > 0 ? AfterFigures : null;

        public PlanPresence Status =>
            Before is null ? PlanPresence.AfterOnly : After is null ? PlanPresence.BeforeOnly : PlanPresence.Both;

        public override string ToString() => Object is null ? Operator : Operator + " " + ObjectDisplayName;
    }

    public sealed class PlanWaitComparison
    {
        internal PlanWaitComparison(string waitType, long? beforeMs, long? afterMs, long? beforeCount, long? afterCount, bool comparable)
        {
            WaitType = waitType;
            Comparable = comparable;
            BeforeMs = beforeMs;
            AfterMs = afterMs;
            BeforeCount = beforeCount;
            AfterCount = afterCount;
        }

        public string WaitType { get; }

        public long? BeforeMs { get; }

        public long? AfterMs { get; }

        public long? BeforeCount { get; }

        public long? AfterCount { get; }

        /// <summary>Both plans could record waits, so a wait missing from one side means it didn't happen.</summary>
        public bool Comparable { get; }

        /// <summary>After less before, counting a wait one side did not record as zero - null when the plans can't be compared on waits.</summary>
        public long? DifferenceMs => Comparable ? (AfterMs ?? 0) - (BeforeMs ?? 0) : null;

        /// <summary>Less waiting is better, judged with the same noise allowance as the Wait Time figure.</summary>
        public PlanComparisonChange Change =>
            DifferenceMs is not { } difference ? PlanComparisonChange.NotComparable
            : Math.Abs(difference) < PlanComparison.MillisecondsNoise ? PlanComparisonChange.Similar
            : difference < 0 ? PlanComparisonChange.Better
            : PlanComparisonChange.Worse;
    }

    public sealed class PlanParameterComparison
    {
        internal PlanParameterComparison(string name, PlanParameter? before, PlanParameter? after)
        {
            Name = name;
            Before = before;
            After = after;
        }

        public string Name { get; }

        public PlanParameter? Before { get; }

        public PlanParameter? After { get; }

        public string? DataType => After?.DataType ?? Before?.DataType;

        /// <summary>The plans were compiled for different values - the classic parameter sniffing comparison.</summary>
        public bool CompiledValueDiffers =>
            Before?.CompiledValue is { } before && After?.CompiledValue is { } after && !string.Equals(before, after, StringComparison.Ordinal);

        public bool RuntimeValueDiffers =>
            Before?.RuntimeValue is { } before && After?.RuntimeValue is { } after && !string.Equals(before, after, StringComparison.Ordinal);
    }

    public sealed class PlanWarningComparison
    {
        internal PlanWarningComparison(string title, bool isAnalysis = false)
        {
            Title = title;
            IsAnalysis = isAnalysis;
        }

        public string Title { get; }

        /// <summary>
        /// One of DBA Dash's own findings rather than a warning SQL Server put in the plan - see
        /// <see cref="PlanInsight.IsAnalysis"/>.
        /// </summary>
        public bool IsAnalysis { get; }

        /// <summary>The worst severity either side gave this warning.</summary>
        public PlanWarningSeverity Severity { get; private set; }

        public int BeforeCount { get; private set; }

        public int AfterCount { get; private set; }

        /// <summary>
        /// A warning only a run can raise - see <see cref="PlanWarning.IsRuntime"/> - or a finding
        /// only one kind of plan gives - see <see cref="PlanInsight.DependsOnPlanKind"/>.  Either
        /// way, meaningless to compare between an actual plan and an estimated one.
        /// </summary>
        public bool DependsOnPlanKind { get; private set; }

        /// <summary>
        /// Fewer is better; not comparable for a run time warning when only one plan is actual, and
        /// only changed, not better or worse, for one of DBA Dash's information notes.
        /// </summary>
        public PlanComparisonChange Change { get; internal set; }

        internal void Add(PlanWarningSeverity severity, bool dependsOnPlanKind, bool isBefore)
        {
            if (severity > Severity) Severity = severity;
            DependsOnPlanKind |= dependsOnPlanKind;
            if (isBefore) BeforeCount++;
            else AfterCount++;
        }
    }

    public sealed class PlanMissingIndexComparison
    {
        internal PlanMissingIndexComparison(PlanMissingIndex? before, PlanMissingIndex? after)
        {
            Before = before;
            After = after;
        }

        public PlanMissingIndex? Before { get; }

        public PlanMissingIndex? After { get; }

        public PlanMissingIndex Index => After ?? Before!;

        public PlanPresence Status =>
            Before is null ? PlanPresence.AfterOnly : After is null ? PlanPresence.BeforeOnly : PlanPresence.Both;
    }
}
