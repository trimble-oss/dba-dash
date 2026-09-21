using System;
using System.Collections.Generic;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// One operator in a query plan, and its children.
    ///
    /// The tree mirrors showplan's RelOp nesting: a parent's children are its inputs, and rows flow
    /// from the children up into the parent.  That direction is worth stating because the picture
    /// draws it the other way round - the root ends up on the left, and the arrows point left, into
    /// the parent - so "child" here is always the operator producing rows, never the one below it on
    /// screen.
    ///
    /// Everything expensive to derive is computed once by the parser and stored, because the layout
    /// and the renderer ask for it repeatedly.
    /// </summary>
    public sealed class PlanOperator
    {
        /// <summary>
        /// Showplan's own id for the operator, unique within a statement.  The synthetic root gets
        /// -1, since SQL Server never emits one.
        /// </summary>
        public int NodeId { get; internal set; }

        /// <summary>The physical operator, exactly as showplan wrote it.</summary>
        public string? PhysicalOp { get; internal set; }

        /// <summary>
        /// The logical operation - Inner Join, Aggregate, Distribute Streams.  Often the more useful
        /// of the two, and the thing that says which job a dual purpose operator is doing.
        /// </summary>
        public string? LogicalOp { get; internal set; }

        public PlanOperatorKind Kind { get; internal set; }

        public PlanOperatorCategory Category => PlanOperatorClassifier.CategoryOf(Kind);

        /// <summary>The caption - see <see cref="PlanOperatorNames"/> for where it differs from
        /// <see cref="PhysicalOp"/>.</summary>
        public string DisplayName => PlanOperatorNames.For(Kind, PhysicalOp);

        /// <summary>
        /// The inputs, in showplan order.  For a join the first is the outer (build, or driving)
        /// input and the second the inner - an order that carries meaning, so it is preserved rather
        /// than sorted.
        /// </summary>
        public IReadOnlyList<PlanOperator> Children { get; internal set; } = [];

        /// <summary>
        /// The operator this one feeds, or null at the root.  Set by the parser so the layout can
        /// walk upwards and the viewer can offer keyboard navigation.
        /// </summary>
        public PlanOperator? Parent { get; internal set; }

        /// <summary>
        /// How deep in the tree, root at zero.  Drives the column the node is placed in.
        /// </summary>
        public int Depth { get; internal set; }

        // ---------------------------------------------------------------- estimates

        public double EstimateRows { get; internal set; }

        /// <summary>
        /// What the estimate would have been without a row goal, where SQL Server reported it.  A
        /// large gap means a row goal is in play, which is a common cause of an optimistic plan.
        /// </summary>
        public double? EstimateRowsWithoutRowGoal { get; internal set; }

        public double EstimateIO { get; internal set; }

        public double EstimateCPU { get; internal set; }

        public double? EstimateRebinds { get; internal set; }

        public double? EstimateRewinds { get; internal set; }

        /// <summary>Row or Batch, as the optimiser expected.</summary>
        public string? EstimatedExecutionMode { get; internal set; }

        /// <summary>
        /// The optimiser's cost for this operator and everything feeding it.  Cost is a unitless
        /// model figure, not a time - useful for comparing operators within one plan and close to
        /// meaningless between plans.
        /// </summary>
        public double EstimatedTotalSubtreeCost { get; internal set; }

        public double? AvgRowSize { get; internal set; }

        /// <summary>True when the operator ran inside a parallel branch.</summary>
        public bool IsParallel { get; internal set; }

        public bool IsPartitioned { get; internal set; }

        /// <summary>
        /// A seek fetching columns its index did not cover, which is what makes it a key lookup.
        /// </summary>
        public bool IsLookup { get; internal set; }

        /// <summary>
        /// True for an ordered scan or seek.  Worth knowing because an ordered scan is what lets a
        /// later operator avoid a sort, and losing it is how adding an index can make things worse.
        /// </summary>
        public bool? IsOrdered { get; internal set; }

        // ---------------------------------------------------------------- actuals

        /// <summary>
        /// What actually happened, or null on an estimated plan.  See
        /// <see cref="PlanRuntimeCounters"/> for why this is null rather than zeroed.
        /// </summary>
        public PlanRuntimeCounters? Runtime { get; internal set; }

        /// <summary>True when this plan carries measurements rather than only estimates.</summary>
        public bool HasRuntime => Runtime is not null;

        /// <summary>
        /// Elapsed time in this operator alone, with the time of its inputs taken out - see
        /// <see cref="PlanOperatorTiming"/>.  The slowest thread's, like the reported figure.  Null
        /// when the operator has no timings.
        /// </summary>
        public long? OwnElapsedMs { get; internal set; }

        /// <summary>CPU in this operator alone, across all its threads.  Null when not measured.</summary>
        public long? OwnCpuMs { get; internal set; }

        /// <summary>Elapsed time as <paramref name="mode"/> defines it.</summary>
        public long? ElapsedMs(OperatorTimeMode mode) =>
            mode == OperatorTimeMode.Own ? OwnElapsedMs : Runtime?.ActualElapsedMs;

        /// <summary>CPU time as <paramref name="mode"/> defines it.</summary>
        public long? CpuMs(OperatorTimeMode mode) =>
            mode == OperatorTimeMode.Own ? OwnCpuMs : Runtime?.ActualCpuMs;

        // ---------------------------------------------------------------- what it touches

        /// <summary>
        /// The objects the operator reads or writes.  Usually none or one; a Clustered Update names
        /// every index it maintains, which is exactly the detail that explains why an update is slow.
        /// </summary>
        public IReadOnlyList<PlanObjectReference> Objects { get; internal set; } = [];

        /// <summary>The first object, which is the one the caption shows.</summary>
        public PlanObjectReference? PrimaryObject => Objects.Count > 0 ? Objects[0] : null;

        public IReadOnlyList<PlanColumnReference> OutputList { get; internal set; } = [];

        /// <summary>
        /// The residual predicate, applied to every row the operator produced.  A predicate on a
        /// scan is rows read and thrown away.
        /// </summary>
        public string? Predicate { get; internal set; }

        /// <summary>
        /// The seek predicate, which is the part that actually navigated the index.  Distinct from
        /// <see cref="Predicate"/> because the difference between the two is the difference between
        /// an index doing its job and an index being read end to end.
        /// </summary>
        public string? SeekPredicate { get; internal set; }

        /// <summary>
        /// The values this operator works out and names - see <see cref="PlanExpression"/>.  The
        /// statement holds all of them together; this is the ones that happen here, which is what
        /// says whether a name on this operator's output list is one it computed or one it was
        /// handed.
        /// </summary>
        public IReadOnlyList<PlanExpression> DefinedExpressions { get; internal set; } = [];

        /// <summary>
        /// Everything else showplan said about this operator, as a tree for the properties panel.
        /// </summary>
        public IReadOnlyList<PlanProperty> Properties { get; internal set; } = [];

        public IReadOnlyList<PlanWarning> Warnings { get; internal set; } = [];

        public bool HasWarnings => Warnings.Count > 0;

        // ---------------------------------------------------------------- derived

        /// <summary>
        /// The cost of this operator alone: its subtree cost less its children's.
        ///
        /// This is the figure worth ranking operators by, and it is not in the plan - showplan only
        /// gives subtree costs, so the root always appears to cost 100% and the expensive operator
        /// hides inside it.  Clamped at zero because the subtraction can go very slightly negative on
        /// rounding.
        /// </summary>
        public double OperatorCost { get; internal set; }

        /// <summary>
        /// <see cref="OperatorCost"/> as a share of the statement's total, from 0 to 1.  Set by the
        /// parser once the statement total is known.
        /// </summary>
        public double CostPercent { get; internal set; }

        /// <summary>
        /// <see cref="EstimatedTotalSubtreeCost"/> as a share of the statement's total, from 0 to 1.
        /// What SSMS puts on the node.
        /// </summary>
        public double SubtreeCostPercent { get; internal set; }

        /// <summary>
        /// The estimate scaled up to the number of times the operator actually ran, which is what
        /// the actual row count has to be compared against.
        ///
        /// Comparing actual rows with the raw per-execution estimate is the classic misreading of a
        /// plan: the inner side of a nested loop that ran ten thousand times is not wrong by four
        /// orders of magnitude, it ran ten thousand times.  Null when there are no actuals to scale
        /// against.
        ///
        /// Scaled by <see cref="PlanRuntimeCounters.LogicalExecutions"/> rather than the raw
        /// execution count - see there for why a parallel operator must not be multiplied by its
        /// thread count.
        /// </summary>
        public double? EstimateRowsAllExecutions =>
            Runtime is null ? null : EstimateRows * Math.Max(1, Runtime.LogicalExecutions);

        /// <summary>
        /// Actual rows divided by estimated rows for the same number of executions.  1.0 is a
        /// perfect estimate, 100 means the optimiser expected a hundredth of what arrived, 0.01 the
        /// reverse.  Null on an estimated plan, and null when the estimate was zero - there is no
        /// meaningful ratio to a zero estimate, and reporting infinity would rank every trivial
        /// operator above the real problem.
        /// </summary>
        public double? RowEstimateRatio
        {
            get
            {
                if (Runtime is null) return null;
                if (EstimateRowsAllExecutions is not { } estimated || estimated <= 0) return null;

                return Runtime.ActualRows / estimated;
            }
        }

        /// <summary>
        /// How far the row estimate was out, as a multiple of at least one whichever way it went, so
        /// an over-estimate and an under-estimate of the same size compare.  Null on an estimated
        /// plan, where there is nothing to compare against.
        ///
        /// Unlike <see cref="RowEstimateRatio"/>, the ends where one of the counts is zero are
        /// measured rather than dropped: an operator that returned nothing against an estimate of a
        /// million is the largest over-estimate a plan can hold, not an absent one, and an estimate
        /// of zero against a million rows is its mirror.  The ratio keeps its nulls because the
        /// properties panel reads it as a ratio and can say "no rows returned"; this is the figure
        /// anything ranking or badging operators wants.
        /// </summary>
        public double? RowEstimateError =>
            Runtime is not null && EstimateRowsAllExecutions is { } estimated
                ? EstimateErrorMultiple(Runtime.ActualRows, estimated)
                : null;

        /// <summary>
        /// How far an estimate was out, as a multiple of at least one either way.  Counts below one
        /// row are taken as one at both ends: an estimate of 0.3 rows against one actual row is not
        /// a threefold error worth colouring, and a zero at either end would otherwise be either
        /// infinitely wrong or not wrong at all.
        ///
        /// One rule, shared, because the node badge, the heat metric and the arrow colour all use
        /// the same thresholds and have to agree about what they are applied to.
        /// </summary>
        internal static double EstimateErrorMultiple(double actual, double estimated)
        {
            var measured = Math.Max(actual, 1);
            var expected = Math.Max(estimated, 1);

            // The larger over the smaller, rather than a ratio and its reciprocal: dividing 1 by
            // 0.00001 gives 99999.99999999999, and a hundred thousand times out should say so.
            return measured >= expected ? measured / expected : expected / measured;
        }

        /// <summary>
        /// How many times the optimiser expected the operator to run: once, plus the rebinds and
        /// rewinds it expected on the inner side of a loop.  SSMS's "Estimated Number of Executions".
        /// </summary>
        public double EstimatedExecutions => 1 + (EstimateRebinds ?? 0) + (EstimateRewinds ?? 0);

        /// <summary>
        /// The rows the optimiser expected across every execution it expected - SSMS's "Estimated
        /// Number of Rows for All Executions".
        ///
        /// What an estimated arrow has to carry to be comparable with an actual one, which counts
        /// every row of every execution.  <see cref="EstimateRows"/> is per execution, so on the
        /// inner side of a loop it draws a hairline beside a thick actual arrow that was estimated
        /// perfectly well.  Different from <see cref="EstimateRowsAllExecutions"/>, which scales by
        /// the executions that actually happened, to measure the estimate's error.
        /// </summary>
        public double EstimatedTotalRows => EstimateRows * EstimatedExecutions;

        /// <summary>
        /// The rows the operator actually handed on: measured, or - where SQL Server left it
        /// unmeasured - taken from its input when it cannot have changed the count.  Null on an
        /// estimated plan.
        ///
        /// A Compute Scalar whose expressions are deferred to the operator that uses them gets no
        /// runtime counters at all, and it is the only operator that turns up unmeasured in actual
        /// plans.  It returns exactly the rows it reads, so its input's count is its own - and
        /// without it every arrow out of one would have no actual figure to draw or compare.  A
        /// chain of them is followed down to the first operator that measured something.
        /// </summary>
        public double? ActualRows => Runtime?.ActualRows ?? InferredActualRows;

        /// <summary>
        /// True when <see cref="ActualRows"/> came from the input rather than from this operator's
        /// own counters, so it can be shown as inferred rather than as measured.
        /// </summary>
        public bool IsActualRowsInferred => Runtime is null && InferredActualRows is not null;

        private double? InferredActualRows =>
            Kind == PlanOperatorKind.ComputeScalar && Children.Count == 1 ? Children[0].ActualRows : null;

        /// <summary>
        /// The rows the arrow into the parent represents: actual where known, the estimate for all
        /// the expected executions otherwise.  One property so the layout does not have to care
        /// which kind of plan it has.
        /// </summary>
        public double RowsForDisplay => ActualRows ?? EstimatedTotalRows;

        /// <summary>
        /// Bytes this operator hands to its parent: <see cref="RowsForDisplay"/> times the
        /// estimated row size.
        ///
        /// On an actual plan this multiplies measured rows by an estimated size - showplan never
        /// reports an actual row size - which is still the best figure available, and far closer
        /// to the truth than the estimated row count would be.  Zero when the plan gave no row size.
        /// </summary>
        public double DataSizeForDisplay => RowsForDisplay * (AvgRowSize ?? 0);

        /// <summary>
        /// Rows read but discarded by the residual predicate, where SQL Server reported both.  This
        /// is work the query paid for and threw away.
        /// </summary>
        public long? RowsDiscarded =>
            Runtime?.ActualRowsRead is { } read && read > Runtime.ActualRows
                ? read - Runtime.ActualRows
                : null;

        /// <summary>Depth-first walk of this operator and everything under it, parents first.</summary>
        public IEnumerable<PlanOperator> DescendantsAndSelf()
        {
            yield return this;

            foreach (var child in Children)
            {
                foreach (var descendant in child.DescendantsAndSelf()) yield return descendant;
            }
        }

        public override string ToString() =>
            PrimaryObject is { } target ? DisplayName + " " + target.ShortName : DisplayName;
    }
}
