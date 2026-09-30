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
    /// </summary>
    public sealed class PlanComparison
    {
        private PlanComparison(PlanStatement before, PlanStatement after)
        {
            Before = before;
            After = after;
        }

        public PlanStatement Before { get; }

        public PlanStatement After { get; }

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
        public bool BothActual => Before.IsActualPlan && After.IsActualPlan;

        /// <summary>
        /// Showplan's query hash matches: the two are the same query, give or take literals.  When it
        /// does not, they may still be - a comment or a changed column list changes the hash - but it
        /// is worth saying, since comparing two different queries is rarely what was meant.
        /// </summary>
        public bool? SameQuery => HashesMatch(Before.QueryHash, After.QueryHash);

        /// <summary>Showplan's query plan hash matches: the optimiser chose the same shape both times.</summary>
        public bool? SamePlanShape => HashesMatch(Before.QueryPlanHash, After.QueryPlanHash);

        /// <summary>
        /// Compare <paramref name="before"/> with <paramref name="after"/>.  The builds are the SQL
        /// Server versions the plans came from, where the caller has them - a plan that changed
        /// across an upgrade is one of the reasons to compare.
        /// </summary>
        public static PlanComparison Compare(PlanStatement before, PlanStatement after, string? beforeBuild = null, string? afterBuild = null)
        {
            ArgumentNullException.ThrowIfNull(before);
            ArgumentNullException.ThrowIfNull(after);

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

        private static bool? HashesMatch(string? before, string? after) =>
            string.IsNullOrEmpty(before) || string.IsNullOrEmpty(after)
                ? null
                : string.Equals(before, after, StringComparison.OrdinalIgnoreCase);

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

        // Differences smaller than these are noise between two runs of the same plan, not a change.
        public const double MillisecondsNoise = 10;
        public const double KilobytesNoise = 1024;

        private static IReadOnlyList<PlanComparisonMetric> BuildMetrics(PlanStatement before, PlanStatement after, string? beforeBuild, string? afterBuild)
        {
            var metrics = new List<PlanComparisonMetric>();
            // An estimated plan can't have the warnings only a run raises, so against an actual plan
            // they would count as the actual plan's fault.  Left out of both sides when the types differ.
            var countRuntimeWarnings = before.IsActualPlan == after.IsActualPlan;
            var b = StatementFigures.For(before, countRuntimeWarnings);
            var a = StatementFigures.For(after, countRuntimeWarnings);

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
                f => f.Statement.QueryTimeStats?.ElapsedMs, "How long the statement took, start to finish.", MillisecondsNoise);
            Number(RuntimeGroup, CpuName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.QueryTimeStats?.CpuMs, "CPU used across every thread.", MillisecondsNoise);
            Number(RuntimeGroup, "CPU per Elapsed", PlanComparisonUnit.Multiple, PlanComparisonDirection.Neutral,
                f => f.CpuPerElapsed, "CPU time divided by elapsed time: roughly how many cores were busy on average.  Well under 1 is a query that spent its time waiting.");
            Number(RuntimeGroup, WaitName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.WaitMs, "The total of the waits the plan recorded - see the Waits tab for which.", MillisecondsNoise);
            Number(RuntimeGroup, "UDF Elapsed Time", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.QueryTimeStats?.UdfElapsedMs, "Time spent inside scalar user defined functions.", MillisecondsNoise);
            Number(RuntimeGroup, "UDF CPU Time", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.QueryTimeStats?.UdfCpuMs, "CPU spent inside scalar user defined functions.", MillisecondsNoise);
            Number(RuntimeGroup, "Rows Returned", PlanComparisonUnit.Rows, PlanComparisonDirection.Neutral,
                f => f.Statement.RootOperator?.ActualRows, "Rows the statement produced.  A different count usually means different parameters or data, which makes the other figures harder to compare.");

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
                f => f.Statement.MemoryGrant?.RequestedMemoryKb, "Query memory the statement asked for, sized from the optimiser's estimates.  More is not worse in itself: a grant sized from better estimates is often what stops a spill.", KilobytesNoise, estimate: true);
            Number(MemoryGroup, GrantedName, PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.MemoryGrant?.GrantedMemoryKb, "Query memory it was given - memory no other query could use meanwhile.  Whether it was the right size is Grant Used and Spills.", KilobytesNoise, estimate: true);
            Number(MemoryGroup, "Max Used", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.Neutral,
                f => f.Statement.MemoryGrant?.MaxUsedMemoryKb, "The most of the grant that was used.", KilobytesNoise);
            // Neither way is better: low is memory reserved for nothing, high is little headroom before a
            // spill, and anywhere between is fine.  The two ends that are a problem are judged on their
            // own - an excessive grant below, and a spill by Spills.
            Number(MemoryGroup, GrantUsedName, PlanComparisonUnit.Percent, PlanComparisonDirection.Neutral,
                f => f.Statement.MemoryGrant?.GrantUsedFraction,
                "The share of the grant that was used.  Neither higher nor lower is better in itself: very low on a large grant is memory reserved for nothing (see Excessive Grant), and very high leaves little headroom before a spill (see Spills).", 0.05);

            var excessive = PlanComparisonMetric.Flag(MemoryGroup, ExcessiveGrantName,
                b.ExcessiveGrant, a.ExcessiveGrant,
                "A large grant that went mostly unused: at least " + PlanFormat.Kilobytes(PlanMemoryGrantInfo.ExcessiveGrantMinKb) +
                " granted and under " + PlanFormat.Percent(PlanMemoryGrantInfo.ExcessiveGrantMaxUsedFraction) +
                " of it used - the same rule as the viewer's excessive grant warning.  Memory reserved that other queries could not have.");
            if (excessive is not null) metrics.Add(excessive);
            Number(MemoryGroup, "Desired", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.Neutral,
                f => f.Statement.MemoryGrant?.EffectiveDesiredMemoryKb, "What the optimiser wanted, from its estimates.", KilobytesNoise, estimate: true);
            Number(MemoryGroup, GrantWaitName, PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.MemoryGrant?.GrantWaitTimeMs, "Time spent waiting for the grant (RESOURCE_SEMAPHORE).", MillisecondsNoise);
            Number(MemoryGroup, SpillsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.Spills, "Operators that ran out of memory and spilled to tempdb.");

            Number(EstimatesGroup, CostName, PlanComparisonUnit.Cost, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.StatementSubTreeCost, "The optimiser's cost for the statement.  A model figure, not a time: it says what the optimiser expected, which is not always what happened.  Lower is better, but a plan built from more accurate estimates can cost more and still run faster.", estimate: true);
            Number(EstimatesGroup, "Estimated Rows", PlanComparisonUnit.Rows, PlanComparisonDirection.Neutral,
                f => f.Statement.StatementEstRows, "Rows the optimiser expected the statement to return.");
            Number(EstimatesGroup, "Worst Row Estimate", PlanComparisonUnit.Multiple, PlanComparisonDirection.LowerIsBetter,
                f => f.WorstEstimateError, "The furthest any operator's row estimate was out, either way.  Bad estimates are the usual reason the optimiser chose a poor plan.");
            Text(EstimatesGroup, "Cardinality Estimator", f => f.Statement.CardinalityEstimationModelVersion,
                "The cardinality estimator version - a change here, from a compatibility level or a hint, changes estimates across the plan.");

            Number(ParallelismGroup, DopName, PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Statement.DegreeOfParallelism, "The degree of parallelism the plan was built for.");
            Number(ParallelismGroup, "Threads Reserved", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Statement.ReservedThreads, "Worker threads reserved for the parallel branches.");
            Number(ParallelismGroup, "Threads Used", PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.Statement.UsedThreads, "Worker threads that were used.");
            Text(ParallelismGroup, "Non-Parallel Reason", f => f.Statement.NonParallelPlanReason,
                "Why the optimiser could not use a parallel plan, where it said.");

            Number(ShapeGroup, OperatorsName, PlanComparisonUnit.Count, PlanComparisonDirection.Neutral,
                f => f.OperatorCount, "Operators in the plan.");
            Number(ShapeGroup, WarningsName, PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.WarningCount, "Warnings SQL Server put on the statement and its operators - listed, with DBA Dash's own findings, on the Insights tab." +
                                     (countRuntimeWarnings ? string.Empty : "  Not counting the warnings only a run can raise - spills, grant warnings, waits - since one plan is estimated."));
            Number(ShapeGroup, "Missing Indexes", PlanComparisonUnit.Count, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.MissingIndexes.Count, "Indexes the optimiser said it wanted.");
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
                f => f.Statement.CompileTimeMs, "Time taken to compile the plan.", MillisecondsNoise);
            Number(CompilationGroup, "Compile CPU", PlanComparisonUnit.Milliseconds, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.CompileCpuMs, "CPU taken to compile the plan.", MillisecondsNoise);
            Number(CompilationGroup, "Compile Memory", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.CompileMemoryKb, "Memory used to compile the plan.", KilobytesNoise);
            Number(CompilationGroup, "Cached Plan Size", PlanComparisonUnit.Kilobytes, PlanComparisonDirection.LowerIsBetter,
                f => f.Statement.CachedPlanSizeKb, "The plan's size in the plan cache.");
            Text(CompilationGroup, "Optimisation Level", f => f.Statement.OptimisationLevel,
                "TRIVIAL or FULL.  A trivial plan was never costed against alternatives.");
            Text(CompilationGroup, "Early Abort Reason", f => f.Statement.OptimisationEarlyAbortReason,
                "Why the optimiser stopped searching early, where it did - a time out means it may have missed a better plan.");

            Text(IdentityGroup, "Plan Type", f => f.Statement.IsActualPlan ? "Actual" : "Estimated",
                "Actual plans carry run time figures; estimated plans only the optimiser's expectations.");
            Text(IdentityGroup, "Statement Type", f => f.Statement.StatementType, "SELECT, INSERT, UPDATE and the rest.");
            Text(IdentityGroup, "Query Hash", f => f.Statement.QueryHash,
                "Identifies the query, give or take literals.  Different hashes usually mean different queries.");
            Text(IdentityGroup, "Query Plan Hash", f => f.Statement.QueryPlanHash,
                "Identifies the plan's shape.  The same hash means the optimiser chose the same plan.");
            var build = PlanComparisonMetric.Text(IdentityGroup, "SQL Server Build", beforeBuild, afterBuild, "The SQL Server version the plan came from.");
            if (build is not null) metrics.Add(build);

            return metrics;
        }

        /// <summary>The statement level figures that have to be worked out from the operators, worked out once.</summary>
        private sealed class StatementFigures
        {
            public required PlanStatement Statement { get; init; }
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
            public double? WorstEstimateError { get; init; }
            public int OperatorCount { get; init; }
            public int WarningCount { get; init; }
            public int Scans { get; init; }
            public int Seeks { get; init; }
            public int Lookups { get; init; }
            public int Sorts { get; init; }
            public int BatchModeOperators { get; init; }

            public static StatementFigures For(PlanStatement statement, bool countRuntimeWarnings)
            {
                var operators = statement.Operators.ToList();
                var runtime = operators.Where(o => o.Runtime is not null).Select(o => o.Runtime!).ToList();
                var actual = statement.IsActualPlan;
                var warnings = statement.AllWarnings.ToList();
                var time = statement.QueryTimeStats;

                return new StatementFigures
                {
                    Statement = statement,
                    CpuPerElapsed = time is { CpuMs: { } cpu, ElapsedMs: > 0 } ? cpu / (double)time.ElapsedMs.Value : null,
                    // Wait stats came to showplan with the time stats (SQL Server 2016 SP1), so an actual
                    // plan without time stats is one from before that - no figure, rather than no waits.
                    WaitMs = actual && time is not null ? statement.WaitStats.Sum(w => w.WaitTimeMs) : null,
                    LogicalReads = SumOf(runtime, r => r.ActualLogicalReads),
                    PhysicalReads = SumOf(runtime, r => r.ActualPhysicalReads),
                    ReadAheads = SumOf(runtime, r => r.ActualReadAheads),
                    LobLogicalReads = SumOf(runtime, r => r.ActualLobLogicalReads),
                    RowsRead = SumOf(runtime, r => r.ActualRowsRead),
                    // Only a run can spill, so an estimated plan has no figure rather than a zero.
                    Spills = actual ? warnings.Count(w => w.IsSpill) : null,
                    ExcessiveGrant = statement.MemoryGrant is { GrantUsedFraction: not null } grant ? grant.IsExcessive : null,
                    WorstEstimateError = operators.Select(o => o.RowEstimateError).Max(),
                    OperatorCount = operators.Count,
                    WarningCount = warnings.Count(w => countRuntimeWarnings || !w.IsRuntime),
                    Scans = operators.Count(o => IsScan(o.Kind)),
                    Seeks = operators.Count(o => IsSeek(o.Kind) && !o.IsLookup),
                    Lookups = operators.Count(o => o.IsLookup || o.Kind is PlanOperatorKind.KeyLookup or PlanOperatorKind.RidLookup),
                    Sorts = operators.Count(o => o.Kind is PlanOperatorKind.Sort or PlanOperatorKind.TopSort),
                    BatchModeOperators = operators.Count(o => string.Equals(o.EstimatedExecutionMode, "Batch", StringComparison.OrdinalIgnoreCase))
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

        private static IReadOnlyList<PlanOperatorGroupComparison> GroupOperators(PlanStatement before, PlanStatement after, bool byObject)
        {
            var groups = new Dictionary<string, PlanOperatorGroupComparison>(StringComparer.OrdinalIgnoreCase);

            void Add(PlanStatement statement, bool isBefore)
            {
                foreach (var op in statement.Operators)
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

                    (isBefore ? group.BeforeFigures : group.AfterFigures).Add(op);
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

        private static IReadOnlyList<PlanWaitComparison> CompareWaits(PlanStatement before, PlanStatement after)
        {
            var beforeWaits = before.WaitStats.GroupBy(w => w.WaitType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (Ms: g.Sum(w => w.WaitTimeMs), Count: g.Sum(w => w.WaitCount)), StringComparer.OrdinalIgnoreCase);
            var afterWaits = after.WaitStats.GroupBy(w => w.WaitType, StringComparer.OrdinalIgnoreCase)
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

        private static IReadOnlyList<PlanParameterComparison> CompareParameters(PlanStatement before, PlanStatement after)
        {
            var names = before.Parameters.Select(p => p.Name)
                .Concat(after.Parameters.Select(p => p.Name))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return names
                .Select(name => new PlanParameterComparison(
                    name,
                    before.Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)),
                    after.Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        private static IReadOnlyList<PlanWarningComparison> CompareWarnings(PlanStatement before, PlanStatement after)
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

            void Add(PlanStatement statement, bool isBefore)
            {
                foreach (var warning in statement.AllWarnings)
                {
                    Group(false, warning.Title).Add(warning.Severity, warning.IsRuntime, isBefore);
                }

                // DBA Dash's own findings too, as the viewer lists them beside the warnings - a scalar
                // UDF or an optimizer time out gone from the new plan is as much the answer as a spill
                // gone.  Not the note that a plan is estimated, which is about the plan, not the query.
                foreach (var insight in PlanInsights.AnalysisFor(statement).Where(i => i.Title != PlanInsights.EstimatedPlanTitle))
                {
                    Group(true, insight.Title!).Add(insight.Severity, insight.DependsOnPlanKind, isBefore);
                }
            }

            Add(before, isBefore: true);
            Add(after, isBefore: false);

            // A warning only a run can raise, against a plan that never ran, says nothing either way.
            var sameType = before.IsActualPlan == after.IsActualPlan;
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

        private static IReadOnlyList<PlanMissingIndexComparison> CompareMissingIndexes(PlanStatement before, PlanStatement after)
        {
            // Matched on the index they describe, which is the recommendation - the impact is the
            // optimiser's guess for that plan and differs even for the same index.
            var beforeIndexes = before.MissingIndexes.GroupBy(i => i.CreateStatementOneLine, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var afterIndexes = after.MissingIndexes.GroupBy(i => i.CreateStatementOneLine, StringComparer.OrdinalIgnoreCase)
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

        private static IReadOnlyList<PlanComparisonHighlight> BuildHighlights(PlanComparison comparison)
        {
            var highlights = new List<PlanComparisonHighlight>();
            var before = comparison.Before;
            var after = comparison.After;

            if (comparison.SameQuery == false)
            {
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.Changed,
                    "The query hashes differ - these may be different queries, so compare with care."));
            }

            if (!before.IsActualPlan || !after.IsActualPlan)
            {
                var estimated = !before.IsActualPlan && !after.IsActualPlan
                    ? "Both plans are estimated"
                    : (before.IsActualPlan ? "The after plan" : "The before plan") + " is estimated";
                highlights.Add(new PlanComparisonHighlight(PlanComparisonChange.NotComparable,
                    estimated + ", so run time figures can't be compared - only the optimiser's estimates."));
            }

            highlights.Add(comparison.SamePlanShape switch
            {
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
                    ? ExcessiveGrantText(metric.Change, before.MemoryGrant!, after.MemoryGrant!)
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

        internal void Add(PlanOperator op)
        {
            _operators.Add(op);
            CostShare += op.CostPercent;
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
