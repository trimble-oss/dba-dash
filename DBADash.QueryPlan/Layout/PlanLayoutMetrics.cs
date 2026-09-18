using System;
using System.Collections.Generic;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// What the node bar and the heat colouring measure.
    ///
    /// Cost is what SSMS shows and it is the right default, but it is the optimiser's guess - on an
    /// actual plan the measured figures are the ones worth ranking by, and which of them matters
    /// depends on the question.  A plan where the expensive operator by cost and the slow operator
    /// by elapsed time are different nodes is the interesting case, and the only way to see it is to
    /// be able to switch.
    /// </summary>
    public enum PlanHeatMetric
    {
        /// <summary>The optimiser's cost for the operator alone.</summary>
        OperatorCost,

        /// <summary>Rows produced - actual where measured, estimated otherwise.</summary>
        Rows,

        /// <summary>CPU burned in the operator, across its threads.  Actual plans only.</summary>
        Cpu,

        /// <summary>Wall clock time in the operator.  Actual plans only.</summary>
        Elapsed,

        /// <summary>Logical reads.  Actual plans only.</summary>
        LogicalReads,

        /// <summary>
        /// How far the actual row count was from the estimate, in either direction.  Actual plans
        /// only.
        /// </summary>
        EstimateError
    }

    /// <summary>
    /// The largest value of each metric anywhere in the statement, so a node can be drawn in
    /// proportion to the worst one.
    ///
    /// Scaled against the maximum rather than the total: a plan with one dominant operator and
    /// thirty trivial ones is the normal case, and against the total every bar but one would be
    /// invisible.  The reader is asking "which is the expensive one", and the answer is a full bar
    /// on that node.
    /// </summary>
    public sealed class PlanLayoutMetrics
    {
        internal PlanLayoutMetrics(
            PlanStatement statement,
            IEnumerable<PlanOperator> operators,
            OperatorTimeMode timeMode = OperatorTimeMode.Own)
        {
            StatementCost = statement.StatementSubTreeCost;
            TimeMode = timeMode;

            foreach (var node in operators)
            {
                MaxOperatorCost = Math.Max(MaxOperatorCost, node.OperatorCost);
                MaxRows = Math.Max(MaxRows, node.RowsForDisplay);
                MaxDataSize = Math.Max(MaxDataSize, node.DataSizeForDisplay);
                MaxEstimatedRows = Math.Max(MaxEstimatedRows, node.EstimatedTotalRows);
                MaxEstimatedDataSize = Math.Max(MaxEstimatedDataSize, node.EstimatedTotalRows * (node.AvgRowSize ?? 0));

                if (node.Runtime is not { } runtime) continue;

                HasRuntime = true;
                if (node.CpuMs(timeMode) is { } cpu) MaxCpuMs = Math.Max(MaxCpuMs, cpu);
                if (node.ElapsedMs(timeMode) is { } elapsed) MaxElapsedMs = Math.Max(MaxElapsedMs, elapsed);
                if (runtime.ActualLogicalReads is { } reads) MaxLogicalReads = Math.Max(MaxLogicalReads, reads);
                if (node.RowEstimateError is { } error) MaxEstimateError = Math.Max(MaxEstimateError, error);
            }
        }

        public double StatementCost { get; }

        /// <summary>
        /// Which operator times the CPU and elapsed bars measure.  With the times as reported, a
        /// row mode operator carries its inputs' time too, so the bars grow towards the root.
        /// </summary>
        public OperatorTimeMode TimeMode { get; }

        public double MaxOperatorCost { get; }

        public double MaxRows { get; }

        /// <summary>The most bytes any one operator hands on - see <see cref="PlanOperator.DataSizeForDisplay"/>.</summary>
        public double MaxDataSize { get; }

        /// <summary>
        /// The most rows the optimiser expected any one operator to hand on, over all the executions
        /// it expected - see <see cref="PlanOperator.EstimatedTotalRows"/>.  The same as
        /// <see cref="MaxRows"/> on an estimated plan.
        /// </summary>
        public double MaxEstimatedRows { get; }

        /// <summary><see cref="MaxEstimatedRows"/> in bytes, by the estimated row size.</summary>
        public double MaxEstimatedDataSize { get; }

        public long MaxCpuMs { get; }

        public long MaxElapsedMs { get; }

        public long MaxLogicalReads { get; }

        public double MaxEstimateError { get; }

        /// <summary>True when the statement carries measurements, so the measured metrics are live.</summary>
        public bool HasRuntime { get; }

        /// <summary>
        /// Whether a metric can be shown at all.  An estimated plan has no CPU to rank by, and
        /// offering the choice would be offering an empty picture.
        /// </summary>
        public bool Supports(PlanHeatMetric metric) => metric switch
        {
            PlanHeatMetric.OperatorCost => MaxOperatorCost > 0,
            PlanHeatMetric.Rows => MaxRows > 0,
            PlanHeatMetric.Cpu => MaxCpuMs > 0,
            PlanHeatMetric.Elapsed => MaxElapsedMs > 0,
            PlanHeatMetric.LogicalReads => MaxLogicalReads > 0,
            PlanHeatMetric.EstimateError => MaxEstimateError > 1,
            _ => false
        };

        /// <summary>
        /// An operator's share of the worst value of <paramref name="metric"/>, from 0 to 1.  Zero
        /// when the metric is not available, which draws as an empty bar rather than a wrong one.
        /// </summary>
        public double Fraction(PlanOperator? node, PlanHeatMetric metric)
        {
            if (node is null) return 0;

            return metric switch
            {
                PlanHeatMetric.OperatorCost => Share(node.OperatorCost, MaxOperatorCost),
                PlanHeatMetric.Rows => Share(node.RowsForDisplay, MaxRows),
                PlanHeatMetric.Cpu => Share(node.CpuMs(TimeMode) ?? 0, MaxCpuMs),
                PlanHeatMetric.Elapsed => Share(node.ElapsedMs(TimeMode) ?? 0, MaxElapsedMs),
                PlanHeatMetric.LogicalReads => Share(node.Runtime?.ActualLogicalReads ?? 0, MaxLogicalReads),
                PlanHeatMetric.EstimateError => Share(node.RowEstimateError ?? 0, MaxEstimateError),
                _ => 0
            };
        }

        /// <summary>
        /// The caption for a metric, for a toolbar or a legend.
        /// </summary>
        public static string NameOf(PlanHeatMetric metric) => metric switch
        {
            PlanHeatMetric.OperatorCost => "Estimated cost",
            PlanHeatMetric.Rows => "Rows",
            PlanHeatMetric.Cpu => "CPU time",
            PlanHeatMetric.Elapsed => "Elapsed time",
            PlanHeatMetric.LogicalReads => "Logical reads",
            PlanHeatMetric.EstimateError => "Estimate error",
            _ => metric.ToString()
        };

        /// <summary>
        /// Metrics span orders of magnitude within one plan, so the bars are scaled
        /// logarithmically.  On a linear scale a plan with one operator at ten million rows draws
        /// every other bar as a hairline, which says only "this one is biggest" - information the
        /// reader already had from the arrow.  A log scale keeps the ranking readable all the way
        /// down.
        /// </summary>
        private static double Share(double value, double max)
        {
            if (value <= 0 || max <= 0) return 0;
            if (value >= max) return 1;

            return Math.Log10(1 + value) / Math.Log10(1 + max);
        }
    }
}
